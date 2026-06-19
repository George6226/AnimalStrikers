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
