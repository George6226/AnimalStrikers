#if UNITY_EDITOR
using NUnit.Framework;

/// <summary>6-D P0: FreeBall / ReceivePass のダッシュ戦術判定。</summary>
public sealed class GoapDashPlanningEditModeTests
{
    [Test]
    public void ShouldDashToward_RequiresDistanceAndCanDash()
    {
        Assert.That(GoapDashPlanning.ShouldDashToward(5f, 3f, canUseDash: true), Is.True);
        Assert.That(GoapDashPlanning.ShouldDashToward(3f, 3f, canUseDash: true), Is.True);
        Assert.That(GoapDashPlanning.ShouldDashToward(2.9f, 3f, canUseDash: true), Is.False);
        Assert.That(GoapDashPlanning.ShouldDashToward(10f, 3f, canUseDash: false), Is.False);
    }

    [Test]
    public void ShouldDashForFreeBall_UsesFreeBallThreshold()
    {
        Assert.That(
            GoapDashPlanning.ShouldDashForFreeBall(
                GoapDashPlanning.FreeBallMinDashDistance,
                canUseDash: true),
            Is.True);
        Assert.That(
            GoapDashPlanning.ShouldDashForFreeBall(
                GoapDashPlanning.FreeBallMinDashDistance - 0.1f,
                canUseDash: true),
            Is.False);
    }

    [Test]
    public void ShouldDashForReceivePass_OffInCatchPhase()
    {
        Assert.That(
            GoapDashPlanning.ShouldDashForReceivePass(8f, canUseDash: true, isCatchPhase: true),
            Is.False);
        Assert.That(
            GoapDashPlanning.ShouldDashForReceivePass(8f, canUseDash: true, isCatchPhase: false),
            Is.True);
        Assert.That(
            GoapDashPlanning.ShouldDashForReceivePass(1f, canUseDash: true, isCatchPhase: false),
            Is.False);
    }
}
#endif
