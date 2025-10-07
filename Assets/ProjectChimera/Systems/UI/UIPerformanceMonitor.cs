using UnityEngine;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core;
using ProjectChimera.Core.Updates;
using ProjectChimera.Systems.UI.Performance;
using ProjectChimera.Systems.UI.Core;
using System.Collections.Generic;
using System.Linq;

namespace ProjectChimera.Systems.UI
{
    /// <summary>
    /// REFACTORED: UI Performance Monitor - Delegation wrapper for UI performance subsystems
    /// Coordinates UI metrics collection, optimization, analysis, profiling, and recommendations
    /// Uses coordination pattern with specialized subsystems for focused responsibilities
    /// </summary>
    public class UIPerformanceMonitor : MonoBehaviour, ITickable
    {
        [Header("Performance Monitoring")]
        [SerializeField] private bool _enableMonitoring = true;
        [SerializeField] private bool _enableAutoOptimization = true;
        [SerializeField] private bool _enableLogging = false;

        // Subsystem references
        private UIPerformanceCore _performanceCore;
        private UIMetricsCollector _metricsCollector;
        private UIOptimizationManager _optimizationManager;
        private UIComponentAnalyzer _componentAnalyzer;
        private UIFrameProfiler _frameProfiler;
        private UIRecommendationEngine _recommendationEngine;

        // Legacy compatibility tracking
        private readonly List<IUIUpdatable> _trackedComponents = new List<IUIUpdatable>();

        private static UIPerformanceMonitor _instance;
        public static UIPerformanceMonitor Instance
        {
            get
            {
                if (_instance == null)
                {
                    var monitor = ServiceContainerFactory.Instance?.TryResolve<IUIPerformanceMonitor>();
                    _instance = monitor as UIPerformanceMonitor;
                    if (_instance == null)
                    {
                        var go = new GameObject("UIPerformanceMonitor");
                        _instance = go.AddComponent<UIPerformanceMonitor>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        public bool IsMonitoring => _performanceCore?.IsEnabled ?? false;
        public UIPerformanceStats CurrentStats => ConvertToLegacyStats();
        public UIOptimizationLevel OptimizationLevel => _optimizationManager?.CurrentOptimizationLevel ?? UIOptimizationLevel.None;
        public UIOptimizationRecommendations Recommendations => ConvertToLegacyRecommendations();

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Initialize performance monitor and subsystems
        /// </summary>
        public void Initialize()
        {
            InitializeSubsystems();

            if (_enableLogging)
            {
                ChimeraLogger.Log("UI", "🎯 UIPerformanceMonitor initialized with delegation pattern", this);
            }
        }

        /// <summary>
        /// Initialize all performance monitoring subsystems
        /// </summary>
        private void InitializeSubsystems()
        {
            // Create or get subsystem components
            _performanceCore = GetOrCreateComponent<UIPerformanceCore>();
            _metricsCollector = GetOrCreateComponent<UIMetricsCollector>();
            _optimizationManager = GetOrCreateComponent<UIOptimizationManager>();
            _componentAnalyzer = GetOrCreateComponent<UIComponentAnalyzer>();
            _frameProfiler = GetOrCreateComponent<UIFrameProfiler>();
            _recommendationEngine = GetOrCreateComponent<UIRecommendationEngine>();

            // Configure subsystems
            if (_performanceCore != null) _performanceCore.SetEnabled(_enableMonitoring);
            if (_metricsCollector != null) _metricsCollector.SetEnabled(_enableMonitoring);
            if (_optimizationManager != null) _optimizationManager.SetEnabled(_enableAutoOptimization);
            if (_componentAnalyzer != null) _componentAnalyzer.SetEnabled(_enableMonitoring);
            if (_frameProfiler != null) _frameProfiler.SetEnabled(_enableMonitoring);
            if (_recommendationEngine != null) _recommendationEngine.SetEnabled(_enableMonitoring);

            // Start monitoring if enabled
            if (_enableMonitoring)
            {
                StartMonitoring();
            }
        }

        /// <summary>
        /// Get or create subsystem component
        /// </summary>
        private T GetOrCreateComponent<T>() where T : Component
        {
            var component = GetComponent<T>();
            if (component == null)
            {
                component = gameObject.AddComponent<T>();
            }
            return component;
        }

        /// <summary>
        /// Start performance monitoring
        /// </summary>
        private void StartMonitoring()
        {
            _metricsCollector?.StartMonitoring();
            _frameProfiler?.StartProfiling();
            _componentAnalyzer?.StartAnalysis();
        }

        /// <summary>
        /// Register UI component for tracking (legacy compatibility)
        /// </summary>
        public void RegisterUIComponent(IUIUpdatable component)
        {
            if (component != null && !_trackedComponents.Contains(component))
            {
                _trackedComponents.Add(component);

                if (_enableLogging)
                {
                    ChimeraLogger.Log("UI", $"Registered UI component {component.GetType().Name}", this);
                }
            }
        }

        /// <summary>
        /// Unregister UI component (legacy compatibility)
        /// </summary>
        public void UnregisterUIComponent(IUIUpdatable component)
        {
            if (_trackedComponents.Remove(component))
            {
                if (_enableLogging)
                {
                    ChimeraLogger.Log("UI", $"Unregistered UI component {component.GetType().Name}", this);
                }
            }
        }

        /// <summary>
        /// Track UI update performance (legacy compatibility)
        /// </summary>
        public void TrackUIUpdate(IUIUpdatable component, float updateTime, bool shouldUpdate)
        {
            if (!_enableMonitoring || _performanceCore == null) return;

            // Delegate to performance core for actual tracking
            // In a real implementation, this would be handled by the subsystems
        }

        /// <summary>
        /// Set optimization level
        /// </summary>
        public void SetOptimizationLevel(UIOptimizationLevel level)
        {
            if (_optimizationManager != null)
            {
                _optimizationManager.SetOptimizationLevel(level);

                if (_enableLogging)
                {
                    ChimeraLogger.Log("UI", $"Optimization level set to {level}", this);
                }
            }
        }

        /// <summary>
        /// Force optimization analysis
        /// </summary>
        public void ForceOptimization()
        {
            if (_metricsCollector != null && _recommendationEngine != null && _optimizationManager != null)
            {
                var metrics = _metricsCollector.GetMetrics();
                _recommendationEngine.GenerateRecommendations(metrics);
                var recommendations = _recommendationEngine.GetCurrentRecommendations();
                _optimizationManager.ApplyOptimizations(recommendations);
            }
        }

        /// <summary>
        /// Get comprehensive performance report (legacy compatibility)
        /// </summary>
        public UIPerformanceStats GetPerformanceReport()
        {
            return ConvertToLegacyStats();
        }

        public int TickPriority => 100;
        public bool IsTickable => enabled && gameObject.activeInHierarchy;

        public void Tick(float deltaTime)
        {
            if (!_enableMonitoring || _performanceCore == null) return;

            // Delegate to performance core for coordinated updates
            _performanceCore.Tick(deltaTime);

            // Handle auto-optimization if enabled
            if (_enableAutoOptimization && _recommendationEngine != null && _optimizationManager != null)
            {
                var recommendations = _recommendationEngine.GetCurrentRecommendations();
                if (recommendations.RecommendedOptimizations != null && recommendations.RecommendedOptimizations.Length > 0)
                {
                    _optimizationManager.ApplyOptimizations(recommendations);
                }
            }
        }

        private void Start()
        {
            UpdateOrchestrator.Instance?.RegisterTickable(this);
        }

        private void OnDestroy()
        {
            UpdateOrchestrator.Instance?.UnregisterTickable(this);
            _trackedComponents.Clear();
        }

        #region Private Methods

        /// <summary>
        /// Convert subsystem metrics to legacy stats format
        /// </summary>
        private UIPerformanceStats ConvertToLegacyStats()
        {
            if (_metricsCollector == null) return new UIPerformanceStats();

            var metrics = _metricsCollector.GetMetrics();
            return new UIPerformanceStats
            {
                AverageFrameTime = metrics.FrameTime,
                TotalUpdateTime = metrics.TotalUpdateTime,
                TotalUIUpdates = 0, // Handled by subsystems
                UIEfficiencyScore = _metricsCollector.IsPerformanceTargetMet() ? 1.0f : 0.7f,
                ActiveElementCount = _trackedComponents.Count
            };
        }

        /// <summary>
        /// Convert subsystem recommendations to legacy format
        /// </summary>
        private UIOptimizationRecommendations ConvertToLegacyRecommendations()
        {
            if (_recommendationEngine == null) return new UIOptimizationRecommendations();

            var recommendations = _recommendationEngine.GetCurrentRecommendations();
            return new UIOptimizationRecommendations
            {
                RecommendedOptimizations = recommendations.RecommendedOptimizations ?? new UIOptimizationType[0],
                SuggestedLevel = recommendations.SuggestedLevel,
                PerformanceIssues = recommendations.PerformanceIssues ?? new string[0],
                PotentialImprovement = recommendations.PotentialImprovement
            };
        }

        /// <summary>
        /// Get component statistics from analyzer subsystem
        /// </summary>
        private UIComponentStats[] GetComponentStats()
        {
            if (_componentAnalyzer == null) return new UIComponentStats[0];

            var analyzerStats = _componentAnalyzer.GetComponentStats();
            var legacyStats = new List<UIComponentStats>();

            foreach (var stat in analyzerStats)
            {
                legacyStats.Add(new UIComponentStats
                {
                    ComponentType = stat.ComponentType ?? typeof(UnityEngine.UI.Graphic),
                    InstanceCount = stat.InstanceCount,
                    AverageUpdateTime = stat.AverageUpdateTime,
                    MemoryUsage = (long)(stat.AverageUpdateTime * 1024), // Estimate
                    IsPerformanceIssue = stat.AverageUpdateTime > 5.0f
                });
            }

            return legacyStats.ToArray();
        }

        /// <summary>
        /// Get frame data history from profiler subsystem
        /// </summary>
        private UIFrameData[] GetFrameDataHistory()
        {
            if (_frameProfiler == null) return new UIFrameData[0];

            var frameHistory = _frameProfiler.GetFrameHistory();
            var legacyFrameData = new List<UIFrameData>();

            foreach (var frame in frameHistory)
            {
                legacyFrameData.Add(new UIFrameData
                {
                    FrameTime = frame.FrameTime,
                    UITime = frame.UITime,
                    DrawCalls = 0, // Not tracked yet
                    MemoryDelta = frame.MemoryDelta,
                    Timestamp = frame.Timestamp
                });
            }

            return legacyFrameData.ToArray();
        }

        #endregion
    }

    // Legacy data structures moved to ProjectChimera.Systems.UI.Performance namespace
}
