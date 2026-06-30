public interface IGoapDefenseProductionSelectionExpectation
{
    bool TryGetExpectation(
        GoapDefenseLayoutPatternId pattern,
        int slot,
        out string expectedAction,
        out bool shouldEvaluate);
}

public static class GoapDefenseProductionSelectionExpectations
{
    public static readonly IGoapDefenseProductionSelectionExpectation MoveToDefensivePosition =
        new GoapMoveToDefensivePositionProductionSelectionExpectation();

    public static readonly IGoapDefenseProductionSelectionExpectation DefenseBaseline =
        new GoapDefenseBaselineProductionSelectionExpectation();

    public static readonly IGoapDefenseProductionSelectionExpectation DefenseTactical =
        new GoapDefenseTacticalProductionSelectionExpectation();

    public static readonly IGoapDefenseProductionSelectionExpectation DefenseDrive =
        new GoapDefenseDriveProductionSelectionExpectation();

    public static readonly IGoapDefenseProductionSelectionExpectation DefenseCombined =
        new GoapDefenseCombinedProductionSelectionExpectation();

    public static readonly IGoapDefenseProductionSelectionExpectation DefenseCombinedDrive =
        new GoapDefenseCombinedDriveProductionSelectionExpectation();
}

/// <summary>
/// MoveToDefensivePosition 単体: 非保持味方 slot0/1/2 が MoveToDefensivePosition を選ぶ。
/// </summary>
public sealed class GoapMoveToDefensivePositionProductionSelectionExpectation
    : IGoapDefenseProductionSelectionExpectation
{
    public bool TryGetExpectation(
        GoapDefenseLayoutPatternId pattern,
        int slot,
        out string expectedAction,
        out bool shouldEvaluate)
    {
        expectedAction = null;
        shouldEvaluate = false;

        if (pattern == GoapDefenseLayoutPatternId.Baseline
            || pattern == GoapDefenseLayoutPatternId.Custom)
        {
            return true;
        }

        if (slot < 0 || slot > 2)
        {
            return true;
        }

        if (pattern is GoapDefenseLayoutPatternId.EnemyOwner_ClusteredAllies
            or GoapDefenseLayoutPatternId.EnemyOwner_SpreadMidfield
            or GoapDefenseLayoutPatternId.EnemyOwner_ClusteredAllies_DriveForward
            or GoapDefenseLayoutPatternId.EnemyOwner_SpreadMidfield_DriveForward)
        {
            shouldEvaluate = true;
            expectedAction = "MoveToDefensivePosition";
        }

        return true;
    }
}

/// <summary>Phase 5 守備基本バッチ: 全評価スロットで MoveToDefensivePosition。</summary>
public sealed class GoapDefenseBaselineProductionSelectionExpectation
    : IGoapDefenseProductionSelectionExpectation
{
    public bool TryGetExpectation(
        GoapDefenseLayoutPatternId pattern,
        int slot,
        out string expectedAction,
        out bool shouldEvaluate)
    {
        return GoapDefenseProductionSelectionExpectations.MoveToDefensivePosition.TryGetExpectation(
            pattern,
            slot,
            out expectedAction,
            out shouldEvaluate);
    }
}

/// <summary>Phase 5b: パターンごとに戦術守備アクション1種を期待。</summary>
public sealed class GoapDefenseTacticalProductionSelectionExpectation
    : IGoapDefenseProductionSelectionExpectation
{
    public bool TryGetExpectation(
        GoapDefenseLayoutPatternId pattern,
        int slot,
        out string expectedAction,
        out bool shouldEvaluate)
    {
        expectedAction = null;
        shouldEvaluate = false;

        if (pattern == GoapDefenseLayoutPatternId.Baseline
            || pattern == GoapDefenseLayoutPatternId.Custom)
        {
            return true;
        }

        if (slot < 0 || slot > 2)
        {
            return true;
        }

        string action = pattern switch
        {
            GoapDefenseLayoutPatternId.EnemyOwner_MarkFreeTarget => "MarkOpponent",
            GoapDefenseLayoutPatternId.EnemyOwner_BlockPassLane => "BlockPassLane",
            GoapDefenseLayoutPatternId.EnemyOwner_BlockShotLane => "BlockShotLane",
            _ => null,
        };

        if (string.IsNullOrEmpty(action))
        {
            return true;
        }

        shouldEvaluate = true;
        expectedAction = action;
        return true;
    }
}

/// <summary>Phase 6 守備ドライブ: 敵保持ドライブ中も MoveToDefensivePosition を期待。</summary>
public sealed class GoapDefenseDriveProductionSelectionExpectation
    : IGoapDefenseProductionSelectionExpectation
{
    public bool TryGetExpectation(
        GoapDefenseLayoutPatternId pattern,
        int slot,
        out string expectedAction,
        out bool shouldEvaluate)
    {
        return GoapDefenseProductionSelectionExpectations.MoveToDefensivePosition.TryGetExpectation(
            pattern,
            slot,
            out expectedAction,
            out shouldEvaluate);
    }
}

/// <summary>
/// Phase 7a 守備統合本番選出: 局面ごとにスロット別の本番勝者を期待（全候補コスト競争）。
/// </summary>
public sealed class GoapDefenseCombinedProductionSelectionExpectation
    : IGoapDefenseProductionSelectionExpectation
{
    public bool TryGetExpectation(
        GoapDefenseLayoutPatternId pattern,
        int slot,
        out string expectedAction,
        out bool shouldEvaluate)
    {
        expectedAction = null;
        shouldEvaluate = false;

        if (pattern == GoapDefenseLayoutPatternId.Baseline
            || pattern == GoapDefenseLayoutPatternId.Custom)
        {
            return true;
        }

        if (slot < 0 || slot > 2)
        {
            return true;
        }

        switch (pattern)
        {
            case GoapDefenseLayoutPatternId.EnemyOwner_ClusteredAllies:
            case GoapDefenseLayoutPatternId.EnemyOwner_SpreadMidfield:
                if (slot == 2)
                {
                    return true;
                }

                shouldEvaluate = true;
                expectedAction = "MoveToDefensivePosition";
                return true;

            case GoapDefenseLayoutPatternId.EnemyOwner_MarkFreeTarget:
                if (slot == 0)
                {
                    return true;
                }

                shouldEvaluate = true;
                expectedAction = "MoveToDefensivePosition";
                return true;

            case GoapDefenseLayoutPatternId.EnemyOwner_BlockPassLane:
                if (slot != 0)
                {
                    return true;
                }

                shouldEvaluate = true;
                expectedAction = "BlockPassLane";
                return true;

            case GoapDefenseLayoutPatternId.EnemyOwner_BlockShotLane:
                if (slot != 1)
                {
                    return true;
                }

                shouldEvaluate = true;
                expectedAction = "MoveToDefensivePosition";
                return true;

            default:
                return true;
        }
    }
}

/// <summary>
/// Phase 7b 守備統合ドライブ: #7/#8 は静止統合（#2/#3）と同じ本番勝者を期待（全候補コスト競争）。
/// </summary>
public sealed class GoapDefenseCombinedDriveProductionSelectionExpectation
    : IGoapDefenseProductionSelectionExpectation
{
    public bool TryGetExpectation(
        GoapDefenseLayoutPatternId pattern,
        int slot,
        out string expectedAction,
        out bool shouldEvaluate)
    {
        expectedAction = null;
        shouldEvaluate = false;

        GoapDefenseLayoutPatternId basePattern = pattern switch
        {
            GoapDefenseLayoutPatternId.EnemyOwner_ClusteredAllies_DriveForward =>
                GoapDefenseLayoutPatternId.EnemyOwner_ClusteredAllies,
            GoapDefenseLayoutPatternId.EnemyOwner_SpreadMidfield_DriveForward =>
                GoapDefenseLayoutPatternId.EnemyOwner_SpreadMidfield,
            _ => pattern,
        };

        if (basePattern == pattern)
        {
            return true;
        }

        return GoapDefenseProductionSelectionExpectations.DefenseCombined.TryGetExpectation(
            basePattern,
            slot,
            out expectedAction,
            out shouldEvaluate);
    }
}
