#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;

/// <summary>守備基本バッチの期待アクション表を Play なしで固定する。</summary>
public sealed class GoapDefenseProductionSelectionExpectationsEditModeTests
{
    private static IEnumerable<TestCaseData> DefenseBaselineCases()
    {
        foreach (GoapDefenseLayoutPatternId pattern in GoapDefenseLayoutPatternCatalog.BuildDefenseBaselineSuite())
        {
            int number = GoapDefenseLayoutPatternCatalog.GetNumber(pattern);
            for (int slot = 0; slot <= 2; slot++)
            {
                yield return new TestCaseData(pattern, slot, "MoveToDefensivePosition")
                    .SetName($"Defense_#{number:D2}_slot{slot}_MoveToDefensivePosition");
            }
        }
    }

    [TestCaseSource(nameof(DefenseBaselineCases))]
    public void DefenseBaseline_MatchesExpectedAction(
        GoapDefenseLayoutPatternId pattern,
        int slot,
        string expectedAction)
    {
        bool ok = GoapDefenseProductionSelectionExpectations.DefenseBaseline.TryGetExpectation(
            pattern,
            slot,
            out string action,
            out bool shouldEvaluate);

        Assert.IsTrue(ok, $"TryGetExpectation failed for {pattern} slot{slot}");
        Assert.IsTrue(shouldEvaluate, $"{pattern} slot{slot} should be evaluated");
        Assert.AreEqual(expectedAction, action, $"{pattern} slot{slot} expected action");
    }

    [Test]
    public void DefenseBaseline_BaselinePattern_SkipsEvaluation()
    {
        bool ok = GoapDefenseProductionSelectionExpectations.DefenseBaseline.TryGetExpectation(
            GoapDefenseLayoutPatternId.Baseline,
            0,
            out _,
            out bool shouldEvaluate);

        Assert.IsTrue(ok);
        Assert.IsFalse(shouldEvaluate, "Baseline should be skipped");
    }
}
#endif
