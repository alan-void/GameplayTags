using System;
using UnityEngine.Assertions;
using UnityEngine;

namespace GameplayTags
{
[Serializable]
public class GameplayTagMultiSet : ISerializationCallbackReceiver
{
    [SerializeField] private SerializableDictionary<GameplayTagSO, int> tagToCountMap = new();

    //TODO: this should be readonly, expose the multiset in inspector
    /// <summary>
    /// A set containing tags with positive count.
    /// </summary>
    /// <remarks>
    /// Built here rather than only in <see cref="OnAfterDeserialize"/>, which does not run for
    /// a multiset that was constructed rather than loaded - a component added at runtime has
    /// no serialized data to apply - and left the first tag update to throw.
    /// </remarks>
    private GameplayTagSet _positiveCountTags = new();

    /// <summary>
    /// Returns set containing tags with positive count. Do not modify
    /// </summary>
    /// <returns></returns>
    public GameplayTagSet GetNormalTagSet() => _positiveCountTags;

    public void UpdateTagCount(GameplayTagSet tagSet, int countDelta)
    {
        if (countDelta == 0) return;
        foreach (GameplayTagSO tag in tagSet)
        {
            UpdateTagCount(tag, countDelta);
        }
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="tag"></param>
    /// <param name="countDelta"></param>
    /// <returns>True if tag was *either* added or removed. (E.g., we had the tag and now dont. or didnt have the tag and now we do. We didn't just change the count (1 count -> 2 count would return false).</returns>
    public bool UpdateTagCount(GameplayTagSO tag, int countDelta)
    {
        if (countDelta == 0) return false;
        tagToCountMap.TryGetValue(tag, out var count);
        var newCount = count + countDelta;
        tagToCountMap[tag] = newCount;
        if (newCount > 0 && count > 0) return false;
        if (newCount <= 0 && count <= 0) return false;
        if (newCount > 0 && count <= 0)
        {
            _positiveCountTags.AddTag(tag);
            return true;
        }

        if (newCount <= 0 && count > 0)
        {
            _positiveCountTags.RemoveTag(tag);
            return true;
        }

        Assert.IsTrue(false);
        return true;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="tag"></param>
    /// <param name="newCount"></param>
    /// <returns>True if tag was *either* added or removed. (E.g., we had the tag and now dont. or didnt have the tag and now we do. We didn't just change the count (1 count -> 2 count would return false).</returns>
    public bool SetTagCount(GameplayTagSO tag, int newCount)
    {
        tagToCountMap.TryGetValue(tag, out var count);
        var countDelta = newCount - count;
        return UpdateTagCount(tag, countDelta);
    }

    public int GetTagCount(GameplayTagSO tag)
    {
        tagToCountMap.TryGetValue(tag, out var count);
        return count;
    }

    public void OnBeforeSerialize()
    {
    }

    public void OnAfterDeserialize()
    {
        _positiveCountTags.Clear();
        foreach (var pair in tagToCountMap)
        {
            if (pair.Value > 0)
            {
                _positiveCountTags.AddTag(pair.Key);
            }
        }
    }
}
}