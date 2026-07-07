using System;

/// <summary>
/// Unity Editor の <c>-goapPhaseDPlaytest</c> CLI 実行時のランタイム設定（3 分統合プレイテスト）。
/// </summary>
public static class GoapPhaseDPlaytestEnvironment
{
    private const string CliFlag = "-goapPhaseDPlaytest";
    private static bool? _isActive;
    private static float? _durationSeconds;

    public const float DefaultDurationSeconds = 180f;

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
