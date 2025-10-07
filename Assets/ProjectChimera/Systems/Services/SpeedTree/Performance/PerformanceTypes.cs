using System;
using UnityEngine;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Services.SpeedTree.Performance
{
    /// <summary>
    /// Core types and data structures for the SpeedTree performance system
    /// Contains enums, structs, and performance-related definitions
    /// </summary>

    /// <summary>
    /// Quality levels for SpeedTree rendering optimization
    /// </summary>
    public enum SpeedTreeQualityLevel
    {
        Ultra = 0,      // Highest quality, no optimization
        High = 1,       // High quality with minimal optimization
        Medium = 2,     // Balanced quality and performance
        Low = 3,        // Low quality with significant optimization
        Minimal = 4     // Minimal quality for maximum performance
    }

    /// <summary>
    /// LOD (Level of Detail) levels for SpeedTree rendering
    /// </summary>
    public enum SpeedTreeLODLevel
    {
        LOD0 = 0,   // Highest detail
        LOD1 = 1,   // Medium detail
        LOD2 = 2,   // Low detail
        LOD3 = 3,   // Billboard/Cutout
        Culled = 4  // Not rendered
    }

    /// <summary>
    /// Performance metrics for SpeedTree rendering
    /// </summary>
    public enum SpeedTreePerformanceMetric
    {
        FrameTime,
        MemoryUsage,
        DrawCalls,
        Triangles,
        Batches,
        SetPassCalls
    }

    /// <summary>
    /// Culling strategies for SpeedTree optimization
    /// </summary>
    public enum SpeedTreeCullingStrategy
    {
        DistanceBased,
        FrustumBased,
        OcclusionBased,
        Hybrid
    }

    /// <summary>
    /// Batching methods for SpeedTree rendering optimization
    /// </summary>
    public enum SpeedTreeBatchingMethod
    {
        None,
        StaticBatching,
        DynamicBatching,
        GPUInstancing
    }

    /// <summary>
    /// Configuration settings for SpeedTree performance
    /// </summary>
    [System.Serializable]
    public struct SpeedTreePerformanceConfig
    {
        public int MaxVisiblePlants;
        public float CullingDistance;
        public bool EnableGPUInstancing;
        public bool EnableDynamicBatching;
        public SpeedTreeQualityLevel DefaultQuality;
        public float[] LODDistances;
        public float[] LODQualityMultipliers;
        public bool EnablePerformanceMonitoring;
        public float PerformanceUpdateInterval;
        public int TargetFrameRate;
        public float MemoryWarningThresholdMB;
        public SpeedTreeCullingStrategy CullingStrategy;
        public SpeedTreeBatchingMethod BatchingMethod;
        public bool AutoQualityAdjustment;
        public int HistoryBufferSize;
    }

    /// <summary>
    /// Performance metrics data structure
    /// </summary>
    [System.Serializable]
    public struct SpeedTreePerformanceMetrics
    {
        public float AverageFrameTime;
        public float PeakFrameTime;
        public float MemoryUsageMB;
        public int DrawCalls;
        public int Triangles;
        public int Batches;
        public int SetPassCalls;
        public int VisiblePlants;
        public int CulledPlants;
        public float QualityMultiplier;
        public SpeedTreeQualityLevel CurrentQuality;
        public DateTime Timestamp;

        public SpeedTreePerformanceMetrics(DateTime timestamp)
        {
            Timestamp = timestamp;
            AverageFrameTime = 0f;
            PeakFrameTime = 0f;
            MemoryUsageMB = 0f;
            DrawCalls = 0;
            Triangles = 0;
            Batches = 0;
            SetPassCalls = 0;
            VisiblePlants = 0;
            CulledPlants = 0;
            QualityMultiplier = 1f;
            CurrentQuality = SpeedTreeQualityLevel.Medium;
        }
    }

    /// <summary>
    /// Renderer data for individual SpeedTree instances
    /// </summary>
    public class SpeedTreeRendererData
    {
        public GameObject GameObject;
        public Renderer Renderer;
        public MeshFilter MeshFilter;
        public Vector3 Position;
        public float DistanceToCamera;
        public SpeedTreeLODLevel CurrentLOD;
        public SpeedTreeQualityLevel QualityLevel;
        public bool IsVisible;
        public bool IsCulled;
        public int BatchIndex;
        public float LastUpdateTime;
        public Bounds Bounds;

        public SpeedTreeRendererData(GameObject gameObject)
        {
            GameObject = gameObject;
            Renderer = gameObject.GetComponent<Renderer>();
            MeshFilter = gameObject.GetComponent<MeshFilter>();
            Position = gameObject.transform.position;
            Bounds = Renderer != null ? Renderer.bounds : new Bounds(Position, Vector3.one);
            CurrentLOD = SpeedTreeLODLevel.LOD0;
            QualityLevel = SpeedTreeQualityLevel.Medium;
            IsVisible = true;
            IsCulled = false;
            LastUpdateTime = Time.time;
        }
    }

    /// <summary>
    /// LOD configuration data
    /// </summary>
    [System.Serializable]
    public struct SpeedTreeLODConfig
    {
        public float[] Distances;
        public float[] QualityMultipliers;
        public AnimationCurve TransitionCurve;
        public bool EnableSmoothTransitions;
        public float TransitionDuration;

        public SpeedTreeLODConfig(float[] distances, float[] qualityMultipliers)
        {
            Distances = distances;
            QualityMultipliers = qualityMultipliers;
            TransitionCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
            EnableSmoothTransitions = true;
            TransitionDuration = 0.5f;
        }
    }

    /// <summary>
    /// Memory usage statistics
    /// </summary>
    public struct SpeedTreeMemoryStats
    {
        public long TotalAllocated;
        public long MeshMemory;
        public long TextureMemory;
        public long MaterialMemory;
        public int InstanceCount;
        public float AverageMemoryPerInstance;

        public float TotalMB => TotalAllocated / (1024f * 1024f);
        public float MeshMB => MeshMemory / (1024f * 1024f);
        public float TextureMB => TextureMemory / (1024f * 1024f);
        public float MaterialMB => MaterialMemory / (1024f * 1024f);
    }

    /// <summary>
    /// Interface for SpeedTree performance service
    /// </summary>
    public interface ISpeedTreePerformanceService
    {
        bool IsInitialized { get; }
        void Initialize();
        void Shutdown();
        void UpdatePerformance();
        SpeedTreePerformanceMetrics GetPerformanceMetrics();
        void SetQualityLevel(SpeedTreeQualityLevel level);
        void OptimizeForDistance(float maxDistance);
        void OptimizeForQuality(SpeedTreeQualityLevel quality);
        void ForceGC();
        void ClearCache();
    }

    /// <summary>
    /// Interface for LOD management
    /// </summary>
    public interface ISpeedTreeLODManager
    {
        void Initialize(SpeedTreeLODConfig config);
        SpeedTreeLODLevel GetLODForDistance(float distance);
        void UpdateLODs(GameObject[] speedTrees, Vector3 cameraPosition);
        void SetLODDistances(float[] distances);
        float GetQualityMultiplier(SpeedTreeLODLevel lod);
        void ForceLOD(SpeedTreeLODLevel lod);
    }

    /// <summary>
    /// Interface for batching management
    /// </summary>
    public interface ISpeedTreeBatchingManager
    {
        void Initialize(SpeedTreeBatchingMethod method);
        void AddToBatch(GameObject speedTree);
        void RemoveFromBatch(GameObject speedTree);
        void UpdateBatches();
        void ClearBatches();
        int GetBatchCount();
        int GetInstanceCount();
        void OptimizeBatching();
    }

    /// <summary>
    /// Interface for culling management
    /// </summary>
    public interface ISpeedTreeCullingManager
    {
        void Initialize(SpeedTreeCullingStrategy strategy, float cullingDistance);
        void UpdateCulling(GameObject[] speedTrees, Vector3 cameraPosition, Plane[] frustumPlanes);
        void SetCullingDistance(float distance);
        void ForceCull(GameObject speedTree);
        void ForceShow(GameObject speedTree);
        int GetVisibleCount();
        int GetCulledCount();
        void ClearCulling();
    }

    /// <summary>
    /// Interface for memory management
    /// </summary>
    public interface ISpeedTreeMemoryManager
    {
        void Initialize(float warningThresholdMB);
        SpeedTreeMemoryStats GetMemoryStats();
        void OptimizeMemory();
        void UnloadUnusedAssets();
        void CompressTextures();
        bool IsMemoryWarning();
        void ForceGC();
        void MonitorMemoryUsage();
    }
}
