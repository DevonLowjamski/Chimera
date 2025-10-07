using UnityEngine;
using System.Collections.Generic;

namespace ProjectChimera.Core.Foundation.Performance
{
    /// <summary>
    /// DEPRECATED: Use OptimizationHistoryService (Core.Foundation.Performance) + OptimizationHistoryTrackerBridge (Systems.Foundation.Performance) instead
    /// This wrapper maintained for backward compatibility during migration
    /// Architecture violation: MonoBehaviour in Core layer
    /// </summary>
    [System.Obsolete("Use OptimizationHistoryService (Core.Foundation.Performance) + OptimizationHistoryTrackerBridge (Systems.Foundation.Performance) instead")]
    public class OptimizationHistoryTracker : MonoBehaviour
    {
        [Header("History Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private int _maxHistoryPerSystem = 50;
        [SerializeField] private int _maxTotalSystems = 100;
        [SerializeField] private float _historyRetentionDays = 7f;

        [Header("Analysis Settings")]
        [SerializeField] private bool _enableTrendAnalysis = true;
        [SerializeField] private int _trendAnalysisSampleSize = 10;
        [SerializeField] private float _analysisInterval = 300f;

        private OptimizationHistoryService _service;

        public bool IsInitialized => _service?.IsInitialized ?? false;
        public OptimizationHistoryStats Stats => _service?.Stats ?? new OptimizationHistoryStats();
        public int TrackedSystemCount => _service?.TrackedSystemCount ?? 0;

        public event System.Action<string, OptimizationAttempt> OnOptimizationRecorded
        {
            add { if (_service != null) _service.OnOptimizationRecorded += value; }
            remove { if (_service != null) _service.OnOptimizationRecorded -= value; }
        }

        public event System.Action<string, OptimizationTrends> OnTrendsUpdated
        {
            add { if (_service != null) _service.OnTrendsUpdated += value; }
            remove { if (_service != null) _service.OnTrendsUpdated -= value; }
        }

        public event System.Action<OptimizationHistoryStats> OnStatsUpdated
        {
            add { if (_service != null) _service.OnStatsUpdated += value; }
            remove { if (_service != null) _service.OnStatsUpdated -= value; }
        }

        private void Awake()
        {
            _service = new OptimizationHistoryService(
                _enableLogging,
                _maxHistoryPerSystem,
                _maxTotalSystems,
                _historyRetentionDays,
                _enableTrendAnalysis,
                _trendAnalysisSampleSize,
                _analysisInterval
            );
        }

        public void Initialize() => _service?.Initialize();

        public void RecordOptimization(OptimizationRequest request, OptimizationStrategy strategy, bool success)
            => _service?.RecordOptimization(request, strategy, success, Time.time);

        public void RecordAttempt(string systemName, OptimizationAttempt attempt)
            => _service?.RecordAttempt(systemName, attempt, Time.time);

        public void ProcessTrendAnalysis() => _service?.ProcessTrendAnalysis(Time.time);
        public OptimizationHistory GetHistory(string systemName) => _service?.GetHistory(systemName);
        public OptimizationTrends GetTrends(string systemName) => _service?.GetTrends(systemName);
        public List<OptimizationAttempt> GetRecentAttempts(string systemName, int count = 10) => _service?.GetRecentAttempts(systemName, count);
        public float GetSuccessRate(string systemName) => _service?.GetSuccessRate(systemName) ?? 0f;
        public OptimizationStrategy GetMostEffectiveStrategy(string systemName) => _service?.GetMostEffectiveStrategy(systemName) ?? OptimizationStrategy.ConfigurationTuning;
        public List<string> GetTrackedSystems() => _service?.GetTrackedSystems() ?? new List<string>();
        public void ClearAllHistory() => _service?.ClearAllHistory();
        public OptimizationHistoryReport GenerateReport() => _service?.GenerateReport();
    }
}
