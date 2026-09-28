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

    /// <summary>What the tag means, in plain words, for people and for tools that apply it.</summary>
    [SerializeField, TextArea(2, 8)] internal string description;

    public GameplayTagSO ParentTag => parentTag;
    public List<GameplayTagSO> ChildTags => new(childTags);
    public string TagName => tagName;
    public string Description => description;

    string _tagFullName;

    /// <summary>The dotted path from the root, e.g. <c>Damage.Fire</c>.</summary>
    /// <remarks>
    /// Built by walking <em>up</em> the parent chain on first use, not pushed down from the root by
    /// <see cref="Awake"/>. Awake fires per object as each asset loads, before its siblings are
    /// necessarily resolved, so pushing down gave a name to whichever tags happened to be wired at
    /// that instant and left the rest empty - load-order roulette that showed up as most of a
    /// vocabulary rendering under its asset filename instead of its tag path.
    /// <para>
    /// Cached after the first walk, and cleared down the tree by <see cref="UpdateFullname"/> when a
    /// rename or reparent makes it stale.
    /// </para>
    /// </remarks>
    public string TagFullName
    {
        get
        {
            if (!string.IsNullOrEmpty(_tagFullName)) return _tagFullName;

            _tagFullName = BuildFullName();
            return _tagFullName;
        }
    }

    /// <summary>
    /// Walks to the root, with a depth cap so a tag that is somehow its own ancestor cannot hang the
    /// editor. <see cref="ValidateSelfParent"/> only catches the one-step case.
    /// </summary>
    string BuildFullName()
    {
        const int maxDepth = 64;

        var name = tagName;
        var ancestor = parentTag;

        for (var depth = 0; ancestor && depth < maxDepth; depth++)
        {
            name = ancestor.tagName + "." + name;
            ancestor = ancestor.parentTag;
        }

        return name;
    }

    public static GameplayTagSO Default { get; private set; }

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

    /// <summary>This tag and every tag below it, skipping children whose asset is gone.</summary>
    /// <remarks>
    /// A deleted tag leaves a missing reference behind in its parent's list. Yielding one hands the
    /// caller an object whose every native member - <c>name</c> included - throws
    /// <c>MissingReferenceException</c>, so the check belongs here rather than at each call site.
    /// </remarks>
    public IEnumerator<GameplayTagSO> GetEnumerator()
    {
        yield return this;
        foreach (var child in childTags)
        {
            if (!child) continue;

            foreach (var descendant in child)
            {
                yield return descendant;
            }
        }
    }

    /// <summary>
    /// Invalidates this tag's cached full name and every name below it, after a rename or reparent.
    /// </summary>
    /// <remarks>
    /// Clears rather than recomputes: <see cref="TagFullName"/> rebuilds on demand, so a subtree
    /// nobody asks about costs nothing, and a child that is not loaded yet cannot be skipped.
    /// </remarks>
    public void UpdateFullname()
    {
        _tagFullName = null;

        foreach (var child in childTags)
        {
            if (child)
                child.UpdateFullname();
        }
    }

#if UNITY_EDITOR
    public static System.Action OnTagsChangedEditorCallback;

    /// <summary>
    /// Drops references to child assets that no longer exist, in memory.
    /// </summary>
    /// <remarks>
    /// Needed because <see cref="ChildTags"/> hands out a copy, so the obvious
    /// <c>tag.ChildTags.RemoveAll(...)</c> mutates nothing and every reload rediscovers the same
    /// dead entries. Not marked dirty: this repairs what is loaded, and the asset is rewritten
    /// whenever the tag is actually edited.
    /// </remarks>
    public void RemoveMissingChildren() => childTags.RemoveAll(child => !child);

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