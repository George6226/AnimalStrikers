#if UNITY_EDITOR
using NUnit.Framework;

/// <summary>6-E 残: 必殺技 Photon 同期の純判定。</summary>
public sealed class SpecialNetworkRulesEditModeTests
{
    [Test]
    public void ShouldSyncSpecialOverPhoton_SkipsNpcAndOffline()
    {
        Assert.That(
            SpecialNetworkRules.ShouldSyncSpecialOverPhoton(ConstData.BATTLE_MODE.NPC, true),
            Is.False);
        Assert.That(
            SpecialNetworkRules.ShouldSyncSpecialOverPhoton(ConstData.BATTLE_MODE.NORMAL, false),
            Is.False);
    }

    [Test]
    public void ShouldSyncSpecialOverPhoton_EnablesInRoomNormal()
    {
        Assert.That(
            SpecialNetworkRules.ShouldSyncSpecialOverPhoton(ConstData.BATTLE_MODE.NORMAL, true),
            Is.True);
    }

    [Test]
    public void ShouldOwnerBroadcastSpecialRpc_OnlyWhenMine()
    {
        Assert.That(SpecialNetworkRules.ShouldOwnerBroadcastSpecialRpc(true), Is.True);
        Assert.That(SpecialNetworkRules.ShouldOwnerBroadcastSpecialRpc(false), Is.False);
    }

    [Test]
    public void NetworkMirror_DoesNotReExecuteSpecial()
    {
        Assert.That(SpecialNetworkRules.ShouldRunExecuteSpecialOnNetworkMirror(), Is.False);
        Assert.That(SpecialNetworkRules.ShouldResetGaugeOnNetworkMirror(), Is.True);
    }
}
#endif
