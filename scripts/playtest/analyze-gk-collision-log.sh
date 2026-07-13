#!/usr/bin/env bash
# GK 当たり判定診断ログの要約（GkDiag_latest.txt）。
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
LOG="${1:-${PROJECT_ROOT}/Assets/DebugLog/GkDiag_latest.txt}"

if [[ ! -f "${LOG}" ]]; then
  echo "❌ ログが見つかりません: ${LOG}" >&2
  echo "   先に ./scripts/playtest/prepare-f3-goalkeeper-visual-check.sh を実行し、Play 後に確認してください。" >&2
  exit 1
fi

echo "=== GK 当たり判定ログ解析 ==="
echo "file: ${LOG}"
echo "lines: $(wc -l < "${LOG}" | tr -d ' ')"
echo ""

echo "--- トリガー接触 ---"
grep -c '\[GK_TRIGGER\]' "${LOG}" 2>/dev/null || echo "0"
grep '\[GK_TRIGGER\]' "${LOG}" | tail -n 8 || true
echo ""

echo "--- GK 処理結果 ---"
grep -E '\[GK_HANDLE\]|\[GK_SAVE\]|\[GK_CATCH\]' "${LOG}" | tail -n 12 || echo "(なし)"
echo ""

echo "--- 近接プローブ（当たり判定なしの候補） ---"
grep '\[GK_PROBE\]' "${LOG}" | tail -n 12 || echo "(なし)"
echo ""

echo "--- コライダ設定 ---"
grep '\[GK_COLLIDER\]' "${LOG}" | tail -n 8 || echo "(なし)"
echo ""

echo "--- スキップ理由 ---"
grep '\[GK_SKIP\]' "${LOG}" | tail -n 8 || echo "(なし)"
echo ""

trigger_count="$(grep -c '\[GK_TRIGGER\]' "${LOG}" 2>/dev/null || echo 0)"
handle_count="$(grep -cE '\[GK_HANDLE\]|\[GK_SAVE\]|\[GK_CATCH\]' "${LOG}" 2>/dev/null || echo 0)"
probe_near="$(grep -c 'probe_near=true' "${LOG}" 2>/dev/null || echo 0)"

echo "=== 判定ヒント ==="
if [[ "${trigger_count}" == "0" && "${probe_near}" != "0" ]]; then
  echo "⚠️  ボールは GK 付近に来ているが Trigger 未発火 → レイヤー/Trigger/Rigidbody を確認"
elif [[ "${trigger_count}" != "0" && "${handle_count}" == "0" ]]; then
  echo "⚠️  Trigger は発火しているが GK 処理なし → ballState / kickoff suppress を確認"
elif [[ "${handle_count}" != "0" ]]; then
  echo "✅ GK ボール処理ログあり"
else
  echo "ℹ️  敵シュート or ゴール前ルーズボールの場面まで試合を進めてください"
fi
