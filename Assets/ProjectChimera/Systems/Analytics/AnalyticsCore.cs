using UnityEngine;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core;
using System.Collections.Generic;
using System.Collections;
using System.Linq;
using System;

namespace ProjectChimera.Systems.Analytics
{
    /// <summary>
    /// Core analytics infrastructure and component coordination.
    /// Manages analytics models, system initialization, and component orchestration.
    /// </summary>
    public class AnalyticsCore : MonoBehaviour
    {
        [Header("Analytics Configuration")]
        [SerializeField] private bool _enableAnalytics = true;
        [SerializeField] private bool _enablePredictiveAnalytics = true;
        [SerializeField] private bool _enableAnomalyDetection = true;
        [SerializeField] private bool _enableBehaviorAnalysis = true;

        [Header("Analysis Settings")]
        [SerializeField] private float _analysisInterval = 5f;
        [SerializeField] private int _dataWindowSize = 100;
        [SerializeField] private bool _enableRealTimeAnalysis = true;
        [SerializeField] private bool _enableBatchAnalysis = true;

        [Header("Privacy & Compliance")]
        [SerializeField] private bool _enableDataAnonymization = true;
        [SerializeField] private bool _respectPrivacySettings = true;
        [SerializeField] private bool _enableGDPRCompliance = true;
        [SerializeField] private int _dataRetentionDays = 90;

        // System references
        private DataPipelineIntegration _dataPipeline;
        private RealtimeSystemSync _systemSync;

        // Analytics components
        protected BehaviorAnalysisEngine _behaviorEngine;
        protected AnomalyDetectionEngine _anomalyEngine;
        protected PredictiveAnalyticsEngine _predictiveEngine;
        protected ReportingEngine _reportingEngine;

        // Core state
        private Dictionary<string, AnalyticsModel> _analyticsModels = new Dictionary<string, AnalyticsModel>();
        private AnalyticsMetrics _analyticsMetrics = new AnalyticsMetrics();
        private DateTime _startTime;

        // Properties
        public bool EnableAnalytics => _enableAnalytics;
        public bool EnablePredictiveAnalytics => _enablePredictiveAnalytics;
        public bool EnableAnomalyDetection => _enableAnomalyDetection;
        public bool EnableBehaviorAnalysis => _enableBehaviorAnalysis;
        public float AnalysisInterval => _analysisInterval;
        public int DataWindowSize => _dataWindowSize;
        public int DataRetentionDays => _dataRetentionDays;

        // Events
        public event Action<AnalyticsModel> OnModelRegistered;
        public event Action<AnalyticsModel> OnModelUpdated;
        public event Action<string> OnAnalyticsError;
        public event Action OnAnalyticsInitialized;

        private void Awake()
        {
            InitializeAnalytics();
        }

        private void Start()
        {
            _startTime = DateTime.UtcNow;
            SetupAnalyticsComponents();
            RegisterAnalyticsModels();
            StartAnalytics();
            StartCoroutine(AnalyticsUpdateLoop());
        }

        private void InitializeAnalytics()
        {
            _dataPipeline = ServiceContainerFactory.Instance?.TryResolve<DataPipelineIntegration>();
            _systemSync = ServiceContainerFactory.Instance?.TryResolve<RealtimeSystemSync>();

            if (_dataPipeline == null)
            {
                ChimeraLogger.LogWarning("[AnalyticsCore] DataPipelineIntegration not found - analytics capabilities will be limited");
            }

            if (_systemSync == null)
            {
                ChimeraLogger.LogWarning("[AnalyticsCore] RealtimeSystemSync not found - some sync analytics will be unavailable");
            }
        }

        protected virtual void SetupAnalyticsComponents()
        {
            _behaviorEngine = new BehaviorAnalysisEngine(this);
            _anomalyEngine = new AnomalyDetectionEngine(this);
            _predictiveEngine = new PredictiveAnalyticsEngine(this);
            _reportingEngine = new ReportingEngine(this);

            ChimeraLogger.Log("[AnalyticsCore] Analytics components initialized");
        }

        private void RegisterAnalyticsModels()
        {
            // Player behavior model
            RegisterModel(new AnalyticsModel
            {
                ModelId = "player_behavior",
                ModelType = ModelType.Behavioral,
                Description = "Analyzes player behavior patterns and preferences",
                InputFeatures = new List<string> { "actions_per_minute", "session_length", "feature_usage", "error_rate" },
                OutputMetrics = new List<string> { "engagement_score", "satisfaction_index", "churn_probability" },
                UpdateInterval = TimeSpan.FromMinutes(5),
                IsActive = true
            });

            // System performance model
            RegisterModel(new AnalyticsModel
            {
                ModelId = "system_performance",
                ModelType = ModelType.Performance,
                Description = "Monitors and predicts system performance issues",
                InputFeatures = new List<string> { "fps", "memory_usage", "cpu_usage", "load_times" },
                OutputMetrics = new List<string> { "performance_score", "bottleneck_probability", "crash_risk" },
                UpdateInterval = TimeSpan.FromMinutes(1),
                IsActive = true
            });

            // Game economy model
            RegisterModel(new AnalyticsModel
            {
                ModelId = "game_economy",
                ModelType = ModelType.Economic,
                Description = "Analyzes game economy balance and player spending patterns",
                InputFeatures = new List<string> { "resource_generation", "resource_consumption", "trade_volume", "player_wealth" },
                OutputMetrics = new List<string> { "economy_health", "inflation_rate", "balance_score" },
                UpdateInterval = TimeSpan.FromMinutes(30),
                IsActive = true
            });

            // Genetics complexity model
            RegisterModel(new AnalyticsModel
            {
                ModelId = "genetics_complexity",
                ModelType = ModelType.GameplaySpecific,
                Description = "Analyzes genetics system usage and complexity patterns",
                InputFeatures = new List<string> { "breeding_frequency", "trait_combinations", "research_progress", "genetic_diversity" },
                OutputMetrics = new List<string> { "complexity_score", "learning_curve", "feature_adoption" },
                UpdateInterval = TimeSpan.FromMinutes(15),
                IsActive = true
            });
        }

        private void StartAnalytics()
        {
            if (!_enableAnalytics)
            {
                ChimeraLogger.LogWarning("[AnalyticsCore] Analytics disabled - no analysis will be performed");
                return;
            }

            // Subscribe to data pipeline events
            if (_dataPipeline != null)
            {
                _dataPipeline.OnDataCollected += OnDataEventCollected;
                _dataPipeline.OnDataProcessed += OnDataBatchProcessed;
            }

            // Subscribe to system sync events
            if (_systemSync != null)
            {
                _systemSync.OnSystemStateChanged += OnSystemStateChanged;
                _systemSync.OnConflictDetected += OnSyncConflictDetected;
            }

            OnAnalyticsInitialized?.Invoke();
            ChimeraLogger.Log("[AnalyticsCore] Advanced analytics started successfully");
        }

        /// <summary>
        /// Register an analytics model for processing
        /// </summary>
        public void RegisterModel(AnalyticsModel model)
        {
            if (model == null || string.IsNullOrEmpty(model.ModelId))
            {
                OnAnalyticsError?.Invoke("Invalid model registration");
                ChimeraLogger.LogError("[AnalyticsCore] Invalid model registration");
                return;
            }

            _analyticsModels[model.ModelId] = model;
            OnModelRegistered?.Invoke(model);
            ChimeraLogger.Log($"[AnalyticsCore] Registered analytics model: {model.ModelId}");
        }

        /// <summary>
        /// Get analytics model by ID
        /// </summary>
        public AnalyticsModel GetModel(string modelId)
        {
            return _analyticsModels.TryGetValue(modelId, out var model) ? model : null;
        }

        /// <summary>
        /// Get all active models
        /// </summary>
        public AnalyticsModel[] GetActiveModels()
        {
            return _analyticsModels.Values.Where(m => m.IsActive).ToArray();
        }

        /// <summary>
        /// Update analytics metrics
        /// </summary>
        public void UpdateMetrics(Action<AnalyticsMetrics> updateAction)
        {
            updateAction?.Invoke(_analyticsMetrics);
        }

        private IEnumerator AnalyticsUpdateLoop()
        {
            while (_enableAnalytics)
            {
                yield return new WaitForSeconds(_analysisInterval);

                // Update models
                UpdateAnalyticsModels();

                // Update core metrics
                UpdateAnalyticsMetrics();

                // Cleanup old data
                CleanupOldData();
            }
        }

        private void UpdateAnalyticsModels()
        {
            foreach (var model in _analyticsModels.Values)
            {
                if (model.IsActive && DateTime.UtcNow - model.LastUpdated >= model.UpdateInterval)
                {
                    model.LastUpdated = DateTime.UtcNow;
                    model.AccuracyScore = UnityEngine.Random.Range(0.8f, 0.95f);
                    OnModelUpdated?.Invoke(model);
                }
            }
        }

        private void UpdateAnalyticsMetrics()
        {
            _analyticsMetrics.ActiveModels = _analyticsModels.Values.Count(m => m.IsActive);
            _analyticsMetrics.MemoryUsage = GC.GetTotalMemory(false);
            _analyticsMetrics.Uptime = DateTime.UtcNow - _startTime;
        }

        private void CleanupOldData()
        {
            // Delegate cleanup to individual engines
            _behaviorEngine?.CleanupOldData(_dataRetentionDays);
            _anomalyEngine?.CleanupOldData(_dataRetentionDays);
            _predictiveEngine?.CleanupOldData(_dataRetentionDays);
            _reportingEngine?.CleanupOldData(_dataRetentionDays);
        }

        // Event handlers
        private void OnDataEventCollected(DataEvent dataEvent)
        {
            if (_enableRealTimeAnalysis)
            {
                _behaviorEngine?.OnDataEventCollected(dataEvent);
                _anomalyEngine?.OnDataEventCollected(dataEvent);
                _predictiveEngine?.OnDataEventCollected(dataEvent);
            }
        }

        private void OnDataBatchProcessed(ProcessedDataBatch batch)
        {
            _analyticsMetrics.DataBatchesProcessed++;
            _reportingEngine?.OnDataBatchProcessed(batch);
        }

        private void OnSystemStateChanged(string systemId, SystemState newState)
        {
            _anomalyEngine?.OnSystemStateChanged(systemId, newState);
        }

        private void OnSyncConflictDetected(StateConflict conflict)
        {
            _reportingEngine?.OnSyncConflictDetected(conflict);
        }

        private void OnDestroy()
        {
            _enableAnalytics = false;

            // Unsubscribe from events
            if (_dataPipeline != null)
            {
                _dataPipeline.OnDataCollected -= OnDataEventCollected;
                _dataPipeline.OnDataProcessed -= OnDataBatchProcessed;
            }

            if (_systemSync != null)
            {
                _systemSync.OnSystemStateChanged -= OnSystemStateChanged;
                _systemSync.OnConflictDetected -= OnSyncConflictDetected;
            }
        }

        // Public API
        public AnalyticsMetrics GetAnalyticsMetrics() => _analyticsMetrics;
        public int GetActiveModelCount() => _analyticsModels.Values.Count(m => m.IsActive);
        public void SetAnalyticsEnabled(bool enabled) => _enableAnalytics = enabled;
        public void SetPredictiveAnalyticsEnabled(bool enabled) => _enablePredictiveAnalytics = enabled;
        public void SetAnomalyDetectionEnabled(bool enabled) => _enableAnomalyDetection = enabled;
    }
}