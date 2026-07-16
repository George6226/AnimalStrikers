/// <summary>
/// 6-H P2: GK バッチ検証ルール（EditMode 向け純関数 + GkDiag 評価）。
/// </summary>
public static class GoalkeeperBatchVerifyRules
{
    public static GoalkeeperPositioning.Mode GetExpectedMode(GoalkeeperBatchScenarioId scenario) =>
        scenario switch
        {
            GoalkeeperBatchScenarioId.EnemyThreatTrackBall => GoalkeeperPositioning.Mode.TrackBall,
            _ => GoalkeeperPositioning.Mode.HoldLine,
        };

    public static bool TryParseSelectionTotal(string logText, out int passCount, out int evalCount)
    {
        passCount = 0;
        evalCount = 0;
        if (string.IsNullOrEmpty(logText))
        {
            return false;
        }

        string[] lines = logText.Split('\n');
        for (int i = lines.Length - 1; i >= 0; i--)
        {
            string line = lines[i];
            int marker = line.IndexOf("SELECTION_TOTAL", System.StringComparison.Ordinal);
            if (marker < 0)
            {
                continue;
            }

            int slash = line.IndexOf('/', marker);
            if (slash < 0)
            {
                return false;
            }

            int start = slash - 1;
            while (start >= 0 && char.IsDigit(line[start]))
            {
                start--;
            }

            int end = slash + 1;
            while (end < line.Length && char.IsDigit(line[end]))
            {
                end++;
            }

            string passText = line.Substring(start + 1, slash - start - 1);
            string evalText = line.Substring(slash + 1, end - slash - 1);
            return int.TryParse(passText, out passCount) && int.TryParse(evalText, out evalCount);
        }

        return false;
    }

    public static bool IsBatchSucceeded(string logText)
    {
        if (string.IsNullOrEmpty(logText)
            || logText.IndexOf("BATCH_ABORT", System.StringComparison.Ordinal) >= 0
            || logText.IndexOf("SELECTION_FAIL", System.StringComparison.Ordinal) >= 0)
        {
            return false;
        }

        if (logText.IndexOf("BATCH_COMPLETE", System.StringComparison.Ordinal) < 0)
        {
            return false;
        }

        if (!TryParseSelectionTotal(logText, out int passCount, out int evalCount))
        {
            return false;
        }

        return evalCount > 0 && passCount == evalCount;
    }
}
