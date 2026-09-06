using System.Collections.Generic;
using System.Linq;
using GameplayTags;
using NUnit.Framework;
using UnityEngine;

namespace GameplayTags.Tests
{
/// <summary>
/// Tags are the ability system's whole vocabulary for "can this happen": every block, cooldown
/// and requirement check is a set query, and the hierarchy means "Damage.Fire" has to answer to
/// a question asked about "Damage".
/// </summary>
public class GameplayTagTests
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

    static GameplayTagSet SetOf(params GameplayTagSO[] tags) => new(tags);

    [TearDown]
    public void TearDown()
    {
        foreach (var tag in _created)
            Object.DestroyImmediate(tag);
        _created.Clear();
    }

    // ---- the hierarchy ----

    [Test]
    public void ATagIsItsOwnChildAndItsOwnParent()
    {
        //Documented, and load bearing: a check for "Damage" has to match "Damage" itself, not
        //only things below it.
        var damage = Tag("Damage");

        Assert.IsTrue(damage.IsChildOf(damage));
        Assert.IsTrue(damage.IsParentOf(damage));
    }

    [Test]
    public void ChildAndParentAreTheSameRelationSeenFromEitherEnd()
    {
        var damage = Tag("Damage");
        var fire = Tag("Fire", damage);

        Assert.IsTrue(fire.IsChildOf(damage));
        Assert.IsTrue(damage.IsParentOf(fire));
        Assert.IsFalse(damage.IsChildOf(fire));
        Assert.IsFalse(fire.IsParentOf(damage));
    }

    [Test]
    public void DescendancyReachesThroughIntermediateTags()
    {
        var damage = Tag("Damage");
        var fire = Tag("Fire", damage);
        var blue = Tag("Blue", fire);

        Assert.IsTrue(blue.IsChildOf(damage), "a grandchild is still a descendant");
        Assert.IsTrue(damage.IsParentOf(blue));
    }

    [Test]
    public void UnrelatedBranchesDoNotMatch()
    {
        var damage = Tag("Damage");
        var fire = Tag("Fire", damage);
        var state = Tag("State");
        var stunned = Tag("Stunned", state);

        Assert.IsFalse(fire.IsChildOf(state));
        Assert.IsFalse(stunned.IsChildOf(damage));
    }

    [Test]
    public void TheFullNameSpellsOutThePathFromTheRoot()
    {
        var damage = Tag("Damage");
        var fire = Tag("Fire", damage);
        var blue = Tag("Blue", fire);

        Assert.AreEqual("Damage", damage.TagFullName);
        Assert.AreEqual("Damage.Fire", fire.TagFullName);
        Assert.AreEqual("Damage.Fire.Blue", blue.TagFullName);
    }

    [Test]
    public void ReparentingRewritesTheFullNamesUnderneath()
    {
        var oldRoot = Tag("Old");
        var newRoot = Tag("New");
        var branch = Tag("Branch", oldRoot);
        var leaf = Tag("Leaf", branch);

        Assert.AreEqual("Old.Branch.Leaf", leaf.TagFullName);

        branch.SetParent(newRoot);

        Assert.AreEqual("New.Branch.Leaf", leaf.TagFullName,
            "a descendant's full name has to follow its ancestor");
        CollectionAssert.DoesNotContain(oldRoot.ChildTags, branch);
        CollectionAssert.Contains(newRoot.ChildTags, branch);
    }

    [Test]
    public void EnumeratingATagWalksItAndEverythingBelowIt()
    {
        var root = Tag("Root");
        var a = Tag("A", root);
        var b = Tag("B", root);
        var deep = Tag("Deep", a);

        var walked = new List<GameplayTagSO>();
        var it = root.GetEnumerator();
        while (it.MoveNext()) walked.Add(it.Current);

        CollectionAssert.AreEquivalent(new[] { root, a, deep, b }, walked);
    }

    // ---- set operations ----

    [Test]
    public void AddingTheSameTagTwiceLeavesOneCopy()
    {
        var fire = Tag("Fire");
        var set = new GameplayTagSet();

        Assert.IsTrue(set.AddTag(fire));
        Assert.IsFalse(set.AddTag(fire), "the second add should report that nothing changed");
        Assert.AreEqual(1, set.Count);
    }

    [Test]
    public void RemovingATagThatIsNotThereReportsNoChange()
    {
        var fire = Tag("Fire");
        var ice = Tag("Ice");
        var set = SetOf(fire);

        Assert.IsFalse(set.RemoveTag(ice));
        Assert.IsTrue(set.RemoveTag(fire));
        Assert.AreEqual(0, set.Count);
    }

    [Test]
    public void AnyIsSatisfiedByOneMatchAndAllNeedsEveryOne()
    {
        var fire = Tag("Fire");
        var ice = Tag("Ice");
        var wind = Tag("Wind");
        var owned = SetOf(fire, ice);

        Assert.IsTrue(owned.HasTagAny(SetOf(ice, wind)));
        Assert.IsFalse(owned.HasTagAny(SetOf(wind)));
        Assert.IsTrue(owned.HasTagAll(SetOf(fire, ice)));
        Assert.IsFalse(owned.HasTagAll(SetOf(fire, wind)));
    }

    [Test]
    public void AnEmptyRequirementIsSatisfiedByAnything()
    {
        //Blocked and required tag lists are usually empty, so this is the common path through
        //every activation check.
        var owned = SetOf(Tag("Fire"));

        Assert.IsTrue(owned.HasTagAll(GameplayTagSet.Empty), "nothing required means nothing missing");
        Assert.IsFalse(owned.HasTagAny(GameplayTagSet.Empty), "nothing to match means no match");
    }

    [Test]
    public void IntersectionKeepsOnlyTheSharedTags()
    {
        var fire = Tag("Fire");
        var ice = Tag("Ice");
        var wind = Tag("Wind");

        var shared = SetOf(fire, ice).Intersection(SetOf(ice, wind));

        Assert.AreEqual(1, shared.Count);
        Assert.IsTrue(shared.HasTag(ice));
        Assert.IsFalse(shared.HasTag(fire));
    }

    [Test]
    public void DifferenceDropsTheTagsTheOtherSetHas()
    {
        var fire = Tag("Fire");
        var ice = Tag("Ice");

        var remaining = SetOf(fire, ice).Difference(SetOf(ice));

        Assert.AreEqual(1, remaining.Count);
        Assert.IsTrue(remaining.HasTag(fire));
    }

    [Test]
    public void CloningGivesASetThatCanBeChangedIndependently()
    {
        var fire = Tag("Fire");
        var ice = Tag("Ice");
        var original = SetOf(fire);

        var clone = original.Clone();
        clone.AddTag(ice);

        Assert.AreEqual(1, original.Count, "the original must not see the clone's change");
        Assert.AreEqual(2, clone.Count);
    }

    [Test]
    public void ChildOfMatchesADescendantOfTheAskedForTag()
    {
        //"Do you have any kind of Damage" answered by a set holding Damage.Fire.
        var damage = Tag("Damage");
        var fire = Tag("Fire", damage);
        var owned = SetOf(fire);

        Assert.IsTrue(owned.HasChildOf(damage));
        Assert.IsFalse(owned.HasTag(damage), "an exact query still does not match");
    }

    [Test]
    public void ParentOfMatchesAnAncestorOfTheAskedForTag()
    {
        var damage = Tag("Damage");
        var fire = Tag("Fire", damage);
        var owned = SetOf(damage);

        Assert.IsTrue(owned.HasParentOf(fire));
        Assert.IsFalse(owned.HasChildOf(fire));
    }

    // ---- ref counted tags ----

    [Test]
    public void ATagStaysUntilAsManyRemovalsAsAdditions()
    {
        //Two effects granting the same tag must not let the first to expire take it away.
        var stunned = Tag("Stunned");
        var multiSet = new GameplayTagMultiSet();

        multiSet.UpdateTagCount(stunned, 1);
        multiSet.UpdateTagCount(stunned, 1);
        Assert.AreEqual(2, multiSet.GetTagCount(stunned));
        Assert.IsTrue(multiSet.GetNormalTagSet().HasTag(stunned));

        multiSet.UpdateTagCount(stunned, -1);
        Assert.IsTrue(multiSet.GetNormalTagSet().HasTag(stunned),
            "one holder left, so the tag stays");

        multiSet.UpdateTagCount(stunned, -1);
        Assert.IsFalse(multiSet.GetNormalTagSet().HasTag(stunned));
        Assert.AreEqual(0, multiSet.GetTagCount(stunned));
    }

    [Test]
    public void CrossingZeroIsWhatCountsAsAChange()
    {
        //The return value drives OnTagSetAltered, and so the tag BVH; it has to be true only
        //when the tag actually appeared or disappeared.
        var stunned = Tag("Stunned");
        var multiSet = new GameplayTagMultiSet();

        Assert.IsTrue(multiSet.UpdateTagCount(stunned, 1), "0 -> 1 is an appearance");
        Assert.IsFalse(multiSet.UpdateTagCount(stunned, 1), "1 -> 2 is not");
        Assert.IsFalse(multiSet.UpdateTagCount(stunned, -1), "2 -> 1 is not");
        Assert.IsTrue(multiSet.UpdateTagCount(stunned, -1), "1 -> 0 is a disappearance");
    }

    [Test]
    public void AWholeSetCanBeGrantedAndRevokedAtOnce()
    {
        var a = Tag("A");
        var b = Tag("B");
        var granted = SetOf(a, b);
        var multiSet = new GameplayTagMultiSet();

        multiSet.UpdateTagCount(granted, 1);
        Assert.IsTrue(multiSet.GetNormalTagSet().HasTagAll(granted));

        multiSet.UpdateTagCount(granted, -1);
        Assert.AreEqual(0, multiSet.GetNormalTagSet().Count);
    }

    [Test]
    public void TheNormalSetOnlyListsTagsWithAPositiveCount()
    {
        var held = Tag("Held");
        var gone = Tag("Gone");
        var multiSet = new GameplayTagMultiSet();

        multiSet.UpdateTagCount(held, 1);
        multiSet.UpdateTagCount(gone, 1);
        multiSet.UpdateTagCount(gone, -1);

        CollectionAssert.AreEquivalent(new[] { held }, multiSet.GetNormalTagSet().ToList());
    }
}
}
