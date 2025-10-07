using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using System.Linq;

namespace ProjectChimera.Core.Foundation.Performance
{
    /// <summary>
    /// REFACTORED: Foundation Performance Metrics - Focused performance metrics collection and calculation
    /// Handles system performance measurement, score calculation, and metrics tracking
    /// Single Responsibility: Performance metrics collection and calculation
    /// </summary>
    public class FoundationPerformanceMetrics : MonoBehaviour
    {
        [Header("Metrics Settings")]
        [SerializeField] private bool _enableMetricsCollection = true;
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private float _metricsUpdateInterval = 1f;

        [Header("Performance Calculation")]
        [SerializeField] private float _healthWeight = 0.4f;
        [SerializeField] private float _initializationWeight = 0.3f;
        [SerializeField] private float _enabledWeight = 0.2f;
        [SerializeField] private float _basePerformanceWeight = 0.1f;

        // Performance tracking
        private readonly Dictionary<string, SystemPerformanceData> _systemPerformance = new Dictionary<string, SystemPerformanceData>();

        // System references
        private FoundationSystemRegistry _systemRegistry;

        // Timing
        private float _lastMetricsUpdate;

        // Cached values
        private float _cachedOverallScore = 1.0f;
        private float _lastCacheUpdate;

        // Properties
        public bool IsEnabled { get; private set; } = true;

        // Events
        public System.Action<string, float> OnSystemPerformanceUpdated;
        public System.Action<float> OnOverallPerformanceChanged;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            // Use SafeResolve helper for clean dependency injection with fallback
            _systemRegistry = DependencyResolutionHelper.SafeResolve<FoundationSystemRegistry>(this, "FOUNDATION");

            if (_systemRegistry == null)
            {
                ChimeraLogger.LogError("FOUNDATION", "Critical dependency FoundationSystemRegistry not found", this);
            }

            if (_enableLogging)
                ChimeraLogger.Log("FOUNDATION", "📊 FoundationPerformanceMetrics initialized", this);
        }

        /// <summary>
        /// Update system performance metrics
        /// </summary>
        public void UpdateSystemPerformanceMetrics()
        {
            if (!IsEnabled || !_enableMetricsCollection) return;

            if (Time.time - _lastMetricsUpdate < _metricsUpdateInterval) return;

            if (_systemRegistry == null) return;

            var systems = _systemRegistry.GetRegisteredSystems();
            foreach (var system in systems)
            {
                UpdateSystemPerformance(system);
            }

            // Update overall performance if needed
            UpdateOverallPerformance();

            _lastMetricsUpdate = Time.time;
        }

        /// <summary>
        /// Get system performance data
        /// </summary>
        public SystemPerformanceData GetSystemPerformance(string systemName)
        {
            _systemPerformance.TryGetValue(systemName, out var performanceData);
            return performanceData;
        }

        /// <summary>
        /// Get all system performance data
        /// </summary>
        public SystemPerformanceData[] GetAllSystemPerformance()
        {
            return _systemPerformance.Values.ToArray();
        }

        /// <summary>
        /// Get overall performance score
        /// </summary>
        public float GetOverallPerformanceScore()
        {
            if (_systemPerformance.Count == 0)
                return 1.0f;

            // Use cached value if recent
            if (Time.time - _lastCacheUpdate < 1f)
                return _cachedOverallScore;

            _cachedOverallScore = _systemPerformance.Values.Average(p => p.PerformanceScore);
            _lastCacheUpdate = Time.time;

            return _cachedOverallScore;
        }

        /// <summary>
        /// Get performance statistics
        /// </summary>
        public PerformanceMetricsStats GetMetricsStats()
        {
            var stats = new PerformanceMetricsStats
            {
                TotalSystems = _systemPerformance.Count,
                OverallPerformanceScore = GetOverallPerformanceScore(),
                AveragePerformanceScore = GetOverallPerformanceScore(),
                HighestPerformanceScore = _systemPerformance.Count > 0 ? _systemPerformance.Values.Max(p => p.PerformanceScore) : 0f,
                LowestPerformanceScore = _systemPerformance.Count > 0 ? _systemPerformance.Values.Min(p => p.PerformanceScore) : 0f
            };

            return stats;
        }

        /// <summary>
        /// Force metrics update for specific system
        /// </summary>
        public void ForceUpdateSystem(string systemName)
        {
            if (!IsEnabled || _systemRegistry == null) return;

            var system = _systemRegistry.GetSystem(systemName);
            if (system != null)
            {
                UpdateSystemPerformance(system);
            }
        }

        /// <summary>
        /// Reset all metrics
        /// </summary>
        public void ResetMetrics()
        {
            _systemPerformance.Clear();
            _cachedOverallScore = 1.0f;
            _lastCacheUpdate = 0f;

            if (_enableLogging)
                ChimeraLogger.Log("FOUNDATION", "Performance metrics reset", this);
        }

        /// <summary>
        /// Set system enabled/disabled
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;

            if (!enabled)
            {
                ResetMetrics();
            }

            if (_enableLogging)
                ChimeraLogger.Log("FOUNDATION", $"FoundationPerformanceMetrics: {(enabled ? "enabled" : "disabled")}", this);
        }

        #region Private Methods

        /// <summary>
        /// Update individual system performance
        /// </summary>
        private void UpdateSystemPerformance(IFoundationSystem system)
        {
            var performanceScore = CalculateSystemPerformanceScore(system);

            if (!_systemPerformance.TryGetValue(system.SystemName, out var perfData))
            {
                perfData = new SystemPerformanceData
                {
                    SystemName = system.SystemName,
                    FirstMeasurementTime = Time.time,
                    PerformanceScore = performanceScore,
                    Trend = ProjectChimera.Core.Foundation.PerformanceTrend.Stable,
                    MeasurementCount = 0,
                    ConsecutivePoorPerformance = 0
                };
            }

            var previousScore = perfData.PerformanceScore;
            perfData.PerformanceScore = performanceScore;
            perfData.LastUpdateTime = Time.time;
            perfData.MeasurementCount++;

            // Update performance trend
            UpdatePerformanceTrend(ref perfData, previousScore, performanceScore);

            // Track consecutive poor performance
            UpdatePoorPerformanceTracking(ref perfData, performanceScore);

            _systemPerformance[system.SystemName] = perfData;

            // Fire event if significant change
            if (Mathf.Abs(performanceScore - previousScore) > 0.1f)
            {
                OnSystemPerformanceUpdated?.Invoke(system.SystemName, performanceScore);
            }
        }

        /// <summary>
        /// Calculate system performance score
        /// </summary>
        private float CalculateSystemPerformanceScore(IFoundationSystem system)
        {
            float score = 0f;

            // Health factor
            float healthScore = GetHealthScore(system.Health);
            score += healthScore * _healthWeight;

            // Initialization factor
            float initScore = system.IsInitialized ? 1.0f : 0.0f;
            score += initScore * _initializationWeight;

            // Enabled factor
            float enabledScore = system.IsEnabled ? 1.0f : 0.0f;
            score += enabledScore * _enabledWeight;

            // Base performance factor (simplified simulation)
            float basePerformance = UnityEngine.Random.Range(0.8f, 1.0f);
            score += basePerformance * _basePerformanceWeight;

            return Mathf.Clamp01(score);
        }

        /// <summary>
        /// Get health score from system health
        /// </summary>
        private float GetHealthScore(SystemHealth health)
        {
            return health switch
            {
                SystemHealth.Healthy => 1.0f,
                SystemHealth.Warning => 0.8f,
                SystemHealth.Critical => 0.4f,
                SystemHealth.Failed => 0.0f,
                SystemHealth.Unknown => 0.5f,
                _ => 0.5f
            };
        }

        /// <summary>
        /// Update performance trend
        /// </summary>
        private void UpdatePerformanceTrend(ref SystemPerformanceData perfData, float previousScore, float currentScore)
        {
            float difference = currentScore - previousScore;
            const float trendThreshold = 0.05f; // 5% threshold for trend detection

            if (difference > trendThreshold)
                perfData.Trend = ProjectChimera.Core.Foundation.PerformanceTrend.Improving;
            else if (difference < -trendThreshold)
                perfData.Trend = ProjectChimera.Core.Foundation.PerformanceTrend.Declining;
            else
                perfData.Trend = ProjectChimera.Core.Foundation.PerformanceTrend.Stable;
        }

        /// <summary>
        /// Update poor performance tracking
        /// </summary>
        private void UpdatePoorPerformanceTracking(ref SystemPerformanceData perfData, float performanceScore)
        {
            const float poorPerformanceThreshold = 0.4f; // 40%

            if (performanceScore < poorPerformanceThreshold)
            {
                perfData.ConsecutivePoorPerformance++;
            }
            else
            {
                perfData.ConsecutivePoorPerformance = 0;
            }
        }

        /// <summary>
        /// Update overall performance and fire events
        /// </summary>
        private void UpdateOverallPerformance()
        {
            float previousOverallScore = _cachedOverallScore;
            float currentOverallScore = GetOverallPerformanceScore();

            // Fire event if significant change
            if (Mathf.Abs(currentOverallScore - previousOverallScore) > 0.05f)
            {
                OnOverallPerformanceChanged?.Invoke(currentOverallScore);
            }
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Performance metrics statistics
    /// </summary>
    [System.Serializable]
    public struct PerformanceMetricsStats
    {
        public int TotalSystems;
        public float OverallPerformanceScore;
        public float AveragePerformanceScore;
        public float HighestPerformanceScore;
        public float LowestPerformanceScore;
    }

    #endregion
}
