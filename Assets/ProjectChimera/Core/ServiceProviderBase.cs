using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjectChimera.Core
{
    /// <summary>
    /// Base class for service providers
    /// Provides common functionality for service registration and resolution
    /// </summary>
    public abstract class ServiceProviderBase : IServiceProvider, IDisposable
    {
        protected readonly Dictionary<Type, ServiceRegistration> _registrations = new();
        protected readonly Dictionary<Type, object> _instances = new();
        protected bool _isDisposed;

        /// <summary>
        /// Register a service instance
        /// </summary>
        public virtual void RegisterInstance<TInterface>(TInterface instance) where TInterface : class
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(ServiceProviderBase));
            if (instance == null) throw new ArgumentNullException(nameof(instance));

            var serviceType = typeof(TInterface);
            _instances[serviceType] = instance;

            var registration = new ServiceRegistration(
                serviceType,
                instance.GetType(),
                ServiceLifetime.Singleton,
                instance,
                null
            );

            _registrations[serviceType] = registration;
        }

        /// <summary>
        /// Register a service factory
        /// </summary>
        public virtual void RegisterFactory<TInterface>(Func<IServiceProvider, TInterface> factory) where TInterface : class
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(ServiceProviderBase));
            if (factory == null) throw new ArgumentNullException(nameof(factory));

            var serviceType = typeof(TInterface);

            var registration = new ServiceRegistration(
                serviceType,
                typeof(TInterface),
                ServiceLifetime.Transient,
                null,
                factory
            );

            _registrations[serviceType] = registration;
        }

        /// <summary>
        /// Get service instance
        /// </summary>
        public virtual object GetService(Type serviceType)
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(ServiceProviderBase));
            if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));

            // Try to get instance first
            if (_instances.TryGetValue(serviceType, out var instance))
            {
                return instance;
            }

            // Try to get registration and create instance
            if (_registrations.TryGetValue(serviceType, out var registration))
            {
                if (registration.Factory != null)
                {
                    instance = registration.Factory(this);
                    if (registration.Lifetime == ServiceLifetime.Singleton)
                    {
                        _instances[serviceType] = instance;
                    }
                    return instance;
                }
            }

            return null;
        }

        /// <summary>
        /// Check if service is registered
        /// </summary>
        public virtual bool IsServiceRegistered(Type serviceType)
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(ServiceProviderBase));
            return _instances.ContainsKey(serviceType) || _registrations.ContainsKey(serviceType);
        }

        /// <summary>
        /// Get all registered service types
        /// </summary>
        public virtual IEnumerable<Type> GetRegisteredServiceTypes()
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(ServiceProviderBase));
            return _registrations.Keys.Union(_instances.Keys);
        }

        /// <summary>
        /// Clear all registrations and instances
        /// </summary>
        public virtual void Clear()
        {
            if (_isDisposed) throw new ObjectDisposedException(nameof(ServiceProviderBase));

            // Dispose of disposable instances
            foreach (var instance in _instances.Values)
            {
                if (instance is IDisposable disposable)
                {
                    disposable.Dispose();
                }
            }

            _instances.Clear();
            _registrations.Clear();
        }

        /// <summary>
        /// Dispose of the service provider
        /// </summary>
        public virtual void Dispose()
        {
            if (!_isDisposed)
            {
                Clear();
                _isDisposed = true;
            }
        }

        /// <summary>
        /// Register manager interfaces (virtual for subclasses to override)
        /// </summary>
        public virtual void RegisterManagerInterfaces(IServiceContainer serviceContainer)
        {
            // Basic implementation does nothing
        }

        /// <summary>
        /// Register utility services (virtual for subclasses to override)
        /// </summary>
        public virtual void RegisterUtilityServices(IServiceContainer serviceContainer)
        {
            // Basic implementation does nothing
        }

        /// <summary>
        /// Register data services (virtual for subclasses to override)
        /// </summary>
        public virtual void RegisterDataServices(IServiceContainer serviceContainer)
        {
            // Basic implementation does nothing
        }

        /// <summary>
        /// Register null implementations (virtual for subclasses to override)
        /// </summary>
        public virtual void RegisterNullImplementations(IServiceContainer serviceContainer)
        {
            // Basic implementation does nothing
        }

        /// <summary>
        /// Initialize services (virtual for subclasses to override)
        /// </summary>
        public virtual void InitializeServices(IServiceContainer serviceContainer)
        {
            // Basic implementation does nothing
        }

        /// <summary>
        /// Validate services (virtual for subclasses to override)
        /// </summary>
        public virtual void ValidateServices(IServiceContainer serviceContainer)
        {
            // Basic implementation does nothing
        }

        /// <summary>
        /// Service registration information
        /// </summary>
        protected class ServiceRegistration
        {
            public Type ServiceType { get; }
            public Type ImplementationType { get; }
            public ServiceLifetime Lifetime { get; }
            public object Instance { get; }
            public Func<IServiceProvider, object> Factory { get; }

            public ServiceRegistration(
                Type serviceType,
                Type implementationType,
                ServiceLifetime lifetime,
                object instance,
                Func<IServiceProvider, object> factory)
            {
                ServiceType = serviceType;
                ImplementationType = implementationType;
                Lifetime = lifetime;
                Instance = instance;
                Factory = factory;
            }
        }
    }

    /// <summary>
    /// Service lifetime options
    /// </summary>
    public enum ServiceLifetime
    {
        Transient,
        Singleton,
        Scoped
    }
}
