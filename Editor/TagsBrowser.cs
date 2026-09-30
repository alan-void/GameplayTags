using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEditor.UIElements;

namespace GameplayTags.Editor
{
public class TagsBrowser : EditorWindow
{
    private TreeView _treeView;
    private ToolbarSearchField _searchField;
    private TextField _newTagField;

    [MenuItem("Window/GameplayTags/Tags Browser %#T")]
    public static void ShowWindow()
    {
        var window = GetWindow<TagsBrowser>("Tags Browser");
        window.Show();
    }

    private void OnEnable()
    {
        BuildUI();
    }

    private void OnFocus()
    {
        RefreshTree();
    }

    private void BuildUI()
    {
        rootVisualElement.Clear();

        // Top Toolbar for Add Tag
        var addContainer = new VisualElement
        {
            style =
            {
                flexDirection = FlexDirection.Row, paddingBottom = 5, paddingTop = 5, paddingLeft = 5, paddingRight = 5
            }
        };
        _newTagField = new TextField("New Tag:") { style = { flexGrow = 1 } };
        var addButton = new Button(TryAddNewTag) { text = "Add new Tag" };
        addContainer.Add(_newTagField);
        addContainer.Add(addButton);
        rootVisualElement.Add(addContainer);

        // Search Toolbar
        var searchContainer = new Toolbar
            { style = { paddingBottom = 5, paddingTop = 5, paddingLeft = 5, paddingRight = 5 } };
        _searchField = new ToolbarSearchField { style = { flexGrow = 1 } };
        _searchField.RegisterValueChangedCallback(evt => OnSearchTextChanged(evt.newValue));
        searchContainer.Add(_searchField);
        rootVisualElement.Add(searchContainer);

        // TreeView
        _treeView = new TreeView
        {
            style = { flexGrow = 1 },
            makeItem = MakeTreeItem,
            bindItem = BindTreeItem
        };

        _treeView.itemsChosen += OnItemsChosen;

        rootVisualElement.Add(_treeView);

        RefreshTree();
    }

    private void OnSearchTextChanged(string searchText)
    {
        RefreshTree(searchText);
    }

    private void RefreshTree(string searchFilter = "")
    {
        if (_treeView == null) return;

        var allTags = GameplayTagConfig.instance.RootTags;
        var roots = new List<TreeViewItemData<GameplayTagSO>>();

        int idCounter = 1;

        foreach (var tag in allTags)
        {
            if (tag == null) continue;
            var node = BuildNode(tag, ref idCounter, searchFilter);
            if (node.HasValue)
            {
                roots.Add(node.Value);
            }
        }

        _treeView.SetRootItems(roots);
        _treeView.Rebuild();
    }

    private TreeViewItemData<GameplayTagSO>? BuildNode(GameplayTagSO tag, ref int idCounter, string searchFilter)
    {
        var childrenData = new List<TreeViewItemData<GameplayTagSO>>();
        bool matchesSearch = string.IsNullOrEmpty(searchFilter) ||
                             tag.TagName.IndexOf(searchFilter, StringComparison.OrdinalIgnoreCase) >= 0;

        foreach (var child in tag.ChildTags)
        {
            if (child == null) continue;
            var childNode = BuildNode(child, ref idCounter, searchFilter);
            if (childNode.HasValue)
            {
                childrenData.Add(childNode.Value);
            }
        }

        // If it doesn't match search and has no matching children, cull it
        if (!matchesSearch && childrenData.Count == 0)
            return null;

        int currentId = idCounter++;
        return new TreeViewItemData<GameplayTagSO>(currentId, tag, childrenData);
    }

    private VisualElement MakeTreeItem()
    {
        var container = new VisualElement { style = { flexDirection = FlexDirection.Row, flexGrow = 1 } };
        var label = new Label { style = { flexGrow = 1, unityTextAlign = TextAnchor.MiddleLeft } };
        var plusButton = new Button { text = "+", style = { width = 24 } };

        container.Add(label);
        container.Add(plusButton);

        // Context menu for Rename / Delete
        container.AddManipulator(new ContextualMenuManipulator(evt =>
        {
            var tag = container.userData as GameplayTagSO;
            if (tag == null) return;

            evt.menu.AppendAction("Rename", _ => BeginRename(tag));
            evt.menu.AppendAction("Delete", _ => DeleteTag(tag));
        }));

        return container;
    }

    private void BindTreeItem(VisualElement element, int index)
    {
        var tag = _treeView.GetItemDataForIndex<GameplayTagSO>(index);
        if (tag == null) return;

        element.userData = tag;
        element.tooltip = tag.Description ?? string.Empty;

        var label = element.Q<Label>();
        label.text = tag.TagName;

        var plusButton = element.Q<Button>();
        plusButton.clicked -= plusButton.userData as Action; // Unsubscribe old

        Action onClick = () =>
        {
            var newTagFullName = tag.TagFullName + $".NewTag{tag.ChildTags.Count + 1}";
            GameplayTagConfig.instance.AddTag(newTagFullName);
            AssetDatabase.SaveAssets();
            GameplayTagConfig.instance.ReloadAndValidateTags();
            RefreshTree(_searchField.value);
        };

        plusButton.clicked += onClick;
        plusButton.userData = onClick;

        // Allow double click on label to select in project window
        label.UnregisterCallback<MouseDownEvent>(OnLabelMouseDown);
        label.RegisterCallback<MouseDownEvent>(OnLabelMouseDown);
    }

    private void OnLabelMouseDown(MouseDownEvent evt)
    {
        if (evt.clickCount == 2)
        {
            var label = evt.currentTarget as Label;
            var tag = label?.parent?.userData as GameplayTagSO;
            if (tag != null)
            {
                Selection.activeObject = tag;
                EditorGUIUtility.PingObject(tag);
            }
        }
    }

    private void OnItemsChosen(IEnumerable<object> items)
    {
        foreach (var item in items)
        {
            var tag = item as GameplayTagSO;
            if (tag != null)
            {
                Selection.activeObject = tag;
            }
        }
    }

    private void BeginRename(GameplayTagSO tag)
    {
        RenameTagWindow.ShowDialog(tag, newName =>
        {
            GameplayTagConfig.instance.RenameTag(tag, newName);
            AssetDatabase.Refresh();
            RefreshTree(_searchField.value);
        });
    }

    private void DeleteTag(GameplayTagSO tag)
    {
        if (EditorUtility.DisplayDialog("Delete Tag", $"Are you sure you want to delete {tag.TagFullName}?", "Delete",
                "Cancel"))
        {
            GameplayTagConfig.instance.DeleteTag(tag);
            RefreshTree(_searchField.value);
        }
    }

    private void TryAddNewTag()
    {
        if (string.IsNullOrWhiteSpace(_newTagField.value)) return;
        GameplayTagConfig.instance.AddTag(_newTagField.value);
        _newTagField.value = "";
        RefreshTree(_searchField.value);
    }
}

public class RenameTagWindow : EditorWindow
{
    private GameplayTagSO _tag;
    private Action<string> _onRename;
    private TextField _textField;

    public static void ShowDialog(GameplayTagSO tag, Action<string> onRename)
    {
        var window = GetWindow<RenameTagWindow>("Rename Tag", true);
        window._tag = tag;
        window._onRename = onRename;
        window.minSize = window.maxSize = new Vector2(300, 80);
        window.ShowUtility();
    }

    private void OnEnable()
    {
        var root = rootVisualElement;
        root.style.paddingBottom = 10;
        root.style.paddingTop = 10;
        root.style.paddingLeft = 10;
        root.style.paddingRight = 10;

        _textField = new TextField("New Name:");
        root.Add(_textField);

        var okButton = new Button(OnOk) { text = "Rename", style = { marginTop = 10 } };
        root.Add(okButton);
    }

    private void OnGUI()
    {
        // Populate value on first frame if not set
        if (_tag != null && string.IsNullOrEmpty(_textField.value))
        {
            _textField.value = _tag.TagName;
        }

        // Handle enter key
        if (Event.current.type == EventType.KeyDown && Event.current.keyCode == KeyCode.Return)
        {
            OnOk();
            Event.current.Use();
        }
    }

    private void OnOk()
    {
        var newName = _textField.value;
        if (!string.IsNullOrWhiteSpace(newName) && newName != _tag?.TagName)
        {
            _onRename?.Invoke(newName);
        }

        Close();
    }
}
}