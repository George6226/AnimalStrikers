#if UNITY_EDITOR
using System.Reflection;
using Game.Goap.Goals;
using NUnit.Framework;
using Photon.Pun;
using UnityEngine;

public sealed class MainNpcPostPassPlanningEditModeTests
{
    [Test]
    public void GetPlaytestDiagnostic_ReturnsIdleWhenNoContext()
    {
        var diagnostic = MainNpcPostPassPlanning.GetPlaytestDiagnostic(null);

        Assert.That(diagnostic.HasSample, Is.False);
    }

    [Test]
    public void VerifyMainNpcPostPassSupportStarted_DetectsGoalChangeAfterPass()
    {
        const string summary =
            "bootstrap complete\n" +
            "[GOAP_SUMMARY] [Goap#1|owner=Lion(Clone),playerId=1001] ActionStart(action=PassToTeammate, goal=BallPossessionAttack)\n" +
            "[GOAP_SUMMARY] [Goap#1|owner=Lion(Clone),playerId=1001] GoalChanged(goal=TeamBallSupport)\n";

        Assert.That(MainNpcPostPassPlanning.VerifyMainNpcPostPassSupportStarted(summary), Is.True);
    }

    [Test]
    public void VerifyMainNpcPostPassSupportStarted_DetectsSupportActionAfterPass()
    {
        const string summary =
            "bootstrap complete\n" +
            "[GOAP_SUMMARY] [Goap#1|owner=Lion(Clone),playerId=1001] ActionStart(action=PassToTeammate, goal=BallPossessionAttack)\n" +
            "[GOAP_SUMMARY] [Goap#1|owner=Lion(Clone),playerId=1001] ActionStart(action=MoveToSupportPosition, goal=TeamBallSupport)\n";

        Assert.That(MainNpcPostPassPlanning.VerifyMainNpcPostPassSupportStarted(summary), Is.True);
    }

    [Test]
    public void VerifyMainNpcPostPassSupportStarted_IgnoresSubNpcSupportOnly()
    {
        const string summary =
            "bootstrap complete\n" +
            "[GOAP_SUMMARY] [Goap#1|owner=Lion(Clone),playerId=1001] ActionStart(action=PassToTeammate, goal=BallPossessionAttack)\n" +
            "[GOAP_SUMMARY] [Goap#2|owner=Gorilla(Clone),playerId=1002] GoalChanged(goal=TeamBallSupport)\n";

        Assert.That(MainNpcPostPassPlanning.VerifyMainNpcPostPassSupportStarted(summary), Is.False);
    }

    [Test]
    public void VerifyMainNpcPostPassSupportStarted_RequiresPassFirst()
    {
        const string summary =
            "bootstrap complete\n" +
            "[GOAP_SUMMARY] [Goap#1|owner=Lion(Clone),playerId=1001] GoalChanged(goal=TeamBallSupport)\n";

        Assert.That(MainNpcPostPassPlanning.VerifyMainNpcPostPassSupportStarted(summary), Is.False);
    }

    [Test]
    public void PostPassPasser_WithHasBallFactLag_SelectsSupportNotAttack()
    {
        var root = CreateTeamFacadeRoot();
        var teamBb = root.GetComponent<TeamBlackboard>();
        teamBb.BallInfo.setExistBall();
        teamBb.BallInfo.updateBallID(1002, BallManager_State.BELONG_TEAM.PLAYER, Vector3.zero);
        teamBb.BallInfo.updateBallState(BallManager_State.BALL_STATE.HOLD);

        var passer = CreateFieldNpc(1001, AnimalControlRole.Human);
        var holder = CreateFieldNpc(1002, AnimalControlRole.TeammateNpc);
        var passerBb = passer.GetComponentInChildren<PlayerBlackboard>();
        passerBb.SetFact(new Fact(SymbolTag.Basic.HAS_BALL, "true"), true);
        passerBb.SetFact(new Fact(SymbolTag.Action.CAN_MOVE, "true"), true);
        passerBb.BallState.updateBallInfo(false, 5f, Vector3.zero);

        GoapMainNpcProductionEnvironment.Sync(true);
        try
        {
            Assert.That(MainNpcAttackPlanning.IsSelfBallOwner(passerBb), Is.True);
            Assert.That(MainNpcAttackPlanning.IsActivelyHoldingBall(passerBb), Is.False);
            Assert.That(MainNpcPostPassPlanning.IsTeamBallSupportContext(passerBb), Is.True);
            var attackGoal = ScriptableObject.CreateInstance<BallPossessionAttackGoalSO>();
            var supportGoal = ScriptableObject.CreateInstance<TeamBallSupportGoalSO>();
            Assert.That(attackGoal.IsAchievable(passerBb), Is.False);
            Assert.That(supportGoal.IsAchievable(passerBb), Is.True);
        }
        finally
        {
            GoapMainNpcProductionEnvironment.Sync(false);
            Object.DestroyImmediate(passer);
            Object.DestroyImmediate(holder);
            Object.DestroyImmediate(root);
            SetStaticField(typeof(TeamFacade), "_instance", null);
        }
    }

    [Test]
    public void IsActivelyHoldingBall_TrueWhenTeamBlackboardHoldMatchesBeforeBallManagerSync()
    {
        var root = CreateTeamFacadeRoot();
        var teamBb = root.GetComponent<TeamBlackboard>();
        teamBb.BallInfo.setExistBall();
        teamBb.BallInfo.updateBallID(1007, BallManager_State.BELONG_TEAM.ENEMY, Vector3.zero);
        teamBb.BallInfo.updateBallState(BallManager_State.BALL_STATE.HOLD);

        var holder = CreateFieldNpc(1007, AnimalControlRole.EnemyFieldNpc);
        var holderBb = holder.GetComponentInChildren<PlayerBlackboard>();
        holderBb.BallState.updateBallInfo(false, 5f, Vector3.zero);

        try
        {
            Assert.That(MainNpcAttackPlanning.IsActivelyHoldingBall(holderBb), Is.True);
            var attackGoal = ScriptableObject.CreateInstance<BallPossessionAttackGoalSO>();
            holderBb.SetFact(new Fact(SymbolTag.Action.CAN_MOVE, "true"), true);
            Assert.That(attackGoal.IsAchievable(holderBb), Is.True);
        }
        finally
        {
            Object.DestroyImmediate(holder);
            Object.DestroyImmediate(root);
            SetStaticField(typeof(TeamFacade), "_instance", null);
        }
    }

    [Test]
    public void IsBallPossessionAttackContext_TrueWhenTeamBlackboardOwnerIsViewIdNotPlayerId()
    {
        const int playerId = 1007;
        const int viewId = 9107;

        var root = CreateTeamFacadeRoot();
        var teamBb = root.GetComponent<TeamBlackboard>();
        teamBb.BallInfo.setExistBall();
        teamBb.BallInfo.updateBallID(viewId, BallManager_State.BELONG_TEAM.ENEMY, Vector3.zero);
        teamBb.BallInfo.updateBallState(BallManager_State.BALL_STATE.HOLD);

        var go = new GameObject("enemy_elephant");
        var photonView = go.AddComponent<PhotonView>();
        photonView.ViewID = viewId;
        go.AddComponent<PhotonAvatarContainerChild>();
        var facade = go.AddComponent<AnimalFacade>();
        SetPrivateField(facade, "_avatar", go.GetComponent<PhotonAvatarContainerChild>());
        go.AddComponent<AnimalControlAssignment>().SetRole(AnimalControlRole.EnemyFieldNpc);

        var bbGo = new GameObject("bb");
        bbGo.transform.SetParent(go.transform, false);
        var bb = bbGo.AddComponent<PlayerBlackboard>();
        bb.BasicData.init(go);
        typeof(PlayerBasicData).GetProperty("PlayerID")!
            .SetValue(bb.BasicData, playerId, null);
        bb.BallState.updateBallInfo(false, 5f, Vector3.zero);
        bb.SetFact(new Fact(SymbolTag.Action.CAN_MOVE, "true"), true);

        try
        {
            Assert.That(MainNpcAttackPlanning.IsActivelyHoldingBall(bb), Is.True);
            Assert.That(MainNpcAttackPlanning.IsBallPossessionAttackContext(bb), Is.True);
            var attackGoal = ScriptableObject.CreateInstance<BallPossessionAttackGoalSO>();
            Assert.That(attackGoal.IsAchievable(bb), Is.True);
        }
        finally
        {
            Object.DestroyImmediate(go);
            Object.DestroyImmediate(root);
            SetStaticField(typeof(TeamFacade), "_instance", null);
        }
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
