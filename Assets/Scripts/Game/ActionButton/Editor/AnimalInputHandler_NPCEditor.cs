#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

/// <summary>
/// AnimalInputHandler_NPC 用カスタムエディタ。
/// 基底クラスの SerializeField（_slidePad など）を NPC 側の Inspector では非表示にする。
/// </summary>
[CustomEditor(typeof(AnimalInputHandler_NPC))]
[CanEditMultipleObjects]
public class AnimalInputHandler_NPCEditor : Editor
{
    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        SerializedProperty prop = serializedObject.GetIterator();
        bool enterChildren = true;

        while (prop.NextVisible(enterChildren))
        {
            enterChildren = false;

            // Script フィールドは表示するが編集不可
            if (prop.name == "m_Script")
            {
                using (new EditorGUI.DisabledGroupScope(true))
                {
                    EditorGUILayout.PropertyField(prop, true);
                }
                continue;
            }

            // 基底クラス側の _slidePad は NPC では非表示にする
            if (prop.name == "_slidePad")
            {
                continue;
            }

            // それ以外のフィールドは通常どおり表示
            EditorGUILayout.PropertyField(prop, true);
        }

        serializedObject.ApplyModifiedProperties();
    }
}
#endif

