using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace GameplayTags.Tests
{
/// <summary>
/// A query is the only place the hierarchy is walked in one direction on purpose: asking about
/// "Damage" must find "Damage.Fire", while asking about "Damage.Fire" must not settle for a bare
/// "Damage". Getting that backwards makes every filter quietly too permissive.
/// </summary>
public class GameplayTagQueryTests
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

    [Test]
    public void AnEmptyQueryMatchesEverything()
    {
        var query = new GameplayTagQuery();

        Assert.IsTrue(query.IsEmpty);
        Assert.IsTrue(query.Matches(GameplayTagSet.Empty));
        Assert.IsTrue(query.Matches(SetOf(Tag("Damage"))));
    }

    [Test]
    public void AQueryOnAParentIsAnsweredByAChild()
    {
        var damage = Tag("Damage");
        var fire = Tag("Fire", damage);

        var query = new GameplayTagQuery(all: SetOf(damage));

        Assert.IsTrue(query.Matches(SetOf(fire)));
    }

    [Test]
    public void AQueryOnAChildIsNotAnsweredByItsParent()
    {
        // The direction that matters: a clip annotated only "Damage" never drew the distinction
        // "Damage.Fire" asks about, so it must not pass.
        var damage = Tag("Damage");
        var fire = Tag("Fire", damage);

        var query = new GameplayTagQuery(all: SetOf(fire));

        Assert.IsFalse(query.Matches(SetOf(damage)));
    }

    [Test]
    public void AllRequiresEveryClauseTagToBeMatched()
    {
        var damage = Tag("Damage");
        var fire = Tag("Fire", damage);
        var state = Tag("State");
        var stunned = Tag("Stunned", state);

        var query = new GameplayTagQuery(all: SetOf(damage, state));

        Assert.IsTrue(query.Matches(SetOf(fire, stunned)));
        Assert.IsFalse(query.Matches(SetOf(fire)));
    }

    [Test]
    public void AnyRequiresOneMatchAndIsIgnoredWhenEmpty()
    {
        var damage = Tag("Damage");
        var state = Tag("State");
        var stunned = Tag("Stunned", state);

        Assert.IsTrue(new GameplayTagQuery(any: SetOf(damage, state)).Matches(SetOf(stunned)));
        Assert.IsFalse(new GameplayTagQuery(any: SetOf(damage)).Matches(SetOf(stunned)));
        Assert.IsTrue(new GameplayTagQuery(all: SetOf(state)).Matches(SetOf(stunned)));
    }

    [Test]
    public void NoneRejectsAMatchAnywhereBelowTheClauseTag()
    {
        var state = Tag("State");
        var stunned = Tag("Stunned", state);
        var damage = Tag("Damage");

        var query = new GameplayTagQuery(all: SetOf(damage), none: SetOf(state));

        Assert.IsTrue(query.Matches(SetOf(damage)));
        Assert.IsFalse(query.Matches(SetOf(damage, stunned)));
    }

    [Test]
    public void AnEmptySetSatisfiesOnlyAQueryThatRequiresNothing()
    {
        var damage = Tag("Damage");

        Assert.IsTrue(new GameplayTagQuery(none: SetOf(damage)).Matches(GameplayTagSet.Empty));
        Assert.IsFalse(new GameplayTagQuery(all: SetOf(damage)).Matches(GameplayTagSet.Empty));
        Assert.IsFalse(new GameplayTagQuery(any: SetOf(damage)).Matches(GameplayTagSet.Empty));
    }
}
}
