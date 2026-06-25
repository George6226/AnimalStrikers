#!/usr/bin/env bash
# ローカル Mac: Unity 直実行で GOAP EditMode + バッチ検証。
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
# shellcheck source=goap-ci-config.sh
source "${SCRIPT_DIR}/goap-ci-config.sh"

MODE="${1:-all}"
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
UNITY_VERSION="${GOAP_UNITY_VERSION}"
LOG_DIR="${PROJECT_ROOT}/Logs"
DIAG_LOG="${PROJECT_ROOT}/Assets/DebugLog/GoapDiag_latest.txt"
SUMMARY_LOG="${PROJECT_ROOT}/Assets/DebugLog/GoapSummary_latest.txt"
PROGRESS_INTERVAL="${GOAP_CI_PROGRESS_INTERVAL:-15}"
mkdir -p "${LOG_DIR}"

if ! goap_ci_mode_valid "${MODE}"; then
  goap_ci_print_usage "$(basename "$0")"
  exit 2
fi

resolve_unity() {
  if [[ -n "${UNITY_PATH:-}" && -x "${UNITY_PATH}" ]]; then
    echo "${UNITY_PATH}"
    return
  fi

  if command -v unity-editor >/dev/null 2>&1; then
    command -v unity-editor
    return
  fi

  local hub_path="/Applications/Unity/Hub/Editor/${UNITY_VERSION}/Unity.app/Contents/MacOS/Unity"
  if [[ -x "${hub_path}" ]]; then
    echo "${hub_path}"
    return
  fi

  if command -v Unity >/dev/null 2>&1; then
    command -v Unity
    return
  fi

  echo "Unity editor not found. Set UNITY_PATH or install Unity ${UNITY_VERSION}." >&2
  exit 127
}

describe_batch_phase() {
  local elapsed="$1"
  local phase="Unity 起動・コンパイル中"

  if [[ -f "${DIAG_LOG}" ]]; then
    if grep -q "BATCH_COMPLETE" "${DIAG_LOG}" 2>/dev/null; then
      local total
      total="$(grep -E "SELECTION_TOTAL|RUNTIME_TOTAL" "${DIAG_LOG}" | tail -1 || true)"
      phase="バッチ完了 → Unity 終了待ち ${total}"
    elif grep -q "BATCH_ABORT" "${DIAG_LOG}" 2>/dev/null; then
      phase="BATCH_ABORT 検出 → 終了処理中"
    elif grep -q "BATCH_START" "${DIAG_LOG}" 2>/dev/null; then
      local last_marker last_begin
      last_marker="$(grep -E "SELECTION_PASS|RUNTIME_PASS|SELECTION_FAIL|RUNTIME_FAIL" "${DIAG_LOG}" | tail -1 || true)"
      last_begin="$(grep "BATCH_BEGIN" "${DIAG_LOG}" | tail -1 || true)"
      if [[ -n "${last_marker}" ]]; then
        phase="パターン検証中 ${last_marker##*========== }"
      elif [[ -n "${last_begin}" ]]; then
        phase="パターン適用中 ${last_begin##*========== }"
      else
        phase="BATCH_START 済み・初回パターン待ち"
      fi
    elif grep -q "GOAP_BATCH_RUNNER armed" "${DIAG_LOG}" 2>/dev/null; then
      phase="Play モード突入・Photon/スポーン待ち"
    fi
  fi

  if [[ -f "${SUMMARY_LOG}" ]]; then
    if grep -q "waiting for GAME state" "${SUMMARY_LOG}" 2>/dev/null; then
      phase="キックオフ(GAME state)待ち"
    elif grep -q "RunBatchVerification start" "${SUMMARY_LOG}" 2>/dev/null \
      && ! grep -q "BATCH_BEGIN" "${SUMMARY_LOG}" 2>/dev/null; then
      phase="バッチ開始直後・GAME state 待ち"
    elif grep -q "layout apply not ready" "${SUMMARY_LOG}" 2>/dev/null; then
      phase="味方スポーン/layout apply 待ち（タイムアウト注意）"
    fi
  fi

  if [[ -f "${LOG_DIR}/goap-batch-pending-exit.txt" ]]; then
    phase="終了コード書き込み済み → Domain Reload / Editor 終了中"
  fi

  printf '[goap-ci +%3ss] %s\n' "${elapsed}" "${phase}"
}

monitor_batch_progress() {
  local unity_pid="$1"
  local start_ts
  start_ts="$(date +%s)"

  while kill -0 "${unity_pid}" 2>/dev/null; do
    local now elapsed
    now="$(date +%s)"
    elapsed=$((now - start_ts))
    describe_batch_phase "${elapsed}"
    sleep "${PROGRESS_INTERVAL}"
  done
}

run_editmode_tests() {
  local unity_bin
  unity_bin="$(resolve_unity)"
  local log_file="${LOG_DIR}/goap-editmode-tests.log"

  echo "[goap-ci] === EditMode tests (約30秒・期待 ${GOAP_EDITMODE_EXPECTED_TESTS} 件) ==="
  echo "[goap-ci] filter=${GOAP_EDITMODE_TEST_FILTER}"
  echo "[goap-ci] log=${log_file}"
  "${unity_bin}" \
    -batchmode \
    -nographics \
    -projectPath "${PROJECT_ROOT}" \
    -runTests \
    -testPlatform EditMode \
    -testFilter "${GOAP_EDITMODE_TEST_FILTER}" \
    -testResults "${LOG_DIR}/goap-editmode-results.xml" \
    -logFile "${log_file}"

  if [[ -f "${LOG_DIR}/goap-editmode-results.xml" ]]; then
    grep -E 'test-run.*(passed|failed)' "${LOG_DIR}/goap-editmode-results.xml" | head -1 || true
  fi
  echo "[goap-ci] EditMode tests PASSED"
}

run_batch_verify() {
  local profile_token="${1:?}"
  goap_ci_resolve_batch_profile "${profile_token}"

  local unity_bin
  unity_bin="$(resolve_unity)"

  rm -f "${LOG_DIR}/${GOAP_PROFILE_RESULT_FILE}"
  goap_ci_clear_batch_markers "${LOG_DIR}"

  echo "[goap-ci] === Batch verify (${GOAP_PROFILE_LABEL}) ==="
  echo "[goap-ci] ${GOAP_PROFILE_FLAG} (${LOG_DIR}/${GOAP_PROFILE_LOG_FILE})"
  echo "[goap-ci] 進捗は ${PROGRESS_INTERVAL}s ごとに表示します"

  set +e
  "${unity_bin}" \
    -batchmode \
    -nographics \
    -projectPath "${PROJECT_ROOT}" \
    "${GOAP_PROFILE_FLAG}" \
    -logFile "${LOG_DIR}/${GOAP_PROFILE_LOG_FILE}" &
  local unity_pid=$!
  monitor_batch_progress "${unity_pid}"
  wait "${unity_pid}"
  local exit_code=$?
  set -e

  echo "[goap-ci] Unity プロセス終了 (exit=${exit_code})"

  if [[ -f "${LOG_DIR}/${GOAP_PROFILE_RESULT_FILE}" ]]; then
    cat "${LOG_DIR}/${GOAP_PROFILE_RESULT_FILE}"
  fi

  # shellcheck source=resolve-batch-verify-result.sh
  source "${SCRIPT_DIR}/resolve-batch-verify-result.sh"

  if resolve_batch_verify_success "${PROJECT_ROOT}" "${GOAP_PROFILE_TOKEN}"; then
    echo "[goap-ci] batch verify PASSED (${GOAP_PROFILE_LABEL})"
    return 0
  fi

  echo "[goap-ci] batch verify FAILED (${GOAP_PROFILE_LABEL}, exit=${exit_code})" >&2
  goap_ci_report_batch_failure "${PROJECT_ROOT}" "${GOAP_PROFILE_TOKEN}" "${LOG_DIR}/${GOAP_PROFILE_LOG_FILE}"
  return 1
}

if goap_ci_mode_runs_editmode "${MODE}"; then
  run_editmode_tests
fi

batch_tokens=()
while IFS= read -r token; do
  [[ -n "${token}" ]] && batch_tokens+=("${token}")
done < <(goap_ci_batch_profiles_for_mode "${MODE}")

if [[ ${#batch_tokens[@]} -gt 0 && "${MODE}" == "all" || "${MODE}" == "batch" ]]; then
  if goap_ci_mode_runs_editmode "${MODE}"; then
    echo ""
  fi
fi

for i in "${!batch_tokens[@]}"; do
  if [[ "${i}" -gt 0 ]]; then
    echo ""
  fi
  run_batch_verify "${batch_tokens[$i]}"
done

echo "[goap-ci] === ALL PASSED ==="
