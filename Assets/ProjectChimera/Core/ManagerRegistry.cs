using ProjectChimera.Core.Logging;
using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using ProjectChimera.Core.SimpleDI;

namespace ProjectChimera.Core
{
    /// <summary>
    /// Simplified manager registry for Project Chimera
    /// Focused on core cultivation game managers with backward compatibility
    /// </summary>
    public class ManagerRegistry : MonoBehaviour
    {
        [Header("Registry Configuration")]
        [SerializeField] private bool _enableRegistryLogging = true;
        [SerializeField] private bool _autoRegisterWithDI = true;

        // Simplified registry storage - only what we need for cultivation
        private readonly Dictionary<Type, ChimeraManager> _managerRegistry = new Dictionary<Type, ChimeraManager>();
        private readonly Dictionary<ChimeraManager, DateTime> _registrationTimes = new Dictionary<ChimeraManager, DateTime>();

        // Service container reference
        private SimpleDIContainer _serviceContainer;

        // Events for backward compatibility
        public System.Action<ChimeraManager> OnManagerRegistered;
        public System.Action<ChimeraManager> OnManagerUnregistered;
        public System.Action<Type, object> OnServiceRegistered;
        public System.Action<string> OnRegistrationError;

        // Properties for backward compatibility
        public int RegisteredManagerCount => _managerRegistry.Count;
        public IEnumerable<ChimeraManager> RegisteredManagers => _managerRegistry.Values;
        public IEnumerable<Type> RegisteredTypes => _managerRegistry.Keys;
        public bool HasServiceContainer => _serviceContainer != null;

        /// <summary>
        /// Initialize the manager registry
        /// </summary>
        public void Initialize(SimpleDIContainer serviceContainer = null)
        {
            _serviceContainer = serviceContainer;
            ChimeraLogger.LogVerbose("Manager registry initialized");

            if (_autoRegisterWithDI && _serviceContainer != null)
            {
                _serviceContainer.RegisterSingleton<ManagerRegistry, ManagerRegistry>(this);
            }

            // Auto-discover managers in scene
            DiscoverAndRegisterManagers();
        }

        /// <summary>
        /// Discover and register managers in the scene
        /// </summary>
        private void DiscoverAndRegisterManagers()
        {
            var managers = FindObjectsOfType<ChimeraManager>();
            foreach (var manager in managers)
            {
                RegisterManager(manager);
            }

            if (_enableRegistryLogging)
                ChimeraLogger.LogVerbose($"Auto-discovered and registered {managers.Length} managers");
        }

        /// <summary>
        /// Register a manager
        /// </summary>
        public void RegisterManager(ChimeraManager manager)
        {
            if (manager == null) return;

            var managerType = manager.GetType();

            // Check if already registered
            if (_managerRegistry.ContainsKey(managerType))
            {
                if (_managerRegistry[managerType] == manager)
                {
                    if (_enableRegistryLogging)
                        ChimeraLogger.LogVerbose($"Manager {managerType.Name} already registered");
                    return;
                }
                else
                {
                    if (_enableRegistryLogging)
                        ChimeraLogger.LogWarning($"Replacing existing manager registration for {managerType.Name}");
                }
            }

            // Register the manager
            _managerRegistry[managerType] = manager;
            _registrationTimes[manager] = DateTime.Now;

            // Register with DI container
            if (_autoRegisterWithDI && _serviceContainer != null)
            {
                _serviceContainer.Register(manager);
            }

            // Notify listeners
            OnManagerRegistered?.Invoke(manager);

            if (_enableRegistryLogging)
                ChimeraLogger.LogVerbose($"Registered manager: {managerType.Name}");
        }

        /// <summary>
        /// Get a manager by type
        /// </summary>
        public ChimeraManager GetManager(Type managerType)
        {
            if (_managerRegistry.TryGetValue(managerType, out var manager))
            {
                return manager;
            }

            if (_enableRegistryLogging)
                ChimeraLogger.LogWarning($"Manager not found: {managerType.Name}");

            return null;
        }

        /// <summary>
        /// Get a manager by generic type
        /// </summary>
        public T GetManager<T>() where T : ChimeraManager
        {
            return GetManager(typeof(T)) as T;
        }

        /// <summary>
        /// Try to get a manager
        /// </summary>
        public bool TryGetManager<T>(out T manager) where T : ChimeraManager
        {
            manager = GetManager<T>();
            return manager != null;
        }

        /// <summary>
        /// Get all registered managers as an array
        /// </summary>
        public ChimeraManager[] GetAllManagers()
        {
            return _managerRegistry.Values.ToArray();
        }

        /// <summary>
        /// Check if a manager is registered
        /// </summary>
        public bool IsManagerRegistered(Type managerType)
        {
            return _managerRegistry.ContainsKey(managerType);
        }

        /// <summary>
        /// Check if a manager is registered (generic)
        /// </summary>
        public bool IsManagerRegistered<T>() where T : ChimeraManager
        {
            return IsManagerRegistered(typeof(T));
        }

        /// <summary>
        /// Unregister a manager
        /// </summary>
        public void UnregisterManager(ChimeraManager manager)
        {
            if (manager == null) return;

            var managerType = manager.GetType();

            if (_managerRegistry.Remove(managerType))
            {
                _registrationTimes.Remove(manager);

                OnManagerUnregistered?.Invoke(manager);

                if (_enableRegistryLogging)
                    ChimeraLogger.LogVerbose($"Unregistered manager: {managerType.Name}");
            }
        }

        /// <summary>
        /// Unregister a manager by type
        /// </summary>
        public void UnregisterManager<T>() where T : ChimeraManager
        {
            var manager = GetManager<T>();
            if (manager != null)
            {
                UnregisterManager(manager);
            }
        }

        /// <summary>
        /// Shutdown all managers
        /// </summary>
        public void ShutdownAll()
        {
            foreach (var manager in _managerRegistry.Values)
            {
                try
                {
                    manager.Shutdown();
                    if (_enableRegistryLogging)
                        ChimeraLogger.LogVerbose($"Shut down manager: {manager.ManagerName}");
                }
                catch (Exception ex)
                {
                    ChimeraLogger.LogError($"Error shutting down {manager.ManagerName}: {ex.Message}");
                }
            }

            _managerRegistry.Clear();
            _registrationTimes.Clear();

            if (_enableRegistryLogging)
                ChimeraLogger.LogVerbose("All managers shut down");
        }

        /// <summary>
        /// Get manager registration info
        /// </summary>
        public Dictionary<string, DateTime> GetRegistrationInfo()
        {
            var info = new Dictionary<string, DateTime>();
            foreach (var kvp in _registrationTimes)
            {
                info[kvp.Key.ManagerName] = kvp.Value;
            }
            return info;
        }
    }
}
