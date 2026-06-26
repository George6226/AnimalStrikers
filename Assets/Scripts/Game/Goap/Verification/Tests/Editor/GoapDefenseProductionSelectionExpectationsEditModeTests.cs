#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;

/// <summary>守備バッチの期待アクション表を Play なしで固定する。</summary>
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
                    .SetName($"DefenseBaseline_#{number:D2}_slot{slot}_MoveToDefensivePosition");
            }
        }
    }

    private static IEnumerable<TestCaseData> DefenseTacticalCases()
    {
        foreach (GoapDefenseLayoutPatternId pattern in GoapDefenseLayoutPatternCatalog.BuildDefenseTacticalSuite())
        {
            int number = GoapDefenseLayoutPatternCatalog.GetNumber(pattern);
            string expected = pattern switch
            {
                GoapDefenseLayoutPatternId.EnemyOwner_MarkFreeTarget => "MarkOpponent",
                GoapDefenseLayoutPatternId.EnemyOwner_BlockPassLane => "BlockPassLane",
                GoapDefenseLayoutPatternId.EnemyOwner_BlockShotLane => "BlockShotLane",
                _ => null,
            };

            if (string.IsNullOrEmpty(expected))
            {
                continue;
            }

            for (int slot = 0; slot <= 2; slot++)
            {
                yield return new TestCaseData(pattern, slot, expected)
                    .SetName($"DefenseTactical_#{number:D2}_slot{slot}_{expected}");
            }
        }
    }

    private static IEnumerable<TestCaseData> DefenseCombinedCases()
    {
        foreach (GoapDefenseLayoutPatternId pattern in GoapDefenseLayoutPatternCatalog.BuildDefenseCombinedSuite())
        {
            int number = GoapDefenseLayoutPatternCatalog.GetNumber(pattern);
            for (int slot = 0; slot <= 2; slot++)
            {
                if (!GoapDefenseProductionSelectionExpectations.DefenseCombined.TryGetExpectation(
                        pattern,
                        slot,
                        out string expected,
                        out bool shouldEvaluate)
                    || !shouldEvaluate
                    || string.IsNullOrEmpty(expected))
                {
                    continue;
                }

                yield return new TestCaseData(pattern, slot, expected)
                    .SetName($"DefenseCombined_#{number:D2}_slot{slot}_{expected}");
            }
        }
    }

    private static IEnumerable<TestCaseData> DefenseDriveCases()
    {
        foreach (GoapDefenseLayoutPatternId pattern in GoapDefenseLayoutPatternCatalog.BuildDefenseDriveSuite())
        {
            int number = GoapDefenseLayoutPatternCatalog.GetNumber(pattern);
            for (int slot = 0; slot <= 2; slot++)
            {
                yield return new TestCaseData(pattern, slot, "MoveToDefensivePosition")
                    .SetName($"DefenseDrive_#{number:D2}_slot{slot}_MoveToDefensivePosition");
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

    [TestCaseSource(nameof(DefenseTacticalCases))]
    public void DefenseTactical_MatchesExpectedAction(
        GoapDefenseLayoutPatternId pattern,
        int slot,
        string expectedAction)
    {
        bool ok = GoapDefenseProductionSelectionExpectations.DefenseTactical.TryGetExpectation(
            pattern,
            slot,
            out string action,
            out bool shouldEvaluate);

        Assert.IsTrue(ok, $"TryGetExpectation failed for {pattern} slot{slot}");
        Assert.IsTrue(shouldEvaluate, $"{pattern} slot{slot} should be evaluated");
        Assert.AreEqual(expectedAction, action, $"{pattern} slot{slot} expected action");
    }

    [TestCaseSource(nameof(DefenseCombinedCases))]
    public void DefenseCombined_MatchesExpectedAction(
        GoapDefenseLayoutPatternId pattern,
        int slot,
        string expectedAction)
    {
        bool ok = GoapDefenseProductionSelectionExpectations.DefenseCombined.TryGetExpectation(
            pattern,
            slot,
            out string action,
            out bool shouldEvaluate);

        Assert.IsTrue(ok, $"TryGetExpectation failed for {pattern} slot{slot}");
        Assert.IsTrue(shouldEvaluate, $"{pattern} slot{slot} should be evaluated");
        Assert.AreEqual(expectedAction, action, $"{pattern} slot{slot} expected action");
    }

    [TestCaseSource(nameof(DefenseDriveCases))]
    public void DefenseDrive_MatchesExpectedAction(
        GoapDefenseLayoutPatternId pattern,
        int slot,
        string expectedAction)
    {
        bool ok = GoapDefenseProductionSelectionExpectations.DefenseDrive.TryGetExpectation(
            pattern,
            slot,
            out string action,
            out bool shouldEvaluate);

        Assert.IsTrue(ok, $"TryGetExpectation failed for {pattern} slot{slot}");
        Assert.IsTrue(shouldEvaluate, $"{pattern} slot{slot} should be evaluated");
        Assert.AreEqual(expectedAction, action, $"{pattern} slot{slot} expected action");
    }

    [Test]
    public void DefenseCombined_BaselinePattern_SkipsEvaluation()
    {
        bool ok = GoapDefenseProductionSelectionExpectations.DefenseCombined.TryGetExpectation(
            GoapDefenseLayoutPatternId.Baseline,
            0,
            out _,
            out bool shouldEvaluate);

        Assert.IsTrue(ok);
        Assert.IsFalse(shouldEvaluate, "Baseline should be skipped");
    }

    [Test]
    public void DefenseDrive_BaselinePattern_SkipsEvaluation()
    {
        bool ok = GoapDefenseProductionSelectionExpectations.DefenseDrive.TryGetExpectation(
            GoapDefenseLayoutPatternId.Baseline,
            0,
            out _,
            out bool shouldEvaluate);

        Assert.IsTrue(ok);
        Assert.IsFalse(shouldEvaluate, "Baseline should be skipped");
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

    [Test]
    public void DefenseTactical_BaselinePattern_SkipsEvaluation()
    {
        bool ok = GoapDefenseProductionSelectionExpectations.DefenseTactical.TryGetExpectation(
            GoapDefenseLayoutPatternId.Baseline,
            0,
            out _,
            out bool shouldEvaluate);

        Assert.IsTrue(ok);
        Assert.IsFalse(shouldEvaluate, "Baseline should be skipped");
    }
}
#endif
