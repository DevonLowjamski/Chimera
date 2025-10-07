using UnityEngine;
using System;

namespace ProjectChimera.Systems.UI.Performance
{
    /// <summary>
    /// UI optimization types for performance recommendations
    /// </summary>
    public enum UIOptimizationType
    {
        EnableUIPooling,
        ReduceUpdateFrequency,
        EnableBatchedUpdates,
        EnableCanvasCulling,
        ReduceMaxUpdatesPerFrame,
        OptimizeCanvasStructure,
        ReduceUIAnimations,
        EnableAsyncUIUpdates
    }

    /// <summary>
    /// UI optimization levels
    /// </summary>
    public enum UIOptimizationLevel
    {
        None = 0,
        Conservative = 1,
        Balanced = 2,
        Aggressive = 3,
        Maximum = 4
    }

    /// <summary>
    /// UI optimization recommendations
    /// </summary>
    [Serializable]
    public struct UIOptimizationRecommendations
    {
        public UIOptimizationType[] RecommendedOptimizations;
        public UIOptimizationLevel SuggestedLevel;
        public string[] PerformanceIssues;
        public float PotentialImprovement;
    }

    /// <summary>
    /// UI component statistics
    /// </summary>
    [Serializable]
    public struct UIComponentStats
    {
        public System.Type ComponentType;
        public int InstanceCount;
        public float AverageUpdateTime;
        public long MemoryUsage;
        public bool IsPerformanceIssue;
    }

    /// <summary>
    /// UI frame data for profiling
    /// </summary>
    [Serializable]
    public struct UIFrameData
    {
        public float Timestamp;
        public float FrameTime;
        public float UITime;
        public int DrawCalls;
        public long MemoryDelta;
    }
}