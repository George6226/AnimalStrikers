using UnityEngine;

/// <summary>
/// F5: GOAP から既存の AnimalAction_Special を呼び出す。
/// </summary>
public static class GoapSpecialBridge
{
    public static AnimalAction_Special ResolveSpecial(PlayerBlackboard bb)
    {
        AnimalFacade facade = GoapMainNpcAttackBridge.ResolveFacade(bb);
        return facade != null ? facade.GetComponentInChildren<AnimalAction_Special>(true) : null;
    }

    public static bool HasSpecialAction(PlayerBlackboard bb)
    {
        return ResolveSpecial(bb) != null;
    }

    public static bool IsGaugeReady(PlayerBlackboard bb)
    {
        AnimalAction_Special special = ResolveSpecial(bb);
        if (special == null)
        {
            return false;
        }

        return special.CanExecuteSpecial();
    }

    public static bool TryExecuteSpecial(PlayerBlackboard bb)
    {
        AnimalAction_Special special = ResolveSpecial(bb);
        if (special == null || !special.CanExecuteSpecial())
        {
            return false;
        }

        special.Execute();
        return AnimalAction_Special.IsSpecialActive;
    }

    /// <summary>
    /// UseSpecial 完了・タイムアウト・Cancel 時に IsSpecialActive を確実に下ろす。
    /// </summary>
    public static void ForceFinishSpecial(PlayerBlackboard bb)
    {
        AnimalAction_Special special = ResolveSpecial(bb);
        if (special != null)
        {
            special.ForceFinishSpecial();
            return;
        }

        if (AnimalAction_Special.IsSpecialActive)
        {
            AnimalAction_Special.ClearSpecialActiveFlag();
        }
    }
}
