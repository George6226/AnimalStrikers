#if UNITY_EDITOR
using System.Collections.Generic;
using NUnit.Framework;

/// <summary>
/// combined 本番選出の期待アクション表を Play なしで固定する。
/// </summary>
public sealed class GoapProductionSelectionExpectationsEditModeTests
{
    private static IEnumerable<TestCaseData> CombinedRegressionCases()
    {
        foreach (GoapSupportLayoutPatternId pattern in GoapSupportLayoutPatternCatalog.BuildCombinedSupportRegressionSuite())
        {
            int number = GoapSupportLayoutPatternCatalog.GetNumber(pattern);
            for (int slot = 0; slot <= 2; slot++)
            {
                if (!TryResolveEvaluatedExpectation(pattern, slot, out string expectedAction))
                {
                    continue;
                }

                yield return new TestCaseData(pattern, slot, expectedAction)
                    .SetName($"Combined_#{number:D2}_slot{slot}_{expectedAction}");
            }
        }
    }

    private static bool TryResolveEvaluatedExpectation(
        GoapSupportLayoutPatternId pattern,
        int slot,
        out string expectedAction)
    {
        expectedAction = null;
        if (!GoapProductionSelectionExpectations.CombinedSupportRegression.TryGetExpectation(
                pattern,
                slot,
                out string action,
                out bool shouldEvaluate))
        {
            return false;
        }

        if (!shouldEvaluate)
        {
            return false;
        }

        expectedAction = action;
        return true;
    }

    [TestCaseSource(nameof(CombinedRegressionCases))]
    public void CombinedRegression_MatchesExpectedAction(
        GoapSupportLayoutPatternId pattern,
        int slot,
        string expectedAction)
    {
        bool ok = GoapProductionSelectionExpectations.CombinedSupportRegression.TryGetExpectation(
            pattern,
            slot,
            out string action,
            out bool shouldEvaluate);

        Assert.IsTrue(ok, $"TryGetExpectation failed for {pattern} slot{slot}");
        Assert.IsTrue(shouldEvaluate, $"{pattern} slot{slot} should be evaluated");
        Assert.AreEqual(expectedAction, action, $"{pattern} slot{slot} expected action");
    }

    [Test]
    public void CombinedRegression_AtCorrectLanes_SkipsBallOwner()
    {
        bool ok = GoapProductionSelectionExpectations.CombinedSupportRegression.TryGetExpectation(
            GoapSupportLayoutPatternId.CfOwner_AtCorrectLanes,
            0,
            out _,
            out bool shouldEvaluate);

        Assert.IsTrue(ok);
        Assert.IsFalse(shouldEvaluate, "#6 slot0 (ball owner) should be skipped");
    }

    [Test]
    public void CombinedRegression_WingHold_SkipsBallOwner()
    {
        bool ok = GoapProductionSelectionExpectations.CombinedSupportRegression.TryGetExpectation(
            GoapSupportLayoutPatternId.RwOwner_WingHold,
            1,
            out _,
            out bool shouldEvaluate);

        Assert.IsTrue(ok);
        Assert.IsFalse(shouldEvaluate, "#8 slot1 (ball owner) should be skipped");
    }

    private static IEnumerable<TestCaseData> DriveRegressionCases()
    {
        foreach (GoapSupportLayoutPatternId pattern in GoapSupportLayoutPatternCatalog.BuildAllDriveSuite())
        {
            int number = GoapSupportLayoutPatternCatalog.GetNumber(pattern);
            for (int slot = 0; slot <= 2; slot++)
            {
                if (!TryResolveDriveEvaluatedExpectation(pattern, slot, out string expectedAction))
                {
                    continue;
                }

                yield return new TestCaseData(pattern, slot, expectedAction)
                    .SetName($"Drive_#{number:D2}_slot{slot}_{expectedAction}");
            }
        }
    }

    private static bool TryResolveDriveEvaluatedExpectation(
        GoapSupportLayoutPatternId pattern,
        int slot,
        out string expectedAction)
    {
        expectedAction = null;
        if (!GoapProductionSelectionExpectations.Drive.TryGetExpectation(
                pattern,
                slot,
                out string action,
                out bool shouldEvaluate))
        {
            return false;
        }

        if (!shouldEvaluate)
        {
            return false;
        }

        expectedAction = action;
        return true;
    }

    [TestCaseSource(nameof(DriveRegressionCases))]
    public void DriveRegression_MatchesExpectedAction(
        GoapSupportLayoutPatternId pattern,
        int slot,
        string expectedAction)
    {
        bool ok = GoapProductionSelectionExpectations.Drive.TryGetExpectation(
            pattern,
            slot,
            out string action,
            out bool shouldEvaluate);

        Assert.IsTrue(ok, $"TryGetExpectation failed for {pattern} slot{slot}");
        Assert.IsTrue(shouldEvaluate, $"{pattern} slot{slot} should be evaluated");
        Assert.AreEqual(expectedAction, action, $"{pattern} slot{slot} expected action");
    }
}
#endif
