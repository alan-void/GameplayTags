using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace GameplayTags
{
/// <summary>
/// A HashSet that Unity can serialize, stored as a list named <c>items</c>. Drawers reach
/// the list by that name, so keep it.
/// </summary>
[Serializable]
internal class SerializableHashSet<T> : ISerializationCallbackReceiver, IEnumerable<T>
{
    [SerializeField]
    private List<T> items = new List<T>();

    private HashSet<T> _set = new HashSet<T>();

    public void OnBeforeSerialize()
    {
        if (items.Count >= 2 && ReferenceEquals(items[^1], items[^2]))
        {
            // + button pressed on unity inspector
            if (!_set.Add(default))
            {
                Debug.LogWarning("The set already contains default value, skipping add");
            }
        }
        items.Clear();
        foreach (T item in _set)
        {
            items.Add(item);
        }
    }

    public void OnAfterDeserialize()
    {
        _set.Clear();
        foreach (var t in items)
        {
            _set.Add(t);
        }
    }

    public SerializableHashSet()
    {
    }

    public SerializableHashSet(int capacity)
    {
        _set = new HashSet<T>(capacity);
        items = new List<T>(capacity);
    }

    public SerializableHashSet(IEnumerable<T> collection)
    {
        _set = new HashSet<T>(collection);
    }

    public SerializableHashSet(IEqualityComparer<T> comparer)
    {
        _set = new HashSet<T>(comparer);
    }

    public SerializableHashSet(IEnumerable<T> collection, IEqualityComparer<T> comparer)
    {
        _set = new HashSet<T>(collection, comparer);
    }

    public int Count => _set.Count;

    public bool Add(T item) => _set.Add(item);

    public bool Remove(T item) => _set.Remove(item);

    public bool Contains(T item) => _set.Contains(item);

    public void Clear() => _set.Clear();

    public void UnionWith(IEnumerable<T> other) => _set.UnionWith(other);

    public IEnumerator<T> GetEnumerator()
    {
        return _set.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return GetEnumerator();
    }
}
}
