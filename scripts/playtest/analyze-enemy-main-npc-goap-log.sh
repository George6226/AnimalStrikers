#!/usr/bin/env bash
# Phase B: 敵 Main NPC GOAP 観察用 — GoapSummary_latest.txt を要約する。
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
LOG="${1:-${PROJECT_ROOT}/Assets/DebugLog/GoapSummary_latest.txt}"
OWNER_FILTER="${2:-owner=Tiger}"

if [[ ! -f "${LOG}" ]]; then
  echo "ログが見つかりません: ${LOG}" >&2
  exit 1
fi

echo "=== Phase B 敵 GOAP 観察レポート ==="
echo "log: ${LOG}"
echo "enemy main filter: ${OWNER_FILTER}"
echo ""

FILTER=(grep -F "${OWNER_FILTER}" "${LOG}")

echo "--- 敵 Main: prodM1 / prodM2 ---"
echo "  prodM1:True 件数 (全体): $(grep -c 'prodM1:True' "${LOG}" || true)"
echo "  prodM2:True 件数 (全体): $(grep -c 'prodM2:True' "${LOG}" || true)"
echo "  prodM1:True (${OWNER_FILTER}): $("${FILTER[@]}" | grep -c 'prodM1:True' || true)"
echo "  prodM2:True (${OWNER_FILTER}): $("${FILTER[@]}" | grep -c 'prodM2:True' || true)"
echo ""

echo "--- 敵 Main ActionStart ---"
"${FILTER[@]}" \
  | grep -E 'ActionStart\(action=' \
  | sed -E 's/.*ActionStart\(action=([^,)]+).*/\1/' \
  | sort | uniq -c | sort -nr || true
echo ""

echo "--- 敵 Sub: ゴール選出（Elephant / Gorilla 想定） ---"
grep -E 'owner=(Elephant|Gorilla)' "${LOG}" \
  | grep -E 'PlanCosts\(goal=' \
  | sed -E 's/.*PlanCosts\(goal=([^,]+).*/\1/' \
  | sort | uniq -c | sort -nr | head -10 || true
echo ""

echo "--- 味方 Sub: 敵ボール時守備 ---"
grep -E 'owner=(Rhinoceros|Boar)' "${LOG}" \
  | grep -E 'ActionStart\(action=MoveToDefensivePosition|GoalChanged\(goal=DefensivePositioning' \
  | head -6 || echo "(該当なし)"
echo ""

echo "--- 敵 Main パス後サポート ---"
python3 - "${LOG}" "${OWNER_FILTER}" <<'PY'
import pathlib
import sys

path = pathlib.Path(sys.argv[1])
owner_filter = sys.argv[2]
lines = [l for l in path.read_text(encoding="utf-8", errors="replace").splitlines() if owner_filter in l]

has_m1 = any("prodM1:True" in l for l in lines)
has_pass = any("ActionStart(action=PassToTeammate" in l for l in lines)
has_m2 = any("prodM2:True" in l for l in lines)
has_support = any(
    t in l
    for l in lines
    for t in (
        "GoalChanged(goal=TeamBallSupport",
        "PlanCosts(goal=TeamBallSupport, tier=Main",
        "ActionStart(action=MoveToSupportPosition",
        "ActionStart(action=GetOpen",
        "ActionStart(action=CreateSupportAngle",
    )
)
no_goal = sum(1 for l in lines if "NoGoalSelected" in l)

print(f"  M1 (prodM1): {'PASS' if has_m1 else 'FAIL'}")
print(f"  PassToTeammate: {'PASS' if has_pass else 'FAIL'}")
print(f"  M2 (prodM2): {'PASS' if has_m2 else 'FAIL — パス後オフボール未稼働'}")
print(f"  TeamBallSupport: {'PASS' if has_support else 'FAIL'}")
if no_goal:
    print(f"  NoGoalSelected ({owner_filter}): {no_goal} 件 — ゴール未選出の可能性")
PY

echo ""
echo "--- 敵 Sub 問題パターン ---"
echo "  EnemyBallDefense PlanCosts: $(grep -E 'owner=(Elephant|Gorilla)' "${LOG}" | grep -c 'goal=EnemyBallDefense' || true) 件"
echo "  TeamBallSupport PlanCosts: $(grep -E 'owner=(Elephant|Gorilla)' "${LOG}" | grep -c 'goal=TeamBallSupport' || true) 件"
echo "  NoPlanFromPlanner: $(grep -E 'owner=(Elephant|Gorilla)' "${LOG}" | grep -c 'NoPlanFromPlanner' || true) 件"
echo ""
echo "再実行: ./scripts/playtest/analyze-enemy-main-npc-goap-log.sh <log> owner=Tiger"
