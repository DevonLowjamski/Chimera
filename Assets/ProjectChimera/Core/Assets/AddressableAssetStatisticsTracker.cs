using UnityEngine;
using System;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core.Assets
{
    /// <summary>
    /// PHASE 0 REFACTORED: Addressable Asset Statistics Tracker Coordinator
    /// Single Responsibility: Orchestrate statistics collection, performance monitoring, and reporting
    /// BEFORE: 767 lines (massive SRP violation)
    /// AFTER: 4 files <500 lines each (AssetStatisticsDataStructures, AssetStatisticsCollector, AssetPerformanceMonitor, this coordinator)
    /// </summary>
    public class AddressableAssetStatisticsTracker
    {
        [Header("Statistics Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private bool _enableDetailedTracking = true;
        [SerializeField] private int _maxHistoryEntries = 1000;
        [SerializeField] private float _updateInterval = 5f; // seconds

        // PHASE 0: Component-based architecture (SRP)
        private AssetStatisticsCollector _collector;
        private AssetPerformanceMonitor _performanceMonitor;
        
        // Overall statistics
        private AssetManagerStats _overallStats;
        private SessionStats _currentSession;
        private DateTime _sessionStartTime;

        // State tracking
        private bool _isInitialized = false;
        private float _lastUpdateTime = 0f;

        // Events
        public event Action<AssetManagerStats> OnStatsUpdated;
        public event Action<PerformanceAlert> OnPerformanceAlert;
        public event Action<SessionStats> OnSessionStatsUpdate;
        public event Action<StatisticsReport> OnReportGenerated;

        // Public properties
        public bool IsInitialized => _isInitialized;
        public AssetManagerStats OverallStats => _overallStats;
        public SessionStats CurrentSession => _currentSession;
        public int TrackedAssets => _collector?.AssetStats.Count ?? 0;
        public int TrackedTypes => _collector?.TypeStats.Count ?? 0;

        /// <summary>
        /// Initialize statistics tracker
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            // Initialize components
            _collector = new AssetStatisticsCollector(_enableLogging, _maxHistoryEntries);
            _performanceMonitor = new AssetPerformanceMonitor();

            // Subscribe to performance alerts
            _performanceMonitor.OnPerformanceAlert += alert =>
            {
                OnPerformanceAlert?.Invoke(alert);
            };

            // Reset statistics
            ResetStats();

            _sessionStartTime = DateTime.Now;
            _currentSession = new SessionStats { StartTime = _sessionStartTime };
            _lastUpdateTime = Time.time;

            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("ASSETS", "Addressable Asset Statistics Tracker initialized", null);
            }
        }

        /// <summary>
        /// Record asset load operation
        /// </summary>
        public void RecordAssetLoad(string address, Type assetType, float loadTime, bool success, long memoryUsage = 0)
        {
            if (!_isInitialized) return;

            // Update overall stats
            _overallStats.TotalLoadAttempts++;
            _overallStats.TotalLoadTime += loadTime;

            if (success)
            {
                _overallStats.SuccessfulLoads++;
                _overallStats.TotalMemoryUsage += memoryUsage;
            }
            else
            {
                _overallStats.FailedLoads++;
            }

            // Delegate to collector
            _collector.RecordAssetLoad(address, assetType, loadTime, success, memoryUsage);

            // Check for performance issues
            if (_enableDetailedTracking)
            {
                _performanceMonitor.CheckPerformanceAlerts(address, loadTime, success, memoryUsage);
            }

            // Update session stats
            _currentSession.TotalOperations++;
            if (success)
            {
                _currentSession.SuccessfulOperations++;
            }
            else
            {
                _currentSession.FailedOperations++;
            }

            _currentSession.AverageLoadTime = _collector.AverageLoadTime;

            if (memoryUsage > _currentSession.PeakMemoryUsage)
            {
                _currentSession.PeakMemoryUsage = memoryUsage;
            }
        }

        /// <summary>
        /// Record cache operation
        /// </summary>
        public void RecordCacheOperation(string address, CacheOperationType operationType, bool success)
        {
            if (!_isInitialized) return;

            switch (operationType)
            {
                case CacheOperationType.Hit:
                    _overallStats.CacheHits++;
                    break;
                case CacheOperationType.Miss:
                    _overallStats.CacheMisses++;
                    break;
                case CacheOperationType.Eviction:
                    _overallStats.CacheEvictions++;
                    break;
            }

            _collector.RecordCacheOperation(address, operationType);
        }

        /// <summary>
        /// Update statistics (call periodically)
        /// </summary>
        public void UpdateStatistics()
        {
            if (!_isInitialized) return;

            var currentTime = Time.time;
            if (currentTime - _lastUpdateTime < _updateInterval)
            {
                return;
            }

            _lastUpdateTime = currentTime;

            // Trigger events
            OnStatsUpdated?.Invoke(_overallStats);
            OnSessionStatsUpdate?.Invoke(_currentSession);

            if (_enableLogging)
            {
                LogStatisticsSummary();
            }
        }

        /// <summary>
        /// Get asset usage statistics
        /// </summary>
        public AssetUsageStats? GetAssetStats(string address)
        {
            return _collector?.GetAssetStats(address);
        }

        /// <summary>
        /// Get type usage statistics
        /// </summary>
        public TypeUsageStats? GetTypeStats<T>()
        {
            return GetTypeStats(typeof(T));
        }

        /// <summary>
        /// Get type usage statistics
        /// </summary>
        public TypeUsageStats? GetTypeStats(Type assetType)
        {
            return _collector?.GetTypeStats(assetType);
        }

        /// <summary>
        /// Get performance history
        /// </summary>
        public List<PerformanceMetric> GetPerformanceHistory(TimeSpan? period = null)
        {
            return _collector?.GetPerformanceHistory(period) ?? new List<PerformanceMetric>();
        }

        /// <summary>
        /// Generate comprehensive statistics report
        /// </summary>
        public StatisticsReport GenerateReport()
        {
            if (!_isInitialized)
            {
                return new StatisticsReport();
            }

            var topAssets = _collector.GetTopAssets(10);
            var typeStats = _collector.GetAllTypeStats();
            var recentAlerts = _performanceMonitor.GetRecentAlerts(null, TimeSpan.FromHours(1));
            var trends = _performanceMonitor.AnalyzeTrends(
                _collector.GetPerformanceHistory(TimeSpan.FromHours(1)),
                TimeSpan.FromHours(1));

            var report = StatisticsReport.Create(
                _overallStats,
                _currentSession,
                trends,
                topAssets,
                typeStats,
                recentAlerts
            );

            OnReportGenerated?.Invoke(report);

            if (_enableLogging)
            {
                ChimeraLogger.Log("ASSETS", 
                    $"Statistics report generated - {report.TopAssets.Count} top assets, {report.RecentAlerts.Count} alerts",
                    null);
            }

            return report;
        }

        /// <summary>
        /// Get performance trends
        /// </summary>
        public PerformanceTrends GetPerformanceTrends(TimeSpan period)
        {
            if (!_isInitialized)
            {
                return new PerformanceTrends();
            }

            var history = _collector.GetPerformanceHistory(period);
            return _performanceMonitor.AnalyzeTrends(history, period);
        }

        /// <summary>
        /// Reset all statistics
        /// </summary>
        public void ResetStatistics()
        {
            _collector?.Clear();
            _performanceMonitor?.ClearAlerts();
            ResetStats();

            _sessionStartTime = DateTime.Now;
            _currentSession = new SessionStats { StartTime = _sessionStartTime };

            if (_enableLogging)
            {
                ChimeraLogger.Log("ASSETS", "Asset statistics reset", null);
            }
        }

        /// <summary>
        /// Record handle release
        /// </summary>
        public void RecordHandleRelease()
        {
            if (!_isInitialized) return;
            _overallStats.ReleasedHandles++;
        }

        /// <summary>
        /// Record active handle
        /// </summary>
        public void RecordActiveHandle(bool isActive)
        {
            if (!_isInitialized) return;
            
            if (isActive)
            {
                _overallStats.ActiveHandles++;
            }
            else
            {
                _overallStats.ActiveHandles = Math.Max(0, _overallStats.ActiveHandles - 1);
            }
        }

        #region Private Methods

        /// <summary>
        /// Reset core statistics
        /// </summary>
        private void ResetStats()
        {
            _overallStats = new AssetManagerStats();
        }

        /// <summary>
        /// Log statistics summary
        /// </summary>
        private void LogStatisticsSummary()
        {
            ChimeraLogger.Log("ASSETS", 
                $"Asset Stats - Loads: {_overallStats.SuccessfulLoads}/{_overallStats.TotalLoadAttempts} " +
                $"({_overallStats.SuccessRate:P1}), " +
                $"Cache: {_overallStats.CacheHitRate:P1} hit rate, " +
                $"Avg Load: {_overallStats.AverageLoadTime:F1}ms",
                null);
        }

        #endregion
    }
}

