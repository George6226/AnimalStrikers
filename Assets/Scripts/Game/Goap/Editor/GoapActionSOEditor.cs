using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

/// <summary>
/// GoapActionSOのカスタムエディター
/// PreconditionとEffectをInspectorで見やすく表示
/// </summary>
[CustomEditor(typeof(GoapActionSO), true)]
public class GoapActionSOEditor : Editor
{
    private bool _showPreconditions = true;
    private bool _showEffects = true;
    
    public override void OnInspectorGUI()
    {
        var actionSO = target as GoapActionSO;
        if (actionSO == null) return;
        
        // 基本情報の表示
        EditorGUILayout.Space();
        EditorGUILayout.LabelField("基本情報", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(actionSO.Description, MessageType.Info);
        
        // アクション名
        EditorGUI.BeginChangeCheck();
        string actionName = EditorGUILayout.TextField("アクション名", actionSO.ActionName);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(actionSO, "Change Action Name");
            // アクション名の変更は直接できないので、コメントで説明
            EditorGUILayout.HelpBox("アクション名はクラス名から自動生成されます", MessageType.Info);
        }
        
        // コスト
        EditorGUI.BeginChangeCheck();
        float cost = EditorGUILayout.FloatField("基礎コスト", actionSO.BaseCost);
        if (EditorGUI.EndChangeCheck())
        {
            Undo.RecordObject(actionSO, "Change Cost");
            // リフレクションを使って_baseCostフィールドに値を設定
            var baseCostField = typeof(GoapActionSO).GetField("_baseCost", 
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            if (baseCostField != null)
            {
                baseCostField.SetValue(actionSO, cost);
                EditorUtility.SetDirty(actionSO);
            }
        }
        
        // 継承先で宣言されたSerializeFieldの表示
        EditorGUILayout.Space();
        // EditorGUILayout.LabelField("追加フィールド", EditorStyles.boldLabel);

        serializedObject.Update();
        SerializedProperty property = serializedObject.GetIterator();
        bool enterChildren = true;
        while (property.NextVisible(enterChildren))
        {
            enterChildren = false;
            if (property.name == "m_Script") continue;
            if (property.name == "_actionName" ||
                property.name == "_baseCost" ||
                property.name == "_preconditions" ||
                property.name == "_effects")
            {
                continue;
            }
            EditorGUILayout.PropertyField(property, true);
        }
        serializedObject.ApplyModifiedProperties();
        
        EditorGUILayout.Space();
        
        // 前提条件の表示
        _showPreconditions = EditorGUILayout.Foldout(_showPreconditions, "前提条件 (Preconditions)", true);
        if (_showPreconditions)
        {
            EditorGUI.indentLevel++;
            DisplayFactList(actionSO.Preconditions, "前提条件");
            EditorGUI.indentLevel--;
        }
        
        EditorGUILayout.Space();
        
        // 効果の表示
        _showEffects = EditorGUILayout.Foldout(_showEffects, "効果 (Effects)", true);
        if (_showEffects)
        {
            EditorGUI.indentLevel++;
            DisplayFactList(actionSO.Effects, "効果");
            EditorGUI.indentLevel--;
        }
        
        // 変更を適用
        if (GUI.changed)
        {
            EditorUtility.SetDirty(actionSO);
        }
    }
    
    /// <summary>
    /// Factリストを表示
    /// </summary>
    private void DisplayFactList(List<GoapCondition> facts, string title)
    {
        if (facts == null || facts.Count == 0)
        {
            EditorGUILayout.HelpBox($"{title}が設定されていません", MessageType.Warning);
            return;
        }
        
        for (int i = 0; i < facts.Count; i++)
        {
            EditorGUILayout.BeginHorizontal();
            
            // Fact名と期待値の表示
            string factName = facts[i].Tag;
            bool expectedValue = facts[i].ExpectedValue;
            bool isValid = SymbolTag.IsValidFactName(factName);
            
            // 有効性に応じて色を変更
            Color originalColor = GUI.color;
            if (!isValid)
            {
                GUI.color = Color.red;
            }
            
            EditorGUILayout.LabelField($"{i + 1}. {factName} = {expectedValue}", EditorStyles.label);
            
            GUI.color = originalColor;
            
            EditorGUILayout.EndHorizontal();
            
            // 無効なFactの場合の警告
            if (!isValid)
            {
                EditorGUILayout.HelpBox($"無効なFact名: {factName}", MessageType.Error);
            }
        }
    }
} 