using UnityEngine;

/// <summary>MoveToDefensivePosition 単体検証セットアップ。</summary>
public class GoapMoveToDefensivePositionDebugSetup : GoapDefenseActionVerificationSetup
{
    protected override string SummaryLogTag => "GOAP_MOVE_TO_DEFENSIVE_SETUP";

    protected override GoapDefenseActionUnderTest ActionUnderTest =>
        GoapDefenseActionUnderTest.MoveToDefensivePosition;

    protected override IGoapDefenseProductionSelectionExpectation ProductionSelectionExpectation =>
        GoapDefenseProductionSelectionExpectations.MoveToDefensivePosition;

    protected override string BatchVerificationBanner => "MoveToDefensivePosition batch verification";

    protected override string ProductionSelectionVerificationBanner =>
        "MoveToDefensivePosition production selection verification";
}
