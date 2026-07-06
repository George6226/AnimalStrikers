#if UNITY_EDITOR
using NUnit.Framework;

public sealed class PassLaneKickPolicyEditModeTests
{
    [Test]
    public void ResolveEnemyBlockingRange_DefaultFieldLength_UsesSixPercentBase()
    {
        float range = PassLaneKickPolicy.ResolveEnemyBlockingRange();
        Assert.That(range, Is.EqualTo(6f).Within(0.01f));
    }
}
#endif
