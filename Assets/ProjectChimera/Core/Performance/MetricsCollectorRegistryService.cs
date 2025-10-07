using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Shared;

namespace ProjectChimera.Core.Performance
{
    /// <summary>
    /// REFACTORED: Metrics Collector Registry Service (POCO - Unity-independent core)
    /// Single Responsibility: Metric collector registration, management, and coordination
    /// Extracted from MetricsCollectorRegistry for clean architecture compliance
    /// </summary>
    public class MetricsCollectorRegistryService
    {
        private readonly bool _enableLogging;
        private readonly int _maxCollectors;
        private readonly bool _enableCollectorHealthMonitoring;
        private readonly float _healthCheckInterval;

        // System monitoring toggles
        private bool _monitorCultivationSystem;
        private bool _monitorConstructionSystem;
        private bool _monitorUISystem;
        private bool _monitorSaveSystem;
        private bool _monitorEconomySystem;

        // Collector registry
        private readonly Dictionary<string, IMetricCollector> _collectors = new Dictionary<string, IMetricCollector>();
        private readonly Dictionary<string, CollectorInfo> _collectorInfo = new Dictionary<string, CollectorInfo>();
        private readonly Dictionary<string, CollectorHealth> _collectorHealth = new Dictionary<string, CollectorHealth>();

        // State tracking
        private float _lastHealthCheck;
        private bool _isInitialized = false;

        // Statistics
        private CollectorRegistryStats _stats = new CollectorRegistryStats();

        // Events
        public event System.Action<string, IMetricCollector> OnCollectorRegistered;
        public event System.Action<string> OnCollectorUnregistered;
        public event System.Action<string, CollectorHealth> OnCollectorHealthUpdated;

        public bool IsInitialized => _isInitialized;
        public CollectorRegistryStats Stats => _stats;
        public int RegisteredCollectorCount => _collectors.Count;

        public MetricsCollectorRegistryService(
            bool enableLogging = false,
            int maxCollectors = 50,
            bool enableCollectorHealthMonitoring = true,
            float healthCheckInterval = 30f,
            bool monitorCultivationSystem = true,
            bool monitorConstructionSystem = true,
            bool monitorUISystem = true,
            bool monitorSaveSystem = true,
            bool monitorEconomySystem = true)
        {
            _enableLogging = enableLogging;
            _maxCollectors = maxCollectors;
            _enableCollectorHealthMonitoring = enableCollectorHealthMonitoring;
            _healthCheckInterval = healthCheckInterval;
            _monitorCultivationSystem = monitorCultivationSystem;
            _monitorConstructionSystem = monitorConstructionSystem;
            _monitorUISystem = monitorUISystem;
            _monitorSaveSystem = monitorSaveSystem;
            _monitorEconomySystem = monitorEconomySystem;
        }

        public void Initialize(float currentTime)
        {
            if (_isInitialized) return;

            _collectors.Clear();
            _collectorInfo.Clear();
            _collectorHealth.Clear();
            RegisterDefaultCollectors(currentTime);
            ResetStats(currentTime);

            _isInitialized = true;

            if (_enableLogging)
            {
                SharedLogger.Log("METRICS", "Metrics Collector Registry initialized", null);
            }
        }

        /// <summary>
        /// Register default metric collectors
        /// </summary>
        private void RegisterDefaultCollectors(float currentTime)
        {
            // Core system collectors
            RegisterCollector("SystemPerformance", new SystemPerformanceCollector(), currentTime);
            RegisterCollector("MemoryUsage", new MemoryUsageCollector(), currentTime);

            // Conditional system collectors
            if (_monitorCultivationSystem)
                RegisterCollector("CultivationSystem", new CultivationSystemCollector(), currentTime);

            if (_monitorConstructionSystem)
                RegisterCollector("ConstructionSystem", new ConstructionSystemCollector(), currentTime);

            if (_monitorUISystem)
                RegisterCollector("UISystem", new UISystemCollector(), currentTime);

            if (_monitorSaveSystem)
                RegisterCollector("SaveSystem", new SaveSystemCollector(), currentTime);

            if (_monitorEconomySystem)
                RegisterCollector("EconomySystem", new EconomySystemCollector(), currentTime);
        }

        /// <summary>
        /// Register a metric collector
        /// </summary>
        public bool RegisterCollector(string systemName, IMetricCollector collector, float currentTime)
        {
            if (string.IsNullOrEmpty(systemName) || collector == null)
            {
                if (_enableLogging)
                {
                    SharedLogger.LogWarning("METRICS", "Cannot register invalid collector", null);
                }
                return false;
            }

            if (_collectors.ContainsKey(systemName))
            {
                if (_enableLogging)
                {
                    SharedLogger.LogWarning("METRICS", $"Collector {systemName} already registered", null);
                }
                return false;
            }

            if (_collectors.Count >= _maxCollectors)
            {
                if (_enableLogging)
                {
                    SharedLogger.LogWarning("METRICS", $"Maximum collectors ({_maxCollectors}) reached", null);
                }
                return false;
            }

            _collectors[systemName] = collector;
            _collectorInfo[systemName] = new CollectorInfo
            {
                SystemName = systemName,
                CollectorType = collector.GetType().Name,
                RegistrationTime = currentTime,
                IsActive = true
            };

            _collectorHealth[systemName] = new CollectorHealth
            {
                SystemName = systemName,
                IsHealthy = true,
                LastSuccessfulCollection = currentTime,
                TotalCollections = 0,
                FailedCollections = 0,
                AverageCollectionTime = 0f
            };

            _stats.TotalCollectorsRegistered++;
            OnCollectorRegistered?.Invoke(systemName, collector);

            if (_enableLogging)
            {
                SharedLogger.Log("METRICS", $"Registered collector for {systemName} ({_collectors.Count} total)", null);
            }

            return true;
        }

        /// <summary>
        /// Unregister a metric collector
        /// </summary>
        public bool UnregisterCollector(string systemName)
        {
            if (!_collectors.ContainsKey(systemName))
            {
                if (_enableLogging)
                {
                    SharedLogger.LogWarning("METRICS", $"Collector {systemName} not found for unregistration", null);
                }
                return false;
            }

            _collectors.Remove(systemName);
            _collectorInfo.Remove(systemName);
            _collectorHealth.Remove(systemName);

            OnCollectorUnregistered?.Invoke(systemName);

            if (_enableLogging)
            {
                SharedLogger.Log("METRICS", $"Unregistered collector for {systemName} ({_collectors.Count} remaining)", null);
            }

            return true;
        }

        /// <summary>
        /// Collect metrics from all registered collectors
        /// </summary>
        public Dictionary<string, MetricSnapshot> CollectAllMetrics(float currentTime, int frameCount, System.Func<float> getRealtimeSinceStartup)
        {
            var results = new Dictionary<string, MetricSnapshot>();

            foreach (var kvp in _collectors)
            {
                var systemName = kvp.Key;
                var collector = kvp.Value;

                try
                {
                    var collectionStartTime = getRealtimeSinceStartup();
                    var snapshot = collector.CollectMetrics();
                    var collectionTime = getRealtimeSinceStartup() - collectionStartTime;

                    if (snapshot != null)
                    {
                        snapshot.SystemName = systemName;
                        snapshot.Timestamp = currentTime;
                        snapshot.FrameCount = frameCount;

                        results[systemName] = snapshot;
                        UpdateCollectorHealth(systemName, true, collectionTime, currentTime);

                        _stats.SuccessfulCollections++;
                    }
                    else
                    {
                        UpdateCollectorHealth(systemName, false, collectionTime, currentTime);
                        _stats.FailedCollections++;

                        if (_enableLogging)
                        {
                            SharedLogger.LogWarning("METRICS", $"Collector {systemName} returned null snapshot", null);
                        }
                    }
                }
                catch (System.Exception ex)
                {
                    UpdateCollectorHealth(systemName, false, 0f, currentTime);
                    _stats.FailedCollections++;
                    _stats.CollectionErrors++;

                    if (_enableLogging)
                    {
                        SharedLogger.LogError("METRICS", $"Error collecting metrics for {systemName}: {ex.Message}", null);
                    }
                }
            }

            _stats.LastCollectionTime = currentTime;
            return results;
        }

        /// <summary>
        /// Collect metrics from specific collector
        /// </summary>
        public MetricSnapshot CollectMetrics(string systemName, float currentTime, int frameCount, System.Func<float> getRealtimeSinceStartup)
        {
            if (!_collectors.TryGetValue(systemName, out var collector))
            {
                if (_enableLogging)
                {
                    SharedLogger.LogWarning("METRICS", $"Collector {systemName} not found", null);
                }
                return null;
            }

            try
            {
                var collectionStartTime = getRealtimeSinceStartup();
                var snapshot = collector.CollectMetrics();
                var collectionTime = getRealtimeSinceStartup() - collectionStartTime;

                if (snapshot != null)
                {
                    snapshot.SystemName = systemName;
                    snapshot.Timestamp = currentTime;
                    snapshot.FrameCount = frameCount;

                    UpdateCollectorHealth(systemName, true, collectionTime, currentTime);
                    _stats.SuccessfulCollections++;
                }
                else
                {
                    UpdateCollectorHealth(systemName, false, collectionTime, currentTime);
                    _stats.FailedCollections++;
                }

                return snapshot;
            }
            catch (System.Exception ex)
            {
                UpdateCollectorHealth(systemName, false, 0f, currentTime);
                _stats.FailedCollections++;
                _stats.CollectionErrors++;

                if (_enableLogging)
                {
                    SharedLogger.LogError("METRICS", $"Error collecting metrics for {systemName}: {ex.Message}", null);
                }

                return null;
            }
        }

        /// <summary>
        /// Process collector health monitoring
        /// </summary>
        public void ProcessHealthMonitoring(float currentTime)
        {
            if (!_isInitialized || !_enableCollectorHealthMonitoring) return;

            if (currentTime - _lastHealthCheck >= _healthCheckInterval)
            {
                UpdateAllCollectorHealth(currentTime);
                _lastHealthCheck = currentTime;
            }
        }

        /// <summary>
        /// Update collector health information
        /// </summary>
        private void UpdateCollectorHealth(string systemName, bool success, float collectionTime, float currentTime)
        {
            if (!_collectorHealth.TryGetValue(systemName, out var health))
                return;

            health.TotalCollections++;

            if (success)
            {
                health.LastSuccessfulCollection = currentTime;
                health.AverageCollectionTime = (health.AverageCollectionTime * 0.9f) + (collectionTime * 0.1f);
            }
            else
            {
                health.FailedCollections++;
            }

            // Update health status
            var failureRate = (float)health.FailedCollections / health.TotalCollections;
            var timeSinceLastSuccess = currentTime - health.LastSuccessfulCollection;

            health.IsHealthy = failureRate < 0.1f && timeSinceLastSuccess < _healthCheckInterval * 2;
            health.LastHealthCheck = currentTime;

            _collectorHealth[systemName] = health;
            OnCollectorHealthUpdated?.Invoke(systemName, health);
        }

        /// <summary>
        /// Update health for all collectors
        /// </summary>
        private void UpdateAllCollectorHealth(float currentTime)
        {
            var unhealthyCollectors = 0;

            foreach (var kvp in _collectorHealth)
            {
                var systemName = kvp.Key;
                var health = kvp.Value;

                var timeSinceLastSuccess = currentTime - health.LastSuccessfulCollection;
                var wasHealthy = health.IsHealthy;

                // Check if collector has been inactive too long
                health.IsHealthy = timeSinceLastSuccess < _healthCheckInterval * 2;
                health.LastHealthCheck = currentTime;

                if (!health.IsHealthy)
                {
                    unhealthyCollectors++;

                    if (wasHealthy && _enableLogging)
                    {
                        SharedLogger.LogWarning("METRICS", $"Collector {systemName} marked as unhealthy", null);
                    }
                }

                _collectorHealth[systemName] = health;
            }

            _stats.UnhealthyCollectors = unhealthyCollectors;
        }

        /// <summary>
        /// Get collector information
        /// </summary>
        public CollectorInfo GetCollectorInfo(string systemName)
        {
            if (_collectorInfo.TryGetValue(systemName, out var info))
            {
                return info;
            }
            return new CollectorInfo();
        }

        /// <summary>
        /// Get collector health
        /// </summary>
        public CollectorHealth GetCollectorHealth(string systemName)
        {
            if (_collectorHealth.TryGetValue(systemName, out var health))
            {
                return health;
            }
            return new CollectorHealth();
        }

        /// <summary>
        /// Get all registered system names
        /// </summary>
        public List<string> GetRegisteredSystems()
        {
            return new List<string>(_collectors.Keys);
        }

        /// <summary>
        /// Get health report for all collectors
        /// </summary>
        public Dictionary<string, CollectorHealth> GetHealthReport()
        {
            return new Dictionary<string, CollectorHealth>(_collectorHealth);
        }

        /// <summary>
        /// Enable or disable system monitoring
        /// </summary>
        public void SetSystemMonitoring(string systemType, bool enabled)
        {
            switch (systemType.ToLower())
            {
                case "cultivation":
                    _monitorCultivationSystem = enabled;
                    break;
                case "construction":
                    _monitorConstructionSystem = enabled;
                    break;
                case "ui":
                    _monitorUISystem = enabled;
                    break;
                case "save":
                    _monitorSaveSystem = enabled;
                    break;
                case "economy":
                    _monitorEconomySystem = enabled;
                    break;
            }

            if (_enableLogging)
            {
                SharedLogger.Log("METRICS", $"{systemType} system monitoring {(enabled ? "enabled" : "disabled")}", null);
            }
        }

        /// <summary>
        /// Reset statistics
        /// </summary>
        private void ResetStats(float currentTime)
        {
            _stats = new CollectorRegistryStats
            {
                TotalCollectorsRegistered = 0,
                SuccessfulCollections = 0,
                FailedCollections = 0,
                CollectionErrors = 0,
                UnhealthyCollectors = 0,
                LastCollectionTime = currentTime
            };
        }

        public void Cleanup()
        {
            _collectors.Clear();
            _collectorInfo.Clear();
            _collectorHealth.Clear();
        }
    }
}
