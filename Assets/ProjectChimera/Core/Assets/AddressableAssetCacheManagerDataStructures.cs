// REFACTORED: Data Structures
// Extracted from AddressableAssetCacheManager.cs for better separation of concerns

using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core.Assets
{
    public class LRUCache<T>
    {
        private readonly int _maxSize;
        private readonly LinkedList<T> _items = new LinkedList<T>();
        private readonly Dictionary<T, LinkedListNode<T>> _itemNodes = new Dictionary<T, LinkedListNode<T>>();

        public int MaxSize => _maxSize;
        public int Count => _items.Count;

        public LRUCache(int maxSize)
        {
            _maxSize = maxSize;
        }

        public void Add(T item)
        {
            if (_itemNodes.ContainsKey(item))
            {
                Access(item);
                return;
            }

            if (_items.Count >= _maxSize)
            {
                var lastItem = _items.Last.Value;
                _items.RemoveLast();
                _itemNodes.Remove(lastItem);
            }

            var node = _items.AddFirst(item);
            _itemNodes[item] = node;
        }

        public void Access(T item)
        {
            if (_itemNodes.TryGetValue(item, out var node))
            {
                _items.Remove(node);
                _items.AddFirst(node);
            }
        }

        public bool Remove(T item)
        {
            if (_itemNodes.TryGetValue(item, out var node))
            {
                _items.Remove(node);
                _itemNodes.Remove(item);
                return true;
            }
            return false;
        }

        public T GetLeastRecentlyUsed()
        {
            return _items.Count > 0 ? _items.Last.Value : default(T);
        }

        public List<T> GetLeastRecentlyUsedItems()
        {
            return _items.Reverse().ToList();
        }

        public void Clear()
        {
            _items.Clear();
            _itemNodes.Clear();
        }
    }

    public struct CachedAsset
    {
        public string Address;
        public object Asset;
        public Type AssetType;
        public DateTime CacheTime;
        public DateTime LastAccessTime;
        public int AccessCount;
        public long EstimatedSize;
    }

    public struct CacheStats
    {
        public int AssetsCached;
        public int AssetsEvicted;
        public int CacheHits;
        public int CacheMisses;
        public int CacheClears;
        public int CacheEvictions;
        public int LRUEvictions;
        public int MemoryEvictions;
        public int CacheOptimizations;

        public readonly float HitRate => (CacheHits + CacheMisses) > 0 ? (float)CacheHits / (CacheHits + CacheMisses) : 0f;
    }

    public struct CacheMemoryStats
    {
        public long TotalMemoryUsage;
        public long MaxMemoryUsage;
        public Dictionary<Type, long> TypeMemoryUsage;
        public int AssetCount;
        public long AverageAssetSize;

        public readonly float MemoryUsagePercent => MaxMemoryUsage > 0 ? (float)TotalMemoryUsage / MaxMemoryUsage : 0f;
    }

    public struct CacheSummary
    {
        public CacheStats Stats;
        public CacheMemoryStats MemoryStats;
        public bool IsEnabled;
        public int MaxSize;
        public int CurrentSize;
        public List<CachedAsset> TopAccessedAssets;
        public Dictionary<Type, int> TypeCounts;
    }

}
