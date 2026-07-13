#!/usr/bin/env bash
# F1（枯渇減速）+ F2（ダッシュ禁止）の目視確認用下準備。
# 手動操作できるよう Main NPC 本番 GOAP を OFF にし、必要なら味方/敵 GOAP も止める。
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
SCENE="${PROJECT_ROOT}/Assets/Scenes/GameScene.unity"
MAIN_MENU_SCENE="${PROJECT_ROOT}/Assets/Scenes/MainMenuScene.unity"
DEBUG_SCENE="${PROJECT_ROOT}/Assets/Scenes/DebugCharaPlaceScene.unity"
UNITY_LAST_SCENE="${PROJECT_ROOT}/Library/LastSceneManagerSetup.txt"
RESET_LOGS="${RESET_LOGS:-1}"
ARCHIVE_LABEL="${1:-f1f2_stamina_visual_before}"
# solo=手動操作のみ（推奨） / observe=GOAP 観戦のまま（F1/F2 目視には不向き）
MODE="${MODE:-solo}"

echo "=== F1/F2 スタミナ目視確認 — 下準備 (MODE=${MODE}) ==="
echo ""

require_file() {
  if [[ ! -f "$1" ]]; then
    echo "❌ 見つかりません: $1" >&2
    exit 1
  fi
  echo "✅ $(basename "$1")"
}

require_file "${PROJECT_ROOT}/Assets/Scripts/Game/AnimalComponent/AnimalHandler.cs"
require_file "${PROJECT_ROOT}/Assets/Scripts/Game/ActionButton/AnimalAction_Dash.cs"
require_file "${PROJECT_ROOT}/Assets/Scripts/Game/Photon/PhotonTest/PhotonHPGauge.cs"

if ! grep -q "ComputeStaminaMoveSpeedMultiplier" "${PROJECT_ROOT}/Assets/Scripts/Game/AnimalComponent/AnimalHandler.cs"; then
  echo "❌ F1: ComputeStaminaMoveSpeedMultiplier が見つかりません。main を pull してください。" >&2
  exit 1
fi
if ! grep -q "CanDashFromStaminaRatio" "${PROJECT_ROOT}/Assets/Scripts/Game/ActionButton/AnimalAction_Dash.cs"; then
  echo "❌ F2: CanDashFromStaminaRatio が見つかりません。PR #54 マージ後の main を pull してください。" >&2
  exit 1
fi
echo "   (F1 枯渇減速 + F2 ダッシュ禁止)"

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

echo "--- GameScene 設定（手動操作向け） ---"
set_scene_flag "Main Npc Goap Verify Mode" "_mainNpcGoapVerifyMode:" "0"
set_scene_flag "Enable Main Npc Goap In Production" "_enableMainNpcGoapInProduction:" "0"
set_scene_flag "Human Formation Slot" "_humanFormationSlot:" "0"
set_scene_flag "Debug Overlay" "_debugOverlayEnabled:" "0"

case "${MODE}" in
  solo)
    set_scene_flag "Goap All Teammate Field Npcs" "_goapAllTeammateFieldNpcs:" "0"
    set_scene_flag "Enable Enemy Goap" "_enableEnemyGoap:" "0"
    ;;
  observe)
    echo "ℹ️  MODE=observe: 味方/敵 GOAP は変更しません（手動操作は Main GOAP OFF で可能）"
    ;;
  *)
    echo "❌ MODE は solo または observe です: ${MODE}" >&2
    exit 1
    ;;
esac

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

if [[ -f "${DEBUG_SCENE}" ]]; then
  echo "✅ DebugCharaPlaceScene（DebugPlace）"
else
  echo "⚠️  DebugCharaPlaceScene が見つかりません"
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
  LOG_SUMMARY="${PROJECT_ROOT}/Assets/DebugLog/GoapSummary_latest.txt"
  LOG_DIAG="${PROJECT_ROOT}/Assets/DebugLog/GoapDiag_latest.txt"
  if [[ -f "${LOG_SUMMARY}" ]]; then
    "${SCRIPT_DIR}/archive-goap-summary.sh" "${ARCHIVE_LABEL}"
  fi
  rm -f "${LOG_SUMMARY}" "${LOG_DIAG}"
  echo "✅ GOAP ログをリセット（目視では不要だが整理済み）"
else
  echo "ℹ️  RESET_LOGS=0: 既存ログを保持"
fi
echo ""

echo "--- 実装パラメータ（目視の期待値） ---"
echo "  F1 減速開始: スタミナ残量 25% 以下で線形減速"
echo "  F1 枯渇時:   移動速度 ×0.55（通常移動のみ。ダッシュ倍率は F2 で無効）"
echo "  F2 ダッシュ: 残量 0% で開始不可・押下中も自動解除"
echo "  消費:        通常 10/s・ダッシュ 20/s（移動入力あり時）"
echo "  回復:        待機 20/s（スライドパッドを離す）"
echo ""
echo "--- Unity 手順（推奨: DebugPlace） ---"
echo "  1. Unity を開く（MainMenuScene）→ Play"
echo "  2. オフライン対戦 → DebugPlace（DebugCharaPlaceScene）"
echo "  3. 味方 slot0（Lion）のみ中央付近に配置。ボールは未所持 or 遠方"
echo "  4. 試合時間 180 秒程度 → スタート → GameScene"
echo "  5. READY → GAME（キックオフ UI 完了）"
echo ""
echo "--- 操作（Editor） ---"
echo "  移動:     画面のスライドパッド（マウスドラッグ）"
echo "  ダッシュ: A キー長押し（AnimalInputKey_Player）"
echo "  待機回復: スライドパッドを離す"
echo ""
echo "  ※ Main NPC 本番 GOAP は OFF 済み → 手動入力が有効です"
echo "  ※ MODE=solo では味方 Sub / 敵 GOAP も OFF（フィールドが静か）"
echo ""
echo "--- 目視チェックリスト ---"
echo "  [F1] 満タン時: ダッシュ＋移動で最速"
echo "  [F1] 残量 ~25% 以下: 同じ操作でも明らかに遅くなる（HP バー参照）"
echo "  [F1] 残量 0%:   さらに遅い（×0.55）が歩ける"
echo "  [F2] 残量 >0%:  A 押下でダッシュ加速（×1.5 相当）"
echo "  [F2] 残量 0%:   A を押しても加速しない（通常移動速度のまま）"
echo "  [F2] 枯渇直前:  ダッシュ中に 0% へ落ちたら加速が切れる"
echo ""
echo "--- 確認後（任意） ---"
echo "  GOAP 回帰（ロジック無変更の確認）:"
echo "    ./scripts/playtest/prepare-goap-npc-watch-match.sh"
echo "    ./scripts/playtest/run-phase-d-3min-playtest.sh"
echo ""
echo "  観戦モードへ戻す:"
echo "    ./scripts/playtest/prepare-goap-npc-watch-match.sh"
echo ""
