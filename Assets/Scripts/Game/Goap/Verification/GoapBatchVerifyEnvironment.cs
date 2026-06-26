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
    private const string ProfileMarkerFileName = "goap-batch-profile.txt";
    private static bool? _isActive;
    private static GoapBatchVerifyProfile? _profile;

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

    public static GoapBatchVerifyProfile Profile
    {
        get
        {
            if (!_profile.HasValue)
            {
                _profile = ResolveProfile();
            }

            return _profile.Value;
        }
    }

    public static float ResolveTimeout(float configuredSeconds, float batchMinimumSeconds) =>
        IsActive ? Math.Max(configuredSeconds, batchMinimumSeconds) : configuredSeconds;

    public static string GetResultFileName(GoapBatchVerifyProfile profile) =>
        profile switch
        {
            GoapBatchVerifyProfile.WingDrive => "goap-batch-wing-result.txt",
            GoapBatchVerifyProfile.CfDrive => "goap-batch-cf-drive-result.txt",
            GoapBatchVerifyProfile.DefenseBaseline => "goap-batch-defense-result.txt",
            GoapBatchVerifyProfile.DefenseTactical => "goap-batch-defense-tactical-result.txt",
            GoapBatchVerifyProfile.DefenseDrive => "goap-batch-defense-drive-result.txt",
            GoapBatchVerifyProfile.DefenseCombined => "goap-batch-defense-combined-result.txt",
            _ => "goap-batch-result.txt",
        };

    public static string GetLogFileName(GoapBatchVerifyProfile profile) =>
        profile switch
        {
            GoapBatchVerifyProfile.WingDrive => "goap-batch-wing-verify.log",
            GoapBatchVerifyProfile.CfDrive => "goap-batch-cf-drive-verify.log",
            GoapBatchVerifyProfile.DefenseBaseline => "goap-batch-defense-verify.log",
            GoapBatchVerifyProfile.DefenseTactical => "goap-batch-defense-tactical-verify.log",
            GoapBatchVerifyProfile.DefenseDrive => "goap-batch-defense-drive-verify.log",
            GoapBatchVerifyProfile.DefenseCombined => "goap-batch-defense-combined-verify.log",
            _ => "goap-batch-verify.log",
        };

    public static void WriteProfileMarker(GoapBatchVerifyProfile profile)
    {
        string path = GetProfileMarkerPath();
        if (string.IsNullOrEmpty(path))
        {
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path));
        File.WriteAllText(path, profile.ToString());
        _profile = profile;
    }

    public static void DeleteProfileMarker()
    {
        string path = GetProfileMarkerPath();
        if (!string.IsNullOrEmpty(path) && File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private static GoapBatchVerifyProfile ResolveProfile()
    {
        if (TryParseProfileFromCli(out GoapBatchVerifyProfile cliProfile))
        {
            return cliProfile;
        }

        string markerPath = GetProfileMarkerPath();
        if (!string.IsNullOrEmpty(markerPath) && File.Exists(markerPath))
        {
            string text = File.ReadAllText(markerPath).Trim();
            if (Enum.TryParse(text, ignoreCase: true, out GoapBatchVerifyProfile markerProfile))
            {
                return markerProfile;
            }
        }

        return GoapBatchVerifyProfile.Combined;
    }

    private static bool TryParseProfileFromCli(out GoapBatchVerifyProfile profile)
    {
        profile = GoapBatchVerifyProfile.Combined;
        foreach (string arg in Environment.GetCommandLineArgs())
        {
            if (string.Equals(arg, CliFlag, StringComparison.Ordinal))
            {
                return true;
            }

            if (!arg.StartsWith(CliFlag + "=", StringComparison.Ordinal))
            {
                continue;
            }

            string value = arg.Substring(CliFlag.Length + 1);
            profile = ParseProfileToken(value);
            return true;
        }

        return false;
    }

    private static GoapBatchVerifyProfile ParseProfileToken(string token)
    {
        if (string.IsNullOrWhiteSpace(token)
            || string.Equals(token, "combined", StringComparison.OrdinalIgnoreCase))
        {
            return GoapBatchVerifyProfile.Combined;
        }

        if (string.Equals(token, "wingDrive", StringComparison.OrdinalIgnoreCase)
            || string.Equals(token, "wing", StringComparison.OrdinalIgnoreCase))
        {
            return GoapBatchVerifyProfile.WingDrive;
        }

        if (string.Equals(token, "cfDrive", StringComparison.OrdinalIgnoreCase)
            || string.Equals(token, "cf", StringComparison.OrdinalIgnoreCase))
        {
            return GoapBatchVerifyProfile.CfDrive;
        }

        if (string.Equals(token, "defenseBaseline", StringComparison.OrdinalIgnoreCase)
            || string.Equals(token, "defense", StringComparison.OrdinalIgnoreCase))
        {
            return GoapBatchVerifyProfile.DefenseBaseline;
        }

        if (string.Equals(token, "defenseTactical", StringComparison.OrdinalIgnoreCase)
            || string.Equals(token, "defense-tactical", StringComparison.OrdinalIgnoreCase))
        {
            return GoapBatchVerifyProfile.DefenseTactical;
        }

        if (string.Equals(token, "defenseDrive", StringComparison.OrdinalIgnoreCase)
            || string.Equals(token, "defense-drive", StringComparison.OrdinalIgnoreCase))
        {
            return GoapBatchVerifyProfile.DefenseDrive;
        }

        if (string.Equals(token, "defenseCombined", StringComparison.OrdinalIgnoreCase)
            || string.Equals(token, "defense-combined", StringComparison.OrdinalIgnoreCase))
        {
            return GoapBatchVerifyProfile.DefenseCombined;
        }

        if (Enum.TryParse(token, ignoreCase: true, out GoapBatchVerifyProfile parsed))
        {
            return parsed;
        }

        Debug.LogWarning($"[GOAP_BATCH] unknown profile token '{token}', defaulting to Combined");
        return GoapBatchVerifyProfile.Combined;
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

    private static bool HasStartedMarker()
    {
        string markerPath = GetStartedMarkerPath();
        return !string.IsNullOrEmpty(markerPath) && File.Exists(markerPath);
    }

    private static string GetStartedMarkerPath()
    {
        string logsDir = GetLogsDirectory();
        return string.IsNullOrEmpty(logsDir)
            ? null
            : Path.Combine(logsDir, StartedMarkerFileName);
    }

    private static string GetProfileMarkerPath()
    {
        string logsDir = GetLogsDirectory();
        return string.IsNullOrEmpty(logsDir)
            ? null
            : Path.Combine(logsDir, ProfileMarkerFileName);
    }

    private static string GetLogsDirectory()
    {
        if (string.IsNullOrEmpty(Application.dataPath))
        {
            return null;
        }

        string projectRoot = Directory.GetParent(Application.dataPath)?.FullName;
        return string.IsNullOrEmpty(projectRoot)
            ? null
            : Path.Combine(projectRoot, "Logs");
    }
}
