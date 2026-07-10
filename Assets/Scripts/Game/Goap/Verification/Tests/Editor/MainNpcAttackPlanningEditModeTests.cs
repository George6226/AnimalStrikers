#if UNITY_EDITOR
using System.Collections.Generic;
using System.Reflection;
using Game.Goap;
using Game.Goap.Goals;
using NUnit.Framework;
using UnityEngine;

public sealed class MainNpcAttackPlanningEditModeTests
{
    private const float MaxShootDistance = 55f;
    private const float ProductionFieldMaxShootDistance = 40f * 0.55f;

    [Test]
    public void IsWithinVeryNearGoalShootZone_UsesThirtyTwoPercentOfMaxRange()
    {
        Assert.That(
            MainNpcAttackPlanning.IsWithinVeryNearGoalShootZone(12f, MaxShootDistance),
            Is.True);
        Assert.That(
            MainNpcAttackPlanning.IsWithinVeryNearGoalShootZone(18f, MaxShootDistance),
            Is.False);
    }

    [Test]
    public void EstimateCosts_ProductionGoalMouthAtZ13_PrefersShootWhenLaneClear()
    {
        float passCost = MainNpcAttackPlanning.EstimatePassCost(
            goalDistance: 6.7f,
            maxShootDistance: ProductionFieldMaxShootDistance,
            pressureCount: 2,
            passRouteClear: true);
        float shootCost = MainNpcAttackPlanning.EstimateShootCost(
            goalDistance: 6.7f,
            maxShootDistance: ProductionFieldMaxShootDistance,
            pressureCount: 2,
            shotLaneClear: true);

        Assert.That(shootCost, Is.LessThan(passCost));
    }

    [Test]
    public void EstimateCosts_BlockedLaneNearGoal_PrefersPassOverShoot()
    {
        float passCost = MainNpcAttackPlanning.EstimatePassCost(
            goalDistance: 6.7f,
            maxShootDistance: ProductionFieldMaxShootDistance,
            pressureCount: 2,
            passRouteClear: true);
        float shootCost = MainNpcAttackPlanning.EstimateShootCost(
            goalDistance: 6.7f,
            maxShootDistance: ProductionFieldMaxShootDistance,
            pressureCount: 2,
            shotLaneClear: false);

        Assert.That(passCost, Is.LessThan(shootCost));
    }

    [Test]
    public void EstimateCosts_VeryNearGoalUnderPressure_PrefersShoot()
    {
        float passCost = MainNpcAttackPlanning.EstimatePassCost(
            goalDistance: 10f,
            maxShootDistance: MaxShootDistance,
            pressureCount: 2,
            passRouteClear: true);
        float shootCost = MainNpcAttackPlanning.EstimateShootCost(
            goalDistance: 10f,
            maxShootDistance: MaxShootDistance,
            pressureCount: 2,
            shotLaneClear: true);

        Assert.That(shootCost, Is.LessThan(passCost));
    }

    [Test]
    public void EstimateCosts_MidRangeUnderPressure_PrefersPass()
    {
        float passCost = MainNpcAttackPlanning.EstimatePassCost(
            goalDistance: 48f,
            maxShootDistance: MaxShootDistance,
            pressureCount: 2,
            passRouteClear: true);
        float shootCost = MainNpcAttackPlanning.EstimateShootCost(
            goalDistance: 48f,
            maxShootDistance: MaxShootDistance,
            pressureCount: 2,
            shotLaneClear: true);

        Assert.That(passCost, Is.LessThan(shootCost));
    }

    [Test]
    public void EstimateCosts_InShootingRangeLightPressure_PrefersShootWhenLaneClear()
    {
        float passCost = MainNpcAttackPlanning.EstimatePassCost(
            goalDistance: 14f,
            maxShootDistance: ProductionFieldMaxShootDistance,
            pressureCount: 1,
            passRouteClear: true);
        float shootCost = MainNpcAttackPlanning.EstimateShootCost(
            goalDistance: 14f,
            maxShootDistance: ProductionFieldMaxShootDistance,
            pressureCount: 1,
            shotLaneClear: true);

        Assert.That(shootCost, Is.LessThan(passCost));
    }

    [Test]
    public void EstimateCosts_InShootingRangeBlockedLane_PrefersPassOverShoot()
    {
        float passCost = MainNpcAttackPlanning.EstimatePassCost(
            goalDistance: 14f,
            maxShootDistance: ProductionFieldMaxShootDistance,
            pressureCount: 1,
            passRouteClear: true);
        float shootCost = MainNpcAttackPlanning.EstimateShootCost(
            goalDistance: 14f,
            maxShootDistance: ProductionFieldMaxShootDistance,
            pressureCount: 1,
            shotLaneClear: false);

        Assert.That(passCost, Is.LessThan(shootCost));
    }

    [Test]
    public void IsShotRouteClear_TeammateBetweenShooterAndGoal_ReturnsFalse()
    {
        Vector3 shooter = Vector3.zero;
        Vector3 goal = new Vector3(0f, 0f, 20f);
        Vector3 blocker = new Vector3(0f, 0f, 10f);

        Assert.That(
            PlayerBlackboardCalculator.IsShotRouteClear(shooter, goal, new[] { blocker }, blockingRange: 2f),
            Is.False);
    }

    [Test]
    public void IsShotRouteClear_GkBetweenShooterAndGoal_IsIgnoredWhenNotInBlockerList()
    {
        Vector3 shooter = Vector3.zero;
        Vector3 goal = new Vector3(0f, 0f, 20f);

        Assert.That(
            PlayerBlackboardCalculator.IsShotRouteClear(shooter, goal, System.Array.Empty<Vector3>(), blockingRange: 2f),
            Is.True);
    }

    [Test]
    public void IsShotRouteClear_BlockerBesideLane_DoesNotBlock()
    {
        Vector3 shooter = Vector3.zero;
        Vector3 goal = new Vector3(0f, 0f, 20f);
        Vector3 besideLane = new Vector3(5f, 0f, 10f);

        Assert.That(
            PlayerBlackboardCalculator.IsShotRouteClear(shooter, goal, new[] { besideLane }, blockingRange: 2f),
            Is.True);
    }

    [Test]
    public void EstimateShootCost_BlockedLane_IsMoreExpensiveThanClearLane()
    {
        float clearLane = MainNpcAttackPlanning.EstimateShootCost(
            goalDistance: 10f,
            maxShootDistance: MaxShootDistance,
            pressureCount: 0,
            shotLaneClear: true);
        float blockedLane = MainNpcAttackPlanning.EstimateShootCost(
            goalDistance: 10f,
            maxShootDistance: MaxShootDistance,
            pressureCount: 0,
            shotLaneClear: false);

        Assert.That(blockedLane, Is.GreaterThan(clearLane));
    }

    [Test]
    public void EstimatePassCost_ClearRouteUnderPressure_IsCheaperThanWithoutBonus()
    {
        float withClearRoute = MainNpcAttackPlanning.EstimatePassCost(
            goalDistance: 30f,
            maxShootDistance: MaxShootDistance,
            pressureCount: 2,
            passRouteClear: true);
        float withoutClearRoute = MainNpcAttackPlanning.EstimatePassCost(
            goalDistance: 30f,
            maxShootDistance: MaxShootDistance,
            pressureCount: 2,
            passRouteClear: false);

        Assert.That(withClearRoute, Is.LessThan(withoutClearRoute));
    }

    [Test]
    public void IsSelfBallOwner_TrueWhenTeamBlackboardOwnerMatchesBeforeHasBallFact()
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
        teamBb.BallInfo.setExistBall();
        teamBb.BallInfo.updateBallID(1007, BallManager_State.BELONG_TEAM.ENEMY, Vector3.zero);
        teamBb.BallInfo.updateBallState(BallManager_State.BALL_STATE.HOLD);

        var enemyGo = new GameObject("enemy");
        enemyGo.AddComponent<AnimalFacade>();
        enemyGo.AddComponent<AnimalControlAssignment>().SetRole(AnimalControlRole.EnemyFieldNpc);
        var bbGo = new GameObject("bb");
        bbGo.transform.SetParent(enemyGo.transform, false);
        var bb = bbGo.AddComponent<PlayerBlackboard>();
        bb.BasicData.init(enemyGo);
        typeof(PlayerBasicData).GetProperty("PlayerID")!
            .SetValue(bb.BasicData, 1007, null);

        try
        {
            Assert.That(bb.GetFact(new Fact(SymbolTag.Basic.HAS_BALL, "true")), Is.Not.EqualTo(true));
            Assert.That(MainNpcAttackPlanning.IsSelfBallOwner(bb), Is.True);
            Assert.That(MainNpcAttackPlanning.IsBallPossessionAttackContext(bb), Is.True);
        }
        finally
        {
            SetStaticField(typeof(TeamFacade), "_instance", null);
            Object.DestroyImmediate(enemyGo);
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void EstimateCosts_MidRangeFarFromGoal_PrefersDribbleOverPassWhenShootUnavailable()
    {
        float passCost = MainNpcAttackPlanning.EstimatePassCost(
            goalDistance: 48f,
            maxShootDistance: MaxShootDistance,
            pressureCount: 0,
            passRouteClear: true);
        float dribbleCost = MainNpcAttackPlanning.EstimateDribbleCost(
            goalDistance: 48f,
            maxShootDistance: MaxShootDistance,
            pressureCount: 0);

        Assert.That(dribbleCost, Is.LessThan(passCost));
    }

    [Test]
    public void EstimateCosts_InShootingRange_DribbleIsMoreExpensiveThanShoot()
    {
        float dribbleCost = MainNpcAttackPlanning.EstimateDribbleCost(
            goalDistance: 14f,
            maxShootDistance: ProductionFieldMaxShootDistance,
            pressureCount: 0);
        float shootCost = MainNpcAttackPlanning.EstimateShootCost(
            goalDistance: 14f,
            maxShootDistance: ProductionFieldMaxShootDistance,
            pressureCount: 0,
            shotLaneClear: true);

        Assert.That(shootCost, Is.LessThan(dribbleCost));
    }

    [Test]
    public void CanDribbleTowardGoal_AllowedInShootingRangeWhenShotLaneBlocked()
    {
        var fixture = CreateKickoffShotLaneFixture(
            blockerOnLane: true,
            out PlayerBlackboard enemyBb,
            out _);

        try
        {
            enemyBb.SetFact(new Fact(SymbolTag.Action.CAN_MOVE, "true"), true);

            Assert.That(MainNpcAttackPlanning.CanShootAtGoal(enemyBb), Is.True);
            Assert.That(MainNpcAttackPlanning.IsShotLaneClear(
                enemyBb,
                enemyBb.PhysicalState.Position,
                new Vector3(0f, 0f, -20f),
                blockingRange: 3.2f), Is.False);
            Assert.That(MainNpcAttackPlanning.CanDribbleTowardGoal(enemyBb), Is.True);

            float dribbleCost = MainNpcAttackPlanning.EstimateDribbleCost(
                goalDistance: 19f,
                maxShootDistance: ProductionFieldMaxShootDistance,
                pressureCount: 0);
            float shootCost = MainNpcAttackPlanning.ComputeShootCostAdjustment(enemyBb)
                + MainNpcAttackPlanning.DefaultShootBaseCost;

            Assert.That(dribbleCost, Is.LessThan(shootCost));
        }
        finally
        {
            fixture.Dispose();
        }
    }

    [Test]
    public void CanDribbleTowardGoal_DisallowedInShootingRangeWhenShotLaneClear()
    {
        var fixture = CreateKickoffShotLaneFixture(
            blockerOnLane: false,
            out PlayerBlackboard enemyBb,
            out _);

        try
        {
            enemyBb.SetFact(new Fact(SymbolTag.Action.CAN_MOVE, "true"), true);

            Assert.That(MainNpcAttackPlanning.CanShootAtGoal(enemyBb), Is.True);
            Assert.That(MainNpcAttackPlanning.CanDribbleTowardGoal(enemyBb), Is.False);
        }
        finally
        {
            fixture.Dispose();
        }
    }

    [Test]
    public void ComputeShootCostAdjustment_EnemyBlockedLane_OmitsEnemyShootDiscount()
    {
        float blockedAdj;
        var blockedFixture = CreateKickoffShotLaneFixture(
            blockerOnLane: true,
            out PlayerBlackboard blockedEnemyBb,
            out _);
        try
        {
            blockedAdj = MainNpcAttackPlanning.ComputeShootCostAdjustment(blockedEnemyBb);
        }
        finally
        {
            blockedFixture.Dispose();
        }

        float clearAdj;
        var clearFixture = CreateKickoffShotLaneFixture(
            blockerOnLane: false,
            out PlayerBlackboard clearEnemyBb,
            out _);
        try
        {
            clearAdj = MainNpcAttackPlanning.ComputeShootCostAdjustment(clearEnemyBb);
        }
        finally
        {
            clearFixture.Dispose();
        }

        Assert.That(blockedAdj, Is.GreaterThanOrEqualTo(0.85f));
        Assert.That(blockedAdj, Is.GreaterThan(clearAdj));
    }

    private sealed class KickoffShotLaneFixture : System.IDisposable
    {
        private readonly GameObject _root;

        public KickoffShotLaneFixture(GameObject root)
        {
            _root = root;
        }

        public void Dispose()
        {
            SetStaticField(typeof(TeamFacade), "_instance", null);
            if (_root != null)
            {
                Object.DestroyImmediate(_root);
            }
        }
    }

    private static KickoffShotLaneFixture CreateKickoffShotLaneFixture(
        bool blockerOnLane,
        out PlayerBlackboard enemyBb,
        out AnimalFacade allyFacade)
    {
        var root = new GameObject("kickoffShotLane");
        var teamFacade = root.AddComponent<TeamFacade>();
        var teamBb = root.AddComponent<TeamBlackboard>();
        var regist = root.AddComponent<TeamRegistar>();
        SetPrivateField(teamFacade, "_teamBlackboard", teamBb);
        SetPrivateField(teamFacade, "_teamRegistar", regist);
        SetStaticField(typeof(TeamFacade), "_instance", teamFacade);

        teamBb.FieldInfo.Initialize(40f, 20f);
        teamBb.BallInfo.Initialize();
        teamBb.BallInfo.setExistBall();

        Vector3 enemyPos = new Vector3(0f, 0f, -1f);
        Vector3 blockerPos = blockerOnLane
            ? new Vector3(0f, 0f, -10f)
            : new Vector3(6f, 0f, -10f);

        var enemyGo = new GameObject("enemy");
        enemyGo.transform.position = enemyPos;
        enemyGo.AddComponent<AnimalFacade>();
        enemyGo.AddComponent<AnimalControlAssignment>().SetRole(AnimalControlRole.EnemyFieldNpc);
        var enemyBbGo = new GameObject("enemyBb");
        enemyBbGo.transform.SetParent(enemyGo.transform, false);
        enemyBb = enemyBbGo.AddComponent<PlayerBlackboard>();
        enemyBb.BasicData.init(enemyGo);
        enemyBb.PhysicalState.updatePhysicalInfo(enemyPos, Vector3.zero);
        typeof(PlayerBasicData).GetProperty("PlayerID")!
            .SetValue(enemyBb.BasicData, 1005, null);
        AddToTeamList(regist, "_enemyList", enemyGo.GetComponent<AnimalFacade>());

        var allyGo = new GameObject("ally");
        allyGo.transform.position = blockerPos;
        allyFacade = allyGo.AddComponent<AnimalFacade>();
        allyGo.AddComponent<AnimalControlAssignment>().SetRole(AnimalControlRole.TeammateNpc);
        AddToTeamList(regist, "_allyList", allyFacade);

        teamBb.BallInfo.updateBallID(1005, BallManager_State.BELONG_TEAM.ENEMY, enemyPos);
        teamBb.BallInfo.updateBallState(BallManager_State.BALL_STATE.HOLD);

        return new KickoffShotLaneFixture(root);
    }

    [Test]
    public void IsBallPossessionAttackContext_TrueWhenHasBallFactBeforeTeamBoardSync()
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
        teamBb.BallInfo.setExistBall();
        teamBb.BallInfo.updateBallID(-1, BallManager_State.BELONG_TEAM.FREE, Vector3.zero);
        teamBb.BallInfo.updateBallState(BallManager_State.BALL_STATE.FREE);

        var allyGo = new GameObject("ally");
        allyGo.AddComponent<AnimalFacade>();
        allyGo.AddComponent<AnimalControlAssignment>().SetRole(AnimalControlRole.Human);
        var bbGo = new GameObject("bb");
        bbGo.transform.SetParent(allyGo.transform, false);
        var bb = bbGo.AddComponent<PlayerBlackboard>();
        bb.BasicData.init(allyGo);
        typeof(PlayerBasicData).GetProperty("PlayerID")!
            .SetValue(bb.BasicData, 1001, null);
        bb.SetFact(new Fact(SymbolTag.Basic.HAS_BALL, "true"), true);
        bb.SetFact(new Fact(SymbolTag.Action.CAN_MOVE, "true"), true);
        bb.PhysicalState.updatePhysicalInfo(new Vector3(0f, 0f, -10f), Vector3.zero);

        try
        {
            Assert.That(MainNpcAttackPlanning.IsBallPossessionAttackContext(bb), Is.True);
            Assert.That(MainNpcAttackPlanning.IsActivelyHoldingBall(bb), Is.False);
            var attackGoal = ScriptableObject.CreateInstance<BallPossessionAttackGoalSO>();
            Assert.That(attackGoal.IsAchievable(bb), Is.False);
        }
        finally
        {
            SetStaticField(typeof(TeamFacade), "_instance", null);
            Object.DestroyImmediate(allyGo);
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void CanExecuteShootAtGoal_FalseWhenShootInProgress()
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
        teamBb.BallInfo.setExistBall();
        teamBb.BallInfo.updateBallID(1001, BallManager_State.BELONG_TEAM.PLAYER, new Vector3(0f, 0f, 6f));
        teamBb.BallInfo.updateBallState(BallManager_State.BALL_STATE.HOLD);

        var allyGo = new GameObject("ally");
        allyGo.AddComponent<AnimalFacade>();
        allyGo.AddComponent<AnimalControlAssignment>().SetRole(AnimalControlRole.Human);
        var shoot = allyGo.AddComponent<AnimalAction_Shoot>();
        var host = allyGo.AddComponent<CoroutineHost>();
        var bbGo = new GameObject("bb");
        bbGo.transform.SetParent(allyGo.transform, false);
        var bb = bbGo.AddComponent<PlayerBlackboard>();
        bb.BasicData.init(allyGo);
        typeof(PlayerBasicData).GetProperty("PlayerID")!
            .SetValue(bb.BasicData, 1001, null);
        bb.SetFact(new Fact(SymbolTag.Basic.HAS_BALL, "true"), true);
        bb.SetFact(new Fact(SymbolTag.Action.CAN_MOVE, "true"), true);
        bb.PhysicalState.updatePhysicalInfo(new Vector3(0f, 0f, 6f), Vector3.zero);

        var actions = new List<GoapActionSO>
        {
            ScriptableObject.CreateInstance<DribbleTowardGoalActionSO>(),
            ScriptableObject.CreateInstance<PassToTeammateActionSO>(),
            ScriptableObject.CreateInstance<ShootAtGoalActionSO>(),
        };

        try
        {
            SetPrivateField(shoot, "_shootCoroutine", host.StartCoroutine(CoroutineHost.DummyWait()));
            Assert.That(MainNpcAttackPlanning.CanShootAtGoal(bb), Is.True);
            Assert.That(MainNpcAttackPlanning.CanExecuteShootAtGoal(bb), Is.False);
            Assert.That(
                MainNpcAttackPlanning.TryBuildForcedAttackPlan(bb, actions, out var plan),
                Is.True);
            Assert.That(plan!.Peek(), Is.InstanceOf<DribbleTowardGoalActionSO>());
        }
        finally
        {
            foreach (var action in actions)
            {
                Object.DestroyImmediate(action);
            }

            SetStaticField(typeof(TeamFacade), "_instance", null);
            Object.DestroyImmediate(allyGo);
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void TryBuildForcedAttackPlan_ExcludeShoot_SkipsShootEvenWhenExecutable()
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
        teamBb.BallInfo.setExistBall();
        teamBb.BallInfo.updateBallID(1001, BallManager_State.BELONG_TEAM.PLAYER, new Vector3(0f, 0f, 6f));
        teamBb.BallInfo.updateBallState(BallManager_State.BALL_STATE.HOLD);

        var allyGo = new GameObject("ally");
        allyGo.AddComponent<AnimalFacade>();
        allyGo.AddComponent<AnimalControlAssignment>().SetRole(AnimalControlRole.Human);
        var bbGo = new GameObject("bb");
        bbGo.transform.SetParent(allyGo.transform, false);
        var bb = bbGo.AddComponent<PlayerBlackboard>();
        bb.BasicData.init(allyGo);
        typeof(PlayerBasicData).GetProperty("PlayerID")!
            .SetValue(bb.BasicData, 1001, null);
        bb.SetFact(new Fact(SymbolTag.Basic.HAS_BALL, "true"), true);
        bb.SetFact(new Fact(SymbolTag.Action.CAN_MOVE, "true"), true);
        bb.PhysicalState.updatePhysicalInfo(new Vector3(0f, 0f, 6f), Vector3.zero);

        var actions = new List<GoapActionSO>
        {
            ScriptableObject.CreateInstance<DribbleTowardGoalActionSO>(),
            ScriptableObject.CreateInstance<PassToTeammateActionSO>(),
            ScriptableObject.CreateInstance<ShootAtGoalActionSO>(),
        };

        try
        {
            Assert.That(MainNpcAttackPlanning.CanExecuteShootAtGoal(bb), Is.True);
            Assert.That(
                MainNpcAttackPlanning.TryBuildForcedAttackPlan(
                    bb,
                    actions,
                    out var plan,
                    excludeShoot: true),
                Is.True);
            Assert.That(plan!.Peek(), Is.Not.InstanceOf<ShootAtGoalActionSO>());
        }
        finally
        {
            foreach (var action in actions)
            {
                Object.DestroyImmediate(action);
            }

            SetStaticField(typeof(TeamFacade), "_instance", null);
            Object.DestroyImmediate(allyGo);
            Object.DestroyImmediate(root);
        }
    }

    [Test]
    public void TryBuildForcedAttackPlan_FallsBackToDribbleWhenOnlyForceDribbleAvailable()
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
        teamBb.BallInfo.setExistBall();
        teamBb.BallInfo.updateBallID(1001, BallManager_State.BELONG_TEAM.PLAYER, new Vector3(0f, 0f, -6f));
        teamBb.BallInfo.updateBallState(BallManager_State.BALL_STATE.HOLD);

        var allyGo = new GameObject("ally");
        allyGo.AddComponent<AnimalFacade>();
        allyGo.AddComponent<AnimalControlAssignment>().SetRole(AnimalControlRole.Human);
        var bbGo = new GameObject("bb");
        bbGo.transform.SetParent(allyGo.transform, false);
        var bb = bbGo.AddComponent<PlayerBlackboard>();
        bb.BasicData.init(allyGo);
        typeof(PlayerBasicData).GetProperty("PlayerID")!
            .SetValue(bb.BasicData, 1001, null);
        bb.SetFact(new Fact(SymbolTag.Basic.HAS_BALL, "true"), true);
        bb.SetFact(new Fact(SymbolTag.Action.CAN_MOVE, "true"), true);
        bb.PhysicalState.updatePhysicalInfo(new Vector3(0f, 0f, 8f), Vector3.zero);

        var actions = new List<GoapActionSO>
        {
            ScriptableObject.CreateInstance<DribbleTowardGoalActionSO>(),
            ScriptableObject.CreateInstance<PassToTeammateActionSO>(),
            ScriptableObject.CreateInstance<ShootAtGoalActionSO>(),
        };

        try
        {
            Assert.That(MainNpcAttackPlanning.CanDribbleTowardGoal(bb), Is.False);
            Assert.That(MainNpcAttackPlanning.CanForceDribbleWhileHolding(bb), Is.True);
            Assert.That(MainNpcAttackPlanning.CanExecuteDribbleTowardGoal(bb), Is.True);
            Assert.That(
                MainNpcAttackPlanning.TryBuildForcedAttackPlan(bb, actions, out var plan),
                Is.True);
            Assert.That(plan, Is.Not.Null);
            Assert.That(plan!.Count, Is.EqualTo(1));
        }
        finally
        {
            foreach (var action in actions)
            {
                Object.DestroyImmediate(action);
            }

            SetStaticField(typeof(TeamFacade), "_instance", null);
            Object.DestroyImmediate(allyGo);
            Object.DestroyImmediate(root);
        }
    }

    private static void AddToTeamList(TeamRegistar regist, string fieldName, AnimalFacade facade)
    {
        var field = typeof(TeamRegistar).GetField(
            fieldName,
            BindingFlags.Instance | BindingFlags.NonPublic);
        if (field?.GetValue(regist) is List<AnimalFacade> list)
        {
            list.Add(facade);
        }
    }

    private static void SetPrivateField(object target, string fieldName, object value)
    {
        var field = target.GetType().GetField(
            fieldName,
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        field?.SetValue(target, value);
    }

    private static void SetStaticField(System.Type type, string fieldName, object value)
    {
        var field = type.GetField(
            fieldName,
            System.Reflection.BindingFlags.Static | System.Reflection.BindingFlags.NonPublic);
        field?.SetValue(null, value);
    }

    private sealed class CoroutineHost : MonoBehaviour
    {
        public static System.Collections.IEnumerator DummyWait()
        {
            yield return null;
        }
    }
}
#endif
