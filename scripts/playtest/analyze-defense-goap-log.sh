#!/usr/bin/env bash
# 本番マッチ後の守備 GOAP 観察用: GoapSummary_latest.txt を要約する。
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
LOG="${1:-${PROJECT_ROOT}/Assets/DebugLog/GoapSummary_latest.txt}"

if [[ ! -f "${LOG}" ]]; then
  echo "ログが見つかりません: ${LOG}" >&2
  echo "Unity で試合をプレイすると Assets/DebugLog/GoapSummary_latest.txt に出力されます。" >&2
  exit 1
fi

echo "=== 守備 GOAP 観察レポート ==="
echo "log: ${LOG}"
echo ""

echo "--- ActionStart 集計（守備アクション） ---"
grep -E 'ActionStart\(action=(MoveToDefensivePosition|RetreatToDefensiveLine|MarkOpponent|BlockPassLane|BlockShotLane)' "${LOG}" \
  | sed -E 's/.*ActionStart\(action=([^,)]+).*/\1/' \
  | sort | uniq -c | sort -nr || true
echo ""

echo "--- DefensivePositioning PlanCosts（直近 20 件） ---"
grep 'PlanCosts(goal=DefensivePositioning' "${LOG}" | tail -20 || true
echo ""

echo "--- 要注目: urgency>=0.45 なのに Retreat 以外が選出 ---"
python3 - "${LOG}" <<'PY'
import re
import sys

path = sys.argv[1]
pattern = re.compile(
    r"PlanCosts\(goal=DefensivePositioning, slot=(\d+), "
    r"(?:defenseDiag=urgency:([\d.]+),ahead:([\d.]+),sigAhead:(True|False), )?"
    r".*selected=([^:]+):"
)

flags = []
with open(path, encoding="utf-8", errors="replace") as f:
    for line in f:
        m = pattern.search(line)
        if not m:
            continue
        slot, urgency, ahead, sig_ahead, selected = m.groups()
        if urgency is None:
            continue
        urgency_f = float(urgency)
        if urgency_f >= 0.45 and selected != "RetreatToDefensiveLine":
            flags.append((urgency_f, slot, ahead, sig_ahead, selected, line.strip()[-120:]))

if not flags:
    print("(該当なし — 高 urgency で Retreat 以外が勝ったケースは見つかりませんでした)")
else:
    print(f"該当 {len(flags)} 件:")
    for item in flags[-15:]:
        urgency_f, slot, ahead, sig_ahead, selected, tail = item
        print(f"  slot{slot} urgency={urgency_f:.2f} ahead={ahead} sigAhead={sig_ahead} selected={selected}")
PY

echo ""
echo "--- 要注目: urgency<0.45 なのに Retreat が選出 ---"
python3 - "${LOG}" <<'PY'
import re
import sys

path = sys.argv[1]
pattern = re.compile(
    r"PlanCosts\(goal=DefensivePositioning, slot=(\d+), "
    r"(?:defenseDiag=urgency:([\d.]+),ahead:([\d.]+),sigAhead:(True|False), )?"
    r".*selected=([^:]+):"
)

flags = []
with open(path, encoding="utf-8", errors="replace") as f:
    for line in f:
        m = pattern.search(line)
        if not m:
            continue
        slot, urgency, ahead, sig_ahead, selected = m.groups()
        if urgency is None:
            continue
        urgency_f = float(urgency)
        if urgency_f < 0.45 and selected == "RetreatToDefensiveLine":
            flags.append((urgency_f, slot, ahead, sig_ahead))

if not flags:
    print("(該当なし)")
else:
    print(f"該当 {len(flags)} 件 (プレス位置で Retreat が選ばれた可能性):")
    for urgency_f, slot, ahead, sig_ahead in flags[-10:]:
        print(f"  slot{slot} urgency={urgency_f:.2f} ahead={ahead} sigAhead={sig_ahead}")
PY

echo ""
echo "観察の目安:"
echo "  - 味方が前に出すぎた局面 → urgency>=0.45 で Retreat が選ばれるか"
echo "  - 通常プレス局面 → urgency<0.45 で MoveToDefensivePosition が選ばれるか"
echo "  - 要注目行が多い場合は TeammateNpcDefensePlanning の閾値調整を検討"
