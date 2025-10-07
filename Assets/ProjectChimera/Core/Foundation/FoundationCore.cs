using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;

namespace ProjectChimera.Core.Foundation
{
    /// <summary>
    /// REFACTORED: Foundation Core - Central coordination for Phase 1 foundation subsystems
    /// Manages initialization, health monitoring, recovery systems, and performance coordination
    /// Follows Single Responsibility Principle with focused subsystem coordination
    /// </summary>
    public class FoundationCore : MonoBehaviour, ITickable
    {
        [Header("Core Foundation Settings")]
        [SerializeField] private bool _enableFoundationSystems = true;
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private float _systemUpdateInterval = 1f;
        [SerializeField] private bool _enableHealthMonitoring = true;

        // Core subsystems
        private FoundationSystemRegistry _systemRegistry;
        private FoundationInitializer _initializer;
        private FoundationHealthMonitor _healthMonitor;
        private FoundationRecoveryManager _recoveryManager;
        private FoundationPerformanceCoordinator _performanceCoordinator;

        // Timing
        private float _lastUpdateTime;

        // Properties
        public bool IsEnabled { get; private set; } = true;
        public bool IsInitialized { get; private set; } = false;

        // Statistics aggregation
        public FoundationStats GetCombinedStats()
        {
            var stats = new FoundationStats();

            if (_systemRegistry != null)
            {
                var registryStats = _systemRegistry.GetStats();
                stats.RegisteredSystems = registryStats.RegisteredSystems;
                stats.ActiveSystems = registryStats.ActiveSystems;
            }

            if (_initializer != null)
            {
                var initStats = _initializer.GetStats();
                stats.InitializedSystems = initStats.InitializedSystems;
                stats.FailedInitializations = initStats.FailedInitializations;
            }

            if (_healthMonitor != null)
            {
                var healthStats = _healthMonitor.GetStats();
                stats.HealthySystems = healthStats.HealthySystems;
                stats.UnhealthySystems = healthStats.UnhealthySystems;
            }

            if (_recoveryManager != null)
            {
                var recoveryStats = _recoveryManager.GetStats();
                stats.RecoveryAttempts = recoveryStats.RecoveryAttempts;
                stats.SuccessfulRecoveries = recoveryStats.SuccessfulRecoveries;
            }

            if (_performanceCoordinator != null)
            {
                var perfStats = _performanceCoordinator.GetStats();
                stats.PerformanceScore = perfStats.PerformanceScore;
                stats.IsPerformingWell = perfStats.IsPerformingWell;
            }

            return stats;
        }

        // Events
        public System.Action OnFoundationInitialized;
        public System.Action<IFoundationSystem> OnSystemRegistered;
        public System.Action<IFoundationSystem> OnSystemInitialized;
        public System.Action<string> OnSystemHealthChanged;
        public System.Action<FoundationStats> OnStatsUpdated;

        public int TickPriority => 50;
        public bool IsTickable => enabled && gameObject.activeInHierarchy && IsEnabled;

        public void Tick(float deltaTime)
        {
            if (!IsEnabled || !_enableFoundationSystems) return;

            if (Time.time - _lastUpdateTime >= _systemUpdateInterval)
            {
                ProcessFoundationUpdate();
                _lastUpdateTime = Time.time;
            }
        }

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (_enableLogging)
                ChimeraLogger.Log("FOUNDATION", "🏗️ Initializing FoundationCore subsystems...", this);

            // Initialize subsystems in dependency order
            InitializeSystemRegistry();
            InitializeFoundationInitializer();
            InitializeHealthMonitor();
            InitializeRecoveryManager();
            InitializePerformanceCoordinator();

            IsInitialized = true;
            OnFoundationInitialized?.Invoke();

            if (_enableLogging)
                ChimeraLogger.Log("FOUNDATION", "✅ FoundationCore initialized with all subsystems", this);
        }

        private void InitializeSystemRegistry()
        {
            var registryGO = new GameObject("FoundationSystemRegistry");
            registryGO.transform.SetParent(transform);
            _systemRegistry = registryGO.AddComponent<FoundationSystemRegistry>();

            _systemRegistry.OnSystemRegistered += (system) => OnSystemRegistered?.Invoke(system);
        }

        private void InitializeFoundationInitializer()
        {
            var initializerGO = new GameObject("FoundationInitializer");
            initializerGO.transform.SetParent(transform);
            _initializer = initializerGO.AddComponent<FoundationInitializer>();

            _initializer.OnSystemInitialized += (system) => OnSystemInitialized?.Invoke(system);
        }

        private void InitializeHealthMonitor()
        {
            var healthGO = new GameObject("FoundationHealthMonitor");
            healthGO.transform.SetParent(transform);
            _healthMonitor = healthGO.AddComponent<FoundationHealthMonitor>();

            _healthMonitor.OnHealthChanged += (systemName, health) => OnSystemHealthChanged?.Invoke(systemName);
        }

        private void InitializeRecoveryManager()
        {
            var recoveryGO = new GameObject("FoundationRecoveryManager");
            recoveryGO.transform.SetParent(transform);
            _recoveryManager = recoveryGO.AddComponent<FoundationRecoveryManager>();
        }

        private void InitializePerformanceCoordinator()
        {
            var perfGO = new GameObject("FoundationPerformanceCoordinator");
            perfGO.transform.SetParent(transform);
            _performanceCoordinator = perfGO.AddComponent<FoundationPerformanceCoordinator>();
        }

        /// <summary>
        /// Coordinate all foundation subsystem updates
        /// </summary>
        private void ProcessFoundationUpdate()
        {
            // Update system health monitoring
            if (_healthMonitor != null && _enableHealthMonitoring)
            {
                _healthMonitor.MonitorSystemHealth();
            }

            // Process recovery operations
            if (_recoveryManager != null)
            {
                _recoveryManager.ProcessRecoveryOperations();
            }

            // Update performance coordination
            if (_performanceCoordinator != null)
            {
                _performanceCoordinator.CoordinatePerformanceOptimizations();
            }

            // Fire stats update event
            OnStatsUpdated?.Invoke(GetCombinedStats());
        }

        /// <summary>
        /// Register foundation system through SystemRegistry
        /// </summary>
        public bool RegisterFoundationSystem(IFoundationSystem system)
        {
            return _systemRegistry?.RegisterSystem(system) ?? false;
        }

        /// <summary>
        /// Initialize foundation system through Initializer
        /// </summary>
        public bool InitializeFoundationSystem(string systemName)
        {
            return _initializer?.InitializeSystem(systemName) ?? false;
        }

        /// <summary>
        /// Initialize all foundation systems
        /// </summary>
        public bool InitializeAllSystems()
        {
            return _initializer?.InitializeAllSystems() ?? false;
        }

        /// <summary>
        /// Get system health through HealthMonitor
        /// </summary>
        public SystemHealth GetSystemHealth(string systemName)
        {
            return _healthMonitor?.GetSystemHealth(systemName) ?? SystemHealth.Unknown;
        }

        /// <summary>
        /// Trigger system recovery through RecoveryManager
        /// </summary>
        public bool TriggerSystemRecovery(string systemName)
        {
            return _recoveryManager?.TriggerRecovery(systemName) ?? false;
        }

        /// <summary>
        /// Get registered systems through SystemRegistry
        /// </summary>
        public IFoundationSystem[] GetRegisteredSystems()
        {
            return _systemRegistry?.GetRegisteredSystems() ?? new IFoundationSystem[0];
        }

        /// <summary>
        /// Get system by name through SystemRegistry
        /// </summary>
        public IFoundationSystem GetSystem(string systemName)
        {
            return _systemRegistry?.GetSystem(systemName);
        }

        /// <summary>
        /// Set system enabled/disabled
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;

            if (_systemRegistry != null) _systemRegistry.SetEnabled(enabled);
            if (_initializer != null) _initializer.SetEnabled(enabled);
            if (_healthMonitor != null) _healthMonitor.SetEnabled(enabled);
            if (_recoveryManager != null) _recoveryManager.SetEnabled(enabled);
            if (_performanceCoordinator != null) _performanceCoordinator.SetEnabled(enabled);

            if (_enableLogging)
                ChimeraLogger.Log("FOUNDATION", $"FoundationCore: {(enabled ? "enabled" : "disabled")}", this);
        }
    }

    #region Data Structures

    /// <summary>
    /// Foundation system interface
    /// </summary>
    public interface IFoundationSystem
    {
        string SystemName { get; }
        bool IsInitialized { get; }
        bool IsEnabled { get; }
        SystemHealth Health { get; }

        bool Initialize();
        void Shutdown();
        void SetEnabled(bool enabled);
        SystemHealth CheckHealth();
    }

    /// <summary>
    /// System health enumeration
    /// </summary>
    public enum SystemHealth
    {
        Unknown,
        Healthy,
        Warning,
        Critical,
        Failed
    }

    /// <summary>
    /// Foundation statistics
    /// </summary>
    [System.Serializable]
    public struct FoundationStats
    {
        public int RegisteredSystems;
        public int ActiveSystems;
        public int InitializedSystems;
        public int FailedInitializations;
        public int HealthySystems;
        public int UnhealthySystems;
        public int RecoveryAttempts;
        public int SuccessfulRecoveries;
        public float PerformanceScore;
        public bool IsPerformingWell;
    }

    #endregion
}
