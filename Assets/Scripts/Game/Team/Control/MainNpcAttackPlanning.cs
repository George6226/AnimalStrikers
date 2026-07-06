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
    private const float ShootNearGoalDiscount = 0.52f;
    private const float BlockedShotLanePenalty = 0.55f;
    private const float VeryNearGoalDistanceRatio = 0.22f;
    private const float VeryNearGoalPassPenalty = 0.55f;
    private const float VeryNearGoalShootDiscount = 0.55f;
    private const float VeryNearGoalShootPressureRelief = 0.20f;
    private const float MidRangeNearGoalPassPenalty = 0.25f;
    private const float MidRangeNearGoalDistanceRatio = 0.35f;

    public const float DefaultPassBaseCost = 1.12f;
    public const float DefaultShootBaseCost = 1.05f;

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
        return teamBB != null && TeammateNpcSupportPlanning.IsTeamBallAttackContext(teamBB, bb);
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
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null || bb == null)
        {
            return 0f;
        }

        bool passRouteClear = false;
        if (GoapMainNpcAttackBridge.TryFindPassTarget(bb, out AnimalFacade target))
        {
            passRouteClear = PlayerBlackboardCalculator.IsPassRouteClear(
                bb.PhysicalState.Position,
                target.transform.position,
                teamBB.BasicInfo.EnemyPositions,
                teamBB.FieldInfo.FieldLength * 0.06f);
        }

        if (!TryGetDistanceToEnemyGoal(bb, out float goalDistance, out float maxDistance))
        {
            return 0f;
        }

        return ComputePassCostAdjustment(
            goalDistance,
            maxDistance,
            teamBB.BallInfo.IsBallOwnerUnderPressure,
            passRouteClear);
    }

    /// <summary>EditMode / 診断用: ゴール距離とプレッシャーからパスコスト補正を見積もる。</summary>
    public static float ComputePassCostAdjustment(
        float goalDistance,
        float maxShootDistance,
        int pressureCount,
        bool passRouteClear)
    {
        float adjustment = 0f;

        if (pressureCount >= 1)
        {
            adjustment -= PassUnderPressureDiscount;
        }

        if (pressureCount >= 2)
        {
            adjustment -= 0.15f;
        }

        if (passRouteClear)
        {
            adjustment -= 0.20f;
        }

        if (goalDistance <= maxShootDistance * MidRangeNearGoalDistanceRatio)
        {
            adjustment += MidRangeNearGoalPassPenalty;
        }

        if (IsWithinVeryNearGoalShootZone(goalDistance, maxShootDistance))
        {
            adjustment += VeryNearGoalPassPenalty;
        }

        return adjustment;
    }

    public static float ComputeShootCostAdjustment(PlayerBlackboard bb)
    {
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null || bb == null)
        {
            return 0f;
        }

        if (!TryGetDistanceToEnemyGoal(bb, out float goalDistance, out float maxDistance))
        {
            return 0.5f;
        }

        Vector3 goalPos = GoapFieldNpcPerspective.GetAttackGoalPosition(
            teamBB,
            GoapFieldNpcPerspective.IsMirrored(bb));
        float laneWidth = teamBB.FieldInfo.FieldLength * 0.08f;
        bool shotLaneClear = PlayerBlackboardCalculator.IsPassRouteClear(
            bb.PhysicalState.Position,
            goalPos,
            teamBB.BasicInfo.EnemyPositions,
            laneWidth);

        return ComputeShootCostAdjustment(
            goalDistance,
            maxDistance,
            teamBB.BallInfo.IsBallOwnerUnderPressure,
            shotLaneClear);
    }

    /// <summary>EditMode / 診断用: ゴール距離とプレッシャーからシュートコスト補正を見積もる。</summary>
    public static float ComputeShootCostAdjustment(
        float goalDistance,
        float maxShootDistance,
        int pressureCount,
        bool shotLaneClear)
    {
        float adjustment = 0f;
        float normalized = 1f - Mathf.Clamp01(goalDistance / Mathf.Max(maxShootDistance, 0.01f));
        adjustment -= normalized * ShootNearGoalDiscount;

        if (!shotLaneClear)
        {
            adjustment += BlockedShotLanePenalty;
        }

        if (pressureCount >= 2)
        {
            adjustment += 0.20f;
        }

        if (IsWithinVeryNearGoalShootZone(goalDistance, maxShootDistance))
        {
            adjustment -= VeryNearGoalShootDiscount;
            if (pressureCount >= 2)
            {
                adjustment -= VeryNearGoalShootPressureRelief;
            }
        }

        return adjustment;
    }

    public static bool IsWithinVeryNearGoalShootZone(float goalDistance, float maxShootDistance)
    {
        return maxShootDistance > 0.01f
            && goalDistance <= maxShootDistance * VeryNearGoalDistanceRatio;
    }

    public static float EstimatePassCost(
        float goalDistance,
        float maxShootDistance,
        int pressureCount,
        bool passRouteClear)
    {
        return DefaultPassBaseCost + ComputePassCostAdjustment(
            goalDistance,
            maxShootDistance,
            pressureCount,
            passRouteClear);
    }

    public static float EstimateShootCost(
        float goalDistance,
        float maxShootDistance,
        int pressureCount,
        bool shotLaneClear)
    {
        return DefaultShootBaseCost + ComputeShootCostAdjustment(
            goalDistance,
            maxShootDistance,
            pressureCount,
            shotLaneClear);
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
        bool mirrored = GoapFieldNpcPerspective.IsMirrored(bb);
        Vector3 goalPos = GoapFieldNpcPerspective.GetAttackGoalPosition(teamBB, mirrored);
        distance = Vector3.Distance(bb.PhysicalState.Position, goalPos);
        return true;
    }
}
