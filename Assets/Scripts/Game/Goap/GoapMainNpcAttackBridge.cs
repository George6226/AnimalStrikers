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
        if (tag == ConstData.NPC_TAG)
        {
            return GoapPassTargetSelection.TrySelectBestEnemyTeammate(facade, out target);
        }

        return GoapPassTargetSelection.TrySelectBestAlly(facade, out target);
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
