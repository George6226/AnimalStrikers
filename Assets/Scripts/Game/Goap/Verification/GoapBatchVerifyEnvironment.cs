using System;
using System.IO;
using UnityEngine;

/// <summary>
/// Unity Editor の <c>-goapBatchVerify</c> CLI 実行時のランタイム設定。
/// Play モードでは CLI 引数が取れない場合があるため、開始マーカーも参照する。
/// </summary>
public static class GoapBatchVerifyEnvironment
{
    private const string CliFlag = "-goapBatchVerify";
    private const string StartedMarkerFileName = "goap-batch-started.marker";
    private static bool? _isActive;

    public static bool IsActive
    {
        get
        {
            if (!_isActive.HasValue)
            {
                _isActive = HasCliFlag() || HasStartedMarker();
            }

            return _isActive.Value;
        }
    }

    public static float ResolveTimeout(float configuredSeconds, float batchMinimumSeconds) =>
        IsActive ? Math.Max(configuredSeconds, batchMinimumSeconds) : configuredSeconds;

    private static bool HasCliFlag() =>
        Array.Exists(
            Environment.GetCommandLineArgs(),
            arg => string.Equals(arg, CliFlag, StringComparison.Ordinal));

    private static bool HasStartedMarker()
    {
        string markerPath = GetStartedMarkerPath();
        return !string.IsNullOrEmpty(markerPath) && File.Exists(markerPath);
    }

    private static string GetStartedMarkerPath()
    {
        if (string.IsNullOrEmpty(Application.dataPath))
        {
            return null;
        }

        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        if (string.IsNullOrEmpty(projectRoot))
        {
            return null;
        }

        return Path.Combine(projectRoot, "Logs", StartedMarkerFileName);
    }
}
