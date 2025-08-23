using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProjectChimera.Core.DependencyInjection;
using ContainerVerificationResult = ProjectChimera.Core.DependencyInjection.ContainerVerificationResult;
using AdvancedServiceDescriptor = ProjectChimera.Core.DependencyInjection.AdvancedServiceDescriptor;

namespace ProjectChimera.Core
{
    /// <summary>
    /// Concrete implementation of IServiceContainer for Project Chimera.
    /// Provides basic dependency injection functionality with lifecycle management.
    /// </summary>
    public class ServiceContainer : IServiceContainer, IDisposable
    {
        private readonly Dictionary<Type, ServiceRegistration> _services = new Dictionary<Type, ServiceRegistration>();
        private readonly Dictionary<Type, object> _singletonInstances = new Dictionary<Type, object>();
        private readonly Dictionary<Type, object> _scopedInstances = new Dictionary<Type, object>();
        private readonly object _lock = new object();
        private bool _disposed = false;

        public bool IsDisposed => _disposed;

        public event Action<ServiceRegistration> ServiceRegistered;
        public event Action<Type, object> ServiceResolved;
        public event Action<Type, Exception> ResolutionFailed;

        #region Registration Methods

        public void RegisterSingleton<TInterface, TImplementation>()
            where TImplementation : class, TInterface, new()
        {
            var registration = new ServiceRegistration(typeof(TInterface), typeof(TImplementation), ServiceLifetime.Singleton, null, null);
            _services[typeof(TInterface)] = registration;
            ServiceRegistered?.Invoke(registration);
        }

        public void RegisterSingleton<TInterface>(Func<IServiceContainer, TInterface> factory)
        {
            var registration = new ServiceRegistration(typeof(TInterface), null, ServiceLifetime.Singleton, null, container => factory((IServiceContainer)container));
            _services[typeof(TInterface)] = registration;
            ServiceRegistered?.Invoke(registration);
        }

        public void RegisterSingleton<TInterface>(TInterface instance)
        {
            var registration = new ServiceRegistration(typeof(TInterface), instance.GetType(), ServiceLifetime.Singleton, instance, null);
            _services[typeof(TInterface)] = registration;
                _singletonInstances[typeof(TInterface)] = instance;
            ServiceRegistered?.Invoke(registration);
        }

        public void RegisterTransient<TInterface, TImplementation>()
            where TImplementation : class, TInterface, new()
        {
            var registration = new ServiceRegistration(typeof(TInterface), typeof(TImplementation), ServiceLifetime.Transient, null, null);
            _services[typeof(TInterface)] = registration;
            ServiceRegistered?.Invoke(registration);
        }

        public void RegisterTransient<TInterface>(Func<IServiceContainer, TInterface> factory)
        {
            var registration = new ServiceRegistration(typeof(TInterface), null, ServiceLifetime.Transient, null, container => factory((IServiceContainer)container));
            _services[typeof(TInterface)] = registration;
            ServiceRegistered?.Invoke(registration);
        }

        public void RegisterScoped<TInterface, TImplementation>()
            where TImplementation : class, TInterface, new()
        {
            var registration = new ServiceRegistration(typeof(TInterface), typeof(TImplementation), ServiceLifetime.Scoped, null, null);
            _services[typeof(TInterface)] = registration;
            ServiceRegistered?.Invoke(registration);
        }

        public void RegisterScoped<TInterface>(Func<IServiceContainer, TInterface> factory)
        {
            var registration = new ServiceRegistration(typeof(TInterface), null, ServiceLifetime.Scoped, null, container => factory((IServiceContainer)container));
            _services[typeof(TInterface)] = registration;
            ServiceRegistered?.Invoke(registration);
        }

        #endregion

        #region Resolution Methods

        public T Resolve<T>()
        {
            return (T)Resolve(typeof(T));
        }

        public object Resolve(Type serviceType)
        {
            try
            {
                var instance = ResolveInternal(serviceType);
                ServiceResolved?.Invoke(serviceType, instance);
                return instance;
            }
            catch (Exception ex)
            {
                ResolutionFailed?.Invoke(serviceType, ex);
                throw;
            }
        }

        public T TryResolve<T>() where T : class
        {
            return TryResolve(typeof(T)) as T;
        }

        public object TryResolve(Type serviceType)
        {
            try
            {
                return ResolveInternal(serviceType);
            }
            catch
            {
                return null;
            }
        }

        private object ResolveInternal(Type serviceType)
        {
            if (!_services.TryGetValue(serviceType, out var registration))
            {
                throw new InvalidOperationException($"Service of type {serviceType.Name} is not registered");
            }

            lock (_lock)
            {
                switch (registration.Lifetime)
                {
                    case ServiceLifetime.Singleton:
                        if (registration.Instance != null)
                            return registration.Instance;
                        
                        if (_singletonInstances.TryGetValue(serviceType, out var singletonInstance))
                            return singletonInstance;

                        var newInstance = CreateInstance(registration);
                        _singletonInstances[serviceType] = newInstance;
                        return newInstance;

                    case ServiceLifetime.Transient:
                        return CreateInstance(registration);

                    case ServiceLifetime.Scoped:
                        if (_scopedInstances.TryGetValue(serviceType, out var scopedInstance))
                            return scopedInstance;

                        var newScopedInstance = CreateInstance(registration);
                        _scopedInstances[serviceType] = newScopedInstance;
                        return newScopedInstance;

                    default:
                        throw new ArgumentOutOfRangeException();
                }
            }
        }

        private object CreateInstance(ServiceRegistration registration)
        {
            if (registration.Factory != null)
            {
                return registration.Factory(new ServiceLocatorAdapter(this));
            }

            if (registration.ImplementationType != null)
            {
                return Activator.CreateInstance(registration.ImplementationType);
            }

            throw new InvalidOperationException($"Cannot create instance for service {registration.ServiceType.Name}");
        }

        #endregion

        #region Query Methods

        public bool IsRegistered<T>()
        {
            return IsRegistered(typeof(T));
        }

        public bool IsRegistered(Type serviceType)
            {
                return _services.ContainsKey(serviceType);
        }

        public IEnumerable<Type> GetRegisteredTypes<T>()
        {
            return GetRegisteredTypes(typeof(T));
        }

        public IEnumerable<Type> GetRegisteredTypes(Type interfaceType)
            {
                return _services.Values
                .Where(r => interfaceType.IsAssignableFrom(r.ServiceType))
                .Select(r => r.ServiceType);
        }

        public IEnumerable<T> ResolveAll<T>()
        {
            return GetRegisteredTypes<T>().Select(Resolve).Cast<T>();
        }

        public IServiceScope CreateScope()
        {
            return new ServiceScope(this);
        }

        public void ValidateServices()
        {
            foreach (var registration in _services.Values)
            {
                try
                {
                    if (registration.Lifetime != ServiceLifetime.Singleton || registration.Instance == null)
                    {
                        ResolveInternal(registration.ServiceType);
                    }
                    }
                    catch (Exception ex)
                    {
                    throw new InvalidOperationException($"Service validation failed for {registration.ServiceType.Name}: {ex.Message}", ex);
                }
            }
        }

        public IEnumerable<ServiceRegistrationInfo> GetRegistrationInfo()
        {
            return _services.Values.Select(r => new ServiceRegistrationInfo
            {
                ServiceType = r.ServiceType,
                ImplementationType = r.ImplementationType,
                Lifetime = r.Lifetime,
                HasFactory = r.Factory != null,
                HasInstance = r.Instance != null,
                RegistrationTime = DateTime.Now
            });
        }

        public void Clear()
        {
            lock (_lock)
            {
                _services.Clear();
                _singletonInstances.Clear();
                _scopedInstances.Clear();
            }
        }

        public IServiceContainer CreateChildContainer()
        {
            var childContainer = new ServiceContainer();
            // Copy registrations from parent
            foreach (var registration in _services.Values)
            {
                childContainer._services[registration.ServiceType] = registration;
            }
            return childContainer;
        }

        public void RegisterFactory<TInterface>(Func<IServiceLocator, TInterface> factory) where TInterface : class
        {
            var registration = new ServiceRegistration(typeof(TInterface), null, ServiceLifetime.Transient, null, container => factory(new ServiceLocatorAdapter(container)));
            _services[typeof(TInterface)] = registration;
            ServiceRegistered?.Invoke(registration);
        }

        public IDictionary<Type, ServiceRegistration> GetRegistrations()
        {
            return new Dictionary<Type, ServiceRegistration>(_services);
        }

        // Additional interface methods
        public T GetService<T>() where T : class
        {
            return TryResolve<T>();
        }

        public object GetService(Type serviceType)
        {
            return TryResolve(serviceType);
        }

        public bool TryGetService<T>(out T service) where T : class
        {
            service = TryResolve<T>();
            return service != null;
        }

        public bool TryGetService(Type serviceType, out object service)
        {
            service = TryResolve(serviceType);
            return service != null;
        }

        public bool ContainsService<T>() where T : class
        {
            return IsRegistered<T>();
        }

        public bool ContainsService(Type serviceType)
        {
            return IsRegistered(serviceType);
        }

        public void RegisterSingleton(Type serviceType, object instance)
        {
            var registration = new ServiceRegistration(serviceType, instance.GetType(), ServiceLifetime.Singleton, instance, null);
            _services[serviceType] = registration;
            _singletonInstances[serviceType] = instance;
            ServiceRegistered?.Invoke(registration);
        }

        public void RegisterCollection<TInterface>(params Type[] implementations) where TInterface : class
        {
            // Basic implementation - register each implementation
            foreach (var impl in implementations)
            {
                var registration = new ServiceRegistration(typeof(TInterface), impl, ServiceLifetime.Transient, null, null);
                _services[impl] = registration;
                ServiceRegistered?.Invoke(registration);
            }
        }

        public void RegisterNamed<TInterface, TImplementation>(string name) 
            where TInterface : class
            where TImplementation : class, TInterface, new()
        {
            // Named services not fully supported in basic implementation
            RegisterTransient<TInterface, TImplementation>();
        }

        public void RegisterConditional<TInterface, TImplementation>(Func<IServiceLocator, bool> condition) 
            where TInterface : class
            where TImplementation : class, TInterface, new()
        {
            // Conditional registration not fully supported in basic implementation
            RegisterTransient<TInterface, TImplementation>();
        }

        public void RegisterDecorator<TInterface, TDecorator>() 
            where TInterface : class
            where TDecorator : class, TInterface, new()
        {
            // Decorator pattern not fully supported in basic implementation
            RegisterTransient<TInterface, TDecorator>();
        }

        public void RegisterWithCallback<TInterface, TImplementation>(Action<TImplementation> initializer) 
            where TInterface : class
            where TImplementation : class, TInterface, new()
        {
            // Callback registration not fully supported in basic implementation
            RegisterTransient<TInterface, TImplementation>();
        }

        public void RegisterOpenGeneric(Type serviceType, Type implementationType)
        {
            // Open generic registration not fully supported in basic implementation
            var registration = new ServiceRegistration(serviceType, implementationType, ServiceLifetime.Transient, null, null);
            _services[serviceType] = registration;
            ServiceRegistered?.Invoke(registration);
        }

        public T ResolveNamed<T>(string name) where T : class
        {
            // Named resolution not fully supported in basic implementation
            return Resolve<T>();
        }

        public IEnumerable<T> ResolveWhere<T>(Func<T, bool> predicate) where T : class
        {
            return ResolveAll<T>().Where(predicate);
        }

        public T ResolveOrCreate<T>(Func<T> factory) where T : class
        {
            var service = TryResolve<T>();
            return service ?? factory();
        }

        public T ResolveLast<T>() where T : class
        {
            return ResolveAll<T>().LastOrDefault();
        }

        public T ResolveWithLifetime<T>(ServiceLifetime lifetime) where T : class
        {
            // Lifetime-specific resolution not fully supported in basic implementation
            return Resolve<T>();
        }

        public ContainerVerificationResult Verify()
        {
            var result = new ContainerVerificationResult
            {
                IsValid = true,
                ValidationMessages = new List<string>()
            };
            
            try
            {
                ValidateServices();
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.ValidationMessages.Add(ex.Message);
            }
            
            return result;
        }

        public IEnumerable<AdvancedServiceDescriptor> GetServiceDescriptors()
        {
            return _services.Values.Select(r => new AdvancedServiceDescriptor
            {
                ServiceType = r.ServiceType,
                ImplementationType = r.ImplementationType,
                Lifetime = r.Lifetime,
                Instance = r.Instance
            });
        }

        public void Replace<TInterface, TImplementation>() 
            where TInterface : class
            where TImplementation : class, TInterface, new()
        {
            // Remove existing registration
            _services.Remove(typeof(TInterface));
            _singletonInstances.Remove(typeof(TInterface));
            // Re-register
            RegisterTransient<TInterface, TImplementation>();
        }

        public bool Unregister<T>() where T : class
        {
            var removed = _services.Remove(typeof(T));
            _singletonInstances.Remove(typeof(T));
            _scopedInstances.Remove(typeof(T));
            return removed;
        }

        #endregion

        #region IDisposable

        public void Dispose()
        {
            if (_disposed) return;

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
                        Debug.LogError($"Error disposing singleton service: {ex.Message}");
                    }
                }

                foreach (var instance in _scopedInstances.Values.OfType<IDisposable>())
                {
                    try
                    {
                        instance.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Error disposing scoped service: {ex.Message}");
                    }
                }

                Clear();
                _disposed = true;
            }
        }

        #endregion
    }

    /// <summary>
    /// Basic service scope implementation
    /// </summary>
    public class ServiceScope : IServiceScope
    {
        private readonly ProjectChimera.Core.DependencyInjection.IServiceProvider _serviceProvider;
        private bool _disposed = false;

        public ServiceScope(IServiceContainer serviceContainer)
        {
            _serviceProvider = ProjectChimera.Core.DependencyInjection.ServiceContainerIntegration.ToServiceProvider(serviceContainer);
        }

        public ProjectChimera.Core.DependencyInjection.IServiceProvider ServiceProvider => _serviceProvider;

        public void Dispose()
        {
            if (_disposed) return;
            _disposed = true;
        }
    }

    /// <summary>
    /// Adapter to convert IServiceContainer to IServiceLocator
    /// </summary>
    public class ServiceLocatorAdapter : IServiceLocator
    {
        private readonly object _container;

        public ServiceLocatorAdapter(object container)
        {
            _container = container;
        }

        // Registration Methods
        public void RegisterSingleton<TInterface, TImplementation>() where TImplementation : class, TInterface, new()
        {
            if (_container is IServiceContainer serviceContainer)
            {
                serviceContainer.RegisterSingleton<TInterface, TImplementation>();
            }
        }

        public void RegisterSingleton<TInterface>(TInterface instance) where TInterface : class
        {
            if (_container is IServiceContainer serviceContainer)
            {
                serviceContainer.RegisterSingleton(instance);
            }
        }

        public void RegisterSingleton(Type serviceType, object instance)
        {
            // Not directly supported by IServiceContainer, use generic version
            if (_container is IServiceContainer serviceContainer)
            {
                var method = typeof(IServiceContainer).GetMethod("RegisterSingleton", new[] { typeof(object) });
                var genericMethod = method?.MakeGenericMethod(serviceType);
                genericMethod?.Invoke(serviceContainer, new[] { instance });
            }
        }

        public void RegisterTransient<TInterface, TImplementation>() where TImplementation : class, TInterface, new()
        {
            if (_container is IServiceContainer serviceContainer)
            {
                serviceContainer.RegisterTransient<TInterface, TImplementation>();
            }
        }

        public void RegisterScoped<TInterface, TImplementation>() where TImplementation : class, TInterface, new()
        {
            if (_container is IServiceContainer serviceContainer)
            {
                serviceContainer.RegisterScoped<TInterface, TImplementation>();
            }
        }

        public void RegisterFactory<TInterface>(Func<IServiceLocator, TInterface> factory) where TInterface : class
        {
            if (_container is IServiceContainer serviceContainer)
            {
                serviceContainer.RegisterFactory(factory);
            }
        }

        // Service Resolution Methods
        public T GetService<T>() where T : class
        {
            if (_container is IServiceContainer serviceContainer)
            {
                return serviceContainer.TryResolve<T>();
            }
            return null;
        }

        public object GetService(Type serviceType)
        {
            if (_container is IServiceContainer serviceContainer)
            {
                return serviceContainer.TryResolve(serviceType);
            }
            return null;
        }

        public bool TryGetService<T>(out T service) where T : class
        {
            service = GetService<T>();
            return service != null;
        }

        public bool TryGetService(Type serviceType, out object service)
        {
            service = GetService(serviceType);
            return service != null;
        }

        public bool ContainsService<T>() where T : class
        {
            if (_container is IServiceContainer serviceContainer)
            {
                return serviceContainer.IsRegistered<T>();
            }
            return false;
        }

        public bool ContainsService(Type serviceType)
        {
            if (_container is IServiceContainer serviceContainer)
            {
                return serviceContainer.IsRegistered(serviceType);
            }
            return false;
        }

        public T Resolve<T>() where T : class
        {
            if (_container is IServiceContainer serviceContainer)
            {
                return serviceContainer.Resolve<T>();
            }
            return null;
        }

        public T TryResolve<T>() where T : class
        {
            if (_container is IServiceContainer serviceContainer)
            {
                return serviceContainer.TryResolve<T>();
            }
            return null;
        }

        public IEnumerable<T> ResolveAll<T>() where T : class
        {
            if (_container is IServiceContainer serviceContainer)
            {
                return serviceContainer.ResolveAll<T>();
            }
            return Enumerable.Empty<T>();
        }

        // Additional IServiceProvider compatibility methods
        public T GetRequiredService<T>() where T : class
        {
            if (_container is IServiceContainer serviceContainer)
            {
                return serviceContainer.Resolve<T>();
            }
            throw new InvalidOperationException($"Required service of type {typeof(T).Name} could not be resolved");
        }

        public object GetRequiredService(Type serviceType)
        {
            if (_container is IServiceContainer serviceContainer)
            {
                return serviceContainer.Resolve(serviceType);
            }
            throw new InvalidOperationException($"Required service of type {serviceType.Name} could not be resolved");
        }

        public IEnumerable<object> GetServices(Type serviceType)
        {
            if (_container is IServiceContainer serviceContainer)
            {
                try
                {
                    // Try to resolve all services of the given type
                    var method = typeof(IServiceContainer).GetMethod("ResolveAll");
                    var genericMethod = method?.MakeGenericMethod(serviceType);
                    var result = genericMethod?.Invoke(serviceContainer, null);
                    return result as IEnumerable<object> ?? Enumerable.Empty<object>();
                }
                catch
                {
                    return Enumerable.Empty<object>();
                }
            }
            return Enumerable.Empty<object>();
        }

        public IEnumerable<T> GetServices<T>() where T : class
        {
            if (_container is IServiceContainer serviceContainer)
            {
                return serviceContainer.ResolveAll<T>();
            }
            return Enumerable.Empty<T>();
        }
    }
}
