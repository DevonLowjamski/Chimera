using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core;
using ProjectChimera.Core.SimpleDI;
using System.Linq;

namespace ProjectChimera.Systems.UI.Performance
{
    /// <summary>
    /// REFACTORED: UI Component Analyzer - Focused UI component analysis and performance profiling
    /// Handles component counting, performance issue detection, and optimization recommendations
    /// Single Responsibility: UI component analysis and performance profiling
    /// </summary>
    public class UIComponentAnalyzer : MonoBehaviour
    {
        [Header("Analysis Settings")]
        [SerializeField] private bool _enableAnalysis = true;
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private float _analysisInterval = 2f;
        [SerializeField] private bool _enableDeepAnalysis = true;

        [Header("Performance Thresholds")]
        [SerializeField] private int _componentCountWarningThreshold = 500;
        [SerializeField] private int _componentCountCriticalThreshold = 1000;
        [SerializeField] private float _updateTimeWarningThreshold = 1f; // 1ms
        [SerializeField] private float _updateTimeCriticalThreshold = 5f; // 5ms

        [Header("Analysis Scope")]
        [SerializeField] private bool _analyzeInactiveComponents = false;
        [SerializeField] private bool _analyzeHiddenCanvases = false;
        [SerializeField] private int _maxComponentsToAnalyze = 2000;

        // Component tracking
        private readonly Dictionary<System.Type, UIComponentStats> _componentStats = new Dictionary<System.Type, UIComponentStats>();
        private readonly List<UIPerformanceIssue> _performanceIssues = new List<UIPerformanceIssue>();
        private readonly HashSet<Component> _analyzedComponents = new HashSet<Component>();

        // Canvas tracking
        private readonly Dictionary<Canvas, CanvasAnalysisData> _canvasAnalysis = new Dictionary<Canvas, CanvasAnalysisData>();

        // Timing
        private float _lastAnalysisTime;

        // Statistics
        private UIComponentAnalysisStats _stats = new UIComponentAnalysisStats();

        // Properties
        public bool IsEnabled { get; private set; } = true;
        public bool IsAnalyzing { get; private set; } = false;
        public UIComponentAnalysisStats GetStats() => _stats;

        // Events
        public System.Action<UIPerformanceIssue> OnPerformanceIssueDetected;
        public System.Action<System.Type, UIComponentStats> OnComponentStatsUpdated;
        public System.Action<Canvas, CanvasAnalysisData> OnCanvasAnalyzed;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            _stats = new UIComponentAnalysisStats();
            _lastAnalysisTime = Time.time;

            if (_enableLogging)
                ChimeraLogger.Log("UI", "🔍 UIComponentAnalyzer initialized", this);
        }

        /// <summary>
        /// Start component analysis
        /// </summary>
        public void StartAnalysis()
        {
            if (!IsEnabled || IsAnalyzing) return;

            IsAnalyzing = true;

            if (_enableLogging)
                ChimeraLogger.Log("UI", "Started UI component analysis", this);
        }

        /// <summary>
        /// Stop component analysis
        /// </summary>
        public void StopAnalysis()
        {
            if (!IsAnalyzing) return;

            IsAnalyzing = false;

            if (_enableLogging)
                ChimeraLogger.Log("UI", "Stopped UI component analysis", this);
        }

        /// <summary>
        /// Analyze UI components
        /// </summary>
        public void AnalyzeComponents()
        {
            if (!IsEnabled || !IsAnalyzing || !_enableAnalysis) return;

            if (Time.time - _lastAnalysisTime < _analysisInterval) return;

            // Clear previous analysis data
            _componentStats.Clear();
            _performanceIssues.Clear();
            _analyzedComponents.Clear();
            _canvasAnalysis.Clear();

            // Analyze all canvases
            // Use GameObjectRegistry for canvas tracking
            var registry = ServiceContainerFactory.Instance?.TryResolve<ProjectChimera.Core.Performance.IGameObjectRegistry>();
            var canvases = registry?.GetAll<Canvas>() ?? new Canvas[0];

            foreach (var canvas in canvases)
            {
                if (ShouldAnalyzeCanvas(canvas))
                {
                    AnalyzeCanvas(canvas);
                }
            }

            // Generate performance issue reports
            GeneratePerformanceIssueReports();

            _lastAnalysisTime = Time.time;
            _stats.AnalysisRuns++;

            if (_enableLogging)
                ChimeraLogger.Log("UI", $"Completed UI analysis. Found {_performanceIssues.Count} issues across {_componentStats.Count} component types", this);
        }

        /// <summary>
        /// Get component statistics
        /// </summary>
        public UIComponentStats[] GetComponentStats()
        {
            return _componentStats.Values.ToArray();
        }

        /// <summary>
        /// Get performance issues
        /// </summary>
        public UIPerformanceIssue[] GetPerformanceIssues()
        {
            return _performanceIssues.ToArray();
        }

        /// <summary>
        /// Get canvas analysis data
        /// </summary>
        public CanvasAnalysisData[] GetCanvasAnalysis()
        {
            return _canvasAnalysis.Values.ToArray();
        }

        /// <summary>
        /// Get component stats for specific type
        /// </summary>
        public UIComponentStats GetComponentStats(System.Type componentType)
        {
            _componentStats.TryGetValue(componentType, out var stats);
            return stats;
        }

        /// <summary>
        /// Get most problematic components
        /// </summary>
        public UIComponentStats[] GetMostProblematicComponents(int count = 10)
        {
            return _componentStats.Values
                .Where(s => s.IsPerformanceIssue)
                .OrderByDescending(s => s.AverageUpdateTime)
                .Take(count)
                .ToArray();
        }

        /// <summary>
        /// Reset analysis data
        /// </summary>
        public void ResetAnalysis()
        {
            _componentStats.Clear();
            _performanceIssues.Clear();
            _analyzedComponents.Clear();
            _canvasAnalysis.Clear();
            _stats = new UIComponentAnalysisStats();

            if (_enableLogging)
                ChimeraLogger.Log("UI", "Reset UI component analysis data", this);
        }

        /// <summary>
        /// Set system enabled/disabled
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;

            if (!enabled)
            {
                StopAnalysis();
                ResetAnalysis();
            }

            if (_enableLogging)
                ChimeraLogger.Log("UI", $"UIComponentAnalyzer: {(enabled ? "enabled" : "disabled")}", this);
        }

        #region Private Methods

        /// <summary>
        /// Check if canvas should be analyzed
        /// </summary>
        private bool ShouldAnalyzeCanvas(Canvas canvas)
        {
            if (!_analyzeInactiveComponents && (!canvas.enabled || !canvas.gameObject.activeInHierarchy))
                return false;

            if (!_analyzeHiddenCanvases && canvas.worldCamera == null && !canvas.isRootCanvas)
                return false;

            return true;
        }

        /// <summary>
        /// Analyze specific canvas
        /// </summary>
        private void AnalyzeCanvas(Canvas canvas)
        {
            var analysisData = new CanvasAnalysisData
            {
                Canvas = canvas,
                ComponentCount = 0,
                DrawCalls = EstimateDrawCalls(canvas),
                IsOptimized = true,
                Issues = new List<string>()
            };

            // Get all UI components in this canvas
            var components = canvas.GetComponentsInChildren<Component>();
            var uiComponents = components.Where(c => IsUIComponent(c)).ToArray();

            analysisData.ComponentCount = uiComponents.Length;

            // Analyze each component
            foreach (var component in uiComponents)
            {
                if (_analyzedComponents.Count >= _maxComponentsToAnalyze)
                    break;

                AnalyzeComponent(component, analysisData);
            }

            // Check for canvas-specific issues
            CheckCanvasIssues(canvas, analysisData);

            _canvasAnalysis[canvas] = analysisData;
            OnCanvasAnalyzed?.Invoke(canvas, analysisData);
        }

        /// <summary>
        /// Analyze individual component
        /// </summary>
        private void AnalyzeComponent(Component component, CanvasAnalysisData canvasData)
        {
            if (_analyzedComponents.Contains(component))
                return;

            _analyzedComponents.Add(component);

            var componentType = component.GetType();

            if (!_componentStats.TryGetValue(componentType, out var stats))
            {
                stats = new UIComponentStats
                {
                    ComponentType = componentType,
                    InstanceCount = 0,
                    AverageUpdateTime = 0f,
                    MemoryUsage = 0,
                    IsPerformanceIssue = false
                };
            }

            stats.InstanceCount++;

            // Estimate component performance impact
            float estimatedUpdateTime = EstimateComponentUpdateTime(component);
            long estimatedMemory = EstimateComponentMemoryUsage(component);

            stats.AverageUpdateTime = (stats.AverageUpdateTime + estimatedUpdateTime) / 2f;
            stats.MemoryUsage += estimatedMemory;

            // Check if component is a performance issue
            if (estimatedUpdateTime > _updateTimeWarningThreshold ||
                stats.InstanceCount > _componentCountWarningThreshold)
            {
                stats.IsPerformanceIssue = true;
                canvasData.IsOptimized = false;

                // Create performance issue
                var issue = new UIPerformanceIssue
                {
                    IssueType = UIPerformanceIssueType.SlowComponent,
                    ComponentType = componentType,
                    Severity = estimatedUpdateTime > _updateTimeCriticalThreshold ?
                              UIPerformanceIssueSeverity.Critical : UIPerformanceIssueSeverity.Warning,
                    Description = $"Component {componentType.Name} has high update time: {estimatedUpdateTime:F2}ms",
                    Component = component,
                    Canvas = canvasData.Canvas
                };

                _performanceIssues.Add(issue);
                OnPerformanceIssueDetected?.Invoke(issue);
            }

            _componentStats[componentType] = stats;
            OnComponentStatsUpdated?.Invoke(componentType, stats);
        }

        /// <summary>
        /// Check for canvas-specific issues
        /// </summary>
        private void CheckCanvasIssues(Canvas canvas, CanvasAnalysisData analysisData)
        {
            // Check for excessive draw calls
            if (analysisData.DrawCalls > 50)
            {
                analysisData.Issues.Add($"High draw call count: {analysisData.DrawCalls}");
                analysisData.IsOptimized = false;

                var issue = new UIPerformanceIssue
                {
                    IssueType = UIPerformanceIssueType.ExcessiveDrawCalls,
                    Severity = analysisData.DrawCalls > 100 ? UIPerformanceIssueSeverity.Critical : UIPerformanceIssueSeverity.Warning,
                    Description = $"Canvas has {analysisData.DrawCalls} draw calls",
                    Canvas = canvas
                };
                _performanceIssues.Add(issue);
            }

            // Check for excessive component count
            if (analysisData.ComponentCount > _componentCountWarningThreshold)
            {
                analysisData.Issues.Add($"High component count: {analysisData.ComponentCount}");
                analysisData.IsOptimized = false;

                var issue = new UIPerformanceIssue
                {
                    IssueType = UIPerformanceIssueType.ExcessiveComponents,
                    Severity = analysisData.ComponentCount > _componentCountCriticalThreshold ?
                              UIPerformanceIssueSeverity.Critical : UIPerformanceIssueSeverity.Warning,
                    Description = $"Canvas has {analysisData.ComponentCount} components",
                    Canvas = canvas
                };
                _performanceIssues.Add(issue);
            }

            // Check for nested canvases
            var childCanvases = canvas.GetComponentsInChildren<Canvas>().Where(c => c != canvas).ToArray();
            if (childCanvases.Length > 3)
            {
                analysisData.Issues.Add($"Too many nested canvases: {childCanvases.Length}");
                analysisData.IsOptimized = false;
            }
        }

        /// <summary>
        /// Generate performance issue reports
        /// </summary>
        private void GeneratePerformanceIssueReports()
        {
            _stats.ComponentsAnalyzed = _analyzedComponents.Count;
            _stats.PerformanceIssues = _performanceIssues.Count;
            _stats.CriticalIssues = _performanceIssues.Count(i => i.Severity == UIPerformanceIssueSeverity.Critical);
            _stats.WarningIssues = _performanceIssues.Count(i => i.Severity == UIPerformanceIssueSeverity.Warning);
        }

        /// <summary>
        /// Check if component is UI-related
        /// </summary>
        private bool IsUIComponent(Component component)
        {
            return component is Graphic ||
                   component is LayoutGroup ||
                   component is ContentSizeFitter ||
                   component is CanvasGroup ||
                   component is Selectable ||
                   component is Canvas ||
                   component is GraphicRaycaster;
        }

        /// <summary>
        /// Estimate component update time
        /// </summary>
        private float EstimateComponentUpdateTime(Component component)
        {
            // Simplified estimation based on component type
            return component switch
            {
                Image _ => 0.1f,
                Text _ => 0.3f,
                RawImage _ => 0.2f,
                Button _ => 0.15f,
                ScrollRect _ => 0.5f,
                GridLayoutGroup _ => 0.8f,
                VerticalLayoutGroup _ => 0.6f,
                HorizontalLayoutGroup _ => 0.6f,
                ContentSizeFitter _ => 0.4f,
                _ => 0.05f
            };
        }

        /// <summary>
        /// Estimate component memory usage
        /// </summary>
        private long EstimateComponentMemoryUsage(Component component)
        {
            // Simplified memory estimation
            return component switch
            {
                Image _ => 512,
                Text _ => 256,
                RawImage _ => 1024,
                Button _ => 384,
                ScrollRect _ => 768,
                Canvas _ => 2048,
                _ => 128
            };
        }

        /// <summary>
        /// Estimate draw calls for canvas
        /// </summary>
        private int EstimateDrawCalls(Canvas canvas)
        {
            var graphics = canvas.GetComponentsInChildren<Graphic>();
            var materials = new HashSet<Material>();

            foreach (var graphic in graphics)
            {
                if (graphic.material != null)
                {
                    materials.Add(graphic.material);
                }
            }

            return materials.Count;
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Canvas analysis data
    /// </summary>
    [System.Serializable]
    public struct CanvasAnalysisData
    {
        public Canvas Canvas;
        public int ComponentCount;
        public int DrawCalls;
        public bool IsOptimized;
        public List<string> Issues;
    }

    /// <summary>
    /// UI performance issue
    /// </summary>
    [System.Serializable]
    public struct UIPerformanceIssue
    {
        public UIPerformanceIssueType IssueType;
        public UIPerformanceIssueSeverity Severity;
        public string Description;
        public System.Type ComponentType;
        public Component Component;
        public Canvas Canvas;
    }

    /// <summary>
    /// UI performance issue types
    /// </summary>
    public enum UIPerformanceIssueType
    {
        SlowComponent,
        ExcessiveComponents,
        ExcessiveDrawCalls,
        MemoryLeak,
        UnoptimizedCanvas,
        RedundantUpdates
    }

    /// <summary>
    /// UI performance issue severity
    /// </summary>
    public enum UIPerformanceIssueSeverity
    {
        Info,
        Warning,
        Critical
    }

    /// <summary>
    /// UI component analysis statistics
    /// </summary>
    [System.Serializable]
    public struct UIComponentAnalysisStats
    {
        public int AnalysisRuns;
        public int ComponentsAnalyzed;
        public int PerformanceIssues;
        public int CriticalIssues;
        public int WarningIssues;
        public int CanvasesAnalyzed;
    }

    #endregion
}