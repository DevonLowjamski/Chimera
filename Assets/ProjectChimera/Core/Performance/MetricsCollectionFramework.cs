using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using ProjectChimera.Shared;
using ProjectChimera.Core.Logging;
// Note: Avoid direct using of Updates to prevent namespace resolution issues in some build setups

namespace ProjectChimera.Core.Performance
{
    /// <summary>
    /// REFACTORED: Metrics Collection Framework - Coordinator using SRP-compliant components
    /// Single Responsibility: Coordinating collection, processing, and export of metrics
    /// Uses composition with MetricsCollector, MetricsProcessor, and MetricsExporter
    /// Reduced from 602 lines to maintain SRP compliance
    /// </summary>
    public class MetricsCollectionFramework : MonoBehaviour, ProjectChimera.Core.Updates.ITickable
    {
        [Header("Framework Settings")]
        [SerializeField] private bool _enableCollection = true;
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private float _exportInterval = 300f; // 5 minutes
        [SerializeField] private string _exportPath = "ProjectChimera/Metrics";
        [SerializeField] private bool _autoExportMetrics = false;

        // Composition: Delegate responsibilities to focused components
        private MetricsCollector _collector;
        private MetricsProcessor _processor;
        private MetricsExporter _exporter;

        // Framework state
        private float _lastExportTime;
        private static MetricsCollectionFramework _instance;
        private bool _isInitialized = false;

        public static MetricsCollectionFramework Instance => _instance;

        // Events
        public event System.Action<MetricSnapshot> OnMetricCollected;
        public event System.Action<string, ExportResult> OnMetricsExported;

        #region Unity Lifecycle

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

        private void OnDestroy()
        {
            if (_instance == this)
            {
                Cleanup();
                _instance = null;
            }
        }

        #endregion

        #region Initialization

        private void Initialize()
        {
            if (_isInitialized) return;

            try
            {
                // Initialize components using composition
                InitializeComponents();

                // Register with UpdateOrchestrator
                ProjectChimera.Core.Updates.UpdateOrchestrator.Instance?.RegisterTickable(this);

                _lastExportTime = Time.time;
                _isInitialized = true;

                if (_enableLogging)
                {
                    ChimeraLogger.LogInfo("METRICS", "Metrics collection framework initialized with composition pattern", this);
                }
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError("METRICS", $"Failed to initialize MetricsCollectionFramework: {ex.Message}", this);
            }
        }

        private void InitializeComponents()
        {
            // Create and initialize MetricsCollector
            var collectorGO = new GameObject("MetricsCollector");
            collectorGO.transform.SetParent(transform);
            _collector = collectorGO.AddComponent<MetricsCollector>();

            // Create MetricsProcessor and MetricsExporter
            _processor = new MetricsProcessor(300, _enableLogging); // 5 minutes history
            _exporter = new MetricsExporter(_exportPath, _enableLogging);

            // Wire up events between components
            _collector.OnMetricCollected += OnMetricCollectedInternal;
            _processor.OnAggregatesUpdated += OnAggregatesUpdatedInternal;
            _exporter.OnExportCompleted += OnExportCompletedInternal;
        }

        #endregion

        #region ITickable Implementation

        public int TickPriority => -20; // Runs after core systems
        public bool IsTickable => _enableCollection && _isInitialized && enabled;

        public void Tick(float deltaTime)
        {
            if (!_enableCollection || !_isInitialized) return;

            // Auto-export metrics if enabled
            if (_autoExportMetrics && Time.time - _lastExportTime >= _exportInterval)
            {
                ExportAllMetrics();
                _lastExportTime = Time.time;
            }
        }

        public void OnRegistered()
        {
            if (_enableLogging)
                ChimeraLogger.LogInfo("METRICS", "MetricsCollectionFramework registered with UpdateOrchestrator", this);
        }

        public void OnUnregistered()
        {
            if (_enableLogging)
                ChimeraLogger.LogInfo("METRICS", "MetricsCollectionFramework unregistered from UpdateOrchestrator", this);
        }

        #endregion

        #region Event Handlers

        private void OnMetricCollectedInternal(MetricSnapshot snapshot)
        {
            // Process the metric through our processor
            _processor?.ProcessMetric(snapshot);

            // Forward the event
            OnMetricCollected?.Invoke(snapshot);
        }

        private void OnAggregatesUpdatedInternal(string systemName, MetricAggregates aggregates)
        {
            if (_enableLogging)
            {
                ChimeraLogger.LogInfo("METRICS", $"Aggregates updated for {systemName}", this);
            }
        }

        private void OnExportCompletedInternal(string systemName, ExportResult result)
        {
            OnMetricsExported?.Invoke(systemName, result);

            if (_enableLogging)
            {
                ChimeraLogger.LogInfo("METRICS", $"Export completed for {systemName}: {result.Message}", this);
            }
        }

        #endregion

        #region Public API - Delegates to Components

        /// <summary>
        /// Register a metric collector for a specific system
        /// </summary>
        public void RegisterCollector(string systemName, IMetricCollector collector)
        {
            _collector?.RegisterCollector(systemName, collector);
        }

        /// <summary>
        /// Unregister a metric collector
        /// </summary>
        public void UnregisterCollector(string systemName)
        {
            _collector?.UnregisterCollector(systemName);
        }

        /// <summary>
        /// Get metric history for a specific system
        /// </summary>
        public MetricSnapshot[] GetMetricHistory(string systemName, int sampleCount = 60)
        {
            return _processor?.GetHistory(systemName, sampleCount) ?? new MetricSnapshot[0];
        }

        /// <summary>
        /// Get latest metric snapshot for a system
        /// </summary>
        public MetricSnapshot GetLatestMetrics(string systemName)
        {
            var history = _processor?.GetHistory(systemName, 1);
            return (history?.Length > 0) ? history[0] : null;
        }

        /// <summary>
        /// Get aggregated metrics across all systems
        /// </summary>
        public Dictionary<string, MetricAggregates> GetAggregatedMetrics()
        {
            return _processor?.GetAllAggregates() ?? new Dictionary<string, MetricAggregates>();
        }

        /// <summary>
        /// Export all metrics to files
        /// </summary>
        public void ExportAllMetrics()
        {
            if (_exporter == null) return;

            var aggregates = _processor?.GetAllAggregates();
            if (aggregates != null && aggregates.Count > 0)
            {
                _exporter.ExportAggregates(aggregates, ExportFormat.CSV);
                _exporter.ExportPerformanceReport(aggregates);
            }
        }

        /// <summary>
        /// Clear all metric history
        /// </summary>
        [ContextMenu("Clear Metric History")]
        public void ClearAllMetrics()
        {
            _processor?.ClearAllHistory();

            if (_enableLogging)
            {
                ChimeraLogger.LogInfo("METRICS", "All metric history cleared", this);
            }
        }

        /// <summary>
        /// Get comprehensive metrics report
        /// </summary>
        public string GenerateReport()
        {
            var aggregates = _processor?.GetAllAggregates();
            if (aggregates == null || aggregates.Count == 0)
            {
                return "No metrics data available";
            }

            return _exporter?.GeneratePerformanceReport(aggregates) ?? "Export component not available";
        }

        /// <summary>
        /// Get collection status
        /// </summary>
        public CollectionStatus GetCollectionStatus()
        {
            return _collector?.GetStatus() ?? new CollectionStatus
            {
                IsCollecting = false,
                CollectionInterval = 0f,
                RegisteredCollectors = 0,
                LastCollectionTime = 0f,
                TimeSinceLastCollection = 0f
            };
        }

        #endregion

        #region Cleanup

        private void Cleanup()
        {
            try
            {
                // Unregister from UpdateOrchestrator
                ProjectChimera.Core.Updates.UpdateOrchestrator.Instance?.UnregisterTickable(this);

                // Cleanup components
                if (_collector != null)
                {
                    _collector.OnMetricCollected -= OnMetricCollectedInternal;
                }

                if (_processor != null)
                {
                    _processor.OnAggregatesUpdated -= OnAggregatesUpdatedInternal;
                }

                if (_exporter != null)
                {
                    _exporter.OnExportCompleted -= OnExportCompletedInternal;
                }

                if (_enableLogging)
                {
                    ChimeraLogger.LogInfo("METRICS", "MetricsCollectionFramework cleanup completed", this);
                }
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError("METRICS", $"Error during cleanup: {ex.Message}", this);
            }
        }

        #endregion
    }

    #region Core Interfaces

    /// <summary>
    /// Interface for metric collectors - moved from heavy implementations to MetricsCollector component
    /// </summary>
    public interface IMetricCollector
    {
        MetricSnapshot CollectMetrics();
    }

    /// <summary>
    /// Single metric snapshot from a system
    /// </summary>
    [System.Serializable]
    public class MetricSnapshot
    {
        public string SystemName;
        public float Timestamp;
        public int FrameCount;
        public Dictionary<string, float> Metrics = new Dictionary<string, float>();

        // Legacy support
        public float UpdateTime;
        public int UpdateCount;
        public Dictionary<string, object> CustomMetrics;
    }

    #endregion
}
