using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Phase 5b 守備戦術: MarkOpponent / BlockPassLane / BlockShotLane / RetreatToDefensiveLine の単体選出を検証。
/// バッチパターンは #4〜#6 + #9（4パターン）。
/// </summary>
public class GoapCombinedDefenseTacticalDebugSetup : GoapDefenseActionVerificationSetup
{
    [Header("敵配置")]
    [SerializeField] private GoapEnemyPositionDebugPatterns _enemyLayouts;
    [SerializeField] private GoapEnemyPositionDebugPatterns.LayoutPattern _defaultEnemyLayout =
        GoapEnemyPositionDebugPatterns.LayoutPattern.PressBallOwner;

    protected override string SummaryLogTag => "GOAP_DEFENSE_TACTICAL_SETUP";

    protected override GoapDefenseActionUnderTest ActionUnderTest => GoapDefenseActionUnderTest.None;

    protected override IGoapDefenseProductionSelectionExpectation ProductionSelectionExpectation =>
        GoapDefenseProductionSelectionExpectations.DefenseTactical;

    protected override string BatchVerificationBanner => "Defense tactical batch verification";

    protected override string ProductionSelectionVerificationBanner =>
        "Defense tactical production selection (Mark/BlockPass/BlockShot/Retreat)";

    protected override bool SuppressAllyBallPickupDuringBatch => true;

    protected override List<GoapDefenseLayoutPatternId> BuildBatchPatternList() =>
        GoapDefenseLayoutPatternCatalog.BuildDefenseTacticalSuite();

    protected override GoapDefenseActionUnderTest ResolveDefenseActionFilterForPattern(
        GoapDefenseLayoutPatternId pattern)
    {
        if (!RestrictCandidatesToActionUnderTest)
        {
            return GoapDefenseActionUnderTest.None;
        }

        return pattern switch
        {
            GoapDefenseLayoutPatternId.EnemyOwner_MarkFreeTarget => GoapDefenseActionUnderTest.MarkOpponent,
            GoapDefenseLayoutPatternId.EnemyOwner_BlockPassLane => GoapDefenseActionUnderTest.BlockPassLane,
            GoapDefenseLayoutPatternId.EnemyOwner_BlockShotLane => GoapDefenseActionUnderTest.BlockShotLane,
            GoapDefenseLayoutPatternId.EnemyOwner_RetreatToDefensiveLine =>
                GoapDefenseActionUnderTest.RetreatToDefensiveLine,
            _ => GoapDefenseActionUnderTest.None,
        };
    }

    protected override void ApplyCompanionVerificationState(GoapDefenseLayoutPatternId pattern)
    {
        if (_enemyLayouts == null)
        {
            _enemyLayouts = FindFirstObjectByType<GoapEnemyPositionDebugPatterns>();
        }

        if (_enemyLayouts == null)
        {
            LogLine($"ApplyEnemyLayout({pattern}) skipped: GoapEnemyPositionDebugPatterns not found");
            return;
        }

        GoapEnemyPositionDebugPatterns.LayoutPattern layout = pattern switch
        {
            GoapDefenseLayoutPatternId.EnemyOwner_MarkFreeTarget
                or GoapDefenseLayoutPatternId.EnemyOwner_BlockPassLane
                or GoapDefenseLayoutPatternId.EnemyOwner_RetreatToDefensiveLine =>
                GoapEnemyPositionDebugPatterns.LayoutPattern.PressBallOwner,
            GoapDefenseLayoutPatternId.EnemyOwner_BlockShotLane =>
                GoapEnemyPositionDebugPatterns.LayoutPattern.DefensiveLine,
            _ => _defaultEnemyLayout,
        };
        _enemyLayouts.ApplyPattern(layout);
        LogLine($"ApplyEnemyLayout({pattern}) layout={layout}");
    }
}
