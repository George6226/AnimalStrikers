using UnityEngine;

/// <summary>シュートの狙い点とキックベクトル（GK 不在側のポスト・ロフト）。</summary>
public static class ShootAimPolicy
{
    public static Vector3 ResolveAimPoint(
        Vector3 shooterPosition,
        Vector3 goalCenter,
        Vector3? defendingGoalkeeperPosition,
        float goalMouthHalfWidth = ConstData.GOAL_MOUTH_HALF_WIDTH)
    {
        float aimX = goalCenter.x;
        if (defendingGoalkeeperPosition.HasValue)
        {
            aimX = ResolveOpenPostAimX(
                shooterPosition,
                goalCenter,
                defendingGoalkeeperPosition.Value,
                goalMouthHalfWidth);
        }
        else if (!AnimalActionAccuracyPolicy.UseDeterministicDirection)
        {
            aimX = goalCenter.x + Random.Range(-0.6f, 0.6f);
        }

        aimX = Mathf.Clamp(
            aimX,
            goalCenter.x - goalMouthHalfWidth,
            goalCenter.x + goalMouthHalfWidth);

        return new Vector3(aimX, goalCenter.y, goalCenter.z);
    }

    public static Vector3 BuildKickVector(
        Vector3 shooterPosition,
        Vector3 aimPoint,
        float shootStat,
        float baseShoot,
        float increaseShoot,
        bool hasDefendingGoalkeeper = false)
    {
        Vector3 toAim = aimPoint - shooterPosition;
        toAim.y = 0f;
        float horizontalDistance = toAim.magnitude;
        if (horizontalDistance < 0.01f)
        {
            toAim = Vector3.forward;
            horizontalDistance = 1f;
        }

        float spreadAngle = hasDefendingGoalkeeper
            ? ConstData.MAX_SHOOT_SPREAD_ANGLE * 0.45f
            : ConstData.MAX_SHOOT_SPREAD_ANGLE * 0.35f;
        Vector3 horizontalDir = AnimalActionAccuracyPolicy.ApplyHorizontalSpread(
            toAim.normalized,
            shootStat,
            spreadAngle);

        if (ShouldUseLoftShot(hasDefendingGoalkeeper))
        {
            return BuildLoftKick(horizontalDir, horizontalDistance);
        }

        float shootTime = baseShoot + (increaseShoot * shootStat / 100.0f);
        shootTime = Mathf.Max(0.01f, shootTime);
        return horizontalDir * (horizontalDistance / shootTime);
    }

    public static AnimalFacade FindDefendingGoalkeeper(AnimalFacade shooter)
    {
        var teamFacade = TeamFacade.Instance;
        var regist = teamFacade != null ? teamFacade.TeamRegist : null;
        var fieldHandler = teamFacade != null ? teamFacade.FieldObjectHandler : null;
        if (shooter == null || regist == null || fieldHandler == null)
        {
            return null;
        }

        var avatar = shooter.GetAvatar();
        string shooterTag = avatar != null ? avatar.gameObject.tag : string.Empty;
        GameObject attackGoal = fieldHandler.GetGoal(shooterTag);
        if (attackGoal == null)
        {
            return null;
        }

        bool shooterOnAllySide = GoapPassTargetSelection.IsAllySidePasser(shooter);
        Vector3 goalPos = attackGoal.transform.position;
        AnimalFacade closestGk = null;
        float bestDist = float.MaxValue;
        foreach (AnimalFacade facade in regist.AllAnimals)
        {
            if (facade == null || !facade.IsGK())
            {
                continue;
            }

            bool gkOnAllySide = regist.Allys.Contains(facade);
            if (gkOnAllySide == shooterOnAllySide)
            {
                continue;
            }

            Vector3 gkPos = facade.transform.position;
            gkPos.y = 0f;
            Vector3 flatGoal = goalPos;
            flatGoal.y = 0f;
            float dist = Vector3.Distance(gkPos, flatGoal);
            if (dist > ConstData.GK_DEFEND_GOAL_MAX_DISTANCE)
            {
                continue;
            }

            if (dist < bestDist)
            {
                bestDist = dist;
                closestGk = facade;
            }
        }

        return closestGk;
    }

    public static AnimalFacade FindDefendingGoalkeeper(string shooterTag)
    {
        var regist = TeamFacade.Instance != null ? TeamFacade.Instance.TeamRegist : null;
        if (regist == null)
        {
            return null;
        }

        foreach (AnimalFacade facade in regist.AllAnimals)
        {
            var avatar = facade != null ? facade.GetAvatar() : null;
            if (avatar != null && avatar.gameObject.tag == shooterTag)
            {
                return FindDefendingGoalkeeper(facade);
            }
        }

        return null;
    }

    private static float ResolveOpenPostAimX(
        Vector3 shooterPosition,
        Vector3 goalCenter,
        Vector3 goalkeeperPosition,
        float goalMouthHalfWidth)
    {
        float gkX = goalkeeperPosition.x;
        float openSign = gkX >= goalCenter.x ? -1f : 1f;

        if (Mathf.Abs(gkX - goalCenter.x) < 1.0f)
        {
            float shooterOffset = shooterPosition.x - goalCenter.x;
            if (Mathf.Abs(shooterOffset) >= 0.35f)
            {
                openSign = shooterOffset >= 0f ? -1f : 1f;
            }
            else if (AnimalActionAccuracyPolicy.UseDeterministicDirection)
            {
                openSign = 1f;
            }
            else
            {
                openSign = Random.value >= 0.5f ? 1f : -1f;
            }
        }

        return goalCenter.x + openSign * goalMouthHalfWidth * ConstData.SHOOT_OPEN_POST_RATIO;
    }

    private static bool ShouldUseLoftShot(bool hasDefendingGoalkeeper)
    {
        if (AnimalActionAccuracyPolicy.UseDeterministicDirection)
        {
            return false;
        }

        float chance = hasDefendingGoalkeeper
            ? ConstData.SHOOT_LOFT_CHANCE * 1.15f
            : ConstData.SHOOT_LOFT_CHANCE * 0.6f;
        return Random.value < Mathf.Clamp01(chance);
    }

    private static Vector3 BuildLoftKick(Vector3 horizontalDir, float horizontalDistance)
    {
        float maxHeight = ConstData.SHOOT_LOFT_MAX_HEIGHT;
        float g = Physics.gravity.magnitude;
        float horizontalSpeed = Mathf.Sqrt(horizontalDistance * g / Mathf.Sin(Mathf.PI / 2f));
        horizontalSpeed = Mathf.Max(horizontalSpeed, 6f);

        Vector3 kickDir = horizontalDir.normalized * horizontalSpeed * Mathf.Cos(Mathf.PI / 4f);
        kickDir.y = horizontalSpeed * Mathf.Sin(Mathf.PI / 4f);

        float calculatedMaxHeight = (kickDir.y * kickDir.y) / (2f * g);
        if (calculatedMaxHeight > maxHeight)
        {
            float scale = Mathf.Sqrt(maxHeight / calculatedMaxHeight);
            kickDir *= scale;
        }

        return kickDir;
    }
}
