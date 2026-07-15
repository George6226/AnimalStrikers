#if UNITY_EDITOR
using NUnit.Framework;

public sealed class EnemyAiBalanceEditModeTests
{
    [TearDown]
    public void TearDown()
    {
        EnemyAiBalance.Apply(EnemyAiDifficulty.Normal);
    }

    [Test]
    public void Apply_Normal_MatchesLegacyEnemyBias()
    {
        EnemyAiBalance.Apply(EnemyAiDifficulty.Normal);

        Assert.That(EnemyAiBalance.Difficulty, Is.EqualTo(EnemyAiDifficulty.Normal));
        Assert.That(EnemyAiBalance.PassPenalty, Is.EqualTo(EnemyAiBalance.NormalPassPenalty).Within(0.001f));
        Assert.That(
            EnemyAiBalance.ShootDiscount,
            Is.EqualTo(EnemyAiBalance.NormalShootDiscount).Within(0.001f));
        Assert.That(
            EnemyAiBalance.PlanningIntervalSeconds,
            Is.EqualTo(EnemyAiBalance.NormalPlanningIntervalSeconds).Within(0.001f));
    }

    [Test]
    public void Apply_Easy_WeakensShootBiasAndSlowsPlanning()
    {
        EnemyAiBalance.Apply(EnemyAiDifficulty.Easy);

        Assert.That(EnemyAiBalance.PassPenalty, Is.LessThan(EnemyAiBalance.NormalPassPenalty));
        Assert.That(EnemyAiBalance.ShootDiscount, Is.LessThan(EnemyAiBalance.NormalShootDiscount));
        Assert.That(
            EnemyAiBalance.PlanningIntervalSeconds,
            Is.GreaterThan(EnemyAiBalance.NormalPlanningIntervalSeconds));
    }

    [Test]
    public void Apply_Hard_BoostsShootBiasAndSpeedsPlanning()
    {
        EnemyAiBalance.Apply(EnemyAiDifficulty.Hard);

        Assert.That(EnemyAiBalance.PassPenalty, Is.LessThan(EnemyAiBalance.NormalPassPenalty));
        Assert.That(EnemyAiBalance.ShootDiscount, Is.GreaterThan(EnemyAiBalance.NormalShootDiscount));
        Assert.That(
            EnemyAiBalance.PlanningIntervalSeconds,
            Is.LessThan(EnemyAiBalance.NormalPlanningIntervalSeconds));
    }

    [Test]
    public void ResolvePlanningInterval_ScalesSerializedNormalBase()
    {
        Assert.That(
            EnemyAiBalance.ResolvePlanningInterval(EnemyAiDifficulty.Normal, 5f),
            Is.EqualTo(5f).Within(0.001f));
        Assert.That(
            EnemyAiBalance.ResolvePlanningInterval(EnemyAiDifficulty.Easy, 5f),
            Is.EqualTo(6.5f).Within(0.001f));
        Assert.That(
            EnemyAiBalance.ResolvePlanningInterval(EnemyAiDifficulty.Hard, 5f),
            Is.EqualTo(3.5f).Within(0.001f));
    }

    [Test]
    public void DifficultyOrdering_ShootDiscount_EasyLessThanNormalLessThanHard()
    {
        EnemyAiBalance.Apply(EnemyAiDifficulty.Easy);
        float easy = EnemyAiBalance.ShootDiscount;
        EnemyAiBalance.Apply(EnemyAiDifficulty.Normal);
        float normal = EnemyAiBalance.ShootDiscount;
        EnemyAiBalance.Apply(EnemyAiDifficulty.Hard);
        float hard = EnemyAiBalance.ShootDiscount;

        Assert.That(easy, Is.LessThan(normal));
        Assert.That(normal, Is.LessThan(hard));
    }
}
#endif
