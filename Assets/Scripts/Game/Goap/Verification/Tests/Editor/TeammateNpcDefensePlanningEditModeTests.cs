#if UNITY_EDITOR
using System.Reflection;
using Game.Goap.Goals;
using NUnit.Framework;
using UnityEngine;

/// <summary>GOAP仕上げ G0: 本番 Main の戦術守備パス。</summary>
public sealed class TeammateNpcDefensePlanningEditModeTests
{
    [TearDown]
    public void TearDown()
    {
        GoapMainNpcProductionEnvironment.Sync(false);
        TeammateNpcGoapRoleDifferentiation.Enabled = true;
        ClearTeamFacadeSingleton();
    }

    [Test]
    public void ShouldUseTacticalDefenseGoal_TrueForProductionMainDuringEnemyBall()
    {
        var (teamGo, humanBb) = CreateEnemyBallDefenseScene(AnimalControlRole.Human);
        GoapMainNpcProductionEnvironment.Sync(true);

        try
        {
            Assert.That(TeammateNpcDefensePlanning.ShouldUseTacticalDefenseGoal(humanBb), Is.True);
        }
        finally
        {
            Object.DestroyImmediate(teamGo);
        }
    }

    [Test]
    public void ShouldUseTacticalDefenseGoal_FalseForHumanWhenProductionOff()
    {
        var (teamGo, humanBb) = CreateEnemyBallDefenseScene(AnimalControlRole.Human);
        GoapMainNpcProductionEnvironment.Sync(false);

        try
        {
            Assert.That(TeammateNpcDefensePlanning.ShouldUseTacticalDefenseGoal(humanBb), Is.False);
        }
        finally
        {
            Object.DestroyImmediate(teamGo);
        }
    }

    [Test]
    public void ShouldUseTacticalDefenseGoal_TrueForTeammateNpcDuringEnemyBall()
    {
        var (teamGo, allyBb) = CreateEnemyBallDefenseScene(AnimalControlRole.TeammateNpc);

        try
        {
            Assert.That(TeammateNpcDefensePlanning.ShouldUseTacticalDefenseGoal(allyBb), Is.True);
        }
        finally
        {
            Object.DestroyImmediate(teamGo);
        }
    }

    [Test]
    public void ProductionMain_InDefensivePosition_UsesDefensivePositioningNotEnemyBallDefense()
    {
        var (teamGo, humanBb) = CreateEnemyBallDefenseScene(AnimalControlRole.Human);
        GoapMainNpcProductionEnvironment.Sync(true);
        SetDefenseFacts(humanBb);

        var enemyBallDefense = ScriptableObject.CreateInstance<EnemyBallDefenseGoalSO>();
        var defensivePositioning = ScriptableObject.CreateInstance<DefensivePositioningGoalSO>();

        try
        {
            Assert.That(TeammateNpcDefensePlanning.ShouldUseTacticalDefenseGoal(humanBb), Is.True);
            Assert.That(enemyBallDefense.IsAchievable(humanBb), Is.False);
            Assert.That(defensivePositioning.IsAchievable(humanBb), Is.True);
        }
        finally
        {
            Object.DestroyImmediate(enemyBallDefense);
            Object.DestroyImmediate(defensivePositioning);
            Object.DestroyImmediate(teamGo);
        }
    }

    [Test]
    public void ProductionMain_InDefensivePosition_CanBuildForcedTacticalDefensePlan()
    {
        var (teamGo, humanBb) = CreateEnemyBallDefenseScene(AnimalControlRole.Human);
        GoapMainNpcProductionEnvironment.Sync(true);
        SetDefenseFacts(humanBb);

        var actions = new List<GoapActionSO>();
        GoapMainNpcCatalog.NormalizeLists(new List<GoapGoalSO>(), actions);
        var scoped = GoapMainNpcCatalog.FilterActionsForGoal(
            ScriptableObject.CreateInstance<DefensivePositioningGoalSO>(),
            actions);

        try
        {
            Assert.That(
                TeammateNpcDefensePlanning.TryBuildForcedTacticalDefensePlan(humanBb, scoped, out var plan),
                Is.True);
            Assert.That(plan, Is.Not.Null);
            Assert.That(plan.Count, Is.GreaterThan(0));
        }
        finally
        {
            Object.DestroyImmediate(teamGo);
        }
    }

    private static (GameObject teamGo, PlayerBlackboard bb) CreateEnemyBallDefenseScene(AnimalControlRole role)
    {
        var teamGo = new GameObject("teamRoot");
        var teamFacade = teamGo.AddComponent<TeamFacade>();
        var teamBB = teamGo.AddComponent<TeamBlackboard>();
        BindTeamFacadeSingleton(teamFacade, teamBB);
        teamBB.FieldInfo.Initialize(100f, 60f);
        teamBB.BallInfo.setExistBall();
        teamBB.BallInfo.updateBallID(1005, BallManager_State.BELONG_TEAM.ENEMY, new Vector3(10f, 0f, 0f));
        teamBB.BallInfo.updateBallState(BallManager_State.BALL_STATE.HOLD);
        teamBB.BallInfo.updateBallOwnerPosition(new Vector3(10f, 0f, 0f));

        var actorGo = new GameObject("actor");
        actorGo.AddComponent<AnimalFacade>();
        actorGo.AddComponent<AnimalFormationSlot>().Initialize(0);
        actorGo.AddComponent<AnimalControlAssignment>().SetRole(role);

        var bbGo = new GameObject("PlayerBlackboard");
        bbGo.transform.SetParent(actorGo.transform, false);
        var bb = bbGo.AddComponent<PlayerBlackboard>();
        bb.BasicData.init(bbGo);
        bb.PhysicalState.init(Vector3.zero);
        bb.ActionState.init();
        bb.SetFact(new Fact(SymbolTag.Action.CAN_MOVE, "true"), true);

        return (teamGo, bb);
    }

    private static void SetDefenseFacts(PlayerBlackboard bb)
    {
        bb.SetFact(new Fact(SymbolTag.Action.IS_IN_DEFENSIVE_POSITION, "true"), true);
        bb.SetFact(new Fact(SymbolTag.Basic.IS_MOVING, "true"), true);
        bb.SetFact(new Fact(SymbolTag.Basic.HAS_BALL, "true"), false);
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
