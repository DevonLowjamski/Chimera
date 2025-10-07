using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Equipment.Degradation.Cache
{
    /// <summary>
    /// REFACTORED: Cache Storage Manager - Focused cache storage and retrieval operations
    /// Single Responsibility: Managing core cache storage, retrieval, and basic operations
    /// Extracted from EstimateCacheManager for better SRP compliance
    /// </summary>
    public class CacheStorageManager
    {
        private readonly bool _enableLogging;
        private readonly int _maxCacheSize;
        private readonly float _defaultCacheExpiration;

        // Core cache storage
        private readonly Dictionary<string, CachedEstimate> _estimateCache = new Dictionary<string, CachedEstimate>();
        private readonly Dictionary<string, CacheMetadata> _cacheMetadata = new Dictionary<string, CacheMetadata>();

        // Cache statistics
        private CacheStatistics _statistics = new CacheStatistics();

        // Events
        public event System.Action<string, CachedEstimate> OnCacheItemAdded;
        public event System.Action<string> OnCacheItemRemoved;
        public event System.Action<string, CachedEstimate> OnCacheItemAccessed;
        public event System.Action<CacheStatistics> OnStatisticsUpdated;

        public CacheStorageManager(bool enableLogging = false, int maxCacheSize = 1000, float defaultCacheExpiration = 3600f)
        {
            _enableLogging = enableLogging;
            _maxCacheSize = maxCacheSize;
            _defaultCacheExpiration = defaultCacheExpiration;
        }

        #region Core Cache Operations

        /// <summary>
        /// Store an estimate in the cache
        /// </summary>
        public bool StoreEstimate(string key, CachedEstimate estimate)
        {
            if (string.IsNullOrEmpty(key) || estimate == null)
                return false;

            try
            {
                // Check if cache is full
                if (_estimateCache.Count >= _maxCacheSize && !_estimateCache.ContainsKey(key))
                {
                    if (_enableLogging)
                        ChimeraLogger.LogWarning("CACHE", $"Cache full, cannot store estimate for key: {key}", null);
                    return false;
                }

                // Store estimate
                var previouslyExists = _estimateCache.ContainsKey(key);
                _estimateCache[key] = estimate;

                // Update metadata
                var metadata = new CacheMetadata
                {
                    CreatedAt = DateTime.Now,
                    LastAccessed = DateTime.Now,
                    AccessCount = 1,
                    ExpiresAt = DateTime.Now.AddSeconds(_defaultCacheExpiration),
                    Size = EstimateSize(estimate)
                };
                _cacheMetadata[key] = metadata;

                // Update statistics
                if (!previouslyExists)
                {
                    _statistics.TotalItems++;
                    _statistics.TotalMemoryUsage += metadata.Size;
                }

                OnCacheItemAdded?.Invoke(key, estimate);

                if (_enableLogging)
                    ChimeraLogger.LogInfo("CACHE", $"Stored estimate for key: {key}", null);

                return true;
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError("CACHE", $"Error storing estimate for key {key}: {ex.Message}", null);
                return false;
            }
        }

        /// <summary>
        /// Retrieve an estimate from the cache
        /// </summary>
        public CachedEstimate GetEstimate(string key)
        {
            if (string.IsNullOrEmpty(key))
                return null;

            try
            {
                if (_estimateCache.TryGetValue(key, out var estimate))
                {
                    // Check if expired
                    if (_cacheMetadata.TryGetValue(key, out var metadata))
                    {
                        if (DateTime.Now > metadata.ExpiresAt)
                        {
                            RemoveEstimate(key);
                            _statistics.ExpiredItems++;
                            return null;
                        }

                        // Update access metadata
                        metadata.LastAccessed = DateTime.Now;
                        metadata.AccessCount++;
                        _statistics.CacheHits++;
                    }

                    OnCacheItemAccessed?.Invoke(key, estimate);

                    if (_enableLogging)
                        ChimeraLogger.LogInfo("CACHE", $"Cache hit for key: {key}", null);

                    return estimate;
                }
                else
                {
                    _statistics.CacheMisses++;

                    if (_enableLogging)
                        ChimeraLogger.LogInfo("CACHE", $"Cache miss for key: {key}", null);

                    return null;
                }
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError("CACHE", $"Error retrieving estimate for key {key}: {ex.Message}", null);
                _statistics.CacheMisses++;
                return null;
            }
        }

        /// <summary>
        /// Remove an estimate from the cache
        /// </summary>
        public bool RemoveEstimate(string key)
        {
            if (string.IsNullOrEmpty(key))
                return false;

            try
            {
                if (_estimateCache.Remove(key))
                {
                    if (_cacheMetadata.TryGetValue(key, out var metadata))
                    {
                        _statistics.TotalMemoryUsage -= metadata.Size;
                        _cacheMetadata.Remove(key);
                    }

                    _statistics.TotalItems--;
                    OnCacheItemRemoved?.Invoke(key);

                    if (_enableLogging)
                        ChimeraLogger.LogInfo("CACHE", $"Removed estimate for key: {key}", null);

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError("CACHE", $"Error removing estimate for key {key}: {ex.Message}", null);
                return false;
            }
        }

        /// <summary>
        /// Check if an estimate exists in the cache
        /// </summary>
        public bool ContainsEstimate(string key)
        {
            if (string.IsNullOrEmpty(key))
                return false;

            var exists = _estimateCache.ContainsKey(key);

            // Check expiration if exists
            if (exists && _cacheMetadata.TryGetValue(key, out var metadata))
            {
                if (DateTime.Now > metadata.ExpiresAt)
                {
                    RemoveEstimate(key);
                    return false;
                }
            }

            return exists;
        }

        /// <summary>
        /// Update the expiration time for a cached estimate
        /// </summary>
        public bool UpdateExpiration(string key, DateTime newExpiration)
        {
            if (string.IsNullOrEmpty(key))
                return false;

            if (_cacheMetadata.TryGetValue(key, out var metadata))
            {
                metadata.ExpiresAt = newExpiration;
                return true;
            }

            return false;
        }

        #endregion

        #region Bulk Operations

        /// <summary>
        /// Store multiple estimates at once
        /// </summary>
        public int StoreBulkEstimates(Dictionary<string, CachedEstimate> estimates)
        {
            if (estimates == null)
                return 0;

            int successCount = 0;
            foreach (var kvp in estimates)
            {
                if (StoreEstimate(kvp.Key, kvp.Value))
                {
                    successCount++;
                }
            }

            if (_enableLogging)
                ChimeraLogger.LogInfo("CACHE", $"Bulk stored {successCount}/{estimates.Count} estimates", null);

            return successCount;
        }

        /// <summary>
        /// Remove multiple estimates at once
        /// </summary>
        public int RemoveBulkEstimates(IEnumerable<string> keys)
        {
            if (keys == null)
                return 0;

            int successCount = 0;
            foreach (var key in keys)
            {
                if (RemoveEstimate(key))
                {
                    successCount++;
                }
            }

            if (_enableLogging)
                ChimeraLogger.LogInfo("CACHE", $"Bulk removed {successCount} estimates", null);

            return successCount;
        }

        /// <summary>
        /// Clear all estimates from the cache
        /// </summary>
        public void ClearAll()
        {
            var previousCount = _estimateCache.Count;
            _estimateCache.Clear();
            _cacheMetadata.Clear();

            _statistics = new CacheStatistics();

            if (_enableLogging)
                ChimeraLogger.LogInfo("CACHE", $"Cleared all {previousCount} estimates from cache", null);
        }

        #endregion

        #region Query Operations

        /// <summary>
        /// Get all cache keys
        /// </summary>
        public IEnumerable<string> GetAllKeys()
        {
            return _estimateCache.Keys.ToList();
        }

        /// <summary>
        /// Get cache keys matching a pattern
        /// </summary>
        public IEnumerable<string> GetKeysByPattern(string pattern)
        {
            if (string.IsNullOrEmpty(pattern))
                return Enumerable.Empty<string>();

            return _estimateCache.Keys.Where(key => key.Contains(pattern));
        }

        /// <summary>
        /// Get expired cache keys
        /// </summary>
        public IEnumerable<string> GetExpiredKeys()
        {
            var now = DateTime.Now;
            return _cacheMetadata
                .Where(kvp => now > kvp.Value.ExpiresAt)
                .Select(kvp => kvp.Key);
        }

        /// <summary>
        /// Get cache metadata for a key
        /// </summary>
        public CacheMetadata GetMetadata(string key)
        {
            return _cacheMetadata.TryGetValue(key, out var metadata) ? metadata : null;
        }

        /// <summary>
        /// Get all cache metadata
        /// </summary>
        public Dictionary<string, CacheMetadata> GetAllMetadata()
        {
            return new Dictionary<string, CacheMetadata>(_cacheMetadata);
        }

        #endregion

        #region Statistics and Monitoring

        /// <summary>
        /// Get current cache statistics
        /// </summary>
        public CacheStatistics GetStatistics()
        {
            _statistics.CacheHitRate = (_statistics.CacheHits + _statistics.CacheMisses) > 0
                ? (float)_statistics.CacheHits / (_statistics.CacheHits + _statistics.CacheMisses)
                : 0f;

            _statistics.MemoryUtilization = _maxCacheSize > 0
                ? (float)_statistics.TotalItems / _maxCacheSize
                : 0f;

            OnStatisticsUpdated?.Invoke(_statistics);
            return _statistics;
        }

        /// <summary>
        /// Reset cache statistics
        /// </summary>
        public void ResetStatistics()
        {
            _statistics = new CacheStatistics
            {
                TotalItems = _estimateCache.Count,
                TotalMemoryUsage = _cacheMetadata.Values.Sum(m => m.Size)
            };

            if (_enableLogging)
                ChimeraLogger.LogInfo("CACHE", "Cache statistics reset", null);
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Estimate the memory size of a cached estimate
        /// </summary>
        private long EstimateSize(CachedEstimate estimate)
        {
            // Simple size estimation (could be more sophisticated)
            long size = 0;
            size += estimate.EstimateKey?.Length * 2 ?? 0; // String size approximation
            size += sizeof(float) * 3; // Cost, Confidence, Timestamp
            size += estimate.Parameters?.Count * 50 ?? 0; // Rough parameter size
            return size;
        }

        /// <summary>
        /// Get cache utilization information
        /// </summary>
        public CacheUtilization GetUtilization()
        {
            return new CacheUtilization
            {
                CurrentItems = _estimateCache.Count,
                MaxItems = _maxCacheSize,
                UtilizationPercentage = _maxCacheSize > 0 ? (float)_estimateCache.Count / _maxCacheSize : 0f,
                TotalMemoryUsage = _statistics.TotalMemoryUsage,
                AverageItemSize = _estimateCache.Count > 0 ? _statistics.TotalMemoryUsage / _estimateCache.Count : 0
            };
        }

        #endregion
    }

    /// <summary>
    /// Cache statistics tracking
    /// </summary>
    [System.Serializable]
    public class CacheStatistics
    {
        public int TotalItems = 0;
        public int CacheHits = 0;
        public int CacheMisses = 0;
        public int ExpiredItems = 0;
        public long TotalMemoryUsage = 0;
        public float CacheHitRate = 0f;
        public float MemoryUtilization = 0f;
    }

    /// <summary>
    /// Cache metadata for tracking
    /// </summary>
    [System.Serializable]
    public class CacheMetadata
    {
        public DateTime CreatedAt;
        public DateTime LastAccessed;
        public DateTime ExpiresAt;
        public int AccessCount;
        public long Size;
    }

    /// <summary>
    /// Cache utilization information
    /// </summary>
    [System.Serializable]
    public struct CacheUtilization
    {
        public int CurrentItems;
        public int MaxItems;
        public float UtilizationPercentage;
        public long TotalMemoryUsage;
        public long AverageItemSize;
    }

    /// <summary>
    /// Cached estimate data structure
    /// </summary>
    [System.Serializable]
    public class CachedEstimate
    {
        public string EstimateKey;
        public float Cost;
        public float Confidence;
        public float Timestamp;
        public Dictionary<string, object> Parameters;
        public string EquipmentType;
        public string MalfunctionType;
    }
}