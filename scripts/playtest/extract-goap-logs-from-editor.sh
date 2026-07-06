#!/usr/bin/env bash
# Editor.log から GOAP_SUMMARY + GOAP_PASS を抽出して Assets/DebugLog/GoapSummary_latest.txt を更新する。
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
OUT="${PROJECT_ROOT}/Assets/DebugLog/GoapSummary_latest.txt"
EDITOR_LOG="${EDITOR_LOG:-${HOME}/Library/Logs/Unity/Editor.log}"
SINCE="${1:-}"

if [[ ! -f "${EDITOR_LOG}" ]]; then
  echo "Editor.log が見つかりません: ${EDITOR_LOG}" >&2
  exit 1
fi

if [[ -n "${SINCE}" ]]; then
  grep -E '\[(GOAP_SUMMARY|GOAP_PASS)\]' "${EDITOR_LOG}" | grep -E "^\[${SINCE}:" > "${OUT}" || true
else
  grep -E '\[(GOAP_SUMMARY|GOAP_PASS)\]' "${EDITOR_LOG}" | tail -600 > "${OUT}" || true
fi

lines=$(wc -l < "${OUT}" | tr -d ' ')
echo "updated: ${OUT} (${lines} lines)"
if [[ -n "${SINCE}" ]]; then
  echo "filter: time prefix ${SINCE}:"
fi
echo ""
"${SCRIPT_DIR}/analyze-main-npc-goap-log.sh" "${OUT}" "${2:-owner=Lion}"
