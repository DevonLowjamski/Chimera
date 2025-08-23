using System;
using System.Linq;
using UnityEngine;
using ProjectChimera.Core.DependencyInjection;

namespace ProjectChimera.Core
{
    /// <summary>
    /// Dependency injection-enabled base class for Project Chimera managers
    /// Extends ChimeraManager with service container integration and dependency resolution
    /// </summary>
    public abstract class DIChimeraManager : ChimeraManager, IDisposable
    {
        [Header("Dependency Injection")]
        [SerializeField] protected bool _autoRegisterWithContainer = true;
        [SerializeField] private ServiceLifetime _serviceLifetime = ServiceLifetime.Singleton;

        protected ProjectChimera.Core.IServiceContainer ServiceContainer { get; private set; }
        protected global::System.IServiceProvider ServiceProvider { get; private set; }

        private bool _isDisposed = false;

        #region Unity Lifecycle

        protected override void Awake()
        {
            base.Awake();
            InitializeDependencyInjection();
        }

        protected override void OnDestroy()
        {
            if (!_isDisposed)
            {
                Dispose();
            }
            base.OnDestroy();
        }

        #endregion

        #region Dependency Injection

        /// <summary>
        /// Initialize dependency injection for this manager using standardized ServiceContainer approach
        /// </summary>
        protected virtual void InitializeDependencyInjection()
        {
            try
            {
                // Use ServiceContainer as the sole standardized DI approach
                ServiceContainer = GetOrCreateServiceContainer();
                ServiceProvider = (global::System.IServiceProvider)ProjectChimera.Core.DependencyInjection.ServiceContainerIntegration.ToServiceProvider(ServiceContainer);
                
                Debug.Log($"[DIChimeraManager] Using ServiceContainer as sole DI provider for {ManagerName}");

                // Auto-register this manager with ServiceContainer
                if (_autoRegisterWithContainer)
                {
                    RegisterSelfWithContainer();
                }

                // Resolve dependencies
                ResolveDependencies();

                Debug.Log($"[DIChimeraManager] Dependency injection initialized for {ManagerName} via ServiceContainer");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DIChimeraManager] Failed to initialize DI for {ManagerName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Get or create the global service container
        /// </summary>
        protected virtual ProjectChimera.Core.IServiceContainer GetOrCreateServiceContainer()
        {
            // Use ServiceContainerFactory as the sole standardized DI approach per Phase 0 goals
            return ServiceContainerFactory.Instance;
        }

        /// <summary>
        /// Set the service container for this manager (protected access for derived classes)
        /// </summary>
        protected void SetServiceContainer(ProjectChimera.Core.IServiceContainer container)
        {
            ServiceContainer = container;
        }

        /// <summary>
        /// Register this manager with the service container
        /// </summary>
        protected virtual void RegisterSelfWithContainer()
        {
            var managerType = GetType();
            var interfaceTypes = managerType.GetInterfaces();

            // Register by concrete type - use RegisterSingleton without type parameter
            switch (_serviceLifetime)
            {
                case ServiceLifetime.Singleton:
                    ServiceContainer.RegisterSingleton(this);
                    break;
                case ServiceLifetime.Transient:
                    Debug.LogWarning($"[DIChimeraManager] Transient lifetime not recommended for managers: {ManagerName}");
                    break;
                case ServiceLifetime.Scoped:
                    Debug.LogWarning($"[DIChimeraManager] Scoped lifetime not recommended for managers: {ManagerName}");
                    break;
            }

            // Register by interfaces - skip automatic interface registration for now
            // TODO: Implement reflection-based interface registration if needed
            Debug.Log($"[DIChimeraManager] Registered {ManagerName} as concrete type");
        }


        /// <summary>
        /// Override this method to resolve dependencies using the service container
        /// </summary>
        protected virtual void ResolveDependencies()
        {
            // Override in derived classes to resolve specific dependencies
            // Example:
            // _timeManager = ServiceContainer.Resolve<ITimeManager>();
            // _dataManager = ServiceContainer.Resolve<IDataManager>();
        }

        #endregion

        #region Service Resolution Helpers

        /// <summary>
        /// Resolve a required service
        /// </summary>
        protected T ResolveService<T>() where T : class
        {
            try
            {
                return (T)ServiceContainer.Resolve(typeof(T));
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DIChimeraManager] Failed to resolve required service {typeof(T).Name} in {ManagerName}: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// Try to resolve an optional service
        /// </summary>
        protected T TryResolveService<T>() where T : class
        {
            try
            {
                return ServiceContainer.TryResolve<T>();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[DIChimeraManager] Failed to resolve optional service {typeof(T).Name} in {ManagerName}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Resolve all implementations of a service
        /// </summary>
        protected System.Collections.Generic.IEnumerable<T> ResolveAllServices<T>() where T : class
        {
            try
            {
                return ServiceContainer.ResolveAll<T>();
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[DIChimeraManager] Failed to resolve all services {typeof(T).Name} in {ManagerName}: {ex.Message}");
                return new T[0];
            }
        }

        /// <summary>
        /// Register a service with the container
        /// </summary>
        protected void RegisterService<TInterface, TImplementation>()
            where TInterface : class
            where TImplementation : class, TInterface, new()
        {
            try
            {
                ServiceContainer.RegisterSingleton<TInterface, TImplementation>();
                Debug.Log($"[DIChimeraManager] Registered service {typeof(TInterface).Name} -> {typeof(TImplementation).Name}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DIChimeraManager] Failed to register service {typeof(TInterface).Name} in {ManagerName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Register a service instance with the container
        /// </summary>
        protected void RegisterServiceInstance<TInterface>(TInterface instance) where TInterface : class
        {
            try
            {
                ServiceContainer.RegisterSingleton(instance);
                Debug.Log($"[DIChimeraManager] Registered service instance {typeof(TInterface).Name}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DIChimeraManager] Failed to register service instance {typeof(TInterface).Name} in {ManagerName}: {ex.Message}");
            }
        }

        #endregion

        #region IDisposable Implementation

        public virtual void Dispose()
        {
            if (_isDisposed) return;

            try
            {
                // Cleanup manager-specific resources
                OnDispose();

                // Cleanup DI resources
                ServiceProvider = null;
                ServiceContainer = null;

                _isDisposed = true;
                Debug.Log($"[DIChimeraManager] Disposed {ManagerName}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[DIChimeraManager] Error disposing {ManagerName}: {ex.Message}");
            }
        }

        /// <summary>
        /// Override this method to perform manager-specific cleanup
        /// </summary>
        protected virtual void OnDispose()
        {
            // Override in derived classes for cleanup logic
        }

        #endregion
    }

    /// <summary>
    /// Component for holding service container reference on GameObjects
    /// </summary>
    public class ServiceContainerComponent : MonoBehaviour
    {
        public ProjectChimera.Core.IServiceContainer Container { get; private set; }

        public void Initialize(ProjectChimera.Core.IServiceContainer container)
        {
            Container = container ?? throw new ArgumentNullException(nameof(container));
            Debug.Log("[ServiceContainerComponent] Service container attached to GameObject");
        }

        private void OnDestroy()
        {
            if (Container is IDisposable disposable)
            {
                try
                {
                    disposable.Dispose();
                    Debug.Log("[ServiceContainerComponent] Service container disposed with GameObject");
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[ServiceContainerComponent] Error disposing service container: {ex.Message}");
                }
            }
        }
    }
}