using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core;
using ProjectChimera.Core.Updates;
using ProjectChimera.Core.Memory.GCOptimization;
using GCOptCore = ProjectChimera.Core.Memory.GCOptimization.GCCore;

namespace ProjectChimera.Core.Memory
{
    /// <summary>
    /// REFACTORED: GC Optimization Manager - Legacy wrapper for backward compatibility
    /// Delegates to specialized GCCore for all GC optimization coordination
    /// Single Responsibility: Backward compatibility delegation
    /// </summary>
    public class GCOptimizationManager : MonoBehaviour, ITickable
    {
        [Header("Legacy Compatibility Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private bool _enableLegacyMode = true;

        // Delegation target - the actual GC optimization system
        private GCOptCore _gcCore;

        // Legacy properties for backward compatibility
        public bool IsInitialized => _gcCore != null;
        public GCOptimizationStats Stats { get { return _gcCore != null ? ConvertCoreStats(_gcCore.Stats) : new GCOptimizationStats(); } }

        // Legacy events for backward compatibility
        public System.Action<GCResult> OnGCCompleted;
        public System.Action OnIdleStateChanged;
        public System.Action<GCOptimizationHealth> OnHealthChanged;

        public enum GCStrategy
        {
            Disabled,           // No automatic GC management
            Conservative,       // Only GC during idle and scene transitions
            Adaptive,          // Smart GC based on memory pressure and usage patterns
            Aggressive         // Frequent GC to minimize memory usage
        }

        // Legacy singleton pattern for backward compatibility
        private static GCOptimizationManager _instance;
        public static GCOptimizationManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = ServiceContainerFactory.Instance?.TryResolve<GCOptimizationManager>();
                    if (_instance == null)
                    {
                        var go = new GameObject("GCOptimizationManager");
                        _instance = go.AddComponent<GCOptimizationManager>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeGCCore();
                ProjectChimera.Core.Updates.UpdateOrchestrator.Instance?.RegisterTickable(this);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Initialize the GC core system
        /// </summary>
        private void InitializeGCCore()
        {
            _gcCore = GetComponent<GCOptCore>();
            if (_gcCore == null)
            {
                _gcCore = gameObject.AddComponent<GCOptCore>();
            }

            // Connect legacy events to the new core
            ConnectLegacyEvents();
        }

        /// <summary>
        /// Connect legacy events to the new GC core
        /// </summary>
        private void ConnectLegacyEvents()
        {
            if (_gcCore != null)
            {
                _gcCore.OnGCCompleted += (coreResult) =>
                {
                    var mapped = new GCResult
                    {
                        WasExecuted = coreResult.WasExecuted,
                        Duration = coreResult.Duration,
                        MemoryFreed = coreResult.MemoryFreed,
                        Reason = coreResult.Reason
                    };
                    OnGCCompleted?.Invoke(mapped);
                };
                _gcCore.OnIdleStateChanged += () => OnIdleStateChanged?.Invoke();
                _gcCore.OnHealthChanged += (health) => OnHealthChanged?.Invoke(health);
            }
        }

        /// <summary>
        /// Initialize GC optimization manager (Legacy method - delegates to GCCore)
        /// </summary>
        public void Initialize()
        {
            if (_enableLogging)
                ChimeraLogger.Log("GC", "🔄 GCOptimizationManager (Legacy) initialization delegated to GCCore", this);
        }

        public int TickPriority => 100;
        public bool IsTickable => enabled && gameObject.activeInHierarchy;

        public void Tick(float deltaTime)
        {
            if (!_enableLegacyMode) return;

            // Delegate to GC core - it handles all coordination
            // The core system automatically runs coordination via its own Tick method
            if (_enableLogging && Time.frameCount % 300 == 0) // Log every 5 seconds at 60fps
                ChimeraLogger.Log("GC", "Legacy coordinator tick delegated to GCCore", this);
        }

        /// <summary>
        /// Notify that the application is idle (Legacy method - delegates to GCCore)
        /// </summary>
        public void NotifyIdle()
        {
            _gcCore?.NotifyIdle();

            if (_enableLogging)
                ChimeraLogger.Log("GC", "Idle notification sent via legacy method", this);
        }

        /// <summary>
        /// Notify that the application is no longer idle (Legacy method - delegates to GCCore)
        /// </summary>
        public void NotifyActive()
        {
            _gcCore?.NotifyActive();

            if (_enableLogging)
                ChimeraLogger.Log("GC", "Active notification sent via legacy method", this);
        }

        /// <summary>
        /// Notify that a scene transition is starting (Legacy method - delegates to GCCore)
        /// </summary>
        public void NotifySceneTransitionStart()
        {
            _gcCore?.NotifySceneTransitionStart();

            if (_enableLogging)
                ChimeraLogger.Log("GC", "Scene transition start notification sent via legacy method", this);
        }

        /// <summary>
        /// Notify that a scene transition has ended (Legacy method - delegates to GCCore)
        /// </summary>
        public void NotifySceneTransitionEnd()
        {
            _gcCore?.NotifySceneTransitionEnd();

            if (_enableLogging)
                ChimeraLogger.Log("GC", "Scene transition end notification sent via legacy method", this);
        }

        /// <summary>
        /// Force garbage collection with optimization (Legacy method - delegates to GCCore)
        /// </summary>
        public GCResult ForceOptimizedGC(bool waitForFinalizers = true)
        {
            if (_gcCore != null)
            {
                var coreResult = _gcCore.ForceOptimizedGC(waitForFinalizers);
                var mapped = new GCResult
                {
                    WasExecuted = coreResult.WasExecuted,
                    Duration = coreResult.Duration,
                    MemoryFreed = coreResult.MemoryFreed,
                    Reason = coreResult.Reason
                };

                if (_enableLogging)
                    ChimeraLogger.Log("GC", "Force GC executed via legacy method", this);

                return mapped;
            }

            if (_enableLogging)
                ChimeraLogger.Log("GC", "Force GC failed: GCCore not available", this);

            return new GCResult { WasExecuted = false, Reason = "GCCore not available" };
        }

        /// <summary>
        /// Get current memory pressure level (Legacy method - delegates to GCCore)
        /// </summary>
        public float GetMemoryPressure()
        {
            return _gcCore?.GetMemoryPressure() ?? 0f;
        }

        /// <summary>
        /// Get GC optimization statistics (Legacy method - delegates to GCCore)
        /// </summary>
        public GCOptimizationStats GetStats()
        {
            return _gcCore != null ? ConvertCoreStats(_gcCore.Stats) : new GCOptimizationStats();
        }

        /// <summary>
        /// Change GC strategy at runtime (Legacy method - delegates to GCCore)
        /// </summary>
        public void SetGCStrategy(GCStrategy strategy)
        {
            // Convert legacy enum to new enum
            var newStrategy = ConvertLegacyStrategy(strategy);
            _gcCore?.SetGCStrategy(newStrategy);

            if (_enableLogging)
                ChimeraLogger.Log("GC", $"GC strategy set to {strategy} via legacy method", this);
        }

        /// <summary>
        /// Convert legacy GC strategy to new strategy
        /// </summary>
        private ProjectChimera.Core.Memory.GCOptimization.GCStrategy ConvertLegacyStrategy(GCStrategy legacyStrategy)
        {
            switch (legacyStrategy)
            {
                case GCStrategy.Disabled:
                    return ProjectChimera.Core.Memory.GCOptimization.GCStrategy.Disabled;
                case GCStrategy.Conservative:
                    return ProjectChimera.Core.Memory.GCOptimization.GCStrategy.Conservative;
                case GCStrategy.Adaptive:
                    return ProjectChimera.Core.Memory.GCOptimization.GCStrategy.Adaptive;
                case GCStrategy.Aggressive:
                    return ProjectChimera.Core.Memory.GCOptimization.GCStrategy.Aggressive;
                default:
                    return ProjectChimera.Core.Memory.GCOptimization.GCStrategy.Adaptive;
            }
        }

        #region Private Methods - Legacy Compatibility Helpers

        /// <summary>
        /// Legacy method - no longer needed as GCCore handles all coordination
        /// </summary>
        [System.Obsolete("Use GCCore instead", false)]
        private void CheckMemoryPressure()
        {
            // All memory pressure monitoring is now handled by GCMemoryPressureMonitor
        }

        /// <summary>
        /// Legacy method - no longer needed as GCIdleStateMonitor handles idle detection
        /// </summary>
        [System.Obsolete("Use GCIdleStateMonitor instead", false)]
        private void UpdateIdleState()
        {
            // All idle state detection is now handled by GCIdleStateMonitor
        }

        /// <summary>
        /// Legacy method - no longer needed as GCStrategyManager handles strategy processing
        /// </summary>
        [System.Obsolete("Use GCStrategyManager instead", false)]
        private void ProcessGCStrategy()
        {
            // All strategy processing is now handled by GCStrategyManager
        }

        /// <summary>
        /// Legacy method - no longer needed as GCExecutor handles execution timing
        /// </summary>
        [System.Obsolete("Use GCExecutor instead", false)]
        private bool CanPerformGC()
        {
            return _gcCore != null ? _gcCore.Stats.LastGCTime > 0f : false;
        }

        private GCOptimizationStats ConvertCoreStats(ProjectChimera.Core.Memory.GCOptimization.GCOptimizationStats core)
        {
            return new GCOptimizationStats
            {
                Strategy = ConvertToLegacyStrategy(core.Strategy),
                AutomaticGCCount = core.TotalGCExecutions,
                TotalGCTime = core.TotalGCTime,
                TotalMemoryFreed = core.TotalMemoryFreed,
                AverageGCTime = core.AverageGCTime,
                CurrentMemoryPressure = core.CurrentMemoryPressure,
                IsIdle = core.IsIdle,
                IsSceneTransitioning = core.IsSceneTransitioning,
                RecentGCEvents = new List<GCEvent>()
            };
        }

        private GCStrategy ConvertToLegacyStrategy(ProjectChimera.Core.Memory.GCOptimization.GCStrategy strategy)
        {
            switch (strategy)
            {
                case ProjectChimera.Core.Memory.GCOptimization.GCStrategy.Disabled:
                    return GCStrategy.Disabled;
                case ProjectChimera.Core.Memory.GCOptimization.GCStrategy.Conservative:
                    return GCStrategy.Conservative;
                case ProjectChimera.Core.Memory.GCOptimization.GCStrategy.Adaptive:
                    return GCStrategy.Adaptive;
                case ProjectChimera.Core.Memory.GCOptimization.GCStrategy.Aggressive:
                    return GCStrategy.Aggressive;
                default:
                    return GCStrategy.Adaptive;
            }
        }

        /// <summary>
        /// Legacy method - no longer needed as GCExecutor handles GC execution
        /// </summary>
        [System.Obsolete("Use GCExecutor instead", false)]
        private GCResult ExecuteGC(bool waitForFinalizers, string reason)
        {
            return new GCResult { WasExecuted = false, Reason = "Now handled by GCExecutor" };
        }

        #endregion

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                NotifyIdle();
            }
            else
            {
                NotifyActive();
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus)
            {
                NotifyActive();
            }
            else
            {
                NotifyIdle();
            }
        }

        private void OnDestroy()
        {
            UpdateOrchestrator.Instance?.UnregisterTickable(this);
        }
    }

    #region Data Structures - Legacy Compatibility

    /// <summary>
    /// Legacy GC result (use GCResult from GCOptimization namespace instead)
    /// </summary>
    [System.Serializable]
    public struct GCResult
    {
        public bool WasExecuted;
        public float Duration;
        public long MemoryFreed;
        public string Reason;
    }

    /// <summary>
    /// Legacy GC event (use GCEvent from GCOptimization namespace instead)
    /// </summary>
    [System.Serializable]
    [System.Obsolete("Use GCEvent from ProjectChimera.Core.Memory.GCOptimization namespace instead")]
    public struct GCEvent
    {
        public float Timestamp;
        public float Duration;
        public long MemoryFreed;
        public string Reason;
    }

    /// <summary>
    /// Legacy GC optimization stats (use GCOptimizationStats from GCOptimization namespace instead)
    /// </summary>
    [System.Serializable]
    public struct GCOptimizationStats
    {
        public GCOptimizationManager.GCStrategy Strategy;
        public int AutomaticGCCount;
        public float TotalGCTime;
        public long TotalMemoryFreed;
        public float AverageGCTime;
        public float CurrentMemoryPressure;
        public bool IsIdle;
        public bool IsSceneTransitioning;
        public List<GCEvent> RecentGCEvents;
    }

    #endregion
}
