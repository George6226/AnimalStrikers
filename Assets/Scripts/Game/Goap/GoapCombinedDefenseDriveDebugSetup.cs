using UnityEngine;

/// <summary>
/// Phase 6 守備ドライブ: 敵保持者前後ドライブ中の MoveToDefensivePosition 選出 + Retarget 追従。
/// _batchPatternIndexStart/End = #7〜#8（2パターン）。
/// </summary>
public class GoapCombinedDefenseDriveDebugSetup : GoapDefenseActionVerificationSetup
{
    [Header("敵配置")]
    [SerializeField] private GoapEnemyPositionDebugPatterns _enemyLayouts;
    [SerializeField] private GoapEnemyPositionDebugPatterns.LayoutPattern _defaultEnemyLayout =
        GoapEnemyPositionDebugPatterns.LayoutPattern.PressBallOwner;

    protected override string SummaryLogTag => "GOAP_DEFENSE_DRIVE_SETUP";

    protected override GoapDefenseActionUnderTest ActionUnderTest =>
        GoapDefenseActionUnderTest.MoveToDefensivePosition;

    protected override IGoapDefenseProductionSelectionExpectation ProductionSelectionExpectation =>
        GoapDefenseProductionSelectionExpectations.DefenseDrive;

    protected override IGoapDefenseProductionSelectionExpectation DriveProductionSelectionExpectation =>
        GoapDefenseProductionSelectionExpectations.DefenseDrive;

    protected override IGoapDefenseActionRuntimePassCriteria RuntimePassCriteria =>
        GoapDefenseActionRuntimePassCriteria.EnemyOwnerDrive;

    protected override string BatchVerificationBanner => "Defense enemy-owner drive follow verification";

    protected override string ProductionSelectionVerificationBanner =>
        "Defense enemy-owner drive follow verification";

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
