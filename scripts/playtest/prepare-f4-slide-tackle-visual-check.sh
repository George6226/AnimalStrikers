#!/usr/bin/env bash
# F4: Main NPC の SlideTackle（相手ボール近接スライディング）目視確認。
# GOAP 観戦モードで slot0 Main が近接時に AnimalAction_Sliding を発火するか観察する。
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
SCENE="${PROJECT_ROOT}/Assets/Scenes/GameScene.unity"
MAIN_MENU_SCENE="${PROJECT_ROOT}/Assets/Scenes/MainMenuScene.unity"
UNITY_LAST_SCENE="${PROJECT_ROOT}/Library/LastSceneManagerSetup.txt"
LOG_SUMMARY="${PROJECT_ROOT}/Assets/DebugLog/GoapSummary_latest.txt"
LOG_DIAG="${PROJECT_ROOT}/Assets/DebugLog/GoapDiag_latest.txt"
LOG_GK_DIAG="${PROJECT_ROOT}/Assets/DebugLog/GkDiag_latest.txt"
RESET_LOGS="${RESET_LOGS:-1}"
ARCHIVE_LABEL="${1:-f4_slide_tackle_visual_before}"
# Verbose 推奨: PlanCosts / 敵守備選出を analyze-defense-goap-log で見るため
GOAP_LOG_LEVEL="${GOAP_LOG_LEVEL:-2}"

echo "=== F4 Main NPC SlideTackle — 目視下準備 ==="
echo ""

require_file() {
  if [[ ! -f "$1" ]]; then
    echo "❌ 見つかりません: $1" >&2
    exit 1
  fi
  echo "✅ $(basename "$1")"
}

require_file "${PROJECT_ROOT}/Assets/Scripts/Game/Goap/GoapActions/GoapActions/DefenseActions/SlideTackleActionSO.cs"
require_file "${PROJECT_ROOT}/Assets/Scripts/Game/Goap/GoapActions/GoapActions/DefenseActions/SlideTackleActionRuntime.cs"
require_file "${PROJECT_ROOT}/Assets/Scripts/Game/Goap/GoapSlideTackleBridge.cs"
require_file "${PROJECT_ROOT}/Assets/Scripts/Game/ActionButton/AnimalAction_Sliding.cs"
require_file "${PROJECT_ROOT}/Assets/Scripts/Game/Goap/Verification/Tests/Editor/SlideTacklePlanningEditModeTests.cs"

if ! grep -q "SlideTackleActionSO" \
  "${PROJECT_ROOT}/Assets/Scripts/Game/Goap/GoapTeammateNpcCatalog.cs"; then
  echo "❌ Catalog に SlideTackle が未登録です。" >&2
  exit 1
fi
echo "   (F4 SlideTackle → AnimalAction_Sliding)"

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

echo "--- GameScene 設定（GOAP 観戦 + SlideTackle 観察） ---"
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
  rm -f "${LOG_SUMMARY}" "${LOG_DIAG}" "${LOG_GK_DIAG}"
  echo "✅ GOAP / GK ログをリセット（Summary / Diag / GkDiag）"
else
  echo "ℹ️  RESET_LOGS=0: 既存ログを保持"
fi
echo ""

echo "--- 観る対象 ---"
echo "  味方 Main（slot0 / Lion 相当・本番 GOAP）が相手ボール近接時に Sliding するか"
echo "  敵 Main（slot0）も対称に Sliding するか"
echo "  オーバーレイで ActionStart=SlideTackle を確認"
echo "  診断: GOAP_LOG_LEVEL=${GOAP_LOG_LEVEL}（既定 2=Verbose）"
echo ""

echo "--- Unity 手順 ---"
echo "  1. Unity を開く（MainMenuScene）→ Play"
echo "  2. オフライン対戦（MATCHING 約 5 秒）→ READY → GAME"
echo "  3. カメラは slot0（本番 Main）追従のまま観戦"
echo "  4. 敵がボールを持って slot0 付近まで来た局面を待つ"
echo "  5. Main がスライディングアニメ → 攻撃コライダで奪取するか観察"
echo "  6. 遠距離では SlideTackle を選ばず Retreat / MTD / Mark になること"
echo ""

echo "--- 目視チェックリスト ---"
echo "  [F4-A] 相手保持＋近接で Main が SlideTackle を ActionStart する"
echo "  [F4-B] Sliding アニメ / 攻撃判定が走り、奪取できることがある"
echo "  [F4-C] 遠距離では SlideTackle を選ばない（MTD/Mark/Retreat）"
echo "  [F4-D] Sub NPC は基本 Slide せず（Main 限定）"
echo "  [F4-E] GoapSummary に ActionStart(action=SlideTackle) が出る"
echo ""

echo "--- 確認後（ログ解析） ---"
echo "  grep 'SlideTackle' Assets/DebugLog/GoapSummary_latest.txt | head -40"
echo "  ./scripts/playtest/analyze-defense-goap-log.sh"
echo ""
echo "--- 関連 ---"
echo "  汎用観戦: ./scripts/playtest/prepare-goap-npc-watch-match.sh"
echo ""
echo "下準備完了。Unity で Play してください。"
echo ""
