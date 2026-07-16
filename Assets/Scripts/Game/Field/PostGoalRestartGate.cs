using UnityEngine;

/// <summary>
/// 6-F P0: 得点直後の GOAP 計画抑制窓（GAME 中でも一時停止）。
/// </summary>
public static class PostGoalRestartGate
{
    private static float _goapSuppressUntil;

    public static void BeginPostGoalRestart(float durationSeconds = PostGoalRestartRules.DefaultGoapSuppressSeconds)
    {
        _goapSuppressUntil = PostGoalRestartRules.ComputeSuppressUntil(Time.time, durationSeconds);
    }

    public static bool IsGoapPlanningSuppressed() =>
        PostGoalRestartRules.ShouldSuppressGoapPlanning(_goapSuppressUntil, Time.time);

#if UNITY_EDITOR
    internal static void ResetForEditModeTests()
    {
        _goapSuppressUntil = 0f;
    }

    internal static void SetSuppressUntilForEditModeTests(float until)
    {
        _goapSuppressUntil = until;
    }
#endif
}
