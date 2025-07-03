using System;
using System.IO;
using System.Collections.Generic;
using H2V.GameplayAbilitySystem.TagSystem;
using spatial;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

public class TagManager : SimBehaviour
{
    private Dictionary<GameplayTagSO, Bvh2d<SimObject>> tagBvh = new();

    public static TagManager I { get; private set; }

    public bool debugDrawBvh = false;

    protected override void SimInit()
    {
        base.SimInit();
        if (!I)
        {
            I = this;
        }
        else
        {
            Destroy(this);
        }
    }
    
    public void RegisterTagComponent(SimTagComponent component)
    {
        Assert.IsNotNull(component, "TagComponent cannot be null");
        
        foreach (var gameplayTag in component.TagSet)
        {
            if (!tagBvh.ContainsKey(gameplayTag))
            {
                tagBvh[gameplayTag] = new();
            }
            tagBvh[gameplayTag].InsertEntity(component.SimObject);
        }

        component.SimObject.OnPeriodicUpdate += UpdateBvh;
        component.SimObject.OnVelocityChanged += UpdateBvh;
        component.OnTagSetAltered += OnTagSetAltered;
    }
    
    void UpdateBvh(SimObject simObject)
    {
        foreach (var gameplayTag in simObject.TagComponent.TagSet)
        {        
            //the remove works because the new bounds will still intersect the old bounds
            //TODO: Add fail warning here
            tagBvh[gameplayTag].RemoveEntity(simObject);
            tagBvh[gameplayTag].InsertEntity(simObject);
        }
    }
    
    public IEnumerable<SimObject> GetObjectsInRangeWithTag(Vector3d position, 
        double radius, GameplayTagSO gameplayTag)
    {
        var circle = new CircleD2d(position.XZ(), radius);
        var results = new List<SimObject>();

        foreach (var simObject in tagBvh[gameplayTag].TopDownQuery(circle))
        {
            if (simObject.SimCollider.GetCurrentGlobalShape().Intersects(circle))
                yield return simObject;
        }
    }
    public void UnregisterTagComponent(SimTagComponent component)
    {
        Assert.IsNotNull(component, "TagComponent cannot be null");
        
        foreach (var gameplayTag in component.TagSet)
        {
            if (tagBvh.ContainsKey(gameplayTag))
            {
                tagBvh[gameplayTag].RemoveEntity(component.SimObject);
            }
        }
        component.OnTagSetAltered -= OnTagSetAltered;
        component.SimObject.OnPeriodicUpdate -= UpdateBvh;
        component.SimObject.OnVelocityChanged -= UpdateBvh;
    }
    
    void OnTagSetAltered(SimTagComponent component, GameplayTagSO gameplayTag, bool added)
    {
        if (added)
        {
            if (!tagBvh.ContainsKey(gameplayTag))
            {
                tagBvh[gameplayTag] = new();
            }
            tagBvh[gameplayTag].InsertEntity(component.SimObject);
        }
        else
        {
            if (tagBvh.ContainsKey(gameplayTag))
            {
                tagBvh[gameplayTag].RemoveEntity(component.SimObject);
            }
        }
    }
    private void OnDrawGizmos()
    {
        if (debugDrawBvh)
        {
            foreach (var bvhTree in tagBvh.Values)
            {
                bvhTree.DebugDraw();                
            }
        }
    }
    
}
