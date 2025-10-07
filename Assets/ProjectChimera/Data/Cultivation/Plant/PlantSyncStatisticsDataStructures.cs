using System;
using System.Collections.Generic;

namespace ProjectChimera.Data.Cultivation.Plant
{
    /// <summary>
    /// PHASE 0 REFACTORED: Plant Sync Statistics Data Structures
    /// Single Responsibility: Define all data types related to plant sync statistics and performance tracking
    /// Extracted from PlantSyncStatisticsTracker for better SRP compliance
    /// </summary>

    /// <summary>
    /// Plant sync performance statistics
    /// </summary>
    [Serializable]
    public struct PlantSyncPerformanceStats
    {
        public int TotalOperations;
        public int SuccessfulOperations;
        public int FailedOperations;
        public float TotalSyncTime;
        public int TotalItemsProcessed;
        public int PerformanceAlerts;

        public readonly float SuccessRate => TotalOperations > 0 ? (float)SuccessfulOperations / TotalOperations : 0f;
        public readonly float AverageSyncTime => TotalOperations > 0 ? TotalSyncTime / TotalOperations : 0f;
    }

    /// <summary>
    /// Sync performance entry
    /// </summary>
    [Serializable]
    public struct SyncPerformanceEntry
    {
        public string OperationType;
        public float SyncTime;
        public bool Success;
        public int ItemsProcessed;
        public DateTime Timestamp;
    }

    /// <summary>
    /// Component performance statistics
    /// </summary>
    [Serializable]
    public struct ComponentPerformanceStats
    {
        public string ComponentName;
        public int TotalOperations;
        public int SuccessfulOperations;
        public int FailedOperations;
        public float TotalOperationTime;
        public DateTime LastOperationTime;
    }

    /// <summary>
    /// Component performance summary
    /// </summary>
    [Serializable]
    public struct ComponentPerformanceSummary
    {
        public string ComponentName;
        public int TotalOperations;
        public float SuccessRate;
        public float AverageOperationTime;
        public DateTime LastOperationTime;
    }

    /// <summary>
    /// Statistics summary
    /// </summary>
    [Serializable]
    public struct StatisticsSummary
    {
        public int TotalOperations;
        public int SuccessfulOperations;
        public int FailedOperations;
        public float SuccessRate;
        public float AverageSyncTime;
        public float TotalSyncTime;
        public int ItemsProcessed;
        public int PerformanceAlerts;
        public float UptimeMinutes;
        public int ComponentCount;
        public int HistoryEntries;
        public DateTime LastUpdateTime;
    }

    /// <summary>
    /// Performance percentiles
    /// </summary>
    [Serializable]
    public struct PerformancePercentiles
    {
        public float P50;  // Median
        public float P75;
        public float P90;
        public float P95;
        public float P99;
        public float Min;
        public float Max;
    }

    /// <summary>
    /// Trend analysis result
    /// </summary>
    [Serializable]
    public struct TrendAnalysis
    {
        public bool IsValid;
        public string ErrorMessage;
        public TrendDirection SyncTimeTrend;
        public TrendDirection SuccessRateTrend;
        public float RecentAverageSyncTime;
        public float RecentSuccessRate;
        public float PerformanceChangePercent;
        public PerformanceGrade OverallPerformanceGrade;
        public DateTime AnalysisTime;
    }

    /// <summary>
    /// Performance report
    /// </summary>
    [Serializable]
    public struct PerformanceReport
    {
        public StatisticsSummary Summary;
        public PerformancePercentiles Percentiles;
        public List<ComponentPerformanceSummary> ComponentSummaries;
        public TrendAnalysis TrendAnalysis;
        public List<SyncPerformanceEntry> RecentHistory;
        public DateTime ReportGeneratedTime;
        public TimeSpan? ReportPeriod;
    }

    /// <summary>
    /// Performance alert
    /// </summary>
    [Serializable]
    public struct PerformanceAlert
    {
        public PerformanceAlertType AlertType;
        public string Message;
        public float SyncTime;
        public string OperationType;
        public DateTime Timestamp;
    }

    /// <summary>
    /// Performance alert types
    /// </summary>
    public enum PerformanceAlertType
    {
        SlowOperation = 0,
        HighFailureRate = 1,
        MemoryIssue = 2
    }

    /// <summary>
    /// Trend direction
    /// </summary>
    public enum TrendDirection
    {
        Stable = 0,
        Increasing = 1,
        Decreasing = 2
    }

    /// <summary>
    /// Performance grade
    /// </summary>
    public enum PerformanceGrade
    {
        Unknown = 0,
        Poor = 1,
        Fair = 2,
        Good = 3,
        Excellent = 4
    }
}

