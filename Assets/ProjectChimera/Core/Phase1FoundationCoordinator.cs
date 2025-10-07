using UnityEngine;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;
using ProjectChimera.Core.Foundation;
using ProjectChimera.Core.SimpleDI;

namespace ProjectChimera.Core
{
    /// <summary>
    /// REFACTORED: Phase 1 Foundation Coordinator - Legacy wrapper for backward compatibility
    /// Delegates to FoundationCore for focused foundation subsystem coordination
    /// Maintains existing API while utilizing Single Responsibility Principle architecture
    /// </summary>
    public class Phase1FoundationCoordinator : MonoBehaviour, ITickable
    {
        [Header("Legacy Wrapper Settings")]
        [SerializeField] private bool _enableFoundationSystems = true;
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private bool _autoInitializeOnAwake = true;

        [Header("Performance Systems")]
        [SerializeField] private bool _enablePerformanceMonitoring = true;
        [SerializeField] private bool _enableMetricsCollection = true;
        [SerializeField] private bool _enablePerformanceOptimization = true;

        [Header("Advanced Systems")]
        [SerializeField] private bool _enableAdvancedCultivation = true;
        [SerializeField] private bool _enableAdvancedConstruction = true;
        [SerializeField] private bool _enableAdvancedGenetics = true;
        [SerializeField] private bool _enableAdvancedEconomy = true;

        // Core foundation system (delegation target)
        private FoundationCore _foundationCore;

        // Properties
        public bool IsInitialized => _foundationCore?.IsInitialized ?? false;
        public bool IsEnabled { get; private set; } = true;

        // ITickable implementation
        public int TickPriority => 50;
        public bool IsTickable => enabled && gameObject.activeInHierarchy && IsEnabled;

        public void Tick(float deltaTime)
        {
            // Delegation handled automatically by FoundationCore's ITickable implementation
        }

        // Events
        public System.Action OnFoundationInitialized;
        public System.Action<IFoundationSystem> OnSystemRegistered;
        public System.Action<string> OnSystemHealthChanged;

        private void Awake()
        {
            if (_autoInitializeOnAwake)
            {
                Initialize();
            }
        }

        private void Start()
        {
            if (!IsInitialized && !_autoInitializeOnAwake)
            {
                Initialize();
            }
        }

        /// <summary>
        /// Initialize foundation coordinator
        /// </summary>
        public void Initialize()
        {
            InitializeFoundationCore();

            if (_enableLogging)
                ChimeraLogger.Log("FOUNDATION", "✅ Phase1FoundationCoordinator initialized", this);
        }

        /// <summary>
        /// Register foundation system - delegates to FoundationCore
        /// </summary>
        public bool RegisterFoundationSystem(IFoundationSystem system)
        {
            return _foundationCore?.RegisterFoundationSystem(system) ?? false;
        }

        /// <summary>
        /// Initialize foundation system - delegates to FoundationCore
        /// </summary>
        public bool InitializeFoundationSystem(string systemName)
        {
            return _foundationCore?.InitializeFoundationSystem(systemName) ?? false;
        }

        /// <summary>
        /// Initialize all foundation systems - delegates to FoundationCore
        /// </summary>
        public bool InitializeAllSystems()
        {
            return _foundationCore?.InitializeAllSystems() ?? false;
        }

        /// <summary>
        /// Get foundation system health - delegates to FoundationCore
        /// </summary>
        public SystemHealth GetSystemHealth(string systemName)
        {
            return _foundationCore?.GetSystemHealth(systemName) ?? SystemHealth.Unknown;
        }

        /// <summary>
        /// Trigger system recovery - delegates to FoundationCore
        /// </summary>
        public bool TriggerSystemRecovery(string systemName)
        {
            return _foundationCore?.TriggerSystemRecovery(systemName) ?? false;
        }

        /// <summary>
        /// Get registered systems - delegates to FoundationCore
        /// </summary>
        public IFoundationSystem[] GetRegisteredSystems()
        {
            return _foundationCore?.GetRegisteredSystems() ?? new IFoundationSystem[0];
        }

        /// <summary>
        /// Get foundation system by name - delegates to FoundationCore
        /// </summary>
        public IFoundationSystem GetSystem(string systemName)
        {
            return _foundationCore?.GetSystem(systemName);
        }

        /// <summary>
        /// Get foundation statistics - delegates to FoundationCore
        /// </summary>
        public FoundationStats GetFoundationStats()
        {
            return _foundationCore?.GetCombinedStats() ?? new FoundationStats();
        }

        /// <summary>
        /// Set foundation systems enabled/disabled - delegates to FoundationCore
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;
            _foundationCore?.SetEnabled(enabled);

            if (_enableLogging)
                ChimeraLogger.Log("FOUNDATION", $"Phase1FoundationCoordinator: {(enabled ? "enabled" : "disabled")}", this);
        }

        /// <summary>
        /// Initialize core foundation system
        /// </summary>
        private void InitializeFoundationCore()
        {
            var coreGO = new GameObject("FoundationCore");
            coreGO.transform.SetParent(transform);
            _foundationCore = coreGO.AddComponent<FoundationCore>();

            // Setup event delegation
            _foundationCore.OnFoundationInitialized += () => OnFoundationInitialized?.Invoke();
            _foundationCore.OnSystemRegistered += (system) => OnSystemRegistered?.Invoke(system);
            _foundationCore.OnSystemHealthChanged += (systemName) => OnSystemHealthChanged?.Invoke(systemName);

            // Register self with UpdateOrchestrator via ServiceContainer
            var updateOrchestrator = ServiceContainerFactory.Instance?.TryResolve<UpdateOrchestrator>();
            if (updateOrchestrator != null)
            {
                updateOrchestrator.RegisterTickable(this);
            }
            else if (_enableLogging)
            {
                ChimeraLogger.LogWarning("FOUNDATION",
                    "UpdateOrchestrator not found in ServiceContainer - tickable registration skipped", this);
            }

            if (_enableLogging)
                ChimeraLogger.Log("FOUNDATION", "FoundationCore initialized and event delegation setup complete", this);
        }

        /// <summary>
        /// Legacy method for backward compatibility
        /// </summary>
        [System.Obsolete("Use RegisterFoundationSystem instead")]
        public bool RegisterSystem(IFoundationSystem system)
        {
            return RegisterFoundationSystem(system);
        }

        /// <summary>
        /// Legacy method for backward compatibility
        /// </summary>
        [System.Obsolete("Use GetFoundationStats instead")]
        public FoundationStats GetStats()
        {
            return GetFoundationStats();
        }

        /// <summary>
        /// Legacy method for backward compatibility
        /// </summary>
        [System.Obsolete("Use InitializeFoundationSystem instead")]
        public bool InitializeSystem(string systemName)
        {
            return InitializeFoundationSystem(systemName);
        }

        private void OnDestroy()
        {
            // Unregister from UpdateOrchestrator via ServiceContainer
            var updateOrchestrator = ServiceContainerFactory.Instance?.TryResolve<UpdateOrchestrator>();
            updateOrchestrator?.UnregisterTickable(this);
        }
    }

    #region Legacy Data Structures for Backward Compatibility

    /// <summary>
    /// Legacy initialization status - maintained for backward compatibility
    /// </summary>
    [System.Serializable]
    public enum Phase1InitializationStatus
    {
        NotStarted,
        Initializing,
        PartiallyInitialized,
        FullyInitialized,
        Failed
    }

    /// <summary>
    /// Legacy system health status - maintained for backward compatibility
    /// </summary>
    [System.Serializable]
    public struct SystemHealthStatus
    {
        public string SystemName;
        public SystemHealth Health;
        public float CheckTime;
        public string StatusMessage;
    }

    #endregion
}