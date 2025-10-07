using ProjectChimera.Core.Logging;
using Logger = ProjectChimera.Core.Logging.ChimeraLogger;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProjectChimera.Core
{
    public class ServiceCollection : IServiceCollection
    {
        private readonly List<ServiceDescriptor> _services = new List<ServiceDescriptor>();
        private readonly IServiceContainer _container;

        public ServiceCollection() : this(null) { }

        public ServiceCollection(IServiceContainer container)
        {
            _container = container;
        }

        public ServiceDescriptor this[int index]
        {
            get => _services[index];
            set => _services[index] = value;
        }

        public int Count => _services.Count;
        public bool IsReadOnly => false;

        public void Add(ServiceDescriptor item)
        {
            if (item == null)
                throw new ArgumentNullException(nameof(item));
            _services.Add(item);
        }

        public void Clear() => _services.Clear();
        public bool Contains(ServiceDescriptor item) => _services.Contains(item);
        public void CopyTo(ServiceDescriptor[] array, int arrayIndex) => _services.CopyTo(array, arrayIndex);
        public IEnumerator<ServiceDescriptor> GetEnumerator() => _services.GetEnumerator();
        public int IndexOf(ServiceDescriptor item) => _services.IndexOf(item);
        public void Insert(int index, ServiceDescriptor item) => _services.Insert(index, item);
        public bool Remove(ServiceDescriptor item) => _services.Remove(item);
        public void RemoveAt(int index) => _services.RemoveAt(index);
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        public IServiceCollection Add(params ServiceDescriptor[] items)
        {
            foreach (var item in items) Add(item);
            return this;
        }

        public IServiceCollection AddSingleton<TService>() where TService : class => AddSingleton<TService, TService>();

        public IServiceCollection AddSingleton<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService
        {
            Add(ServiceDescriptor.Singleton<TService, TImplementation>());
            return this;
        }

        public IServiceCollection AddSingleton<TService>(TService instance) where TService : class
        {
            Add(ServiceDescriptor.Singleton(instance));
            return this;
        }

        public IServiceCollection AddSingleton<TService>(Func<IServiceProvider, TService> factory) where TService : class
        {
            Add(ServiceDescriptor.Singleton(factory));
            return this;
        }

        public IServiceCollection AddTransient<TService>() where TService : class => AddTransient<TService, TService>();

        public IServiceCollection AddTransient<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService
        {
            Add(ServiceDescriptor.Transient<TService, TImplementation>());
            return this;
        }

        public IServiceCollection AddTransient<TService>(Func<IServiceProvider, TService> factory) where TService : class
        {
            Add(ServiceDescriptor.Transient(factory));
            return this;
        }

        public IServiceCollection AddScoped<TService>() where TService : class => AddScoped<TService, TService>();

        public IServiceCollection AddScoped<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService
        {
            Add(ServiceDescriptor.Scoped<TService, TImplementation>());
            return this;
        }

        public IServiceCollection AddScoped<TService>(Func<IServiceProvider, TService> factory) where TService : class
        {
            Add(ServiceDescriptor.Scoped(factory));
            return this;
        }

        public IServiceCollection Replace<TService, TImplementation>()
            where TService : class
            where TImplementation : class, TService
        {
            var toRemove = _services.Where(s => s.ServiceType == typeof(TService)).ToList();
            foreach (var service in toRemove) _services.Remove(service);
            AddTransient<TService, TImplementation>();
            return this;
        }

        public bool Remove<TService>() where TService : class
        {
            var toRemove = _services.Where(s => s.ServiceType == typeof(TService)).ToList();
            var removed = false;
            foreach (var service in toRemove)
            {
                if (_services.Remove(service)) removed = true;
            }
            return removed;
        }

        public IServiceProvider BuildServiceProvider() => BuildServiceProvider(new ServiceProviderOptions());

        public IServiceProvider BuildServiceProvider(ServiceProviderOptions options)
        {
            options ??= new ServiceProviderOptions();
            var container = _container ?? (IServiceContainer)new ServiceContainer();
            foreach (var descriptor in _services)
            {
                RegisterWithContainer(container, descriptor);
            }
            var serviceProvider = new ServiceProviderAdapter(container, options);
            if (options.ValidateOnBuild)
            {
                var result = container.Verify();
                if (!result.IsValid)
                {
                    var errorMessage = $"Service provider validation failed with {result.Errors.Count} errors:\n{string.Join("\n", result.Errors)}";
                    throw new InvalidOperationException(errorMessage);
                }
            }
            return serviceProvider;
        }

        private void RegisterWithContainer(IServiceContainer container, ServiceDescriptor descriptor)
        {
            try
            {
                if (descriptor.ImplementationInstance != null)
                {
                    var method = typeof(IServiceContainer).GetMethod(nameof(IServiceContainer.RegisterSingleton), new[] { descriptor.ServiceType });
                    method?.Invoke(container, new[] { descriptor.ImplementationInstance });
                }
                else if (descriptor.ImplementationFactory != null)
                {
                    var serviceProviderAdapter = new ServiceProviderAdapter(container, new ServiceProviderOptions());
                    Func<IServiceLocator, object> locatorFactory = locator => descriptor.ImplementationFactory(serviceProviderAdapter);

                    var method = typeof(IServiceContainer).GetMethod(nameof(IServiceContainer.RegisterFactory))?.MakeGenericMethod(descriptor.ServiceType);
                    method?.Invoke(container, new object[] { locatorFactory });
                }
                else if (descriptor.ImplementationType != null)
                {
                    switch (descriptor.Lifetime)
                    {
                        case ServiceLifetime.Singleton:
                            RegisterByLifetime(container, "RegisterSingleton", descriptor.ServiceType, descriptor.ImplementationType);
                            break;
                        case ServiceLifetime.Transient:
                            RegisterByLifetime(container, "RegisterTransient", descriptor.ServiceType, descriptor.ImplementationType);
                            break;
                        case ServiceLifetime.Scoped:
                            RegisterByLifetime(container, "RegisterScoped", descriptor.ServiceType, descriptor.ImplementationType);
                            break;
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogInfo("ServiceCollection", "$1");
                throw;
            }
        }

        private void RegisterByLifetime(IServiceContainer container, string methodName, Type serviceType, Type implementationType)
        {
            var method = typeof(IServiceContainer).GetMethod(methodName, Type.EmptyTypes)?.MakeGenericMethod(serviceType, implementationType);
            method?.Invoke(container, Array.Empty<object>());
        }
    }

    public class ServiceProviderAdapter : IServiceProvider, IServiceScopeFactory, IDisposable
    {
        private readonly IServiceContainer _container;
        private readonly ServiceProviderOptions _options;
        private readonly List<ProjectChimera.Core.IServiceScope> _activeScopes = new List<ProjectChimera.Core.IServiceScope>();
        private bool _disposed = false;

        /// <summary>
        /// Get the underlying container (replaces reflection-based access)
        /// </summary>
        public IServiceContainer UnderlyingContainer => _container;

        public ServiceProviderAdapter(IServiceContainer container, ServiceProviderOptions options)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
            _options = options ?? new ServiceProviderOptions();
        }

        public object GetService(Type serviceType)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(ServiceProviderAdapter));
            try
            {
                if (_options.EnableLogging) Logger.LogInfo("ServiceCollection", "$1");
                return _container.TryResolve<object>();
            }
            catch (Exception ex)
            {
                if (_options.EnableLogging) Logger.LogInfo("ServiceCollection", "$1");
                return null;
            }
        }

        public T GetService<T>() where T : class
        {
            var service = GetService(typeof(T));
            return service != null ? (T)service : default;
        }

        public object GetRequiredService(Type serviceType)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(ServiceProviderAdapter));
            try
            {
                if (_options.EnableLogging) Logger.LogInfo("ServiceCollection", "$1");
                return _container.Resolve(serviceType);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException($"Required service of type {serviceType.Name} could not be resolved", ex);
            }
        }

        public T GetRequiredService<T>() where T : class => (T)GetRequiredService(typeof(T));

        public IEnumerable<object> GetServices(Type serviceType)
        {
            if (_disposed) throw new ObjectDisposedException(nameof(ServiceProviderAdapter));
            try
            {
                var method = typeof(IServiceContainer).GetMethod(nameof(IServiceContainer.ResolveAll))?.MakeGenericMethod(serviceType);
                var result = method?.Invoke(_container, Array.Empty<object>());
                return result as IEnumerable<object> ?? Array.Empty<object>();
            }
            catch (Exception ex)
            {
                if (_options.EnableLogging) Logger.LogInfo("ServiceCollection", "$1");
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

        public ProjectChimera.Core.IServiceScope CreateScope()
        {
            if (_disposed) throw new ObjectDisposedException(nameof(ServiceProviderAdapter));
            var containerScope = new ServiceProviderAdapter(_container, _options).CreateScope();
            var scope = new ServiceScopeAdapter(containerScope, this);
            lock (_activeScopes)
            {
                _activeScopes.Add(scope);
            }
            return scope;
        }

        internal void RemoveScope(ProjectChimera.Core.IServiceScope scope)
        {
            lock (_activeScopes)
            {
                _activeScopes.Remove(scope);
            }
        }

        public void Dispose()
        {
            if (_disposed) return;
            try
            {
                lock (_activeScopes)
                {
                    foreach (var scope in _activeScopes.ToList())
                    {
                        try
                        {
                            scope.Dispose();
                        }
                        catch (Exception ex)
                        {
                            Logger.LogInfo("ServiceCollection", "$1");
                        }
                    }
                    _activeScopes.Clear();
                }
                if (_container is IDisposable disposableContainer) disposableContainer.Dispose();
                _disposed = true;
            }
            catch (Exception ex)
            {
                Logger.LogInfo("ServiceCollection", "$1");
            }
        }
    }

    public class ServiceScopeAdapter : ProjectChimera.Core.IServiceScope
    {
        private readonly ProjectChimera.Core.IServiceScope _containerScope;
        private readonly ServiceProviderAdapter _parentProvider;
        private bool _disposed = false;

        public ServiceScopeAdapter(ProjectChimera.Core.IServiceScope containerScope, ServiceProviderAdapter parentProvider)
        {
            _containerScope = containerScope ?? throw new ArgumentNullException(nameof(containerScope));
            _parentProvider = parentProvider ?? throw new ArgumentNullException(nameof(parentProvider));
        }

        public IServiceProvider ServiceProvider => new ServiceProviderAdapter(_containerScope.ServiceProvider as IServiceContainer, new ServiceProviderOptions());

        public void Dispose()
        {
            if (_disposed) return;
            try
            {
                _containerScope?.Dispose();
                _parentProvider?.RemoveScope(this);
                _disposed = true;
            }
            catch (Exception ex)
            {
                Logger.LogInfo("ServiceCollection", "$1");
            }
        }
    }
}
