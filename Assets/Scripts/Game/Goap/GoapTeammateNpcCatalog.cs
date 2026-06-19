using System.Collections.Generic;
using System.Linq;
using Game.Goap.Goals;
using UnityEngine;

/// <summary>
/// 味方フィールドNPC向け GOAP（移動のみ・パス/シュートなし）の許可リスト。
/// </summary>
public static class GoapTeammateNpcCatalog
{
    public static bool IsAllowedGoal(GoapGoalSO goal)
    {
        return goal is FreeBallRecoveryGoalSO
            || goal is TeamBallSupportGoalSO
            || goal is EnemyBallDefenseGoalSO
            || goal is DefensivePositioningGoalSO;
    }

    public static bool IsAllowedAction(GoapActionSO action)
    {
        return action is MoveToFreeBallActionSO
            || action is MoveToSupportPositionActionSO
            || action is MoveToDefensivePositionActionSO
            || action is MarkOpponentActionSO
            || action is BlockPassLaneActionSO
            || action is BlockShotLaneActionSO
            || action is RetreatToDefensiveLineActionSO
            || action is GetOpenActionSO
            || action is CreateSupportAngleActionSO
            || action is MakeRunBehindActionSO;
    }

    /// <summary>ゴールに応じてプランナー候補アクションを絞る（守備/攻撃の相互混入防止。WM ラグを避ける）。</summary>
    public static List<GoapActionSO> FilterActionsForGoal(GoapGoalSO goal, List<GoapActionSO> actions)
    {
        if (goal == null || actions == null || actions.Count == 0)
        {
            return actions ?? new List<GoapActionSO>();
        }

        if (goal is TeamBallSupportGoalSO)
        {
            if (TeammateNpcSupportPlanning.VerificationOnlySupportAction != GoapSupportActionUnderTest.None)
            {
                string onlyName = TeammateNpcSupportPlanning.VerificationOnlySupportAction.ToActionName();
                return actions.Where(a => a.ActionName == onlyName).ToList();
            }

            return actions.Where(IsSupportAttackAction).ToList();
        }

        if (goal is DefensivePositioningGoalSO or EnemyBallDefenseGoalSO)
        {
            return actions.Where(IsDefenseAction).ToList();
        }

        if (goal is FreeBallRecoveryGoalSO)
        {
            return actions.Where(a => a is MoveToFreeBallActionSO).ToList();
        }

        return actions;
    }

    public static bool IsSupportAttackAction(GoapActionSO action)
    {
        return action is MoveToSupportPositionActionSO
            or GetOpenActionSO
            or CreateSupportAngleActionSO
            or MakeRunBehindActionSO;
    }

    public static bool IsDefenseAction(GoapActionSO action)
    {
        return action is MoveToDefensivePositionActionSO
            or MarkOpponentActionSO
            or BlockPassLaneActionSO
            or BlockShotLaneActionSO
            or RetreatToDefensiveLineActionSO;
    }

    public static bool IsTacticalMoveRuntime(GoapActionRuntime runtime)
    {
        return runtime is MoveToFreeBallActionRuntime
            or MoveToSupportPositionActionRuntime
            or MoveToDefensivePositionActionRuntime
            or MarkOpponentActionRuntime
            or BlockPassLaneActionRuntime
            or BlockShotLaneActionRuntime
            or RetreatToDefensiveLineActionRuntime
            or GetOpenActionRuntime
            or CreateSupportAngleActionRuntime
            or MakeRunBehindActionRuntime;
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
        EnsureGoal<EnemyBallDefenseGoalSO>(goals);
        EnsureGoal<DefensivePositioningGoalSO>(goals);

        EnsureAction<MoveToFreeBallActionSO>(actions);
        EnsureAction<MoveToSupportPositionActionSO>(actions);
        EnsureAction<MoveToDefensivePositionActionSO>(actions);
        EnsureAction<MarkOpponentActionSO>(actions);
        EnsureAction<BlockPassLaneActionSO>(actions);
        EnsureAction<BlockShotLaneActionSO>(actions);
        EnsureAction<RetreatToDefensiveLineActionSO>(actions);
        EnsureAction<GetOpenActionSO>(actions);
        EnsureAction<CreateSupportAngleActionSO>(actions);
        EnsureAction<MakeRunBehindActionSO>(actions);

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
