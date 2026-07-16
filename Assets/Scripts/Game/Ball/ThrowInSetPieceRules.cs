using UnityEngine;

/// <summary>
/// 6-B P2: スローイン開始可否の純判定（EditMode 向け）。
/// </summary>
public static class ThrowInSetPieceRules
{
    public const float DefaultCooldownSeconds = GoalKickSetPieceRules.DefaultCooldownSeconds;
    public const float DefaultSuppressSeconds = GoalKickSetPieceRules.DefaultSuppressSeconds;

    public static bool ShouldEvaluate(
        bool isMatchPlayActive,
        bool ballExists,
        BallManager_State.BALL_STATE ballState,
        float now,
        float cooldownUntil) =>
        GoalKickSetPieceRules.ShouldEvaluate(
            isMatchPlayActive,
            ballExists,
            ballState,
            now,
            cooldownUntil);

    public static bool IsThrowInCandidate(OutOfPlayClassifier.Result classify) =>
        classify.IsOutOfPlay
        && classify.Kind == SetPieceKind.ThrowIn
        && classify.HasRestartTeam;

    /// <summary>
    /// LastPossessionBelongTeam → Classify の lastTouchByOtherTeam。
    /// FREE（未確定）は null。
    /// </summary>
    public static bool? ResolveLastTouchByOtherTeam(BallManager_State.BELONG_TEAM lastPossession)
    {
        switch (lastPossession)
        {
            case BallManager_State.BELONG_TEAM.PLAYER:
                return false;
            case BallManager_State.BELONG_TEAM.ENEMY:
                return true;
            default:
                return null;
        }
    }
}
