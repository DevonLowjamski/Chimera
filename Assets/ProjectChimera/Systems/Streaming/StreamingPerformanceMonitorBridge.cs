using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Streaming;
using ProjectChimera.Core.Updates;
using static ProjectChimera.Core.Streaming.StreamingPerformanceService;

namespace ProjectChimera.Systems.Streaming
{
    /// <summary>
    /// REFACTORED: Thin MonoBehaviour wrapper for StreamingPerformanceService
    /// Bridges Unity lifecycle events and ITickable to Core layer service
    /// Complies with clean architecture: Unity-specific code in Systems, business logic in Core
    /// </summary>
    public class StreamingPerformanceMonitorBridge : MonoBehaviour, ITickable
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
        private static StreamingPerformanceMonitorBridge _instance;

        public static StreamingPerformanceMonitorBridge Instance => _instance;

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
}
