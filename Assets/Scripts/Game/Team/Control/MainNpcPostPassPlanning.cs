using System.Collections.Generic;
using Game.Goap;
using Game.Goap.Goals;

/// <summary>
/// Phase M2: パス後・非保持時のメイン NPC 行動（TeamBallSupport / FreeBallRecovery 連携）。
/// </summary>
public static class MainNpcPostPassPlanning
{
    public static bool IsTeamBallSupportContext(PlayerBlackboard bb)
    {
        if (bb == null)
        {
            return false;
        }

        if (bb.GetFact(new Fact(SymbolTag.Basic.HAS_BALL, "true")) == true)
        {
            return false;
        }

        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        return TeammateNpcSupportPlanning.IsTeamBallAttackContext(teamBB);
    }

    public static bool IsFreeBallRecoveryContext(PlayerBlackboard bb)
    {
        if (bb == null)
        {
            return false;
        }

        if (bb.GetFact(new Fact(SymbolTag.Basic.HAS_BALL, "true")) == true)
        {
            return false;
        }

        var teamBB = TeamFacade.Instance != null ? TeamFacade.Instance.TeamBlackboard : null;
        if (teamBB == null)
        {
            return false;
        }

        var ball = teamBB.BallInfo;
        return ball.BallState == BallManager_State.BALL_STATE.FREE
            && !ball.TeamHasBall
            && !ball.EnemyHasBall;
    }

    /// <summary>味方ボール保持中にメイン NPC がサポート移動を継続すべきか。</summary>
    public static bool NeedsPostPassSupportMovement(PlayerBlackboard bb)
    {
        return IsTeamBallSupportContext(bb)
            && TeammateNpcSupportPlanning.NeedsTacticalSupportMovement(bb);
    }

    public static bool TryBuildForcedPostPassSupportPlan(
        PlayerBlackboard bb,
        IEnumerable<GoapActionSO> scopedActions,
        out Queue<GoapActionSO> plan)
    {
        plan = null;
        if (!NeedsPostPassSupportMovement(bb) || scopedActions == null)
        {
            return false;
        }

        return TeammateNpcSupportPlanning.TryBuildForcedTacticalSupportPlan(bb, scopedActions, out plan);
    }

    public static bool IsSupportAttackAction(GoapActionSO action)
    {
        return GoapMainNpcCatalog.IsTeamBallSupportAction(action);
    }

    /// <summary>CLI / Play 検証: メイン NPC がパス後に TeamBallSupport を開始したか。</summary>
    public static bool VerifyMainNpcPostPassSupportStarted(string summary, string mainOwnerMarker = "owner=Lion")
    {
        if (string.IsNullOrEmpty(summary)
            || !summary.Contains("ActionStart(action=PassToTeammate", System.StringComparison.Ordinal))
        {
            return false;
        }

        foreach (string line in summary.Split('\n'))
        {
            if (!line.Contains(mainOwnerMarker, System.StringComparison.Ordinal))
            {
                continue;
            }

            if (line.Contains("GoalChanged(goal=TeamBallSupport", System.StringComparison.Ordinal)
                || line.Contains("PlanSuccess(goal=TeamBallSupport", System.StringComparison.Ordinal))
            {
                return true;
            }

            if (line.Contains("ActionStart(action=MoveToSupportPosition", System.StringComparison.Ordinal)
                || line.Contains("ActionStart(action=CreateSupportAngle", System.StringComparison.Ordinal)
                || line.Contains("ActionStart(action=GetOpen", System.StringComparison.Ordinal)
                || line.Contains("ActionStart(action=MakeRunBehind", System.StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
