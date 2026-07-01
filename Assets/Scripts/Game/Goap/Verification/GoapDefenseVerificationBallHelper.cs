using System.Collections.Generic;
using UnityEngine;

public static class GoapDefenseVerificationBallHelper
{
    public static List<AnimalFacade> GetFieldEnemies()
    {
        var result = new List<AnimalFacade>();
        var regist = TeamFacade.Instance != null ? TeamFacade.Instance.TeamRegist : null;
        if (regist == null)
        {
            return result;
        }

        foreach (var enemy in regist.Enemies)
        {
            if (enemy == null || enemy.IsGK())
            {
                continue;
            }

            result.Add(enemy);
        }

        return result;
    }

    public static AnimalFacade GetEnemyByIndex(int index)
    {
        List<AnimalFacade> enemies = GetFieldEnemies();
        if (index < 0 || index >= enemies.Count)
        {
            return null;
        }

        return enemies[index];
    }

    public static bool TryAssignBallToEnemyIndex(int index, out string reason, out bool ownershipChanged)
    {
        ownershipChanged = false;
        reason = "unknown";

        var teamFacade = TeamFacade.Instance;
        if (teamFacade == null || teamFacade.BallManager == null)
        {
            reason = "BallManager_unavailable";
            return false;
        }

        if (teamFacade.BallManager.Ball == null)
        {
            reason = "Ball_null";
            return false;
        }

        AnimalFacade owner = GetEnemyByIndex(index);
        if (owner == null)
        {
            reason = $"enemy{index}_not_found";
            return false;
        }

        PhotonAvatarContainerChild avatar = owner.GetAvatar();
        if (avatar == null)
        {
            reason = "avatar_null";
            return false;
        }

        int ownerViewId = avatar.ViewID;
        if (teamFacade.BallManager.isHoldBall(ownerViewId)
            && teamFacade.BallManager.State.BallState == BallManager_State.BALL_STATE.HOLD
            && teamFacade.TeamBlackboard != null
            && teamFacade.TeamBlackboard.BallInfo.EnemyHasBall)
        {
            reason = $"already_enemy_owned viewId={ownerViewId}";
            return true;
        }

        if (!teamFacade.BallManager.changeOwnership(ownerViewId, BallManager_State.BALL_STATE.HOLD))
        {
            reason = "changeOwnership_failed";
            return false;
        }

        ownershipChanged = true;
        reason = $"enemy viewId={ownerViewId}";
        return true;
    }

    public static AnimalFacade GetAllyByFormationSlot(int slot)
    {
        foreach (GoapSupportVerificationAllyHelper.AllySlot ally in GoapSupportVerificationAllyHelper.GetFieldAlliesBySlot())
        {
            if (ally.Slot == slot)
            {
                return ally.Facade;
            }
        }

        return null;
    }

    public static bool TryAssignBallToAllyFormationSlot(int slot, out string reason, out bool ownershipChanged)
    {
        ownershipChanged = false;
        reason = "unknown";

        var teamFacade = TeamFacade.Instance;
        if (teamFacade == null || teamFacade.BallManager == null)
        {
            reason = "BallManager_unavailable";
            return false;
        }

        if (teamFacade.BallManager.Ball == null)
        {
            reason = "Ball_null";
            return false;
        }

        AnimalFacade owner = GetAllyByFormationSlot(slot);
        if (owner == null)
        {
            reason = $"allySlot{slot}_not_found";
            return false;
        }

        PhotonAvatarContainerChild avatar = owner.GetAvatar();
        if (avatar == null)
        {
            reason = "avatar_null";
            return false;
        }

        int ownerViewId = avatar.ViewID;
        if (teamFacade.BallManager.isHoldBall(ownerViewId)
            && teamFacade.BallManager.State.BallState == BallManager_State.BALL_STATE.HOLD
            && teamFacade.TeamBlackboard != null
            && teamFacade.TeamBlackboard.BallInfo.TeamHasBall)
        {
            reason = $"already_ally_owned viewId={ownerViewId}";
            return true;
        }

        if (!teamFacade.BallManager.changeOwnership(ownerViewId, BallManager_State.BALL_STATE.HOLD))
        {
            reason = "changeOwnership_failed";
            return false;
        }

        ownershipChanged = true;
        reason = $"ally viewId={ownerViewId} slot={slot}";
        return true;
    }
}
