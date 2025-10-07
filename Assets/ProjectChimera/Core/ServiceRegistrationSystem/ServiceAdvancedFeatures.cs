using System;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;


namespace ProjectChimera.Core.ServiceRegistrationSystem
{
    /// <summary>
    /// FOCUSED: Advanced service registration features for Project Chimera ServiceContainer
    /// Single responsibility: Handle factory, conditional, decorator, and collection registration
    /// Extracted from ServiceContainer.cs for SRP compliance (Week 8)
    /// </summary>
    public class ServiceAdvancedFeatures
    {
        private readonly Dictionary<Type, object> _services;
        private readonly object _lock;
        private readonly IServiceLocator _serviceLocator;

        // Events
        public event Action<ProjectChimera.Core.ServiceRegistrationData> ServiceRegistered;

        public ServiceAdvancedFeatures(Dictionary<Type, object> services, object lockObject, IServiceLocator serviceLocator)
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _lock = lockObject ?? throw new ArgumentNullException(nameof(lockObject));
            _serviceLocator = serviceLocator ?? throw new ArgumentNullException(nameof(serviceLocator));
        }

        /// <summary>
        /// Register factory for creating services
        /// </summary>
        public void RegisterFactory<TInterface>(Func<IServiceLocator, TInterface> factory) where TInterface : class
        {
            if (factory == null) throw new ArgumentNullException(nameof(factory));

            lock (_lock)
            {
                var serviceType = typeof(TInterface);
                var instance = factory(_serviceLocator);
                _services[serviceType] = instance;

                ServiceRegistered?.Invoke(new ProjectChimera.Core.ServiceRegistrationData(
                    serviceType,
                    instance?.GetType(),
                    ProjectChimera.Core.ServiceLifetime.Singleton,
                    instance,
                    null
                ));

                ChimeraLogger.LogInfo("ServiceAdvancedFeatures", "$1");
            }
        }

        /// <summary>
        /// Register service with condition
        /// </summary>
        public void RegisterConditional<TInterface, TImplementation>(Func<IServiceLocator, bool> condition)
            where TInterface : class
            where TImplementation : class, TInterface, new()
        {
            if (condition == null) throw new ArgumentNullException(nameof(condition));

            if (condition(_serviceLocator))
            {
                lock (_lock)
                {
                    var serviceType = typeof(TInterface);
                    var implementationType = typeof(TImplementation);
                    var instance = new TImplementation();
                    _services[serviceType] = instance;

                    ServiceRegistered?.Invoke(new ProjectChimera.Core.ServiceRegistrationData(
                        serviceType,
                        implementationType,
                        ProjectChimera.Core.ServiceLifetime.Singleton,
                        instance,
                        null
                    ));

                    ChimeraLogger.LogInfo("ServiceAdvancedFeatures", "$1");
                }
            }
        }

        /// <summary>
        /// Register decorator for existing service
        /// </summary>
        public void RegisterDecorator<TInterface, TDecorator>()
            where TInterface : class
            where TDecorator : class, TInterface, new()
        {
            lock (_lock)
            {
                var serviceType = typeof(TInterface);
                var decoratorType = typeof(TDecorator);
                var decorator = new TDecorator();
                _services[serviceType] = decorator;

                ServiceRegistered?.Invoke(new ProjectChimera.Core.ServiceRegistrationData(
                    serviceType,
                    decoratorType,
                    ProjectChimera.Core.ServiceLifetime.Singleton,
                    decorator,
                    null
                ));

                ChimeraLogger.LogInfo("ServiceAdvancedFeatures", "$1");
            }
        }

        /// <summary>
        /// Register service with callback
        /// </summary>
        public void RegisterWithCallback<TInterface, TImplementation>(Action<TImplementation> callback)
            where TInterface : class
            where TImplementation : class, TInterface, new()
        {
            if (callback == null) throw new ArgumentNullException(nameof(callback));

            lock (_lock)
            {
                var serviceType = typeof(TInterface);
                var implementationType = typeof(TImplementation);
                var instance = new TImplementation();
                callback(instance);
                _services[serviceType] = instance;

                ServiceRegistered?.Invoke(new ProjectChimera.Core.ServiceRegistrationData(
                    serviceType,
                    implementationType,
                    ProjectChimera.Core.ServiceLifetime.Singleton,
                    instance,
                    null
                ));

                ChimeraLogger.LogInfo("ServiceAdvancedFeatures", "$1");
            }
        }

        /// <summary>
        /// Register open generic types (simplified implementation)
        /// </summary>
        public void RegisterOpenGeneric(Type serviceType, Type implementationType)
        {
            if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));
            if (implementationType == null) throw new ArgumentNullException(nameof(implementationType));

            ChimeraLogger.LogInfo("ServiceAdvancedFeatures", "$1");
        }

        /// <summary>
        /// Register collection of services
        /// </summary>
        public void RegisterCollection<TInterface>(params Type[] implementationTypes) where TInterface : class
        {
            if (implementationTypes == null) throw new ArgumentNullException(nameof(implementationTypes));

            lock (_lock)
            {
                foreach (var implType in implementationTypes)
                {
                    if (typeof(TInterface).IsAssignableFrom(implType))
                    {
                        var instance = Activator.CreateInstance(implType);
                        if (instance is TInterface typedInstance)
                        {
                            var serviceType = typeof(TInterface);
                            _services[implType] = typedInstance;

                            ServiceRegistered?.Invoke(new ProjectChimera.Core.ServiceRegistrationData(
                                serviceType,
                                implType,
                                ProjectChimera.Core.ServiceLifetime.Singleton,
                                typedInstance,
                                null
                            ));

                            ChimeraLogger.LogInfo("ServiceAdvancedFeatures", "$1");
                        }
                    }
                }
            }
        }

        /// <summary>
        /// Register named service (simplified - name not used in basic implementation)
        /// </summary>
        public void RegisterNamed<TInterface, TImplementation>(string name)
            where TInterface : class
            where TImplementation : class, TInterface, new()
        {
            if (string.IsNullOrEmpty(name)) throw new ArgumentException("Name cannot be null or empty", nameof(name));

            lock (_lock)
            {
                var serviceType = typeof(TInterface);
                var implementationType = typeof(TImplementation);
                var instance = new TImplementation();
                _services[serviceType] = instance;

                ServiceRegistered?.Invoke(new ProjectChimera.Core.ServiceRegistrationData(
                    serviceType,
                    implementationType,
                    ProjectChimera.Core.ServiceLifetime.Singleton,
                    instance,
                    null
                ));

                ChimeraLogger.LogInfo("ServiceAdvancedFeatures", "$1");
            }
        }

        /// <summary>
        /// Replace existing service registration
        /// </summary>
        public void Replace<TInterface, TImplementation>()
            where TInterface : class
            where TImplementation : class, TInterface, new()
        {
            lock (_lock)
            {
                var serviceType = typeof(TInterface);
                var implementationType = typeof(TImplementation);
                var instance = new TImplementation();
                _services[serviceType] = instance;

                ServiceRegistered?.Invoke(new ProjectChimera.Core.ServiceRegistrationData(
                    serviceType,
                    implementationType,
                    ProjectChimera.Core.ServiceLifetime.Singleton,
                    instance,
                    null
                ));

                ChimeraLogger.LogInfo("ServiceAdvancedFeatures", "$1");
            }
        }

        /// <summary>
        /// Unregister service
        /// </summary>
        public bool Unregister<T>() where T : class
        {
            lock (_lock)
            {
                var removed = _services.Remove(typeof(T));
                if (removed)
                {
                    ChimeraLogger.LogInfo("ServiceAdvancedFeatures", "$1");
                }
                return removed;
            }
        }

        /// <summary>
        /// Register singleton by type and instance
        /// </summary>
        public void RegisterSingleton(Type serviceType, object instance)
        {
            if (serviceType == null) throw new ArgumentNullException(nameof(serviceType));
            if (instance == null) throw new ArgumentNullException(nameof(instance));

            lock (_lock)
            {
                _services[serviceType] = instance;

                ServiceRegistered?.Invoke(new ProjectChimera.Core.ServiceRegistrationData(
                    serviceType,
                    instance.GetType(),
                    ProjectChimera.Core.ServiceLifetime.Singleton,
                    instance,
                    null
                ));

                ChimeraLogger.LogInfo("ServiceAdvancedFeatures", "$1");
            }
        }
    }
}
