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
    private readonly struct RecentPassInfo
    {
        public readonly int TargetPlayerId;
        public readonly float AtTime;
        public readonly int SameTargetStreak;

        public RecentPassInfo(int targetPlayerId, float atTime, int sameTargetStreak)
        {
            TargetPlayerId = targetPlayerId;
            AtTime = atTime;
            SameTargetStreak = sameTargetStreak;
        }
    }

    private const float MaxShootDistanceRatio = 0.55f;
    private const float MinShootDistanceRatio = 0.08f;
    private const float PassUnderPressureDiscount = 0.35f;
    private const float LightPressurePassDiscount = 0.18f;
    private const float BackwardPassPenalty = 0.30f;
    private const float ShootNearGoalDiscount = 0.52f;
    private const float BlockedShotLanePenalty = 0.90f;
    private const float ShotLaneEndpointMargin = 0.08f;
    private const float VeryNearGoalDistanceRatio = 0.32f;
    private const float VeryNearGoalPassPenalty = 0.55f;
    private const float VeryNearGoalShootDiscount = 0.55f;
    private const float VeryNearGoalShootPressureRelief = 0.20f;
    private const float MidRangeNearGoalPassPenalty = 0.25f;
    private const float MidRangeNearGoalDistanceRatio = 0.35f;
    private const float InShootingRangePassPenalty = 0.50f;
    private const float InShootingRangeDistanceRatio = 0.85f;

    public const float DefaultPassBaseCost = 1.12f;
    public const float DefaultShootBaseCost = 1.05f;
    public const float DefaultDribbleBaseCost = 1.18f;
    private const float EnemyFieldPassPenalty = 0.40f;
    private const float EnemyFieldShootDiscount = 0.28f;
    private const float DribbleFarFromGoalDiscount = 0.45f;
    private const float DribbleInShootingRangePenalty = 0.80f;
    private const float DribbleUnderPressurePenalty = 0.32f;
    private const float RepeatPassWindowSeconds = 4.0f;
    private const float AnyRapidPassPenalty = 0.22f;
    private const float SameTargetRepeatPassPenalty = 0.85f;
    private const float MaxRepeatPassPenalty = 2.2f;
    private static readonly Dictionary<int, RecentPassInfo> RecentPassByPasser = new();

    /// <summary>BallManager 上で実際に保持しているか（パス直後の HAS_BALL 同期ズレを除外）。</summary>
    public static bool IsActivelyHoldingBall(PlayerBlackboard bb)
    {
        if (bb == null)
        {
            return false;
        }

        var teamFacade = TeamFacade.Instance;
        var teamBB = teamFacade != null ? teamFacade.TeamBlackboard : null;
        if (teamFacade?.BallManager != null)
        {
            if (GoapMainNpcAttackBridge.IsHoldingBall(bb))
            {
                return true;
            }

            // pickup 直後の BallManager 同期ズレ: TeamBlackboard が自分 HOLD なら保持扱い。
            // PASS/SHOOT 中はパス出し後の HAS_BALL 残りを拾わない。
            return IsTeamBlackboardHoldOwner(bb, teamBB);
        }

        // EditMode 等 BallManager 未接続時: TeamBlackboard の HOLD のみ（HAS_BALL ファクト単体は除外）
        return IsTeamBlackboardHoldOwner(bb, teamBB);
    }

    private static bool IsTeamBlackboardHoldOwner(PlayerBlackboard bb, TeamBlackboard teamBB)
    {
        if (teamBB == null || bb?.BasicData == null)
        {
            return false;
        }

        var ball = teamBB.BallInfo;
        if (ball.BallState == BallManager_State.BALL_STATE.PASS
            || ball.BallState == BallManager_State.BALL_STATE.SHOOT)
        {
            return false;
        }

        int playerId = bb.BasicData.PlayerID;
        return playerId > 0
            && MatchesBallOwnerId(bb, ball.BallOwnerID)
            && ball.BallState == BallManager_State.BALL_STATE.HOLD;
    }

    /// <summary>TeamBlackboard の BallOwnerID（ViewID）が自分か。</summary>
    private static bool MatchesBallOwnerId(PlayerBlackboard bb, int ballOwnerId)
    {
        if (bb?.BasicData == null || ballOwnerId < 0)
        {
            return false;
        }

        if (ballOwnerId == bb.BasicData.PlayerID)
        {
            return true;
        }

        AnimalFacade facade = GoapMainNpcAttackBridge.ResolveFacade(bb);
        var avatar = facade != null ? facade.GetAvatar() : null;
        return avatar != null && ballOwnerId == avatar.ViewID;
    }

    /// <summary>
    /// TeamBlackboard の ownerId が自分か（HAS_BALL Fact 更新前の1フレームずれを吸収）。
    /// </summary>
    public static bool IsSelfBallOwner(PlayerBlackboard bb)
    {
        if (bb == null)
        {
            return false;
        }

        if (bb.GetFact(new Fact(SymbolTag.Basic.HAS_BALL, "true")) == true)
        {
            return true;
        }

        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null || bb.BasicData == null)
        {
            return false;
        }

        var ball = teamBB.BallInfo;
        return MatchesBallOwnerId(bb, ball.BallOwnerID)
            && ball.BallState == BallManager_State.BALL_STATE.HOLD;
    }

    /// <summary>パス受け直前の HAS_BALL 同期ズレを吸収する。</summary>
    public static bool IsEffectiveBallOwner(PlayerBlackboard bb)
    {
        return IsSelfBallOwner(bb)
            || IncomingPassPlanning.IsAnticipatedBallOwner(bb)
            || IncomingPassPlanning.IsReceiveCatchPhase(bb);
    }

    public static bool IsBallPossessionAttackContext(PlayerBlackboard bb)
    {
        if (!IsActivelyHoldingBall(bb) && !IsEffectiveBallOwner(bb))
        {
            return false;
        }

        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB != null && TeammateNpcSupportPlanning.IsTeamBallAttackContext(teamBB, bb))
        {
            return true;
        }

        // TeamBlackboard 追随前の HAS_BALL / HOLD 同期ズレでも M1 を維持する。
        return IsActivelyHoldingBall(bb) || IsSelfBallOwner(bb);
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

    /// <summary>プランナー選出とランタイム実行の整合用（シュート進行中は不可）。</summary>
    public static bool CanExecuteShootAtGoal(PlayerBlackboard bb)
    {
        if (!CanShootAtGoal(bb))
        {
            return false;
        }

        AnimalFacade facade = GoapMainNpcAttackBridge.ResolveFacade(bb);
        return facade != null && !GoapBallActionGuard.IsShootInProgress(facade);
    }

    public static bool CanDribbleTowardGoal(PlayerBlackboard bb)
    {
        if (!IsBallPossessionAttackContext(bb))
        {
            return false;
        }

        if (bb.GetFact(new Fact(SymbolTag.Action.CAN_MOVE, "true")) != true)
        {
            return false;
        }

        if (CanShootAtGoal(bb) && !IsShotLaneBlocked(bb))
        {
            return false;
        }

        if (!TryGetDistanceToEnemyGoal(bb, out float distance, out float maxDistance))
        {
            return false;
        }

        float minDistance = maxDistance * (MinShootDistanceRatio / MaxShootDistanceRatio);
        return distance > minDistance;
    }

    /// <summary>Pass/Shoot が選べない局面でも保持者を止めない最低限の前進ドリブル。</summary>
    public static bool CanForceDribbleWhileHolding(PlayerBlackboard bb)
    {
        if (!IsBallPossessionAttackContext(bb) || !IsActivelyHoldingBall(bb))
        {
            return false;
        }

        if (bb.GetFact(new Fact(SymbolTag.Action.CAN_MOVE, "true")) != true)
        {
            return false;
        }

        if (!TryGetDistanceToEnemyGoal(bb, out float distance, out float maxDistance))
        {
            return false;
        }

        float minDistance = maxDistance * (MinShootDistanceRatio / MaxShootDistanceRatio);
        return distance > minDistance;
    }

    public static bool CanExecuteDribbleTowardGoal(PlayerBlackboard bb)
    {
        return CanDribbleTowardGoal(bb) || CanForceDribbleWhileHolding(bb);
    }

    public static float ComputeDribbleCostAdjustment(PlayerBlackboard bb)
    {
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null || bb == null)
        {
            return 0f;
        }

        if (!TryGetDistanceToEnemyGoal(bb, out float goalDistance, out float maxDistance))
        {
            return 0.35f;
        }

        return ComputeDribbleCostAdjustment(
            goalDistance,
            maxDistance,
            teamBB.BallInfo.IsBallOwnerUnderPressure);
    }

    public static float ComputeDribbleCostAdjustment(
        float goalDistance,
        float maxShootDistance,
        int pressureCount)
    {
        float adjustment = 0f;
        float normalized = Mathf.Clamp01(goalDistance / Mathf.Max(maxShootDistance, 0.01f));
        adjustment -= normalized * DribbleFarFromGoalDiscount;

        if (goalDistance <= maxShootDistance * InShootingRangeDistanceRatio)
        {
            adjustment += DribbleInShootingRangePenalty;
        }

        if (pressureCount >= 1)
        {
            adjustment += DribbleUnderPressurePenalty * Mathf.Clamp(pressureCount, 1, 3);
        }

        return adjustment;
    }

    public static float EstimateDribbleCost(
        float goalDistance,
        float maxShootDistance,
        int pressureCount)
    {
        return DefaultDribbleBaseCost + ComputeDribbleCostAdjustment(
            goalDistance,
            maxShootDistance,
            pressureCount);
    }

    public static float ComputePassCostAdjustment(PlayerBlackboard bb)
    {
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null || bb == null)
        {
            return 0f;
        }

        bool passRouteClear = false;
        AnimalFacade target = null;
        if (GoapMainNpcAttackBridge.TryFindPassTarget(bb, out target))
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
            passRouteClear,
            bb,
            target);
    }

    /// <summary>EditMode / 診断用: ゴール距離とプレッシャーからパスコスト補正を見積もる。</summary>
    public static float ComputePassCostAdjustment(
        float goalDistance,
        float maxShootDistance,
        int pressureCount,
        bool passRouteClear)
    {
        return ComputePassCostAdjustment(
            goalDistance,
            maxShootDistance,
            pressureCount,
            passRouteClear,
            bb: null,
            target: null);
    }

    private static float ComputePassCostAdjustment(
        float goalDistance,
        float maxShootDistance,
        int pressureCount,
        bool passRouteClear,
        PlayerBlackboard bb,
        AnimalFacade target)
    {
        float adjustment = 0f;

        if (pressureCount >= 2)
        {
            adjustment -= PassUnderPressureDiscount;
        }
        else if (pressureCount >= 1 && passRouteClear)
        {
            adjustment -= LightPressurePassDiscount * 0.5f;
        }

        if (pressureCount >= 2)
        {
            adjustment -= 0.15f;
        }

        if (passRouteClear && pressureCount >= 2)
        {
            adjustment -= 0.20f;
        }

        if (goalDistance <= maxShootDistance * InShootingRangeDistanceRatio)
        {
            adjustment += InShootingRangePassPenalty;
            if (pressureCount < 2)
            {
                adjustment += 0.22f;
            }
        }

        if (goalDistance <= maxShootDistance * MidRangeNearGoalDistanceRatio)
        {
            adjustment += MidRangeNearGoalPassPenalty;
        }

        if (IsWithinVeryNearGoalShootZone(goalDistance, maxShootDistance))
        {
            adjustment += VeryNearGoalPassPenalty;
        }

        if (bb != null && TryGetBackwardPassPenalty(bb, out float backwardPenalty))
        {
            adjustment += backwardPenalty;
        }

        if (bb != null)
        {
            adjustment = ApplyEnemyFieldPassBias(bb, adjustment);
            adjustment += ComputeRepeatPassPenalty(bb, target);
        }

        return adjustment;
    }

    public static void RecordPassExecution(PlayerBlackboard bb, AnimalFacade target)
    {
        int passerId = ResolvePlayerId(bb);
        int targetId = ResolvePlayerId(target);
        if (passerId <= 0 || targetId <= 0 || passerId == targetId)
        {
            return;
        }

        float now = Time.time;
        if (!RecentPassByPasser.TryGetValue(passerId, out RecentPassInfo last)
            || now - last.AtTime > RepeatPassWindowSeconds)
        {
            RecentPassByPasser[passerId] = new RecentPassInfo(targetId, now, 1);
            return;
        }

        int streak = last.TargetPlayerId == targetId ? last.SameTargetStreak + 1 : 1;
        RecentPassByPasser[passerId] = new RecentPassInfo(targetId, now, streak);
    }

    private static float ComputeRepeatPassPenalty(PlayerBlackboard bb, AnimalFacade target)
    {
        int passerId = ResolvePlayerId(bb);
        int targetId = ResolvePlayerId(target);
        if (passerId <= 0 || targetId <= 0)
        {
            return 0f;
        }

        if (!RecentPassByPasser.TryGetValue(passerId, out RecentPassInfo recent))
        {
            return 0f;
        }

        float elapsed = Time.time - recent.AtTime;
        if (elapsed > RepeatPassWindowSeconds)
        {
            return 0f;
        }

        float penalty = AnyRapidPassPenalty;
        if (recent.TargetPlayerId == targetId)
        {
            penalty += SameTargetRepeatPassPenalty * Mathf.Max(1, recent.SameTargetStreak);
        }

        return Mathf.Min(MaxRepeatPassPenalty, penalty);
    }

    private static int ResolvePlayerId(PlayerBlackboard bb)
    {
        if (bb?.BasicData != null && bb.BasicData.PlayerID > 0)
        {
            return bb.BasicData.PlayerID;
        }

        AnimalFacade facade = GoapMainNpcAttackBridge.ResolveFacade(bb);
        var avatar = facade != null ? facade.GetAvatar() : null;
        return avatar != null ? avatar.ViewID : -1;
    }

    private static int ResolvePlayerId(AnimalFacade facade)
    {
        if (facade == null)
        {
            return -1;
        }

        var bb = facade.GetComponentInChildren<PlayerBlackboard>(true);
        if (bb?.BasicData != null && bb.BasicData.PlayerID > 0)
        {
            return bb.BasicData.PlayerID;
        }

        var avatar = facade.GetAvatar();
        return avatar != null ? avatar.ViewID : -1;
    }

    private static float ApplyEnemyFieldPassBias(PlayerBlackboard bb, float passAdjustment)
    {
        if (!GoapFieldNpcPerspective.IsMirrored(bb))
        {
            return passAdjustment;
        }

        return passAdjustment + EnemyFieldPassPenalty;
    }

    private static bool TryGetBackwardPassPenalty(PlayerBlackboard bb, out float penalty)
    {
        penalty = 0f;
        if (!GoapMainNpcAttackBridge.TryFindPassTarget(bb, out AnimalFacade target))
        {
            return false;
        }

        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null || target == null)
        {
            return false;
        }

        bool mirrored = GoapFieldNpcPerspective.IsMirrored(bb);
        Vector3 attackGoal = GoapFieldNpcPerspective.GetAttackGoalPosition(teamBB, mirrored);
        Vector3 passerPos = bb.PhysicalState.Position;
        Vector3 toGoal = attackGoal - passerPos;
        toGoal.y = 0f;
        if (toGoal.sqrMagnitude < 0.01f)
        {
            return false;
        }

        Vector3 toReceiver = target.transform.position - passerPos;
        toReceiver.y = 0f;
        if (toReceiver.sqrMagnitude < 0.01f)
        {
            return false;
        }

        float forward = Vector3.Dot(toGoal.normalized, toReceiver.normalized);
        if (forward >= 0f)
        {
            return false;
        }

        penalty = BackwardPassPenalty;
        return true;
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
        bool shotLaneClear = IsShotLaneClear(
            bb,
            bb.PhysicalState.Position,
            goalPos,
            laneWidth);

        return ApplyEnemyFieldShootBias(
            bb,
            shotLaneClear,
            ComputeShootCostAdjustment(
                goalDistance,
                maxDistance,
                teamBB.BallInfo.IsBallOwnerUnderPressure,
                shotLaneClear));
    }

    private static float ApplyEnemyFieldShootBias(
        PlayerBlackboard bb,
        bool shotLaneClear,
        float shootAdjustment)
    {
        if (!GoapFieldNpcPerspective.IsMirrored(bb))
        {
            return shootAdjustment;
        }

        if (!shotLaneClear)
        {
            return shootAdjustment;
        }

        return shootAdjustment - EnemyFieldShootDiscount;
    }

    private static bool IsShotLaneBlocked(PlayerBlackboard bb)
    {
        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null || bb == null)
        {
            return false;
        }

        Vector3 goalPos = GoapFieldNpcPerspective.GetAttackGoalPosition(
            teamBB,
            GoapFieldNpcPerspective.IsMirrored(bb));
        float laneWidth = teamBB.FieldInfo.FieldLength * 0.08f;
        return !IsShotLaneClear(bb, bb.PhysicalState.Position, goalPos, laneWidth);
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

        if (!shotLaneClear)
        {
            adjustment += BlockedShotLanePenalty;
        }
        else
        {
            adjustment -= normalized * ShootNearGoalDiscount;
        }

        if (pressureCount >= 2)
        {
            adjustment += 0.20f;
        }

        if (shotLaneClear)
        {
            if (IsWithinVeryNearGoalShootZone(goalDistance, maxShootDistance))
            {
                adjustment -= VeryNearGoalShootDiscount;
                if (pressureCount >= 2)
                {
                    adjustment -= VeryNearGoalShootPressureRelief;
                }
            }
            else if (goalDistance <= maxShootDistance * InShootingRangeDistanceRatio)
            {
                adjustment -= 0.28f;
            }
        }

        return adjustment;
    }

    /// <summary>保持者から攻撃ゴールへの射線が開いているか（GK 以外の全フィールドプレイヤーを考慮）。</summary>
    public static bool IsShotLaneClear(
        PlayerBlackboard bb,
        Vector3 shooterPosition,
        Vector3 goalPosition,
        float blockingRange)
    {
        var blockers = new List<Vector3>();
        CollectShotLaneBlockerPositions(bb, blockers);
        return PlayerBlackboardCalculator.IsShotRouteClear(
            shooterPosition,
            goalPosition,
            blockers,
            blockingRange,
            ShotLaneEndpointMargin);
    }

    private static void CollectShotLaneBlockerPositions(PlayerBlackboard bb, List<Vector3> blockers)
    {
        var regist = TeamFacade.Instance != null ? TeamFacade.Instance.TeamRegist : null;
        if (regist == null)
        {
            return;
        }

        int ownerPlayerId = ResolvePlayerId(bb);
        AppendShotLaneBlockerPositions(regist.Allys, ownerPlayerId, blockers);
        AppendShotLaneBlockerPositions(regist.Enemies, ownerPlayerId, blockers);
    }

    private static void AppendShotLaneBlockerPositions(
        IEnumerable<AnimalFacade> facades,
        int ownerPlayerId,
        List<Vector3> blockers)
    {
        if (facades == null)
        {
            return;
        }

        foreach (AnimalFacade facade in facades)
        {
            if (facade == null || IsFieldGoalkeeper(facade))
            {
                continue;
            }

            if (ownerPlayerId > 0 && ResolvePlayerId(facade) == ownerPlayerId)
            {
                continue;
            }

            blockers.Add(facade.transform.position);
        }
    }

    private static bool IsFieldGoalkeeper(AnimalFacade facade)
    {
        if (facade == null)
        {
            return true;
        }

        AnimalInfo info = facade.GetAnimalInfo();
        return info != null && info.IsGK;
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
        out Queue<GoapActionSO> plan,
        bool excludeShoot = false)
    {
        plan = null;
        if (!IsActivelyHoldingBall(bb) || !IsBallPossessionAttackContext(bb) || scopedActions == null)
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

            if (action is ShootAtGoalActionSO
                && (excludeShoot || !CanExecuteShootAtGoal(bb)))
            {
                continue;
            }

            if (action is DribbleTowardGoalActionSO && !CanExecuteDribbleTowardGoal(bb))
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

        if (bestAction == null && CanForceDribbleWhileHolding(bb))
        {
            foreach (GoapActionSO action in scopedActions)
            {
                if (action is DribbleTowardGoalActionSO)
                {
                    bestAction = action;
                    break;
                }
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
        return IsActivelyHoldingBall(bb) && IsBallPossessionAttackContext(bb);
    }

    /// <summary>SelectBestGoal が null のときの攻撃強制（保持中または戦術スキップ猶予）。</summary>
    public static bool NeedsForcedAttackPlanWhenNoGoal(
        PlayerBlackboard bb,
        float postAttackContextGraceUntil = float.NegativeInfinity)
    {
        if (NeedsForcedAttackPlan(bb))
        {
            return true;
        }

        return postAttackContextGraceUntil > Time.time && IsBallPossessionAttackContext(bb);
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
