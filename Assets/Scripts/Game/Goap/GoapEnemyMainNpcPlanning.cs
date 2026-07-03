/// <summary>
/// Phase B: 敵メイン NPC（slot0）の GOAP 稼働文脈（味方 Phase A の鏡像・手動入力なし）。
/// </summary>
public static class GoapEnemyMainNpcPlanning
{
    public static bool IsEnemyMainPlayer(AnimalFacade facade)
    {
        if (facade == null)
        {
            return false;
        }

        var assignment = facade.GetComponent<AnimalControlAssignment>();
        if (assignment == null || assignment.Role != AnimalControlRole.EnemyFieldNpc)
        {
            return false;
        }

        var enemySquad = TeamFacade.Instance != null ? TeamFacade.Instance.EnemySquadControl : null;
        return enemySquad != null
            && enemySquad.ShouldUseGoapFor(facade)
            && enemySquad.ResolveNpcTier(facade) == GoapNpcTier.Main;
    }

    public static bool ShouldEnableGoap(PlayerBlackboard bb, AnimalFacade facade)
    {
        if (!IsEnemyMainPlayer(facade) || bb == null)
        {
            return false;
        }

        if (bb.GetFact(new Fact(SymbolTag.Basic.HAS_BALL, "true")) == true)
        {
            return MainNpcAttackPlanning.IsBallPossessionAttackContext(bb);
        }

        return MainNpcPostPassPlanning.IsTeamBallSupportContext(bb)
            || MainNpcPostPassPlanning.IsFreeBallRecoveryContext(bb);
    }

    /// <summary>デバッグラベル用: M1 / M2 / なし。</summary>
    public static string ResolveEnemyGoapPhaseTag(PlayerBlackboard bb, AnimalFacade facade)
    {
        if (!ShouldEnableGoap(bb, facade))
        {
            return string.Empty;
        }

        if (bb.GetFact(new Fact(SymbolTag.Basic.HAS_BALL, "true")) == true)
        {
            return "M1";
        }

        return "M2";
    }
}
