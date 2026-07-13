using System;
using System.IO;
using UnityEngine;

/// <summary>
/// GK 当たり判定・位置取りの診断ログ（Assets/DebugLog/GkDiag_latest.txt）。
/// GOAP Summary ログ有効時、または GK_DIAG=1 で有効化。
/// </summary>
public static class GoalkeeperDiagnosticLog
{
    private const string FileName = "GkDiag_latest.txt";
    private static string _filePath;
    private static bool _initialized;
    private static bool _enabled;
    private static float _lastProximityLogTime = -999f;

    public static bool Enabled => _enabled;

    public static void SetEnabled(bool enabled)
    {
        _enabled = enabled;
    }

    public static void SyncFromEnvironmentAndGoap()
    {
        string env = Environment.GetEnvironmentVariable("GK_DIAG");
        bool fromEnv = env == "1" || string.Equals(env, "true", StringComparison.OrdinalIgnoreCase);
        _enabled = fromEnv || GoapRuntimeDiagnostics.SummaryLoggingEnabled;
    }

    public static void ResetSession()
    {
        SyncFromEnvironmentAndGoap();
        if (!_enabled)
        {
            return;
        }

        try
        {
            string dir = Path.Combine(Application.dataPath, "DebugLog");
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            _filePath = Path.Combine(dir, FileName);
            File.WriteAllText(_filePath, string.Empty);
            _initialized = true;
            WriteBanner("GK_DIAG_SESSION_START");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[GK_DIAG] reset failed: {e.Message}");
        }
    }

    public static void Write(string message)
    {
        if (!_enabled)
        {
            return;
        }

        try
        {
            EnsureInitialized();
            string line = $"[{DateTime.Now:HH:mm:ss.fff}] {message}";
            File.AppendAllText(_filePath, line + Environment.NewLine);
            Debug.Log($"[GK_DIAG] {message}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[GK_DIAG] file write failed: {e.Message}");
        }
    }

    public static void WriteBanner(string title)
    {
        Write($"========== {title} ==========");
    }

  /// <summary>近接監視ログ（スパム防止で間引き）。</summary>
    public static void WriteProximityThrottled(string message, float intervalSeconds = 1.5f)
    {
        if (!_enabled)
        {
            return;
        }

        if (Time.time - _lastProximityLogTime < intervalSeconds)
        {
            return;
        }

        _lastProximityLogTime = Time.time;
        Write(message);
    }

    private static void EnsureInitialized()
    {
        if (_initialized)
        {
            return;
        }

        string dir = Path.Combine(Application.dataPath, "DebugLog");
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        _filePath = Path.Combine(dir, FileName);
        if (!File.Exists(_filePath))
        {
            File.WriteAllText(_filePath, string.Empty);
        }

        _initialized = true;
    }
}
