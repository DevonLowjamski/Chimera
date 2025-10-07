using UnityEngine;
using ProjectChimera.Core.Foundation.Performance;
using ProjectChimera.Core.Updates;

namespace ProjectChimera.Systems.Foundation.Performance
{
    /// <summary>
    /// REFACTORED: Thin MonoBehaviour wrapper for OptimizationHistoryService with ITickable
    /// Bridges Unity lifecycle events to Core layer service
    /// Complies with clean architecture: Unity-specific code in Systems, business logic in Core
    /// Uses ITickable for centralized update management
    /// </summary>
    public class OptimizationHistoryTrackerBridge : MonoBehaviour, ITickable
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
        private static OptimizationHistoryTrackerBridge _instance;

        public static OptimizationHistoryTrackerBridge Instance => _instance;

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
            _service = new OptimizationHistoryService(
                _enableLogging,
                _maxHistoryPerSystem,
                _maxTotalSystems,
                _historyRetentionDays,
                _enableTrendAnalysis,
                _trendAnalysisSampleSize,
                _analysisInterval
            );
            _service.Initialize();
        }

        #region ITickable Implementation

        public int TickPriority => ProjectChimera.Core.Updates.TickPriority.AnalyticsManager;
        public bool IsTickable => enabled && gameObject.activeInHierarchy && (_service?.IsInitialized ?? false);

        public void Tick(float deltaTime)
        {
            if (_service != null)
            {
                _service.ProcessTrendAnalysis(Time.time);
            }
        }

        private void OnEnable()
        {
            UpdateOrchestrator.Instance?.RegisterTickable(this);
        }

        private void OnDisable()
        {
            UpdateOrchestrator.Instance?.UnregisterTickable(this);
        }

        #endregion

        private void OnDestroy()
        {
            if (_service != null)
            {
                _service.ClearAllHistory();
            }

            if (_instance == this)
            {
                _instance = null;
            }
        }

        #region Public API (delegates to service)

        public bool IsInitialized => _service?.IsInitialized ?? false;
        public OptimizationHistoryStats Stats => _service?.Stats ?? new OptimizationHistoryStats();
        public int TrackedSystemCount => _service?.TrackedSystemCount ?? 0;

        public void Initialize()
            => _service?.Initialize();

        public void RecordOptimization(OptimizationRequest request, OptimizationStrategy strategy, bool success)
            => _service?.RecordOptimization(request, strategy, success, Time.time);

        public void RecordAttempt(string systemName, OptimizationAttempt attempt)
            => _service?.RecordAttempt(systemName, attempt, Time.time);

        public void ProcessTrendAnalysis()
            => _service?.ProcessTrendAnalysis(Time.time);

        public OptimizationHistory GetHistory(string systemName)
            => _service?.GetHistory(systemName);

        public OptimizationTrends GetTrends(string systemName)
            => _service?.GetTrends(systemName);

        public System.Collections.Generic.List<OptimizationAttempt> GetRecentAttempts(string systemName, int count = 10)
            => _service?.GetRecentAttempts(systemName, count);

        public float GetSuccessRate(string systemName)
            => _service?.GetSuccessRate(systemName) ?? 0f;

        public OptimizationStrategy GetMostEffectiveStrategy(string systemName)
            => _service?.GetMostEffectiveStrategy(systemName) ?? OptimizationStrategy.ConfigurationTuning;

        public System.Collections.Generic.List<string> GetTrackedSystems()
            => _service?.GetTrackedSystems() ?? new System.Collections.Generic.List<string>();

        public void ClearAllHistory()
            => _service?.ClearAllHistory();

        public OptimizationHistoryReport GenerateReport()
            => _service?.GenerateReport();

        // Events
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

        #endregion
    }
}
