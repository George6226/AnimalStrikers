#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public sealed class AnimalActionAccuracyPolicyEditModeTests
{
    [TearDown]
    public void TearDown()
    {
        GoapMainNpcVerifyEnvironment.Sync(false, 0);
    }

    [Test]
    public void UseDeterministicDirection_Default_IsFalse()
    {
        GoapMainNpcVerifyEnvironment.Sync(false, 0);

        Assert.That(AnimalActionAccuracyPolicy.UseDeterministicDirection, Is.False);
    }

    [Test]
    public void UseDeterministicDirection_MainNpcVerify_IsTrue()
    {
        GoapMainNpcVerifyEnvironment.Sync(true, 0);

        Assert.That(AnimalActionAccuracyPolicy.UseDeterministicDirection, Is.True);
    }

    [Test]
    public void ApplyHorizontalSpread_VerifyMode_KeepsDirection()
    {
        GoapMainNpcVerifyEnvironment.Sync(true, 0);
        Vector3 dir = new Vector3(0f, 0f, 1f);

        Vector3 adjusted = AnimalActionAccuracyPolicy.ApplyHorizontalSpread(dir, 0f, 20f);

        Assert.That(adjusted, Is.EqualTo(dir.normalized));
    }
}
#endif
