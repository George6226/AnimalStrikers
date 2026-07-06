#!/usr/bin/env bash
# Phase A 残項目 C: ShootAtGoal 本番確認の下準備。
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
SCENE="${PROJECT_ROOT}/Assets/Scenes/GameScene.unity"
LOG_SUMMARY="${PROJECT_ROOT}/Assets/DebugLog/GoapSummary_latest.txt"
LOG_DIAG="${PROJECT_ROOT}/Assets/DebugLog/GoapDiag_latest.txt"

echo "=== Phase A シュート確認 — 下準備 ==="
echo ""

if ! grep -q "CanShootAtGoal" "${PROJECT_ROOT}/Assets/Scripts/Game/Team/Control/MainNpcAttackPlanning.cs" 2>/dev/null; then
  echo "❌ MainNpcAttackPlanning が見つかりません。" >&2
  exit 1
fi
echo "✅ M1 シュート判定（MainNpcAttackPlanning.CanShootAtGoal）"

BRANCH="$(git -C "${PROJECT_ROOT}" branch --show-current 2>/dev/null || echo "?")"
echo "   ブランチ: ${BRANCH}"

if [[ ! -f "${SCENE}" ]]; then
  echo "❌ GameScene が見つかりません: ${SCENE}" >&2
  exit 1
fi

check_scene_flag() {
  local label="$1"
  local pattern="$2"
  local expected="$3"
  if grep -q "${pattern}" "${SCENE}"; then
    local value
    value="$(grep "${pattern}" "${SCENE}" | head -1 | awk '{print $2}')"
    if [[ "${value}" == "${expected}" ]]; then
      echo "✅ ${label} = ${expected}"
    else
      echo "⚠️  ${label} = ${value}（期待: ${expected}）"
    fi
  else
    echo "ℹ️  ${label} 未シリアライズ（C# デフォルト ${expected} 想定）"
  fi
}

check_scene_flag "Main Npc Goap Verify Mode" "_mainNpcGoapVerifyMode:" "0"
check_scene_flag "Enable Main Npc Goap In Production" "_enableMainNpcGoapInProduction:" "1"

if [[ -f "${LOG_SUMMARY}" ]]; then
  "${SCRIPT_DIR}/archive-goap-summary.sh" "phaseA_before_shoot"
fi

rm -f "${LOG_SUMMARY}" "${LOG_DIAG}"
echo ""
echo "✅ ログをリセットしました:"
echo "   ${LOG_SUMMARY}"
echo "   ${LOG_DIAG}"

echo ""
echo "--- 完了済み（直近プレイ） ---"
echo "  ✅ A 静止ラリー / B 敵接近パス+ロブ / D パス後サポート"
echo "  ⬜ C ShootAtGoal（本セッションの目的）"
echo ""
echo "--- Unity 手順（シュート確認） ---"
echo "  1. MainMenu → オフライン対戦 → DebugPlace"
echo "  2. Lion（slot0）を敵ゴール寄りに配置"
echo "     本番フィールド長 40m → 敵ゴール z=+20、VeryNear は約 7.0m 以内（z≳13.0）"
echo "     直近ログ: z≈13.3（距離 6.7m）は 0.28 では圏外 → Pass 選出"
echo "  3. Lion にボール保持を設定"
echo "  4. 味方はゴール前に密集させすぎない（パスが常に安いとシュートが選ばれにくい）"
echo "  5. SquadControl: Verify=OFF, Production ON → Play → READY→GAME 後 約30秒"
echo ""
echo "  Play 終了後:"
echo "    ./scripts/playtest/extract-goap-logs-from-editor.sh <HH:MM> owner=Lion"
echo "    # 例: ./scripts/playtest/extract-goap-logs-from-editor.sh 17:15 owner=Lion"
echo ""
echo "期待ログ:"
echo "  - PlanCosts(..., canShoot:True, ...) selected=ShootAtGoal"
echo "  - ActionStart(action=ShootAtGoal, goal=BallPossessionAttack)"
echo "  - prodM1:True + ctx:Attack"
echo ""
echo "Phase A 完了後の次ステップ:"
echo "    ./scripts/playtest/prepare-phase-b-enemy-check.sh"
