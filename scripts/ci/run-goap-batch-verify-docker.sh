#!/usr/bin/env bash
set -euo pipefail

PROJECT_ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
UNITY_VERSION="${UNITY_VERSION:-6000.2.7f2}"
IMAGE="${GOAP_UNITY_DOCKER_IMAGE:-unityci/editor:ubuntu-${UNITY_VERSION}-base-3}"
LOG_DIR="${PROJECT_ROOT}/Logs"
UNITY_TIMEOUT="${GOAP_UNITY_DOCKER_TIMEOUT:-3300}"
mkdir -p "${LOG_DIR}"

if [[ -z "${UNITY_EMAIL:-}" || -z "${UNITY_PASSWORD:-}" ]]; then
  echo "UNITY_EMAIL and UNITY_PASSWORD are required." >&2
  exit 2
fi

if [[ -z "${UNITY_LICENSE:-}" && -z "${UNITY_SERIAL:-}" ]]; then
  echo "Set UNITY_LICENSE (CI .ulf XML) or UNITY_SERIAL (Hub の本物シリアル) in GitHub Secrets." >&2
  exit 2
fi

echo "[goap-ci] docker batch verify image=${IMAGE} timeout=${UNITY_TIMEOUT}s"

rm -f \
  "${LOG_DIR}/goap-batch-result.txt" \
  "${LOG_DIR}/goap-batch-pending-exit.txt" \
  "${LOG_DIR}/goap-batch-started.marker"

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
echo "[goap-ci] starting batch verify at \$(date -u +%H:%M:%S)"
timeout ${UNITY_TIMEOUT} unity-editor \
  -batchmode \
  -nographics \
  -projectPath /project \
  -goapBatchVerify \
  -logFile /project/Logs/goap-batch-verify.log
INNER
)"
exit_code=$?
set -e

if [[ -f "${LOG_DIR}/goap-batch-result.txt" ]]; then
  cat "${LOG_DIR}/goap-batch-result.txt"
fi

if [[ "${exit_code}" -ne 0 ]]; then
  if [[ -f "${LOG_DIR}/goap-batch-verify.log" ]]; then
    tail -40 "${LOG_DIR}/goap-batch-verify.log" >&2 || true
  fi
  if [[ -f "${LOG_DIR}/ci-unity-activate.log" ]]; then
    tail -40 "${LOG_DIR}/ci-unity-activate.log" >&2 || true
  fi
fi

if [[ "${exit_code}" -eq 124 ]]; then
  echo "[goap-ci] docker batch verify timed out after ${UNITY_TIMEOUT}s" >&2
  exit 124
fi

if [[ "${exit_code}" -ne 0 ]]; then
  echo "[goap-ci] docker batch verify failed (exit=${exit_code})" >&2
  exit "${exit_code}"
fi

echo "[goap-ci] docker batch verify passed"
