using UnityEngine;

/// <summary>
/// CreateSupportAngle 単体検証用セットアップ。
/// 回帰本番選出: _batchPreset=CsaRegression（#6,8,9）または統合は GoapCombinedSupportRegressionDebugSetup + CombinedSupportRegression（#2〜12）。
/// 単体検証: _verificationOnlyCreateSupportAngle=ON + CfOwnerStatic でランタイム動作を確認。
/// 共通ロジックは <see cref="GoapSupportActionVerificationSetup"/> を参照。
/// </summary>
public class GoapCreateSupportAngleDebugSetup : GoapSupportActionVerificationSetup
{
    protected override string SummaryLogTag => "GOAP_SUPPORT_ANGLE_SETUP";

    protected override GoapSupportActionUnderTest ActionUnderTest =>
        GoapSupportActionUnderTest.CreateSupportAngle;

    protected override IGoapProductionSelectionExpectation ProductionSelectionExpectation =>
        GoapProductionSelectionExpectations.CreateSupportAngle;

    protected override string BatchVerificationBanner => "CreateSupportAngle batch verification";

    protected override string ProductionSelectionVerificationBanner =>
        "CreateSupportAngle production selection verification";

    protected override IGoapSupportActionRuntimePassCriteria RuntimePassCriteria =>
        GoapSupportActionRuntimePassCriteria.CreateSupportAngle;
}
