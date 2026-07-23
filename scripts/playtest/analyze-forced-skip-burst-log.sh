#!/usr/bin/env bash
# Summary の MoveToDefensive ActionSkipped(context_changed) 連発（P0'' 型）を検出。
#
# Usage:
#   analyze-forced-skip-burst-log.sh [GoapSummary_latest.txt]
#
# Exit:
#   0 = PASS / SKIP
#   1 = FAIL（高密度 Skip が連続）
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
if [[ -f "${SCRIPT_DIR}/goap-play-gate-config.sh" ]]; then
  # shellcheck source=goap-play-gate-config.sh
  source "${SCRIPT_DIR}/goap-play-gate-config.sh"
fi

LOG="${1:-${PROJECT_ROOT}/Assets/DebugLog/GoapSummary_latest.txt}"
WARN_PER_SEC="${GOAP_GATE_WARN_MTD_SKIP_PER_SEC:-15}"
FAIL_BURST_SEC="${GOAP_GATE_FAIL_MTD_SKIP_BURST_SEC:-8}"

echo "========================================"
echo " Forced→Skip 密度（MoveToDefensive）"
echo "========================================"
echo "log: ${LOG}"
echo "warn>=${WARN_PER_SEC}/s for window, fail if >=${FAIL_BURST_SEC}s continuous"
echo ""

if [[ ! -f "${LOG}" ]]; then
  echo "  ⬜ Summary なし — スキップ"
  exit 0
fi

export GOAP_SKIP_LOG="${LOG}"
export GOAP_SKIP_WARN_PER_SEC="${WARN_PER_SEC}"
export GOAP_SKIP_FAIL_BURST_SEC="${FAIL_BURST_SEC}"

python3 <<'PY'
from collections import Counter
from pathlib import Path
import os
import re
import sys

log_path = Path(os.environ["GOAP_SKIP_LOG"])
warn_per_sec = float(os.environ["GOAP_SKIP_WARN_PER_SEC"])
fail_burst = float(os.environ["GOAP_SKIP_FAIL_BURST_SEC"])
time_re = re.compile(r"^\[(\d{2}):(\d{2}):(\d{2})\.(\d+)\]")

times = []
with log_path.open(errors="replace") as fh:
    for line in fh:
        if "ActionSkipped" not in line or "MoveToDefensivePosition" not in line:
            continue
        if "context_changed" not in line and "already_holding_defensive" not in line:
            continue
        m = time_re.match(line)
        if not m:
            continue
        h, mi, s, ms = map(int, m.groups())
        times.append(h * 3600 + mi * 60 + s + ms / 1000.0)

print(f"  MTD ActionSkipped: {len(times)}")
if len(times) < 30:
    print("  ✅ Skip サンプル少 — PASS")
    print("GOAP_SKIP_BURST_RESULT=PASS")
    sys.exit(0)

t0, t1 = min(times), max(times)
# per-second counts
by_sec = Counter(int(t) for t in times)
hot_start = None
events = []
warn_peaks = 0
for sec in range(int(t0), int(t1) + 1):
    rate = by_sec.get(sec, 0)
    if rate >= warn_per_sec:
        warn_peaks += 1
        if hot_start is None:
            hot_start = sec
    else:
        if hot_start is not None:
            dur = sec - hot_start
            events.append((hot_start - t0, dur, max(by_sec.get(s, 0) for s in range(hot_start, sec))))
            hot_start = None
if hot_start is not None:
    dur = int(t1) + 1 - hot_start
    events.append((hot_start - t0, dur, max(by_sec.get(s, 0) for s in range(hot_start, int(t1) + 1))))

long_events = [(e, d, peak) for e, d, peak in events if d >= fail_burst]
print(f"  高密度区間: {len(events)}（うち >= {fail_burst:.0f}s: {len(long_events)}）")
for e, d, peak in long_events[:5]:
    print(f"    - t+{e:.1f}s から {d:.1f}s（peak {peak}/s）")

if long_events:
    print("")
    print("  ❌ Forced→Skip 連発（P0'' 型）を検出")
    print("GOAP_SKIP_BURST_RESULT=FAIL")
    sys.exit(1)

if warn_peaks > 0:
    print(f"  ⚠️  高密度秒が {warn_peaks} 秒分あったが連続 {fail_burst:.0f}s 未満 — WARN")
    print("GOAP_SKIP_BURST_RESULT=WARN")
else:
    print("  ✅ Forced→Skip 連発なし")
    print("GOAP_SKIP_BURST_RESULT=PASS")
sys.exit(0)
PY
