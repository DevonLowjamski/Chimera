using UnityEngine;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core.Memory.GCOptimization
{
    /// <summary>
    /// REFACTORED: GC Strategy Manager - Focused GC strategy selection and execution logic
    /// Handles different GC strategies (Conservative, Adaptive, Aggressive) and determines when GC should occur
    /// Single Responsibility: GC strategy management and decision-making
    /// </summary>
    public class GCStrategyManager : MonoBehaviour
    {
        [Header("Strategy Settings")]
        [SerializeField] private GCStrategy _gcStrategy = GCStrategy.Adaptive;
        [SerializeField] private bool _enableLogging = false;

        [Header("Strategy Thresholds")]
        [SerializeField] private float _memoryPressureThreshold = 0.8f; // 80% of available memory
        [SerializeField] private long _forceGCThreshold = 100 * 1024 * 1024; // 100MB
        [SerializeField] private int _allocationRateThreshold = 50 * 1024 * 1024; // 50MB/s

        [Header("Strategy-Specific Settings")]
        [SerializeField] private float _aggressiveMemoryThreshold = 0.5f; // Aggressive: 50%
        [SerializeField] private float _conservativeMemoryThreshold = 0.9f; // Conservative: 90%

        // Properties
        public bool IsEnabled { get; private set; } = true;
        public GCStrategy CurrentStrategy => _gcStrategy;

        // Events
        public System.Action<GCStrategy, GCStrategy> OnStrategyChanged;
        public System.Action<string> OnGCRecommended;

        /// <summary>
        /// Evaluate if GC should be performed based on current strategy
        /// </summary>
        public GCDecision EvaluateGCNeed(float memoryPressure, long allocationRate, long currentMemory)
        {
            if (!IsEnabled || _gcStrategy == GCStrategy.Disabled)
                return new GCDecision { ShouldPerformGC = false, Reason = "Strategy disabled" };

            switch (_gcStrategy)
            {
                case GCStrategy.Aggressive:
                    return EvaluateAggressiveStrategy(memoryPressure, allocationRate, currentMemory);

                case GCStrategy.Adaptive:
                    return EvaluateAdaptiveStrategy(memoryPressure, allocationRate, currentMemory);

                case GCStrategy.Conservative:
                    return EvaluateConservativeStrategy(memoryPressure, allocationRate, currentMemory);

                default:
                    return new GCDecision { ShouldPerformGC = false, Reason = "Unknown strategy" };
            }
        }

        /// <summary>
        /// Get recommended GC mode for current strategy
        /// </summary>
        public GCExecutionMode GetRecommendedGCMode(GCContext context)
        {
            switch (context.Type)
            {
                case GCTriggerType.Idle:
                    return GCExecutionMode.Thorough; // Wait for finalizers during idle

                case GCTriggerType.SceneTransition:
                    return GCExecutionMode.Thorough; // Clean up during transitions

                case GCTriggerType.MemoryPressure:
                    return _gcStrategy == GCStrategy.Aggressive ? GCExecutionMode.Fast : GCExecutionMode.Standard;

                case GCTriggerType.Manual:
                    return GCExecutionMode.Thorough;

                default:
                    return GCExecutionMode.Standard;
            }
        }

        /// <summary>
        /// Set GC strategy
        /// </summary>
        public void SetStrategy(GCStrategy strategy)
        {
            var previousStrategy = _gcStrategy;
            _gcStrategy = strategy;

            OnStrategyChanged?.Invoke(previousStrategy, _gcStrategy);

            if (_enableLogging)
                ChimeraLogger.Log("GC", $"Strategy changed: {previousStrategy} -> {_gcStrategy}", this);
        }

        /// <summary>
        /// Get strategy configuration details
        /// </summary>
        public GCStrategyConfig GetStrategyConfig()
        {
            return new GCStrategyConfig
            {
                Strategy = _gcStrategy,
                MemoryPressureThreshold = _memoryPressureThreshold,
                ForceGCThreshold = _forceGCThreshold,
                AllocationRateThreshold = _allocationRateThreshold,
                IsEnabled = IsEnabled
            };
        }

        /// <summary>
        /// Set system enabled/disabled
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;

            if (_enableLogging)
                ChimeraLogger.Log("GC", $"GCStrategyManager: {(enabled ? "enabled" : "disabled")}", this);
        }

        #region Private Strategy Evaluation Methods

        /// <summary>
        /// Evaluate aggressive GC strategy
        /// </summary>
        private GCDecision EvaluateAggressiveStrategy(float memoryPressure, long allocationRate, long currentMemory)
        {
            if (memoryPressure > _aggressiveMemoryThreshold || allocationRate > _allocationRateThreshold / 2)
            {
                return new GCDecision
                {
                    ShouldPerformGC = true,
                    Reason = "Aggressive strategy threshold",
                    Priority = GCPriority.High
                };
            }

            return new GCDecision { ShouldPerformGC = false, Reason = "Aggressive thresholds not met" };
        }

        /// <summary>
        /// Evaluate adaptive GC strategy
        /// </summary>
        private GCDecision EvaluateAdaptiveStrategy(float memoryPressure, long allocationRate, long currentMemory)
        {
            if (memoryPressure > _memoryPressureThreshold ||
                allocationRate > _allocationRateThreshold ||
                currentMemory > _forceGCThreshold)
            {
                var priority = memoryPressure > 0.9f ? GCPriority.Critical :
                              memoryPressure > 0.7f ? GCPriority.High : GCPriority.Medium;

                return new GCDecision
                {
                    ShouldPerformGC = true,
                    Reason = "Adaptive strategy threshold",
                    Priority = priority
                };
            }

            return new GCDecision { ShouldPerformGC = false, Reason = "Adaptive thresholds not met" };
        }

        /// <summary>
        /// Evaluate conservative GC strategy
        /// </summary>
        private GCDecision EvaluateConservativeStrategy(float memoryPressure, long allocationRate, long currentMemory)
        {
            if (memoryPressure > _conservativeMemoryThreshold || currentMemory > _forceGCThreshold * 2)
            {
                return new GCDecision
                {
                    ShouldPerformGC = true,
                    Reason = "Conservative strategy threshold",
                    Priority = GCPriority.Critical
                };
            }

            return new GCDecision { ShouldPerformGC = false, Reason = "Conservative thresholds not met" };
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// GC strategy enumeration
    /// </summary>
    public enum GCStrategy
    {
        Disabled,           // No automatic GC management
        Conservative,       // Only GC during idle and scene transitions
        Adaptive,          // Smart GC based on memory pressure and usage patterns
        Aggressive         // Frequent GC to minimize memory usage
    }

    /// <summary>
    /// GC execution modes
    /// </summary>
    public enum GCExecutionMode
    {
        Fast,       // GC.Collect() only
        Standard,   // GC.Collect() with minimal wait
        Thorough    // GC.Collect() + WaitForPendingFinalizers + GC.Collect()
    }

    /// <summary>
    /// GC trigger types
    /// </summary>
    public enum GCTriggerType
    {
        Manual,
        Idle,
        SceneTransition,
        MemoryPressure,
        AllocationRate
    }

    /// <summary>
    /// GC priority levels
    /// </summary>
    public enum GCPriority
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// GC decision result
    /// </summary>
    [System.Serializable]
    public struct GCDecision
    {
        public bool ShouldPerformGC;
        public string Reason;
        public GCPriority Priority;
    }

    /// <summary>
    /// GC execution context
    /// </summary>
    [System.Serializable]
    public struct GCContext
    {
        public GCTriggerType Type;
        public float MemoryPressure;
        public long AllocationRate;
        public bool IsIdle;
        public bool IsSceneTransition;
    }

    /// <summary>
    /// GC strategy configuration
    /// </summary>
    [System.Serializable]
    public struct GCStrategyConfig
    {
        public GCStrategy Strategy;
        public float MemoryPressureThreshold;
        public long ForceGCThreshold;
        public int AllocationRateThreshold;
        public bool IsEnabled;
    }

    #endregion
}