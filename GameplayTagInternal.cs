using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace GameplayTags
{
[Serializable]
public class GameplayTagInternal
{
    public string tagName;
    [SerializeReference] public GameplayTagInternal parentTag;
    [SerializeReference]
    public List<GameplayTagInternal> childTags = new();

    public GameplayTagInternal(string name, GameplayTagInternal parent = null)
    {
        tagName = name;
        parentTag = parent;
        if (parent != null)
        {
            parent.childTags.Add(this);
        }
    }

    public void SetParent(GameplayTagInternal newParent)
    {
        if (parentTag != null)
        {
            parentTag.childTags.Remove(this);
        }
        parentTag = newParent;
        if (newParent != null)
        {
            newParent.childTags.Add(this);
        }
    }

    public bool IsChildOf(GameplayTagInternal other)
    {
        GameplayTagInternal current = this;
        while (current != null)
        {
            if (current == other)
                return true;
            current = current.parentTag;
        }
        return false;
    }

    public bool IsParentOf(GameplayTagInternal other)
    {
        return other.IsChildOf(this);
    }
    
    public string GetFullName()
    {
        if (parentTag == null)
            return tagName;
        if (string.IsNullOrEmpty(parentTag.tagName))
            return tagName;
        return $"{parentTag.GetFullName()}.{tagName}";
    }
    
    public IEnumerator<GameplayTagInternal> GetEnumerator()
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
}
