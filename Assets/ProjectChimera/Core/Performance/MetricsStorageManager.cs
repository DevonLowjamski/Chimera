using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Shared;

namespace ProjectChimera.Core.Performance
{
    /// <summary>
    /// REFACTORED: Metrics Storage Manager
    /// Single Responsibility: Metric data persistence, history management, and storage optimization
    /// Extracted from MetricsCollectionFramework for better separation of concerns
    /// </summary>
    public class MetricsStorageManager : MonoBehaviour
    {
        [Header("Storage Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private int _maxHistorySize = 300; // 5 minutes at 1s intervals
        [SerializeField] private int _maxSystemsTracked = 20;
        [SerializeField] private bool _enableCompressionOptimization = true;

        // Metric storage
        private readonly Dictionary<string, Queue<MetricSnapshot>> _metricHistory = new Dictionary<string, Queue<MetricSnapshot>>();
        private readonly Dictionary<string, MetricSnapshot> _latestSnapshots = new Dictionary<string, MetricSnapshot>();
        private readonly Dictionary<string, MetricStorageStats> _storageStats = new Dictionary<string, MetricStorageStats>();

        // State tracking
        private bool _isInitialized = false;
        private float _lastCleanupTime;
        private readonly float _cleanupInterval = 60f; // Clean up every minute

        // Global storage statistics
        private GlobalStorageStats _globalStats = new GlobalStorageStats();

        // Events
        public event System.Action<string, MetricSnapshot> OnMetricStored;
        public event System.Action<string, int> OnHistoryCleaned;
        public event System.Action<GlobalStorageStats> OnStorageStatsUpdated;

        public bool IsInitialized => _isInitialized;
        public GlobalStorageStats GlobalStats => _globalStats;
        public int TrackedSystemCount => _metricHistory.Count;

        public void Initialize()
        {
            if (_isInitialized) return;

            _metricHistory.Clear();
            _latestSnapshots.Clear();
            _storageStats.Clear();
            ResetGlobalStats();

            _isInitialized = true;

            if (_enableLogging)
            {
                SharedLogger.Log("METRICS", "Metrics Storage Manager initialized", this);
            }
        }

        /// <summary>
        /// Store metric snapshot
        /// </summary>
        public bool StoreMetricSnapshot(string systemName, MetricSnapshot snapshot)
        {
            if (!_isInitialized || snapshot == null || string.IsNullOrEmpty(systemName))
            {
                if (_enableLogging)
                {
                    SharedLogger.LogWarning("METRICS", "Cannot store invalid metric snapshot", this);
                }
                return false;
            }

            // Check system limit
            if (!_metricHistory.ContainsKey(systemName) && _metricHistory.Count >= _maxSystemsTracked)
            {
                if (_enableLogging)
                {
                    SharedLogger.LogWarning("METRICS", $"Maximum tracked systems ({_maxSystemsTracked}) reached", this);
                }
                return false;
            }

            // Get or create history queue
            if (!_metricHistory.TryGetValue(systemName, out var history))
            {
                history = new Queue<MetricSnapshot>();
                _metricHistory[systemName] = history;
                _storageStats[systemName] = new MetricStorageStats();
            }

            // Store snapshot
            snapshot.SystemName = systemName;
            history.Enqueue(snapshot);
            _latestSnapshots[systemName] = snapshot;

            // Update storage stats
            var stats = _storageStats[systemName];
            stats.TotalSnapshotsStored++;
            stats.LastStorageTime = Time.time;
            _storageStats[systemName] = stats;

            // Update global stats
            _globalStats.TotalSnapshotsStored++;
            _globalStats.LastStorageTime = Time.time;

            // Maintain history size
            MaintainHistorySize(systemName, history);

            OnMetricStored?.Invoke(systemName, snapshot);

            if (_enableLogging)
            {
                SharedLogger.Log("METRICS", $"Stored metric snapshot for {systemName} ({history.Count} in history)", this);
            }

            return true;
        }

        /// <summary>
        /// Get metric history for a system
        /// </summary>
        public List<MetricSnapshot> GetMetricHistory(string systemName, int sampleCount = 60)
        {
            if (!_metricHistory.TryGetValue(systemName, out var history))
            {
                return new List<MetricSnapshot>();
            }

            return history.TakeLast(sampleCount).ToList();
        }

        /// <summary>
        /// Get latest metric snapshot for a system
        /// </summary>
        public MetricSnapshot GetLatestMetrics(string systemName)
        {
            _latestSnapshots.TryGetValue(systemName, out var snapshot);
            return snapshot;
        }

        /// <summary>
        /// Get all tracked system names
        /// </summary>
        public List<string> GetTrackedSystems()
        {
            return new List<string>(_metricHistory.Keys);
        }

        /// <summary>
        /// Get storage statistics for a system
        /// </summary>
        public MetricStorageStats GetStorageStats(string systemName)
        {
            if (_storageStats.TryGetValue(systemName, out var stats))
            {
                return stats;
            }
            return new MetricStorageStats();
        }

        /// <summary>
        /// Process storage maintenance (call periodically)
        /// </summary>
        public void ProcessStorageMaintenance()
        {
            if (!_isInitialized) return;

            if (Time.time - _lastCleanupTime >= _cleanupInterval)
            {
                PerformStorageCleanup();
                OptimizeStorage();
                UpdateGlobalStats();
                _lastCleanupTime = Time.time;
            }
        }

        /// <summary>
        /// Clear all metric history
        /// </summary>
        public void ClearAllMetrics()
        {
            var totalCleared = 0;

            foreach (var kvp in _metricHistory)
            {
                totalCleared += kvp.Value.Count;
                kvp.Value.Clear();
            }

            _latestSnapshots.Clear();
            ResetGlobalStats();

            foreach (var system in _storageStats.Keys.ToList())
            {
                var stats = _storageStats[system];
                stats.HistoryClearCount++;
                _storageStats[system] = stats;
            }

            if (_enableLogging)
            {
                SharedLogger.Log("METRICS", $"Cleared all metric history ({totalCleared} snapshots)", this);
            }
        }

        /// <summary>
        /// Clear metric history for specific system
        /// </summary>
        public int ClearSystemMetrics(string systemName)
        {
            if (!_metricHistory.TryGetValue(systemName, out var history))
                return 0;

            var count = history.Count;
            history.Clear();
            _latestSnapshots.Remove(systemName);

            var stats = _storageStats[systemName];
            stats.HistoryClearCount++;
            _storageStats[systemName] = stats;

            OnHistoryCleaned?.Invoke(systemName, count);

            if (_enableLogging)
            {
                SharedLogger.Log("METRICS", $"Cleared {count} snapshots for {systemName}", this);
            }

            return count;
        }

        /// <summary>
        /// Remove system from tracking
        /// </summary>
        public bool RemoveSystem(string systemName)
        {
            if (!_metricHistory.ContainsKey(systemName))
                return false;

            var count = ClearSystemMetrics(systemName);
            _metricHistory.Remove(systemName);
            _storageStats.Remove(systemName);

            if (_enableLogging)
            {
                SharedLogger.Log("METRICS", $"Removed system {systemName} from tracking", this);
            }

            return true;
        }

        /// <summary>
        /// Maintain history size for a system
        /// </summary>
        private void MaintainHistorySize(string systemName, Queue<MetricSnapshot> history)
        {
            var removed = 0;

            while (history.Count > _maxHistorySize)
            {
                history.Dequeue();
                removed++;
            }

            if (removed > 0)
            {
                var stats = _storageStats[systemName];
                stats.SnapshotsDiscarded += removed;
                _storageStats[systemName] = stats;

                _globalStats.SnapshotsDiscarded += removed;
            }
        }

        /// <summary>
        /// Perform storage cleanup
        /// </summary>
        private void PerformStorageCleanup()
        {
            var systemsToRemove = new List<string>();
            var currentTime = Time.time;

            foreach (var kvp in _storageStats)
            {
                var stats = kvp.Value;

                // Remove systems with no recent activity (5 minutes)
                if (currentTime - stats.LastStorageTime > 300f)
                {
                    systemsToRemove.Add(kvp.Key);
                }
            }

            foreach (var system in systemsToRemove)
            {
                RemoveSystem(system);
            }

            if (systemsToRemove.Count > 0 && _enableLogging)
            {
                SharedLogger.Log("METRICS", $"Cleaned up {systemsToRemove.Count} inactive systems", this);
            }
        }

        /// <summary>
        /// Optimize storage for better memory usage
        /// </summary>
        private void OptimizeStorage()
        {
            if (!_enableCompressionOptimization) return;

            // Compress old snapshots (simplified - could implement actual compression)
            foreach (var kvp in _metricHistory)
            {
                var history = kvp.Value;
                if (history.Count > _maxHistorySize * 0.8f)
                {
                    // Could implement compression logic here
                    var stats = _storageStats[kvp.Key];
                    stats.OptimizationCount++;
                    _storageStats[kvp.Key] = stats;
                }
            }
        }

        /// <summary>
        /// Update global storage statistics
        /// </summary>
        private void UpdateGlobalStats()
        {
            _globalStats.TrackedSystems = _metricHistory.Count;
            _globalStats.TotalHistorySize = _metricHistory.Values.Sum(q => q.Count);
            _globalStats.LastMaintenanceTime = Time.time;

            OnStorageStatsUpdated?.Invoke(_globalStats);
        }

        /// <summary>
        /// Set maximum history size
        /// </summary>
        public void SetMaxHistorySize(int maxSize)
        {
            _maxHistorySize = Mathf.Max(1, maxSize);

            // Apply new limit to existing histories
            foreach (var kvp in _metricHistory)
            {
                MaintainHistorySize(kvp.Key, kvp.Value);
            }

            if (_enableLogging)
            {
                SharedLogger.Log("METRICS", $"Max history size set to {_maxHistorySize}", this);
            }
        }

        /// <summary>
        /// Get memory usage estimate
        /// </summary>
        public long GetMemoryUsageEstimate()
        {
            // Rough estimate: each snapshot ~500 bytes
            return _globalStats.TotalHistorySize * 500L;
        }

        /// <summary>
        /// Reset global statistics
        /// </summary>
        private void ResetGlobalStats()
        {
            _globalStats = new GlobalStorageStats
            {
                TotalSnapshotsStored = 0,
                SnapshotsDiscarded = 0,
                TrackedSystems = 0,
                TotalHistorySize = 0,
                LastStorageTime = Time.time,
                LastMaintenanceTime = Time.time
            };
        }

        private void OnDestroy()
        {
            ClearAllMetrics();
        }
    }

    /// <summary>
    /// Storage statistics for individual systems
    /// </summary>
    [System.Serializable]
    public struct MetricStorageStats
    {
        public int TotalSnapshotsStored;
        public int SnapshotsDiscarded;
        public int HistoryClearCount;
        public int OptimizationCount;
        public float LastStorageTime;
    }

    /// <summary>
    /// Global storage statistics
    /// </summary>
    [System.Serializable]
    public struct GlobalStorageStats
    {
        public int TotalSnapshotsStored;
        public int SnapshotsDiscarded;
        public int TrackedSystems;
        public int TotalHistorySize;
        public float LastStorageTime;
        public float LastMaintenanceTime;
    }

    // NOTE: MetricSnapshot is defined in MetricsCollectionFramework.cs. Do not duplicate here.
}
