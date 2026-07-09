using NUnit.Framework;

public class BallKickoffResetRulesEditModeTests
{
    [Test]
    public void ShouldRejectOwnershipClaim_BlocksPositiveOwnerDuringSuppressWindow()
    {
        Assert.That(BallKickoffResetRules.ShouldRejectOwnershipClaim(1001, 10f, 5f), Is.True);
    }

    [Test]
    public void ShouldRejectOwnershipClaim_AllowsFreeBallDuringSuppressWindow()
    {
        Assert.That(BallKickoffResetRules.ShouldRejectOwnershipClaim(-1, 10f, 5f), Is.False);
        Assert.That(BallKickoffResetRules.ShouldRejectOwnershipClaim(0, 10f, 5f), Is.False);
    }

    [Test]
    public void ShouldRejectOwnershipClaim_AllowsPickupAfterSuppressWindow()
    {
        Assert.That(BallKickoffResetRules.ShouldRejectOwnershipClaim(1001, 10f, 10f), Is.False);
        Assert.That(BallKickoffResetRules.ShouldRejectOwnershipClaim(1001, 10f, 11f), Is.False);
    }
}
