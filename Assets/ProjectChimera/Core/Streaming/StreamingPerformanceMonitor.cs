using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Updates;
using static ProjectChimera.Core.Streaming.StreamingPerformanceService;

namespace ProjectChimera.Core.Streaming
{
    /// <summary>
    /// DEPRECATED: Use StreamingPerformanceService (Core.Streaming) + StreamingPerformanceMonitorBridge (Systems.Streaming) instead
    /// This wrapper maintained for backward compatibility during migration
    /// Architecture violation: MonoBehaviour in Core layer
    /// </summary>
    [System.Obsolete("Use StreamingPerformanceService (Core.Streaming) + StreamingPerformanceMonitorBridge (Systems.Streaming) instead")]
    public class StreamingPerformanceMonitor : MonoBehaviour, ITickable
    {
        [Header("Performance Monitor Settings")]
        [SerializeField] private bool _enableMonitoring = true;
        [SerializeField] private bool _enableAutoOptimization = true;
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private float _monitoringInterval = 1f;

        [Header("Performance Targets")]
        [SerializeField] private float _targetFrameRate = 60f;
        [SerializeField] private float _acceptableFrameRate = 45f;
        [SerializeField] private float _criticalFrameRate = 30f;
        [SerializeField] private long _memoryWarningThreshold = 400 * 1024 * 1024;

        [Header("Optimization Settings")]
        [SerializeField] private float _optimizationReactionTime = 2f;
        [SerializeField] private float _maxStreamingRadiusReduction = 0.5f;
        [SerializeField] private float _maxLODDistanceReduction = 0.3f;

        private StreamingPerformanceService _service;
        private static StreamingPerformanceMonitor _instance;

        public static StreamingPerformanceMonitor Instance => _instance;

        public StreamingOptimizationState CurrentState => _service?.CurrentState ?? StreamingOptimizationState.Optimal;
        public float StreamingRadiusMultiplier => _service?.StreamingRadiusMultiplier ?? 1f;
        public float LODDistanceMultiplier => _service?.LODDistanceMultiplier ?? 1f;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeService();
                UpdateOrchestrator.Instance?.RegisterTickable(this);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void InitializeService()
        {
            _service = new StreamingPerformanceService(
                _enableMonitoring,
                _enableAutoOptimization,
                _enableLogging,
                _monitoringInterval,
                _targetFrameRate,
                _acceptableFrameRate,
                _criticalFrameRate,
                _memoryWarningThreshold,
                _optimizationReactionTime,
                _maxStreamingRadiusReduction,
                _maxLODDistanceReduction
            );
        }

        #region ITickable Implementation

        public int TickPriority => 100;
        public bool IsTickable => enabled && gameObject.activeInHierarchy;

        public void Tick(float deltaTime)
        {
            _service?.Tick(deltaTime, Time.time, Time.unscaledDeltaTime);
        }

        #endregion

        #region Public API (delegates to service)

        public void Initialize()
        {
            var streamingManager = AssetStreamingManager.Instance;
            var lodManager = LODManager.Instance;
            _service?.Initialize(Time.time, streamingManager, lodManager);
        }

        public StreamingPerformanceStats GetStats()
            => _service?.GetStats() ?? new StreamingPerformanceStats();

        public void ForceOptimization(StreamingOptimizationState targetState)
            => _service?.ForceOptimization(targetState, Time.deltaTime);

        public void ResetOptimizations()
            => _service?.ResetOptimizations();

        public List<PerformanceRecommendation> GetRecommendations()
            => _service?.GetRecommendations() ?? new List<PerformanceRecommendation>();

        #endregion

        private void OnDestroy()
        {
            _service?.Cleanup();
            UpdateOrchestrator.Instance?.UnregisterTickable(this);

            if (_instance == this)
                _instance = null;
        }
    }

    #region Data Structures

    /// <summary>
    /// Performance snapshot at a point in time
    /// </summary>
    [System.Serializable]
    public struct PerformanceSnapshot
    {
        public float Timestamp;
        public float FrameRate;
        public long MemoryUsage;
        public int LoadedAssets;
        public int VisibleObjects;
    }

    /// <summary>
    /// Performance recommendation
    /// </summary>
    [System.Serializable]
    public struct PerformanceRecommendation
    {
        public RecommendationPriority Priority;
        public string Category;
        public string Title;
        public string Description;
        public string Impact;
    }

    /// <summary>
    /// Streaming performance statistics
    /// </summary>
    [System.Serializable]
    public struct StreamingPerformanceStats
    {
        public float CurrentFrameRate;
        public float AverageFrameTime;
        public long CurrentMemoryUsage;
        public StreamingOptimizationState OptimizationState;
        public float StreamingRadiusMultiplier;
        public float LODDistanceMultiplier;
        public int LoadedAssets;
        public int LoadingAssets;
        public int VisibleObjects;
        public int RegisteredLODObjects;
        public int StateChanges;
        public int OptimizationChanges;
    }

    /// <summary>
    /// Recommendation priority levels
    /// </summary>
    public enum RecommendationPriority
    {
        Low,
        Medium,
        High,
        Critical
    }

    #endregion
}
