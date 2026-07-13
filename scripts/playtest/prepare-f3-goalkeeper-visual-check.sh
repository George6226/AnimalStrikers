#!/usr/bin/env bash
# F3（GK 位置取り）の目視確認用下準備。
# GOAP 対戦観戦モードでフィールドが動き、Bear（味方 GK）/ 敵 GK の横移動・前出しを観察する。
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
SCENE="${PROJECT_ROOT}/Assets/Scenes/GameScene.unity"
MAIN_MENU_SCENE="${PROJECT_ROOT}/Assets/Scenes/MainMenuScene.unity"
UNITY_LAST_SCENE="${PROJECT_ROOT}/Library/LastSceneManagerSetup.txt"
LOG_SUMMARY="${PROJECT_ROOT}/Assets/DebugLog/GoapSummary_latest.txt"
LOG_DIAG="${PROJECT_ROOT}/Assets/DebugLog/GoapDiag_latest.txt"
RESET_LOGS="${RESET_LOGS:-1}"
ARCHIVE_LABEL="${1:-f3_gk_visual_before}"
GOAP_LOG_LEVEL="${GOAP_LOG_LEVEL:-1}"

echo "=== F3 GK 位置取り目視確認 — 下準備 ==="
echo ""

require_file() {
  if [[ ! -f "$1" ]]; then
    echo "❌ 見つかりません: $1" >&2
    exit 1
  fi
  echo "✅ $(basename "$1")"
}

require_file "${PROJECT_ROOT}/Assets/Scripts/Game/Team/Control/GoalkeeperPositioning.cs"
require_file "${PROJECT_ROOT}/Assets/Scripts/Game/Team/Control/GoalkeeperNpcBrain.cs"
require_file "${PROJECT_ROOT}/Assets/Scripts/Game/Team/Control/Tests/Editor/GoalkeeperPositioningEditModeTests.cs"

if ! grep -q "readonly struct Result" "${PROJECT_ROOT}/Assets/Scripts/Game/Team/Control/GoalkeeperPositioning.cs"; then
  echo "❌ F3: GoalkeeperPositioning.Result が見つかりません。PR #56 マージ後の main を pull してください。" >&2
  exit 1
fi
if ! grep -q "GoalkeeperNpc" "${PROJECT_ROOT}/Assets/Scripts/Game/Team/Control/GoalkeeperNpcBrain.cs"; then
  echo "❌ F3: GoalkeeperNpcBrain が未実装です。" >&2
  exit 1
fi
echo "   (F3 GK 位置取り + GoalkeeperNpcBrain)"

BRANCH="$(git -C "${PROJECT_ROOT}" branch --show-current 2>/dev/null || echo "?")"
MAIN_SHA="$(git -C "${PROJECT_ROOT}" rev-parse --short HEAD 2>/dev/null || echo "?")"
echo "   ブランチ: ${BRANCH} @ ${MAIN_SHA}"
if [[ "${BRANCH}" != "main" ]]; then
  echo "   ℹ️  目視前に main へ切り替え推奨: git checkout main && git pull"
fi
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

echo "--- GameScene 設定（GOAP 観戦 + GK ラベル表示） ---"
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
  rm -f "${LOG_SUMMARY}" "${LOG_DIAG}"
  echo "✅ GOAP ログをリセット"
else
  echo "ℹ️  RESET_LOGS=0: 既存ログを保持"
fi
echo ""

echo "--- 実装パラメータ（目視の期待値） ---"
echo "  味方 GK: 編成 slot3 = Bear（PhotonAvatarCreator で GK 固定）"
echo "  敵 GK:   敵チーム slot3（同様に GK キャラ）"
echo "  ホームライン: 自ゴールからフィールド中心方向へ depth=3.5m（z≈±16.5）"
echo "  HoldLine:   脅威なし → ゴール中央付近で待機"
echo "  TrackBall:  敵保持+守備ゾーン / シュート → ボール X に横移動（Z はライン維持）"
echo "  RushLooseBall: ゴール近く（8m 以内）のルーズボール → 前に出る"
echo "  ※ GK は GOAP 外（GoalkeeperNpcBrain が FixedUpdate で移動）"
echo ""
echo "--- Unity 手順 ---"
echo "  1. Unity を開く（MainMenuScene）→ Play"
echo "  2. オフライン対戦（MATCHING 約 5 秒）"
echo "  3. READY → GAME（キックオフ UI 完了）"
echo "  4. 手動操作は不要。カメラは slot0（Lion）追従のまま観戦"
echo "  5. 味方ゴール付近（画面手前・z マイナス側）の Bear に注目"
echo "     - 頭上ラベル「GK」（黄色系）が出ていれば Role 割当 OK"
echo "  6. 敵が攻め込んできたら Bear が左右に追従するか確認"
echo "  7. 敵シュート時もゴール前で X 追従するか確認"
echo ""
echo "--- 目視チェックリスト ---"
echo "  [F3-A] キックオフ直後: 味方 Bear が自ゴール前（z≈-16.5）で待機"
echo "  [F3-B] 味方ボール保持・敵が遠い: Bear はホームライン中央付近（HoldLine）"
echo "  [F3-C] 敵が自陣深くでボール保持: Bear がボール X に横移動（TrackBall）"
echo "  [F3-D] 敵シュート: Bear がシュートコース方向へ横移動（TrackBall）"
echo "  [F3-E] ゴール近くルーズボール: Bear が前に出る（RushLooseBall）"
echo "  [F3-F] 敵 GK も同様に動く（フィールド反対側・z プラス付近）"
echo "  [F3-G] GK は GOAP の YOU·M1/M2 ラベルではなく「GK」ラベル"
echo ""
echo "--- カメラのコツ ---"
echo "  味方 GK を見やすくするには、敵攻撃時（Bear が横移動する場面）を待つ。"
echo "  Scene ビューで Bear を選択して追うのも可（Game ビューは slot0 追従のまま）。"
echo ""
echo "--- 確認後（任意） ---"
echo "  GOAP 回帰ログ:"
echo "    ./scripts/playtest/run-phase-d-3min-playtest.sh"
echo "  汎用観戦スクリプト（本スクリプトと同等設定）:"
echo "    ./scripts/playtest/prepare-goap-npc-watch-match.sh"
echo ""
