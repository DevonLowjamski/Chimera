using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Collections;
using UnityEngine;

namespace ProjectChimera.Core.Memory
{
    public class MemoryOptimizedDictionary<TKey, TValue> : IDictionary<TKey, TValue>, IDisposable
        where TKey : IEquatable<TKey>
    {
        private Dictionary<TKey, TValue> _dictionary = new Dictionary<TKey, TValue>();

        public TValue this[TKey key]
        {
            get => _dictionary[key];
            set => _dictionary[key] = value;
        }

        public ICollection<TKey> Keys => _dictionary.Keys;
        public ICollection<TValue> Values => _dictionary.Values;
        public int Count => _dictionary.Count;
        public bool IsReadOnly => false;

        public void Add(TKey key, TValue value) => _dictionary.Add(key, value);
        public void Add(KeyValuePair<TKey, TValue> item) => _dictionary.Add(item.Key, item.Value);
        public void Clear() => _dictionary.Clear();
        public bool Contains(KeyValuePair<TKey, TValue> item) => ((IDictionary<TKey, TValue>)_dictionary).Contains(item);
        public bool ContainsKey(TKey key) => _dictionary.ContainsKey(key);
        public void CopyTo(KeyValuePair<TKey, TValue>[] array, int arrayIndex) => ((IDictionary<TKey, TValue>)_dictionary).CopyTo(array, arrayIndex);
        public IEnumerator<KeyValuePair<TKey, TValue>> GetEnumerator() => _dictionary.GetEnumerator();
        public bool Remove(TKey key) => _dictionary.Remove(key);
        public bool Remove(KeyValuePair<TKey, TValue> item) => ((IDictionary<TKey, TValue>)_dictionary).Remove(item);
        public bool TryGetValue(TKey key, out TValue value) => _dictionary.TryGetValue(key, out value);
        IEnumerator IEnumerable.GetEnumerator() => _dictionary.GetEnumerator();

        public void Dispose()
        {
            _dictionary.Clear();
            _dictionary = null;
        }
    }
}
