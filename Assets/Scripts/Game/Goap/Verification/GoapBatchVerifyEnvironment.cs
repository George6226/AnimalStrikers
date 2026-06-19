using System;

/// <summary>
/// Unity Editor の <c>-goapBatchVerify</c> CLI 実行時のランタイム設定。
/// </summary>
public static class GoapBatchVerifyEnvironment
{
    private const string CliFlag = "-goapBatchVerify";
    private static bool? _isActive;

    public static bool IsActive
    {
        get
        {
            if (!_isActive.HasValue)
            {
                _isActive = Array.Exists(
                    Environment.GetCommandLineArgs(),
                    arg => string.Equals(arg, CliFlag, StringComparison.Ordinal));
            }

            return _isActive.Value;
        }
    }

    public static float ResolveTimeout(float configuredSeconds, float batchMinimumSeconds) =>
        IsActive ? Math.Max(configuredSeconds, batchMinimumSeconds) : configuredSeconds;
}
