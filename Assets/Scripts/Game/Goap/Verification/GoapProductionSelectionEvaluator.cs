using System;
using System.Collections.Generic;

public enum GoapProductionSelectionResolveMode
{
    /// <summary>パターン適用後の最初の PlanCosts（本番選出検証の既定）。</summary>
    FirstPlanCosts,
    /// <summary>観測ウィンドウ内の最新 PlanCosts（ドライブ中本番選出検証）。</summary>
    LastPlanCosts,
    /// <summary>最新の ActionStart を優先（旧挙動）。</summary>
    LastActionStart,
}

public sealed class GoapProductionSelectionEvaluationResult
{
    public bool PatternPass;
    public int PassCount;
    public int EvalCount;
    public string DetailText;
}

public static class GoapProductionSelectionEvaluator
{
    public static GoapProductionSelectionEvaluationResult EvaluatePattern(
        GoapSupportLayoutPatternId pattern,
        IGoapProductionSelectionExpectation expectation,
        IList<string> summaryLines,
        Func<int, int?> resolvePlayerIdForSlot)
    {
        return EvaluatePattern(
            pattern,
            expectation,
            summaryLines,
            resolvePlayerIdForSlot,
            GoapProductionSelectionResolveMode.FirstPlanCosts);
    }

    public static GoapProductionSelectionEvaluationResult EvaluatePattern(
        GoapSupportLayoutPatternId pattern,
        IGoapProductionSelectionExpectation expectation,
        IList<string> summaryLines,
        Func<int, int?> resolvePlayerIdForSlot,
        GoapProductionSelectionResolveMode resolveMode)
    {
        var result = new GoapProductionSelectionEvaluationResult();
        if (pattern == GoapSupportLayoutPatternId.Baseline
            || pattern == GoapSupportLayoutPatternId.Custom
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
            if (TryResolveSelectedActionForSlot(
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

    public static bool TryResolveSelectedActionForSlot(
        IList<string> lines,
        int slot,
        Func<int, int?> resolvePlayerIdForSlot,
        out string action,
        out string source)
    {
        return TryResolveSelectedActionForSlot(
            lines,
            slot,
            resolvePlayerIdForSlot,
            GoapProductionSelectionResolveMode.FirstPlanCosts,
            out action,
            out source);
    }

    public static bool TryResolveSelectedActionForSlot(
        IList<string> lines,
        int slot,
        Func<int, int?> resolvePlayerIdForSlot,
        GoapProductionSelectionResolveMode resolveMode,
        out string action,
        out string source)
    {
        action = null;
        source = null;
        int? playerId = resolvePlayerIdForSlot?.Invoke(slot);

        if (resolveMode == GoapProductionSelectionResolveMode.FirstPlanCosts)
        {
            return TryResolveFirstPlanCosts(lines, slot, playerId, out action, out source);
        }

        if (resolveMode == GoapProductionSelectionResolveMode.LastPlanCosts)
        {
            return TryResolveLastPlanCosts(lines, slot, playerId, out action, out source);
        }

        for (int i = lines.Count - 1; i >= 0; i--)
        {
            string line = lines[i];
            if (!line.Contains("[GOAP_SUMMARY]"))
            {
                continue;
            }

            if (line.Contains("ActionStart(action=")
                && playerId.HasValue
                && line.Contains($"playerId={playerId.Value}"))
            {
                action = ExtractBetween(line, "ActionStart(action=", ",");
                source = "ActionStart";
                return !string.IsNullOrEmpty(action);
            }
        }

        for (int i = lines.Count - 1; i >= 0; i--)
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

            action = ExtractSelectedActionFromPlanCosts(line);
            if (!string.IsNullOrEmpty(action))
            {
                source = "PlanCosts";
                return true;
            }
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

            action = ExtractBetween(line, "ForcedTacticalSupportPlan(action=", ",");
            source = "Forced";
            return !string.IsNullOrEmpty(action);
        }

        return false;
    }

    private static bool TryResolveFirstPlanCosts(
        IList<string> lines,
        int slot,
        int? playerId,
        out string action,
        out string source)
    {
        action = null;
        source = null;

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

            action = ExtractSelectedActionFromPlanCosts(line);
            if (string.IsNullOrEmpty(action) || action.StartsWith("empty", StringComparison.Ordinal))
            {
                continue;
            }

            source = "PlanCosts:first";
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

            action = ExtractBetween(line, "ForcedTacticalSupportPlan(action=", ",");
            source = "Forced:first";
            return !string.IsNullOrEmpty(action);
        }

        return false;
    }

    private static bool TryResolveLastPlanCosts(
        IList<string> lines,
        int slot,
        int? playerId,
        out string action,
        out string source)
    {
        action = null;
        source = null;

        for (int i = lines.Count - 1; i >= 0; i--)
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

            if (playerId.HasValue && !line.Contains($"playerId={playerId.Value}"))
            {
                continue;
            }

            action = ExtractSelectedActionFromPlanCosts(line);
            if (string.IsNullOrEmpty(action) || action.StartsWith("empty", StringComparison.Ordinal))
            {
                continue;
            }

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

            action = ExtractBetween(line, "ForcedTacticalSupportPlan(action=", ",");
            source = "Forced:last";
            return !string.IsNullOrEmpty(action);
        }

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

    private static string ExtractBetween(string line, string startToken, string endToken)
    {
        int start = line.IndexOf(startToken, StringComparison.Ordinal);
        if (start < 0)
        {
            return null;
        }

        start += startToken.Length;
        int end = line.IndexOf(endToken, start, StringComparison.Ordinal);
        if (end < 0)
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

        if (expected.IndexOf('|', StringComparison.Ordinal) >= 0)
        {
            foreach (string candidate in expected.Split('|'))
            {
                if (ActionsMatchSingle(candidate, actual))
                {
                    return true;
                }
            }

            return false;
        }

        return ActionsMatchSingle(expected, actual);
    }

    private static bool ActionsMatchSingle(string expected, string actual)
    {
        return string.Equals(expected, actual, StringComparison.Ordinal)
            || actual.StartsWith(expected, StringComparison.Ordinal);
    }
}
