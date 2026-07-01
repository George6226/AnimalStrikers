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

    [TestCase(GoapSupportLayoutPatternId.CfOwner_AtCorrectLanes_DriveForwardBack, "GetOpen", true)]
    [TestCase(GoapSupportLayoutPatternId.CfOwner_AtCorrectLanes_DriveForwardBack, "CreateSupportAngle", true)]
    [TestCase(GoapSupportLayoutPatternId.CfOwner_AtCorrectLanes_DriveForwardBack, "MoveToSupportPosition", false)]
    [TestCase(GoapSupportLayoutPatternId.LwOwner_WingHold_DriveForward, "GetOpen", true)]
    [TestCase(GoapSupportLayoutPatternId.LwOwner_WingHold_DriveForward, "CreateSupportAngle", true)]
    public void DriveRegression_FlexibleWingSelection_AcceptsGetOpenOrCsa(
        GoapSupportLayoutPatternId pattern,
        string actualAction,
        bool shouldMatch)
    {
        int slot = pattern == GoapSupportLayoutPatternId.LwOwner_WingHold_DriveForward ? 1 : 1;
        Assert.IsTrue(GoapProductionSelectionExpectations.Drive.TryGetExpectation(
            pattern,
            slot,
            out string expectedAction,
            out bool shouldEvaluate));
        Assert.IsTrue(shouldEvaluate);
        Assert.AreEqual("GetOpen|CreateSupportAngle", expectedAction);

        bool matched = MatchesExpectedAction(expectedAction, actualAction);
        Assert.AreEqual(shouldMatch, matched, $"{actualAction} vs {expectedAction}");
    }

    [Test]
    public void DriveRegression_LastPlanCosts_IgnoresTrailingMoveToSupportPosition()
    {
        var lines = new List<string>
        {
            "[GOAP_SUMMARY] [Goap#-1000|owner=Gorilla(Clone),playerId=1002] PlanCosts(goal=TeamBallSupport, slot=1, actionCosts=CreateSupportAngle:0.10, selected=CreateSupportAngle:0.10)",
            "[GOAP_SUMMARY] [Goap#-3256|owner=Boar(Clone),playerId=1003] PlanCosts(goal=TeamBallSupport, slot=2, actionCosts=CreateSupportAngle:0.10,GetOpen:1.11, selected=CreateSupportAngle:0.10)",
            "[GOAP_SUMMARY] [Goap#-3256|owner=Boar(Clone),playerId=1003] PlanCosts(goal=TeamBallSupport, slot=2, actionCosts=MoveToSupportPosition:1.69,CreateSupportAngle:2.63,GetOpen:3.59, selected=MoveToSupportPosition:1.69)",
        };

        var result = GoapProductionSelectionEvaluator.EvaluatePattern(
            GoapSupportLayoutPatternId.CfOwner_NearCorrectLanes_DriveForward,
            GoapProductionSelectionExpectations.Drive,
            lines,
            slot => slot == 2 ? 1003 : null,
            GoapProductionSelectionResolveMode.LastPlanCosts);

        Assert.IsTrue(result.PatternPass);
        Assert.AreEqual(2, result.PassCount);
        Assert.That(result.DetailText, Does.Contain("CreateSupportAngle(PlanCosts:last-matching)"));
    }

    private static bool MatchesExpectedAction(string expectedAction, string actualAction)
    {
        if (string.IsNullOrEmpty(expectedAction) || string.IsNullOrEmpty(actualAction))
        {
            return false;
        }

        if (expectedAction.IndexOf('|') >= 0)
        {
            foreach (string candidate in expectedAction.Split('|'))
            {
                if (actualAction == candidate || actualAction.StartsWith(candidate))
                {
                    return true;
                }
            }

            return false;
        }

        return actualAction == expectedAction || actualAction.StartsWith(expectedAction);
    }
}
#endif
