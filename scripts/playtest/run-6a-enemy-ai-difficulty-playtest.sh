#!/usr/bin/env bash
# 6-A P0: 敵 AI 難易度目視の CLI 実行（MainMenu → Play → オフライン対戦 → 自動終了）。
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
UNITY_VERSION="${GOAP_UNITY_VERSION:-${UNITY_VERSION:-6000.2.7f2}}"
DURATION="${GOAP_WATCH_SECONDS:-90}"
DIFFICULTY="${DIFFICULTY:-Normal}"
LOG_DIR="${PROJECT_ROOT}/Logs"
UNITY_LOG="${LOG_DIR}/goap-6a-difficulty-playtest.log"
RESULT_FILE="${LOG_DIR}/goap-watch-result.txt"
SUMMARY="${PROJECT_ROOT}/Assets/DebugLog/GoapSummary_latest.txt"

resolve_unity() {
  if [[ -n "${UNITY_PATH:-}" && -x "${UNITY_PATH}" ]]; then
    echo "${UNITY_PATH}"
    return
  fi
  local hub_path="/Applications/Unity/Hub/Editor/${UNITY_VERSION}/Unity.app/Contents/MacOS/Unity"
  if [[ -x "${hub_path}" ]]; then
    echo "${hub_path}"
    return
  fi
  echo "Unity editor not found. Set UNITY_PATH or install Unity ${UNITY_VERSION}." >&2
  exit 127
}

mkdir -p "${LOG_DIR}"
rm -f \
  "${LOG_DIR}/goap-watch-pending-exit.txt" \
  "${LOG_DIR}/goap-watch-started.marker" \
  "${RESULT_FILE}"

echo "[6a] preparing scene/logs (difficulty=${DIFFICULTY}, duration=${DURATION}s)"
DIFFICULTY="${DIFFICULTY}" \
  "${SCRIPT_DIR}/prepare-6a-enemy-ai-difficulty-visual-check.sh" "6a_enemy_ai_difficulty_cli_before"

if pgrep -f "Unity.*AnimalStrikers" >/dev/null 2>&1; then
  echo "[6a] closing open Unity Editor for this project..."
  osascript -e 'tell application "Unity" to quit' 2>/dev/null || true
  for _ in $(seq 1 30); do
    pgrep -f "Unity.*AnimalStrikers" >/dev/null 2>&1 || break
    sleep 2
  done
  if pgrep -f "Unity.*AnimalStrikers" >/dev/null 2>&1; then
    echo "[6a] Unity did not quit cleanly; aborting to avoid project lock" >&2
    exit 1
  fi
fi

UNITY_BIN="$(resolve_unity)"
echo "[6a] starting Unity watch playtest at $(date +%H:%M:%S)"
echo "[6a] log: ${UNITY_LOG}"

set +e
"${UNITY_BIN}" \
  -batchmode \
  -nographics \
  -projectPath "${PROJECT_ROOT}" \
  "-goapWatchPlaytest=${DURATION}" \
  -logFile "${UNITY_LOG}"
exit_code=$?
set -e

echo ""
if [[ -f "${RESULT_FILE}" ]]; then
  echo "[6a] result: $(cat "${RESULT_FILE}")"
else
  echo "[6a] result file missing (exit=${exit_code})"
fi

if [[ ! -f "${SUMMARY}" ]]; then
  echo "[6a] GoapSummary_latest.txt not found (exit=${exit_code})" >&2
  exit "${exit_code:-1}"
fi

lines=$(wc -l < "${SUMMARY}" | tr -d ' ')
echo "[6a] updated ${SUMMARY} (${lines} lines, exit=${exit_code})"
echo ""

OWNER="${ENEMY_OWNER:-Elephant}"
if [[ -x "${SCRIPT_DIR}/analyze-enemy-main-npc-goap-log.sh" ]]; then
  "${SCRIPT_DIR}/analyze-enemy-main-npc-goap-log.sh" "${SUMMARY}" "owner=${OWNER}" || true
fi

echo ""
echo "--- 6-A 攻撃アクション要約（敵寄り） ---"
echo "ShootAtGoal ActionStart: $(grep -c 'ActionStart(action=ShootAtGoal' "${SUMMARY}" 2>/dev/null || echo 0)"
echo "PassToTeammate ActionStart: $(grep -c 'ActionStart(action=PassToTeammate' "${SUMMARY}" 2>/dev/null || echo 0)"
echo "（難易度=${DIFFICULTY} / gameplay≈${DURATION}s。比較は DIFFICULTY=Easy|Hard で再実行）"
echo ""

exit "${exit_code}"
