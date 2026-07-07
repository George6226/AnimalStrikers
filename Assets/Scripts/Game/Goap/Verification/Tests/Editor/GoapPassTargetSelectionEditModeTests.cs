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

    [Test]
    public void ScoreCandidate_UnderPressure_PrefersShortForwardReceiver()
    {
        Vector3 passer = Vector3.zero;
        Vector3 goal = new Vector3(0f, 0f, 100f);
        Vector3 shortForward = new Vector3(0f, 0f, 18f);
        Vector3 longLateral = new Vector3(30f, 0f, -5f);

        var shortInput = BaseInput(passer, shortForward, goal);
        shortInput.OwnerPressureCount = 2;
        var longInput = BaseInput(passer, longLateral, goal);
        longInput.OwnerPressureCount = 2;

        float shortScore = GoapPassTargetSelection.ScoreCandidate(shortInput);
        float longScore = GoapPassTargetSelection.ScoreCandidate(longInput);

        Assert.That(shortScore, Is.GreaterThan(longScore));
    }

    [Test]
    public void ScoreCandidate_UnderHeavyPressure_StillScoresBlockedRoute()
    {
        Vector3 passer = Vector3.zero;
        Vector3 goal = new Vector3(0f, 0f, 100f);
        Vector3 blockedReceiver = new Vector3(10f, 0f, 25f);
        var blocker = new List<Vector3> { new Vector3(5f, 0f, 12f) };

        var input = BaseInput(passer, blockedReceiver, goal, blocker);
        input.OwnerPressureCount = 2;

        float score = GoapPassTargetSelection.ScoreCandidate(input);

        Assert.That(score, Is.GreaterThan(float.MinValue));
    }

    [Test]
    public void ScoreCandidate_PenalizesMovingReceiver()
    {
        Vector3 passer = Vector3.zero;
        Vector3 receiver = new Vector3(0f, 0f, 28f);
        Vector3 goal = new Vector3(0f, 0f, 100f);

        var stationary = BaseInput(passer, receiver, goal);
        var moving = BaseInput(passer, receiver, goal);
        moving.ReceiverIsMoving = true;

        float stationaryScore = GoapPassTargetSelection.ScoreCandidate(stationary);
        float movingScore = GoapPassTargetSelection.ScoreCandidate(moving);

        Assert.That(stationaryScore, Is.GreaterThan(movingScore));
    }

    [Test]
    public void IsSameTeamFieldReceiver_RejectsEnemyFieldNpcForHumanPasser()
    {
        var passerGo = new GameObject("passer");
        var passerAssignment = passerGo.AddComponent<AnimalControlAssignment>();
        passerGo.AddComponent<AnimalFacade>();
        passerAssignment.SetRole(AnimalControlRole.Human);

        var enemyGo = new GameObject("enemy");
        var enemyAssignment = enemyGo.AddComponent<AnimalControlAssignment>();
        enemyGo.AddComponent<AnimalFacade>();
        enemyAssignment.SetRole(AnimalControlRole.EnemyFieldNpc);

        var allyGo = new GameObject("ally");
        var allyAssignment = allyGo.AddComponent<AnimalControlAssignment>();
        allyGo.AddComponent<AnimalFacade>();
        allyAssignment.SetRole(AnimalControlRole.TeammateNpc);

        try
        {
            Assert.That(GoapPassTargetSelection.IsSameTeamFieldReceiver(
                passerGo.GetComponent<AnimalFacade>(),
                enemyGo.GetComponent<AnimalFacade>()), Is.False);
            Assert.That(GoapPassTargetSelection.IsSameTeamFieldReceiver(
                passerGo.GetComponent<AnimalFacade>(),
                allyGo.GetComponent<AnimalFacade>()), Is.True);
        }
        finally
        {
            Object.DestroyImmediate(passerGo);
            Object.DestroyImmediate(enemyGo);
            Object.DestroyImmediate(allyGo);
        }
    }

    [Test]
    public void IsFieldPassReceiver_RejectsHumanEvenWhenRegisteredAsAlly()
    {
        var passerGo = new GameObject("passer");
        passerGo.AddComponent<AnimalControlAssignment>().SetRole(AnimalControlRole.Human);
        passerGo.AddComponent<AnimalFacade>();

        var humanAllyGo = new GameObject("human_ally");
        humanAllyGo.AddComponent<AnimalControlAssignment>().SetRole(AnimalControlRole.Human);
        humanAllyGo.AddComponent<AnimalFacade>();

        var npcAllyGo = new GameObject("npc_ally");
        npcAllyGo.AddComponent<AnimalControlAssignment>().SetRole(AnimalControlRole.TeammateNpc);
        npcAllyGo.AddComponent<AnimalFacade>();

        try
        {
            var passer = passerGo.GetComponent<AnimalFacade>();
            Assert.That(GoapPassTargetSelection.IsFieldPassReceiver(passer, humanAllyGo.GetComponent<AnimalFacade>()),
                Is.False);
            Assert.That(GoapPassTargetSelection.IsFieldPassReceiver(passer, npcAllyGo.GetComponent<AnimalFacade>()),
                Is.True);
        }
        finally
        {
            Object.DestroyImmediate(passerGo);
            Object.DestroyImmediate(humanAllyGo);
            Object.DestroyImmediate(npcAllyGo);
        }
    }
}
#endif
