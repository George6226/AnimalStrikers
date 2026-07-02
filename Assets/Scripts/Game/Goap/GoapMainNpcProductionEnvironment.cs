using UnityEngine;

/// <summary>
/// Phase M2 本番: 操作キャラ（Human）がボール非保持時に Main NPC GOAP（パス後サポート・ルーズボール）を動かす。
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

    /// <summary>本番 Main NPC が GOAP を動かすべきオフボール文脈か（M2 のみ。保持中 M1 は対象外）。</summary>
    public static bool ShouldEnableGoap(PlayerBlackboard bb, AnimalFacade facade)
    {
        if (!IsProductionMainPlayer(facade) || bb == null)
        {
            return false;
        }

        if (bb.GetFact(new Fact(SymbolTag.Basic.HAS_BALL, "true")) == true)
        {
            return false;
        }

        return MainNpcPostPassPlanning.IsTeamBallSupportContext(bb)
            || MainNpcPostPassPlanning.IsFreeBallRecoveryContext(bb);
    }
}
