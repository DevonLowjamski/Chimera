using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProjectChimera.Core;
using IServiceProvider = ProjectChimera.Core.DependencyInjection.IServiceProvider;

namespace ProjectChimera.Core.DependencyInjection
{
    public class ServiceLocator : IServiceLocator
    {
        private static ServiceLocator _instance;
        private static readonly object _lock = new object();

        private readonly Dictionary<Type, ServiceRegistration> _services = new Dictionary<Type, ServiceRegistration>();
        private readonly Dictionary<Type, object> _singletonInstances = new Dictionary<Type, object>();
        private readonly List<ProjectChimera.Core.IServiceScope> _activeScopes = new List<ProjectChimera.Core.IServiceScope>();

        // Enhanced caching and discovery modules
        private readonly ServiceCache _serviceCache = new ServiceCache();
        private readonly ServiceDiscovery _serviceDiscovery = new ServiceDiscovery();
        
        private int _totalResolutions = 0;
        private readonly Dictionary<Type, int> _resolutionCounts = new Dictionary<Type, int>();
        
        private bool _autoDiscoveryEnabled = true;

        public static ServiceLocator Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new ServiceLocator();
                            _instance.RegisterCoreServices();
                        }
                    }
                }
                return _instance;
            }
        }

        private ServiceLocator() { }

        public void RegisterSingleton<TInterface, TImplementation>()
            where TImplementation : class, TInterface, new()
        {
            lock (_lock)
            {
                var registration = new ServiceRegistration(typeof(TInterface), typeof(TImplementation), ServiceLifetime.Singleton, null, (object locator) => new TImplementation());
                _services[typeof(TInterface)] = registration;
                Debug.Log($"[ServiceLocator] Registered Singleton: {typeof(TInterface).Name} -> {typeof(TImplementation).Name}");
            }
        }

        public void RegisterSingleton<TInterface>(TInterface instance) where TInterface : class
        {
            lock (_lock)
            {
                var registration = new ServiceRegistration(typeof(TInterface), instance.GetType(), ServiceLifetime.Singleton, instance, null);
                _services[typeof(TInterface)] = registration;
                _singletonInstances[typeof(TInterface)] = instance;
                Debug.Log($"[ServiceLocator] Registered Singleton Instance: {typeof(TInterface).Name}");
            }
        }

        public void RegisterSingleton(Type serviceType, object instance)
        {
            if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            if (!serviceType.IsAssignableFrom(instance.GetType()))
                throw new ArgumentException($"Instance of type {instance.GetType().Name} is not assignable to service type {serviceType.Name}");

            lock (_lock)
            {
                var registration = new ServiceRegistration(serviceType, instance.GetType(), ServiceLifetime.Singleton, instance, null);
                _services[serviceType] = registration;
                _singletonInstances[serviceType] = instance;
                Debug.Log($"[ServiceLocator] Registered Singleton Instance: {serviceType.Name}");
            }
        }

        public void RegisterTransient<TInterface, TImplementation>()
            where TImplementation : class, TInterface, new()
        {
            lock (_lock)
            {
                var registration = new ServiceRegistration(typeof(TInterface), typeof(TImplementation), ServiceLifetime.Transient, null, (object locator) => new TImplementation());
                _services[typeof(TInterface)] = registration;
                Debug.Log($"[ServiceLocator] Registered Transient: {typeof(TInterface).Name} -> {typeof(TImplementation).Name}");
            }
        }

        public void RegisterScoped<TInterface, TImplementation>()
            where TImplementation : class, TInterface, new()
        {
            lock (_lock)
            {
                var registration = new ServiceRegistration(typeof(TInterface), typeof(TImplementation), ServiceLifetime.Scoped, null, (object locator) => new TImplementation());
                _services[typeof(TInterface)] = registration;
                Debug.Log($"[ServiceLocator] Registered Scoped: {typeof(TInterface).Name} -> {typeof(TImplementation).Name}");
            }
        }

        public void RegisterFactory<TInterface>(Func<IServiceLocator, TInterface> factory)
            where TInterface : class
        {
            lock (_lock)
            {
                var registration = new ServiceRegistration(typeof(TInterface), typeof(TInterface), ServiceLifetime.Transient, null, (object locator) => factory((IServiceLocator)locator));
                _services[typeof(TInterface)] = registration;
                Debug.Log($"[ServiceLocator] Registered Factory: {typeof(TInterface).Name}");
            }
        }

        public void Unregister<TInterface>() where TInterface : class
        {
            Unregister(typeof(TInterface));
        }

        public void Unregister(Type serviceType)
        {
            if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));

            lock (_lock)
            {
                if (_services.ContainsKey(serviceType))
                {
                    _services.Remove(serviceType);
                    _singletonInstances.Remove(serviceType);
                    Debug.Log($"[ServiceLocator] Unregistered service: {serviceType.Name}");
                }
            }
        }

        public T GetService<T>() where T : class => Resolve<T>();
        public object GetService(Type serviceType) => Resolve(serviceType);

        public bool TryGetService<T>(out T service) where T : class
        {
            service = TryResolve<T>();
            return service != null;
        }

        public bool TryGetService(Type serviceType, out object service)
        {
            try
            {
                service = Resolve(serviceType);
                return true;
            }
            catch
            {
                service = null;
                return false;
            }
        }

        public bool ContainsService<T>() where T : class => IsRegistered<T>();
        public bool ContainsService(Type serviceType) => IsRegistered(serviceType);

        public T Resolve<T>() where T : class
        {
            return (T)Resolve(typeof(T));
        }

        public T Resolve<T>(T fallback) where T : class
        {
            try
            {
                return Resolve<T>() ?? fallback;
            }
            catch
            {
                return fallback;
            }
        }

        public T TryResolve<T>() where T : class
        {
            try
            {
                return Resolve<T>();
            }
            catch
            {
                return null;
            }
        }

        private object Resolve(Type serviceType)
        {
            lock (_lock)
            {
                _totalResolutions++;
                UpdateResolutionCount(serviceType);

                // Try cache first if enabled
                if (_serviceCache.TryGetFromCache(serviceType, _singletonInstances, out var cachedInstance))
                {
                    return cachedInstance;
                }

                // Try direct registration
                if (_services.TryGetValue(serviceType, out var registration))
                {
                    var instance = CreateServiceInstance(registration);
                    _serviceCache.CacheInstance(serviceType, instance, _singletonInstances);
                    return instance;
                }

                // Try auto-discovery if enabled
                if (_autoDiscoveryEnabled && _serviceDiscovery.TryDiscoverService(serviceType, out var discoveredInstance))
                {
                    // Auto-register the discovered service
                    RegisterSingleton(serviceType, discoveredInstance);
                    _serviceCache.CacheInstance(serviceType, discoveredInstance, _singletonInstances);
                    return discoveredInstance;
                }

                throw new InvalidOperationException($"Service of type {serviceType.Name} is not registered and could not be discovered");
            }
        }

        private object CreateServiceInstance(ServiceRegistration registration)
        {
            switch (registration.Lifetime)
            {
                case ServiceLifetime.Singleton:
                    return GetOrCreateSingleton(registration);

                case ServiceLifetime.Transient:
                    return registration.Factory(this);

                case ServiceLifetime.Scoped:
                    return GetOrCreateSingleton(registration);

                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private object GetOrCreateSingleton(ServiceRegistration registration)
        {
            if (registration.Instance != null)
            {
                return registration.Instance;
            }

            if (_singletonInstances.TryGetValue(registration.ServiceType, out var existingInstance))
            {
                return existingInstance;
            }

            var newInstance = registration.Factory(this);
            _singletonInstances[registration.ServiceType] = newInstance;
            registration.Instance = newInstance;

            return newInstance;
        }

        public bool IsRegistered<T>() where T : class
        {
            return IsRegistered(typeof(T));
        }

        public bool IsRegistered(Type serviceType)
        {
            lock (_lock)
            {
                return _services.ContainsKey(serviceType);
            }
        }

        public IEnumerable<T> ResolveAll<T>() where T : class
        {
            lock (_lock)
            {
                var serviceType = typeof(T);
                return _services.Values
                    .Where(r => serviceType.IsAssignableFrom(r.ImplementationType))
                    .Select(r => (T)CreateServiceInstance(r))
                    .ToList();
            }
        }

        public IDictionary<Type, ServiceRegistration> GetRegistrations()
        {
            lock (_lock)
            {
                return new Dictionary<Type, ServiceRegistration>(_services);
            }
        }

        public ProjectChimera.Core.IServiceScope CreateScope()
        {
            var scope = new ServiceScope(this);
            lock (_lock)
            {
                _activeScopes.Add(scope);
            }
            return scope;
        }

        internal void RemoveScope(ProjectChimera.Core.IServiceScope scope)
        {
            lock (_lock)
            {
                _activeScopes.Remove(scope);
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                foreach (var instance in _singletonInstances.Values.OfType<IDisposable>())
                {
                    try
                    {
                        instance.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[ServiceLocator] Error disposing service: {ex.Message}");
                    }
                }

                foreach (var scope in _activeScopes.ToList())
                {
                    try
                    {
                        scope.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[ServiceLocator] Error disposing scope: {ex.Message}");
                    }
                }

                _services.Clear();
                _singletonInstances.Clear();
                _activeScopes.Clear();
                _resolutionCounts.Clear();
                
                // Clear caching and discovery data
                _serviceCache.ClearCache();
                _serviceDiscovery.ClearCache();
                
                _totalResolutions = 0;

                Debug.Log("[ServiceLocator] All services and caches cleared");
            }
        }

        public ServiceLocatorMetrics GetMetrics()
        {
            lock (_lock)
            {
                return new ServiceLocatorMetrics
                {
                    TotalResolutions = _totalResolutions,
                    CacheHits = _serviceCache.CacheHits,
                    CacheHitRate = _totalResolutions > 0 ? (float)_serviceCache.CacheHits / _totalResolutions : 0f,
                    RegisteredServices = _services.Count,
                    SingletonInstances = _singletonInstances.Count,
                    ActiveScopes = _activeScopes.Count,
                    ResolutionCounts = new Dictionary<Type, int>(_resolutionCounts),
                    DiscoveryAttempts = _serviceDiscovery.DiscoveryAttempts,
                    ValidationErrors = _serviceDiscovery.ValidationErrors,
                    CachedTypes = _serviceCache.CachedTypesCount,
                    DiscoveredTypes = _serviceDiscovery.DiscoveredTypesCount
                };
            }
        }
        
        // Additional IServiceProvider compatibility methods
        public T GetRequiredService<T>() where T : class
        {
            var service = Resolve<T>();
            if (service == null)
            {
                throw new InvalidOperationException($"Required service of type {typeof(T).Name} could not be resolved");
            }
            return service;
        }
        
        public object GetRequiredService(Type serviceType)
        {
            var service = Resolve(serviceType);
            if (service == null)
            {
                throw new InvalidOperationException($"Required service of type {serviceType.Name} could not be resolved");
            }
            return service;
        }
        
        public IEnumerable<object> GetServices(Type serviceType)
        {
            try
            {
                var method = typeof(ServiceLocator).GetMethod(nameof(ResolveAll))?.MakeGenericMethod(serviceType);
                var result = method?.Invoke(this, Array.Empty<object>());
                return result as IEnumerable<object> ?? Array.Empty<object>();
            }
            catch
            {
                return Array.Empty<object>();
            }
        }
        
        public IEnumerable<T> GetServices<T>() where T : class => ResolveAll<T>();

        private void RegisterCoreServices()
        {
            RegisterSingleton<IServiceLocator>(this);
            Debug.Log("[ServiceLocator] Core services registered");
        }

        private void UpdateResolutionCount(Type serviceType)
        {
            if (_resolutionCounts.ContainsKey(serviceType))
            {
                _resolutionCounts[serviceType]++;
            }
            else
            {
                _resolutionCounts[serviceType] = 1;
            }
        }
        
        #region Service Discovery and Caching
        
        /// <summary>
        /// Enables or disables automatic service discovery
        /// </summary>
        public void SetAutoDiscovery(bool enabled)
        {
            lock (_lock)
            {
                _autoDiscoveryEnabled = enabled;
                Debug.Log($"[ServiceLocator] Auto-discovery {(enabled ? "enabled" : "disabled")}");
            }
        }
        
        /// <summary>
        /// Enables or disables instance caching
        /// </summary>
        public void SetCaching(bool enabled)
        {
            lock (_lock)
            {
                _serviceCache.CachingEnabled = enabled;
                Debug.Log($"[ServiceLocator] Caching {(enabled ? "enabled" : "disabled")}");
            }
        }
        
        /// <summary>
        /// Clears all cached data
        /// </summary>
        public void ClearCache()
        {
            lock (_lock)
            {
                _serviceCache.ClearCache();
                _serviceDiscovery.ClearCache();
                Debug.Log("[ServiceLocator] All caches cleared");
            }
        }
        
        /// <summary>
        /// Validates all registered services
        /// </summary>
        public ServiceValidationResult ValidateServices()
        {
            lock (_lock)
            {
                return _serviceDiscovery.ValidateServices(_services);
            }
        }
        
        #endregion
    }

}
