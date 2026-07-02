#!/usr/bin/env bash
# GOAP CI 共通設定（run-goap-ci.sh / run-goap-docker.sh / resolve-batch-verify-result.sh で source）。
set -euo pipefail

GOAP_UNITY_VERSION="${GOAP_UNITY_VERSION:-${UNITY_VERSION:-6000.2.7f2}}"
GOAP_DOCKER_IMAGE="${GOAP_UNITY_DOCKER_IMAGE:-unityci/editor:ubuntu-${GOAP_UNITY_VERSION}-base-3}"
GOAP_EDITMODE_TEST_FILTER="${GOAP_EDITMODE_TEST_FILTER:-GoapBatchVerificationLogParserTests|TeammateNpcSupportPlanningEditModeTests|GoapProductionSelectionExpectationsEditModeTests|GoapDefenseProductionSelectionExpectationsEditModeTests|GoapMainNpcCatalogEditModeTests|MainNpcPostPassPlanningEditModeTests|GoapPassTargetSelectionEditModeTests}"
GOAP_EDITMODE_EXPECTED_TESTS="${GOAP_EDITMODE_EXPECTED_TESTS:-140}"

# token|cli_flag|result_file|unity_log|label
GOAP_BATCH_PROFILES=(
  "combined|-goapBatchVerify=combined|goap-batch-result.txt|goap-batch-verify.log|統合本番選出 #2-#12 (11/11)"
  "wingDrive|-goapBatchVerify=wingDrive|goap-batch-wing-result.txt|goap-batch-wing-verify.log|翼ドライブ #17/#18 (SELECTION+RUNTIME 2/2)"
  "cfDrive|-goapBatchVerify=cfDrive|goap-batch-cf-drive-result.txt|goap-batch-cf-drive-verify.log|CFドライブ #13-#16 (SELECTION+RUNTIME 4/4)"
  "defenseBaseline|-goapBatchVerify=defenseBaseline|goap-batch-defense-result.txt|goap-batch-defense-verify.log|守備基本 #2-#3 (SELECTION 2/2)"
  "defenseTactical|-goapBatchVerify=defenseTactical|goap-batch-defense-tactical-result.txt|goap-batch-defense-tactical-verify.log|守備戦術 #4-#6,#9 (SELECTION 4/4)"
  "defenseCombined|-goapBatchVerify=defenseCombined|goap-batch-defense-combined-result.txt|goap-batch-defense-combined-verify.log|守備統合 #2-#6,#9 (SELECTION 6/6)"
  "defenseCombinedDrive|-goapBatchVerify=defenseCombinedDrive|goap-batch-defense-combined-drive-result.txt|goap-batch-defense-combined-drive-verify.log|守備統合ドライブ #7-#8,#9 (SELECTION+RUNTIME 3/3)"
  "defenseDrive|-goapBatchVerify=defenseDrive|goap-batch-defense-drive-result.txt|goap-batch-defense-drive-verify.log|守備ドライブ #7-#8 (SELECTION+RUNTIME 2/2)"
  "mainNpcAttack|-goapMainNpcAttackVerify|goap-main-npc-attack-result.txt|goap-main-npc-attack-verify.log|Main NPC 攻撃+パス後サポート (M1/M2)"
)

GOAP_CI_MODES=(all editmode batch batch-combined batch-wing batch-cf-drive batch-main-npc-attack batch-defense batch-defense-tactical batch-defense-combined batch-defense-combined-drive batch-defense-drive)

goap_ci_script_dir() {
  cd "$(dirname "${BASH_SOURCE[1]:-${BASH_SOURCE[0]}}")" && pwd
}

goap_ci_print_usage() {
  local script_name="${1:-run-goap-ci.sh}"
  echo "Usage: ${script_name} [$(IFS='|'; echo "${GOAP_CI_MODES[*]}")]" >&2
  echo "" >&2
  echo "  editmode        EditMode ${GOAP_EDITMODE_EXPECTED_TESTS} 件（約30秒）" >&2
  echo "  batch-combined  combined 本番選出のみ" >&2
  echo "  batch-wing      wingDrive 選出+追従のみ" >&2
  echo "  batch-cf-drive  cfDrive 選出+追従のみ" >&2
  echo "  batch-main-npc-attack  mainNpcAttack Main NPC 攻撃+パス後サポートのみ" >&2
  echo "  batch-defense   defenseBaseline 守備基本のみ" >&2
  echo "  batch-defense-tactical  defenseTactical 守備戦術のみ" >&2
  echo "  batch-defense-combined  defenseCombined 守備統合本番選出のみ" >&2
  echo "  batch-defense-combined-drive  defenseCombinedDrive 守備統合ドライブのみ" >&2
  echo "  batch-defense-drive     defenseDrive 守備ドライブのみ（Phase 6 MTD単体・任意）" >&2
  echo "  batch           上記8バッチ連続（defenseDrive は含まない）" >&2
  echo "  all             EditMode + 8バッチ（CI 相当・約16-20分）" >&2
}

goap_ci_mode_valid() {
  local mode="$1"
  local candidate
  for candidate in "${GOAP_CI_MODES[@]}"; do
    if [[ "${mode}" == "${candidate}" ]]; then
      return 0
    fi
  done
  return 1
}

goap_ci_mode_runs_editmode() {
  [[ "${1}" == "editmode" || "${1}" == "all" ]]
}

goap_ci_mode_runs_batch() {
  case "${1}" in
    batch|all|batch-combined|batch-wing|batch-cf-drive|batch-main-npc-attack|batch-defense|batch-defense-tactical|batch-defense-combined|batch-defense-combined-drive|batch-defense-drive) return 0 ;;
    *) return 1 ;;
  esac
}

goap_ci_batch_profiles_for_mode() {
  local mode="$1"
  local entry token
  for entry in "${GOAP_BATCH_PROFILES[@]}"; do
    IFS='|' read -r token _ _ _ _ <<< "${entry}"
    case "${mode}" in
      batch|all)
        # defenseDrive (#7/#8 MTD単体) は defenseCombinedDrive と重複。
        # Docker 連続実行時に 8 本目でハング→timeout(124) する事例があるため all/batch から除外。
        [[ "${token}" == "defenseDrive" ]] && continue
        echo "${token}"
        ;;
      batch-combined)
        [[ "${token}" == "combined" ]] && echo "${token}"
        ;;
      batch-wing)
        [[ "${token}" == "wingDrive" ]] && echo "${token}"
        ;;
      batch-cf-drive)
        [[ "${token}" == "cfDrive" ]] && echo "${token}"
        ;;
      batch-main-npc-attack)
        [[ "${token}" == "mainNpcAttack" ]] && echo "${token}"
        ;;
      batch-defense)
        [[ "${token}" == "defenseBaseline" ]] && echo "${token}"
        ;;
      batch-defense-tactical)
        [[ "${token}" == "defenseTactical" ]] && echo "${token}"
        ;;
      batch-defense-combined)
        [[ "${token}" == "defenseCombined" ]] && echo "${token}"
        ;;
      batch-defense-combined-drive)
        [[ "${token}" == "defenseCombinedDrive" ]] && echo "${token}"
        ;;
      batch-defense-drive)
        [[ "${token}" == "defenseDrive" ]] && echo "${token}"
        ;;
    esac
  done
}

goap_ci_resolve_batch_profile() {
  local query="${1:?}"
  local entry token flag result log label
  for entry in "${GOAP_BATCH_PROFILES[@]}"; do
    IFS='|' read -r token flag result log label <<< "${entry}"
    if [[ "${query}" == "${token}" || "${query}" == "${flag}" ]]; then
      GOAP_PROFILE_TOKEN="${token}"
      GOAP_PROFILE_FLAG="${flag}"
      GOAP_PROFILE_RESULT_FILE="${result}"
      GOAP_PROFILE_LOG_FILE="${log}"
      GOAP_PROFILE_LABEL="${label}"
      return 0
    fi
  done
  return 1
}

goap_ci_batch_diag_candidates() {
  local project_root="${1:?}"
  local token="${2:?}"
  local log_dir="${project_root}/Logs"
  case "${token}" in
    wingDrive)
      echo "${log_dir}/GoapDiag_wing_latest.txt"
      echo "${project_root}/Assets/DebugLog/GoapDiag_latest.txt"
      echo "${log_dir}/GoapDiag_latest.txt"
      ;;
    cfDrive)
      echo "${log_dir}/GoapDiag_cf_drive_latest.txt"
      echo "${project_root}/Assets/DebugLog/GoapDiag_latest.txt"
      echo "${log_dir}/GoapDiag_latest.txt"
      ;;
    defenseBaseline)
      echo "${log_dir}/GoapDiag_defense_latest.txt"
      echo "${project_root}/Assets/DebugLog/GoapDiag_latest.txt"
      echo "${log_dir}/GoapDiag_latest.txt"
      ;;
    defenseTactical)
      echo "${log_dir}/GoapDiag_defense_tactical_latest.txt"
      echo "${project_root}/Assets/DebugLog/GoapDiag_latest.txt"
      echo "${log_dir}/GoapDiag_latest.txt"
      ;;
    defenseCombined)
      echo "${log_dir}/GoapDiag_defense_combined_latest.txt"
      echo "${project_root}/Assets/DebugLog/GoapDiag_latest.txt"
      echo "${log_dir}/GoapDiag_latest.txt"
      ;;
    defenseCombinedDrive)
      echo "${log_dir}/GoapDiag_defense_combined_drive_latest.txt"
      echo "${project_root}/Assets/DebugLog/GoapDiag_latest.txt"
      echo "${log_dir}/GoapDiag_latest.txt"
      ;;
    defenseDrive)
      echo "${log_dir}/GoapDiag_defense_drive_latest.txt"
      echo "${project_root}/Assets/DebugLog/GoapDiag_latest.txt"
      echo "${log_dir}/GoapDiag_latest.txt"
      ;;
    mainNpcAttack)
      echo "${log_dir}/GoapSummary_latest.txt"
      echo "${project_root}/Assets/DebugLog/GoapSummary_latest.txt"
      ;;
    *)
      echo "${log_dir}/GoapDiag_latest.txt"
      echo "${project_root}/Assets/DebugLog/GoapDiag_latest.txt"
      ;;
  esac
}

goap_ci_clear_batch_markers() {
  local log_dir="${1:?}"
  rm -f \
    "${log_dir}/goap-batch-pending-exit.txt" \
    "${log_dir}/goap-batch-started.marker" \
    "${log_dir}/goap-batch-profile.txt"
}

goap_ci_clear_profile_markers() {
  local log_dir="${1:?}"
  local token="${2:-}"
  goap_ci_clear_batch_markers "${log_dir}"
  if [[ "${token}" == "mainNpcAttack" ]]; then
    rm -f \
      "${log_dir}/goap-main-npc-attack-pending-exit.txt" \
      "${log_dir}/goap-main-npc-attack-started.marker"
  fi
}

goap_ci_report_batch_failure() {
  local project_root="${1:?}"
  local token="${2:?}"
  local unity_log="${3:-}"
  local diag candidate
  if [[ -n "${unity_log}" && -f "${unity_log}" ]]; then
    echo "[goap-ci] --- tail ${unity_log} ---" >&2
    tail -40 "${unity_log}" >&2 || true
  fi
  while IFS= read -r diag; do
    [[ -z "${diag}" ]] && continue
    if [[ -f "${diag}" ]]; then
      echo "[goap-ci] --- ${diag} (${token} BATCH markers) ---" >&2
      if [[ "${token}" == "mainNpcAttack" ]]; then
        grep -E 'GOAP_M1_ATTACK_RUNNER|GoapMainNpcVerifyBootstrap|bootstrap complete|ActionStart\(action=' "${diag}" | tail -40 >&2 || true
      else
        grep -E 'BATCH_|SELECTION_|RUNTIME_|GOAP_BATCH_RUNNER|AUTO_DRIVE|GoapDebugPlayBootstrap' "${diag}" | tail -40 >&2 || true
      fi
      return 0
    fi
  done < <(goap_ci_batch_diag_candidates "${project_root}" "${token}")
}
