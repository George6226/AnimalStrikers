using UnityEngine;

/// <summary>
/// MoveToSupportPosition 単体検証用セットアップ。
/// CF 保持パターンでは slot0=保持者のため評価対象外。
/// RwOwner / LwOwner パターン（#8/#9）で slot0 の本番選出・単体動作を検証する。
/// ドライブ追従は #17/#18（_batchPreset=WingOwnerDrive 推奨）。
/// </summary>
public class GoapMoveToSupportPositionDebugSetup : GoapSupportActionVerificationSetup
{
    protected override string SummaryLogTag => "GOAP_MOVE_TO_SUPPORT_SETUP";

    protected override GoapSupportActionUnderTest ActionUnderTest =>
        GoapSupportActionUnderTest.MoveToSupportPosition;

    protected override IGoapProductionSelectionExpectation ProductionSelectionExpectation =>
        GoapProductionSelectionExpectations.MoveToSupportPosition;

    protected override IGoapSupportActionRuntimePassCriteria RuntimePassCriteria =>
        GoapSupportActionRuntimePassCriteria.MoveToSupportPosition;

    protected override string BatchVerificationBanner => "MoveToSupportPosition batch verification";

    protected override string ProductionSelectionVerificationBanner =>
        "MoveToSupportPosition production selection verification";
}
