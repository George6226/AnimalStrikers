using System.Collections.Generic;
using System.Linq;
using Game.Goap;
using Game.Goap.Goals;
using UnityEngine;

/// <summary>
/// 味方ボール時: TeamBallSupport ゴール＋複数サポートアクションの動的コスト競争。
/// </summary>
public static class TeammateNpcSupportPlanning
{
    public const float TeamBallTacticalSupportPriority = 88f;

    /// <summary>
    /// GetOpen を本番候補に戻す（単体・本番選出検証で使用）。
    /// </summary>
    public const bool TemporarilyDisableGetOpenAndRunBehind = false;

    /// <summary>
    /// 単体検証時に TeamBallSupport の候補を1アクションに絞る（GoapSupportActionVerificationSetup が設定）。
    /// </summary>
    public static GoapSupportActionUnderTest VerificationOnlySupportAction { get; private set; }

    /// <summary>後方互換: CreateSupportAngle 単体検証 ON/OFF。</summary>
    public static bool VerificationOnlyCreateSupportAngle =>
        VerificationOnlySupportAction == GoapSupportActionUnderTest.CreateSupportAngle;

    public static void SetVerificationOnlySupportAction(GoapSupportActionUnderTest action)
    {
        VerificationOnlySupportAction = action;
    }

    public static void SetVerificationOnlyCreateSupportAngle(bool enabled)
    {
        VerificationOnlySupportAction = enabled
            ? GoapSupportActionUnderTest.CreateSupportAngle
            : GoapSupportActionUnderTest.None;
    }

    private const float TemporarilyDisabledActionCostPenalty = 50f;
    private const float LaneTargetDriftThresholdRatio = 0.06f;
    private const float LaneTargetDriftMinMeters = 0.6f;
    private const float LaneTargetArriveThresholdRatio = 0.045f;
    private const float LaneTargetArriveMinMeters = 0.45f;
    private const float WideLaneLateralArriveThresholdRatio = 0.06f;
    private const float WideLaneNearOwnerMaxAheadRatio = 0.025f;
    private const float WideLaneNearOwnerMaxBehindRatio = 0.02f;

    /// <summary>
    /// サポート到達は IS_IN_PASS_RECEIVE_POSITION。IS_MOVING だけだと MoveToDefensivePosition が誤選択される。
    /// </summary>
    private static readonly List<GoapCondition> TacticalSupportPlanningRequiredFacts = new()
    {
        new GoapCondition(SymbolTag.Action.CAN_MOVE, true),
        new GoapCondition(SymbolTag.Action.IS_IN_PASS_RECEIVE_POSITION, true),
        new GoapCondition(SymbolTag.Action.IS_MAINTAINING_SUPPORT_RELATIONSHIP, true),
    };

    public static List<GoapCondition> GetTacticalSupportPlanningRequiredFacts()
    {
        return TacticalSupportPlanningRequiredFacts;
    }

    public static bool IsTeamBallAttackContext(TeamBlackboard teamBB)
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

        return ball.TeamHasBall && !ball.EnemyHasBall;
    }

    public static bool ShouldUseTacticalSupportGoal(PlayerBlackboard bb)
    {
        if (!TeammateNpcGoapRoleDifferentiation.Enabled || !IsAllyFieldSupportActor(bb))
        {
            return false;
        }

        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        return IsTeamBallAttackContext(teamBB);
    }

    /// <summary>味方ボール時にスロット別コストを適用する対象（TeammateNpc 以外の味方フィールドプレイヤーも含む）。</summary>
    public static bool ShouldApplySupportActionCostDifferentiation(PlayerBlackboard bb)
    {
        if (bb.GetFact(new Fact(SymbolTag.Basic.HAS_BALL, "true")) == true)
        {
            return false;
        }

        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (!IsTeamBallAttackContext(teamBB))
        {
            return false;
        }

        return IsAllyFieldSupportActor(bb) || IsCentralWidthLayoutFieldPlayer(bb);
    }

    private static bool IsFieldPlayer(PlayerBlackboard bb)
    {
        if (bb?.BasicData?.Self == null)
        {
            return false;
        }

        var assignment = bb.BasicData.Self.GetComponentInParent<AnimalControlAssignment>()
            ?? bb.BasicData.Self.GetComponent<AnimalControlAssignment>();
        return assignment != null && !assignment.IsGoalkeeperNpc;
    }

    private static bool IsCentralWidthLayoutFieldPlayer(PlayerBlackboard bb)
    {
        if (!IsFieldPlayer(bb))
        {
            return false;
        }

        return TeammateNpcGoapRoleDifferentiation.ResolveFormationSlot(bb) == 0;
    }

    /// <summary>GK・操作キャラ以外の味方フィールドプレイヤー。</summary>
    public static bool IsAllyFieldSupportActor(PlayerBlackboard bb)
    {
        if (bb?.BasicData?.Self == null)
        {
            return false;
        }

        var assignment = bb.BasicData.Self.GetComponentInParent<AnimalControlAssignment>()
            ?? bb.BasicData.Self.GetComponent<AnimalControlAssignment>();
        if (assignment == null)
        {
            return false;
        }

        if (assignment.IsHumanControlled || assignment.IsGoalkeeperNpc)
        {
            return false;
        }

        return assignment.Role == AnimalControlRole.TeammateNpc
            || assignment.Role == AnimalControlRole.Unassigned;
    }

    public static bool ShouldIgnorePassReceivePositionGate(PlayerBlackboard bb)
    {
        return ShouldUseTacticalSupportGoal(bb);
    }

    /// <summary>
    /// 3レーン幅レイアウトの追従対象（slot0〜2 の味方フィールドプレイヤー）。
    /// </summary>
    public static bool ShouldTrackWidthLayoutLaneTarget(PlayerBlackboard bb)
    {
        if (bb == null)
        {
            return false;
        }

        if (bb.GetFact(new Fact(SymbolTag.Basic.HAS_BALL, "true")) == true)
        {
            return false;
        }

        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (!IsTeamBallAttackContext(teamBB) || !IsFieldPlayer(bb))
        {
            return false;
        }

        int slot = TeammateNpcGoapRoleDifferentiation.ResolveFormationSlot(bb);
        return slot >= 0 && slot <= 2;
    }

    /// <summary>現在位置とレーン理想位置の距離（m）。追従対象外は 0。</summary>
    public static float MeasureWidthLayoutTargetDrift(PlayerBlackboard bb)
    {
        if (!ShouldTrackWidthLayoutLaneTarget(bb))
        {
            return 0f;
        }

        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null)
        {
            return 0f;
        }

        Vector3 selfPos = bb.PhysicalState.Position;
        int slot = TeammateNpcGoapRoleDifferentiation.ResolveFormationSlot(bb);
        Vector3 layoutTarget = CreateSupportAnglePositioning.GetLayoutIdealPosition(slot, teamBB);
        return Vector3.Distance(selfPos, layoutTarget);
    }

    public static float GetLaneTargetArriveThreshold(TeamBlackboard teamBB)
    {
        if (teamBB == null)
        {
            return LaneTargetArriveMinMeters;
        }

        return Mathf.Max(
            LaneTargetArriveMinMeters,
            teamBB.FieldInfo.FieldLength * LaneTargetArriveThresholdRatio);
    }

    public static float GetLaneTargetFollowDriftThreshold(TeamBlackboard teamBB)
    {
        if (teamBB == null)
        {
            return LaneTargetDriftMinMeters;
        }

        return Mathf.Max(
            LaneTargetDriftMinMeters,
            teamBB.FieldInfo.FieldLength * LaneTargetDriftThresholdRatio);
    }

    /// <summary>レーン理想位置に十分近い（ゴール達成・関係維持の判定用）。</summary>
    public static bool IsAtWidthLayoutLaneTarget(PlayerBlackboard bb)
    {
        if (!ShouldTrackWidthLayoutLaneTarget(bb))
        {
            return true;
        }

        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null)
        {
            return false;
        }

        return MeasureWidthLayoutTargetDrift(bb) <= GetLaneTargetArriveThreshold(teamBB);
    }

    /// <summary>割当ワイドレーンのアンカーとの横方向ずれ（m）。Z 追従差は含めない。</summary>
    public static float MeasureWidthLayoutLateralDrift(PlayerBlackboard bb)
    {
        if (!ShouldTrackWidthLayoutLaneTarget(bb))
        {
            return 0f;
        }

        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null)
        {
            return float.MaxValue;
        }

        int slot = TeammateNpcGoapRoleDifferentiation.ResolveFormationSlot(bb);
        Vector3 selfPos = bb.PhysicalState.Position;
        Vector3 anchor = CreateSupportAnglePositioning.GetAssignedLaneAnchor(slot, teamBB);
        Vector3 ownerPos = teamBB.BallInfo.BallOwnerPosition;
        Vector3 toGoal = (teamBB.FieldInfo.EnemyGoalPosition - ownerPos).normalized;
        if (toGoal.sqrMagnitude < 0.0001f)
        {
            toGoal = Vector3.forward;
        }

        Vector3 right = Vector3.Cross(Vector3.up, toGoal).normalized;
        Vector3 fieldCenter = teamBB.FieldInfo.FieldCenter;
        float lateralSelf = Vector3.Dot(selfPos - fieldCenter, right);
        float lateralAnchor = Vector3.Dot(anchor - fieldCenter, right);
        return Mathf.Abs(lateralSelf - lateralAnchor);
    }

    public static float GetWideLaneLateralArriveThreshold(TeamBlackboard teamBB)
    {
        if (teamBB == null)
        {
            return LaneTargetArriveMinMeters;
        }

        return Mathf.Max(
            LaneTargetArriveMinMeters,
            teamBB.FieldInfo.FieldWidth * WideLaneLateralArriveThresholdRatio);
    }

    /// <summary>CF 保持時、翼が割当ワイドレーンの横位置にいる（#6 理想レーン上など）。</summary>
    public static bool IsOnAssignedWideLaneLaterally(PlayerBlackboard bb)
    {
        if (!ShouldTrackWidthLayoutLaneTarget(bb))
        {
            return true;
        }

        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null || !IsTeamBallAttackContext(teamBB))
        {
            return false;
        }

        int slot = TeammateNpcGoapRoleDifferentiation.ResolveFormationSlot(bb);
        if (slot != 1 && slot != 2)
        {
            return false;
        }

        if (CreateSupportAnglePositioning.ResolveBallOwnerFormationSlot(teamBB) != 0)
        {
            return false;
        }

        if (CreateSupportAnglePositioning.ResolveOwnerZone(teamBB) != CreateSupportAnglePositioning.OwnerZone.Central)
        {
            return false;
        }

        return MeasureWidthLayoutLateralDrift(bb) <= GetWideLaneLateralArriveThreshold(teamBB);
    }

    /// <summary>保持者からの縦方向オフセット（攻撃方向、+ = 前）。</summary>
    public static float MeasureForwardOffsetFromOwner(PlayerBlackboard bb)
    {
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null || bb == null)
        {
            return float.MaxValue;
        }

        Vector3 ownerPos = teamBB.BallInfo.BallOwnerPosition;
        Vector3 toGoal = (teamBB.FieldInfo.EnemyGoalPosition - ownerPos).normalized;
        if (toGoal.sqrMagnitude < 0.0001f)
        {
            toGoal = Vector3.forward;
        }

        return Vector3.Dot(bb.PhysicalState.Position - ownerPos, toGoal);
    }

    public static bool IsWingNearOwnerForwardPlane(PlayerBlackboard bb, TeamBlackboard teamBB)
    {
        if (bb == null || teamBB == null)
        {
            return false;
        }

        float forwardFromOwner = MeasureForwardOffsetFromOwner(bb);
        float fieldLength = teamBB.FieldInfo.FieldLength;
        float maxAhead = fieldLength * WideLaneNearOwnerMaxAheadRatio;
        float maxBehind = fieldLength * WideLaneNearOwnerMaxBehindRatio;
        return forwardFromOwner >= -maxBehind && forwardFromOwner <= maxAhead;
    }

    /// <summary>CF 保持・横位置が割当ワイドレーン上かつ保持者付近（#6 理想レーン上）。手前/後方ずれは GetOpen 優先。</summary>
    public static bool IsOnIdealWideLaneForCreateSupportAngle(PlayerBlackboard bb)
    {
        if (!IsOnAssignedWideLaneLaterally(bb))
        {
            return false;
        }

        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        return teamBB != null && IsWingNearOwnerForwardPlane(bb, teamBB);
    }

    /// <summary>3D 到達、または理想ワイドレーン横位置＋保持者付近なら CSA を GetOpen より優先する。</summary>
    public static bool ShouldPreferCreateSupportAngleOverGetOpen(PlayerBlackboard bb) =>
        IsAtWidthLayoutLaneTarget(bb) || IsOnIdealWideLaneForCreateSupportAngle(bb);

    public static bool IsFarFromWidthLayoutLaneTarget(PlayerBlackboard bb)
    {
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null)
        {
            return false;
        }

        return MeasureWidthLayoutTargetDrift(bb) > GetLaneTargetFollowDriftThreshold(teamBB);
    }

    /// <summary>#6 AtCorrectLanes 修正用: 翼 slot のレーン判定・WM 事実・コスト調整を1行で返す。</summary>
    public static string BuildAtCorrectLanesPlanningDiag(
        PlayerBlackboard bb,
        CreateSupportAngleActionSO csaAction = null,
        GetOpenActionSO getOpenAction = null)
    {
        if (bb == null)
        {
            return "bb=null";
        }

        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        int slot = TeammateNpcGoapRoleDifferentiation.ResolveFormationSlot(bb);
        Vector3 selfPos = bb.PhysicalState.Position;
        int playerId = bb.BasicData != null ? bb.BasicData.PlayerID : -1;
        int ownerSlot = teamBB != null
            ? CreateSupportAnglePositioning.ResolveBallOwnerFormationSlot(teamBB)
            : -1;
        var ownerZone = teamBB != null
            ? CreateSupportAnglePositioning.ResolveOwnerZone(teamBB)
            : CreateSupportAnglePositioning.OwnerZone.Central;

        Vector3 layoutIdeal = teamBB != null
            ? CreateSupportAnglePositioning.GetLayoutIdealPosition(slot, teamBB)
            : Vector3.zero;
        Vector3 laneAnchor = teamBB != null
            ? CreateSupportAnglePositioning.GetAssignedLaneAnchor(slot, teamBB)
            : Vector3.zero;
        float drift = MeasureWidthLayoutTargetDrift(bb);
        float lateralDrift = MeasureWidthLayoutLateralDrift(bb);
        float arriveThreshold = GetLaneTargetArriveThreshold(teamBB);
        float lateralThr = GetWideLaneLateralArriveThreshold(teamBB);
        bool atLane = IsAtWidthLayoutLaneTarget(bb);
        bool onWideLaneLaterally = IsOnAssignedWideLaneLaterally(bb);
        bool nearOwnerForward = teamBB != null && IsWingNearOwnerForwardPlane(bb, teamBB);
        float forwardFromOwner = MeasureForwardOffsetFromOwner(bb);
        bool onIdealWideLane = IsOnIdealWideLaneForCreateSupportAngle(bb);
        bool preferCsa = ShouldPreferCreateSupportAngleOverGetOpen(bb);
        bool trackLane = ShouldTrackWidthLayoutLaneTarget(bb);

        bool wmPassReceive = bb.GetFact(new Fact(SymbolTag.Action.IS_IN_PASS_RECEIVE_POSITION, "true")) == true;
        bool wmSupportRel = bb.GetFact(new Fact(SymbolTag.Action.IS_MAINTAINING_SUPPORT_RELATIONSHIP, "true")) == true;
        bool geomSupportRel = teamBB != null && PlayerBlackboardCalculator.IsMaintainingSupportRelationship(
            selfPos,
            teamBB.BallInfo.BallOwnerPosition,
            teamBB.FieldInfo.EnemyGoalPosition,
            teamBB.FieldInfo.FieldLength);
        bool calcPassReceive = teamBB != null && PlayerBlackboardCalculator.CalculateIsInPassReceivePosition(
            teamBB.BallInfo.TeamHasBall,
            bb.BallState.HasBall,
            bb.ActionState.IsStunned,
            bb.BallState.BallDistance,
            teamBB.FieldInfo.FieldLength,
            selfPos,
            teamBB.BasicInfo.EnemyPositions,
            teamBB.BallInfo.BallOwnerPosition,
            teamBB.FieldInfo.EnemyGoalPosition);
        bool evalPassReceive = EvaluatePassReceivePosition(bb, teamBB);
        bool evalSupportRel = EvaluateMaintainingSupportRelationship(bb, teamBB);
        bool blocksGetOpen = BlocksCreateSupportAngleWhenGetOpenPreferred(bb);

        float widthAdjCsa = csaAction != null
            ? ComputeWidthLayoutActionCostAdjustment(csaAction, bb)
            : float.NaN;
        float widthAdjGo = getOpenAction != null
            ? ComputeWidthLayoutActionCostAdjustment(getOpenAction, bb)
            : float.NaN;
        float costCsa = csaAction != null ? csaAction.CalculateDynamicCost(bb) : float.NaN;
        float costGo = getOpenAction != null ? getOpenAction.CalculateDynamicCost(bb) : float.NaN;

        return
            $"playerId={playerId} slot={slot} ownerSlot={ownerSlot} ownerZone={ownerZone} " +
            $"self={FmtVec(selfPos)} layoutIdeal={FmtVec(layoutIdeal)} laneAnchor={FmtVec(laneAnchor)} " +
            $"drift={drift:F2} lateralDrift={lateralDrift:F2} arriveThr={arriveThreshold:F2} lateralThr={lateralThr:F2} " +
            $"fwdFromOwner={forwardFromOwner:F2} atLane={atLane} onWideLaneLaterally={onWideLaneLaterally} " +
            $"nearOwnerForward={nearOwnerForward} onIdealWideLane={onIdealWideLane} preferCSA={preferCsa} trackLane={trackLane} " +
            $"wmPassReceive={wmPassReceive} wmSupportRel={wmSupportRel} " +
            $"calcPassReceive={calcPassReceive} geomSupportRel={geomSupportRel} " +
            $"evalPassReceive={evalPassReceive} evalSupportRel={evalSupportRel} blocksGetOpen={blocksGetOpen} " +
            $"widthAdjCSA={(float.IsNaN(widthAdjCsa) ? "n/a" : widthAdjCsa.ToString("F2"))} " +
            $"widthAdjGO={(float.IsNaN(widthAdjGo) ? "n/a" : widthAdjGo.ToString("F2"))} " +
            $"costCSA={(float.IsNaN(costCsa) ? "n/a" : costCsa.ToString("F2"))} " +
            $"costGO={(float.IsNaN(costGo) ? "n/a" : costGo.ToString("F2"))}";
    }

    private static string FmtVec(Vector3 v) => $"({v.x:F1},{v.y:F1},{v.z:F1})";

    /// <summary>
    /// WM 更新用: 幾何サポート関係に加え、3レーン理想位置への追従も満たすまで true にしない。
    /// </summary>
    public static bool EvaluateMaintainingSupportRelationship(PlayerBlackboard bb, TeamBlackboard teamBB)
    {
        if (teamBB == null || bb == null)
        {
            return false;
        }

        if (!IsTeamBallAttackContext(teamBB))
        {
            return true;
        }

        if (bb.BallState.HasBall)
        {
            return true;
        }

        bool geometric = PlayerBlackboardCalculator.IsMaintainingSupportRelationship(
            bb.PhysicalState.Position,
            teamBB.BallInfo.BallOwnerPosition,
            teamBB.FieldInfo.EnemyGoalPosition,
            teamBB.FieldInfo.FieldLength);
        if (!geometric)
        {
            return false;
        }

        if (!ShouldTrackWidthLayoutLaneTarget(bb))
        {
            return true;
        }

        return IsAtWidthLayoutLaneTarget(bb);
    }

    /// <summary>
    /// WM 更新用: レーン追従対象は理想位置にいるまで IS_IN_PASS_RECEIVE_POSITION を true にしない。
    /// </summary>
    public static bool EvaluatePassReceivePosition(PlayerBlackboard bb, TeamBlackboard teamBB)
    {
        if (teamBB == null || bb == null)
        {
            return false;
        }

        bool inPosition = PlayerBlackboardCalculator.CalculateIsInPassReceivePosition(
            teamBB.BallInfo.TeamHasBall,
            bb.BallState.HasBall,
            bb.ActionState.IsStunned,
            bb.BallState.BallDistance,
            teamBB.FieldInfo.FieldLength,
            bb.PhysicalState.Position,
            teamBB.BasicInfo.EnemyPositions,
            teamBB.BallInfo.BallOwnerPosition,
            teamBB.FieldInfo.EnemyGoalPosition);
        if (!inPosition)
        {
            return false;
        }

        if (!ShouldTrackWidthLayoutLaneTarget(bb))
        {
            return true;
        }

        return IsAtWidthLayoutLaneTarget(bb);
    }

    /// <summary>
    /// 戦術サポートでまだ移動が必要か（空プラン＝立ち止まりを避ける判定）。
    /// </summary>
    public static bool NeedsTacticalSupportMovement(PlayerBlackboard bb)
    {
        if (!ShouldTrackWidthLayoutLaneTarget(bb))
        {
            return false;
        }

        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (!IsTeamBallAttackContext(teamBB) || bb == null || teamBB == null)
        {
            return false;
        }

        if (!IsAtWidthLayoutLaneTarget(bb))
        {
            return true;
        }

        // 幅レイアウトは理想レーン到達後もレーン維持のため移動を継続（空プラン回避）
        return true;
    }

    /// <summary>
    /// プランナーが空プランを返したとき、戦術サポート移動を強制する。
    /// </summary>
    public static bool TryBuildForcedTacticalSupportPlan(
        PlayerBlackboard bb,
        IEnumerable<GoapActionSO> scopedActions,
        out Queue<GoapActionSO> plan)
    {
        plan = null;
        if (!NeedsTacticalSupportMovement(bb) || scopedActions == null)
        {
            return false;
        }

        GoapActionSO action = VerificationOnlySupportAction != GoapSupportActionUnderTest.None
            ? scopedActions.FirstOrDefault(a => VerificationOnlySupportAction.MatchesAction(a))
            : ResolveForcedSupportActionForSlot(bb, scopedActions);
        if (action == null)
        {
            return false;
        }

        plan = new Queue<GoapActionSO>();
        plan.Enqueue(action);
        return true;
    }

    public static bool BlocksWhenAlreadyInPassReceivePosition(PlayerBlackboard bb)
    {
        return !ShouldIgnorePassReceivePositionGate(bb);
    }

    /// <summary>
    /// 味方ボール時の翼プレイヤーは 3 レーン幅レイアウトのため CreateSupportAngle 専用とする。
    /// </summary>
    public static bool BlocksMoveToSupportForWingLayout(PlayerBlackboard bb)
    {
        if (!ShouldUseTacticalSupportGoal(bb))
        {
            return false;
        }

        int slot = TeammateNpcGoapRoleDifferentiation.ResolveFormationSlot(bb);
        return slot == 1 || slot == 2;
    }

    /// <summary>
    /// slot0 は MoveToSupport 経由でも 3 レーン幅レイアウトの中央レーンへ配置する。
    /// </summary>
    public static bool ShouldUseWidthLayoutSupportPosition(PlayerBlackboard bb)
    {
        if (bb.GetFact(new Fact(SymbolTag.Basic.HAS_BALL, "true")) == true)
        {
            return false;
        }

        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (!IsTeamBallAttackContext(teamBB) || !IsFieldPlayer(bb))
        {
            return false;
        }

        int slot = TeammateNpcGoapRoleDifferentiation.ResolveFormationSlot(bb);
        return slot == 0;
    }

    /// <summary>
    /// slot0 は中央レーン専用のため CreateSupportAngle を選ばせない（翼と MoveToSupport の関係の逆）。
    /// </summary>
    public static bool BlocksCreateSupportAngleForCentralWidthLayout(PlayerBlackboard bb)
    {
        if (VerificationOnlySupportAction != GoapSupportActionUnderTest.None)
        {
            return false;
        }

        return ShouldUseWidthLayoutSupportPosition(bb);
    }

    /// <summary>
    /// 翼保持時の slot0（CF）は中央レーン追従（MTS）専用。GetOpen はコスト下限で MTS と同率になるためブロックする。
    /// </summary>
    public static bool BlocksGetOpenForCentralSupportWhenWingOwnerHolds(PlayerBlackboard bb)
    {
        if (VerificationOnlySupportAction != GoapSupportActionUnderTest.None)
        {
            return false;
        }

        if (!ShouldUseTacticalSupportGoal(bb))
        {
            return false;
        }

        int slot = TeammateNpcGoapRoleDifferentiation.ResolveFormationSlot(bb);
        if (slot != 0)
        {
            return false;
        }

        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (!IsTeamBallAttackContext(teamBB))
        {
            return false;
        }

        int ownerSlot = CreateSupportAnglePositioning.ResolveBallOwnerFormationSlot(teamBB);
        return ownerSlot == 1 || ownerSlot == 2;
    }

    /// <summary>
    /// CF 保持時の翼: 受け位置・サポート関係が未達なら GetOpen を優先し CSA を候補から外す。
    /// </summary>
    public static bool BlocksCreateSupportAngleWhenGetOpenPreferred(PlayerBlackboard bb)
    {
        if (VerificationOnlySupportAction != GoapSupportActionUnderTest.None)
        {
            return false;
        }

        if (!ShouldUseTacticalSupportGoal(bb))
        {
            return false;
        }

        int slot = TeammateNpcGoapRoleDifferentiation.ResolveFormationSlot(bb);
        if (slot != 1 && slot != 2)
        {
            return false;
        }

        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (!IsTeamBallAttackContext(teamBB))
        {
            return false;
        }

        if (CreateSupportAnglePositioning.ResolveBallOwnerFormationSlot(teamBB) != 0)
        {
            return false;
        }

        if (ShouldPreferCreateSupportAngleOverGetOpen(bb))
        {
            return false;
        }

        bool needsPassReceive =
            bb.GetFact(new Fact(SymbolTag.Action.IS_IN_PASS_RECEIVE_POSITION, "true")) != true;
        bool needsSupportRelationship =
            bb.GetFact(new Fact(SymbolTag.Action.IS_MAINTAINING_SUPPORT_RELATIONSHIP, "true")) != true;
        return needsPassReceive || needsSupportRelationship;
    }

    /// <summary>
    /// 戦術攻撃アクション（GetOpen 等）の SO から外した IS_IN_PASS_RECEIVE=false の Runtime 代替。
    /// 味方ボール・非保持者・移動可を TeamBB で判定し、非戦術時は既に受け位置なら false。
    /// </summary>
    public static bool PassesTacticalAttackRuntimeGate(PlayerBlackboard bb)
    {
        if (bb == null)
        {
            return false;
        }

        if (bb.GetFact(new Fact(SymbolTag.Action.CAN_MOVE, "true")) != true)
        {
            return false;
        }

        if (bb.GetFact(new Fact(SymbolTag.Basic.HAS_BALL, "true")) == true)
        {
            return false;
        }

        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (!IsTeamBallAttackContext(teamBB))
        {
            return false;
        }

        if (BlocksWhenAlreadyInPassReceivePosition(bb)
            && bb.GetFact(new Fact(SymbolTag.Action.IS_IN_PASS_RECEIVE_POSITION, "true")) == true)
        {
            return false;
        }

        return true;
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

    private static GoapActionSO ResolveForcedSupportActionForSlot(
        PlayerBlackboard bb,
        IEnumerable<GoapActionSO> scopedActions)
    {
        if (scopedActions == null)
        {
            return null;
        }

        int slot = TeammateNpcGoapRoleDifferentiation.ResolveFormationSlot(bb);
        var candidates = new List<GoapActionSO>();
        if (slot == 0)
        {
            TryAddCandidate(scopedActions, candidates, typeof(MoveToSupportPositionActionSO));
            TryAddCandidate(scopedActions, candidates, typeof(CreateSupportAngleActionSO));
        }
        else
        {
            TryAddCandidate(scopedActions, candidates, typeof(GetOpenActionSO));
            TryAddCandidate(scopedActions, candidates, typeof(CreateSupportAngleActionSO));
            TryAddCandidate(scopedActions, candidates, typeof(MoveToSupportPositionActionSO));
        }

        GoapActionSO best = null;
        float bestCost = float.MaxValue;
        foreach (GoapActionSO action in candidates)
        {
            float cost = action.CalculateDynamicCost(bb);
            if (cost < bestCost)
            {
                bestCost = cost;
                best = action;
            }
        }

        return best;
    }

    private static void TryAddCandidate(
        IEnumerable<GoapActionSO> scopedActions,
        List<GoapActionSO> candidates,
        System.Type actionType)
    {
        GoapActionSO action = scopedActions.FirstOrDefault(a => actionType.IsInstanceOfType(a));
        if (action != null)
        {
            candidates.Add(action);
        }
    }

    public static float ComputeDynamicCost(
        GoapActionSO action,
        PlayerBlackboard bb,
        float baseCost,
        float situationalAdjustment)
    {
        if (VerificationOnlySupportAction != GoapSupportActionUnderTest.None
            && !VerificationOnlySupportAction.MatchesAction(action))
        {
            return TemporarilyDisabledActionCostPenalty + baseCost;
        }

        if (action is CreateSupportAngleActionSO
            && BlocksCreateSupportAngleForCentralWidthLayout(bb))
        {
            return TemporarilyDisabledActionCostPenalty + baseCost;
        }

        if (action is GetOpenActionSO
            && BlocksGetOpenForCentralSupportWhenWingOwnerHolds(bb))
        {
            return TemporarilyDisabledActionCostPenalty + baseCost;
        }

        float cost = baseCost + situationalAdjustment;
        if (!ShouldApplySupportActionCostDifferentiation(bb))
        {
            return Mathf.Max(0.1f, cost);
        }

        cost += ComputeRoleAffinityCost(action, bb);
        cost += ComputeWidthLayoutActionCostAdjustment(action, bb);
        cost += ComputeScoreContextAdjustment(action);
        cost += ComputeTemporaryDisabledActionPenalty(action);
        if (TeammateNpcGoapRoleDifferentiation.Enabled)
        {
            cost = TeammateNpcGoapRoleDifferentiation.AdjustActionCost(cost, bb, TeammateNpcTacticalMode.Support, action);
        }

        return Mathf.Max(0.1f, cost);
    }

    /// <summary>味方スコア − 敵スコア。負ならビハインド。</summary>
    public static int GetMatchScoreDiff()
    {
        const float cacheSeconds = 0.25f;
        if (Time.time - _cachedScoreDiffTime < cacheSeconds)
        {
            return _cachedScoreDiff;
        }

        _cachedScoreDiffTime = Time.time;
        int teamScore = 0;
        int enemyScore = 0;

        var display = Object.FindFirstObjectByType<DebugPlace_GameInfoDisplay>();
        if (display != null)
        {
            teamScore = display.GetPlayerScore();
            enemyScore = display.GetEnemyScore();
        }
        else if (ScoreManager.Instance != null)
        {
            bool isMaster = PhotonPlayerInfo.Instance == null || PhotonPlayerInfo.Instance.IsMasterClient;
            teamScore = ScoreManager.Instance.GetScore(isMaster);
            enemyScore = ScoreManager.Instance.GetScore(!isMaster);
        }
        else
        {
            teamScore = ES3.Load<int>(DataKey.DATAKEY_GAME_INFO + DataKey.INT_TEAM_SCORE, 0);
            enemyScore = ES3.Load<int>(DataKey.DATAKEY_GAME_INFO + DataKey.INT_ENEMY_SCORE, 0);
        }

        _cachedScoreDiff = teamScore - enemyScore;
        return _cachedScoreDiff;
    }

    private static int _cachedScoreDiff;
    private static float _cachedScoreDiffTime = -999f;

    private static float ComputeTemporaryDisabledActionPenalty(GoapActionSO action)
    {
        if (!TemporarilyDisableGetOpenAndRunBehind)
        {
            return 0f;
        }

        if (action is GetOpenActionSO || action is MakeRunBehindActionSO)
        {
            return TemporarilyDisabledActionCostPenalty;
        }

        return 0f;
    }

    /// <summary>ビハインド時は裏抜けを抑制し、GetOpen をやや優先する。</summary>
    private static float ComputeScoreContextAdjustment(GoapActionSO action)
    {
        int diff = GetMatchScoreDiff();
        if (diff >= 0)
        {
            return 0f;
        }

        float goalsBehind = -diff;
        if (action is MakeRunBehindActionSO)
        {
            return Mathf.Min(goalsBehind * 0.75f, 2f);
        }

        if (action is GetOpenActionSO)
        {
            return -Mathf.Min(goalsBehind * 0.15f, 0.35f);
        }

        return 0f;
    }

    /// <summary>
    /// 3レーン幅レイアウト時は翼プレイヤーに CreateSupportAngle を優先させ、MoveToSupport への逃げを抑える。
    /// </summary>
    private static float ComputeWidthLayoutActionCostAdjustment(GoapActionSO action, PlayerBlackboard bb)
    {
        if (action == null || bb == null)
        {
            return 0f;
        }

        int slot = TeammateNpcGoapRoleDifferentiation.ResolveFormationSlot(bb);
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (!IsTeamBallAttackContext(teamBB))
        {
            return 0f;
        }

        int ownerSlot = CreateSupportAnglePositioning.ResolveBallOwnerFormationSlot(teamBB);
        bool wingOwnerHolds = ownerSlot == 1 || ownerSlot == 2;

        if (slot == 0)
        {
            if (action is MoveToSupportPositionActionSO)
            {
                return wingOwnerHolds ? -0.55f : -0.45f;
            }

            if (action is CreateSupportAngleActionSO)
            {
                return wingOwnerHolds ? 0.75f : 0.55f;
            }

            return 0f;
        }

        if (slot != 1 && slot != 2)
        {
            return 0f;
        }

        if (wingOwnerHolds && slot != ownerSlot)
        {
            if (action is CreateSupportAngleActionSO)
            {
                return -0.55f;
            }

            if (action is GetOpenActionSO)
            {
                return 0.70f;
            }

            if (action is MoveToSupportPositionActionSO)
            {
                return 0.45f;
            }

            return 0f;
        }

        var ownerZone = CreateSupportAnglePositioning.ResolveOwnerZone(teamBB);
        if (ownerZone == CreateSupportAnglePositioning.OwnerZone.Central)
        {
            bool preferCsa = ShouldPreferCreateSupportAngleOverGetOpen(bb);

            if (action is CreateSupportAngleActionSO)
            {
                if (preferCsa)
                {
                    return -0.55f;
                }

                return BlocksCreateSupportAngleWhenGetOpenPreferred(bb) ? 0.55f : -0.25f;
            }

            if (action is GetOpenActionSO)
            {
                if (preferCsa)
                {
                    return 0.85f;
                }

                float adjust = bb.GetFact(new Fact(SymbolTag.Action.IS_IN_PASS_RECEIVE_POSITION, "true")) != true
                    ? -0.25f
                    : -0.05f;
                if (BlocksCreateSupportAngleWhenGetOpenPreferred(bb))
                {
                    adjust -= 0.15f;
                }

                return adjust;
            }

            if (action is MoveToSupportPositionActionSO)
            {
                return 0.2f;
            }

            return 0f;
        }

        var laneRole = CreateSupportAnglePositioning.ResolveSlotLaneRole(slot, ownerZone, ownerSlot);
        if (action is CreateSupportAngleActionSO)
        {
            return laneRole == CreateSupportAnglePositioning.SupportLaneRole.CentralChannel
                ? -0.55f
                : -0.4f;
        }

        if (action is MoveToSupportPositionActionSO)
        {
            return 0.45f;
        }

        return 0f;
    }

    /// <summary>編成スロットごとにサポート役割のコストをずらす（2体が同じ MoveToSupport だけにならない）。</summary>
    private static float ComputeRoleAffinityCost(GoapActionSO action, PlayerBlackboard bb)
    {
        if (action == null)
        {
            return 0f;
        }

        int slot = TeammateNpcGoapRoleDifferentiation.ResolveFormationSlot(bb);
        float adjust = 0f;

        if (action is MakeRunBehindActionSO)
        {
            adjust = slot == 0 ? -0.35f : 0.2f;
        }
        else if (action is CreateSupportAngleActionSO)
        {
            adjust = slot == 2 ? -0.3f : (slot == 1 ? -0.15f : 0.1f);
        }
        else if (action is GetOpenActionSO)
        {
            adjust = slot == 1 ? -0.30f : (slot == 0 ? -0.12f : (slot == 2 ? -0.15f : 0.05f));
        }
        else if (action is MoveToSupportPositionActionSO)
        {
            adjust = 0.18f;
        }

        return adjust;
    }
}
