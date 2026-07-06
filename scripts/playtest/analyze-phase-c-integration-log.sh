#!/usr/bin/env bash
# Phase C: 統合マッチ後の GOAP 観察 — 味方 Main / 敵 Main / 守備を一括要約。
#
# Usage:
#   analyze-phase-c-integration-log.sh [log] [owner=Lion] [owner=Elephant]
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
LOG="${1:-${PROJECT_ROOT}/Assets/DebugLog/GoapSummary_latest.txt}"
ALLY_OWNER="${ALLY_OWNER:-Lion}"
ENEMY_OWNER="${ENEMY_OWNER:-Elephant}"

shift 2>/dev/null || true
owners=()
for arg in "$@"; do
  case "${arg}" in
    owner=*) owners+=("${arg#owner=}") ;;
  esac
done
if [[ ${#owners[@]} -ge 1 ]]; then
  ALLY_OWNER="${owners[0]}"
fi
if [[ ${#owners[@]} -ge 2 ]]; then
  ENEMY_OWNER="${owners[1]}"
fi

if [[ ! -f "${LOG}" ]]; then
  echo "ログが見つかりません: ${LOG}" >&2
  exit 1
fi

echo "========================================"
echo " Phase C 統合マッチ GOAP 観察レポート"
echo "========================================"
echo "log: ${LOG}"
echo "ally main: owner=${ALLY_OWNER}"
echo "enemy main: owner=${ENEMY_OWNER}"
echo ""

pass_count=0
fail_count=0
warn_count=0

check_count() {
  local label="$1"
  local count="$2"
  local min="${3:-1}"
  if [[ "${count}" -ge "${min}" ]]; then
    echo "  ✅ ${label}: ${count} 件"
    pass_count=$((pass_count + 1))
  else
    echo "  ⬜ ${label}: ${count} 件（期待 >= ${min}）"
    fail_count=$((fail_count + 1))
  fi
}

echo "--- クイックチェックリスト ---"

ally_m1=$(grep -E "owner=${ALLY_OWNER}" "${LOG}" | grep -c 'prodM1:True' || true)
ally_m2=$(grep -E "owner=${ALLY_OWNER}" "${LOG}" | grep -c 'prodM2:True' || true)
enemy_m1=$(grep -E "owner=${ENEMY_OWNER}" "${LOG}" | grep -c 'prodM1:True' || true)
enemy_m2=$(grep -E "owner=${ENEMY_OWNER}" "${LOG}" | grep -c 'prodM2:True' || true)

check_count "味方 Main prodM1" "${ally_m1}"
check_count "味方 Main prodM2" "${ally_m2}"
check_count "敵 Main prodM1" "${enemy_m1}"
check_count "敵 Main prodM2" "${enemy_m2}"

ally_attack=$(grep -E "owner=${ALLY_OWNER}" "${LOG}" | grep -cE 'ActionStart\(action=(PassToTeammate|ShootAtGoal)' || true)
enemy_attack=$(grep -E "owner=${ENEMY_OWNER}" "${LOG}" | grep -cE 'ActionStart\(action=(PassToTeammate|ShootAtGoal)' || true)
check_count "味方 Main 攻撃実行 (Pass/Shoot)" "${ally_attack}"
check_count "敵 Main 攻撃実行 (Pass/Shoot)" "${enemy_attack}"

ally_support=$(grep -E "owner=${ALLY_OWNER}" "${LOG}" | grep -c 'GoalChanged(goal=TeamBallSupport)' || true)
enemy_support=$(grep -E "owner=${ENEMY_OWNER}" "${LOG}" | grep -c 'GoalChanged(goal=TeamBallSupport)' || true)
if [[ "${ally_support}" -ge 1 ]]; then
  echo "  ✅ 味方 Main TeamBallSupport: ${ally_support} 件"
  pass_count=$((pass_count + 1))
else
  echo "  ⬜ 味方 Main TeamBallSupport: ${ally_support} 件（パス後に発生しやすい）"
  warn_count=$((warn_count + 1))
fi
if [[ "${enemy_support}" -ge 1 ]]; then
  echo "  ✅ 敵 Main TeamBallSupport: ${enemy_support} 件"
  pass_count=$((pass_count + 1))
else
  echo "  ⬜ 敵 Main TeamBallSupport: ${enemy_support} 件（パス後に発生しやすい）"
  warn_count=$((warn_count + 1))
fi

ally_defense=$(grep -c 'GoalChanged(goal=DefensivePositioning)' "${LOG}" || true)
enemy_sub_support=$(grep -E 'owner=(Gorilla|Crocodile|Shark|Tiger)' "${LOG}" | grep -c 'GoalChanged(goal=TeamBallSupport)' || true)
if [[ "${ally_defense}" -ge 1 ]]; then
  echo "  ✅ 味方 Sub 守備開始: ${ally_defense} 件"
  pass_count=$((pass_count + 1))
else
  echo "  ⬜ 味方 Sub 守備開始: ${ally_defense} 件（敵ボール時に発生）"
  warn_count=$((warn_count + 1))
fi
if [[ "${enemy_sub_support}" -ge 1 ]]; then
  echo "  ✅ 敵 Sub サポート開始: ${enemy_sub_support} 件"
  pass_count=$((pass_count + 1))
else
  echo "  ⬜ 敵 Sub サポート開始: ${enemy_sub_support} 件（味方ボール時に発生）"
  warn_count=$((warn_count + 1))
fi

echo ""
echo "--- サマリ ---"
echo "  PASS 項目: ${pass_count}"
echo "  未達項目: ${fail_count}"
echo "  要確認（状況依存）: ${warn_count}"
if [[ "${fail_count}" -eq 0 ]]; then
  echo "  判定: コア項目は観察済み（状況依存項目はプレイ内容による）"
else
  echo "  判定: 未達あり — プレイ時間を延ばすか、ボール遷移を増やして再試行"
fi
echo ""

echo "========================================"
echo " 詳細: 味方 Main"
echo "========================================"
"${SCRIPT_DIR}/analyze-main-npc-goap-log.sh" "${LOG}" "owner=${ALLY_OWNER}"
echo ""

echo "========================================"
echo " 詳細: 敵 Main"
echo "========================================"
"${SCRIPT_DIR}/analyze-enemy-main-npc-goap-log.sh" "${LOG}" "owner=${ENEMY_OWNER}"
echo ""

echo "========================================"
echo " 詳細: 味方 Sub 守備"
echo "========================================"
"${SCRIPT_DIR}/analyze-defense-goap-log.sh" "${LOG}"
