#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// CLI: Unity -goapWatchPlaytest で MainMenuScene から Play → 試合観戦 → 自動終了する。
/// </summary>
public static class GoapWatchPlaytestEditorRunner
{
    private const string CliFlag = "-goapWatchPlaytest";
    private const string ScenePath = "Assets/Scenes/MainMenuScene.unity";
    private const string PendingExitFileName = "goap-watch-pending-exit.txt";
    private const string StartedMarkerFileName = "goap-watch-started.marker";

    private static bool _handlersRegistered;
    private static bool _shutdownRequested;
    private static bool _offlineMatchStarted;
    private static double _playEnteredAt;
    private static double _playModeEnteredAt;
    private static string _summaryPath;
    private static string _ciLogDir;

    [InitializeOnLoadMethod]
    private static void OnLoad()
    {
        if (!HasCliFlag())
        {
            return;
        }

        EditorApplication.delayCall += OnDelayedStartup;
    }

    private static void OnDelayedStartup()
    {
        EnsurePaths();

        if (TryConsumePendingExit(out int exitCode))
        {
            Debug.Log($"[GOAP_WATCH_RUNNER] exiting after domain reload (code={exitCode})");
            RestoreMainMenuLastScene();
            EditorApplication.Exit(exitCode);
            return;
        }

        RegisterHandlers();

        if (File.Exists(StartedMarkerPath()))
        {
            Debug.Log("[GOAP_WATCH_RUNNER] resumed after domain reload; waiting for gameplay");
            return;
        }

        BeginFreshRun();
    }

    private static void BeginFreshRun()
    {
        ResetLogsForNewRun();
        File.WriteAllText(StartedMarkerPath(), DateTime.UtcNow.ToString("O"));

        if (!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            if (EditorSceneManager.GetActiveScene().path != ScenePath)
            {
                EditorSceneManager.OpenScene(ScenePath);
            }

            Debug.Log(
                $"[GOAP_WATCH_RUNNER] entering play mode from MainMenuScene duration={GoapWatchPlaytestEnvironment.DurationSeconds:F0}s");
            EditorApplication.EnterPlaymode();
        }
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
            if (!_shutdownRequested)
            {
                _offlineMatchStarted = false;
                _playModeEnteredAt = EditorApplication.timeSinceStartup;
                Debug.Log("[GOAP_WATCH_RUNNER] entered play mode; starting offline match from MainMenu");
                EditorApplication.delayCall += TryStartOfflineMatchFromMainMenu;
            }

            return;
        }

        if (state == PlayModeStateChange.EnteredEditMode)
        {
            TryExitAfterPlayModeEnded();
        }
    }

    private static void TryStartOfflineMatchFromMainMenu()
    {
        if (_offlineMatchStarted || _shutdownRequested || !EditorApplication.isPlaying)
        {
            return;
        }

        PhotonRoomMatching matching = UnityEngine.Object.FindFirstObjectByType<PhotonRoomMatching>(FindObjectsInactive.Include);
        if (matching == null)
        {
            if (EditorApplication.timeSinceStartup - _playModeEnteredAt < 15d)
            {
                EditorApplication.delayCall += TryStartOfflineMatchFromMainMenu;
            }

            return;
        }

        if (!matching.gameObject.activeSelf)
        {
            matching.gameObject.SetActive(true);
            Debug.Log("[GOAP_WATCH_RUNNER] activated Matchings (offline NPC battle)");
        }

        _offlineMatchStarted = true;
    }

    private static void OnUpdate()
    {
        if (_shutdownRequested || !EditorApplication.isPlaying)
        {
            return;
        }

        if (!_offlineMatchStarted)
        {
            TryStartOfflineMatchFromMainMenu();
        }

        if (_playEnteredAt <= 0d)
        {
            if (_playModeEnteredAt > 0d
                && EditorApplication.timeSinceStartup - _playModeEnteredAt > 180d)
            {
                Debug.LogError("[GOAP_WATCH_RUNNER] spawn / match timeout");
                CompleteRun(false, "spawn_timeout");
                return;
            }

            if (!TryStartGameplayTimer())
            {
                return;
            }
        }

        double elapsed = EditorApplication.timeSinceStartup - _playEnteredAt;
        if (elapsed >= GoapWatchPlaytestEnvironment.DurationSeconds)
        {
            Debug.Log($"[GOAP_WATCH_RUNNER] duration complete ({elapsed:F0}s)");
            CompleteRun(true, $"duration_complete_{elapsed:F0}s");
        }
    }

    private static bool TryStartGameplayTimer()
    {
        if (GoapDebugPlayBootstrap.IsSpawnReady)
        {
            _playEnteredAt = EditorApplication.timeSinceStartup;
            Debug.Log("[GOAP_WATCH_RUNNER] gameplay timer started (spawn ready)");
            return true;
        }

        if (!string.IsNullOrEmpty(_summaryPath) && File.Exists(_summaryPath))
        {
            string summary = File.ReadAllText(_summaryPath);
            if (summary.Contains("PlanCosts(", StringComparison.Ordinal)
                || summary.Contains("ActionStart(action=", StringComparison.Ordinal)
                || summary.Contains("PlanSuccess(", StringComparison.Ordinal))
            {
                _playEnteredAt = EditorApplication.timeSinceStartup;
                Debug.Log("[GOAP_WATCH_RUNNER] gameplay timer started (first GOAP activity)");
                return true;
            }
        }

        return false;
    }

    private static void TryExitAfterPlayModeEnded()
    {
        EnsurePaths();

        if (TryConsumePendingExit(out int exitCode))
        {
            UnregisterHandlers();
            RestoreMainMenuLastScene();
            Debug.Log($"[GOAP_WATCH_RUNNER] exiting after play mode ended (code={exitCode})");
            EditorApplication.delayCall += () => EditorApplication.Exit(exitCode);
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

        try
        {
            File.WriteAllText(
                Path.Combine(_ciLogDir, "goap-watch-result.txt"),
                $"{(success ? "PASS" : "FAIL")}: {summary}\n");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[GOAP_WATCH_RUNNER] failed to write result: {ex.Message}");
        }

        _shutdownRequested = true;
        EditorApplication.update -= OnUpdate;

        if (EditorApplication.isPlaying)
        {
            EditorApplication.isPlaying = false;
            return;
        }

        UnregisterHandlers();
        CleanupMarkerFiles();
        RestoreMainMenuLastScene();
        EditorApplication.Exit(exitCode);
    }

    private static void UnregisterHandlers()
    {
        if (!_handlersRegistered)
        {
            return;
        }

        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.update -= OnUpdate;
        _handlersRegistered = false;
    }

    private static void EnsurePaths()
    {
        _summaryPath = Path.Combine(Application.dataPath, "DebugLog/GoapSummary_latest.txt");
        _ciLogDir = Path.Combine(Directory.GetParent(Application.dataPath).FullName, "Logs");
        Directory.CreateDirectory(_ciLogDir);
    }

    private static void ResetLogsForNewRun()
    {
        EnsurePaths();
        string marker = $"[{DateTime.Now:HH:mm:ss.fff}] GOAP_WATCH_RUNNER armed\n";
        string dir = Path.GetDirectoryName(_summaryPath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        string diagPath = Path.Combine(Application.dataPath, "DebugLog/GoapDiag_latest.txt");
        GoapAgent.MarkSummaryLogSessionActive();
        File.WriteAllText(_summaryPath, marker);
        File.WriteAllText(diagPath, marker);
    }

    private static void RestoreMainMenuLastScene()
    {
        string lastScenePath = Path.Combine(
            Directory.GetParent(Application.dataPath).FullName,
            "Library/LastSceneManagerSetup.txt");
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(lastScenePath)!);
            File.WriteAllText(
                lastScenePath,
                "sceneSetups:\n- path: Assets/Scenes/MainMenuScene.unity\n  isLoaded: 1\n  isActive: 1\n  isSubScene: 0\n");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[GOAP_WATCH_RUNNER] failed to restore MainMenu last scene: {ex.Message}");
        }
    }

    private static string PendingExitPath() => Path.Combine(_ciLogDir, PendingExitFileName);
    private static string StartedMarkerPath() => Path.Combine(_ciLogDir, StartedMarkerFileName);

    private static void WritePendingExit(int exitCode) =>
        File.WriteAllText(PendingExitPath(), exitCode.ToString());

    private static bool TryConsumePendingExit(out int exitCode)
    {
        exitCode = 1;
        string path = PendingExitPath();
        if (!File.Exists(path))
        {
            return false;
        }

        string text = File.ReadAllText(path).Trim();
        CleanupMarkerFiles();
        return int.TryParse(text, out exitCode);
    }

    private static void CleanupMarkerFiles()
    {
        string pending = PendingExitPath();
        if (File.Exists(pending))
        {
            File.Delete(pending);
        }

        string started = StartedMarkerPath();
        if (File.Exists(started))
        {
            File.Delete(started);
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
}
#endif
