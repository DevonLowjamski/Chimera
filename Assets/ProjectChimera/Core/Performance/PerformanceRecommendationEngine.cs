using UnityEngine;
using System.Collections.Generic;

namespace ProjectChimera.Core.Performance
{
    /// <summary>
    /// DEPRECATED: Use PerformanceRecommendationService (Core.Performance) + PerformanceRecommendationEngineBridge (Systems.Performance) instead
    /// This wrapper maintained for backward compatibility during migration
    /// Architecture violation: MonoBehaviour in Core layer
    /// </summary>
    [System.Obsolete("Use PerformanceRecommendationService (Core.Performance) + PerformanceRecommendationEngineBridge (Systems.Performance) instead")]
    public class PerformanceRecommendationEngine : MonoBehaviour, ProjectChimera.Core.Updates.ITickable
    {
        [Header("Recommendation Settings")]
        [SerializeField] private bool _enableRecommendations = true;
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private float _analysisInterval = 5.0f;
        [SerializeField] private int _analysisHistorySize = 60;

        [Header("Recommendation Thresholds")]
        [SerializeField] private float _frameTimeThreshold = 20f;
        [SerializeField] private long _memoryThreshold = 400 * 1024 * 1024;
        [SerializeField] private int _drawCallThreshold = 1000;
        [SerializeField] private int _triangleThreshold = 100000;

        [Header("Priority Settings")]
        [SerializeField] private bool _prioritizeCriticalIssues = true;
        [SerializeField] private int _maxRecommendationsPerAnalysis = 5;

        private PerformanceRecommendationService _service;
        private AdvancedPerformanceMonitor _performanceMonitor;
        private MetricsCollectionFramework _metricsFramework;

        public event System.Action<PerformanceRecommendation> OnRecommendationGenerated
        {
            add { if (_service != null) _service.OnRecommendationGenerated += value; }
            remove { if (_service != null) _service.OnRecommendationGenerated -= value; }
        }

        public event System.Action<List<PerformanceRecommendation>> OnRecommendationSetUpdated
        {
            add { if (_service != null) _service.OnRecommendationSetUpdated += value; }
            remove { if (_service != null) _service.OnRecommendationSetUpdated -= value; }
        }

        private void Awake()
        {
            _service = new PerformanceRecommendationService(
                _enableRecommendations,
                _enableLogging,
                _analysisInterval,
                _analysisHistorySize,
                _frameTimeThreshold,
                _memoryThreshold,
                _drawCallThreshold,
                _triangleThreshold,
                _prioritizeCriticalIssues,
                _maxRecommendationsPerAnalysis
            );

            _metricsFramework = MetricsCollectionFramework.Instance;
            _service.Initialize(_performanceMonitor, _metricsFramework);

            ProjectChimera.Core.Updates.UpdateOrchestrator.Instance.RegisterTickable(this);
        }

        private void OnDestroy()
        {
            if (ProjectChimera.Core.Updates.UpdateOrchestrator.Instance != null)
                ProjectChimera.Core.Updates.UpdateOrchestrator.Instance.UnregisterTickable(this);
        }

        public int TickPriority => -30;
        public bool IsTickable => _service?.IsEnabled ?? false && enabled;
        public void Tick(float deltaTime) => _service?.Tick(deltaTime, Time.time);

        public List<PerformanceRecommendation> GetActiveRecommendations() => _service?.GetActiveRecommendations() ?? new List<PerformanceRecommendation>();
        public List<PerformanceRecommendation> GetRecommendationHistory(int count = 20) => _service?.GetRecommendationHistory(count) ?? new List<PerformanceRecommendation>();

        [ContextMenu("Generate Immediate Recommendations")]
        public void GenerateImmediateRecommendations() => _service?.AnalyzePerformanceAndGenerateRecommendations(Time.time);

        [ContextMenu("Clear Recommendation History")]
        public void ClearRecommendationHistory() => _service?.ClearRecommendationHistory();
    }
}
