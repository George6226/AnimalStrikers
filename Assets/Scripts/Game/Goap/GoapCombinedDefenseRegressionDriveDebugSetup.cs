using UnityEngine;

/// <summary>
/// Phase 7b 守備統合ドライブ: #7〜#8 で全候補コスト競争 + 敵保持ドライブ中の選出 + Retarget 追従。
/// _batchPatternIndexStart/End = #7〜#8（2パターン）。verificationOnly=OFF。
/// </summary>
public class GoapCombinedDefenseRegressionDriveDebugSetup : GoapDefenseActionVerificationSetup
{
    [Header("敵配置")]
    [SerializeField] private GoapEnemyPositionDebugPatterns _enemyLayouts;
    [SerializeField] private GoapEnemyPositionDebugPatterns.LayoutPattern _defaultEnemyLayout =
        GoapEnemyPositionDebugPatterns.LayoutPattern.PressBallOwner;

    protected override string SummaryLogTag => "GOAP_DEFENSE_COMBINED_DRIVE_SETUP";

    protected override GoapDefenseActionUnderTest ActionUnderTest => GoapDefenseActionUnderTest.None;

    protected override IGoapDefenseProductionSelectionExpectation ProductionSelectionExpectation =>
        GoapDefenseProductionSelectionExpectations.DefenseCombinedDrive;

    protected override IGoapDefenseProductionSelectionExpectation DriveProductionSelectionExpectation =>
        GoapDefenseProductionSelectionExpectations.DefenseCombinedDrive;

    protected override IGoapDefenseActionRuntimePassCriteria RuntimePassCriteria =>
        GoapDefenseActionRuntimePassCriteria.EnemyOwnerDrive;

    protected override string BatchVerificationBanner =>
        "Defense combined drive follow verification (#7-#8)";

    protected override string ProductionSelectionVerificationBanner =>
        "Defense combined drive (full competition + enemy-owner drive)";

    protected override GoapProductionSelectionResolveMode ResolveProductionSelectionModeForPattern(
        GoapDefenseLayoutPatternId pattern) =>
        GoapProductionSelectionResolveMode.MinCostFirstPlanCosts;

    protected override GoapProductionSelectionResolveMode ResolveDriveProductionSelectionModeForPattern(
        GoapDefenseLayoutPatternId pattern) =>
        GoapProductionSelectionResolveMode.MinCostFirstPlanCosts;

    protected override float ProductionSelectionPlanCostsTimeoutSeconds => 80f;

    protected override void ApplyCompanionVerificationState(GoapDefenseLayoutPatternId pattern)
    {
        if (_enemyLayouts == null)
        {
            _enemyLayouts = FindFirstObjectByType<GoapEnemyPositionDebugPatterns>();
        }

        if (_enemyLayouts == null)
        {
            LogLine($"ApplyEnemyLayout({pattern}) skipped: GoapEnemyPositionDebugPatterns not found");
            return;
        }

        _enemyLayouts.ApplyPattern(_defaultEnemyLayout);
        LogLine($"ApplyEnemyLayout({pattern}) layout={_defaultEnemyLayout}");
    }
}
