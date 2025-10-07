using System;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Data.Cultivation.Plant
{
    /// <summary>
    /// PHASE 0 REFACTORED: Sync Performance Collector
    /// Single Responsibility: Record and track sync operations, history, and component-specific performance
    /// Extracted from PlantSyncStatisticsTracker for better SRP compliance
    /// </summary>
    public class SyncPerformanceCollector
    {
        private readonly bool _enableLogging;
        private readonly bool _trackDetailedMetrics;
        private readonly int _maxHistoryEntries;
        private readonly float _performanceAlertThreshold;

        private PlantSyncPerformanceStats _performanceStats = new PlantSyncPerformanceStats();
        private readonly List<SyncPerformanceEntry> _performanceHistory = new List<SyncPerformanceEntry>();
        private readonly Dictionary<string, ComponentPerformanceStats> _componentStats = new Dictionary<string, ComponentPerformanceStats>();

        private readonly Action<PerformanceAlert> _onPerformanceAlert;
        private readonly Action<StatisticsSummary> _onStatisticsUpdated;

        public PlantSyncPerformanceStats PerformanceStats => _performanceStats;
        public int HistoryEntryCount => _performanceHistory.Count;
        public int ComponentCount => _componentStats.Count;

        public SyncPerformanceCollector(
            bool enableLogging,
            bool trackDetailedMetrics,
            int maxHistoryEntries,
            float performanceAlertThreshold,
            Action<PerformanceAlert> onPerformanceAlert,
            Action<StatisticsSummary> onStatisticsUpdated)
        {
            _enableLogging = enableLogging;
            _trackDetailedMetrics = trackDetailedMetrics;
            _maxHistoryEntries = maxHistoryEntries;
            _performanceAlertThreshold = performanceAlertThreshold;
            _onPerformanceAlert = onPerformanceAlert;
            _onStatisticsUpdated = onStatisticsUpdated;
        }

        /// <summary>
        /// Record synchronization operation
        /// </summary>
        public void RecordSyncOperation(string operationType, float syncTime, bool success, int itemsProcessed, float avgSyncTime, float successRate)
        {
            // Update performance stats
            _performanceStats.TotalOperations++;
            _performanceStats.TotalSyncTime += syncTime;

            if (success)
            {
                _performanceStats.SuccessfulOperations++;
                _performanceStats.TotalItemsProcessed += itemsProcessed;
            }
            else
            {
                _performanceStats.FailedOperations++;
            }

            // Track performance history
            if (_trackDetailedMetrics)
            {
                var entry = new SyncPerformanceEntry
                {
                    OperationType = operationType,
                    SyncTime = syncTime,
                    Success = success,
                    ItemsProcessed = itemsProcessed,
                    Timestamp = DateTime.Now
                };

                _performanceHistory.Add(entry);

                // Maintain history size limit
                if (_performanceHistory.Count > _maxHistoryEntries)
                {
                    _performanceHistory.RemoveAt(0);
                }
            }

            // Check for performance alerts
            if (syncTime > _performanceAlertThreshold)
            {
                var alert = new PerformanceAlert
                {
                    AlertType = PerformanceAlertType.SlowOperation,
                    Message = $"Slow {operationType}: {syncTime:F1}ms (threshold: {_performanceAlertThreshold:F1}ms)",
                    SyncTime = syncTime,
                    OperationType = operationType,
                    Timestamp = DateTime.Now
                };

                _onPerformanceAlert?.Invoke(alert);
                _performanceStats.PerformanceAlerts++;
            }

            if (_enableLogging && _performanceStats.TotalOperations % 100 == 0)
            {
                ChimeraLogger.Log("PLANT", $"Sync stats: {_performanceStats.TotalOperations} ops, {successRate:P1} success rate, {avgSyncTime:F1}ms avg");
            }
        }

        /// <summary>
        /// Record component-specific operation
        /// </summary>
        public void RecordComponentOperation(string componentName, string operation, float operationTime, bool success)
        {
            if (!_componentStats.TryGetValue(componentName, out var stats))
            {
                stats = new ComponentPerformanceStats { ComponentName = componentName };
                _componentStats[componentName] = stats;
            }

            stats.TotalOperations++;
            stats.TotalOperationTime += operationTime;

            if (success)
            {
                stats.SuccessfulOperations++;
            }
            else
            {
                stats.FailedOperations++;
            }

            stats.LastOperationTime = DateTime.Now;
            _componentStats[componentName] = stats;
        }

        /// <summary>
        /// Get component performance summary
        /// </summary>
        public List<ComponentPerformanceSummary> GetComponentPerformanceSummary()
        {
            var summaries = new List<ComponentPerformanceSummary>();

            foreach (var kvp in _componentStats)
            {
                var stats = kvp.Value;
                summaries.Add(new ComponentPerformanceSummary
                {
                    ComponentName = stats.ComponentName,
                    TotalOperations = stats.TotalOperations,
                    SuccessRate = stats.TotalOperations > 0 ? (float)stats.SuccessfulOperations / stats.TotalOperations : 0f,
                    AverageOperationTime = stats.TotalOperations > 0 ? stats.TotalOperationTime / stats.TotalOperations : 0f,
                    LastOperationTime = stats.LastOperationTime
                });
            }

            return summaries.OrderByDescending(s => s.TotalOperations).ToList();
        }

        /// <summary>
        /// Get performance history for time period
        /// </summary>
        public List<SyncPerformanceEntry> GetPerformanceHistory(TimeSpan? period = null)
        {
            if (period.HasValue)
            {
                var cutoffTime = DateTime.Now - period.Value;
                return _performanceHistory.Where(e => e.Timestamp >= cutoffTime).ToList();
            }

            return new List<SyncPerformanceEntry>(_performanceHistory);
        }

        /// <summary>
        /// Get performance percentiles
        /// </summary>
        public PerformancePercentiles GetPerformancePercentiles()
        {
            if (_performanceHistory.Count == 0)
            {
                return new PerformancePercentiles();
            }

            var syncTimes = _performanceHistory.Select(e => e.SyncTime).OrderBy(t => t).ToArray();

            return new PerformancePercentiles
            {
                P50 = GetPercentile(syncTimes, 0.5f),
                P75 = GetPercentile(syncTimes, 0.75f),
                P90 = GetPercentile(syncTimes, 0.9f),
                P95 = GetPercentile(syncTimes, 0.95f),
                P99 = GetPercentile(syncTimes, 0.99f),
                Min = syncTimes.Min(),
                Max = syncTimes.Max()
            };
        }

        /// <summary>
        /// Reset all performance data
        /// </summary>
        public void Reset()
        {
            _performanceHistory.Clear();
            _componentStats.Clear();
            _performanceStats = new PlantSyncPerformanceStats
            {
                TotalOperations = 0,
                SuccessfulOperations = 0,
                FailedOperations = 0,
                TotalSyncTime = 0f,
                TotalItemsProcessed = 0,
                PerformanceAlerts = 0
            };
        }

        /// <summary>
        /// Enable or disable detailed metrics tracking
        /// </summary>
        public void SetDetailedMetricsTracking(bool enabled)
        {
            if (!enabled)
            {
                _performanceHistory.Clear();
            }

            if (_enableLogging)
            {
                ChimeraLogger.Log("PLANT", $"Detailed metrics tracking {(enabled ? "enabled" : "disabled")}");
            }
        }

        /// <summary>
        /// Calculate percentile value
        /// </summary>
        private float GetPercentile(float[] sortedArray, float percentile)
        {
            if (sortedArray.Length == 0) return 0f;

            var index = (int)Math.Ceiling(sortedArray.Length * percentile) - 1;
            index = UnityEngine.Mathf.Clamp(index, 0, sortedArray.Length - 1);
            return sortedArray[index];
        }

        /// <summary>
        /// Get current performance statistics
        /// </summary>
        public PlantSyncPerformanceStats GetCurrentStats()
        {
            return _performanceStats;
        }

        /// <summary>
        /// Get history data for analysis
        /// </summary>
        public SyncPerformanceEntry[] GetHistoryDataArray()
        {
            return _performanceHistory.ToArray();
        }
    }
}

