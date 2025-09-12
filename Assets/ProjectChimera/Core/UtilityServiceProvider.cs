using System;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using UnityEngine;

namespace ProjectChimera.Core
{
    /// <summary>
    /// SIMPLE: Basic utility service provider aligned with Project Chimera's service architecture vision.
    /// Focuses on essential utility services without complex dependency injection.
    /// </summary>
    public class UtilityServiceProvider : MonoBehaviour
    {
        [Header("Basic Settings")]
        [SerializeField] private bool _enableLogging = true;

        // Basic utility services
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();
        private bool _isInitialized = false;

        // Logging methods
        private void LogProviderAction(string action)
        {
            ChimeraLogger.Log("CORE", $"UtilityServiceProvider: {action}", this);
        }

        private void LogProviderError(string error)
        {
            ChimeraLogger.LogError("CORE", $"UtilityServiceProvider Error: {error}", this);
        }

        private void LogProviderWarning(string warning)
        {
            ChimeraLogger.LogWarning("CORE", $"UtilityServiceProvider Warning: {warning}", this);
        }

        // Configuration method
        public void ConfigureProvider()
        {
            LogProviderAction("Configuring UtilityServiceProvider");
            // Configuration logic can be added here if needed
        }

        /// <summary>
        /// Initialize the utility service provider
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            RegisterBasicServices();

            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("[UtilityServiceProvider] Initialized successfully");
            }
        }

        /// <summary>
        /// Shutdown the utility service provider
        /// </summary>
        public void Shutdown()
        {
            _services.Clear();
            _isInitialized = false;

            if (_enableLogging)
            {
                ChimeraLogger.Log("[UtilityServiceProvider] Shutdown completed");
            }
        }

        /// <summary>
        /// Register a service
        /// </summary>
        public void RegisterService<T>(T service) where T : class
        {
            _services[typeof(T)] = service;

            if (_enableLogging)
            {
                ChimeraLogger.Log($"[UtilityServiceProvider] Registered service: {typeof(T).Name}");
            }
        }

        /// <summary>
        /// Get a service
        /// </summary>
        public T GetService<T>() where T : class
        {
            if (_services.TryGetValue(typeof(T), out var service))
            {
                return service as T;
            }

            if (_enableLogging)
            {
                ChimeraLogger.LogWarning($"[UtilityServiceProvider] Service not found: {typeof(T).Name}");
            }

            return null;
        }

        /// <summary>
        /// Check if service is registered
        /// </summary>
        public bool HasService<T>() where T : class
        {
            return _services.ContainsKey(typeof(T));
        }

        /// <summary>
        /// Unregister a service
        /// </summary>
        public void UnregisterService<T>() where T : class
        {
            if (_services.Remove(typeof(T)))
            {
                if (_enableLogging)
                {
                    ChimeraLogger.Log($"[UtilityServiceProvider] Unregistered service: {typeof(T).Name}");
                }
            }
        }

        /// <summary>
        /// Get all registered services
        /// </summary>
        public Dictionary<Type, object> GetAllServices()
        {
            return new Dictionary<Type, object>(_services);
        }

        /// <summary>
        /// Clear all services
        /// </summary>
        public void ClearAllServices()
        {
            int count = _services.Count;
            _services.Clear();

            if (_enableLogging)
            {
                ChimeraLogger.Log($"[UtilityServiceProvider] Cleared {count} services");
            }
        }

        #region Private Methods

        private void RegisterBasicServices()
        {
            // Register basic configuration service
            var configService = new BasicConfigurationService();
            RegisterService<IConfigurationService>(configService);

            // Register basic math service
            var mathService = new BasicMathService();
            RegisterService<IMathService>(mathService);
        }

        /// <summary>
        /// Get utility service statistics for monitoring
        /// </summary>
        public UtilityServiceStats GetUtilityStats()
        {
            return new UtilityServiceStats
            {
                RegisteredServicesCount = _services.Count,
                IsInitialized = _isInitialized,
                Status = _isInitialized ? "Healthy" : "Not Initialized"
            };
        }

        #endregion

        /// <summary>
        /// Statistics class for utility service provider
        /// </summary>
        [System.Serializable]
        public class UtilityServiceStats
        {
            public int RegisteredServicesCount { get; set; }
            public bool IsInitialized { get; set; }
            public string Status { get; set; }
            public DateTime LastUpdate { get; set; }

            public UtilityServiceStats()
            {
                LastUpdate = DateTime.Now;
                Status = "Unknown";
            }
        }
    }

    /// <summary>
    /// Basic configuration service interface
    /// </summary>
    public interface IConfigurationService
    {
        string GetSetting(string key);
        void SetSetting(string key, string value);
    }

    /// <summary>
    /// Basic math service interface
    /// </summary>
    public interface IMathService
    {
        float CalculateDistance(Vector3 a, Vector3 b);
        float CalculateAngle(Vector3 from, Vector3 to);
    }

    /// <summary>
    /// Basic configuration service implementation
    /// </summary>
    public class BasicConfigurationService : IConfigurationService
    {
        private readonly Dictionary<string, string> _settings = new Dictionary<string, string>();

        public string GetSetting(string key)
        {
            return _settings.GetValueOrDefault(key, string.Empty);
        }

        public void SetSetting(string key, string value)
        {
            _settings[key] = value;
        }
    }

    /// <summary>
    /// Basic math service implementation
    /// </summary>
    public class BasicMathService : IMathService
    {
        public float CalculateDistance(Vector3 a, Vector3 b)
        {
            return Vector3.Distance(a, b);
        }

        public float CalculateAngle(Vector3 from, Vector3 to)
        {
            return Vector3.Angle(from, to);
        }
    }
}
