using UnityEngine;

/// <summary>
/// GetOpen 単体検証用セットアップ。
/// CF 保持パターン（CfOwnerStaticGetOpen）＋敵配置で非保持翼 slot1/2 の選出を検証する。
/// 統合リグレッション（GetOpen+CSA 1 Play）は <see cref="GoapCombinedSupportRegressionDebugSetup"/> を使用。
/// #6 AtCorrectLanes は理想レーン上のため SKIP（GetOpen 不要）。
/// 本番選出: _verifyProductionSelection=ON → slot1/2 で GetOpen が選ばれること。
/// </summary>
public class GoapGetOpenDebugSetup : GoapSupportActionVerificationSetup
{
    [Header("GetOpen 検証")]
    [SerializeField] private GoapEnemyPositionDebugPatterns _enemyLayouts;
    [Tooltip("パターン別に敵配置が無い場合のデフォルト")]
    [SerializeField] private GoapEnemyPositionDebugPatterns.LayoutPattern _defaultEnemyLayout =
        GoapEnemyPositionDebugPatterns.LayoutPattern.PressBallOwner;

    protected override string SummaryLogTag => "GOAP_GET_OPEN_SETUP";

    protected override GoapSupportActionUnderTest ActionUnderTest =>
        GoapSupportActionUnderTest.GetOpen;

    protected override IGoapProductionSelectionExpectation ProductionSelectionExpectation =>
        GoapProductionSelectionExpectations.GetOpen;

    protected override IGoapSupportActionRuntimePassCriteria RuntimePassCriteria =>
        GoapSupportActionRuntimePassCriteria.GetOpen;

    protected override string BatchVerificationBanner => "GetOpen batch verification";

    protected override string ProductionSelectionVerificationBanner =>
        "GetOpen production selection verification";

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
