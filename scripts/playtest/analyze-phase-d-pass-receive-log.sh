#!/usr/bin/env bash
# Phase D: #37 マージ後 — パス受け・攻撃安定化のログ観察。
#
# Usage:
#   analyze-phase-d-pass-receive-log.sh [log] [owner=Lion] [owner=Elephant]
#   MODE=ally|full（表示ラベルのみ）
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
# shellcheck source=goap-play-gate-config.sh
source "${SCRIPT_DIR}/goap-play-gate-config.sh"
LOG="${1:-${PROJECT_ROOT}/Assets/DebugLog/GoapSummary_latest.txt}"
MODE="${MODE:-full}"
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
echo " Phase D パス受け安定化 GOAP 観察レポート"
echo "========================================"
echo "log: ${LOG}"
echo "mode: ${MODE}"
echo "ally: owner=${ALLY_OWNER}"
if [[ "${MODE}" == "full" ]]; then
  echo "enemy: owner=${ENEMY_OWNER}"
fi
echo ""

pass_count=0
warn_count=0
fail_count=0

check_count() {
  local label="$1"
  local count="$2"
  local min="${3:-1}"
  local severity="${4:-pass}"
  if [[ "${count}" -ge "${min}" ]]; then
    echo "  ✅ ${label}: ${count} 件"
    pass_count=$((pass_count + 1))
  elif [[ "${severity}" == "warn" ]]; then
    echo "  ⬜ ${label}: ${count} 件（状況依存・期待 >= ${min}）"
    warn_count=$((warn_count + 1))
  else
    echo "  ❌ ${label}: ${count} 件（期待 >= ${min}）"
    fail_count=$((fail_count + 1))
  fi
}

check_max() {
  local label="$1"
  local count="$2"
  local max="$3"
  local severity="${4:-warn}"
  if [[ "${count}" -le "${max}" ]]; then
    echo "  ✅ ${label}: ${count} 件（上限 ${max}）"
    pass_count=$((pass_count + 1))
  elif [[ "${severity}" == "fail" ]]; then
    echo "  ❌ ${label}: ${count} 件（上限 ${max}）"
    fail_count=$((fail_count + 1))
  else
    echo "  ⚠️  ${label}: ${count} 件（目安 <= ${max}）"
    warn_count=$((warn_count + 1))
  fi
}

echo "--- #37 コア指標 ---"

receive_goal=$(grep -c 'GoalChanged(goal=IncomingPassReceive' "${LOG}" || true)
receive_start=$(grep -c 'ActionStart(action=MoveToReceivePass' "${LOG}" || true)
receive_done=$(grep -c 'ActionComplete(action=MoveToReceivePass' "${LOG}" || true)
check_count "IncomingPassReceive ゴール選択" "${receive_goal}" 1 warn
check_count "MoveToReceivePass 開始" "${receive_start}" 1 warn
check_count "MoveToReceivePass 完了" "${receive_done}" 1 warn

dribble_start=$(grep -c 'ActionStart(action=DribbleTowardGoal' "${LOG}" || true)
dribble_done=$(grep -c 'ActionComplete(action=DribbleTowardGoal' "${LOG}" || true)
check_count "DribbleTowardGoal 開始" "${dribble_start}" 1 warn
check_count "DribbleTowardGoal 完了" "${dribble_done}" 0 warn

nogoal_5s=$(grep -c 'NoGoalIdle(wait=5\.0s' "${LOG}" || true)
nogoal_long=$(grep -cE 'NoGoalIdle\(wait=[3-9]\.[0-9]+s' "${LOG}" || true)
check_max "NoGoalIdle(wait=5.0s)" "${nogoal_5s}" 0
check_max "NoGoalIdle(wait>=3.0s)" "${nogoal_long}" 2

not_holding=$(grep -c 'not_holding_ball' "${LOG}" || true)
check_max "[GOAP_PASS] not_holding_ball" "${not_holding}" 0

aborted_total=$(grep -c 'Aborted(' "${LOG}" || true)
aborted_gameplay=$(grep -Ec 'Aborted\(attempt=[1-9][0-9]*,' "${LOG}" || true)
aborted_configure_noise=$(grep -c 'Aborted(attempt=0,' "${LOG}" || true)
plan_cleared=$(grep -c 'PlanCleared(reason=ConfigurePilot' "${LOG}" || true)
replan_deferred=$(grep -c 'ReplanDeferred' "${LOG}" || true)
abort_deferred=$(grep -c 'AbortDeferred' "${LOG}" || true)
check_max "Aborted（試合中・attempt>=1）" "${aborted_gameplay}" 25
if [[ "${aborted_configure_noise}" -gt 0 ]]; then
  echo "  ℹ️  Aborted(attempt=0) 設定ノイズ（旧ログ）: ${aborted_configure_noise} 件 — 新ビルドでは PlanCleared / 無ログに分離"
fi
if [[ "${plan_cleared}" -gt 0 ]]; then
  echo "  ℹ️  PlanCleared(ConfigurePilot): ${plan_cleared} 件"
fi
echo "  ℹ️  Aborted 合計（参考）: ${aborted_total} / ReplanDeferred: ${replan_deferred} / AbortDeferred: ${abort_deferred}"

echo ""
echo "--- 攻撃バランス（味方 Main: ${ALLY_OWNER}） ---"
ally_pass=$(grep -E "owner=${ALLY_OWNER}" "${LOG}" | grep -c 'ActionStart(action=PassToTeammate' || true)
ally_shoot=$(grep -E "owner=${ALLY_OWNER}" "${LOG}" | grep -c 'ActionStart(action=ShootAtGoal' || true)
ally_dribble=$(grep -E "owner=${ALLY_OWNER}" "${LOG}" | grep -c 'ActionStart(action=DribbleTowardGoal' || true)
echo "  PassToTeammate: ${ally_pass}"
echo "  ShootAtGoal:    ${ally_shoot}"
echo "  DribbleTowardGoal: ${ally_dribble}"
if [[ "${ally_pass}" -gt 0 && "${ally_shoot}" -gt 0 ]]; then
  echo "  ✅ パスとシュートの両方が記録されています"
  pass_count=$((pass_count + 1))
elif [[ "${ally_pass}" -ge 3 && "${ally_shoot}" -eq 0 ]]; then
  echo "  ⚠️  パス偏重の可能性（シュート 0）"
  warn_count=$((warn_count + 1))
else
  echo "  ⬜ 攻撃サンプル不足（保持・シュートレーンを増やして再試行）"
  warn_count=$((warn_count + 1))
fi

echo ""
echo "--- 受け結果の内訳 ---"
python3 - <<PY
import re
from collections import Counter

log_path = "${LOG}"
lines = open(log_path, encoding="utf-8", errors="replace").read().splitlines()

outcomes = Counter()
transitions = Counter()
by_finish = Counter()
received_attack = 0
received_nogal = 0
miss_nogal = 0

for line in lines:
    m = re.search(r"ReceivePassOutcome\(([^)]+)\)", line)
    if m:
        body = m.group(1)
        finish = re.search(r"finishReason=([^,]+)", body)
        received = re.search(r"received=(true|false)", body)
        finish_reason = finish.group(1) if finish else "unknown"
        is_received = received.group(1) == "true" if received else False
        outcomes["total"] += 1
        by_finish[finish_reason] += 1
        outcomes["received" if is_received else "missed"] += 1
        continue

    m = re.search(r"ReceivePassTransition\(([^)]+)\)", line)
    if not m:
        continue
    body = m.group(1)
    transition = re.search(r"transition=([^,]+)", body)
    received = re.search(r"received=(true|false)", body)
    if not transition:
        continue
    t = transition.group(1)
    is_received = received.group(1) == "true" if received else False
    transitions[t] += 1
    if t == "attack" and is_received:
        received_attack += 1
    elif t == "nogal" and is_received:
        received_nogal += 1
    elif t == "nogal" and not is_received:
        miss_nogal += 1

pass_receive_complete = sum(1 for l in lines if "PassReceiveComplete(" in l)
pass_issued = sum(1 for l in lines if "PassIssued(" in l)

print(f"  PassReceiveComplete ログ: {pass_receive_complete} 件")
print(f"  PassIssued ログ:           {pass_issued} 件")
print(f"  ReceivePassOutcome 合計:   {outcomes['total']} 件")
if outcomes["total"] == 0:
    print("  ⬜ 新ビルド未適用、または Summary ログなし")
else:
    print(f"    受け切り (received=true):  {outcomes['received']} 件")
    print(f"    受け失敗 (received=false): {outcomes['missed']} 件")
    print("    finishReason 内訳:")
    for reason, count in sorted(by_finish.items(), key=lambda x: (-x[1], x[0])):
        print(f"      {reason}: {count}")

print(f"  ReceivePassTransition 合計: {sum(transitions.values())} 件")
if sum(transitions.values()) > 0:
    print("    transition 内訳:")
    for t, count in sorted(transitions.items(), key=lambda x: (-x[1], x[0])):
        print(f"      {t}: {count}")
    print(f"    受け成功→攻撃 (received+attack): {received_attack} 件")
    print(f"    受け成功→NoGoal (received+nogal): {received_nogal} 件  ← 要調査")
    print(f"    受け失敗→NoGoal (missed+nogal):   {miss_nogal} 件  ← 想定内")
PY

echo ""
echo "--- 受け→攻撃遷移（直近） ---"
grep -E 'ReceivePassOutcome|PassReceiveComplete|ReceivePassTransition|ActionComplete\(action=MoveToReceivePass|GoalChanged\(goal=BallPossessionAttack|ActionStart\(action=(PassToTeammate|ShootAtGoal|DribbleTowardGoal)' "${LOG}" \
  | tail -24 || echo "(該当なし)"
echo ""

echo "--- GOAP仕上げゲート (G0〜G4) ---"

shoot_rejected=$(grep -c 'ActionRejected(action=ShootAtGoal' "${LOG}" || true)
freeball_failure=$(grep -c 'PlanFailure(goal=FreeBallRecovery' "${LOG}" || true)
incoming_failure=$(grep -c 'PlanFailure(goal=IncomingPassReceive' "${LOG}" || true)
forced_post_shoot=$(grep -c 'ForcedPostShootDefensePlan' "${LOG}" || true)
nogoal_total=$(grep -c 'NoGoalSelected' "${LOG}" || true)

check_max "ActionRejected(ShootAtGoal)" "${shoot_rejected}" "${GOAP_GATE_MAX_ACTION_REJECTED_SHOOT}" fail
check_max "PlanFailure(FreeBallRecovery)" "${freeball_failure}" "${GOAP_GATE_MAX_PLAN_FAILURE_FREEBALL}" fail
check_max "PlanFailure(IncomingPassReceive)" "${incoming_failure}" "${GOAP_GATE_MAX_PLAN_FAILURE_INCOMING}" fail

eval "$(python3 - "${LOG}" "${GOAP_GATE_SHOOT_NOGOAL_LINES}" <<'PY'
import sys

log_path, window_lines = sys.argv[1], int(sys.argv[2])
lines = open(log_path, encoding="utf-8", errors="replace").read().splitlines()

shoot_nogal = 0
for i, line in enumerate(lines):
    if "ActionComplete(action=ShootAtGoal" not in line and "ActionStart(action=ShootAtGoal" not in line:
        continue
    window = "\n".join(lines[i + 1 : i + 1 + window_lines])
    if "NoGoalSelected" in window:
        shoot_nogal += 1

miss_nogal = sum(
    1
    for line in lines
    if "ReceivePassTransition" in line and "transition=nogal" in line and "received=false" in line
)

print(f"GOAP_MET_SHOOT_NOGOAL={shoot_nogal}")
print(f"GOAP_MET_MISSED_NOGAL={miss_nogal}")
PY
)"
check_max "Shoot→NoGoal（直後 ${GOAP_GATE_SHOOT_NOGOAL_LINES} 行）" "${GOAP_MET_SHOOT_NOGOAL}" "${GOAP_GATE_MAX_SHOOT_NOGOAL_WINDOW}" fail
check_max "missed+nogal（受け失敗→NoGoal）" "${GOAP_MET_MISSED_NOGAL}" 0 fail

if [[ "${MODE}" == "full" ]]; then
  croc_nogal=$(grep -E 'owner=Crocodile' "${LOG}" | grep -c 'NoGoalSelected' || true)
  elephant_nogal=$(grep -E 'owner=Elephant' "${LOG}" | grep -c 'NoGoalSelected' || true)
  shark_nogal=$(grep -E 'owner=Shark' "${LOG}" | grep -c 'NoGoalSelected' || true)
  enemy_nogal=$((croc_nogal + elephant_nogal + shark_nogal))

  check_max "敵 Crocodile NoGoalSelected" "${croc_nogal}" "${GOAP_GATE_MAX_ENEMY_NOGOAL_CROCODILE}" fail
  check_max "敵 Elephant NoGoalSelected" "${elephant_nogal}" "${GOAP_GATE_MAX_ENEMY_NOGOAL_ELEPHANT}" fail
  check_max "敵 NPC NoGoalSelected 合計" "${enemy_nogal}" "${GOAP_GATE_MAX_ENEMY_NOGOAL_TOTAL}" fail
  check_count "ForcedPostShootDefensePlan" "${forced_post_shoot}" "${GOAP_GATE_MIN_FORCED_POST_SHOOT_DEFENSE}" warn

  lion_nogal=$(grep -E 'owner=Lion' "${LOG}" | grep -c 'NoGoalSelected' || true)
  gorilla_nogal=$(grep -E 'owner=Gorilla' "${LOG}" | grep -c 'NoGoalSelected' || true)
  boar_nogal=$(grep -E 'owner=Boar' "${LOG}" | grep -c 'NoGoalSelected' || true)
  check_max "味方 Lion NoGoalSelected" "${lion_nogal}" "${GOAP_GATE_MAX_ALLY_NOGOAL_LION}" fail
  check_max "味方 Gorilla NoGoalSelected" "${gorilla_nogal}" "${GOAP_GATE_MAX_ALLY_NOGOAL_GORILLA}" fail
  check_max "味方 Boar NoGoalSelected" "${boar_nogal}" "${GOAP_GATE_MAX_ALLY_NOGOAL_BOAR}" fail
else
  echo "  ℹ️  MODE=ally: G4 敵 NPC 指標はスキップ（MODE=full で検証）"
fi

check_max "全体 NoGoalSelected（G6 まで WARN）" "${nogoal_total}" "${GOAP_GATE_WARN_TOTAL_NOGOAL}" warn
echo "  ℹ️  上限定義: scripts/playtest/goap-play-gate-config.sh"

echo "--- サマリ ---"
echo "  PASS: ${pass_count} / WARN: ${warn_count} / FAIL: ${fail_count}"
if [[ "${fail_count}" -eq 0 ]]; then
  echo "  判定: コア回帰なし（WARN はプレイ内容・MODE・G6 指標による）"
else
  echo "  判定: 要調査 — ログ全文または Diag を確認"
fi
echo ""

if [[ "${MODE}" == "full" ]]; then
  echo "========================================"
  echo " 統合サマリ（Phase C 互換）"
  echo "========================================"
  ALLY_OWNER="${ALLY_OWNER}" ENEMY_OWNER="${ENEMY_OWNER}" \
    "${SCRIPT_DIR}/analyze-phase-c-integration-log.sh" "${LOG}" "owner=${ALLY_OWNER}" "owner=${ENEMY_OWNER}" \
    | sed -n '1,120p'
  echo ""
  echo "（詳細は analyze-phase-c-integration-log.sh を直接実行）"
else
  echo "========================================"
  echo " 味方 Main 詳細"
  echo "========================================"
  "${SCRIPT_DIR}/analyze-main-npc-goap-log.sh" "${LOG}" "owner=${ALLY_OWNER}"
fi
