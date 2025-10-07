using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using Logger = ProjectChimera.Core.Logging.ChimeraLogger;

namespace ProjectChimera.Core
{
    /// <summary>
    /// SIMPLE: Basic data service provider aligned with Project Chimera's data needs.
    /// Focuses on essential data management without complex service architectures.
    /// </summary>
    public class DataServiceProvider : MonoBehaviour
    {
        [Header("Basic Data Settings")]
        [SerializeField] private bool _enableBasicDataServices = true;
        [SerializeField] private bool _enableLogging = true;

        // Basic service registry
        private readonly Dictionary<string, object> _services = new Dictionary<string, object>();
        private bool _isInitialized = false;

        // Logging methods
        private void LogProviderAction(string action)
        {
            Logger.LogInfo("DataServiceProvider", action);
        }

        private void LogProviderError(string error)
        {
            Logger.LogInfo("DataServiceProvider", error);
        }

        private void LogProviderWarning(string warning)
        {
            Logger.LogInfo("DataServiceProvider", warning);
        }

        // Configuration method
        public void ConfigureProvider()
        {
            LogProviderAction("Configuring DataServiceProvider");
            // Configuration logic can be added here if needed
        }

        /// <summary>
        /// Initialize basic data services
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            if (_enableBasicDataServices)
            {
                RegisterBasicServices();
            }

            _isInitialized = true;

            if (_enableLogging)
            {
                Logger.LogInfo("DataServiceProvider", "Initialized");
            }
        }

        /// <summary>
        /// Register a service
        /// </summary>
        public void RegisterService<T>(T service) where T : class
        {
            string serviceName = typeof(T).Name;
            _services[serviceName] = service;

            if (_enableLogging)
            {
                Logger.LogInfo("DataServiceProvider", $"Registered service {serviceName}");
            }
        }

        /// <summary>
        /// Get a registered service
        /// </summary>
        public T GetService<T>() where T : class
        {
            string serviceName = typeof(T).Name;
            if (_services.TryGetValue(serviceName, out object service))
            {
                return service as T;
            }

            if (_enableLogging)
            {
                Logger.LogInfo("DataServiceProvider", $"Service not found: {serviceName}");
            }

            return null;
        }

        /// <summary>
        /// Check if service is registered
        /// </summary>
        public bool HasService<T>() where T : class
        {
            string serviceName = typeof(T).Name;
            return _services.ContainsKey(serviceName);
        }

        /// <summary>
        /// Unregister a service
        /// </summary>
        public void UnregisterService<T>() where T : class
        {
            string serviceName = typeof(T).Name;
            if (_services.Remove(serviceName))
            {
                if (_enableLogging)
                {
                    Logger.LogInfo("DataServiceProvider", $"Unregistered service {serviceName}");
                }
            }
        }

        /// <summary>
        /// Get all registered services
        /// </summary>
        public Dictionary<string, object> GetAllServices()
        {
            return new Dictionary<string, object>(_services);
        }

        /// <summary>
        /// Clear all services
        /// </summary>
        public void ClearAllServices()
        {
            _services.Clear();

            if (_enableLogging)
            {
                Logger.LogInfo("DataServiceProvider", "Cleared all services");
            }
        }

        /// <summary>
        /// Get service count
        /// </summary>
        public int GetServiceCount()
        {
            return _services.Count;
        }

        /// <summary>
        /// Get service names
        /// </summary>
        public List<string> GetServiceNames()
        {
            return new List<string>(_services.Keys);
        }

        #region Private Methods

        private void RegisterBasicServices()
        {
            // Register basic data services that are commonly needed
            // This could include things like basic save/load, settings, etc.
            // For now, keeping it minimal as specific services should be registered by their respective systems
        }

        /// <summary>
        /// Get data service statistics
        /// </summary>
        public DataServiceStats GetDataStats()
        {
            return new DataServiceStats
            {
                TotalRegistered = _services.Count,
                ActiveServices = _services.Count,
                CachedItems = 0, // No caching implemented yet
                AverageResponseTime = 0f,
                TotalRequests = 0,
                FailedRequests = 0
            };
        }

        /// <summary>
        /// Statistics for data services
        /// </summary>
        public class DataServiceStats
        {
            public int TotalRegistered { get; set; }
            public int ActiveServices { get; set; }
            public int CachedItems { get; set; }
            public float AverageResponseTime { get; set; }
            public long TotalRequests { get; set; }
            public long FailedRequests { get; set; }
            public System.DateTime LastUpdate { get; set; }

            public DataServiceStats()
            {
                LastUpdate = System.DateTime.Now;
            }

            public float GetSuccessRate()
            {
                if (TotalRequests == 0) return 1f;
                return (float)(TotalRequests - FailedRequests) / TotalRequests;
            }
        }

        // Additional methods for ChimeraServiceModule compatibility
        public void RegisterDataServices()
        {
            RegisterBasicServices();
            LogProviderAction("Registered data services");
        }

        public void InitializeServices()
        {
            if (!_isInitialized)
            {
                _isInitialized = true;
                LogProviderAction("Initialized data services");
            }
        }

        public bool ValidateServices()
        {
            var hasServices = _services.Count > 0;
            if (!hasServices)
            {
                LogProviderWarning("No data services registered");
            }
            else
            {
                LogProviderAction($"Validated {_services.Count} data services");
            }
            return hasServices;
        }

        #endregion
    }

    /// <summary>
    /// Basic data service interface
    /// </summary>
    public interface IDataService
    {
        void Initialize();
        void Shutdown();
        string ServiceName { get; }
    }

    /// <summary>
    /// Basic save/load service interface
    /// </summary>
    public interface ISaveLoadService
    {
        void SaveData(string key, object data);
        object LoadData(string key);
        bool HasData(string key);
        void DeleteData(string key);
    }

    /// <summary>
    /// Basic settings service interface
    /// </summary>
    public interface ISettingsService
    {
        T GetSetting<T>(string key, T defaultValue = default);
        void SetSetting<T>(string key, T value);
        void SaveSettings();
        void LoadSettings();
    }

    /// <summary>
    /// Basic validation service interface
    /// </summary>
    public interface IValidationService
    {
        bool Validate(object data);
        string GetValidationMessage(object data);
    }

    /// <summary>
    /// Basic caching service interface
    /// </summary>
    public interface ICachingService
    {
        void CacheData(string key, object data, float lifetime = -1f);
        object GetCachedData(string key);
        bool HasCachedData(string key);
        void ClearCache(string key = null);
    }

}
