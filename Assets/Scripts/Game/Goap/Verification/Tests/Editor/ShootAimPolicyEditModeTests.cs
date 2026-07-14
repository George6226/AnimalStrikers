#if UNITY_EDITOR
using NUnit.Framework;
using UnityEngine;

public sealed class ShootAimPolicyEditModeTests
{
    [TearDown]
    public void TearDown()
    {
        GoapMainNpcVerifyEnvironment.Sync(false, 0);
        GoapMainNpcProductionEnvironment.Sync(false);
    }

    [Test]
    public void ResolveAimPoint_GkOnRight_AimsLeftCorner()
    {
        GoapMainNpcVerifyEnvironment.Sync(true, 0);
        Vector3 goalCenter = new Vector3(0f, 0f, 20f);
        Vector3 shooter = new Vector3(0f, 0f, 8f);
        Vector3 gk = new Vector3(2f, 0f, 18f);

        Vector3 aim = ShootAimPolicy.ResolveAimPoint(shooter, goalCenter, gk, 3.5f);

        Assert.That(aim.x, Is.LessThan(-2.5f));
        Assert.That(aim.z, Is.EqualTo(20f).Within(0.01f));
    }

    [Test]
    public void ResolveAimPoint_GkOnLeft_AimsRightFarPost()
    {
        GoapMainNpcVerifyEnvironment.Sync(true, 0);
        Vector3 goalCenter = new Vector3(0f, 0f, 20f);
        Vector3 shooter = new Vector3(0f, 0f, 8f);
        Vector3 gk = new Vector3(-2f, 0f, 18f);

        Vector3 aim = ShootAimPolicy.ResolveAimPoint(shooter, goalCenter, gk, 3.5f);

        Assert.That(aim.x, Is.GreaterThan(2.5f));
    }

    [Test]
    public void ResolveAimPoint_CentralGk_ShooterLeft_AimsRightOpenPost()
    {
        GoapMainNpcVerifyEnvironment.Sync(true, 0);
        Vector3 goalCenter = new Vector3(0f, 0f, 20f);
        Vector3 shooter = new Vector3(-4f, 0f, 6f);
        Vector3 gk = new Vector3(0.2f, 0f, 18f);

        Vector3 aim = ShootAimPolicy.ResolveAimPoint(shooter, goalCenter, gk, 3.5f);

        Assert.That(aim.x, Is.GreaterThan(2.5f));
    }

    [Test]
    public void ResolveAimPoint_NoGoalkeeper_AimsNearCenter()
    {
        GoapMainNpcVerifyEnvironment.Sync(true, 0);
        Vector3 goalCenter = new Vector3(0f, 0f, 20f);
        Vector3 shooter = new Vector3(0f, 0f, 8f);

        Vector3 aim = ShootAimPolicy.ResolveAimPoint(shooter, goalCenter, null, 3.5f);

        Assert.That(Mathf.Abs(aim.x), Is.LessThan(1.0f));
    }

    [Test]
    public void BuildKickVector_DeterministicMode_UsesGroundKickWithoutVertical()
    {
        GoapMainNpcVerifyEnvironment.Sync(true, 0);
        Vector3 shooter = new Vector3(0f, 0f, 0f);
        Vector3 aim = new Vector3(2.5f, 0f, 20f);

        Vector3 kick = ShootAimPolicy.BuildKickVector(shooter, aim, 50f, 0.8f, 0f);

        Assert.That(kick.y, Is.EqualTo(0f).Within(0.001f));
        Assert.That(kick.magnitude, Is.GreaterThan(0f));
    }
}
#endif
