using NUnit.Framework;

public sealed class GoapReplanLoadRulesEditModeTests
{
    [Test]
    public void ShouldCoalesceImmediateReplan_SuppressesBallContextWithinWindow()
    {
        Assert.That(
            GoapReplanLoadRules.ShouldCoalesceImmediateReplan("BallContextChanged", 5f, 6f),
            Is.True);
        Assert.That(
            GoapReplanLoadRules.ShouldCoalesceImmediateReplan("BallContextChanged", 6f, 6f),
            Is.False);
    }

    [Test]
    public void ShouldCoalesceImmediateReplan_BypassesBallOwnerChanged()
    {
        Assert.That(
            GoapReplanLoadRules.ShouldCoalesceImmediateReplan("BallOwnerChanged", 5f, 100f),
            Is.False);
    }

    [Test]
    public void ShouldCoalesceImmediateReplan_BypassesBallPossessionChanged()
    {
        Assert.That(
            GoapReplanLoadRules.ShouldCoalesceImmediateReplan("BallPossessionChanged", 5f, 100f),
            Is.False);
    }

    [Test]
    public void ShouldCoalesceImmediateReplan_BypassesPassIssued()
    {
        Assert.That(
            GoapReplanLoadRules.ShouldCoalesceImmediateReplan("PassIssued", 5f, 100f),
            Is.False);
    }

    [Test]
    public void ResolveCoalesceCooldownSeconds_ReturnsReasonSpecificValues()
    {
        Assert.That(
            GoapReplanLoadRules.ResolveCoalesceCooldownSeconds("BallContextChanged"),
            Is.EqualTo(GoapReplanLoadRules.BallContextCoalesceSeconds));
        Assert.That(
            GoapReplanLoadRules.ResolveCoalesceCooldownSeconds("EnemyLayoutChanged"),
            Is.EqualTo(GoapReplanLoadRules.EnemyLayoutCoalesceSeconds));
        Assert.That(
            GoapReplanLoadRules.ResolveCoalesceCooldownSeconds("MatchPlayStarted"),
            Is.EqualTo(0f));
    }

    [Test]
    public void ComputeCoalesceUntil_AddsCooldownFromNow()
    {
        Assert.That(
            GoapReplanLoadRules.ComputeCoalesceUntil(10f, "EnemyLayoutChanged"),
            Is.EqualTo(10f + GoapReplanLoadRules.EnemyLayoutCoalesceSeconds));
    }
}
