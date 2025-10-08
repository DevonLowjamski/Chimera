using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
#if UNITY_ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
#endif
using ProjectChimera.Core;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Addressables
{
#if UNITY_ADDRESSABLES
    /// <summary>
    /// REAL ADDRESSABLES: Advanced prefab resolver using genuine Unity Addressables
    /// Provides instantiation, pooling, and proper memory management for addressable prefabs
    /// </summary>
    public class AddressablePrefabResolver : MonoBehaviour, IPrefabResolver
    {
        [Header("Prefab Settings")]
        [SerializeField] private bool _enablePooling = true;
        [SerializeField] private int _maxPoolSize = 20;
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private bool _enablePreloading = true;

        // Addressables handles for proper reference counting
        private readonly Dictionary<string, AsyncOperationHandle<GameObject>> _prefabHandles = new Dictionary<string, AsyncOperationHandle<GameObject>>();
        private readonly Dictionary<string, Queue<GameObject>> _prefabPools = new Dictionary<string, Queue<GameObject>>();
        private readonly Dictionary<GameObject, string> _instanceToAddress = new Dictionary<GameObject, string>();
        private readonly Dictionary<GameObject, AsyncOperationHandle<GameObject>> _instanceToHandle = new Dictionary<GameObject, AsyncOperationHandle<GameObject>>();
        private readonly HashSet<string> _preloadedPrefabs = new HashSet<string>();

        private bool _isInitialized = false;

        // Events
        public event Action<string, GameObject> OnPrefabLoaded;
        public event Action<string, string> OnPrefabLoadFailed;
        public event Action<GameObject> OnPrefabReleased;
        public event Action<GameObject> OnPrefabInstantiated;

        /// <summary>
        /// Common prefabs to preload
        /// </summary>
        private readonly string[] COMMON_PREFABS = {
            "UI/BasicButton",
            "UI/BasicPanel",
            "Construction/Foundation",
            "Plants/DefaultPlant",
            "Effects/BasicParticle"
        };

        #region Initialization

        private void Start()
        {
            InitializeAsync();
        }

        /// <summary>
        /// Initialize the prefab resolver with Addressables
        /// </summary>
        public async void InitializeAsync()
        {
            if (_isInitialized) return;

            // Ensure Addressables package is usable; we use Addressables static API directly

            _isInitialized = true;

            // Preload common prefabs if enabled
            if (_enablePreloading)
            {
                await PreloadCommonPrefabs();
            }

            if (_enableLogging)
            {
                ChimeraLogger.Log("PREFABS", "AddressablePrefabResolver initialized with Addressables", this);
            }
        }

        /// <summary>
        /// IResolver legacy compatibility
        /// </summary>
        public void Initialize()
        {
            if (!_isInitialized)
            {
                InitializeAsync();
            }
        }

        /// <summary>
        /// Preload commonly used prefabs
        /// </summary>
        private async Task PreloadCommonPrefabs()
        {
            ChimeraLogger.Log("PREFABS", "Preloading common prefabs...", this);
            int successCount = 0;

            foreach (var prefabAddress in COMMON_PREFABS)
            {
                try
                {
                    var prefab = await LoadPrefabAsync(prefabAddress);
                    if (prefab != null)
                    {
                        _preloadedPrefabs.Add(prefabAddress);
                        successCount++;

                        // Return prefab to pool for later use
                        ReturnToPool(prefab);
                    }
                }
                catch (Exception ex)
                {
                    ChimeraLogger.LogWarning("PREFABS", $"Failed to preload prefab '{prefabAddress}': {ex.Message}", this);
                }
            }

            ChimeraLogger.Log("PREFABS", $"Preloaded {successCount}/{COMMON_PREFABS.Length} common prefabs", this);
        }

        #endregion

        #region Prefab Loading (Async)

        /// <summary>
        /// Load a prefab asynchronously using Addressables
        /// </summary>
        public async Task<GameObject> LoadPrefabAsync(string address)
        {
            if (!_isInitialized || string.IsNullOrEmpty(address))
                return null;

            try
            {
                // Check if we already have this prefab loaded
                if (_prefabHandles.TryGetValue(address, out var existingHandle))
                {
                    if (existingHandle.Status == AsyncOperationStatus.Succeeded)
                    {
                        OnPrefabLoaded?.Invoke(address, existingHandle.Result);
                        return existingHandle.Result;
                    }
                }

                // Load via Addressables
                var handle = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<GameObject>(address);
                _prefabHandles[address] = handle;

                await handle.Task;

                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    var prefab = handle.Result;
                    OnPrefabLoaded?.Invoke(address, prefab);

                    if (_enableLogging)
                        ChimeraLogger.Log("PREFABS", $"Loaded prefab: {address}", this);

                    return prefab;
                }
                else
                {
                    var errorMsg = $"Failed to load prefab '{address}': {handle.OperationException?.Message}";
                    ChimeraLogger.LogError("PREFABS", errorMsg, this);
                    OnPrefabLoadFailed?.Invoke(address, errorMsg);

                    UnityEngine.AddressableAssets.Addressables.Release(handle);
                    _prefabHandles.Remove(address);
                    return null;
                }
            }
            catch (Exception ex)
            {
                var errorMsg = $"Exception loading prefab '{address}': {ex.Message}";
                ChimeraLogger.LogError("PREFABS", errorMsg, this);
                OnPrefabLoadFailed?.Invoke(address, errorMsg);
                return null;
            }
        }

        /// <summary>
        /// Instantiate a prefab asynchronously
        /// </summary>
        public async Task<GameObject> InstantiatePrefabAsync(string address, Vector3 position = default, Quaternion rotation = default, Transform parent = null)
        {
            if (!_isInitialized) return null;

            // Check pool first if pooling is enabled
            if (_enablePooling && TryGetFromPool(address, out var pooledInstance))
            {
                ConfigureInstance(pooledInstance, position, rotation, parent);
                OnPrefabInstantiated?.Invoke(pooledInstance);
                return pooledInstance;
            }

            // Load the prefab first
            var prefab = await LoadPrefabAsync(address);
            if (prefab == null) return null;

            // Instantiate using Addressables for proper reference tracking
                    var handle = UnityEngine.AddressableAssets.Addressables.InstantiateAsync(address, position, rotation, parent);
            await handle.Task;

            if (handle.Status == AsyncOperationStatus.Succeeded)
            {
                var instance = handle.Result;
                _instanceToAddress[instance] = address;
                _instanceToHandle[instance] = handle;

                OnPrefabInstantiated?.Invoke(instance);

                if (_enableLogging)
                    ChimeraLogger.Log("PREFABS", $"Instantiated prefab: {address}", this);

                return instance;
            }
            else
            {
                ChimeraLogger.LogError("PREFABS", $"Failed to instantiate prefab '{address}': {handle.OperationException?.Message}", this);
                UnityEngine.AddressableAssets.Addressables.Release(handle);
                return null;
            }
        }

        #endregion

        #region Prefab Loading (Synchronous - Legacy)

        /// <summary>
        /// Load prefab synchronously (legacy compatibility - not recommended)
        /// </summary>
        [Obsolete("Use LoadPrefabAsync instead for better performance")]
        public GameObject LoadPrefab(string address)
        {
            ChimeraLogger.LogWarning("PREFABS", $"Synchronous prefab loading used for '{address}' - consider using async version", this);

            var task = LoadPrefabAsync(address);
            task.Wait();
            return task.Result;
        }

        /// <summary>
        /// Instantiate prefab synchronously (legacy compatibility - not recommended)
        /// </summary>
        [Obsolete("Use InstantiatePrefabAsync instead for better performance")]
        public GameObject InstantiatePrefab(string address, Vector3 position = default, Quaternion rotation = default, Transform parent = null)
        {
            ChimeraLogger.LogWarning("PREFABS", $"Synchronous prefab instantiation used for '{address}' - consider using async version", this);

            var task = InstantiatePrefabAsync(address, position, rotation, parent);
            task.Wait();
            return task.Result;
        }

        #endregion

        #region Object Pooling

        /// <summary>
        /// Try to get an instance from the pool
        /// </summary>
        private bool TryGetFromPool(string address, out GameObject instance)
        {
            instance = null;

            if (!_enablePooling || !_prefabPools.TryGetValue(address, out var pool) || pool.Count == 0)
                return false;

            instance = pool.Dequeue();
            if (instance == null) return false;

            instance.SetActive(true);
            return true;
        }

        /// <summary>
        /// Return an instance to the pool
        /// </summary>
        private void ReturnToPool(GameObject instance)
        {
            if (!_enablePooling) return;

            if (!_instanceToAddress.TryGetValue(instance, out var address))
                return;

            if (!_prefabPools.TryGetValue(address, out var pool))
            {
                pool = new Queue<GameObject>();
                _prefabPools[address] = pool;
            }

            if (pool.Count < _maxPoolSize)
            {
                instance.SetActive(false);
                instance.transform.SetParent(transform); // Pool under this object
                pool.Enqueue(instance);
            }
            else
            {
                // Pool is full, destroy the instance
                DestroyInstance(instance);
            }
        }

        /// <summary>
        /// Configure instance position, rotation, and parent
        /// </summary>
        private void ConfigureInstance(GameObject instance, Vector3 position, Quaternion rotation, Transform parent)
        {
            if (parent != null)
                instance.transform.SetParent(parent);

            instance.transform.position = position;
            instance.transform.rotation = rotation;
        }

        #endregion

        #region Instance Management

        /// <summary>
        /// Release/destroy a prefab instance
        /// </summary>
        public void ReleasePrefabInstance(GameObject instance)
        {
            if (instance == null) return;

            // Try to return to pool first
            if (_enablePooling && _instanceToAddress.ContainsKey(instance))
            {
                ReturnToPool(instance);
                OnPrefabReleased?.Invoke(instance);
                return;
            }

            // Otherwise destroy the instance
            DestroyInstance(instance);
        }

        /// <summary>
        /// Destroy instance and release Addressables handle
        /// </summary>
        private void DestroyInstance(GameObject instance)
        {
            if (instance == null) return;

            // Release Addressables handle if we have one
            if (_instanceToHandle.TryGetValue(instance, out var handle))
            {
                UnityEngine.AddressableAssets.Addressables.ReleaseInstance(handle);
                _instanceToHandle.Remove(instance);
            }

            _instanceToAddress.Remove(instance);
            OnPrefabReleased?.Invoke(instance);

            if (instance != null)
                Destroy(instance);
        }

        /// <summary>
        /// Check if prefab is available (exists in Addressables catalog)
        /// </summary>
        public async Task<bool> HasPrefabAsync(string address)
        {
            try
            {
                var locations = await UnityEngine.AddressableAssets.Addressables.LoadResourceLocationsAsync(address).Task;
                return locations != null && locations.Count > 0;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Synchronous version for legacy compatibility
        /// </summary>
        public bool HasPrefab(string address)
        {
            // For immediate results, check if we have it loaded
            return _prefabHandles.ContainsKey(address);
        }

        /// <summary>
        /// Get loading progress (not directly applicable but estimates based on handles)
        /// </summary>
        public float GetLoadProgress(string address)
        {
            if (_prefabHandles.TryGetValue(address, out var handle))
            {
                return handle.PercentComplete;
            }
            return 0f;
        }

        #endregion

        #region Statistics and Utilities

        /// <summary>
        /// Get prefab resolver statistics
        /// </summary>
        public PrefabResolverStats GetStats()
        {
            int pooledInstances = 0;
            foreach (var pool in _prefabPools.Values)
            {
                pooledInstances += pool.Count;
            }

            return new PrefabResolverStats
            {
                IsInitialized = _isInitialized,
                LoadedPrefabs = _prefabHandles.Count,
                ActiveInstances = _instanceToAddress.Count,
                PooledInstances = pooledInstances,
                PreloadedPrefabs = _preloadedPrefabs.Count,
                PoolingEnabled = _enablePooling,
                MaxPoolSize = _maxPoolSize
            };
        }

        /// <summary>
        /// Clear all pools and release resources
        /// </summary>
        public void ClearPools()
        {
            foreach (var pool in _prefabPools.Values)
            {
                while (pool.Count > 0)
                {
                    var instance = pool.Dequeue();
                    if (instance != null)
                        DestroyInstance(instance);
                }
            }

            _prefabPools.Clear();
            ChimeraLogger.Log("PREFABS", "All prefab pools cleared", this);
        }

        #endregion

        #region Unity Lifecycle

        private void OnDestroy()
        {
            // Release all handles and clean up
            foreach (var handle in _prefabHandles.Values)
            {
                if (handle.IsValid())
                    UnityEngine.AddressableAssets.Addressables.Release(handle);
            }

            foreach (var handle in _instanceToHandle.Values)
            {
                if (handle.IsValid())
                    UnityEngine.AddressableAssets.Addressables.ReleaseInstance(handle);
            }

            ClearPools();

            if (_enableLogging)
                ChimeraLogger.Log("PREFABS", "AddressablePrefabResolver cleanup completed", this);
        }

        #endregion
    }

    /// <summary>
    /// Interface for prefab resolution
    /// </summary>
    public interface IPrefabResolver
    {
        void Initialize();
        Task<GameObject> LoadPrefabAsync(string address);
        Task<GameObject> InstantiatePrefabAsync(string address, Vector3 position = default, Quaternion rotation = default, Transform parent = null);
        void ReleasePrefabInstance(GameObject instance);
        Task<bool> HasPrefabAsync(string address);
    }

    /// <summary>
    /// Prefab resolver statistics
    /// </summary>
    [System.Serializable]
    public struct PrefabResolverStats
    {
        public bool IsInitialized;
        public int LoadedPrefabs;
        public int ActiveInstances;
        public int PooledInstances;
        public int PreloadedPrefabs;
        public bool PoolingEnabled;
        public int MaxPoolSize;
    }
#endif
}
