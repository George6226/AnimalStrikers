#!/usr/bin/env bash
# GoapDiag の MoveToward transformPos から「全員（ほぼ）停止」を検出する。
#
# Usage:
#   analyze-locomotion-freeze-log.sh [GoapDiag_latest.txt]
#
# Exit:
#   0 = PASS（または Diag 不足で SKIP）
#   1 = FAIL（連続停止が閾値超え）
#
# 環境変数（goap-play-gate-config.sh からも設定可）:
#   GOAP_FREEZE_WINDOW_SEC          変位計測窓（既定 5）
#   GOAP_FREEZE_DISP_THRESH         窓内変位の停止判定（既定 0.2）
#   GOAP_FREEZE_MIN_STUCK_ACTORS    停止とみなす人数（既定 5）
#   GOAP_FREEZE_MIN_ACTIVE_ACTORS   窓に十分なサンプルがある人数（既定 4）
#   GOAP_FREEZE_MAX_CONTINUOUS_SEC  連続停止の FAIL 閾値秒（既定 8・必殺中の短停止を許容）
#   GOAP_FREEZE_REQUIRE_DIAG        1 なら Diag 不足を FAIL（既定 0=SKIP）
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
if [[ -f "${SCRIPT_DIR}/goap-play-gate-config.sh" ]]; then
  # shellcheck source=goap-play-gate-config.sh
  source "${SCRIPT_DIR}/goap-play-gate-config.sh"
fi

DIAG="${1:-${PROJECT_ROOT}/Assets/DebugLog/GoapDiag_latest.txt}"
WINDOW_SEC="${GOAP_FREEZE_WINDOW_SEC:-5}"
DISP_THRESH="${GOAP_FREEZE_DISP_THRESH:-0.2}"
MIN_STUCK="${GOAP_FREEZE_MIN_STUCK_ACTORS:-5}"
MIN_ACTIVE="${GOAP_FREEZE_MIN_ACTIVE_ACTORS:-4}"
MAX_CONTINUOUS="${GOAP_FREEZE_MAX_CONTINUOUS_SEC:-8}"
REQUIRE_DIAG="${GOAP_FREEZE_REQUIRE_DIAG:-0}"

echo "========================================"
echo " 移動フリーズ検出（GoapDiag 座標）"
echo "========================================"
echo "diag: ${DIAG}"
echo "window=${WINDOW_SEC}s disp<${DISP_THRESH} stuck>=${MIN_STUCK}/${MIN_ACTIVE} fail>=${MAX_CONTINUOUS}s"
echo ""

if [[ ! -f "${DIAG}" ]]; then
  echo "  ⬜ GoapDiag なし — スキップ（Verbose ログが必要）"
  if [[ "${REQUIRE_DIAG}" == "1" ]]; then
    echo "  ❌ GOAP_FREEZE_REQUIRE_DIAG=1 のため FAIL"
    exit 1
  fi
  exit 0
fi

export GOAP_FREEZE_WINDOW_SEC="${WINDOW_SEC}"
export GOAP_FREEZE_DISP_THRESH="${DISP_THRESH}"
export GOAP_FREEZE_MIN_STUCK_ACTORS="${MIN_STUCK}"
export GOAP_FREEZE_MIN_ACTIVE_ACTORS="${MIN_ACTIVE}"
export GOAP_FREEZE_MAX_CONTINUOUS_SEC="${MAX_CONTINUOUS}"
export GOAP_FREEZE_DIAG="${DIAG}"

python3 <<'PY'
from collections import defaultdict
from pathlib import Path
import os
import re
import sys

diag_path = Path(os.environ["GOAP_FREEZE_DIAG"])
window = float(os.environ["GOAP_FREEZE_WINDOW_SEC"])
thresh = float(os.environ["GOAP_FREEZE_DISP_THRESH"])
min_stuck = int(os.environ["GOAP_FREEZE_MIN_STUCK_ACTORS"])
min_active = int(os.environ["GOAP_FREEZE_MIN_ACTIVE_ACTORS"])
max_continuous = float(os.environ["GOAP_FREEZE_MAX_CONTINUOUS_SEC"])

time_re = re.compile(r"^\[(\d{2}):(\d{2}):(\d{2})\.(\d+)\]")
pos_re = re.compile(r"transformPos=\(([-\d.]+),([-\d.]+),([-\d.]+)\)")
actor_re = re.compile(r"actor=([^,\]]+)")

tracks = defaultdict(list)
move_ok = 0
with diag_path.open(errors="replace") as fh:
    for line in fh:
        if "[GOAP_MOVE]" not in line or "MoveToward ok" not in line:
            continue
        m = time_re.match(line)
        am = actor_re.search(line)
        pm = pos_re.search(line)
        if not (m and am and pm):
            continue
        h, mi, s, ms = map(int, m.groups())
        t = h * 3600 + mi * 60 + s + ms / 1000.0
        x, _y, z = map(float, pm.groups())
        tracks[am.group(1)].append((t, x, z))
        move_ok += 1

print(f"  MoveToward ok サンプル: {move_ok}")
print(f"  actors: {len(tracks)}")

if move_ok < 20 or len(tracks) < min_active:
    print("  ⬜ Diag サンプル不足 — スキップ（GOAP_LOG_LEVEL=2 / Phase D Verbose を確認）")
    print("GOAP_FREEZE_RESULT=SKIP")
    print("GOAP_FREEZE_LONGEST_SEC=0")
    sys.exit(0)

all_t = [t for pts in tracks.values() for t, _, _ in pts]
t0, t1 = min(all_t), max(all_t)
print(f"  範囲: {t1 - t0:.1f}s")

events = []
freeze_start = None
t = t0
step = 1.0
while t <= t1 - window + 1e-6:
    stuck = 0
    active = 0
    for pts in tracks.values():
        inw = [(tt, x, z) for tt, x, z in pts if t <= tt < t + window]
        if len(inw) < 2:
            continue
        active += 1
        d = ((inw[-1][1] - inw[0][1]) ** 2 + (inw[-1][2] - inw[0][2]) ** 2) ** 0.5
        if d < thresh:
            stuck += 1
    is_freeze = active >= min_active and stuck >= min_stuck
    if is_freeze:
        if freeze_start is None:
            freeze_start = t
    else:
        if freeze_start is not None:
            dur = t - freeze_start
            events.append((freeze_start - t0, dur))
            freeze_start = None
    t += step

if freeze_start is not None:
    events.append((freeze_start - t0, t1 - freeze_start))

longest = max((d for _, d in events), default=0.0)
long_events = [(e, d) for e, d in events if d >= max_continuous]

print(f"  停止区間数: {len(events)}（うち >= {max_continuous:.0f}s: {len(long_events)}）")
print(f"  最長連続停止: {longest:.1f}s")
for e, d in long_events[:5]:
    print(f"    - t+{e:.1f}s から {d:.1f}s")

print(f"GOAP_FREEZE_RESULT={'FAIL' if long_events else 'PASS'}")
print(f"GOAP_FREEZE_LONGEST_SEC={longest:.3f}")
print(f"GOAP_FREEZE_FAIL_COUNT={len(long_events)}")

if long_events:
    print("")
    print("  ❌ 全員（ほぼ）移動停止を検出 — AnimalHandler / IsSpecialActive / Forced Skip 等を調査")
    sys.exit(1)

print("")
print("  ✅ 連続移動停止なし（閾値内）")
sys.exit(0)
PY
