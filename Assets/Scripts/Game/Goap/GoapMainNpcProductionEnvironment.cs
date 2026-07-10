using UnityEngine;

/// <summary>
/// Phase A 本番: 操作キャラ（Human）に Main NPC GOAP（M1 保持中 Pass/Shoot + M2 オフボール）を動かす。
/// 検証モード（M0 CLI / Inspector verify）が有効なときは常に OFF。
/// </summary>
public static class GoapMainNpcProductionEnvironment
{
    private static bool _isActive;

    public static bool IsActive => _isActive;

    public static void Sync(bool active)
    {
        if (GoapBatchVerifyEnvironment.IsActive || GoapMainNpcVerifyEnvironment.IsActive)
        {
            active = false;
        }

        _isActive = active;
    }

    public static bool IsProductionMainPlayer(AnimalFacade facade)
    {
        if (!_isActive || facade == null)
        {
            return false;
        }

        var assignment = facade.GetComponent<AnimalControlAssignment>();
        return assignment != null && assignment.IsHumanControlled;
    }

    public static GoapNpcTier ResolveTier(AnimalFacade facade)
    {
        return IsProductionMainPlayer(facade) ? GoapNpcTier.Main : GoapNpcTier.Sub;
    }

    /// <summary>本番 Main NPC が GOAP を動かすべき文脈か（M1 + M2）。</summary>
    public static bool ShouldEnableGoap(PlayerBlackboard bb, AnimalFacade facade)
    {
        if (!IsProductionMainPlayer(facade) || bb == null)
        {
            return false;
        }

        if (GoapBallActionGuard.IsAnyInProgress(facade))
        {
            return true;
        }

        var goap = AnimalGoapBrainComponents.Resolve(facade.gameObject);
        if (goap.Agent != null && goap.Agent.HasUnfinishedCommittedBallAction)
        {
            return true;
        }

        // HAS_BALL と TeamBlackboard の同期ズレ中も保持者 GOAP を落とさない（画面上の停止防止）。
        if (MainNpcAttackPlanning.IsEffectiveBallOwner(bb))
        {
            return true;
        }

        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB != null
            && IsDeadPossessionContext(teamBB)
            && !IncomingPassPlanning.IsIncomingPassReceiveContext(bb)
            && !IncomingPassPlanning.IsAnticipatedBallOwner(bb)
            && !IncomingPassPlanning.IsReceiveCatchPhase(bb))
        {
            // 敵保持・シュート飛行中でも守備 GOAP は継続（受け失敗後の棒立ち防止）。
            if (TeammateNpcDefensePlanning.IsEnemyBallDefenseContext(teamBB, bb))
            {
                return true;
            }

            return false;
        }

        if (MainNpcPostPassPlanning.IsTeamBallSupportContext(bb))
        {
            return true;
        }

        if (IncomingPassPlanning.IsIncomingPassReceiveContext(bb)
            || IncomingPassPlanning.IsAnticipatedBallOwner(bb)
            || IncomingPassPlanning.IsReceiveCatchPhase(bb))
        {
            return true;
        }

        return (MainNpcPostPassPlanning.IsFreeBallRecoveryContext(bb) && !IsKickoffPickupSuppressed())
            || TeammateNpcDefensePlanning.IsEnemyBallDefenseContext(teamBB, bb);
    }

    /// <summary>本番 Main GOAP 稼働中は手動入力を抑止（GOAP とプレイヤー操作の二重実行防止）。</summary>
    public static bool ShouldSuppressHumanInput(AnimalFacade facade)
    {
        if (!IsProductionMainPlayer(facade))
        {
            return false;
        }

        var goap = AnimalGoapBrainComponents.Resolve(facade.gameObject);
        if (goap.Agent != null && goap.Agent.HasUnfinishedCommittedBallAction)
        {
            return true;
        }

        if (goap.Agent == null || !goap.Agent.enabled)
        {
            return GoapBallActionGuard.IsAnyInProgress(facade);
        }

        return ShouldEnableGoap(goap.Blackboard, facade);
    }

    /// <summary>デバッグラベル用: M1 / M2 / なし。</summary>
    public static string ResolveProductionGoapPhaseTag(PlayerBlackboard bb, AnimalFacade facade)
    {
        if (!ShouldEnableGoap(bb, facade))
        {
            return string.Empty;
        }

        if (bb != null && bb.GetFact(new Fact(SymbolTag.Basic.HAS_BALL, "true")) == true)
        {
            return "M1";
        }

        return "M2";
    }

    private static bool IsOpponentSettledPossession(TeamBlackboard teamBB)
    {
        if (teamBB == null)
        {
            return false;
        }

        var ball = teamBB.BallInfo;
        return ball.EnemyHasBall
            && !ball.TeamHasBall
            && ball.BallOwnerID > 0;
    }

    /// <summary>
    /// Main が達成可能ゴールを持たない局面（敵保持・味方シュート飛行・キックオフ抑制中）。
    /// NoGoal スパムで画面上は停止しているように見えるため、GOAP を先に落とす。
    /// </summary>
    private static bool IsDeadPossessionContext(TeamBlackboard teamBB)
    {
        if (IsOpponentSettledPossession(teamBB) || IsKickoffPickupSuppressed())
        {
            return true;
        }

        var ball = teamBB.BallInfo;
        return ball.BallState == BallManager_State.BALL_STATE.SHOOT
            && !ball.TeamHasBall
            && !ball.EnemyHasBall;
    }

    private static bool IsKickoffPickupSuppressed()
    {
        var ballManager = TeamFacade.Instance != null ? TeamFacade.Instance.BallManager : null;
        return ballManager != null && ballManager.IsKickoffBallPickupSuppressed;
    }
}
