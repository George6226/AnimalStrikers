#if UNITY_EDITOR
using System.Collections.Generic;
using Game.Goap.Goals;
using NUnit.Framework;
using UnityEngine;

public sealed class GoapEnemyNpcCatalogEditModeTests
{
    [Test]
    public void NormalizeLists_SubTier_IncludesFreeBallAndBallPossessionAttack()
    {
        var goals = new List<GoapGoalSO>();
        var actions = new List<GoapActionSO>();

        GoapEnemyNpcCatalog.NormalizeLists(goals, actions, GoapNpcTier.Sub);

        Assert.That(goals.Exists(g => g is FreeBallRecoveryGoalSO), Is.True);
        Assert.That(goals.Exists(g => g is BallPossessionAttackGoalSO), Is.True);
        Assert.That(actions.Exists(a => a is MoveToFreeBallActionSO), Is.True);
        Assert.That(actions.Exists(a => a is PassToTeammateActionSO), Is.True);
        Assert.That(actions.Exists(a => a is ShootAtGoalActionSO), Is.True);
    }

    [Test]
    public void FilterActionsForGoal_SubBallPossession_ReturnsPassAndShootOnly()
    {
        var goals = new List<GoapGoalSO>();
        var actions = new List<GoapActionSO>();
        GoapEnemyNpcCatalog.NormalizeLists(goals, actions, GoapNpcTier.Sub);

        var goal = goals.Find(g => g is BallPossessionAttackGoalSO);
        var filtered = GoapEnemyNpcCatalog.FilterActionsForGoal(goal, actions, GoapNpcTier.Sub);

        Assert.That(filtered.Count, Is.EqualTo(3));
        Assert.That(filtered.TrueForAll(GoapMainNpcCatalog.IsBallPossessionAttackAction), Is.True);
    }

    [Test]
    public void NormalizeLists_MainTier_IncludesDefenseGoals()
    {
        var goals = new List<GoapGoalSO>();
        var actions = new List<GoapActionSO>();

        GoapEnemyNpcCatalog.NormalizeLists(goals, actions, GoapNpcTier.Main);

        Assert.That(goals.Exists(g => g is DefensivePositioningGoalSO), Is.True);
        Assert.That(goals.Exists(g => g is EnemyBallDefenseGoalSO), Is.True);
        Assert.That(actions.Exists(a => a is MarkOpponentActionSO), Is.True);
    }
}
#endif
