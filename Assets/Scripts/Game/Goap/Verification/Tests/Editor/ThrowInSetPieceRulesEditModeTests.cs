#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public sealed class ThrowInSetPieceRulesEditModeTests
{
    [Test]
    public void ShouldEvaluate_DelegatesToGoalKickRules()
    {
        Assert.That(
            ThrowInSetPieceRules.ShouldEvaluate(
                isMatchPlayActive: true,
                ballExists: true,
                ballState: BallManager_State.BALL_STATE.FREE,
                now: 10f,
                cooldownUntil: 0f),
            Is.True);
        Assert.That(
            ThrowInSetPieceRules.ShouldEvaluate(
                isMatchPlayActive: true,
                ballExists: true,
                ballState: BallManager_State.BALL_STATE.HOLD,
                now: 10f,
                cooldownUntil: 0f),
            Is.False);
    }

    [Test]
    public void IsThrowInCandidate_RequiresThrowInWithRestartTeam()
    {
        Assert.That(
            ThrowInSetPieceRules.IsThrowInCandidate(OutOfPlayClassifier.Result.InPlay),
            Is.False);

        var goalKick = new OutOfPlayClassifier.Result(
            SetPieceKind.GoalKick,
            sideSignX: 0f,
            endSignZ: 1f,
            hasRestartTeam: true,
            restartTeamIsOther: true);
        Assert.That(ThrowInSetPieceRules.IsThrowInCandidate(goalKick), Is.False);

        var throwInNoTeam = new OutOfPlayClassifier.Result(
            SetPieceKind.ThrowIn,
            sideSignX: 1f,
            endSignZ: 0f,
            hasRestartTeam: false,
            restartTeamIsOther: false);
        Assert.That(ThrowInSetPieceRules.IsThrowInCandidate(throwInNoTeam), Is.False);

        var throwIn = new OutOfPlayClassifier.Result(
            SetPieceKind.ThrowIn,
            sideSignX: -1f,
            endSignZ: 0f,
            hasRestartTeam: true,
            restartTeamIsOther: true);
        Assert.That(ThrowInSetPieceRules.IsThrowInCandidate(throwIn), Is.True);
    }

    [Test]
    public void ResolveLastTouchByOtherTeam_MapsBelongTeam()
    {
        Assert.That(
            ThrowInSetPieceRules.ResolveLastTouchByOtherTeam(BallManager_State.BELONG_TEAM.PLAYER),
            Is.EqualTo(false));
        Assert.That(
            ThrowInSetPieceRules.ResolveLastTouchByOtherTeam(BallManager_State.BELONG_TEAM.ENEMY),
            Is.EqualTo(true));
        Assert.That(
            ThrowInSetPieceRules.ResolveLastTouchByOtherTeam(BallManager_State.BELONG_TEAM.FREE),
            Is.Null);
    }

    [Test]
    public void ResolveThrowInBallPosition_OnSidelineClampedZ()
    {
        var field = new TeamFieldInfo();
        field.Initialize(ConstData.FIELD_SIZE_Z, ConstData.FIELD_SIZE_X);

        Vector3 pos = SetPieceAssignmentRules.ResolveThrowInBallPosition(
            field,
            sideSignX: 1f,
            ballWorldZ: 50f);

        Assert.That(pos.x, Is.EqualTo(field.FieldCenter.x + field.FieldWidth * 0.5f).Within(0.01f));
        Assert.That(pos.z, Is.EqualTo(field.FieldCenter.z + field.FieldLength * 0.5f).Within(0.01f));
        Assert.That(pos.y, Is.EqualTo(0.5f).Within(0.01f));
    }
}
#endif
