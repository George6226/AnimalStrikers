using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public static class GoapActionVerificationSessionLog
{
    private static string _filePath;
    private static bool _initialized;

    public static void ResetSession(string bannerTag, string bannerMessage)
    {
        GoapDiagnosticLog.ResetSession();
        try
        {
            string dir = Path.Combine(Application.dataPath, "DebugLog");
            if (!Directory.Exists(dir))
            {
                Directory.CreateDirectory(dir);
            }

            _filePath = Path.Combine(dir, "GoapSummary_latest.txt");
            File.WriteAllText(_filePath, string.Empty);
            _initialized = true;
            GoapAgent.MarkSummaryLogSessionActive();
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[{bannerTag}] reset log failed: {e.Message}");
        }

        if (!string.IsNullOrEmpty(bannerMessage))
        {
            Append(bannerTag, bannerMessage);
        }
    }

    public static void Append(string tag, string message)
    {
        string line = $"[{DateTime.Now:HH:mm:ss.fff}] [{tag}] {message}";
        Debug.Log(line);
        WriteLine(line);
    }

    public static int CountLines()
    {
        try
        {
            if (!_initialized || string.IsNullOrEmpty(_filePath) || !File.Exists(_filePath))
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
            if (!_initialized || string.IsNullOrEmpty(_filePath) || !File.Exists(_filePath))
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
            Debug.LogWarning($"GoapActionVerificationSessionLog.ReadLinesSince failed: {e.Message}");
        }

        return result;
    }

    private static void WriteLine(string line)
    {
        try
        {
            if (!_initialized)
            {
                string dir = Path.Combine(Application.dataPath, "DebugLog");
                if (!Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }

                _filePath = Path.Combine(dir, "GoapSummary_latest.txt");
                _initialized = true;
            }

            File.AppendAllText(_filePath, line + Environment.NewLine);
        }
        catch (Exception e)
        {
            Debug.LogWarning($"GoapActionVerificationSessionLog write failed: {e.Message}");
        }
    }
}
