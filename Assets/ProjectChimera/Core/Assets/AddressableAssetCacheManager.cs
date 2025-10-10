using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core.Assets
{
    /// <summary>
    /// REFACTORED: Addressable Asset Cache Manager
    /// Single Responsibility: Asset caching, memory management, and cache optimization
    /// Extracted from AddressablesAssetManager for better separation of concerns
    /// </summary>
    public class AddressableAssetCacheManager
    {
        [Header("Cache Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private bool _enableCaching = true;
        [SerializeField] private int _maxCacheSize = 100;
        [SerializeField] private long _maxMemoryUsage = 512 * 1024 * 1024; // 512MB

        // Cache storage
        private Dictionary<string, CachedAsset> _assetCache = new Dictionary<string, CachedAsset>();
        private Dictionary<Type, List<string>> _typeIndex = new Dictionary<Type, List<string>>();
        private LRUCache<string> _lruTracker;

        // Cache statistics
        private CacheStats _stats = new CacheStats();

        // Memory tracking
        private long _currentMemoryUsage = 0;
        private Dictionary<string, long> _assetMemoryUsage = new Dictionary<string, long>();

        // State tracking
        private bool _isInitialized = false;

        // Events
        public event System.Action<string, object> OnAssetCached;
        public event System.Action<string> OnAssetEvicted;
        public event System.Action<CacheStats> OnStatsUpdated;
        public event System.Action OnCacheCleared;

        public bool IsInitialized => _isInitialized;
        public CacheStats Stats => _stats;
        public bool CachingEnabled => _enableCaching;
        public int CachedAssetCount => _assetCache.Count;
        public long CurrentMemoryUsage => _currentMemoryUsage;
        public float MemoryUsagePercent => _maxMemoryUsage > 0 ? (float)_currentMemoryUsage / _maxMemoryUsage : 0f;

        public void Initialize()
        {
            if (_isInitialized) return;

            _assetCache.Clear();
            _typeIndex.Clear();
            _assetMemoryUsage.Clear();
            _lruTracker = new LRUCache<string>(_maxCacheSize);
            ResetStats();

            _isInitialized = true;
            _currentMemoryUsage = 0;

            if (_enableLogging)
            {
                ChimeraLogger.Log("ASSETS", "Addressable Asset Cache Manager initialized");
            }
        }
        /// <summary>
        /// Cache asset with memory tracking
        /// </summary>
        public bool CacheAsset(string address, object asset)
        {
            if (!_isInitialized || !_enableCaching || asset == null)
            {
                return false;
            }

            // Check if already cached
            if (_assetCache.ContainsKey(address))
            {
                _stats.CacheHits++;
                _lruTracker.Access(address);
                return true;
            }

            var assetType = asset.GetType();
            var estimatedSize = EstimateAssetSize(asset);

            // Check memory limits
            if (_currentMemoryUsage + estimatedSize > _maxMemoryUsage)
            {
                if (!EvictAssetsToFitSize(estimatedSize))
                {
                    _stats.CacheEvictions++;

                    if (_enableLogging)
                    {
                        ChimeraLogger.LogWarning("ASSETS", $"Cannot cache '{address}': insufficient memory");
                    }

                    return false;
                }
            }

            // Check cache size limit
            if (_assetCache.Count >= _maxCacheSize)
            {
                EvictLeastRecentlyUsed();
            }

            // Create cached asset entry
            var cachedAsset = new CachedAsset
            {
                Address = address,
                Asset = asset,
                AssetType = assetType,
                CacheTime = DateTime.Now,
                LastAccessTime = DateTime.Now,
                AccessCount = 1,
                EstimatedSize = estimatedSize
            };

            // Add to cache
            _assetCache[address] = cachedAsset;
            _assetMemoryUsage[address] = estimatedSize;
            _currentMemoryUsage += estimatedSize;
            _lruTracker.Add(address);

            // Update type index
            if (!_typeIndex.TryGetValue(assetType, out var typeList))
            {
                typeList = new List<string>();
                _typeIndex[assetType] = typeList;
            }
            typeList.Add(address);

            _stats.AssetsCached++;
            OnAssetCached?.Invoke(address, asset);
            OnStatsUpdated?.Invoke(_stats);

            if (_enableLogging)
            {
                ChimeraLogger.Log("ASSETS", $"Cached asset '{address}' ({estimatedSize} bytes, {_assetCache.Count} total)");
            }

            return true;
        }
        /// <summary>
        /// Retrieve cached asset
        /// </summary>
        public T GetCachedAsset<T>(string address) where T : class
        {
            if (!_isInitialized || !_enableCaching)
            {
                return null;
            }

            if (_assetCache.TryGetValue(address, out var cachedAsset))
            {
                // Update access tracking
                cachedAsset.LastAccessTime = DateTime.Now;
                cachedAsset.AccessCount++;
                _assetCache[address] = cachedAsset;
                _lruTracker.Access(address);

                _stats.CacheHits++;

                if (_enableLogging)
                {
                    ChimeraLogger.Log("ASSETS", $"Cache hit for '{address}' (accessed {cachedAsset.AccessCount} times)");
                }

                return cachedAsset.Asset as T;
            }

            _stats.CacheMisses++;
            return null;
        }
        /// <summary>
        /// Check if asset is cached
        /// </summary>
        public bool IsAssetCached(string address)
        {
            return _assetCache.ContainsKey(address);
        }
        /// <summary>
        /// Remove specific asset from cache
        /// </summary>
        public bool RemoveFromCache(string address)
        {
            if (!_assetCache.TryGetValue(address, out var cachedAsset))
            {
                return false;
            }

            // Remove from cache
            _assetCache.Remove(address);
            _lruTracker.Remove(address);

            // Update memory tracking
            if (_assetMemoryUsage.TryGetValue(address, out var size))
            {
                _currentMemoryUsage -= size;
                _assetMemoryUsage.Remove(address);
            }

            // Update type index
            if (_typeIndex.TryGetValue(cachedAsset.AssetType, out var typeList))
            {
                typeList.Remove(address);
                if (typeList.Count == 0)
                {
                    _typeIndex.Remove(cachedAsset.AssetType);
                }
            }

            _stats.AssetsEvicted++;
            OnAssetEvicted?.Invoke(address);

            if (_enableLogging)
            {
                ChimeraLogger.Log("ASSETS", $"Removed '{address}' from cache");
            }

            return true;
        }
        /// <summary>
        /// Clear all cached assets
        /// </summary>
        public void ClearCache()
        {
            var count = _assetCache.Count;

            _assetCache.Clear();
            _typeIndex.Clear();
            _assetMemoryUsage.Clear();
            _lruTracker.Clear();
            _currentMemoryUsage = 0;

            _stats.CacheClears++;
            OnCacheCleared?.Invoke();

            if (_enableLogging)
            {
                ChimeraLogger.Log("ASSETS", $"Cleared cache ({count} assets removed)");
            }
        }
        /// <summary>
        /// Clear assets of specific type
        /// </summary>
        public int ClearAssetType<T>()
        {
            var targetType = typeof(T);
            return ClearAssetType(targetType);
        }
        /// <summary>
        /// Clear assets of specific type
        /// </summary>
        public int ClearAssetType(Type targetType)
        {
            if (!_typeIndex.TryGetValue(targetType, out var addresses))
            {
                return 0;
            }

            var addressesCopy = new List<string>(addresses);
            var removedCount = 0;

            foreach (var address in addressesCopy)
            {
                if (RemoveFromCache(address))
                {
                    removedCount++;
                }
            }

            if (_enableLogging && removedCount > 0)
            {
                ChimeraLogger.Log("ASSETS", $"Cleared {removedCount} assets of type {targetType.Name}");
            }

            return removedCount;
        }
        /// <summary>
        /// Evict least recently used assets
        /// </summary>
        private void EvictLeastRecentlyUsed()
        {
            var lruAddress = _lruTracker.GetLeastRecentlyUsed();
            if (!string.IsNullOrEmpty(lruAddress))
            {
                RemoveFromCache(lruAddress);
                _stats.LRUEvictions++;
            }
        }
        /// <summary>
        /// Evict assets to fit required size
        /// </summary>
        private bool EvictAssetsToFitSize(long requiredSize)
        {
            var freedMemory = 0L;
            var evictedAssets = new List<string>();

            // Get assets ordered by LRU
            var lruAddresses = _lruTracker.GetLeastRecentlyUsedItems();

            foreach (var address in lruAddresses)
            {
                if (freedMemory >= requiredSize)
                {
                    break;
                }

                if (_assetMemoryUsage.TryGetValue(address, out var assetSize))
                {
                    evictedAssets.Add(address);
                    freedMemory += assetSize;
                }
            }

            // Remove the selected assets
            foreach (var address in evictedAssets)
            {
                RemoveFromCache(address);
            }

            _stats.MemoryEvictions += evictedAssets.Count;

            if (_enableLogging && evictedAssets.Count > 0)
            {
                ChimeraLogger.Log("ASSETS", $"Evicted {evictedAssets.Count} assets to free {freedMemory} bytes");
            }

            return freedMemory >= requiredSize;
        }
        /// <summary>
        /// Get cached assets by type
        /// </summary>
        public List<T> GetCachedAssetsByType<T>() where T : class
        {
            var results = new List<T>();
            var targetType = typeof(T);

            if (_typeIndex.TryGetValue(targetType, out var addresses))
            {
                foreach (var address in addresses)
                {
                    if (_assetCache.TryGetValue(address, out var cachedAsset) && cachedAsset.Asset is T asset)
                    {
                        results.Add(asset);
                    }
                }
            }

            return results;
        }
        /// <summary>
        /// Get cache memory statistics
        /// </summary>
        public CacheMemoryStats GetMemoryStats()
        {
            var typeMemoryUsage = new Dictionary<Type, long>();

            foreach (var kvp in _assetCache)
            {
                var assetType = kvp.Value.AssetType;
                var size = _assetMemoryUsage.GetValueOrDefault(kvp.Key, 0);

                if (!typeMemoryUsage.ContainsKey(assetType))
                {
                    typeMemoryUsage[assetType] = 0;
                }
                typeMemoryUsage[assetType] += size;
            }

            return new CacheMemoryStats
            {
                TotalMemoryUsage = _currentMemoryUsage,
                MaxMemoryUsage = _maxMemoryUsage,
                TypeMemoryUsage = typeMemoryUsage,
                AssetCount = _assetCache.Count,
                AverageAssetSize = _assetCache.Count > 0 ? _currentMemoryUsage / _assetCache.Count : 0
            };
        }
        /// <summary>
        /// Estimate asset memory usage
        /// </summary>
        private long EstimateAssetSize(object asset)
        {
            return asset switch
            {
                Texture2D texture => texture.width * texture.height * 4, // Assume RGBA32
                AudioClip audioClip => (long)(audioClip.length * audioClip.frequency * audioClip.channels * 4),
                Mesh mesh => mesh.vertexCount * 48, // Rough estimate for vertex data
                Material material => 1024, // Base material size estimate
                GameObject gameObject => 2048, // Base GameObject estimate
                ScriptableObject scriptableObject => 512, // Base ScriptableObject estimate
                _ => 1024 // Default estimate
            };
        }
        /// <summary>
        /// Optimize cache by removing unused assets
        /// </summary>
        public int OptimizeCache(TimeSpan maxAge)
        {
            var cutoffTime = DateTime.Now - maxAge;
            var assetsToRemove = new List<string>();

            foreach (var kvp in _assetCache)
            {
                if (kvp.Value.LastAccessTime < cutoffTime)
                {
                    assetsToRemove.Add(kvp.Key);
                }
            }

            foreach (var address in assetsToRemove)
            {
                RemoveFromCache(address);
            }

            _stats.CacheOptimizations++;

            if (_enableLogging && assetsToRemove.Count > 0)
            {
                ChimeraLogger.Log("ASSETS", $"Cache optimization removed {assetsToRemove.Count} aged assets");
            }

            return assetsToRemove.Count;
        }
        /// <summary>
        /// Set cache configuration
        /// </summary>
        public void SetCacheConfig(bool enabled, int maxSize, long maxMemory)
        {
            _enableCaching = enabled;
            _maxCacheSize = Mathf.Max(1, maxSize);
            _maxMemoryUsage = System.Math.Max(1024L, maxMemory);

            // Recreate LRU tracker if size changed
            if (_lruTracker?.MaxSize != _maxCacheSize)
            {
                _lruTracker = new LRUCache<string>(_maxCacheSize);
            }

            // Evict excess assets if needed
            if (_assetCache.Count > _maxCacheSize)
            {
                var excessCount = _assetCache.Count - _maxCacheSize;
                for (int i = 0; i < excessCount; i++)
                {
                    EvictLeastRecentlyUsed();
                }
            }

            if (_enableLogging)
            {
                ChimeraLogger.Log("ASSETS", $"Cache config updated: Enabled={enabled}, MaxSize={maxSize}, MaxMemory={maxMemory}");
            }
        }
        /// <summary>
        /// Get comprehensive cache summary
        /// </summary>
        public CacheSummary GetCacheSummary()
        {
            var memoryStats = GetMemoryStats();
            var topAssets = _assetCache.Values
                .OrderByDescending(a => a.AccessCount)
                .Take(10)
                .ToList();

            return new CacheSummary
            {
                Stats = _stats,
                MemoryStats = memoryStats,
                IsEnabled = _enableCaching,
                MaxSize = _maxCacheSize,
                CurrentSize = _assetCache.Count,
                TopAccessedAssets = topAssets,
                TypeCounts = _typeIndex.ToDictionary(kvp => kvp.Key, kvp => kvp.Value.Count)
            };
        }
        /// <summary>
        /// Reset cache statistics
        /// </summary>
        private void ResetStats()
        {
            _stats = new CacheStats();
        }
    }

    /// <summary>
    /// LRU Cache implementation
    /// </summary>
    }
