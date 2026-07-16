using UnityEngine;

/// <summary>
/// 6-B P1: ゴールキック開始可否の純判定（EditMode 向け）。
/// </summary>
public static class GoalKickSetPieceRules
{
    public const float DefaultCooldownSeconds = 3f;
    public const float DefaultSuppressSeconds = 2f;

    public static bool ShouldEvaluate(
        bool isMatchPlayActive,
        bool ballExists,
        BallManager_State.BALL_STATE ballState,
        float now,
        float cooldownUntil)
    {
        if (!isMatchPlayActive || !ballExists)
        {
            return false;
        }

        if (ballState != BallManager_State.BALL_STATE.FREE)
        {
            return false;
        }

        return now >= cooldownUntil;
    }

    public static bool IsGoalKickCandidate(OutOfPlayClassifier.Result classify) =>
        classify.IsOutOfPlay
        && classify.Kind == SetPieceKind.GoalKick
        && classify.HasRestartTeam;

    public static float ResolveHomeDepth(bool restartTeamIsOther) =>
        restartTeamIsOther ? ConstData.GK_SPAWN_DEPTH_ENEMY : ConstData.GK_SPAWN_DEPTH_ALLY;
}
