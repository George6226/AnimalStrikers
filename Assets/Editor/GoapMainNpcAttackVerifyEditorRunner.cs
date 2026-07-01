#if UNITY_EDITOR
using System;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// CLI: Unity -goapMainNpcAttackVerify で M1 Main NPC Pass/Shoot 検証 Play を実行する。
/// </summary>
public static class GoapMainNpcAttackVerifyEditorRunner
{
    private const string CliFlag = "-goapMainNpcAttackVerify";
    private const string ScenePath = "Assets/Scenes/GameScene.unity";
    private const float TimeoutSeconds = 180f;
    private const string PendingExitFileName = "goap-main-npc-attack-pending-exit.txt";
    private const string StartedMarkerFileName = "goap-main-npc-attack-started.marker";

    private static bool _handlersRegistered;
    private static bool _playRequested;
    private static bool _shutdownRequested;
    private static double _playEnteredAt;
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
            Debug.Log($"[GOAP_M1_ATTACK_RUNNER] exiting after domain reload (code={exitCode})");
            EditorApplication.Exit(exitCode);
            return;
        }

        EnsurePaths();
        RegisterHandlers();

        if (File.Exists(StartedMarkerPath()))
        {
            Debug.Log("[GOAP_M1_ATTACK_RUNNER] resumed after domain reload; waiting for finish");
            return;
        }

        BeginFreshRun();
    }

    private static void BeginFreshRun()
    {
        ConfigureSceneForAttackVerify();
        ResetLogsForNewRun();
        File.WriteAllText(StartedMarkerPath(), DateTime.UtcNow.ToString("O"));

        if (!EditorApplication.isPlaying && !EditorApplication.isPlayingOrWillChangePlaymode)
        {
            if (EditorSceneManager.GetActiveScene().path != ScenePath)
            {
                EditorSceneManager.OpenScene(ScenePath);
            }

            _playRequested = true;
            Debug.Log("[GOAP_M1_ATTACK_RUNNER] entering play mode");
            EditorApplication.EnterPlaymode();
        }
    }

    private static void ConfigureSceneForAttackVerify()
    {
        EnsureGameSceneOpen();

        var squad = UnityEngine.Object.FindFirstObjectByType<SquadControlController>(FindObjectsInactive.Include);
        if (squad != null)
        {
            var serialized = new SerializedObject(squad);
            SetBool(serialized, "_mainNpcGoapVerifyMode", true);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        var bootstrap = UnityEngine.Object.FindFirstObjectByType<GoapMainNpcVerifyBootstrap>(FindObjectsInactive.Include);
        if (bootstrap != null)
        {
            bootstrap.enabled = true;
            var serialized = new SerializedObject(bootstrap);
            SetBool(serialized, "_enabled", true);
            SetBool(serialized, "_requireMainNpcVerifyMode", true);
            SetInt(serialized, "_ballTarget", (int)GoapMainNpcVerifyBootstrapBallTarget.MainNpcForAttackVerify);
            SetBool(serialized, "_applyEnemyLayoutOnStart", false);
            SetBool(serialized, "_triggerGoapReplanAfterAssign", true);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        DisableOtherVerificationSetups();
        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
    }

    private static void DisableOtherVerificationSetups()
    {
        foreach (GoapSupportActionVerificationSetup setup in
                 UnityEngine.Object.FindObjectsByType<GoapSupportActionVerificationSetup>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            setup.enabled = false;
        }

        foreach (GoapDefenseActionVerificationSetup setup in
                 UnityEngine.Object.FindObjectsByType<GoapDefenseActionVerificationSetup>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            setup.enabled = false;
        }
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
            Debug.Log("[GOAP_M1_ATTACK_RUNNER] entered play mode");
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
            Debug.LogError("[GOAP_M1_ATTACK_RUNNER] timeout");
            CompleteRun(false, "timeout");
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
            }

            return;
        }

        if (string.IsNullOrEmpty(_summaryPath) || !File.Exists(_summaryPath))
        {
            return;
        }

        string summary = File.ReadAllText(_summaryPath);
        if (!summary.Contains("bootstrap complete", StringComparison.Ordinal))
        {
            return;
        }

        if (summary.Contains("ActionStart(action=PassToTeammate", StringComparison.Ordinal)
            || summary.Contains("ActionStart(action=ShootAtGoal", StringComparison.Ordinal))
        {
            CompleteRun(true, "main_npc_attack_action_started");
            if (EditorApplication.isPlaying)
            {
                EditorApplication.isPlaying = false;
            }
        }
    }

    private static void TryExitAfterPlayModeEnded()
    {
        EnsurePaths();

        if (TryConsumePendingExit(out int exitCode))
        {
            Debug.Log($"[GOAP_M1_ATTACK_RUNNER] exiting after play mode ended (code={exitCode})");
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
                Path.Combine(_ciLogDir, "goap-main-npc-attack-result.txt"),
                $"{(success ? "PASS" : "FAIL")}: {summary}\n");
            if (File.Exists(_summaryPath))
            {
                File.Copy(_summaryPath, Path.Combine(_ciLogDir, "GoapSummary_latest.txt"), overwrite: true);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[GOAP_M1_ATTACK_RUNNER] failed to write CI artifacts: {ex.Message}");
        }

        _shutdownRequested = true;
        EditorApplication.playModeStateChanged -= OnPlayModeStateChanged;
        EditorApplication.update -= OnUpdate;
        _handlersRegistered = false;

        if (!EditorApplication.isPlaying)
        {
            CleanupMarkerFiles();
            EditorApplication.Exit(exitCode);
        }
    }

    private static void ResetLogsForNewRun()
    {
        EnsurePaths();
        string marker = $"[{DateTime.Now:HH:mm:ss.fff}] GOAP_M1_ATTACK_RUNNER armed\n";
        string dir = Path.GetDirectoryName(_summaryPath);
        if (!string.IsNullOrEmpty(dir))
        {
            Directory.CreateDirectory(dir);
        }

        GoapAgent.MarkSummaryLogSessionActive();
        File.WriteAllText(_summaryPath, marker);
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

    private static void SetInt(SerializedObject serialized, string propertyName, int value)
    {
        SerializedProperty property = serialized.FindProperty(propertyName);
        if (property != null)
        {
            property.intValue = value;
        }
    }
}
#endif
