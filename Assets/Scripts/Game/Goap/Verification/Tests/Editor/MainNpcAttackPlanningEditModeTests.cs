#if UNITY_EDITOR
using System.Reflection;
using Game.Goap;
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
    public void EstimateCosts_ProductionGoalMouthAtZ13_PrefersShoot()
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

        Assert.That(shootCost, Is.LessThan(passCost));
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
    public void EstimateCosts_InShootingRangeLightPressure_PrefersShootOverClearPass()
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

        Assert.That(shootCost, Is.LessThan(passCost));
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
}
#endif
