/// <summary>
/// Phase C: 敵サブ NPC の GOAP 稼働文脈（保持時攻撃・オフボール支援/守備/フリーボール）。
/// </summary>
public static class GoapEnemySubNpcPlanning
{
    public static bool ShouldEnableGoap(PlayerBlackboard bb, AnimalFacade facade)
    {
        if (bb == null || facade == null)
        {
            return false;
        }

        var assignment = facade.GetComponent<AnimalControlAssignment>();
        if (assignment == null || assignment.Role != AnimalControlRole.EnemyFieldNpc)
        {
            return false;
        }

        var enemySquad = TeamFacade.Instance != null ? TeamFacade.Instance.EnemySquadControl : null;
        if (enemySquad == null
            || !enemySquad.ShouldUseGoapFor(facade)
            || enemySquad.ResolveNpcTier(facade) != GoapNpcTier.Sub)
        {
            return false;
        }

        if (!GoapMatchPlayGate.IsMatchPlayActive())
        {
            return false;
        }

        if (MainNpcAttackPlanning.IsBallPossessionAttackContext(bb))
        {
            // Easy (pass-only): パス可なら GOAP 継続。不可でも true にして NoGoal 棒立ちを避ける
            // （Planner が Pass を選ぶ／完了するまで保持文脈を維持する）。
            if (!EnemyAiBalance.AllowEnemySubBallPossessionAttack)
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
}
