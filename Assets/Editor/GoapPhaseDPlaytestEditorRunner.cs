#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// CLI: Unity -goapPhaseDPlaytest で GameScene 直 Play を約 3 分実行し GOAP ログを収集する。
/// </summary>
public static class GoapPhaseDPlaytestEditorRunner
{
    private const string CliFlag = "-goapPhaseDPlaytest";
    private const string ScenePath = "Assets/Scenes/GameScene.unity";
    private const string PendingExitFileName = "goap-phase-d-pending-exit.txt";
    private const string StartedMarkerFileName = "goap-phase-d-started.marker";

    private static bool _handlersRegistered;
    private static bool _shutdownRequested;
    private static double _playEnteredAt;
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
            Debug.Log($"[GOAP_PHASE_D_RUNNER] exiting after domain reload (code={exitCode})");
            EditorApplication.Exit(exitCode);
            return;
        }

        RegisterHandlers();

        if (File.Exists(StartedMarkerPath()))
        {
            RegisterHandlers();
            Debug.Log("[GOAP_PHASE_D_RUNNER] resumed after domain reload; waiting for spawn/gameplay");
            return;
        }

        BeginFreshRun();
    }

    private static void BeginFreshRun()
    {
        ConfigureSceneForPhaseDPlaytest();
        ResetLogsForNewRun();
        File.WriteAllText(StartedMarkerPath(), DateTime.UtcNow.ToString("O"));

        if (!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            if (EditorSceneManager.GetActiveScene().path != ScenePath)
            {
                EditorSceneManager.OpenScene(ScenePath);
            }

            Debug.Log($"[GOAP_PHASE_D_RUNNER] entering play mode duration={GoapPhaseDPlaytestEnvironment.DurationSeconds:F0}s");
            EditorApplication.EnterPlaymode();
        }
    }

    private static void ConfigureSceneForPhaseDPlaytest()
    {
        EnsureGameSceneOpen();

        var squad = UnityEngine.Object.FindFirstObjectByType<SquadControlController>(FindObjectsInactive.Include);
        if (squad != null)
        {
            var serialized = new SerializedObject(squad);
            SetBool(serialized, "_mainNpcGoapVerifyMode", false);
            SetBool(serialized, "_enableMainNpcGoapInProduction", true);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        var enemySquad = UnityEngine.Object.FindFirstObjectByType<EnemySquadControlController>(FindObjectsInactive.Include);
        if (enemySquad != null)
        {
            var serialized = new SerializedObject(enemySquad);
            SetBool(serialized, "_enableEnemyGoap", true);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        var bootstrap = UnityEngine.Object.FindFirstObjectByType<GoapMainNpcVerifyBootstrap>(FindObjectsInactive.Include);
        if (bootstrap != null)
        {
            bootstrap.enabled = false;
            var serialized = new SerializedObject(bootstrap);
            SetBool(serialized, "_enabled", false);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        foreach (GoapSupportActionVerificationSetup setup in
                 UnityEngine.Object.FindObjectsByType<GoapSupportActionVerificationSetup>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            setup.enabled = false;
            var serialized = new SerializedObject(setup);
            SetBool(serialized, "_runBatchVerificationOnStart", false);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        foreach (GoapDefenseActionVerificationSetup setup in
                 UnityEngine.Object.FindObjectsByType<GoapDefenseActionVerificationSetup>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            setup.enabled = false;
        }

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log("[GOAP_PHASE_D_RUNNER] scene configured (production+enemy GOAP ON)");
    }

    private static void EnsureGameSceneOpen()
    {
        Scene active = SceneManager.GetActiveScene();
        if (active.path == ScenePath)
        {
            return;
        }

        EditorSceneManager.OpenScene(ScenePath);
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

    private static double _playModeEnteredAt;

    private static void OnPlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.EnteredPlayMode)
        {
            if (!_shutdownRequested)
            {
                _playModeEnteredAt = EditorApplication.timeSinceStartup;
                Debug.Log("[GOAP_PHASE_D_RUNNER] entered play mode; waiting for spawn before timer");
            }

            return;
        }

        if (state == PlayModeStateChange.EnteredEditMode)
        {
            TryExitAfterPlayModeEnded();
        }
    }

    private static void OnUpdate()
    {
        if (_shutdownRequested || !EditorApplication.isPlaying)
        {
            return;
        }

        if (_playEnteredAt <= 0d)
        {
            if (_playModeEnteredAt > 0d
                && EditorApplication.timeSinceStartup - _playModeEnteredAt > 120d)
            {
                Debug.LogError("[GOAP_PHASE_D_RUNNER] spawn timeout");
                CompleteRun(false, "spawn_timeout");
                return;
            }

            if (!TryStartGameplayTimer())
            {
                return;
            }
        }

        double elapsed = EditorApplication.timeSinceStartup - _playEnteredAt;
        if (elapsed >= GoapPhaseDPlaytestEnvironment.DurationSeconds)
        {
            Debug.Log($"[GOAP_PHASE_D_RUNNER] duration complete ({elapsed:F0}s)");
            CompleteRun(true, $"duration_complete_{elapsed:F0}s");
        }
    }

    private static bool TryStartGameplayTimer()
    {
        if (GoapDebugPlayBootstrap.IsSpawnReady)
        {
            _playEnteredAt = EditorApplication.timeSinceStartup;
            Debug.Log("[GOAP_PHASE_D_RUNNER] gameplay timer started (spawn ready)");
            return true;
        }

        if (!string.IsNullOrEmpty(_summaryPath) && File.Exists(_summaryPath))
        {
            string summary = File.ReadAllText(_summaryPath);
            if (summary.Contains("PlanCosts(", StringComparison.Ordinal)
                || summary.Contains("ActionStart(action=", StringComparison.Ordinal))
            {
                _playEnteredAt = EditorApplication.timeSinceStartup;
                Debug.Log("[GOAP_PHASE_D_RUNNER] gameplay timer started (first GOAP activity)");
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
            Debug.Log($"[GOAP_PHASE_D_RUNNER] exiting after play mode ended (code={exitCode})");
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
                Path.Combine(_ciLogDir, "goap-phase-d-result.txt"),
                $"{(success ? "PASS" : "FAIL")}: {summary}\n");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[GOAP_PHASE_D_RUNNER] failed to write CI artifacts: {ex.Message}");
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
        string marker = $"[{DateTime.Now:HH:mm:ss.fff}] GOAP_PHASE_D_RUNNER armed\n";
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

    private static void DeletePendingExit()
    {
        string path = PendingExitPath();
        if (File.Exists(path))
        {
            File.Delete(path);
        }
    }

    private static void CleanupMarkerFiles()
    {
        DeletePendingExit();
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

    private static void SetBool(SerializedObject serialized, string propertyName, bool value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null)
        {
            property.boolValue = value;
        }
    }
}
#endif
