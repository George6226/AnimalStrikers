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
            // 保持時は必ず攻撃ゴールを載せる（外すと SelectBestGoal null → 棒立ち）。
            EnsureGoal<BallPossessionAttackGoalSO>(goals);
            EnsureAction<PassToTeammateActionSO>(actions);
            if (EnemyAiBalance.AllowEnemySubBallPossessionAttack)
            {
                EnsureAction<ShootAtGoalActionSO>(actions);
                EnsureAction<DribbleTowardGoalActionSO>(actions);
            }
            else
            {
                // 6-A P2 Easy: パスでボールを捌くだけ。Shoot/Dribble は抑止（Special は守備用に残す）。
                RemoveActionsOfType<ShootAtGoalActionSO>(actions);
                RemoveActionsOfType<DribbleTowardGoalActionSO>(actions);
            }
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
            var attackActions = actions.Where(GoapMainNpcCatalog.IsBallPossessionAttackAction);
            // Easy Sub: 保持時はパスのみ（UseSpecial はカタログに残しても攻撃文脈では出さない）。
            if (tier == GoapNpcTier.Sub && !EnemyAiBalance.AllowEnemySubBallPossessionAttack)
            {
                attackActions = attackActions.Where(a => a is PassToTeammateActionSO);
            }

            return attackActions.ToList();
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

    private static void RemoveActionsOfType<T>(List<GoapActionSO> actions) where T : GoapActionSO
    {
        actions.RemoveAll(a => a is T);
    }
}
