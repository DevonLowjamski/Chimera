using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Data.Cultivation.Plant
{
    /// <summary>
    /// PHASE 0 REFACTORED: Plant Sync Statistics Tracker (Coordinator)
    /// Single Responsibility: Orchestrate performance tracking using dedicated components
    /// Original file (686 lines) refactored into 4 files (<500 lines each)
    /// </summary>
    [Serializable]
    public class PlantSyncStatisticsTracker
    {
        [Header("Statistics Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private bool _trackDetailedMetrics = true;
        [SerializeField] private int _maxHistoryEntries = 1000;
        [SerializeField] private float _performanceAlertThreshold = 50f; // milliseconds

        // Dependencies
        private SyncPerformanceCollector _collector;
        private SyncTrendAnalyzer _trendAnalyzer;

        // State tracking
        private bool _isInitialized = false;
        private DateTime _trackingStartTime;

        // Events
        public event System.Action<PerformanceAlert> OnPerformanceAlert;
        public event System.Action<StatisticsSummary> OnStatisticsUpdated;
        public event System.Action<TrendAnalysis> OnTrendAnalysisComplete;
        public event System.Action OnStatisticsReset;

        public bool IsInitialized => _isInitialized;
        public PlantSyncPerformanceStats PerformanceStats => _collector?.PerformanceStats ?? new PlantSyncPerformanceStats();
        public int HistoryEntryCount => _collector?.HistoryEntryCount ?? 0;
        public float AverageSyncTime => _trendAnalyzer?.AverageSyncTime ?? 0f;
        public float SuccessRate => _trendAnalyzer?.SuccessRate ?? 0f;

        public void Initialize()
        {
            if (_isInitialized) return;

            // Initialize components
            _collector = new SyncPerformanceCollector(
                _enableLogging,
                _trackDetailedMetrics,
                _maxHistoryEntries,
                _performanceAlertThreshold,
                OnPerformanceAlert,
                OnStatisticsUpdated
            );

            _trendAnalyzer = new SyncTrendAnalyzer(
                _enableLogging,
                OnTrendAnalysisComplete
            );

            _trendAnalyzer.Initialize();
            _trackingStartTime = DateTime.Now;
            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("PLANT", "Plant Sync Statistics Tracker initialized (Coordinator)");
            }
        }

        /// <summary>
        /// Record synchronization operation
        /// </summary>
        public void RecordSyncOperation(string operationType, float syncTime, bool success, int itemsProcessed = 0)
        {
            if (!_isInitialized) return;

            // Update trend analyzer with new values
            _trendAnalyzer.UpdateAverages(syncTime, success);

            // Record operation in collector
            _collector.RecordSyncOperation(operationType, syncTime, success, itemsProcessed, _trendAnalyzer.AverageSyncTime, _trendAnalyzer.SuccessRate);

            // Update statistics summary
            var summary = GenerateStatisticsSummary();
            OnStatisticsUpdated?.Invoke(summary);
        }

        /// <summary>
        /// Record component-specific operation
        /// </summary>
        public void RecordComponentOperation(string componentName, string operation, float operationTime, bool success)
        {
            if (!_isInitialized) return;

            _collector.RecordComponentOperation(componentName, operation, operationTime, success);
        }

        /// <summary>
        /// Perform trend analysis
        /// </summary>
        public TrendAnalysis AnalyzeTrends()
        {
            if (!_isInitialized || HistoryEntryCount < 10)
            {
                return new TrendAnalysis
                {
                    IsValid = false,
                    ErrorMessage = "Insufficient data for trend analysis"
                };
            }

            var historyData = _collector.GetHistoryDataArray();
            return _trendAnalyzer.AnalyzeTrends(historyData);
        }

        /// <summary>
        /// Generate comprehensive statistics summary
        /// </summary>
        public StatisticsSummary GenerateStatisticsSummary()
        {
            var uptime = _isInitialized ? (float)(DateTime.Now - _trackingStartTime).TotalMinutes : 0f;
            var stats = _collector.GetCurrentStats();

            return new StatisticsSummary
            {
                TotalOperations = stats.TotalOperations,
                SuccessfulOperations = stats.SuccessfulOperations,
                FailedOperations = stats.FailedOperations,
                SuccessRate = stats.SuccessRate,
                AverageSyncTime = _trendAnalyzer.AverageSyncTime,
                TotalSyncTime = stats.TotalSyncTime,
                ItemsProcessed = stats.TotalItemsProcessed,
                PerformanceAlerts = stats.PerformanceAlerts,
                UptimeMinutes = uptime,
                ComponentCount = _collector.ComponentCount,
                HistoryEntries = _collector.HistoryEntryCount,
                LastUpdateTime = DateTime.Now
            };
        }

        /// <summary>
        /// Get component performance summary
        /// </summary>
        public List<ComponentPerformanceSummary> GetComponentPerformanceSummary()
        {
            return _collector?.GetComponentPerformanceSummary() ?? new List<ComponentPerformanceSummary>();
        }

        /// <summary>
        /// Get performance history for time period
        /// </summary>
        public List<SyncPerformanceEntry> GetPerformanceHistory(TimeSpan? period = null)
        {
            return _collector?.GetPerformanceHistory(period) ?? new List<SyncPerformanceEntry>();
        }

        /// <summary>
        /// Get performance percentiles
        /// </summary>
        public PerformancePercentiles GetPerformancePercentiles()
        {
            return _collector?.GetPerformancePercentiles() ?? new PerformancePercentiles();
        }

        /// <summary>
        /// Generate performance report
        /// </summary>
        public PerformanceReport GeneratePerformanceReport(TimeSpan? period = null)
        {
            var historyData = GetPerformanceHistory(period);
            var percentiles = GetPerformancePercentiles();
            var componentSummaries = GetComponentPerformanceSummary();
            var trendAnalysis = AnalyzeTrends();

            return new PerformanceReport
            {
                Summary = GenerateStatisticsSummary(),
                Percentiles = percentiles,
                ComponentSummaries = componentSummaries,
                TrendAnalysis = trendAnalysis,
                RecentHistory = historyData.TakeLast(100).ToList(),
                ReportGeneratedTime = DateTime.Now,
                ReportPeriod = period
            };
        }

        /// <summary>
        /// Set performance alert threshold
        /// </summary>
        public void SetPerformanceAlertThreshold(float thresholdMs)
        {
            _performanceAlertThreshold = Mathf.Max(1f, thresholdMs);

            if (_enableLogging)
            {
                ChimeraLogger.Log("PLANT", $"Performance alert threshold set to {_performanceAlertThreshold:F1}ms");
            }
        }

        /// <summary>
        /// Reset all statistics
        /// </summary>
        public void ResetStatistics()
        {
            _collector?.Reset();
            _trendAnalyzer?.Reset();
            _trackingStartTime = DateTime.Now;

            OnStatisticsReset?.Invoke();

            if (_enableLogging)
            {
                ChimeraLogger.Log("PLANT", "Plant sync statistics reset");
            }
        }

        /// <summary>
        /// Enable or disable detailed metrics tracking
        /// </summary>
        public void SetDetailedMetricsTracking(bool enabled)
        {
            _trackDetailedMetrics = enabled;
            _collector?.SetDetailedMetricsTracking(enabled);

            if (_enableLogging)
            {
                ChimeraLogger.Log("PLANT", $"Detailed metrics tracking {(enabled ? "enabled" : "disabled")}");
            }
        }

        /// <summary>
        /// Get current performance grade
        /// </summary>
        public PerformanceGrade GetCurrentPerformanceGrade()
        {
            if (!_isInitialized || _trendAnalyzer == null || _collector == null)
                return PerformanceGrade.Unknown;

            var stats = _collector.GetCurrentStats();
            return _trendAnalyzer.GetCurrentPerformanceGrade(stats);
        }
    }
}

