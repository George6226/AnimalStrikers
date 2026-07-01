using System.Collections.Generic;
using System.Linq;
using Game.Goap;
using Game.Goap.Goals;
using UnityEngine;

/// <summary>
/// Phase M1: メイン NPC のボール保持中攻撃（Pass/Shoot）の可否判定と動的コスト。
/// </summary>
public static class MainNpcAttackPlanning
{
    private const float MaxShootDistanceRatio = 0.55f;
    private const float MinShootDistanceRatio = 0.08f;
    private const float PassUnderPressureDiscount = 0.35f;
    private const float ShootNearGoalDiscount = 0.45f;
    private const float BlockedShotLanePenalty = 0.55f;

    public static bool IsBallPossessionAttackContext(PlayerBlackboard bb)
    {
        if (bb == null)
        {
            return false;
        }

        if (bb.GetFact(new Fact(SymbolTag.Basic.HAS_BALL, "true")) != true)
        {
            return false;
        }

        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        return teamBB != null && TeammateNpcSupportPlanning.IsTeamBallAttackContext(teamBB);
    }

    public static bool CanPassToTeammate(PlayerBlackboard bb)
    {
        if (!IsBallPossessionAttackContext(bb))
        {
            return false;
        }

        return GoapMainNpcAttackBridge.TryFindPassTarget(bb, out _);
    }

    public static bool CanShootAtGoal(PlayerBlackboard bb)
    {
        if (!IsBallPossessionAttackContext(bb))
        {
            return false;
        }

        if (!TryGetDistanceToEnemyGoal(bb, out float distance, out float maxDistance))
        {
            return false;
        }

        float minDistance = maxDistance * (MinShootDistanceRatio / MaxShootDistanceRatio);
        return distance >= minDistance && distance <= maxDistance;
    }

    public static float ComputePassCostAdjustment(PlayerBlackboard bb)
    {
        float adjustment = 0f;
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null || bb == null)
        {
            return adjustment;
        }

        int pressureCount = teamBB.BallInfo.IsBallOwnerUnderPressure;
        if (pressureCount >= 1)
        {
            adjustment -= PassUnderPressureDiscount;
        }

        if (pressureCount >= 2)
        {
            adjustment -= 0.15f;
        }

        if (GoapMainNpcAttackBridge.TryFindPassTarget(bb, out AnimalFacade target)
            && PlayerBlackboardCalculator.IsPassRouteClear(
                bb.PhysicalState.Position,
                target.transform.position,
                teamBB.BasicInfo.EnemyPositions,
                teamBB.FieldInfo.FieldLength * 0.06f))
        {
            adjustment -= 0.20f;
        }

        if (TryGetDistanceToEnemyGoal(bb, out float goalDistance, out float maxDistance)
            && goalDistance <= maxDistance * 0.35f)
        {
            adjustment += 0.25f;
        }

        return adjustment;
    }

    public static float ComputeShootCostAdjustment(PlayerBlackboard bb)
    {
        float adjustment = 0f;
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null || bb == null)
        {
            return adjustment;
        }

        if (!TryGetDistanceToEnemyGoal(bb, out float goalDistance, out float maxDistance))
        {
            return 0.5f;
        }

        float normalized = 1f - Mathf.Clamp01(goalDistance / Mathf.Max(maxDistance, 0.01f));
        adjustment -= normalized * ShootNearGoalDiscount;

        Vector3 goalPos = teamBB.FieldInfo.EnemyGoalPosition;
        float laneWidth = teamBB.FieldInfo.FieldLength * 0.08f;
        if (!PlayerBlackboardCalculator.IsPassRouteClear(
                bb.PhysicalState.Position,
                goalPos,
                teamBB.BasicInfo.EnemyPositions,
                laneWidth))
        {
            adjustment += BlockedShotLanePenalty;
        }

        int pressureCount = teamBB.BallInfo.IsBallOwnerUnderPressure;
        if (pressureCount >= 2)
        {
            adjustment += 0.20f;
        }

        return adjustment;
    }

    /// <summary>
    /// プランナーが空プランを返したとき、Pass/Shoot のいずれかを強制する。
    /// </summary>
    public static bool TryBuildForcedAttackPlan(
        PlayerBlackboard bb,
        IEnumerable<GoapActionSO> scopedActions,
        out Queue<GoapActionSO> plan)
    {
        plan = null;
        if (!IsBallPossessionAttackContext(bb) || scopedActions == null)
        {
            return false;
        }

        GoapActionSO bestAction = null;
        float bestCost = float.MaxValue;
        foreach (GoapActionSO action in scopedActions)
        {
            if (action == null || !GoapMainNpcCatalog.IsBallPossessionAttackAction(action))
            {
                continue;
            }

            if (action is PassToTeammateActionSO && !CanPassToTeammate(bb))
            {
                continue;
            }

            if (action is ShootAtGoalActionSO && !CanShootAtGoal(bb))
            {
                continue;
            }

            float cost = action.CalculateDynamicCost(bb);
            if (cost < bestCost)
            {
                bestCost = cost;
                bestAction = action;
            }
        }

        if (bestAction == null)
        {
            return false;
        }

        plan = new Queue<GoapActionSO>();
        plan.Enqueue(bestAction);
        return true;
    }

    public static bool NeedsForcedAttackPlan(PlayerBlackboard bb)
    {
        return IsBallPossessionAttackContext(bb)
            && (CanPassToTeammate(bb) || CanShootAtGoal(bb));
    }

    public static bool TryGetDistanceToEnemyGoal(
        PlayerBlackboard bb,
        out float distance,
        out float maxDistance)
    {
        distance = float.MaxValue;
        maxDistance = float.MaxValue;

        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null || bb == null)
        {
            return false;
        }

        maxDistance = teamBB.FieldInfo.FieldLength * MaxShootDistanceRatio;
        distance = Vector3.Distance(bb.PhysicalState.Position, teamBB.FieldInfo.EnemyGoalPosition);
        return true;
    }
}
