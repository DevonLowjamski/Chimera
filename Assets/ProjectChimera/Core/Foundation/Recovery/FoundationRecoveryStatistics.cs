using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using System.Linq;
using System;

namespace ProjectChimera.Core.Foundation.Recovery
{
    /// <summary>
    /// REFACTORED: Foundation Recovery Statistics - Focused recovery metrics and analytics
    /// Handles recovery statistics calculation, trend analysis, and performance metrics
    /// Single Responsibility: Recovery statistics and analytics
    /// </summary>
    public class FoundationRecoveryStatistics : MonoBehaviour
    {
        [Header("Statistics Settings")]
        [SerializeField] private bool _enableStatistics = true;
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private float _statsUpdateInterval = 10f;
        [SerializeField] private bool _enableTrendAnalysis = true;

        [Header("Analysis Configuration")]
        [SerializeField] private int _trendAnalysisPeriod = 100; // Number of attempts to analyze
        [SerializeField] private float _trendThreshold = 0.1f; // 10% change for trend detection
        [SerializeField] private int _minAttemptsForTrend = 10;

        // Statistics tracking
        private RecoveryManagerStats _stats = new RecoveryManagerStats();
        private readonly Dictionary<RecoveryStrategy, StrategyStats> _strategyStats = new Dictionary<RecoveryStrategy, StrategyStats>();
        private readonly Queue<RecoveryMetricSnapshot> _metricsHistory = new Queue<RecoveryMetricSnapshot>();
        private const int MAX_METRICS_HISTORY = 200;

        // Timing
        private float _lastStatsUpdate;

        // Properties
        public bool IsEnabled { get; private set; } = true;
        public RecoveryManagerStats GetStats() => _stats;

        // Events
        public System.Action<RecoveryManagerStats> OnStatsUpdated;
        public System.Action<RecoveryTrend> OnTrendDetected;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            _stats = new RecoveryManagerStats();
            InitializeStrategyStats();

            if (_enableLogging)
                ChimeraLogger.Log("FOUNDATION", "📊 FoundationRecoveryStatistics initialized", this);
        }

        /// <summary>
        /// Update recovery statistics
        /// </summary>
        public void UpdateStatistics()
        {
            if (!IsEnabled || !_enableStatistics) return;

            if (Time.time - _lastStatsUpdate < _statsUpdateInterval) return;

            CalculateOverallStatistics();
            UpdateStrategyStatistics();
            CreateMetricsSnapshot();

            if (_enableTrendAnalysis)
            {
                AnalyzeTrends();
            }

            OnStatsUpdated?.Invoke(_stats);
            _lastStatsUpdate = Time.time;

            if (_enableLogging)
                ChimeraLogger.Log("FOUNDATION", $"Updated recovery statistics - Success Rate: {_stats.RecoverySuccessRate:P2}", this);
        }

        /// <summary>
        /// Record recovery result
        /// </summary>
        public void RecordRecoveryResult(bool success)
        {
            if (!IsEnabled) return;

            _stats.RecoveryAttempts++;

            if (success)
            {
                _stats.SuccessfulRecoveries++;
            }
            else
            {
                _stats.FailedRecoveries++;
            }

            // Update success rate
            if (_stats.RecoveryAttempts > 0)
            {
                _stats.RecoverySuccessRate = (float)_stats.SuccessfulRecoveries / _stats.RecoveryAttempts;
            }
        }

        /// <summary>
        /// Record strategy usage and result
        /// </summary>
        public void RecordStrategyResult(RecoveryStrategy strategy, bool success, float duration)
        {
            if (!IsEnabled) return;

            if (!_strategyStats.TryGetValue(strategy, out var stats))
            {
                stats = new StrategyStats
                {
                    Strategy = strategy,
                    TotalAttempts = 0,
                    SuccessfulAttempts = 0,
                    TotalDuration = 0f,
                    AverageDuration = 0f,
                    SuccessRate = 0f
                };
            }

            stats.TotalAttempts++;
            stats.TotalDuration += duration;
            stats.AverageDuration = stats.TotalDuration / stats.TotalAttempts;

            if (success)
            {
                stats.SuccessfulAttempts++;
            }

            stats.SuccessRate = (float)stats.SuccessfulAttempts / stats.TotalAttempts;

            _strategyStats[strategy] = stats;
        }

        /// <summary>
        /// Get strategy statistics
        /// </summary>
        public StrategyStats GetStrategyStats(RecoveryStrategy strategy)
        {
            _strategyStats.TryGetValue(strategy, out var stats);
            return stats;
        }

        /// <summary>
        /// Get all strategy statistics
        /// </summary>
        public StrategyStats[] GetAllStrategyStats()
        {
            return _strategyStats.Values.ToArray();
        }

        /// <summary>
        /// Get best performing strategy
        /// </summary>
        public RecoveryStrategy GetBestStrategy()
        {
            if (_strategyStats.Count == 0)
                return RecoveryStrategy.Restart; // Default

            var bestStrategy = _strategyStats.Values
                .Where(s => s.TotalAttempts >= 5) // Require minimum attempts
                .OrderByDescending(s => s.SuccessRate)
                .ThenBy(s => s.AverageDuration)
                .FirstOrDefault();

            return bestStrategy.Strategy;
        }

        /// <summary>
        /// Get recovery metrics over time
        /// </summary>
        public RecoveryMetricSnapshot[] GetMetricsHistory()
        {
            return _metricsHistory.ToArray();
        }

        /// <summary>
        /// Get current recovery trend
        /// </summary>
        public RecoveryTrend GetCurrentTrend()
        {
            if (_metricsHistory.Count < _minAttemptsForTrend)
                return RecoveryTrend.Stable;

            var recentMetrics = _metricsHistory.TakeLast(_minAttemptsForTrend).ToArray();
            var earlierMetrics = _metricsHistory.Skip(Math.Max(0, _metricsHistory.Count - _minAttemptsForTrend * 2))
                                                .Take(_minAttemptsForTrend).ToArray();

            if (earlierMetrics.Length < _minAttemptsForTrend)
                return RecoveryTrend.Stable;

            float recentSuccessRate = recentMetrics.Average(m => m.SuccessRate);
            float earlierSuccessRate = earlierMetrics.Average(m => m.SuccessRate);

            float change = recentSuccessRate - earlierSuccessRate;

            if (change > _trendThreshold)
                return RecoveryTrend.Improving;
            else if (change < -_trendThreshold)
                return RecoveryTrend.Degrading;
            else
                return RecoveryTrend.Stable;
        }

        /// <summary>
        /// Reset all statistics
        /// </summary>
        public void ResetStatistics()
        {
            _stats = new RecoveryManagerStats();
            _strategyStats.Clear();
            _metricsHistory.Clear();
            InitializeStrategyStats();

            if (_enableLogging)
                ChimeraLogger.Log("FOUNDATION", "Recovery statistics reset", this);
        }

        /// <summary>
        /// Set system enabled/disabled
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;

            if (_enableLogging)
                ChimeraLogger.Log("FOUNDATION", $"FoundationRecoveryStatistics: {(enabled ? "enabled" : "disabled")}", this);
        }

        #region Private Methods

        /// <summary>
        /// Initialize strategy statistics
        /// </summary>
        private void InitializeStrategyStats()
        {
            foreach (RecoveryStrategy strategy in System.Enum.GetValues(typeof(RecoveryStrategy)))
            {
                if (!_strategyStats.ContainsKey(strategy))
                {
                    _strategyStats[strategy] = new StrategyStats
                    {
                        Strategy = strategy,
                        TotalAttempts = 0,
                        SuccessfulAttempts = 0,
                        TotalDuration = 0f,
                        AverageDuration = 0f,
                        SuccessRate = 0f
                    };
                }
            }
        }

        /// <summary>
        /// Calculate overall recovery statistics
        /// </summary>
        private void CalculateOverallStatistics()
        {
            // Update active recoveries count (this would be provided by other systems)
            // For now, we'll keep the existing value

            // Recalculate success rate
            if (_stats.RecoveryAttempts > 0)
            {
                _stats.RecoverySuccessRate = (float)_stats.SuccessfulRecoveries / _stats.RecoveryAttempts;
            }

            // Update timestamp
            _stats.LastUpdateTime = Time.time;
        }

        /// <summary>
        /// Update strategy-specific statistics
        /// </summary>
        private void UpdateStrategyStatistics()
        {
            // Statistics are updated in real-time through RecordStrategyResult
            // This method can be used for additional calculations or cleanup
        }

        /// <summary>
        /// Create metrics snapshot
        /// </summary>
        private void CreateMetricsSnapshot()
        {
            var snapshot = new RecoveryMetricSnapshot
            {
                Timestamp = Time.time,
                TotalAttempts = _stats.RecoveryAttempts,
                SuccessfulAttempts = _stats.SuccessfulRecoveries,
                FailedAttempts = _stats.FailedRecoveries,
                SuccessRate = _stats.RecoverySuccessRate,
                ActiveRecoveries = _stats.ActiveRecoveries
            };

            _metricsHistory.Enqueue(snapshot);

            // Maintain history size
            while (_metricsHistory.Count > MAX_METRICS_HISTORY)
            {
                _metricsHistory.Dequeue();
            }
        }

        /// <summary>
        /// Analyze recovery trends
        /// </summary>
        private void AnalyzeTrends()
        {
            if (_metricsHistory.Count < _minAttemptsForTrend)
                return;

            var currentTrend = GetCurrentTrend();
            var previousSnapshot = _metricsHistory.Count >= 2
                ? _metricsHistory.ElementAt(_metricsHistory.Count - 2)
                : new RecoveryMetricSnapshot();

            // Check if trend has changed significantly
            bool significantChange = Math.Abs(_stats.RecoverySuccessRate - previousSnapshot.SuccessRate) > _trendThreshold;

            if (significantChange)
            {
                OnTrendDetected?.Invoke(currentTrend);

                if (_enableLogging)
                    ChimeraLogger.Log("FOUNDATION", $"Recovery trend detected: {currentTrend}", this);
            }
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Strategy statistics structure
    /// </summary>
    [System.Serializable]
    public struct StrategyStats
    {
        public RecoveryStrategy Strategy;
        public int TotalAttempts;
        public int SuccessfulAttempts;
        public float TotalDuration;
        public float AverageDuration;
        public float SuccessRate;
    }

    /// <summary>
    /// Recovery metrics snapshot
    /// </summary>
    [System.Serializable]
    public struct RecoveryMetricSnapshot
    {
        public float Timestamp;
        public int TotalAttempts;
        public int SuccessfulAttempts;
        public int FailedAttempts;
        public float SuccessRate;
        public int ActiveRecoveries;
    }

    /// <summary>
    /// Recovery trend enumeration
    /// </summary>
    public enum RecoveryTrend
    {
        Improving,
        Stable,
        Degrading
    }

    #endregion
}
