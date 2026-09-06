using System;
using System.Text;
using UnityEngine;

namespace GameplayTags
{
/// <summary>
/// A question about a <see cref="GameplayTagSet"/>: which tags it must all have, which it must have
/// at least one of, and which it must have none of.
/// </summary>
/// <remarks>
/// Matching walks the hierarchy in one direction only: a query tag is satisfied by anything at or
/// below it. A set holding <c>Damage.Fire</c> answers a query for <c>Damage</c>, because it is a
/// kind of damage; a set holding only <c>Damage</c> does not answer a query for <c>Damage.Fire</c>,
/// because that asks for a distinction the set never drew.
/// <para>
/// Every clause is optional and an empty query matches everything, so a serialized field can be
/// left blank to mean "no filter" without a separate enabled flag.
/// </para>
/// </remarks>
[Serializable]
public class GameplayTagQuery
{
    [Tooltip("Every one of these must be matched.")]
    [SerializeField] private GameplayTagSet all = new();

    [Tooltip("At least one of these must be matched. Ignored when empty.")]
    [SerializeField] private GameplayTagSet any = new();

    [Tooltip("None of these may be matched.")]
    [SerializeField] private GameplayTagSet none = new();

    public GameplayTagSet All => all;
    public GameplayTagSet Any => any;
    public GameplayTagSet None => none;

    /// <summary>Whether the query asks nothing, and so matches every set.</summary>
    public bool IsEmpty => all.Count == 0 && any.Count == 0 && none.Count == 0;

    public GameplayTagQuery()
    {
    }

    public GameplayTagQuery(GameplayTagSet all = null, GameplayTagSet any = null,
        GameplayTagSet none = null)
    {
        this.all = all ?? new GameplayTagSet();
        this.any = any ?? new GameplayTagSet();
        this.none = none ?? new GameplayTagSet();
    }

    public bool Matches(GameplayTagSet tags)
    {
        if (tags == null) return IsEmpty;

        return tags.HasChildOfAll(all)
               && (any.Count == 0 || tags.HasChildOfAny(any))
               && !tags.HasChildOfAny(none);
    }

    public override string ToString()
    {
        if (IsEmpty) return "(any)";

        var text = new StringBuilder();
        Append(text, "all", all);
        Append(text, "any", any);
        Append(text, "none", none);
        return text.ToString();
    }

    private static void Append(StringBuilder text, string clause, GameplayTagSet tags)
    {
        if (tags.Count == 0) return;

        if (text.Length > 0) text.Append(" · ");
        text.Append(clause).Append(": ");

        var first = true;
        foreach (var tag in tags)
        {
            if (!first) text.Append(", ");
            text.Append(tag.TagFullName);
            first = false;
        }
    }
}
}
