using System;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;
// DI types now available directly in ProjectChimera.Core namespace

namespace ProjectChimera.Core
{
    public class ServiceContainer : IServiceContainer
    {
        private readonly Dictionary<Type, object> _services = new();
        private readonly Dictionary<Type, ServiceRegistrationData> _registrations = new();
        private bool _isDisposed = false;

        // Events
        public event Action<ServiceRegistrationData> ServiceRegistered;
        public event Action<Type, object> ServiceResolved;
        public event Action<Type, Exception> ResolutionFailed;

        /// <summary>
        /// Register a service with its implementation type
        /// </summary>
        public void RegisterSingleton<TInterface, TImplementation>()
            where TImplementation : class, TInterface, new()
        {
            var serviceType = typeof(TInterface);
            var implementationType = typeof(TImplementation);

            if (_services.ContainsKey(serviceType))
                throw new InvalidOperationException($"Service {serviceType.Name} is already registered");

            var createdInstance = new TImplementation();
            _services[serviceType] = createdInstance;

            var registration = new ServiceRegistrationData(serviceType, implementationType, ServiceLifetime.Singleton, createdInstance, null);
            _registrations[serviceType] = registration;

            ServiceRegistered?.Invoke(registration);
        }

        /// <summary>
        /// Register a service instance directly
        /// </summary>
        public void RegisterSingleton<TInterface>(TInterface instance)
        {
            var serviceType = typeof(TInterface);

            if (_services.ContainsKey(serviceType))
                throw new InvalidOperationException($"Service {serviceType.Name} is already registered");

            _services[serviceType] = instance;

            var registration = new ServiceRegistrationData(serviceType, instance.GetType(), ServiceLifetime.Singleton, instance, null);
            _registrations[serviceType] = registration;

            ServiceRegistered?.Invoke(registration);
        }

        // Factory overload (singleton)
        public void RegisterSingleton<TInterface>(Func<IServiceContainer, TInterface> factory)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            var created = factory(this);
            RegisterSingleton(created);
        }

        /// <summary>
        /// Register an instance directly
        /// </summary>
        public void RegisterInstance<TInterface>(TInterface instance) where TInterface : class
        {
            RegisterSingleton(instance);
        }

        /// <summary>
        /// Register a transient service (new instance each time)
        /// </summary>
        public void RegisterTransient<TInterface, TImplementation>()
            where TImplementation : class, TInterface, new()
        {
            var serviceType = typeof(TInterface);
            var implementationType = typeof(TImplementation);

            var registration = new ServiceRegistrationData(
                serviceType,
                implementationType,
                ServiceLifetime.Transient,
                null,
                (IServiceLocator _) => Activator.CreateInstance(implementationType)
            );
            _registrations[serviceType] = registration;

            ServiceRegistered?.Invoke(registration);
        }

        public void RegisterTransient<TInterface>(Func<IServiceContainer, TInterface> factory)
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));
            var serviceType = typeof(TInterface);
            var registration = new ServiceRegistrationData(
                serviceType,
                typeof(TInterface),
                ServiceLifetime.Transient,
                null,
                (IServiceLocator loc) => factory(this)
            );
            _registrations[serviceType] = registration;
            ServiceRegistered?.Invoke(registration);
        }

        /// <summary>
        /// Register a scoped service
        /// </summary>
        public void RegisterScoped<TInterface, TImplementation>()
            where TImplementation : class, TInterface, new()
        {
            // For simplicity, treat scoped as singleton
            RegisterSingleton<TInterface, TImplementation>();
        }

        public void RegisterScoped<TInterface>(Func<IServiceContainer, TInterface> factory)
        {
            // Treat scoped as singleton in this simple container
            RegisterSingleton(factory);
        }

        /// <summary>
        /// Register with factory function
        /// </summary>
        public void RegisterFactory<TInterface>(Func<IServiceLocator, TInterface> factory)
            where TInterface : class
        {
            var serviceType = typeof(TInterface);

            var registration = new ServiceRegistrationData(
                serviceType,
                typeof(TInterface),
                ServiceLifetime.Singleton,
                null,
                (IServiceLocator loc) => factory(loc)
            );
            _registrations[serviceType] = registration;

            ServiceRegistered?.Invoke(registration);
        }

        /// <summary>
        /// Resolve a service
        /// </summary>
        public T Resolve<T>()
        {
            return (T)Resolve(typeof(T));
        }

        /// <summary>
        /// Try to resolve a service
        /// </summary>
        public T TryResolve<T>() where T : class
        {
            return TryResolve(typeof(T)) as T;
        }

        /// <summary>
        /// Try to resolve a service by type
        /// </summary>
        public object TryResolve(Type serviceType)
        {
            if (_services.TryGetValue(serviceType, out var instance))
                return instance;

            if (_registrations.TryGetValue(serviceType, out var registration))
            {
                if (registration.Instance != null)
                    return registration.Instance;

                if (registration.Factory != null)
                {
                    try
                    {
                        var locator = new InternalServiceLocator(this);
                        var created = registration.Factory(locator);
                        if (registration.Lifetime == ServiceLifetime.Singleton)
                        {
                            _services[serviceType] = created;
                        }
                        ServiceResolved?.Invoke(serviceType, created);
                        return created;
                    }
                    catch (Exception ex)
                    {
                        ResolutionFailed?.Invoke(serviceType, ex);
                        throw;
                    }
                }

                // Try to create instance directly
                try
                {
                    var created = Activator.CreateInstance(registration.ImplementationType);
                    if (registration.Lifetime == ServiceLifetime.Singleton)
                    {
                        _services[serviceType] = created;
                    }
                    ServiceResolved?.Invoke(serviceType, created);
                    return created;
                }
                catch (Exception ex)
                {
                    ResolutionFailed?.Invoke(serviceType, ex);
                    throw;
                }
            }

            return null;
        }

        /// <summary>
        /// Resolve a service by type
        /// </summary>
        public object Resolve(Type serviceType)
        {
            var instance = TryResolve(serviceType);
            if (instance == null)
                throw new InvalidOperationException($"Service {serviceType.Name} is not registered");

            return instance;
        }

        // Minimal implementations for advanced IServiceContainer API
        public IEnumerable<T> ResolveAll<T>()
        {
            var list = new List<T>();
            var single = TryResolve(typeof(T));
            if (single != null) list.Add((T)single);
            return list;
        }

        /// <summary>
        /// Check if a service is registered
        /// </summary>
        public bool IsRegistered<T>()
        {
            return IsRegistered(typeof(T));
        }

        /// <summary>
        /// Check if a service type is registered
        /// </summary>
        public bool IsRegistered(Type serviceType)
        {
            return _services.ContainsKey(serviceType) || _registrations.ContainsKey(serviceType);
        }

        /// <summary>
        /// Get all registered service types
        /// </summary>
        public IEnumerable<Type> GetRegisteredTypes()
        {
            return _services.Keys.Concat(_registrations.Keys).Distinct();
        }

        /// <summary>
        /// Get registered types for a specific interface
        /// </summary>
        public IEnumerable<Type> GetRegisteredTypes<T>()
        {
            var interfaceType = typeof(T);
            return _services.Keys.Concat(_registrations.Keys)
                .Where(type => interfaceType.IsAssignableFrom(type))
                .Distinct();
        }

        /// <summary>
        /// Get registered types for a specific interface type
        /// </summary>
        public IEnumerable<Type> GetRegisteredTypes(Type interfaceType)
        {
            return _services.Keys.Concat(_registrations.Keys)
                .Where(type => interfaceType.IsAssignableFrom(type))
                .Distinct();
        }

        /// <summary>
        /// Unregister a service
        /// </summary>
        public bool Unregister<T>() where T : class
        {
            var serviceType = typeof(T);
            var removed = _services.Remove(serviceType) || _registrations.Remove(serviceType);
            return removed;
        }

        /// <summary>
        /// Clear all services
        /// </summary>
        public void Clear()
        {
            _services.Clear();
            _registrations.Clear();
        }

        public IServiceContainer CreateChildContainer()
        {
            return new ServiceContainer();
        }

        public IDictionary<Type, ServiceRegistrationData> GetRegistrations() => _registrations;

        public bool IsDisposed => _isDisposed;

        public void Dispose()
        {
            if (_isDisposed) return;
            Clear();
            _isDisposed = true;
        }

        public T GetService<T>() where T : class => TryResolve<T>();
        public object GetService(Type serviceType) => TryResolve(serviceType);
        public bool TryGetService<T>(out T service) where T : class { service = TryResolve<T>(); return service != null; }
        public bool TryGetService(Type serviceType, out object service) { service = TryResolve(serviceType); return service != null; }
        public bool ContainsService<T>() where T : class => IsRegistered<T>();
        public bool ContainsService(Type serviceType) => IsRegistered(serviceType);

        public IServiceScope CreateScope() => new EmptyScope();
        public IEnumerable<T> GetServices<T>() where T : class => ResolveAll<T>();
        public IEnumerable<object> GetServices(Type serviceType)
        {
            var obj = TryResolve(serviceType);
            if (obj != null) return new object[] { obj };
            return Array.Empty<object>();
        }
        public void ValidateServices() { }
        public IEnumerable<ServiceRegistrationInfo> GetRegistrationInfo() => _registrations.Values.Select(r => new ServiceRegistrationInfo
        {
            ServiceType = r.ServiceType,
            ImplementationType = r.ImplementationType,
            Lifetime = r.Lifetime,
            HasFactory = r.Factory != null,
            HasInstance = r.Instance != null,
            RegistrationTime = DateTime.Now
        });
        public void RegisterSingleton(Type serviceType, object instance)
        {
            if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            if (!serviceType.IsAssignableFrom(instance.GetType()))
                throw new ArgumentException($"Instance of type {instance.GetType().Name} is not assignable to service type {serviceType.Name}");
            _services[serviceType] = instance;
            var registration = new ServiceRegistrationData(serviceType, instance.GetType(), ServiceLifetime.Singleton, instance, null);
            _registrations[serviceType] = registration;
            ServiceRegistered?.Invoke(registration);
        }
        public void RegisterCollection<TInterface>(params Type[] implementations) where TInterface : class
        {
            if (implementations == null || implementations.Length == 0) return;
            var impl = implementations.Last();
            var created = Activator.CreateInstance(impl);
            RegisterSingleton(typeof(TInterface), created);
        }
        public void RegisterNamed<TInterface, TImplementation>(string name) where TInterface : class where TImplementation : class, TInterface, new() => RegisterSingleton<TInterface, TImplementation>();
        public void RegisterConditional<TInterface, TImplementation>(Func<IServiceLocator, bool> condition) where TInterface : class where TImplementation : class, TInterface, new()
        {
            if (condition == null || !condition(new InternalServiceLocator(this))) return;
            RegisterSingleton<TInterface, TImplementation>();
        }
        public void RegisterDecorator<TInterface, TDecorator>() where TInterface : class where TDecorator : class, TInterface, new() { }
        public void RegisterWithCallback<TInterface, TImplementation>(Action<TImplementation> initializer) where TInterface : class where TImplementation : class, TInterface, new()
        {
            var obj = new TImplementation();
            initializer?.Invoke(obj);
            RegisterSingleton<TInterface>(obj);
        }
        public void RegisterOpenGeneric(Type serviceType, Type implementationType) { }
        public T ResolveNamed<T>(string name) where T : class => TryResolve<T>();
        public IEnumerable<T> ResolveWhere<T>(Func<T, bool> predicate) where T : class => ResolveAll<T>().Where(predicate);
        public T ResolveOrCreate<T>(Func<T> factory) where T : class => TryResolve<T>() ?? factory();
        public T ResolveLast<T>() where T : class => ResolveAll<T>().LastOrDefault();
        public T ResolveWithLifetime<T>(ServiceLifetime lifetime) where T : class => TryResolve<T>();
        public ContainerVerificationResult Verify() => new ContainerVerificationResult { IsValid = true };
        public IEnumerable<AdvancedServiceDescriptor> GetServiceDescriptors() => Enumerable.Empty<AdvancedServiceDescriptor>();
        public void Replace<TInterface, TImplementation>() where TInterface : class where TImplementation : class, TInterface, new() => RegisterSingleton<TInterface, TImplementation>();

        private sealed class EmptyScope : IServiceScope
        {
            public IServiceProvider ServiceProvider => null;
            public void Dispose() { }
        }

        private sealed class InternalServiceLocator : IServiceLocator
        {
            private readonly ServiceContainer _container;
            public InternalServiceLocator(ServiceContainer container) { _container = container; }
            public T GetService<T>() where T : class => _container.TryResolve<T>();
            public object GetService(Type serviceType) => _container.TryResolve(serviceType);
            public bool TryGetService<T>(out T service) where T : class { service = _container.TryResolve<T>(); return service != null; }
            public bool TryGetService(Type serviceType, out object service) { service = _container.TryResolve(serviceType); return service != null; }
            public IEnumerable<T> GetServices<T>() where T : class => _container.ResolveAll<T>();
            public IEnumerable<object> GetServices(Type serviceType) => Array.Empty<object>();
        }
    }
}
