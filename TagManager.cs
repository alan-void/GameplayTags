using System;
using System.IO;
using System.Collections.Generic;
using GameplayTags;
using UnityEditor;
using UnityEngine;

public class TagManager : MonoBehaviour
{
    private Dictionary<GameplayTagInternal, HashSet<TagComponent>> tagMap = new();

    [SerializeField] private GameplayTag testTag;
        
    [SerializeField]
    TextAsset tagConfigFile;
    
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
}
