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
        if (!GoapPassFlightTracker.IsTargetPlayer(bb.BasicData.PlayerID))
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

        if (bb.GetFact(new Fact(SymbolTag.Action.CAN_MOVE, "true")) != true)
        {
            return false;
        }

        if (IsBallArrivingForReceive(bb))
        {
            return false;
        }

        return true;
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
            && ball.BallOwnerID == bb.BasicData.PlayerID)
        {
            return true;
        }

        float dist = bb.BallState.BallDistance;
        if (dist > ReceiveAnticipationDistance)
        {
            return false;
        }

        return ball.BallState == BallManager_State.BALL_STATE.PASS
            || (ball.BallState == BallManager_State.BALL_STATE.FREE && ball.BallVelocity.sqrMagnitude > 0.05f);
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

        if (GoapPassFlightTracker.TryGetActivePass(out GoapPassFlightTracker.PassFlight flight)
            && flight.TargetPlayerId == bb.BasicData.PlayerID)
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
            && ball.BallOwnerID == bb.BasicData.PlayerID;
    }

    public static bool IsNearIncomingBall(PlayerBlackboard bb)
    {
        return bb != null && bb.BallState.BallDistance <= ReceiveNearBallDistance;
    }
}
