#!/usr/bin/env bash
set -euo pipefail

PROJECT_ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
UNITY_VERSION="${UNITY_VERSION:-6000.2.7f2}"
IMAGE="${GOAP_UNITY_DOCKER_IMAGE:-unityci/editor:ubuntu-${UNITY_VERSION}-base-3}"
LOG_DIR="${PROJECT_ROOT}/Logs"
mkdir -p "${LOG_DIR}"

if [[ -z "${UNITY_EMAIL:-}" || -z "${UNITY_PASSWORD:-}" ]]; then
  echo "UNITY_EMAIL and UNITY_PASSWORD are required (Unity Personal license)." >&2
  exit 2
fi

echo "[goap-ci] docker editmode tests image=${IMAGE}"

set +e
docker run --rm \
  -e UNITY_EMAIL \
  -e UNITY_PASSWORD \
  -v "${PROJECT_ROOT}:/project" \
  -w /project \
  --entrypoint bash \
  "${IMAGE}" \
  -ec "$(cat <<'INNER'
set -euo pipefail
source /project/scripts/ci/docker-unity-personal.sh
unity_docker_activate_personal /project/Logs/ci-unity-activate.log
xvfb-run --auto-servernum --server-args='-screen 0 640x480x24' \
  unity-editor \
    -batchmode \
    -nographics \
    -projectPath /project \
    -runTests \
    -testPlatform EditMode \
    -testFilter "GoapBatchVerificationLogParserTests|TeammateNpcSupportPlanningEditModeTests" \
    -testResults /project/Logs/goap-editmode-results.xml \
    -logFile /project/Logs/goap-editmode-tests.log
INNER
)"
exit_code=$?
set -e

if [[ -f "${LOG_DIR}/goap-editmode-results.xml" ]]; then
  grep -E 'test-run.*(passed|failed)' "${LOG_DIR}/goap-editmode-results.xml" | head -1 || true
fi

if [[ "${exit_code}" -ne 0 ]]; then
  echo "[goap-ci] docker editmode tests failed (exit=${exit_code})" >&2
  exit "${exit_code}"
fi

echo "[goap-ci] docker editmode tests passed"
