#!/usr/bin/env bash
# Editor.log から GOAP_SUMMARY + GOAP_PASS を抽出して Assets/DebugLog/GoapSummary_latest.txt を更新する。
#
# Usage:
#   extract-goap-logs-from-editor.sh [time] [owner=Lion]
#   time: 17:01（分単位） / 17:00-17:02（範囲） / 省略時は直近 600 行
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
OUT="${PROJECT_ROOT}/Assets/DebugLog/GoapSummary_latest.txt"
EDITOR_LOG="${EDITOR_LOG:-${HOME}/Library/Logs/Unity/Editor.log}"
SINCE="${1:-}"
OWNER_ARG="${2:-owner=Lion}"

if [[ ! -f "${EDITOR_LOG}" ]]; then
  echo "Editor.log が見つかりません: ${EDITOR_LOG}" >&2
  exit 1
fi

build_time_pattern() {
  local filter="$1"
  if [[ "${filter}" == *-* ]]; then
    local start="${filter%-*}"
    local end="${filter#*-}"
    local sh sm eh em
    sh="${start%%:*}"
    sm="${start#*:}"
    eh="${end%%:*}"
    em="${end#*:}"
    if [[ "${sh}" != "${eh}" ]]; then
      echo "同一時刻内の範囲のみ対応しています（例: 17:00-17:02）" >&2
      return 1
    fi
    local minutes=()
    local m
    for ((m = 10#${sm}; m <= 10#${em}; m++)); do
      minutes+=("${sh}:$(printf '%02d' "${m}")")
    done
    local joined
    joined="$(IFS='|'; echo "${minutes[*]}")"
    printf '^\[(%s):' "${joined}"
    return 0
  fi

  printf '^\[%s:' "${filter}"
}

if [[ -n "${SINCE}" ]]; then
  TIME_PATTERN="$(build_time_pattern "${SINCE}")"
  grep -E '\[(GOAP_SUMMARY|GOAP_PASS)\]' "${EDITOR_LOG}" | grep -E "${TIME_PATTERN}" > "${OUT}" || true
else
  grep -E '\[(GOAP_SUMMARY|GOAP_PASS)\]' "${EDITOR_LOG}" | tail -600 > "${OUT}" || true
fi

lines=$(wc -l < "${OUT}" | tr -d ' ')
echo "updated: ${OUT} (${lines} lines)"
if [[ -n "${SINCE}" ]]; then
  echo "filter: time ${SINCE}"
fi
echo ""
"${SCRIPT_DIR}/analyze-main-npc-goap-log.sh" "${OUT}" "${OWNER_ARG}"
