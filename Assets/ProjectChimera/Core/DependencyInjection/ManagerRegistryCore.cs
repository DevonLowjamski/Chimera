using ProjectChimera.Core.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProjectChimera.Core;

namespace ProjectChimera.Core.DependencyInjection
{
    /// <summary>
    /// Core registry for managing Project Chimera managers with modular architecture
    /// </summary>
    public class ManagerRegistry : IDisposable
    {
        private static ManagerRegistry _instance;
        private static readonly object _lock = new object();
        
        private readonly Dictionary<Type, ManagerRegistration> _registrations = new Dictionary<Type, ManagerRegistration>();
        private readonly Dictionary<Type, ChimeraManager> _instances = new Dictionary<Type, ChimeraManager>();
        private readonly Dictionary<Type, List<Type>> _dependencies = new Dictionary<Type, List<Type>>();
        
        private ManagerInitializer _initializer;
        private ManagerDependencyResolver _dependencyResolver;
        private ManagerDiagnostics _diagnostics;
        
        private bool _isDisposed = false;
        
        public static ManagerRegistry Instance
        {
            get
            {
                if (_instance == null)
                {
                    lock (_lock)
                    {
                        if (_instance == null)
                        {
                            _instance = new ManagerRegistry();
                        }
                    }
                }
                return _instance;
            }
        }
        
        public bool IsInitialized => _initializer?.IsInitialized ?? false;
        public int RegisteredManagerCount => _registrations.Count;
        public int ActiveManagerCount => _instances.Count;
        
        private ManagerRegistry() 
        {
            InitializeModules();
        }
        
        private void InitializeModules()
        {
            _initializer = new ManagerInitializer(_registrations, _instances, _dependencies);
            _dependencyResolver = new ManagerDependencyResolver(_registrations, _dependencies);
            _diagnostics = new ManagerDiagnostics(_registrations, _instances, _dependencies, _initializer, _dependencyResolver);
        }
        
        #region Manager Registration
        
        /// <summary>
        /// Registers a manager type with its initialization priority and dependencies
        /// </summary>
        public void RegisterManager<T>(int priority = 0, params Type[] dependencies) where T : ChimeraManager
        {
            RegisterManager(typeof(T), priority, dependencies);
        }
        
        /// <summary>
        /// Registers a manager type with its initialization priority and dependencies
        /// </summary>
        public void RegisterManager(Type managerType, int priority = 0, params Type[] dependencies)
        {
            if (!typeof(ChimeraManager).IsAssignableFrom(managerType))
            {
                throw new ManagerRegistryException(managerType, $"Manager type {managerType.Name} must derive from ChimeraManager");
            }
            
            lock (_lock)
            {
                if (IsInitialized)
                {
                    ChimeraLogger.LogWarning($"[ManagerRegistry] Cannot register {managerType.Name} after initialization. Use late registration methods.");
                    return;
                }
                
                var registration = new ManagerRegistration
                {
                    ManagerType = managerType,
                    Priority = priority,
                    Dependencies = dependencies?.ToList() ?? new List<Type>(),
                    RegistrationTime = Time.time
                };
                
                _registrations[managerType] = registration;
                
                if (dependencies != null && dependencies.Length > 0)
                {
                    _dependencies[managerType] = dependencies.ToList();
                }
                
                ChimeraLogger.Log($"[ManagerRegistry] Registered manager: {managerType.Name} (Priority: {priority})");
            }
        }
        
        /// <summary>
        /// Registers a manager instance directly
        /// </summary>
        public void RegisterManagerInstance<T>(T instance, int priority = 0) where T : ChimeraManager
        {
            if (instance == null)
                throw new ArgumentNullException(nameof(instance));
            
            var managerType = typeof(T);
            
            lock (_lock)
            {
                var registration = new ManagerRegistration
                {
                    ManagerType = managerType,
                    Priority = priority,
                    Dependencies = new List<Type>(),
                    RegistrationTime = Time.time,
                    Instance = instance
                };
                
                _registrations[managerType] = registration;
                _instances[managerType] = instance;
                
                ChimeraLogger.Log($"[ManagerRegistry] Registered manager instance: {managerType.Name}");
            }
        }
        
        #endregion
        
        #region Manager Resolution
        
        /// <summary>
        /// Gets a registered manager instance
        /// </summary>
        public T GetManager<T>() where T : ChimeraManager
        {
            return (T)GetManager(typeof(T));
        }
        
        /// <summary>
        /// Gets a registered manager instance by type
        /// </summary>
        public ChimeraManager GetManager(Type managerType)
        {
            lock (_lock)
            {
                if (_instances.TryGetValue(managerType, out var instance))
                {
                    return instance;
                }
                
                // Try to find and instantiate if registered but not created
                if (_registrations.TryGetValue(managerType, out var registration))
                {
                    if (registration.Instance != null)
                    {
                        _instances[managerType] = registration.Instance;
                        return registration.Instance;
                    }
                    
                    // Late instantiation using the initializer
                    if (_initializer != null)
                    {
                        var newInstance = _initializer.CreateManagerInstance(registration);
                        if (newInstance != null)
                        {
                            _instances[managerType] = newInstance;
                            registration.Instance = newInstance;
                            return newInstance;
                        }
                    }
                }
                
                ChimeraLogger.LogWarning($"[ManagerRegistry] Manager not found: {managerType.Name}");
                return null;
            }
        }
        
        /// <summary>
        /// Tries to get a manager, returns null if not found
        /// </summary>
        public T TryGetManager<T>() where T : ChimeraManager
        {
            try
            {
                return GetManager<T>();
            }
            catch
            {
                return null;
            }
        }
        
        /// <summary>
        /// Checks if a manager type is registered
        /// </summary>
        public bool IsRegistered<T>() where T : ChimeraManager
        {
            return IsRegistered(typeof(T));
        }
        
        /// <summary>
        /// Checks if a manager type is registered
        /// </summary>
        public bool IsRegistered(Type managerType)
        {
            lock (_lock)
            {
                return _registrations.ContainsKey(managerType);
            }
        }
        
        #endregion
        
        #region Initialization and Dependency Management
        
        /// <summary>
        /// Initializes all registered managers in the correct dependency order
        /// </summary>
        public void InitializeAllManagers()
        {
            lock (_lock)
            {
                _initializer?.InitializeAllManagers();
            }
        }
        
        /// <summary>
        /// Validates that all dependencies are registered
        /// </summary>
        public ManagerValidationResult ValidateDependencies()
        {
            lock (_lock)
            {
                return _dependencyResolver?.ValidateDependencies() ?? new ManagerValidationResult { IsValid = false };
            }
        }
        
        /// <summary>
        /// Gets dependency analysis for optimization
        /// </summary>
        public DependencyAnalysis AnalyzeDependencies()
        {
            lock (_lock)
            {
                return _dependencyResolver?.AnalyzeDependencies() ?? new DependencyAnalysis();
            }
        }
        
        #endregion
        
        #region Diagnostics and Metrics
        
        /// <summary>
        /// Gets comprehensive metrics about the manager registry
        /// </summary>
        public ManagerRegistryMetrics GetMetrics()
        {
            lock (_lock)
            {
                return _diagnostics?.GetMetrics() ?? new ManagerRegistryMetrics();
            }
        }
        
        /// <summary>
        /// Gets all registered manager types and their status
        /// </summary>
        public Dictionary<Type, ManagerStatus> GetManagerStatus()
        {
            lock (_lock)
            {
                return _diagnostics?.GetManagerStatus() ?? new Dictionary<Type, ManagerStatus>();
            }
        }
        
        /// <summary>
        /// Generates a comprehensive health report
        /// </summary>
        public ManagerHealthReport GenerateHealthReport()
        {
            lock (_lock)
            {
                return _diagnostics?.GenerateHealthReport() ?? new ManagerHealthReport();
            }
        }
        
        #endregion
        
        #region Cleanup
        
        /// <summary>
        /// Disposes all managers and clears the registry
        /// </summary>
        public void Dispose()
        {
            if (_isDisposed) return;
            
            lock (_lock)
            {
                if (_isDisposed) return;
                
                // Dispose managers in reverse initialization order
                var disposalOrder = _initializer?.InitializationOrder?.AsEnumerable().Reverse().ToList() ?? new List<Type>();
                
                foreach (var managerType in disposalOrder)
                {
                    if (_instances.TryGetValue(managerType, out var instance))
                    {
                        try
                        {
                            if (instance is IDisposable disposable)
                            {
                                disposable.Dispose();
                            }
                            
                            if (instance != null && instance.gameObject != null)
                            {
                                UnityEngine.Object.Destroy(instance.gameObject);
                            }
                        }
                        catch (Exception ex)
                        {
                            ChimeraLogger.LogError($"[ManagerRegistry] Error disposing {managerType.Name}: {ex.Message}");
                        }
                    }
                }
                
                _registrations.Clear();
                _instances.Clear();
                _dependencies.Clear();
                
                _initializer = null;
                _dependencyResolver = null;
                _diagnostics = null;
                
                _isDisposed = true;
                
                ChimeraLogger.Log("[ManagerRegistry] All managers disposed and registry cleared");
            }
        }
        
        #endregion
    }
}