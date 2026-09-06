using System.Text;
using UnityEditor;
using UnityEngine;

namespace GameplayTags.Editor
{
/// <summary>
/// Draws a <see cref="GameplayTagQuery"/> as a foldout whose closed header states the whole query
/// on one line, so a list of queries can be read without opening any of them.
/// </summary>
[CustomPropertyDrawer(typeof(GameplayTagQuery))]
public class GameplayTagQueryDrawer : PropertyDrawer
{
    const float CLAUSE_SPACING = 2f;

    static readonly GUIContent[] CLAUSE_LABELS =
    {
        new("All", "Every one of these must be matched."),
        new("Any", "At least one of these must be matched. Ignored when empty."),
        new("None", "None of these may be matched.")
    };

    static readonly string[] CLAUSE_PROPERTIES = { "all", "any", "none" };

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        EditorGUI.BeginProperty(position, label, property);

        var headerRect = new Rect(position.x, position.y, position.width,
            EditorGUIUtility.singleLineHeight);

        property.isExpanded = EditorGUI.Foldout(headerRect, property.isExpanded, label, true);

        if (!property.isExpanded)
        {
            var summaryRect = EditorGUI.PrefixLabel(headerRect, GUIContent.none);
            GUI.Label(summaryRect, Summarise(property), EditorStyles.miniLabel);
            EditorGUI.EndProperty();
            return;
        }

        using (new EditorGUI.IndentLevelScope())
        {
            var y = headerRect.yMax + CLAUSE_SPACING;
            for (var i = 0; i < CLAUSE_PROPERTIES.Length; i++)
            {
                var clause = property.FindPropertyRelative(CLAUSE_PROPERTIES[i]);
                var height = EditorGUI.GetPropertyHeight(clause, CLAUSE_LABELS[i], true);

                EditorGUI.PropertyField(new Rect(position.x, y, position.width, height), clause,
                    CLAUSE_LABELS[i], true);

                y += height + CLAUSE_SPACING;
            }
        }

        EditorGUI.EndProperty();
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        var height = EditorGUIUtility.singleLineHeight;
        if (!property.isExpanded) return height;

        for (var i = 0; i < CLAUSE_PROPERTIES.Length; i++)
        {
            height += CLAUSE_SPACING +
                      EditorGUI.GetPropertyHeight(property.FindPropertyRelative(CLAUSE_PROPERTIES[i]),
                          CLAUSE_LABELS[i], true);
        }

        return height;
    }

    /// <summary>
    /// Reads the summary off the serialized properties rather than the boxed object, so it is
    /// correct mid-edit and on a multi-object selection where the object itself is not available.
    /// </summary>
    static string Summarise(SerializedProperty property)
    {
        var text = new StringBuilder();

        for (var i = 0; i < CLAUSE_PROPERTIES.Length; i++)
        {
            var items = ItemsOf(property.FindPropertyRelative(CLAUSE_PROPERTIES[i]));
            if (items == null || items.arraySize == 0) continue;

            if (text.Length > 0) text.Append("  ·  ");
            text.Append(CLAUSE_LABELS[i].text.ToLowerInvariant()).Append(": ");

            for (var element = 0; element < items.arraySize; element++)
            {
                if (element > 0) text.Append(", ");
                text.Append(NameOf(items.GetArrayElementAtIndex(element).objectReferenceValue));
            }
        }

        return text.Length == 0 ? "(any)" : text.ToString();
    }

    static SerializedProperty ItemsOf(SerializedProperty tagSet) =>
        tagSet?.FindPropertyRelative("tags")?.FindPropertyRelative("items");

    /// <summary>
    /// Falls back to the asset name because <see cref="GameplayTagSO.TagFullName"/> is rebuilt at
    /// load time from the root down, and is empty for a tag whose root has not been loaded.
    /// </summary>
    static string NameOf(Object reference)
    {
        if (reference is not GameplayTagSO tag) return "(none)";

        return string.IsNullOrEmpty(tag.TagFullName) ? tag.name : tag.TagFullName;
    }
}
}
