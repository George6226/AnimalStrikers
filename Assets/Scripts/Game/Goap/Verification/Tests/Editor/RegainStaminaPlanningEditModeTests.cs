#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using Game.Goap.Goals;
using NUnit.Framework;
using UnityEngine;

/// <summary>6-C P0: hasStamina / RegainStamina の純判定とカタログ接続。</summary>
public sealed class RegainStaminaPlanningEditModeTests
{
    [TearDown]
    public void TearDown()
    {
        ClearTeamFacadeSingleton();
    }

    [Test]
    public void HasSufficientStamina_UsesLowMoveThreshold()
    {
        float threshold = ConstData.STAMINA_LOW_MOVE_RATIO_THRESHOLD;
        Assert.That(GoapStaminaPlanning.HasSufficientStamina(threshold), Is.True);
        Assert.That(GoapStaminaPlanning.HasSufficientStamina(threshold - 0.01f), Is.False);
        Assert.That(GoapStaminaPlanning.HasSufficientStamina(1f), Is.True);
    }

    [Test]
    public void ShouldConsiderRegain_RequiresLowStaminaAndCalmPossession()
    {
        var (teamGo, bb, teamBB) = CreateScene();

        try
        {
            bb.SetFact(new Fact(SymbolTag.Basic.HAS_STAMINA, "true"), true);
            bb.SetFact(new Fact(SymbolTag.Basic.HAS_STAMINA, "false"), false);
            Assert.That(GoapStaminaPlanning.ShouldConsiderRegain(bb, teamBB), Is.False);

            bb.SetFact(new Fact(SymbolTag.Basic.HAS_STAMINA, "true"), false);
            bb.SetFact(new Fact(SymbolTag.Basic.HAS_STAMINA, "false"), true);
            Assert.That(GoapStaminaPlanning.ShouldConsiderRegain(bb, teamBB), Is.True);

            teamBB.BallInfo.updateBallID(-1, BallManager_State.BELONG_TEAM.FREE, Vector3.forward);
            teamBB.BallInfo.updateBallState(BallManager_State.BALL_STATE.FREE);
            Assert.That(GoapStaminaPlanning.ShouldConsiderRegain(bb, teamBB), Is.False);

            teamBB.BallInfo.updateBallID(1005, BallManager_State.BELONG_TEAM.ENEMY, Vector3.zero);
            teamBB.BallInfo.updateBallState(BallManager_State.BALL_STATE.HOLD);
            Assert.That(GoapStaminaPlanning.ShouldConsiderRegain(bb, teamBB), Is.False);
        }
        finally
        {
            Object.DestroyImmediate(teamGo);
        }
    }

    [Test]
    public void RegainStaminaGoal_IsAchievableWhenShouldConsiderRegain()
    {
        var goal = ScriptableObject.CreateInstance<RegainStaminaGoalSO>();
        var (teamGo, bb, teamBB) = CreateScene();

        try
        {
            bb.SetFact(new Fact(SymbolTag.Basic.HAS_STAMINA, "true"), false);
            bb.SetFact(new Fact(SymbolTag.Basic.HAS_STAMINA, "false"), true);
            Assert.That(goal.IsAchievable(bb), Is.True);
            Assert.That(goal.EvaluatePriority(bb, teamBB), Is.GreaterThan(10f));

            bb.SetFact(new Fact(SymbolTag.Basic.HAS_STAMINA, "true"), true);
            bb.SetFact(new Fact(SymbolTag.Basic.HAS_STAMINA, "false"), false);
            Assert.That(goal.IsAchievable(bb), Is.False);
        }
        finally
        {
            Object.DestroyImmediate(teamGo);
            Object.DestroyImmediate(goal);
        }
    }

    [Test]
    public void NormalizeLists_IncludesRegainStaminaAndStandRecover()
    {
        var goals = new List<GoapGoalSO>();
        var actions = new List<GoapActionSO>();
        GoapMainNpcCatalog.NormalizeLists(goals, actions);

        Assert.That(goals.Exists(g => g is RegainStaminaGoalSO), Is.True);
        Assert.That(actions.Exists(a => a is StandRecoverStaminaActionSO), Is.True);

        var filtered = GoapMainNpcCatalog.FilterActionsForGoal(
            goals.Find(g => g is RegainStaminaGoalSO),
            actions);
        Assert.That(filtered, Has.Count.EqualTo(1));
        Assert.That(filtered[0], Is.InstanceOf<StandRecoverStaminaActionSO>());
    }

    [Test]
    public void StandRecover_DynamicCost_Is99WhenHasStamina()
    {
        var action = ScriptableObject.CreateInstance<StandRecoverStaminaActionSO>();
        var (teamGo, bb, _) = CreateScene();

        try
        {
            bb.SetFact(new Fact(SymbolTag.Basic.HAS_STAMINA, "true"), true);
            bb.SetFact(new Fact(SymbolTag.Basic.HAS_STAMINA, "false"), false);
            Assert.That(action.CalculateDynamicCost(bb), Is.EqualTo(99f));

            bb.SetFact(new Fact(SymbolTag.Basic.HAS_STAMINA, "true"), false);
            bb.SetFact(new Fact(SymbolTag.Basic.HAS_STAMINA, "false"), true);
            Assert.That(action.CalculateDynamicCost(bb), Is.LessThan(10f));
        }
        finally
        {
            Object.DestroyImmediate(teamGo);
            Object.DestroyImmediate(action);
        }
    }

    private static (GameObject teamGo, PlayerBlackboard bb, TeamBlackboard teamBB) CreateScene()
    {
        var teamGo = new GameObject("teamRoot");
        var teamFacade = teamGo.AddComponent<TeamFacade>();
        var teamBB = teamGo.AddComponent<TeamBlackboard>();
        BindTeamFacadeSingleton(teamFacade, teamBB);
        teamBB.FieldInfo.Initialize(100f, 60f);
        teamBB.BallInfo.setExistBall();
        teamBB.BallInfo.updateBallID(1001, BallManager_State.BELONG_TEAM.PLAYER, Vector3.zero);
        teamBB.BallInfo.updateBallState(BallManager_State.BALL_STATE.HOLD);

        var actorGo = new GameObject("actor");
        actorGo.transform.SetParent(teamGo.transform, false);
        actorGo.AddComponent<AnimalFacade>();
        actorGo.AddComponent<AnimalFormationSlot>().Initialize(0);
        actorGo.AddComponent<AnimalControlAssignment>().SetRole(AnimalControlRole.Human);

        var bbGo = new GameObject("PlayerBlackboard");
        bbGo.transform.SetParent(actorGo.transform, false);
        var bb = bbGo.AddComponent<PlayerBlackboard>();
        bb.BasicData.init(bbGo);
        bb.PhysicalState.init(Vector3.zero);
        bb.ActionState.init();
        bb.SetFact(new Fact(SymbolTag.Action.CAN_MOVE, "true"), true);
        bb.SetFact(new Fact(SymbolTag.Action.CAN_MOVE, "false"), false);
        bb.SetFact(new Fact(SymbolTag.Basic.HAS_BALL, "true"), false);
        bb.SetFact(new Fact(SymbolTag.Basic.HAS_BALL, "false"), true);

        return (teamGo, bb, teamBB);
    }

    private static void BindTeamFacadeSingleton(TeamFacade facade, TeamBlackboard teamBB)
    {
        typeof(TeamFacade).GetField("_teamBlackboard", BindingFlags.Instance | BindingFlags.NonPublic)
            ?.SetValue(facade, teamBB);
        typeof(TeamFacade).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)
            ?.SetValue(null, facade);
    }

    private static void ClearTeamFacadeSingleton()
    {
        typeof(TeamFacade).GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)
            ?.SetValue(null, null);
    }
}
#endif
