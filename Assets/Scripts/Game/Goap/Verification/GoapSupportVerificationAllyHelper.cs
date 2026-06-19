using System.Collections.Generic;
using UnityEngine;

public static class GoapSupportVerificationAllyHelper
{
    public readonly struct AllySlot
    {
        public readonly AnimalFacade Facade;
        public readonly int Slot;

        public AllySlot(AnimalFacade facade, int slot)
        {
            Facade = facade;
            Slot = slot;
        }
    }

    public static List<AnimalFacade> GetFieldAllies()
    {
        var result = new List<AnimalFacade>();
        var regist = TeamFacade.Instance != null ? TeamFacade.Instance.TeamRegist : null;
        if (regist == null)
        {
            return result;
        }

        foreach (var ally in regist.Allys)
        {
            if (ally == null || ally.IsGK())
            {
                continue;
            }

            result.Add(ally);
        }

        return result;
    }

    public static List<AllySlot> GetFieldAlliesBySlot()
    {
        var list = new List<AllySlot>();
        foreach (var ally in GetFieldAllies())
        {
            var slot = ally.GetComponentInParent<AnimalFormationSlot>()
                ?? ally.GetComponent<AnimalFormationSlot>();
            if (slot == null || !slot.IsAssigned)
            {
                continue;
            }

            list.Add(new AllySlot(ally, slot.Index));
        }

        return list;
    }

    public static AnimalFacade GetFacadeBySlot(int slot)
    {
        foreach (AllySlot ally in GetFieldAlliesBySlot())
        {
            if (ally.Slot == slot)
            {
                return ally.Facade;
            }
        }

        return null;
    }

    public static int? ResolvePlayerIdForSlot(int slot)
    {
        AnimalFacade facade = GetFacadeBySlot(slot);
        if (facade == null)
        {
            return null;
        }

        PlayerBlackboard bb = facade.GetComponentInChildren<PlayerBlackboard>();
        return bb != null ? bb.BasicData.PlayerID : null;
    }
}
