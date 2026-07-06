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
echo "  【手動操作について】"
echo "    M1/M2 稼働中は手動入力オフ（GOAP 同士の試合のため）。"
echo "    キックオフ前（READY→GAME 前）は GAME 状態でないため操作不可。"
echo "    Lion を M1 で見るにはルーズボール奪取 → 保持が必要（DebugPlace 不要）。"
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
echo ""
echo "--- ルーズボール奪取ログの見方（味方 Main → M1 への入口） ---"
echo ""
echo "  流れ（本番 GOAP 既存・新規実装不要）:"
echo "    FREE（誰も未保持）"
echo "      → FreeBallRecovery + mainNpcDiag=ctx:FreeBall,prodM2:True"
echo "      → ActionStart(MoveToFreeBall)"
echo "    取得後"
echo "      → BallContextChanged(ballState=HOLD) + prodM1:True"
echo "      → BallPossessionAttack + PassToTeammate / ShootAtGoal"
echo ""
echo "  ログで探すキーワード（owner=${ALLY_OWNER}）:"
echo "    grep 'owner=${ALLY_OWNER}' GoapSummary_latest.txt | grep FreeBall"
echo "    grep 'owner=${ALLY_OWNER}' GoapSummary_latest.txt | grep MoveToFreeBall"
echo "    grep 'owner=${ALLY_OWNER}' GoapSummary_latest.txt | grep 'prodM1:True'"
echo ""
echo "  典型行の例:"
echo "    PlanCosts(goal=FreeBallRecovery, tier=Main, mainNpcDiag=prodM2:True,ctx:FreeBall,...)"
echo "    ActionStart(action=MoveToFreeBall, goal=FreeBallRecovery)"
echo "    PlanCosts(goal=BallPossessionAttack, mainNpcDiag=prodM1:True,ctx:Attack,...)"
echo ""
echo "  見つからないときの切り分け:"
echo "    | ログの様子 | 意味 |"
echo "    | NoGoal のみ | 味方/敵が保持中で FREE になっていない |"
echo "    | ctx:Support のみ | 味方が先にボール取得（Main はサポート役） |"
echo "    | キックオフ直後のみ | FREE 時間が短い → プレイ時間を延ばす |"
echo "    | 敵 Main が先に MoveToFreeBall | 取り合いで負けた（正常） |"
echo ""
echo "  敵 Main の owner は編成により ${ENEMY_OWNER} 以外（例: Crocodile）のことがある。"
echo "  ログの tier=Main, slot=0 を確認して analyze の owner= を合わせる。"
echo ""
echo "  M1/Shoot を確実に見たいだけなら（統合とは別）:"
echo "    ./scripts/playtest/prepare-phase-a-shoot-check.sh  # DebugPlace + Lion にボール"
