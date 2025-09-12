using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core
{
    /// <summary>
    /// BASIC: Simple service module core for Project Chimera.
    /// Focuses on essential service management without complex providers and validation systems.
    /// </summary>
    public abstract class ServiceModuleCore : MonoBehaviour
    {
        [Header("Basic Service Settings")]
        [SerializeField] private bool _enableBasicServices = true;
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private string _moduleName = "Basic Service Module";

        // Basic service tracking
        private readonly Dictionary<string, IService> _registeredServices = new Dictionary<string, IService>();
        private bool _isInitialized = false;

        /// <summary>
        /// Events for service module
        /// </summary>
        public event System.Action<string> OnServiceRegistered;
        public event System.Action OnModuleInitialized;

        /// <summary>
        /// Initialize basic service module
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            _isInitialized = true;
            OnModuleInitialized?.Invoke();

            if (_enableLogging)
            {
                ChimeraLogger.Log($"[ServiceModuleCore] {_moduleName} initialized successfully");
            }
        }

        /// <summary>
        /// Register a service
        /// </summary>
        public void RegisterService(string serviceName, IService service)
        {
            if (!_enableBasicServices || !_isInitialized || service == null) return;

            _registeredServices[serviceName] = service;
            OnServiceRegistered?.Invoke(serviceName);

            if (_enableLogging)
            {
                ChimeraLogger.Log($"[ServiceModuleCore] Registered service: {serviceName}");
            }
        }

        /// <summary>
        /// Get a registered service
        /// </summary>
        public T GetService<T>(string serviceName) where T : IService
        {
            if (_registeredServices.TryGetValue(serviceName, out IService service))
            {
                try
                {
                    return (T)service;
                }
                catch
                {
                    if (_enableLogging)
                    {
                        ChimeraLogger.LogWarning($"[ServiceModuleCore] Type mismatch for service: {serviceName}");
                    }
                    return default;
                }
            }

            if (_enableLogging)
            {
                ChimeraLogger.LogWarning($"[ServiceModuleCore] Service not found: {serviceName}");
            }
            return default;
        }

        /// <summary>
        /// Check if service is registered
        /// </summary>
        public bool IsServiceRegistered(string serviceName)
        {
            return _registeredServices.ContainsKey(serviceName);
        }

        /// <summary>
        /// Get all registered service names
        /// </summary>
        public List<string> GetRegisteredServiceNames()
        {
            return new List<string>(_registeredServices.Keys);
        }

        /// <summary>
        /// Remove a service
        /// </summary>
        public void RemoveService(string serviceName)
        {
            if (_registeredServices.Remove(serviceName))
            {
                if (_enableLogging)
                {
                    ChimeraLogger.Log($"[ServiceModuleCore] Removed service: {serviceName}");
                }
            }
        }

        /// <summary>
        /// Clear all services
        /// </summary>
        public void ClearAllServices()
        {
            var serviceNames = new List<string>(_registeredServices.Keys);
            _registeredServices.Clear();

            if (_enableLogging)
            {
                ChimeraLogger.Log($"[ServiceModuleCore] Cleared {serviceNames.Count} services");
            }
        }

        /// <summary>
        /// Set services enabled state
        /// </summary>
        public void SetServicesEnabled(bool enabled)
        {
            _enableBasicServices = enabled;

            if (!enabled)
            {
                ClearAllServices();
            }

            if (_enableLogging)
            {
                ChimeraLogger.Log($"[ServiceModuleCore] Services {(enabled ? "enabled" : "disabled")}");
            }
        }

        /// <summary>
        /// Get module information
        /// </summary>
        public ModuleInfo GetModuleInfo()
        {
            return new ModuleInfo
            {
                ModuleName = _moduleName,
                RegisteredServices = _registeredServices.Count,
                IsInitialized = _isInitialized,
                IsServicesEnabled = _enableBasicServices
            };
        }

        /// <summary>
        /// Get module statistics
        /// </summary>
        public ModuleStats GetStats()
        {
            return new ModuleStats
            {
                TotalServices = _registeredServices.Count,
                ActiveServices = _registeredServices.Count, // All registered services are considered active
                FailedServices = 0, // Basic implementation doesn't track failures
                ModuleUptime = _isInitialized ? Time.time : 0f
            };
        }

        /// <summary>
        /// Get the module name (virtual for subclasses to override)
        /// </summary>
        public virtual string ModuleName => _moduleName;

        /// <summary>
        /// Initialize service components (virtual for subclasses to override)
        /// </summary>
        protected virtual void InitializeServiceComponents()
        {
            // Basic implementation does nothing
        }

        /// <summary>
        /// Configure service providers (virtual for subclasses to override)
        /// </summary>
        protected virtual void ConfigureServiceProviders(IServiceContainer container)
        {
            // Basic implementation does nothing
        }

        /// <summary>
        /// Initialize service providers (virtual for subclasses to override)
        /// </summary>
        protected virtual void InitializeServiceProviders(IServiceContainer container)
        {
            // Basic implementation does nothing
        }

        /// <summary>
        /// Validate service providers (virtual for subclasses to override)
        /// </summary>
        protected virtual void ValidateServiceProviders(IServiceContainer container)
        {
            // Basic implementation does nothing
        }
    }

    // IService interface moved to ProjectChimera.Core.IService.cs for consistency

    /// <summary>
    /// Module information
    /// </summary>
    [System.Serializable]
    public struct ModuleInfo
    {
        public string ModuleName;
        public int RegisteredServices;
        public bool IsInitialized;
        public bool IsServicesEnabled;
    }

    /// <summary>
    /// Module statistics
    /// </summary>
    [System.Serializable]
    public struct ModuleStats
    {
        public int TotalServices;
        public int ActiveServices;
        public int FailedServices;
        public float ModuleUptime;
    }
}
