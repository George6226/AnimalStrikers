#!/usr/bin/env bash
set -euo pipefail

PROJECT_ROOT="$(cd "$(dirname "$0")/../.." && pwd)"
UNITY_VERSION="${UNITY_VERSION:-6000.2.7f2}"
IMAGE="${GOAP_UNITY_DOCKER_IMAGE:-unityci/editor:ubuntu-${UNITY_VERSION}-base-3}"
LOG_DIR="${PROJECT_ROOT}/Logs"
UNITY_TIMEOUT="${GOAP_UNITY_DOCKER_TIMEOUT:-2700}"
mkdir -p "${LOG_DIR}"

if [[ -z "${UNITY_EMAIL:-}" || -z "${UNITY_PASSWORD:-}" ]]; then
  echo "UNITY_EMAIL and UNITY_PASSWORD are required." >&2
  exit 2
fi

if [[ -z "${UNITY_LICENSE:-}" && -z "${UNITY_SERIAL:-}" ]]; then
  echo "Set UNITY_LICENSE (CI .ulf XML) or UNITY_SERIAL (Hub の本物シリアル) in GitHub Secrets." >&2
  exit 2
fi

echo "[goap-ci] docker editmode tests image=${IMAGE} timeout=${UNITY_TIMEOUT}s"

docker_env=( -e UNITY_EMAIL -e UNITY_PASSWORD )
if [[ -n "${UNITY_LICENSE:-}" ]]; then
  docker_env+=( -e UNITY_LICENSE )
fi
if [[ -n "${UNITY_SERIAL:-}" ]]; then
  docker_env+=( -e UNITY_SERIAL )
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
echo "[goap-ci] starting EditMode tests at \$(date -u +%H:%M:%S)"
timeout ${UNITY_TIMEOUT} unity-editor \
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

if [[ "${exit_code}" -ne 0 ]]; then
  if [[ -f "${LOG_DIR}/goap-editmode-tests.log" ]]; then
    tail -40 "${LOG_DIR}/goap-editmode-tests.log" >&2 || true
  fi
  if [[ -f "${LOG_DIR}/ci-unity-activate.log" ]]; then
    tail -40 "${LOG_DIR}/ci-unity-activate.log" >&2 || true
  fi
fi

if [[ -f "${LOG_DIR}/goap-editmode-results.xml" ]]; then
  grep -E 'test-run.*(passed|failed)' "${LOG_DIR}/goap-editmode-results.xml" | head -1 || true
fi

if [[ "${exit_code}" -eq 124 ]]; then
  echo "[goap-ci] docker editmode tests timed out after ${UNITY_TIMEOUT}s" >&2
  exit 124
fi

if [[ "${exit_code}" -ne 0 ]]; then
  echo "[goap-ci] docker editmode tests failed (exit=${exit_code})" >&2
  exit "${exit_code}"
fi

echo "[goap-ci] docker editmode tests passed"
