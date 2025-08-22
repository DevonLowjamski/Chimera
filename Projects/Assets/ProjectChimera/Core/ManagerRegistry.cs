using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using ProjectChimera.Core.DependencyInjection;

namespace ProjectChimera.Core
{
    /// <summary>
    /// Centralized registry for all ChimeraManager instances.
    /// Extracted from DIGameManager for modular architecture.
    /// Handles manager registration, discovery, and access with both DI container and legacy support.
    /// </summary>
    public class ManagerRegistry : MonoBehaviour
    {
        [Header("Registry Configuration")]
        [SerializeField] private bool _enableRegistryLogging = false;
        [SerializeField] private bool _autoRegisterWithDI = true;
        [SerializeField] private bool _enableInterfaceRegistration = true;
        [SerializeField] private bool _validateRegistrations = true;
        
        [Header("Discovery Settings")]
        [SerializeField] private bool _enableAutoDiscovery = true;
        [SerializeField] private bool _excludeSelfFromDiscovery = true;
        
        // Core registry storage
        private readonly Dictionary<Type, ChimeraManager> _managerRegistry = new Dictionary<Type, ChimeraManager>();
        private readonly Dictionary<Type, List<Type>> _interfaceToTypeMapping = new Dictionary<Type, List<Type>>();
        private readonly Dictionary<ChimeraManager, DateTime> _registrationTimes = new Dictionary<ChimeraManager, DateTime>();
        
        // Service container reference
        private IChimeraServiceContainer _serviceContainer;
        
        // Events
        public System.Action<ChimeraManager> OnManagerRegistered;
        public System.Action<ChimeraManager> OnManagerUnregistered;
        public System.Action<Type, object> OnServiceRegistered;
        public System.Action<string> OnRegistrationError;
        
        // Properties
        public int RegisteredManagerCount => _managerRegistry.Count;
        public IEnumerable<ChimeraManager> RegisteredManagers => _managerRegistry.Values;
        public IEnumerable<Type> RegisteredTypes => _managerRegistry.Keys;
        public bool HasServiceContainer => _serviceContainer != null;
        
        /// <summary>
        /// Initialize the manager registry with optional service container
        /// </summary>
        public void Initialize(IChimeraServiceContainer serviceContainer = null)
        {
            _serviceContainer = serviceContainer;
            LogDebug("Manager registry initialized");
            
            if (_enableAutoDiscovery)
            {
                DiscoverAndRegisterAllManagers();
            }
        }
        
        /// <summary>
        /// Register a manager with both the registry and DI container
        /// </summary>
        public void RegisterManager<T>(T manager) where T : ChimeraManager
        {
            if (manager == null)
            {
                LogError("Cannot register null manager");
                return;
            }
            
            try
            {
                var managerType = typeof(T);
                
                // Check if already registered
                if (_managerRegistry.ContainsKey(managerType))
                {
                    if (_managerRegistry[managerType] == manager)
                    {
                        LogDebug($"Manager {manager.ManagerName} already registered, skipping");
                        return;
                    }
                    else
                    {
                        LogDebug($"Replacing existing registration for {managerType.Name}");
                    }
                }
                
                // Register in local dictionary
                _managerRegistry[managerType] = manager;
                _registrationTimes[manager] = DateTime.Now;
                
                // Register with DI container if available
                if (_autoRegisterWithDI && _serviceContainer != null)
                {
                    RegisterWithDIContainer(manager, managerType);
                }
                
                // Register interfaces if enabled
                if (_enableInterfaceRegistration)
                {
                    RegisterManagerInterfaces(manager, managerType);
                }
                
                LogDebug($"Successfully registered manager: {manager.ManagerName} ({managerType.Name})");
                OnManagerRegistered?.Invoke(manager);
            }
            catch (Exception ex)
            {
                var errorMsg = $"Failed to register manager {manager?.ManagerName}: {ex.Message}";
                LogError(errorMsg);
                OnRegistrationError?.Invoke(errorMsg);
            }
        }
        
        /// <summary>
        /// Register manager with DI container
        /// </summary>
        private void RegisterWithDIContainer<T>(T manager, Type managerType) where T : ChimeraManager
        {
            try
            {
                _serviceContainer.RegisterSingleton<T>(manager);
                LogDebug($"Registered {managerType.Name} with DI container");
            }
            catch (Exception ex)
            {
                LogError($"Failed to register {managerType.Name} with DI container: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Register all interfaces implemented by the manager
        /// </summary>
        private void RegisterManagerInterfaces<T>(T manager, Type managerType) where T : ChimeraManager
        {
            var interfaces = managerType.GetInterfaces()
                .Where(i => i != typeof(IDisposable) && i != typeof(IChimeraManager))
                .ToList();
            
            foreach (var interfaceType in interfaces)
            {
                try
                {
                    // Track interface to type mapping
                    if (!_interfaceToTypeMapping.ContainsKey(interfaceType))
                    {
                        _interfaceToTypeMapping[interfaceType] = new List<Type>();
                    }
                    _interfaceToTypeMapping[interfaceType].Add(managerType);
                    
                    // Register with DI container if available
                    if (_serviceContainer != null)
                    {
                        _serviceContainer.RegisterSingleton(interfaceType, manager);
                        OnServiceRegistered?.Invoke(interfaceType, manager);
                        LogDebug($"Registered interface {interfaceType.Name} -> {managerType.Name}");
                    }
                }
                catch (Exception ex)
                {
                    LogError($"Failed to register interface {interfaceType.Name} for {managerType.Name}: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// Unregister a manager from both registry and DI container
        /// </summary>
        public void UnregisterManager<T>(T manager) where T : ChimeraManager
        {
            if (manager == null) return;
            
            try
            {
                var managerType = typeof(T);
                
                if (_managerRegistry.ContainsKey(managerType) && _managerRegistry[managerType] == manager)
                {
                    // Remove from registry
                    _managerRegistry.Remove(managerType);
                    _registrationTimes.Remove(manager);
                    
                    // Clean up interface mappings
                    var interfacesToRemove = _interfaceToTypeMapping
                        .Where(kvp => kvp.Value.Contains(managerType))
                        .Select(kvp => kvp.Key)
                        .ToList();
                    
                    foreach (var interfaceType in interfacesToRemove)
                    {
                        _interfaceToTypeMapping[interfaceType].Remove(managerType);
                        if (_interfaceToTypeMapping[interfaceType].Count == 0)
                        {
                            _interfaceToTypeMapping.Remove(interfaceType);
                        }
                    }
                    
                    LogDebug($"Unregistered manager: {manager.ManagerName}");
                    OnManagerUnregistered?.Invoke(manager);
                }
            }
            catch (Exception ex)
            {
                LogError($"Failed to unregister manager {manager?.ManagerName}: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Get manager using DI container (preferred) or legacy registry
        /// </summary>
        public T GetManager<T>() where T : ChimeraManager
        {
            try
            {
                // Try DI container first
                if (_serviceContainer != null)
                {
                    var service = _serviceContainer.TryResolve<T>();
                    if (service != null)
                    {
                        LogDebug($"Retrieved {typeof(T).Name} from DI container");
                        return service;
                    }
                }
                
                // Fallback to legacy registry
                if (_managerRegistry.TryGetValue(typeof(T), out var manager))
                {
                    LogDebug($"Retrieved {typeof(T).Name} from registry");
                    return manager as T;
                }
                
                LogDebug($"Manager {typeof(T).Name} not found in container or registry");
                return null;
            }
            catch (Exception ex)
            {
                LogError($"Error retrieving manager {typeof(T).Name}: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Get manager by Type using DI container (preferred) or legacy registry
        /// </summary>
        public ChimeraManager GetManager(System.Type managerType)
        {
            try
            {
                // Try DI container first
                if (_serviceContainer != null)
                {
                    try
                    {
                        var service = _serviceContainer.Resolve(managerType);
                        if (service != null)
                        {
                            LogDebug($"Retrieved {managerType.Name} from DI container");
                            return service as ChimeraManager;
                        }
                    }
                    catch
                    {
                        // Fall through to registry lookup
                    }
                }
                
                // Fallback to legacy registry
                if (_managerRegistry.TryGetValue(managerType, out var manager))
                {
                    LogDebug($"Retrieved {managerType.Name} from registry");
                    return manager;
                }
                
                LogDebug($"Manager {managerType.Name} not found in container or registry");
                return null;
            }
            catch (Exception ex)
            {
                LogError($"Error retrieving manager {managerType.Name}: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Get manager by interface type
        /// </summary>
        public T GetManagerByInterface<T>() where T : class
        {
            try
            {
                // Try DI container first
                if (_serviceContainer != null)
                {
                    var service = _serviceContainer.TryResolve<T>();
                    if (service != null)
                    {
                        LogDebug($"Retrieved {typeof(T).Name} interface from DI container");
                        return service;
                    }
                }
                
                // Fallback to interface mapping
                var interfaceType = typeof(T);
                if (_interfaceToTypeMapping.TryGetValue(interfaceType, out var implementingTypes) && implementingTypes.Count > 0)
                {
                    var firstImplementingType = implementingTypes[0];
                    if (_managerRegistry.TryGetValue(firstImplementingType, out var manager))
                    {
                        LogDebug($"Retrieved {interfaceType.Name} interface from registry mapping");
                        return manager as T;
                    }
                }
                
                LogDebug($"Interface {typeof(T).Name} not found");
                return null;
            }
            catch (Exception ex)
            {
                LogError($"Error retrieving interface {typeof(T).Name}: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Get all managers implementing a specific interface
        /// </summary>
        public IEnumerable<T> GetManagersByInterface<T>() where T : class
        {
            var results = new List<T>();
            var interfaceType = typeof(T);
            
            if (_interfaceToTypeMapping.TryGetValue(interfaceType, out var implementingTypes))
            {
                foreach (var type in implementingTypes)
                {
                    if (_managerRegistry.TryGetValue(type, out var manager) && manager is T typedManager)
                    {
                        results.Add(typedManager);
                    }
                }
            }
            
            return results;
        }
        
        /// <summary>
        /// Check if a manager type is registered
        /// </summary>
        public bool IsManagerRegistered<T>() where T : ChimeraManager
        {
            return _managerRegistry.ContainsKey(typeof(T));
        }
        
        /// <summary>
        /// Check if a manager instance is registered
        /// </summary>
        public bool IsManagerRegistered(ChimeraManager manager)
        {
            return manager != null && _managerRegistry.ContainsValue(manager);
        }
        
        /// <summary>
        /// Auto-discover and register all managers in the scene
        /// </summary>
        public void DiscoverAndRegisterAllManagers()
        {
            try
            {
                LogDebug("Starting auto-discovery of ChimeraManager instances");
                
                // Find all ChimeraManager instances using Unity 6 API
                var allManagers = UnityEngine.Object.FindObjectsByType<ChimeraManager>(FindObjectsSortMode.None);
                
                int registeredCount = 0;
                
                foreach (var manager in allManagers)
                {
                    // Skip self if enabled
                    if (_excludeSelfFromDiscovery && manager == this) continue;
                    
                    // Skip already registered managers
                    if (IsManagerRegistered(manager)) continue;
                    
                    // Register using reflection to call generic method
                    var managerType = manager.GetType();
                    var registerMethod = GetType().GetMethod(nameof(RegisterManager))?.MakeGenericMethod(managerType);
                    registerMethod?.Invoke(this, new object[] { manager });
                    
                    registeredCount++;
                }
                
                LogDebug($"Auto-discovery complete: {registeredCount} new managers registered (total: {_managerRegistry.Count})");
            }
            catch (Exception ex)
            {
                LogError($"Auto-discovery failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Get registration summary for all managers
        /// </summary>
        public RegistrationSummary GetRegistrationSummary()
        {
            var summary = new RegistrationSummary
            {
                TotalRegisteredManagers = _managerRegistry.Count,
                RegisteredTypes = _managerRegistry.Keys.Select(t => t.Name).ToList(),
                RegisteredInterfaces = _interfaceToTypeMapping.Keys.Select(t => t.Name).ToList(),
                HasServiceContainer = _serviceContainer != null
            };
            
            // Calculate average registration time
            if (_registrationTimes.Count > 0)
            {
                var totalTime = _registrationTimes.Values.Sum(dt => dt.Ticks);
                summary.AverageRegistrationTime = new DateTime(totalTime / _registrationTimes.Count);
            }
            
            return summary;
        }
        
        /// <summary>
        /// Validate all registrations are healthy
        /// </summary>
        public RegistrationValidationResult ValidateRegistrations()
        {
            var result = new RegistrationValidationResult
            {
                ValidatedManagers = 0,
                InvalidManagers = new List<string>(),
                MissingDependencies = new List<string>()
            };
            
            foreach (var kvp in _managerRegistry)
            {
                var managerType = kvp.Key;
                var manager = kvp.Value;
                
                if (manager == null)
                {
                    result.InvalidManagers.Add($"Null manager registered for type {managerType.Name}");
                    continue;
                }
                
                if (!manager.IsInitialized)
                {
                    result.InvalidManagers.Add($"Manager {manager.ManagerName} is not initialized");
                    continue;
                }
                
                result.ValidatedManagers++;
            }
            
            result.IsValid = result.InvalidManagers.Count == 0;
            return result;
        }
        
        /// <summary>
        /// Clear all registrations
        /// </summary>
        public void ClearAllRegistrations()
        {
            LogDebug($"Clearing all registrations ({_managerRegistry.Count} managers)");
            
            _managerRegistry.Clear();
            _interfaceToTypeMapping.Clear();
            _registrationTimes.Clear();
            
            LogDebug("All registrations cleared");
        }
        
        /// <summary>
        /// Set or update the service container reference
        /// </summary>
        public void SetServiceContainer(IChimeraServiceContainer serviceContainer)
        {
            _serviceContainer = serviceContainer;
            LogDebug($"Service container {(serviceContainer != null ? "set" : "cleared")}");
        }
        
        private void LogDebug(string message)
        {
            if (_enableRegistryLogging)
                Debug.Log($"[ManagerRegistry] {message}");
        }
        
        private void LogError(string message)
        {
            Debug.LogError($"[ManagerRegistry] {message}");
        }
        
        private void OnDestroy()
        {
            ClearAllRegistrations();
        }
    }
    
    /// <summary>
    /// Summary of manager registration state
    /// </summary>
    public class RegistrationSummary
    {
        public int TotalRegisteredManagers { get; set; }
        public List<string> RegisteredTypes { get; set; } = new List<string>();
        public List<string> RegisteredInterfaces { get; set; } = new List<string>();
        public bool HasServiceContainer { get; set; }
        public DateTime AverageRegistrationTime { get; set; }
    }
    
    /// <summary>
    /// Result of registration validation
    /// </summary>
    public class RegistrationValidationResult
    {
        public bool IsValid { get; set; }
        public int ValidatedManagers { get; set; }
        public List<string> InvalidManagers { get; set; } = new List<string>();
        public List<string> MissingDependencies { get; set; } = new List<string>();
    }
}