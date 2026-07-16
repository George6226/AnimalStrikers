#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// CLI: Unity -goalkeeperBatchVerify で GK バッチ検証 Play を実行する。
/// </summary>
public static class GoalkeeperBatchVerifyEditorRunner
{
    private const string CliFlag = "-goalkeeperBatchVerify";
    private const string ScenePath = "Assets/Scenes/GameScene.unity";
    private const float TimeoutSeconds = 180f;
    private const string PendingExitFileName = "goap-batch-goalkeeper-pending-exit.txt";
    private const string StartedMarkerFileName = "goap-batch-goalkeeper-started.marker";

    private static bool _handlersRegistered;
    private static bool _shutdownRequested;
    private static double _playEnteredAt;
    private static string _gkDiagPath;
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
            Debug.Log($"[GK_BATCH_RUNNER] exiting after domain reload (code={exitCode})");
            EditorApplication.Exit(exitCode);
            return;
        }

        EnsurePaths();
        RegisterHandlers();

        if (File.Exists(StartedMarkerPath()))
        {
            Debug.Log("[GK_BATCH_RUNNER] resumed after domain reload; waiting for finish");
            return;
        }

        BeginFreshRun();
    }

    private static void BeginFreshRun()
    {
        ConfigureSceneForGoalkeeperBatch();
        ResetLogsForNewRun();
        File.WriteAllText(StartedMarkerPath(), DateTime.UtcNow.ToString("O"));

        if (!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            if (EditorSceneManager.GetActiveScene().path != ScenePath)
            {
                EditorSceneManager.OpenScene(ScenePath);
            }

            Debug.Log("[GK_BATCH_RUNNER] entering play mode");
            EditorApplication.EnterPlaymode();
        }
    }

    private static void ConfigureSceneForGoalkeeperBatch()
    {
        EnsureGameSceneOpen();

        foreach (GoapSupportActionVerificationSetup setup in
                 UnityEngine.Object.FindObjectsByType<GoapSupportActionVerificationSetup>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            setup.enabled = false;
            var serialized = new SerializedObject(setup);
            SetBool(serialized, "_runBatchVerificationOnStart", false);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        foreach (GoapDefenseActionVerificationSetup setup in
                 UnityEngine.Object.FindObjectsByType<GoapDefenseActionVerificationSetup>(
                     FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            setup.enabled = false;
        }

        var bootstrap = UnityEngine.Object.FindFirstObjectByType<GoapMainNpcVerifyBootstrap>(
            FindObjectsInactive.Include);
        if (bootstrap != null)
        {
            bootstrap.enabled = false;
        }

        var squad = UnityEngine.Object.FindFirstObjectByType<SquadControlController>(FindObjectsInactive.Include);
        if (squad != null)
        {
            var serialized = new SerializedObject(squad);
            SetBool(serialized, "_mainNpcGoapVerifyMode", false);
            SetBool(serialized, "_enableMainNpcGoapInProduction", false);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        var enemySquad = UnityEngine.Object.FindFirstObjectByType<EnemySquadControlController>(
            FindObjectsInactive.Include);
        if (enemySquad != null)
        {
            var serialized = new SerializedObject(enemySquad);
            SetBool(serialized, "_enableEnemyGoap", false);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        EnsureGoalkeeperBatchSetup();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    private static void EnsureGoalkeeperBatchSetup()
    {
        var setup = UnityEngine.Object.FindFirstObjectByType<GoalkeeperBatchVerificationSetup>(
            FindObjectsInactive.Include);
        if (setup != null)
        {
            setup.enabled = true;
            return;
        }

        var anchor = UnityEngine.Object.FindFirstObjectByType<GoapCombinedSupportRegressionDebugSetup>(
            FindObjectsInactive.Include);
        if (anchor == null)
        {
            Debug.LogError("[GK_BATCH_RUNNER] GoapCombinedSupportRegressionDebugSetup not found");
            return;
        }

        setup = anchor.gameObject.AddComponent<GoalkeeperBatchVerificationSetup>();
        var serialized = new SerializedObject(setup);
        SetBool(serialized, "_runBatchVerificationOnStart", true);
        SetBool(serialized, "_stopPlayModeWhenBatchEnds", true);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        Debug.Log("[GK_BATCH_RUNNER] added GoalkeeperBatchVerificationSetup");
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

    private static void EnsurePaths()
    {
        _gkDiagPath = Path.Combine(Application.dataPath, "DebugLog/GkDiag_latest.txt");
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
            if (!_shutdownRequested)
            {
                _playEnteredAt = EditorApplication.timeSinceStartup;
                Debug.Log("[GK_BATCH_RUNNER] entered play mode");
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
        if (_shutdownRequested)
        {
            return;
        }

        if (_playEnteredAt > 0d
            && EditorApplication.isPlaying
            && EditorApplication.timeSinceStartup - _playEnteredAt > TimeoutSeconds)
        {
            Debug.LogError("[GK_BATCH_RUNNER] timeout");
            CompleteRun(false, "timeout");
            return;
        }

        if (string.IsNullOrEmpty(_gkDiagPath) || !File.Exists(_gkDiagPath))
        {
            return;
        }

        string logText = File.ReadAllText(_gkDiagPath);
        if (!logText.Contains("BATCH_COMPLETE", StringComparison.Ordinal))
        {
            return;
        }

        bool success = GoalkeeperBatchVerifyRules.IsBatchSucceeded(logText);
        CompleteRun(success, success ? "gk_batch_selection_pass" : "gk_batch_selection_fail");
    }

    private static void TryExitAfterPlayModeEnded()
    {
        EnsurePaths();

        if (TryConsumePendingExit(out int exitCode))
        {
            UnregisterHandlers();
            Debug.Log($"[GK_BATCH_RUNNER] exiting after play mode ended (code={exitCode})");
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
                Path.Combine(_ciLogDir, GoalkeeperBatchVerifyEnvironment.GetResultFileName()),
                $"{(success ? "PASS" : "FAIL")}: {summary}\n");
            if (File.Exists(_gkDiagPath))
            {
                File.Copy(
                    _gkDiagPath,
                    Path.Combine(_ciLogDir, "GkDiag_latest.txt"),
                    overwrite: true);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[GK_BATCH_RUNNER] failed to write CI artifacts: {ex.Message}");
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
        _handlersRegistered = false;
    }

    private static void ResetLogsForNewRun()
    {
        EnsurePaths();
        string dir = Path.GetDirectoryName(_gkDiagPath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        File.WriteAllText(_gkDiagPath, $"[{DateTime.Now:HH:mm:ss.fff}] GK_BATCH_RUNNER armed\n");
    }

    private static string PendingExitPath() => Path.Combine(_ciLogDir, PendingExitFileName);
    private static string StartedMarkerPath() => Path.Combine(_ciLogDir, StartedMarkerFileName);

    private static void WritePendingExit(int exitCode) =>
        File.WriteAllText(PendingExitPath(), exitCode.ToString());

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

    private static bool HasCliFlag(string flag)
    {
        foreach (string arg in Environment.GetCommandLineArgs())
        {
            if (string.Equals(arg, flag, StringComparison.Ordinal))
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
