using Game.Goap;
using UnityEngine;

/// <summary>
/// パス飛行中の受け手 NPC 向け GOAP 文脈（P0: 受け位置への移動・保持直前の攻撃文脈）。
/// </summary>
public static class IncomingPassPlanning
{
    private const float ReceiveAnticipationDistance = 1.25f;
    private const float ReceiveNearBallDistance = 2.2f;

    public static bool IsIncomingPassTarget(PlayerBlackboard bb)
    {
        if (bb?.BasicData == null || bb.BasicData.PlayerID <= 0)
        {
            return false;
        }

        if (bb.GetFact(new Fact(SymbolTag.Basic.HAS_BALL, "true")) == true)
        {
            return false;
        }

        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null)
        {
            return false;
        }

        GoapPassFlightTracker.SyncStalePassFlight(teamBB.BallInfo);
        if (!GoapPassFlightTracker.IsTargetPlayer(bb))
        {
            return false;
        }

        return TeammateNpcSupportPlanning.IsTeamBallAttackContext(teamBB, bb);
    }

    public static bool IsIncomingPassReceiveContext(PlayerBlackboard bb)
    {
        if (!IsIncomingPassTarget(bb))
        {
            return false;
        }

        if (HasReceivedIncomingPass(bb))
        {
            return false;
        }

        if (bb.GetFact(new Fact(SymbolTag.Action.CAN_MOVE, "true")) != true)
        {
            return false;
        }

        if (IsReceiveCatchPhase(bb))
        {
            return true;
        }

        if (IsBallArrivingForReceive(bb))
        {
            return false;
        }

        return TryGetReceiveMoveTarget(bb, out _);
    }

    /// <summary>受け手がボール近傍で保持同期を待っているフェーズ（near_ball 完了後のデッドゾーン防止）。</summary>
    public static bool IsReceiveCatchPhase(PlayerBlackboard bb)
    {
        if (bb == null || HasReceivedIncomingPass(bb) || !IsNearIncomingBall(bb))
        {
            return false;
        }

        return IsIncomingPassTarget(bb) || IsBallArrivingForReceive(bb);
    }

    /// <summary>HAS_BALL 同期前に保持者として扱う（受け切り直前のみ）。</summary>
    public static bool IsAnticipatedBallOwner(PlayerBlackboard bb)
    {
        if (!IsIncomingPassTarget(bb))
        {
            return false;
        }

        return IsBallArrivingForReceive(bb);
    }

    public static bool IsBallArrivingForReceive(PlayerBlackboard bb)
    {
        if (bb == null)
        {
            return false;
        }

        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null)
        {
            return false;
        }

        var ball = teamBB.BallInfo;
        if (ball.BallState == BallManager_State.BALL_STATE.HOLD
            && MatchesBallOwnerId(bb, ball.BallOwnerID))
        {
            return true;
        }

        float dist = bb.BallState.BallDistance;
        if (dist > ReceiveAnticipationDistance)
        {
            return false;
        }

        if (ball.BallState == BallManager_State.BALL_STATE.PASS)
        {
            return true;
        }

        if (ball.BallState == BallManager_State.BALL_STATE.FREE)
        {
            if (ball.BallVelocity.sqrMagnitude > 0.05f)
            {
                return true;
            }

            return GoapPassFlightTracker.IsTargetPlayer(bb);
        }

        return false;
    }

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

    public static bool TryGetReceiveMoveTarget(PlayerBlackboard bb, out Vector3 target)
    {
        target = default;
        if (bb == null)
        {
            return false;
        }

        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null)
        {
            return false;
        }

        var ball = teamBB.BallInfo;
        if (ball.BallState == BallManager_State.BALL_STATE.PASS
            || (ball.BallFree && ball.BallVelocity.sqrMagnitude > 0.05f))
        {
            target = ball.BallPosition;
            return true;
        }

        if (ball.BallState == BallManager_State.BALL_STATE.FREE
            && GoapPassFlightTracker.IsTargetPlayer(bb)
            && IsNearIncomingBall(bb))
        {
            target = ball.BallPosition;
            return true;
        }

        if (GoapPassFlightTracker.TryGetActivePass(out _)
            && GoapPassFlightTracker.IsTargetPlayer(bb))
        {
            AnimalFacade selfFacade = GoapMainNpcAttackBridge.ResolveFacade(bb);
            GameObject ballKeep = selfFacade != null ? selfFacade.GetBallKeep() : null;
            target = ballKeep != null
                ? ballKeep.transform.position
                : bb.PhysicalState.Position;
            return true;
        }

        return false;
    }

    public static bool HasReceivedIncomingPass(PlayerBlackboard bb)
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
        return ball.BallState == BallManager_State.BALL_STATE.HOLD
            && MatchesBallOwnerId(bb, ball.BallOwnerID);
    }

    public static bool IsNearIncomingBall(PlayerBlackboard bb)
    {
        return bb != null && bb.BallState.BallDistance <= ReceiveNearBallDistance;
    }
}
