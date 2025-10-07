using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System;
using ProjectChimera.Core.Assets;

namespace ProjectChimera.Core
{
#if UNITY_ADDRESSABLES
    /// <summary>
    /// DEPRECATED: Use AddressablesAssetService (Core.Assets) + AddressablesAssetManagerBridge (Systems.Assets) instead
    /// This wrapper maintained for backward compatibility during migration
    /// Architecture violation: MonoBehaviour in Core layer
    /// </summary>
    [System.Obsolete("Use AddressablesAssetService (Core.Assets) + AddressablesAssetManagerBridge (Systems.Assets) instead")]
    public class AddressablesAssetManagerRefactored : MonoBehaviour, IAssetManager
    {
        [Header("Core Configuration")]
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private bool _preloadCriticalAssets = true;
        [SerializeField] private int _maxCacheSize = 100;

        private AddressablesAssetService _service;
        private static AddressablesAssetManagerRefactored _instance;

        public static AddressablesAssetManagerRefactored Instance => _instance;
        public bool IsInitialized => _service?.IsInitialized ?? false;
        public int CachedAssetCount => _service?.CachedAssetCount ?? 0;
        public long CacheMemoryUsage => GC.GetTotalMemory(false);

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                _service = new AddressablesAssetService(_enableLogging, _preloadCriticalAssets, _maxCacheSize);
                _service.InitializeComponents();
                _ = _service.InitializeAsync();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void OnDestroy() => _service?.ReleaseAllAssets();

        public void Initialize() => _service?.InitializeAsync();
        public Task InitializeAsync() => _service?.InitializeAsync() ?? Task.CompletedTask;
        public Task InitializeAsync(CancellationToken cancellationToken) => InitializeAsync();
        public async Task<T> LoadAssetAsync<T>(string address) where T : UnityEngine.Object => await (_service?.LoadAssetAsync<T>(address, Time.realtimeSinceStartup) ?? Task.FromResult<T>(null));
        public T LoadAsset<T>(string assetPath) where T : UnityEngine.Object => _service?.LoadAssetSync<T>(assetPath);
        public Task<T> LoadAssetAsync<T>(string assetPath, AssetLoadPriority priority) where T : UnityEngine.Object => LoadAssetAsync<T>(assetPath);
        public Task<T> LoadAssetAsync<T>(string assetPath, CancellationToken cancellationToken) where T : UnityEngine.Object => LoadAssetAsync<T>(assetPath);
        public Task<T> LoadAssetAsync<T>(string assetPath, AssetLoadPriority priority, CancellationToken cancellationToken) where T : UnityEngine.Object => LoadAssetAsync<T>(assetPath);
        public void LoadAssetAsync<T>(string assetPath, Action<T> onComplete, Action<string> onError = null) where T : UnityEngine.Object
        {
            _ = LoadAssetAsync<T>(assetPath).ContinueWith(t =>
            {
                if (t.IsFaulted) onError?.Invoke(t.Exception?.GetBaseException().Message ?? "Load failed");
                else
                {
                    var result = t.Result;
                    if (result != null) onComplete?.Invoke(result);
                    else onError?.Invoke($"Asset '{assetPath}' returned null");
                }
            });
        }
        public Task<IList<T>> LoadAssetsAsync<T>(IList<string> assetPaths) where T : UnityEngine.Object => _service?.LoadAssetsAsync<T>(assetPaths, Time.realtimeSinceStartup) ?? Task.FromResult<IList<T>>(new List<T>());
        public Task<IList<T>> LoadAssetsAsync<T>(IList<string> assetPaths, CancellationToken cancellationToken) where T : UnityEngine.Object => LoadAssetsAsync<T>(assetPaths);
        public Task<IList<T>> LoadAssetsByLabelAsync<T>(string label) where T : UnityEngine.Object => _service?.LoadAssetsByLabelAsync<T>(label) ?? Task.FromResult<IList<T>>(new List<T>());
        public Task<IList<T>> LoadAssetsByLabelAsync<T>(string label, CancellationToken cancellationToken) where T : UnityEngine.Object => LoadAssetsByLabelAsync<T>(label);
        public Task<bool> HasAssetAsync(string assetPath) => _service?.HasAssetAsync(assetPath) ?? Task.FromResult(false);
        public Task<bool> HasAssetAsync(string assetPath, CancellationToken cancellationToken) => HasAssetAsync(assetPath);
        public void PreloadAssets(string[] assetPaths) => _ = PreloadAssetsAsync(assetPaths);
        public Task PreloadAssetsAsync(string[] assetPaths) => _service?.PreloadAssetsAsync(assetPaths, Time.realtimeSinceStartup) ?? Task.CompletedTask;
        public Task PreloadAssetsAsync(string[] assetPaths, CancellationToken cancellationToken) => PreloadAssetsAsync(assetPaths);
        public void ReleaseAsset(string address) => _service?.ReleaseAsset(address);
        public void ReleaseAllAssets() { _service?.ReleaseAllAssets(); Resources.UnloadUnusedAssets(); }
        public void UnloadAsset(string assetPath) => ReleaseAsset(assetPath);
        public void UnloadAsset<T>(T asset) where T : UnityEngine.Object => _service?.UnloadAsset(asset);
        public bool IsAssetLoaded(string assetPath) => _service?.IsAssetLoaded(assetPath) ?? false;
        public AssetManagerStats GetStats() => _service?.GetStats() ?? new AssetManagerStats();
        public string GetPerformanceReport() => _service?.GetPerformanceReport() ?? "Statistics not available";
        public Task<bool> CheckForContentUpdatesAsync() => _service?.CheckForContentUpdatesAsync() ?? Task.FromResult(false);
        public Task<bool> CheckForContentUpdatesAsync(CancellationToken cancellationToken) => CheckForContentUpdatesAsync();
        public void ClearCache() { ReleaseAllAssets(); _service?.ClearCache(); }
        public void ClearCache(bool persistentOnly) { if (!persistentOnly) ClearCache(); }
        public AssetCacheEntry[] GetCacheEntries() => Array.Empty<AssetCacheEntry>();
    }
#endif
}
