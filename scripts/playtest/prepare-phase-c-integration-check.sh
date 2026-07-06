#!/usr/bin/env bash
# Phase C: 統合マッチ本番確認（味方 Main + 敵 Main + Sub 守備/サポート）の下準備。
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
SCENE="${PROJECT_ROOT}/Assets/Scenes/GameScene.unity"
LOG_SUMMARY="${PROJECT_ROOT}/Assets/DebugLog/GoapSummary_latest.txt"
LOG_DIAG="${PROJECT_ROOT}/Assets/DebugLog/GoapDiag_latest.txt"
ALLY_OWNER="${ALLY_OWNER:-Lion}"
ENEMY_OWNER="${ENEMY_OWNER:-Elephant}"
ARCHIVE_LABEL="${1:-phaseC}"

echo "=== Phase C 統合マッチ確認 — 下準備 ==="
echo ""

if [[ ! -f "${PROJECT_ROOT}/Assets/Scripts/Game/Goap/GoapMainNpcProductionEnvironment.cs" ]]; then
  echo "❌ Phase A コードが見つかりません。" >&2
  exit 1
fi
if [[ ! -f "${PROJECT_ROOT}/Assets/Scripts/Game/Goap/GoapEnemyMainNpcPlanning.cs" ]]; then
  echo "❌ Phase B コードが見つかりません。" >&2
  exit 1
fi
echo "✅ Phase A/B コード（味方 Main + 敵 Main GOAP）"

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
check_scene_flag "Enable Enemy Goap" "_enableEnemyGoap:" "1"
check_scene_flag "Debug Overlay" "_debugOverlayEnabled:" "1"

if grep -q "GoapCombinedSupportRegressionDebugSetup" "${SCENE}"; then
  if grep -A20 "GoapCombinedSupportRegressionDebugSetup" "${SCENE}" | grep -q "m_Enabled: 0"; then
    echo "✅ GoapCombinedSupportRegressionDebugSetup = OFF（CI バッチ自動実行なし）"
  else
    echo "⚠️  GoapCombinedSupportRegressionDebugSetup が ON の可能性"
    echo "   → Hierarchy: GoapDebug 配下を OFF にしてください"
  fi
fi

if [[ -f "${LOG_SUMMARY}" ]]; then
  "${SCRIPT_DIR}/archive-goap-summary.sh" "${ARCHIVE_LABEL}"
fi

rm -f "${LOG_SUMMARY}" "${LOG_DIAG}"
echo ""
echo "✅ ログをリセットしました:"
echo "   ${LOG_SUMMARY}"
echo "   ${LOG_DIAG}"

echo ""
echo "--- 完了済み（個別シナリオ） ---"
echo "  ✅ Phase A: 味方 Main M1/M2 + ShootAtGoal（${ALLY_OWNER}）"
echo "  ✅ Phase B: 敵 Main M1/M2 + ShootAtGoal（${ENEMY_OWNER}）"
echo "  ⬜ Phase C: 統合マッチ（本セッションの目的）"
echo ""
echo "--- Unity 手順（統合マッチ） ---"
echo "  【重要】DebugPlace なし・ステージ配置なしの通常プレイ"
echo "  1. MainMenu → オフライン対戦（そのまま開始）"
echo "  2. SquadControlController:"
echo "       Main Npc Goap Verify Mode = OFF"
echo "       Enable Main Npc Goap In Production = ON"
echo "       Enable Enemy Goap = ON"
echo "  3. Play → READY→GAME（キックオフ UI 完了まで約 5 秒待つ）"
echo "  4. 2〜3 分プレイ（ボール奪取・パス・シュート・守備を自然に発生させる）"
echo ""
echo "  画面上の確認:"
echo "    - 操作キャラ: YOU·M1（保持中）/ YOU·M2（パス後）"
echo "    - 敵 Main: パス・シュート・サポート"
echo "    - 味方 Sub: 敵ボール時 DefensivePositioning"
echo "    - 敵 Sub: 味方ボール時 TeamBallSupport"
echo ""
echo "  Play 終了後:"
echo "    ./scripts/playtest/extract-goap-logs-from-editor.sh <HH:MM>"
echo "    ./scripts/playtest/analyze-phase-c-integration-log.sh Assets/DebugLog/GoapSummary_latest.txt"
echo ""
echo "  owner 指定（編成が違う場合）:"
echo "    ALLY_OWNER=${ALLY_OWNER} ENEMY_OWNER=${ENEMY_OWNER} \\"
echo "      ./scripts/playtest/analyze-phase-c-integration-log.sh <log> owner=${ALLY_OWNER} owner=${ENEMY_OWNER}"
echo ""
echo "期待ログ（統合）:"
echo "  - 味方 Main: prodM1/prodM2 + Pass/Shoot/サポート"
echo "  - 敵 Main: prodM1/prodM2 + Pass/Shoot/サポート"
echo "  - 味方 Sub: DefensivePositioning + 守備アクション"
echo "  - 敵 Sub: TeamBallSupport + サポートアクション"
