using UnityEngine;
using System.Collections.Generic;

namespace ProjectChimera.Core.Foundation.Performance
{
    /// <summary>
    /// DEPRECATED: Use FoundationPerformanceOptimizerService (Core.Foundation.Performance) + FoundationPerformanceOptimizerBridge (Systems.Foundation.Performance) instead
    /// This wrapper maintained for backward compatibility during migration
    /// Architecture violation: MonoBehaviour in Core layer
    /// </summary>
    [System.Obsolete("Use FoundationPerformanceOptimizerService (Core.Foundation.Performance) + FoundationPerformanceOptimizerBridge (Systems.Foundation.Performance) instead")]
    public class FoundationPerformanceOptimizer : MonoBehaviour
    {
        [Header("Optimization Settings")]
        [SerializeField] private bool _enableOptimization = true;
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private bool _enableAutomaticOptimization = false;
        [SerializeField] private float _optimizationCooldown = 60f;

        [Header("Optimization Thresholds")]
        [SerializeField] private float _optimizationTriggerThreshold = 0.5f;
        [SerializeField] private int _consecutivePoorThreshold = 3;
        [SerializeField] private float _criticalPerformanceThreshold = 0.3f;

        [Header("Optimization Strategy")]
        [SerializeField] private bool _enableReinitialization = true;
        [SerializeField] private bool _enableResourceOptimization = true;
        [SerializeField] private bool _enableConfigurationTuning = true;
        [SerializeField] private bool _enableGracefulDegradation = true;

        private FoundationPerformanceOptimizerService _service;

        public bool IsEnabled => _service?.IsEnabled ?? false;
        public OptimizationStats GetStats() => _service?.GetStats() ?? new OptimizationStats();

        public System.Action<string> OnOptimizationTriggered
        {
            get => _service?.OnOptimizationTriggered;
            set { if (_service != null) _service.OnOptimizationTriggered = value; }
        }

        public System.Action<string, bool> OnOptimizationCompleted
        {
            get => _service?.OnOptimizationCompleted;
            set { if (_service != null) _service.OnOptimizationCompleted = value; }
        }

        public System.Action<string, OptimizationStrategy> OnOptimizationStrategyApplied
        {
            get => _service?.OnOptimizationStrategyApplied;
            set { if (_service != null) _service.OnOptimizationStrategyApplied = value; }
        }

        private void Start()
        {
            _service = new FoundationPerformanceOptimizerService(
                _enableOptimization,
                _enableLogging,
                _enableAutomaticOptimization,
                _optimizationCooldown,
                _optimizationTriggerThreshold,
                _consecutivePoorThreshold,
                _criticalPerformanceThreshold,
                _enableReinitialization,
                _enableResourceOptimization,
                _enableConfigurationTuning,
                _enableGracefulDegradation
            );
            _service.Initialize();
        }

        public void ProcessOptimizations() => _service?.ProcessOptimizations();
        public void ProcessAnalysisResult(string systemName, PerformanceAnalysisResult result) => _service?.ProcessAnalysisResult(systemName, result);
        public bool TriggerOptimization(string systemName, OptimizationStrategy strategy = OptimizationStrategy.Auto) => _service?.TriggerOptimization(systemName, Time.time, strategy) ?? false;
        public OptimizationData GetOptimizationData(string systemName) => _service?.GetOptimizationData(systemName) ?? default;
        public Dictionary<string, OptimizationData> GetAllOptimizationData() => _service?.GetAllOptimizationData() ?? new Dictionary<string, OptimizationData>();
        public string[] GetSystemsPendingOptimization() => _service?.GetSystemsPendingOptimization() ?? new string[0];
        public void ClearOptimizationQueue() => _service?.ClearOptimizationQueue();
        public void SetEnabled(bool enabled) => _service?.SetEnabled(enabled);
    }

    #region Data Structures

    public enum OptimizationStrategy
    {
        Auto,
        Reinitialization,
        ResourceOptimization,
        ConfigurationTuning,
        GracefulDegradation
    }

    public enum OptimizationPriority
    {
        Critical = 0,
        High = 1,
        Medium = 2,
        Low = 3,
        Manual = 4
    }

    [System.Serializable]
    public struct OptimizationRequest
    {
        public string SystemName;
        public OptimizationStrategy Strategy;
        public OptimizationPriority Priority;
        public float RequestTime;
        public string Reason;
        public string RequestId;
        public float ProcessingStartTime;
        public float CompletionTime;
        public bool Success;
        public float ProcessingDuration;
    }

    [System.Serializable]
    public struct OptimizationAttempt
    {
        public OptimizationStrategy Strategy;
        public float AttemptTime;
        public bool Success;
        public string Reason;
        public OptimizationPriority Priority;
        public string AttemptId;
        public float ProcessingDuration;
        public string RequestId;
    }

    [System.Serializable]
    public struct OptimizationData
    {
        public string SystemName;
        public float FirstOptimizationTime;
        public float LastOptimizationTime;
        public int TotalAttempts;
        public int SuccessfulAttempts;
        public List<OptimizationAttempt> OptimizationHistory;
    }

    [System.Serializable]
    public struct OptimizationStats
    {
        public int TotalOptimizations;
        public int SuccessfulOptimizations;
        public float SuccessRate;
        public int CriticalOptimizations;
        public int AutomaticOptimizations;
    }

    #endregion
}
