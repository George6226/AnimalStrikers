using UnityEngine;

/// <summary>
/// GOAP バッチ検証の layout apply 準備完了判定（GoapSupportActionVerificationSetup と同等）。
/// </summary>
public static class GoapBatchVerifyLayoutReadiness
{
    public static bool IsReady(GoapSupportLayoutTuning tuning)
    {
        if (tuning == null)
        {
            tuning = new GoapSupportLayoutTuning();
        }

        if (!GoapSupportLayoutPatternLibrary.TryGetFieldContext(tuning, out _))
        {
            return false;
        }

        if (GoapSupportVerificationAllyHelper.GetFieldAlliesBySlot().Count < 3)
        {
            return false;
        }

        for (int slot = 0; slot <= 2; slot++)
        {
            if (GoapSupportVerificationAllyHelper.GetFacadeBySlot(slot) == null)
            {
                return false;
            }
        }

        return true;
    }

    public static string DescribeBlocked(GoapSupportLayoutTuning tuning)
    {
        if (tuning == null)
        {
            tuning = new GoapSupportLayoutTuning();
        }

        var teamFacade = TeamFacade.Instance;
        var teamBlackboard = teamFacade != null ? teamFacade.TeamBlackboard : null;
        if (teamBlackboard == null)
        {
            return "TeamBlackboard=null";
        }

        if (!GoapSupportLayoutPatternLibrary.TryGetFieldContext(tuning, out _))
        {
            return "fieldContext=unavailable";
        }

        int slottedAllies = GoapSupportVerificationAllyHelper.GetFieldAlliesBySlot().Count;
        if (slottedAllies < 3)
        {
            return $"slottedAllies={slottedAllies}/3 registAllies={CountRegisteredFieldAllies()} spawnReady={GoapDebugPlayBootstrap.IsSpawnReady}";
        }

        for (int slot = 0; slot <= 2; slot++)
        {
            if (GoapSupportVerificationAllyHelper.GetFacadeBySlot(slot) == null)
            {
                return $"slot{slot}=missing slottedAllies={slottedAllies}/3";
            }
        }

        return "ready";
    }

    private static int CountRegisteredFieldAllies()
    {
        var regist = TeamFacade.Instance != null ? TeamFacade.Instance.TeamRegist : null;
        if (regist == null)
        {
            return 0;
        }

        int count = 0;
        foreach (var ally in regist.Allys)
        {
            if (ally != null && !ally.IsGK())
            {
                count++;
            }
        }

        return count;
    }
}
