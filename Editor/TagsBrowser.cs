using System.Collections.Generic;
using System.IO;
using System.Linq;
using H2V.ExtensionsCore.Editor.Helpers;
using UnityEditor;
using UnityEditor.IMGUI.Controls;
using UnityEngine;

namespace GameplayTags.Editor
{
    public class TagsBrowser : EditorWindow
    {
        private static TagsBrowser _window;

        private static TreeViewState _treeViewState;
        private static TagsTreeView _tagTreeView;
        private SearchField _searchField;

        private Vector2 _scrollPosition;
        private string _newTag = "";
        
        private void OnEnable()
        {
            CreateTreeView();
        }

        [MenuItem("Window/Gameplay Ability System/Tags Browser %#T")]
        public static void ShowWindow()
        {
            _window = GetWindow<TagsBrowser>($"Tags Browser");
            _tagTreeView.Reload();
            _window.Show();
        }

        private void OnGUI()
        {
            AddTagSection();
            EditorGUILayout.Space();
            TagsTreeSection();
            EditorGUILayout.Space();
        }

        private void TagsTreeSection()
        {
            _searchField ??= new SearchField();

            var searchRect = EditorGUILayout.GetControlRect(false, GUILayout.ExpandWidth(true),
                GUILayout.Height(EditorGUIUtility.singleLineHeight));
            if (_tagTreeView != null)
            {
                _tagTreeView.searchString = _searchField.OnGUI(searchRect, _tagTreeView.searchString);
                _scrollPosition = EditorGUILayout.BeginScrollView(_scrollPosition);
                _tagTreeView.OnGUI(new Rect(0, 0, position.width, position.height - 80));
                EditorGUILayout.EndScrollView();
            }
        }

        private void AddTagSection()
        {
            EditorGUILayout.Space();
            _newTag = EditorGUILayout.TextField("New Tag:", _newTag);
            if (GUILayout.Button("Add new Tag"))
            {
                TryAddNewTag();
                _tagTreeView.Reload();
            }
        }
        private void TryAddNewTag()
        {
            if (string.IsNullOrWhiteSpace(_newTag)) return;
            GameplayTagConfig.instance.AddTag(_newTag);
            _newTag = "";
        }
        private T AddNewSO<T>(string directory, string name) where T : ScriptableObject
        {
            var newAsset = ScriptableObject.CreateInstance<T>();
            newAsset.name = name;
            if (!Directory.Exists(directory))
                Directory.CreateDirectory(directory);
            
            AssetDatabase.CreateAsset(newAsset, $"{directory}/{newAsset.name}.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return newAsset;
        }

        private void CreateTreeView()
        {
            _treeViewState ??= new TreeViewState ();
            _tagTreeView = new TagsTreeView(_treeViewState);
        }

        private void OnFocus()
        {
            CreateTreeView();
        }

        private void OnProjectChange()
        {
            CreateTreeView();
        }
    }
}