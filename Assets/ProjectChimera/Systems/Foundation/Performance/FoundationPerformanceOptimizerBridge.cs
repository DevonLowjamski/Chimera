using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Foundation.Performance;

namespace ProjectChimera.Systems.Foundation.Performance
{
    /// <summary>
    /// REFACTORED: Thin MonoBehaviour wrapper for FoundationPerformanceOptimizerService
    /// Bridges Unity lifecycle events to Core layer service
    /// Complies with clean architecture: Unity-specific code in Systems, business logic in Core
    /// </summary>
    public class FoundationPerformanceOptimizerBridge : MonoBehaviour
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
        private static FoundationPerformanceOptimizerBridge _instance;

        public static FoundationPerformanceOptimizerBridge Instance => _instance;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeService();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void InitializeService()
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

        private void OnDestroy()
        {
            if (_instance == this)
            {
                _instance = null;
            }
        }

        #region Public API (delegates to service)

        public bool IsEnabled => _service?.IsEnabled ?? false;
        public OptimizationStats GetStats() => _service?.GetStats() ?? new OptimizationStats();

        public void ProcessOptimizations()
            => _service?.ProcessOptimizations();

        public void ProcessAnalysisResult(string systemName, PerformanceAnalysisResult result)
            => _service?.ProcessAnalysisResult(systemName, result);

        public bool TriggerOptimization(string systemName, OptimizationStrategy strategy = OptimizationStrategy.Auto)
            => _service?.TriggerOptimization(systemName, Time.time, strategy) ?? false;

        public OptimizationData GetOptimizationData(string systemName)
            => _service?.GetOptimizationData(systemName) ?? default;

        public Dictionary<string, OptimizationData> GetAllOptimizationData()
            => _service?.GetAllOptimizationData() ?? new Dictionary<string, OptimizationData>();

        public string[] GetSystemsPendingOptimization()
            => _service?.GetSystemsPendingOptimization() ?? new string[0];

        public void ClearOptimizationQueue()
            => _service?.ClearOptimizationQueue();

        public void SetEnabled(bool enabled)
            => _service?.SetEnabled(enabled);

        // Events
        public event System.Action<string> OnOptimizationTriggered
        {
            add { if (_service != null) _service.OnOptimizationTriggered += value; }
            remove { if (_service != null) _service.OnOptimizationTriggered -= value; }
        }

        public event System.Action<string, bool> OnOptimizationCompleted
        {
            add { if (_service != null) _service.OnOptimizationCompleted += value; }
            remove { if (_service != null) _service.OnOptimizationCompleted -= value; }
        }

        public event System.Action<string, OptimizationStrategy> OnOptimizationStrategyApplied
        {
            add { if (_service != null) _service.OnOptimizationStrategyApplied += value; }
            remove { if (_service != null) _service.OnOptimizationStrategyApplied -= value; }
        }

        #endregion
    }
}
