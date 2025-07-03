

using UnityEngine;
using System.Collections.Generic;
using H2V.GameplayAbilitySystem.TagSystem;

[DisallowMultipleComponent]
public class SimTagComponent : SimBehaviour
{
    [field: SerializeField] 
    public GameplayTagSet TagSet { get; private set; } = new();
    public delegate void OnTagSetAlteredCallback(
        SimTagComponent component, GameplayTagSO tag, bool added);
    public event OnTagSetAlteredCallback OnTagSetAltered;
    
    public override void SimStart()
    {
        base.SimStart();
        TagManager.I.RegisterTagComponent(this);
    }
    
    public bool AddTag(GameplayTagSO newTag)
    {
        if(TagSet.AddTag(newTag))
        {
            OnTagSetAltered?.Invoke(this, newTag, true);
            return true;
        }

        return false;
    }

    public bool RemoveTag(GameplayTagSO newTag)
    {
        if (TagSet.RemoveTag(newTag))
        {
            OnTagSetAltered?.Invoke(this, newTag, false);
            return true;
        }
        return false;
    }
    public void AddTags(params GameplayTagSO[] tags)
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

    public override void OnSimDestroy()
    {
        base.OnSimDestroy();
        TagManager.I.UnregisterTagComponent(this);
    }
}