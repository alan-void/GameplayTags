

using UnityEngine;
using GameplayTags;

namespace GameplayTags
{
public class SimTagComponent : SimBehaviour
{
    [field: SerializeField] public GameplayTagMultiSet TagMultiSet { get; private set; } = new();
    
    public GameplayTagSet TagSet => TagMultiSet.GetNormalTagSet();
    
    public delegate void OnTagSetAlteredCallback(
        SimTagComponent component, GameplayTagSO tag, bool added);

    public event OnTagSetAlteredCallback OnTagSetAltered;

    public override void SimStart()
    {
        base.SimStart();
        TagManager.I.RegisterTagComponent(this);
    }

    /// <summary>
    /// Updates the tagset. will invoke <see cref="OnTagSetAltered"/> if this function returns true
    /// </summary>
    /// <param name="gameplayTag"></param>
    /// <param name="countDelta"></param>
    /// <returns>True if tag was *either* added or removed. (E.g., we had the tag and now dont. or didnt have the tag and now we do. We didn't just change the count (1 count -> 2 count would return false).</returns>
    public bool UpdateTagCount(GameplayTagSO gameplayTag, int countDelta)
    {
        if (!TagMultiSet.UpdateTagCount(gameplayTag, countDelta)) return false;
        OnTagSetAltered?.Invoke(this, gameplayTag, countDelta > 0);
        return true;
    }

    public void UpdateTagCount(GameplayTagSet gameplayTagSet, int countDelta)
    {
        foreach (var gameplayTag in gameplayTagSet)
        {
            UpdateTagCount(gameplayTag, countDelta);
        }
    }

    public bool AddTag(GameplayTagSO newTag)
    {
        return UpdateTagCount(newTag, 1);
    }

    public bool RemoveTag(GameplayTagSO newTag)
    {
        return UpdateTagCount(newTag, -1);
    }

    public void AddTags(params GameplayTagSO[] tags)
    {
        foreach (var gameplayTag in tags)
        {
            AddTag(gameplayTag);
        }
    }
    public void AddTags(GameplayTagSet tags)
    {
        foreach (var gameplayTag in tags)
        {
            AddTag(gameplayTag);
        }
    }
    public void RemoveTags(params GameplayTagSO[] tags)
    {
        foreach (var gameplayTag in tags)
        {
            RemoveTag(gameplayTag);
        }
    }
    public void RemoveTags(GameplayTagSet tags)
    {
        foreach (var gameplayTag in tags)
        {
            RemoveTag(gameplayTag);
        }
    }

    public override void OnSimDestroy()
    {
        base.OnSimDestroy();
        TagManager.I.UnregisterTagComponent(this);
    }
}
}