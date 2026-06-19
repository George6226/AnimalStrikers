#!/usr/bin/env bash
set -euo pipefail

MODE="${1:-all}"
PROJECT_ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
UNITY_VERSION="${UNITY_VERSION:-6000.2.7f2}"
LOG_DIR="${PROJECT_ROOT}/Logs"
DIAG_LOG="${PROJECT_ROOT}/Assets/DebugLog/GoapDiag_latest.txt"
SUMMARY_LOG="${PROJECT_ROOT}/Assets/DebugLog/GoapSummary_latest.txt"
PROGRESS_INTERVAL="${GOAP_CI_PROGRESS_INTERVAL:-15}"
mkdir -p "${LOG_DIR}"

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
      local last_pass last_begin count
      last_pass="$(grep "SELECTION_PASS" "${DIAG_LOG}" | tail -1 || true)"
      last_begin="$(grep "BATCH_BEGIN" "${DIAG_LOG}" | tail -1 || true)"
      count="$(grep -c "SELECTION_PASS" "${DIAG_LOG}" 2>/dev/null || echo 0)"
      if [[ -n "${last_pass}" ]]; then
        phase="パターン検証中 (${count}/11 PASS) ${last_pass##*========== }"
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

  echo "[goap-ci] === EditMode tests (約30秒) ==="
  echo "[goap-ci] 幾何判定 + ログパーサー (${log_file})"
  "${unity_bin}" \
    -batchmode \
    -nographics \
    -projectPath "${PROJECT_ROOT}" \
    -runTests \
    -testPlatform EditMode \
    -testFilter "GoapBatchVerificationLogParserTests|TeammateNpcSupportPlanningEditModeTests" \
    -testResults "${LOG_DIR}/goap-editmode-results.xml" \
    -logFile "${log_file}"

  if [[ -f "${LOG_DIR}/goap-editmode-results.xml" ]]; then
    grep -E 'test-run.*(passed|failed)' "${LOG_DIR}/goap-editmode-results.xml" | head -1 || true
  fi
  echo "[goap-ci] EditMode tests PASSED"
}

run_batch_verify() {
  local unity_bin
  unity_bin="$(resolve_unity)"
  local log_file="${LOG_DIR}/goap-batch-verify.log"

  rm -f \
    "${LOG_DIR}/goap-batch-result.txt" \
    "${LOG_DIR}/goap-batch-pending-exit.txt" \
    "${LOG_DIR}/goap-batch-started.marker"

  echo "[goap-ci] === Batch verify (約2〜4分) ==="
  echo "[goap-ci] 統合本番選出 11 パターン (${log_file})"
  echo "[goap-ci] 進捗は ${PROGRESS_INTERVAL}s ごとに表示します"

  set +e
  "${unity_bin}" \
    -batchmode \
    -nographics \
    -projectPath "${PROJECT_ROOT}" \
    -goapBatchVerify \
    -logFile "${log_file}" &
  local unity_pid=$!
  monitor_batch_progress "${unity_pid}"
  wait "${unity_pid}"
  local exit_code=$?
  set -e

  echo "[goap-ci] Unity プロセス終了 (exit=${exit_code})"

  if [[ -f "${LOG_DIR}/goap-batch-result.txt" ]]; then
    cat "${LOG_DIR}/goap-batch-result.txt"
  fi

  if [[ "${exit_code}" -ne 0 ]]; then
    echo "[goap-ci] batch verify FAILED (exit=${exit_code})" >&2
    echo "[goap-ci] 直近ログ:" >&2
    tail -5 "${DIAG_LOG}" 2>/dev/null || true
    exit "${exit_code}"
  fi

  echo "[goap-ci] batch verify PASSED"
}

case "${MODE}" in
  editmode)
    run_editmode_tests
    ;;
  batch)
    run_batch_verify
    ;;
  all)
    run_editmode_tests
    echo ""
    run_batch_verify
    ;;
  *)
    echo "Usage: $0 [editmode|batch|all]" >&2
    exit 2
    ;;
esac

echo "[goap-ci] === ALL PASSED ==="
