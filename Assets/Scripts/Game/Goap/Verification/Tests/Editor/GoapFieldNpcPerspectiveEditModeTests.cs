#if UNITY_EDITOR
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

public sealed class GoapFieldNpcPerspectiveEditModeTests
{
    [Test]
    public void IsTeamBallAttackContext_UsesMirroredBallOwnershipForEnemyNpc()
    {
        var teamGo = new GameObject("teamBB");
        var teamBB = teamGo.AddComponent<TeamBlackboard>();
        teamBB.FieldInfo.Initialize(100f, 60f);
        teamBB.BallInfo.setExistBall();
        teamBB.BallInfo.updateBallID(1, BallManager_State.BELONG_TEAM.PLAYER, Vector3.zero);
        teamBB.BallInfo.updateBallState(BallManager_State.BALL_STATE.HOLD);

        var enemyGo = new GameObject("enemy");
        var assignment = enemyGo.AddComponent<AnimalControlAssignment>();
        enemyGo.AddComponent<AnimalFacade>();
        assignment.SetRole(AnimalControlRole.EnemyFieldNpc);
        var enemyBbGo = new GameObject("PlayerBlackboard");
        enemyBbGo.transform.SetParent(enemyGo.transform, false);
        var enemyBb = enemyBbGo.AddComponent<PlayerBlackboard>();
        enemyBb.BasicData.init(enemyBbGo);

        try
        {
            Assert.That(GoapFieldNpcPerspective.IsTeamBallAttackContext(teamBB, null), Is.True);
            Assert.That(GoapFieldNpcPerspective.IsTeamBallAttackContext(teamBB, enemyBb), Is.False);
            Assert.That(GoapFieldNpcPerspective.IsOpponentBallDefenseContext(teamBB, enemyBb), Is.True);
        }
        finally
        {
            Object.DestroyImmediate(enemyGo);
            Object.DestroyImmediate(teamGo);
        }
    }

    [Test]
    public void FreeBall_LastTeamPossession_RemainsAttackContext()
    {
        var teamGo = new GameObject("teamBB");
        var teamBB = teamGo.AddComponent<TeamBlackboard>();
        teamBB.FieldInfo.Initialize(100f, 60f);
        teamBB.BallInfo.setExistBall();
        teamBB.BallInfo.updateBallID(1, BallManager_State.BELONG_TEAM.PLAYER, Vector3.zero);
        teamBB.BallInfo.updateBallState(BallManager_State.BALL_STATE.HOLD);
        teamBB.BallInfo.updateBallID(-1, BallManager_State.BELONG_TEAM.FREE, Vector3.forward);
        teamBB.BallInfo.updateBallState(BallManager_State.BALL_STATE.FREE);

        try
        {
            Assert.That(GoapFieldNpcPerspective.IsTeamBallAttackContext(teamBB), Is.True);
            Assert.That(GoapFieldNpcPerspective.IsOpponentBallDefenseContext(teamBB), Is.False);
            Assert.That(GoapFieldNpcPerspective.IsFreeBallContext(teamBB), Is.True);
        }
        finally
        {
            Object.DestroyImmediate(teamGo);
        }
    }

    [Test]
    public void FreeBall_LastEnemyPossession_RemainsDefenseContext()
    {
        var teamGo = new GameObject("teamBB");
        var teamBB = teamGo.AddComponent<TeamBlackboard>();
        teamBB.FieldInfo.Initialize(100f, 60f);
        teamBB.BallInfo.setExistBall();
        teamBB.BallInfo.updateBallID(1005, BallManager_State.BELONG_TEAM.ENEMY, Vector3.zero);
        teamBB.BallInfo.updateBallState(BallManager_State.BALL_STATE.HOLD);
        teamBB.BallInfo.updateBallID(-1, BallManager_State.BELONG_TEAM.FREE, Vector3.forward);
        teamBB.BallInfo.updateBallState(BallManager_State.BALL_STATE.FREE);

        try
        {
            Assert.That(GoapFieldNpcPerspective.IsOpponentBallDefenseContext(teamBB), Is.True);
            Assert.That(GoapFieldNpcPerspective.IsTeamBallAttackContext(teamBB), Is.False);
            Assert.That(GoapFieldNpcPerspective.IsFreeBallContext(teamBB), Is.True);
        }
        finally
        {
            Object.DestroyImmediate(teamGo);
        }
    }

    [Test]
    public void FreeBallRecovery_IsAchievable_WhenNearBallAsChaseLeader()
    {
        var teamGo = new GameObject("teamRoot");
        var teamFacade = teamGo.AddComponent<TeamFacade>();
        var teamBB = teamGo.AddComponent<TeamBlackboard>();
        BindTeamFacadeSingleton(teamFacade, teamBB);
        teamBB.FieldInfo.Initialize(100f, 60f);
        teamBB.BallInfo.setExistBall();
        teamBB.BallInfo.updateBallID(-1, BallManager_State.BELONG_TEAM.FREE, Vector3.zero);
        teamBB.BallInfo.updateBallState(BallManager_State.BALL_STATE.FREE);
        teamBB.BallInfo.updateBallPhysics(Vector3.zero, Vector3.zero);

        var humanGo = new GameObject("human");
        humanGo.AddComponent<AnimalFacade>();
        humanGo.AddComponent<AnimalControlAssignment>().SetRole(AnimalControlRole.Human);
        var humanBbGo = new GameObject("bb");
        humanBbGo.transform.SetParent(humanGo.transform, false);
        var humanBb = humanBbGo.AddComponent<PlayerBlackboard>();
        humanBb.BasicData.init(humanBbGo);
        humanBb.PhysicalState.updatePhysicalInfo(Vector3.forward * 20f, Vector3.zero);
        humanBb.BallState.updateBallInfo(false, 20f, Vector3.zero);
        humanBb.SetFact(new Fact(SymbolTag.Action.CAN_MOVE, "true"), true);
        humanBb.SetFact(new Fact(SymbolTag.Position.NEAR_BALL, "true"), true);

        var goal = ScriptableObject.CreateInstance<Game.Goap.Goals.FreeBallRecoveryGoalSO>();
        bool roleDiffEnabled = TeammateNpcGoapRoleDifferentiation.Enabled;
        TeammateNpcGoapRoleDifferentiation.Enabled = false;

        try
        {
            Assert.That(goal.IsAchievable(humanBb), Is.True);
        }
        finally
        {
            TeammateNpcGoapRoleDifferentiation.Enabled = roleDiffEnabled;
            ClearTeamFacadeSingleton();
            Object.DestroyImmediate(goal);
            Object.DestroyImmediate(humanGo);
            Object.DestroyImmediate(teamGo);
        }
    }

    [Test]
    public void MirroredContext_EnemyTeamBall_IsAttackForEnemyNpc_DefenseForAlly()
    {
        var teamGo = new GameObject("teamBB");
        var teamBB = teamGo.AddComponent<TeamBlackboard>();
        teamBB.FieldInfo.Initialize(100f, 60f);
        teamBB.BallInfo.setExistBall();
        teamBB.BallInfo.updateBallID(1005, BallManager_State.BELONG_TEAM.ENEMY, Vector3.zero);
        teamBB.BallInfo.updateBallState(BallManager_State.BALL_STATE.HOLD);

        var enemyGo = new GameObject("enemy");
        var enemyAssignment = enemyGo.AddComponent<AnimalControlAssignment>();
        enemyGo.AddComponent<AnimalFacade>();
        enemyAssignment.SetRole(AnimalControlRole.EnemyFieldNpc);
        var enemyBbGo = new GameObject("PlayerBlackboard");
        enemyBbGo.transform.SetParent(enemyGo.transform, false);
        var enemyBb = enemyBbGo.AddComponent<PlayerBlackboard>();
        enemyBb.BasicData.init(enemyBbGo);

        var allyGo = new GameObject("ally");
        var allyAssignment = allyGo.AddComponent<AnimalControlAssignment>();
        allyGo.AddComponent<AnimalFacade>();
        allyAssignment.SetRole(AnimalControlRole.TeammateNpc);
        var allyBbGo = new GameObject("PlayerBlackboard");
        allyBbGo.transform.SetParent(allyGo.transform, false);
        var allyBb = allyBbGo.AddComponent<PlayerBlackboard>();
        allyBb.BasicData.init(allyBbGo);

        try
        {
            Assert.That(GoapFieldNpcPerspective.IsTeamBallAttackContext(teamBB, enemyBb), Is.True);
            Assert.That(GoapFieldNpcPerspective.IsTeamBallAttackContext(teamBB, allyBb), Is.False);
            Assert.That(TeammateNpcDefensePlanning.IsEnemyBallDefenseContext(teamBB, allyBb), Is.True);
            Assert.That(TeammateNpcDefensePlanning.IsEnemyBallDefenseContext(teamBB, enemyBb), Is.False);
        }
        finally
        {
            Object.DestroyImmediate(enemyGo);
            Object.DestroyImmediate(allyGo);
            Object.DestroyImmediate(teamGo);
        }
    }

    [Test]
    public void TeamBallSupportGoal_IsAchievable_ForOffBallEnemyWhenEnemyTeamHasBall()
    {
        var teamGo = new GameObject("teamRoot");
        var teamFacade = teamGo.AddComponent<TeamFacade>();
        var teamBB = teamGo.AddComponent<TeamBlackboard>();
        BindTeamFacadeSingleton(teamFacade, teamBB);
        teamBB.FieldInfo.Initialize(100f, 60f);
        teamBB.BallInfo.setExistBall();
        teamBB.BallInfo.updateBallID(1005, BallManager_State.BELONG_TEAM.ENEMY, Vector3.zero);
        teamBB.BallInfo.updateBallState(BallManager_State.BALL_STATE.HOLD);

        var enemyGo = new GameObject("enemy-sub");
        var enemyAssignment = enemyGo.AddComponent<AnimalControlAssignment>();
        enemyGo.AddComponent<AnimalFacade>();
        enemyGo.AddComponent<AnimalFormationSlot>().Initialize(1);
        enemyAssignment.SetRole(AnimalControlRole.EnemyFieldNpc);
        var enemyBbGo = new GameObject("PlayerBlackboard");
        enemyBbGo.transform.SetParent(enemyGo.transform, false);
        var enemyBb = enemyBbGo.AddComponent<PlayerBlackboard>();
        enemyBb.BasicData.init(enemyBbGo);
        enemyBb.ActionState.init();
        enemyBb.SetFact(new Fact(SymbolTag.Action.CAN_MOVE, "true"), true);

        var goal = ScriptableObject.CreateInstance<Game.Goap.Goals.TeamBallSupportGoalSO>();
        bool roleDiffEnabled = TeammateNpcGoapRoleDifferentiation.Enabled;
        TeammateNpcGoapRoleDifferentiation.Enabled = true;

        try
        {
            Assert.That(goal.IsAchievable(enemyBb), Is.True);
        }
        finally
        {
            TeammateNpcGoapRoleDifferentiation.Enabled = roleDiffEnabled;
            ClearTeamFacadeSingleton();
            Object.DestroyImmediate(goal);
            Object.DestroyImmediate(enemyGo);
            Object.DestroyImmediate(teamGo);
        }
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

    [Test]
    public void GetAttackGoalPosition_ReturnsOwnGoalWhenMirrored()
    {
        var teamGo = new GameObject("teamBB");
        var teamBB = teamGo.AddComponent<TeamBlackboard>();
        teamBB.FieldInfo.Initialize(100f, 60f);

        try
        {
            Assert.That(
                GoapFieldNpcPerspective.GetAttackGoalPosition(teamBB, mirrored: true),
                Is.EqualTo(teamBB.FieldInfo.OwnGoalPosition));
            Assert.That(
                GoapFieldNpcPerspective.GetAttackGoalPosition(teamBB, mirrored: false),
                Is.EqualTo(teamBB.FieldInfo.EnemyGoalPosition));
        }
        finally
        {
            Object.DestroyImmediate(teamGo);
        }
    }

    [Test]
    public void IsEnemyFieldNpc_DetectsAssignmentRole()
    {
        var enemyGo = new GameObject("enemy");
        var assignment = enemyGo.AddComponent<AnimalControlAssignment>();
        var facade = enemyGo.AddComponent<AnimalFacade>();
        assignment.SetRole(AnimalControlRole.EnemyFieldNpc);

        try
        {
            Assert.That(GoapFieldNpcPerspective.IsEnemyFieldNpc(facade), Is.True);
        }
        finally
        {
            Object.DestroyImmediate(enemyGo);
        }
    }

    [Test]
    public void IsOpponentBallDefenseContext_TrueDuringOpponentPassTransition()
    {
        var teamGo = new GameObject("teamBB");
        var teamBB = teamGo.AddComponent<TeamBlackboard>();
        teamBB.FieldInfo.Initialize(100f, 60f);
        teamBB.BallInfo.setExistBall();
        teamBB.BallInfo.updateBallID(1001, BallManager_State.BELONG_TEAM.PLAYER, Vector3.zero);
        teamBB.BallInfo.updateBallID(-1, BallManager_State.BELONG_TEAM.FREE, Vector3.zero);
        teamBB.BallInfo.updateBallState(BallManager_State.BALL_STATE.PASS);

        var enemyGo = new GameObject("enemy");
        var enemyAssignment = enemyGo.AddComponent<AnimalControlAssignment>();
        enemyGo.AddComponent<AnimalFacade>();
        enemyAssignment.SetRole(AnimalControlRole.EnemyFieldNpc);
        var enemyBbGo = new GameObject("PlayerBlackboard");
        enemyBbGo.transform.SetParent(enemyGo.transform, false);
        var enemyBb = enemyBbGo.AddComponent<PlayerBlackboard>();
        enemyBb.BasicData.init(enemyBbGo);

        var allyGo = new GameObject("ally");
        var allyAssignment = allyGo.AddComponent<AnimalControlAssignment>();
        allyGo.AddComponent<AnimalFacade>();
        allyAssignment.SetRole(AnimalControlRole.TeammateNpc);
        var allyBbGo = new GameObject("PlayerBlackboard");
        allyBbGo.transform.SetParent(allyGo.transform, false);
        var allyBb = allyBbGo.AddComponent<PlayerBlackboard>();
        allyBb.BasicData.init(allyBbGo);

        try
        {
            Assert.That(GoapFieldNpcPerspective.IsOpponentBallDefenseContext(teamBB, enemyBb), Is.True);
            Assert.That(GoapFieldNpcPerspective.IsOpponentBallDefenseContext(teamBB, allyBb), Is.False);
            Assert.That(GoapFieldNpcPerspective.IsTeamBallAttackContext(teamBB, allyBb), Is.True);
            Assert.That(GoapFieldNpcPerspective.IsTeamBallAttackContext(teamBB, enemyBb), Is.False);
        }
        finally
        {
            Object.DestroyImmediate(enemyGo);
            Object.DestroyImmediate(allyGo);
            Object.DestroyImmediate(teamGo);
        }
    }
}
#endif
