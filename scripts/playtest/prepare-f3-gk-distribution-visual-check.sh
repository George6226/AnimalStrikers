#!/usr/bin/env bash
# F3 延長: GK キャッチ後の配球 + 味方 GK 保持時の敵フィールド NPC 守備（ミラー視点）目視確認。
# GOAP 観戦モードで Bear（味方 GK）のパスと、敵側 Retreat / DefensivePosition を観察する。
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
ARCHIVE_LABEL="${1:-f3_gk_distribution_visual_before}"
# Verbose 推奨: PlanCosts / 敵守備選出を analyze-defense-goap-log で見るため
GOAP_LOG_LEVEL="${GOAP_LOG_LEVEL:-2}"

echo "=== F3 GK 配球 + 敵守備ミラー — 目視下準備 ==="
echo ""

require_file() {
  if [[ ! -f "$1" ]]; then
    echo "❌ 見つかりません: $1" >&2
    exit 1
  fi
  echo "✅ $(basename "$1")"
}

require_file "${PROJECT_ROOT}/Assets/Scripts/Game/Team/Control/GoalkeeperDistribution.cs"
require_file "${PROJECT_ROOT}/Assets/Scripts/Game/Team/Control/GoalkeeperDistributionBridge.cs"
require_file "${PROJECT_ROOT}/Assets/Scripts/Game/Team/Control/GoalkeeperNpcBrain.cs"
require_file "${PROJECT_ROOT}/Assets/Scripts/Game/Team/Control/Tests/Editor/GoalkeeperDistributionEditModeTests.cs"
require_file "${PROJECT_ROOT}/Assets/Scripts/Game/Goap/GoapActions/GoapActions/MovementActions/MoveToDefensivePositionActionRuntime.cs"
require_file "${PROJECT_ROOT}/Assets/Scripts/Game/Goap/GoapFieldNpcPerspective.cs"

if ! grep -q "GoalkeeperDistribution" "${PROJECT_ROOT}/Assets/Scripts/Game/Team/Control/GoalkeeperNpcBrain.cs"; then
  echo "❌ GoalkeeperNpcBrain に配球呼び出しがありません。" >&2
  exit 1
fi
if ! grep -q "GoapFieldNpcPerspective" \
  "${PROJECT_ROOT}/Assets/Scripts/Game/Goap/GoapActions/GoapActions/MovementActions/MoveToDefensivePositionActionRuntime.cs"; then
  echo "❌ MoveToDefensivePosition に敵視点ミラーがありません。" >&2
  exit 1
fi
echo "   (GK 配球 + 敵守備アクションのミラー視点)"

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

echo "--- GameScene 設定（GOAP 観戦 + 配球/敵守備観察） ---"
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

echo "--- 観る対象（2 系統） ---"
echo "  A. 味方 GK 配球（Bear / slot3）"
echo "     キャッチ後 ~0.55s で味方へパス → 味方が受け位置 or 前進"
echo "  B. 敵フィールド NPC の守備（ミラー視点）"
echo "     味方 GK / 味方保持中に、敵が自ゴール側（z プラス）へ下がる・マークする"
echo "  診断: GOAP_LOG_LEVEL=${GOAP_LOG_LEVEL}（既定 2=Verbose）"
echo ""

echo "--- Unity 手順 ---"
echo "  1. Unity を開く（MainMenuScene）→ Play"
echo "  2. オフライン対戦（MATCHING 約 5 秒）→ READY → GAME"
echo "  3. 手動操作なしで観戦（カメラは slot0 追従）"
echo "  4. 敵が味方ゴールへシュートし、Bear がキャッチする場面を待つ"
echo "  5. その直後の 5〜10 秒を重点観察:"
echo "     - Bear が味方へパスするか"
echo "     - 味方が受けに動くか（反対サイドや敵ゴール方向）"
echo "     - 敵 NPC（手前ではなく相手陣側）が守備ラインへ戻るか"
echo "  6. Scene ビューで敵キャラを選択すると動きが見やすい"
echo ""

echo "--- 目視チェックリスト ---"
echo "  [DIST-A] GK キャッチ後、短時間の保持のあとパスが飛ぶ"
echo "  [DIST-B] パス先の味方が受け位置へ寄る / 前進する（棒立ちにしない）"
echo "  [DIST-C] GkDiag に [GK_DIST] pass_invoke が出る（失敗時は pass_rejected）"
echo "  [DEF-A] 味方 GK 保持中、敵 Field NPC が DefensivePositioning を選ぶ"
echo "  [DEF-B] 敵が自ゴール側（敵陣・z プラス寄り）へ Retreat / MoveToDefensive する"
echo "  [DEF-C] ActionStart 直後の Aborted / context_changed 連発が減っている"
echo "  [DEF-D] 敵が味方ゴール側（z マイナス）へ「攻め」続けない（ミラー破綻の典型）"
echo ""

echo "--- 確認後（ログ解析） ---"
echo "  GK 配球:"
echo "    ./scripts/playtest/analyze-gk-collision-log.sh"
echo "    grep 'GK_DIST' Assets/DebugLog/GkDiag_latest.txt"
echo "  敵守備:"
echo "    ./scripts/playtest/analyze-defense-goap-log.sh"
echo "    （必要なら）Assets/DebugLog/GoapSummary_latest.txt で敵キャラ名の ActionStart"
echo ""

echo "--- 関連 ---"
echo "  GK 位置取りのみ: ./scripts/playtest/prepare-f3-goalkeeper-visual-check.sh"
echo "  汎用観戦:         ./scripts/playtest/prepare-goap-npc-watch-match.sh"
echo ""
echo "下準備完了。Unity で Play してください。"
echo ""
