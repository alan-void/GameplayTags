using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace GameplayTags
{
[FilePath("Settings/tags/TagConfig.json", FilePathAttribute.Location.ProjectFolder)]
public class GameplayTagConfig : ScriptableSingleton<GameplayTagConfig>
{
    // [SerializeField] private List<GameplayTag> rootTags;
    const string ROOT_TAG_PATH = "Assets/Resources/Tags/root.asset";
    public GameplayTag rootTag
    {
        get
        {
            var root = AssetDatabase.LoadAssetAtPath<GameplayTag>(ROOT_TAG_PATH);
            if (!root)
            {
                var newTag = CreateInstance<GameplayTag>();
                AssetDatabase.CreateAsset(newTag, ROOT_TAG_PATH);
                AssetDatabase.SaveAssets();
                root = AssetDatabase.LoadAssetAtPath<GameplayTag>(ROOT_TAG_PATH);
            }
            return root;
        }
    }

    public void AddTag(string tag)
    {
        // create and add tag from the . separated string format
        var tagParts = tag.Split('.');
        GameplayTag currentTag = rootTag;
        foreach (var part in tagParts)
        {
            if (part == "")
            {
                Debug.LogAssertion("Tag name cannot be empty.");
                return;
            }
            //part should only contain alphabets
            if (!System.Text.RegularExpressions.Regex.IsMatch(part, @"^[a-zA-Z]+$"))
            {
                Debug.LogAssertion($"Tag name '{part}' can only contain alphabets.");
                return;
            }
        }
        foreach (var part in tagParts)
        {
            var partTag = currentTag.childTags.Find(t => t.TagName == part);
            if (partTag == null)
            {
                // partTag = new GameplayTag(part, currentTag);
                partTag = CreateInstance<GameplayTag>();
                partTag.UpdateName(part);
                partTag.SetParent(currentTag);
                EditorUtility.SetDirty(currentTag);
                var assetPath = $"Assets/Resources/Tags/{partTag.TagFullName}_tag.asset";
                AssetDatabase.CreateAsset(partTag, assetPath);
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
                partTag = AssetDatabase.LoadAssetAtPath<GameplayTag>(assetPath);
            }
            currentTag = partTag;
        }
        AssetDatabase.SaveAssets();
        Save(true);
    }

    public void RemoveTag(GameplayTag tag)
    {
        tag.SetParent(null);
        var children = new List<GameplayTag>(tag.childTags);
        foreach (var child in children)
        {
            RemoveTag(child);
        }
        AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(tag));
        DestroyImmediate(tag);
        Save(true);
        AssetDatabase.SaveAssets();
    }
    
    public void RenameTag(GameplayTag tag, string newName)
    {
        if (string.IsNullOrEmpty(newName) || newName.Contains(' '))
        {
            Debug.LogAssertion("New tag name cannot be empty.");
            return;
        }

        // Check if the new name already exists
        foreach (var sibling in tag.parentTag.childTags)
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
        Save(true);
        AssetDatabase.SaveAssets();
    }
    
    

    public GameplayTag GetTag(string tagFullName)
    {
        if(tagFullName=="")
        {
            return null;
        }
        var tagParts = tagFullName.Split('.');
        GameplayTag currentTag = rootTag;
        foreach (var part in tagParts)
        {
            var partTag = currentTag.childTags.Find(t => t.TagName == part);
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