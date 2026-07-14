#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using Game.Goap.Goals;
using NUnit.Framework;
using UnityEngine;

/// <summary>F5: Main NPC の UseSpecial 選出条件とカタログ接続。</summary>
public sealed class UseSpecialPlanningEditModeTests
{
    [TearDown]
    public void TearDown()
    {
        GoapMainNpcProductionEnvironment.Sync(false);
        ClearTeamFacadeSingleton();
    }

    [Test]
    public void NormalizeLists_IncludesUseSpecialInAttackAndDefenseFilters()
    {
        var goals = new List<GoapGoalSO>();
        var actions = new List<GoapActionSO>();
        GoapMainNpcCatalog.NormalizeLists(goals, actions);

        var attack = GoapMainNpcCatalog.FilterActionsForGoal(
            goals.Find(g => g is BallPossessionAttackGoalSO),
            actions);
        var defense = GoapMainNpcCatalog.FilterActionsForGoal(
            goals.Find(g => g is DefensivePositioningGoalSO),
            actions);

        Assert.That(attack.Exists(a => a is UseSpecialActionSO), Is.True);
        Assert.That(defense.Exists(a => a is UseSpecialActionSO), Is.True);
    }

    [Test]
    public void CanUseSpecial_FalseWithoutSpecialComponent()
    {
        var (teamGo, humanBb) = CreateMainScene(withSpecialReady: false);
        GoapMainNpcProductionEnvironment.Sync(true);

        try
        {
            Assert.That(MainNpcAttackPlanning.CanUseSpecial(humanBb), Is.False);
            var action = ScriptableObject.CreateInstance<UseSpecialActionSO>();
            try
            {
                Assert.That(action.CalculateDynamicCost(humanBb), Is.EqualTo(99f));
            }
            finally
            {
                Object.DestroyImmediate(action);
            }
        }
        finally
        {
            Object.DestroyImmediate(teamGo);
        }
    }

    [Test]
    public void CanUseSpecial_TrueForProductionMainWhenGaugeFull()
    {
        var (teamGo, humanBb) = CreateMainScene(withSpecialReady: true);
        GoapMainNpcProductionEnvironment.Sync(true);

        try
        {
            Assert.That(MainNpcAttackPlanning.CanUseSpecial(humanBb), Is.True);
            var action = ScriptableObject.CreateInstance<UseSpecialActionSO>();
            try
            {
                Assert.That(action.CalculateDynamicCost(humanBb), Is.LessThan(10f));
            }
            finally
            {
                Object.DestroyImmediate(action);
            }
        }
        finally
        {
            Object.DestroyImmediate(teamGo);
        }
    }

    [Test]
    public void CanUseSpecial_FalseForTeammateNpcSubEvenWithGauge()
    {
        var (teamGo, allyBb) = CreateAllyNpcScene(withSpecialReady: true);

        try
        {
            Assert.That(MainNpcAttackPlanning.CanUseSpecial(allyBb), Is.False);
        }
        finally
        {
            Object.DestroyImmediate(teamGo);
        }
    }

    private static (GameObject teamGo, PlayerBlackboard bb) CreateMainScene(bool withSpecialReady)
    {
        return CreateScene(AnimalControlRole.Human, withSpecialReady);
    }

    private static (GameObject teamGo, PlayerBlackboard bb) CreateAllyNpcScene(bool withSpecialReady)
    {
        return CreateScene(AnimalControlRole.TeammateNpc, withSpecialReady);
    }

    private static (GameObject teamGo, PlayerBlackboard bb) CreateScene(
        AnimalControlRole role,
        bool withSpecialReady)
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
        actorGo.AddComponent<AnimalFacade>();
        actorGo.AddComponent<AnimalFormationSlot>().Initialize(0);
        actorGo.AddComponent<AnimalControlAssignment>().SetRole(role);

        if (withSpecialReady)
        {
            var gauge = actorGo.AddComponent<AnimalAction_Gauge>();
            SetPrivateField(gauge, "_gaugeValue", 1f);
            var specialAction = actorGo.AddComponent<DefaultSpecialAction>();
            var special = actorGo.AddComponent<AnimalAction_Special>();
            SetPrivateField(special, "_specialGauge", gauge);
            SetPrivateField(special, "_specialAction", specialAction);
        }

        var bbGo = new GameObject("PlayerBlackboard");
        bbGo.transform.SetParent(actorGo.transform, false);
        var bb = bbGo.AddComponent<PlayerBlackboard>();
        bb.BasicData.init(bbGo);
        bb.PhysicalState.init(Vector3.zero);
        bb.ActionState.init();
        bb.SetFact(new Fact(SymbolTag.Action.CAN_MOVE, "true"), true);

        return (teamGo, bb);
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        FieldInfo field = target.GetType().GetField(fieldName, BindingFlags.NonPublic | BindingFlags.Instance);
        Assert.That(field, Is.Not.Null, fieldName);
        field.SetValue(target, value);
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
