using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Search;

namespace GameplayTags.Editor
{
/// <summary>
/// Popup window that shows filtered gameplay tag suggestions as the user types.
/// </summary>
/// <remarks>
/// Takes a callback rather than the drawer it was written for, so anything that needs to pick a tag
/// can open it - see <see cref="GameplayTagPicker"/>.
/// </remarks>
public class GameplayTagDropdown : AdvancedDropdown
{
    private readonly Action<GameplayTagSO> _onPicked;

    private Dictionary<AdvancedDropdownItem, GameplayTagSO> _dropdownTags = new();

    public GameplayTagDropdown(Action<GameplayTagSO> onPicked) :
        base(new AdvancedDropdownState())
    {
        _onPicked = onPicked;
    }

    protected override AdvancedDropdownItem BuildRoot()
    {
        var root = new AdvancedDropdownItem("Tags");
        foreach (var tag in GameplayTagConfig.instance.GetAllTags())
        {
            var item = new AdvancedDropdownItem(tag.TagFullName);
            _dropdownTags[item] = tag;
            root.AddChild(item);
        }

        return root;
    }

    protected override void ItemSelected(AdvancedDropdownItem item)
    {
        _onPicked?.Invoke(_dropdownTags[item]);
    }
}

/// <summary>
/// Custom property drawer for GameplayTag that provides a searchable tag selector with dropdown autocomplete.
/// </summary>
[CustomPropertyDrawer(typeof(GameplayTagSO))]
public class GameplayTagDrawer : PropertyDrawer
{
    private SerializedProperty _currentProperty;
    private const float BUTTON_WIDTH = 20f;
    private const string OBJECT_FIELD_BUTTON_STYLE = "ObjectFieldButton";
    private static PopupWindowContent _activePopup;

    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        _currentProperty = property;

        EditorGUI.BeginProperty(position, label, property);
        // EditorGUI.BeginChangeCheck();

        // Draw label
        position = EditorGUI.PrefixLabel(position, label);

        // Calculate rects
        var fieldRect = new Rect(position.x, position.y, position.width - BUTTON_WIDTH, position.height);
        var buttonRect = new Rect(fieldRect.xMax, position.y, BUTTON_WIDTH, position.height);

        // Get the GameplayTag object
        var gameplayTag = (GameplayTagSO)property.objectReferenceValue;

        if (GUI.Button(fieldRect, gameplayTag ? gameplayTag.TagFullName : "", EditorStyles.toolbarButton))
        {
            ShowDropdown(fieldRect);
        }

        // Draw the selector button
        if (GUI.Button(buttonRect, GUIContent.none, GUI.skin.FindStyle(OBJECT_FIELD_BUTTON_STYLE)))
        {
            ShowTagSelector(position);
        }

        EditorGUI.EndProperty();
    }

    private void ShowDropdown(Rect fieldRect)
    {
        var dropdownRect = new Rect(fieldRect.x, fieldRect.yMax, fieldRect.width, 0);
        (new GameplayTagDropdown(HandleTagSelection)).Show(dropdownRect);
    }

    /// <summary>
    /// Handles tag selection from dropdown or other validated sources.
    /// This uses the TagFullName property which validates the tag.
    /// </summary>
    public void HandleTagSelection(GameplayTagSO tag)
    {
        try
        {
            _currentProperty.serializedObject.Update();

            var gameplayTag = (GameplayTagSO)_currentProperty.objectReferenceValue;
            Undo.RecordObject(_currentProperty.serializedObject.targetObject,
                "Change Gameplay Tag Selection Value");

            _currentProperty.objectReferenceValue = tag;

            _currentProperty.serializedObject.ApplyModifiedProperties();
            MarkSceneDirty(_currentProperty);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Error setting gameplay tag from selection: {ex.Message}");
        }
    }


    /// <summary>
    /// Marks the scene as dirty if the target object is in a scene.
    /// </summary>
    private void MarkSceneDirty(SerializedProperty property)
    {
        if (!EditorUtility.IsPersistent(property.serializedObject.targetObject))
        {
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                SceneManager.GetActiveScene());
        }
    }

    /// <summary>
    /// Shows the tag selector popup.
    /// </summary>
    private void ShowTagSelector(Rect position)
    {
        var provider = CreateSearchProvider();
        var context = SearchService.CreateContext(provider);

        var viewState = new SearchViewState(context,
            SearchViewFlags.CompactView | SearchViewFlags.DisableSavedSearchQuery)
        {
            windowTitle = new GUIContent("Gameplay Tag Selector"),
            title = "Select Gameplay Tag",
            selectHandler = OnTagSelected,
            position = position,
            itemIconSize = 0
        };

        SearchService.ShowPicker(viewState);
    }

    /// <summary>
    /// Handles tag selection from the search window.
    /// </summary>
    private void OnTagSelected(SearchItem searchItem, bool canceled)
    {
        if (canceled || _currentProperty == null)
            return;
        HandleTagSelection(ReferenceEquals(searchItem, SearchItem.clear) ? null : (GameplayTagSO)searchItem.data);
    }

    /// <summary>
    /// Creates the search provider for gameplay tags.
    /// </summary>
    private static SearchProvider CreateSearchProvider()
    {
        return new SearchProvider("GameplayTag", "Gameplay Tag")
        {
            fetchItems = FetchTagItems,
            fetchDescription = (item, _) => item.id
        };
    }

    /// <summary>
    /// Fetches available gameplay tags for the search window.
    /// </summary>
    private static IEnumerable<SearchItem> FetchTagItems(SearchContext context, List<SearchItem> _,
        SearchProvider provider)
    {
        if (GameplayTagConfig.instance == null)
            yield break;

        // Add all available tags
        foreach (var tag in GameplayTagConfig.instance.GetAllTags())
        {
            var tagName = tag.TagFullName;
            long score = 0;

            if (string.IsNullOrEmpty(context.searchText) ||
                FuzzySearch.FuzzyMatch(context.searchText, tagName, ref score))
            {
                yield return provider.CreateItem(context, tagName, (int)score,
                    tagName, $"Tag: {tagName}", null, tag);
            }
        }
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUIUtility.singleLineHeight;
    }
}
}