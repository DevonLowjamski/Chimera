using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using System.Linq;

namespace ProjectChimera.Systems.UI.Performance
{
    /// <summary>
    /// REFACTORED: UI Optimization Manager - Focused UI optimization and performance enhancement
    /// Handles optimization level management, automatic optimizations, and performance tuning
    /// Single Responsibility: UI optimization and performance improvement
    /// </summary>
    public class UIOptimizationManager : MonoBehaviour
    {
        [Header("Optimization Settings")]
        [SerializeField] private bool _enableOptimization = true;
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private UIOptimizationLevel _currentOptimizationLevel = UIOptimizationLevel.Balanced;
        [SerializeField] private bool _enableAutomaticOptimization = true;

        [Header("Optimization Thresholds")]
        [SerializeField] private float _frameTimeThreshold = 20f; // 20ms triggers optimization
        [SerializeField] private long _memoryThreshold = 75 * 1024 * 1024; // 75MB triggers optimization
        [SerializeField] private int _componentCountThreshold = 1000; // 1000+ components triggers optimization

        [Header("Optimization Parameters")]
        [SerializeField] private float _optimizationCooldown = 5f; // 5 seconds between optimizations
        [SerializeField] private int _maxOptimizationsPerFrame = 1;
        [SerializeField] private bool _persistOptimizations = true;

        // Optimization tracking
        private readonly Dictionary<UIOptimizationType, OptimizationState> _optimizationStates = new Dictionary<UIOptimizationType, OptimizationState>();
        private readonly Queue<UIOptimizationType> _optimizationQueue = new Queue<UIOptimizationType>();
        private readonly List<UIOptimizationType> _activeOptimizations = new List<UIOptimizationType>();

        // Timing
        private float _lastOptimizationTime;

        // Statistics
        private UIOptimizationStats _stats = new UIOptimizationStats();

        // Properties
        public bool IsEnabled { get; private set; } = true;
        public UIOptimizationLevel CurrentOptimizationLevel => _currentOptimizationLevel;
        public UIOptimizationStats GetStats() => _stats;

        // Events
        public System.Action<UIOptimizationLevel> OnOptimizationLevelChanged;
        public System.Action<UIOptimizationType> OnOptimizationApplied;
        public System.Action<UIOptimizationType> OnOptimizationReverted;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            _stats = new UIOptimizationStats();
            InitializeOptimizationStates();

            if (_enableLogging)
                ChimeraLogger.Log("UI", "⚡ UIOptimizationManager initialized", this);
        }

        /// <summary>
        /// Apply optimization recommendations
        /// </summary>
        public void ApplyOptimizations(UIOptimizationRecommendations recommendations)
        {
            if (!IsEnabled || !_enableOptimization || !_enableAutomaticOptimization) return;

            if (Time.time - _lastOptimizationTime < _optimizationCooldown) return;

            foreach (var optimization in recommendations.RecommendedOptimizations)
            {
                if (_optimizationQueue.Count < _maxOptimizationsPerFrame)
                {
                    _optimizationQueue.Enqueue(optimization);
                }
            }

            ProcessOptimizationQueue();

            if (recommendations.SuggestedLevel != _currentOptimizationLevel)
            {
                SetOptimizationLevel(recommendations.SuggestedLevel);
            }
        }

        /// <summary>
        /// Apply specific optimization
        /// </summary>
        public bool ApplyOptimization(UIOptimizationType optimization)
        {
            if (!IsEnabled || !_enableOptimization) return false;

            if (IsOptimizationActive(optimization))
            {
                if (_enableLogging)
                    ChimeraLogger.LogWarning("UI", $"Optimization already active: {optimization}", this);
                return false;
            }

            bool success = ExecuteOptimization(optimization);
            if (success)
            {
                _activeOptimizations.Add(optimization);
                _stats.OptimizationsApplied++;
                _lastOptimizationTime = Time.time;

                UpdateOptimizationState(optimization, true);
                OnOptimizationApplied?.Invoke(optimization);

                if (_enableLogging)
                    ChimeraLogger.Log("UI", $"Applied UI optimization: {optimization}", this);
            }

            return success;
        }

        /// <summary>
        /// Revert specific optimization
        /// </summary>
        public bool RevertOptimization(UIOptimizationType optimization)
        {
            if (!IsEnabled || !IsOptimizationActive(optimization)) return false;

            bool success = UndoOptimization(optimization);
            if (success)
            {
                _activeOptimizations.Remove(optimization);
                _stats.OptimizationsReverted++;

                UpdateOptimizationState(optimization, false);
                OnOptimizationReverted?.Invoke(optimization);

                if (_enableLogging)
                    ChimeraLogger.Log("UI", $"Reverted UI optimization: {optimization}", this);
            }

            return success;
        }

        /// <summary>
        /// Set optimization level
        /// </summary>
        public void SetOptimizationLevel(UIOptimizationLevel level)
        {
            if (_currentOptimizationLevel == level) return;

            var previousLevel = _currentOptimizationLevel;
            _currentOptimizationLevel = level;

            ApplyOptimizationLevel(level);
            OnOptimizationLevelChanged?.Invoke(level);

            if (_enableLogging)
                ChimeraLogger.Log("UI", $"Changed optimization level: {previousLevel} → {level}", this);
        }

        /// <summary>
        /// Check if optimization is active
        /// </summary>
        public bool IsOptimizationActive(UIOptimizationType optimization)
        {
            return _activeOptimizations.Contains(optimization);
        }

        /// <summary>
        /// Get active optimizations
        /// </summary>
        public UIOptimizationType[] GetActiveOptimizations()
        {
            return _activeOptimizations.ToArray();
        }

        /// <summary>
        /// Get optimization effectiveness
        /// </summary>
        public float GetOptimizationEffectiveness(UIOptimizationType optimization)
        {
            if (_optimizationStates.TryGetValue(optimization, out var state))
            {
                return state.Effectiveness;
            }
            return 0f;
        }

        /// <summary>
        /// Reset all optimizations
        /// </summary>
        public void ResetOptimizations()
        {
            // Revert all active optimizations
            var activeOpts = new List<UIOptimizationType>(_activeOptimizations);
            foreach (var optimization in activeOpts)
            {
                RevertOptimization(optimization);
            }

            _optimizationQueue.Clear();
            _currentOptimizationLevel = UIOptimizationLevel.None;

            if (_enableLogging)
                ChimeraLogger.Log("UI", "Reset all UI optimizations", this);
        }

        /// <summary>
        /// Set system enabled/disabled
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;

            if (!enabled)
            {
                ResetOptimizations();
            }

            if (_enableLogging)
                ChimeraLogger.Log("UI", $"UIOptimizationManager: {(enabled ? "enabled" : "disabled")}", this);
        }

        #region Private Methods

        /// <summary>
        /// Initialize optimization states
        /// </summary>
        private void InitializeOptimizationStates()
        {
            foreach (UIOptimizationType optimization in System.Enum.GetValues(typeof(UIOptimizationType)))
            {
                _optimizationStates[optimization] = new OptimizationState
                {
                    Optimization = optimization,
                    IsActive = false,
                    Effectiveness = 0f,
                    LastAppliedTime = 0f
                };
            }
        }

        /// <summary>
        /// Process optimization queue
        /// </summary>
        private void ProcessOptimizationQueue()
        {
            int optimizationsProcessed = 0;

            while (_optimizationQueue.Count > 0 && optimizationsProcessed < _maxOptimizationsPerFrame)
            {
                var optimization = _optimizationQueue.Dequeue();
                ApplyOptimization(optimization);
                optimizationsProcessed++;
            }
        }

        /// <summary>
        /// Execute specific optimization
        /// </summary>
        private bool ExecuteOptimization(UIOptimizationType optimization)
        {
            try
            {
                switch (optimization)
                {
                    case UIOptimizationType.EnableUIPooling:
                        return EnableUIPooling();
                    case UIOptimizationType.ReduceUpdateFrequency:
                        return ReduceUpdateFrequency();
                    case UIOptimizationType.EnableBatchedUpdates:
                        return EnableBatchedUpdates();
                    case UIOptimizationType.EnableCanvasCulling:
                        return EnableCanvasCulling();
                    case UIOptimizationType.ReduceMaxUpdatesPerFrame:
                        return ReduceMaxUpdatesPerFrame();
                    case UIOptimizationType.OptimizeCanvasStructure:
                        return OptimizeCanvasStructure();
                    case UIOptimizationType.ReduceUIAnimations:
                        return ReduceUIAnimations();
                    case UIOptimizationType.EnableAsyncUIUpdates:
                        return EnableAsyncUIUpdates();
                    default:
                        return false;
                }
            }
            catch (System.Exception ex)
            {
                if (_enableLogging)
                    ChimeraLogger.LogError("UI", $"Failed to execute optimization {optimization}: {ex.Message}", this);
                return false;
            }
        }

        /// <summary>
        /// Undo specific optimization
        /// </summary>
        private bool UndoOptimization(UIOptimizationType optimization)
        {
            try
            {
                switch (optimization)
                {
                    case UIOptimizationType.EnableUIPooling:
                        return DisableUIPooling();
                    case UIOptimizationType.ReduceUpdateFrequency:
                        return RestoreUpdateFrequency();
                    case UIOptimizationType.EnableBatchedUpdates:
                        return DisableBatchedUpdates();
                    case UIOptimizationType.EnableCanvasCulling:
                        return DisableCanvasCulling();
                    case UIOptimizationType.ReduceMaxUpdatesPerFrame:
                        return RestoreMaxUpdatesPerFrame();
                    case UIOptimizationType.OptimizeCanvasStructure:
                        return RestoreCanvasStructure();
                    case UIOptimizationType.ReduceUIAnimations:
                        return RestoreUIAnimations();
                    case UIOptimizationType.EnableAsyncUIUpdates:
                        return DisableAsyncUIUpdates();
                    default:
                        return false;
                }
            }
            catch (System.Exception ex)
            {
                if (_enableLogging)
                    ChimeraLogger.LogError("UI", $"Failed to undo optimization {optimization}: {ex.Message}", this);
                return false;
            }
        }

        /// <summary>
        /// Apply optimization level
        /// </summary>
        private void ApplyOptimizationLevel(UIOptimizationLevel level)
        {
            switch (level)
            {
                case UIOptimizationLevel.None:
                    ResetOptimizations();
                    break;
                case UIOptimizationLevel.Conservative:
                    ApplyConservativeOptimizations();
                    break;
                case UIOptimizationLevel.Balanced:
                    ApplyBalancedOptimizations();
                    break;
                case UIOptimizationLevel.Aggressive:
                    ApplyAggressiveOptimizations();
                    break;
                case UIOptimizationLevel.Maximum:
                    ApplyMaximumOptimizations();
                    break;
            }
        }

        /// <summary>
        /// Apply conservative optimizations
        /// </summary>
        private void ApplyConservativeOptimizations()
        {
            ApplyOptimization(UIOptimizationType.EnableUIPooling);
            ApplyOptimization(UIOptimizationType.EnableBatchedUpdates);
        }

        /// <summary>
        /// Apply balanced optimizations
        /// </summary>
        private void ApplyBalancedOptimizations()
        {
            ApplyConservativeOptimizations();
            ApplyOptimization(UIOptimizationType.EnableCanvasCulling);
            ApplyOptimization(UIOptimizationType.ReduceUpdateFrequency);
        }

        /// <summary>
        /// Apply aggressive optimizations
        /// </summary>
        private void ApplyAggressiveOptimizations()
        {
            ApplyBalancedOptimizations();
            ApplyOptimization(UIOptimizationType.ReduceMaxUpdatesPerFrame);
            ApplyOptimization(UIOptimizationType.OptimizeCanvasStructure);
        }

        /// <summary>
        /// Apply maximum optimizations
        /// </summary>
        private void ApplyMaximumOptimizations()
        {
            ApplyAggressiveOptimizations();
            ApplyOptimization(UIOptimizationType.ReduceUIAnimations);
            ApplyOptimization(UIOptimizationType.EnableAsyncUIUpdates);
        }

        /// <summary>
        /// Update optimization state
        /// </summary>
        private void UpdateOptimizationState(UIOptimizationType optimization, bool isActive)
        {
            if (_optimizationStates.TryGetValue(optimization, out var state))
            {
                state.IsActive = isActive;
                state.LastAppliedTime = Time.time;
                state.Effectiveness = CalculateOptimizationEffectiveness(optimization);
                _optimizationStates[optimization] = state;
            }
        }

        /// <summary>
        /// Calculate optimization effectiveness
        /// </summary>
        private float CalculateOptimizationEffectiveness(UIOptimizationType optimization)
        {
            // Simplified effectiveness calculation
            // In a real implementation, this would measure actual performance improvements
            return optimization switch
            {
                UIOptimizationType.EnableUIPooling => 0.8f,
                UIOptimizationType.EnableBatchedUpdates => 0.7f,
                UIOptimizationType.EnableCanvasCulling => 0.6f,
                UIOptimizationType.ReduceUpdateFrequency => 0.5f,
                UIOptimizationType.ReduceMaxUpdatesPerFrame => 0.4f,
                UIOptimizationType.OptimizeCanvasStructure => 0.9f,
                UIOptimizationType.ReduceUIAnimations => 0.3f,
                UIOptimizationType.EnableAsyncUIUpdates => 0.6f,
                _ => 0.1f
            };
        }

        #endregion

        #region Optimization Implementations

        // Simplified optimization implementations
        // In a real system, these would interact with actual UI systems

        private bool EnableUIPooling() => true;
        private bool DisableUIPooling() => true;
        private bool ReduceUpdateFrequency() => true;
        private bool RestoreUpdateFrequency() => true;
        private bool EnableBatchedUpdates() => true;
        private bool DisableBatchedUpdates() => true;
        private bool EnableCanvasCulling() => true;
        private bool DisableCanvasCulling() => true;
        private bool ReduceMaxUpdatesPerFrame() => true;
        private bool RestoreMaxUpdatesPerFrame() => true;
        private bool OptimizeCanvasStructure() => true;
        private bool RestoreCanvasStructure() => true;
        private bool ReduceUIAnimations() => true;
        private bool RestoreUIAnimations() => true;
        private bool EnableAsyncUIUpdates() => true;
        private bool DisableAsyncUIUpdates() => true;

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Optimization state data
    /// </summary>
    [System.Serializable]
    public struct OptimizationState
    {
        public UIOptimizationType Optimization;
        public bool IsActive;
        public float Effectiveness;
        public float LastAppliedTime;
    }

    /// <summary>
    /// UI optimization statistics
    /// </summary>
    [System.Serializable]
    public struct UIOptimizationStats
    {
        public int OptimizationsApplied;
        public int OptimizationsReverted;
        public UIOptimizationLevel CurrentOptimizationLevel;
        public float TotalOptimizationTime;
        public int LevelChanges;
    }

    #endregion
}