#!/usr/bin/env bash
# EditMode テストと GOAP バッチ検証を 1 回の Docker 実行で行う（ライセンス有効化・Library 再利用）。
set -euo pipefail

MODE="${1:-all}"
PROJECT_ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
UNITY_VERSION="${UNITY_VERSION:-6000.2.7f2}"
IMAGE="${GOAP_UNITY_DOCKER_IMAGE:-unityci/editor:ubuntu-${UNITY_VERSION}-base-3}"
LOG_DIR="${PROJECT_ROOT}/Logs"
SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
EDITMODE_TIMEOUT="${GOAP_EDITMODE_DOCKER_TIMEOUT:-2700}"
BATCH_TIMEOUT="${GOAP_UNITY_DOCKER_TIMEOUT:-2400}"
mkdir -p "${LOG_DIR}"

case "${MODE}" in
  all | editmode | batch | batch-combined | batch-wing) ;;
  *)
    echo "usage: $0 [all|editmode|batch|batch-combined|batch-wing]" >&2
    exit 2
    ;;
esac

if [[ -z "${UNITY_EMAIL:-}" || -z "${UNITY_PASSWORD:-}" ]]; then
  echo "UNITY_EMAIL and UNITY_PASSWORD are required." >&2
  exit 2
fi

if [[ -z "${UNITY_LICENSE:-}" && -z "${UNITY_SERIAL:-}" ]]; then
  echo "Set UNITY_LICENSE (CI .ulf XML) or UNITY_SERIAL in GitHub Secrets." >&2
  exit 2
fi

echo "[goap-ci] docker goap-ci mode=${MODE} image=${IMAGE}"

if [[ "${MODE}" == "batch" || "${MODE}" == "all" || "${MODE}" == "batch-combined" || "${MODE}" == "batch-wing" ]]; then
  rm -f \
    "${LOG_DIR}/goap-batch-result.txt" \
    "${LOG_DIR}/goap-batch-wing-result.txt" \
    "${LOG_DIR}/goap-batch-pending-exit.txt" \
    "${LOG_DIR}/goap-batch-started.marker" \
    "${LOG_DIR}/goap-batch-profile.txt"
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
source /project/scripts/ci/docker-unity-personal.sh
unity_docker_activate_personal /project/Logs/ci-unity-activate.log

editmode_exit=0
combined_exit=0
wing_exit=0

if [[ "${MODE}" == "editmode" || "${MODE}" == "all" ]]; then
  echo "[goap-ci] starting EditMode tests at \$(date -u +%H:%M:%S)"
  set +e
  timeout ${EDITMODE_TIMEOUT} unity-editor \
    -batchmode \
    -nographics \
    -projectPath /project \
    -runTests \
    -testPlatform EditMode \
    -testFilter "GoapBatchVerificationLogParserTests|TeammateNpcSupportPlanningEditModeTests" \
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
  timeout ${BATCH_TIMEOUT} unity-editor \
    -batchmode \
    -nographics \
    -projectPath /project \
    "\${profile_flag}" \
    -logFile "/project/Logs/\${log_name}"
  local exit_code=\$?
  set -e
  return "\${exit_code}"
}

if [[ "${MODE}" == "batch" || "${MODE}" == "all" || "${MODE}" == "batch-combined" ]]; then
  rm -f /project/Logs/goap-batch-pending-exit.txt /project/Logs/goap-batch-started.marker /project/Logs/goap-batch-profile.txt
  set +e
  run_batch_profile "-goapBatchVerify=combined" "goap-batch-verify.log"
  combined_exit=\$?
  set -e
  if [[ "\${combined_exit}" -ne 0 ]]; then
    echo "[goap-ci] combined batch verify unity failed (exit=\${combined_exit})" >&2
    exit "\${combined_exit}"
  fi
fi

if [[ "${MODE}" == "batch" || "${MODE}" == "all" || "${MODE}" == "batch-wing" ]]; then
  rm -f /project/Logs/goap-batch-pending-exit.txt /project/Logs/goap-batch-started.marker /project/Logs/goap-batch-profile.txt
  set +e
  run_batch_profile "-goapBatchVerify=wingDrive" "goap-batch-wing-verify.log"
  wing_exit=\$?
  set -e
  if [[ "\${wing_exit}" -ne 0 ]]; then
    echo "[goap-ci] wing drive batch verify unity failed (exit=\${wing_exit})" >&2
    exit "\${wing_exit}"
  fi
fi
INNER
)"
docker_exit=$?
set -e

# shellcheck source=resolve-batch-verify-result.sh
source "${SCRIPT_DIR}/resolve-batch-verify-result.sh"

if [[ "${MODE}" == "editmode" || "${MODE}" == "all" ]]; then
  if [[ -f "${LOG_DIR}/goap-editmode-results.xml" ]]; then
    grep -E 'test-run.*(passed|failed)' "${LOG_DIR}/goap-editmode-results.xml" | head -1 || true
  fi
fi

if [[ "${MODE}" == "batch" || "${MODE}" == "all" || "${MODE}" == "batch-combined" ]]; then
  if [[ -f "${LOG_DIR}/goap-batch-result.txt" ]]; then
    cat "${LOG_DIR}/goap-batch-result.txt"
  fi

  if resolve_batch_verify_success "${PROJECT_ROOT}" combined; then
    if [[ "${docker_exit}" -ne 0 ]]; then
      echo "[goap-ci] combined batch passed by result artifacts (unity exit=${docker_exit})"
    else
      echo "[goap-ci] docker combined batch verify passed"
    fi
    docker_exit=0
  else
    docker_exit=1
    if [[ -f "${LOG_DIR}/goap-batch-verify.log" ]]; then
      tail -40 "${LOG_DIR}/goap-batch-verify.log" >&2 || true
    fi
    for diag in \
      "${LOG_DIR}/GoapDiag_latest.txt" \
      "${PROJECT_ROOT}/Assets/DebugLog/GoapDiag_latest.txt"; do
      if [[ -f "${diag}" ]]; then
        echo "[goap-ci] --- ${diag} (combined BATCH markers) ---" >&2
        grep -E 'BATCH_|SELECTION_|RUNTIME_|GOAP_BATCH_RUNNER|GoapDebugPlayBootstrap|GameDataInitializer' "${diag}" | tail -40 >&2 || true
      fi
    done
    echo "[goap-ci] docker combined batch verify failed" >&2
  fi
fi

if [[ "${docker_exit}" -eq 0 && ( "${MODE}" == "batch" || "${MODE}" == "all" || "${MODE}" == "batch-wing" ) ]]; then
  if [[ -f "${LOG_DIR}/goap-batch-wing-result.txt" ]]; then
    cat "${LOG_DIR}/goap-batch-wing-result.txt"
  fi

  if resolve_batch_verify_success "${PROJECT_ROOT}" wingDrive; then
    if [[ "${docker_exit}" -ne 0 ]]; then
      echo "[goap-ci] wing drive batch passed by result artifacts (unity exit=${docker_exit})"
    else
      echo "[goap-ci] docker wing drive batch verify passed"
    fi
    docker_exit=0
  else
    docker_exit=1
    if [[ -f "${LOG_DIR}/goap-batch-wing-verify.log" ]]; then
      tail -40 "${LOG_DIR}/goap-batch-wing-verify.log" >&2 || true
    fi
    for diag in \
      "${LOG_DIR}/GoapDiag_wing_latest.txt" \
      "${PROJECT_ROOT}/Assets/DebugLog/GoapDiag_latest.txt"; do
      if [[ -f "${diag}" ]]; then
        echo "[goap-ci] --- ${diag} (wing BATCH markers) ---" >&2
        grep -E 'BATCH_|SELECTION_|RUNTIME_|GOAP_BATCH_RUNNER|AUTO_DRIVE|GoapDebugPlayBootstrap' "${diag}" | tail -40 >&2 || true
      fi
    done
    echo "[goap-ci] docker wing drive batch verify failed" >&2
  fi
fi

if [[ "${docker_exit}" -ne 0 ]]; then
  if [[ -f "${LOG_DIR}/ci-unity-activate.log" ]]; then
    tail -40 "${LOG_DIR}/ci-unity-activate.log" >&2 || true
  fi
  exit "${docker_exit}"
fi

echo "[goap-ci] docker goap-ci passed (mode=${MODE})"
