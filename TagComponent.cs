

using UnityEngine;
using System.Collections.Generic;
using GameplayTags;

[DisallowMultipleComponent]
public class TagComponent : SimBehaviour
{
    [SerializeField]
    GameplayTagSet tagSet = new();
    
    public GameplayTagSet TagSet => tagSet;

    public delegate void OnTagSetAlteredCallback(
        TagComponent component, GameplayTag tag, bool added);
    
    public event OnTagSetAlteredCallback OnTagSetAltered;

    protected override void SimInit()
    {
        base.SimInit();
        tagSet ??= new GameplayTagSet();
    }
    
    public override void SimStart()
    {
        base.SimStart();
        TagManager.I.RegisterTagComponent(this);
    }
    
    public bool AddTag(GameplayTag newTag)
    {
        if(tagSet.AddTag(newTag))
        {
            OnTagSetAltered?.Invoke(this, newTag, true);
            return true;
        }

        return false;
    }

    public bool RemoveTag(GameplayTag newTag)
    {
        if (tagSet.RemoveTag(newTag))
        {
            OnTagSetAltered?.Invoke(this, newTag, false);
            return true;
        }
        return false;
    }

    public override void OnSimDestroy()
    {
        base.OnSimDestroy();
        TagManager.I.UnregisterTagComponent(this);
    }
}