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
                    resolveMode,
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

    public static bool IsSlotSelectionReady(
        IList<string> lines,
        int slot,
        Func<int, int?> resolvePlayerIdForSlot,
        GoapProductionSelectionResolveMode resolveMode = GoapProductionSelectionResolveMode.FirstPlanCosts)
    {
        return IsSlotSelectionReady(lines, slot, resolvePlayerIdForSlot, resolveMode, null);
    }

    public static bool IsSlotSelectionReady(
        IList<string> lines,
        int slot,
        Func<int, int?> resolvePlayerIdForSlot,
        GoapProductionSelectionResolveMode resolveMode,
        string expectedAction)
    {
        if (!TryResolveFirstNonEmptySelectedForSlot(
                lines,
                slot,
                resolvePlayerIdForSlot,
                resolveMode,
                out string action,
                out _))
        {
            return false;
        }

        return string.IsNullOrEmpty(expectedAction) || ActionsMatch(expectedAction, action);
    }

    private static bool TryResolveFirstNonEmptySelectedForSlot(
        IList<string> lines,
        int slot,
        Func<int, int?> resolvePlayerIdForSlot,
        GoapProductionSelectionResolveMode resolveMode,
        out string action,
        out string source)
    {
        if (GoapProductionSelectionEvaluator.TryResolveSelectedActionForSlot(
                lines,
                slot,
                resolvePlayerIdForSlot,
                resolveMode,
                out action,
                out source)
            && !string.IsNullOrEmpty(action)
            && !action.StartsWith("empty", StringComparison.Ordinal))
        {
            source = resolveMode switch
            {
                GoapProductionSelectionResolveMode.LastPlanCosts => "PlanCosts:last",
                GoapProductionSelectionResolveMode.MinCostFirstPlanCosts => "PlanCosts:min-first",
                _ => "PlanCosts:first",
            };
            return true;
        }

        int? playerId = resolvePlayerIdForSlot?.Invoke(slot);
        if (resolveMode == GoapProductionSelectionResolveMode.LastPlanCosts)
        {
            for (int i = lines.Count - 1; i >= 0; i--)
            {
                if (!TryReadNonEmptyPlanCostsSelection(lines[i], slot, playerId, out string candidate))
                {
                    continue;
                }

                action = candidate;
                source = "PlanCosts:last";
                return true;
            }

            for (int i = lines.Count - 1; i >= 0; i--)
            {
                string line = lines[i];
                if (!line.Contains("[GOAP_SUMMARY]")
                    || !line.Contains("ForcedTacticalSupportPlan(action="))
                {
                    continue;
                }

                if (playerId.HasValue && !line.Contains($"playerId={playerId.Value}"))
                {
                    continue;
                }

                string forced = ExtractForcedTacticalAction(line);
                if (!string.IsNullOrEmpty(forced))
                {
                    action = forced;
                    source = "Forced:last";
                    return true;
                }
            }
        }
        else
        {
            for (int i = 0; i < lines.Count; i++)
            {
                if (!TryReadNonEmptyPlanCostsSelection(lines[i], slot, playerId, out string candidate))
                {
                    continue;
                }

                action = candidate;
                source = "PlanCosts:scan";
                return true;
            }

            for (int i = 0; i < lines.Count; i++)
            {
                string line = lines[i];
                if (!line.Contains("[GOAP_SUMMARY]")
                    || !line.Contains("ForcedTacticalSupportPlan(action="))
                {
                    continue;
                }

                if (playerId.HasValue && !line.Contains($"playerId={playerId.Value}"))
                {
                    continue;
                }

                string forced = ExtractForcedTacticalAction(line);
                if (!string.IsNullOrEmpty(forced))
                {
                    action = forced;
                    source = "Forced:first";
                    return true;
                }
            }
        }

        action = null;
        source = null;
        return false;
    }

    private static bool TryReadNonEmptyPlanCostsSelection(
        string line,
        int slot,
        int? playerId,
        out string candidate)
    {
        candidate = null;
        if (!line.Contains("[GOAP_SUMMARY]") || !line.Contains("PlanCosts("))
        {
            return false;
        }

        if (!line.Contains($"slot={slot},") && !line.Contains($"slot={slot} "))
        {
            return false;
        }

        if (playerId.HasValue && !line.Contains($"playerId={playerId.Value}"))
        {
            return false;
        }

        candidate = ExtractSelectedActionFromPlanCosts(line);
        return !string.IsNullOrEmpty(candidate) && !candidate.StartsWith("empty", StringComparison.Ordinal);
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

    private static string ExtractForcedTacticalAction(string line)
    {
        const string marker = "ForcedTacticalSupportPlan(action=";
        int start = line.IndexOf(marker, StringComparison.Ordinal);
        if (start < 0)
        {
            return null;
        }

        start += marker.Length;
        int end = line.IndexOf(',', start);
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
