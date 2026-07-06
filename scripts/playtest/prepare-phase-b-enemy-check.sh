#!/usr/bin/env bash
# Phase B 敵 GOAP 確認: ログリセットと GameScene 設定チェック。
set -euo pipefail

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
PROJECT_ROOT="$(cd "${SCRIPT_DIR}/../.." && pwd)"
SCENE="${PROJECT_ROOT}/Assets/Scenes/GameScene.unity"
LOG_SUMMARY="${PROJECT_ROOT}/Assets/DebugLog/GoapSummary_latest.txt"
LOG_DIAG="${PROJECT_ROOT}/Assets/DebugLog/GoapDiag_latest.txt"
ARCHIVE_LABEL="${1:-}"

echo "=== Phase B 敵 GOAP 確認 — 下準備 ==="
echo ""

if [[ ! -f "${PROJECT_ROOT}/Assets/Scripts/Game/Goap/GoapFieldNpcPerspective.cs" ]]; then
  echo "❌ Phase B コードが見つかりません。" >&2
  exit 1
fi
echo "✅ Phase B コード（GoapFieldNpcPerspective / EnemySquadControl）"

BRANCH="$(git -C "${PROJECT_ROOT}" branch --show-current 2>/dev/null || echo "?")"
echo "   ブランチ: ${BRANCH}"

if [[ ! -f "${SCENE}" ]]; then
  echo "❌ GameScene が見つかりません: ${SCENE}" >&2
  exit 1
fi

if grep -q "_enemySquadControl:" "${SCENE}"; then
  echo "✅ TeamFacade → EnemySquadControl 参照あり"
else
  echo "⚠️  EnemySquadControl が GameScene に未設定"
fi

if grep -q "_enableEnemyGoap: 1" "${SCENE}"; then
  echo "✅ Enable Enemy Goap = ON"
else
  echo "⚠️  Enable Enemy Goap を確認してください（Inspector: SquadControlController）"
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

if [[ -f "${LOG_SUMMARY}" ]]; then
  label="phaseB"
  if [[ -n "${ARCHIVE_LABEL}" ]]; then
    label="${ARCHIVE_LABEL}"
  fi
  "${SCRIPT_DIR}/archive-goap-summary.sh" "${label}"
fi

rm -f "${LOG_SUMMARY}" "${LOG_DIAG}"
echo ""
echo "✅ ログをリセットしました:"
echo "   ${LOG_SUMMARY}"
echo "   ${LOG_DIAG}"

echo ""
echo "--- Unity での確認手順 ---"
echo "  推奨: MainMenu → オフライン対戦"
echo "  DebugPlace: 敵 slot0（Tiger 等）にボール保持 + 敵ゴール寄りに配置"
echo "  または GameScene + GoapMainNpcVerifyBootstrap:"
echo "    Ball Target = EnemyForDefenseVerify, Enemy Ball Owner Index = 0"
echo ""
echo "  Play 約 30 秒 → 停止後:"
echo "    ./scripts/playtest/extract-goap-logs-from-editor.sh <HH:MM> owner=Tiger"
echo "    ./scripts/playtest/analyze-enemy-main-npc-goap-log.sh Assets/DebugLog/GoapSummary_latest.txt owner=Tiger"
echo ""
echo "期待ログ:"
echo "  敵 Main: prodM1:True + BallPossessionAttack + PassToTeammate/ShootAtGoal"
echo "  敵 Main パス後: prodM2:True + TeamBallSupport + サポート行動"
echo "  敵 Sub（味方ボール時）: TeamBallSupport + GetOpen/CreateSupportAngle 等"
echo "  味方 Sub（敵ボール時）: DefensivePositioning + MoveToDefensivePosition 等"
