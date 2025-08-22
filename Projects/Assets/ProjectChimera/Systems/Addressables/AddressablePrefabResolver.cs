using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using ProjectChimera.Systems.Diagnostics;

namespace ProjectChimera.Systems.Addressables
{
    /// <summary>
    /// Prefab resolver interface for Project Chimera
    /// Provides abstraction layer for loading and releasing prefabs via Addressables or Resources
    /// </summary>
    public interface IPrefabResolver
    {
        Task<GameObject> LoadPrefabAsync(string address);
        void Release(GameObject instance);
        Task<T> LoadAssetAsync<T>(string address) where T : UnityEngine.Object;
        void ReleaseAsset(string address);
        bool IsAssetLoaded(string address);
        float GetLoadProgress(string address);
    }
    
    /// <summary>
    /// Addressables-based prefab resolver implementation
    /// Handles prefab loading, instantiation, and lifecycle management
    /// Integrates with AddressablesInfrastructure for centralized asset management
    /// </summary>
    public class AddressablePrefabResolver : MonoBehaviour, IPrefabResolver
    {
        [Header("Prefab Resolver Configuration")]
        [SerializeField] private bool _enablePrefabPooling = true;
        [SerializeField] private bool _enablePrewarming = true;
        [SerializeField] private int _defaultPoolSize = 5;
        [SerializeField] private int _maxPoolSize = 20;
        [SerializeField] private float _poolCleanupInterval = 60f; // seconds
        
        [Header("Performance Settings")]
        [SerializeField] private bool _enableAsyncInstantiation = true;
        [SerializeField] private int _maxConcurrentLoads = 10;
        [SerializeField] private bool _enableLoadPrioritySystem = true;
        [SerializeField] private float _highPriorityTimeout = 5f;
        [SerializeField] private float _normalPriorityTimeout = 15f;
        
        [Header("Prefab Categories")]
        [SerializeField] private PrefabCategory[] _prefabCategories;
        
        // Core Systems
        private AddressablesInfrastructure _addressablesInfrastructure;
        
        // Prefab Management
        private readonly Dictionary<string, PrefabPool> _prefabPools = new Dictionary<string, PrefabPool>();
        private readonly Dictionary<GameObject, string> _instanceToAddress = new Dictionary<GameObject, string>();
        private readonly Dictionary<string, PrefabLoadRequest> _activeLoadRequests = new Dictionary<string, PrefabLoadRequest>();
        
        // Performance Tracking
        private readonly Dictionary<string, PrefabMetrics> _prefabMetrics = new Dictionary<string, PrefabMetrics>();
        private float _lastPoolCleanup = 0f;
        private int _activeConcurrentLoads = 0;
        
        // Events
        public event Action<string, GameObject> OnPrefabLoaded;
        public event Action<string, float> OnPrefabLoadProgress;
        public event Action<string, string> OnPrefabLoadFailed;
        public event Action<GameObject> OnPrefabReleased;
        
        private void Start()
        {
            FindSystemReferences();
            InitializePrefabResolver();
        }
        
        private void Update()
        {
            // Periodic pool cleanup
            if (Time.time - _lastPoolCleanup >= _poolCleanupInterval)
            {
                CleanupPrefabPools();
                _lastPoolCleanup = Time.time;
            }
        }
        
        private void FindSystemReferences()
        {
            _addressablesInfrastructure = FindObjectOfType<AddressablesInfrastructure>();
            
            if (_addressablesInfrastructure == null)
            {
                LoggingInfrastructure.LogError("AddressablePrefabResolver", "AddressablesInfrastructure not found");
            }
        }
        
        private void InitializePrefabResolver()
        {
            // Initialize prefab categories
            InitializePrefabCategories();
            
            // Prewarm critical prefabs if enabled
            if (_enablePrewarming)
            {
                _ = PrewarmCriticalPrefabsAsync();
            }
            
            LoggingInfrastructure.LogInfo("AddressablePrefabResolver", "Prefab resolver initialized");
        }
        
        private void InitializePrefabCategories()
        {
            if (_prefabCategories == null) return;
            
            foreach (var category in _prefabCategories)
            {
                if (category.Addresses != null)
                {
                    foreach (var address in category.Addresses)
                    {
                        _prefabMetrics[address] = new PrefabMetrics
                        {
                            Address = address,
                            Category = category.CategoryName,
                            Priority = category.LoadPriority
                        };
                        
                        if (_enablePrefabPooling && category.EnablePooling)
                        {
                            CreatePrefabPool(address, category.PoolSize > 0 ? category.PoolSize : _defaultPoolSize);
                        }
                    }
                }
            }
        }
        
        private async Task PrewarmCriticalPrefabsAsync()
        {
            LoggingInfrastructure.LogInfo("AddressablePrefabResolver", "Prewarming critical prefabs");
            
            var prewarmTasks = new List<Task>();
            
            if (_prefabCategories != null)
            {
                foreach (var category in _prefabCategories)
                {
                    if (category.PrewarmOnStart && category.Addresses != null)
                    {
                        foreach (var address in category.Addresses)
                        {
                            prewarmTasks.Add(PrewarmPrefabAsync(address, category.PoolSize));
                        }
                    }
                }
            }
            
            try
            {
                await Task.WhenAll(prewarmTasks);
                LoggingInfrastructure.LogInfo("AddressablePrefabResolver", "Critical prefab prewarming completed");
            }
            catch (Exception ex)
            {
                LoggingInfrastructure.LogError("AddressablePrefabResolver", "Error during prefab prewarming", ex);
            }
        }
        
        private async Task PrewarmPrefabAsync(string address, int count)
        {
            try
            {
                var prefab = await LoadAssetAsync<GameObject>(address);
                if (prefab != null && _enablePrefabPooling)
                {
                    var pool = GetOrCreatePrefabPool(address);
                    for (int i = 0; i < count; i++)
                    {
                        var instance = Instantiate(prefab);
                        instance.SetActive(false);
                        pool.ReturnToPool(instance);
                    }
                }
            }
            catch (Exception ex)
            {
                LoggingInfrastructure.LogWarning("AddressablePrefabResolver", $"Failed to prewarm prefab {address}: {ex.Message}");
            }
        }
        
        public async Task<GameObject> LoadPrefabAsync(string address)
        {
            if (string.IsNullOrEmpty(address))
            {
                LoggingInfrastructure.LogWarning("AddressablePrefabResolver", "Attempted to load prefab with null or empty address");
                return null;
            }
            
            try
            {
                // Check if we can get from pool first
                if (_enablePrefabPooling && _prefabPools.TryGetValue(address, out var pool))
                {
                    var pooledInstance = pool.GetFromPool();
                    if (pooledInstance != null)
                    {
                        pooledInstance.SetActive(true);
                        _instanceToAddress[pooledInstance] = address;
                        UpdatePrefabMetrics(address, true, 0f);
                        OnPrefabLoaded?.Invoke(address, pooledInstance);
                        return pooledInstance;
                    }
                }
                
                // Check concurrent load limit
                if (_activeConcurrentLoads >= _maxConcurrentLoads)
                {
                    LoggingInfrastructure.LogWarning("AddressablePrefabResolver", $"Concurrent load limit reached. Queuing load for {address}");
                    await WaitForAvailableLoadSlot();
                }
                
                _activeConcurrentLoads++;
                
                // Check if already loading
                if (_activeLoadRequests.TryGetValue(address, out var existingRequest))
                {
                    await existingRequest.LoadTask;
                    _activeConcurrentLoads--;
                    return existingRequest.Result;
                }
                
                // Create new load request
                var loadRequest = new PrefabLoadRequest
                {
                    Address = address,
                    StartTime = DateTime.UtcNow,
                    Priority = GetLoadPriority(address)
                };
                
                _activeLoadRequests[address] = loadRequest;
                
                try
                {
                    // Load via Addressables infrastructure
                    var loadStartTime = DateTime.UtcNow;
                    GameObject instance = null;
                    
                    if (_enableAsyncInstantiation && _addressablesInfrastructure != null)
                    {
                        instance = await _addressablesInfrastructure.InstantiateAsync(address);
                    }
                    else
                    {
                        // Fallback to load + instantiate
                        var prefab = await LoadAssetAsync<GameObject>(address);
                        if (prefab != null)
                        {
                            instance = Instantiate(prefab);
                        }
                    }
                    
                    var loadDuration = (float)(DateTime.UtcNow - loadStartTime).TotalSeconds;
                    
                    if (instance != null)
                    {
                        _instanceToAddress[instance] = address;
                        loadRequest.Result = instance;
                        UpdatePrefabMetrics(address, true, loadDuration);
                        OnPrefabLoaded?.Invoke(address, instance);
                        
                        // Create pool if it doesn't exist and pooling is enabled
                        if (_enablePrefabPooling && !_prefabPools.ContainsKey(address))
                        {
                            CreatePrefabPool(address, _defaultPoolSize);
                        }
                        
                        LoggingInfrastructure.LogTrace("AddressablePrefabResolver", $"Successfully loaded prefab: {address} in {loadDuration:F2}s");
                        return instance;
                    }
                    else
                    {
                        UpdatePrefabMetrics(address, false, loadDuration);
                        OnPrefabLoadFailed?.Invoke(address, "Failed to instantiate prefab");
                        LoggingInfrastructure.LogError("AddressablePrefabResolver", $"Failed to load prefab: {address}");
                        return null;
                    }
                }
                finally
                {
                    loadRequest.LoadTask = Task.CompletedTask;
                    _activeLoadRequests.Remove(address);
                    _activeConcurrentLoads--;
                }
            }
            catch (Exception ex)
            {
                _activeConcurrentLoads--;
                UpdatePrefabMetrics(address, false, 0f);
                OnPrefabLoadFailed?.Invoke(address, ex.Message);
                LoggingInfrastructure.LogError("AddressablePrefabResolver", $"Exception loading prefab {address}", ex);
                return null;
            }
        }
        
        public void Release(GameObject instance)
        {
            if (instance == null) return;
            
            try
            {
                if (_instanceToAddress.TryGetValue(instance, out var address))
                {
                    _instanceToAddress.Remove(instance);
                    
                    // Try to return to pool first
                    if (_enablePrefabPooling && _prefabPools.TryGetValue(address, out var pool))
                    {
                        if (pool.CanReturnToPool())
                        {
                            instance.SetActive(false);
                            pool.ReturnToPool(instance);
                            OnPrefabReleased?.Invoke(instance);
                            return;
                        }
                    }
                    
                    // Release via Addressables infrastructure
                    if (_addressablesInfrastructure != null)
                    {
                        _addressablesInfrastructure.ReleaseInstance(instance);
                    }
                    else
                    {
                        Destroy(instance);
                    }
                    
                    OnPrefabReleased?.Invoke(instance);
                    LoggingInfrastructure.LogTrace("AddressablePrefabResolver", $"Released prefab instance for address: {address}");
                }
                else
                {
                    // Instance not tracked, just destroy it
                    Destroy(instance);
                    LoggingInfrastructure.LogWarning("AddressablePrefabResolver", "Released untracked prefab instance");
                }
            }
            catch (Exception ex)
            {
                LoggingInfrastructure.LogError("AddressablePrefabResolver", "Exception releasing prefab instance", ex);
                // Fallback to destroy
                if (instance != null)
                {
                    Destroy(instance);
                }
            }
        }
        
        public async Task<T> LoadAssetAsync<T>(string address) where T : UnityEngine.Object
        {
            if (_addressablesInfrastructure != null)
            {
                return await _addressablesInfrastructure.LoadAssetAsync<T>(address);
            }
            else
            {
                LoggingInfrastructure.LogError("AddressablePrefabResolver", "AddressablesInfrastructure not available for asset loading");
                return null;
            }
        }
        
        public void ReleaseAsset(string address)
        {
            if (_addressablesInfrastructure != null)
            {
                _addressablesInfrastructure.ReleaseAsset(address);
            }
        }
        
        public bool IsAssetLoaded(string address)
        {
            return _activeLoadRequests.ContainsKey(address) || 
                   (_prefabPools.ContainsKey(address) && _prefabPools[address].HasAvailableInstances());
        }
        
        public float GetLoadProgress(string address)
        {
            if (_activeLoadRequests.TryGetValue(address, out var request))
            {
                // Simple progress calculation based on time elapsed
                var elapsed = (DateTime.UtcNow - request.StartTime).TotalSeconds;
                var timeout = request.Priority == LoadPriority.High ? _highPriorityTimeout : _normalPriorityTimeout;
                return Mathf.Clamp01((float)(elapsed / timeout));
            }
            
            return IsAssetLoaded(address) ? 1f : 0f;
        }
        
        private async Task WaitForAvailableLoadSlot()
        {
            while (_activeConcurrentLoads >= _maxConcurrentLoads)
            {
                await Task.Delay(100); // Wait 100ms before checking again
            }
        }
        
        private LoadPriority GetLoadPriority(string address)
        {
            if (_prefabMetrics.TryGetValue(address, out var metrics))
            {
                return metrics.Priority;
            }
            
            return LoadPriority.Normal;
        }
        
        private void CreatePrefabPool(string address, int poolSize)
        {
            if (!_prefabPools.ContainsKey(address))
            {
                _prefabPools[address] = new PrefabPool(address, poolSize, _maxPoolSize);
                LoggingInfrastructure.LogTrace("AddressablePrefabResolver", $"Created prefab pool for {address} with size {poolSize}");
            }
        }
        
        private PrefabPool GetOrCreatePrefabPool(string address)
        {
            if (!_prefabPools.TryGetValue(address, out var pool))
            {
                pool = new PrefabPool(address, _defaultPoolSize, _maxPoolSize);
                _prefabPools[address] = pool;
            }
            
            return pool;
        }
        
        private void UpdatePrefabMetrics(string address, bool success, float loadTime)
        {
            if (!_prefabMetrics.TryGetValue(address, out var metrics))
            {
                metrics = new PrefabMetrics { Address = address };
                _prefabMetrics[address] = metrics;
            }
            
            metrics.TotalLoads++;
            if (success)
            {
                metrics.SuccessfulLoads++;
                metrics.TotalLoadTime += loadTime;
                metrics.AverageLoadTime = metrics.TotalLoadTime / metrics.SuccessfulLoads;
            }
            else
            {
                metrics.FailedLoads++;
            }
            
            metrics.LastLoadTime = DateTime.UtcNow;
        }
        
        private void CleanupPrefabPools()
        {
            var poolsToCleanup = new List<string>();
            
            foreach (var pool in _prefabPools)
            {
                var cleaned = pool.Value.Cleanup();
                if (cleaned > 0)
                {
                    LoggingInfrastructure.LogTrace("AddressablePrefabResolver", $"Cleaned up {cleaned} instances from pool {pool.Key}");
                }
                
                if (pool.Value.IsEmpty() && !IsPoolInUse(pool.Key))
                {
                    poolsToCleanup.Add(pool.Key);
                }
            }
            
            // Remove empty, unused pools
            foreach (var poolAddress in poolsToCleanup)
            {
                _prefabPools.Remove(poolAddress);
                LoggingInfrastructure.LogTrace("AddressablePrefabResolver", $"Removed unused pool for {poolAddress}");
            }
        }
        
        private bool IsPoolInUse(string address)
        {
            // Check if any instances are currently in use (not in pool)
            return _instanceToAddress.ContainsValue(address);
        }
        
        public Dictionary<string, object> GetResolverStatus()
        {
            var poolStats = new Dictionary<string, object>();
            foreach (var pool in _prefabPools)
            {
                poolStats[pool.Key] = new
                {
                    available = pool.Value.AvailableCount,
                    total_created = pool.Value.TotalCreated,
                    max_size = pool.Value.MaxSize
                };
            }
            
            return new Dictionary<string, object>
            {
                ["prefab_pooling_enabled"] = _enablePrefabPooling,
                ["async_instantiation_enabled"] = _enableAsyncInstantiation,
                ["active_pools"] = _prefabPools.Count,
                ["active_load_requests"] = _activeLoadRequests.Count,
                ["concurrent_loads"] = _activeConcurrentLoads,
                ["max_concurrent_loads"] = _maxConcurrentLoads,
                ["tracked_instances"] = _instanceToAddress.Count,
                ["pool_stats"] = poolStats
            };
        }
        
        public void ClearAllPools()
        {
            LoggingInfrastructure.LogInfo("AddressablePrefabResolver", "Clearing all prefab pools");
            
            foreach (var pool in _prefabPools.Values)
            {
                pool.Clear();
            }
            
            _prefabPools.Clear();
        }
        
        private void OnDestroy()
        {
            ClearAllPools();
        }
    }
    
    // Supporting Data Structures
    
    [System.Serializable]
    public class PrefabCategory
    {
        public string CategoryName;
        public string[] Addresses;
        public bool EnablePooling = true;
        public int PoolSize = 5;
        public bool PrewarmOnStart = false;
        public LoadPriority LoadPriority = LoadPriority.Normal;
    }
    
    [System.Serializable]
    public class PrefabMetrics
    {
        public string Address;
        public string Category;
        public LoadPriority Priority;
        public int TotalLoads;
        public int SuccessfulLoads;
        public int FailedLoads;
        public float TotalLoadTime;
        public float AverageLoadTime;
        public DateTime LastLoadTime;
        
        public float SuccessRate => TotalLoads > 0 ? (float)SuccessfulLoads / TotalLoads : 0f;
    }
    
    public class PrefabLoadRequest
    {
        public string Address;
        public DateTime StartTime;
        public LoadPriority Priority;
        public Task LoadTask = Task.CompletedTask;
        public GameObject Result;
    }
    
    public class PrefabPool
    {
        private readonly string _address;
        private readonly Queue<GameObject> _availableInstances = new Queue<GameObject>();
        private readonly int _maxSize;
        private int _totalCreated = 0;
        
        public string Address => _address;
        public int AvailableCount => _availableInstances.Count;
        public int TotalCreated => _totalCreated;
        public int MaxSize => _maxSize;
        
        public PrefabPool(string address, int initialSize, int maxSize)
        {
            _address = address;
            _maxSize = maxSize;
        }
        
        public GameObject GetFromPool()
        {
            if (_availableInstances.Count > 0)
            {
                return _availableInstances.Dequeue();
            }
            
            return null;
        }
        
        public void ReturnToPool(GameObject instance)
        {
            if (instance != null && _availableInstances.Count < _maxSize)
            {
                _availableInstances.Enqueue(instance);
            }
            else if (instance != null)
            {
                // Pool is full, destroy the instance
                UnityEngine.Object.Destroy(instance);
            }
        }
        
        public bool CanReturnToPool()
        {
            return _availableInstances.Count < _maxSize;
        }
        
        public bool HasAvailableInstances()
        {
            return _availableInstances.Count > 0;
        }
        
        public bool IsEmpty()
        {
            return _availableInstances.Count == 0;
        }
        
        public int Cleanup()
        {
            var cleanedCount = 0;
            var instancesToKeep = new Queue<GameObject>();
            
            while (_availableInstances.Count > 0)
            {
                var instance = _availableInstances.Dequeue();
                if (instance != null)
                {
                    instancesToKeep.Enqueue(instance);
                }
                else
                {
                    cleanedCount++;
                }
            }
            
            _availableInstances.Clear();
            while (instancesToKeep.Count > 0)
            {
                _availableInstances.Enqueue(instancesToKeep.Dequeue());
            }
            
            return cleanedCount;
        }
        
        public void Clear()
        {
            while (_availableInstances.Count > 0)
            {
                var instance = _availableInstances.Dequeue();
                if (instance != null)
                {
                    UnityEngine.Object.Destroy(instance);
                }
            }
            
            _availableInstances.Clear();
        }
    }
    
    public enum LoadPriority
    {
        Low,
        Normal,
        High,
        Critical
    }
}