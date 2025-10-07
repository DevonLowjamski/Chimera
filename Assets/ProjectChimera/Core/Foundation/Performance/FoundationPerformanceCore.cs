using UnityEngine;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;

namespace ProjectChimera.Core.Foundation.Performance
{
    /// <summary>
    /// REFACTORED: Foundation Performance Core - Central coordination for foundation performance subsystems
    /// Coordinates performance metrics, analysis, optimization, reporting, and history tracking
    /// Single Responsibility: Central performance system coordination
    /// </summary>
    public class FoundationPerformanceCore : MonoBehaviour, ITickable
    {
        [Header("Performance Core Settings")]
        [SerializeField] private bool _enablePerformanceCoordination = true;
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private float _updateInterval = 2f;

        // Subsystem references
        private FoundationPerformanceMetrics _performanceMetrics;
        private FoundationPerformanceAnalyzer _performanceAnalyzer;
        private FoundationPerformanceOptimizer _performanceOptimizer;
        private FoundationPerformanceReporter _performanceReporter;
        private FoundationPerformanceHistory _performanceHistory;

        // Timing
        private float _lastUpdate;

        // Properties
        public bool IsEnabled { get; private set; } = true;
        public float OverallPerformanceScore => _performanceMetrics?.GetOverallPerformanceScore() ?? 1.0f;

        // Events
        public System.Action<float> OnOverallPerformanceChanged;
        public System.Action<string, float> OnSystemPerformanceChanged;
        public System.Action<string> OnOptimizationRecommended;

        public int TickPriority => 60;
        public bool IsTickable => enabled && gameObject.activeInHierarchy;

        private void Start()
        {
            Initialize();
            UpdateOrchestrator.Instance?.RegisterTickable(this);
        }

        private void Initialize()
        {
            InitializeSubsystems();

            if (_enableLogging)
                ChimeraLogger.Log("FOUNDATION", "⚡ FoundationPerformanceCore initialized", this);
        }

        /// <summary>
        /// Initialize all performance subsystems
        /// </summary>
        private void InitializeSubsystems()
        {
            // Get or create subsystem components
            _performanceMetrics = GetOrCreateComponent<FoundationPerformanceMetrics>();
            _performanceAnalyzer = GetOrCreateComponent<FoundationPerformanceAnalyzer>();
            _performanceOptimizer = GetOrCreateComponent<FoundationPerformanceOptimizer>();
            _performanceReporter = GetOrCreateComponent<FoundationPerformanceReporter>();
            _performanceHistory = GetOrCreateComponent<FoundationPerformanceHistory>();

            // Configure subsystems
            _performanceMetrics?.SetEnabled(_enablePerformanceCoordination);
            _performanceAnalyzer?.SetEnabled(_enablePerformanceCoordination);
            _performanceOptimizer?.SetEnabled(_enablePerformanceCoordination);
            _performanceReporter?.SetEnabled(_enablePerformanceCoordination);
            _performanceHistory?.SetEnabled(_enablePerformanceCoordination);

            // Connect event handlers
            ConnectEventHandlers();
        }

        /// <summary>
        /// Connect inter-subsystem event handlers
        /// </summary>
        private void ConnectEventHandlers()
        {
            if (_performanceMetrics != null)
            {
                _performanceMetrics.OnSystemPerformanceUpdated += HandleSystemPerformanceUpdated;
                _performanceMetrics.OnOverallPerformanceChanged += HandleOverallPerformanceChanged;
            }

            if (_performanceAnalyzer != null)
            {
                _performanceAnalyzer.OnPerformanceAnalyzed += HandlePerformanceAnalyzed;
                _performanceAnalyzer.OnOptimizationRecommended += HandleOptimizationRecommended;
            }

            if (_performanceOptimizer != null)
            {
                _performanceOptimizer.OnOptimizationTriggered += HandleOptimizationTriggered;
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

        public void Tick(float deltaTime)
        {
            if (!IsEnabled || !_enablePerformanceCoordination) return;

            if (Time.time - _lastUpdate < _updateInterval) return;

            // Coordinate subsystem updates
            ProcessPerformanceCoordination();

            _lastUpdate = Time.time;
        }

        /// <summary>
        /// Coordinate performance operations across subsystems
        /// </summary>
        private void ProcessPerformanceCoordination()
        {
            // Update performance metrics
            _performanceMetrics?.UpdateSystemPerformanceMetrics();

            // Analyze performance data
            _performanceAnalyzer?.AnalyzePerformanceData();

            // Generate optimizations if needed
            _performanceOptimizer?.ProcessOptimizations();

            // Update performance history
            _performanceHistory?.RecordPerformanceSnapshot();

            // Update performance reports
            _performanceReporter?.UpdateReports();
        }

        /// <summary>
        /// Get system performance data
        /// </summary>
        public SystemPerformanceData GetSystemPerformance(string systemName)
        {
            return _performanceMetrics?.GetSystemPerformance(systemName) ?? new SystemPerformanceData();
        }

        /// <summary>
        /// Get all system performance data
        /// </summary>
        public SystemPerformanceData[] GetAllSystemPerformance()
        {
            return _performanceMetrics?.GetAllSystemPerformance() ?? new SystemPerformanceData[0];
        }

        /// <summary>
        /// Get performance history
        /// </summary>
        public PerformanceSnapshot[] GetPerformanceHistory()
        {
            return _performanceHistory?.GetPerformanceHistory() ?? new PerformanceSnapshot[0];
        }

        /// <summary>
        /// Generate performance report
        /// </summary>
        public PerformanceReport GeneratePerformanceReport()
        {
            return _performanceReporter?.GeneratePerformanceReport() ?? new PerformanceReport();
        }

        /// <summary>
        /// Get performance category
        /// </summary>
        public PerformanceCategory GetPerformanceCategory()
        {
            return _performanceAnalyzer?.GetPerformanceCategory() ?? PerformanceCategory.Acceptable;
        }

        /// <summary>
        /// Get poor performing systems
        /// </summary>
        public string[] GetPoorPerformingSystems()
        {
            return _performanceAnalyzer?.GetPoorPerformingSystems() ?? new string[0];
        }

        /// <summary>
        /// Set system enabled/disabled
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;

            // Update all subsystems
            _performanceMetrics?.SetEnabled(enabled);
            _performanceAnalyzer?.SetEnabled(enabled);
            _performanceOptimizer?.SetEnabled(enabled);
            _performanceReporter?.SetEnabled(enabled);
            _performanceHistory?.SetEnabled(enabled);

            if (_enableLogging)
                ChimeraLogger.Log("FOUNDATION", $"FoundationPerformanceCore: {(enabled ? "enabled" : "disabled")}", this);
        }

        #region Event Handlers

        private void HandleSystemPerformanceUpdated(string systemName, float performanceScore)
        {
            // Notify analyzer about performance update
            _performanceAnalyzer?.NotifySystemPerformanceUpdated(systemName, performanceScore);

            OnSystemPerformanceChanged?.Invoke(systemName, performanceScore);
        }

        private void HandleOverallPerformanceChanged(float overallScore)
        {
            // Notify analyzer about overall performance change
            _performanceAnalyzer?.NotifyOverallPerformanceChanged(overallScore);

            OnOverallPerformanceChanged?.Invoke(overallScore);
        }

        private void HandlePerformanceAnalyzed(string systemName, PerformanceAnalysisResult result)
        {
            // Pass analysis results to optimizer
            _performanceOptimizer?.ProcessAnalysisResult(systemName, result);
        }

        private void HandleOptimizationRecommended(string systemName)
        {
            OnOptimizationRecommended?.Invoke(systemName);
        }

        private void HandleOptimizationTriggered(string systemName)
        {
            if (_enableLogging)
                ChimeraLogger.Log("FOUNDATION", $"Performance optimization triggered for: {systemName}", this);
        }

        #endregion

        private void OnDestroy()
        {
            UpdateOrchestrator.Instance?.UnregisterTickable(this);
        }
    }
}
