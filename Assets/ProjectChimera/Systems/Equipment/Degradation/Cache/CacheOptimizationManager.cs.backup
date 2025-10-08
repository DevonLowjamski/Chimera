using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;

namespace ProjectChimera.Systems.Equipment.Degradation.Cache
{
    /// <summary>
    /// REFACTORED: Cache Optimization Manager - Focused cache optimization and memory management
    /// Single Responsibility: Managing cache optimization, eviction policies, and memory efficiency
    /// Extracted from EstimateCacheManager for better SRP compliance
    /// </summary>
    public class CacheOptimizationManager : ITickable
    {
        private readonly bool _enableLogging;
        private readonly float _optimizationInterval;
        private readonly float _memoryThreshold;
        private readonly int _compressionThreshold;
        private readonly CacheEvictionPolicy _evictionPolicy;

        // Cache optimization structures
        private readonly Queue<string> _lruQueue = new Queue<string>();
        private readonly Dictionary<string, int> _accessFrequency = new Dictionary<string, int>();
        private readonly SortedDictionary<DateTime, List<string>> _expirationIndex = new SortedDictionary<DateTime, List<string>>();

        // Optimization state
        private float _lastOptimizationTime;
        private OptimizationStatistics _optimizationStats = new OptimizationStatistics();

        // Dependencies
        private CacheStorageManager _storageManager;

        // Events
        public event System.Action<OptimizationResult> OnOptimizationCompleted;
        public event System.Action<string> OnItemEvicted;
        public event System.Action<MemoryPressureLevel> OnMemoryPressure;

        public CacheOptimizationManager(bool enableLogging = false, float optimizationInterval = 600f,
                                      float memoryThreshold = 0.8f, int compressionThreshold = 100,
                                      CacheEvictionPolicy evictionPolicy = CacheEvictionPolicy.LRU)
        {
            _enableLogging = enableLogging;
            _optimizationInterval = optimizationInterval;
            _memoryThreshold = memoryThreshold;
            _compressionThreshold = compressionThreshold;
            _evictionPolicy = evictionPolicy;
        }

        // ITickable implementation
        public int TickPriority => ProjectChimera.Core.Updates.TickPriority.CacheOptimization;
        public bool IsTickable => _storageManager != null;

        #region Initialization

        /// <summary>
        /// Initialize with storage manager dependency
        /// </summary>
        public void Initialize(CacheStorageManager storageManager)
        {
            _storageManager = storageManager ?? throw new ArgumentNullException(nameof(storageManager));

            // Wire up events
            _storageManager.OnCacheItemAdded += OnCacheItemAdded;
            _storageManager.OnCacheItemAccessed += OnCacheItemAccessed;
            _storageManager.OnCacheItemRemoved += OnCacheItemRemoved;

            _lastOptimizationTime = Time.time;

            if (_enableLogging)
                ChimeraLogger.LogInfo("CACHE_OPT", "Cache optimization manager initialized", null);
        }

        #endregion

        #region ITickable Implementation

        public void Tick(float deltaTime)
        {
            if (Time.time - _lastOptimizationTime >= _optimizationInterval)
            {
                PerformOptimization();
                _lastOptimizationTime = Time.time;
            }

            // Check for memory pressure more frequently
            CheckMemoryPressure();
        }

        public void OnRegistered()
        {
            if (_enableLogging)
                ChimeraLogger.LogInfo("CACHE_OPT", "Cache optimization manager registered with UpdateOrchestrator", null);
        }

        public void OnUnregistered()
        {
            if (_enableLogging)
                ChimeraLogger.LogInfo("CACHE_OPT", "Cache optimization manager unregistered from UpdateOrchestrator", null);
        }

        #endregion

        #region Optimization Operations

        /// <summary>
        /// Perform comprehensive cache optimization
        /// </summary>
        public OptimizationResult PerformOptimization()
        {
            var startTime = Time.realtimeSinceStartup;
            var result = new OptimizationResult { StartTime = DateTime.Now };

            try
            {
                if (_enableLogging)
                    ChimeraLogger.LogInfo("CACHE_OPT", "Starting cache optimization", null);

                // Step 1: Remove expired items
                var expiredCount = RemoveExpiredItems();
                result.ExpiredItemsRemoved = expiredCount;

                // Step 2: Check memory pressure and evict if necessary
                var evictedCount = HandleMemoryPressure();
                result.ItemsEvicted = evictedCount;

                // Step 3: Optimize data structures
                OptimizeDataStructures();

                // Step 4: Update optimization statistics
                UpdateOptimizationStatistics();

                result.Success = true;
                result.ExecutionTime = Time.realtimeSinceStartup - startTime;

                _optimizationStats.TotalOptimizations++;
                _optimizationStats.TotalItemsEvicted += evictedCount;
                _optimizationStats.TotalExpiredItemsRemoved += expiredCount;

                OnOptimizationCompleted?.Invoke(result);

                if (_enableLogging)
                {
                    ChimeraLogger.LogInfo("CACHE_OPT",
                        $"Optimization completed: {expiredCount} expired, {evictedCount} evicted in {result.ExecutionTime:F2}s", null);
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                ChimeraLogger.LogError("CACHE_OPT", $"Optimization failed: {ex.Message}", null);
            }

            return result;
        }

        /// <summary>
        /// Remove all expired cache items
        /// </summary>
        private int RemoveExpiredItems()
        {
            var expiredKeys = _storageManager.GetExpiredKeys().ToList();
            return _storageManager.RemoveBulkEstimates(expiredKeys);
        }

        /// <summary>
        /// Handle memory pressure by evicting items
        /// </summary>
        private int HandleMemoryPressure()
        {
            var utilization = _storageManager.GetUtilization();

            if (utilization.UtilizationPercentage < _memoryThreshold)
                return 0;

            var pressureLevel = GetMemoryPressureLevel(utilization.UtilizationPercentage);
            OnMemoryPressure?.Invoke(pressureLevel);

            // Calculate how many items to evict
            var targetUtilization = _memoryThreshold * 0.8f; // Target 80% of threshold
            var itemsToEvict = (int)((utilization.UtilizationPercentage - targetUtilization) * utilization.MaxItems);

            return EvictItems(itemsToEvict);
        }

        /// <summary>
        /// Evict items based on the configured eviction policy
        /// </summary>
        private int EvictItems(int itemCount)
        {
            if (itemCount <= 0)
                return 0;

            var keysToEvict = SelectItemsForEviction(itemCount);
            var evictedCount = _storageManager.RemoveBulkEstimates(keysToEvict);

            foreach (var key in keysToEvict)
            {
                OnItemEvicted?.Invoke(key);
                RemoveFromOptimizationStructures(key);
            }

            return evictedCount;
        }

        /// <summary>
        /// Select items for eviction based on policy
        /// </summary>
        private IEnumerable<string> SelectItemsForEviction(int itemCount)
        {
            switch (_evictionPolicy)
            {
                case CacheEvictionPolicy.LRU:
                    return SelectLRUItems(itemCount);
                case CacheEvictionPolicy.LFU:
                    return SelectLFUItems(itemCount);
                case CacheEvictionPolicy.FIFO:
                    return SelectFIFOItems(itemCount);
                case CacheEvictionPolicy.Random:
                    return SelectRandomItems(itemCount);
                default:
                    return SelectLRUItems(itemCount);
            }
        }

        /// <summary>
        /// Select Least Recently Used items
        /// </summary>
        private IEnumerable<string> SelectLRUItems(int itemCount)
        {
            var allMetadata = _storageManager.GetAllMetadata();
            return allMetadata
                .OrderBy(kvp => kvp.Value.LastAccessed)
                .Take(itemCount)
                .Select(kvp => kvp.Key);
        }

        /// <summary>
        /// Select Least Frequently Used items
        /// </summary>
        private IEnumerable<string> SelectLFUItems(int itemCount)
        {
            return _accessFrequency
                .OrderBy(kvp => kvp.Value)
                .Take(itemCount)
                .Select(kvp => kvp.Key);
        }

        /// <summary>
        /// Select First In First Out items
        /// </summary>
        private IEnumerable<string> SelectFIFOItems(int itemCount)
        {
            var allMetadata = _storageManager.GetAllMetadata();
            return allMetadata
                .OrderBy(kvp => kvp.Value.CreatedAt)
                .Take(itemCount)
                .Select(kvp => kvp.Key);
        }

        /// <summary>
        /// Select random items for eviction
        /// </summary>
        private IEnumerable<string> SelectRandomItems(int itemCount)
        {
            var allKeys = _storageManager.GetAllKeys().ToList();
            var random = new System.Random();

            return allKeys
                .OrderBy(x => random.Next())
                .Take(itemCount);
        }

        #endregion

        #region Data Structure Optimization

        /// <summary>
        /// Optimize internal data structures
        /// </summary>
        private void OptimizeDataStructures()
        {
            // Clean up LRU queue
            OptimizeLRUQueue();

            // Clean up expiration index
            OptimizeExpirationIndex();

            // Compress access frequency data if needed
            if (_accessFrequency.Count > _compressionThreshold)
            {
                CompressAccessFrequency();
            }
        }

        /// <summary>
        /// Optimize LRU queue by removing invalid entries
        /// </summary>
        private void OptimizeLRUQueue()
        {
            var validKeys = new HashSet<string>(_storageManager.GetAllKeys());
            var newQueue = new Queue<string>();

            while (_lruQueue.Count > 0)
            {
                var key = _lruQueue.Dequeue();
                if (validKeys.Contains(key))
                {
                    newQueue.Enqueue(key);
                }
            }

            _lruQueue.Clear();
            while (newQueue.Count > 0)
            {
                _lruQueue.Enqueue(newQueue.Dequeue());
            }
        }

        /// <summary>
        /// Optimize expiration index by removing past entries
        /// </summary>
        private void OptimizeExpirationIndex()
        {
            var now = DateTime.Now;
            var keysToRemove = _expirationIndex.Keys.Where(dt => dt < now).ToList();

            foreach (var key in keysToRemove)
            {
                _expirationIndex.Remove(key);
            }
        }

        /// <summary>
        /// Compress access frequency data
        /// </summary>
        private void CompressAccessFrequency()
        {
            var validKeys = new HashSet<string>(_storageManager.GetAllKeys());
            var keysToRemove = _accessFrequency.Keys.Where(k => !validKeys.Contains(k)).ToList();

            foreach (var key in keysToRemove)
            {
                _accessFrequency.Remove(key);
            }

            // Normalize frequency values if they get too large
            var maxFrequency = _accessFrequency.Values.DefaultIfEmpty(0).Max();
            if (maxFrequency > 10000)
            {
                var keys = _accessFrequency.Keys.ToList();
                foreach (var key in keys)
                {
                    _accessFrequency[key] = _accessFrequency[key] / 10;
                }
            }
        }

        #endregion

        #region Event Handlers

        private void OnCacheItemAdded(string key, CachedEstimate estimate)
        {
            // Update LRU queue
            _lruQueue.Enqueue(key);

            // Initialize access frequency
            _accessFrequency[key] = 1;

            // Add to expiration index
            var metadata = _storageManager.GetMetadata(key);
            if (metadata != null)
            {
                if (!_expirationIndex.TryGetValue(metadata.ExpiresAt, out var list))
                {
                    list = new List<string>();
                    _expirationIndex[metadata.ExpiresAt] = list;
                }
                list.Add(key);
            }
        }

        private void OnCacheItemAccessed(string key, CachedEstimate estimate)
        {
            // Update access frequency
            if (_accessFrequency.ContainsKey(key))
            {
                _accessFrequency[key]++;
            }
            else
            {
                _accessFrequency[key] = 1;
            }
        }

        private void OnCacheItemRemoved(string key)
        {
            RemoveFromOptimizationStructures(key);
        }

        private void RemoveFromOptimizationStructures(string key)
        {
            // Remove from access frequency
            _accessFrequency.Remove(key);

            // Remove from expiration index (expensive operation, done during optimization)
        }

        #endregion

        #region Memory Pressure Management

        private void CheckMemoryPressure()
        {
            var utilization = _storageManager.GetUtilization();
            var pressureLevel = GetMemoryPressureLevel(utilization.UtilizationPercentage);

            if (pressureLevel != MemoryPressureLevel.Normal)
            {
                OnMemoryPressure?.Invoke(pressureLevel);

                if (pressureLevel == MemoryPressureLevel.Critical)
                {
                    // Immediate eviction for critical pressure
                    var itemsToEvict = (int)(utilization.CurrentItems * 0.1f); // Evict 10%
                    EvictItems(itemsToEvict);
                }
            }
        }

        private MemoryPressureLevel GetMemoryPressureLevel(float utilizationPercentage)
        {
            if (utilizationPercentage >= 0.95f)
                return MemoryPressureLevel.Critical;
            else if (utilizationPercentage >= _memoryThreshold)
                return MemoryPressureLevel.High;
            else if (utilizationPercentage >= _memoryThreshold * 0.8f)
                return MemoryPressureLevel.Medium;
            else
                return MemoryPressureLevel.Normal;
        }

        #endregion

        #region Statistics and Monitoring

        private void UpdateOptimizationStatistics()
        {
            _optimizationStats.LastOptimizationTime = DateTime.Now;
            _optimizationStats.CurrentCacheUtilization = _storageManager.GetUtilization().UtilizationPercentage;
        }

        /// <summary>
        /// Get optimization statistics
        /// </summary>
        public OptimizationStatistics GetOptimizationStatistics()
        {
            return _optimizationStats;
        }

        /// <summary>
        /// Reset optimization statistics
        /// </summary>
        public void ResetStatistics()
        {
            _optimizationStats = new OptimizationStatistics();

            if (_enableLogging)
                ChimeraLogger.LogInfo("CACHE_OPT", "Optimization statistics reset", null);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Force immediate optimization
        /// </summary>
        public OptimizationResult ForceOptimization()
        {
            return PerformOptimization();
        }

        /// <summary>
        /// Manually evict specific items
        /// </summary>
        public bool EvictItem(string key)
        {
            if (_storageManager.ContainsEstimate(key))
            {
                var success = _storageManager.RemoveEstimate(key);
                if (success)
                {
                    OnItemEvicted?.Invoke(key);
                    RemoveFromOptimizationStructures(key);
                }
                return success;
            }
            return false;
        }

        /// <summary>
        /// Get current memory pressure level
        /// </summary>
        public MemoryPressureLevel GetCurrentMemoryPressure()
        {
            var utilization = _storageManager.GetUtilization();
            return GetMemoryPressureLevel(utilization.UtilizationPercentage);
        }

        #endregion
    }

    /// <summary>
    /// Cache eviction policies
    /// </summary>
    public enum CacheEvictionPolicy
    {
        LRU,    // Least Recently Used
        LFU,    // Least Frequently Used
        FIFO,   // First In First Out
        Random  // Random eviction
    }

    /// <summary>
    /// Memory pressure levels
    /// </summary>
    public enum MemoryPressureLevel
    {
        Normal,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// Optimization result information
    /// </summary>
    [System.Serializable]
    public struct OptimizationResult
    {
        public bool Success;
        public DateTime StartTime;
        public float ExecutionTime;
        public int ExpiredItemsRemoved;
        public int ItemsEvicted;
        public string ErrorMessage;
    }

    /// <summary>
    /// Optimization statistics tracking
    /// </summary>
    [System.Serializable]
    public class OptimizationStatistics
    {
        public int TotalOptimizations = 0;
        public int TotalItemsEvicted = 0;
        public int TotalExpiredItemsRemoved = 0;
        public DateTime LastOptimizationTime = DateTime.MinValue;
        public float CurrentCacheUtilization = 0f;
    }
}