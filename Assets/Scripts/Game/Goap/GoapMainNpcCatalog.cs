using System.Collections.Generic;
using System.Linq;
using Game.Goap.Goals;
using UnityEngine;

/// <summary>
/// メイン NPC 向け GOAP（M1: パス/シュート、M2: サポート/ルーズボール、M3: 敵保持時守備）。
/// </summary>
public static class GoapMainNpcCatalog
{
    public static bool IsAllowedGoal(GoapGoalSO goal)
    {
        return goal is FreeBallRecoveryGoalSO
            || goal is BallPossessionAttackGoalSO
            || goal is IncomingPassReceiveGoalSO
            || goal is TeamBallSupportGoalSO
            || goal is DefensivePositioningGoalSO
            || goal is EnemyBallDefenseGoalSO;
    }

    public static bool IsAllowedAction(GoapActionSO action)
    {
        return action is MoveToFreeBallActionSO
            || action is MoveToReceivePassActionSO
            || action is PassToTeammateActionSO
            || action is ShootAtGoalActionSO
            || action is DribbleTowardGoalActionSO
            || IsTeamBallSupportAction(action)
            || GoapTeammateNpcCatalog.IsDefenseAction(action);
    }

    public static bool IsIncomingPassReceiveAction(GoapActionSO action)
    {
        return action is MoveToReceivePassActionSO;
    }

    public static bool IsBallPossessionAttackAction(GoapActionSO action)
    {
        return action is PassToTeammateActionSO
            or ShootAtGoalActionSO
            or DribbleTowardGoalActionSO;
    }

    public static bool IsTeamBallSupportAction(GoapActionSO action)
    {
        return action is MoveToSupportPositionActionSO
            or GetOpenActionSO
            or CreateSupportAngleActionSO
            or MakeRunBehindActionSO;
    }

    public static List<GoapActionSO> FilterActionsForGoal(GoapGoalSO goal, List<GoapActionSO> actions)
    {
        if (goal == null || actions == null || actions.Count == 0)
        {
            return actions ?? new List<GoapActionSO>();
        }

        if (goal is BallPossessionAttackGoalSO)
        {
            return actions.Where(IsBallPossessionAttackAction).ToList();
        }

        if (goal is TeamBallSupportGoalSO)
        {
            return actions.Where(IsTeamBallSupportAction).ToList();
        }

        if (goal is FreeBallRecoveryGoalSO)
        {
            return actions.Where(a => a is MoveToFreeBallActionSO).ToList();
        }

        if (goal is IncomingPassReceiveGoalSO)
        {
            return actions.Where(IsIncomingPassReceiveAction).ToList();
        }

        if (goal is DefensivePositioningGoalSO or EnemyBallDefenseGoalSO)
        {
            return actions.Where(GoapTeammateNpcCatalog.IsDefenseAction).ToList();
        }

        return actions;
    }

    /// <summary>Phase A 以前の M2 限定本番向け: BallPossessionAttack を除外（レガシー・テスト用）。</summary>
    public static void RestrictToOffBallProduction(List<GoapGoalSO> goals, List<GoapActionSO> actions)
    {
        if (goals != null)
        {
            goals.RemoveAll(g => g is BallPossessionAttackGoalSO);
        }

        if (actions != null)
        {
            actions.RemoveAll(IsBallPossessionAttackAction);
        }
    }

    public static void NormalizeLists(List<GoapGoalSO> goals, List<GoapActionSO> actions)
    {
        if (goals == null)
        {
            goals = new List<GoapGoalSO>();
        }

        if (actions == null)
        {
            actions = new List<GoapActionSO>();
        }

        goals.RemoveAll(g => g == null || !IsAllowedGoal(g));
        actions.RemoveAll(a => a == null || !IsAllowedAction(a));

        EnsureGoal<FreeBallRecoveryGoalSO>(goals);
        EnsureGoal<TeamBallSupportGoalSO>(goals);
        EnsureGoal<IncomingPassReceiveGoalSO>(goals);
        EnsureGoal<BallPossessionAttackGoalSO>(goals);
        EnsureGoal<DefensivePositioningGoalSO>(goals);
        EnsureGoal<EnemyBallDefenseGoalSO>(goals);

        EnsureAction<MoveToFreeBallActionSO>(actions);
        EnsureAction<MoveToReceivePassActionSO>(actions);
        EnsureAction<MoveToSupportPositionActionSO>(actions);
        EnsureAction<GetOpenActionSO>(actions);
        EnsureAction<CreateSupportAngleActionSO>(actions);
        EnsureAction<MakeRunBehindActionSO>(actions);
        EnsureAction<PassToTeammateActionSO>(actions);
        EnsureAction<ShootAtGoalActionSO>(actions);
        EnsureAction<DribbleTowardGoalActionSO>(actions);
        EnsureDefenseActions(actions);

        foreach (GoapActionSO action in actions)
        {
            action?.EnsurePlanningFactsConfigured();
        }
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

    private static void EnsureDefenseActions(List<GoapActionSO> actions)
    {
        EnsureAction<MoveToDefensivePositionActionSO>(actions);
        EnsureAction<MarkOpponentActionSO>(actions);
        EnsureAction<BlockPassLaneActionSO>(actions);
        EnsureAction<BlockShotLaneActionSO>(actions);
        EnsureAction<RetreatToDefensiveLineActionSO>(actions);
        EnsureAction<SlideTackleActionSO>(actions);
    }
}
