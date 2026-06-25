using System;
using System.Collections.Generic;

public sealed class GoapDefenseProductionSelectionEvaluationResult
{
    public bool PatternPass;
    public int PassCount;
    public int EvalCount;
    public string DetailText;
}

public static class GoapDefenseProductionSelectionEvaluator
{
    public static GoapDefenseProductionSelectionEvaluationResult EvaluatePattern(
        GoapDefenseLayoutPatternId pattern,
        IGoapDefenseProductionSelectionExpectation expectation,
        IList<string> summaryLines,
        Func<int, int?> resolvePlayerIdForSlot,
        GoapProductionSelectionResolveMode resolveMode = GoapProductionSelectionResolveMode.FirstPlanCosts)
    {
        var result = new GoapDefenseProductionSelectionEvaluationResult();
        if (pattern == GoapDefenseLayoutPatternId.Baseline
            || pattern == GoapDefenseLayoutPatternId.Custom
            || expectation == null)
        {
            result.PatternPass = true;
            return result;
        }

        var details = new List<string>();
        for (int slot = 0; slot <= 2; slot++)
        {
            if (!expectation.TryGetExpectation(pattern, slot, out string expected, out bool shouldEvaluate))
            {
                continue;
            }

            if (!shouldEvaluate)
            {
                continue;
            }

            result.EvalCount++;
            if (TryResolveFirstNonEmptySelectedForSlot(
                    summaryLines,
                    slot,
                    resolvePlayerIdForSlot,
                    out string actual,
                    out string source)
                && ActionsMatch(expected, actual))
            {
                result.PassCount++;
                details.Add($"slot{slot}={actual}({source}) OK");
            }
            else
            {
                string actualLabel = string.IsNullOrEmpty(actual) ? "none" : $"{actual}({source})";
                details.Add($"slot{slot} expect={expected} actual={actualLabel} NG");
            }
        }

        result.PatternPass = result.EvalCount > 0 && result.PassCount == result.EvalCount;
        result.DetailText = string.Join("; ", details);
        return result;
    }

    private static bool TryResolveFirstNonEmptySelectedForSlot(
        IList<string> lines,
        int slot,
        Func<int, int?> resolvePlayerIdForSlot,
        out string action,
        out string source)
    {
        if (GoapProductionSelectionEvaluator.TryResolveSelectedActionForSlot(
                lines,
                slot,
                resolvePlayerIdForSlot,
                GoapProductionSelectionResolveMode.FirstPlanCosts,
                out action,
                out source)
            && !string.IsNullOrEmpty(action)
            && !action.StartsWith("empty", StringComparison.Ordinal))
        {
            source = "PlanCosts:first";
            return true;
        }

        for (int i = 0; i < lines.Count; i++)
        {
            string line = lines[i];
            if (!line.Contains("[GOAP_SUMMARY]") || !line.Contains("PlanCosts("))
            {
                continue;
            }

            if (!line.Contains($"slot={slot},") && !line.Contains($"slot={slot} "))
            {
                continue;
            }

            string candidate = ExtractSelectedActionFromPlanCosts(line);
            if (!string.IsNullOrEmpty(candidate) && !candidate.StartsWith("empty", StringComparison.Ordinal))
            {
                action = candidate;
                source = "PlanCosts:scan";
                return true;
            }
        }

        action = null;
        source = null;
        return false;
    }

    private static string ExtractSelectedActionFromPlanCosts(string line)
    {
        const string marker = "selected=";
        int start = line.IndexOf(marker, StringComparison.Ordinal);
        if (start < 0)
        {
            return null;
        }

        start += marker.Length;
        int end = line.IndexOf(':', start);
        if (end < 0)
        {
            end = line.IndexOf(')', start);
        }

        if (end <= start)
        {
            return null;
        }

        return line.Substring(start, end - start);
    }

    private static bool ActionsMatch(string expected, string actual)
    {
        if (string.IsNullOrEmpty(expected) || string.IsNullOrEmpty(actual))
        {
            return false;
        }

        return string.Equals(expected, actual, StringComparison.Ordinal)
            || actual.StartsWith(expected, StringComparison.Ordinal);
    }
}
