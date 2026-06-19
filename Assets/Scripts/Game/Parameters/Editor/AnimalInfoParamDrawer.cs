using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(Param_AnimalInfo.AnimalInfoParam))]
public class AnimalInfoParamDrawer : PropertyDrawer
{
    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        float line = EditorGUIUtility.singleLineHeight;
        float space = EditorGUIUtility.standardVerticalSpacing;

        // _animalName / animalType / _hasCombatParam / _combatParam / icon / _isGK
        var combatParam = property.FindPropertyRelative("_combatParam");
        float combatHeight = EditorGUI.GetPropertyHeight(combatParam, true);

        return (line * 5) + combatHeight + (space * 5);
    }

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        float line = EditorGUIUtility.singleLineHeight;
        float space = EditorGUIUtility.standardVerticalSpacing;

        Rect row = new Rect(position.x, position.y, position.width, line);

        var animalName = property.FindPropertyRelative("_animalName");
        var animalType = property.FindPropertyRelative("animalType");
        var hasCombatParam = property.FindPropertyRelative("_hasCombatParam");
        var combatParam = property.FindPropertyRelative("_combatParam");
        var icon = property.FindPropertyRelative("icon");
        var isGK = property.FindPropertyRelative("_isGK");

        EditorGUI.PropertyField(row, animalName);
        row.y += line + space;

        EditorGUI.PropertyField(row, animalType);
        row.y += line + space;

        EditorGUI.PropertyField(row, hasCombatParam);
        row.y += line + space;

        float combatHeight = EditorGUI.GetPropertyHeight(combatParam, true);
        var combatRect = new Rect(row.x, row.y, row.width, combatHeight);
        EditorGUI.BeginDisabledGroup(!hasCombatParam.boolValue);
        EditorGUI.PropertyField(combatRect, combatParam, true);
        EditorGUI.EndDisabledGroup();
        row.y += combatHeight + space;

        EditorGUI.PropertyField(row, icon);
        row.y += line + space;

        EditorGUI.PropertyField(row, isGK);

        EditorGUI.EndProperty();
    }
}
