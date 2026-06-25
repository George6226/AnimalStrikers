using UnityEngine;

/// <summary>
/// Phase 5 守備基本: 相手ボール時に全味方 slot が MoveToDefensivePosition を選ぶことを検証。
/// _batchPatternIndexStart/End = #2〜#3（2パターン）。
/// </summary>
public class GoapCombinedDefenseBaselineDebugSetup : GoapDefenseActionVerificationSetup
{
    [Header("敵配置")]
    [SerializeField] private GoapEnemyPositionDebugPatterns _enemyLayouts;
    [SerializeField] private GoapEnemyPositionDebugPatterns.LayoutPattern _defaultEnemyLayout =
        GoapEnemyPositionDebugPatterns.LayoutPattern.DefensiveLine;

    protected override string SummaryLogTag => "GOAP_DEFENSE_BASELINE_SETUP";

    protected override GoapDefenseActionUnderTest ActionUnderTest =>
        GoapDefenseActionUnderTest.MoveToDefensivePosition;

    protected override IGoapDefenseProductionSelectionExpectation ProductionSelectionExpectation =>
        GoapDefenseProductionSelectionExpectations.DefenseBaseline;

    protected override string BatchVerificationBanner => "Defense baseline batch verification";

    protected override string ProductionSelectionVerificationBanner =>
        "Defense baseline production selection (MoveToDefensivePosition)";

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
                or GoapDefenseLayoutPatternId.EnemyOwner_SpreadMidfield =>
                GoapEnemyPositionDebugPatterns.LayoutPattern.PressBallOwner,
            _ => _defaultEnemyLayout,
        };
        _enemyLayouts.ApplyPattern(layout);
        LogLine($"ApplyEnemyLayout({pattern}) layout={layout}");
    }
}
