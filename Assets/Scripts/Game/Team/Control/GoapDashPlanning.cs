using UnityEngine;

/// <summary>
/// 6-D P0: GOAP 移動でのダッシュ ON/OFF 純判定（EditMode 向け）。
/// </summary>
public static class GoapDashPlanning
{
    /// <summary>ルーズボール追跡でダッシュを始める最短距離。</summary>
    public const float FreeBallMinDashDistance = 3f;

    /// <summary>パス受け移動でダッシュを始める最短距離。</summary>
    public const float ReceivePassMinDashDistance = 3f;

    public static bool ShouldDashToward(float distance, float minDistanceForDash, bool canUseDash)
    {
        if (!canUseDash)
        {
            return false;
        }

        float minDist = Mathf.Max(0.01f, minDistanceForDash);
        return distance >= minDist;
    }

    public static bool ShouldDashForFreeBall(float distanceToBall, bool canUseDash) =>
        ShouldDashToward(distanceToBall, FreeBallMinDashDistance, canUseDash);

    public static bool ShouldDashForReceivePass(
        float distanceToTarget,
        bool canUseDash,
        bool isCatchPhase)
    {
        if (isCatchPhase)
        {
            return false;
        }

        return ShouldDashToward(distanceToTarget, ReceivePassMinDashDistance, canUseDash);
    }

    /// <summary>スタミナ可否を解決して FreeBall 用ダッシュ判定。</summary>
    public static bool ResolveDashForFreeBall(PlayerBlackboard bb, float distanceToBall) =>
        ShouldDashForFreeBall(distanceToBall, GoapNpcMotor.CanUseDash(bb));

    /// <summary>スタミナ可否を解決して ReceivePass 用ダッシュ判定。</summary>
    public static bool ResolveDashForReceivePass(
        PlayerBlackboard bb,
        float distanceToTarget,
        bool isCatchPhase) =>
        ShouldDashForReceivePass(distanceToTarget, GoapNpcMotor.CanUseDash(bb), isCatchPhase);
}
