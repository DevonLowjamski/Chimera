#if UNITY_ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using System.Threading;
using System;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core.Assets
{
#if UNITY_ADDRESSABLES
    /// <summary>
    /// REFACTORED: Addressables Asset Service (POCO - Unity-independent core)
    /// Single Responsibility: Core asset loading, handle management, and lifecycle coordination
    /// Extracted from AddressablesAssetManagerRefactored for clean architecture compliance
    /// </summary>
    public class AddressablesAssetService
    {
        private readonly bool _enableLogging;
        private readonly bool _preloadCriticalAssets;
        private readonly int _maxCacheSize;

        private AddressablesCacheManager _cacheManager;
        private AddressablesStatisticsManager _statisticsManager;

        private readonly Dictionary<string, object> _activeHandles = new Dictionary<string, object>();
        private bool _isInitialized = false;

        private readonly string[] CRITICAL_ASSETS = {
            "CoreUI", "DefaultPlantStrain", "BasicConstructionPrefab", "ErrorAudio"
        };

        public bool IsInitialized => _isInitialized;
        public int CachedAssetCount => _cacheManager?.CachedAssetCount ?? 0;

        public AddressablesAssetService(bool enableLogging = true, bool preloadCriticalAssets = true, int maxCacheSize = 100)
        {
            _enableLogging = enableLogging;
            _preloadCriticalAssets = preloadCriticalAssets;
            _maxCacheSize = maxCacheSize;
        }

        public void InitializeComponents()
        {
            _cacheManager = new AddressablesCacheManager(_maxCacheSize, _enableLogging);
            _statisticsManager = new AddressablesStatisticsManager(100, _enableLogging);
        }

        public async Task InitializeAsync()
        {
            if (_isInitialized) return;

            try
            {
                ChimeraLogger.LogInfo("ASSETS", "Initializing Addressables system...");

                var initHandle = Addressables.InitializeAsync();
                await initHandle.Task;

                if (initHandle.IsDone && initHandle.OperationException == null)
                {
                    _isInitialized = true;
                    ChimeraLogger.LogInfo("ASSETS", "Addressables initialization successful");

                    if (_preloadCriticalAssets)
                        await PreloadCriticalAssets();
                }
                else
                {
                    ChimeraLogger.LogError("ASSETS", $"Addressables initialization failed: {initHandle.OperationException?.Message}");
                }

                Addressables.Release(initHandle);
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError("ASSETS", $"Exception during Addressables initialization: {ex.Message}");
            }
        }

        private async Task PreloadCriticalAssets()
        {
            ChimeraLogger.LogInfo("ASSETS", "Preloading critical assets...");
            int successCount = 0;

            foreach (var assetKey in CRITICAL_ASSETS)
            {
                try
                {
                    var asset = await LoadAssetAsync<UnityEngine.Object>(assetKey, 0f);
                    if (asset != null)
                    {
                        _cacheManager.MarkAsPreloaded(assetKey);
                        successCount++;
                    }
                }
                catch (Exception ex)
                {
                    ChimeraLogger.LogWarning("ASSETS", $"Failed to preload critical asset '{assetKey}': {ex.Message}");
                }
            }

            ChimeraLogger.LogInfo("ASSETS", $"Preloaded {successCount}/{CRITICAL_ASSETS.Length} critical assets");
        }

        public async Task<T> LoadAssetAsync<T>(string address, float realtimeSinceStartup) where T : UnityEngine.Object
        {
            if (!_isInitialized)
                ChimeraLogger.LogWarning("ASSETS", "Addressables not initialized, attempting to load anyway...");

            var startTime = realtimeSinceStartup;

            var cachedAsset = _cacheManager?.TryGetCachedAsset<T>(address);
            if (cachedAsset != null)
                return cachedAsset;

            try
            {
                var handle = Addressables.LoadAssetAsync<T>(address);
                _activeHandles[address] = handle;

                await handle.Task;

                if (handle.IsDone && handle.OperationException == null)
                {
                    var asset = handle.Result;
                    var loadTime = (realtimeSinceStartup - startTime) * 1000f;

                    _cacheManager?.CacheAsset(address, asset);
                    _statisticsManager?.RecordSuccessfulLoad(address, loadTime);

                    if (_enableLogging)
                        ChimeraLogger.LogInfo("ASSETS", $"Loaded via Addressables: {address}");

                    return asset;
                }
                else
                {
                    var errorMessage = handle.OperationException?.Message ?? "Unknown error";
                    _statisticsManager?.RecordFailedLoad(address, errorMessage);
                    ChimeraLogger.LogError("ASSETS", $"Failed to load asset '{address}': {errorMessage}");

                    Addressables.Release(handle);
                    _activeHandles.Remove(address);
                    return null;
                }
            }
            catch (Exception ex)
            {
                _statisticsManager?.RecordFailedLoad(address, ex.Message);
                ChimeraLogger.LogError("ASSETS", $"Exception loading '{address}': {ex.Message}");

                if (_activeHandles.TryGetValue(address, out var handle))
                {
                    Addressables.Release(handle);
                    _activeHandles.Remove(address);
                }
                return null;
            }
        }

        public T LoadAssetSync<T>(string assetPath) where T : UnityEngine.Object
        {
            try
            {
                var handle = Addressables.LoadAssetAsync<T>(assetPath);
                _activeHandles[assetPath] = handle;
                var asset = handle.WaitForCompletion();
                if (asset != null)
                {
                    _cacheManager?.CacheAsset(assetPath, asset);
                    return asset;
                }
                else
                {
                    Addressables.Release(handle);
                    _activeHandles.Remove(assetPath);
                    return null;
                }
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError("ASSETS", $"Synchronous load failed for '{assetPath}': {ex.Message}");
                return null;
            }
        }

        public async Task<IList<T>> LoadAssetsAsync<T>(IList<string> assetPaths, float realtimeSinceStartup) where T : UnityEngine.Object
        {
            var results = new List<T>(assetPaths?.Count ?? 0);
            if (assetPaths == null) return results;

            foreach (var path in assetPaths)
            {
                var asset = await LoadAssetAsync<T>(path, realtimeSinceStartup);
                if (asset != null) results.Add(asset);
            }
            return results;
        }

        public async Task<IList<T>> LoadAssetsByLabelAsync<T>(string label) where T : UnityEngine.Object
        {
            var collected = new List<T>();
            var handle = Addressables.LoadAssetsAsync<T>(label, a => { if (a != null) collected.Add(a); });
            await handle.Task;
            Addressables.Release(handle);
            return collected;
        }

        public async Task<bool> HasAssetAsync(string assetPath)
        {
            var handle = Addressables.LoadResourceLocationsAsync(assetPath);
            await handle.Task;
            var exists = handle.Result != null && handle.Result.Count > 0;
            Addressables.Release(handle);
            return exists;
        }

        public async Task PreloadAssetsAsync(string[] assetPaths, float realtimeSinceStartup)
        {
            if (assetPaths == null || assetPaths.Length == 0) return;
            foreach (var path in assetPaths)
            {
                var obj = await LoadAssetAsync<UnityEngine.Object>(path, realtimeSinceStartup);
                if (obj != null)
                    _cacheManager?.MarkAsPreloaded(path);
            }
        }

        public void ReleaseAsset(string address)
        {
            if (_activeHandles.TryGetValue(address, out var handle))
            {
                Addressables.Release(handle);
                _activeHandles.Remove(address);
                _cacheManager?.RemoveFromCache(address);
                _statisticsManager?.RecordAssetRelease(address);

                if (_enableLogging)
                    ChimeraLogger.LogInfo("ASSETS", $"Released asset: {address}");
            }
        }

        public void ReleaseAllAssets()
        {
            ChimeraLogger.LogInfo("ASSETS", $"Releasing {_activeHandles.Count} active asset handles");

            foreach (var handle in _activeHandles.Values)
                Addressables.Release(handle);

            _activeHandles.Clear();
            _cacheManager?.ClearCache();
        }

        public void UnloadAsset<T>(T asset) where T : UnityEngine.Object
        {
            if (asset == null) return;
            try
            {
                Addressables.Release(asset);
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogWarning("ASSETS", $"UnloadAsset object release failed: {ex.Message}");
            }
        }

        public bool IsAssetLoaded(string assetPath)
        {
            return _cacheManager?.TryGetCachedAsset<UnityEngine.Object>(assetPath) != null;
        }

        public AssetManagerStats GetStats()
        {
            var overallStats = _statisticsManager?.GetOverallStatistics() ?? new AssetManagerStatistics();
            var successfulLoads = overallStats.TotalLoads - overallStats.FailedLoads;
            var successRate = overallStats.TotalLoads > 0 ? (float)successfulLoads / overallStats.TotalLoads : 0f;

            return new AssetManagerStats
            {
                TotalLoadAttempts = overallStats.TotalLoads,
                SuccessfulLoads = successfulLoads,
                FailedLoads = overallStats.FailedLoads,
                CacheHits = 0, // Not tracked in AssetManagerStatistics
                CacheMisses = 0, // Not tracked in AssetManagerStatistics
                CacheEvictions = 0, // Not tracked in AssetManagerStatistics
                TotalReleases = overallStats.TotalReleases,
                TotalLoadTime = overallStats.TotalLoadTime,
                TotalMemoryUsage = GC.GetTotalMemory(false),
                AverageLoadTime = overallStats.AverageLoadTime,
                SuccessRate = successRate,
                CacheHitRate = 0f // Not tracked in AssetManagerStatistics
            };
        }

        public string GetPerformanceReport()
        {
            return _statisticsManager?.GeneratePerformanceReport() ?? "Statistics not available";
        }

        public async Task<bool> CheckForContentUpdatesAsync()
        {
            try
            {
                var catalogsToUpdate = await Addressables.CheckForCatalogUpdates().Task;

                if (catalogsToUpdate.Count > 0)
                {
                    ChimeraLogger.LogInfo("ASSETS", $"Found {catalogsToUpdate.Count} catalog updates");
                    await Addressables.UpdateCatalogs(catalogsToUpdate).Task;
                    ChimeraLogger.LogInfo("ASSETS", "Catalog updates completed");
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError("ASSETS", $"Failed to check for content updates: {ex.Message}");
                return false;
            }
        }

        public void ClearCache()
        {
            _cacheManager?.ClearCache();
        }
    }
#endif
}
