using UnityEngine;

/// <summary>
/// GetOpen + CSA + MTS 統合本番選出リグレッション。
/// _batchPreset=CombinedSupportRegression（#2〜12）で 1 Play により 3 アクションの本番選出を検証する。
/// _verifyProductionSelection=ON、verificationOnly=OFF（本番候補）を推奨。
/// </summary>
public class GoapCombinedSupportRegressionDebugSetup : GoapSupportActionVerificationSetup
{
    [Header("統合リグレッション")]
    [SerializeField] private GoapEnemyPositionDebugPatterns _enemyLayouts;
    [Tooltip("パターン別に敵配置が無い場合のデフォルト")]
    [SerializeField] private GoapEnemyPositionDebugPatterns.LayoutPattern _defaultEnemyLayout =
        GoapEnemyPositionDebugPatterns.LayoutPattern.PressBallOwner;

    protected override string SummaryLogTag => "GOAP_SUPPORT_REGRESSION_SETUP";

    protected override GoapSupportActionUnderTest ActionUnderTest =>
        GoapSupportActionUnderTest.None;

    protected override IGoapProductionSelectionExpectation ProductionSelectionExpectation =>
        GoapProductionSelectionExpectations.CombinedSupportRegression;

    protected override string BatchVerificationBanner =>
        "Combined support action regression (GetOpen + CSA + MTS)";

    protected override string ProductionSelectionVerificationBanner =>
        "Combined support action production selection regression (GetOpen + CSA + MTS)";

    protected override void ApplyCompanionVerificationState(GoapSupportLayoutPatternId pattern)
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

        var enemyLayout = ResolveEnemyLayout(pattern);
        _enemyLayouts.ApplyPattern(enemyLayout);
        LogLine($"ApplyEnemyLayout({pattern}) layout={enemyLayout}");
    }

    private GoapEnemyPositionDebugPatterns.LayoutPattern ResolveEnemyLayout(GoapSupportLayoutPatternId pattern)
    {
        return pattern switch
        {
            GoapSupportLayoutPatternId.CfOwner_RwWrongSide
                or GoapSupportLayoutPatternId.CfOwner_LwOnWrongSide
                => GoapEnemyPositionDebugPatterns.LayoutPattern.PressBallOwner,
            GoapSupportLayoutPatternId.CfOwner_OnRightWing
                => GoapEnemyPositionDebugPatterns.LayoutPattern.PressRightWing,
            GoapSupportLayoutPatternId.CfOwner_OnLeftWing
                => GoapEnemyPositionDebugPatterns.LayoutPattern.PressLeftWing,
            GoapSupportLayoutPatternId.CfOwner_AllOverlapped
                or GoapSupportLayoutPatternId.CfOwner_Clustered
                => GoapEnemyPositionDebugPatterns.LayoutPattern.PressBallOwner,
            GoapSupportLayoutPatternId.CfOwner_NearCorrectLanes
                or GoapSupportLayoutPatternId.CfOwner_AtCorrectLanes
                => GoapEnemyPositionDebugPatterns.LayoutPattern.BlockPassLane,
            _ => _defaultEnemyLayout,
        };
    }
}
