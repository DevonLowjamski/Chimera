using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core.Performance
{
    /// <summary>
    /// REFACTORED: Metrics Processor - Focused metric analysis and computation
    /// Single Responsibility: Processing, aggregating, and analyzing collected metrics
    /// Extracted from MetricsCollectionFramework for better SRP compliance
    /// </summary>
    public class MetricsProcessor
    {
        private readonly int _maxHistorySize;
        private readonly bool _enableLogging;

        // Metric storage and history
        private readonly Dictionary<string, Queue<MetricSnapshot>> _metricHistory = new Dictionary<string, Queue<MetricSnapshot>>();
        private readonly Dictionary<string, MetricAggregates> _aggregates = new Dictionary<string, MetricAggregates>();

        // Events
        public event System.Action<string, MetricAggregates> OnAggregatesUpdated;

        public MetricsProcessor(int maxHistorySize = 300, bool enableLogging = false)
        {
            _maxHistorySize = maxHistorySize;
            _enableLogging = enableLogging;
        }

        #region Metric Processing

        /// <summary>
        /// Process a new metric snapshot
        /// </summary>
        public void ProcessMetric(MetricSnapshot snapshot)
        {
            if (snapshot == null) return;

            var systemName = snapshot.SystemName;

            // Add to history
            AddToHistory(systemName, snapshot);

            // Update aggregates
            UpdateAggregates(systemName);

            if (_enableLogging)
            {
                ChimeraLogger.LogInfo("METRICS", $"Processed metric for {systemName}", null);
            }
        }

        /// <summary>
        /// Process multiple metrics at once
        /// </summary>
        public void ProcessMetrics(IEnumerable<MetricSnapshot> snapshots)
        {
            foreach (var snapshot in snapshots)
            {
                ProcessMetric(snapshot);
            }
        }

        #endregion

        #region History Management

        /// <summary>
        /// Add metric snapshot to history
        /// </summary>
        private void AddToHistory(string systemName, MetricSnapshot snapshot)
        {
            if (!_metricHistory.TryGetValue(systemName, out var history))
            {
                history = new Queue<MetricSnapshot>();
                _metricHistory[systemName] = history;
            }

            history.Enqueue(snapshot);

            // Maintain history size limit
            while (history.Count > _maxHistorySize)
            {
                history.Dequeue();
            }
        }

        /// <summary>
        /// Get metric history for a system
        /// </summary>
        public MetricSnapshot[] GetHistory(string systemName, int count = -1)
        {
            if (!_metricHistory.TryGetValue(systemName, out var history))
            {
                return new MetricSnapshot[0];
            }

            var snapshots = history.ToArray();

            if (count > 0 && count < snapshots.Length)
            {
                return snapshots.Skip(snapshots.Length - count).ToArray();
            }

            return snapshots;
        }

        /// <summary>
        /// Get recent metric history for a system
        /// </summary>
        public MetricSnapshot[] GetRecentHistory(string systemName, float timeWindowSeconds)
        {
            var history = GetHistory(systemName);
            var cutoffTime = Time.time - timeWindowSeconds;

            return history.Where(s => s.Timestamp >= cutoffTime).ToArray();
        }

        /// <summary>
        /// Clear history for a system
        /// </summary>
        public void ClearHistory(string systemName)
        {
            if (_metricHistory.TryGetValue(systemName, out var history))
            {
                history.Clear();
            }

            if (_aggregates.ContainsKey(systemName))
            {
                _aggregates.Remove(systemName);
            }

            if (_enableLogging)
            {
                ChimeraLogger.LogInfo("METRICS", $"Cleared history for {systemName}", null);
            }
        }

        /// <summary>
        /// Clear all metric history
        /// </summary>
        public void ClearAllHistory()
        {
            _metricHistory.Clear();
            _aggregates.Clear();

            if (_enableLogging)
            {
                ChimeraLogger.LogInfo("METRICS", "Cleared all metric history", null);
            }
        }

        #endregion

        #region Aggregation and Analysis

        /// <summary>
        /// Update aggregates for a system
        /// </summary>
        private void UpdateAggregates(string systemName)
        {
            var history = GetHistory(systemName);
            if (history.Length == 0) return;

            var aggregates = CalculateAggregates(history);
            _aggregates[systemName] = aggregates;

            OnAggregatesUpdated?.Invoke(systemName, aggregates);
        }

        /// <summary>
        /// Calculate aggregates from metric history
        /// </summary>
        private MetricAggregates CalculateAggregates(MetricSnapshot[] history)
        {
            if (history.Length == 0)
                return new MetricAggregates();

            var aggregates = new MetricAggregates
            {
                Count = history.Length,
                MinTimestamp = history.Min(h => h.Timestamp),
                MaxTimestamp = history.Max(h => h.Timestamp)
            };

            // Calculate aggregates for each metric type
            var allMetrics = history.SelectMany(h => h.Metrics).GroupBy(m => m.Key);

            foreach (var metricGroup in allMetrics)
            {
                var values = metricGroup.Select(m => m.Value).ToArray();

                var stats = new MetricStatistics
                {
                    MetricName = metricGroup.Key,
                    Count = values.Length,
                    Min = values.Min(),
                    Max = values.Max(),
                    Average = values.Average(),
                    Sum = values.Sum()
                };

                // Calculate standard deviation
                if (values.Length > 1)
                {
                    var mean = stats.Average;
                    var variance = values.Select(v => Math.Pow(v - mean, 2)).Average();
                    stats.StandardDeviation = (float)Math.Sqrt(variance);
                }

                aggregates.Statistics[metricGroup.Key] = stats;
            }

            return aggregates;
        }

        /// <summary>
        /// Get aggregates for a system
        /// </summary>
        public MetricAggregates GetAggregates(string systemName)
        {
            return _aggregates.TryGetValue(systemName, out var aggregates) ? aggregates : new MetricAggregates();
        }

        /// <summary>
        /// Get all system aggregates
        /// </summary>
        public Dictionary<string, MetricAggregates> GetAllAggregates()
        {
            return new Dictionary<string, MetricAggregates>(_aggregates);
        }

        #endregion

        #region Trend Analysis

        /// <summary>
        /// Calculate trend for a specific metric
        /// </summary>
        public TrendAnalysis CalculateTrend(string systemName, string metricName, int sampleCount = 10)
        {
            var history = GetRecentHistory(systemName, sampleCount);
            if (history.Length < 2)
            {
                return new TrendAnalysis { Trend = TrendDirection.Stable };
            }

            var values = history.Where(h => h.Metrics.ContainsKey(metricName))
                                .Select(h => h.Metrics[metricName])
                                .ToArray();

            if (values.Length < 2)
            {
                return new TrendAnalysis { Trend = TrendDirection.Stable };
            }

            // Simple linear regression for trend detection
            var n = values.Length;
            var sumX = n * (n - 1) / 2; // Sum of indices 0, 1, 2, ..., n-1
            var sumY = values.Sum();
            var sumXY = values.Select((v, i) => i * v).Sum();
            var sumX2 = n * (n - 1) * (2 * n - 1) / 6; // Sum of squares 0², 1², 2², ..., (n-1)²

            var slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
            var intercept = (sumY - slope * sumX) / n;

            var trend = new TrendAnalysis
            {
                Slope = slope,
                Intercept = intercept,
                Trend = slope > 0.1f ? TrendDirection.Increasing :
                        slope < -0.1f ? TrendDirection.Decreasing :
                        TrendDirection.Stable,
                Confidence = CalculateTrendConfidence(values, slope, intercept)
            };

            return trend;
        }

        private float CalculateTrendConfidence(float[] values, float slope, float intercept)
        {
            if (values.Length < 3) return 0f;

            // Calculate R-squared
            var mean = values.Average();
            var totalSumSquares = values.Select(v => Math.Pow(v - mean, 2)).Sum();

            var residualSumSquares = values.Select((v, i) =>
                Math.Pow(v - (slope * i + intercept), 2)).Sum();

            var rSquared = 1 - (residualSumSquares / totalSumSquares);
            return (float)Math.Max(0, Math.Min(1, rSquared));
        }

        #endregion

        #region Performance Monitoring

        /// <summary>
        /// Get processing statistics
        /// </summary>
        public ProcessingStatistics GetProcessingStatistics()
        {
            var totalSnapshots = _metricHistory.Values.Sum(h => h.Count);
            var systemCount = _metricHistory.Count;
            var aggregateCount = _aggregates.Count;

            return new ProcessingStatistics
            {
                TotalSnapshots = totalSnapshots,
                TrackedSystems = systemCount,
                AvailableAggregates = aggregateCount,
                MemoryUsageEstimate = EstimateMemoryUsage()
            };
        }

        private long EstimateMemoryUsage()
        {
            // Rough estimate of memory usage
            var snapshotSize = 100; // Estimated bytes per snapshot
            var totalSnapshots = _metricHistory.Values.Sum(h => h.Count);
            return totalSnapshots * snapshotSize;
        }

        #endregion
    }

    /// <summary>
    /// Metric aggregates for a system
    /// </summary>
    [System.Serializable]
    public class MetricAggregates
    {
        public int Count;
        public float MinTimestamp;
        public float MaxTimestamp;
        public Dictionary<string, MetricStatistics> Statistics = new Dictionary<string, MetricStatistics>();
    }

    /// <summary>
    /// Statistics for a specific metric
    /// </summary>
    [System.Serializable]
    public struct MetricStatistics
    {
        public string MetricName;
        public int Count;
        public float Min;
        public float Max;
        public float Average;
        public float Sum;
        public float StandardDeviation;
    }

    /// <summary>
    /// Trend analysis result
    /// </summary>
    [System.Serializable]
    public struct TrendAnalysis
    {
        public float Slope;
        public float Intercept;
        public TrendDirection Trend;
        public float Confidence;
    }

    /// <summary>
    /// Trend direction enumeration
    /// </summary>
    public enum TrendDirection
    {
        Decreasing,
        Stable,
        Increasing
    }

    /// <summary>
    /// Processing statistics
    /// </summary>
    [System.Serializable]
    public struct ProcessingStatistics
    {
        public int TotalSnapshots;
        public int TrackedSystems;
        public int AvailableAggregates;
        public long MemoryUsageEstimate;
    }
}