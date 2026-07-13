#!/usr/bin/env bash
# Unity エディタで GOAP 対戦を観戦する下準備（手動操作なし・味方 Main/Sub + 敵 Main/Sub）。
# 本番 GOAP（Production）+ 敵 GOAP を有効にし、Verify モードは OFF にする。
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
SCENE="${PROJECT_ROOT}/Assets/Scenes/GameScene.unity"
MAIN_MENU_SCENE="${PROJECT_ROOT}/Assets/Scenes/MainMenuScene.unity"
UNITY_LAST_SCENE="${PROJECT_ROOT}/Library/LastSceneManagerSetup.txt"
LOG_SUMMARY="${PROJECT_ROOT}/Assets/DebugLog/GoapSummary_latest.txt"
LOG_DIAG="${PROJECT_ROOT}/Assets/DebugLog/GoapDiag_latest.txt"
RESET_LOGS="${RESET_LOGS:-1}"
ARCHIVE_LABEL="${1:-goap_npc_watch}"
# 0=Off / 1=Summary（軽量・推奨） / 2=Verbose（PlanCosts 等の詳細解析用）
GOAP_LOG_LEVEL="${GOAP_LOG_LEVEL:-1}"

echo "=== GOAP NPC 対戦観戦 — 下準備 ==="
echo ""

require_file() {
  if [[ ! -f "$1" ]]; then
    echo "❌ 見つかりません: $1" >&2
    exit 1
  fi
  echo "✅ $(basename "$1")"
}

require_file "${PROJECT_ROOT}/Assets/Scripts/Game/Goap/GoapMainNpcProductionEnvironment.cs"
require_file "${PROJECT_ROOT}/Assets/Scripts/Game/Goap/GoapEnemyMainNpcPlanning.cs"
require_file "${PROJECT_ROOT}/Assets/Scripts/Game/Team/Control/EnemySquadControlController.cs"
echo "   (味方 Main/Sub 本番 GOAP + 敵 Main/Sub GOAP)"

BRANCH="$(git -C "${PROJECT_ROOT}" branch --show-current 2>/dev/null || echo "?")"
MAIN_SHA="$(git -C "${PROJECT_ROOT}" rev-parse --short HEAD 2>/dev/null || echo "?")"
echo "   ブランチ: ${BRANCH} @ ${MAIN_SHA}"
echo ""

if [[ ! -f "${SCENE}" ]]; then
  echo "❌ GameScene が見つかりません: ${SCENE}" >&2
  exit 1
fi

set_scene_flag() {
  local label="$1"
  local pattern="$2"
  local expected="$3"
  if ! grep -q "${pattern}" "${SCENE}"; then
    echo "⚠️  ${label} が GameScene に見つかりません（C# デフォルト ${expected} 想定）"
    return
  fi

  local value
  value="$(grep "${pattern}" "${SCENE}" | head -1 | awk '{print $2}')"
  if [[ "${value}" == "${expected}" ]]; then
    echo "✅ ${label} = ${expected}"
    return
  fi

  sed -i '' "s/${pattern} ${value}/${pattern} ${expected}/" "${SCENE}"
  echo "✅ ${label} = ${expected}（${value} から自動更新）"
}

ensure_goap_diagnostic_level() {
  local level="$1"
  case "${level}" in
    0|1|2) ;;
    *)
      echo "❌ GOAP_LOG_LEVEL は 0/1/2 です: ${level}" >&2
      exit 1
      ;;
  esac

  if grep -q "_goapDiagnosticLevel:" "${SCENE}"; then
    set_scene_flag "Goap Diagnostic Level" "_goapDiagnosticLevel:" "${level}"
    return
  fi

  if ! grep -q "_debugOverlayEnabled:" "${SCENE}"; then
    echo "⚠️  _goapDiagnosticLevel を書き込めません（SquadControlController 未検出）"
    return
  fi

  sed -i '' "/_debugOverlayEnabled:/a\\
  _goapDiagnosticLevel: ${level}
" "${SCENE}"
  echo "✅ Goap Diagnostic Level = ${level}（新規追加）"
}

echo "--- GameScene 設定（TeamFacade 上の Squad / EnemySquad を自動適用） ---"
set_scene_flag "Main Npc Goap Verify Mode" "_mainNpcGoapVerifyMode:" "0"
set_scene_flag "Enable Main Npc Goap In Production" "_enableMainNpcGoapInProduction:" "1"
set_scene_flag "Enable Enemy Goap" "_enableEnemyGoap:" "1"
set_scene_flag "Goap All Teammate Field Npcs" "_goapAllTeammateFieldNpcs:" "1"
set_scene_flag "Goap All Enemy Field Npcs" "_goapAllEnemyFieldNpcs:" "1"
set_scene_flag "Allow Goap Idle Fallback" "_allowGoapIdleFallback:" "0"
ensure_goap_diagnostic_level "${GOAP_LOG_LEVEL}"
set_scene_flag "Debug Overlay" "_debugOverlayEnabled:" "1"
set_scene_flag "Human Formation Slot" "_humanFormationSlot:" "0"
set_scene_flag "Main Npc Formation Slot" "_mainNpcFormationSlot:" "0"
set_scene_flag "Enemy Main Formation Slot" "_enemyMainFormationSlot:" "0"
set_scene_flag "Stage4 Role Differentiation" "_enableStage4RoleDifferentiation:" "1"

if grep -q "GoapCombinedSupportRegressionDebugSetup" "${SCENE}"; then
  if grep -A20 "GoapCombinedSupportRegressionDebugSetup" "${SCENE}" | grep -q "m_Enabled: 0"; then
    echo "✅ GoapCombinedSupportRegressionDebugSetup = OFF"
  else
    echo "⚠️  GoapCombinedSupportRegressionDebugSetup が ON の可能性"
    echo "   → Hierarchy: GoapDebug 配下を OFF にしてください"
  fi
fi

if grep -q "_requireMainNpcVerifyMode: 1" "${SCENE}" && grep -q "_mainNpcGoapVerifyMode: 0" "${SCENE}"; then
  echo "✅ GoapMainNpcVerifyBootstrap は verify 時のみ（本番観戦では起動しない）"
fi

if [[ -f "${MAIN_MENU_SCENE}" ]]; then
  if grep -q "MATCHING_TIMEOUT: 5" "${MAIN_MENU_SCENE}"; then
    echo "✅ MainMenu MATCHING_TIMEOUT = 5"
  elif grep -q "MATCHING_TIMEOUT:" "${MAIN_MENU_SCENE}"; then
    sed -i '' 's/MATCHING_TIMEOUT: [0-9]*/MATCHING_TIMEOUT: 5/' "${MAIN_MENU_SCENE}"
    echo "✅ MainMenu MATCHING_TIMEOUT を 5 に更新"
  fi
else
  echo "⚠️  MainMenuScene が見つかりません"
fi

mkdir -p "$(dirname "${UNITY_LAST_SCENE}")"
cat > "${UNITY_LAST_SCENE}" <<'EOF'
sceneSetups:
- path: Assets/Scenes/MainMenuScene.unity
  isLoaded: 1
  isActive: 1
  isSubScene: 0
EOF
echo "✅ Unity 起動シーン = MainMenuScene"
echo ""

if [[ "${RESET_LOGS}" == "1" ]]; then
  if [[ -f "${LOG_SUMMARY}" ]]; then
    "${SCRIPT_DIR}/archive-goap-summary.sh" "${ARCHIVE_LABEL}"
  fi
  rm -f "${LOG_SUMMARY}" "${LOG_DIAG}"
  echo "✅ ログをリセット: GoapSummary_latest.txt / GoapDiag_latest.txt"
else
  echo "ℹ️  RESET_LOGS=0: 既存ログを保持"
fi
echo ""

echo "--- 観戦モードの説明 ---"
echo "  味方: slot0=Main GOAP（YOU·M1/M2 ラベル）+ slot1/2=Sub GOAP"
echo "  敵:   slot0=Main GOAP + slot1/2=Sub GOAP（全員 NPC）"
echo "  手動操作は不要です。GOAP 稼働中は Human 入力が自動抑止されます。"
echo ""
echo "  ※ Verify Mode は ON にしないでください（敵 GOAP が無効になります）"
echo "  ※ DebugPlace は使わず、通常のキックオフから観戦するのがおすすめです"
echo ""
echo "--- Unity 手順 ---"
echo "  1. Unity を開く（MainMenuScene がアクティブ）"
echo "  2. Play"
echo "  3. オフライン対戦を開始（MATCHING 約 5 秒）"
echo "  4. READY → GAME（キックオフ UI 完了まで約 5 秒待つ）"
echo "  5. キーボード・パッドは触らず観戦（約 3 分）"
echo ""
echo "  GameScene 設定は本スクリプトが TeamFacade に書き込み済みです。"
echo "  Inspector で見る場合: GameScene → Hierarchy → TeamFacade"
echo ""
echo "  画面上で見るもの:"
echo "    - 味方 Main: YOU·M1（保持中）/ YOU·M2（オフボール）"
echo "    - 味方 Sub: 守備・サポート走り（敵ボール時は DefensivePositioning）"
echo "    - 敵 Main: パス・シュート・フリーボール奪取"
echo "    - 敵 Sub: 味方ボール時の TeamBallSupport"
echo "    - カメラは操作キャラ（slot0）追従。操作不要のまま試合が進みます"
echo ""
echo "  ログを残す場合（Play 終了後）:"
case "${GOAP_LOG_LEVEL}" in
  0) echo "    ⚠️  GOAP_LOG_LEVEL=0: ログは出ません。GOAP_LOG_LEVEL=1 で再実行してください。" ;;
  1) echo "    Summary → Assets/DebugLog/GoapSummary_latest.txt（ActionStart/PlanSuccess 等）" ;;
  2) echo "    Verbose → GoapSummary + GoapDiag_latest.txt + Console（PlanCosts 解析向け）" ;;
esac
echo "    詳細解析が必要なら: GOAP_LOG_LEVEL=2 ./scripts/playtest/prepare-goap-npc-watch-match.sh"
echo "    ./scripts/playtest/extract-goap-logs-from-editor.sh <HH:MM>   # Summary 時は Editor.log 併用"
echo "    ./scripts/playtest/analyze-phase-c-integration-log.sh Assets/DebugLog/GoapSummary_latest.txt"
echo ""
echo "  パス受け指標も見る場合:"
echo "    MODE=full ./scripts/playtest/analyze-phase-d-pass-receive-log.sh Assets/DebugLog/GoapSummary_latest.txt"
echo "    （末尾の GOAP仕上げゲート G0〜G4 で ActionRejected / PlanFailure / 敵 NoGoal 上限を判定）"
echo ""
echo "  3 分 CLI 自動実行（Unity バッチ）:"
echo "    ./scripts/playtest/run-phase-d-3min-playtest.sh"
echo ""
