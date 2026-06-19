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
  all | editmode | batch) ;;
  *)
    echo "usage: $0 [all|editmode|batch]" >&2
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

if [[ "${MODE}" == "batch" || "${MODE}" == "all" ]]; then
  rm -f \
    "${LOG_DIR}/goap-batch-result.txt" \
    "${LOG_DIR}/goap-batch-pending-exit.txt" \
    "${LOG_DIR}/goap-batch-started.marker"
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
batch_exit=0

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

if [[ "${MODE}" == "batch" || "${MODE}" == "all" ]]; then
  echo "[goap-ci] starting batch verify at \$(date -u +%H:%M:%S)"
  set +e
  timeout ${BATCH_TIMEOUT} unity-editor \
    -batchmode \
    -nographics \
    -projectPath /project \
    -goapBatchVerify \
    -logFile /project/Logs/goap-batch-verify.log
  batch_exit=\$?
  set -e
fi
INNER
)"
docker_exit=$?
set -e

if [[ "${MODE}" == "editmode" || "${MODE}" == "all" ]]; then
  if [[ -f "${LOG_DIR}/goap-editmode-results.xml" ]]; then
    grep -E 'test-run.*(passed|failed)' "${LOG_DIR}/goap-editmode-results.xml" | head -1 || true
  fi
fi

if [[ "${MODE}" == "batch" || "${MODE}" == "all" ]]; then
  if [[ -f "${LOG_DIR}/goap-batch-result.txt" ]]; then
    cat "${LOG_DIR}/goap-batch-result.txt"
  fi

  # shellcheck source=resolve-batch-verify-result.sh
  source "${SCRIPT_DIR}/resolve-batch-verify-result.sh"
  if resolve_batch_verify_success "${PROJECT_ROOT}"; then
    if [[ "${docker_exit}" -ne 0 ]]; then
      echo "[goap-ci] batch verify passed by result artifacts (unity exit=${docker_exit})"
    else
      echo "[goap-ci] docker batch verify passed"
    fi
    docker_exit=0
  elif [[ "${docker_exit}" -ne 0 ]]; then
    if [[ -f "${LOG_DIR}/goap-batch-verify.log" ]]; then
      tail -40 "${LOG_DIR}/goap-batch-verify.log" >&2 || true
    fi
    for diag in \
      "${LOG_DIR}/GoapDiag_latest.txt" \
      "${PROJECT_ROOT}/Assets/DebugLog/GoapDiag_latest.txt"; do
      if [[ -f "${diag}" ]]; then
        echo "[goap-ci] --- ${diag} (BATCH markers) ---" >&2
        grep -E 'BATCH_|SELECTION_|RUNTIME_|GOAP_BATCH_RUNNER|GoapDebugPlayBootstrap' "${diag}" | tail -30 >&2 || true
      fi
    done
    if [[ "${docker_exit}" -eq 124 ]]; then
      echo "[goap-ci] docker batch verify timed out after ${BATCH_TIMEOUT}s" >&2
    else
      echo "[goap-ci] docker batch verify failed (exit=${docker_exit})" >&2
    fi
  fi
fi

if [[ "${docker_exit}" -ne 0 ]]; then
  if [[ -f "${LOG_DIR}/ci-unity-activate.log" ]]; then
    tail -40 "${LOG_DIR}/ci-unity-activate.log" >&2 || true
  fi
  exit "${docker_exit}"
fi

echo "[goap-ci] docker goap-ci passed (mode=${MODE})"
