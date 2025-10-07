using System;
using System.Collections.Generic;

namespace ProjectChimera.Core.Performance
{
    /// <summary>
    /// PERFORMANCE: Data structures for advanced performance monitoring
    /// Supports comprehensive frame analysis and system optimization
    /// Phase 1: Performance Foundation Implementation
    /// </summary>

    /// <summary>
    /// Performance data for a single frame or sampling period
    /// </summary>
    [System.Serializable]
    public class FramePerformanceData
    {
        public float Timestamp;
        public int FrameCount;
        public int SampleFrameCount;

        // Frame timing
        public float FrameTime; // in milliseconds
        public float AverageFrameTime;
        public float FPS;

        // Memory metrics
        public long GCMemory;
        public long UnityMemory;

        // Rendering metrics
        public int DrawCalls;
        public int Triangles;
        public int Vertices;

        // System metrics
        public int TickableCount;
        public bool UpdateOrchestratorActive;
        public float DistanceFromCenter;
    }

    /// <summary>
    /// Comprehensive performance analysis results
    /// </summary>
    [System.Serializable]
    public class PerformanceAnalysis
    {
        public float Timestamp;
        public int SampleCount;

        // Frame time analysis
        public float AverageFrameTime;
        public float MinFrameTime;
        public float MaxFrameTime;
        public float FrameTimeStdDev;

        // FPS analysis
        public float AverageFPS;
        public float MinFPS;
        public float MaxFPS;

        // Memory analysis
        public double AverageGCMemory;
        public long MaxGCMemory;
        public double AverageUnityMemory;
        public long MaxUnityMemory;

        // Rendering analysis
        public double AverageDrawCalls;
        public int MaxDrawCalls;
        public double AverageTriangles;
        public int MaxTriangles;

        // System health
        public PerformanceHealth PerformanceHealth;
        public List<string> Recommendations;
    }

    /// <summary>
    /// Performance warning information
    /// </summary>
    [System.Serializable]
    public class PerformanceWarning
    {
        public PerformanceWarningType Type;
        public string Message;
        public float Value;
        public float Threshold;
        public PerformanceSeverity Severity;
        public float Timestamp;
    }

    /// <summary>
    /// System-specific performance metrics
    /// </summary>
    [System.Serializable]
    public class SystemPerformanceData
    {
        public string SystemName;
        public float LastUpdateTime;
        public float AverageUpdateTime;
        public float MaxUpdateTime;
        public int UpdateCount;
        public bool IsActive;
        public Dictionary<string, object> CustomMetrics;
    }

    /// <summary>
    /// Overall system performance health assessment
    /// </summary>
    [System.Serializable]
    public class PerformanceHealth
    {
        public float OverallScore; // 0.0 to 1.0
        public float FrameTimeScore;
        public float MemoryScore;
        public float ConsistencyScore;
        public PerformanceLevel HealthLevel;
    }

    /// <summary>
    /// Comprehensive performance report for export
    /// </summary>
    [System.Serializable]
    public class PerformanceReport
    {
        public DateTime GeneratedAt;
        public float MonitoringDuration;
        public int TotalSamples;
        public PerformanceAnalysis RecentAnalysis;
        public List<FramePerformanceData> PerformanceHistory;
        public Dictionary<string, SystemPerformanceData> SystemMetrics;
    }

    /// <summary>
    /// Performance warning types
    /// </summary>
    public enum PerformanceWarningType
    {
        HighFrameTime,
        CriticalFrameTime,
        HighMemoryUsage,
        CriticalMemoryUsage,
        HighDrawCalls,
        HighTriangleCount,
        SystemOverload,
        FrameTimeSpike,
        MemoryLeak
    }

    /// <summary>
    /// Performance severity levels
    /// </summary>
    public enum PerformanceSeverity
    {
        Info,
        Warning,
        Error,
        Critical
    }

    /// <summary>
    /// Performance health levels
    /// </summary>
    public enum PerformanceLevel
    {
        Critical,
        Poor,
        Fair,
        Good,
        Excellent
    }
}