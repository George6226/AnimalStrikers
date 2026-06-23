using UnityEngine;

/// <summary>
/// CF 保持ドライブ追従検証（#13〜16）。
/// _batchPreset=CfOwnerDrive、_verifyRuntimeFollowDuringBatch=ON、_enableBallOwnerAutoDrive=ON を推奨。
/// 本番候補のまま CF をドライブさせ、両翼の CSA Retarget 追従を検証する。
/// </summary>
public class GoapCfOwnerDriveDebugSetup : GoapSupportActionVerificationSetup
{
    [Header("CF 保持ドライブ検証")]
    [SerializeField] private GoapEnemyPositionDebugPatterns _enemyLayouts;
    [SerializeField] private GoapEnemyPositionDebugPatterns.LayoutPattern _defaultEnemyLayout =
        GoapEnemyPositionDebugPatterns.LayoutPattern.PressBallOwner;

    protected override string SummaryLogTag => "GOAP_CF_DRIVE_SETUP";

    protected override GoapSupportActionUnderTest ActionUnderTest =>
        GoapSupportActionUnderTest.CfOwnerDriveFollow;

    protected override IGoapProductionSelectionExpectation ProductionSelectionExpectation =>
        GoapProductionSelectionExpectations.CreateSupportAngle;

    protected override IGoapSupportActionRuntimePassCriteria RuntimePassCriteria =>
        GoapSupportActionRuntimePassCriteria.CfOwnerDrive;

    protected override string BatchVerificationBanner => "CF owner drive follow verification";

    protected override string ProductionSelectionVerificationBanner =>
        "CF owner drive follow verification";

    protected override void ApplyCompanionVerificationState(GoapSupportLayoutPatternId pattern)
    {
        if (_enemyLayouts == null)
        {
            _enemyLayouts = FindFirstObjectByType<GoapEnemyPositionDebugPatterns>();
        }

        if (_enemyLayouts == null)
        {
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
            GoapSupportLayoutPatternId.CfOwner_NearCorrectLanes_DriveForward
                or GoapSupportLayoutPatternId.CfOwner_NearCorrectLanes
                or GoapSupportLayoutPatternId.CfOwner_AtCorrectLanes_DriveLateralRight
                => GoapEnemyPositionDebugPatterns.LayoutPattern.BlockPassLane,
            GoapSupportLayoutPatternId.CfOwner_AtCorrectLanes_DriveForward
                or GoapSupportLayoutPatternId.CfOwner_AtCorrectLanes_DriveForwardBack
                or GoapSupportLayoutPatternId.CfOwner_AtCorrectLanes
                => GoapEnemyPositionDebugPatterns.LayoutPattern.BlockPassLane,
            _ => _defaultEnemyLayout,
        };
    }
}
