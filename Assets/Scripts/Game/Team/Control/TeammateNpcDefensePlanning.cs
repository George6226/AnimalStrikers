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

        var facade = bb.BasicData.Self.GetComponentInParent<AnimalFacade>()
            ?? bb.BasicData.Self.GetComponent<AnimalFacade>();
        if (facade != null && facade.IsGK())
        {
            return false;
        }

        if (GoapBatchVerifyEnvironment.IsActive)
        {
            return true;
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
        float situationalAdjustment,
        bool applyFloor = true)
    {
        if (VerificationOnlyDefenseAction != GoapDefenseActionUnderTest.None
            && !VerificationOnlyDefenseAction.MatchesAction(action))
        {
            return TemporarilyDisabledActionCostPenalty + baseCost;
        }

        float cost = baseCost + situationalAdjustment;
        if (!ShouldUseTacticalDefenseGoal(bb))
        {
            return applyFloor ? Mathf.Max(0.1f, cost) : cost;
        }

        cost = TeammateNpcGoapRoleDifferentiation.AdjustActionCost(
            cost,
            bb,
            TeammateNpcTacticalMode.Defend,
            action,
            applyFloor);
        return applyFloor ? Mathf.Max(0.1f, cost) : cost;
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

        GoapActionSO action;
        if (VerificationOnlyDefenseAction != GoapDefenseActionUnderTest.None)
        {
            action = scopedActions.FirstOrDefault(a => VerificationOnlyDefenseAction.MatchesAction(a));
        }
        else
        {
            action = scopedActions
                .OrderBy(a => a.CalculateDynamicCost(bb))
                .ThenBy(a => a.CalculateTacticalSelectionCost(bb))
                .FirstOrDefault();
        }

        if (action == null)
        {
            return false;
        }

        plan = new Queue<GoapActionSO>();
        plan.Enqueue(action);
        return true;
    }

    /// <summary>保持者→フリー受け手のパスレーン幾何（BlockPassLane / MTD 委譲判定の共有）。</summary>
    public struct PassLaneGeometry
    {
        public bool HasPassTarget;
        public float LaneAlign;
        public float AlongLane;
        public float DistPlayerToOwner;
        public float DistPlayerToPassTarget;
    }

    public static bool TryEvaluatePassLaneGeometry(PlayerBlackboard bb, out PassLaneGeometry geometry)
    {
        geometry = default;
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null || bb == null || !teamBB.BallInfo.EnemyHasBall)
        {
            return false;
        }

        return TryEvaluatePassLaneGeometryAt(bb.PhysicalState.Position, teamBB, out geometry);
    }

    public static bool TryEvaluatePassLaneGeometryAt(
        Vector3 playerPos,
        TeamBlackboard teamBB,
        out PassLaneGeometry geometry)
    {
        geometry = default;
        if (teamBB == null || !teamBB.BallInfo.EnemyHasBall)
        {
            return false;
        }

        Vector3 ownerPos = teamBB.BallInfo.BallOwnerPosition;
        float fieldLen = teamBB.FieldInfo.FieldLength;
        float markThreshold = fieldLen * 0.15f;

        Vector3 passTarget = default;
        float passTargetDist = float.MaxValue;
        foreach (Vector3 enemyPos in teamBB.BasicInfo.EnemyPositions)
        {
            if (Vector3.Distance(enemyPos, ownerPos) <= 0.1f)
            {
                continue;
            }

            bool isMarked = false;
            foreach (Vector3 allyPos in teamBB.BasicInfo.TeammatePositions)
            {
                if (Vector3.Distance(allyPos, playerPos) < 0.1f)
                {
                    continue;
                }

                if (Vector3.Distance(allyPos, enemyPos) <= markThreshold)
                {
                    isMarked = true;
                    break;
                }
            }

            if (isMarked)
            {
                continue;
            }

            float distFromOwner = Vector3.Distance(enemyPos, ownerPos);
            if (distFromOwner < passTargetDist)
            {
                passTargetDist = distFromOwner;
                passTarget = enemyPos;
            }
        }

        if (passTargetDist >= float.MaxValue * 0.5f)
        {
            return false;
        }

        Vector3 passDir = passTarget - ownerPos;
        passDir.y = 0f;
        if (passDir.sqrMagnitude < 0.01f)
        {
            return false;
        }

        passDir.Normalize();
        Vector3 ownerToPlayer = playerPos - ownerPos;
        ownerToPlayer.y = 0f;
        geometry = new PassLaneGeometry
        {
            HasPassTarget = true,
            LaneAlign = ownerToPlayer.sqrMagnitude < 0.01f
                ? 0f
                : Vector3.Dot(ownerToPlayer.normalized, passDir),
            AlongLane = Vector3.Dot(ownerToPlayer, passDir),
            DistPlayerToOwner = Vector3.Distance(playerPos, ownerPos),
            DistPlayerToPassTarget = Vector3.Distance(playerPos, passTarget),
        };
        return true;
    }

    private static float ComputePassLaneBlockUrgencyFromGeometry(PassLaneGeometry geo, float fieldLen)
    {
        if (geo.AlongLane > 0f
            && geo.AlongLane <= fieldLen * 0.24f
            && geo.LaneAlign > 0.32f)
        {
            return Mathf.Clamp01(0.75f + geo.LaneAlign * 0.25f);
        }

        if (geo.DistPlayerToOwner <= fieldLen * 0.22f
            && geo.LaneAlign >= 0.25f
            && geo.DistPlayerToOwner < geo.DistPlayerToPassTarget * 0.8f)
        {
            return 0.7f;
        }

        return 0f;
    }

    public static float ComputePassLaneBlockUrgencyAt(Vector3 playerPos, TeamBlackboard teamBB)
    {
        if (!TryEvaluatePassLaneGeometryAt(playerPos, teamBB, out PassLaneGeometry geo))
        {
            return 0f;
        }

        float fieldLen = teamBB.FieldInfo.FieldLength;
        return ComputePassLaneBlockUrgencyFromGeometry(geo, fieldLen);
    }

    /// <summary>パスレーン遮断の主担当（レーン上で最も近い、urgency 0.75+ の味方）か。</summary>
    public static bool IsPrimaryPassLaneBlocker(PlayerBlackboard bb)
    {
        float selfUrgency = ComputePassLaneBlockUrgency(bb);
        if (selfUrgency < 0.75f)
        {
            return false;
        }

        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null)
        {
            return true;
        }

        if (!TryComputePassLaneInterceptDistance(bb.PhysicalState.Position, teamBB, out float selfDist))
        {
            return true;
        }

        float fieldLen = teamBB.FieldInfo.FieldLength;
        float margin = fieldLen * 0.025f;
        Vector3 selfPos = bb.PhysicalState.Position;
        foreach (Vector3 allyPos in teamBB.BasicInfo.TeammatePositions)
        {
            if (Vector3.Distance(allyPos, selfPos) < 0.1f)
            {
                continue;
            }

            if (ComputePassLaneBlockUrgencyAt(allyPos, teamBB) < 0.75f)
            {
                continue;
            }

            if (TryComputePassLaneInterceptDistance(allyPos, teamBB, out float allyDist)
                && allyDist + margin < selfDist)
            {
                return false;
            }
        }

        return true;
    }

    private static bool TryComputePassLaneInterceptDistance(
        Vector3 playerPos,
        TeamBlackboard teamBB,
        out float distance)
    {
        distance = float.MaxValue;
        if (!TryEvaluatePassLaneGeometryAt(playerPos, teamBB, out PassLaneGeometry geo))
        {
            return false;
        }

        Vector3 ownerPos = teamBB.BallInfo.BallOwnerPosition;
        Vector3 passTarget = default;
        float passTargetDist = float.MaxValue;
        float markThreshold = teamBB.FieldInfo.FieldLength * 0.15f;
        foreach (Vector3 enemyPos in teamBB.BasicInfo.EnemyPositions)
        {
            if (Vector3.Distance(enemyPos, ownerPos) <= 0.1f)
            {
                continue;
            }

            bool isMarked = false;
            foreach (Vector3 allyPos in teamBB.BasicInfo.TeammatePositions)
            {
                if (Vector3.Distance(allyPos, playerPos) < 0.1f)
                {
                    continue;
                }

                if (Vector3.Distance(allyPos, enemyPos) <= markThreshold)
                {
                    isMarked = true;
                    break;
                }
            }

            if (isMarked)
            {
                continue;
            }

            float distFromOwner = Vector3.Distance(enemyPos, ownerPos);
            if (distFromOwner < passTargetDist)
            {
                passTargetDist = distFromOwner;
                passTarget = enemyPos;
            }
        }

        if (passTargetDist >= float.MaxValue * 0.5f)
        {
            return false;
        }

        Vector3 passDir = passTarget - ownerPos;
        passDir.y = 0f;
        if (passDir.sqrMagnitude < 0.01f)
        {
            return false;
        }

        float laneLength = passDir.magnitude;
        passDir /= laneLength;
        Vector3 ownerToPlayer = playerPos - ownerPos;
        ownerToPlayer.y = 0f;
        float along = Mathf.Clamp(Vector3.Dot(ownerToPlayer, passDir), 0f, laneLength);
        Vector3 closest = ownerPos + passDir * along;
        distance = Vector3.Distance(playerPos, closest);
        return geo.LaneAlign > 0.1f;
    }

    /// <summary>0〜1。高いほど BlockPassLane が MTD より適切。</summary>
    public static float ComputePassLaneBlockUrgency(PlayerBlackboard bb)
    {
        if (!TryEvaluatePassLaneGeometry(bb, out PassLaneGeometry geo))
        {
            return 0f;
        }

        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        float fieldLen = teamBB != null ? teamBB.FieldInfo.FieldLength : 20f;
        return ComputePassLaneBlockUrgencyFromGeometry(geo, fieldLen);
    }

    /// <summary>パスレーン遮断が優先なら MTD に委譲ペナルティを付与（0.1 下限の同点化を防ぐ）。</summary>
    public static float ComputePassLaneDelegationPenalty(PlayerBlackboard bb)
    {
        float urgency = ComputePassLaneBlockUrgency(bb);
        if (urgency < 0.75f)
        {
            return 0f;
        }

        return urgency * 0.9f;
    }

    public readonly struct DefensiveRetreatLineSample
    {
        public float GoalSide { get; }
        public float FieldLength { get; }

        public DefensiveRetreatLineSample(float goalSide, float fieldLength)
        {
            GoalSide = goalSide;
            FieldLength = fieldLength;
        }

        public float AheadOfLineDistance => GoalSide < 0f ? -GoalSide : 0f;

        public bool IsAheadOfLine() => GoalSide < -FieldLength * 0.02f;

        public bool IsSignificantlyAhead() => GoalSide < -FieldLength * 0.04f;

        public float AheadRatio(float referenceSpanRatio = 0.35f) =>
            Mathf.Clamp(AheadOfLineDistance / Mathf.Max(FieldLength * referenceSpanRatio, 0.01f), 0f, 1f);
    }

    /// <summary>RetreatToDefensiveLine と同じ幾何で、プレイヤーが守備ラインより敵陣側にいる度合いを返す。</summary>
    public static bool TrySampleDefensiveRetreatLine(
        PlayerBlackboard bb,
        float retreatDepthRatio,
        float centralBias,
        out DefensiveRetreatLineSample sample)
    {
        sample = default;
        if (bb == null)
        {
            return false;
        }

        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null)
        {
            return false;
        }

        float fieldLen = teamBB.FieldInfo.FieldLength;
        float depth = fieldLen * retreatDepthRatio;
        Vector3 ownGoal = teamBB.FieldInfo.OwnGoalPosition;
        Vector3 center = teamBB.FieldInfo.FieldCenter;
        Vector3 ball = teamBB.BallInfo.BallPosition;

        Vector3 linePoint = Vector3.Lerp(
            center,
            new Vector3(center.x, center.y, ownGoal.z + depth * Mathf.Sign(center.z - ownGoal.z)),
            centralBias);
        Vector3 toBallLateral = Vector3.ProjectOnPlane(ball - linePoint, Vector3.up);
        linePoint += Vector3.ClampMagnitude(toBallLateral, teamBB.FieldInfo.FieldWidth * 0.15f)
            * (1f - centralBias);

        Vector3 playerPos = bb.PhysicalState.Position;
        Vector3 fromLineToGoal = ownGoal - linePoint;
        if (fromLineToGoal.sqrMagnitude < 0.01f)
        {
            return false;
        }

        float goalSide = Vector3.Dot(playerPos - linePoint, fromLineToGoal.normalized);
        sample = new DefensiveRetreatLineSample(goalSide, fieldLen);
        return true;
    }

    public static float ComputeOverextendedDefensePenalty(
        PlayerBlackboard bb,
        float retreatDepthRatio = 0.28f,
        float centralBias = 0.6f,
        float minUrgency = 0.45f,
        float maxPenalty = 2.15f)
    {
        float urgency = ComputeSevereRetreatOverextensionUrgency(bb, retreatDepthRatio, centralBias);
        if (urgency < minUrgency)
        {
            return 0f;
        }

        float t = (urgency - minUrgency) / Mathf.Max(1f - minUrgency, 0.01f);
        return t * maxPenalty;
    }

    /// <summary>
    /// 守備ラインより前にいるが、保持者へのプレッシャー位置でもない「戻るべき」局面の緊急度（0〜1）。
    /// </summary>
    public static float ComputeSevereRetreatOverextensionUrgency(
        PlayerBlackboard bb,
        float retreatDepthRatio = 0.28f,
        float centralBias = 0.6f)
    {
        if (!TrySampleDefensiveRetreatLine(bb, retreatDepthRatio, centralBias, out DefensiveRetreatLineSample line)
            || !line.IsSignificantlyAhead())
        {
            return 0f;
        }

        float aheadRatio = line.AheadRatio();
        if (aheadRatio < 0.55f)
        {
            return 0f;
        }

        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null)
        {
            return 0f;
        }

        float fieldLen = line.FieldLength;
        Vector3 ownerPos = teamBB.BallInfo.BallOwnerPosition;
        Vector3 playerPos = bb.PhysicalState.Position;
        float distToOwner = Vector3.Distance(playerPos, ownerPos);
        float optimalPressDistance = fieldLen * 0.12f;
        float pressMisalignment = Mathf.Clamp01(
            Mathf.Abs(distToOwner - optimalPressDistance) / Mathf.Max(optimalPressDistance * 1.2f, 0.01f));

        if (distToOwner <= fieldLen * 0.16f)
        {
            pressMisalignment *= 0.35f;
        }

        float urgency = aheadRatio * pressMisalignment;
        float lateralOffset = Mathf.Abs(playerPos.x - ownerPos.x);
        float lateralRatio = lateralOffset / Mathf.Max(teamBB.FieldInfo.FieldWidth * 0.5f, 0.01f);
        if (aheadRatio >= 0.75f && lateralRatio >= 0.22f)
        {
            urgency = Mathf.Max(urgency, aheadRatio * 0.58f);
        }

        return urgency;
    }
}
