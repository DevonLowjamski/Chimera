using System;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core.Assets
{
    /// <summary>
    /// PHASE 0 REFACTORED: Asset Statistics Collector
    /// Single Responsibility: Collect and aggregate asset usage statistics
    /// Extracted from AddressableAssetStatisticsTracker (767 lines → 4 files <500 lines each)
    /// </summary>
    public class AssetStatisticsCollector
    {
        private readonly Dictionary<string, AssetUsageStats> _assetStats;
        private readonly Dictionary<Type, TypeUsageStats> _typeStats;
        private readonly List<PerformanceMetric> _performanceHistory;
        private readonly MovingAverage _loadTimeAverage;
        private readonly MovingAverage _memoryUsageAverage;
        private readonly bool _enableLogging;
        private readonly int _maxHistoryEntries;

        public IReadOnlyDictionary<string, AssetUsageStats> AssetStats => _assetStats;
        public IReadOnlyDictionary<Type, TypeUsageStats> TypeStats => _typeStats;
        public IReadOnlyList<PerformanceMetric> PerformanceHistory => _performanceHistory;
        public float AverageLoadTime => _loadTimeAverage.Average;
        public float AverageMemoryUsage => _memoryUsageAverage.Average;

        public AssetStatisticsCollector(bool enableLogging, int maxHistoryEntries)
        {
            _assetStats = new Dictionary<string, AssetUsageStats>();
            _typeStats = new Dictionary<Type, TypeUsageStats>();
            _performanceHistory = new List<PerformanceMetric>();
            _loadTimeAverage = new MovingAverage(100);
            _memoryUsageAverage = new MovingAverage(50);
            _enableLogging = enableLogging;
            _maxHistoryEntries = maxHistoryEntries;
        }

        /// <summary>
        /// Record asset load operation
        /// </summary>
        public void RecordAssetLoad(string address, Type assetType, float loadTime, bool success, long memoryUsage)
        {
            // Update asset-specific stats
            UpdateAssetStats(address, assetType, loadTime, success, memoryUsage);

            // Update type-specific stats
            UpdateTypeStats(assetType, loadTime, success, memoryUsage);

            // Record performance metric
            RecordPerformanceMetric(address, assetType, loadTime, success, memoryUsage);

            // Update moving averages
            _loadTimeAverage.AddValue(loadTime);
            if (memoryUsage > 0)
            {
                _memoryUsageAverage.AddValue(memoryUsage);
            }
        }

        /// <summary>
        /// Record cache operation
        /// </summary>
        public void RecordCacheOperation(string address, CacheOperationType operationType)
        {
            if (!_assetStats.TryGetValue(address, out var stats))
                return;

            switch (operationType)
            {
                case CacheOperationType.Hit:
                    stats.CacheHits++;
                    break;
                case CacheOperationType.Miss:
                    stats.CacheMisses++;
                    break;
                case CacheOperationType.Eviction:
                    // Handle eviction if needed
                    break;
            }

            _assetStats[address] = stats;
        }

        /// <summary>
        /// Get asset statistics
        /// </summary>
        public AssetUsageStats? GetAssetStats(string address)
        {
            return _assetStats.TryGetValue(address, out var stats) ? stats : (AssetUsageStats?)null;
        }

        /// <summary>
        /// Get type statistics
        /// </summary>
        public TypeUsageStats? GetTypeStats(Type assetType)
        {
            return _typeStats.TryGetValue(assetType, out var stats) ? stats : (TypeUsageStats?)null;
        }

        /// <summary>
        /// Get performance history for specific period
        /// </summary>
        public List<PerformanceMetric> GetPerformanceHistory(TimeSpan? period = null)
        {
            if (!period.HasValue)
            {
                return new List<PerformanceMetric>(_performanceHistory);
            }

            var cutoffTime = DateTime.Now - period.Value;
            return _performanceHistory.Where(p => p.Timestamp >= cutoffTime).ToList();
        }

        /// <summary>
        /// Get top assets by load count
        /// </summary>
        public List<AssetUsageStats> GetTopAssets(int count = 10)
        {
            return _assetStats.Values
                .OrderByDescending(a => a.LoadCount)
                .Take(count)
                .ToList();
        }

        /// <summary>
        /// Get all type statistics
        /// </summary>
        public List<TypeUsageStats> GetAllTypeStats()
        {
            return _typeStats.Values.ToList();
        }

        /// <summary>
        /// Clear all statistics
        /// </summary>
        public void Clear()
        {
            _assetStats.Clear();
            _typeStats.Clear();
            _performanceHistory.Clear();
            _loadTimeAverage.Clear();
            _memoryUsageAverage.Clear();

            if (_enableLogging)
            {
                ChimeraLogger.Log("ASSETS", "Asset statistics cleared", null);
            }
        }

        #region Private Methods

        /// <summary>
        /// Update asset-specific statistics
        /// </summary>
        private void UpdateAssetStats(string address, Type assetType, float loadTime, bool success, long memoryUsage)
        {
            if (!_assetStats.TryGetValue(address, out var stats))
            {
                stats = new AssetUsageStats
                {
                    Address = address,
                    AssetType = assetType,
                    FirstLoadTime = DateTime.Now
                };
            }

            stats.LoadCount++;
            if (!success)
            {
                stats.FailureCount++;
            }

            stats.TotalLoadTime += loadTime;
            stats.AverageLoadTime = stats.LoadCount > 0 ? stats.TotalLoadTime / stats.LoadCount : 0f;
            stats.LastLoadTime = loadTime;
            stats.LastLoadTime_DateTime = DateTime.Now;

            if (success && memoryUsage > 0)
            {
                stats.MemoryUsage = memoryUsage;
            }

            _assetStats[address] = stats;
        }

        /// <summary>
        /// Update type-specific statistics
        /// </summary>
        private void UpdateTypeStats(Type assetType, float loadTime, bool success, long memoryUsage)
        {
            if (!_typeStats.TryGetValue(assetType, out var stats))
            {
                stats = new TypeUsageStats
                {
                    AssetType = assetType,
                    UniqueAssets = 0
                };
            }

            stats.TotalLoads++;
            if (!success)
            {
                stats.FailedLoads++;
            }

            stats.TotalLoadTime += loadTime;
            if (success && memoryUsage > 0)
            {
                stats.TotalMemoryUsage += memoryUsage;
            }

            _typeStats[assetType] = stats;
        }

        /// <summary>
        /// Record performance metric
        /// </summary>
        private void RecordPerformanceMetric(string address, Type assetType, float loadTime, bool success, long memoryUsage)
        {
            var metric = new PerformanceMetric
            {
                Timestamp = DateTime.Now,
                AssetAddress = address,
                AssetType = assetType,
                LoadTime = loadTime,
                Success = success,
                MemoryUsage = memoryUsage
            };

            _performanceHistory.Add(metric);

            // Limit history size
            while (_performanceHistory.Count > _maxHistoryEntries)
            {
                _performanceHistory.RemoveAt(0);
            }
        }

        #endregion
    }
}

