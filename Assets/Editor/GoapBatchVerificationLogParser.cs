#if UNITY_EDITOR
using System;
using System.Text.RegularExpressions;

/// <summary>
/// GoapDiag_latest.txt のバッチ検証結果を解析する。
/// </summary>
public static class GoapBatchVerificationLogParser
{
    private static readonly Regex TotalBannerRegex = new(
        @"(?:SELECTION_TOTAL|RUNTIME_TOTAL)\s+(\d+)\s*/\s*(\d+)",
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
            return new Result(false, "BATCH_ABORT detected");
        }

        if (ContainsFailureBanner(diagText))
        {
            return new Result(false, "SELECTION_FAIL or RUNTIME_FAIL detected");
        }

        if (!diagText.Contains("BATCH_COMPLETE", StringComparison.Ordinal))
        {
            return new Result(false, "BATCH_COMPLETE missing");
        }

        Match lastTotal = null;
        foreach (Match match in TotalBannerRegex.Matches(diagText))
        {
            lastTotal = match;
        }

        if (lastTotal == null)
        {
            return new Result(false, "SELECTION_TOTAL / RUNTIME_TOTAL missing");
        }

        int passCount = int.Parse(lastTotal.Groups[1].Value);
        int evalCount = int.Parse(lastTotal.Groups[2].Value);
        if (evalCount <= 0)
        {
            return new Result(false, $"invalid total banner: {passCount}/{evalCount}");
        }

        if (passCount != evalCount)
        {
            return new Result(
                false,
                $"total mismatch: {passCount}/{evalCount}",
                passCount,
                evalCount);
        }

        return new Result(
            true,
            $"batch passed {passCount}/{evalCount}",
            passCount,
            evalCount);
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
}
#endif
