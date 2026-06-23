#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// <c>-goapBatchVerify</c> プロファイルに応じて GameScene の検証セットアップを切り替える。
/// </summary>
public static class GoapBatchVerifySceneConfigurator
{
    private const string ScenePath = "Assets/Scenes/GameScene.unity";

    public static void ApplyProfile(GoapBatchVerifyProfile profile)
    {
        EnsureGameSceneOpen();

        var combined = Object.FindFirstObjectByType<GoapCombinedSupportRegressionDebugSetup>(FindObjectsInactive.Include);
        var wingDrive = Object.FindFirstObjectByType<GoapWingOwnerDriveDebugSetup>(FindObjectsInactive.Include);
        var cfDrive = Object.FindFirstObjectByType<GoapCfOwnerDriveDebugSetup>(FindObjectsInactive.Include);

        if (combined == null && wingDrive == null && cfDrive == null)
        {
            Debug.LogError("[GOAP_BATCH] no GoapSupportActionVerificationSetup found in scene");
            return;
        }

        ConfigureSetup(combined, profile == GoapBatchVerifyProfile.Combined);
        ConfigureSetup(wingDrive, profile == GoapBatchVerifyProfile.WingDrive);
        ConfigureSetup(cfDrive, profile == GoapBatchVerifyProfile.CfDrive);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log(
            $"[GOAP_BATCH] scene profile={profile} " +
            $"combined={(combined != null && profile == GoapBatchVerifyProfile.Combined)} " +
            $"wingDrive={(wingDrive != null && profile == GoapBatchVerifyProfile.WingDrive)} " +
            $"cfDrive={(cfDrive != null && profile == GoapBatchVerifyProfile.CfDrive)}");
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

    private static void ConfigureSetup(GoapSupportActionVerificationSetup setup, bool active)
    {
        if (setup == null)
        {
            return;
        }

        setup.enabled = active;
    }
}
#endif
