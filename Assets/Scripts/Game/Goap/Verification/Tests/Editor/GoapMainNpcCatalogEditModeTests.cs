#if UNITY_EDITOR
using System.Collections.Generic;
using Game.Goap.Goals;
using NUnit.Framework;
using UnityEngine;

public sealed class GoapMainNpcCatalogEditModeTests
{
    [Test]
    public void NormalizeLists_AddsAttackGoalsOnly()
    {
        var goals = new List<GoapGoalSO>();
        var actions = new List<GoapActionSO>();

        GoapMainNpcCatalog.NormalizeLists(goals, actions);

        Assert.That(goals, Has.All.Matches<GoapGoalSO>(GoapMainNpcCatalog.IsAllowedGoal));
        Assert.That(goals.Exists(g => g is TeamBallSupportGoalSO), Is.True);
        Assert.That(goals.Exists(g => g is FreeBallRecoveryGoalSO), Is.True);
        Assert.That(goals.Exists(g => g is DefensivePositioningGoalSO), Is.False);
    }

    [Test]
    public void FilterActionsForGoal_TeamBallSupport_UsesSupportActionsOnly()
    {
        var goals = new List<GoapGoalSO>();
        var actions = new List<GoapActionSO>();
        GoapMainNpcCatalog.NormalizeLists(goals, actions);

        var goal = goals.Find(g => g is TeamBallSupportGoalSO);
        var filtered = GoapMainNpcCatalog.FilterActionsForGoal(goal, actions);

        Assert.That(filtered, Has.All.Matches<GoapActionSO>(GoapTeammateNpcCatalog.IsSupportAttackAction));
        Assert.That(filtered.Exists(a => a is MoveToDefensivePositionActionSO), Is.False);
    }

    [Test]
    public void ResolveTier_MainNpcVerifyMode_UsesConfiguredSlot()
    {
        GoapMainNpcVerifyEnvironment.Sync(true, 0);
        var slot0 = new GameObject("slot0").AddComponent<AnimalFormationSlot>();
        slot0.Initialize(0);
        var slot1 = new GameObject("slot1").AddComponent<AnimalFormationSlot>();
        slot1.Initialize(1);

        var facade0 = slot0.gameObject.AddComponent<AnimalFacade>();
        var facade1 = slot1.gameObject.AddComponent<AnimalFacade>();

        try
        {
            Assert.That(GoapMainNpcVerifyEnvironment.ResolveTier(facade0), Is.EqualTo(GoapNpcTier.Main));
            Assert.That(GoapMainNpcVerifyEnvironment.ResolveTier(facade1), Is.EqualTo(GoapNpcTier.Sub));
            Assert.That(GoapMainNpcVerifyEnvironment.RequiresBootstrap, Is.True);
            Assert.That(GoapMainNpcVerifyEnvironment.IsBootstrapComplete, Is.False);
            GoapMainNpcVerifyEnvironment.MarkBootstrapComplete();
            Assert.That(GoapMainNpcVerifyEnvironment.RequiresBootstrap, Is.False);
            Assert.That(GoapMainNpcVerifyEnvironment.IsBootstrapComplete, Is.True);
        }
        finally
        {
            Object.DestroyImmediate(slot0.gameObject);
            Object.DestroyImmediate(slot1.gameObject);
            GoapMainNpcVerifyEnvironment.Sync(false, 0);
        }
    }
}
#endif
