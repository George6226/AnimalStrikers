#!/usr/bin/env bash
# 本番マッチ後の Main NPC GOAP 観察用: GoapSummary_latest.txt を要約する。
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
LOG="${1:-${PROJECT_ROOT}/Assets/DebugLog/GoapSummary_latest.txt}"
OWNER_FILTER="${2:-}"

if [[ ! -f "${LOG}" ]]; then
  echo "ログが見つかりません: ${LOG}" >&2
  echo "Unity で試合をプレイすると Assets/DebugLog/GoapSummary_latest.txt に出力されます。" >&2
  exit 1
fi

echo "=== Main NPC GOAP 観察レポート ==="
echo "log: ${LOG}"
if [[ -n "${OWNER_FILTER}" ]]; then
  echo "owner filter: ${OWNER_FILTER}"
fi
echo ""

FILTER_CMD=(cat "${LOG}")
if [[ -n "${OWNER_FILTER}" ]]; then
  NAME="${OWNER_FILTER#owner=}"
  FILTER_CMD=(grep -E "(owner=${NAME}|passer=${NAME})" "${LOG}")
fi

echo "--- Main tier ActionStart 集計 ---"
"${FILTER_CMD[@]}" \
  | grep -E 'ActionStart\(action=(MoveToSupportPosition|CreateSupportAngle|GetOpen|MakeRunBehind|MoveToFreeBall|PassToTeammate|ShootAtGoal)' \
  | sed -E 's/.*ActionStart\(action=([^,)]+).*/\1/' \
  | sort | uniq -c | sort -nr || true
echo ""

echo "--- ForcedMainPostPassSupportPlan ---"
"${FILTER_CMD[@]}" | grep -c 'ForcedMainPostPassSupportPlan' || true
echo ""

echo "--- TeamBallSupport PlanCosts tier=Main（直近 20 件） ---"
"${FILTER_CMD[@]}" | grep 'PlanCosts(goal=TeamBallSupport, tier=Main' | tail -20 || true
echo ""

echo "--- FreeBallRecovery PlanCosts tier=Main（直近 10 件） ---"
"${FILTER_CMD[@]}" | grep 'PlanCosts(goal=FreeBallRecovery, tier=Main' | tail -10 || true
echo ""

echo "--- 要注目: ctx=Support なのに needsMove=True でサポート系が選出されない ---"
python3 - "${LOG}" "${OWNER_FILTER}" <<'PY'
import re
import sys

path = sys.argv[1]
owner_filter = sys.argv[2]
pattern = re.compile(
    r"PlanCosts\(goal=TeamBallSupport, tier=Main, slot=\d+, "
    r"(?:mainNpcDiag=(?:prodM2:True,)?ctx:Support,needsMove:True,.*?)"
    r".*selected=([^:]+):"
)
support_actions = {
    "MoveToSupportPosition",
    "CreateSupportAngle",
    "GetOpen",
    "MakeRunBehind",
}

flags = []
with open(path, encoding="utf-8", errors="replace") as f:
    for line in f:
        if owner_filter and owner_filter not in line:
            continue
        m = pattern.search(line)
        if not m:
            continue
        selected = m.group(1)
        if selected not in support_actions:
            flags.append((selected, line.strip()[-140:]))

if not flags:
    print("(該当なし — サポート移動が必要なのに非サポート行動が選ばれたケースは見つかりませんでした)")
else:
    print(f"該当 {len(flags)} 件:")
    for selected, tail in flags[-10:]:
        print(f"  selected={selected}")
PY

echo ""
echo "--- 要注目: ctx=FreeBall なのに MoveToFreeBall 以外が選出 ---"
python3 - "${LOG}" "${OWNER_FILTER}" <<'PY'
import re
import sys

path = sys.argv[1]
owner_filter = sys.argv[2]
pattern = re.compile(
    r"PlanCosts\(goal=FreeBallRecovery, tier=Main, slot=\d+, "
    r"(?:mainNpcDiag=(?:prodM2:True,)?ctx:FreeBall,.*?)"
    r".*selected=([^:]+):"
)

flags = []
with open(path, encoding="utf-8", errors="replace") as f:
    for line in f:
        if owner_filter and owner_filter not in line:
            continue
        m = pattern.search(line)
        if not m:
            continue
        selected = m.group(1)
        if selected != "MoveToFreeBall":
            flags.append((selected, line.strip()[-140:]))

if not flags:
    print("(該当なし)")
else:
    print(f"該当 {len(flags)} 件:")
    for selected, tail in flags[-10:]:
        print(f"  selected={selected}")
PY

echo ""
echo "--- パス診断 [GOAP_PASS]（直近 15 件） ---"
"${FILTER_CMD[@]}" | grep '\[GOAP_PASS\]' | tail -15 || echo "(該当なし — コード更新後に再プレイしてください)"
echo ""

echo "--- パス結果サマリ [GOAP_PASS] ---"
python3 - "${LOG}" "${OWNER_FILTER}" <<'PY'
import re
import sys

path = sys.argv[1]
owner_filter = sys.argv[2]
name = owner_filter.replace("owner=", "") if owner_filter else ""
kick = 0
skipped = 0
cancelled = 0
aim_shifts = []

def matches_filter(line: str) -> bool:
    if not name:
        return True
    return f"owner={name}" in line or f"passer={name}" in line

with open(path, encoding="utf-8", errors="replace") as f:
    for line in f:
        if not matches_filter(line):
            continue
        if "[GOAP_PASS]" not in line:
            continue
        if "Kicked " in line or line.rstrip().endswith("Kicked"):
            kick += 1
        if "Skipped duplicate" in line:
            skipped += 1
        if "Cancelled reason=" in line:
            cancelled += 1
        m = re.search(r"aimDeltaDeg=([0-9.]+)", line)
        if m:
            aim_shifts.append(float(m.group(1)))

print(f"kick={kick} skipped_duplicate={skipped} cancelled={cancelled}")
if aim_shifts:
    print(f"aimDeltaDeg avg={sum(aim_shifts)/len(aim_shifts):.1f} max={max(aim_shifts):.1f} (起動→キックの向き変化)")
else:
    print("aimDeltaDeg=(Kick 行なし)")
PY

echo ""
echo "--- パス後サポート開始チェック ---"
python3 - "${LOG}" "${OWNER_FILTER}" <<'PY'
import pathlib
import sys

path = pathlib.Path(sys.argv[1])
owner_filter = sys.argv[2]
text = path.read_text(encoding="utf-8", errors="replace")
lines = [line for line in text.splitlines() if not owner_filter or owner_filter in line]

has_pass = any("ActionStart(action=PassToTeammate" in line for line in lines)
has_main_support = False
for line in lines:
    if "owner=Lion" not in line and owner_filter:
        continue
    if not owner_filter and "tier=Main" not in line and "ForcedMainPostPassSupportPlan" not in line:
        continue
    if any(
        token in line
        for token in (
            "GoalChanged(goal=TeamBallSupport",
            "ActionStart(action=MoveToSupportPosition",
            "ActionStart(action=CreateSupportAngle",
            "ActionStart(action=GetOpen",
            "ActionStart(action=MakeRunBehind",
            "ForcedMainPostPassSupportPlan",
            "PlanCosts(goal=TeamBallSupport, tier=Main",
        )
    ):
        has_main_support = True
        break

if has_pass and has_main_support:
    print("PASS: パス後に TeamBallSupport / サポート行動が記録されています")
elif has_pass:
    print("要確認: Pass はあるが Main のサポート開始ログが見つかりません")
else:
    print("(パス未実行 — 本番 M2 観察では手動パス後のログを確認してください)")
PY

echo ""
echo "観察の目安:"
echo "  - パス後（味方保持）→ tier=Main で TeamBallSupport + prodM2:True + サポート走り"
echo "  - ルーズボール → FreeBallRecovery + MoveToFreeBall"
echo "  - 操作キャラ頭上ラベル YOU·M2 が出ているか（本番のみ）"
echo "  - 特定キャラだけ見る場合: ./scripts/playtest/analyze-main-npc-goap-log.sh <log> owner=Shark"
