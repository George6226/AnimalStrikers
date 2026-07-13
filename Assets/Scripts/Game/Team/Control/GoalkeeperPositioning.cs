using UnityEngine;

/// <summary>F3: GK のゴールライン位置取り（GOAP 外・静的計算）。</summary>
public static class GoalkeeperPositioning
{
    public enum Mode
    {
        HoldLine,
        TrackBall,
        RushLooseBall,
    }

    public readonly struct Result
    {
        public bool IsValid;
        public Vector3 TargetPosition;
        public Mode Mode;
        public bool IsUnderThreat;
    }

    /// <summary>敵 GK は ENEMY タグでミラー視点。</summary>
    public static bool IsMirroredGoalkeeper(AnimalFacade facade)
    {
        var avatar = facade != null ? facade.GetAvatar() : null;
        if (avatar == null)
        {
            return false;
        }

        string tag = !string.IsNullOrEmpty(avatar.CurrentTag) ? avatar.CurrentTag : avatar.tag;
        return tag == ConstData.ENEMY_TAG;
    }

    public static Result Compute(
        TeamBlackboard teamBB,
        bool mirrored,
        Vector3 ballPosition,
        BallManager_State.BALL_STATE ballState,
        bool enemyHasBall,
        bool teamHasBall,
        float lineDepth = 3.5f,
        float goalMouthHalfWidth = 3.5f,
        float rushLooseBallDistance = 8f)
    {
        if (teamBB == null)
        {
            return default;
        }

        var field = teamBB.FieldInfo;
        Vector3 defendGoal = GoapFieldNpcPerspective.GetDefendGoalPosition(teamBB, mirrored);
        Vector3 towardCenter = field.FieldCenter - defendGoal;
        towardCenter.y = 0f;
        if (towardCenter.sqrMagnitude < 0.001f)
        {
            towardCenter = Vector3.forward;
        }

        float homeZ = defendGoal.z + Mathf.Sign(towardCenter.z) * lineDepth;
        Vector3 homeLine = new Vector3(defendGoal.x, defendGoal.y, homeZ);

        bool shootThreat = ballState == BallManager_State.BALL_STATE.SHOOT;
        bool looseBallNearGoal = ballState == BallManager_State.BALL_STATE.FREE
            && HorizontalDistance(ballPosition, defendGoal) <= rushLooseBallDistance;
        bool enemyThreat = enemyHasBall
            && !teamHasBall
            && IsInDefensiveZone(ballPosition, defendGoal, field.FieldCenter);

        bool underThreat = shootThreat || looseBallNearGoal || enemyThreat;
        Mode mode = Mode.HoldLine;
        Vector3 target = homeLine;

        if (looseBallNearGoal)
        {
            mode = Mode.RushLooseBall;
            target = new Vector3(
                Mathf.Clamp(ballPosition.x, defendGoal.x - goalMouthHalfWidth, defendGoal.x + goalMouthHalfWidth),
                defendGoal.y,
                Mathf.Clamp(ballPosition.z, Mathf.Min(defendGoal.z, homeZ), Mathf.Max(defendGoal.z, homeZ)));
        }
        else if (underThreat)
        {
            mode = Mode.TrackBall;
            target = new Vector3(
                Mathf.Clamp(ballPosition.x, defendGoal.x - goalMouthHalfWidth, defendGoal.x + goalMouthHalfWidth),
                defendGoal.y,
                homeZ);
        }

        return new Result
        {
            IsValid = true,
            TargetPosition = ClampToField(target, field),
            Mode = mode,
            IsUnderThreat = underThreat,
        };
    }

    public static bool IsInDefensiveZone(Vector3 ballPosition, Vector3 defendGoal, Vector3 fieldCenter)
    {
        Vector3 toBall = ballPosition - defendGoal;
        Vector3 toCenter = fieldCenter - defendGoal;
        toBall.y = 0f;
        toCenter.y = 0f;
        if (toCenter.sqrMagnitude < 0.001f)
        {
            return false;
        }

        return Vector3.Dot(toBall, toCenter) > 0f;
    }

    public static Vector3 ClampToField(Vector3 pos, TeamFieldInfo field)
    {
        float halfW = field.FieldWidth * 0.5f;
        float halfL = field.FieldLength * 0.5f;
        Vector3 c = field.FieldCenter;
        return new Vector3(
            Mathf.Clamp(pos.x, c.x - halfW, c.x + halfW),
            pos.y,
            Mathf.Clamp(pos.z, c.z - halfL, c.z + halfL));
    }

    private static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
