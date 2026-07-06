#!/usr/bin/env bash
# Phase B 残項目: 敵 Main の ShootAtGoal 本番確認の下準備。
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
SCENE="${PROJECT_ROOT}/Assets/Scenes/GameScene.unity"
LOG_SUMMARY="${PROJECT_ROOT}/Assets/DebugLog/GoapSummary_latest.txt"
LOG_DIAG="${PROJECT_ROOT}/Assets/DebugLog/GoapDiag_latest.txt"
ENEMY_OWNER="${ENEMY_OWNER:-Elephant}"

echo "=== Phase B 敵シュート確認 — 下準備 ==="
echo ""

if [[ ! -f "${PROJECT_ROOT}/Assets/Scripts/Game/Goap/GoapEnemyMainNpcPlanning.cs" ]]; then
  echo "❌ Phase B コードが見つかりません。" >&2
  exit 1
fi
echo "✅ 敵 Main GOAP（GoapEnemyMainNpcPlanning / MainNpcAttackPlanning 鏡像）"

BRANCH="$(git -C "${PROJECT_ROOT}" branch --show-current 2>/dev/null || echo "?")"
echo "   ブランチ: ${BRANCH}"

if [[ ! -f "${SCENE}" ]]; then
  echo "❌ GameScene が見つかりません: ${SCENE}" >&2
  exit 1
fi

if grep -q "_enableEnemyGoap: 1" "${SCENE}"; then
  echo "✅ Enable Enemy Goap = ON"
else
  echo "⚠️  Enable Enemy Goap を ON にしてください"
fi

if [[ -f "${LOG_SUMMARY}" ]]; then
  "${SCRIPT_DIR}/archive-goap-summary.sh" "phaseB_before_enemy_shoot"
fi

rm -f "${LOG_SUMMARY}" "${LOG_DIAG}"
echo ""
echo "✅ ログをリセットしました:"
echo "   ${LOG_SUMMARY}"
echo "   ${LOG_DIAG}"

echo ""
echo "--- 完了済み（Phase B） ---"
echo "  ✅ 敵 Main M1 Pass / M2 サポート（Elephant）"
echo "  ✅ 敵 Sub サポート / 味方 Sub 守備"
echo "  ⬜ 敵 ShootAtGoal（本セッションの目的）"
echo ""
echo "--- Unity 手順（敵シュート確認） ---"
echo "  1. MainMenu → オフライン対戦 → DebugPlace"
echo "  2. 敵 slot0（通常 ${ENEMY_OWNER}）を味方ゴール寄りに配置"
echo "     本番フィールド長 40m → 味方ゴール z=-20、VeryNear は約 7.0m 以内（z≲-13.0）"
echo "     直近: press:0 では Pass 1.12 < Shoot 1.28 → ゴール寄り配置が必須"
echo "  3. 敵 slot0 にボール保持を設定"
echo "  4. 味方はゴール前に密集させすぎない（パスが安くなる）"
echo "  5. Enemy GOAP = ON → Play → READY→GAME 後 約30秒"
echo ""
echo "  Play 終了後:"
echo "    ./scripts/playtest/extract-goap-logs-from-editor.sh <HH:MM> owner=${ENEMY_OWNER}"
echo "    ./scripts/playtest/analyze-enemy-main-npc-goap-log.sh Assets/DebugLog/GoapSummary_latest.txt owner=${ENEMY_OWNER}"
echo ""
echo "期待ログ:"
echo "  - tier=Main, slot=0, prodM1:True, canShoot:True, selected=ShootAtGoal"
echo "  - ActionStart(action=ShootAtGoal, goal=BallPossessionAttack)"
echo ""
echo "注: 敵 Main は編成により Tiger 等の場合あり。owner= はログの owner= に合わせる。"
