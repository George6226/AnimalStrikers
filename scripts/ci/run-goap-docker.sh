#!/usr/bin/env bash
# EditMode テストと GOAP バッチ検証を 1 回の Docker 実行で行う（ライセンス有効化・Library 再利用）。
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
# shellcheck source=goap-ci-config.sh
source "${SCRIPT_DIR}/goap-ci-config.sh"

MODE="${1:-all}"
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
IMAGE="${GOAP_DOCKER_IMAGE}"
LOG_DIR="${PROJECT_ROOT}/Logs"
EDITMODE_TIMEOUT="${GOAP_EDITMODE_DOCKER_TIMEOUT:-2700}"
BATCH_TIMEOUT="${GOAP_UNITY_DOCKER_TIMEOUT:-2400}"
mkdir -p "${LOG_DIR}"

if ! goap_ci_mode_valid "${MODE}"; then
  goap_ci_print_usage "$(basename "$0")"
  exit 2
fi

if [[ -z "${UNITY_EMAIL:-}" || -z "${UNITY_PASSWORD:-}" ]]; then
  echo "UNITY_EMAIL and UNITY_PASSWORD are required." >&2
  exit 2
fi

if [[ -z "${UNITY_LICENSE:-}" && -z "${UNITY_SERIAL:-}" ]]; then
  echo "Set UNITY_LICENSE (CI .ulf XML) or UNITY_SERIAL in GitHub Secrets." >&2
  exit 2
fi

echo "[goap-ci] docker goap-ci mode=${MODE} image=${IMAGE}"

if goap_ci_mode_runs_batch "${MODE}"; then
  local_entry=""
  for local_entry in "${GOAP_BATCH_PROFILES[@]}"; do
    IFS='|' read -r _ _ result _ _ <<< "${local_entry}"
    rm -f "${LOG_DIR}/${result}"
  done
  goap_ci_clear_batch_markers "${LOG_DIR}"
  rm -f \
    "${LOG_DIR}/goap-main-npc-attack-pending-exit.txt" \
    "${LOG_DIR}/goap-main-npc-attack-started.marker"
fi

docker_env=( -e UNITY_EMAIL -e UNITY_PASSWORD )
if [[ -n "${UNITY_SERIAL:-}" ]]; then
  docker_env+=( -e UNITY_SERIAL )
  echo "[goap-ci] license mode: UNITY_SERIAL (UNITY_LICENSE is not passed to Docker)"
elif [[ -n "${UNITY_LICENSE:-}" ]]; then
  docker_env+=( -e UNITY_LICENSE )
  echo "[goap-ci] license mode: UNITY_LICENSE"
fi

set +e
docker run --rm \
  "${docker_env[@]}" \
  -v "${PROJECT_ROOT}:/project" \
  -w /project \
  --entrypoint bash \
  "${IMAGE}" \
  -ec "$(cat <<INNER
set -euo pipefail
source /project/scripts/ci/goap-ci-config.sh
source /project/scripts/ci/docker-unity-personal.sh
unity_docker_activate_personal /project/Logs/ci-unity-activate.log

MODE="${MODE}"
EDITMODE_FILTER="${GOAP_EDITMODE_TEST_FILTER}"
EDITMODE_TIMEOUT="${EDITMODE_TIMEOUT}"
BATCH_TIMEOUT="${BATCH_TIMEOUT}"

if goap_ci_mode_runs_editmode "\${MODE}"; then
  echo "[goap-ci] starting EditMode tests at \$(date -u +%H:%M:%S)"
  set +e
  timeout "\${EDITMODE_TIMEOUT}" unity-editor \
    -batchmode \
    -nographics \
    -projectPath /project \
    -runTests \
    -testPlatform EditMode \
    -testFilter "\${EDITMODE_FILTER}" \
    -testResults /project/Logs/goap-editmode-results.xml \
    -logFile /project/Logs/goap-editmode-tests.log
  editmode_exit=\$?
  set -e
  if [[ "\${editmode_exit}" -ne 0 ]]; then
    echo "[goap-ci] EditMode tests failed (exit=\${editmode_exit})" >&2
    exit "\${editmode_exit}"
  fi
  echo "[goap-ci] EditMode tests passed"
fi

run_batch_profile() {
  local profile_flag="\$1"
  local log_name="\$2"
  echo "[goap-ci] starting batch verify (\${profile_flag}) at \$(date -u +%H:%M:%S)"
  set +e
  timeout "\${BATCH_TIMEOUT}" unity-editor \
    -batchmode \
    -nographics \
    -projectPath /project \
    "\${profile_flag}" \
    -logFile "/project/Logs/\${log_name}"
  local exit_code=\$?
  set -e
  return "\${exit_code}"
}

batch_tokens=()
while IFS= read -r token; do
  [[ -n "\${token}" ]] && batch_tokens+=("\${token}")
done < <(goap_ci_batch_profiles_for_mode "\${MODE}")

for token in "\${batch_tokens[@]}"; do
  goap_ci_resolve_batch_profile "\${token}"
  goap_ci_clear_profile_markers /project/Logs "\${token}"
  set +e
  run_batch_profile "\${GOAP_PROFILE_FLAG}" "\${GOAP_PROFILE_LOG_FILE}"
  batch_exit=\$?
  set -e
  if [[ "\${batch_exit}" -ne 0 ]]; then
    echo "[goap-ci] batch verify failed (\${GOAP_PROFILE_LABEL}, exit=\${batch_exit})" >&2
    exit "\${batch_exit}"
  fi
done
INNER
)"
docker_exit=$?
set -e

# shellcheck source=resolve-batch-verify-result.sh
source "${SCRIPT_DIR}/resolve-batch-verify-result.sh"

if goap_ci_mode_runs_editmode "${MODE}"; then
  if [[ -f "${LOG_DIR}/goap-editmode-results.xml" ]]; then
    grep -E 'test-run.*(passed|failed)' "${LOG_DIR}/goap-editmode-results.xml" | head -1 || true
  fi
fi

batch_tokens=()
while IFS= read -r token; do
  [[ -n "${token}" ]] && batch_tokens+=("${token}")
done < <(goap_ci_batch_profiles_for_mode "${MODE}")

for token in "${batch_tokens[@]}"; do
  if [[ "${docker_exit}" -ne 0 ]]; then
    break
  fi

  goap_ci_resolve_batch_profile "${token}"

  if [[ -f "${LOG_DIR}/${GOAP_PROFILE_RESULT_FILE}" ]]; then
    cat "${LOG_DIR}/${GOAP_PROFILE_RESULT_FILE}"
  fi

  if resolve_batch_verify_success "${PROJECT_ROOT}" "${GOAP_PROFILE_TOKEN}"; then
    echo "[goap-ci] docker batch verify passed (${GOAP_PROFILE_LABEL})"
    docker_exit=0
  else
    docker_exit=1
    goap_ci_report_batch_failure "${PROJECT_ROOT}" "${GOAP_PROFILE_TOKEN}" "${LOG_DIR}/${GOAP_PROFILE_LOG_FILE}"
    echo "[goap-ci] docker batch verify failed (${GOAP_PROFILE_LABEL})" >&2
  fi
done

if [[ "${docker_exit}" -ne 0 ]]; then
  if [[ -f "${LOG_DIR}/ci-unity-activate.log" ]]; then
    tail -40 "${LOG_DIR}/ci-unity-activate.log" >&2 || true
  fi
  exit "${docker_exit}"
fi

echo "[goap-ci] docker goap-ci passed (mode=${MODE})"
