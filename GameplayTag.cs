using System;
using System.Collections.Generic;
using Unity.Collections;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Search;

namespace GameplayTags
{
/// <summary>
/// A ScriptableObject wrapper for GameplayTag that provides editor integration
/// and automatic validation. This class maintains a reference to a tag in the central
/// tag configuration and updates automatically when tags are renamed or deleted.
/// Since this class needs a CustomPropertyDrawer it needs to be a UnityEngine.Object,
/// so we make it a child of ScriptableObject.
/// </summary>
[Serializable]
public class GameplayTag : ScriptableObject
{
    
    [SerializeField] string tagName;
    [SerializeReference] public GameplayTag parentTag;
    [SerializeField] public List<GameplayTag> childTags = new();

    private bool _dirty = true;
    string _tagFullName;
    public string TagName => tagName;

    public void UpdateName(string newName)
    {
        tagName = newName;
        _dirty = true;
    }

    public string TagFullName
    {
        get
        {
            if (!_dirty) return _tagFullName;
            if (!parentTag)
                _tagFullName = "";
            else
            {
                if(!parentTag.parentTag)
                    _tagFullName = tagName;
                else
                    _tagFullName = parentTag.TagFullName + "." + tagName;
            }
            return _tagFullName;
        }
    }

    public void SetParent(GameplayTag newParent)
    {
        if (parentTag)
        {
            parentTag.childTags.Remove(this);
        }
        ScriptableObjectUtil.ForceSave(parentTag);
        parentTag = newParent;
        if (newParent)
        {
            newParent.childTags.Add(this);
        }
        ScriptableObjectUtil.ForceSave(parentTag);
        _dirty = true;
    }

    public bool IsChildOf(GameplayTag other)
    {
        GameplayTag current = this;
        while (current != null)
        {
            if (current == other)
                return true;
            current = current.parentTag;
        }
        return false;
    }

    public bool IsParentOf(GameplayTag other)
    {
        return other.IsChildOf(this);
    }
    
    public IEnumerator<GameplayTag> GetEnumerator()
    {
        yield return this;
        foreach (var child in childTags)
        {
            foreach (var descendant in child)
            {
                yield return descendant;
            }
        }
    }
}


#if UNITY_EDITOR
/// <summary>
/// Popup window that shows filtered gameplay tag suggestions as the user types.
/// </summary>
public class GameplayTagDropdown : AdvancedDropdown
{
    private readonly GameplayTagDrawer _ownerDrawer;
    
    private Dictionary<AdvancedDropdownItem, GameplayTag> _dropdownTags = new();
    public GameplayTagDropdown(GameplayTagDrawer ownerDrawer) :
        base(new AdvancedDropdownState())
    {
        _ownerDrawer = ownerDrawer;
    }

    protected override AdvancedDropdownItem BuildRoot()
    {
        var root = new AdvancedDropdownItem("Tags");
        foreach (var tag in TagManager.RootTag)
        {
            var item = new AdvancedDropdownItem(tag.TagFullName);
            _dropdownTags[item] = tag;
            root.AddChild(item);
        }
        return root;
    }

    protected override void ItemSelected(AdvancedDropdownItem item)
    {
        _ownerDrawer.HandleTagSelection(_dropdownTags[item]);
    }
}
#endif

#if UNITY_EDITOR
/// <summary>
/// Custom property drawer for GameplayTag that provides a searchable tag selector with dropdown autocomplete.
/// </summary>
[CustomPropertyDrawer(typeof(GameplayTag))]
public class GameplayTagDrawer : PropertyDrawer
{
    private SerializedProperty _currentProperty;
    private const float BUTTON_WIDTH = 20f;
    private const string OBJECT_FIELD_BUTTON_STYLE = "ObjectFieldButton";
    private static GameplayTagDropdown _activeDropdown;
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
        var gameplayTag = (GameplayTag)property.objectReferenceValue;

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
        _activeDropdown = new GameplayTagDropdown(this);
        _activeDropdown.Show(dropdownRect);
    }

    /// <summary>
    /// Handles tag selection from dropdown or other validated sources.
    /// This uses the TagFullName property which validates the tag.
    /// </summary>
    public void HandleTagSelection(GameplayTag tag)
    {
        try
        {
            _currentProperty.serializedObject.Update();
            
            var gameplayTag = (GameplayTag)_currentProperty.objectReferenceValue;
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
            itemSize = 0
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
        HandleTagSelection(ReferenceEquals(searchItem, SearchItem.clear) ? null : (GameplayTag)searchItem.data);
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
    private static IEnumerable<SearchItem> FetchTagItems(SearchContext context, List<SearchItem> _, SearchProvider provider)
    {
        if (GameplayTagConfig.instance == null)
            yield break;

        // Add all available tags
        foreach (var tag in TagManager.RootTag)
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
#endif
}