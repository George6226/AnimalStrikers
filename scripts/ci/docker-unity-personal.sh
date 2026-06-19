#!/usr/bin/env bash
# unityci Docker 内で Unity Personal をメール/パスワードのみで活性化する共通処理。
# game-ci の UNITY_LICENSE + シリアル抽出は Unity 6 で壊れるため使わない。
set -euo pipefail

unity_docker_activate_personal() {
  local log_file="${1:-/project/Logs/ci-unity-activate.log}"
  echo "[goap-ci] activating Unity Personal (email/password only)"
  xvfb-run --auto-servernum --server-args='-screen 0 640x480x24' \
    unity-editor \
      -batchmode \
      -nographics \
      -quit \
      -username "${UNITY_EMAIL}" \
      -password "${UNITY_PASSWORD}" \
      -logFile "${log_file}"
}
