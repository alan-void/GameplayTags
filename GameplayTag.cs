using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEditor.Search;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Search;

namespace GameplayTags
{
/// <summary>
/// A ScriptableObject wrapper for GameplayTagInternal that provides editor integration
/// and automatic validation. This class maintains a reference to a tag in the central
/// tag configuration and updates automatically when tags are renamed or deleted.
/// Since this class needs a CustomPropertyDrawer it needs to be a UnityEngine.Object,
/// so we make it a child of ScriptableObject.
/// </summary>
[Serializable]
public class GameplayTag : ScriptableObject
{
private GameplayTagInternal _tag;
[SerializeField] private string _tagFullName = string.Empty;

/// <summary>
/// Gets or sets the full name of the gameplay tag.
/// Setting this property will validate the tag name and update the internal tag reference.
/// </summary>
public string TagFullName
{
    get => _tagFullName;
    set
    {
        if (string.Equals(_tagFullName, value, StringComparison.Ordinal))
            return;

        _tagFullName = value ?? string.Empty;
        ValidateFullName();
        
        #if UNITY_EDITOR
        MarkDirty();
        #endif
    }
}

/// <summary>
/// Gets the internal GameplayTagInternal reference. May be null if the tag doesn't exist.
/// </summary>
public GameplayTagInternal Tag => _tag;

/// <summary>
/// Returns true if this GameplayTag has a valid tag reference.
/// </summary>
public bool IsValid => _tag != null && !string.IsNullOrEmpty(_tagFullName);

/// <summary>
/// Returns true if this GameplayTag is empty (no tag assigned).
/// </summary>
public bool IsEmpty => string.IsNullOrEmpty(_tagFullName);

/// <summary>
/// Implicit conversion to GameplayTagInternal for easy usage.
/// </summary>
public static implicit operator GameplayTagInternal(GameplayTag tag)
{
    return tag?._tag;
}

/// <summary>
/// Implicit conversion to bool for null/validity checking.
/// </summary>
public static implicit operator bool(GameplayTag tag)
{
    return tag != null && tag.IsValid;
}

private void OnEnable()
{
    ValidateFullName();
}

private void OnValidate()
{
    ValidateFullName();
}

/// <summary>
/// Sets the tag reference based on the tagFullName.
/// Clears the tag name if a corresponding GameplayTagInternal does not exist.
/// </summary>
private void ValidateFullName()
{
    if (GameplayTagConfig.instance == null)
    {
        _tag = null;
        return;
    }

    var previousTag = _tag;
    _tag = GameplayTagConfig.instance.GetTag(_tagFullName);
    
    // If we have a tag name but couldn't find the tag, clear it and warn
    if (!string.IsNullOrEmpty(_tagFullName) && _tag == null)
    {
        #if UNITY_EDITOR
        Debug.LogWarning($"GameplayTag '{_tagFullName}' could not be found in the tag configuration. " +
                       $"The tag reference has been cleared.", this);
        #endif
        _tagFullName = string.Empty;
    }

    // Mark dirty if the tag reference changed
    #if UNITY_EDITOR
    if (previousTag != _tag)
    {
        MarkDirty();
    }
    #endif
}

/// <summary>
/// Called by the tag manager when a tag is deleted.
/// Clears this reference if it points to the deleted tag or any of its children.
/// </summary>
public void OnTagDeleted(GameplayTagInternal deletedTag)
{
    if (_tag == null || deletedTag == null) 
        return;

    if (_tag == deletedTag || _tag.IsChildOf(deletedTag))
    {
        #if UNITY_EDITOR
        Debug.Log($"GameplayTag reference to '{_tagFullName}' was cleared because the tag was deleted.", this);
        #endif
        
        _tag = null;
        _tagFullName = string.Empty;
        
        #if UNITY_EDITOR
        MarkDirty();
        #endif
    }
}

/// <summary>
/// Called by the tag manager when a tag is renamed.
/// Updates this reference if it points to the renamed tag or any of its children.
/// </summary>
public void OnTagRenamed(GameplayTagInternal renamedTag)
{
    if (_tag == null || renamedTag == null) 
        return;

    if (_tag == renamedTag || _tag.IsChildOf(renamedTag))
    {
        var oldName = _tagFullName;
        _tagFullName = _tag.GetFullName();
        
        #if UNITY_EDITOR
        Debug.Log($"GameplayTag reference updated from '{oldName}' to '{_tagFullName}' due to tag rename.", this);
        MarkDirty();
        #endif
        
        ValidateFullName();
    }
}

/// <summary>
/// Clear the tag reference.
/// </summary>
public void Clear()
{
    TagFullName = string.Empty;
}

#if UNITY_EDITOR
/// <summary>
/// Marks this object and its parent objects as dirty for proper serialization.
/// </summary>
private void MarkDirty()
{
    if (this != null)
    {
        EditorUtility.SetDirty(this);
    }
}
#endif

public override string ToString()
{
    return IsEmpty ? "<Empty>" : _tagFullName;
}

public override bool Equals(object obj)
{
    if (obj is GameplayTag other)
    {
        return string.Equals(_tagFullName, other._tagFullName, StringComparison.Ordinal);
    }
    return false;
}

// public override int GetHashCode()
// {
//     return _tagFullName?.GetHashCode() ?? 0;
// }
}


#if UNITY_EDITOR
/// <summary>
/// Popup window that shows filtered gameplay tag suggestions as the user types.
/// </summary>
public class GameplayTagDropdown : AdvancedDropdown
{
    private readonly GameplayTagDrawer _ownerDrawer;
    public GameplayTagDropdown(GameplayTagDrawer ownerDrawer) :
        base(new AdvancedDropdownState())
    {
        _ownerDrawer = ownerDrawer;
    }

    protected override AdvancedDropdownItem BuildRoot()
    {
        var root = new AdvancedDropdownItem("Tags");
        foreach (var tag in GameplayTagConfig.instance.rootTag)
        {
            root.AddChild(new AdvancedDropdownItem(tag.GetFullName()));
        }
        return root;
    }

    protected override void ItemSelected(AdvancedDropdownItem item)
    {
        _ownerDrawer.HandleTagSelection(item.name);
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
    public void HandleTagSelection(string tagName)
    {
        try
        {
            _currentProperty.serializedObject.Update();
            // Get or create the GameplayTag object
            var gameplayTag = (GameplayTag)_currentProperty.objectReferenceValue;
            if (gameplayTag == null)
            {
                gameplayTag = ScriptableObject.CreateInstance<GameplayTag>();
                _currentProperty.objectReferenceValue = gameplayTag;
            }
            
            // Record undo for the GameplayTag object
            Undo.RecordObject(gameplayTag, "Change Gameplay Tag Selection Value");
            
            // Handle empty input (clear the tag)
            if (string.IsNullOrEmpty(tagName))
            {
                _currentProperty.objectReferenceValue = null;
                _currentProperty.serializedObject.ApplyModifiedProperties();
                MarkSceneDirty(_currentProperty);
                return;
            }

            // Set the tag value using the property (which validates)
            gameplayTag.TagFullName = tagName;

            // Mark objects as dirty
            EditorUtility.SetDirty(gameplayTag);

            // Apply changes
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
            trackingHandler = OnTagTracked,
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
        HandleTagSelection(ReferenceEquals(searchItem, SearchItem.clear) ? "" : searchItem.id);
    }

    /// <summary>
    /// Handles tag tracking (preview) in the search window.
    /// </summary>
    private static void OnTagTracked(SearchItem searchItem)
    {
        // Optional: Add preview functionality here
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
        foreach (var tag in GameplayTagConfig.instance.rootTag)
        {
            var tagName = tag.GetFullName();
            long score = 0;
            
            if (string.IsNullOrEmpty(context.searchText) || 
                FuzzySearch.FuzzyMatch(context.searchText, tagName, ref score))
            {
                yield return provider.CreateItem(context, tagName, (int)score, 
                    tagName, $"Tag: {tagName}", null, null);
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