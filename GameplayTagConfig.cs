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
    [SerializeReference]
    public GameplayTagInternal rootTag = new("");


    public void AddTag(string tag)
    {
        // create and add tag from the . separated string format
        var tagParts = tag.Split('.');
        GameplayTagInternal currentTag = rootTag;
        foreach (var part in tagParts)
        {
            var partTag = currentTag.childTags.Find(t => t.tagName == part);
            if (partTag == null)
            {
                partTag = new GameplayTagInternal(part, currentTag);
            }
            currentTag = partTag;
        }
        Save(true);
    }

    public void RemoveTag(GameplayTagInternal tag)
    {
        tag.SetParent(null);
        Save(true);

        foreach (var tagRef in FindObjectsByType<GameplayTag>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            tagRef.OnTagDeleted(tag);
        }
    }
    
    public void RenameTag(GameplayTagInternal tag, string newName)
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
            if (sibling.tagName == newName)
            {
                Debug.LogAssertion($"Tag '{newName}' already exists in the same parent.");
                return;
            }
        }

        // Update the tag name
        tag.tagName = newName;
        Save(true);
        
        foreach (var tagRef in FindObjectsByType<GameplayTag>(FindObjectsInactive.Include, FindObjectsSortMode.None))
        {
            tagRef.OnTagRenamed(tag);
        }
    }
    
    

    public GameplayTagInternal GetTag(string tagFullName)
    {
        if(tagFullName=="")
        {
            return null;
        }
        var tagParts = tagFullName.Split('.');
        GameplayTagInternal currentTag = rootTag;
        foreach (var part in tagParts)
        {
            var partTag = currentTag.childTags.Find(t => t.tagName == part);
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