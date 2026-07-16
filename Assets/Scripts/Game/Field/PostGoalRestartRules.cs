using UnityEngine;

/// <summary>
/// 6-F P0: 得点後リスタートの純ロジック（失点側キックオフ・GOAP 抑制窓）。
/// </summary>
public static class PostGoalRestartRules
{
    public const float DefaultGoapSuppressSeconds = 2f;

    /// <summary>Master 側ゴールに入った = 失点側 Master / Sub 側ゴール = 失点側 Sub(NPC)。</summary>
    public static bool ResolveConcedingTeamIsMaster(bool isMasterGoal) => isMasterGoal;

    public static int ResolveKickoffOwnerStoredIndex(bool isMasterGoal) =>
        BallKickoffAssignment.GetStoredOwnerIndexForTeamLeader(ResolveConcedingTeamIsMaster(isMasterGoal));

    public static bool ShouldSuppressGoapPlanning(float suppressUntil, float now) =>
        now < suppressUntil;

    public static float ComputeSuppressUntil(float now, float durationSeconds) =>
        now + Mathf.Max(0f, durationSeconds);
}
