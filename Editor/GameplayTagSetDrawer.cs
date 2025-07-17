using UnityEditor;
using UnityEngine;

namespace GameplayTags.Editor
{
#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(GameplayTagSet))]
public class GameplayTagSetDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        SerializedProperty tagsProp = property.FindPropertyRelative("tags");
        SerializedProperty valuesProp = tagsProp.FindPropertyRelative("items");
        EditorGUI.PropertyField(position, valuesProp, label, true);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        SerializedProperty tagsProp = property.FindPropertyRelative("tags");
        SerializedProperty valuesProp = tagsProp.FindPropertyRelative("items");
        return EditorGUI.GetPropertyHeight(valuesProp, label, true);
    }
}
#endif
}