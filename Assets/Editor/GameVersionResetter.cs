using UnityEngine;
using UnityEditor;

public static class GameVersionResetter
{
    [MenuItem("Tools/Reset Game Version PlayerPrefs")]
    public static void ResetGameVersion()
    {
        PlayerPrefs.SetInt(DataKey.DATAKEY_INT_GAME_VERSION, 0);
        PlayerPrefs.Save();

        Debug.Log($"PlayerPrefs {DataKey.DATAKEY_INT_GAME_VERSION} を0に設定しました。");
        EditorUtility.DisplayDialog("Reset Game Version", $"PlayerPrefsキー {DataKey.DATAKEY_INT_GAME_VERSION} を0にしました。", "OK");
    }
}

