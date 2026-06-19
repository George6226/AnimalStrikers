#!/usr/bin/env bash
# goap-batch-result.txt / GoapDiag からバッチ合格を判定する（Unity 終了ハング時の CI フォールバック）。
set -euo pipefail

resolve_batch_verify_success() {
  local project_root="${1:?}"
  local log_dir="${project_root}/Logs"

  if [[ -f "${log_dir}/goap-batch-result.txt" ]]; then
    if grep -q '^PASS:' "${log_dir}/goap-batch-result.txt"; then
      echo "[goap-ci] batch PASS from goap-batch-result.txt"
      cat "${log_dir}/goap-batch-result.txt"
      return 0
    fi
    if grep -q '^FAIL:' "${log_dir}/goap-batch-result.txt"; then
      echo "[goap-ci] batch FAIL from goap-batch-result.txt" >&2
      cat "${log_dir}/goap-batch-result.txt" >&2
      return 1
    fi
  fi

  local diag=""
  for candidate in \
    "${log_dir}/GoapDiag_latest.txt" \
    "${project_root}/Assets/DebugLog/GoapDiag_latest.txt"; do
    if [[ -f "${candidate}" ]]; then
      diag="${candidate}"
      break
    fi
  done

  if [[ -z "${diag}" ]]; then
    return 1
  fi

  if grep -q 'BATCH_ABORT\|SELECTION_FAIL\|RUNTIME_FAIL' "${diag}"; then
    echo "[goap-ci] batch failure markers found in ${diag}" >&2
    return 1
  fi

  if ! grep -q 'BATCH_COMPLETE' "${diag}"; then
    return 1
  fi

  local total_line
  total_line="$(grep -E 'SELECTION_TOTAL|RUNTIME_TOTAL' "${diag}" | tail -1 || true)"
  if [[ -z "${total_line}" ]]; then
    return 1
  fi

  local pass_count eval_count
  if [[ "${total_line}" =~ (SELECTION_TOTAL|RUNTIME_TOTAL)[[:space:]]+([0-9]+)/([0-9]+) ]]; then
    pass_count="${BASH_REMATCH[2]}"
    eval_count="${BASH_REMATCH[3]}"
  else
    return 1
  fi

  if [[ "${eval_count}" -gt 0 && "${pass_count}" == "${eval_count}" ]]; then
    echo "[goap-ci] batch PASS from ${diag}: ${total_line}"
    return 0
  fi

  echo "[goap-ci] batch total not satisfied: ${total_line}" >&2
  return 1
}

if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
  resolve_batch_verify_success "${1:-.}"
fi
