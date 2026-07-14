using System;

/// <summary>
/// Unity Editor の <c>-goapWatchPlaytest</c> CLI 実行時設定（MainMenu 経由の観戦プレイ）。
/// </summary>
public static class GoapWatchPlaytestEnvironment
{
    private const string CliFlag = "-goapWatchPlaytest";
    private static bool? _isActive;
    private static float? _durationSeconds;

    public const float DefaultDurationSeconds = 120f;

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

    public static float DurationSeconds
    {
        get
        {
            if (!_durationSeconds.HasValue)
            {
                _durationSeconds = ResolveDurationSeconds();
            }

            return _durationSeconds.Value;
        }
    }

    private static bool HasCliFlag()
    {
        foreach (string arg in Environment.GetCommandLineArgs())
        {
            if (string.Equals(arg, CliFlag, StringComparison.Ordinal)
                || arg.StartsWith(CliFlag + "=", StringComparison.Ordinal))
            {
                return true;
            }
        }

        return false;
    }

    private static float ResolveDurationSeconds()
    {
        foreach (string arg in Environment.GetCommandLineArgs())
        {
            if (!arg.StartsWith(CliFlag + "=", StringComparison.Ordinal))
            {
                continue;
            }

            string value = arg.Substring(CliFlag.Length + 1);
            if (float.TryParse(value, out float seconds) && seconds > 10f)
            {
                return seconds;
            }
        }

        return DefaultDurationSeconds;
    }
}
