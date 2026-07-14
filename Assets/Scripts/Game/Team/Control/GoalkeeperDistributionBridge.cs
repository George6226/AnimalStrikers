using UnityEngine;

/// <summary>
/// GK 配球を既存の AnimalAction_Pass へ委譲する。
/// </summary>
public static class GoalkeeperDistributionBridge
{
    public static bool TryExecutePass(AnimalFacade goalkeeper, bool mirrored, out AnimalFacade target)
    {
        target = null;
        if (goalkeeper == null)
        {
            return false;
        }

        var teamFacade = TeamFacade.Instance;
        var avatar = goalkeeper.GetAvatar();
        if (teamFacade == null || teamFacade.BallManager == null || avatar == null)
        {
            return false;
        }

        if (!teamFacade.BallManager.isHoldBall(avatar.ViewID))
        {
            GoalkeeperDiagnosticLog.Write("[GK_DIST] pass_rejected reason=not_holding_ball");
            return false;
        }

        if (GoapBallActionGuard.IsPassInProgress(goalkeeper))
        {
            return false;
        }

        if (!GoalkeeperDistribution.TrySelectPassTarget(goalkeeper, mirrored, out target))
        {
            GoalkeeperDiagnosticLog.Write("[GK_DIST] pass_rejected reason=no_target");
            return false;
        }

        var pass = goalkeeper.GetComponentInChildren<AnimalAction_Pass>(true);
        if (pass == null)
        {
            GoalkeeperDiagnosticLog.Write("[GK_DIST] pass_rejected reason=no_pass_component");
            return false;
        }

        GoalkeeperDiagnosticLog.Write($"[GK_DIST] pass_invoke target={target.name}");
        pass.pass(target);
        return true;
    }
}
