using UnityEngine;

/// <summary>
/// Phase M1: GOAP ランタイムから既存の AnimalAction_Pass / Shoot を呼び出す。
/// </summary>
public static class GoapMainNpcAttackBridge
{
    public static bool TryFindPassTarget(PlayerBlackboard bb, out AnimalFacade target)
    {
        target = null;
        AnimalFacade facade = ResolveFacade(bb);
        if (facade == null)
        {
            return false;
        }

        PhotonAvatarContainerChild avatar = facade.GetAvatar();
        string tag = avatar != null ? avatar.gameObject.tag : null;
        if (tag == ConstData.PLAYER_TAG)
        {
            var passSearch = facade.GetComponentInChildren<AnimalPass_Search>(true);
            if (passSearch != null)
            {
                target = passSearch.FindAllyForPass(facade);
            }
        }
        else
        {
            target = FindTeammatePassTarget(facade);
        }

        return target != null;
    }

    private static AnimalFacade FindTeammatePassTarget(AnimalFacade passer)
    {
        if (passer == null)
        {
            return null;
        }

        var regist = TeamFacade.Instance != null ? TeamFacade.Instance.TeamRegist : null;
        if (regist == null)
        {
            return null;
        }

        const float angleThreshold = 30f;
        var candidates = new System.Collections.Generic.List<AnimalFacade>();
        float facingY = 360f - passer.transform.localEulerAngles.y;
        Vector3 origin = passer.transform.position;

        foreach (AnimalFacade ally in regist.Allys)
        {
            if (ally == null || ally == passer || ally.IsGK())
            {
                continue;
            }

            Vector3 targetPos = ally.transform.position;
            float theta = Mathf.Atan2(targetPos.z - origin.z, targetPos.x - origin.x) * Mathf.Rad2Deg - 90f;
            if (theta < 0f)
            {
                theta += 360f;
            }

            if (Mathf.Abs(facingY - theta) <= angleThreshold)
            {
                candidates.Add(ally);
            }
        }

        if (candidates.Count > 0)
        {
            return candidates[Random.Range(0, candidates.Count)];
        }

        AnimalFacade nearest = null;
        float bestAngle = float.MaxValue;
        foreach (AnimalFacade ally in regist.Allys)
        {
            if (ally == null || ally == passer || ally.IsGK())
            {
                continue;
            }

            Vector3 targetPos = ally.transform.position;
            float theta = Mathf.Atan2(targetPos.z - origin.z, targetPos.x - origin.x) * Mathf.Rad2Deg - 90f;
            if (theta < 0f)
            {
                theta += 360f;
            }

            float angleDiff = Mathf.Abs(facingY - theta);
            if (angleDiff < bestAngle)
            {
                bestAngle = angleDiff;
                nearest = ally;
            }
        }

        return nearest;
    }

    public static bool TryExecutePass(PlayerBlackboard bb)
    {
        AnimalFacade facade = ResolveFacade(bb);
        if (facade == null)
        {
            return false;
        }

        var pass = facade.GetComponentInChildren<AnimalAction_Pass>(true);
        if (pass == null)
        {
            return false;
        }

        if (!TryFindPassTarget(bb, out AnimalFacade target))
        {
            return false;
        }

        pass.pass(target);
        return true;
    }

    public static bool TryExecuteShoot(PlayerBlackboard bb)
    {
        AnimalFacade facade = ResolveFacade(bb);
        if (facade == null)
        {
            return false;
        }

        var shoot = facade.GetComponentInChildren<AnimalAction_Shoot>(true);
        if (shoot == null)
        {
            return false;
        }

        shoot.shoot();
        return true;
    }

    public static bool IsHoldingBall(PlayerBlackboard bb)
    {
        AnimalFacade facade = ResolveFacade(bb);
        if (facade == null)
        {
            return false;
        }

        var avatar = facade.GetAvatar();
        var teamFacade = TeamFacade.Instance;
        if (avatar == null || teamFacade == null || teamFacade.BallManager == null)
        {
            return false;
        }

        return teamFacade.BallManager.isHoldBall(avatar.ViewID);
    }

    public static AnimalFacade ResolveFacade(PlayerBlackboard bb)
    {
        if (bb?.BasicData?.Self == null)
        {
            return null;
        }

        var facade = bb.BasicData.Self.GetComponentInParent<AnimalFacade>();
        return facade != null ? facade : bb.BasicData.Self.GetComponent<AnimalFacade>();
    }
}
