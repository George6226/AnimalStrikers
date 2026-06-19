using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

/// <summary>
/// GOAP診断ログ専用のファイル出力ユーティリティ。
/// </summary>
public static class GoapDiagnosticLog
{
    private const string FileName = "GoapDiag_latest.txt";
    private static string _filePath;
    private static bool _initialized;

    public static void Write(string message)
    {
        try
        {
            EnsureInitialized();
            File.AppendAllText(_filePath, $"[{DateTime.Now:HH:mm:ss.fff}] {message}{Environment.NewLine}");
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[GOAP_DIAG] file write failed: {e.Message}");
        }
    }

    public static void WriteBanner(string title)
    {
        Write($"========== {title} ==========");
    }

    /// <summary>バッチ検証開始時など、診断ログを空にして再初期化する。</summary>
    public static void ResetSession()
    {
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
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[GOAP_DIAG] reset failed: {e.Message}");
        }
    }

    public static int CountLines()
    {
        try
        {
            EnsureInitialized();
            if (string.IsNullOrEmpty(_filePath) || !File.Exists(_filePath))
            {
                return 0;
            }

            return File.ReadAllLines(_filePath).Length;
        }
        catch
        {
            return 0;
        }
    }

    public static List<string> ReadLinesSince(int offset)
    {
        var result = new List<string>();
        try
        {
            EnsureInitialized();
            if (string.IsNullOrEmpty(_filePath) || !File.Exists(_filePath))
            {
                return result;
            }

            string[] lines = File.ReadAllLines(_filePath);
            if (offset < 0 || offset >= lines.Length)
            {
                result.AddRange(lines);
                return result;
            }

            for (int i = offset; i < lines.Length; i++)
            {
                result.Add(lines[i]);
            }
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[GOAP_DIAG] ReadLinesSince failed: {e.Message}");
        }

        return result;
    }

    private static void EnsureInitialized()
    {
        if (_initialized) return;

        string dir = Path.Combine(Application.dataPath, "DebugLog");
        if (!Directory.Exists(dir))
        {
            Directory.CreateDirectory(dir);
        }

        _filePath = Path.Combine(dir, FileName);
        File.WriteAllText(_filePath, string.Empty);
        _initialized = true;
    }
}
