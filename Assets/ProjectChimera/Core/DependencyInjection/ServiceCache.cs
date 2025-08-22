using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectChimera.Core.DependencyInjection
{
    /// <summary>
    /// Handles intelligent caching for service resolution in the ServiceLocator
    /// </summary>
    public class ServiceCache
    {
        private readonly Dictionary<Type, object> _resolutionCache = new Dictionary<Type, object>();
        private readonly Dictionary<Type, DateTime> _cacheTimestamps = new Dictionary<Type, DateTime>();
        private readonly TimeSpan _cacheExpirationTime = TimeSpan.FromMinutes(5);
        
        private bool _cachingEnabled = true;
        private int _cacheHits = 0;
        
        public bool CachingEnabled 
        { 
            get => _cachingEnabled; 
            set 
            { 
                _cachingEnabled = value;
                if (!value)
                {
                    ClearCache();
                }
            } 
        }
        
        public int CacheHits => _cacheHits;
        public int CachedTypesCount => _resolutionCache.Count;
        
        /// <summary>
        /// Tries to get an instance from cache
        /// </summary>
        public bool TryGetFromCache(Type serviceType, Dictionary<Type, object> singletonInstances, out object instance)
        {
            instance = null;
            
            if (!_cachingEnabled) return false;
            
            // Check singleton cache first (highest priority)
            if (singletonInstances.TryGetValue(serviceType, out instance))
            {
                _cacheHits++;
                return true;
            }
            
            // Check resolution cache
            if (_resolutionCache.TryGetValue(serviceType, out instance))
            {
                // Validate cache timestamp
                if (IsCacheValid(serviceType))
                {
                    _cacheHits++;
                    return true;
                }
                else
                {
                    // Cache expired, remove it
                    _resolutionCache.Remove(serviceType);
                    _cacheTimestamps.Remove(serviceType);
                    instance = null;
                }
            }
            
            return false;
        }
        
        /// <summary>
        /// Caches an instance for future resolution
        /// </summary>
        public void CacheInstance(Type serviceType, object instance, Dictionary<Type, object> singletonInstances)
        {
            if (!_cachingEnabled || instance == null) return;
            
            // Don't cache singletons in resolution cache (they're already in singleton cache)
            if (singletonInstances.ContainsKey(serviceType)) return;
            
            _resolutionCache[serviceType] = instance;
            _cacheTimestamps[serviceType] = DateTime.Now;
        }
        
        /// <summary>
        /// Checks if cached data is still valid
        /// </summary>
        private bool IsCacheValid(Type serviceType)
        {
            if (_cacheTimestamps.TryGetValue(serviceType, out var timestamp))
            {
                return DateTime.Now - timestamp < _cacheExpirationTime;
            }
            return false;
        }
        
        /// <summary>
        /// Clears all cached instances
        /// </summary>
        public void ClearCache()
        {
            _resolutionCache.Clear();
            _cacheTimestamps.Clear();
            Debug.Log("[ServiceCache] Resolution cache cleared");
        }
        
        /// <summary>
        /// Removes expired cache entries
        /// </summary>
        public int CleanupExpiredEntries()
        {
            var expiredTypes = new List<Type>();
            
            foreach (var kvp in _cacheTimestamps)
            {
                if (!IsCacheValid(kvp.Key))
                {
                    expiredTypes.Add(kvp.Key);
                }
            }
            
            foreach (var expiredType in expiredTypes)
            {
                _resolutionCache.Remove(expiredType);
                _cacheTimestamps.Remove(expiredType);
            }
            
            if (expiredTypes.Count > 0)
            {
                Debug.Log($"[ServiceCache] Cleaned up {expiredTypes.Count} expired cache entries");
            }
            
            return expiredTypes.Count;
        }
        
        /// <summary>
        /// Gets cache statistics
        /// </summary>
        public ServiceCacheMetrics GetMetrics()
        {
            return new ServiceCacheMetrics
            {
                CacheHits = _cacheHits,
                CachedTypes = _resolutionCache.Count,
                CachingEnabled = _cachingEnabled,
                ExpiredEntries = CleanupExpiredEntries()
            };
        }
    }
    
    /// <summary>
    /// Metrics for service cache performance
    /// </summary>
    public class ServiceCacheMetrics
    {
        public int CacheHits { get; set; }
        public int CachedTypes { get; set; }
        public bool CachingEnabled { get; set; }
        public int ExpiredEntries { get; set; }
        
        public string GetSummary()
        {
            return $"Cache: {(CachingEnabled ? "Enabled" : "Disabled")} | " +
                   $"Hits: {CacheHits} | Types: {CachedTypes} | Expired: {ExpiredEntries}";
        }
    }
}