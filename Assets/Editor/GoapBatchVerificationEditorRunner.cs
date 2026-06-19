#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// CLI: Unity -goapBatchVerify で Play 開始し、BATCH_COMPLETE / BATCH_ABORT 後に終了コードを返して Editor を終了する。
/// Play 終了時の Domain Reload 後も <see cref="PendingExitFileName"/> で終了コードを引き継ぐ。
/// </summary>
public static class GoapBatchVerificationEditorRunner
{
    private const string CliFlag = "-goapBatchVerify";
    private const string ScenePath = "Assets/Scenes/GameScene.unity";
    private const float TimeoutSeconds = 420f;
    private const string PendingExitFileName = "goap-batch-pending-exit.txt";
    private const string StartedMarkerFileName = "goap-batch-started.marker";

    private static bool _handlersRegistered;
    private static bool _playRequested;
    private static bool _shutdownRequested;
    private static bool _sawBatchStart;
    private static double _playEnteredAt;
    private static string _diagPath;
    private static string _summaryPath;
    private static string _ciLogDir;

    [InitializeOnLoadMethod]
    private static void OnLoad()
    {
        if (!HasCliFlag(CliFlag))
        {
            return;
        }

        EditorApplication.delayCall += OnDelayedStartup;
    }

    private static void OnDelayedStartup()
    {
        if (TryConsumePendingExit(out int exitCode))
        {
            Debug.Log($"[GOAP_BATCH_RUNNER] exiting after domain reload (code={exitCode})");
            EditorApplication.Exit(exitCode);
            return;
        }

        EnsurePaths();
        RegisterHandlers();

        if (File.Exists(StartedMarkerPath()))
        {
            Debug.Log("[GOAP_BATCH_RUNNER] resumed after domain reload; waiting for batch finish");
            return;
        }

        BeginFreshRun();
    }

    private static void BeginFreshRun()
    {
        ResetLogsForNewRun();
        File.WriteAllText(StartedMarkerPath(), DateTime.UtcNow.ToString("O"));
        DeletePendingExit();

        if (!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            if (EditorSceneManager.GetActiveScene().path != ScenePath)
            {
                EditorSceneManager.OpenScene(ScenePath);
            }

            _playRequested = true;
            Debug.Log("[GOAP_BATCH_RUNNER] entering play mode");
            EditorApplication.EnterPlaymode();
        }
    }

    private static void EnsurePaths()
    {
        _diagPath = Path.Combine(Application.dataPath, "DebugLog/GoapDiag_latest.txt");
        _summaryPath = Path.Combine(Application.dataPath, "DebugLog/GoapSummary_latest.txt");
        _ciLogDir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Logs");
        Directory.CreateDirectory(_ciLogDir);
    }

    private static void RegisterHandlers()
    {
        if (_handlersRegistered)
        {
            return;
        }

        _handlersRegistered = true;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;
        EditorApplication.update += OnUpdate;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            _playEnteredAt = EditorApplication.timeSinceStartup;
            _sawBatchStart = false;
            Debug.Log("[GOAP_BATCH_RUNNER] entered play mode");
            return;
        }

        if (state == PlayModeStateChange.EnteredEditMode)
        {
            TryExitAfterPlayModeEnded();
        }
    }

    private static void TryExitAfterPlayModeEnded()
    {
        EnsurePaths();

        if (TryConsumePendingExit(out int exitCode))
        {
            Debug.Log($"[GOAP_BATCH_RUNNER] exiting after play mode ended (code={exitCode})");
            EditorApplication.delayCall += () => EditorApplication.Exit(exitCode);
            return;
        }

        if (!File.Exists(StartedMarkerPath()) || !File.Exists(_diagPath))
        {
            return;
        }

        string text = File.ReadAllText(_diagPath);
        bool batchFinished = text.Contains("BATCH_COMPLETE", StringComparison.Ordinal)
            || text.Contains("BATCH_ABORT", StringComparison.Ordinal);
        if (!batchFinished)
        {
            return;
        }

        if (text.Contains("BATCH_COMPLETE", StringComparison.Ordinal)
            && !text.Contains("BATCH_START", StringComparison.Ordinal))
        {
            return;
        }

        GoapBatchVerificationLogParser.Result result = EvaluateBatchResult(text);
        Debug.Log($"[GOAP_BATCH_RUNNER] batch finished after play mode ended: {result.Summary}");
        CompleteRun(result.Succeeded, result.Summary);
    }

    private static void OnUpdate()
    {
        if (_shutdownRequested)
        {
            return;
        }

        if (_playEnteredAt > 0d
            && EditorApplication.isPlaying
            && EditorApplication.timeSinceStartup - _playEnteredAt > TimeoutSeconds)
        {
            Debug.LogError("[GOAP_BATCH_RUNNER] timeout");
            CompleteRun(success: false, summary: "timeout");
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
            }

            return;
        }

        if (string.IsNullOrEmpty(_diagPath) || !File.Exists(_diagPath))
        {
            return;
        }

        string text = File.ReadAllText(_diagPath);
        if (text.Contains("BATCH_START", StringComparison.Ordinal))
        {
            _sawBatchStart = true;
        }

        if (!_sawBatchStart)
        {
            return;
        }

        bool batchFinished = text.Contains("BATCH_COMPLETE", StringComparison.Ordinal)
            || text.Contains("BATCH_ABORT", StringComparison.Ordinal);
        if (!batchFinished)
        {
            return;
        }

        GoapBatchVerificationLogParser.Result result = EvaluateBatchResult(text);
        Debug.Log($"[GOAP_BATCH_RUNNER] batch finished: {result.Summary}");
        CompleteRun(result.Succeeded, result.Summary);

        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
        }
    }

    private static void CompleteRun(bool success, string summary)
    {
        if (_shutdownRequested)
        {
            return;
        }

        int exitCode = success ? 0 : 1;
        WritePendingExit(exitCode);
        WriteCiArtifacts(
            new GoapBatchVerificationLogParser.Result(success, summary),
            File.Exists(_diagPath) ? File.ReadAllText(_diagPath) : string.Empty);

        _shutdownRequested = true;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.update -= OnUpdate;
        _handlersRegistered = false;

        if (!EditorApplication.isPlaying)
        {
            Debug.Log($"[GOAP_BATCH_RUNNER] exiting immediately (code={exitCode})");
            CleanupMarkerFiles();
            EditorApplication.Exit(exitCode);
        }
    }

    private static GoapBatchVerificationLogParser.Result EvaluateBatchResult(string diagText)
    {
        GoapBatchVerificationLogParser.Result result = GoapBatchVerificationLogParser.Evaluate(diagText);
        Debug.Log($"[GOAP_BATCH_RUNNER] {result.Summary}");
        return result;
    }

    private static void WriteCiArtifacts(GoapBatchVerificationLogParser.Result result, string diagText)
    {
        try
        {
            string resultPath = Path.Combine(_ciLogDir, "goap-batch-result.txt");
            File.WriteAllText(
                resultPath,
                $"{(result.Succeeded ? "PASS" : "FAIL")}: {result.Summary}\n");

            if (!string.IsNullOrEmpty(diagText))
            {
                File.WriteAllText(Path.Combine(_ciLogDir, "GoapDiag_latest.txt"), diagText);
            }

            if (File.Exists(_summaryPath))
            {
                File.Copy(_summaryPath, Path.Combine(_ciLogDir, "GoapSummary_latest.txt"), overwrite: true);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[GOAP_BATCH_RUNNER] failed to write CI artifacts: {ex.Message}");
        }
    }

    private static void ResetLogsForNewRun()
    {
        EnsurePaths();
        string marker = $"[{DateTime.Now:HH:mm:ss.fff}] GOAP_BATCH_RUNNER armed\n";
        string diagDir = Path.GetDirectoryName(_diagPath);
        if (!string.IsNullOrEmpty(diagDir))
        {
            Directory.CreateDirectory(diagDir);
        }

        File.WriteAllText(_diagPath, marker);
        File.WriteAllText(_summaryPath, marker);
        _sawBatchStart = false;
    }

    private static string PendingExitPath() => Path.Combine(_ciLogDir, PendingExitFileName);
    private static string StartedMarkerPath() => Path.Combine(_ciLogDir, StartedMarkerFileName);

    private static void WritePendingExit(int exitCode) =>
        File.WriteAllText(PendingExitPath(), exitCode.ToString());

    private static void DeletePendingExit()
    {
        if (File.Exists(PendingExitPath()))
        {
            File.Delete(PendingExitPath());
        }
    }

    private static bool TryConsumePendingExit(out int exitCode)
    {
        exitCode = 1;
        EnsurePaths();
        string path = PendingExitPath();
        if (!File.Exists(path))
        {
            return false;
        }

        string text = File.ReadAllText(path).Trim();
        exitCode = int.TryParse(text, out int parsed) ? parsed : 1;
        CleanupMarkerFiles();
        return true;
    }

    private static void CleanupMarkerFiles()
    {
        if (File.Exists(PendingExitPath()))
        {
            File.Delete(PendingExitPath());
        }

        if (File.Exists(StartedMarkerPath()))
        {
            File.Delete(StartedMarkerPath());
        }
    }

    private static bool HasCliFlag(string flag) =>
        Array.Exists(Environment.GetCommandLineArgs(), arg => string.Equals(arg, flag, StringComparison.Ordinal));
}
#endif
