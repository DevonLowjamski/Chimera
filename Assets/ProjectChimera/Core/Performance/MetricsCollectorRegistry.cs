using UnityEngine;
using System.Collections.Generic;

namespace ProjectChimera.Core.Performance
{
    /// <summary>
    /// DEPRECATED: Use MetricsCollectorRegistryService (Core.Performance) + MetricsCollectorRegistryBridge (Systems.Performance) instead
    /// This wrapper maintained for backward compatibility during migration
    /// Architecture violation: MonoBehaviour in Core layer
    /// </summary>
    [System.Obsolete("Use MetricsCollectorRegistryService (Core.Performance) + MetricsCollectorRegistryBridge (Systems.Performance) instead")]
    public class MetricsCollectorRegistry : MonoBehaviour
    {
        [Header("Registry Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private int _maxCollectors = 50;
        [SerializeField] private bool _enableCollectorHealthMonitoring = true;
        [SerializeField] private float _healthCheckInterval = 30f;

        [Header("System Monitoring Toggles")]
        [SerializeField] private bool _monitorCultivationSystem = true;
        [SerializeField] private bool _monitorConstructionSystem = true;
        [SerializeField] private bool _monitorUISystem = true;
        [SerializeField] private bool _monitorSaveSystem = true;
        [SerializeField] private bool _monitorEconomySystem = true;

        private MetricsCollectorRegistryService _service;

        // Events
        public event System.Action<string, IMetricCollector> OnCollectorRegistered
        {
            add { if (_service != null) _service.OnCollectorRegistered += value; }
            remove { if (_service != null) _service.OnCollectorRegistered -= value; }
        }

        public event System.Action<string> OnCollectorUnregistered
        {
            add { if (_service != null) _service.OnCollectorUnregistered += value; }
            remove { if (_service != null) _service.OnCollectorUnregistered -= value; }
        }

        public event System.Action<string, CollectorHealth> OnCollectorHealthUpdated
        {
            add { if (_service != null) _service.OnCollectorHealthUpdated += value; }
            remove { if (_service != null) _service.OnCollectorHealthUpdated -= value; }
        }

        public bool IsInitialized => _service?.IsInitialized ?? false;
        public CollectorRegistryStats Stats => _service?.Stats ?? new CollectorRegistryStats();
        public int RegisteredCollectorCount => _service?.RegisteredCollectorCount ?? 0;

        private void Awake()
        {
            InitializeService();
        }

        private void InitializeService()
        {
            _service = new MetricsCollectorRegistryService(
                _enableLogging,
                _maxCollectors,
                _enableCollectorHealthMonitoring,
                _healthCheckInterval,
                _monitorCultivationSystem,
                _monitorConstructionSystem,
                _monitorUISystem,
                _monitorSaveSystem,
                _monitorEconomySystem
            );
        }

        #region Public API (delegates to service)

        public void Initialize()
            => _service?.Initialize(Time.time);

        public bool RegisterCollector(string systemName, IMetricCollector collector)
            => _service?.RegisterCollector(systemName, collector, Time.time) ?? false;

        public bool UnregisterCollector(string systemName)
            => _service?.UnregisterCollector(systemName) ?? false;

        public Dictionary<string, MetricSnapshot> CollectAllMetrics()
            => _service?.CollectAllMetrics(Time.time, Time.frameCount, () => Time.realtimeSinceStartup) ?? new Dictionary<string, MetricSnapshot>();

        public MetricSnapshot CollectMetrics(string systemName)
            => _service?.CollectMetrics(systemName, Time.time, Time.frameCount, () => Time.realtimeSinceStartup);

        public void ProcessHealthMonitoring()
            => _service?.ProcessHealthMonitoring(Time.time);

        public CollectorInfo GetCollectorInfo(string systemName)
            => _service?.GetCollectorInfo(systemName) ?? new CollectorInfo();

        public CollectorHealth GetCollectorHealth(string systemName)
            => _service?.GetCollectorHealth(systemName) ?? new CollectorHealth();

        public List<string> GetRegisteredSystems()
            => _service?.GetRegisteredSystems() ?? new List<string>();

        public Dictionary<string, CollectorHealth> GetHealthReport()
            => _service?.GetHealthReport() ?? new Dictionary<string, CollectorHealth>();

        public void SetSystemMonitoring(string systemType, bool enabled)
            => _service?.SetSystemMonitoring(systemType, enabled);

        #endregion

        private void OnDestroy()
        {
            _service?.Cleanup();
        }
    }

    #region Data Structures

    /// <summary>
    /// Collector information structure
    /// </summary>
    [System.Serializable]
    public struct CollectorInfo
    {
        public string SystemName;
        public string CollectorType;
        public float RegistrationTime;
        public bool IsActive;
    }

    /// <summary>
    /// Collector health structure
    /// </summary>
    [System.Serializable]
    public struct CollectorHealth
    {
        public string SystemName;
        public bool IsHealthy;
        public float LastSuccessfulCollection;
        public float LastHealthCheck;
        public int TotalCollections;
        public int FailedCollections;
        public float AverageCollectionTime;
    }

    /// <summary>
    /// Registry statistics
    /// </summary>
    [System.Serializable]
    public struct CollectorRegistryStats
    {
        public int TotalCollectorsRegistered;
        public int SuccessfulCollections;
        public int FailedCollections;
        public int CollectionErrors;
        public int UnhealthyCollectors;
        public float LastCollectionTime;
    }

    #endregion
}
