#!/usr/bin/env bash
# GoapSummary_latest.txt をタイムスタンプ付きでアーカイブする（ログリセット前に実行）。
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
SRC="${PROJECT_ROOT}/Assets/DebugLog/GoapSummary_latest.txt"
ARCHIVE_DIR="${PROJECT_ROOT}/Assets/DebugLog/archives"
LABEL="${1:-session}"

if [[ ! -f "${SRC}" ]]; then
  echo "アーカイブ対象なし: ${SRC}" >&2
  exit 0
fi

mkdir -p "${ARCHIVE_DIR}"
STAMP="$(date +%Y%m%d_%H%M%S)"
DEST="${ARCHIVE_DIR}/GoapSummary_${LABEL}_${STAMP}.txt"
cp "${SRC}" "${DEST}"
lines=$(wc -l < "${DEST}" | tr -d ' ')
echo "archived: ${DEST} (${lines} lines)"
