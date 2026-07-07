#!/usr/bin/env bash
# Phase D: Unity バッチで約 3 分の統合 GOAP プレイテストを実行しログを更新する。
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
UNITY_VERSION="${GOAP_UNITY_VERSION:-${UNITY_VERSION:-6000.2.7f2}}"
DURATION="${GOAP_PHASE_D_SECONDS:-180}"
LOG_DIR="${PROJECT_ROOT}/Logs"
UNITY_LOG="${LOG_DIR}/goap-phase-d-playtest.log"

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
  "${LOG_DIR}/goap-phase-d-pending-exit.txt" \
  "${LOG_DIR}/goap-phase-d-started.marker" \
  "${LOG_DIR}/goap-phase-d-result.txt"

echo "[phase-d] preparing logs (duration=${DURATION}s)"
MODE=full "${SCRIPT_DIR}/prepare-phase-d-pass-receive-check.sh" "phaseD_3min_cli"

if pgrep -f "Unity.*AnimalStrikers" >/dev/null 2>&1; then
  echo "[phase-d] closing open Unity Editor for this project..."
  osascript -e 'tell application "Unity" to quit' 2>/dev/null || true
  for _ in $(seq 1 30); do
    pgrep -f "Unity.*AnimalStrikers" >/dev/null 2>&1 || break
    sleep 2
  done
  if pgrep -f "Unity.*AnimalStrikers" >/dev/null 2>&1; then
    echo "[phase-d] Unity did not quit cleanly; aborting to avoid project lock" >&2
    exit 1
  fi
fi

UNITY_BIN="$(resolve_unity)"
echo "[phase-d] starting Unity batch playtest at $(date +%H:%M:%S)"
echo "[phase-d] log: ${UNITY_LOG}"

set +e
"${UNITY_BIN}" \
  -batchmode \
  -nographics \
  -projectPath "${PROJECT_ROOT}" \
  "-goapPhaseDPlaytest=${DURATION}" \
  -logFile "${UNITY_LOG}"
exit_code=$?
set -e

if [[ -f "${LOG_DIR}/goap-phase-d-result.txt" ]]; then
  cat "${LOG_DIR}/goap-phase-d-result.txt"
fi

SUMMARY="${PROJECT_ROOT}/Assets/DebugLog/GoapSummary_latest.txt"
if [[ ! -f "${SUMMARY}" ]]; then
  echo "[phase-d] GoapSummary_latest.txt not found (exit=${exit_code})" >&2
  exit "${exit_code:-1}"
fi

lines=$(wc -l < "${SUMMARY}" | tr -d ' ')
echo "[phase-d] updated ${SUMMARY} (${lines} lines, exit=${exit_code})"
echo ""
MODE=full ALLY_OWNER="${ALLY_OWNER:-Lion}" ENEMY_OWNER="${ENEMY_OWNER:-Crocodile}" \
  "${SCRIPT_DIR}/analyze-phase-d-pass-receive-log.sh" "${SUMMARY}"

exit "${exit_code}"
