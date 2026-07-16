using UnityEngine;

/// <summary>
/// 6-G P0: volatile 即時 replan の coalesce 判定（CPU 負荷・ログスパム抑制）。
/// </summary>
public static class GoapReplanLoadRules
{
    public const float BallContextCoalesceSeconds = 0.20f;
    public const float BallStateCoalesceSeconds = 0.20f;
    public const float EnemyLayoutCoalesceSeconds = 0.30f;
    public const float BallOwnerMovedCoalesceSeconds = 0.20f;

    public static bool IsCoalescableImmediateReason(string reason) =>
        reason == "BallContextChanged"
        || reason == "BallStateChanged"
        || reason == "EnemyLayoutChanged"
        || reason == "BallOwnerMoved";

    public static bool IsHighPriorityImmediateReason(string reason) =>
        reason == "BallOwnerChanged"
        || reason == "BallPossessionChanged"
        || reason == "PassIssued"
        || reason == "PassReceiveComplete"
        || reason == "PassReceiveEligibilityChanged"
        || reason == "MatchPlayStarted"
        || reason == "PlanFailed";

    public static float ResolveCoalesceCooldownSeconds(string reason)
    {
        switch (reason)
        {
            case "BallContextChanged":
                return BallContextCoalesceSeconds;
            case "BallStateChanged":
                return BallStateCoalesceSeconds;
            case "EnemyLayoutChanged":
                return EnemyLayoutCoalesceSeconds;
            case "BallOwnerMoved":
                return BallOwnerMovedCoalesceSeconds;
            default:
                return 0f;
        }
    }

    public static bool ShouldCoalesceImmediateReplan(string reason, float now, float coalesceUntil)
    {
        if (IsHighPriorityImmediateReason(reason) || !IsCoalescableImmediateReason(reason))
        {
            return false;
        }

        return now < coalesceUntil;
    }

    public static float ComputeCoalesceUntil(float now, string reason) =>
        now + Mathf.Max(0f, ResolveCoalesceCooldownSeconds(reason));
}
