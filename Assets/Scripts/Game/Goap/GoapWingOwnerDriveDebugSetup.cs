using UnityEngine;

/// <summary>
/// 翼保持ドライブ追従検証（#17/#18）。
/// _batchPreset=WingOwnerDrive、_verifyRuntimeFollowDuringBatch=ON、_enableBallOwnerAutoDrive=ON を推奨。
/// 本番候補のまま保持者を前後ドライブさせ、slot0=MTS / 非保持翼=CSA の Retarget 追従を検証する。
/// </summary>
public class GoapWingOwnerDriveDebugSetup : GoapSupportActionVerificationSetup
{
    [Header("翼保持ドライブ検証")]
    [SerializeField] private GoapEnemyPositionDebugPatterns _enemyLayouts;
    [SerializeField] private GoapEnemyPositionDebugPatterns.LayoutPattern _defaultEnemyLayout =
        GoapEnemyPositionDebugPatterns.LayoutPattern.PressBallOwner;

    protected override string SummaryLogTag => "GOAP_WING_DRIVE_SETUP";

    protected override GoapSupportActionUnderTest ActionUnderTest =>
        GoapSupportActionUnderTest.WingOwnerDriveFollow;

    protected override IGoapProductionSelectionExpectation ProductionSelectionExpectation =>
        GoapProductionSelectionExpectations.GetOpen;

    protected override IGoapSupportActionRuntimePassCriteria RuntimePassCriteria =>
        GoapSupportActionRuntimePassCriteria.WingOwnerDrive;

    protected override string BatchVerificationBanner => "Wing owner drive follow verification";

    protected override string ProductionSelectionVerificationBanner =>
        "Wing owner drive follow verification";

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

        _enemyLayouts.ApplyPattern(_defaultEnemyLayout);
        LogLine($"ApplyEnemyLayout({pattern}) layout={_defaultEnemyLayout}");
    }
}
