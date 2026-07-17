using UnityEngine;

/// <summary>
/// 味方フィールド NPC はローカル TeamBlackboard 視点、敵フィールド NPC は鏡像視点（攻守・陣営を反転）で GOAP を解釈する。
/// </summary>
public static class GoapFieldNpcPerspective
{
    public static bool IsMirrored(PlayerBlackboard bb) =>
        IsEnemyFieldNpc(ResolveFacade(bb));

    public static bool IsEnemyFieldNpc(AnimalFacade facade)
    {
        if (facade == null)
        {
            return false;
        }

        var assignment = facade.GetComponent<AnimalControlAssignment>();
        return assignment != null && assignment.Role == AnimalControlRole.EnemyFieldNpc;
    }

    public static bool IsGoapFieldNpc(PlayerBlackboard bb)
    {
        if (bb?.BasicData?.Self == null)
        {
            return false;
        }

        var assignment = bb.BasicData.Self.GetComponentInParent<AnimalControlAssignment>()
            ?? bb.BasicData.Self.GetComponent<AnimalControlAssignment>();
        if (assignment == null)
        {
            return false;
        }

        return assignment.Role == AnimalControlRole.TeammateNpc
            || assignment.Role == AnimalControlRole.EnemyFieldNpc;
    }

    public static bool EffectiveTeamHasBall(TeamBlackboard teamBB, bool mirrored)
    {
        if (teamBB == null)
        {
            return false;
        }

        var ball = teamBB.BallInfo;
        return mirrored ? ball.EnemyHasBall : ball.TeamHasBall;
    }

    public static bool EffectiveEnemyHasBall(TeamBlackboard teamBB, bool mirrored)
    {
        if (teamBB == null)
        {
            return false;
        }

        var ball = teamBB.BallInfo;
        return mirrored ? ball.TeamHasBall : ball.EnemyHasBall;
    }

    public static bool IsTeamBallAttackContext(TeamBlackboard teamBB, PlayerBlackboard bb = null)
    {
        if (teamBB == null)
        {
            return false;
        }

        var ball = teamBB.BallInfo;
        bool mirrored = IsMirrored(bb);
        if (ball.BallState == BallManager_State.BALL_STATE.FREE)
        {
            if (GoapPassFlightTracker.TryGetActivePass(out GoapPassFlightTracker.PassFlight _)
                && ball.LastPossessionBelongTeam == ResolveGlobalOwnTeam(mirrored))
            {
                return true;
            }

            // 味方最終保持のルーズボール: 非リーダーが Support できず NoGoal 固着しないよう攻撃文脈を残す。
            return ball.LastPossessionBelongTeam == ResolveGlobalOwnTeam(mirrored);
        }

        if (IsPassOrShootTransition(ball))
        {
            return ball.LastPossessionBelongTeam == ResolveGlobalOwnTeam(mirrored);
        }

        return EffectiveTeamHasBall(teamBB, mirrored) && !EffectiveEnemyHasBall(teamBB, mirrored);
    }

    public static bool IsOpponentBallDefenseContext(TeamBlackboard teamBB, PlayerBlackboard bb = null)
    {
        if (teamBB == null)
        {
            return false;
        }

        var ball = teamBB.BallInfo;
        bool mirrored = IsMirrored(bb);
        if (ball.BallState == BallManager_State.BALL_STATE.FREE)
        {
            // フリーボール追従の非リーダーが NoGoal で止まらないよう、相手最終保持なら守備文脈を残す。
            return ball.LastPossessionBelongTeam == ResolveGlobalOpponentTeam(mirrored)
                && !EffectiveTeamHasBall(teamBB, mirrored);
        }

        if (IsPassOrShootTransition(ball))
        {
            // 自軍シュート直後（ボール未所属）: 攻守切替まで守備へ（敵NPC鏡像で NoGoal 固着を防ぐ）
            if (IsOwnTeamShootReleaseTransition(teamBB, bb))
            {
                return true;
            }

            // 相手最終保持のパス/シュート遷移: 守備側 NPC は守備文脈を維持（NoGoal 固着防止）。
            return (ball.BallState == BallManager_State.BALL_STATE.PASS
                    || ball.BallState == BallManager_State.BALL_STATE.SHOOT)
                && ball.LastPossessionBelongTeam == ResolveGlobalOpponentTeam(mirrored);
        }

        return EffectiveEnemyHasBall(teamBB, mirrored) && !EffectiveTeamHasBall(teamBB, mirrored);
    }

    /// <summary>自軍がシュートを放ちボールが未所属の SHOOT 遷移中。</summary>
    public static bool IsOwnTeamShootReleaseTransition(TeamBlackboard teamBB, PlayerBlackboard bb = null)
    {
        if (teamBB == null)
        {
            return false;
        }

        var ball = teamBB.BallInfo;
        if (ball.BallState != BallManager_State.BALL_STATE.SHOOT
            || ball.TeamHasBall
            || ball.EnemyHasBall)
        {
            return false;
        }

        bool mirrored = IsMirrored(bb);
        return ball.LastPossessionBelongTeam == ResolveGlobalOwnTeam(mirrored);
    }

    private static bool IsPassOrShootTransition(TeamBallInfo ball)
    {
        if (ball == null)
        {
            return false;
        }

        return (ball.BallState == BallManager_State.BALL_STATE.PASS
                || ball.BallState == BallManager_State.BALL_STATE.SHOOT)
            && !ball.TeamHasBall
            && !ball.EnemyHasBall;
    }

    private static BallManager_State.BELONG_TEAM ResolveGlobalOwnTeam(bool mirrored)
    {
        return mirrored
            ? BallManager_State.BELONG_TEAM.ENEMY
            : BallManager_State.BELONG_TEAM.PLAYER;
    }

    private static BallManager_State.BELONG_TEAM ResolveGlobalOpponentTeam(bool mirrored)
    {
        return mirrored
            ? BallManager_State.BELONG_TEAM.PLAYER
            : BallManager_State.BELONG_TEAM.ENEMY;
    }

    public static bool IsFreeBallContext(TeamBlackboard teamBB)
    {
        if (teamBB == null)
        {
            return false;
        }

        var ball = teamBB.BallInfo;
        return ball.BallState == BallManager_State.BALL_STATE.FREE
            && !ball.TeamHasBall
            && !ball.EnemyHasBall;
    }

    public static Vector3 GetAttackGoalPosition(TeamBlackboard teamBB, bool mirrored)
    {
        if (teamBB == null)
        {
            return Vector3.zero;
        }

        return mirrored
            ? teamBB.FieldInfo.OwnGoalPosition
            : teamBB.FieldInfo.EnemyGoalPosition;
    }

    public static Vector3 GetDefendGoalPosition(TeamBlackboard teamBB, bool mirrored)
    {
        if (teamBB == null)
        {
            return Vector3.zero;
        }

        return mirrored
            ? teamBB.FieldInfo.EnemyGoalPosition
            : teamBB.FieldInfo.OwnGoalPosition;
    }

    public static void ResolveTeamPositions(
        TeamBlackboard teamBB,
        bool mirrored,
        out System.Collections.Generic.List<Vector3> allyFieldPositions,
        out System.Collections.Generic.List<Vector3> opponentFieldPositions)
    {
        allyFieldPositions = mirrored
            ? teamBB.BasicInfo.EnemyPositions
            : teamBB.BasicInfo.TeammatePositions;
        opponentFieldPositions = mirrored
            ? teamBB.BasicInfo.TeammatePositions
            : teamBB.BasicInfo.EnemyPositions;
    }

    private static AnimalFacade ResolveFacade(PlayerBlackboard bb)
    {
        if (bb?.BasicData?.Self == null)
        {
            return null;
        }

        return bb.BasicData.Self.GetComponentInParent<AnimalFacade>()
            ?? bb.BasicData.Self.GetComponent<AnimalFacade>();
    }
}
