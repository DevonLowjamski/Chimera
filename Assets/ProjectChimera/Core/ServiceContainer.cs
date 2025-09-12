using System;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.DependencyInjection;

namespace ProjectChimera.Core
{
    /// <summary>
    /// SIMPLE: Basic service container aligned with Project Chimera's service architecture vision.
    /// Focuses on essential service registration and resolution without complex dependency injection.
    /// </summary>
    public class ServiceContainer : IServiceContainer, IServiceLocator
    {
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();
        private readonly object _lock = new object();
        private bool _disposed = false;

        // Events
        public event Action<Type, object> ServiceRegistered;
        public event Action<Type, object> ServiceResolved;
        public event Action<Type, Exception> ResolutionFailed;


        /// <summary>
        /// IServiceContainer interface implementation
        /// </summary>

        // Register singleton with implementation type
        public void RegisterSingleton<TInterface, TImplementation>()
            where TImplementation : class, TInterface, new()
        {
            var instance = new TImplementation();
            RegisterInstance<TInterface>(instance);
        }

        // Register singleton with existing instance
        public void RegisterSingleton<TInterface>(TInterface instance)
        {
            RegisterInstance<TInterface>(instance);
        }

        // Register instance (alias for RegisterSingleton)
        public void RegisterInstance<TInterface>(TInterface instance) where TInterface : class
        {
            if (_disposed) return;

            lock (_lock)
            {
                var serviceType = typeof(TInterface);
                _services[serviceType] = instance;

                ServiceRegistered?.Invoke(serviceType, instance);
            }
        }

        // Register transient with implementation type
        public void RegisterTransient<TInterface, TImplementation>()
            where TImplementation : class, TInterface, new()
        {
            // For simplicity, treat transient as singleton in this basic implementation
            RegisterSingleton<TInterface, TImplementation>();
        }

        public void RegisterSingleton<TInterface>(Func<IServiceContainer, TInterface> factory)
        {
            var instance = factory(this);
            RegisterInstance<TInterface>(instance);
        }

        public void RegisterTransient<TInterface>(Func<IServiceContainer, TInterface> factory)
        {
            // For simplicity, treat transient as singleton in this basic implementation
            var instance = factory(this);
            RegisterInstance<TInterface>(instance);
        }

        public void RegisterScoped<TInterface, TImplementation>() where TImplementation : class, TInterface, new()
        {
            // For simplicity, treat scoped as singleton in this basic implementation
            RegisterInstance<TInterface>(new TImplementation());
        }

        public void RegisterScoped<TInterface>(Func<IServiceContainer, TInterface> factory)
        {
            var instance = factory(this);
            RegisterInstance<TInterface>(instance);
        }

        // Resolve service by type
        public T Resolve<T>()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ServiceContainer));

            lock (_lock)
            {
                var serviceType = typeof(T);
                if (_services.TryGetValue(serviceType, out var service))
                {
                    ServiceResolved?.Invoke(serviceType, service);
                    return (T)service;
                }
            }

            throw new InvalidOperationException($"Service of type {typeof(T).Name} is not registered.");
        }

        // Try to resolve service, returns null if not found
        public T TryResolve<T>() where T : class
        {
            if (_disposed) return null;

            lock (_lock)
            {
                var serviceType = typeof(T);
                if (_services.TryGetValue(serviceType, out var service))
                {
                    ServiceResolved?.Invoke(serviceType, service);
                    return (T)service;
                }
            }

            return null;
        }

        // Try to resolve service by type, returns null if not found
        public object TryResolve(Type serviceType)
        {
            if (_disposed) return null;

            lock (_lock)
            {
                if (_services.TryGetValue(serviceType, out var service))
                {
                    ServiceResolved?.Invoke(serviceType, service);
                    return service;
                }
            }

            return null;
        }

        public object Resolve(Type serviceType)
        {
            if (_disposed) return null;

            lock (_lock)
            {
                if (_services.TryGetValue(serviceType, out var service))
                {
                    ServiceResolved?.Invoke(serviceType, service);
                    return service;
                }
            }
            return null;
        }

        public bool TryResolve<T>(out T service) where T : class
        {
            service = default;
            if (_disposed) return false;

            lock (_lock)
            {
                if (_services.TryGetValue(typeof(T), out var obj) && obj is T typedService)
                {
                    service = typedService;
                    ServiceResolved?.Invoke(typeof(T), service);
                    return true;
                }
            }
            return false;
        }

        public bool TryResolve(Type serviceType, out object service)
        {
            service = null;
            if (_disposed) return false;

            lock (_lock)
            {
                if (_services.TryGetValue(serviceType, out service))
                {
                    ServiceResolved?.Invoke(serviceType, service);
                    return true;
                }
            }
            return false;
        }

        public bool IsRegistered<T>()
        {
            if (_disposed) return false;

            lock (_lock)
            {
                return _services.ContainsKey(typeof(T));
            }
        }

        public bool IsRegistered(Type serviceType)
        {
            if (_disposed) return false;

            lock (_lock)
            {
                return _services.ContainsKey(serviceType);
            }
        }

        public IEnumerable<Type> GetRegisteredTypes<T>()
        {
            if (_disposed) yield break;

            lock (_lock)
            {
                foreach (var kvp in _services)
                {
                    if (typeof(T).IsAssignableFrom(kvp.Key))
                    {
                        yield return kvp.Key;
                    }
                }
            }
        }

        public IEnumerable<Type> GetRegisteredTypes(Type serviceType)
        {
            if (_disposed) yield break;

            lock (_lock)
            {
                foreach (var kvp in _services)
                {
                    if (serviceType.IsAssignableFrom(kvp.Key))
                    {
                        yield return kvp.Key;
                    }
                }
            }
        }

        public IEnumerable<T> ResolveAll<T>()
        {
            if (_disposed) yield break;

            lock (_lock)
            {
                foreach (var kvp in _services)
                {
                    if (kvp.Value is T service)
                    {
                        ServiceResolved?.Invoke(typeof(T), service);
                        yield return service;
                    }
                }
            }
        }

        public IServiceContainer CreateScope()
        {
            // For simplicity, return this same container
            return this;
        }

        public void ValidateServices()
        {
            // Basic validation - could be enhanced
            lock (_lock)
            {
                foreach (var kvp in _services)
                {
                    if (kvp.Value == null)
                    {
                        ChimeraLogger.LogWarning("CORE", $"Service {kvp.Key.Name} is null", this);
                    }
                }
            }
        }

        public IEnumerable<KeyValuePair<Type, object>> GetRegistrations()
        {
            if (_disposed) return new List<KeyValuePair<Type, object>>();

            lock (_lock)
            {
                return new List<KeyValuePair<Type, object>>(_services);
            }
        }

        // Additional IServiceContainer methods (simplified implementations)
        public void RegisterFactory<TInterface>(Func<IServiceLocator, TInterface> factory) where TInterface : class
        {
            // Simplified implementation
            var instance = factory(this);
            RegisterInstance<TInterface>(instance);
        }

        public void RegisterConditional<TInterface, TImplementation>(Func<IServiceLocator, bool> condition) where TInterface : class where TImplementation : class, TInterface, new()
        {
            if (condition(this))
            {
                RegisterInstance<TInterface>(new TImplementation());
            }
        }

        public void RegisterDecorator<TInterface, TDecorator>() where TInterface : class where TDecorator : class, TInterface, new()
        {
            // Simplified decorator implementation
            RegisterInstance<TInterface>(new TDecorator());
        }

        public void RegisterWithCallback<TInterface, TImplementation>(Action<TImplementation> callback) where TInterface : class where TImplementation : class, TInterface, new()
        {
            var instance = new TImplementation();
            callback(instance);
            RegisterInstance<TInterface>(instance);
        }

        public void RegisterOpenGeneric(Type serviceType, Type implementationType)
        {
            // Simplified - not fully implemented for this basic container
            ChimeraLogger.LogWarning("CORE", "RegisterOpenGeneric not fully implemented", this);
        }

        public T ResolveNamed<T>(string name) where T : class
        {
            // Simplified - name-based resolution not implemented
            return Resolve<T>();
        }

        public IEnumerable<T> ResolveWhere<T>(Func<T, bool> predicate) where T : class
        {
            foreach (var service in ResolveAll<T>())
            {
                if (predicate(service))
                {
                    yield return service;
                }
            }
        }

        public T ResolveOrCreate<T>(Func<T> factory) where T : class
        {
            if (TryResolve<T>(out var service))
            {
                return service;
            }
            service = factory();
            RegisterInstance<T>(service);
            return service;
        }

        public T ResolveLast<T>() where T : class
        {
            // Simplified - return first match
            return Resolve<T>();
        }

        public T ResolveWithLifetime<T>(ServiceLifetime lifetime) where T : class
        {
            // Simplified - ignore lifetime for this basic implementation
            return Resolve<T>();
        }

        public void Verify()
        {
            ValidateServices();
        }

        public IEnumerable<ServiceDescriptor> GetServiceDescriptors()
        {
            // Simplified - return basic descriptors
            if (_disposed) return new List<ServiceDescriptor>();

            lock (_lock)
            {
                var descriptors = new List<ServiceDescriptor>();
                foreach (var kvp in _services)
                {
                    descriptors.Add(new ServiceDescriptor
                    {
                        ServiceType = kvp.Key,
                        ImplementationType = kvp.Value?.GetType(),
                        Lifetime = ServiceLifetime.Singleton
                    });
                }
                return descriptors;
            }
        }

        public void Replace<TInterface, TImplementation>() where TInterface : class where TImplementation : class, TInterface, new()
        {
            RegisterInstance<TInterface>(new TImplementation());
        }

        public bool Unregister<T>() where T : class
        {
            if (_disposed) return false;

            lock (_lock)
            {
                return _services.Remove(typeof(T));
            }
        }

        // IServiceLocator interface implementation
        public T GetService<T>() where T : class
        {
            return Resolve<T>();
        }

        public object GetService(Type serviceType)
        {
            return Resolve(serviceType);
        }

        public bool TryGetService<T>(out T service) where T : class
        {
            return TryResolve<T>(out service);
        }

        public bool TryGetService(Type serviceType, out object service)
        {
            return TryResolve(serviceType, out service);
        }

        public IEnumerable<T> GetServices<T>() where T : class
        {
            return ResolveAll<T>();
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            if (_disposed) return new List<object>();

            lock (_lock)
            {
                var services = new List<object>();
                foreach (var kvp in _services)
                {
                    if (serviceType.IsAssignableFrom(kvp.Key))
                    {
                        services.Add(kvp.Value);
                    }
                }
                return services;
            }
        }

        // Additional properties and events
        public bool IsDisposed => _disposed;

        // Validate services
        public void ValidateServices()
        {
            // Basic validation - could be enhanced
            if (_disposed)
                throw new ObjectDisposedException(nameof(ServiceContainer));
        }

        // Get registration info
        public IEnumerable<ServiceRegistrationInfo> GetRegistrationInfo()
        {
            if (_disposed) yield break;

            lock (_lock)
            {
                foreach (var kvp in _services)
                {
                    yield return new ServiceRegistrationInfo
                    {
                        ServiceType = kvp.Key,
                        ImplementationType = kvp.Value?.GetType(),
                        Lifetime = ServiceLifetime.Singleton,
                        HasInstance = kvp.Value != null,
                        RegistrationTime = DateTime.Now
                    };
                }
            }
        }

        // Clear all services
        public void Clear()
        {
            if (_disposed) return;

            lock (_lock)
            {
                _services.Clear();
            }
        }

        // Create child container
        public IServiceContainer CreateChildContainer()
        {
            // For simplicity, return a new instance
            return new ServiceContainer();
        }

        // Dispose implementation
        public void Dispose()
        {
            if (_disposed) return;

            lock (_lock)
            {
                _services.Clear();
                _disposed = true;
            }
        }


        public void RegisterSingleton(Type serviceType, object instance)
        {
            if (_disposed) return;

            lock (_lock)
            {
                _services[serviceType] = instance;
                ServiceResolved?.Invoke(serviceType, instance);
            }
        }

        public void RegisterCollection<TInterface>(params Type[] implementationTypes)
        {
            // Simplified implementation
            foreach (var implType in implementationTypes)
            {
                if (typeof(TInterface).IsAssignableFrom(implType))
                {
                    var instance = Activator.CreateInstance(implType);
                    RegisterInstance<TInterface>((TInterface)instance);
                }
            }
        }

        public void RegisterNamed<TInterface, TImplementation>(string name) where TInterface : class where TImplementation : class, TInterface, new()
        {
            // Simplified - name not used in basic implementation
            RegisterInstance<TInterface>(new TImplementation());
        }

        public bool ContainsService<T>() where T : class
        {
            return IsRegistered<T>();
        }

        public bool ContainsService(Type serviceType)
        {
            return IsRegistered(serviceType);
        }
    }

    /// <summary>
    /// Service descriptor for IServiceContainer
    /// </summary>
    public class ServiceDescriptor
    {
        public Type ServiceType { get; set; }
        public Type ImplementationType { get; set; }
        public ServiceLifetime Lifetime { get; set; }
    }

}
