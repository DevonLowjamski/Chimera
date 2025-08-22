using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
// Force recompilation - Added missing IServiceProvider interface methods to ServiceProviderContainerAdapter

namespace ProjectChimera.Core.DependencyInjection
{
    public class ServiceContainerIntegration
    {
        public static IServiceContainer ToServiceContainer(IServiceProvider serviceProvider)
        {
            if (serviceProvider == null)
                throw new ArgumentNullException(nameof(serviceProvider));

            if (serviceProvider is ServiceProviderAdapter adapter)
            {
                return adapter.GetUnderlyingContainer();
            }

            return new ServiceProviderContainerAdapter(serviceProvider);
        }

        public static IServiceProvider ToServiceProvider(IServiceContainer serviceContainer, ServiceProviderOptions options = null)
        {
            if (serviceContainer == null)
                throw new ArgumentNullException(nameof(serviceContainer));

            return new ServiceProviderAdapter(serviceContainer, options ?? new ServiceProviderOptions());
        }

        public static UnifiedDIContainer CreateUnified()
        {
            return new UnifiedDIContainer();
        }

        public static void MigrateServices(IServiceProvider source, IServiceContainer target)
        {
            if (source == null)
                throw new ArgumentNullException(nameof(source));
            if (target == null)
                throw new ArgumentNullException(nameof(target));

            Debug.Log("[ServiceContainerIntegration] Migrating services from IServiceProvider to IServiceContainer");
            Debug.LogWarning("[ServiceContainerIntegration] Service migration requires manual registration of services");
        }
    }

    internal class ServiceProviderContainerAdapter : IServiceContainer
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly Dictionary<Type, object> _additionalServices = new Dictionary<Type, object>();

        public ServiceProviderContainerAdapter(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
        }

        public bool IsDisposed => false;

        public event Action<AdvancedServiceDescriptor> ServiceRegistered;
        public event Action<Type, object> ServiceResolved;
        public event Action<Type, Exception> ResolutionFailed;

        public T GetService<T>() where T : class => (T)GetService(typeof(T));
        public object GetService(Type serviceType) => _serviceProvider.GetService(serviceType);

        public bool TryGetService<T>(out T service) where T : class
        {
            service = (T)_serviceProvider.GetService(typeof(T));
            return service != null;
        }

        public bool TryGetService(Type serviceType, out object service)
        {
            service = _serviceProvider.GetService(serviceType);
            return service != null;
        }

        public bool ContainsService<T>() where T : class => IsRegistered<T>();
        public bool ContainsService(Type serviceType) => IsRegistered(serviceType);
        
        // Missing IServiceProvider interface methods
        public T GetRequiredService<T>() where T : class
        {
            var service = GetService<T>();
            if (service == null)
            {
                throw new InvalidOperationException($"Required service of type {typeof(T).Name} could not be resolved");
            }
            return service;
        }
        
        public object GetRequiredService(Type serviceType)
        {
            var service = GetService(serviceType);
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
                return (IEnumerable<object>)_serviceProvider.GetService(typeof(IEnumerable<>).MakeGenericType(serviceType)) ?? Array.Empty<object>();
            }
            catch
            {
                return Array.Empty<object>();
            }
        }
        
        public IEnumerable<T> GetServices<T>() where T : class 
        {
            try
            {
                return GetServices(typeof(T)).OfType<T>();
            }
            catch
            {
                return Array.Empty<T>();
            }
        }
        
        public T Resolve<T>() where T : class
        {
            try
            {
                var service = (T)_serviceProvider.GetService(typeof(T));
                ServiceResolved?.Invoke(typeof(T), service);
                return service;
            }
            catch (Exception ex)
            {
                ResolutionFailed?.Invoke(typeof(T), ex);
                throw;
            }
        }
        
        public object Resolve(Type serviceType)
        {
            try
            {
                var service = _serviceProvider.GetService(serviceType);
                ServiceResolved?.Invoke(serviceType, service);
                return service;
            }
            catch (Exception ex)
            {
                ResolutionFailed?.Invoke(serviceType, ex);
                throw;
            }
        }
        
        public T TryResolve<T>() where T : class => (T)_serviceProvider.GetService(typeof(T));
        public bool IsRegistered<T>() where T : class => _serviceProvider.GetService(typeof(T)) != null;
        public bool IsRegistered(Type serviceType) => _serviceProvider.GetService(serviceType) != null;
        public IEnumerable<T> ResolveAll<T>() where T : class 
        {
            try
            {
                var result = _serviceProvider.GetService(typeof(IEnumerable<T>));
                return result as IEnumerable<T> ?? Array.Empty<T>();
            }
            catch
            {
                return Array.Empty<T>();
            }
        }
        public IServiceScope CreateScope()
        {
            var scopeFactory = (IServiceScopeFactory)_serviceProvider.GetService(typeof(IServiceScopeFactory));
            return scopeFactory?.CreateScope();
        }

        public void Clear()
        {
            _additionalServices.Clear();
            Debug.LogWarning("[ServiceProviderContainerAdapter] Cannot clear services from underlying IServiceProvider");
        }

        public IDictionary<Type, ServiceRegistration> GetRegistrations()
        {
            Debug.LogWarning("[ServiceProviderContainerAdapter] Cannot get registrations from underlying IServiceProvider");
            return new Dictionary<Type, ServiceRegistration>();
        }
        
        public void RegisterSingleton<TInterface, TImplementation>() where TImplementation : class, TInterface, new() => throw new NotSupportedException("Cannot register services on IServiceProvider adapter");
        public void RegisterSingleton<TInterface>(TInterface instance) where TInterface : class
        {
            _additionalServices[typeof(TInterface)] = instance;
            ServiceRegistered?.Invoke(new AdvancedServiceDescriptor
            {
                ServiceType = typeof(TInterface),
                ImplementationType = instance.GetType(),
                Lifetime = ServiceLifetime.Singleton,
                Instance = instance
            });
        }

        public void RegisterSingleton(Type serviceType, object instance)
        {
            if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));
            if (instance == null) throw new ArgumentNullException(nameof(instance));
            if (!serviceType.IsAssignableFrom(instance.GetType()))
                throw new ArgumentException($"Instance of type {instance.GetType().Name} is not assignable to service type {serviceType.Name}");

            _additionalServices[serviceType] = instance;
            ServiceRegistered?.Invoke(new AdvancedServiceDescriptor
            {
                ServiceType = serviceType,
                ImplementationType = instance.GetType(),
                Lifetime = ServiceLifetime.Singleton,
                Instance = instance
            });
        }
        public void RegisterTransient<TInterface, TImplementation>() where TImplementation : class, TInterface, new() => throw new NotSupportedException("Cannot register services on IServiceProvider adapter");
        public void RegisterScoped<TInterface, TImplementation>() where TImplementation : class, TInterface, new() => throw new NotSupportedException("Cannot register services on IServiceProvider adapter");
        public void RegisterFactory<TInterface>(Func<IServiceLocator, TInterface> factory) where TInterface : class => throw new NotSupportedException("Cannot register factories on IServiceProvider adapter");
        
        public void RegisterCollection<TInterface>(params Type[] implementations) where TInterface : class => throw new NotSupportedException("Advanced registration not supported on IServiceProvider adapter");
        public void RegisterNamed<TInterface, TImplementation>(string name) where TImplementation : class, TInterface, new() => throw new NotSupportedException("Named registration not supported on IServiceProvider adapter");
        public void RegisterConditional<TInterface, TImplementation>(Func<IServiceLocator, bool> condition) where TImplementation : class, TInterface, new() => throw new NotSupportedException("Conditional registration not supported on IServiceProvider adapter");
        public void RegisterDecorator<TInterface, TDecorator>() where TDecorator : class, TInterface where TInterface : class => throw new NotSupportedException("Decorator registration not supported on IServiceProvider adapter");
        public void RegisterWithCallback<TInterface, TImplementation>(Action<TImplementation> initializer) where TImplementation : class, TInterface, new() => throw new NotSupportedException("Callback registration not supported on IServiceProvider adapter");
        public void RegisterOpenGeneric(Type serviceType, Type implementationType) => throw new NotSupportedException("Open generic registration not supported on IServiceProvider adapter");
        public T ResolveNamed<T>(string name) where T : class => throw new NotSupportedException("Named resolution not supported on IServiceProvider adapter");
        public IEnumerable<T> ResolveWhere<T>(Func<T, bool> predicate) where T : class => ((IEnumerable<T>)_serviceProvider.GetService(typeof(IEnumerable<T>))).Where(predicate);
        public T ResolveOrCreate<T>(Func<T> factory) where T : class => (T)_serviceProvider.GetService(typeof(T)) ?? factory();
        public T ResolveLast<T>() where T : class => ((IEnumerable<T>)_serviceProvider.GetService(typeof(IEnumerable<T>))).LastOrDefault();
        public T ResolveWithLifetime<T>(ServiceLifetime lifetime) where T : class => (T)_serviceProvider.GetService(typeof(T));
        public IServiceContainer CreateChildContainer() => throw new NotSupportedException("Child containers not supported on IServiceProvider adapter");
        public ContainerVerificationResult Verify() => new ContainerVerificationResult { IsValid = true };
        public IEnumerable<AdvancedServiceDescriptor> GetServiceDescriptors() => Enumerable.Empty<AdvancedServiceDescriptor>();
        public void Replace<TInterface, TImplementation>() where TImplementation : class, TInterface, new() => throw new NotSupportedException("Service replacement not supported on IServiceProvider adapter");
        public bool Unregister<T>() where T : class => _additionalServices.Remove(typeof(T));

        public void Dispose()
        {
            _additionalServices.Clear();
            if (_serviceProvider is IDisposable disposable)
            {
                disposable.Dispose();
            }
        }
    }

    internal static class ServiceProviderAdapterExtensions
    {
        public static IServiceContainer GetUnderlyingContainer(this ServiceProviderAdapter adapter)
        {
            var field = typeof(ServiceProviderAdapter).GetField("_container", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
            return field?.GetValue(adapter) as IServiceContainer;
        }
    }
    
    public class UnifiedDIContainer : IServiceContainer, IServiceProvider, IServiceScopeFactory
    {
        private readonly IServiceContainer _container;
        private readonly IServiceProvider _serviceProvider;
        
        public UnifiedDIContainer()
        {
            _container = new ServiceContainer();
            _serviceProvider = new ServiceProviderAdapter(_container, new ServiceProviderOptions());
        }
        
        public bool IsDisposed => _container.IsDisposed;
        
        public event Action<AdvancedServiceDescriptor> ServiceRegistered
        {
            add => _container.ServiceRegistered += value;
            remove => _container.ServiceRegistered -= value;
        }
        
        public event Action<Type, object> ServiceResolved
        {
            add => _container.ServiceResolved += value;
            remove => _container.ServiceResolved -= value;
        }
        
        public event Action<Type, Exception> ResolutionFailed
        {
            add => _container.ResolutionFailed += value;
            remove => _container.ResolutionFailed -= value;
        }
        
        public T GetService<T>() where T : class => _container.GetService<T>();
        public object GetService(Type serviceType) => _container.GetService(serviceType);
        public bool TryGetService<T>(out T service) where T : class => _container.TryGetService(out service);
        public bool TryGetService(Type serviceType, out object service) => _container.TryGetService(serviceType, out service);
        public bool ContainsService<T>() where T : class => _container.ContainsService<T>();
        public bool ContainsService(Type serviceType) => _container.ContainsService(serviceType);
        public T Resolve<T>() where T : class => _container.Resolve<T>();
        public object Resolve(Type serviceType) => _container.Resolve(serviceType);
        public T TryResolve<T>() where T : class => _container.TryResolve<T>();
        public bool IsRegistered<T>() where T : class => _container.IsRegistered<T>();
        public bool IsRegistered(Type serviceType) => _container.IsRegistered(serviceType);
        public IEnumerable<T> ResolveAll<T>() where T : class => _container.ResolveAll<T>();
        public IServiceScope CreateScope() => _container.CreateScope();
        public void Clear() => _container.Clear();
        public IDictionary<Type, ServiceRegistration> GetRegistrations() => _container.GetRegistrations();
        public void RegisterSingleton<TInterface, TImplementation>() where TImplementation : class, TInterface, new() => _container.RegisterSingleton<TInterface, TImplementation>();
        public void RegisterSingleton<TInterface>(TInterface instance) where TInterface : class => _container.RegisterSingleton(instance);
        public void RegisterSingleton(Type serviceType, object instance) => _container.RegisterSingleton(serviceType, instance);
        public void RegisterTransient<TInterface, TImplementation>() where TImplementation : class, TInterface, new() => _container.RegisterTransient<TInterface, TImplementation>();
        public void RegisterScoped<TInterface, TImplementation>() where TImplementation : class, TInterface, new() => _container.RegisterScoped<TInterface, TImplementation>();
        public void RegisterFactory<TInterface>(Func<IServiceLocator, TInterface> factory) where TInterface : class => _container.RegisterFactory(factory);
        public void RegisterCollection<TInterface>(params Type[] implementations) where TInterface : class => _container.RegisterCollection<TInterface>(implementations);
        public void RegisterNamed<TInterface, TImplementation>(string name) where TImplementation : class, TInterface, new() => _container.RegisterNamed<TInterface, TImplementation>(name);
        public void RegisterConditional<TInterface, TImplementation>(Func<IServiceLocator, bool> condition) where TImplementation : class, TInterface, new() => _container.RegisterConditional<TInterface, TImplementation>(condition);
        public void RegisterDecorator<TInterface, TDecorator>() where TDecorator : class, TInterface where TInterface : class => _container.RegisterDecorator<TInterface, TDecorator>();
        public void RegisterWithCallback<TInterface, TImplementation>(Action<TImplementation> initializer) where TImplementation : class, TInterface, new() => _container.RegisterWithCallback<TInterface, TImplementation>(initializer);
        public void RegisterOpenGeneric(Type serviceType, Type implementationType) => _container.RegisterOpenGeneric(serviceType, implementationType);
        public T ResolveNamed<T>(string name) where T : class => _container.ResolveNamed<T>(name);
        public IEnumerable<T> ResolveWhere<T>(Func<T, bool> predicate) where T : class => _container.ResolveWhere(predicate);
        public T ResolveOrCreate<T>(Func<T> factory) where T : class => _container.ResolveOrCreate(factory);
        public T ResolveLast<T>() where T : class => _container.ResolveLast<T>();
        public T ResolveWithLifetime<T>(ServiceLifetime lifetime) where T : class => _container.ResolveWithLifetime<T>(lifetime);
        public IServiceContainer CreateChildContainer() => _container.CreateChildContainer();
        public ContainerVerificationResult Verify() => _container.Verify();
        public IEnumerable<AdvancedServiceDescriptor> GetServiceDescriptors() => _container.GetServiceDescriptors();
        public void Replace<TInterface, TImplementation>() where TImplementation : class, TInterface, new() => _container.Replace<TInterface, TImplementation>();
        public bool Unregister<T>() where T : class => _container.Unregister<T>();
        
        public object GetRequiredService(Type serviceType) => _serviceProvider.GetService(serviceType) ?? throw new InvalidOperationException($"Service of type {serviceType.Name} not found.");
        public T GetRequiredService<T>() where T : class => (T)GetRequiredService(typeof(T));
        public IEnumerable<object> GetServices(Type serviceType) => (IEnumerable<object>)_serviceProvider.GetService(typeof(IEnumerable<>).MakeGenericType(serviceType));
        public IEnumerable<T> GetServices<T>() where T : class 
        {
            try
            {
                return _container.ResolveAll<T>();
            }
            catch
            {
                return Array.Empty<T>();
            }
        }
        
        IServiceScope IServiceScopeFactory.CreateScope()
        {
            var scopeFactory = (IServiceScopeFactory)_serviceProvider.GetService(typeof(IServiceScopeFactory));
            return scopeFactory?.CreateScope();
        }
        
        public void Dispose()
        {
            if (_container is IDisposable disposableContainer)
            {
                disposableContainer.Dispose();
            }
        }
    }
}
