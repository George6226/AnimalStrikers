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
        public readonly bool IsValid;
        public readonly Vector3 TargetPosition;
        public readonly Mode Mode;
        public readonly bool IsUnderThreat;

        public Result(bool isValid, Vector3 targetPosition, Mode mode, bool isUnderThreat)
        {
            IsValid = isValid;
            TargetPosition = targetPosition;
            Mode = mode;
            IsUnderThreat = isUnderThreat;
        }
    }

    /// <summary>敵 GK は NPC/Enemy タグまたは +Z 半球でミラー視点（Photon 敵は NPC タグ）。</summary>
    public static bool IsMirroredGoalkeeper(AnimalFacade facade)
    {
        if (facade == null || !facade.IsGK())
        {
            return false;
        }

        var avatar = facade.GetAvatar();
        if (avatar != null)
        {
            string tag = !string.IsNullOrEmpty(avatar.CurrentTag) ? avatar.CurrentTag : avatar.tag;
            if (tag == ConstData.ENEMY_TAG || tag == ConstData.NPC_TAG)
            {
                return true;
            }

            if (tag == ConstData.PLAYER_TAG)
            {
                return false;
            }
        }

        // タグ未設定時: 敵 GK は +Z 側にスポーン
        return facade.transform.position.z > 0f;
    }

    /// <summary>
    /// キックオフ深さに合わせたホームライン深さ。敵は浅いスポーン深度、味方は通常深さ。
    /// </summary>
    public static float ResolveHomeLineDepth(bool mirrored) =>
        mirrored ? ConstData.GK_SPAWN_DEPTH_ENEMY : ConstData.GK_SPAWN_DEPTH_ALLY;

    /// <summary>
    /// Rush 後などに Z がホームから離れているとき、XZ 復帰移動が必要か。
    /// </summary>
    public static bool NeedsHomeDepthCorrection(
        float currentZ,
        float targetZ,
        float threshold = 0.25f)
    {
        return Mathf.Abs(currentZ - targetZ) > Mathf.Max(0.05f, threshold);
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
        float rushLooseBallDistance = 10f,
        float goalAreaDepth = 6f,
        float rushForwardDepth = 2.5f)
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

        float towardCenterSign = Mathf.Sign(towardCenter.z);
        float homeZ = defendGoal.z + towardCenterSign * lineDepth;
        Vector3 homeLine = new Vector3(defendGoal.x, defendGoal.y, homeZ);
        float maxForwardZ = defendGoal.z + towardCenterSign * (lineDepth + rushForwardDepth);

        bool shootThreat = ballState == BallManager_State.BALL_STATE.SHOOT;
        bool ballInGoalArea = IsBallInGoalArea(
            ballPosition,
            defendGoal,
            field.FieldCenter,
            goalMouthHalfWidth,
            goalAreaDepth);
        bool shootInGoalArea = shootThreat
            && IsBallInGoalArea(
                ballPosition,
                defendGoal,
                field.FieldCenter,
                goalMouthHalfWidth,
                ConstData.GK_SHOOT_RUSH_MAX_DEPTH);
        bool looseBallNearGoal = ballState == BallManager_State.BALL_STATE.FREE
            && HorizontalDistance(ballPosition, defendGoal) <= rushLooseBallDistance;
        bool enemyThreat = enemyHasBall
            && !teamHasBall
            && IsInDefensiveZone(ballPosition, defendGoal, field.FieldCenter);

        bool underThreat = shootThreat || ballInGoalArea || looseBallNearGoal || enemyThreat;
        Mode mode = Mode.HoldLine;
        Vector3 target = homeLine;

        if ((ballState == BallManager_State.BALL_STATE.FREE && (ballInGoalArea || looseBallNearGoal))
            || shootInGoalArea)
        {
            mode = Mode.RushLooseBall;
            target = BuildRushTarget(
                ballPosition,
                defendGoal,
                goalMouthHalfWidth,
                maxForwardZ);
        }
        else if (underThreat)
        {
            mode = Mode.TrackBall;
            target = new Vector3(
                Mathf.Clamp(ballPosition.x, defendGoal.x - goalMouthHalfWidth, defendGoal.x + goalMouthHalfWidth),
                defendGoal.y,
                homeZ);
        }

        return new Result(
            true,
            ClampToField(target, field),
            mode,
            underThreat);
    }

    /// <summary>自ゴール前ペナルティエリア相当（FREE ボール積極拾い判定）。</summary>
    public static bool IsBallInGoalArea(
        Vector3 ballPosition,
        Vector3 defendGoal,
        Vector3 fieldCenter,
        float goalMouthHalfWidth,
        float goalAreaDepth)
    {
        Vector3 toBall = ballPosition - defendGoal;
        Vector3 toCenter = fieldCenter - defendGoal;
        toBall.y = 0f;
        toCenter.y = 0f;
        if (toCenter.sqrMagnitude < 0.001f)
        {
            return false;
        }

        toCenter.Normalize();
        float depth = Vector3.Dot(toBall, toCenter);
        float lateral = Mathf.Abs(ballPosition.x - defendGoal.x);
        return depth >= 0f
            && depth <= goalAreaDepth
            && lateral <= goalMouthHalfWidth * 1.15f;
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

        float projection = Vector3.Dot(toBall, toCenter);
        return projection > 0f && projection <= toCenter.sqrMagnitude;
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

    private static Vector3 BuildRushTarget(
        Vector3 ballPosition,
        Vector3 defendGoal,
        float goalMouthHalfWidth,
        float maxForwardZ)
    {
        float minZ = Mathf.Min(defendGoal.z, maxForwardZ);
        float maxZ = Mathf.Max(defendGoal.z, maxForwardZ);
        float targetZ = Mathf.Clamp(ballPosition.z, minZ, maxZ);

        return new Vector3(
            Mathf.Clamp(ballPosition.x, defendGoal.x - goalMouthHalfWidth, defendGoal.x + goalMouthHalfWidth),
            defendGoal.y,
            targetZ);
    }

    private static float HorizontalDistance(Vector3 a, Vector3 b)
    {
        a.y = 0f;
        b.y = 0f;
        return Vector3.Distance(a, b);
    }
}
