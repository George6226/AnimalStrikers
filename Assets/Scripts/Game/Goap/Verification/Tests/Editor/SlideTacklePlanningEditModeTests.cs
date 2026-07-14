#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using Game.Goap.Goals;
using NUnit.Framework;
using UnityEngine;

/// <summary>F4: Main NPC の SlideTackle 選出条件とカタログ接続。</summary>
public sealed class SlideTacklePlanningEditModeTests
{
    [TearDown]
    public void TearDown()
    {
        GoapMainNpcProductionEnvironment.Sync(false);
        TeammateNpcGoapRoleDifferentiation.Enabled = true;
        ClearTeamFacadeSingleton();
    }

    [Test]
    public void NormalizeLists_IncludesSlideTackleInDefenseFilter()
    {
        var goals = new List<GoapGoalSO>();
        var actions = new List<GoapActionSO>();
        GoapMainNpcCatalog.NormalizeLists(goals, actions);

        var goal = goals.Find(g => g is DefensivePositioningGoalSO);
        var filtered = GoapMainNpcCatalog.FilterActionsForGoal(goal, actions);

        Assert.That(filtered.Exists(a => a is SlideTackleActionSO), Is.True);
        Assert.That(
            GoapMainNpcCatalog.FilterActionsForGoal(
                goals.Find(g => g is BallPossessionAttackGoalSO),
                actions).Exists(a => a is SlideTackleActionSO),
            Is.False);
    }

    [Test]
    public void CanSlideTackle_TrueForProductionMainNearEnemyBall()
    {
        var (teamGo, humanBb) = CreateNearEnemyBallMainScene();
        GoapMainNpcProductionEnvironment.Sync(true);

        try
        {
            Assert.That(TeammateNpcDefensePlanning.IsSlideTackleEligibleAgent(humanBb), Is.True);
            Assert.That(TeammateNpcDefensePlanning.CanSlideTackle(humanBb), Is.True);
        }
        finally
        {
            Object.DestroyImmediate(teamGo);
        }
    }

    [Test]
    public void CanSlideTackle_FalseForTeammateNpcSubEvenWhenNear()
    {
        var (teamGo, allyBb) = CreateNearEnemyBallAllyNpcScene();

        try
        {
            Assert.That(TeammateNpcDefensePlanning.IsSlideTackleEligibleAgent(allyBb), Is.False);
            Assert.That(TeammateNpcDefensePlanning.CanSlideTackle(allyBb), Is.False);
        }
        finally
        {
            Object.DestroyImmediate(teamGo);
        }
    }

    [Test]
    public void ComputeSlideTackleCostAdjustment_NearIsCheaperThanFar()
    {
        var (teamGo, humanBb) = CreateNearEnemyBallMainScene();
        GoapMainNpcProductionEnvironment.Sync(true);
        var teamBB = teamGo.GetComponent<TeamBlackboard>();

        try
        {
            float nearAdj = TeammateNpcDefensePlanning.ComputeSlideTackleCostAdjustment(humanBb);
            humanBb.PhysicalState.updatePhysicalInfo(new Vector3(0f, 0f, -20f), Vector3.zero);
            humanBb.SetFact(new Fact(SymbolTag.Position.NEAR_ENEMY_HAS_BALL, "true"), false);
            teamBB.BallInfo.updateBallOwnerPosition(new Vector3(10f, 0f, 0f));
            float farAdj = TeammateNpcDefensePlanning.ComputeSlideTackleCostAdjustment(humanBb);

            Assert.That(nearAdj, Is.LessThan(0f));
            Assert.That(farAdj, Is.GreaterThanOrEqualTo(40f));
        }
        finally
        {
            Object.DestroyImmediate(teamGo);
        }
    }

    private static (GameObject teamGo, PlayerBlackboard bb) CreateNearEnemyBallMainScene()
    {
        return CreateNearEnemyBallScene(AnimalControlRole.Human, near: true);
    }

    private static (GameObject teamGo, PlayerBlackboard bb) CreateNearEnemyBallAllyNpcScene()
    {
        return CreateNearEnemyBallScene(AnimalControlRole.TeammateNpc, near: true);
    }

    private static (GameObject teamGo, PlayerBlackboard bb) CreateNearEnemyBallScene(
        AnimalControlRole role,
        bool near)
    {
        var teamGo = new GameObject("teamRoot");
        var teamFacade = teamGo.AddComponent<TeamFacade>();
        var teamBB = teamGo.AddComponent<TeamBlackboard>();
        BindTeamFacadeSingleton(teamFacade, teamBB);
        teamBB.FieldInfo.Initialize(100f, 60f);
        teamBB.BallInfo.setExistBall();
        Vector3 ownerPos = new Vector3(10f, 0f, 0f);
        teamBB.BallInfo.updateBallID(1005, BallManager_State.BELONG_TEAM.ENEMY, ownerPos);
        teamBB.BallInfo.updateBallState(BallManager_State.BALL_STATE.HOLD);
        teamBB.BallInfo.updateBallOwnerPosition(ownerPos);

        var actorGo = new GameObject("actor");
        actorGo.AddComponent<AnimalFacade>();
        actorGo.AddComponent<AnimalFormationSlot>().Initialize(0);
        actorGo.AddComponent<AnimalControlAssignment>().SetRole(role);

        var bbGo = new GameObject("PlayerBlackboard");
        bbGo.transform.SetParent(actorGo.transform, false);
        var bb = bbGo.AddComponent<PlayerBlackboard>();
        bb.BasicData.init(bbGo);
        Vector3 selfPos = near ? new Vector3(10.5f, 0f, 0.5f) : new Vector3(0f, 0f, -20f);
        bb.PhysicalState.init(selfPos);
        bb.ActionState.init();
        bb.SetFact(new Fact(SymbolTag.Action.CAN_MOVE, "true"), true);
        bb.SetFact(new Fact(SymbolTag.Basic.HAS_BALL, "true"), false);
        bb.SetFact(new Fact(SymbolTag.Position.NEAR_ENEMY_HAS_BALL, "true"), near);

        return (teamGo, bb);
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
