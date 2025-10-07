using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core.Initialization
{
    /// <summary>
    /// REFACTORED: Manager Discovery Service - Focused manager discovery and registration
    /// Single Responsibility: Discovering and cataloging available ChimeraManager instances
    /// Extracted from GameSystemInitializer for better SRP compliance
    /// </summary>
    public class ManagerDiscoveryService
    {
        private readonly bool _enableLogging;
        private readonly bool _autoDiscoverManagers;

        // Discovery state
        private readonly Dictionary<Type, ChimeraManager> _discoveredManagers = new Dictionary<Type, ChimeraManager>();
        private readonly List<ChimeraManager> _discoveryOrder = new List<ChimeraManager>();

        // Events
        public event System.Action<ChimeraManager> OnManagerDiscovered;
        public event System.Action<int> OnDiscoveryCompleted;

        public ManagerDiscoveryService(bool enableLogging = true, bool autoDiscoverManagers = true)
        {
            _enableLogging = enableLogging;
            _autoDiscoverManagers = autoDiscoverManagers;
        }

        // Properties
        public int DiscoveredManagerCount => _discoveredManagers.Count;
        public IEnumerable<ChimeraManager> DiscoveredManagers => _discoveredManagers.Values;
        public Dictionary<Type, ChimeraManager> DiscoveredManagersByType => new Dictionary<Type, ChimeraManager>(_discoveredManagers);

        #region Discovery Operations

        /// <summary>
        /// Discover all available ChimeraManager instances
        /// </summary>
        public DiscoveryResult DiscoverAllManagers()
        {
            if (_enableLogging)
                ChimeraLogger.LogInfo("INIT", "Discovering all ChimeraManager instances", null);

            var result = new DiscoveryResult();
            var startTime = DateTime.Now;

            try
            {
                ChimeraManager[] allManagers;

                // Use ServiceContainer for unified manager discovery
                if (ServiceContainerFactory.Instance != null)
                {
                    allManagers = ServiceContainerFactory.Instance.ResolveAll<ChimeraManager>().ToArray();
                    if (_enableLogging)
                        ChimeraLogger.LogInfo("INIT", $"Discovered managers via ServiceContainer: {allManagers.Length}", null);
                }
                else
                {
                    // No ServiceContainer available - managers must self-register
                    allManagers = new ChimeraManager[0];
                    if (_enableLogging)
                    {
                        ChimeraLogger.LogWarning("INIT",
                            "ServiceContainer not available - no managers discovered. " +
                            "Managers must self-register via ServiceContainer.RegisterSingleton() in Awake()", null);
                    }
                }

                // Process discovered managers
                foreach (var manager in allManagers)
                {
                    if (RegisterDiscoveredManager(manager))
                    {
                        result.SuccessfullyDiscovered++;
                        OnManagerDiscovered?.Invoke(manager);
                    }
                    else
                    {
                        result.FailedDiscovery++;
                    }
                }

                result.Success = true;
                result.TotalManagers = allManagers.Length;
                result.DiscoveryTime = DateTime.Now - startTime;

                if (_enableLogging)
                {
                    ChimeraLogger.LogInfo("INIT",
                        $"Manager discovery completed: {result.SuccessfullyDiscovered}/{result.TotalManagers} " +
                        $"managers in {result.DiscoveryTime.TotalMilliseconds:F1}ms", null);
                }

                OnDiscoveryCompleted?.Invoke(result.SuccessfullyDiscovered);
                return result;
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = ex.Message;
                ChimeraLogger.LogError("INIT", $"Manager discovery failed: {ex.Message}", null);
                return result;
            }
        }

        /// <summary>
        /// Register a discovered manager
        /// </summary>
        private bool RegisterDiscoveredManager(ChimeraManager manager)
        {
            if (manager == null) return false;

            var managerType = manager.GetType();

            // Avoid duplicate registration
            if (_discoveredManagers.ContainsKey(managerType))
            {
                if (_enableLogging)
                    ChimeraLogger.LogWarning("INIT", $"Manager {managerType.Name} already registered, skipping", null);
                return false;
            }

            // Register the manager
            _discoveredManagers[managerType] = manager;
            _discoveryOrder.Add(manager);

            if (_enableLogging)
                ChimeraLogger.LogInfo("INIT", $"Registered manager: {managerType.Name}", null);

            return true;
        }

        #endregion

        #region Query Operations

        /// <summary>
        /// Get a specific manager by type
        /// </summary>
        public T GetManager<T>() where T : ChimeraManager
        {
            return _discoveredManagers.TryGetValue(typeof(T), out var manager) ? manager as T : null;
        }

        /// <summary>
        /// Check if a manager type has been discovered
        /// </summary>
        public bool HasManager<T>() where T : ChimeraManager
        {
            return _discoveredManagers.ContainsKey(typeof(T));
        }

        /// <summary>
        /// Get managers by category
        /// </summary>
        public IEnumerable<ChimeraManager> GetManagersByCategory(ManagerCategory category)
        {
            return _discoveredManagers.Values.Where(m => GetManagerCategory(m) == category);
        }

        /// <summary>
        /// Determine manager category for initialization phases
        /// </summary>
        private ManagerCategory GetManagerCategory(ChimeraManager manager)
        {
            var managerType = manager.GetType();

            // Core systems (must be initialized first)
            if (IsCoreManger(managerType))
                return ManagerCategory.Core;

            // Domain systems (game-specific systems)
            if (IsDomainManager(managerType))
                return ManagerCategory.Domain;

            // Progression systems
            if (IsProgressionManager(managerType))
                return ManagerCategory.Progression;

            // UI systems (initialized last)
            if (IsUIManager(managerType))
                return ManagerCategory.UI;

            // Default to domain
            return ManagerCategory.Domain;
        }

        private bool IsCoreManger(Type managerType)
        {
            var coreManagerNames = new[] { "ServiceManager", "EventManager", "TimeManager", "DataManager", "SceneManager" };
            return coreManagerNames.Any(name => managerType.Name.Contains(name));
        }

        private bool IsDomainManager(Type managerType)
        {
            var domainManagerNames = new[] { "CultivationManager", "ConstructionManager", "EconomyManager", "FacilityManager" };
            return domainManagerNames.Any(name => managerType.Name.Contains(name));
        }

        private bool IsProgressionManager(Type managerType)
        {
            var progressionManagerNames = new[] { "ProgressionManager", "SaveManager", "OfflineProgression" };
            return progressionManagerNames.Any(name => managerType.Name.Contains(name));
        }

        private bool IsUIManager(Type managerType)
        {
            var uiManagerNames = new[] { "UIManager", "MenuManager", "HUDManager", "NotificationManager" };
            return uiManagerNames.Any(name => managerType.Name.Contains(name));
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Clear all discovered managers
        /// </summary>
        public void ClearDiscoveredManagers()
        {
            _discoveredManagers.Clear();
            _discoveryOrder.Clear();

            if (_enableLogging)
                ChimeraLogger.LogInfo("INIT", "Cleared all discovered managers", null);
        }

        /// <summary>
        /// Get discovery statistics
        /// </summary>
        public DiscoveryStatistics GetStatistics()
        {
            return new DiscoveryStatistics
            {
                TotalDiscovered = _discoveredManagers.Count,
                CoreManagers = GetManagersByCategory(ManagerCategory.Core).Count(),
                DomainManagers = GetManagersByCategory(ManagerCategory.Domain).Count(),
                ProgressionManagers = GetManagersByCategory(ManagerCategory.Progression).Count(),
                UIManagers = GetManagersByCategory(ManagerCategory.UI).Count()
            };
        }

        #endregion
    }

    /// <summary>
    /// Manager categories for initialization phases
    /// </summary>
    public enum ManagerCategory
    {
        Core,
        Domain,
        Progression,
        UI
    }

    /// <summary>
    /// Discovery operation result
    /// </summary>
    [System.Serializable]
    public struct DiscoveryResult
    {
        public bool Success;
        public int TotalManagers;
        public int SuccessfullyDiscovered;
        public int FailedDiscovery;
        public TimeSpan DiscoveryTime;
        public string ErrorMessage;
    }

    /// <summary>
    /// Discovery statistics
    /// </summary>
    [System.Serializable]
    public struct DiscoveryStatistics
    {
        public int TotalDiscovered;
        public int CoreManagers;
        public int DomainManagers;
        public int ProgressionManagers;
        public int UIManagers;
    }
}