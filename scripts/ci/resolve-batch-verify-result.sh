#!/usr/bin/env bash
# goap-batch-result.txt / GoapDiag からバッチ合格を判定する（Unity 終了ハング時の CI フォールバック）。
set -euo pipefail

resolve_batch_verify_success() {
  local project_root="${1:?}"
  local profile="${2:-combined}"
  local log_dir="${project_root}/Logs"
  local result_file="goap-batch-result.txt"
  local diag_candidates=(
    "${log_dir}/GoapDiag_latest.txt"
    "${project_root}/Assets/DebugLog/GoapDiag_latest.txt"
  )

  case "${profile}" in
    wing | wingDrive | WingDrive)
      result_file="goap-batch-wing-result.txt"
      diag_candidates=(
        "${log_dir}/GoapDiag_wing_latest.txt"
        "${project_root}/Assets/DebugLog/GoapDiag_latest.txt"
        "${log_dir}/GoapDiag_latest.txt"
      )
      ;;
    cf | cfDrive | CfDrive)
      result_file="goap-batch-cf-drive-result.txt"
      diag_candidates=(
        "${log_dir}/GoapDiag_cf_drive_latest.txt"
        "${project_root}/Assets/DebugLog/GoapDiag_latest.txt"
        "${log_dir}/GoapDiag_latest.txt"
      )
      ;;
    combined | *)
      ;;
  esac

  if [[ -f "${log_dir}/${result_file}" ]]; then
    if grep -q '^PASS:' "${log_dir}/${result_file}"; then
      echo "[goap-ci] batch PASS (${profile}) from ${result_file}"
      cat "${log_dir}/${result_file}"
      return 0
    fi
    if grep -q '^FAIL:' "${log_dir}/${result_file}"; then
      echo "[goap-ci] batch FAIL (${profile}) from ${result_file}" >&2
      cat "${log_dir}/${result_file}" >&2
      return 1
    fi
  fi

  local diag=""
  for candidate in "${diag_candidates[@]}"; do
    if [[ -f "${candidate}" ]]; then
      diag="${candidate}"
      break
    fi
  done

  if [[ -z "${diag}" ]]; then
    return 1
  fi

  if grep -q 'BATCH_ABORT\|SELECTION_FAIL\|RUNTIME_FAIL' "${diag}"; then
    echo "[goap-ci] batch failure markers found in ${diag} (${profile})" >&2
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
    echo "[goap-ci] batch PASS (${profile}) from ${diag}: ${total_line}"
    return 0
  fi

  echo "[goap-ci] batch total not satisfied (${profile}): ${total_line}" >&2
  return 1
}

if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
  resolve_batch_verify_success "${1:-.}" "${2:-combined}"
fi
