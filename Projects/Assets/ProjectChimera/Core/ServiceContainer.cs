using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using IServiceProvider = ProjectChimera.Core.DependencyInjection.IServiceProvider;

namespace ProjectChimera.Core.DependencyInjection
{
    public class ServiceContainer : IServiceContainer, IDisposable
    {
        private readonly object _lock = new object();
        private readonly Dictionary<Type, List<AdvancedServiceDescriptor>> _services = new Dictionary<Type, List<AdvancedServiceDescriptor>>();
        private readonly Dictionary<string, AdvancedServiceDescriptor> _namedServices = new Dictionary<string, AdvancedServiceDescriptor>();
        private readonly Dictionary<Type, object> _singletonInstances = new Dictionary<Type, object>();
        private readonly List<IServiceScope> _activeScopes = new List<IServiceScope>();
        private readonly List<IServiceContainer> _childContainers = new List<IServiceContainer>();
        private readonly Dictionary<Type, int> _resolutionCounts = new Dictionary<Type, int>();
        private readonly Dictionary<Type, TimeSpan> _resolutionTimes = new Dictionary<Type, TimeSpan>();
        private int _totalResolutions = 0;
        private int _cacheHits = 0;
        private bool _isDisposed = false;
        private readonly IServiceContainer _parentContainer;
        public event Action<AdvancedServiceDescriptor> ServiceRegistered;
        public event Action<Type, object> ServiceResolved;
        public event Action<Type, Exception> ResolutionFailed;
        public bool IsDisposed => _isDisposed;

        public ServiceContainer() : this(null) { }

        public ServiceContainer(IServiceContainer parentContainer)
        {
            _parentContainer = parentContainer;
            RegisterSelf();
        }

        public void RegisterSingleton<TInterface, TImplementation>() where TImplementation : class, TInterface, new()
        {
            RegisterService<TInterface, TImplementation>(ServiceLifetime.Singleton);
        }

        public void RegisterSingleton<TInterface>(TInterface instance) where TInterface : class
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));

            lock (_lock)
            {
                ThrowIfDisposed();
                var descriptor = new AdvancedServiceDescriptor
                {
                    ServiceType = typeof(TInterface),
                    ImplementationType = instance.GetType(),
                    Lifetime = ServiceLifetime.Singleton,
                    Instance = instance,
                    Factory = _ => instance
                };
                AddServiceDescriptor(typeof(TInterface), descriptor);
                _singletonInstances[typeof(TInterface)] = instance;
                ServiceRegistered?.Invoke(descriptor);
                LogRegistration($"Singleton Instance: {typeof(TInterface).Name}");
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
                ThrowIfDisposed();
                var descriptor = new AdvancedServiceDescriptor
                {
                    ServiceType = serviceType,
                    ImplementationType = instance.GetType(),
                    Lifetime = ServiceLifetime.Singleton,
                    Instance = instance,
                    Factory = _ => instance
                };
                AddServiceDescriptor(serviceType, descriptor);
                _singletonInstances[serviceType] = instance;
                ServiceRegistered?.Invoke(descriptor);
                LogRegistration($"Singleton Instance: {serviceType.Name}");
            }
        }

        public void RegisterTransient<TInterface, TImplementation>() where TImplementation : class, TInterface, new()
        {
            RegisterService<TInterface, TImplementation>(ServiceLifetime.Transient);
        }

        public void RegisterScoped<TInterface, TImplementation>() where TImplementation : class, TInterface, new()
        {
            RegisterService<TInterface, TImplementation>(ServiceLifetime.Scoped);
        }

        public void RegisterFactory<TInterface>(Func<IServiceLocator, TInterface> factory) where TInterface : class
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));

            lock (_lock)
            {
                ThrowIfDisposed();
                var descriptor = new AdvancedServiceDescriptor
                {
                    ServiceType = typeof(TInterface),
                    ImplementationType = typeof(TInterface),
                    Lifetime = ServiceLifetime.Transient,
                    Factory = locator => factory(locator)
                };
                AddServiceDescriptor(typeof(TInterface), descriptor);
                ServiceRegistered?.Invoke(descriptor);
                LogRegistration($"Factory: {typeof(TInterface).Name}");
            }
        }

        public void RegisterCollection<TInterface>(params Type[] implementations) where TInterface : class
        {
            if (implementations == null || implementations.Length == 0)
                throw new ArgumentException("At least one implementation must be provided", nameof(implementations));

            lock (_lock)
            {
                ThrowIfDisposed();
                foreach (var implementation in implementations)
                {
                    if (!typeof(TInterface).IsAssignableFrom(implementation))
                        throw new ArgumentException($"Type {implementation.Name} does not implement {typeof(TInterface).Name}");

                    var descriptor = new AdvancedServiceDescriptor
                    {
                        ServiceType = typeof(TInterface),
                        ImplementationType = implementation,
                        Lifetime = ServiceLifetime.Transient,
                        Factory = locator => Activator.CreateInstance(implementation)
                    };
                    AddServiceDescriptor(typeof(TInterface), descriptor);
                    ServiceRegistered?.Invoke(descriptor);
                }
                LogRegistration($"Collection: {typeof(TInterface).Name} with {implementations.Length} implementations");
            }
        }

        public void RegisterNamed<TInterface, TImplementation>(string name) where TImplementation : class, TInterface, new()
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Name cannot be null or empty", nameof(name));

            lock (_lock)
            {
                ThrowIfDisposed();
                var key = $"{typeof(TInterface).FullName}:{name}";
                if (_namedServices.ContainsKey(key))
                    throw new InvalidOperationException($"Named service '{name}' for type {typeof(TInterface).Name} is already registered");

                var descriptor = new AdvancedServiceDescriptor
                {
                    ServiceType = typeof(TInterface),
                    ImplementationType = typeof(TImplementation),
                    Lifetime = ServiceLifetime.Transient,
                    Name = name,
                    Factory = _ => new TImplementation()
                };
                _namedServices[key] = descriptor;
                ServiceRegistered?.Invoke(descriptor);
                LogRegistration($"Named: {typeof(TInterface).Name} as '{name}' -> {typeof(TImplementation).Name}");
            }
        }

        public void RegisterConditional<TInterface, TImplementation>(Func<IServiceLocator, bool> condition) where TImplementation : class, TInterface, new()
        {
            if (condition == null)
                throw new ArgumentNullException(nameof(condition));

            lock (_lock)
            {
                ThrowIfDisposed();
                var descriptor = new AdvancedServiceDescriptor
                {
                    ServiceType = typeof(TInterface),
                    ImplementationType = typeof(TImplementation),
                    Lifetime = ServiceLifetime.Transient,
                    Condition = condition,
                    Factory = _ => new TImplementation()
                };
                AddServiceDescriptor(typeof(TInterface), descriptor);
                ServiceRegistered?.Invoke(descriptor);
                LogRegistration($"Conditional: {typeof(TInterface).Name} -> {typeof(TImplementation).Name}");
            }
        }

        public void RegisterDecorator<TInterface, TDecorator>() where TDecorator : class, TInterface where TInterface : class
        {
            lock (_lock)
            {
                ThrowIfDisposed();
                if (!_services.ContainsKey(typeof(TInterface)) || _services[typeof(TInterface)].Count == 0)
                    throw new InvalidOperationException($"No existing registration found for {typeof(TInterface).Name} to decorate");

                var descriptor = new AdvancedServiceDescriptor
                {
                    ServiceType = typeof(TInterface),
                    ImplementationType = typeof(TDecorator),
                    Lifetime = ServiceLifetime.Transient,
                    IsDecorator = true,
                    Factory = locator => CreateDecorator<TInterface, TDecorator>(locator)
                };
                AddServiceDescriptor(typeof(TInterface), descriptor);
                ServiceRegistered?.Invoke(descriptor);
                LogRegistration($"Decorator: {typeof(TDecorator).Name} decorating {typeof(TInterface).Name}");
            }
        }

        public void RegisterWithCallback<TInterface, TImplementation>(Action<TImplementation> initializer) where TImplementation : class, TInterface, new()
        {
            if (initializer == null)
                throw new ArgumentNullException(nameof(initializer));

            lock (_lock)
            {
                ThrowIfDisposed();
                var descriptor = new AdvancedServiceDescriptor
                {
                    ServiceType = typeof(TInterface),
                    ImplementationType = typeof(TImplementation),
                    Lifetime = ServiceLifetime.Transient,
                    Factory = _ =>
                    {
                        var instance = new TImplementation();
                        initializer(instance);
                        return instance;
                    }
                };
                AddServiceDescriptor(typeof(TInterface), descriptor);
                ServiceRegistered?.Invoke(descriptor);
                LogRegistration($"With Callback: {typeof(TInterface).Name} -> {typeof(TImplementation).Name}");
            }
        }

        public void RegisterOpenGeneric(Type serviceType, Type implementationType)
        {
            if (serviceType == null)
                throw new ArgumentNullException(nameof(serviceType));
            if (implementationType == null)
                throw new ArgumentNullException(nameof(implementationType));
            if (!serviceType.IsGenericTypeDefinition)
                throw new ArgumentException("Service type must be a generic type definition", nameof(serviceType));
            if (!implementationType.IsGenericTypeDefinition)
                throw new ArgumentException("Implementation type must be a generic type definition", nameof(implementationType));

            lock (_lock)
            {
                ThrowIfDisposed();
                var descriptor = new AdvancedServiceDescriptor
                {
                    ServiceType = serviceType,
                    ImplementationType = implementationType,
                    Lifetime = ServiceLifetime.Transient,
                    IsOpenGeneric = true,
                    Factory = locator => throw new InvalidOperationException("Open generic types must be closed before resolution")
                };
                AddServiceDescriptor(serviceType, descriptor);
                ServiceRegistered?.Invoke(descriptor);
                LogRegistration($"Open Generic: {serviceType.Name} -> {implementationType.Name}");
            }
        }

        public T Resolve<T>() where T : class
        {
            return (T)Resolve(typeof(T));
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

        public bool IsRegistered<T>() where T : class
        {
            return IsRegistered(typeof(T));
        }

        public bool IsRegistered(Type serviceType)
        {
            lock (_lock)
            {
                ThrowIfDisposed();
                return _services.ContainsKey(serviceType) && _services[serviceType].Count > 0;
            }
        }

        public IEnumerable<T> ResolveAll<T>() where T : class
        {
            lock (_lock)
            {
                ThrowIfDisposed();
                var serviceType = typeof(T);
                var results = new List<T>();
                if (_services.TryGetValue(serviceType, out var descriptors))
                {
                    foreach (var descriptor in descriptors.Where(d => !d.IsDecorator))
                    {
                        try
                        {
                            if (descriptor.Condition == null || descriptor.Condition(this))
                            {
                                var instance = CreateInstance(descriptor);
                                if (instance is T typedInstance)
                                {
                                    results.Add(typedInstance);
                                }
                            }
                        }
                        catch (Exception ex)
                        {
                            ResolutionFailed?.Invoke(serviceType, ex);
                            Debug.LogError($"[ServiceContainer] Error resolving {serviceType.Name}: {ex.Message}");
                        }
                    }
                }
                return results;
            }
        }

        public IServiceScope CreateScope()
        {
            lock (_lock)
            {
                ThrowIfDisposed();
                var scope = new ServiceContainerScope(this);
                _activeScopes.Add(scope);
                return scope;
            }
        }

        public void Clear()
        {
            lock (_lock)
            {
                if (_isDisposed) return;
                foreach (var scope in _activeScopes.ToList())
                {
                    try
                    {
                        scope.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[ServiceContainer] Error disposing scope: {ex.Message}");
                    }
                }
                foreach (var instance in _singletonInstances.Values.OfType<IDisposable>())
                {
                    try
                    {
                        instance.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[ServiceContainer] Error disposing singleton: {ex.Message}");
                    }
                }
                _services.Clear();
                _namedServices.Clear();
                _singletonInstances.Clear();
                _activeScopes.Clear();
                _resolutionCounts.Clear();
                _resolutionTimes.Clear();
                _totalResolutions = 0;
                _cacheHits = 0;
                Debug.Log("[ServiceContainer] All services cleared");
            }
        }

        public IDictionary<Type, ServiceRegistration> GetRegistrations()
        {
            lock (_lock)
            {
                var result = new Dictionary<Type, ServiceRegistration>();
                foreach (var kvp in _services)
                {
                    var descriptor = kvp.Value.FirstOrDefault();
                    if (descriptor != null)
                    {
                        // Convert factory signature from Func<IServiceLocator, object> to Func<object, object>
                        Func<object, object> convertedFactory = null;
                        if (descriptor.Factory != null)
                        {
                            convertedFactory = (object locator) => descriptor.Factory((IServiceLocator)locator);
                        }
                        result[kvp.Key] = new ServiceRegistration(descriptor.ServiceType, descriptor.ImplementationType, descriptor.Lifetime, descriptor.Instance, convertedFactory);
                    }
                }
                return result;
            }
        }

        public T ResolveNamed<T>(string name) where T : class
        {
            if (string.IsNullOrEmpty(name))
                throw new ArgumentException("Name cannot be null or empty", nameof(name));
            lock (_lock)
            {
                ThrowIfDisposed();
                var key = $"{typeof(T).FullName}:{name}";
                if (_namedServices.TryGetValue(key, out var descriptor))
                {
                    try
                    {
                        var instance = CreateInstance(descriptor);
                        ServiceResolved?.Invoke(typeof(T), instance);
                        return (T)instance;
                    }
                    catch (Exception ex)
                    {
                        ResolutionFailed?.Invoke(typeof(T), ex);
                        throw new InvalidOperationException($"Failed to resolve named service '{name}' of type {typeof(T).Name}", ex);
                    }
                }
                throw new InvalidOperationException($"Named service '{name}' of type {typeof(T).Name} is not registered");
            }
        }

        public IEnumerable<T> ResolveWhere<T>(Func<T, bool> predicate) where T : class
        {
            if (predicate == null)
                throw new ArgumentNullException(nameof(predicate));
            return ResolveAll<T>().Where(predicate);
        }

        public T ResolveOrCreate<T>(Func<T> factory) where T : class
        {
            if (factory == null)
                throw new ArgumentNullException(nameof(factory));
            try
            {
                return Resolve<T>();
            }
            catch
            {
                return factory();
            }
        }

        public T ResolveLast<T>() where T : class
        {
            lock (_lock)
            {
                ThrowIfDisposed();
                if (_services.TryGetValue(typeof(T), out var descriptors) && descriptors.Count > 0)
                {
                    var lastDescriptor = descriptors.LastOrDefault(d => !d.IsDecorator);
                    if (lastDescriptor != null)
                    {
                        var instance = CreateInstance(lastDescriptor);
                        ServiceResolved?.Invoke(typeof(T), instance);
                        return (T)instance;
                    }
                }
                throw new InvalidOperationException($"No registration found for type {typeof(T).Name}");
            }
        }

        public T ResolveWithLifetime<T>(ServiceLifetime lifetime) where T : class
        {
            Debug.Log($"[ServiceContainer] Resolving {typeof(T).Name} with requested lifetime: {lifetime}");
            return Resolve<T>();
        }

        public IServiceContainer CreateChildContainer()
        {
            lock (_lock)
            {
                ThrowIfDisposed();
                var childContainer = new ServiceContainer(this);
                _childContainers.Add(childContainer);
                return childContainer;
            }
        }

        public ContainerVerificationResult Verify()
        {
            var result = new ContainerVerificationResult();
            var startTime = DateTime.Now;
            try
            {
                lock (_lock)
                {
                    ThrowIfDisposed();
                    result.TotalServices = _services.Values.Sum(list => list.Count) + _namedServices.Count;
                    foreach (var serviceType in _services.Keys)
                    {
                        try
                        {
                            Resolve(serviceType);
                            result.VerifiedServices++;
                        }
                        catch (Exception ex)
                        {
                            result.Errors.Add($"Failed to resolve {serviceType.Name}: {ex.Message}");
                        }
                    }
                    foreach (var namedService in _namedServices.Values)
                    {
                        try
                        {
                            CreateInstance(namedService);
                            result.VerifiedServices++;
                        }
                        catch (Exception ex)
                        {
                            result.Errors.Add($"Failed to resolve named service '{namedService.Name}' of type {namedService.ServiceType.Name}: {ex.Message}");
                        }
                    }
                    result.IsValid = result.Errors.Count == 0;
                }
            }
            catch (Exception ex)
            {
                result.Errors.Add($"Container verification failed: {ex.Message}");
                result.IsValid = false;
            }
            result.VerificationTime = DateTime.Now - startTime;
            return result;
        }

        public IEnumerable<AdvancedServiceDescriptor> GetServiceDescriptors()
        {
            lock (_lock)
            {
                ThrowIfDisposed();
                return _services.Values.SelectMany(list => list).Concat(_namedServices.Values).ToList();
            }
        }

        public void Replace<TInterface, TImplementation>() where TImplementation : class, TInterface, new()
        {
            lock (_lock)
            {
                ThrowIfDisposed();
                var serviceType = typeof(TInterface);
                if (_services.ContainsKey(serviceType))
                {
                    _services[serviceType].Clear();
                }
                _singletonInstances.Remove(serviceType);
                RegisterTransient<TInterface, TImplementation>();
                LogRegistration($"Replaced: {typeof(TInterface).Name} -> {typeof(TImplementation).Name}");
            }
        }

        public bool Unregister<T>() where T : class
        {
            lock (_lock)
            {
                ThrowIfDisposed();
                var removed = false;
                var serviceType = typeof(T);
                if (_services.ContainsKey(serviceType))
                {
                    _services.Remove(serviceType);
                    removed = true;
                }
                if (_singletonInstances.ContainsKey(serviceType))
                {
                    var instance = _singletonInstances[serviceType];
                    if (instance is IDisposable disposable)
                    {
                        try
                        {
                            disposable.Dispose();
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"[ServiceContainer] Error disposing unregistered service: {ex.Message}");
                        }
                    }
                    _singletonInstances.Remove(serviceType);
                    removed = true;
                }
                if (removed)
                {
                    LogRegistration($"Unregistered: {typeof(T).Name}");
                }
                return removed;
            }
        }

        private void RegisterService<TInterface, TImplementation>(ServiceLifetime lifetime) where TImplementation : class, TInterface, new()
        {
            lock (_lock)
            {
                ThrowIfDisposed();
                var descriptor = new AdvancedServiceDescriptor
                {
                    ServiceType = typeof(TInterface),
                    ImplementationType = typeof(TImplementation),
                    Lifetime = lifetime,
                    Factory = _ => new TImplementation()
                };
                AddServiceDescriptor(typeof(TInterface), descriptor);
                ServiceRegistered?.Invoke(descriptor);
                LogRegistration($"{lifetime}: {typeof(TInterface).Name} -> {typeof(TImplementation).Name}");
            }
        }

        private void AddServiceDescriptor(Type serviceType, AdvancedServiceDescriptor descriptor)
        {
            if (!_services.ContainsKey(serviceType))
            {
                _services[serviceType] = new List<AdvancedServiceDescriptor>();
            }
            _services[serviceType].Add(descriptor);
        }

        public object Resolve(Type serviceType)
        {
            lock (_lock)
            {
                ThrowIfDisposed();
                _totalResolutions++;
                UpdateResolutionCount(serviceType);
                var startTime = DateTime.Now;
                try
                {
                    var instance = ResolveInternal(serviceType);
                    var resolutionTime = DateTime.Now - startTime;
                    UpdateResolutionTime(serviceType, resolutionTime);
                    ServiceResolved?.Invoke(serviceType, instance);
                    return instance;
                }
                catch (Exception ex)
                {
                    ResolutionFailed?.Invoke(serviceType, ex);
                    throw;
                }
            }
        }

        private object ResolveInternal(Type serviceType)
        {
            if (_parentContainer != null && _parentContainer.IsRegistered(serviceType))
            {
                return _parentContainer.GetType().GetMethod("Resolve", new[] { typeof(Type) })?.Invoke(_parentContainer, new object[] { serviceType });
            }
            if (!_services.TryGetValue(serviceType, out var descriptors) || descriptors.Count == 0)
            {
                throw new InvalidOperationException($"Service of type {serviceType.Name} is not registered");
            }
            var descriptor = descriptors.LastOrDefault(d => !d.IsDecorator);
            if (descriptor == null)
            {
                throw new InvalidOperationException($"No non-decorator registration found for type {serviceType.Name}");
            }
            if (descriptor.Condition != null && !descriptor.Condition(this))
            {
                throw new InvalidOperationException($"Condition not met for service {serviceType.Name}");
            }
            var instance = CreateInstance(descriptor);
            var decorators = descriptors.Where(d => d.IsDecorator).ToList();
            foreach (var decorator in decorators)
            {
                instance = decorator.Factory(this);
            }
            return instance;
        }

        private object CreateInstance(AdvancedServiceDescriptor descriptor)
        {
            switch (descriptor.Lifetime)
            {
                case ServiceLifetime.Singleton:
                    return GetOrCreateSingleton(descriptor);
                case ServiceLifetime.Transient:
                    return descriptor.Factory(this);
                case ServiceLifetime.Scoped:
                    return GetOrCreateSingleton(descriptor);
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private object GetOrCreateSingleton(AdvancedServiceDescriptor descriptor)
        {
            if (descriptor.Instance != null)
            {
                _cacheHits++;
                return descriptor.Instance;
            }
            if (_singletonInstances.TryGetValue(descriptor.ServiceType, out var existingInstance))
            {
                _cacheHits++;
                return existingInstance;
            }
            var newInstance = descriptor.Factory(this);
            _singletonInstances[descriptor.ServiceType] = newInstance;
            descriptor.Instance = newInstance;
            return newInstance;
        }

        private TInterface CreateDecorator<TInterface, TDecorator>(IServiceLocator locator) where TDecorator : class, TInterface where TInterface : class
        {
            var original = ResolveAll<TInterface>().FirstOrDefault();
            if (original == null)
            {
                throw new InvalidOperationException($"No original service found to decorate for {typeof(TInterface).Name}");
            }
            return Activator.CreateInstance(typeof(TDecorator), original) as TInterface;
        }

        private void RegisterSelf()
        {
            var descriptor = new AdvancedServiceDescriptor
            {
                ServiceType = typeof(IServiceContainer),
                ImplementationType = GetType(),
                Lifetime = ServiceLifetime.Singleton,
                Instance = this,
                Factory = _ => this
            };
            _services[typeof(IServiceContainer)] = new List<AdvancedServiceDescriptor> { descriptor };
            _services[typeof(IServiceLocator)] = new List<AdvancedServiceDescriptor> { descriptor };
            _singletonInstances[typeof(IServiceContainer)] = this;
            _singletonInstances[typeof(IServiceLocator)] = this;
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

        private void UpdateResolutionTime(Type serviceType, TimeSpan time)
        {
            if (_resolutionTimes.ContainsKey(serviceType))
            {
                _resolutionTimes[serviceType] = TimeSpan.FromTicks((_resolutionTimes[serviceType].Ticks + time.Ticks) / 2);
            }
            else
            {
                _resolutionTimes[serviceType] = time;
            }
        }

        private void ThrowIfDisposed()
        {
            if (_isDisposed)
            {
                throw new ObjectDisposedException(nameof(ServiceContainer));
            }
        }

        private static void LogRegistration(string message)
        {
            Debug.Log($"[ServiceContainer] Registered {message}");
        }

        internal void RemoveScope(IServiceScope scope)
        {
            lock (_lock)
            {
                _activeScopes.Remove(scope);
            }
        }

        public void Dispose()
        {
            if (_isDisposed) return;
            lock (_lock)
            {
                if (_isDisposed) return;
                try
                {
                    foreach (var child in _childContainers.ToList())
                    {
                        try
                        {
                            child.Dispose();
                        }
                        catch (Exception ex)
                        {
                            Debug.LogError($"[ServiceContainer] Error disposing child container: {ex.Message}");
                        }
                    }
                    Clear();
                    _isDisposed = true;
                    Debug.Log("[ServiceContainer] Container disposed");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ServiceContainer] Error during disposal: {ex.Message}");
                }
            }
        }
        
        public T GetService<T>() where T : class => Resolve<T>();
        public object GetService(Type serviceType) => Resolve(serviceType);

        public bool TryGetService<T>(out T service) where T : class
        {
            try
            {
                service = Resolve<T>();
                return true;
            }
            catch
            {
                service = default;
                return false;
            }
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
                service = default;
                return false;
            }
        }

        public bool ContainsService<T>() where T : class => IsRegistered<T>();
        public bool ContainsService(Type serviceType) => IsRegistered(serviceType);
        
        // IServiceProvider interface implementations
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
                var method = typeof(ServiceContainer).GetMethod(nameof(ResolveAll))?.MakeGenericMethod(serviceType);
                var result = method?.Invoke(this, Array.Empty<object>());
                return result as IEnumerable<object> ?? Array.Empty<object>();
            }
            catch
            {
                return Array.Empty<object>();
            }
        }
        
        public IEnumerable<T> GetServices<T>() where T : class => ResolveAll<T>();
    }

    internal class ServiceContainerScope : IServiceScope
    {
        private readonly ServiceContainer _container;
        private readonly Dictionary<Type, object> _scopedInstances = new Dictionary<Type, object>();
        private bool _disposed = false;
        public ProjectChimera.Core.DependencyInjection.IServiceProvider ServiceProvider => new ServiceProviderAdapter(_container, new ServiceProviderOptions());

        internal ServiceContainerScope(ServiceContainer container)
        {
            _container = container;
        }

        public void Dispose()
        {
            if (_disposed) return;
            try
            {
                foreach (var instance in _scopedInstances.Values.OfType<IDisposable>())
                {
                    try
                    {
                        instance.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[ServiceContainerScope] Error disposing scoped service: {ex.Message}");
                    }
                }
                _scopedInstances.Clear();
                _container.RemoveScope(this);
                _disposed = true;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ServiceContainerScope] Error during disposal: {ex.Message}");
            }
        }
    }
}
