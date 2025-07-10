using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace GameplayTags
{
[Serializable]
public class GameplayTagSet : IEnumerable<GameplayTagSO>
{ 
    [SerializeField] private SerializableHashSet<GameplayTagSO> tags = new();

    public int Count => tags.Count;
    public static GameplayTagSet Empty { get; } = new();
    public GameplayTagSet() { }

    public GameplayTagSet(IEnumerable<GameplayTagSO> initialTags)
    {
        tags = new SerializableHashSet<GameplayTagSO>(initialTags);
    }
    
    public void Clear() => tags.Clear();
    public bool AddTag(GameplayTagSO tag)
    {
        return tags.Add(tag);
    }

    public void AddTagSet(GameplayTagSet otherSet)
    {
        tags.Value.UnionWith(otherSet.tags);
    }

    public bool RemoveTag(GameplayTagSO tag)
    {
        return tags.Remove(tag);
    }
    
    public GameplayTagSet Difference(GameplayTagSet other)
    {
        return new GameplayTagSet(tags.Except(other.tags));
    }

    public bool HasTag(GameplayTagSO tag)
    {
        return tags.Contains(tag);
    }
    
    public bool HasTagAny(GameplayTagSet other)
    {
        return other.tags.Any(HasTag);
    }
    
    public bool HasTagAll(GameplayTagSet other)
    {
        return other.tags.All(HasTag);
    }
    /// <summary>
    /// a tag is considered a parent of itself.
    /// </summary>
    /// <param name="tag"></param>
    /// <returns></returns>
    public bool HasParentOf(GameplayTagSO tag)
    {
        return tags.Any(tag.IsChildOf);
    }

    public bool AreAllParentOf(GameplayTagSO tag)
    {
        return tags.All(tag.IsChildOf);
    }

    public bool HasParentOfAny(GameplayTagSet other)
    {
        return other.tags.Any(HasParentOf);
    }
    
    public bool HasParentOfAll(GameplayTagSet other)
    {
        return other.tags.All(HasParentOf);
    }
    
    public bool HasChildOf(GameplayTagSO tag)
    {
        return tags.Any(t => t.IsChildOf(tag));
    }
    
    public bool AreAllChildOf(GameplayTagSO tag)
    {
        return tags.All(t => t.IsChildOf(tag));
    }
    
    public bool HasChildOfAny(GameplayTagSet other)
    {
        return other.tags.Any(HasChildOf);
    }

    public bool HasChildOfAll(GameplayTagSet other)
    {
        return other.tags.All(HasChildOf);
    }

    // public IEnumerable<GameplayTagSO> GetAllTags()
    // {
    //     return tags;
    // }

    public override string ToString()
    {
        return $"[{string.Join(", ", tags.Select(t => t.TagName))}]";
    }
    public IEnumerator<GameplayTagSO> GetEnumerator()
    {
        foreach (var tag in tags)
        {
            if(!tag) continue;
            yield return tag;
        }
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }


}
}
