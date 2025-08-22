using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System;

namespace ProjectChimera.UI.Core
{
    /// <summary>
    /// Manages data caching, binding, and updates for the UI system.
    /// Handles efficient data refresh and provides cached access to system data.
    /// Modernized to use generic data providers instead of typed delegates.
    /// </summary>
    public class UIDataManager : IDisposable
    {
        private Dictionary<string, CachedData> _dataCache = new Dictionary<string, CachedData>();
        private Dictionary<string, List<IDataBinding>> _dataBindings = new Dictionary<string, List<IDataBinding>>();
        private Dictionary<string, IDataProvider> _dataProviders = new Dictionary<string, IDataProvider>();
        private float _cacheExpirationTime = 1f; // 1 second cache
        
        // Common data key constants for type safety
        public const string ENVIRONMENTAL_DATA_KEY = "environmental_data";
        public const string PLANT_STATUS_DATA_KEY = "plant_status_data";
        public const string SYSTEM_PERFORMANCE_DATA_KEY = "system_performance_data";
        public const string CONSTRUCTION_DATA_KEY = "construction_data";
        public const string ECONOMIC_DATA_KEY = "economic_data";
        
        /// <summary>
        /// Register a generic data provider for a specific data key
        /// </summary>
        public void RegisterDataProvider<T>(string dataKey, IDataProvider<T> provider) where T : class
        {
            _dataProviders[dataKey] = provider;
        }
        
        /// <summary>
        /// Register a data provider using a simple function delegate
        /// </summary>
        public void RegisterDataProvider<T>(string dataKey, System.Func<T> providerFunc) where T : class
        {
            _dataProviders[dataKey] = new FuncDataProvider<T>(providerFunc);
        }
        
        /// <summary>
        /// Unregister a data provider
        /// </summary>
        public void UnregisterDataProvider(string dataKey)
        {
            _dataProviders.Remove(dataKey);
        }
        
        /// <summary>
        /// Get cached data using the registered provider for the data key
        /// </summary>
        public T GetCachedData<T>(string dataKey) where T : class
        {
            if (_dataCache.TryGetValue(dataKey, out var cachedData))
            {
                if (Time.time - cachedData.Timestamp < _cacheExpirationTime)
                {
                    return cachedData.Data as T;
                }
            }
            
            // Cache expired or doesn't exist, refresh using registered provider
            if (_dataProviders.TryGetValue(dataKey, out var provider))
            {
                if (provider is IDataProvider<T> typedProvider)
                {
                    var newData = typedProvider.GetData();
                    if (newData != null)
                    {
                        _dataCache[dataKey] = new CachedData
                        {
                            Data = newData,
                            Timestamp = Time.time
                        };
                        
                        // Notify bindings
                        NotifyDataBindings(dataKey, newData);
                    }
                    return newData;
                }
            }
            
            return null;
        }
        
        /// <summary>
        /// Get cached data with explicit data provider (legacy method for backward compatibility)
        /// </summary>
        public T GetCachedData<T>(string dataKey, System.Func<T> dataProvider) where T : class
        {
            if (_dataCache.TryGetValue(dataKey, out var cachedData))
            {
                if (Time.time - cachedData.Timestamp < _cacheExpirationTime)
                {
                    return cachedData.Data as T;
                }
            }
            
            // Cache expired or doesn't exist, refresh
            var newData = dataProvider?.Invoke();
            if (newData != null)
            {
                _dataCache[dataKey] = new CachedData
                {
                    Data = newData,
                    Timestamp = Time.time
                };
                
                // Notify bindings
                NotifyDataBindings(dataKey, newData);
            }
            
            return newData;
        }
        
        public void RegisterDataBinding(string dataKey, IDataBinding binding)
        {
            if (!_dataBindings.ContainsKey(dataKey))
            {
                _dataBindings[dataKey] = new List<IDataBinding>();
            }
            
            _dataBindings[dataKey].Add(binding);
        }
        
        public void UnregisterDataBinding(string dataKey, IDataBinding binding)
        {
            if (_dataBindings.TryGetValue(dataKey, out var bindings))
            {
                bindings.Remove(binding);
            }
        }
        
        private void NotifyDataBindings(string dataKey, object data)
        {
            if (_dataBindings.TryGetValue(dataKey, out var bindings))
            {
                foreach (var binding in bindings.ToList())
                {
                    binding.UpdateData(data);
                }
            }
        }
        
        public void RefreshAllData()
        {
            var keysToRefresh = _dataCache.Keys.ToList();
            foreach (var key in keysToRefresh)
            {
                // Force cache expiration
                _dataCache[key].Timestamp = 0f;
            }
        }
        
        public void ClearCache()
        {
            _dataCache.Clear();
        }
        
        /// <summary>
        /// Clear cache for specific data key
        /// </summary>
        public void ClearCache(string dataKey)
        {
            _dataCache.Remove(dataKey);
        }
        
        /// <summary>
        /// Force refresh data for specific key
        /// </summary>
        public T RefreshData<T>(string dataKey) where T : class
        {
            // Remove from cache to force refresh
            _dataCache.Remove(dataKey);
            return GetCachedData<T>(dataKey);
        }
        
        /// <summary>
        /// Check if data provider is registered for a key
        /// </summary>
        public bool HasProvider(string dataKey)
        {
            return _dataProviders.ContainsKey(dataKey);
        }
        
        /// <summary>
        /// Get all registered data keys
        /// </summary>
        public string[] GetRegisteredKeys()
        {
            return _dataProviders.Keys.ToArray();
        }
        
        /// <summary>
        /// Set cache expiration time in seconds
        /// </summary>
        public void SetCacheExpirationTime(float seconds)
        {
            _cacheExpirationTime = Mathf.Max(0.1f, seconds);
        }
        
        public void Dispose()
        {
            _dataCache.Clear();
            _dataBindings.Clear();
            _dataProviders.Clear();
        }
        
        private class CachedData
        {
            public object Data;
            public float Timestamp;
        }
    }
    
    public interface IDataBinding
    {
        void UpdateData(object data);
    }
    
    /// <summary>
    /// Base interface for data providers
    /// </summary>
    public interface IDataProvider
    {
        // Marker interface for type safety
    }
    
    /// <summary>
    /// Generic interface for typed data providers
    /// </summary>
    public interface IDataProvider<T> : IDataProvider where T : class
    {
        T GetData();
    }
    
    /// <summary>
    /// Implementation of IDataProvider using a function delegate
    /// </summary>
    public class FuncDataProvider<T> : IDataProvider<T> where T : class
    {
        private readonly System.Func<T> _dataProvider;
        
        public FuncDataProvider(System.Func<T> dataProvider)
        {
            _dataProvider = dataProvider ?? throw new ArgumentNullException(nameof(dataProvider));
        }
        
        public T GetData()
        {
            return _dataProvider.Invoke();
        }
    }
}