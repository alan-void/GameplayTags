using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace GameplayTags
{   
    public class GameplayTagConfig : ScriptableSingleton<GameplayTagConfig>
    {
        
        const string TAG_DIRECTORY = "GameplayTags";
        const string TAG_RESOURCE_DIRECTORY = "Assets/Resources/GameplayTags";
        public string TagDirectory => TAG_RESOURCE_DIRECTORY;
        
        private List<GameplayTagSO> rootTags;
        
        public List<GameplayTagSO> RootTags => new (rootTags);

        public IEnumerable<GameplayTagSO> GetAllTags()
        {
            foreach (var rootTag in rootTags)
            {
                foreach (var tag in rootTag)
                {
                    yield return tag;
                }
            }
        }
        private void OnEnable()
        {
            FetchTags();
        }
        
        private void OnValidate()
        {
            FetchTags();
        }

        private void FetchTags()
        {
            var allTags = Resources.LoadAll<GameplayTagSO>(
                "GameplayTags").ToList();
            rootTags = new List<GameplayTagSO>();
            foreach (var tag in allTags)
            {
                tag.childTags.RemoveAll(t => !t);
                if (!tag.ParentTag)
                    rootTags.Add(tag);
            }
        }

        public void ReloadAndValidateTags()
        {
            FetchTags();
            //TODO: validate tags
        }

        public GameplayTagSO AddTag(string fullTagName)
        {
            // create and add tag from the '.' separated string format
            var tagParts = fullTagName.Split('.');
            foreach (var part in tagParts)
            {
                if (part == "")
                {
                    Debug.LogAssertion("Tag name cannot be empty.");
                    return null;
                }
                //part should only contain alphabets
                if (!System.Text.RegularExpressions.Regex.IsMatch(part, @"^[a-zA-Z0-9]+$"))
                {
                    Debug.LogAssertion($"Tag name '{part}' can only contain alphabets.");
                    return null;
                }
            }
            GameplayTagSO currentTag = rootTags.Find(t => t.TagName == tagParts[0]);
            if (!currentTag)
            {
                currentTag = CreateAndSaveTagAsset(tagParts[0], null);
                rootTags.Add(currentTag);
            }

            for (int i = 1; i < tagParts.Length; i++)
            {
                var partTag = currentTag.childTags.Find(t => 
                    t.TagName == tagParts[i]);
                if (!partTag)
                {
                    partTag = CreateAndSaveTagAsset(tagParts[i], currentTag);
                }
                currentTag = partTag;
            }
            AssetDatabase.SaveAssets();
            return currentTag;
        }

        GameplayTagSO CreateAndSaveTagAsset(string tagName, GameplayTagSO parentTag)
        {
            var newTag = CreateInstance<GameplayTagSO>();
            newTag.UpdateName(tagName);
            newTag.SetParent(parentTag);
            Directory.CreateDirectory(TAG_RESOURCE_DIRECTORY);
            var assetPath = $"{TAG_RESOURCE_DIRECTORY}/{newTag.TagFullName}_tag.asset";
            AssetDatabase.CreateAsset(newTag, assetPath);
            EditorUtility.SetDirty(newTag);
            if(parentTag)
                EditorUtility.SetDirty(parentTag);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            return AssetDatabase.LoadAssetAtPath<GameplayTagSO>(assetPath);
        }

        public void DeleteTag(GameplayTagSO tag)
        {
            tag.SetParent(null);
            var children = new List<GameplayTagSO>(tag.childTags);
            foreach (var child in children)
            {
                DeleteTag(child);
            }
            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(tag));
            DestroyImmediate(tag);
            AssetDatabase.SaveAssets();
        }
        
        public void RenameTag(GameplayTagSO tag, string newName)
        {
            if (string.IsNullOrEmpty(newName) || newName.Contains(' '))
            {
                Debug.LogAssertion("New tag name cannot be empty.");
                return;
            }

            // Check if the new name already exists
            var siblings = rootTags;
            if (tag.ParentTag) siblings = tag.ParentTag.childTags;
            
            foreach (var sibling in siblings)
            {
                if(sibling == tag) continue; // Skip the tag itself
                if (sibling.TagName == newName)
                {
                    Debug.LogAssertion($"Tag '{newName}' already exists in the same parent.");
                    return;
                }
            }
        
            // Update the tag name
            tag.UpdateName(newName);
            EditorUtility.SetDirty(tag);
            AssetDatabase.SaveAssets();
            RenameTagAssets(tag);
        }

        void RenameTagAssets(GameplayTagSO tag)
        {
            AssetEditorUtils.RenameAsset(tag, $"{tag.TagFullName}_tag");
            AssetDatabase.SaveAssets();
            foreach (var child in tag.childTags)
                RenameTagAssets(child);
        }

        public GameplayTagSO GetTag(string tagFullName)
        {
            if (tagFullName == "")
            {
                return null;
            }

            var tagParts = tagFullName.Split('.');
            GameplayTagSO currentTag = rootTags.Find(t => t.TagName == tagParts[0]);
            if (currentTag == null)
                return null;
            for (int i = 1; i < tagParts.Length; i++)
            {
                var partTag = currentTag.childTags.Find(t =>
                    t.TagName == tagParts[i]);
                if (partTag == null)
                {
                    return null; // Tag not found
                }

                currentTag = partTag;
            }

            return currentTag;
        }
    }
}