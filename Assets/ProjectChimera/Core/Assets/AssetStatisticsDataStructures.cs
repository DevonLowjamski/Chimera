using System;
using System.Collections.Generic;

namespace ProjectChimera.Core.Assets
{
    /// <summary>
    /// PHASE 0 REFACTORED: Asset Statistics Data Structures
    /// Single Responsibility: Define all statistics and performance data types
    /// Extracted from AddressableAssetStatisticsTracker (767 lines → 4 files <500 lines each)
    /// </summary>

    /// <summary>
    /// Cache operation types
    /// </summary>
    public enum CacheOperationType
    {
        Hit,
        Miss,
        Eviction
    }

    /// <summary>
    /// Overall asset manager statistics
    /// </summary>
    [Serializable]
    public struct AssetManagerStats
    {
        public int TotalLoadAttempts;
        public int SuccessfulLoads;
        public int FailedLoads;
        public float TotalLoadTime;
        public long TotalMemoryUsage;
        public int CacheHits;
        public int CacheMisses;
        public int CacheEvictions;
        public int ActiveHandles;
        public int ReleasedHandles;
        public int TotalReleases; // Total asset releases
        public float AverageLoadTime;
        public float SuccessRate;
        public float CacheHitRate;
    }

    /// <summary>
    /// Per-asset usage statistics
    /// </summary>
    [Serializable]
    public struct AssetUsageStats
    {
        public string Address;
        public Type AssetType;
        public int LoadCount;
        public int FailureCount;
        public float TotalLoadTime;
        public float AverageLoadTime;
        public float LastLoadTime;
        public DateTime FirstLoadTime;
        public DateTime LastLoadTime_DateTime;
        public long MemoryUsage;
        public int CacheHits;
        public int CacheMisses;
        public int ActiveReferences;

        public float SuccessRate => LoadCount > 0 ? (float)(LoadCount - FailureCount) / LoadCount : 0f;
    }

    /// <summary>
    /// Per-type usage statistics
    /// </summary>
    [Serializable]
    public struct TypeUsageStats
    {
        public Type AssetType;
        public int TotalLoads;
        public int FailedLoads;
        public float TotalLoadTime;
        public long TotalMemoryUsage;
        public int UniqueAssets;

        public float AverageLoadTime => TotalLoads > 0 ? TotalLoadTime / TotalLoads : 0f;
        public float SuccessRate => TotalLoads > 0 ? (float)(TotalLoads - FailedLoads) / TotalLoads : 0f;
    }

    /// <summary>
    /// Performance metric entry
    /// </summary>
    [Serializable]
    public struct PerformanceMetric
    {
        public DateTime Timestamp;
        public string AssetAddress;
        public Type AssetType;
        public float LoadTime;
        public bool Success;
        public long MemoryUsage;
    }

    /// <summary>
    /// Session statistics
    /// </summary>
    [Serializable]
    public struct SessionStats
    {
        public DateTime StartTime;
        public int TotalOperations;
        public int SuccessfulOperations;
        public int FailedOperations;
        public float PeakMemoryUsage;
        public float AverageLoadTime;

        public TimeSpan Duration => DateTime.Now - StartTime;
        public float SuccessRate => TotalOperations > 0 ? (float)SuccessfulOperations / TotalOperations : 0f;
    }

    /// <summary>
    /// Performance alert
    /// </summary>
    [Serializable]
    public struct PerformanceAlert
    {
        public DateTime Timestamp;
        public AlertType Type;
        public string Message;
        public string AssetAddress;
        public float Value;
        public float Threshold;
    }

    /// <summary>
    /// Alert types
    /// </summary>
    public enum AlertType
    {
        SlowLoad,
        HighMemory,
        LoadFailure,
        CacheMiss
    }

    /// <summary>
    /// Performance trends analysis
    /// </summary>
    [Serializable]
    public struct PerformanceTrends
    {
        public float LoadTimeTrend; // Positive = getting slower, Negative = getting faster
        public float MemoryUsageTrend;
        public float SuccessRateTrend;
        public float CacheHitRateTrend;
        public int TrendSampleSize;

        public bool IsLoadTimeImproving => LoadTimeTrend < -0.05f;
        public bool IsLoadTimeDegrading => LoadTimeTrend > 0.05f;
        public bool IsMemoryIncreasing => MemoryUsageTrend > 0.05f;
    }

    /// <summary>
    /// Comprehensive statistics report
    /// </summary>
    [Serializable]
    public struct StatisticsReport
    {
        public DateTime GeneratedAt;
        public AssetManagerStats OverallStats;
        public SessionStats CurrentSession;
        public PerformanceTrends Trends;
        public List<AssetUsageStats> TopAssets;
        public List<TypeUsageStats> TypeBreakdown;
        public List<PerformanceAlert> RecentAlerts;

        public static StatisticsReport Create(
            AssetManagerStats overall,
            SessionStats session,
            PerformanceTrends trends,
            List<AssetUsageStats> topAssets,
            List<TypeUsageStats> typeStats,
            List<PerformanceAlert> alerts)
        {
            return new StatisticsReport
            {
                GeneratedAt = DateTime.Now,
                OverallStats = overall,
                CurrentSession = session,
                Trends = trends,
                TopAssets = topAssets ?? new List<AssetUsageStats>(),
                TypeBreakdown = typeStats ?? new List<TypeUsageStats>(),
                RecentAlerts = alerts ?? new List<PerformanceAlert>()
            };
        }
    }

    /// <summary>
    /// Moving average calculator
    /// </summary>
    public class MovingAverage
    {
        private readonly Queue<float> _values;
        private readonly int _maxSize;
        private float _sum;

        public float Average => _values.Count > 0 ? _sum / _values.Count : 0f;
        public int Count => _values.Count;

        public MovingAverage(int maxSize)
        {
            _maxSize = maxSize;
            _values = new Queue<float>(maxSize);
            _sum = 0f;
        }

        public void AddValue(float value)
        {
            _values.Enqueue(value);
            _sum += value;

            while (_values.Count > _maxSize)
            {
                _sum -= _values.Dequeue();
            }
        }

        public void Clear()
        {
            _values.Clear();
            _sum = 0f;
        }
    }
}

