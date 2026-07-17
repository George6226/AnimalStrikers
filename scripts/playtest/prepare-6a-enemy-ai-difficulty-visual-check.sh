#!/usr/bin/env bash
# 6-A P0: 敵 AI 難易度（Easy / Normal / Hard）目視確認の下準備。
# EnemySquadControlController._difficulty を切り替えて、攻撃バイアス差を観戦する。
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
ARCHIVE_LABEL="${1:-6a_enemy_ai_difficulty_visual_before}"
GOAP_LOG_LEVEL="${GOAP_LOG_LEVEL:-2}"
# Easy | Normal | Hard（または 0/1/2）
DIFFICULTY="${DIFFICULTY:-Normal}"

echo "=== 6-A 敵 AI 難易度 — 目視下準備 ==="
echo ""

require_file() {
  if [[ ! -f "$1" ]]; then
    echo "❌ 見つかりません: $1" >&2
    exit 1
  fi
  echo "✅ $(basename "$1")"
}

require_file "${PROJECT_ROOT}/Assets/Scripts/Game/Team/Control/EnemyAiDifficulty.cs"
require_file "${PROJECT_ROOT}/Assets/Scripts/Game/Team/Control/EnemyAiBalance.cs"
require_file "${PROJECT_ROOT}/Assets/Scripts/Game/Team/Control/EnemySquadControlController.cs"
require_file "${PROJECT_ROOT}/Assets/Scripts/Game/Team/Control/MainNpcAttackPlanning.cs"
require_file "${PROJECT_ROOT}/Assets/Scripts/Game/Goap/Verification/Tests/Editor/EnemyAiBalanceEditModeTests.cs"

if ! grep -q "EnemyAiBalance.PassPenalty" \
  "${PROJECT_ROOT}/Assets/Scripts/Game/Team/Control/MainNpcAttackPlanning.cs"; then
  echo "❌ MainNpcAttackPlanning が EnemyAiBalance を参照していません。" >&2
  exit 1
fi
if ! grep -q "_difficulty" \
  "${PROJECT_ROOT}/Assets/Scripts/Game/Team/Control/EnemySquadControlController.cs"; then
  echo "❌ EnemySquadControlController に _difficulty がありません。" >&2
  exit 1
fi
echo "   (6-A P0: Easy/Normal/Hard → PassPenalty / ShootDiscount / planning interval)"

BRANCH="$(git -C "${PROJECT_ROOT}" branch --show-current 2>/dev/null || echo "?")"
MAIN_SHA="$(git -C "${PROJECT_ROOT}" rev-parse --short HEAD 2>/dev/null || echo "?")"
echo "   ブランチ: ${BRANCH} @ ${MAIN_SHA}"
echo ""

if [[ ! -f "${SCENE}" ]]; then
  echo "❌ GameScene が見つかりません: ${SCENE}" >&2
  exit 1
fi

resolve_difficulty_value() {
  local raw
  raw="$(echo "$1" | tr '[:upper:]' '[:lower:]')"
  case "${raw}" in
    easy|0) echo 0 ;;
    normal|1) echo 1 ;;
    hard|2) echo 2 ;;
    *)
      echo "❌ DIFFICULTY は Easy/Normal/Hard（または 0/1/2）: $1" >&2
      exit 1
      ;;
  esac
}

difficulty_label() {
  case "$1" in
    0) echo "Easy" ;;
    1) echo "Normal" ;;
    2) echo "Hard" ;;
    *) echo "?" ;;
  esac
}

DIFFICULTY_VALUE="$(resolve_difficulty_value "${DIFFICULTY}")"
DIFFICULTY_NAME="$(difficulty_label "${DIFFICULTY_VALUE}")"

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

ensure_enemy_difficulty() {
  local expected="$1"
  python3 - <<PY
from pathlib import Path
import re

path = Path("${SCENE}")
text = path.read_text()
marker = "m_EditorClassIdentifier: Assembly-CSharp::EnemySquadControlController"
idx = text.find(marker)
if idx < 0:
    raise SystemExit("❌ EnemySquadControlController が GameScene にありません")
chunk_end = text.find("\n--- ", idx)
if chunk_end < 0:
    chunk_end = len(text)
block = text[idx:chunk_end]
expected = "${expected}"
label = "$(difficulty_label "${expected}")"
if "_difficulty:" in block:
    block2, n = re.subn(r"_difficulty: \\d+", f"_difficulty: {expected}", block, count=1)
    if n != 1:
        raise SystemExit("❌ _difficulty の更新に失敗")
    msg = f"✅ Enemy AI Difficulty = {expected}（{label}）"
    if f"_difficulty: {expected}" in block:
        msg = f"✅ Enemy AI Difficulty = {expected}（{label}）"
    else:
        msg = f"✅ Enemy AI Difficulty = {expected}（更新 → {label}）"
else:
    block2, n = re.subn(
        r"(_goapPlanningInterval: \\d+(?:\\.\\d+)?)",
        rf"\\1\\n  _difficulty: {expected}",
        block,
        count=1,
    )
    if n != 1:
        raise SystemExit("❌ _difficulty の挿入に失敗（_goapPlanningInterval 未検出）")
    msg = f"✅ Enemy AI Difficulty = {expected}（新規追加 / {label}）"
path.write_text(text[:idx] + block2 + text[chunk_end:])
print(msg)
PY
}

echo "--- GameScene 設定（GOAP 観戦 + 敵難易度） ---"
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
ensure_enemy_difficulty "${DIFFICULTY_VALUE}"
echo "   → 今回の難易度: ${DIFFICULTY_NAME} (${DIFFICULTY_VALUE})"

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

echo "--- 観る対象（難易度=${DIFFICULTY_NAME}） ---"
echo "  敵 Main（slot0・通常 Elephant）がボール保持したときの Pass / Shoot の頻度"
echo "  Easy: バイアス弱め・判断ゆっくり → 無謀な長距離シュートが減りやすい"
echo "  Normal: 従来どおり（Pass+0.55 / レーン空き Shoot−0.28）"
echo "  Hard: シュート寄り・判断速め → フィニッシュが積極的"
echo "  診断: GOAP_LOG_LEVEL=${GOAP_LOG_LEVEL}（既定 2=Verbose / PlanCosts 向け）"
echo ""

echo "--- Unity 手順 ---"
echo "  1. Unity を開く（MainMenuScene）→ Play"
echo "  2. オフライン対戦（MATCHING 約 5 秒）→ READY → GAME"
echo "  3. 手動操作なしで観戦（カメラは slot0 追従）"
echo "  4. 敵が保持〜シュートする場面を 1〜2 分観察"
echo "  5. Scene ビューで敵 Main を選択すると見やすい"
echo "  6. 難易度を変えて比較する場合:"
echo "       DIFFICULTY=Easy  ./scripts/playtest/prepare-6a-enemy-ai-difficulty-visual-check.sh"
echo "       DIFFICULTY=Hard  ./scripts/playtest/prepare-6a-enemy-ai-difficulty-visual-check.sh"
echo "     （Inspector: EnemySquadControl → 6-A: 難易度 でも可）"
echo ""

echo "--- 目視チェックリスト ---"
echo "  [6A-A] EnemySquadControl の Difficulty が ${DIFFICULTY_NAME} になっている"
echo "  [6A-B] 敵 Field NPC が GOAP で動いている（棒立ちしない）"
echo "  [6A-C] 敵保持時に PassToTeammate / ShootAtGoal が選ばれる"
echo "  [6A-D] Easy と Hard でシュート積極性が違う（同じ場面を難易度違いで比較）"
echo "  [6A-E] Normal が現行プレイ感覚から大きく崩れていない"
echo ""

echo "--- 確認後（ログ解析） ---"
echo "  ./scripts/playtest/analyze-enemy-main-npc-goap-log.sh Assets/DebugLog/GoapSummary_latest.txt owner=Elephant"
echo "  grep -E 'ShootAtGoal|PassToTeammate|PlanCosts' Assets/DebugLog/GoapSummary_latest.txt | head"
echo ""

echo "--- 関連 ---"
echo "  汎用観戦: ./scripts/playtest/prepare-goap-npc-watch-match.sh"
echo "  敵シュート寄せ: ./scripts/playtest/prepare-phase-b-enemy-shoot-check.sh"
echo ""
echo "下準備完了。Unity で Play してください。（難易度=${DIFFICULTY_NAME}）"
echo ""
