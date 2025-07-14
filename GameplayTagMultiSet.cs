using System;
using NUnit.Framework;
using UnityEngine;

namespace GameplayTags
{
[Serializable]
public class GameplayTagMultiSet
{
    [SerializeField] private SerializableDictionary<GameplayTagSO, int> tagToCountMap = new();

    /// <summary>
    /// A set containing tags with positive count
    /// </summary>
    [SerializeField] private GameplayTagSet positiveCountTags;
    
    /// <summary>
    /// Returns set containing tags with positive count. Do not modify
    /// </summary>
    /// <returns></returns>
    public GameplayTagSet GetNormalTagSet() =>  positiveCountTags;

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
        var newCount =  count + countDelta;
        tagToCountMap[tag] = newCount;
        if(newCount>0 && count > 0) return false;
        if(newCount<=0 && count <= 0) return false;
        if (newCount > 0 && count <= 0)
        {
            positiveCountTags.AddTag(tag);
            return true;
        }
        if (newCount <= 0 && count > 0)
        {
            positiveCountTags.RemoveTag(tag);
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
}
}