#!/usr/bin/env bash
set -euo pipefail

PROJECT_ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
UNITY_VERSION="${UNITY_VERSION:-6000.2.7f2}"
IMAGE="${GOAP_UNITY_DOCKER_IMAGE:-unityci/editor:ubuntu-${UNITY_VERSION}-base-3}"
LOG_DIR="${PROJECT_ROOT}/Logs"
mkdir -p "${LOG_DIR}"

if [[ -z "${UNITY_LICENSE:-}" ]]; then
  echo "UNITY_LICENSE is required for Docker batch verify." >&2
  exit 2
fi

if [[ -z "${UNITY_EMAIL:-}" || -z "${UNITY_PASSWORD:-}" ]]; then
  echo "UNITY_EMAIL and UNITY_PASSWORD are required (Unity Personal license)." >&2
  exit 2
fi

echo "[goap-ci] docker batch verify image=${IMAGE}"

rm -f \
  "${LOG_DIR}/goap-batch-result.txt" \
  "${LOG_DIR}/goap-batch-pending-exit.txt" \
  "${LOG_DIR}/goap-batch-started.marker"

set +e
docker run --rm \
  -e UNITY_LICENSE \
  -e UNITY_EMAIL \
  -e UNITY_PASSWORD \
  -v "${PROJECT_ROOT}:/project" \
  -w /project \
  "${IMAGE}" \
  unity-editor \
    -batchmode \
    -nographics \
    -projectPath /project \
    -goapBatchVerify \
    -logFile /project/Logs/goap-batch-verify.log
exit_code=$?
set -e

if [[ -f "${LOG_DIR}/goap-batch-result.txt" ]]; then
  cat "${LOG_DIR}/goap-batch-result.txt"
fi

if [[ "${exit_code}" -ne 0 ]]; then
  echo "[goap-ci] docker batch verify failed (exit=${exit_code})" >&2
  exit "${exit_code}"
fi

echo "[goap-ci] docker batch verify passed"
