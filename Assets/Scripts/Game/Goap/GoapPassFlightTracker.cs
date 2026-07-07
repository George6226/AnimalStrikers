using UnityEngine;

/// <summary>
/// 進行中パスのパス先を記録し、受け手 GOAP がパス飛行中に反応できるようにする。
/// </summary>
public static class GoapPassFlightTracker
{
    public readonly struct PassFlight
    {
        public readonly int PasserPlayerId;
        public readonly int TargetPlayerId;
        public readonly float StartedAt;

        public PassFlight(int passerPlayerId, int targetPlayerId, float startedAt)
        {
            PasserPlayerId = passerPlayerId;
            TargetPlayerId = targetPlayerId;
            StartedAt = startedAt;
        }
    }

    private const float MaxFlightSeconds = 4f;

    private static PassFlight? _active;

    public static void RegisterPass(AnimalFacade passer, AnimalFacade target)
    {
        int passerId = ResolvePlayerId(passer);
        int targetId = ResolvePlayerId(target);
        if (passerId <= 0 || targetId <= 0 || passerId == targetId)
        {
            return;
        }

        _active = new PassFlight(passerId, targetId, Time.time);
    }

    public static void Clear()
    {
        _active = null;
    }

    public static bool TryGetActivePass(out PassFlight flight)
    {
        if (!_active.HasValue)
        {
            flight = default;
            return false;
        }

        flight = _active.Value;
        return true;
    }

    public static bool IsTargetPlayer(int playerId)
    {
        return _active.HasValue && _active.Value.TargetPlayerId == playerId;
    }

    public static void SyncStalePassFlight(TeamBallInfo ball)
    {
        if (!_active.HasValue || ball == null)
        {
            return;
        }

        PassFlight flight = _active.Value;
        if (Time.time - flight.StartedAt > MaxFlightSeconds)
        {
            Clear();
            return;
        }

        if (ball.BallState == BallManager_State.BALL_STATE.HOLD)
        {
            if (ball.BallOwnerID == flight.TargetPlayerId)
            {
                Clear();
                return;
            }

            // パッサー HOLD は wind-up 中。トラッカーを維持して受け手 GOAP を有効にする。
            if (ball.BallOwnerID == flight.PasserPlayerId)
            {
                return;
            }

            if (ball.BallOwnerID > 0)
            {
                Clear();
            }

            return;
        }

        if (ball.BallState == BallManager_State.BALL_STATE.FREE
            && ball.BallFree
            && ball.BallVelocity.sqrMagnitude < 0.05f
            && Time.time - flight.StartedAt > 1.5f)
        {
            Clear();
        }
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
}
