#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

public sealed class GoapPassTargetSelectionEditModeTests
{
    private static GoapPassTargetSelection.CandidateScoreInput BaseInput(
        Vector3 passer,
        Vector3 receiver,
        Vector3 goal,
        List<Vector3> enemies = null)
    {
        return new GoapPassTargetSelection.CandidateScoreInput
        {
            PasserPosition = passer,
            PasserFacingYDegrees = 0f,
            ReceiverPosition = receiver,
            AttackGoalPosition = goal,
            EnemyPositions = enemies ?? new List<Vector3>(),
            FieldLength = 100f,
            OwnerPressureCount = 0,
        };
    }

    [Test]
    public void ScoreCandidate_PrefersClearRouteOverBlocked()
    {
        Vector3 passer = Vector3.zero;
        Vector3 goal = new Vector3(0f, 0f, 100f);
        Vector3 clearReceiver = new Vector3(0f, 0f, 25f);
        Vector3 blockedReceiver = new Vector3(10f, 0f, 25f);
        var blocker = new List<Vector3> { new Vector3(5f, 0f, 12f) };

        float clearScore = GoapPassTargetSelection.ScoreCandidate(
            BaseInput(passer, clearReceiver, goal));
        float blockedScore = GoapPassTargetSelection.ScoreCandidate(
            BaseInput(passer, blockedReceiver, goal, blocker));

        Assert.That(clearScore, Is.GreaterThan(blockedScore));
    }

    [Test]
    public void ScoreCandidate_PrefersForwardReceiverWhenBothRoutesClear()
    {
        Vector3 passer = Vector3.zero;
        Vector3 goal = new Vector3(0f, 0f, 100f);
        Vector3 forwardReceiver = new Vector3(0f, 0f, 30f);
        Vector3 backwardReceiver = new Vector3(0f, 0f, -20f);

        float forwardScore = GoapPassTargetSelection.ScoreCandidate(
            BaseInput(passer, forwardReceiver, goal));
        float backwardScore = GoapPassTargetSelection.ScoreCandidate(
            BaseInput(passer, backwardReceiver, goal));

        Assert.That(forwardScore, Is.GreaterThan(backwardScore));
    }

    [Test]
    public void ScoreCandidate_PrefersFacingConeReceiver()
    {
        Vector3 passer = Vector3.zero;
        Vector3 goal = new Vector3(0f, 0f, 100f);
        Vector3 inCone = new Vector3(0f, 0f, 28f);
        Vector3 wideAngle = new Vector3(40f, 0f, 10f);

        float inConeScore = GoapPassTargetSelection.ScoreCandidate(
            BaseInput(passer, inCone, goal));
        float wideScore = GoapPassTargetSelection.ScoreCandidate(
            BaseInput(passer, wideAngle, goal));

        Assert.That(inConeScore, Is.GreaterThan(wideScore));
    }

    [Test]
    public void ScoreCandidate_IsDeterministicForSameInput()
    {
        var input = BaseInput(Vector3.zero, new Vector3(5f, 0f, 28f), new Vector3(0f, 0f, 100f));
        float first = GoapPassTargetSelection.ScoreCandidate(input);
        float second = GoapPassTargetSelection.ScoreCandidate(input);
        Assert.That(second, Is.EqualTo(first));
    }

    [Test]
    public void ComputeFacingAngleDiff_UsesPasserFacing()
    {
        Vector3 origin = Vector3.zero;
        Vector3 straightAhead = new Vector3(0f, 0f, 10f);
        float diff = GoapPassTargetSelection.ComputeFacingAngleDiff(origin, straightAhead, 0f);
        Assert.That(diff, Is.LessThanOrEqualTo(1f));
    }
}
#endif
