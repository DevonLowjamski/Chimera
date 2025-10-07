using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core.SimpleDI
{
    /// <summary>
    /// Simple dependency injection container for Project Chimera
    /// Focused on core cultivation game systems
    /// </summary>
    public class SimpleDIContainer : MonoBehaviour
    {
        [Header("DI Configuration")]
        [SerializeField] private bool _enableLogging = true;

        // Service storage - only what we need for cultivation
        private readonly Dictionary<Type, object> _services = new Dictionary<Type, object>();
        private readonly Dictionary<Type, Func<object>> _factories = new Dictionary<Type, Func<object>>();

        // Core game system registrations
        private void Awake()
        {
            RegisterCoreServices();
        }

        /// <summary>
        /// Register core services needed for cultivation game
        /// </summary>
        private void RegisterCoreServices()
        {
            // Register null/default implementations for optional services
            // These can be replaced with real implementations as needed

            if (_enableLogging)
                ChimeraLogger.LogInfo("SimpleDIContainer", "$1");
        }

        #region Service Registration

        /// <summary>
        /// Register a singleton service instance
        /// </summary>
        public void RegisterSingleton<TInterface, TImplementation>(TImplementation instance)
            where TImplementation : TInterface
        {
            _services[typeof(TInterface)] = instance;

            if (_enableLogging)
                ChimeraLogger.LogInfo("SimpleDIContainer", "$1");
        }

        /// <summary>
        /// Register a service factory
        /// </summary>
        public void RegisterFactory<TInterface>(Func<TInterface> factory)
        {
            _factories[typeof(TInterface)] = () => factory();

            if (_enableLogging)
                ChimeraLogger.LogInfo("SimpleDIContainer", "$1");
        }

        /// <summary>
        /// Register a service by type (assumes TInterface and TImplementation are the same)
        /// </summary>
        public void Register<TService>(TService instance)
        {
            _services[typeof(TService)] = instance;

            if (_enableLogging)
                ChimeraLogger.LogInfo("SimpleDIContainer", "$1");
        }

        #endregion

        #region Service Resolution

        /// <summary>
        /// Resolve a service by type
        /// </summary>
        public TService Resolve<TService>()
        {
            Type serviceType = typeof(TService);

            // Try to get existing instance
            if (_services.TryGetValue(serviceType, out object instance))
            {
                return (TService)instance;
            }

            // Try to create from factory
            if (_factories.TryGetValue(serviceType, out Func<object> factory))
            {
                instance = factory();
                _services[serviceType] = instance; // Cache it
                return (TService)instance;
            }

            if (_enableLogging)
                ChimeraLogger.LogInfo("SimpleDIContainer", "$1");

            return default;
        }

        /// <summary>
        /// Try to resolve a service (returns false if not found)
        /// </summary>
        public bool TryResolve<TService>(out TService service)
        {
            Type serviceType = typeof(TService);

            if (_services.TryGetValue(serviceType, out object instance))
            {
                service = (TService)instance;
                return true;
            }

            if (_factories.TryGetValue(serviceType, out Func<object> factory))
            {
                instance = factory();
                _services[serviceType] = instance;
                service = (TService)instance;
                return true;
            }

            service = default;
            return false;
        }

        /// <summary>
        /// Check if a service is registered
        /// </summary>
        public bool IsRegistered<TService>()
        {
            Type serviceType = typeof(TService);
            return _services.ContainsKey(serviceType) || _factories.ContainsKey(serviceType);
        }

        #endregion

        #region Service Management

        /// <summary>
        /// Unregister a service
        /// </summary>
        public void Unregister<TService>()
        {
            Type serviceType = typeof(TService);

            if (_services.Remove(serviceType))
            {
                if (_enableLogging)
                    ChimeraLogger.LogInfo("SimpleDIContainer", "$1");
            }

            _factories.Remove(serviceType);
        }

        /// <summary>
        /// Clear all services
        /// </summary>
        public void Clear()
        {
            _services.Clear();
            _factories.Clear();

            if (_enableLogging)
                ChimeraLogger.LogInfo("SimpleDIContainer", "$1");
        }

        /// <summary>
        /// Get count of registered services
        /// </summary>
        public int ServiceCount => _services.Count + _factories.Count;

        #endregion

        #region Utility Methods

        /// <summary>
        /// Inject dependencies into an object (basic property injection)
        /// </summary>
        public void InjectDependencies(object target)
        {
            if (target == null) return;

            var targetType = target.GetType();
            var properties = targetType.GetProperties();

            foreach (var property in properties)
            {
                if (property.CanWrite && _services.TryGetValue(property.PropertyType, out object service))
                {
                    property.SetValue(target, service);

                    if (_enableLogging)
                        ChimeraLogger.LogInfo("SimpleDIContainer", "$1");
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// Simple service locator for global access
    /// </summary>
    public static class ServiceLocator
    {
        private static SimpleDIContainer _container;

        /// <summary>
        /// Initialize the service locator
        /// </summary>
        public static void Initialize(SimpleDIContainer container)
        {
            _container = container;
        }

        /// <summary>
        /// Get a service
        /// </summary>
        public static T Get<T>()
        {
            if (_container == null)
            {
                ChimeraLogger.LogInfo("SimpleDIContainer", "$1");
                return default;
            }

            return _container.Resolve<T>();
        }

        /// <summary>
        /// Try to get a service
        /// </summary>
        public static bool TryGet<T>(out T service)
        {
            if (_container == null)
            {
                ChimeraLogger.LogInfo("SimpleDIContainer", "$1");
                service = default;
                return false;
            }

            return _container.TryResolve(out service);
        }

        /// <summary>
        /// Register a service
        /// </summary>
        public static void Register<T>(T service)
        {
            if (_container != null)
            {
                _container.Register(service);
            }
        }
    }
}
