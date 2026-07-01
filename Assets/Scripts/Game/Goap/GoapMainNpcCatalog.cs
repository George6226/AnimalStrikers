using System.Collections.Generic;
using System.Linq;
using Game.Goap.Goals;
using UnityEngine;

/// <summary>
/// メイン NPC 向け GOAP（M1: パス/シュート、M2: パス後サポート・ルーズボール追跡）。
/// </summary>
public static class GoapMainNpcCatalog
{
    public static bool IsAllowedGoal(GoapGoalSO goal)
    {
        return goal is FreeBallRecoveryGoalSO
            || goal is BallPossessionAttackGoalSO
            || goal is TeamBallSupportGoalSO;
    }

    public static bool IsAllowedAction(GoapActionSO action)
    {
        return action is MoveToFreeBallActionSO
            || action is PassToTeammateActionSO
            || action is ShootAtGoalActionSO
            || IsTeamBallSupportAction(action);
    }

    public static bool IsBallPossessionAttackAction(GoapActionSO action)
    {
        return action is PassToTeammateActionSO or ShootAtGoalActionSO;
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

        return actions;
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
        EnsureGoal<BallPossessionAttackGoalSO>(goals);

        EnsureAction<MoveToFreeBallActionSO>(actions);
        EnsureAction<MoveToSupportPositionActionSO>(actions);
        EnsureAction<GetOpenActionSO>(actions);
        EnsureAction<CreateSupportAngleActionSO>(actions);
        EnsureAction<MakeRunBehindActionSO>(actions);
        EnsureAction<PassToTeammateActionSO>(actions);
        EnsureAction<ShootAtGoalActionSO>(actions);

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
}
