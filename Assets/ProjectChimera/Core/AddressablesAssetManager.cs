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

namespace ProjectChimera.Core
{
#if UNITY_ADDRESSABLES
    /// <summary>
    /// DEPRECATED: This MonoBehaviour-based implementation violates clean architecture
    /// Use ProjectChimera.Core.Assets.AddressablesService (POCO) instead
    /// Use ProjectChimera.Systems.Assets.AddressablesManagerBridge (MonoBehaviour wrapper) for Unity lifecycle
    ///
    /// REFACTORED: Core layer purification - MonoBehaviour extracted to Systems layer
    /// This file is kept temporarily for backward compatibility and will be removed in Phase 1
    /// </summary>
    [System.Obsolete("Use AddressablesService (Core.Assets) + AddressablesManagerBridge (Systems.Assets) instead")]
    public class AddressablesAssetManager : MonoBehaviour, IAssetManager
    {
        [Header("⚠️ DEPRECATED - Use AddressablesManagerBridge instead")]
        [SerializeField] private bool _enableCaching = true;
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private int _maxCacheSize = 100;
        [SerializeField] private bool _preloadCriticalAssets = true;

        // Forward to new implementation via ServiceContainer
        private IAssetManager _service;

        // Singleton pattern (DEPRECATED)
        private static AddressablesAssetManager _instance;
        public static AddressablesAssetManager Instance => _instance;

        public bool IsInitialized => _service?.IsInitialized ?? false;
        public int CachedAssetCount => _service?.CachedAssetCount ?? 0;
        public long CacheMemoryUsage => _service?.CacheMemoryUsage ?? 0;

        #region Unity Lifecycle

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);

                Logger.LogWarning("ASSETS",
                    "⚠️ AddressablesAssetManager is DEPRECATED. Please use AddressablesManagerBridge instead.",
                    this);

                // Create forwarding service
                var service = new Assets.AddressablesService(_enableCaching, _enableLogging, _maxCacheSize, _preloadCriticalAssets);
                _service = service;
                _ = service.InitializeAsync();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy()
        {
            if (_service is Assets.AddressablesService service)
            {
                service.Cleanup();
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && _service is Assets.AddressablesService service)
            {
                service.ReleaseNonCriticalAssets();
            }
        }

        #endregion

        #region Forwarding Methods (All delegate to AddressablesService)

        public void Initialize() => _service?.Initialize();
        public Task InitializeAsync() => _service?.InitializeAsync() ?? Task.CompletedTask;
        public Task InitializeAsync(CancellationToken cancellationToken) => _service?.InitializeAsync(cancellationToken) ?? Task.CompletedTask;

        public Task<T> LoadAssetAsync<T>(string address) where T : UnityEngine.Object
            => _service?.LoadAssetAsync<T>(address) ?? Task.FromResult<T>(null);

        public Task<T> LoadAssetAsync<T>(string assetPath, AssetLoadPriority priority) where T : UnityEngine.Object
            => _service?.LoadAssetAsync<T>(assetPath, priority) ?? Task.FromResult<T>(null);

        public Task<T> LoadAssetAsync<T>(string assetPath, CancellationToken cancellationToken) where T : UnityEngine.Object
            => _service?.LoadAssetAsync<T>(assetPath, cancellationToken) ?? Task.FromResult<T>(null);

        public Task<T> LoadAssetAsync<T>(string assetPath, AssetLoadPriority priority, CancellationToken cancellationToken) where T : UnityEngine.Object
            => _service?.LoadAssetAsync<T>(assetPath, priority, cancellationToken) ?? Task.FromResult<T>(null);

        public void LoadAssetAsync<T>(string assetPath, Action<T> onComplete, Action<string> onError = null) where T : UnityEngine.Object
            => _service?.LoadAssetAsync(assetPath, onComplete, onError);

        public Task<IList<T>> LoadAssetsAsync<T>(IList<string> addresses) where T : UnityEngine.Object
            => _service?.LoadAssetsAsync<T>(addresses) ?? Task.FromResult<IList<T>>(new List<T>());

        public Task<IList<T>> LoadAssetsAsync<T>(IList<string> addresses, CancellationToken cancellationToken) where T : UnityEngine.Object
            => _service?.LoadAssetsAsync<T>(addresses, cancellationToken) ?? Task.FromResult<IList<T>>(new List<T>());

        public Task<IList<T>> LoadAssetsByLabelAsync<T>(string label) where T : UnityEngine.Object
            => _service?.LoadAssetsByLabelAsync<T>(label) ?? Task.FromResult<IList<T>>(new List<T>());

        public Task<IList<T>> LoadAssetsByLabelAsync<T>(string label, CancellationToken cancellationToken) where T : UnityEngine.Object
            => _service?.LoadAssetsByLabelAsync<T>(label, cancellationToken) ?? Task.FromResult<IList<T>>(new List<T>());

        public void ReleaseAsset(string address)
        {
            if (_service is Assets.AddressablesService service)
                service.ReleaseAsset(address);
            else
                _service?.UnloadAsset(address);
        }

        public void ReleaseAllAssets()
        {
            if (_service is Assets.AddressablesService service)
                service.ReleaseAllAssets();
            else
                _service?.ClearCache();
        }
        public void UnloadAsset(string assetPath) => _service?.UnloadAsset(assetPath);
        public void UnloadAsset<T>(T asset) where T : UnityEngine.Object => _service?.UnloadAsset(asset);
        public void PreloadAssets(string[] assetPaths) => _service?.PreloadAssets(assetPaths);

        public Task PreloadAssetsAsync(string[] assetPaths)
            => _service?.PreloadAssetsAsync(assetPaths) ?? Task.CompletedTask;

        public Task PreloadAssetsAsync(string[] assetPaths, CancellationToken cancellationToken)
            => _service?.PreloadAssetsAsync(assetPaths, cancellationToken) ?? Task.CompletedTask;

        public void ClearCache() => _service?.ClearCache();
        public void ClearCache(bool persistentOnly) => _service?.ClearCache(persistentOnly);

        public Task<bool> HasAssetAsync(string address)
            => _service?.HasAssetAsync(address) ?? Task.FromResult(false);

        public Task<bool> HasAssetAsync(string address, CancellationToken cancellationToken)
            => _service?.HasAssetAsync(address, cancellationToken) ?? Task.FromResult(false);

        public Task<bool> CheckForContentUpdates()
            => _service?.CheckForContentUpdatesAsync() ?? Task.FromResult(false);

        public Task<bool> CheckForContentUpdatesAsync()
            => _service?.CheckForContentUpdatesAsync() ?? Task.FromResult(false);

        public Task<bool> CheckForContentUpdatesAsync(CancellationToken cancellationToken)
            => _service?.CheckForContentUpdatesAsync(cancellationToken) ?? Task.FromResult(false);

        [System.Obsolete("Use LoadAssetAsync instead for better performance")]
        public T LoadAsset<T>(string assetPath) where T : UnityEngine.Object
            => _service?.LoadAsset<T>(assetPath);

        public bool IsAssetLoaded(string assetPath)
            => _service?.IsAssetLoaded(assetPath) ?? false;

        public AssetCacheEntry[] GetCacheEntries()
            => _service?.GetCacheEntries() ?? new AssetCacheEntry[0];

        #endregion
    }
#else
    /// <summary>
    /// Stub when Addressables not available
    /// </summary>
    [System.Obsolete("Use AddressablesService instead")]
    public class AddressablesAssetManager : MonoBehaviour, IAssetManager
    {
        public bool IsInitialized => false;
        public int CachedAssetCount => 0;
        public long CacheMemoryUsage => 0;

        private void Awake()
        {
            Logger.LogWarning("ASSETS", "Addressables not available in this build");
        }

        public void Initialize() { }
        public Task InitializeAsync() => Task.CompletedTask;
        public Task InitializeAsync(CancellationToken cancellationToken) => Task.CompletedTask;
        public Task<T> LoadAssetAsync<T>(string address) where T : UnityEngine.Object => Task.FromResult<T>(null);
        public Task<T> LoadAssetAsync<T>(string assetPath, AssetLoadPriority priority) where T : UnityEngine.Object => Task.FromResult<T>(null);
        public Task<T> LoadAssetAsync<T>(string assetPath, CancellationToken cancellationToken) where T : UnityEngine.Object => Task.FromResult<T>(null);
        public Task<T> LoadAssetAsync<T>(string assetPath, AssetLoadPriority priority, CancellationToken cancellationToken) where T : UnityEngine.Object => Task.FromResult<T>(null);
        public void LoadAssetAsync<T>(string assetPath, Action<T> onComplete, Action<string> onError = null) where T : UnityEngine.Object { }
        public Task<IList<T>> LoadAssetsAsync<T>(IList<string> addresses) where T : UnityEngine.Object => Task.FromResult<IList<T>>(new List<T>());
        public Task<IList<T>> LoadAssetsAsync<T>(IList<string> addresses, CancellationToken cancellationToken) where T : UnityEngine.Object => Task.FromResult<IList<T>>(new List<T>());
        public Task<IList<T>> LoadAssetsByLabelAsync<T>(string label) where T : UnityEngine.Object => Task.FromResult<IList<T>>(new List<T>());
        public Task<IList<T>> LoadAssetsByLabelAsync<T>(string label, CancellationToken cancellationToken) where T : UnityEngine.Object => Task.FromResult<IList<T>>(new List<T>());
        public void ReleaseAsset(string address) { }
        public void ReleaseAllAssets() { }
        public void UnloadAsset(string assetPath) { }
        public void UnloadAsset<T>(T asset) where T : UnityEngine.Object { }
        public void PreloadAssets(string[] assetPaths) { }
        public Task PreloadAssetsAsync(string[] assetPaths) => Task.CompletedTask;
        public Task PreloadAssetsAsync(string[] assetPaths, CancellationToken cancellationToken) => Task.CompletedTask;
        public void ClearCache() { }
        public void ClearCache(bool persistentOnly) { }
        public Task<bool> HasAssetAsync(string address) => Task.FromResult(false);
        public Task<bool> HasAssetAsync(string address, CancellationToken cancellationToken) => Task.FromResult(false);
        public Task<bool> CheckForContentUpdatesAsync() => Task.FromResult(false);
        public Task<bool> CheckForContentUpdatesAsync(CancellationToken cancellationToken) => Task.FromResult(false);
        [System.Obsolete("Use LoadAssetAsync instead")]
        public T LoadAsset<T>(string assetPath) where T : UnityEngine.Object => null;
        public bool IsAssetLoaded(string assetPath) => false;
        public AssetCacheEntry[] GetCacheEntries() => new AssetCacheEntry[0];
    }
#endif
}
