using System.Collections.Generic;
using System.Linq;
using Game.Goap.Goals;
using UnityEngine;

/// <summary>
/// 敵フィールド NPC 向け GOAP カタログ（味方カタログを拡張し、保持時攻撃・守備を補完する）。
/// </summary>
public static class GoapEnemyNpcCatalog
{
    public static void NormalizeLists(List<GoapGoalSO> goals, List<GoapActionSO> actions, GoapNpcTier tier)
    {
        if (goals == null)
        {
            goals = new List<GoapGoalSO>();
        }

        if (actions == null)
        {
            actions = new List<GoapActionSO>();
        }

        if (tier == GoapNpcTier.Main)
        {
            GoapMainNpcCatalog.NormalizeLists(goals, actions);
            EnsureGoal<DefensivePositioningGoalSO>(goals);
            EnsureGoal<EnemyBallDefenseGoalSO>(goals);
            EnsureDefenseActions(actions);
        }
        else
        {
            GoapTeammateNpcCatalog.NormalizeLists(goals, actions);
            EnsureGoal<BallPossessionAttackGoalSO>(goals);
            EnsureAction<PassToTeammateActionSO>(actions);
            EnsureAction<ShootAtGoalActionSO>(actions);
            EnsureAction<DribbleTowardGoalActionSO>(actions);
        }

        foreach (GoapActionSO action in actions)
        {
            action?.EnsurePlanningFactsConfigured();
        }
    }

    public static List<GoapActionSO> FilterActionsForGoal(
        GoapGoalSO goal,
        List<GoapActionSO> actions,
        GoapNpcTier tier)
    {
        if (goal is BallPossessionAttackGoalSO)
        {
            return actions.Where(GoapMainNpcCatalog.IsBallPossessionAttackAction).ToList();
        }

        if (goal is IncomingPassReceiveGoalSO)
        {
            return actions.Where(GoapMainNpcCatalog.IsIncomingPassReceiveAction).ToList();
        }

        return tier == GoapNpcTier.Main
            ? FilterMainActionsForGoal(goal, actions)
            : GoapTeammateNpcCatalog.FilterActionsForGoal(goal, actions);
    }

    private static List<GoapActionSO> FilterMainActionsForGoal(GoapGoalSO goal, List<GoapActionSO> actions)
    {
        if (goal is DefensivePositioningGoalSO or EnemyBallDefenseGoalSO)
        {
            return actions.Where(GoapTeammateNpcCatalog.IsDefenseAction).ToList();
        }

        return GoapMainNpcCatalog.FilterActionsForGoal(goal, actions);
    }

    private static void EnsureDefenseActions(List<GoapActionSO> actions)
    {
        EnsureAction<MoveToDefensivePositionActionSO>(actions);
        EnsureAction<MarkOpponentActionSO>(actions);
        EnsureAction<BlockPassLaneActionSO>(actions);
        EnsureAction<BlockShotLaneActionSO>(actions);
        EnsureAction<RetreatToDefensiveLineActionSO>(actions);
        EnsureAction<SlideTackleActionSO>(actions);
        EnsureAction<UseSpecialActionSO>(actions);
    }

    private static void EnsureGoal<T>(List<GoapGoalSO> goals) where T : GoapGoalSO
    {
        if (goals.Any(g => g is T))
        {
            return;
        }

        goals.Add(ScriptableObject.CreateInstance<T>());
    }

    private static void EnsureAction<T>(List<GoapActionSO> actions) where T : GoapActionSO
    {
        if (actions.Any(a => a is T))
        {
            return;
        }

        actions.Add(ScriptableObject.CreateInstance<T>());
    }
}
