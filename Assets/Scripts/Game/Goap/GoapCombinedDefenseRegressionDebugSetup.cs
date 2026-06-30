using UnityEngine;

/// <summary>
/// Phase 7a 守備統合本番選出: MoveToDefensive + Mark + BlockPass + BlockShot がコスト競争。
/// _batchPatternIndexStart/End = #2〜#6（5パターン）。verificationOnly=OFF。
/// </summary>
public class GoapCombinedDefenseRegressionDebugSetup : GoapDefenseActionVerificationSetup
{
    [Header("敵配置")]
    [SerializeField] private GoapEnemyPositionDebugPatterns _enemyLayouts;
    [SerializeField] private GoapEnemyPositionDebugPatterns.LayoutPattern _defaultEnemyLayout =
        GoapEnemyPositionDebugPatterns.LayoutPattern.DefensiveLine;

    protected override string SummaryLogTag => "GOAP_DEFENSE_COMBINED_SETUP";

    protected override GoapDefenseActionUnderTest ActionUnderTest => GoapDefenseActionUnderTest.None;

    protected override IGoapDefenseProductionSelectionExpectation ProductionSelectionExpectation =>
        GoapDefenseProductionSelectionExpectations.DefenseCombined;

    protected override string BatchVerificationBanner =>
        "Defense combined production selection regression (#2-#6)";

    protected override string ProductionSelectionVerificationBanner =>
        "Defense combined production selection (MTD + Mark + BlockPass + BlockShot)";

    protected override GoapProductionSelectionResolveMode ProductionSelectionResolveModeAtApply =>
        GoapProductionSelectionResolveMode.LastPlanCosts;

    protected override GoapProductionSelectionResolveMode ResolveProductionSelectionModeForPattern(
        GoapDefenseLayoutPatternId pattern) =>
        pattern is GoapDefenseLayoutPatternId.EnemyOwner_BlockPassLane
            or GoapDefenseLayoutPatternId.EnemyOwner_BlockShotLane
            ? GoapProductionSelectionResolveMode.LastPlanCosts
            : GoapProductionSelectionResolveMode.FirstPlanCosts;

    protected override float ProductionSelectionPlanCostsTimeoutSeconds => 80f;

    protected override bool RequiresExpectedActionMatchForReady(GoapDefenseLayoutPatternId pattern) =>
        pattern is GoapDefenseLayoutPatternId.EnemyOwner_BlockPassLane
            or GoapDefenseLayoutPatternId.EnemyOwner_BlockShotLane;

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

        GoapEnemyPositionDebugPatterns.LayoutPattern layout = pattern switch
        {
            GoapDefenseLayoutPatternId.EnemyOwner_ClusteredAllies
                or GoapDefenseLayoutPatternId.EnemyOwner_SpreadMidfield
                or GoapDefenseLayoutPatternId.EnemyOwner_MarkFreeTarget
                or GoapDefenseLayoutPatternId.EnemyOwner_BlockPassLane =>
                GoapEnemyPositionDebugPatterns.LayoutPattern.PressBallOwner,
            GoapDefenseLayoutPatternId.EnemyOwner_BlockShotLane =>
                GoapEnemyPositionDebugPatterns.LayoutPattern.DefensiveLine,
            _ => _defaultEnemyLayout,
        };
        _enemyLayouts.ApplyPattern(layout);
        LogLine($"ApplyEnemyLayout({pattern}) layout={layout}");
    }
}
