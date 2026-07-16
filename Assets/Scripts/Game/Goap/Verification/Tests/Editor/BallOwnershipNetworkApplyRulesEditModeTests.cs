#if UNITY_EDITOR
using NUnit.Framework;

/// <summary>6-E P0: ボール所有権ネットワーク適用の純判定。</summary>
public sealed class BallOwnershipNetworkApplyRulesEditModeTests
{
    [Test]
    public void ResolveBallStateFromOwnerId_HoldWhenPositiveOwner()
    {
        Assert.That(
            BallOwnershipNetworkApplyRules.ResolveBallStateFromOwnerId(1001),
            Is.EqualTo(BallManager_State.BALL_STATE.HOLD));
        Assert.That(
            BallOwnershipNetworkApplyRules.ResolveBallStateFromOwnerId(-1),
            Is.EqualTo(BallManager_State.BALL_STATE.FREE));
        Assert.That(
            BallOwnershipNetworkApplyRules.ResolveBallStateFromOwnerId(0),
            Is.EqualTo(BallManager_State.BALL_STATE.FREE));
    }

    [Test]
    public void ShouldBypassKickoffSuppressForNetworkApply_IsTrue()
    {
        Assert.That(BallOwnershipNetworkApplyRules.ShouldBypassKickoffSuppressForNetworkApply(), Is.True);
    }

    [Test]
    public void ShouldApplyPhotonOwnerId_AlwaysAppliesToHealDesync()
    {
        Assert.That(BallOwnershipNetworkApplyRules.ShouldApplyPhotonOwnerId(1001, -1), Is.True);
        Assert.That(BallOwnershipNetworkApplyRules.ShouldApplyPhotonOwnerId(1001, 1001), Is.True);
        Assert.That(BallOwnershipNetworkApplyRules.ShouldApplyPhotonOwnerId(-1, 1001), Is.True);
    }

    [Test]
    public void ResolveAppliedPhotonOwnerId_PassesThrough()
    {
        Assert.That(BallOwnershipNetworkApplyRules.ResolveAppliedPhotonOwnerId(42), Is.EqualTo(42));
        Assert.That(BallOwnershipNetworkApplyRules.ResolveAppliedPhotonOwnerId(-1), Is.EqualTo(-1));
    }

    [Test]
    public void KickoffSuppress_StillBlocksLocalClaims_ButNetworkBypassIsIndependent()
    {
        // ローカル経路は従来どおり suppress する
        Assert.That(BallKickoffResetRules.ShouldRejectOwnershipClaim(1001, 10f, 5f), Is.True);
        // ネットワーク適用は bypass 方針（RPC で TeamBB / BallOwnerID を揃える）
        Assert.That(BallOwnershipNetworkApplyRules.ShouldBypassKickoffSuppressForNetworkApply(), Is.True);
    }
}
#endif
