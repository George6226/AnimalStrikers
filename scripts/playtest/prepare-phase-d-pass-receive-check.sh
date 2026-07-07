#!/usr/bin/env bash
# Phase D: #37 マージ後 — パス受け・攻撃安定化のプレイテスト下準備。
# MODE=ally（味方 GOAP のみ・高速反復）| full（味方+敵 統合・デフォルト）
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
SCENE="${PROJECT_ROOT}/Assets/Scenes/GameScene.unity"
MAIN_MENU_SCENE="${PROJECT_ROOT}/Assets/Scenes/MainMenuScene.unity"
UNITY_LAST_SCENE="${PROJECT_ROOT}/Library/LastSceneManagerSetup.txt"
LOG_SUMMARY="${PROJECT_ROOT}/Assets/DebugLog/GoapSummary_latest.txt"
LOG_DIAG="${PROJECT_ROOT}/Assets/DebugLog/GoapDiag_latest.txt"
MODE="${MODE:-full}"
ALLY_OWNER="${ALLY_OWNER:-Lion}"
ENEMY_OWNER="${ENEMY_OWNER:-Elephant}"
ARCHIVE_LABEL="${1:-phaseD_${MODE}}"

echo "=== Phase D パス受け安定化 — 下準備 (MODE=${MODE}) ==="
echo ""

require_file() {
  if [[ ! -f "$1" ]]; then
    echo "❌ 見つかりません: $1" >&2
    exit 1
  fi
  echo "✅ $(basename "$1")"
}

require_file "${PROJECT_ROOT}/Assets/Scripts/Game/Team/Control/IncomingPassPlanning.cs"
require_file "${PROJECT_ROOT}/Assets/Scripts/Game/Goap/GoapPassFlightTracker.cs"
require_file "${PROJECT_ROOT}/Assets/Scripts/Game/Goap/Goals/Goals/IncomingPassReceiveGoalSO.cs"
require_file "${PROJECT_ROOT}/Assets/Scripts/Game/Goap/GoapActions/GoapActions/MovementActions/MoveToReceivePassActionSO.cs"
require_file "${PROJECT_ROOT}/Assets/Scripts/Game/Goap/GoapActions/GoapActions/AttackActions/DribbleTowardGoalActionSO.cs"
echo "   (#37 パス受け・ドリブル・安定化コード)"

BRANCH="$(git -C "${PROJECT_ROOT}" branch --show-current 2>/dev/null || echo "?")"
MAIN_SHA="$(git -C "${PROJECT_ROOT}" rev-parse --short HEAD 2>/dev/null || echo "?")"
echo "   ブランチ: ${BRANCH} @ ${MAIN_SHA}"
if ! git -C "${PROJECT_ROOT}" merge-base --is-ancestor b55d965 HEAD 2>/dev/null; then
  echo "⚠️  #37 マージコミット (b55d965) が含まれていない可能性があります"
  echo "   git checkout main && git pull origin main を実行してください"
fi
echo ""

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

if [[ "${MODE}" == "full" ]]; then
  check_scene_flag "Enable Enemy Goap" "_enableEnemyGoap:" "1"
else
  echo "ℹ️  MODE=ally: Enable Enemy Goap は OFF 推奨（敵は従来 AI のまま）"
  if grep -q "_enableEnemyGoap: 1" "${SCENE}"; then
    echo "   → SquadControlController: Enable Enemy Goap = OFF にすると観察しやすい"
  fi
fi

check_scene_flag "Debug Overlay" "_debugOverlayEnabled:" "1"

if grep -q "GoapCombinedSupportRegressionDebugSetup" "${SCENE}"; then
  if grep -A20 "GoapCombinedSupportRegressionDebugSetup" "${SCENE}" | grep -q "m_Enabled: 0"; then
    echo "✅ GoapCombinedSupportRegressionDebugSetup = OFF"
  else
    echo "⚠️  GoapCombinedSupportRegressionDebugSetup が ON の可能性"
  fi
fi

if [[ -f "${MAIN_MENU_SCENE}" ]]; then
  if grep -q "MATCHING_TIMEOUT: 5" "${MAIN_MENU_SCENE}"; then
    echo "✅ MainMenu MATCHING_TIMEOUT = 5"
  elif grep -q "MATCHING_TIMEOUT:" "${MAIN_MENU_SCENE}"; then
    sed -i '' 's/MATCHING_TIMEOUT: [0-9]*/MATCHING_TIMEOUT: 5/' "${MAIN_MENU_SCENE}"
    echo "✅ MainMenu MATCHING_TIMEOUT を 5 に更新"
  fi
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

if [[ -f "${LOG_SUMMARY}" ]]; then
  "${SCRIPT_DIR}/archive-goap-summary.sh" "${ARCHIVE_LABEL}"
fi
rm -f "${LOG_SUMMARY}" "${LOG_DIAG}"
echo "✅ ログをリセット: GoapSummary_latest.txt / GoapDiag_latest.txt"
echo ""

echo "--- 前回まで ---"
echo "  ✅ Phase C 統合マッチ手順 (prepare-phase-c-integration-check.sh)"
echo "  ✅ #37 マージ: パス受け / NoGoalIdle / 空パス防止 / 連続パスペナルティ"
echo "  ⬜ Phase D: 本セッション（パス受けフローと攻撃バランスの観察）"
echo ""

if [[ "${MODE}" == "ally" ]]; then
  echo "--- Unity 手順（MODE=ally: 味方 GOAP のみ） ---"
  echo "  1. MainMenu → オフライン対戦 → Play → READY→GAME"
  echo "  2. SquadControlController:"
  echo "       Main Npc Goap Verify Mode = OFF"
  echo "       Enable Main Npc Goap In Production = ON"
  echo "       Enable Enemy Goap = OFF（推奨）"
  echo "  3. 各味方フィールド NPC の AnimalControlBrainRouter（Play 中に Hierarchy で確認）:"
  echo "       Enable Local Goap Filter = ON"
  echo "       Local Goap Debug Side = Ally Only"
  echo "     ※ プレハブに未保存の場合、試合開始前にシーン上で設定"
  echo "  4. 約 3 分プレイ（パス多め・中央停滞の有無を観察）"
else
  echo "--- Unity 手順（MODE=full: 味方+敵 GOAP 統合） ---"
  echo "  1. MainMenu → オフライン対戦 → Play → READY→GAME"
  echo "  2. SquadControlController:"
  echo "       Main Npc Goap Verify Mode = OFF"
  echo "       Enable Main Npc Goap In Production = ON"
  echo "       Enable Enemy Goap = ON"
  echo "  3. AnimalControlBrainRouter: Enable Local Goap Filter = OFF（デフォルト）"
  echo "  4. 約 3 分プレイ（奪取・パス・シュート・守備を自然に発生させる）"
fi

echo ""
echo "  Play 終了後:"
echo "    ./scripts/playtest/extract-goap-logs-from-editor.sh <HH:MM>"
echo "    MODE=${MODE} ./scripts/playtest/analyze-phase-d-pass-receive-log.sh Assets/DebugLog/GoapSummary_latest.txt"
echo ""
echo "  owner 指定:"
echo "    ALLY_OWNER=${ALLY_OWNER} ENEMY_OWNER=${ENEMY_OWNER} \\"
echo "      ./scripts/playtest/analyze-phase-d-pass-receive-log.sh <log> owner=${ALLY_OWNER} owner=${ENEMY_OWNER}"
echo ""
echo "--- Phase D で見る指標（#37 改善確認） ---"
echo "  | 指標 | 改善目安 |"
echo "  | MoveToReceivePass / IncomingPassReceive | パス時に受け手が走る |"
echo "  | ActionComplete(MoveToReceivePass) | 受け完了後 BallPossessionAttack へ |"
echo "  | ActionStart(DribbleTowardGoal) | 保持中の前進ドリブル |"
echo "  | NoGoalIdle(wait=5.0s) | 0 件（長い待機なし） |"
echo "  | [GOAP_PASS] not_holding_ball | 0 件 |"
echo "  | Aborted（試合中） | attempt>=1 が過多でない（目安 <= 25 / 3分） |"
echo "  | PassToTeammate vs ShootAtGoal | パス一辺倒でない |"
echo ""
echo "  味方のみの反復: MODE=ally $0"
echo "  統合確認:       MODE=full $0"
echo "  従来の統合手順: ./scripts/playtest/prepare-phase-c-integration-check.sh"
