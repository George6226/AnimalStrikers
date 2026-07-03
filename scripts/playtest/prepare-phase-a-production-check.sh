#!/usr/bin/env bash
# Phase A 本番確認（YOU·M1 / YOU·M2）の下準備: ログリセットと GameScene 設定チェック。
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
SCENE="${PROJECT_ROOT}/Assets/Scenes/GameScene.unity"
LOG_SUMMARY="${PROJECT_ROOT}/Assets/DebugLog/GoapSummary_latest.txt"
LOG_DIAG="${PROJECT_ROOT}/Assets/DebugLog/GoapDiag_latest.txt"

echo "=== Phase A 本番確認 — 下準備 ==="
echo ""

# 1. main + Phase A コード
if ! grep -q "ShouldSuppressHumanInput" "${PROJECT_ROOT}/Assets/Scripts/Game/Goap/GoapMainNpcProductionEnvironment.cs" 2>/dev/null; then
  echo "❌ Phase A コードが見つかりません。git checkout main && git pull してください。" >&2
  exit 1
fi
echo "✅ Phase A コード（GoapMainNpcProductionEnvironment）"

BRANCH="$(git -C "${PROJECT_ROOT}" branch --show-current 2>/dev/null || echo "?")"
echo "   ブランチ: ${BRANCH}"

# 2. GameScene 本番向け設定
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
      echo "   → Unity Inspector: SquadControlController を確認"
    fi
  else
    echo "ℹ️  ${label} 未シリアライズ（C# デフォルト ${expected} 想定）"
  fi
}

check_scene_flag "Main Npc Goap Verify Mode" "_mainNpcGoapVerifyMode:" "0"
check_scene_flag "Enable Main Npc Goap In Production" "_enableMainNpcGoapInProduction:" "1"
check_scene_flag "Debug Overlay" "_debugOverlayEnabled:" "1"
check_scene_flag "Human Formation Slot" "_humanFormationSlot:" "0"

if grep -q "_requireMainNpcVerifyMode: 1" "${SCENE}" && grep -q "_mainNpcGoapVerifyMode: 0" "${SCENE}"; then
  echo "✅ Bootstrap は verify 時のみ動作（本番 Play では起動しない）"
fi

# 3. ログリセット
rm -f "${LOG_SUMMARY}" "${LOG_DIAG}"
echo ""
echo "✅ ログをリセットしました:"
echo "   ${LOG_SUMMARY}"
echo "   ${LOG_DIAG}"

# 4. 手順
echo ""
echo "--- Unity での確認手順 ---"
echo "  【重要】GameScene 直 Play だけだとキックオフ前に全員静止しやすい（下記ログ分析参照）"
echo "  推奨: MainMenu → オフライン対戦（DebugPlace で Lion にボール保持を設定してから開始）"
echo ""
echo "  1. SquadControlController:"
echo "       Main Npc Goap Verify Mode = OFF"
echo "       Enable Main Npc Goap In Production = ON"
echo "       Allow Goap Idle Fallback = OFF（現状のまま）"
echo "  2. Play → キャラ出現後、キックオフ UI フェード完了まで約 5 秒待つ（READY→GAME）"
echo "  3. Lion がボール保持 → YOU·M1 + 自動 Pass/Shoot"
echo "  4. パス後 → YOU·M2 + サポート走り"
echo "  5. Play 終了後:"
echo "       ./scripts/playtest/analyze-main-npc-goap-log.sh Assets/DebugLog/GoapSummary_latest.txt owner=Lion"
echo ""
echo "  すぐ試す代替（GameScene 直 Play）:"
echo "    - 一時的に Enable Main Npc Goap In Production = OFF → 手動でボール取得後 ON"
echo "    - または Verify Mode ON + Bootstrap（MainNpcForAttackVerify）でボール自動付与"
echo ""
echo "期待ログ:"
echo "  - prodM1:True + BallPossessionAttack + PassToTeammate/ShootAtGoal"
echo "  - prodM2:True + TeamBallSupport + サポート行動"
echo ""
echo "注意: batch-main-npc-attack（CI）は verify モードのため prodM1/prodM2 は出ません。"
echo "      本番確認は上記の通常 Play のみです。"
