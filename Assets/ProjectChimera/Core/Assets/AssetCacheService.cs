using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;
using System;

namespace ProjectChimera.Core.Assets
{
    /// <summary>
    /// REFACTORED: Asset Cache Service (POCO - Unity-independent)
    /// Single Responsibility: Asset caching, memory management, and cache optimization
    /// Extracted from AssetCacheManager for clean architecture compliance
    /// </summary>
    public class AssetCacheService
    {
        private readonly bool _enableLogging;
        private readonly bool _enableCaching;
        private readonly int _maxCacheSize;
        private readonly long _maxCacheMemoryBytes;
        private readonly CacheEvictionStrategy _evictionStrategy;
        private readonly float _cacheCleanupInterval;
        private readonly float _unusedAssetTimeout;

        private readonly Dictionary<string, RuntimeCachedAsset> _assetCache = new Dictionary<string, RuntimeCachedAsset>();
        private readonly Dictionary<string, float> _assetAccessTimes = new Dictionary<string, float>();
        private readonly Dictionary<string, int> _assetAccessCounts = new Dictionary<string, int>();
        private readonly Dictionary<string, long> _assetMemoryUsage = new Dictionary<string, long>();

        private long _currentCacheMemoryUsage = 0;
        private float _lastCleanupTime = 0f;
        private AssetCacheManagerStats _stats = new AssetCacheManagerStats();
        private bool _isInitialized = false;

        public event Action<string, object> OnAssetCached;
        public event Action<string> OnAssetEvicted;
        public event Action<CacheCleanupResult> OnCacheCleanup;

        public bool IsInitialized => _isInitialized;
        public bool IsCachingEnabled => _enableCaching;
        public AssetCacheManagerStats Stats => _stats;
        public int CachedAssetCount => _assetCache.Count;
        public long CacheMemoryUsage => _currentCacheMemoryUsage;
        public float CacheUtilization => _maxCacheSize > 0 ? (float)_assetCache.Count / _maxCacheSize : 0f;
        public float MemoryUtilization => _maxCacheMemoryBytes > 0 ? (float)_currentCacheMemoryUsage / _maxCacheMemoryBytes : 0f;

        public AssetCacheService(
            bool enableLogging = false,
            bool enableCaching = true,
            int maxCacheSize = 100,
            long maxCacheMemoryBytes = 536870912,
            CacheEvictionStrategy evictionStrategy = CacheEvictionStrategy.LRU,
            float cacheCleanupInterval = 60f,
            float unusedAssetTimeout = 300f)
        {
            _enableLogging = enableLogging;
            _enableCaching = enableCaching;
            _maxCacheSize = maxCacheSize;
            _maxCacheMemoryBytes = maxCacheMemoryBytes;
            _evictionStrategy = evictionStrategy;
            _cacheCleanupInterval = cacheCleanupInterval;
            _unusedAssetTimeout = unusedAssetTimeout;
        }

        public void Initialize()
        {
            if (_isInitialized) return;
            _assetCache.Clear();
            _assetAccessTimes.Clear();
            _assetAccessCounts.Clear();
            _assetMemoryUsage.Clear();
            _currentCacheMemoryUsage = 0;
            ResetStats();
            _isInitialized = true;
            if (_enableLogging)
                ChimeraLogger.Log("ASSETS", "Asset Cache Service initialized");
        }

        public bool CacheAsset(string address, object asset, float currentTime)
        {
            if (!_isInitialized || !_enableCaching || string.IsNullOrEmpty(address) || asset == null)
                return false;

            try
            {
                long assetMemory = EstimateAssetMemoryUsage(asset);
                if (_currentCacheMemoryUsage + assetMemory > _maxCacheMemoryBytes)
                {
                    if (!FreeMemoryForAsset(assetMemory))
                    {
                        _stats.CacheEvictions++;
                        if (_enableLogging)
                            ChimeraLogger.LogWarning("ASSETS", $"Cannot cache {address} - insufficient memory");
                        return false;
                    }
                }

                if (_assetCache.Count >= _maxCacheSize)
                {
                    if (!EvictAssetsByStrategy(1))
                    {
                        _stats.CacheEvictions++;
                        if (_enableLogging)
                            ChimeraLogger.LogWarning("ASSETS", $"Cannot cache {address} - cache full");
                        return false;
                    }
                }

                var cachedAsset = new RuntimeCachedAsset
                {
                    Address = address,
                    Asset = asset,
                    CacheTime = currentTime,
                    LastAccessTime = currentTime,
                    AccessCount = 1,
                    MemoryUsage = assetMemory
                };

                _assetCache[address] = cachedAsset;
                _assetAccessTimes[address] = currentTime;
                _assetAccessCounts[address] = 1;
                _assetMemoryUsage[address] = assetMemory;
                _currentCacheMemoryUsage += assetMemory;
                _stats.AssetsCached++;
                _stats.TotalMemoryCached += assetMemory;

                OnAssetCached?.Invoke(address, asset);
                if (_enableLogging)
                    ChimeraLogger.Log("ASSETS", $"Cached asset {address} ({FormatBytes(assetMemory)})");
                return true;
            }
            catch (Exception ex)
            {
                if (_enableLogging)
                    ChimeraLogger.LogError("ASSETS", $"Error caching {address}: {ex.Message}");
                return false;
            }
        }

        public T GetCachedAsset<T>(string address, float currentTime) where T : class
        {
            if (!_isInitialized || !_enableCaching || !_assetCache.TryGetValue(address, out var cachedAsset))
                return null;

            cachedAsset.LastAccessTime = currentTime;
            cachedAsset.AccessCount++;
            _assetCache[address] = cachedAsset;
            _assetAccessTimes[address] = currentTime;
            _assetAccessCounts[address] = cachedAsset.AccessCount;
            _stats.CacheHits++;
            return cachedAsset.Asset as T;
        }

        public bool IsCached(string address) => _assetCache.ContainsKey(address);

        public bool RemoveFromCache(string address)
        {
            if (!_assetCache.ContainsKey(address))
                return false;

            if (_assetMemoryUsage.TryGetValue(address, out long memory))
            {
                _currentCacheMemoryUsage -= memory;
                _assetMemoryUsage.Remove(address);
            }

            _assetCache.Remove(address);
            _assetAccessTimes.Remove(address);
            _assetAccessCounts.Remove(address);
            _stats.AssetsRemoved++;

            OnAssetEvicted?.Invoke(address);
            if (_enableLogging)
                ChimeraLogger.Log("ASSETS", $"Removed from cache: {address}");
            return true;
        }

        public void ClearCache()
        {
            int count = _assetCache.Count;
            _assetCache.Clear();
            _assetAccessTimes.Clear();
            _assetAccessCounts.Clear();
            _assetMemoryUsage.Clear();
            _currentCacheMemoryUsage = 0;
            if (_enableLogging)
                ChimeraLogger.Log("ASSETS", $"Cleared cache ({count} assets)");
        }

        public CacheCleanupResult PerformCacheCleanup(float currentTime)
        {
            if (!_isInitialized || !_enableCaching)
                return new CacheCleanupResult();

            var result = new CacheCleanupResult { StartTime = currentTime };
            var assetsToEvict = _assetCache
                .Where(kvp => currentTime - kvp.Value.LastAccessTime > _unusedAssetTimeout)
                .Select(kvp => kvp.Key)
                .ToList();

            foreach (var address in assetsToEvict)
            {
                if (RemoveFromCache(address))
                    result.AssetsEvicted++;
            }

            _lastCleanupTime = currentTime;
            result.EndTime = currentTime;
            result.Success = true;

            OnCacheCleanup?.Invoke(result);
            if (_enableLogging && result.AssetsEvicted > 0)
                ChimeraLogger.Log("ASSETS", $"Cache cleanup: evicted {result.AssetsEvicted} assets");
            return result;
        }

        public bool ShouldPerformCleanup(float currentTime) =>
            _enableCaching && currentTime - _lastCleanupTime >= _cacheCleanupInterval;

        public RuntimeCachedAsset[] GetCachedAssets() => _assetCache.Values.ToArray();

        #region Private Methods

        private bool FreeMemoryForAsset(long requiredMemory)
        {
            long freedMemory = 0;
            var assetsToEvict = GetAssetsToEvict();

            foreach (var address in assetsToEvict)
            {
                if (_assetMemoryUsage.TryGetValue(address, out long memory))
                {
                    RemoveFromCache(address);
                    freedMemory += memory;
                    if (freedMemory >= requiredMemory)
                        return true;
                }
            }
            return freedMemory >= requiredMemory;
        }

        private bool EvictAssetsByStrategy(int count)
        {
            var assetsToEvict = GetAssetsToEvict().Take(count).ToList();
            foreach (var address in assetsToEvict)
                RemoveFromCache(address);
            return assetsToEvict.Count > 0;
        }

        private List<string> GetAssetsToEvict()
        {
            switch (_evictionStrategy)
            {
                case CacheEvictionStrategy.LRU:
                    return _assetAccessTimes.OrderBy(kvp => kvp.Value).Select(kvp => kvp.Key).ToList();
                case CacheEvictionStrategy.LFU:
                    return _assetAccessCounts.OrderBy(kvp => kvp.Value).Select(kvp => kvp.Key).ToList();
                case CacheEvictionStrategy.FIFO:
                    return _assetCache.OrderBy(kvp => kvp.Value.CacheTime).Select(kvp => kvp.Key).ToList();
                case CacheEvictionStrategy.Random:
                    return _assetCache.Keys.OrderBy(_ => Guid.NewGuid()).ToList();
                default:
                    return _assetCache.Keys.ToList();
            }
        }

        private long EstimateAssetMemoryUsage(object asset)
        {
            if (asset == null) return 0;
            if (asset is UnityEngine.Object unityObj)
            {
                if (unityObj is UnityEngine.Texture texture)
                    return texture.width * texture.height * 4;
                if (unityObj is UnityEngine.Mesh mesh)
                    return mesh.vertexCount * 64;
                if (unityObj is UnityEngine.GameObject)
                    return 1024;
                if (unityObj is UnityEngine.ScriptableObject)
                    return 512;
            }
            return 256;
        }

        private string FormatBytes(long bytes)
        {
            if (bytes < 1024) return $"{bytes} B";
            if (bytes < 1024 * 1024) return $"{bytes / 1024f:F1} KB";
            if (bytes < 1024 * 1024 * 1024) return $"{bytes / (1024f * 1024f):F1} MB";
            return $"{bytes / (1024f * 1024f * 1024f):F1} GB";
        }

        private void ResetStats()
        {
            _stats = new AssetCacheManagerStats();
        }

        #endregion
    }

    public enum CacheEvictionStrategy { LRU, LFU, FIFO, Random }

    [Serializable]
    public struct RuntimeCachedAsset
    {
        public string Address;
        public object Asset;
        public float CacheTime;
        public float LastAccessTime;
        public int AccessCount;
        public long MemoryUsage;
    }

    [Serializable]
    public struct CacheCleanupResult
    {
        public float StartTime;
        public float EndTime;
        public int AssetsEvicted;
        public bool Success;
    }

    [Serializable]
    public struct AssetCacheManagerStats
    {
        public int AssetsCached;
        public int AssetsRemoved;
        public int CacheHits;
        public int CacheMisses;
        public int CacheEvictions;
        public long TotalMemoryCached;
    }
}
