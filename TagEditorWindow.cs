using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using GameplayTags;

public class TagEditorWindow : EditorWindow
{
    #region Constants
    private const string RENAME_CONTROL_NAME = "TagRenameField";
    private const float INDENT_WIDTH = 15f;
    private const float FOLDOUT_WIDTH = 16f;
    private const float LABEL_PADDING = 2f;
    #endregion

    #region Fields
    private string _newTag = "";
    private readonly Dictionary<GameplayTagInternal, bool> _foldouts = new();
    
    // Rename state
    private GameplayTagInternal _renamingTag = null;
    private string _renameBuffer = "";
    private bool _renameChanged = false;
    private bool _renameJustStarted = false;
    #endregion

    #region Unity Methods
    [MenuItem("Tools/Tag Editor")]
    public static void ShowWindow()
    {
        GetWindow<TagEditorWindow>("Tag Editor");
    }
    
    private void OnGUI()
    {
        DrawHeader();
        DrawTagHierarchy();
    }
    #endregion

    #region UI Drawing
    private void DrawHeader()
    {
        GUILayout.Label("Tag Management", EditorStyles.boldLabel);

        _newTag = EditorGUILayout.TextField("New Tag:", _newTag);

        if (GUILayout.Button("Add Tag"))
        {
            TryAddNewTag();
        }

        GUILayout.Space(10);
        GUILayout.Label("Tags:", EditorStyles.boldLabel);
    }

    private void DrawTagHierarchy()
    {
        if (GameplayTagConfig.instance?.rootTag != null)
        {
            DrawTagHierarchy(GameplayTagConfig.instance.rootTag);
        }
    }

    private void DrawTagHierarchy(GameplayTagInternal tag, int indent = 0)
    {
        if (!HasChildTags(tag)) return;

        foreach (var child in tag.childTags)
        {
            EnsureFoldoutExists(child);
            DrawTagLine(child, indent);
            
            if (_foldouts[child])
            {
                DrawTagHierarchy(child, indent + 1);
            }
        }
    }

    private void DrawTagLine(GameplayTagInternal tag, int indent)
    {
        var rects = CalculateTagLineRects(indent);
        
        DrawFoldout(tag, rects.foldout);
        
        if (IsRenaming(tag))
        {
            DrawRenameField(tag, rects.label);
        }
        else
        {
            DrawTagLabel(tag, rects.label);
        }
    }

    private void DrawFoldout(GameplayTagInternal tag, Rect foldoutRect)
    {
        _foldouts[tag] = EditorGUI.Foldout(foldoutRect, _foldouts[tag], GUIContent.none, true);
    }

    private void DrawTagLabel(GameplayTagInternal tag, Rect labelRect)
    {
        EditorGUI.LabelField(labelRect, tag.tagName);
        HandleLabelMouseEvents(tag, labelRect);
    }

    private void DrawRenameField(GameplayTagInternal tag, Rect textFieldRect)
    {
        HandleRenameKeyboardInput(tag);
        HandleRenameFocusLoss(tag);
        
        GUI.SetNextControlName(RENAME_CONTROL_NAME);
        EditorGUI.BeginChangeCheck();
        
        _renameBuffer = EditorGUI.TextField(textFieldRect, _renameBuffer);
        
        if (EditorGUI.EndChangeCheck())
        {
            _renameChanged = true;
        }

        HandleInitialRenameFocus();
    }
    #endregion

    #region Event Handling
    private void HandleLabelMouseEvents(GameplayTagInternal tag, Rect labelRect)
    {
        if (!labelRect.Contains(Event.current.mousePosition)) return;

        switch (Event.current.type)
        {
            case EventType.MouseDown when Event.current.clickCount == 2:
                StartRename(tag);
                Event.current.Use();
                Repaint();
                break;
                
            case EventType.ContextClick:
                ShowContextMenu(tag);
                Event.current.Use();
                break;
        }
    }

    private void HandleRenameKeyboardInput(GameplayTagInternal tag)
    {
        if (Event.current.type == EventType.KeyDown && 
            Event.current.keyCode == KeyCode.Return &&
            GUI.GetNameOfFocusedControl() == RENAME_CONTROL_NAME)
        {
            CommitRename(tag);
            Event.current.Use();
        }
    }

    private void HandleRenameFocusLoss(GameplayTagInternal tag)
    {
        if (IsRenaming(tag) &&
            GUI.GetNameOfFocusedControl() != RENAME_CONTROL_NAME &&
            _renameChanged)
        {
            CommitRename(tag);
            _renameChanged = false;
        }
    }

    private void HandleInitialRenameFocus()
    {
        if (_renameJustStarted)
        {
            EditorGUI.FocusTextInControl(RENAME_CONTROL_NAME);
            _renameJustStarted = false;
        }
    }
    #endregion

    #region Context Menu
    private void ShowContextMenu(GameplayTagInternal tag)
    {
        var menu = new GenericMenu();

        menu.AddItem(new GUIContent("Rename"), false, () => StartRename(tag));
        menu.AddItem(new GUIContent("Delete"), false, () => TryDeleteTag(tag));

        menu.ShowAsContext();
    }
    #endregion

    #region Tag Operations
    private void TryAddNewTag()
    {
        if (string.IsNullOrWhiteSpace(_newTag)) return;
        
        Debug.Log($"Adding tag {_newTag}");
        GameplayTagConfig.instance.AddTag(_newTag);
        _newTag = "";
    }

    private void TryDeleteTag(GameplayTagInternal tag)
    {
        if (!EditorUtility.DisplayDialog("Confirm Delete",
                $"Are you sure you want to delete the tag '{tag.tagName}'?",
                "Delete", "Cancel"))
        {
            return;
        }

        GameplayTagConfig.instance.RemoveTag(tag);
        CleanupTagReferences(tag);
        Repaint();
    }

    private void StartRename(GameplayTagInternal tag)
    {
        _renamingTag = tag;
        _renameBuffer = tag.tagName;
        _renameChanged = false;
        _renameJustStarted = true;
        GUI.FocusControl(null);
    }

    private void CommitRename(GameplayTagInternal tag)
    {
        if (ShouldCommitRename(tag))
        {
            Debug.Log($"Renaming tag {tag.tagName} to {_renameBuffer}");
            GameplayTagConfig.instance.RenameTag(tag, _renameBuffer);
        }

        ClearRenameState();
    }
    #endregion

    #region Helper Methods
    private bool HasChildTags(GameplayTagInternal tag)
    {
        return tag.childTags != null && tag.childTags.Count > 0;
    }

    private void EnsureFoldoutExists(GameplayTagInternal tag)
    {
        if (!_foldouts.ContainsKey(tag))
        {
            _foldouts[tag] = true;
        }
    }

    private bool IsRenaming(GameplayTagInternal tag)
    {
        return _renamingTag == tag;
    }

    private (Rect foldout, Rect label) CalculateTagLineRects(int indent)
    {
        var lineRect = GUILayoutUtility.GetRect(0, EditorGUIUtility.singleLineHeight, 
            GUILayout.ExpandWidth(true));
        
        float indentOffset = indent * INDENT_WIDTH;
        
        var foldoutRect = new Rect(
            lineRect.x + indentOffset, 
            lineRect.y, 
            FOLDOUT_WIDTH, 
            lineRect.height);
        
        var labelRect = new Rect(
            foldoutRect.xMax + LABEL_PADDING, 
            lineRect.y,
            lineRect.width - indentOffset - FOLDOUT_WIDTH - LABEL_PADDING, 
            lineRect.height);

        return (foldoutRect, labelRect);
    }

    private bool ShouldCommitRename(GameplayTagInternal tag)
    {
        return !string.IsNullOrWhiteSpace(_renameBuffer) && _renameBuffer != tag.tagName;
    }

    private void CleanupTagReferences(GameplayTagInternal tag)
    {
        _foldouts.Remove(tag);
        if (_renamingTag == tag)
        {
            ClearRenameState();
        }
    }

    private void ClearRenameState()
    {
        _renamingTag = null;
        _renameBuffer = "";
        _renameChanged = false;
        _renameJustStarted = false;
    }
    #endregion
}