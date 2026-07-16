using UnityEngine;

/// <summary>
/// 6-H P0: 敵保持・近接時に slot0 が SlideTackle を本番選出することを検証。
/// _batchPatternIndexStart/End = #10（1パターン）。
/// </summary>
public class GoapCombinedDefenseSlideTackleDebugSetup : GoapDefenseActionVerificationSetup
{
    [Header("敵配置")]
    [SerializeField] private GoapEnemyPositionDebugPatterns _enemyLayouts;

    protected override string SummaryLogTag => "GOAP_SLIDE_TACKLE_SETUP";

    protected override GoapDefenseActionUnderTest ActionUnderTest =>
        GoapDefenseActionUnderTest.SlideTackle;

    protected override IGoapDefenseProductionSelectionExpectation ProductionSelectionExpectation =>
        GoapDefenseProductionSelectionExpectations.SlideTackle;

    protected override string BatchVerificationBanner => "Slide tackle batch verification";

    protected override string ProductionSelectionVerificationBanner =>
        "Slide tackle production selection (SlideTackle)";

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

        _enemyLayouts.ApplyPattern(GoapEnemyPositionDebugPatterns.LayoutPattern.PressBallOwner);
        LogLine($"ApplyEnemyLayout({pattern}) layout=PressBallOwner");
    }
}
