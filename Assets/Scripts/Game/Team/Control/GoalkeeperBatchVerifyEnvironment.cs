using System;

/// <summary>6-H P2: <c>-goalkeeperBatchVerify</c> CLI 実行時のランタイム設定。</summary>
public static class GoalkeeperBatchVerifyEnvironment
{
    public const string CliFlag = "-goalkeeperBatchVerify";
    private const string StartedMarkerFileName = "goap-batch-goalkeeper-started.marker";

    private static bool? _isActive;

    public static bool IsActive
    {
        get
        {
            if (!_isActive.HasValue)
            {
                _isActive = HasCliFlag();
            }

            return _isActive.Value;
        }
    }

    public static string GetResultFileName() => "goap-batch-goalkeeper-result.txt";

    public static string GetLogFileName() => "goap-batch-goalkeeper-verify.log";

    private static bool HasCliFlag()
    {
        foreach (string arg in Environment.GetCommandLineArgs())
        {
            if (string.Equals(arg, CliFlag, StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }
}
