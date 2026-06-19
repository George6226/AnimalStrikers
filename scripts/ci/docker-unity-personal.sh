#!/usr/bin/env bash
# unityci Docker 内で Unity Personal 認証用の CLI 引数を組み立てる。
set -euo pipefail

unity_auth_cli_args() {
  echo -username
  echo "${UNITY_EMAIL}"
  echo -password
  echo "${UNITY_PASSWORD}"
  if [[ -n "${UNITY_SERIAL:-}" ]]; then
    echo -serial
    echo "${UNITY_SERIAL}"
  fi
}
