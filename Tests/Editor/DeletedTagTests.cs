using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace GameplayTags.Tests
{
/// <summary>
/// What the tree does once one of its assets is deleted out from under it.
/// </summary>
/// <remarks>
/// A deleted tag leaves a missing reference in its parent's child list, and a missing
/// <c>UnityEngine.Object</c> is not null — it compares false to null but throws
/// <c>MissingReferenceException</c> from every native member, <c>name</c> included. So enumerating
/// the tree handed callers objects that exploded on use, and any picker built on
/// <c>GetAllTags</c> threw as soon as it tried to label one.
/// </remarks>
public class DeletedTagTests
{
    readonly List<GameplayTagSO> _created = new();

    GameplayTagSO Tag(string name, GameplayTagSO parent = null)
    {
        var tag = ScriptableObject.CreateInstance<GameplayTagSO>();
        tag.name = name;
        tag.UpdateName(name);
        if (parent)
            tag.SetParent(parent);
        _created.Add(tag);
        return tag;
    }

    [TearDown]
    public void TearDown()
    {
        foreach (var tag in _created)
        {
            if (tag)
                Object.DestroyImmediate(tag);
        }

        _created.Clear();
    }

    [Test]
    public void EnumerationSkipsAChildWhoseAssetIsGone()
    {
        var root = Tag("Damage");
        var fire = Tag("Fire", root);
        Tag("Ice", root);

        Object.DestroyImmediate(fire);

        var names = new List<string>();
        foreach (var tag in root) names.Add(tag.TagName);

        Assert.AreEqual(new[] { "Damage", "Ice" }, names);
    }

    [Test]
    public void EveryTagEnumerationYieldsIsUsable()
    {
        // The failure this guards: reading .name off a yielded tag threw, which is exactly what a
        // menu or dropdown does to label it.
        var root = Tag("Damage");
        Object.DestroyImmediate(Tag("Fire", root));

        foreach (var tag in root)
        {
            Assert.IsTrue(tag, "enumeration yielded a destroyed tag");
            Assert.DoesNotThrow(() =>
            {
                var _ = tag.name;
            });
        }
    }

    [Test]
    public void RemoveMissingChildrenActuallyMutatesTheList()
    {
        // ChildTags hands out a copy, so the obvious tag.ChildTags.RemoveAll(...) removed nothing
        // and every reload rediscovered the same dead entries.
        var root = Tag("Damage");
        Tag("Fire", root);
        Assert.AreEqual(1, root.ChildTags.Count);

        Object.DestroyImmediate(root.ChildTags[0]);

        root.ChildTags.RemoveAll(child => !child);
        Assert.AreEqual(1, root.ChildTags.Count, "the copy is not the list");

        root.RemoveMissingChildren();
        Assert.AreEqual(0, root.ChildTags.Count);
    }

    [Test]
    public void AFullNameIsBuiltWithoutAnyoneHavingPrimedIt()
    {
        // The bug this replaces: the name was pushed down from the root in Awake, which fires per
        // object as each asset loads and before its siblings are resolved. Whatever was not wired at
        // that instant kept an empty name and rendered under its asset filename instead.
        var root = Tag("Damage");
        var fire = Tag("Fire", root);
        var hot = Tag("Hot", fire);

        Assert.AreEqual("Damage.Fire.Hot", hot.TagFullName);
        Assert.AreEqual("Damage.Fire", fire.TagFullName);
        Assert.AreEqual("Damage", root.TagFullName);
    }

    [Test]
    public void RenamingAnAncestorRefreshesTheNamesBelowIt()
    {
        var root = Tag("Damage");
        var fire = Tag("Fire", root);
        Assert.AreEqual("Damage.Fire", fire.TagFullName);

        root.UpdateName("Harm");

        Assert.AreEqual("Harm.Fire", fire.TagFullName);
    }

    [Test]
    public void UpdatingFullNamesSurvivesADeletedChild()
    {
        var root = Tag("Damage");
        Tag("Fire", root);
        Object.DestroyImmediate(root.ChildTags[0]);

        Assert.DoesNotThrow(() => root.UpdateFullname());
    }
}
}
