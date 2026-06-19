using System.Collections.Generic;
using Game.Goap.Goals;
using UnityEngine;

/// <summary>
/// 段階4: 編成スロットに応じた GOAP 優先度・アクションコスト調整（2体が同じ位置に集まらない）。
/// </summary>
public static class TeammateNpcGoapRoleDifferentiation
{
    public static bool Enabled { get; set; } = true;

    private const float MinTeammateSeparation = 4f;
    private const float OverlapCostPerMeter = 2.2f;
    private const float GoalOverlapPenaltyScale = 3.5f;
    private const float SlotAffinityMaxBonus = 8f;
    /// <summary>MoveToFreeBall の到達距離と揃える（これより遠いときだけ追跡ゴールを立てる）。</summary>
    public const float FreeBallChaseArriveDistance = 1.2f;
    public const float FreeBallPursueMinDistance = 1.35f;

    /// <summary>条件A: この距離より人間がボールから遠いときのみ NPC 追跡を開始（m）。</summary>
    public const float FreeBallHumanFarEngageDistance = 10f;
    /// <summary>条件A: 追跡中に人間がこの距離以内に入ったら NPC 追跡を解除（m・ヒステリシス）。</summary>
    public const float FreeBallHumanNearReleaseDistance = 7f;
    /// <summary>条件A: 最寄り味方 NPC がボールからこの距離以内のときのみ代走（m）。</summary>
    public const float FreeBallNpcNearBallDistance = 6f;

    private const float FreeBallCloserGoalBonus = 12f;
    private const float FreeBallFartherGoalPenalty = 18f;
    private const float FreeBallCloserActionDiscount = 0.45f;
    private const float FreeBallFartherActionPenalty = 1.2f;
    private const float FreeBallLeaderTieBreakEpsilon = 0.15f;
    private const float ChaseLeaderLockSeconds = 10f;
    private const float NearBallFactDistance = 3f;

    private static int _lockedChaseLeaderPlayerId = -1;
    private static float _lockedChaseLeaderUntil;

    public static int DebugLockedChaseLeaderPlayerId => _lockedChaseLeaderPlayerId;

    public static TeammateNpcTacticalMode GetModeForGoal(GoapGoalSO goal)
    {
        if (goal is FreeBallRecoveryGoalSO) return TeammateNpcTacticalMode.ChaseBall;
        if (goal is TeamBallSupportGoalSO) return TeammateNpcTacticalMode.Support;
        if (goal is EnemyBallDefenseGoalSO or DefensivePositioningGoalSO) return TeammateNpcTacticalMode.Defend;
        return TeammateNpcTacticalMode.Hold;
    }

    public static int ResolveFormationSlot(PlayerBlackboard bb)
    {
        if (bb?.BasicData?.Self == null)
        {
            return 0;
        }

        var slot = bb.BasicData.Self.GetComponentInParent<AnimalFormationSlot>();
        return slot != null && slot.IsAssigned ? slot.Index : 0;
    }

    public static List<Vector3> CollectOtherTeammateFieldPositions(PlayerBlackboard selfBb, float minDistFromSelf = 0.5f)
    {
        var list = new List<Vector3>();
        if (selfBb?.BasicData?.Self == null)
        {
            return list;
        }

        var regist = TeamFacade.Instance != null ? TeamFacade.Instance.TeamRegist : null;
        if (regist == null)
        {
            return list;
        }

        Vector3 selfPos = selfBb.BasicData.Self.transform.position;
        float minSqr = minDistFromSelf * minDistFromSelf;

        foreach (var ally in regist.Allys)
        {
            if (ally == null || ally.IsGK())
            {
                continue;
            }

            if (ally.gameObject == selfBb.BasicData.Self
                || ally.transform.IsChildOf(selfBb.BasicData.Self.transform)
                || selfBb.BasicData.Self.transform.IsChildOf(ally.transform))
            {
                continue;
            }

            Vector3 p = ally.transform.position;
            if ((p - selfPos).sqrMagnitude < minSqr)
            {
                continue;
            }

            list.Add(p);
        }

        return list;
    }

    public static Vector3 PredictTacticalTarget(PlayerBlackboard bb, TeammateNpcTacticalMode mode)
    {
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (bb?.BasicData?.Self == null || teamBB == null)
        {
            return bb != null ? bb.PhysicalState.Position : Vector3.zero;
        }

        Vector3 selfPos = bb.BasicData.Self.transform.position;
        int slot = ResolveFormationSlot(bb);
        var others = CollectOtherTeammateFieldPositions(bb);

        var facade = bb.BasicData.Self.GetComponentInParent<AnimalFacade>()
            ?? bb.BasicData.Self.GetComponent<AnimalFacade>();
        var result = TeammateNpcTacticalPositionCalculator.Calculate(selfPos, slot, teamBB, others, facade);
        if (result.IsValid)
        {
            return result.TargetPosition;
        }

        if (mode == TeammateNpcTacticalMode.ChaseBall && teamBB.BallInfo.IsExistBall)
        {
            return teamBB.BallInfo.BallPosition;
        }

        return selfPos;
    }

    public static float ComputeOverlapCost(Vector3 target, IEnumerable<Vector3> otherTeammates)
    {
        if (otherTeammates == null)
        {
            return 0f;
        }

        float cost = 0f;
        foreach (Vector3 mate in otherTeammates)
        {
            Vector3 diff = target - mate;
            diff.y = 0f;
            float dist = diff.magnitude;
            if (dist < MinTeammateSeparation && dist > 0.01f)
            {
                cost += (MinTeammateSeparation - dist) * OverlapCostPerMeter;
            }
        }

        return cost;
    }

    public static float ComputeSlotSideAffinity(int slotIndex, Vector3 target, TeamBlackboard teamBB)
    {
        if (teamBB == null)
        {
            return 0f;
        }

        var field = teamBB.FieldInfo;
        var ball = teamBB.BallInfo;
        Vector3 anchor = ball.BallOwnerPosition;
        if (anchor.sqrMagnitude < 0.01f)
        {
            anchor = ball.BallPosition;
        }

        Vector3 toGoal = (field.EnemyGoalPosition - anchor).normalized;
        if (toGoal.sqrMagnitude < 0.0001f)
        {
            toGoal = Vector3.forward;
        }

        Vector3 right = Vector3.Cross(Vector3.up, toGoal).normalized;
        float lateral = Vector3.Dot(target - anchor, right);

        return slotIndex switch
        {
            1 => Mathf.Clamp(lateral * 0.15f, -SlotAffinityMaxBonus, SlotAffinityMaxBonus),
            2 => Mathf.Clamp(-lateral * 0.15f, -SlotAffinityMaxBonus, SlotAffinityMaxBonus),
            0 => Mathf.Clamp(SlotAffinityMaxBonus - Mathf.Abs(lateral) * 0.08f, 0f, SlotAffinityMaxBonus * 0.5f),
            _ => 0f,
        };
    }

    /// <summary>追跡開始時にリーダーを固定（追跡中に近い別NPCへ奪われない）。</summary>
    public static void RegisterFreeBallChaseLeader(PlayerBlackboard bb)
    {
        if (bb?.BasicData?.Self == null)
        {
            return;
        }

        var facade = bb.BasicData.Self.GetComponentInParent<AnimalFacade>()
            ?? bb.BasicData.Self.GetComponent<AnimalFacade>();
        if (facade == null)
        {
            return;
        }

        _lockedChaseLeaderPlayerId = ResolvePlayerId(facade);
        _lockedChaseLeaderUntil = Time.time + ChaseLeaderLockSeconds;
    }

    public static void ReleaseFreeBallChaseLeader(PlayerBlackboard bb)
    {
        if (bb?.BasicData?.Self == null)
        {
            return;
        }

        var facade = bb.BasicData.Self.GetComponentInParent<AnimalFacade>()
            ?? bb.BasicData.Self.GetComponent<AnimalFacade>();
        if (facade == null)
        {
            return;
        }

        if (ResolvePlayerId(facade) == _lockedChaseLeaderPlayerId)
        {
            _lockedChaseLeaderPlayerId = -1;
            _lockedChaseLeaderUntil = 0f;
        }
    }

    /// <summary>
    /// 条件A: 操作プレイヤーがボールから十分離れ、かつ最寄り味方 NPC がボール近くかつ人間より近いときだけ NPC に追跡を委譲する。
    /// </summary>
    public static bool ShouldDelegateFreeBallChaseToNpc()
    {
        if (!Enabled)
        {
            return true;
        }

        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null || !IsFreeBallContext(teamBB))
        {
            return false;
        }

        Vector3 ballPos = teamBB.BallInfo.BallPosition;
        if (!TryGetNearestTeammateNpcBallDistance(ballPos, out float nearestNpcDist))
        {
            return false;
        }

        if (nearestNpcDist >= FreeBallNpcNearBallDistance)
        {
            return false;
        }

        if (!TryGetHumanDistanceToBall(ballPos, out float humanDist))
        {
            return true;
        }

        bool chaseLocked = Time.time < _lockedChaseLeaderUntil && _lockedChaseLeaderPlayerId > 0;
        float humanFarThreshold = chaseLocked
            ? FreeBallHumanNearReleaseDistance
            : FreeBallHumanFarEngageDistance;
        if (humanDist <= humanFarThreshold)
        {
            return false;
        }

        return nearestNpcDist < humanDist - FreeBallLeaderTieBreakEpsilon;
    }

    /// <summary>フリーボール時、味方フィールドNPCのうち1体だけが追いかけるリーダーを決定する。</summary>
    public static AnimalFacade ResolveFreeBallChaseLeader()
    {
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null || !IsFreeBallContext(teamBB))
        {
            ClearFreeBallChaseLeaderLock();
            return null;
        }

        if (!ShouldDelegateFreeBallChaseToNpc())
        {
            ClearFreeBallChaseLeaderLock();
            return null;
        }

        if (Time.time < _lockedChaseLeaderUntil
            && TryFindTeammateNpcByPlayerId(_lockedChaseLeaderPlayerId, out AnimalFacade lockedLeader))
        {
            return lockedLeader;
        }

        var regist = TeamFacade.Instance != null ? TeamFacade.Instance.TeamRegist : null;
        if (regist == null)
        {
            return null;
        }

        Vector3 ballPos = teamBB.BallInfo.BallPosition;
        AnimalFacade leader = null;
        float leaderDist = float.MaxValue;
        int leaderPlayerId = int.MaxValue;

        foreach (var facade in regist.Allys)
        {
            if (facade == null || facade.IsGK())
            {
                continue;
            }

            var assignment = facade.GetComponent<AnimalControlAssignment>();
            if (assignment == null || assignment.Role != AnimalControlRole.TeammateNpc)
            {
                continue;
            }

            float dist = HorizontalDistance(facade.transform.position, ballPos);
            int playerId = ResolvePlayerId(facade);
            bool isCloser = dist < leaderDist - FreeBallLeaderTieBreakEpsilon;
            bool isTieAndLowerId = Mathf.Abs(dist - leaderDist) <= FreeBallLeaderTieBreakEpsilon
                && playerId < leaderPlayerId;

            if (isCloser || isTieAndLowerId)
            {
                leader = facade;
                leaderDist = dist;
                leaderPlayerId = playerId;
            }
        }

        return leader;
    }

    public static bool IsFreeBallChaseLeader(AnimalFacade facade)
    {
        if (!Enabled || facade == null)
        {
            return true;
        }

        var leader = ResolveFreeBallChaseLeader();
        return leader != null && leader == facade;
    }

    /// <summary>FREEボール時にこの個体が主に追いかけるべきか（リーダー1体のみ true）。</summary>
    public static bool ShouldLeadFreeBallChase(PlayerBlackboard bb)
    {
        if (!Enabled || bb?.BasicData?.Self == null)
        {
            return true;
        }

        var facade = bb.BasicData.Self.GetComponentInParent<AnimalFacade>();
        if (facade == null)
        {
            facade = bb.BasicData.Self.GetComponent<AnimalFacade>();
        }

        return IsFreeBallChaseLeader(facade);
    }

    public static float GetDistanceToBall(PlayerBlackboard bb)
    {
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (bb == null || teamBB == null || !teamBB.BallInfo.IsExistBall)
        {
            return float.MaxValue;
        }

        Vector3 selfPos = bb.BasicData?.Self != null
            ? bb.BasicData.Self.transform.position
            : bb.PhysicalState.Position;
        return HorizontalDistance(selfPos, teamBB.BallInfo.BallPosition);
    }

    public static float ComputeFreeBallChaseAdjustment(PlayerBlackboard bb, bool forGoalPriority)
    {
        if (ShouldLeadFreeBallChase(bb))
        {
            return forGoalPriority ? FreeBallCloserGoalBonus : -FreeBallCloserActionDiscount;
        }

        return forGoalPriority ? -FreeBallFartherGoalPenalty : FreeBallFartherActionPenalty;
    }

    private static void ClearFreeBallChaseLeaderLock()
    {
        _lockedChaseLeaderPlayerId = -1;
        _lockedChaseLeaderUntil = 0f;
    }

    private static bool TryFindTeammateNpcByPlayerId(int playerId, out AnimalFacade facade)
    {
        facade = null;
        if (playerId <= 0)
        {
            return false;
        }

        var regist = TeamFacade.Instance != null ? TeamFacade.Instance.TeamRegist : null;
        if (regist == null)
        {
            return false;
        }

        foreach (var ally in regist.Allys)
        {
            if (ally == null || ally.IsGK())
            {
                continue;
            }

            if (ResolvePlayerId(ally) != playerId)
            {
                continue;
            }

            var assignment = ally.GetComponent<AnimalControlAssignment>();
            if (assignment != null && assignment.Role == AnimalControlRole.TeammateNpc)
            {
                facade = ally;
                return true;
            }
        }

        return false;
    }

    private static bool IsFreeBallContext(TeamBlackboard teamBB)
    {
        var ball = teamBB.BallInfo;
        return ball.BallState == BallManager_State.BALL_STATE.FREE
            && !ball.TeamHasBall
            && !ball.EnemyHasBall;
    }

    private static bool TryGetHumanDistanceToBall(Vector3 ballPos, out float distance)
    {
        distance = float.MaxValue;
        var squad = TeamFacade.Instance != null ? TeamFacade.Instance.SquadControl : null;
        if (squad == null)
        {
            return false;
        }

        bool found = false;
        float minDist = float.MaxValue;
        foreach (var human in squad.GetHumanControllableFieldPlayers())
        {
            if (human == null)
            {
                continue;
            }

            float d = HorizontalDistance(human.transform.position, ballPos);
            if (d < minDist)
            {
                minDist = d;
                found = true;
            }
        }

        if (!found)
        {
            return false;
        }

        distance = minDist;
        return true;
    }

    private static bool TryGetNearestTeammateNpcBallDistance(Vector3 ballPos, out float distance)
    {
        distance = float.MaxValue;
        var regist = TeamFacade.Instance != null ? TeamFacade.Instance.TeamRegist : null;
        if (regist == null)
        {
            return false;
        }

        bool found = false;
        float minDist = float.MaxValue;
        foreach (var facade in regist.Allys)
        {
            if (facade == null || facade.IsGK())
            {
                continue;
            }

            var assignment = facade.GetComponent<AnimalControlAssignment>();
            if (assignment == null || assignment.Role != AnimalControlRole.TeammateNpc)
            {
                continue;
            }

            float d = HorizontalDistance(facade.transform.position, ballPos);
            if (d < minDist)
            {
                minDist = d;
                found = true;
            }
        }

        if (!found)
        {
            return false;
        }

        distance = minDist;
        return true;
    }

    private static int ResolvePlayerId(AnimalFacade facade)
    {
        if (facade == null)
        {
            return int.MaxValue;
        }

        var view = facade.GetComponentInParent<Photon.Pun.PhotonView>();
        return view != null ? view.ViewID : int.MaxValue;
    }

    public static float AdjustGoalPriority(float basePriority, PlayerBlackboard bb, TeammateNpcTacticalMode mode)
    {
        if (!Enabled || bb == null)
        {
            return basePriority;
        }

        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null)
        {
            return basePriority;
        }

        Vector3 target = PredictTacticalTarget(bb, mode);
        var others = CollectOtherTeammateFieldPositions(bb);
        float overlap = ComputeOverlapCost(target, others);
        int slot = ResolveFormationSlot(bb);

        float adjusted = basePriority;
        adjusted -= overlap * GoalOverlapPenaltyScale;
        adjusted += ComputeSlotSideAffinity(slot, target, teamBB);

        if (mode == TeammateNpcTacticalMode.ChaseBall)
        {
            adjusted += ComputeFreeBallChaseAdjustment(bb, forGoalPriority: true);
        }

        return Mathf.Max(0f, adjusted);
    }

    public static float AdjustActionCost(float baseCost, PlayerBlackboard bb, TeammateNpcTacticalMode mode, GoapActionSO action = null)
    {
        if (!Enabled || bb == null)
        {
            return baseCost;
        }

        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null)
        {
            return baseCost;
        }

        Vector3 target = PredictActionTargetForCost(bb, mode, action);
        var others = CollectOtherTeammateFieldPositions(bb);
        float overlap = ComputeOverlapCost(target, others);
        int slot = ResolveFormationSlot(bb);

        float adjusted = baseCost;
        adjusted += overlap;
        adjusted -= ComputeSlotSideAffinity(slot, target, teamBB) * 0.35f;

        if (mode == TeammateNpcTacticalMode.ChaseBall)
        {
            adjusted += ComputeFreeBallChaseAdjustment(bb, forGoalPriority: false);
        }

        return Mathf.Max(0.1f, adjusted);
    }

    /// <summary>アクション別の移動先予測（サポート攻撃は各 SO ごと、それ以外は戦術モード既定）。</summary>
    public static Vector3 PredictActionTargetForCost(PlayerBlackboard bb, TeammateNpcTacticalMode mode, GoapActionSO action)
    {
        if (bb == null)
        {
            return Vector3.zero;
        }

        if (mode == TeammateNpcTacticalMode.Support
            && action != null
            && TeammateNpcSupportActionTargetPredictor.TryPredictSupportTarget(action, bb, out Vector3 supportTarget))
        {
            return supportTarget;
        }

        return PredictTacticalTarget(bb, mode);
    }

    public static float MeasureNpcFieldSeparation()
    {
        var squad = TeamFacade.Instance != null ? TeamFacade.Instance.SquadControl : null;
        if (squad == null)
        {
            return 0f;
        }

        var positions = new List<Vector3>();
        foreach (var facade in squad.GetTeammateNpcFieldPlayers())
        {
            if (facade != null)
            {
                positions.Add(facade.transform.position);
            }
        }

        if (positions.Count < 2)
        {
            return 0f;
        }

        float minDist = float.MaxValue;
        for (int i = 0; i < positions.Count; i++)
        {
            for (int j = i + 1; j < positions.Count; j++)
            {
                float d = HorizontalDistance(positions[i], positions[j]);
                if (d < minDist)
                {
                    minDist = d;
                }
            }
        }

        return minDist;
    }

    private static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
