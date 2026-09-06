using UnityEngine;
using UnityEditor;

namespace GameplayTags.Editor
{
[CustomPropertyDrawer(typeof(SerializableDictionary<,>))]
public class SerializableDictionaryDrawer : PropertyDrawer
{
    private bool foldout = false;
    private const float lineHeight = 18f;
    private const float spacing = 4f;
    private const float buttonWidth = 20f;
    private const float foldoutWidth = 20f;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        SerializedProperty keysProperty = property.FindPropertyRelative("keys");
        SerializedProperty valuesProperty = property.FindPropertyRelative("values");

        if (keysProperty == null || valuesProperty == null)
        {
            EditorGUI.LabelField(position, label.text, "SerializableDictionary not properly initialized");
            EditorGUI.EndProperty();
            return;
        }

        // Ensure both lists have the same size
        while (keysProperty.arraySize > valuesProperty.arraySize)
            valuesProperty.InsertArrayElementAtIndex(valuesProperty.arraySize);
        while (valuesProperty.arraySize > keysProperty.arraySize)
            keysProperty.InsertArrayElementAtIndex(keysProperty.arraySize);

        Rect headerRect = new Rect(position.x, position.y, position.width, lineHeight);
        Rect foldoutRect = new Rect(headerRect.x, headerRect.y, foldoutWidth, headerRect.height);
        Rect labelRect = new Rect(headerRect.x + foldoutWidth, headerRect.y, headerRect.width - foldoutWidth - buttonWidth - spacing, headerRect.height);
        Rect addButtonRect = new Rect(headerRect.x + headerRect.width - buttonWidth, headerRect.y, buttonWidth, headerRect.height);

        foldout = EditorGUI.Foldout(foldoutRect, foldout, GUIContent.none);
        EditorGUI.LabelField(labelRect, $"{label.text} (Size: {keysProperty.arraySize})");

        if (GUI.Button(addButtonRect, "+"))
        {
            int newIndex = keysProperty.arraySize;
            keysProperty.InsertArrayElementAtIndex(newIndex);
            valuesProperty.InsertArrayElementAtIndex(newIndex);

            // Inserted elements copy the previous entry; reset them so the new row is blank.
            ResetProperty(keysProperty.GetArrayElementAtIndex(newIndex));
            ResetProperty(valuesProperty.GetArrayElementAtIndex(newIndex));
        }

        if (foldout)
        {
            EditorGUI.indentLevel++;
            for (int i = 0; i < keysProperty.arraySize; i++)
            {
                Rect itemRect = new Rect(position.x, position.y + (lineHeight + spacing) * (i + 1), position.width, lineHeight);
                DrawKeyValuePair(itemRect, keysProperty.GetArrayElementAtIndex(i), valuesProperty.GetArrayElementAtIndex(i), i, keysProperty, valuesProperty);
            }
            EditorGUI.indentLevel--;
        }

        EditorGUI.EndProperty();
    }

    private void DrawKeyValuePair(Rect rect, SerializedProperty keyProp, SerializedProperty valueProp, int index, SerializedProperty keysArray, SerializedProperty valuesArray)
    {
        float keyWidth = (rect.width - buttonWidth - spacing * 2) * 0.4f;
        float valueWidth = (rect.width - buttonWidth - spacing * 2) * 0.6f;

        Rect keyRect = new Rect(rect.x, rect.y, keyWidth, rect.height);
        Rect valueRect = new Rect(rect.x + keyWidth + spacing, rect.y, valueWidth, rect.height);
        Rect removeButtonRect = new Rect(rect.x + rect.width - buttonWidth, rect.y, buttonWidth, rect.height);

        EditorGUI.PropertyField(keyRect, keyProp, GUIContent.none);
        EditorGUI.PropertyField(valueRect, valueProp, GUIContent.none);

        if (GUI.Button(removeButtonRect, "-"))
        {
            keysArray.DeleteArrayElementAtIndex(index);
            valuesArray.DeleteArrayElementAtIndex(index);
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        if (!foldout)
            return lineHeight;

        SerializedProperty keysProperty = property.FindPropertyRelative("keys");
        if (keysProperty == null)
            return lineHeight;

        return (lineHeight + spacing) * (keysProperty.arraySize + 1);
    }

    private static void ResetProperty(SerializedProperty prop)
    {
        switch (prop.propertyType)
        {
            case SerializedPropertyType.Integer:
                prop.intValue = 0;
                break;
            case SerializedPropertyType.Boolean:
                prop.boolValue = false;
                break;
            case SerializedPropertyType.Float:
                prop.floatValue = 0f;
                break;
            case SerializedPropertyType.String:
                prop.stringValue = string.Empty;
                break;
            case SerializedPropertyType.ObjectReference:
                prop.objectReferenceValue = null;
                break;
            case SerializedPropertyType.Enum:
                prop.enumValueIndex = 0;
                break;
        }
    }
}
}
