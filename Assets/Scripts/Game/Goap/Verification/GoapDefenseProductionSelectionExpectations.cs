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

        shouldEvaluate = true;
        expectedAction = "MoveToDefensivePosition";
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
