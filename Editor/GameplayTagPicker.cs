using System;
using UnityEditor;
using UnityEngine;

namespace GameplayTags.Editor
{
/// <summary>
/// Opens the searchable tag dropdown from anywhere, not only from a serialized field's drawer.
/// </summary>
/// <remarks>
/// The same list and the same fuzzy search <see cref="GameplayTagDrawer"/> uses, so a tag is picked
/// the same way wherever you are.
/// </remarks>
public static class GameplayTagPicker
{
    /// <summary>Shows the picker under <paramref name="anchor"/> and reports the chosen tag.</summary>
    public static void Show(Rect anchor, Action<GameplayTagSO> onPicked)
    {
        if (onPicked == null) return;

        new GameplayTagDropdown(onPicked).Show(new Rect(anchor.x, anchor.yMax, anchor.width, 0f));
    }

    /// <summary>
    /// Shows the picker with a trailing entry that creates a tag from a dotted name, so a vocabulary
    /// can be extended without leaving for the Tags Browser.
    /// </summary>
    public static void ShowWithCreate(Rect anchor, Action<GameplayTagSO> onPicked)
    {
        if (onPicked == null) return;

        var menu = new GenericMenu();
        foreach (var tag in GameplayTagConfig.instance.GetAllTags())
        {
            if (!tag) continue;

            var captured = tag;
            menu.AddItem(new GUIContent(NameOf(tag)), false, () => onPicked(captured));
        }

        if (menu.GetItemCount() > 0) menu.AddSeparator(string.Empty);

        menu.AddItem(new GUIContent("New Tag…"), false,
            () => NewTagWindow.ShowDialog(name =>
            {
                var created = GameplayTagConfig.instance.AddTag(name);
                if (created != null) onPicked(created);
            }));

        menu.DropDown(anchor);
    }

    /// <summary>
    /// Falls back to the asset name: <see cref="GameplayTagSO.TagFullName"/> is rebuilt at load time
    /// from the root down and is empty for a tag whose root has not been loaded.
    /// </summary>
    static string NameOf(GameplayTagSO tag)
    {
        if (!tag) return "(missing)";

        return string.IsNullOrEmpty(tag.TagFullName) ? tag.name : tag.TagFullName;
    }
}

/// <summary>Asks for a dotted tag name. Validation lives in <see cref="GameplayTagConfig.AddTag"/>.</summary>
public class NewTagWindow : EditorWindow
{
    string _name = string.Empty;
    Action<string> _onSubmit;

    public static void ShowDialog(Action<string> onSubmit)
    {
        var window = CreateInstance<NewTagWindow>();
        window.titleContent = new GUIContent("New Tag");
        window._onSubmit = onSubmit;
        window.position = new Rect(Screen.width / 2f, Screen.height / 2f, 320f, 82f);
        window.ShowUtility();
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Dotted name, e.g. action.walk", EditorStyles.miniLabel);

        GUI.SetNextControlName("NewTagName");
        _name = EditorGUILayout.TextField(_name);
        GUI.FocusControl("NewTagName");

        var submitted = Event.current.type == EventType.KeyDown &&
                        Event.current.keyCode is KeyCode.Return or KeyCode.KeypadEnter;

        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("Cancel")) Close();

            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(_name)))
            {
                if (!GUILayout.Button("Create") && !submitted) return;

                _onSubmit?.Invoke(_name.Trim());
                Close();
            }
        }
    }
}
}
