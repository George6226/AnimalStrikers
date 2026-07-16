#if UNITY_EDITOR
using NUnit.Framework;

public sealed class GoalKickSetPieceRulesEditModeTests
{
    [Test]
    public void ShouldEvaluate_RequiresMatchPlayAndFreeAndCooldown()
    {
        Assert.That(
            GoalKickSetPieceRules.ShouldEvaluate(
                isMatchPlayActive: false,
                ballExists: true,
                ballState: BallManager_State.BALL_STATE.FREE,
                now: 10f,
                cooldownUntil: 0f),
            Is.False);

        Assert.That(
            GoalKickSetPieceRules.ShouldEvaluate(
                isMatchPlayActive: true,
                ballExists: false,
                ballState: BallManager_State.BALL_STATE.FREE,
                now: 10f,
                cooldownUntil: 0f),
            Is.False);

        Assert.That(
            GoalKickSetPieceRules.ShouldEvaluate(
                isMatchPlayActive: true,
                ballExists: true,
                ballState: BallManager_State.BALL_STATE.HOLD,
                now: 10f,
                cooldownUntil: 0f),
            Is.False);

        Assert.That(
            GoalKickSetPieceRules.ShouldEvaluate(
                isMatchPlayActive: true,
                ballExists: true,
                ballState: BallManager_State.BALL_STATE.FREE,
                now: 5f,
                cooldownUntil: 8f),
            Is.False);

        Assert.That(
            GoalKickSetPieceRules.ShouldEvaluate(
                isMatchPlayActive: true,
                ballExists: true,
                ballState: BallManager_State.BALL_STATE.FREE,
                now: 10f,
                cooldownUntil: 8f),
            Is.True);
    }

    [Test]
    public void IsGoalKickCandidate_RequiresGoalKickWithRestartTeam()
    {
        Assert.That(
            GoalKickSetPieceRules.IsGoalKickCandidate(OutOfPlayClassifier.Result.InPlay),
            Is.False);

        var throwIn = new OutOfPlayClassifier.Result(
            SetPieceKind.ThrowIn,
            sideSignX: 1f,
            endSignZ: 0f,
            hasRestartTeam: true,
            restartTeamIsOther: false);
        Assert.That(GoalKickSetPieceRules.IsGoalKickCandidate(throwIn), Is.False);

        var goalKickNoTeam = new OutOfPlayClassifier.Result(
            SetPieceKind.GoalKick,
            sideSignX: 0f,
            endSignZ: -1f,
            hasRestartTeam: false,
            restartTeamIsOther: false);
        Assert.That(GoalKickSetPieceRules.IsGoalKickCandidate(goalKickNoTeam), Is.False);

        var goalKick = new OutOfPlayClassifier.Result(
            SetPieceKind.GoalKick,
            sideSignX: 0f,
            endSignZ: 1f,
            hasRestartTeam: true,
            restartTeamIsOther: true);
        Assert.That(GoalKickSetPieceRules.IsGoalKickCandidate(goalKick), Is.True);
    }

    [Test]
    public void ResolveHomeDepth_UsesTeamGkSpawnDepth()
    {
        Assert.That(
            GoalKickSetPieceRules.ResolveHomeDepth(restartTeamIsOther: false),
            Is.EqualTo(ConstData.GK_SPAWN_DEPTH_ALLY));
        Assert.That(
            GoalKickSetPieceRules.ResolveHomeDepth(restartTeamIsOther: true),
            Is.EqualTo(ConstData.GK_SPAWN_DEPTH_ENEMY));
    }
}
#endif
