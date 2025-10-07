using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core;
using ProjectChimera.Core.Updates;

namespace ProjectChimera.Systems.UI.Performance
{
    /// <summary>
    /// REFACTORED: UI Performance Core - Central coordination for UI performance monitoring subsystems
    /// Manages performance tracking, optimization coordination, metrics collection, and component analysis
    /// Follows Single Responsibility Principle with focused subsystem coordination
    /// </summary>
    public class UIPerformanceCore : MonoBehaviour, ITickable
    {
        [Header("Core Performance Settings")]
        [SerializeField] private bool _enablePerformanceMonitoring = true;
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private float _performanceUpdateInterval = 1f;
        [SerializeField] private bool _enableAutoOptimization = true;

        // Core subsystems
        private UIMetricsCollector _metricsCollector;
        private UIOptimizationManager _optimizationManager;
        private UIComponentAnalyzer _componentAnalyzer;
        private UIFrameProfiler _frameProfiler;
        private UIRecommendationEngine _recommendationEngine;

        // Timing
        private float _lastUpdateTime;

        // Properties
        public bool IsEnabled { get; private set; } = true;
        public bool IsMonitoring => _enablePerformanceMonitoring && IsEnabled;

        // ITickable implementation
        public int TickPriority => 80;
        public bool IsTickable => enabled && gameObject.activeInHierarchy && IsEnabled;

        public void Tick(float deltaTime)
        {
            if (!IsEnabled || !_enablePerformanceMonitoring) return;

            if (Time.time - _lastUpdateTime >= _performanceUpdateInterval)
            {
                ProcessPerformanceUpdate();
                _lastUpdateTime = Time.time;
            }
        }

        // Statistics aggregation
        public UIPerformanceAggregateStats GetCombinedStats()
        {
            var stats = new UIPerformanceAggregateStats();

            if (_metricsCollector != null)
            {
                var metrics = _metricsCollector.GetMetrics();
                stats.FrameTime = metrics.FrameTime;
                stats.UIUpdateTime = metrics.UIUpdateTime;
                stats.MemoryUsage = metrics.MemoryUsage;
                stats.ActiveComponents = metrics.ActiveComponents;
            }

            if (_frameProfiler != null)
            {
                var profilerStats = _frameProfiler.GetStats();
                stats.AverageFrameTime = profilerStats.AverageFrameTime;
                stats.FramesProcessed = profilerStats.FramesProcessed;
            }

            if (_optimizationManager != null)
            {
                var optStats = _optimizationManager.GetStats();
                stats.OptimizationsApplied = optStats.OptimizationsApplied;
                stats.CurrentOptimizationLevel = optStats.CurrentOptimizationLevel;
            }

            if (_componentAnalyzer != null)
            {
                var compStats = _componentAnalyzer.GetStats();
                stats.ComponentsAnalyzed = compStats.ComponentsAnalyzed;
                stats.PerformanceIssues = compStats.PerformanceIssues;
            }

            return stats;
        }

        // Events
        public System.Action<UIPerformanceAggregateStats> OnPerformanceUpdated;
        public System.Action<UIOptimizationRecommendations> OnOptimizationRecommended;
        public System.Action<float> OnFrameTimeChanged;
        public System.Action<UIOptimizationLevel> OnOptimizationLevelChanged;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (_enableLogging)
                ChimeraLogger.Log("UI", "📊 Initializing UIPerformanceCore subsystems...", this);

            // Initialize subsystems in dependency order
            InitializeMetricsCollector();
            InitializeFrameProfiler();
            InitializeComponentAnalyzer();
            InitializeOptimizationManager();
            InitializeRecommendationEngine();

            // Register with UpdateOrchestrator
            var updateOrchestrator = ServiceContainerFactory.Instance?.TryResolve<UpdateOrchestrator>();
            updateOrchestrator?.RegisterTickable(this);

            if (_enableLogging)
                ChimeraLogger.Log("UI", "✅ UIPerformanceCore initialized with all subsystems", this);
        }

        private void InitializeMetricsCollector()
        {
            var metricsGO = new GameObject("UIMetricsCollector");
            metricsGO.transform.SetParent(transform);
            _metricsCollector = metricsGO.AddComponent<UIMetricsCollector>();

            _metricsCollector.OnFrameTimeChanged += (frameTime) => OnFrameTimeChanged?.Invoke(frameTime);
        }

        private void InitializeFrameProfiler()
        {
            var profilerGO = new GameObject("UIFrameProfiler");
            profilerGO.transform.SetParent(transform);
            _frameProfiler = profilerGO.AddComponent<UIFrameProfiler>();
        }

        private void InitializeComponentAnalyzer()
        {
            var analyzerGO = new GameObject("UIComponentAnalyzer");
            analyzerGO.transform.SetParent(transform);
            _componentAnalyzer = analyzerGO.AddComponent<UIComponentAnalyzer>();
        }

        private void InitializeOptimizationManager()
        {
            var optimizationGO = new GameObject("UIOptimizationManager");
            optimizationGO.transform.SetParent(transform);
            _optimizationManager = optimizationGO.AddComponent<UIOptimizationManager>();

            _optimizationManager.OnOptimizationLevelChanged += (level) => OnOptimizationLevelChanged?.Invoke(level);
        }

        private void InitializeRecommendationEngine()
        {
            var recommendationGO = new GameObject("UIRecommendationEngine");
            recommendationGO.transform.SetParent(transform);
            _recommendationEngine = recommendationGO.AddComponent<UIRecommendationEngine>();

            _recommendationEngine.OnRecommendationsGenerated += (recs) => OnOptimizationRecommended?.Invoke(recs);
        }

        /// <summary>
        /// Coordinate all performance subsystem updates
        /// </summary>
        private void ProcessPerformanceUpdate()
        {
            // Collect current metrics
            if (_metricsCollector != null)
            {
                _metricsCollector.CollectMetrics();
            }

            // Update frame profiling
            if (_frameProfiler != null)
            {
                _frameProfiler.ProcessFrameData();
            }

            // Analyze UI components
            if (_componentAnalyzer != null)
            {
                _componentAnalyzer.AnalyzeComponents();
            }

            // Generate recommendations
            if (_recommendationEngine != null && _metricsCollector != null)
            {
                var currentMetrics = _metricsCollector.GetMetrics();
                _recommendationEngine.GenerateRecommendations(currentMetrics);
            }

            // Apply automatic optimizations if enabled
            if (_enableAutoOptimization && _optimizationManager != null && _recommendationEngine != null)
            {
                var recommendations = _recommendationEngine.GetCurrentRecommendations();
                _optimizationManager.ApplyOptimizations(recommendations);
            }

            // Fire combined stats event
            OnPerformanceUpdated?.Invoke(GetCombinedStats());
        }

        /// <summary>
        /// Start performance monitoring
        /// </summary>
        public void StartMonitoring()
        {
            _metricsCollector?.StartMonitoring();
            _frameProfiler?.StartProfiling();
            _componentAnalyzer?.StartAnalysis();

            if (_enableLogging)
                ChimeraLogger.Log("UI", "Started UI performance monitoring", this);
        }

        /// <summary>
        /// Stop performance monitoring
        /// </summary>
        public void StopMonitoring()
        {
            _metricsCollector?.StopMonitoring();
            _frameProfiler?.StopProfiling();
            _componentAnalyzer?.StopAnalysis();

            if (_enableLogging)
                ChimeraLogger.Log("UI", "Stopped UI performance monitoring", this);
        }

        /// <summary>
        /// Get current UI metrics - delegates to MetricsCollector
        /// </summary>
        public UIMetrics GetCurrentMetrics()
        {
            return _metricsCollector?.GetMetrics() ?? new UIMetrics();
        }

        /// <summary>
        /// Get frame profiling data - delegates to FrameProfiler
        /// </summary>
        public UIFrameData[] GetFrameHistory()
        {
            return _frameProfiler?.GetFrameHistory() ?? new UIFrameData[0];
        }

        /// <summary>
        /// Get component analysis results - delegates to ComponentAnalyzer
        /// </summary>
        public UIComponentStats[] GetComponentStats()
        {
            return _componentAnalyzer?.GetComponentStats() ?? new UIComponentStats[0];
        }

        /// <summary>
        /// Get optimization recommendations - delegates to RecommendationEngine
        /// </summary>
        public UIOptimizationRecommendations GetOptimizationRecommendations()
        {
            return _recommendationEngine?.GetCurrentRecommendations() ?? new UIOptimizationRecommendations();
        }

        /// <summary>
        /// Apply specific optimization - delegates to OptimizationManager
        /// </summary>
        public bool ApplyOptimization(UIOptimizationType optimization)
        {
            return _optimizationManager?.ApplyOptimization(optimization) ?? false;
        }

        /// <summary>
        /// Set optimization level - delegates to OptimizationManager
        /// </summary>
        public void SetOptimizationLevel(UIOptimizationLevel level)
        {
            _optimizationManager?.SetOptimizationLevel(level);
        }

        /// <summary>
        /// Reset performance data
        /// </summary>
        public void ResetPerformanceData()
        {
            _metricsCollector?.ResetMetrics();
            _frameProfiler?.ResetProfileData();
            _componentAnalyzer?.ResetAnalysis();

            if (_enableLogging)
                ChimeraLogger.Log("UI", "Reset UI performance data", this);
        }

        /// <summary>
        /// Set system enabled/disabled
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;

            if (_metricsCollector != null) _metricsCollector.SetEnabled(enabled);
            if (_frameProfiler != null) _frameProfiler.SetEnabled(enabled);
            if (_componentAnalyzer != null) _componentAnalyzer.SetEnabled(enabled);
            if (_optimizationManager != null) _optimizationManager.SetEnabled(enabled);
            if (_recommendationEngine != null) _recommendationEngine.SetEnabled(enabled);

            if (!enabled)
            {
                StopMonitoring();
            }
            else if (_enablePerformanceMonitoring)
            {
                StartMonitoring();
            }

            if (_enableLogging)
                ChimeraLogger.Log("UI", $"UIPerformanceCore: {(enabled ? "enabled" : "disabled")}", this);
        }

        private void OnDestroy()
        {
            // Unregister from UpdateOrchestrator
            var updateOrchestrator = ServiceContainerFactory.Instance?.TryResolve<UpdateOrchestrator>();
            updateOrchestrator?.UnregisterTickable(this);
        }
    }

    #region Data Structures

    /// <summary>
    /// Aggregated UI performance statistics for UIPerformanceCore
    /// (renamed to avoid conflict with monitor-level stats)
    /// </summary>
    [System.Serializable]
    public struct UIPerformanceAggregateStats
    {
        public float FrameTime;
        public float UIUpdateTime;
        public long MemoryUsage;
        public int ActiveComponents;
        public float AverageFrameTime;
        public int FramesProcessed;
        public int OptimizationsApplied;
        public UIOptimizationLevel CurrentOptimizationLevel;
        public int ComponentsAnalyzed;
        public int PerformanceIssues;
    }



    #endregion
}
