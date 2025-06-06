using System;
using System.IO;
using System.Collections.Generic;
using GameplayTags;
using UnityEditor;
using UnityEngine;
using UnityEngine.Assertions;

public class TagManager : MonoBehaviour
{
    private Dictionary<GameplayTag, HashSet<TagComponent>> tagMap = new();

    //TODO: remove this
    [SerializeField] private GameplayTag testTag;
    // [SerializeField] private GameplayTagSet testTagSet;

    public static GameplayTag RootTag => GameplayTagConfig.instance.rootTag;
    
    public static TagManager I { get; private set; }

    private void Awake()
    {
        if (I == null)
        {
            I = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // private void OnValidate()
    // {
    //     SyncTagsWithConfig();
    // }

    // void SyncTagsWithConfig()
    // {
    //     SyncRecursive(rootTag, GameplayTagConfig.instance.rootTag);
    // }
    //
    // private void SyncRecursive(GameplayTag tagSo, GameplayTagInternal internalTag)
    // {
    //     if (internalTag == null && tagSo == null)
    //         return;
    //
    //     if (internalTag == null)
    //     {
    //         //delete the tags from the SO
    //         foreach(var childTagSo in tagSo.childTags)
    //         {
    //             SyncRecursive(childTagSo, null);
    //         }
    //         DestroyImmediate(tagSo);
    //         return;
    //     }
    //     
    //     if (!tagSo)
    //     {
    //         Assert.IsTrue(false);
    //     }
    //     
    //     if(tagSo.TagName != internalTag.tagName)
    //     {
    //         if (internalTag.previousNames.Contains(tagSo.TagName))
    //         {
    //             tagSo.UpdateName(internalTag.tagName);
    //         }
    //         else
    //         {
    //             Assert.IsTrue(false);
    //         }
    //     }
    //     
    //     var childTagsSo = new List<GameplayTag>(tagSo.childTags);
    //
    //     foreach (var childTagInternal in internalTag.childTags)
    //     {
    //         GameplayTag childTagSo = null;
    //         // Match with same name if exists
    //         foreach (var child in childTagsSo)
    //         {
    //             if (child.TagName == childTagInternal.tagName)
    //             {
    //                 childTagSo = child;
    //                 break;
    //             }
    //         }
    //         if (childTagSo != null)
    //         {
    //             childTagsSo.Remove(childTagSo);
    //             SyncRecursive(childTagSo, childTagInternal);
    //             break;
    //         }
    //         // Match with previous names if exists (renamed tags)
    //         foreach (var child in childTagsSo)
    //         {
    //             if (childTagInternal.previousNames.Contains(child.TagName))
    //             {
    //                 childTagSo = child;
    //                 break;
    //             }
    //         }
    //         if (childTagSo != null)
    //         {
    //             childTagsSo.Remove(childTagSo);
    //             SyncRecursive(childTagSo, childTagInternal);
    //             break;
    //         }
    //         // tag does not exist in SO, create it
    //         childTagSo = ScriptableObject.CreateInstance<GameplayTag>();
    //         childTagSo.UpdateName(internalTag.tagName);
    //         childTagSo.SetParent(tagSo);
    //         SyncRecursive(childTagSo, childTagInternal);
    //     }
    //     
    //     // childTagsSo now contains unmatched SO tags
    //     foreach (var childTagSo in childTagsSo)
    //     {
    //         SyncRecursive(childTagSo, null);
    //     }
    // }
}
