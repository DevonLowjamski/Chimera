using UnityEngine;
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
using Logger = ProjectChimera.Core.Logging.ChimeraLogger;

namespace ProjectChimera.Core.Assets
{
#if UNITY_ADDRESSABLES
    /// <summary>
    /// REFACTORED: Core Addressables functionality - Asset loading and initialization
    /// Part 1 of 2: Loading, initialization, and basic operations
    /// Kept under 500 lines per clean architecture guidelines
    /// </summary>
    public partial class AddressablesService : IAssetManager
    {
        // Configuration
        private readonly bool _enableCaching;
        private readonly bool _enableLogging;
        private readonly int _maxCacheSize;
        private readonly bool _preloadCriticalAssets;

        // Store Addressables handles as object to avoid hard dependency on handle type
        private readonly Dictionary<string, object> _activeHandles = new Dictionary<string, object>();
        private readonly Dictionary<string, AssetCacheEntry> _assetCache = new Dictionary<string, AssetCacheEntry>();
        private readonly HashSet<string> _preloadedAssets = new HashSet<string>();

        private bool _isInitialized = false;

        public bool IsInitialized => _isInitialized;
        public int CachedAssetCount => _assetCache.Count;
        public long CacheMemoryUsage => GC.GetTotalMemory(false);

        /// <summary>
        /// Critical assets to preload at startup
        /// </summary>
        private readonly string[] CRITICAL_ASSETS = {
            "CoreUI",
            "DefaultPlantStrain",
            "BasicConstructionPrefab",
            "ErrorAudio"
        };

        public AddressablesService(bool enableCaching = true, bool enableLogging = true, int maxCacheSize = 100, bool preloadCriticalAssets = true)
        {
            _enableCaching = enableCaching;
            _enableLogging = enableLogging;
            _maxCacheSize = maxCacheSize;
            _preloadCriticalAssets = preloadCriticalAssets;
        }

        #region Initialization

        public async Task InitializeAsync()
        {
            if (_isInitialized) return;

            try
            {
                Logger.Log("ASSETS", "Initializing Addressables system...");

                var initHandle = Addressables.InitializeAsync();
                await initHandle.Task;

                if (initHandle.IsDone && initHandle.OperationException == null)
                {
                    _isInitialized = true;
                    Logger.Log("ASSETS", "Addressables initialization successful");

                    if (_preloadCriticalAssets)
                    {
                        await PreloadCriticalAssets();
                    }
                }
                else
                {
                    Logger.LogError("ASSETS", $"Addressables initialization failed: {initHandle.OperationException?.Message}");
                }

                Addressables.Release(initHandle);
            }
            catch (Exception ex)
            {
                Logger.LogError("ASSETS", $"Exception during Addressables initialization: {ex.Message}");
            }
        }

        public void Initialize()
        {
            if (!_isInitialized)
            {
                _ = InitializeAsync();
            }
        }

        public Task InitializeAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.CompletedTask;
            }
            return InitializeAsync();
        }

        private async Task PreloadCriticalAssets()
        {
            Logger.Log("ASSETS", "Preloading critical assets...");
            int successCount = 0;

            foreach (var assetKey in CRITICAL_ASSETS)
            {
                try
                {
                    var asset = await LoadAssetAsync<UnityEngine.Object>(assetKey);
                    if (asset != null)
                    {
                        _preloadedAssets.Add(assetKey);
                        successCount++;
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogWarning("ASSETS", $"Failed to preload critical asset '{assetKey}': {ex.Message}");
                }
            }

            Logger.Log("ASSETS", $"Preloaded {successCount}/{CRITICAL_ASSETS.Length} critical assets");
        }

        #endregion

        #region Asset Loading (Async)

        public async Task<T> LoadAssetAsync<T>(string address) where T : UnityEngine.Object
        {
            if (!_isInitialized)
            {
                Logger.LogWarning("ASSETS", "Addressables not initialized, attempting to load anyway...");
            }

            // Check cache first
            if (_enableCaching && _assetCache.TryGetValue(address, out var cacheEntry))
            {
                if (cacheEntry.Asset is T typedAsset)
                {
                    cacheEntry.LastAccessed = DateTime.Now;
                    cacheEntry.ReferenceCount++;
                    if (_enableLogging)
                        Logger.Log("ASSETS", $"Cache hit: {address}");
                    return typedAsset;
                }
            }

            try
            {
                var handle = Addressables.LoadAssetAsync<T>(address);
                _activeHandles[address] = handle;

                await handle.Task;

                if (handle.IsDone && handle.OperationException == null)
                {
                    var asset = handle.Result;

                    if (_enableCaching)
                    {
                        CacheAsset(address, asset);
                    }

                    if (_enableLogging)
                        Logger.Log("ASSETS", $"Loaded via Addressables: {address}");

                    return asset;
                }
                else
                {
                    Logger.LogError("ASSETS", $"Failed to load asset '{address}': {handle.OperationException?.Message}");
                    Addressables.Release(handle);
                    _activeHandles.Remove(address);
                    return null;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("ASSETS", $"Exception loading '{address}': {ex.Message}");
                if (_activeHandles.TryGetValue(address, out var handle))
                {
                    Addressables.Release(handle);
                    _activeHandles.Remove(address);
                }
                return null;
            }
        }

        public Task<T> LoadAssetAsync<T>(string assetPath, AssetLoadPriority priority) where T : UnityEngine.Object
        {
            return LoadAssetAsync<T>(assetPath);
        }

        public Task<T> LoadAssetAsync<T>(string assetPath, CancellationToken cancellationToken) where T : UnityEngine.Object
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromResult<T>(null);
            }
            return LoadAssetAsync<T>(assetPath);
        }

        public Task<T> LoadAssetAsync<T>(string assetPath, AssetLoadPriority priority, CancellationToken cancellationToken) where T : UnityEngine.Object
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return Task.FromResult<T>(null);
            }
            return LoadAssetAsync<T>(assetPath, priority);
        }

        public async void LoadAssetAsync<T>(string assetPath, Action<T> onComplete, Action<string> onError = null) where T : UnityEngine.Object
        {
            try
            {
                var asset = await LoadAssetAsync<T>(assetPath);
                if (asset != null)
                {
                    onComplete?.Invoke(asset);
                }
                else
                {
                    onError?.Invoke($"Asset not found: {assetPath}");
                }
            }
            catch (Exception ex)
            {
                onError?.Invoke($"Error loading asset: {ex.Message}");
            }
        }

        public async Task<IList<T>> LoadAssetsAsync<T>(IList<string> addresses) where T : UnityEngine.Object
        {
            var tasks = addresses.Select(addr => LoadAssetAsync<T>(addr));
            var results = await Task.WhenAll(tasks);
            return results.Where(r => r != null).ToList();
        }

        public async Task<IList<T>> LoadAssetsAsync<T>(IList<string> addresses, CancellationToken cancellationToken) where T : UnityEngine.Object
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return new List<T>();
            }
            return await LoadAssetsAsync<T>(addresses);
        }

        public async Task<IList<T>> LoadAssetsByLabelAsync<T>(string label) where T : UnityEngine.Object
        {
            try
            {
                var handle = Addressables.LoadAssetsAsync<T>(label, null);
                await handle.Task;

                if (handle.IsDone && handle.OperationException == null)
                {
                    return handle.Result.ToList();
                }
                else
                {
                    Logger.LogError("ASSETS", $"Failed to load assets by label '{label}': {handle.OperationException?.Message}");
                    Addressables.Release(handle);
                    return new List<T>();
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("ASSETS", $"Exception loading assets by label '{label}': {ex.Message}");
                return new List<T>();
            }
        }

        public async Task<IList<T>> LoadAssetsByLabelAsync<T>(string label, CancellationToken cancellationToken) where T : UnityEngine.Object
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return new List<T>();
            }
            return await LoadAssetsByLabelAsync<T>(label);
        }

        [Obsolete("Use LoadAssetAsync instead for better performance")]
        public T LoadAsset<T>(string assetPath) where T : UnityEngine.Object
        {
            Logger.LogWarning("ASSETS", "Synchronous LoadAsset is deprecated. Use LoadAssetAsync instead.");
            return LoadAssetAsync<T>(assetPath).Result;
        }

        #endregion

        #region Asset Validation

        public async Task<bool> HasAssetAsync(string address)
        {
            try
            {
                var locations = Addressables.LoadResourceLocationsAsync(address);
                await locations.Task;
                bool exists = locations.Result.Count > 0;
                Addressables.Release(locations);
                return exists;
            }
            catch
            {
                return false;
            }
        }

        public async Task<bool> HasAssetAsync(string address, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }
            return await HasAssetAsync(address);
        }

        public bool IsAssetLoaded(string assetPath)
        {
            return _assetCache.ContainsKey(assetPath);
        }

        #endregion

        #region Preloading

        public void PreloadAssets(string[] assetPaths)
        {
            _ = PreloadAssetsAsync(assetPaths);
        }

        public async Task PreloadAssetsAsync(string[] assetPaths)
        {
            foreach (var path in assetPaths)
            {
                await LoadAssetAsync<UnityEngine.Object>(path);
            }
        }

        public async Task PreloadAssetsAsync(string[] assetPaths, CancellationToken cancellationToken)
        {
            foreach (var path in assetPaths)
            {
                if (cancellationToken.IsCancellationRequested)
                    return;

                await LoadAssetAsync<UnityEngine.Object>(path);
            }
        }

        #endregion

        #region Content Updates

        public async Task<bool> CheckForContentUpdatesAsync()
        {
            try
            {
                var checkHandle = Addressables.CheckForCatalogUpdates();
                await checkHandle.Task;

                if (checkHandle.IsDone && checkHandle.OperationException == null)
                {
                    bool hasUpdates = checkHandle.Result.Count > 0;
                    Addressables.Release(checkHandle);
                    return hasUpdates;
                }
                else
                {
                    Logger.LogError("ASSETS", $"Failed to check for updates: {checkHandle.OperationException?.Message}");
                    Addressables.Release(checkHandle);
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("ASSETS", $"Exception checking for updates: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> CheckForContentUpdatesAsync(CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return false;
            }
            return await CheckForContentUpdatesAsync();
        }

        #endregion

        private void CacheAsset(string address, object asset)
        {
            if (_assetCache.Count >= _maxCacheSize)
            {
                var firstKey = _assetCache.Keys.FirstOrDefault();
                if (firstKey != null && !_preloadedAssets.Contains(firstKey))
                {
                    _assetCache.Remove(firstKey);
                    if (_enableLogging)
                        Logger.Log("ASSETS", $"Cache full, evicted: {firstKey}");
                }
            }

            _assetCache[address] = new AssetCacheEntry
            {
                Asset = asset as UnityEngine.Object,
                AssetPath = address,
                AssetType = asset.GetType(),
                IsPersistent = _preloadedAssets.Contains(address),
                LastAccessed = DateTime.Now,
                ReferenceCount = 1
            };
        }
    }
#else
    /// <summary>
    /// Stub implementation when Addressables is not available - Core part
    /// </summary>
    public partial class AddressablesService : IAssetManager
    {
        private readonly bool _enableCaching;
        private readonly bool _enableLogging;
        private readonly int _maxCacheSize;
        private readonly bool _preloadCriticalAssets;

        public bool IsInitialized => false;
        public int CachedAssetCount => 0;
        public long CacheMemoryUsage => 0;

        public AddressablesService(bool enableCaching = true, bool enableLogging = true, int maxCacheSize = 100, bool preloadCriticalAssets = true)
        {
            _enableCaching = enableCaching;
            _enableLogging = enableLogging;
            _maxCacheSize = maxCacheSize;
            _preloadCriticalAssets = preloadCriticalAssets;
        }

        public void Initialize() { }
        public Task InitializeAsync() => Task.CompletedTask;
        public Task InitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<T> LoadAssetAsync<T>(string address) where T : UnityEngine.Object => Task.FromResult<T>(null);
        public Task<T> LoadAssetAsync<T>(string assetPath, AssetLoadPriority priority) where T : UnityEngine.Object => Task.FromResult<T>(null);
        public Task<T> LoadAssetAsync<T>(string assetPath, CancellationToken cancellationToken) where T : UnityEngine.Object => Task.FromResult<T>(null);
        public Task<T> LoadAssetAsync<T>(string assetPath, AssetLoadPriority priority, CancellationToken cancellationToken) where T : UnityEngine.Object => Task.FromResult<T>(null);
        public void LoadAssetAsync<T>(string assetPath, Action<T> onComplete, Action<string> onError = null) where T : UnityEngine.Object => onError?.Invoke("Addressables not available");
        public Task<IList<T>> LoadAssetsAsync<T>(IList<string> addresses) where T : UnityEngine.Object => Task.FromResult<IList<T>>(new List<T>());
        public Task<IList<T>> LoadAssetsAsync<T>(IList<string> addresses, CancellationToken cancellationToken) where T : UnityEngine.Object => Task.FromResult<IList<T>>(new List<T>());
        public Task<IList<T>> LoadAssetsByLabelAsync<T>(string label) where T : UnityEngine.Object => Task.FromResult<IList<T>>(new List<T>());
        public Task<IList<T>> LoadAssetsByLabelAsync<T>(string label, CancellationToken cancellationToken) where T : UnityEngine.Object => Task.FromResult<IList<T>>(new List<T>());
        [Obsolete("Use LoadAssetAsync instead")]
        public T LoadAsset<T>(string assetPath) where T : UnityEngine.Object => null;
        public Task<bool> HasAssetAsync(string address) => Task.FromResult(false);
        public Task<bool> HasAssetAsync(string address, CancellationToken cancellationToken) => Task.FromResult(false);
        public bool IsAssetLoaded(string assetPath) => false;
        public void PreloadAssets(string[] assetPaths) { }
        public Task PreloadAssetsAsync(string[] assetPaths) => Task.CompletedTask;
        public Task PreloadAssetsAsync(string[] assetPaths, CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<bool> CheckForContentUpdatesAsync() => Task.FromResult(false);
        public Task<bool> CheckForContentUpdatesAsync(CancellationToken cancellationToken) => Task.FromResult(false);
    }
#endif
}
