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
        if (ball.BallState == BallManager_State.BALL_STATE.FREE)
        {
            return false;
        }

        bool mirrored = IsMirrored(bb);
        return EffectiveTeamHasBall(teamBB, mirrored) && !EffectiveEnemyHasBall(teamBB, mirrored);
    }

    public static bool IsOpponentBallDefenseContext(TeamBlackboard teamBB, PlayerBlackboard bb = null)
    {
        if (teamBB == null)
        {
            return false;
        }

        var ball = teamBB.BallInfo;
        if (ball.BallState == BallManager_State.BALL_STATE.FREE)
        {
            return false;
        }

        bool mirrored = IsMirrored(bb);
        return EffectiveEnemyHasBall(teamBB, mirrored) && !EffectiveTeamHasBall(teamBB, mirrored);
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
