#!/usr/bin/env bash
# UNITY_LICENSE を game-ci が期待する生 XML に正規化する。
# - GitHub Secret に base64 が入っている場合はデコードする
# - 既に <?xml で始まる場合はそのまま
set -euo pipefail

normalize_unity_license() {
  if [[ -z "${UNITY_LICENSE:-}" ]]; then
    echo "UNITY_LICENSE is not set" >&2
    return 1
  fi

  if [[ "${UNITY_LICENSE}" == "<?xml"* ]]; then
    echo "[ci-license] UNITY_LICENSE is already raw XML"
    return 0
  fi

  local decoded
  decoded="$(printf '%s' "${UNITY_LICENSE}" | base64 -d 2>/dev/null || true)"
  if [[ "${decoded}" == "<?xml"* ]]; then
    echo "[ci-license] decoded UNITY_LICENSE from base64"
    UNITY_LICENSE="${decoded}"
    export UNITY_LICENSE
    if [[ -n "${GITHUB_ENV:-}" ]]; then
      {
        echo "UNITY_LICENSE<<EOF"
        printf '%s' "${decoded}"
        echo
        echo "EOF"
      } >> "${GITHUB_ENV}"
    fi
    return 0
  fi

  echo "[ci-license] UNITY_LICENSE is neither raw XML nor valid base64-encoded XML" >&2
  echo "[ci-license] Register the full .ulf XML (starting with <?xml) in GitHub Secret UNITY_LICENSE" >&2
  echo "[ci-license] For CI, generate .alf via ./scripts/ci/generate-ci-unity-license.sh and activate at license.unity3d.com/manual" >&2
  return 1
}

if [[ "${BASH_SOURCE[0]}" == "${0}" ]]; then
  normalize_unity_license
fi
