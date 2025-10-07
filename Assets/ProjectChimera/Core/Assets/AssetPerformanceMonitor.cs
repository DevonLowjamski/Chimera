using System;
using System.Collections.Generic;
using System.Linq;

namespace ProjectChimera.Core.Assets
{
    /// <summary>
    /// PHASE 0 REFACTORED: Asset Performance Monitor
    /// Single Responsibility: Monitor performance metrics and generate alerts
    /// Extracted from AddressableAssetStatisticsTracker (767 lines → 4 files <500 lines each)
    /// </summary>
    public class AssetPerformanceMonitor
    {
        private readonly List<PerformanceAlert> _recentAlerts;
        private readonly int _maxAlertHistory = 100;

        // Performance thresholds
        private float _slowLoadThreshold = 1000f; // 1 second
        private float _verySlowLoadThreshold = 5000f; // 5 seconds
        private long _highMemoryThreshold = 50 * 1024 * 1024; // 50MB

        public event Action<PerformanceAlert> OnPerformanceAlert;

        public IReadOnlyList<PerformanceAlert> RecentAlerts => _recentAlerts;

        public AssetPerformanceMonitor()
        {
            _recentAlerts = new List<PerformanceAlert>();
        }

        /// <summary>
        /// Check for performance issues and generate alerts
        /// </summary>
        public void CheckPerformanceAlerts(string address, float loadTime, bool success, long memoryUsage)
        {
            // Check load time thresholds
            if (loadTime > _verySlowLoadThreshold)
            {
                RaiseAlert(AlertType.SlowLoad,
                    $"Very slow load: {address} took {loadTime:F1}ms",
                    address, loadTime, _verySlowLoadThreshold);
            }
            else if (loadTime > _slowLoadThreshold)
            {
                RaiseAlert(AlertType.SlowLoad,
                    $"Slow load: {address} took {loadTime:F1}ms",
                    address, loadTime, _slowLoadThreshold);
            }

            // Check for load failures
            if (!success)
            {
                RaiseAlert(AlertType.LoadFailure,
                    $"Load failed: {address}",
                    address, loadTime, 0f);
            }

            // Check memory usage
            if (memoryUsage > _highMemoryThreshold)
            {
                RaiseAlert(AlertType.HighMemory,
                    $"High memory usage: {address} used {memoryUsage / (1024f * 1024f):F2}MB",
                    address, memoryUsage / (1024f * 1024f), _highMemoryThreshold / (1024f * 1024f));
            }
        }

        /// <summary>
        /// Analyze performance trends
        /// </summary>
        public PerformanceTrends AnalyzeTrends(List<PerformanceMetric> history, TimeSpan period)
        {
            if (history == null || history.Count == 0)
            {
                return new PerformanceTrends
                {
                    TrendSampleSize = 0
                };
            }

            var cutoffTime = DateTime.Now - period;
            var recentMetrics = history.Where(m => m.Timestamp >= cutoffTime).ToList();

            if (recentMetrics.Count < 2)
            {
                return new PerformanceTrends
                {
                    TrendSampleSize = recentMetrics.Count
                };
            }

            // Analyze load time trend
            var loadTimes = recentMetrics.Select(m => m.LoadTime).ToArray();
            var loadTimeTrend = CalculateTrend(loadTimes);

            // Analyze memory usage trend
            var memoryUsages = recentMetrics.Select(m => (float)m.MemoryUsage).ToArray();
            var memoryTrend = CalculateTrend(memoryUsages);

            // Calculate success rate trend
            var successRateFirstHalf = recentMetrics.Take(recentMetrics.Count / 2).Count(m => m.Success) /
                                       (float)Math.Max(1, recentMetrics.Count / 2);
            var successRateSecondHalf = recentMetrics.Skip(recentMetrics.Count / 2).Count(m => m.Success) /
                                        (float)Math.Max(1, recentMetrics.Count - recentMetrics.Count / 2);
            var successRateTrend = successRateSecondHalf - successRateFirstHalf;

            return new PerformanceTrends
            {
                LoadTimeTrend = loadTimeTrend,
                MemoryUsageTrend = memoryTrend,
                SuccessRateTrend = successRateTrend,
                CacheHitRateTrend = 0f, // Would need cache data
                TrendSampleSize = recentMetrics.Count
            };
        }

        /// <summary>
        /// Get recent alerts filtered by type
        /// </summary>
        public List<PerformanceAlert> GetRecentAlerts(AlertType? filterType = null, TimeSpan? period = null)
        {
            var alerts = _recentAlerts.AsEnumerable();

            if (filterType.HasValue)
            {
                alerts = alerts.Where(a => a.Type == filterType.Value);
            }

            if (period.HasValue)
            {
                var cutoffTime = DateTime.Now - period.Value;
                alerts = alerts.Where(a => a.Timestamp >= cutoffTime);
            }

            return alerts.ToList();
        }

        /// <summary>
        /// Clear alert history
        /// </summary>
        public void ClearAlerts()
        {
            _recentAlerts.Clear();
        }

        /// <summary>
        /// Set performance thresholds
        /// </summary>
        public void SetThresholds(float slowLoad, float verySlowLoad, long highMemory)
        {
            _slowLoadThreshold = slowLoad;
            _verySlowLoadThreshold = verySlowLoad;
            _highMemoryThreshold = highMemory;
        }

        #region Private Methods

        /// <summary>
        /// Raise a performance alert
        /// </summary>
        private void RaiseAlert(AlertType type, string message, string assetAddress, float value, float threshold)
        {
            var alert = new PerformanceAlert
            {
                Timestamp = DateTime.Now,
                Type = type,
                Message = message,
                AssetAddress = assetAddress,
                Value = value,
                Threshold = threshold
            };

            _recentAlerts.Add(alert);

            // Maintain alert history size
            while (_recentAlerts.Count > _maxAlertHistory)
            {
                _recentAlerts.RemoveAt(0);
            }

            OnPerformanceAlert?.Invoke(alert);
        }

        /// <summary>
        /// Calculate trend for a series of values
        /// </summary>
        private float CalculateTrend(float[] values)
        {
            if (values.Length < 2) return 0f;

            var firstHalf = values.Take(values.Length / 2).Average();
            var secondHalf = values.Skip(values.Length / 2).Average();

            // Return normalized difference
            return firstHalf > 0 ? (secondHalf - firstHalf) / firstHalf : 0f;
        }

        #endregion
    }
}

