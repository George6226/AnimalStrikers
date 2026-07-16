using UnityEngine;

/// <summary>
/// 6-C: スタミナ GOAP 用の純判定（EditMode 向け）。
/// </summary>
public static class GoapStaminaPlanning
{
    public static float SufficientRatioThreshold =>
        Mathf.Max(0.001f, ConstData.STAMINA_LOW_MOVE_RATIO_THRESHOLD);

    public static bool HasSufficientStamina(float staminaRatio) =>
        staminaRatio >= SufficientRatioThreshold;

    /// <summary>
    /// 回復ゴールを検討してよいか（緊急文脈では false）。
    /// FREE / 敵保持 / 自保持は除外。
    /// </summary>
    public static bool ShouldConsiderRegain(PlayerBlackboard bb, TeamBlackboard teamBB)
    {
        if (bb == null || teamBB == null)
        {
            return false;
        }

        if (bb.GetFact(new Fact(SymbolTag.Action.CAN_MOVE, "true")) != true)
        {
            return false;
        }

        if (bb.GetFact(new Fact(SymbolTag.Basic.HAS_BALL, "true")) == true)
        {
            return false;
        }

        if (bb.GetFact(new Fact(SymbolTag.Basic.HAS_STAMINA, "true")) == true)
        {
            return false;
        }

        if (teamBB.BallInfo.BallState == BallManager_State.BALL_STATE.FREE)
        {
            return false;
        }

        bool mirrored = GoapFieldNpcPerspective.IsMirrored(bb);
        if (GoapFieldNpcPerspective.EffectiveEnemyHasBall(teamBB, mirrored))
        {
            return false;
        }

        return true;
    }

    public static bool TryReadStaminaRatio(PlayerBlackboard bb, out float ratio)
    {
        ratio = 1f;
        if (!GoapNpcMotor.TryResolve(bb, out var facade, out _, out _))
        {
            return false;
        }

        PhotonHPGauge gauge = facade != null ? facade.GetHPGauge() : null;
        if (gauge == null)
        {
            return false;
        }

        ratio = gauge.StaminaRatio;
        return true;
    }
}
