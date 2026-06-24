#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;

/// <summary>
/// GoapDiag_latest.txt のバッチ検証結果を解析する。
/// </summary>
public static class GoapBatchVerificationLogParser
{
    private static readonly Regex TotalBannerRegex = new(
        @"(SELECTION_TOTAL|RUNTIME_TOTAL)\s+(\d+)\s*/\s*(\d+)",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);

    public readonly struct Result
    {
        public bool Succeeded { get; }
        public string Summary { get; }
        public int PassCount { get; }
        public int EvalCount { get; }

        public Result(bool succeeded, string summary, int passCount = 0, int evalCount = 0)
        {
            Succeeded = succeeded;
            Summary = summary;
            PassCount = passCount;
            EvalCount = evalCount;
        }
    }

    public static Result Evaluate(string diagText)
    {
        if (string.IsNullOrWhiteSpace(diagText))
        {
            return new Result(false, "GoapDiag log is empty");
        }

        if (diagText.Contains("BATCH_ABORT", StringComparison.Ordinal))
        {
            return new Result(false, DescribeBatchAbort(diagText));
        }

        if (ContainsFailureBanner(diagText))
        {
            return new Result(false, "SELECTION_FAIL or RUNTIME_FAIL detected");
        }

        if (!diagText.Contains("BATCH_COMPLETE", StringComparison.Ordinal))
        {
            return new Result(false, "BATCH_COMPLETE missing");
        }

        List<(string label, int pass, int eval)> totals = CollectTotals(diagText);
        if (totals.Count == 0)
        {
            return new Result(false, "SELECTION_TOTAL / RUNTIME_TOTAL missing");
        }

        int totalPass = 0;
        int totalEval = 0;
        foreach ((string label, int pass, int eval) in totals)
        {
            if (eval <= 0)
            {
                return new Result(false, $"invalid {label}: {pass}/{eval}");
            }

            if (pass != eval)
            {
                return new Result(false, $"{label} mismatch: {pass}/{eval}", pass, eval);
            }

            totalPass += pass;
            totalEval += eval;
        }

        string summary = totals.Count == 1
            ? $"batch passed {totals[0].pass}/{totals[0].eval}"
            : $"batch passed {string.Join(" + ", FormatTotals(totals))}";

        return new Result(true, summary, totalPass, totalEval);
    }

    private static List<(string label, int pass, int eval)> CollectTotals(string diagText)
    {
        var totals = new Dictionary<string, (int pass, int eval)>(StringComparer.Ordinal);

        foreach (Match match in TotalBannerRegex.Matches(diagText))
        {
            string label = match.Groups[1].Value;
            int passCount = int.Parse(match.Groups[2].Value);
            int evalCount = int.Parse(match.Groups[3].Value);
            totals[label] = (passCount, evalCount);
        }

        var result = new List<(string label, int pass, int eval)>();
        foreach (KeyValuePair<string, (int pass, int eval)> entry in totals)
        {
            result.Add((entry.Key, entry.Value.pass, entry.Value.eval));
        }

        return result;
    }

    private static IEnumerable<string> FormatTotals(List<(string label, int pass, int eval)> totals)
    {
        foreach ((string label, int pass, int eval) in totals)
        {
            yield return $"{label} {pass}/{eval}";
        }
    }

    private static bool ContainsFailureBanner(string diagText)
    {
        foreach (string line in diagText.Split('\n'))
        {
            if (line.Contains("SELECTION_FAIL", StringComparison.Ordinal)
                || line.Contains("RUNTIME_FAIL", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static string DescribeBatchAbort(string diagText)
    {
        foreach (string line in diagText.Split('\n'))
        {
            if (!line.Contains("BATCH_ABORT", StringComparison.Ordinal))
            {
                continue;
            }

            int index = line.IndexOf("BATCH_ABORT", StringComparison.Ordinal);
            string detail = line.Substring(index).Trim();
            int bannerEnd = detail.IndexOf(" ==========", StringComparison.Ordinal);
            if (bannerEnd > 0)
            {
                detail = detail.Substring(0, bannerEnd).Trim();
            }

            return detail;
        }

        return "BATCH_ABORT detected";
    }
}
#endif
