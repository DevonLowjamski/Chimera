using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;

namespace ProjectChimera.Core.Performance
{
    /// <summary>
    /// REFACTORED: Metrics Collector - Focused metric collection and data gathering
    /// Single Responsibility: Collecting metrics from various game systems
    /// Extracted from MetricsCollectionFramework for better SRP compliance
    /// </summary>
    public class MetricsCollector : MonoBehaviour, ITickable
    {
        [Header("Collection Settings")]
        [SerializeField] private bool _enableCollection = true;
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private float _collectionInterval = 1.0f;

        [Header("System Monitoring")]
        [SerializeField] private bool _monitorCultivationSystem = true;
        [SerializeField] private bool _monitorConstructionSystem = true;
        [SerializeField] private bool _monitorUISystem = true;
        [SerializeField] private bool _monitorSaveSystem = true;
        [SerializeField] private bool _monitorEconomySystem = true;

        // Metric collectors registry
        private readonly Dictionary<string, IMetricCollector> _collectors = new Dictionary<string, IMetricCollector>();

        // Collection state
        private float _lastCollectionTime;
        private bool _isInitialized = false;

        // Events
        public event System.Action<MetricSnapshot> OnMetricCollected;

        // ITickable implementation
        public int TickPriority => Updates.TickPriority.AnalyticsManager;
        public bool IsTickable => _enableCollection && _isInitialized && isActiveAndEnabled;

        #region Unity Lifecycle

        private void Start()
        {
            Initialize();
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

        #region Initialization

        private void Initialize()
        {
            if (_isInitialized) return;

            // Initialize metric collectors for each system
            InitializeSystemCollectors();

            _lastCollectionTime = Time.time;
            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.LogInfo("METRICS", "MetricsCollector initialized", this);
            }
        }

        private void InitializeSystemCollectors()
        {
            // Register collectors for enabled systems
            if (_monitorCultivationSystem)
                RegisterCollector("Cultivation", new CultivationMetricCollector());

            if (_monitorConstructionSystem)
                RegisterCollector("Construction", new ConstructionMetricCollector());

            if (_monitorUISystem)
                RegisterCollector("UI", new UIMetricCollector());

            if (_monitorSaveSystem)
                RegisterCollector("Save", new SaveSystemMetricCollector());

            if (_monitorEconomySystem)
                RegisterCollector("Economy", new EconomyMetricCollector());

            if (_enableLogging)
            {
                ChimeraLogger.LogInfo("METRICS", $"Initialized {_collectors.Count} metric collectors", this);
            }
        }

        #endregion

        #region ITickable Implementation

        public void Tick(float deltaTime)
        {
            if (Time.time - _lastCollectionTime >= _collectionInterval)
            {
                CollectAllMetrics();
                _lastCollectionTime = Time.time;
            }
        }

        public void OnRegistered()
        {
            if (_enableLogging)
                ChimeraLogger.LogInfo("METRICS", "MetricsCollector registered with UpdateOrchestrator", this);
        }

        public void OnUnregistered()
        {
            if (_enableLogging)
                ChimeraLogger.LogInfo("METRICS", "MetricsCollector unregistered from UpdateOrchestrator", this);
        }

        #endregion

        #region Collection Operations

        /// <summary>
        /// Collect metrics from all registered collectors
        /// </summary>
        private void CollectAllMetrics()
        {
            foreach (var collector in _collectors.Values)
            {
                try
                {
                    var snapshot = collector.CollectMetrics();
                    if (snapshot != null)
                    {
                        OnMetricCollected?.Invoke(snapshot);

                        if (_enableLogging)
                        {
                            ChimeraLogger.LogInfo("METRICS", $"Collected metrics: {snapshot.SystemName}", this);
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    ChimeraLogger.LogError("METRICS", $"Error collecting metrics from {collector.GetType().Name}: {ex.Message}", this);
                }
            }
        }

        /// <summary>
        /// Collect metrics from a specific system
        /// </summary>
        public MetricSnapshot CollectSystemMetrics(string systemName)
        {
            if (_collectors.TryGetValue(systemName, out var collector))
            {
                try
                {
                    var snapshot = collector.CollectMetrics();
                    OnMetricCollected?.Invoke(snapshot);
                    return snapshot;
                }
                catch (System.Exception ex)
                {
                    ChimeraLogger.LogError("METRICS", $"Error collecting metrics from {systemName}: {ex.Message}", this);
                }
            }

            return null;
        }

        #endregion

        #region Collector Management

        /// <summary>
        /// Register a metric collector for a system
        /// </summary>
        public void RegisterCollector(string systemName, IMetricCollector collector)
        {
            if (collector == null)
            {
                ChimeraLogger.LogWarning("METRICS", $"Cannot register null collector for {systemName}", this);
                return;
            }

            _collectors[systemName] = collector;

            if (_enableLogging)
            {
                ChimeraLogger.LogInfo("METRICS", $"Registered collector for {systemName}", this);
            }
        }

        /// <summary>
        /// Unregister a metric collector
        /// </summary>
        public void UnregisterCollector(string systemName)
        {
            if (_collectors.Remove(systemName))
            {
                if (_enableLogging)
                {
                    ChimeraLogger.LogInfo("METRICS", $"Unregistered collector for {systemName}", this);
                }
            }
        }

        /// <summary>
        /// Get all registered collector names
        /// </summary>
        public string[] GetRegisteredCollectors()
        {
            return _collectors.Keys.ToArray();
        }

        /// <summary>
        /// Check if a collector is registered
        /// </summary>
        public bool IsCollectorRegistered(string systemName)
        {
            return _collectors.ContainsKey(systemName);
        }

        #endregion

        #region Configuration

        /// <summary>
        /// Enable/disable collection
        /// </summary>
        public void SetCollectionEnabled(bool enabled)
        {
            _enableCollection = enabled;

            if (_enableLogging)
            {
                ChimeraLogger.LogInfo("METRICS", $"Collection {(enabled ? "enabled" : "disabled")}", this);
            }
        }

        /// <summary>
        /// Set collection interval
        /// </summary>
        public void SetCollectionInterval(float interval)
        {
            _collectionInterval = Mathf.Max(0.1f, interval);

            if (_enableLogging)
            {
                ChimeraLogger.LogInfo("METRICS", $"Collection interval set to {_collectionInterval}s", this);
            }
        }

        /// <summary>
        /// Get current collection status
        /// </summary>
        public CollectionStatus GetStatus()
        {
            return new CollectionStatus
            {
                IsCollecting = _enableCollection && _isInitialized,
                CollectionInterval = _collectionInterval,
                RegisteredCollectors = _collectors.Count,
                LastCollectionTime = _lastCollectionTime,
                TimeSinceLastCollection = Time.time - _lastCollectionTime
            };
        }

        #endregion
    }

    /// <summary>
    /// Collection status information
    /// </summary>
    [System.Serializable]
    public struct CollectionStatus
    {
        public bool IsCollecting;
        public float CollectionInterval;
        public int RegisteredCollectors;
        public float LastCollectionTime;
        public float TimeSinceLastCollection;
    }
}