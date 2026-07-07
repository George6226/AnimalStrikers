#if UNITY_EDITOR
using Game.Goap.Goals;
using NUnit.Framework;
using UnityEngine;

public sealed class IncomingPassPlanningEditModeTests
{
    [TearDown]
    public void TearDown()
    {
        GoapPassFlightTracker.Clear();
        SetStaticField(typeof(TeamFacade), "_instance", null);
    }

    [Test]
    public void IsIncomingPassTarget_TrueForRegisteredTargetDuringTeamAttack()
    {
        var root = CreateTeamFacadeRoot();
        var teamBb = root.GetComponent<TeamBlackboard>();
        teamBb.BallInfo.setExistBall();
        teamBb.BallInfo.updateBallID(1001, BallManager_State.BELONG_TEAM.PLAYER, Vector3.zero);
        teamBb.BallInfo.updateBallState(BallManager_State.BALL_STATE.PASS);

        var passer = CreateFieldNpc(1001, AnimalControlRole.TeammateNpc);
        var target = CreateFieldNpc(1002, AnimalControlRole.TeammateNpc);
        GoapPassFlightTracker.RegisterPass(
            passer.GetComponent<AnimalFacade>(),
            target.GetComponent<AnimalFacade>());

        try
        {
            var targetBb = target.GetComponentInChildren<PlayerBlackboard>();
            var passerBb = passer.GetComponentInChildren<PlayerBlackboard>();
            Assert.That(IncomingPassPlanning.IsIncomingPassTarget(targetBb), Is.True);
            Assert.That(IncomingPassPlanning.IsIncomingPassTarget(passerBb), Is.False);
        }
        finally
        {
            Object.DestroyImmediate(passer);
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void IsIncomingPassTarget_RemainsActiveWhilePasserHoldsDuringWindUp()
    {
        var root = CreateTeamFacadeRoot();
        var teamBb = root.GetComponent<TeamBlackboard>();
        teamBb.BallInfo.setExistBall();
        teamBb.BallInfo.updateBallID(1001, BallManager_State.BELONG_TEAM.PLAYER, Vector3.zero);
        teamBb.BallInfo.updateBallState(BallManager_State.BALL_STATE.HOLD);

        var passer = CreateFieldNpc(1001, AnimalControlRole.TeammateNpc);
        var target = CreateFieldNpc(1002, AnimalControlRole.TeammateNpc);
        GoapPassFlightTracker.RegisterPass(
            passer.GetComponent<AnimalFacade>(),
            target.GetComponent<AnimalFacade>());

        try
        {
            var targetBb = target.GetComponentInChildren<PlayerBlackboard>();
            Assert.That(GoapPassFlightTracker.IsTargetPlayer(1002), Is.True);
            Assert.That(IncomingPassPlanning.IsIncomingPassTarget(targetBb), Is.True);
        }
        finally
        {
            Object.DestroyImmediate(passer);
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void SyncStalePassFlight_ClearsWhenTargetReceivesBall()
    {
        var root = CreateTeamFacadeRoot();
        var teamBb = root.GetComponent<TeamBlackboard>();
        teamBb.BallInfo.setExistBall();
        teamBb.BallInfo.updateBallID(1002, BallManager_State.BELONG_TEAM.PLAYER, Vector3.zero);
        teamBb.BallInfo.updateBallState(BallManager_State.BALL_STATE.HOLD);

        var passer = CreateFieldNpc(1001, AnimalControlRole.TeammateNpc);
        var target = CreateFieldNpc(1002, AnimalControlRole.TeammateNpc);
        GoapPassFlightTracker.RegisterPass(
            passer.GetComponent<AnimalFacade>(),
            target.GetComponent<AnimalFacade>());

        try
        {
            GoapPassFlightTracker.SyncStalePassFlight(teamBb.BallInfo);
            Assert.That(GoapPassFlightTracker.TryGetActivePass(out _), Is.False);
        }
        finally
        {
            Object.DestroyImmediate(passer);
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void IsAnticipatedBallOwner_TrueWhenPassBallIsVeryClose()
    {
        var root = CreateTeamFacadeRoot();
        var teamBb = root.GetComponent<TeamBlackboard>();
        teamBb.BallInfo.setExistBall();
        teamBb.BallInfo.updateBallID(-1, BallManager_State.BELONG_TEAM.PLAYER, Vector3.zero);
        teamBb.BallInfo.updateBallState(BallManager_State.BALL_STATE.PASS);
        teamBb.BallInfo.updateBallPhysics(Vector3.forward * 0.5f, Vector3.forward);

        var passer = CreateFieldNpc(1001, AnimalControlRole.TeammateNpc);
        var target = CreateFieldNpc(1002, AnimalControlRole.TeammateNpc);
        var targetBb = target.GetComponentInChildren<PlayerBlackboard>();
        targetBb.PhysicalState.updatePhysicalInfo(Vector3.zero, Vector3.zero);
        targetBb.BallState.updateBallInfo(false, 0.8f, Vector3.forward);

        GoapPassFlightTracker.RegisterPass(
            passer.GetComponent<AnimalFacade>(),
            target.GetComponent<AnimalFacade>());

        try
        {
            Assert.That(IncomingPassPlanning.IsAnticipatedBallOwner(targetBb), Is.True);
            Assert.That(MainNpcAttackPlanning.IsEffectiveBallOwner(targetBb), Is.True);
        }
        finally
        {
            Object.DestroyImmediate(passer);
            Object.DestroyImmediate(target);
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void NormalizeLists_AddsIncomingPassReceiveGoalAndAction()
    {
        var goals = new System.Collections.Generic.List<GoapGoalSO>();
        var actions = new System.Collections.Generic.List<GoapActionSO>();

        GoapMainNpcCatalog.NormalizeLists(goals, actions);

        Assert.That(goals.Exists(g => g is IncomingPassReceiveGoalSO), Is.True);
        Assert.That(actions.Exists(a => a is MoveToReceivePassActionSO), Is.True);

        var goal = goals.Find(g => g is IncomingPassReceiveGoalSO);
        var filtered = GoapMainNpcCatalog.FilterActionsForGoal(goal, actions);
        Assert.That(filtered, Has.Count.EqualTo(1));
        Assert.That(filtered[0], Is.InstanceOf<MoveToReceivePassActionSO>());
    }

    private static GameObject CreateTeamFacadeRoot()
    {
        var root = new GameObject("teamFacade");
        var teamFacade = root.AddComponent<TeamFacade>();
        var teamBb = root.AddComponent<TeamBlackboard>();
        var regist = root.AddComponent<TeamRegistar>();
        SetPrivateField(teamFacade, "_teamBlackboard", teamBb);
        SetPrivateField(teamFacade, "_teamRegistar", regist);
        SetStaticField(typeof(TeamFacade), "_instance", teamFacade);
        teamBb.FieldInfo.Initialize(40f, 20f);
        teamBb.BallInfo.Initialize();
        return root;
    }

    private static GameObject CreateFieldNpc(int playerId, AnimalControlRole role)
    {
        var go = new GameObject($"npc_{playerId}");
        go.AddComponent<AnimalFacade>();
        go.AddComponent<AnimalControlAssignment>().SetRole(role);
        var bbGo = new GameObject("bb");
        bbGo.transform.SetParent(go.transform, false);
        var bb = bbGo.AddComponent<PlayerBlackboard>();
        bb.BasicData.init(go);
        typeof(PlayerBasicData).GetProperty("PlayerID")!
            .SetValue(bb.BasicData, playerId, null);
        return go;
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(
            fieldName,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        field!.SetValue(target, value);
    }

    private static void SetStaticField(System.Type type, string fieldName, object value)
    {
        var field = type.GetField(
            fieldName,
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        field!.SetValue(null, value);
    }
}
#endif
