using UnityEngine;

/// <summary>
/// キックオフ時のボール渡し先を決定し、編成スロット先頭へ割り当てる。
/// INT_BALL_OWNER 形式: 0〜3 = 味方スロット、4〜7 = 敵スロット。
/// </summary>
public static class BallKickoffAssignment
{
    public const int MasterTeamLeaderStoredIndex = 0;
    public const int OtherTeamLeaderStoredIndex = 4;

    public static int PickRandomOpeningOwnerIndex() =>
        Random.value < 0.5f ? MasterTeamLeaderStoredIndex : OtherTeamLeaderStoredIndex;

    public static int GetStoredOwnerIndexForTeamLeader(bool isMasterTeam) =>
        isMasterTeam ? MasterTeamLeaderStoredIndex : OtherTeamLeaderStoredIndex;

    public static bool TryDecodeStoredOwnerIndex(int storedOwnerIndex, out bool isOtherTeam, out int formationSlot)
    {
        isOtherTeam = false;
        formationSlot = 0;

        if (storedOwnerIndex < 0 || storedOwnerIndex > 7)
        {
            return false;
        }

        if (storedOwnerIndex >= OtherTeamLeaderStoredIndex)
        {
            isOtherTeam = true;
            formationSlot = storedOwnerIndex - OtherTeamLeaderStoredIndex;
            return formationSlot >= 0 && formationSlot <= 3;
        }

        formationSlot = storedOwnerIndex;
        return true;
    }

    public static AnimalFacade FindFormationSlotLeader(bool isOtherTeam, int formationSlot = 0)
    {
        var regist = TeamFacade.Instance != null ? TeamFacade.Instance.TeamRegist : null;
        if (regist == null)
        {
            return null;
        }

        var candidates = isOtherTeam ? regist.Enemies : regist.Allys;
        AnimalFacade fallback = null;
        foreach (var facade in candidates)
        {
            if (facade == null || facade.IsGK())
            {
                continue;
            }

            var slot = facade.GetComponent<AnimalFormationSlot>();
            if (slot != null && slot.IsAssigned && slot.Index == formationSlot)
            {
                return facade;
            }

            fallback ??= facade;
        }

        return fallback;
    }

    public static bool TryResolveViewIdFromStoredIndex(int storedOwnerIndex, out int viewId, out AnimalFacade facade)
    {
        facade = null;
        viewId = -1;

        if (!TryDecodeStoredOwnerIndex(storedOwnerIndex, out bool isOtherTeam, out int formationSlot))
        {
            return false;
        }

        facade = FindFormationSlotLeader(isOtherTeam, formationSlot);
        if (facade == null)
        {
            return false;
        }

        PhotonAvatarContainerChild avatar = facade.GetAvatar();
        if (avatar == null)
        {
            return false;
        }

        viewId = avatar.ViewID;
        return viewId > 0;
    }

    public static bool TryAssignFromStoredIndex(BallManager ballManager, int storedOwnerIndex, out string reason)
    {
        reason = "unknown";

        if (ballManager == null)
        {
            reason = "ball_manager_null";
            return false;
        }

        if (!TryResolveViewIdFromStoredIndex(storedOwnerIndex, out int viewId, out AnimalFacade facade))
        {
            reason = $"leader_not_found index={storedOwnerIndex}";
            return false;
        }

        if (!ballManager.AssignKickoffPossession(viewId))
        {
            reason = $"assign_failed viewId={viewId}";
            return false;
        }

        reason = $"assigned viewId={viewId} leader={facade.name}";
        return true;
    }
}
