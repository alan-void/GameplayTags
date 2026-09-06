using System;
using System.Collections.Generic;
using UnityEngine;

namespace GameplayTags
{
/// <summary>
/// A Dictionary that Unity can serialize, stored as parallel <c>keys</c> / <c>values</c>
/// lists. Drawers reach the lists by those names, so keep them.
/// </summary>
[Serializable]
internal class SerializableDictionary<TKey, TValue> : Dictionary<TKey, TValue>, ISerializationCallbackReceiver
{
    [SerializeField]
    private List<TKey> keys = new List<TKey>();

    [SerializeField]
    private List<TValue> values = new List<TValue>();

    public void OnBeforeSerialize()
    {
        bool hadNull = keys.Count > 0 && keys[^1] == null;
        keys.Clear();
        values.Clear();

        foreach (KeyValuePair<TKey, TValue> pair in this)
        {
            keys.Add(pair.Key);
            values.Add(pair.Value);
        }
        if (hadNull)
        {
            keys.Add(default);
            values.Add(default);
        }
    }

    public void OnAfterDeserialize()
    {
        this.Clear();

        if (keys.Count != values.Count)
        {
            Debug.LogError($"SerializableDictionary deserialization failed: keys count ({keys.Count}) does not match values count ({values.Count})");
            return;
        }

        for (int i = 0; i < keys.Count; i++)
        {
            if (keys[i] != null && !this.ContainsKey(keys[i]))
            {
                this.Add(keys[i], values[i]);
            }
        }
    }

    public SerializableDictionary(int capacity) : base(capacity)
    {
        keys = new List<TKey>(capacity);
        values = new List<TValue>(capacity);
    }

    public SerializableDictionary() : base()
    {
    }

    public SerializableDictionary(IDictionary<TKey, TValue> dictionary) : base(dictionary)
    {
    }

    public SerializableDictionary(IEqualityComparer<TKey> comparer) : base(comparer)
    {
    }
}
}
