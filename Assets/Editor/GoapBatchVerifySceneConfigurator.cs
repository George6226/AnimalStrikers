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
        var defenseBaseline = EnsureDefenseBaselineSetup(combined);

        if (combined == null && wingDrive == null && cfDrive == null && defenseBaseline == null)
        {
            Debug.LogError("[GOAP_BATCH] no Goap verification setup found in scene");
            return;
        }

        ConfigureSupportSetup(combined, profile == GoapBatchVerifyProfile.Combined);
        ConfigureSupportSetup(wingDrive, profile == GoapBatchVerifyProfile.WingDrive);
        ConfigureSupportSetup(cfDrive, profile == GoapBatchVerifyProfile.CfDrive);
        ConfigureDefenseSetup(defenseBaseline, profile == GoapBatchVerifyProfile.DefenseBaseline);

        EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
        Debug.Log(
            $"[GOAP_BATCH] scene profile={profile} " +
            $"combined={(combined != null && profile == GoapBatchVerifyProfile.Combined)} " +
            $"wingDrive={(wingDrive != null && profile == GoapBatchVerifyProfile.WingDrive)} " +
            $"cfDrive={(cfDrive != null && profile == GoapBatchVerifyProfile.CfDrive)} " +
            $"defenseBaseline={(defenseBaseline != null && profile == GoapBatchVerifyProfile.DefenseBaseline)}");
    }

    private static GoapCombinedDefenseBaselineDebugSetup EnsureDefenseBaselineSetup(
        GoapCombinedSupportRegressionDebugSetup combined)
    {
        var defense = Object.FindFirstObjectByType<GoapCombinedDefenseBaselineDebugSetup>(FindObjectsInactive.Include);
        if (defense != null)
        {
            return defense;
        }

        if (combined == null)
        {
            return null;
        }

        defense = combined.gameObject.AddComponent<GoapCombinedDefenseBaselineDebugSetup>();
        var serialized = new SerializedObject(defense);
        SetBool(serialized, "_runBatchVerificationOnStart", true);
        SetBool(serialized, "_verifyProductionSelection", true);
        SetBool(serialized, "_restrictCandidatesToActionUnderTest", true);
        SetBool(serialized, "_verificationOnlyDefenseAction", false);
        SetBool(serialized, "_assignBallToEnemyOnApply", true);
        SetInt(serialized, "_batchPatternIndexStart", 2);
        SetInt(serialized, "_batchPatternIndexEnd", 3);
        serialized.ApplyModifiedPropertiesWithoutUndo();
        Debug.Log("[GOAP_BATCH] added GoapCombinedDefenseBaselineDebugSetup to scene");
        return defense;
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

    private static void ConfigureSupportSetup(GoapSupportActionVerificationSetup setup, bool active)
    {
        if (setup == null)
        {
            return;
        }

        setup.enabled = active;
    }

    private static void ConfigureDefenseSetup(GoapCombinedDefenseBaselineDebugSetup setup, bool active)
    {
        if (setup == null)
        {
            return;
        }

        setup.enabled = active;
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
