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

        if (MainNpcAttackPlanning.IsBallPossessionAttackContext(bb))
        {
            if (MainNpcAttackPlanning.IsActivelyHoldingBall(bb))
            {
                return true;
            }

            return MainNpcAttackPlanning.CanPassToTeammate(bb)
                || MainNpcAttackPlanning.CanShootAtGoal(bb)
                || MainNpcAttackPlanning.CanDribbleTowardGoal(bb);
        }

        return MainNpcPostPassPlanning.IsTeamBallSupportContext(bb)
            || IncomingPassPlanning.IsIncomingPassReceiveContext(bb)
            || IncomingPassPlanning.IsAnticipatedBallOwner(bb)
            || MainNpcPostPassPlanning.IsFreeBallRecoveryContext(bb)
            || TeammateNpcDefensePlanning.IsEnemyBallDefenseContext(
                TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null,
                bb);
    }

    /// <summary>デバッグラベル用: M1 / M2 / なし。</summary>
    public static string ResolveEnemyGoapPhaseTag(PlayerBlackboard bb, AnimalFacade facade)
    {
        if (!ShouldEnableGoap(bb, facade))
        {
            return string.Empty;
        }

        if (MainNpcAttackPlanning.IsSelfBallOwner(bb))
        {
            return "M1";
        }

        return "M2";
    }
}
