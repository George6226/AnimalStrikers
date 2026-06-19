#!/usr/bin/env bash
# unityci Docker 内での Unity Personal ライセンス準備・活性化。
set -euo pipefail

unity_license_file_path() {
  echo "/root/.local/share/unity3d/Unity/Unity_lic.ulf"
}

unity_docker_remove_license_file() {
  rm -f "$(unity_license_file_path)"
}

unity_docker_write_license_file() {
  if [[ -z "${UNITY_LICENSE:-}" ]]; then
    return 1
  fi

  local license_file
  license_file="$(unity_license_file_path)"
  mkdir -p "$(dirname "${license_file}")"
  if [[ "${UNITY_LICENSE}" == "<?xml"* ]]; then
    printf '%s' "${UNITY_LICENSE}" > "${license_file}"
  else
    printf '%s' "${UNITY_LICENSE}" | base64 -d > "${license_file}"
  fi
  echo "${license_file}"
}

unity_docker_activate_personal() {
  local log_file="${1:-/project/Logs/ci-unity-activate.log}"
  local -a cmd=(unity-editor -batchmode -nographics -quit -logFile "${log_file}")
  local wrote_license="false"

  # SERIAL 方式では古い UNITY_LICENSE (.ulf) を書かない（Mac 用 ulf があると再活性化が拒否される）
  if [[ -n "${UNITY_SERIAL:-}" ]]; then
    unity_docker_remove_license_file
    echo "[goap-ci] using UNITY_SERIAL for activation (UNITY_LICENSE ignored)"
  elif unity_docker_write_license_file >/dev/null 2>&1; then
    wrote_license="true"
    echo "[goap-ci] wrote UNITY_LICENSE to $(unity_license_file_path)"
  fi

  if [[ -n "${UNITY_EMAIL:-}" && -n "${UNITY_PASSWORD:-}" ]]; then
    cmd+=(-username "${UNITY_EMAIL}" -password "${UNITY_PASSWORD}")
  fi

  if [[ -n "${UNITY_SERIAL:-}" ]]; then
    cmd+=(-serial "${UNITY_SERIAL}")
  fi

  if [[ "${wrote_license}" != "true" && -z "${UNITY_EMAIL:-}" ]]; then
    echo "[goap-ci] no UNITY_LICENSE and no UNITY_EMAIL; cannot activate" >&2
    return 2
  fi

  echo "[goap-ci] activating Unity Personal (timeout 180s)"
  timeout 180 "${cmd[@]}"

  if grep -q "aborting activation\|License activation has failed\|serial invalid\|No valid Unity Editor license" "${log_file}" 2>/dev/null; then
    echo "[goap-ci] license activation failed; see ${log_file}" >&2
    tail -30 "${log_file}" >&2 || true
    return 1
  fi

  if grep -q "license is already valid\|License activated successfully\|Successfully activated the entitlement license" "${log_file}" 2>/dev/null; then
    echo "[goap-ci] license activation looks OK"
    return 0
  fi

  echo "[goap-ci] activation finished (check ${log_file} if tests fail)"
}

unity_docker_print_log_tail() {
  local log_file="$1"
  if [[ -f "${log_file}" ]]; then
    echo "[goap-ci] --- tail of ${log_file} ---"
    tail -40 "${log_file}" || true
    echo "[goap-ci] --- end ---"
  fi
}
