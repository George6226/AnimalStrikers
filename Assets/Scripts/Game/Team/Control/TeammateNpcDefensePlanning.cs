using System.Collections.Generic;
using Game.Goap;
using System.Linq;
using UnityEngine;

/// <summary>
/// 味方フィールドNPCの相手ボール時守備: DefensivePositioning ゴール＋複数アクションの動的コスト競争。
/// </summary>
public static class TeammateNpcDefensePlanning
{
    private const float TemporarilyDisabledActionCostPenalty = 50f;

    /// <summary>EnemyBallDefense より DefensivePositioning を優先する味方NPC向け。</summary>
    public const float DefensivePositioningEnemyBallPriority = 88f;

    public static GoapDefenseActionUnderTest VerificationOnlyDefenseAction { get; private set; }

    public static void SetVerificationOnlyDefenseAction(GoapDefenseActionUnderTest action)
    {
        VerificationOnlyDefenseAction = action;
    }

    /// <summary>
    /// 戦術守備の到達は IS_IN_DEFENSIVE_POSITION（IS_MOVING だけだと MoveToSupport が誤選択される）。
    /// teamHasBall/hasBall は WM 遷移ラグで逆転しやすいため含めない（各アクション前提で後方連鎖）。
    /// </summary>
    private static readonly List<GoapCondition> TacticalDefensivePlanningRequiredFacts = new()
    {
        new GoapCondition(SymbolTag.Tactical.ENEMY_HAS_BALL, true),
        new GoapCondition(SymbolTag.Action.CAN_MOVE, true),
        new GoapCondition(SymbolTag.Action.IS_IN_DEFENSIVE_POSITION, true),
    };

    public static List<GoapCondition> GetTacticalDefensivePlanningRequiredFacts()
    {
        return TacticalDefensivePlanningRequiredFacts;
    }

    /// <summary>既に守備位置 Fact でも戦術アクションを選べる（味方NPC・敵保持時）。</summary>
    public static bool ShouldIgnoreDefensivePositionGate(PlayerBlackboard bb)
    {
        return ShouldUseTacticalDefenseGoal(bb);
    }

    /// <summary>非戦術時: 既に守備位置なら実行不可。</summary>
    public static bool BlocksWhenAlreadyInDefensivePosition(PlayerBlackboard bb)
    {
        return !ShouldIgnoreDefensivePositionGate(bb);
    }

    public static bool IsTeammateNpc(PlayerBlackboard bb)
    {
        if (bb?.BasicData?.Self == null)
        {
            return false;
        }

        var assignment = bb.BasicData.Self.GetComponentInParent<AnimalControlAssignment>()
            ?? bb.BasicData.Self.GetComponent<AnimalControlAssignment>();
        return assignment != null && assignment.Role == AnimalControlRole.TeammateNpc;
    }

    /// <summary>DefensivePositioning が有効な相手ボール局面（FREE/味方保持を除く）。</summary>
    public static bool IsEnemyBallDefenseContext(TeamBlackboard teamBB)
    {
        if (teamBB == null)
        {
            return false;
        }

        var ball = teamBB.BallInfo;
        if (ball.BallState == BallManager_State.BALL_STATE.FREE)
        {
            return false;
        }

        return ball.EnemyHasBall && !ball.TeamHasBall;
    }

    /// <summary>
    /// 味方NPCでは EnemyBallDefense を使わず、守備アクションをコスト比較で選ぶ。
    /// </summary>
    public static bool ShouldUseTacticalDefenseGoal(PlayerBlackboard bb)
    {
        if (!TeammateNpcGoapRoleDifferentiation.Enabled || !IsTeammateNpc(bb))
        {
            return false;
        }

        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        return IsEnemyBallDefenseContext(teamBB);
    }

    /// <summary>プランナー用の動的コスト（重なり回避＋状況調整を反映）。</summary>
    public static float ComputeDynamicCost(
        GoapActionSO action,
        PlayerBlackboard bb,
        float baseCost,
        float situationalAdjustment)
    {
        if (VerificationOnlyDefenseAction != GoapDefenseActionUnderTest.None
            && !VerificationOnlyDefenseAction.MatchesAction(action))
        {
            return TemporarilyDisabledActionCostPenalty + baseCost;
        }

        float cost = baseCost + situationalAdjustment;
        if (!ShouldUseTacticalDefenseGoal(bb))
        {
            return Mathf.Max(0.1f, cost);
        }

        cost = TeammateNpcGoapRoleDifferentiation.AdjustActionCost(cost, bb, TeammateNpcTacticalMode.Defend);
        return Mathf.Max(0.1f, cost);
    }

    /// <summary>戦術守備で空プランを避け、DefensivePositioning 候補を継続する。</summary>
    public static bool NeedsTacticalDefenseMovement(PlayerBlackboard bb)
    {
        return ShouldUseTacticalDefenseGoal(bb);
    }

    /// <summary>プランナーが空プランを返したとき、戦術守備移動を強制する。</summary>
    public static bool TryBuildForcedTacticalDefensePlan(
        PlayerBlackboard bb,
        List<GoapActionSO> scopedActions,
        out Queue<GoapActionSO> plan)
    {
        plan = null;
        if (!NeedsTacticalDefenseMovement(bb) || scopedActions == null || scopedActions.Count == 0)
        {
            return false;
        }

        GoapActionSO action = VerificationOnlyDefenseAction != GoapDefenseActionUnderTest.None
            ? scopedActions.FirstOrDefault(a => VerificationOnlyDefenseAction.MatchesAction(a))
            : scopedActions
                .OrderBy(a => a.CalculateDynamicCost(bb))
                .FirstOrDefault();
        if (action == null)
        {
            return false;
        }

        plan = new Queue<GoapActionSO>();
        plan.Enqueue(action);
        return true;
    }
}
