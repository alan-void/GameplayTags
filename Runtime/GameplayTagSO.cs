using System.Collections.Generic;
#if UNITY_EDITOR
using UnityEditor;
#endif
using UnityEngine;

namespace GameplayTags
{
[CreateAssetMenu(fileName = "TagSO", menuName = "GameplayTags/Tag")]
public class GameplayTagSO : ScriptableObject
{
    [SerializeField] internal string tagName;
    [SerializeField] internal GameplayTagSO parentTag;
    [SerializeField] internal List<GameplayTagSO> childTags = new();
    public GameplayTagSO ParentTag => parentTag;
    public List<GameplayTagSO> ChildTags => new(childTags);
    public string TagName => tagName;

    string _tagFullName;
    public string TagFullName => _tagFullName;

    public static GameplayTagSO Default { get; private set; }

    private void Awake()
    {
        if (!parentTag)
            UpdateFullname();
    }

    /// <summary>
    /// returns true if this tag is a child of the other tag.
    /// a tag is considered a child of itself.
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool IsChildOf(GameplayTagSO other)
    {
        GameplayTagSO current = this;
        while (current != null)
        {
            if (current == other)
                return true;
            current = current.parentTag;
        }

        return false;
    }

    /// <summary>
    /// returns true if this tag is a parent of the other tag.
    /// a tag is considered a parent of itself.
    /// </summary>
    /// <param name="other"></param>
    /// <returns></returns>
    public bool IsParentOf(GameplayTagSO other)
    {
        return other.IsChildOf(this);
    }

    public IEnumerator<GameplayTagSO> GetEnumerator()
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

    public void UpdateFullname()
    {
        if (parentTag)
            _tagFullName = parentTag.TagFullName + "." + tagName;
        else
            _tagFullName = tagName;
        foreach (var child in childTags)
            child.UpdateFullname();
    }

#if UNITY_EDITOR
    public static System.Action OnTagsChangedEditorCallback;

    private void OnValidate()
    {
        OnTagsChangedEditorCallback?.Invoke();
    }

    public void UpdateName(string newName)
    {
        tagName = newName;
        UpdateFullname();
    }

    public void SetParent(GameplayTagSO newParent)
    {
        if (parentTag)
        {
            parentTag.childTags.Remove(this);
        }

        parentTag = newParent;
        if (newParent)
        {
            newParent.childTags.Add(this);
        }

        UpdateFullname();
    }


    private void ValidateSelfParent()
    {
        if (parentTag != this) return;
        Debug.LogError("Tag cannot be its own parent", this);
        parentTag = null;
    }

    private void OnDestroy()
    {
        OnTagsChangedEditorCallback?.Invoke();
    }
#endif
}
}