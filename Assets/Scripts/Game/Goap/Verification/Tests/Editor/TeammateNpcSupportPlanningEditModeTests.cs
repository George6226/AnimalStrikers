using NUnit.Framework;
using UnityEngine;

/// <summary>
/// サポート幾何判定の EditMode 回帰。Play なしで preferCSA / NeedsMovement / GetOpen ブロック境界を固定する。
/// </summary>
public sealed class TeammateNpcSupportPlanningEditModeTests
{
    private GoapSupportPlanningTestFixture _fixture;

    [SetUp]
    public void SetUp()
    {
        _fixture = new GoapSupportPlanningTestFixture();
    }

    [TearDown]
    public void TearDown()
    {
        _fixture?.Dispose();
        _fixture = null;
    }

    [Test]
    public void Pattern2_Clustered_WingsPreferGetOpen_AndNeedMovement()
    {
        _fixture.ApplyPattern(GoapSupportLayoutPatternId.CfOwner_Clustered);

        AssertWingExpectations(
            preferCreateSupportAngle: false,
            needsMovement: true,
            label: "#2 Clustered");
    }

    [Test]
    public void Pattern3_RwWrongSide_WingsPreferGetOpen_AndNeedMovement()
    {
        _fixture.ApplyPattern(GoapSupportLayoutPatternId.CfOwner_RwWrongSide);

        AssertWingExpectations(
            preferCreateSupportAngle: false,
            needsMovement: true,
            label: "#3 RwWrongSide");
    }

    [Test]
    public void Pattern4_LwOnWrongSide_WingsPreferGetOpen_AndNeedMovement()
    {
        _fixture.ApplyPattern(GoapSupportLayoutPatternId.CfOwner_LwOnWrongSide);

        AssertWingExpectations(
            preferCreateSupportAngle: false,
            needsMovement: true,
            label: "#4 LwOnWrongSide");
    }

    [Test]
    public void Pattern5_NearCorrectLanes_WingsPreferGetOpen_AndNeedMovement()
    {
        _fixture.ApplyPattern(GoapSupportLayoutPatternId.CfOwner_NearCorrectLanes);

        AssertWingExpectations(
            preferCreateSupportAngle: false,
            needsMovement: true,
            label: "#5 NearCorrectLanes");
    }

    [Test]
    public void Pattern6_AtCorrectLanes_WingsPreferCreateSupportAngle_AndNeedMovement()
    {
        _fixture.ApplyPattern(GoapSupportLayoutPatternId.CfOwner_AtCorrectLanes);

        AssertWingExpectations(
            preferCreateSupportAngle: true,
            needsMovement: true,
            label: "#6 AtCorrectLanes");
    }

    [Test]
    public void Pattern7_AllOverlapped_WingsPreferGetOpen_AndNeedMovement()
    {
        _fixture.ApplyPattern(GoapSupportLayoutPatternId.CfOwner_AllOverlapped);

        AssertWingExpectations(
            preferCreateSupportAngle: false,
            needsMovement: true,
            label: "#7 AllOverlapped");
    }

    [Test]
    public void Pattern10_OnRightWing_WingsPreferGetOpen_AndNeedMovement()
    {
        _fixture.ApplyPattern(GoapSupportLayoutPatternId.CfOwner_OnRightWing);

        AssertWingExpectations(
            preferCreateSupportAngle: false,
            needsMovement: true,
            label: "#10 OnRightWing");
    }

    [Test]
    public void Pattern11_OnLeftWing_WingsPreferGetOpen_AndNeedMovement()
    {
        _fixture.ApplyPattern(GoapSupportLayoutPatternId.CfOwner_OnLeftWing);

        AssertWingExpectations(
            preferCreateSupportAngle: false,
            needsMovement: true,
            label: "#11 OnLeftWing");
    }

    [Test]
    public void Pattern12_WingsTooDeepBehind_WingsPreferGetOpen_AndNeedMovement()
    {
        _fixture.ApplyPattern(GoapSupportLayoutPatternId.CfOwner_WingsTooDeepBehind);

        AssertWingExpectations(
            preferCreateSupportAngle: false,
            needsMovement: true,
            label: "#12 WingsTooDeepBehind");
    }

    [Test]
    public void Pattern8_RwOwnerHold_Slot0BlocksGetOpen()
    {
        _fixture.ApplyPattern(GoapSupportLayoutPatternId.RwOwner_WingHold);

        PlayerBlackboard slot0 = _fixture.GetBlackboard(0);
        Assert.IsTrue(
            TeammateNpcSupportPlanning.BlocksGetOpenForCentralSupportWhenWingOwnerHolds(slot0),
            "#8 slot0 should block GetOpen when RW holds");
        Assert.IsFalse(
            TeammateNpcSupportPlanning.BlocksGetOpenForCentralSupportWhenWingOwnerHolds(_fixture.GetBlackboard(1)),
            "#8 slot1 (ball owner) should not block GetOpen");
    }

    [Test]
    public void Pattern9_LwOwnerHold_Slot0BlocksGetOpen()
    {
        _fixture.ApplyPattern(GoapSupportLayoutPatternId.LwOwner_WingHold);

        PlayerBlackboard slot0 = _fixture.GetBlackboard(0);
        Assert.IsTrue(
            TeammateNpcSupportPlanning.BlocksGetOpenForCentralSupportWhenWingOwnerHolds(slot0),
            "#9 slot0 should block GetOpen when LW holds");
        Assert.IsFalse(
            TeammateNpcSupportPlanning.BlocksGetOpenForCentralSupportWhenWingOwnerHolds(_fixture.GetBlackboard(2)),
            "#9 slot2 (ball owner) should not block GetOpen");
    }

    [Test]
    public void MeasureForwardOffsetFromOwner_UsesBallOwnerAnchor()
    {
        Vector3 ownerPos = new Vector3(0f, 0f, 4f);
        _fixture.ConfigureTeamBallAttack(0, ownerPos);

        PlayerBlackboard wing = _fixture.GetBlackboard(1);
        Vector3 ahead = ownerPos + Vector3.forward * 0.5f;
        _fixture.SetSlotPosition(1, ahead);

        float offset = TeammateNpcSupportPlanning.MeasureForwardOffsetFromOwner(wing);
        Assert.AreEqual(0.5f, offset, 0.001f);
    }

    [Test]
    public void IsWingNearOwnerForwardPlane_RespectsAheadAndBehindThresholds()
    {
        float fieldLength = _fixture.TeamBlackboard.FieldInfo.FieldLength;
        float maxAhead = fieldLength * 0.025f;
        float maxBehind = fieldLength * 0.02f;

        Vector3 ownerPos = Vector3.zero;
        _fixture.ConfigureTeamBallAttack(0, ownerPos);

        PlayerBlackboard wing = _fixture.GetBlackboard(1);
        _fixture.SetSlotPosition(1, ownerPos + Vector3.forward * (maxAhead - 0.05f));
        Assert.IsTrue(
            TeammateNpcSupportPlanning.IsWingNearOwnerForwardPlane(wing, _fixture.TeamBlackboard),
            "slightly inside ahead threshold should be near owner plane");

        _fixture.SetSlotPosition(1, ownerPos + Vector3.forward * (maxAhead + 0.05f));
        Assert.IsFalse(
            TeammateNpcSupportPlanning.IsWingNearOwnerForwardPlane(wing, _fixture.TeamBlackboard),
            "beyond ahead threshold should not be near owner plane");

        _fixture.SetSlotPosition(1, ownerPos - Vector3.forward * (maxBehind - 0.05f));
        Assert.IsTrue(
            TeammateNpcSupportPlanning.IsWingNearOwnerForwardPlane(wing, _fixture.TeamBlackboard),
            "slightly inside behind threshold should be near owner plane");

        _fixture.SetSlotPosition(1, ownerPos - Vector3.forward * (maxBehind + 0.05f));
        Assert.IsFalse(
            TeammateNpcSupportPlanning.IsWingNearOwnerForwardPlane(wing, _fixture.TeamBlackboard),
            "beyond behind threshold should not be near owner plane");
    }

    [Test]
    public void Pattern6_WingsAreOnAssignedWideLaneLaterally()
    {
        _fixture.ApplyPattern(GoapSupportLayoutPatternId.CfOwner_AtCorrectLanes);

        foreach (int slot in new[] { 1, 2 })
        {
            Assert.IsTrue(
                TeammateNpcSupportPlanning.IsOnAssignedWideLaneLaterally(_fixture.GetBlackboard(slot)),
                $"#6 slot{slot} should be on assigned wide lane laterally");
        }
    }

    [Test]
    public void Pattern2_Clustered_WingsAreNotOnIdealWideLane()
    {
        _fixture.ApplyPattern(GoapSupportLayoutPatternId.CfOwner_Clustered);

        foreach (int slot in new[] { 1, 2 })
        {
            Assert.IsFalse(
                TeammateNpcSupportPlanning.IsOnIdealWideLaneForCreateSupportAngle(_fixture.GetBlackboard(slot)),
                $"#2 slot{slot} should not be on ideal wide lane (clustered near owner)");
        }
    }

    [Test]
    public void Pattern3_WrongSideWings_AreNotOnAssignedWideLaneLaterally()
    {
        _fixture.ApplyPattern(GoapSupportLayoutPatternId.CfOwner_RwWrongSide);

        foreach (int slot in new[] { 1, 2 })
        {
            Assert.IsFalse(
                TeammateNpcSupportPlanning.IsOnAssignedWideLaneLaterally(_fixture.GetBlackboard(slot)),
                $"#3 slot{slot} should not be on assigned wide lane (wrong side)");
        }
    }

    [Test]
    public void Pattern4_WrongSideWings_AreNotOnAssignedWideLaneLaterally()
    {
        _fixture.ApplyPattern(GoapSupportLayoutPatternId.CfOwner_LwOnWrongSide);

        foreach (int slot in new[] { 1, 2 })
        {
            Assert.IsFalse(
                TeammateNpcSupportPlanning.IsOnAssignedWideLaneLaterally(_fixture.GetBlackboard(slot)),
                $"#4 slot{slot} should not be on assigned wide lane (wrong side)");
        }
    }

    [Test]
    public void Pattern7_AllOverlapped_WingsAreNotOnIdealWideLane()
    {
        _fixture.ApplyPattern(GoapSupportLayoutPatternId.CfOwner_AllOverlapped);

        foreach (int slot in new[] { 1, 2 })
        {
            Assert.IsFalse(
                TeammateNpcSupportPlanning.IsOnIdealWideLaneForCreateSupportAngle(_fixture.GetBlackboard(slot)),
                $"#7 slot{slot} should not be on ideal wide lane (overlapped near owner)");
        }
    }

    [Test]
    public void Pattern7_AllOverlapped_WingsAreNotOnAssignedWideLaneLaterally()
    {
        _fixture.ApplyPattern(GoapSupportLayoutPatternId.CfOwner_AllOverlapped);

        foreach (int slot in new[] { 1, 2 })
        {
            Assert.IsFalse(
                TeammateNpcSupportPlanning.IsOnAssignedWideLaneLaterally(_fixture.GetBlackboard(slot)),
                $"#7 slot{slot} should not be on assigned wide lane (overlapped near owner)");
        }
    }

    [Test]
    public void Pattern10_OnRightWing_OwnerIsGeometricallyOnRightSide()
    {
        _fixture.ApplyPattern(GoapSupportLayoutPatternId.CfOwner_OnRightWing);

        float wingEnter = _fixture.TeamBlackboard.FieldInfo.FieldWidth * 0.12f;
        Assert.Greater(
            MeasureOwnerLateralFromFieldCenter(),
            wingEnter,
            "#10 owner should be shifted past right wing-enter threshold");
    }

    [Test]
    public void Pattern10_OnRightWing_WingsAreNotOnIdealWideLane()
    {
        _fixture.ApplyPattern(GoapSupportLayoutPatternId.CfOwner_OnRightWing);

        foreach (int slot in new[] { 1, 2 })
        {
            Assert.IsFalse(
                TeammateNpcSupportPlanning.IsOnIdealWideLaneForCreateSupportAngle(_fixture.GetBlackboard(slot)),
                $"#10 slot{slot} should not be on ideal wide lane (CF on right wing)");
        }
    }

    [Test]
    public void Pattern11_OnLeftWing_OwnerIsGeometricallyOnLeftSide()
    {
        _fixture.ApplyPattern(GoapSupportLayoutPatternId.CfOwner_OnLeftWing);

        float wingEnter = _fixture.TeamBlackboard.FieldInfo.FieldWidth * 0.12f;
        Assert.Less(
            MeasureOwnerLateralFromFieldCenter(),
            -wingEnter,
            "#11 owner should be shifted past left wing-enter threshold");
    }

    [Test]
    public void Pattern11_OnLeftWing_WingsAreNotOnIdealWideLane()
    {
        _fixture.ApplyPattern(GoapSupportLayoutPatternId.CfOwner_OnLeftWing);

        foreach (int slot in new[] { 1, 2 })
        {
            Assert.IsFalse(
                TeammateNpcSupportPlanning.IsOnIdealWideLaneForCreateSupportAngle(_fixture.GetBlackboard(slot)),
                $"#11 slot{slot} should not be on ideal wide lane (CF on left wing)");
        }
    }

    [Test]
    public void Pattern5_WingsAreNotOnIdealWideLane()
    {
        _fixture.ApplyPattern(GoapSupportLayoutPatternId.CfOwner_NearCorrectLanes);

        foreach (int slot in new[] { 1, 2 })
        {
            PlayerBlackboard bb = _fixture.GetBlackboard(slot);
            Assert.IsFalse(
                TeammateNpcSupportPlanning.IsOnIdealWideLaneForCreateSupportAngle(bb),
                $"#5 slot{slot} should not be on ideal wide lane (behind owner plane)");
        }
    }

    [Test]
    public void CfOwnerHeld_Slot0UsesCentralWidthLayout_AndBlocksCreateSupportAngle()
    {
        _fixture.ApplyPattern(GoapSupportLayoutPatternId.CfOwner_Clustered);

        PlayerBlackboard slot0 = _fixture.GetBlackboard(0);
        Assert.IsTrue(
            TeammateNpcSupportPlanning.ShouldUseWidthLayoutSupportPosition(slot0),
            "slot0 should use central width layout under CF hold");
        Assert.IsTrue(
            TeammateNpcSupportPlanning.BlocksCreateSupportAngleForCentralWidthLayout(slot0),
            "slot0 should block CSA under central width layout");
        Assert.IsFalse(
            TeammateNpcSupportPlanning.BlocksCreateSupportAngleForCentralWidthLayout(_fixture.GetBlackboard(1)),
            "wing slots should not block CSA via central width layout");
    }

    [Test]
    public void CfOwnerHeld_WingsBlockMoveToSupport()
    {
        _fixture.ApplyPattern(GoapSupportLayoutPatternId.CfOwner_Clustered);

        Assert.IsTrue(
            TeammateNpcSupportPlanning.BlocksMoveToSupportForWingLayout(_fixture.GetBlackboard(1)),
            "slot1 should block MoveToSupport on wing layout");
        Assert.IsTrue(
            TeammateNpcSupportPlanning.BlocksMoveToSupportForWingLayout(_fixture.GetBlackboard(2)),
            "slot2 should block MoveToSupport on wing layout");
        Assert.IsFalse(
            TeammateNpcSupportPlanning.BlocksMoveToSupportForWingLayout(_fixture.GetBlackboard(0)),
            "slot0 should not block MoveToSupport on wing layout");
    }

    [Test]
    public void Pattern5_WingsBlockCreateSupportAngleWhenGetOpenPreferred()
    {
        _fixture.ApplyPattern(GoapSupportLayoutPatternId.CfOwner_NearCorrectLanes);

        foreach (int slot in new[] { 1, 2 })
        {
            Assert.IsTrue(
                TeammateNpcSupportPlanning.BlocksCreateSupportAngleWhenGetOpenPreferred(_fixture.GetBlackboard(slot)),
                $"#5 slot{slot} should block CSA when GetOpen is preferred");
        }
    }

    [Test]
    public void Pattern6_WingsDoNotBlockCreateSupportAngleWhenGetOpenPreferred()
    {
        _fixture.ApplyPattern(GoapSupportLayoutPatternId.CfOwner_AtCorrectLanes);

        foreach (int slot in new[] { 1, 2 })
        {
            Assert.IsFalse(
                TeammateNpcSupportPlanning.BlocksCreateSupportAngleWhenGetOpenPreferred(_fixture.GetBlackboard(slot)),
                $"#6 slot{slot} should not block CSA when CSA is preferred");
        }
    }

    private float MeasureOwnerLateralFromFieldCenter()
    {
        TeamBlackboard teamBB = _fixture.TeamBlackboard;
        Vector3 ownerPos = teamBB.BallInfo.BallOwnerPosition;
        var field = teamBB.FieldInfo;
        Vector3 toGoal = (field.EnemyGoalPosition - field.FieldCenter).normalized;
        Vector3 right = Vector3.Cross(Vector3.up, toGoal).normalized;
        return Vector3.Dot(ownerPos - field.FieldCenter, right);
    }

    private void AssertWingExpectations(bool preferCreateSupportAngle, bool needsMovement, string label)
    {
        foreach (int slot in new[] { 1, 2 })
        {
            PlayerBlackboard bb = _fixture.GetBlackboard(slot);
            Assert.AreEqual(
                preferCreateSupportAngle,
                TeammateNpcSupportPlanning.ShouldPreferCreateSupportAngleOverGetOpen(bb),
                $"{label} slot{slot} preferCSA");
            Assert.AreEqual(
                needsMovement,
                TeammateNpcSupportPlanning.NeedsTacticalSupportMovement(bb),
                $"{label} slot{slot} needsMovement");
        }
    }
}
