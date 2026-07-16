#if UNITY_EDITOR
using System.Reflection;
using NUnit.Framework;
using UnityEngine;

/// <summary>6-F P0: 得点後リスタートの純判定と GOAP ゲート連携。</summary>
public sealed class PostGoalRestartRulesEditModeTests
{
    [SetUp]
    public void SetUp()
    {
        PostGoalRestartGate.ResetForEditModeTests();
    }

    [TearDown]
    public void TearDown()
    {
        PostGoalRestartGate.ResetForEditModeTests();
        var existing = Object.FindObjectsOfType<StateManager>();
        foreach (var sm in existing)
        {
            Object.DestroyImmediate(sm.gameObject);
        }

        typeof(StateManager)
            .GetField("_instance", BindingFlags.Static | BindingFlags.NonPublic)
            ?.SetValue(null, null);
    }

    [Test]
    public void ResolveConcedingTeamIsMaster_MasterGoalMeansMasterConcedes()
    {
        Assert.That(PostGoalRestartRules.ResolveConcedingTeamIsMaster(true), Is.True);
        Assert.That(PostGoalRestartRules.ResolveConcedingTeamIsMaster(false), Is.False);
    }

    [Test]
    public void ResolveKickoffOwnerStoredIndex_AssignsConcedingTeamLeader()
    {
        Assert.That(
            PostGoalRestartRules.ResolveKickoffOwnerStoredIndex(true),
            Is.EqualTo(BallKickoffAssignment.MasterTeamLeaderStoredIndex));
        Assert.That(
            PostGoalRestartRules.ResolveKickoffOwnerStoredIndex(false),
            Is.EqualTo(BallKickoffAssignment.OtherTeamLeaderStoredIndex));
    }

    [Test]
    public void ShouldSuppressGoapPlanning_TrueInsideWindow()
    {
        Assert.That(PostGoalRestartRules.ShouldSuppressGoapPlanning(10f, 5f), Is.True);
        Assert.That(PostGoalRestartRules.ShouldSuppressGoapPlanning(10f, 9.9f), Is.True);
    }

    [Test]
    public void ShouldSuppressGoapPlanning_FalseAfterWindow()
    {
        Assert.That(PostGoalRestartRules.ShouldSuppressGoapPlanning(10f, 10f), Is.False);
        Assert.That(PostGoalRestartRules.ShouldSuppressGoapPlanning(10f, 11f), Is.False);
    }

    [Test]
    public void ComputeSuppressUntil_AddsNonNegativeDuration()
    {
        Assert.That(PostGoalRestartRules.ComputeSuppressUntil(5f, 2f), Is.EqualTo(7f));
        Assert.That(PostGoalRestartRules.ComputeSuppressUntil(5f, -1f), Is.EqualTo(5f));
    }

    [Test]
    public void MatchPlayGate_FalseDuringPostGoalSuppressEvenInGame()
    {
        var stateGo = new GameObject("stateManager");
        var stateManager = stateGo.AddComponent<StateManager>();
        SetStateKind(stateManager, StateManager.STATE_KIND.GAME);

        PostGoalRestartGate.SetSuppressUntilForEditModeTests(100f);
        try
        {
            Assert.That(GoapMatchPlayGate.IsMatchPlayActive(), Is.False);
        }
        finally
        {
            Object.DestroyImmediate(stateGo);
        }
    }

    [Test]
    public void MatchPlayGate_TrueAfterPostGoalSuppressExpires()
    {
        var stateGo = new GameObject("stateManager");
        var stateManager = stateGo.AddComponent<StateManager>();
        SetStateKind(stateManager, StateManager.STATE_KIND.GAME);

        PostGoalRestartGate.SetSuppressUntilForEditModeTests(0f);
        try
        {
            Assert.That(GoapMatchPlayGate.IsMatchPlayActive(), Is.True);
        }
        finally
        {
            Object.DestroyImmediate(stateGo);
        }
    }

    private static void SetStateKind(StateManager stateManager, StateManager.STATE_KIND kind)
    {
        var field = typeof(StateManager).GetField(
            "_stateKind",
            BindingFlags.Instance | BindingFlags.NonPublic);
        field!.SetValue(stateManager, kind);
    }
}
#endif
