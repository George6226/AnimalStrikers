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

        if (bb.GetFact(new Fact(SymbolTag.Basic.HAS_BALL, "true")) == true)
        {
            return MainNpcAttackPlanning.IsBallPossessionAttackContext(bb);
        }

        return MainNpcPostPassPlanning.IsTeamBallSupportContext(bb)
            || MainNpcPostPassPlanning.IsFreeBallRecoveryContext(bb);
    }

    /// <summary>本番 Main GOAP 稼働中は手動入力を抑止（GOAP とプレイヤー操作の二重実行防止）。</summary>
    public static bool ShouldSuppressHumanInput(AnimalFacade facade)
    {
        if (!IsProductionMainPlayer(facade))
        {
            return false;
        }

        var goap = AnimalGoapBrainComponents.Resolve(facade.gameObject);
        if (goap.Agent == null || !goap.Agent.enabled)
        {
            return false;
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
}
