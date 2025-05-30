using System;
using System.Collections.Generic;
using System.Linq;

namespace GameplayTags
{
[Serializable]
public class GameplayTagSet
{
    private HashSet<GameplayTagInternal> _tags = new();

    public GameplayTagSet() { }

    public GameplayTagSet(IEnumerable<GameplayTagInternal> initialTags)
    {
        _tags = new HashSet<GameplayTagInternal>(initialTags);
    }

    public void AddTag(GameplayTagInternal tag)
    {
        _tags.Add(tag);
    }

    public void RemoveTag(GameplayTagInternal tag)
    {
        _tags.Remove(tag);
    }
    
    public GameplayTagSet Difference(GameplayTagSet other)
    {
        return new GameplayTagSet(_tags.Except(other._tags));
    }

    public bool HasTag(GameplayTagInternal tag)
    {
        return _tags.Contains(tag);
    }
    
    public bool HasTagAny(GameplayTagSet other)
    {
        return other._tags.Any(HasTag);
    }
    
    public bool HasTagAll(GameplayTagSet other)
    {
        return other._tags.All(HasTag);
    }

    public bool HasParentOf(GameplayTagInternal tag)
    {
        return _tags.Any(tag.IsChildOf);
    }

    public bool AreAllParentOf(GameplayTagInternal tag)
    {
        return _tags.All(tag.IsChildOf);
    }

    public bool HasParentOfAny(GameplayTagSet other)
    {
        return other._tags.Any(HasParentOf);
    }
    
    public bool HasParentOfAll(GameplayTagSet other)
    {
        return other._tags.All(HasParentOf);
    }
    
    public bool HasChildOf(GameplayTagInternal tag)
    {
        return _tags.Any(t => t.IsChildOf(tag));
    }
    
    public bool AreAllChildOf(GameplayTagInternal tag)
    {
        return _tags.All(t => t.IsChildOf(tag));
    }
    
    public bool HasChildOfAny(GameplayTagSet other)
    {
        return other._tags.Any(HasChildOf);
    }

    public bool HasChildOfAll(GameplayTagSet other)
    {
        return other._tags.All(HasChildOf);
    }

    public IEnumerable<GameplayTagInternal> GetAllTags()
    {
        return _tags;
    }

    public override string ToString()
    {
        return $"[{string.Join(", ", _tags.Select(t => t.tagName))}]";
    }
}
}
