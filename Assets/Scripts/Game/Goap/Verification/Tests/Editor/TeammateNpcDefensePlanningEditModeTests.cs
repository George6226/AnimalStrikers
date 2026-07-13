#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using Game.Goap;
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

    [Test]
    public void NeedsForcedPostShootDefensePlan_TrueDuringAgentGraceEvenWithoutShootBallState()
    {
        var (teamGo, enemyBb, _, _) = CreateOwnTeamShootReleaseScene(enemyNpc: true);
        teamGo.GetComponent<TeamBlackboard>().BallInfo.updateBallState(BallManager_State.BALL_STATE.HOLD);
        teamGo.GetComponent<TeamBlackboard>().BallInfo.updateBallID(
            1001,
            BallManager_State.BELONG_TEAM.PLAYER,
            Vector3.zero);

        try
        {
            Assert.That(
                GoapFieldNpcPerspective.IsOwnTeamShootReleaseTransition(
                    teamGo.GetComponent<TeamBlackboard>(),
                    enemyBb),
                Is.False);
            Assert.That(
                TeammateNpcDefensePlanning.NeedsForcedPostShootDefensePlan(
                    enemyBb,
                    Time.time + 1f),
                Is.True);
        }
        finally
        {
            Object.DestroyImmediate(teamGo);
        }
    }

    [Test]
    public void NeedsForcedPostShootDefensePlan_TrueForEnemyNpcDuringOwnShootTransition()
    {
        var (teamGo, enemyBb, _, _) = CreateOwnTeamShootReleaseScene(enemyNpc: true);
        var defensivePositioning = ScriptableObject.CreateInstance<DefensivePositioningGoalSO>();

        try
        {
            Assert.That(TeammateNpcDefensePlanning.NeedsForcedPostShootDefensePlan(enemyBb), Is.True);
            Assert.That(defensivePositioning.IsAchievable(enemyBb), Is.False);
        }
        finally
        {
            Object.DestroyImmediate(defensivePositioning);
            Object.DestroyImmediate(teamGo);
        }
    }

    [Test]
    public void TryBuildForcedPostShootDefensePlan_ReturnsDefenseWhenSelectBestGoalWouldFail()
    {
        var (teamGo, enemyBb, goals, actions) = CreateOwnTeamShootReleaseScene(enemyNpc: true);

        try
        {
            Assert.That(
                TeammateNpcDefensePlanning.TryBuildForcedPostShootDefensePlan(
                    enemyBb,
                    goals,
                    actions,
                    out var goal,
                    out var plan),
                Is.True);
            Assert.That(goal, Is.InstanceOf<DefensivePositioningGoalSO>());
            Assert.That(plan, Is.Not.Null);
            Assert.That(plan.Count, Is.EqualTo(1));
            Assert.That(GoapTeammateNpcCatalog.IsDefenseAction(plan.Peek()), Is.True);
        }
        finally
        {
            Object.DestroyImmediate(teamGo);
        }
    }

    [Test]
    public void NeedsForcedDefensePlanWhenNoGoal_TrueDuringEnemyBallContext()
    {
        var (teamGo, humanBb) = CreateEnemyBallDefenseScene(AnimalControlRole.Human);
        GoapMainNpcProductionEnvironment.Sync(true);
        SetDefenseFacts(humanBb);

        try
        {
            Assert.That(TeammateNpcDefensePlanning.NeedsForcedDefensePlanWhenNoGoal(humanBb), Is.True);
        }
        finally
        {
            Object.DestroyImmediate(teamGo);
        }
    }

    [Test]
    public void NeedsForcedDefensePlanWhenNoGoal_TrueDuringContextGraceWithoutEnemyBall()
    {
        var (teamGo, humanBb) = CreateEnemyBallDefenseScene(AnimalControlRole.Human);
        GoapMainNpcProductionEnvironment.Sync(true);
        SetDefenseFacts(humanBb);
        var teamBB = teamGo.GetComponent<TeamBlackboard>();
        teamBB.BallInfo.updateBallID(1001, BallManager_State.BELONG_TEAM.PLAYER, Vector3.zero);
        teamBB.BallInfo.updateBallState(BallManager_State.BALL_STATE.HOLD);

        try
        {
            Assert.That(
                TeammateNpcDefensePlanning.NeedsForcedDefensePlanWhenNoGoal(
                    humanBb,
                    float.NegativeInfinity,
                    Time.time + 1f),
                Is.True);
        }
        finally
        {
            Object.DestroyImmediate(teamGo);
        }
    }

    [Test]
    public void TryBuildForcedDefensePlanWhenNoGoal_ReturnsPlanWhenGoalNotAchievableButGraceActive()
    {
        var (teamGo, humanBb) = CreateEnemyBallDefenseScene(AnimalControlRole.Human);
        GoapMainNpcProductionEnvironment.Sync(true);
        SetDefenseFacts(humanBb);
        var teamBB = teamGo.GetComponent<TeamBlackboard>();
        teamBB.BallInfo.updateBallID(1001, BallManager_State.BELONG_TEAM.PLAYER, Vector3.zero);
        teamBB.BallInfo.updateBallState(BallManager_State.BALL_STATE.HOLD);

        var goals = new List<GoapGoalSO> { ScriptableObject.CreateInstance<DefensivePositioningGoalSO>() };
        var actions = new List<GoapActionSO>();
        GoapMainNpcCatalog.NormalizeLists(goals, actions);

        try
        {
            Assert.That(goals[0].IsAchievable(humanBb), Is.False);
            Assert.That(
                TeammateNpcDefensePlanning.TryBuildForcedDefensePlanWhenNoGoal(
                    humanBb,
                    goals,
                    actions,
                    out var goal,
                    out var plan,
                    float.NegativeInfinity,
                    Time.time + 1f),
                Is.True);
            Assert.That(goal, Is.InstanceOf<DefensivePositioningGoalSO>());
            Assert.That(plan.Count, Is.EqualTo(1));
        }
        finally
        {
            Object.DestroyImmediate(goals[0]);
            Object.DestroyImmediate(teamGo);
        }
    }

    private static (GameObject teamGo, PlayerBlackboard bb, List<GoapGoalSO> goals, List<GoapActionSO> actions)
        CreateOwnTeamShootReleaseScene(bool enemyNpc)
    {
        var teamGo = new GameObject("teamRoot");
        var teamFacade = teamGo.AddComponent<TeamFacade>();
        var teamBB = teamGo.AddComponent<TeamBlackboard>();
        BindTeamFacadeSingleton(teamFacade, teamBB);
        teamBB.FieldInfo.Initialize(100f, 60f);
        teamBB.BallInfo.setExistBall();
        teamBB.BallInfo.updateBallID(1005, BallManager_State.BELONG_TEAM.ENEMY, Vector3.zero);
        teamBB.BallInfo.updateBallState(BallManager_State.BALL_STATE.HOLD);
        teamBB.BallInfo.updateBallID(-1, BallManager_State.BELONG_TEAM.FREE, Vector3.forward);
        teamBB.BallInfo.updateBallState(BallManager_State.BALL_STATE.SHOOT);

        var actorGo = new GameObject("actor");
        actorGo.AddComponent<AnimalFacade>();
        actorGo.AddComponent<AnimalControlAssignment>().SetRole(
            enemyNpc ? AnimalControlRole.EnemyFieldNpc : AnimalControlRole.TeammateNpc);
        var bbGo = new GameObject("PlayerBlackboard");
        bbGo.transform.SetParent(actorGo.transform, false);
        var bb = bbGo.AddComponent<PlayerBlackboard>();
        bb.BasicData.init(bbGo);
        bb.ActionState.init();
        bb.SetFact(new Fact(SymbolTag.Action.CAN_MOVE, "true"), true);
        bb.SetFact(new Fact(SymbolTag.Basic.HAS_BALL, "true"), true);

        var goals = new List<GoapGoalSO>();
        var actions = new List<GoapActionSO>();
        GoapEnemyNpcCatalog.NormalizeLists(goals, actions, GoapNpcTier.Sub);

        return (teamGo, bb, goals, actions);
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
