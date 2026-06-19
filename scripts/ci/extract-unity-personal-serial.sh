#!/usr/bin/env bash
# Mac 上の Unity_lic.ulf から Personal シリアルを表示する（マスク有無を確認）。
# 実行: ./scripts/ci/extract-unity-personal-serial.sh
set -euo pipefail

ULF="${1:-/Library/Application Support/Unity/Unity_lic.ulf}"

if [[ ! -f "${ULF}" ]]; then
  echo "Not found: ${ULF}" >&2
  exit 1
fi

if grep -q DeveloperData "${ULF}"; then
  serial="$(grep DeveloperData "${ULF}" | sed -E 's/.*Value="([^"]+)".*/\1/' | base64 -d 2>/dev/null | tail -c 28 || true)"
  if [[ -n "${serial}" ]]; then
    echo "${serial}"
    exit 0
  fi
fi

serial="$(grep -Eo '[A-Z0-9]{2}-[A-Z0-9]{4}-[A-Z0-9]{4}-[A-Z0-9]{4}-[A-Z0-9]{4}-[A-Z0-9]{4}' "${ULF}" | head -1 || true)"
if [[ -z "${serial}" ]]; then
  echo "シリアルを抽出できませんでした。CI 用 .ulf (UNITY_LICENSE) を使う方法を検討してください。" >&2
  exit 1
fi

if [[ "${serial}" == *XXXX ]]; then
  echo "${serial}" >&2
  echo "警告: シリアル末尾が XXXX でマスクされています。CI では使えません。" >&2
  echo "Unity Hub または https://id.unity.com で本物のシリアルを確認してください。" >&2
  exit 2
fi

echo "${serial}"
