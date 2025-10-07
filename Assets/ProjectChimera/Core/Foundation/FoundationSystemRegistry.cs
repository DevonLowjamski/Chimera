using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using System.Linq;

namespace ProjectChimera.Core.Foundation
{
    /// <summary>
    /// REFACTORED: Foundation System Registry - Focused system registration and management
    /// Handles system registration, discovery, lifecycle tracking, and dependency resolution
    /// Single Responsibility: System registry and lifecycle management
    /// </summary>
    public class FoundationSystemRegistry : MonoBehaviour
    {
        [Header("System Registry Settings")]
        [SerializeField] private bool _enableSystemRegistry = true;
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private bool _enableDependencyTracking = true;
        [SerializeField] private int _maxRegisteredSystems = 50;

        // System storage
        private readonly Dictionary<string, IFoundationSystem> _registeredSystems = new Dictionary<string, IFoundationSystem>();
        private readonly Dictionary<string, SystemRegistrationData> _systemMetadata = new Dictionary<string, SystemRegistrationData>();
        private readonly Dictionary<string, List<string>> _systemDependencies = new Dictionary<string, List<string>>();

        // System categories
        private readonly Dictionary<SystemCategory, List<string>> _systemsByCategory = new Dictionary<SystemCategory, List<string>>();

        // Statistics
        private SystemRegistryStats _stats = new SystemRegistryStats();

        // Properties
        public bool IsEnabled { get; private set; } = true;
        public SystemRegistryStats GetStats() => _stats;

        // Events
        public System.Action<IFoundationSystem> OnSystemRegistered;
        public System.Action<IFoundationSystem> OnSystemUnregistered;
        public System.Action<string, SystemHealth> OnSystemHealthChanged;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            _stats = new SystemRegistryStats();
            InitializeSystemCategories();

            if (_enableLogging)
                ChimeraLogger.Log("FOUNDATION", "📋 FoundationSystemRegistry initialized", this);
        }

        /// <summary>
        /// Register foundation system
        /// </summary>
        public bool RegisterSystem(IFoundationSystem system)
        {
            if (!IsEnabled || !_enableSystemRegistry || system == null)
                return false;

            if (string.IsNullOrEmpty(system.SystemName))
            {
                if (_enableLogging)
                    ChimeraLogger.LogWarning("FOUNDATION", "Cannot register system with empty name", this);
                return false;
            }

            if (_registeredSystems.ContainsKey(system.SystemName))
            {
                if (_enableLogging)
                    ChimeraLogger.LogWarning("FOUNDATION", $"System already registered: {system.SystemName}", this);
                return false;
            }

            if (_registeredSystems.Count >= _maxRegisteredSystems)
            {
                if (_enableLogging)
                    ChimeraLogger.LogWarning("FOUNDATION", $"Maximum registered systems reached: {_maxRegisteredSystems}", this);
                return false;
            }

            var registrationData = new SystemRegistrationData
            {
                SystemName = system.SystemName,
                RegistrationTime = Time.time,
                Category = DetermineSystemCategory(system.SystemName),
                IsActive = system.IsEnabled
            };

            _registeredSystems[system.SystemName] = system;
            _systemMetadata[system.SystemName] = registrationData;

            // Add to category tracking
            AddSystemToCategory(system.SystemName, registrationData.Category);

            _stats.RegisteredSystems++;
            if (system.IsEnabled) _stats.ActiveSystems++;

            OnSystemRegistered?.Invoke(system);

            if (_enableLogging)
                ChimeraLogger.Log("FOUNDATION", $"Registered foundation system: {system.SystemName} ({registrationData.Category})", this);

            return true;
        }

        /// <summary>
        /// Unregister foundation system
        /// </summary>
        public bool UnregisterSystem(string systemName)
        {
            if (!IsEnabled || string.IsNullOrEmpty(systemName))
                return false;

            if (!_registeredSystems.TryGetValue(systemName, out var system))
                return false;

            // Remove from category tracking
            if (_systemMetadata.TryGetValue(systemName, out var metadata))
            {
                RemoveSystemFromCategory(systemName, metadata.Category);
                _systemMetadata.Remove(systemName);
            }

            // Remove dependencies
            if (_enableDependencyTracking)
            {
                _systemDependencies.Remove(systemName);
            }

            _registeredSystems.Remove(systemName);

            _stats.RegisteredSystems--;
            if (system.IsEnabled) _stats.ActiveSystems--;

            OnSystemUnregistered?.Invoke(system);

            if (_enableLogging)
                ChimeraLogger.Log("FOUNDATION", $"Unregistered foundation system: {systemName}", this);

            return true;
        }

        /// <summary>
        /// Get registered system by name
        /// </summary>
        public IFoundationSystem GetSystem(string systemName)
        {
            if (!IsEnabled || string.IsNullOrEmpty(systemName))
                return null;

            _registeredSystems.TryGetValue(systemName, out var system);
            return system;
        }

        /// <summary>
        /// Get all registered systems
        /// </summary>
        public IFoundationSystem[] GetRegisteredSystems()
        {
            if (!IsEnabled)
                return new IFoundationSystem[0];

            return _registeredSystems.Values.ToArray();
        }

        /// <summary>
        /// Get systems by category
        /// </summary>
        public IFoundationSystem[] GetSystemsByCategory(SystemCategory category)
        {
            if (!IsEnabled || !_systemsByCategory.TryGetValue(category, out var systemNames))
                return new IFoundationSystem[0];

            var systems = new List<IFoundationSystem>();
            foreach (var systemName in systemNames)
            {
                if (_registeredSystems.TryGetValue(systemName, out var system))
                {
                    systems.Add(system);
                }
            }

            return systems.ToArray();
        }

        /// <summary>
        /// Get system registration data
        /// </summary>
        public SystemRegistrationData GetSystemMetadata(string systemName)
        {
            _systemMetadata.TryGetValue(systemName, out var metadata);
            return metadata;
        }

        /// <summary>
        /// Register system dependency
        /// </summary>
        public bool RegisterSystemDependency(string systemName, string dependencyName)
        {
            if (!IsEnabled || !_enableDependencyTracking)
                return false;

            if (!_systemDependencies.ContainsKey(systemName))
            {
                _systemDependencies[systemName] = new List<string>();
            }

            if (!_systemDependencies[systemName].Contains(dependencyName))
            {
                _systemDependencies[systemName].Add(dependencyName);

                if (_enableLogging)
                    ChimeraLogger.Log("FOUNDATION", $"Registered dependency: {systemName} -> {dependencyName}", this);
            }

            return true;
        }

        /// <summary>
        /// Get system dependencies
        /// </summary>
        public string[] GetSystemDependencies(string systemName)
        {
            if (!IsEnabled || !_enableDependencyTracking || string.IsNullOrEmpty(systemName))
                return new string[0];

            if (_systemDependencies.TryGetValue(systemName, out var dependencies))
            {
                return dependencies.ToArray();
            }

            return new string[0];
        }

        /// <summary>
        /// Check if all system dependencies are satisfied
        /// </summary>
        public bool AreSystemDependenciesSatisfied(string systemName)
        {
            if (!_enableDependencyTracking)
                return true;

            var dependencies = GetSystemDependencies(systemName);
            foreach (var dependency in dependencies)
            {
                var depSystem = GetSystem(dependency);
                if (depSystem == null || !depSystem.IsInitialized)
                {
                    return false;
                }
            }

            return true;
        }

        /// <summary>
        /// Get systems in dependency order
        /// </summary>
        public List<string> GetSystemsInDependencyOrder()
        {
            if (!_enableDependencyTracking)
                return new List<string>(_registeredSystems.Keys);

            // Simple topological sort for dependency resolution
            var result = new List<string>();
            var visited = new HashSet<string>();
            var visiting = new HashSet<string>();

            foreach (var systemName in _registeredSystems.Keys)
            {
                if (!visited.Contains(systemName))
                {
                    if (!VisitSystemForDependencySort(systemName, visited, visiting, result))
                    {
                        // Circular dependency detected - fallback to simple order
                        if (_enableLogging)
                            ChimeraLogger.LogWarning("FOUNDATION", $"Circular dependency detected involving: {systemName}", this);
                        return new List<string>(_registeredSystems.Keys);
                    }
                }
            }

            return result;
        }

        /// <summary>
        /// Update system activity status
        /// </summary>
        public void UpdateSystemActivity(string systemName, bool isActive)
        {
            if (_systemMetadata.TryGetValue(systemName, out var metadata))
            {
                bool wasActive = metadata.IsActive;
                metadata.IsActive = isActive;
                _systemMetadata[systemName] = metadata;

                // Update stats
                if (wasActive && !isActive) _stats.ActiveSystems--;
                else if (!wasActive && isActive) _stats.ActiveSystems++;
            }
        }

        /// <summary>
        /// Set system enabled/disabled
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;

            if (_enableLogging)
                ChimeraLogger.Log("FOUNDATION", $"FoundationSystemRegistry: {(enabled ? "enabled" : "disabled")}", this);
        }

        #region Private Methods

        /// <summary>
        /// Initialize system categories
        /// </summary>
        private void InitializeSystemCategories()
        {
            foreach (SystemCategory category in System.Enum.GetValues(typeof(SystemCategory)))
            {
                _systemsByCategory[category] = new List<string>();
            }
        }

        /// <summary>
        /// Determine system category based on name
        /// </summary>
        private SystemCategory DetermineSystemCategory(string systemName)
        {
            var lowerName = systemName.ToLower();

            if (lowerName.Contains("performance") || lowerName.Contains("metrics") || lowerName.Contains("monitor"))
                return SystemCategory.Performance;
            else if (lowerName.Contains("cultivation") || lowerName.Contains("plant") || lowerName.Contains("genetics"))
                return SystemCategory.Cultivation;
            else if (lowerName.Contains("construction") || lowerName.Contains("building") || lowerName.Contains("architecture"))
                return SystemCategory.Construction;
            else if (lowerName.Contains("economy") || lowerName.Contains("trading") || lowerName.Contains("market"))
                return SystemCategory.Economy;
            else if (lowerName.Contains("ui") || lowerName.Contains("input") || lowerName.Contains("interface"))
                return SystemCategory.UserInterface;
            else if (lowerName.Contains("rendering") || lowerName.Contains("graphics") || lowerName.Contains("visual"))
                return SystemCategory.Rendering;

            return SystemCategory.Core;
        }

        /// <summary>
        /// Add system to category tracking
        /// </summary>
        private void AddSystemToCategory(string systemName, SystemCategory category)
        {
            if (!_systemsByCategory[category].Contains(systemName))
            {
                _systemsByCategory[category].Add(systemName);
            }
        }

        /// <summary>
        /// Remove system from category tracking
        /// </summary>
        private void RemoveSystemFromCategory(string systemName, SystemCategory category)
        {
            _systemsByCategory[category].Remove(systemName);
        }

        /// <summary>
        /// Visit system for dependency sorting (topological sort)
        /// </summary>
        private bool VisitSystemForDependencySort(string systemName, HashSet<string> visited, HashSet<string> visiting, List<string> result)
        {
            if (visiting.Contains(systemName))
                return false; // Circular dependency

            if (visited.Contains(systemName))
                return true;

            visiting.Add(systemName);

            if (_systemDependencies.TryGetValue(systemName, out var dependencies))
            {
                foreach (var dependency in dependencies)
                {
                    if (!VisitSystemForDependencySort(dependency, visited, visiting, result))
                        return false;
                }
            }

            visiting.Remove(systemName);
            visited.Add(systemName);
            result.Add(systemName);

            return true;
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// System category enumeration
    /// </summary>
    public enum SystemCategory
    {
        Core,
        Performance,
        Cultivation,
        Construction,
        Economy,
        UserInterface,
        Rendering,
        Other
    }

    /// <summary>
    /// System registration data
    /// </summary>
    [System.Serializable]
    public struct SystemRegistrationData
    {
        public string SystemName;
        public float RegistrationTime;
        public SystemCategory Category;
        public bool IsActive;
    }

    /// <summary>
    /// System registry statistics
    /// </summary>
    [System.Serializable]
    public struct SystemRegistryStats
    {
        public int RegisteredSystems;
        public int ActiveSystems;
        public int SystemsWithDependencies;
        public int CircularDependencies;
    }

    #endregion
}