using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core.Foundation.Performance
{
    /// <summary>
    /// REFACTORED: Optimization Strategy Manager
    /// Single Responsibility: Optimization strategy selection, execution, and validation
    /// Extracted from FoundationPerformanceOptimizer for better separation of concerns
    /// </summary>
    public class OptimizationStrategyManager : MonoBehaviour
    {
        [Header("Strategy Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private float _criticalPerformanceThreshold = 0.3f;
        [SerializeField] private int _consecutivePoorThreshold = 3;

        [Header("Strategy Enablement")]
        [SerializeField] private bool _enableReinitialization = true;
        [SerializeField] private bool _enableResourceOptimization = true;
        [SerializeField] private bool _enableConfigurationTuning = true;
        [SerializeField] private bool _enableGracefulDegradation = true;

        // Strategy implementations
        private readonly Dictionary<OptimizationStrategy, IOptimizationStrategy> _strategies = new Dictionary<OptimizationStrategy, IOptimizationStrategy>();

        // State tracking
        private bool _isInitialized = false;

        // Statistics
        private OptimizationStrategyStats _stats = new OptimizationStrategyStats();

        // Events
        public event System.Action<string, OptimizationStrategy> OnStrategySelected;
        public event System.Action<string, OptimizationStrategy, bool> OnStrategyExecuted;

        public bool IsInitialized => _isInitialized;
        public OptimizationStrategyStats Stats => _stats;

        public void Initialize()
        {
            if (_isInitialized) return;

            InitializeStrategies();
            ResetStats();

            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("FOUNDATION", "Optimization Strategy Manager initialized", this);
            }
        }

        /// <summary>
        /// Initialize strategy implementations
        /// </summary>
        private void InitializeStrategies()
        {
            _strategies.Clear();

            _strategies[OptimizationStrategy.Reinitialization] = new ReinitializationStrategy();
            _strategies[OptimizationStrategy.ResourceOptimization] = new ResourceOptimizationStrategy();
            _strategies[OptimizationStrategy.ConfigurationTuning] = new ConfigurationTuningStrategy();
            _strategies[OptimizationStrategy.GracefulDegradation] = new GracefulDegradationStrategy();
        }

        /// <summary>
        /// Determine optimal optimization strategy based on performance analysis
        /// </summary>
        public OptimizationStrategy DetermineStrategy(PerformanceAnalysisResult result)
        {
            // PerformanceAnalysisResult is a struct; use default check instead of null
            if (string.IsNullOrEmpty(result.SystemName) && result.CurrentScore == 0f && result.Recommendations == null)
            {
                return GetFallbackStrategy();
            }

            var strategy = SelectStrategyByAnalysis(result);

            _stats.StrategiesSelected++;
            OnStrategySelected?.Invoke(result.SystemName ?? "Unknown", strategy);

            if (_enableLogging)
            {
                ChimeraLogger.Log("FOUNDATION", $"Selected {strategy} strategy for system with score {result.CurrentScore:F2}", this);
            }

            return strategy;
        }

        /// <summary>
        /// Determine strategy for system name only (fallback)
        /// </summary>
        public OptimizationStrategy DetermineStrategy(string systemName)
        {
            var strategy = GetFallbackStrategy();

            _stats.StrategiesSelected++;
            OnStrategySelected?.Invoke(systemName, strategy);

            if (_enableLogging)
            {
                ChimeraLogger.Log("FOUNDATION", $"Selected fallback {strategy} strategy for {systemName}", this);
            }

            return strategy;
        }

        /// <summary>
        /// Execute optimization strategy for a system
        /// </summary>
        public bool ExecuteStrategy(string systemName, OptimizationStrategy strategy)
        {
            if (!_isInitialized)
            {
                if (_enableLogging)
                {
                    ChimeraLogger.LogWarning("FOUNDATION", "Cannot execute strategy - manager not initialized", this);
                }
                return false;
            }

            if (string.IsNullOrEmpty(systemName))
            {
                if (_enableLogging)
                {
                    ChimeraLogger.LogWarning("FOUNDATION", "Cannot execute strategy - invalid system name", this);
                }
                return false;
            }

            if (!IsStrategyEnabled(strategy))
            {
                if (_enableLogging)
                {
                    ChimeraLogger.LogWarning("FOUNDATION", $"Strategy {strategy} is disabled", this);
                }
                return false;
            }

            try
            {
                var success = ApplyOptimizationStrategy(systemName, strategy);

                // Update statistics
                _stats.StrategiesExecuted++;
                if (success) _stats.SuccessfulExecutions++;
                else _stats.FailedExecutions++;

                _stats.LastExecutionTime = Time.time;

                OnStrategyExecuted?.Invoke(systemName, strategy, success);

                if (_enableLogging)
                {
                    var result = success ? "succeeded" : "failed";
                    ChimeraLogger.Log("FOUNDATION", $"Strategy {strategy} {result} for {systemName}", this);
                }

                return success;
            }
            catch (System.Exception ex)
            {
                _stats.StrategiesExecuted++;
                _stats.FailedExecutions++;
                _stats.ExecutionErrors++;

                OnStrategyExecuted?.Invoke(systemName, strategy, false);

                if (_enableLogging)
                {
                    ChimeraLogger.LogError("FOUNDATION", $"Strategy {strategy} failed for {systemName}: {ex.Message}", this);
                }

                return false;
            }
        }

        /// <summary>
        /// Check if a strategy is currently enabled
        /// </summary>
        public bool IsStrategyEnabled(OptimizationStrategy strategy)
        {
            switch (strategy)
            {
                case OptimizationStrategy.Reinitialization:
                    return _enableReinitialization;
                case OptimizationStrategy.ResourceOptimization:
                    return _enableResourceOptimization;
                case OptimizationStrategy.ConfigurationTuning:
                    return _enableConfigurationTuning;
                case OptimizationStrategy.GracefulDegradation:
                    return _enableGracefulDegradation;
                default:
                    return false;
            }
        }

        /// <summary>
        /// Get list of available strategies
        /// </summary>
        public List<OptimizationStrategy> GetAvailableStrategies()
        {
            var available = new List<OptimizationStrategy>();

            foreach (var strategy in System.Enum.GetValues(typeof(OptimizationStrategy)))
            {
                var strategyEnum = (OptimizationStrategy)strategy;
                if (IsStrategyEnabled(strategyEnum))
                {
                    available.Add(strategyEnum);
                }
            }

            return available;
        }

        /// <summary>
        /// Select strategy based on performance analysis
        /// </summary>
        private OptimizationStrategy SelectStrategyByAnalysis(PerformanceAnalysisResult result)
        {
            // Critical performance - try reinitialization first
            if (result.CurrentScore < _criticalPerformanceThreshold && _enableReinitialization)
            {
                return OptimizationStrategy.Reinitialization;
            }

            // High variability - try configuration tuning
            if (result.ScoreVariability > 0.2f && _enableConfigurationTuning)
            {
                return OptimizationStrategy.ConfigurationTuning;
            }

            // Declining trend - try resource optimization
            if (result.Trend == PerformanceTrend.Declining && _enableResourceOptimization)
            {
                return OptimizationStrategy.ResourceOptimization;
            }

            // Consecutive poor performance - try graceful degradation
            if (result.ConsecutivePoorPerformance >= _consecutivePoorThreshold && _enableGracefulDegradation)
            {
                return OptimizationStrategy.GracefulDegradation;
            }

            return GetFallbackStrategy();
        }

        /// <summary>
        /// Get fallback strategy based on enabled options
        /// </summary>
        private OptimizationStrategy GetFallbackStrategy()
        {
            if (_enableConfigurationTuning)
                return OptimizationStrategy.ConfigurationTuning;

            if (_enableResourceOptimization)
                return OptimizationStrategy.ResourceOptimization;

            if (_enableGracefulDegradation)
                return OptimizationStrategy.GracefulDegradation;

            if (_enableReinitialization)
                return OptimizationStrategy.Reinitialization;

            // Default fallback
            return OptimizationStrategy.ConfigurationTuning;
        }

        /// <summary>
        /// Apply optimization strategy using strategy implementation
        /// </summary>
        private bool ApplyOptimizationStrategy(string systemName, OptimizationStrategy strategy)
        {
            if (_strategies.TryGetValue(strategy, out var implementation))
            {
                return implementation.Apply(systemName);
            }

            // Fallback to basic implementations
            switch (strategy)
            {
                case OptimizationStrategy.Reinitialization:
                    return ApplyReinitializationOptimization(systemName);
                case OptimizationStrategy.ResourceOptimization:
                    return ApplyResourceOptimization(systemName);
                case OptimizationStrategy.ConfigurationTuning:
                    return ApplyConfigurationTuning(systemName);
                case OptimizationStrategy.GracefulDegradation:
                    return ApplyGracefulDegradation(systemName);
                default:
                    return false;
            }
        }

        /// <summary>
        /// Enable or disable a specific strategy
        /// </summary>
        public void SetStrategyEnabled(OptimizationStrategy strategy, bool enabled)
        {
            switch (strategy)
            {
                case OptimizationStrategy.Reinitialization:
                    _enableReinitialization = enabled;
                    break;
                case OptimizationStrategy.ResourceOptimization:
                    _enableResourceOptimization = enabled;
                    break;
                case OptimizationStrategy.ConfigurationTuning:
                    _enableConfigurationTuning = enabled;
                    break;
                case OptimizationStrategy.GracefulDegradation:
                    _enableGracefulDegradation = enabled;
                    break;
            }

            if (_enableLogging)
            {
                ChimeraLogger.Log("FOUNDATION", $"Strategy {strategy} {(enabled ? "enabled" : "disabled")}", this);
            }
        }

        /// <summary>
        /// Set performance thresholds
        /// </summary>
        public void SetPerformanceThresholds(float criticalThreshold, int consecutivePoorThreshold)
        {
            _criticalPerformanceThreshold = Mathf.Clamp01(criticalThreshold);
            _consecutivePoorThreshold = Mathf.Max(1, consecutivePoorThreshold);

            if (_enableLogging)
            {
                ChimeraLogger.Log("FOUNDATION", $"Performance thresholds updated: Critical={_criticalPerformanceThreshold:F2}, Consecutive={_consecutivePoorThreshold}", this);
            }
        }

        #region Basic Strategy Implementations

        private bool ApplyReinitializationOptimization(string systemName)
        {
            // Simulated reinitialization - 70% success rate
            return UnityEngine.Random.Range(0f, 1f) > 0.3f;
        }

        private bool ApplyResourceOptimization(string systemName)
        {
            // Simulated resource optimization - 80% success rate
            return UnityEngine.Random.Range(0f, 1f) > 0.2f;
        }

        private bool ApplyConfigurationTuning(string systemName)
        {
            // Simulated configuration tuning - 75% success rate
            return UnityEngine.Random.Range(0f, 1f) > 0.25f;
        }

        private bool ApplyGracefulDegradation(string systemName)
        {
            // Simulated graceful degradation - 90% success rate (usually works)
            return UnityEngine.Random.Range(0f, 1f) > 0.1f;
        }

        #endregion

        /// <summary>
        /// Reset statistics
        /// </summary>
        private void ResetStats()
        {
            _stats = new OptimizationStrategyStats
            {
                StrategiesSelected = 0,
                StrategiesExecuted = 0,
                SuccessfulExecutions = 0,
                FailedExecutions = 0,
                ExecutionErrors = 0,
                LastExecutionTime = Time.time
            };
        }
    }



    /// <summary>
    /// Performance trend enumeration
    /// </summary>
    public enum PerformanceTrend
    {
        Stable,
        Improving,
        Declining,
        Volatile
    }



    /// <summary>
    /// Strategy statistics
    /// </summary>
    [System.Serializable]
    public struct OptimizationStrategyStats
    {
        public int StrategiesSelected;
        public int StrategiesExecuted;
        public int SuccessfulExecutions;
        public int FailedExecutions;
        public int ExecutionErrors;
        public float LastExecutionTime;
    }

    /// <summary>
    /// Interface for optimization strategy implementations
    /// </summary>
    public interface IOptimizationStrategy
    {
        bool Apply(string systemName);
    }

    /// <summary>
    /// Reinitialization strategy implementation
    /// </summary>
    public class ReinitializationStrategy : IOptimizationStrategy
    {
        public bool Apply(string systemName)
        {
            // Implement actual reinitialization logic here
            return UnityEngine.Random.Range(0f, 1f) > 0.3f;
        }
    }

    /// <summary>
    /// Resource optimization strategy implementation
    /// </summary>
    public class ResourceOptimizationStrategy : IOptimizationStrategy
    {
        public bool Apply(string systemName)
        {
            // Implement actual resource optimization logic here
            return UnityEngine.Random.Range(0f, 1f) > 0.2f;
        }
    }

    /// <summary>
    /// Configuration tuning strategy implementation
    /// </summary>
    public class ConfigurationTuningStrategy : IOptimizationStrategy
    {
        public bool Apply(string systemName)
        {
            // Implement actual configuration tuning logic here
            return UnityEngine.Random.Range(0f, 1f) > 0.25f;
        }
    }

    /// <summary>
    /// Graceful degradation strategy implementation
    /// </summary>
    public class GracefulDegradationStrategy : IOptimizationStrategy
    {
        public bool Apply(string systemName)
        {
            // Implement actual graceful degradation logic here
            return UnityEngine.Random.Range(0f, 1f) > 0.1f;
        }
    }
}
