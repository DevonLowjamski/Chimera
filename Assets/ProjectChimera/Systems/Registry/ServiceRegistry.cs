using ProjectChimera.Core.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProjectChimera.Core;

namespace ProjectChimera.Systems.Registry
{
    /// <summary>
    /// Central service registry for managing all specialized services in Project Chimera
    /// Provides discovery, registration, and lifecycle management for 150+ services
    /// Part of Module 2: Manager Decomposition architecture
    /// </summary>
    public class ServiceRegistry : MonoBehaviour
    {
        #region Singleton Implementation
        
        private static ServiceRegistry _instance;
        public static ServiceRegistry Instance
        {
            get
            {
                if (_instance == null)
                {
                    // Try ServiceContainer first, then fall back to scene search, finally create if needed
                    _instance = ServiceContainerFactory.Instance?.TryResolve<ServiceRegistry>();
                    if (_instance == null)
                    {
                        GameObject go = new GameObject("ServiceRegistry");
                        _instance = go.AddComponent<ServiceRegistry>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        #endregion

        #region Service Registry Data
        
        private readonly Dictionary<Type, IService> _services = new Dictionary<Type, IService>();
        private readonly Dictionary<string, IService> _servicesByName = new Dictionary<string, IService>();
        private readonly List<ServiceRegistration> _registrations = new List<ServiceRegistration>();
        private readonly Dictionary<ServiceDomain, List<IService>> _servicesByDomain = new Dictionary<ServiceDomain, List<IService>>();
        
        #endregion

        #region Properties
        
        public int RegisteredServiceCount => _services.Count;
        public int InitializedServiceCount => _services.Values.Count(s => s.IsInitialized);
        public bool AllServicesInitialized => _services.Values.All(s => s.IsInitialized);
        
        #endregion

        #region Events
        
        public event Action<IService> OnServiceRegistered;
        public event Action<IService> OnServiceInitialized;
        public event Action<IService> OnServiceShutdown;
        public event Action OnAllServicesInitialized;
        
        #endregion

        #region Unity Lifecycle
        
        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeServiceDomains();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            RegisterCoreServices();
        }

        private void OnDestroy()
        {
            ShutdownAllServices();
        }

        #endregion

        #region Service Registration

        /// <summary>
        /// Register a service with the registry
        /// </summary>
        public void RegisterService<T>(T service, ServiceDomain domain = ServiceDomain.Core) where T : class, IService
        {
            if (service == null)
            {
                ChimeraLogger.LogError($"Cannot register null service of type {typeof(T).Name}");
                return;
            }

            Type serviceType = typeof(T);
            
            if (_services.ContainsKey(serviceType))
            {
                ChimeraLogger.LogWarning($"Service {serviceType.Name} is already registered. Replacing existing service.");
            }

            _services[serviceType] = service;
            _servicesByName[serviceType.Name] = service;
            
            var registration = new ServiceRegistration
            {
                ServiceType = serviceType,
                Service = service,
                Domain = domain,
                RegistrationTime = DateTime.Now
            };
            _registrations.Add(registration);

            if (!_servicesByDomain.ContainsKey(domain))
            {
                _servicesByDomain[domain] = new List<IService>();
            }
            _servicesByDomain[domain].Add(service);

            OnServiceRegistered?.Invoke(service);
            
            ChimeraLogger.Log($"Registered service: {serviceType.Name} in domain: {domain}");
        }

        /// <summary>
        /// Get a service by type
        /// </summary>
        public T GetService<T>() where T : class, IService
        {
            Type serviceType = typeof(T);
            if (_services.TryGetValue(serviceType, out IService service))
            {
                return service as T;
            }
            
            ChimeraLogger.LogWarning($"Service {serviceType.Name} not found in registry");
            return null;
        }

        /// <summary>
        /// Get a service by name
        /// </summary>
        public IService GetService(string serviceName)
        {
            if (_servicesByName.TryGetValue(serviceName, out IService service))
            {
                return service;
            }
            
            ChimeraLogger.LogWarning($"Service {serviceName} not found in registry");
            return null;
        }

        /// <summary>
        /// Get all services in a domain
        /// </summary>
        public List<IService> GetServicesByDomain(ServiceDomain domain)
        {
            if (_servicesByDomain.TryGetValue(domain, out List<IService> services))
            {
                return new List<IService>(services);
            }
            
            return new List<IService>();
        }

        /// <summary>
        /// Check if a service is registered
        /// </summary>
        public bool IsServiceRegistered<T>() where T : class, IService
        {
            return _services.ContainsKey(typeof(T));
        }

        /// <summary>
        /// Check if a service is registered by name
        /// </summary>
        public bool IsServiceRegistered(string serviceName)
        {
            return _servicesByName.ContainsKey(serviceName);
        }

        #endregion

        #region Service Management

        /// <summary>
        /// Initialize all registered services
        /// </summary>
        public void InitializeAllServices()
        {
            ChimeraLogger.Log($"Initializing {_services.Count} registered services...");
            
            foreach (var service in _services.Values)
            {
                if (!service.IsInitialized)
                {
                    try
                    {
                        service.Initialize();
                        OnServiceInitialized?.Invoke(service);
                        ChimeraLogger.Log($"Initialized service: {service.GetType().Name}");
                    }
                    catch (Exception ex)
                    {
                        ChimeraLogger.LogError($"Failed to initialize service {service.GetType().Name}: {ex.Message}");
                    }
                }
            }
            
            if (AllServicesInitialized)
            {
                OnAllServicesInitialized?.Invoke();
                ChimeraLogger.Log("All services initialized successfully");
            }
        }

        /// <summary>
        /// Shutdown all registered services
        /// </summary>
        public void ShutdownAllServices()
        {
            ChimeraLogger.Log($"Shutting down {_services.Count} registered services...");
            
            foreach (var service in _services.Values.Reverse())
            {
                try
                {
                    service.Shutdown();
                    OnServiceShutdown?.Invoke(service);
                    ChimeraLogger.Log($"Shutdown service: {service.GetType().Name}");
                }
                catch (Exception ex)
                {
                    ChimeraLogger.LogError($"Failed to shutdown service {service.GetType().Name}: {ex.Message}");
                }
            }
            
            _services.Clear();
            _servicesByName.Clear();
            _registrations.Clear();
            _servicesByDomain.Clear();
        }

        /// <summary>
        /// Get service registration information
        /// </summary>
        public List<ServiceRegistration> GetServiceRegistrations()
        {
            return new List<ServiceRegistration>(_registrations);
        }

        /// <summary>
        /// Get services by initialization status
        /// </summary>
        public List<IService> GetServicesByStatus(bool initialized)
        {
            return _services.Values.Where(s => s.IsInitialized == initialized).ToList();
        }

        #endregion

        #region Private Methods

        private void InitializeServiceDomains()
        {
            foreach (ServiceDomain domain in Enum.GetValues(typeof(ServiceDomain)))
            {
                _servicesByDomain[domain] = new List<IService>();
            }
        }

        private void RegisterCoreServices()
        {
            // Core services will be auto-registered by GameManager and other systems
            ChimeraLogger.Log("ServiceRegistry ready for service registration");
        }

        #endregion

        #region Debugging and Analytics

        /// <summary>
        /// Generate comprehensive service registry report
        /// </summary>
        public ServiceRegistryReport GenerateReport()
        {
            var report = new ServiceRegistryReport
            {
                TotalServicesRegistered = _services.Count,
                ServicesInitialized = InitializedServiceCount,
                ServicesByDomain = new Dictionary<ServiceDomain, int>(),
                ServiceRegistrations = GetServiceRegistrations(),
                GeneratedAt = DateTime.Now
            };

            foreach (var domain in _servicesByDomain)
            {
                report.ServicesByDomain[domain.Key] = domain.Value.Count;
            }

            return report;
        }

        /// <summary>
        /// Log detailed service registry status
        /// </summary>
        [ContextMenu("Log Service Registry Status")]
        public void LogRegistryStatus()
        {
            ChimeraLogger.Log($"=== SERVICE REGISTRY STATUS ===");
            ChimeraLogger.Log($"Total Services: {RegisteredServiceCount}");
            ChimeraLogger.Log($"Initialized: {InitializedServiceCount}");
            ChimeraLogger.Log($"Pending: {RegisteredServiceCount - InitializedServiceCount}");
            
            foreach (var domain in _servicesByDomain)
            {
                ChimeraLogger.Log($"Domain {domain.Key}: {domain.Value.Count} services");
            }
        }

        #endregion
    }

    #region Supporting Data Structures



    /// <summary>
    /// Service domain categories for organization
    /// </summary>
    public enum ServiceDomain
    {
        Core,
        Cultivation,
        Genetics,
        Environment,
        Economy,
        AI,
        Analytics,
        Progression,
        UI,
        SpeedTree,
        Performance,
        Events,
        Testing,
        Competition,
        Research
    }

    /// <summary>
    /// Service registration metadata
    /// </summary>
    [System.Serializable]
    public class ServiceRegistration
    {
        public Type ServiceType;
        public IService Service;
        public ServiceDomain Domain;
        public DateTime RegistrationTime;
        public string Description;
    }

    /// <summary>
    /// Comprehensive service registry report
    /// </summary>
    [System.Serializable]
    public class ServiceRegistryReport
    {
        public int TotalServicesRegistered;
        public int ServicesInitialized;
        public Dictionary<ServiceDomain, int> ServicesByDomain;
        public List<ServiceRegistration> ServiceRegistrations;
        public DateTime GeneratedAt;
    }

    #endregion
}