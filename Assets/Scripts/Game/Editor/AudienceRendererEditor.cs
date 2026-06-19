using UnityEngine;
using UnityEditor;

[CustomEditor(typeof(AudienceRenderer))]
public class AudienceRendererEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        AudienceRenderer renderer = (AudienceRenderer)target;
        if (GUILayout.Button("再設定"))
        {
            renderer.SetupAudience();
        }
    }
} 