using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using spatial;
using UnityEditor;
using UnityEngine;
using UnityEngine.Serialization;

namespace GameplayTags
{
[Serializable]
public class GameplayTagSet : IEnumerable<GameplayTag>
{ 
    [SerializeField] private SerializableHashSet<GameplayTag> tags = new();

    
    public GameplayTagSet() { }

    public GameplayTagSet(IEnumerable<GameplayTag> initialTags)
    {
        tags = new SerializableHashSet<GameplayTag>(initialTags);
    }

    public bool AddTag(GameplayTag tag)
    {
        return tags.Add(tag);
    }

    public bool RemoveTag(GameplayTag tag)
    {
        return tags.Remove(tag);
    }
    
    public GameplayTagSet Difference(GameplayTagSet other)
    {
        return new GameplayTagSet(tags.Except(other.tags));
    }

    public bool HasTag(GameplayTag tag)
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

    public bool HasParentOf(GameplayTag tag)
    {
        return tags.Any(tag.IsChildOf);
    }

    public bool AreAllParentOf(GameplayTag tag)
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
    
    public bool HasChildOf(GameplayTag tag)
    {
        return tags.Any(t => t.IsChildOf(tag));
    }
    
    public bool AreAllChildOf(GameplayTag tag)
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

    public IEnumerable<GameplayTag> GetAllTags()
    {
        return tags;
    }

    public override string ToString()
    {
        return $"[{string.Join(", ", tags.Select(t => t.TagName))}]";
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }

    public IEnumerator<GameplayTag> GetEnumerator()
    {
        return tags.GetEnumerator();
    }

}

#if UNITY_EDITOR
[CustomPropertyDrawer(typeof(GameplayTagSet))]
public class GameplayTagSetDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        SerializedProperty tagsProp = property.FindPropertyRelative("tags");
        SerializedProperty valuesProp = tagsProp.FindPropertyRelative("values");
        EditorGUI.PropertyField(position, valuesProp, label, true);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        SerializedProperty tagsProp = property.FindPropertyRelative("tags");
        SerializedProperty valuesProp = tagsProp.FindPropertyRelative("values");
        return EditorGUI.GetPropertyHeight(valuesProp, label, true);
    }
}
#endif
}
