#if UNITY_EDITOR
using NUnit.Framework;

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
            goalDistance: 30f,
            maxShootDistance: MaxShootDistance,
            pressureCount: 2,
            passRouteClear: true);
        float shootCost = MainNpcAttackPlanning.EstimateShootCost(
            goalDistance: 30f,
            maxShootDistance: MaxShootDistance,
            pressureCount: 2,
            shotLaneClear: true);

        Assert.That(passCost, Is.LessThan(shootCost));
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
}
#endif
