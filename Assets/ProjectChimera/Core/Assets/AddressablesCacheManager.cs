using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core.Assets
{
    /// <summary>
    /// Dedicated cache management for Addressables assets
    /// Single Responsibility: Asset caching, memory management, and cache optimization
    /// Extracted from AddressablesAssetManager for better SRP compliance
    /// </summary>
    public class AddressablesCacheManager
    {
        private readonly Dictionary<string, object> _assetCache = new Dictionary<string, object>();
        private readonly HashSet<string> _preloadedAssets = new HashSet<string>();
        private readonly int _maxCacheSize;
        private readonly bool _enableLogging;

        public int CachedAssetCount => _assetCache.Count;
        public int PreloadedAssetCount => _preloadedAssets.Count;

        public AddressablesCacheManager(int maxCacheSize = 100, bool enableLogging = false)
        {
            _maxCacheSize = maxCacheSize;
            _enableLogging = enableLogging;
        }

        /// <summary>
        /// Cache an asset for future use
        /// </summary>
        public void CacheAsset(string address, object asset)
        {
            if (asset == null) return;

            // Remove oldest cached asset if at capacity (simple FIFO)
            if (_assetCache.Count >= _maxCacheSize)
            {
                var oldestKey = "";
                foreach (var key in _assetCache.Keys)
                {
                    oldestKey = key;
                    break;
                }

                if (!string.IsNullOrEmpty(oldestKey))
                {
                    _assetCache.Remove(oldestKey);
                    if (_enableLogging)
                    {
                        ChimeraLogger.LogInfo("CACHE", $"Evicted cached asset: {oldestKey}");
                    }
                }
            }

            _assetCache[address] = asset;

            if (_enableLogging)
            {
                ChimeraLogger.LogInfo("CACHE", $"Cached asset: {address}");
            }
        }

        /// <summary>
        /// Try to get a cached asset
        /// </summary>
        public T TryGetCachedAsset<T>(string address) where T : class
        {
            if (_assetCache.TryGetValue(address, out var cachedAsset))
            {
                if (cachedAsset is T typedAsset)
                {
                    if (_enableLogging)
                    {
                        ChimeraLogger.LogInfo("CACHE", $"Cache hit: {address}");
                    }
                    return typedAsset;
                }
            }
            return null;
        }

        /// <summary>
        /// Mark an asset as preloaded
        /// </summary>
        public void MarkAsPreloaded(string address)
        {
            _preloadedAssets.Add(address);
        }

        /// <summary>
        /// Check if an asset is preloaded
        /// </summary>
        public bool IsPreloaded(string address)
        {
            return _preloadedAssets.Contains(address);
        }

        /// <summary>
        /// Remove asset from cache
        /// </summary>
        public bool RemoveFromCache(string address)
        {
            var removed = _assetCache.Remove(address);
            _preloadedAssets.Remove(address);

            if (removed && _enableLogging)
            {
                ChimeraLogger.LogInfo("CACHE", $"Removed from cache: {address}");
            }

            return removed;
        }

        /// <summary>
        /// Clear all cached assets
        /// </summary>
        public void ClearCache()
        {
            int count = _assetCache.Count;
            _assetCache.Clear();
            _preloadedAssets.Clear();

            if (_enableLogging)
            {
                ChimeraLogger.LogInfo("CACHE", $"Cleared {count} cached assets");
            }
        }

        /// <summary>
        /// Get list of non-critical assets that can be released
        /// </summary>
        public List<string> GetNonCriticalAssets()
        {
            var assetsToRelease = new List<string>();

            foreach (var address in _assetCache.Keys)
            {
                if (!_preloadedAssets.Contains(address))
                {
                    assetsToRelease.Add(address);
                }
            }

            return assetsToRelease;
        }

        /// <summary>
        /// Get cache statistics
        /// </summary>
        public CacheStatistics GetStatistics()
        {
            return new CacheStatistics
            {
                CachedAssets = _assetCache.Count,
                MaxCacheSize = _maxCacheSize,
                PreloadedAssets = _preloadedAssets.Count,
                CacheUtilization = (float)_assetCache.Count / _maxCacheSize,
                MemoryEstimate = System.GC.GetTotalMemory(false)
            };
        }
    }

    /// <summary>
    /// Cache statistics data structure
    /// </summary>
    [System.Serializable]
    public struct CacheStatistics
    {
        public int CachedAssets;
        public int MaxCacheSize;
        public int PreloadedAssets;
        public float CacheUtilization;
        public long MemoryEstimate;
    }
}