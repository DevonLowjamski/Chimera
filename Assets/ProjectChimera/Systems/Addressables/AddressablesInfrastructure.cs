using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
#if UNITY_ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
#endif
using ProjectChimera.Systems.Diagnostics;

namespace ProjectChimera.Systems.Addressables
{
    /// <summary>
    /// Core Addressables infrastructure for Project Chimera
    /// Provides centralized asset loading and management with fallback to Resources.Load
    /// Replaces direct Resources.Load calls with async Addressables system
    /// </summary>
    public class AddressablesInfrastructure : MonoBehaviour
    {
        [Header("Addressables Configuration")]
        [SerializeField] private bool _enableAddressables = true;
        [SerializeField] private bool _enableCaching = true;
        [SerializeField] private bool _enableProgressTracking = true;
        [SerializeField] private bool _enableResourcesFallback = true;
        [SerializeField] private int _maxCacheSize = 100;
        [SerializeField] private float _cacheExpirationTime = 300f; // 5 minutes
        
        [Header("Debug Configuration")]
        [SerializeField] private bool _enableDebugLogging = true;
        [SerializeField] private bool _logCacheOperations = false;
        [SerializeField] private bool _logLoadingProgress = false;
        
        [Header("Critical Asset Preloading")]
        [SerializeField] private string[] _criticalPrefabAddresses;
        [SerializeField] private string[] _criticalUIAddresses;
        [SerializeField] private string[] _criticalAudioAddresses;
        
        // Asset Management
#if UNITY_ADDRESSABLES
        private readonly Dictionary<string, AsyncOperationHandle> _loadingOperations = new Dictionary<string, AsyncOperationHandle>();
#else
        private readonly Dictionary<string, Task> _loadingOperations = new Dictionary<string, Task>();
#endif
        private readonly Dictionary<string, CachedAsset> _assetCache = new Dictionary<string, CachedAsset>();
        private readonly Dictionary<string, List<GameObject>> _instancePools = new Dictionary<string, List<GameObject>>();
        
        // Reference Tracking
        private readonly Dictionary<GameObject, string> _instanceToAddress = new Dictionary<GameObject, string>();
        private readonly Dictionary<string, int> _referenceCounters = new Dictionary<string, int>();
        
        // Performance Metrics
        private readonly Dictionary<string, LoadingProgress> _loadingProgress = new Dictionary<string, LoadingProgress>();
        private int _totalAssetsLoaded = 0;
        private int _totalAssetsCached = 0;
        private int _cacheHits = 0;
        private int _cacheMisses = 0;
        
        // System References
        private DevelopmentMonitoring _diagnostics;
        private bool _isInitialized = false;
        
        // Events
        public event Action OnAddressablesInitialized;
        public event Action<string, UnityEngine.Object> OnAssetLoaded;
        public event Action<string, string> OnAssetLoadFailed;
        public event Action<string, float> OnLoadingProgress;
        
        // Properties
        public bool IsInitialized => _isInitialized;
        public int CachedAssetCount => _assetCache.Count;
        public int ActiveLoadingOperations => _loadingOperations.Count;
        
        #region Unity Lifecycle
        
        private async void Start()
        {
            await InitializeAsync();
        }
        
        private void OnDestroy()
        {
            Shutdown();
        }
        
        #endregion
        
        #region Initialization
        
        public async Task<bool> InitializeAsync()
        {
            if (_isInitialized) return true;
            
            try
            {
                LoggingInfrastructure.LogInfo("AddressablesInfrastructure", "Initializing Addressables Infrastructure...");
                
                // Find diagnostics system
                _diagnostics = UnityEngine.Object.FindObjectOfType<DevelopmentMonitoring>();
                
                if (_enableAddressables)
                {
#if UNITY_ADDRESSABLES
                    // Initialize Addressables
                    var initHandle = Addressables.InitializeAsync();
                    await initHandle.Task;
                    
                    if (initHandle.Status == AsyncOperationStatus.Succeeded)
                    {
                        _isInitialized = true;
                        LoggingInfrastructure.LogInfo("AddressablesInfrastructure", "Addressables initialized successfully");
                        OnAddressablesInitialized?.Invoke();
                    }
                    else
                    {
                        LoggingInfrastructure.LogWarning("AddressablesInfrastructure", "Addressables initialization failed, enabling fallback mode");
                        _enableAddressables = false;
                        _isInitialized = true;
                    }
#else
                    LoggingInfrastructure.LogInfo("AddressablesInfrastructure", "Addressables package not available, using Resources fallback");
                    _enableAddressables = false;
                    _isInitialized = true;
#endif
                }
                else
                {
                    _isInitialized = true;
                }
                
                // Preload critical assets
                if (_criticalPrefabAddresses?.Length > 0 || _criticalUIAddresses?.Length > 0 || _criticalAudioAddresses?.Length > 0)
                {
                    await PreloadCriticalAssets();
                }
                
                LoggingInfrastructure.LogInfo("AddressablesInfrastructure", 
                    $"Addressables Infrastructure initialized - Mode: {(_enableAddressables ? "Addressables" : "Resources")}");
                
                return true;
            }
            catch (Exception ex)
            {
                LoggingInfrastructure.LogError("AddressablesInfrastructure", "Failed to initialize Addressables Infrastructure", ex);
                _enableAddressables = false; // Fallback to Resources
                _isInitialized = true; // Still allow operation
                return false;
            }
        }
        
        public void Shutdown()
        {
            try
            {
                // Release all cached assets
                foreach (var cachedAsset in _assetCache.Values)
                {
#if UNITY_ADDRESSABLES
                    if (_enableAddressables && cachedAsset.Handle.IsValid())
                    {
                        Addressables.Release(cachedAsset.Handle);
                    }
#endif
                }
                
                _assetCache.Clear();
                _loadingOperations.Clear();
                _instancePools.Clear();
                _instanceToAddress.Clear();
                _referenceCounters.Clear();
                _loadingProgress.Clear();
                
                _isInitialized = false;
                LoggingInfrastructure.LogInfo("AddressablesInfrastructure", "Addressables Infrastructure shut down");
            }
            catch (Exception ex)
            {
                LoggingInfrastructure.LogError("AddressablesInfrastructure", "Error during shutdown", ex);
            }
        }
        
        #endregion
        
        #region Asset Loading
        
        public async Task<T> LoadAssetAsync<T>(string address) where T : UnityEngine.Object
        {
            if (!_isInitialized)
            {
                await InitializeAsync();
            }
            
            // Check cache first
            if (_enableCaching && _assetCache.TryGetValue(address, out var cached))
            {
                if (cached.Asset is T cachedAsset && cached.IsValid())
                {
                    _cacheHits++;
                    IncrementReferenceCount(address);
                    OnAssetLoaded?.Invoke(address, cachedAsset);
                    return cachedAsset;
                }
                else
                {
                    // Remove invalid cache entry
                    _assetCache.Remove(address);
                }
            }
            
            _cacheMisses++;
            
            // Load asset
            T asset = null;
            try
            {
                if (_enableAddressables)
                {
                    asset = await LoadAssetViaAddressablesAsync<T>(address);
                }
                else if (_enableResourcesFallback)
                {
                    asset = await LoadAssetViaResourcesAsync<T>(address);
                }
                
                if (asset == null)
                {
                    OnAssetLoadFailed?.Invoke(address, "Asset not found in Addressables or Resources");
                    LoggingInfrastructure.LogWarning("AddressablesInfrastructure", $"Failed to load asset: {address}");
                }
            }
            catch (Exception ex)
            {
                OnAssetLoadFailed?.Invoke(address, ex.Message);
                LoggingInfrastructure.LogError("AddressablesInfrastructure", $"Exception loading asset {address}", ex);
            }
            
            return asset;
        }
        
        private async Task<T> LoadAssetViaAddressablesAsync<T>(string address) where T : UnityEngine.Object
        {
#if UNITY_ADDRESSABLES
            try
            {
                // Check if already loading
                if (_loadingOperations.ContainsKey(address))
                {
                    var existingHandle = (AsyncOperationHandle<T>)_loadingOperations[address];
                    await existingHandle.Task;
                    return existingHandle.Result;
                }
                
                // Initialize loading progress
                if (_enableProgressTracking)
                {
                    _loadingProgress[address] = new LoadingProgress
                    {
                        Address = address,
                        StartTime = Time.time,
                        Progress = 0f
                    };
                }
                
                // Load via Addressables
                var handle = Addressables.LoadAssetAsync<T>(address);
                _loadingOperations[address] = handle;
                
                // Track progress if enabled
                if (_enableProgressTracking)
                {
                    _ = TrackLoadingProgressAsync(address, handle);
                }
                
                await handle.Task;
                var result = handle.Result;
                
                // Remove from loading operations
                _loadingOperations.Remove(address);
                
                // Cache if successful
                if (result != null)
                {
                    CacheAsset(address, result, handle);
                }
                
                OnAssetLoaded?.Invoke(address, result);
                return result;
            }
            catch (Exception ex)
            {
                _loadingOperations.Remove(address);
                LoggingInfrastructure.LogError("AddressablesInfrastructure", $"Failed to load asset via Addressables: {address}", ex);
                throw;
            }
#else
            // Fallback to Resources.Load when Addressables not available
            return await LoadAssetViaResourcesAsync<T>(address);
#endif
        }
        
        private async Task<T> LoadAssetViaResourcesAsync<T>(string address) where T : UnityEngine.Object
        {
            try
            {
                await Task.Yield(); // Make it async for consistency
                
                var resourcePath = ConvertAddressToResourcePath(address);
                var asset = Resources.Load<T>(resourcePath);
                
                if (asset != null)
                {
                    CacheAsset(address, asset);
                    _totalAssetsLoaded++;
                    OnAssetLoaded?.Invoke(address, asset);
                }
                
                return asset;
            }
            catch (Exception ex)
            {
                LoggingInfrastructure.LogError("AddressablesInfrastructure", $"Failed to load asset via Resources: {address}", ex);
                throw;
            }
        }
        
        #endregion
        
        #region Asset Management
        
#if UNITY_ADDRESSABLES
        private void CacheAsset<T>(string address, T asset, AsyncOperationHandle handle = default) where T : UnityEngine.Object
#else
        private void CacheAsset<T>(string address, T asset) where T : UnityEngine.Object
#endif
        {
            if (!_enableCaching) return;
            
            // Manage cache size
            if (_assetCache.Count >= _maxCacheSize)
            {
                EvictOldestAsset();
            }
            
            var cachedAsset = new CachedAsset
            {
                Asset = asset,
                Address = address,
                CacheTime = Time.time,
#if UNITY_ADDRESSABLES
                Handle = handle
#endif
            };
            
            _assetCache[address] = cachedAsset;
            _totalAssetsCached++;
            
            IncrementReferenceCount(address);
            
            if (_logCacheOperations)
            {
                LoggingInfrastructure.LogTrace("AddressablesInfrastructure", $"Cached asset: {address}");
            }
        }
        
        private void EvictOldestAsset()
        {
            string oldestAddress = null;
            float oldestTime = float.MaxValue;
            
            foreach (var kvp in _assetCache)
            {
                if (kvp.Value.CacheTime < oldestTime)
                {
                    oldestTime = kvp.Value.CacheTime;
                    oldestAddress = kvp.Key;
                }
            }
            
            if (oldestAddress != null)
            {
                ReleaseAsset(oldestAddress);
            }
        }
        
        #endregion
        
        #region Progress Tracking
        
#if UNITY_ADDRESSABLES
        private async Task TrackLoadingProgressAsync(string address, AsyncOperationHandle handle)
#else
        private async Task TrackLoadingProgressAsync(string address, Task handle)
#endif
        {
            try
            {
#if UNITY_ADDRESSABLES
                while (!handle.IsDone)
                {
                    if (_loadingProgress.TryGetValue(address, out var progress))
                    {
                        progress.Progress = handle.PercentComplete;
                        OnLoadingProgress?.Invoke(address, progress.Progress);
                        
                        if (_logLoadingProgress)
                        {
                            LoggingInfrastructure.LogTrace("AddressablesInfrastructure", $"Loading {address}: {progress.Progress * 100:F1}%");
                        }
                    }
                    
                    await Task.Delay(50);
                }
#else
                while (!handle.IsCompleted)
                {
                    if (_loadingProgress.TryGetValue(address, out var progress))
                    {
                        // For Tasks, we can't get specific progress, so just show 50% while loading
                        progress.Progress = 0.5f;
                        OnLoadingProgress?.Invoke(address, 0.5f);
                        
                        if (_logLoadingProgress)
                        {
                            LoggingInfrastructure.LogTrace("AddressablesInfrastructure", $"Loading {address}: In Progress");
                        }
                    }
                    
                    await Task.Delay(50);
                }
#endif
                
                // Final progress update
                if (_loadingProgress.TryGetValue(address, out var finalProgress))
                {
                    finalProgress.Progress = 1f;
                    finalProgress.CompletionTime = Time.time;
                    OnLoadingProgress?.Invoke(address, 1f);
                }
            }
            catch (Exception ex)
            {
                LoggingInfrastructure.LogError("AddressablesInfrastructure", $"Error tracking progress for {address}", ex);
            }
        }
        
        #endregion
        
        #region Instantiation
        
        public async Task<GameObject> InstantiateAsync(string address, Transform parent = null, bool instantiateInWorldSpace = false)
        {
            try
            {
                GameObject prefab = null;
                
                if (_enableAddressables && _isInitialized)
                {
#if UNITY_ADDRESSABLES
                    var handle = Addressables.InstantiateAsync(address, parent, instantiateInWorldSpace);
                    prefab = await handle.Task;
                    
                    if (handle.Status == AsyncOperationStatus.Succeeded)
                    {
                        _instanceToAddress[prefab] = address;
                        IncrementReferenceCount(address);
                        return prefab;
                    }
#else
                    // Fallback: Load prefab and instantiate manually
                    var template = await LoadAssetAsync<GameObject>(address);
                    if (template != null)
                    {
                        prefab = UnityEngine.Object.Instantiate(template, parent, instantiateInWorldSpace);
                        _instanceToAddress[prefab] = address;
                        IncrementReferenceCount(address);
                        return prefab;
                    }
#endif
                }
                
                // Fallback to manual instantiation
                if (_enableResourcesFallback)
                {
                    var template = await LoadAssetAsync<GameObject>(address);
                    if (template != null)
                    {
                        prefab = UnityEngine.Object.Instantiate(template, parent, instantiateInWorldSpace);
                        _instanceToAddress[prefab] = address;
                        IncrementReferenceCount(address);
                        return prefab;
                    }
                }
                
                LoggingInfrastructure.LogWarning("AddressablesInfrastructure", $"Failed to instantiate prefab: {address}");
                return null;
            }
            catch (Exception ex)
            {
                LoggingInfrastructure.LogError("AddressablesInfrastructure", $"Exception instantiating prefab {address}", ex);
                return null;
            }
        }
        
        public void ReleaseInstance(GameObject instance)
        {
            if (instance == null) return;
            
            try
            {
                if (_instanceToAddress.TryGetValue(instance, out var address))
                {
                    _instanceToAddress.Remove(instance);
                    
                    if (_enableAddressables && _isInitialized)
                    {
#if UNITY_ADDRESSABLES
                        Addressables.ReleaseInstance(instance);
#else
                        // Fallback: Just destroy the instance
                        UnityEngine.Object.Destroy(instance);
#endif
                    }
                    else
                    {
                        UnityEngine.Object.Destroy(instance);
                    }
                    
                    DecrementReferenceCount(address);
                }
                else
                {
                    // Instance not tracked, just destroy it
                    UnityEngine.Object.Destroy(instance);
                }
            }
            catch (Exception ex)
            {
                LoggingInfrastructure.LogError("AddressablesInfrastructure", $"Exception releasing instance", ex);
                // Fallback to destroy
                if (instance != null)
                {
                    UnityEngine.Object.Destroy(instance);
                }
            }
        }
        
        #endregion
        
        #region Asset Release
        
        public void ReleaseAsset(string address)
        {
            try
            {
                if (_assetCache.TryGetValue(address, out var cached))
                {
#if UNITY_ADDRESSABLES
                    if (_enableAddressables && cached.Handle.IsValid())
                    {
                        Addressables.Release(cached.Handle);
                    }
#endif
                    _assetCache.Remove(address);
                    
                    if (_logCacheOperations)
                    {
                        LoggingInfrastructure.LogTrace("AddressablesInfrastructure", $"Released asset: {address}");
                    }
                }
                
                DecrementReferenceCount(address);
            }
            catch (Exception ex)
            {
                LoggingInfrastructure.LogError("AddressablesInfrastructure", $"Error releasing asset {address}", ex);
            }
        }
        
        public void ReleaseAllAssets()
        {
            var addressesToRelease = new List<string>(_assetCache.Keys);
            foreach (var address in addressesToRelease)
            {
                ReleaseAsset(address);
            }
        }
        
        #endregion
        
        #region Utility Methods
        
        private string ConvertAddressToResourcePath(string address)
        {
            // Convert Addressables address to Resources path
            // Remove file extensions and convert to Resources-compatible path
            var resourcePath = address.Replace(".prefab", "").Replace(".asset", "");
            
            // If it starts with "Assets/Resources/", remove that prefix
            if (resourcePath.StartsWith("Assets/Resources/"))
            {
                resourcePath = resourcePath.Substring("Assets/Resources/".Length);
            }
            
            // If not in Resources folder, try using the address directly
            return resourcePath;
        }
        
        private void IncrementReferenceCount(string address)
        {
            if (_referenceCounters.ContainsKey(address))
            {
                _referenceCounters[address]++;
            }
            else
            {
                _referenceCounters[address] = 1;
            }
        }
        
        private void DecrementReferenceCount(string address)
        {
            if (_referenceCounters.ContainsKey(address))
            {
                _referenceCounters[address]--;
                if (_referenceCounters[address] <= 0)
                {
                    _referenceCounters.Remove(address);
                }
            }
        }
        
        #endregion
        
        #region Preloading
        
        private async Task PreloadCriticalAssets()
        {
            var preloadTasks = new List<Task>();
            
            // Preload critical prefabs
            if (_criticalPrefabAddresses != null)
            {
                foreach (var address in _criticalPrefabAddresses)
                {
                    preloadTasks.Add(LoadAssetAsync<GameObject>(address));
                }
            }
            
            // Preload critical UI assets
            if (_criticalUIAddresses != null)
            {
                foreach (var address in _criticalUIAddresses)
                {
                    preloadTasks.Add(LoadAssetAsync<UnityEngine.Object>(address));
                }
            }
            
            // Preload critical audio
            if (_criticalAudioAddresses != null)
            {
                foreach (var address in _criticalAudioAddresses)
                {
                    preloadTasks.Add(LoadAssetAsync<AudioClip>(address));
                }
            }
            
            await Task.WhenAll(preloadTasks);
            LoggingInfrastructure.LogInfo("AddressablesInfrastructure", "Critical assets preloaded");
        }
        
        #endregion
        
        #region Data Structures
        
        [System.Serializable]
        public class CachedAsset
        {
            public UnityEngine.Object Asset;
            public string Address;
            public float CacheTime;
#if UNITY_ADDRESSABLES
            public AsyncOperationHandle Handle;
#endif
            
            public bool IsValid()
            {
                return Asset != null && Time.time - CacheTime < 300f; // 5 minute expiration
            }
        }
        
        [System.Serializable]
        public class LoadingProgress
        {
            public string Address;
            public float StartTime;
            public float Progress;
            public float CompletionTime;
        }
        
        #endregion
    }
}