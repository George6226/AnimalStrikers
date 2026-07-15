#if UNITY_EDITOR
using System.Collections.Generic;
using Game.Goap.Goals;
using NUnit.Framework;
using UnityEngine;

public sealed class GoapEnemyNpcCatalogEditModeTests
{
    [TearDown]
    public void TearDown()
    {
        EnemyAiBalance.Apply(EnemyAiDifficulty.Normal);
    }

    [Test]
    public void NormalizeLists_SubTier_IncludesFreeBallAndBallPossessionAttack()
    {
        EnemyAiBalance.Apply(EnemyAiDifficulty.Normal);
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
    public void NormalizeLists_SubTier_Easy_OmitsBallPossessionAttack()
    {
        EnemyAiBalance.Apply(EnemyAiDifficulty.Easy);
        var goals = new List<GoapGoalSO>
        {
            ScriptableObject.CreateInstance<BallPossessionAttackGoalSO>(),
        };
        var actions = new List<GoapActionSO>
        {
            ScriptableObject.CreateInstance<PassToTeammateActionSO>(),
            ScriptableObject.CreateInstance<ShootAtGoalActionSO>(),
            ScriptableObject.CreateInstance<DribbleTowardGoalActionSO>(),
        };

        GoapEnemyNpcCatalog.NormalizeLists(goals, actions, GoapNpcTier.Sub);

        Assert.That(goals.Exists(g => g is BallPossessionAttackGoalSO), Is.False);
        Assert.That(actions.Exists(a => a is PassToTeammateActionSO), Is.False);
        Assert.That(actions.Exists(a => a is ShootAtGoalActionSO), Is.False);
        Assert.That(actions.Exists(a => a is DribbleTowardGoalActionSO), Is.False);
        Assert.That(goals.Exists(g => g is FreeBallRecoveryGoalSO), Is.True);
    }

    [Test]
    public void FilterActionsForGoal_SubBallPossession_ReturnsPassAndShootOnly()
    {
        EnemyAiBalance.Apply(EnemyAiDifficulty.Normal);
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
