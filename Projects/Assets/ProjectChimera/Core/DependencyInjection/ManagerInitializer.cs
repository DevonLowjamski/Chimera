using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProjectChimera.Core;

namespace ProjectChimera.Core.DependencyInjection
{
    /// <summary>
    /// Handles manager initialization, dependency resolution, and lifecycle management
    /// </summary>
    public class ManagerInitializer
    {
        private readonly Dictionary<Type, ManagerRegistration> _registrations;
        private readonly Dictionary<Type, ChimeraManager> _instances;
        private readonly Dictionary<Type, List<Type>> _dependencies;
        private readonly List<Type> _initializationOrder = new List<Type>();
        
        private bool _isInitialized = false;
        
        public bool IsInitialized => _isInitialized;
        public List<Type> InitializationOrder => new List<Type>(_initializationOrder);
        
        public ManagerInitializer(
            Dictionary<Type, ManagerRegistration> registrations,
            Dictionary<Type, ChimeraManager> instances,
            Dictionary<Type, List<Type>> dependencies)
        {
            _registrations = registrations ?? throw new ArgumentNullException(nameof(registrations));
            _instances = instances ?? throw new ArgumentNullException(nameof(instances));
            _dependencies = dependencies ?? throw new ArgumentNullException(nameof(dependencies));
        }
        
        /// <summary>
        /// Initializes all registered managers in the correct dependency order
        /// </summary>
        public void InitializeAllManagers()
        {
            if (_isInitialized)
            {
                Debug.LogWarning("[ManagerInitializer] Managers already initialized");
                return;
            }
            
            try
            {
                // Calculate initialization order
                CalculateInitializationOrder();
                
                // Initialize managers in order
                foreach (var managerType in _initializationOrder)
                {
                    InitializeManager(managerType);
                }
                
                _isInitialized = true;
                Debug.Log($"[ManagerInitializer] Initialized {_instances.Count} managers successfully");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ManagerInitializer] Failed to initialize managers: {ex.Message}");
                throw;
            }
        }
        
        /// <summary>
        /// Initializes a specific manager and its dependencies
        /// </summary>
        public void InitializeManager(Type managerType)
        {
            if (_instances.ContainsKey(managerType))
            {
                return; // Already initialized
            }
            
            if (!_registrations.TryGetValue(managerType, out var registration))
            {
                throw new InvalidOperationException($"Manager type {managerType.Name} is not registered");
            }
            
            // Initialize dependencies first
            if (_dependencies.TryGetValue(managerType, out var deps))
            {
                foreach (var dependency in deps)
                {
                    if (!_instances.ContainsKey(dependency))
                    {
                        InitializeManager(dependency);
                    }
                }
            }
            
            // Create and initialize the manager
            var instance = registration.Instance ?? CreateManagerInstance(registration);
            if (instance != null)
            {
                _instances[managerType] = instance;
                registration.Instance = instance;
                
                // Call custom initialization if supported
                if (instance is IManagerCustomInitialization customInit)
                {
                    try
                    {
                        customInit.InitializeManager();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[ManagerInitializer] Custom initialization failed for {managerType.Name}: {ex.Message}");
                    }
                }
                
                // Notify about dependency resolution
                if (instance is IManagerDependencyAware dependencyAware)
                {
                    try
                    {
                        dependencyAware.OnDependenciesResolved();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"[ManagerInitializer] Dependency notification failed for {managerType.Name}: {ex.Message}");
                    }
                }
                
                Debug.Log($"[ManagerInitializer] Initialized manager: {managerType.Name}");
            }
            else
            {
                throw new InvalidOperationException($"Failed to create instance of {managerType.Name}");
            }
        }
        
        /// <summary>
        /// Creates a manager instance using reflection and Unity components
        /// </summary>
        public ChimeraManager CreateManagerInstance(ManagerRegistration registration)
        {
            try
            {
                // Try to find existing GameObject with the manager
                var existing = UnityEngine.Object.FindObjectOfType(registration.ManagerType) as ChimeraManager;
                if (existing != null)
                {
                    Debug.Log($"[ManagerInitializer] Found existing instance of {registration.ManagerType.Name}");
                    return existing;
                }
                
                // Create new GameObject with the manager component
                var managerObject = new GameObject($"{registration.ManagerType.Name}");
                var managerComponent = managerObject.AddComponent(registration.ManagerType) as ChimeraManager;
                
                if (managerComponent == null)
                {
                    UnityEngine.Object.Destroy(managerObject);
                    throw new InvalidOperationException($"Failed to add component {registration.ManagerType.Name}");
                }
                
                // Don't destroy on load for persistent managers
                UnityEngine.Object.DontDestroyOnLoad(managerObject);
                
                Debug.Log($"[ManagerInitializer] Created new instance of {registration.ManagerType.Name}");
                return managerComponent;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ManagerInitializer] Failed to create {registration.ManagerType.Name}: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Calculates the correct initialization order based on dependencies and priorities
        /// </summary>
        public void CalculateInitializationOrder()
        {
            _initializationOrder.Clear();
            
            var remaining = new HashSet<Type>(_registrations.Keys);
            var resolved = new HashSet<Type>();
            
            // Sort by priority first (higher priority = earlier initialization)
            var sortedByPriority = _registrations.Values
                .OrderByDescending(r => r.Priority)
                .ThenBy(r => r.RegistrationTime)
                .ToList();
            
            while (remaining.Count > 0)
            {
                var progress = false;
                
                foreach (var registration in sortedByPriority)
                {
                    var managerType = registration.ManagerType;
                    
                    if (!remaining.Contains(managerType))
                        continue;
                    
                    // Check if all dependencies are resolved
                    var canInitialize = true;
                    if (_dependencies.TryGetValue(managerType, out var deps))
                    {
                        foreach (var dep in deps)
                        {
                            if (!resolved.Contains(dep) && remaining.Contains(dep))
                            {
                                canInitialize = false;
                                break;
                            }
                        }
                    }
                    
                    if (canInitialize)
                    {
                        _initializationOrder.Add(managerType);
                        remaining.Remove(managerType);
                        resolved.Add(managerType);
                        progress = true;
                    }
                }
                
                if (!progress)
                {
                    // Circular dependency detected
                    var remainingTypes = string.Join(", ", remaining.Select(t => t.Name));
                    throw new CircularDependencyException(remaining.ToList());
                }
            }
            
            Debug.Log($"[ManagerInitializer] Calculated initialization order: {string.Join(" → ", _initializationOrder.Select(t => t.Name))}");
        }
        
        /// <summary>
        /// Resets initialization state for re-initialization
        /// </summary>
        public void Reset()
        {
            _isInitialized = false;
            _initializationOrder.Clear();
            Debug.Log("[ManagerInitializer] Reset initialization state");
        }
        
        /// <summary>
        /// Gets the current initialization progress
        /// </summary>
        public InitializationProgress GetProgress()
        {
            return new InitializationProgress
            {
                TotalManagers = _registrations.Count,
                InitializedManagers = _instances.Count,
                IsComplete = _isInitialized,
                CurrentOrder = new List<Type>(_initializationOrder),
                ProgressPercentage = _registrations.Count > 0 ? (float)_instances.Count / _registrations.Count : 0f
            };
        }
    }
    
    /// <summary>
    /// Progress information for manager initialization
    /// </summary>
    public class InitializationProgress
    {
        public int TotalManagers { get; set; }
        public int InitializedManagers { get; set; }
        public bool IsComplete { get; set; }
        public List<Type> CurrentOrder { get; set; } = new List<Type>();
        public float ProgressPercentage { get; set; }
        
        public string GetSummary()
        {
            return $"Initialization: {InitializedManagers}/{TotalManagers} ({ProgressPercentage:P1}) - {(IsComplete ? "Complete" : "In Progress")}";
        }
    }
}