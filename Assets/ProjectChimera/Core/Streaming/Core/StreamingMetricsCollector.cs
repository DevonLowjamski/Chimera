using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core.Streaming.Core
{
    /// <summary>
    /// REFACTORED: Streaming Metrics Collector - Focused metrics collection and performance tracking
    /// Handles performance metrics, statistics aggregation, and monitoring data
    /// Single Responsibility: Metrics collection and performance analysis
    /// </summary>
    public class StreamingMetricsCollector : MonoBehaviour
    {
        [Header("Metrics Collection Settings")]
        [SerializeField] private bool _enableMetricsCollection = true;
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private float _metricsUpdateInterval = 1f;
        [SerializeField] private int _maxMetricsHistory = 60; // 1 minute at 1 second intervals

        [Header("Performance Tracking")]
        [SerializeField] private bool _trackPerformanceMetrics = true;
        [SerializeField] private bool _trackMemoryMetrics = true;
        [SerializeField] private bool _trackThroughputMetrics = true;

        // Metrics tracking
        private readonly Queue<StreamingPerformanceSnapshot> _performanceHistory = new Queue<StreamingPerformanceSnapshot>();
        private StreamingMetricsData _currentMetrics = new StreamingMetricsData();
        private StreamingMetricsData _aggregatedMetrics = new StreamingMetricsData();

        // Timing
        private float _lastMetricsUpdate;
        private float _metricsStartTime;

        // Performance tracking
        private float _lastFrameTime;
        private int _frameCount;

        // Properties
        public bool IsEnabled { get; private set; } = true;
        public StreamingMetricsData GetCurrentMetrics() => _currentMetrics;
        public StreamingMetricsData GetAggregatedMetrics() => _aggregatedMetrics;
        public Queue<StreamingPerformanceSnapshot> GetPerformanceHistory() => new Queue<StreamingPerformanceSnapshot>(_performanceHistory);

        // Events
        public System.Action<StreamingPerformanceSnapshot> OnMetricsSnapshot;
        public System.Action<StreamingMetricsData> OnMetricsUpdated;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            _metricsStartTime = Time.time;
            _lastMetricsUpdate = Time.time;
            _lastFrameTime = Time.realtimeSinceStartup;

            ResetMetrics();

            if (_enableLogging)
                ChimeraLogger.Log("STREAMING", "📊 StreamingMetricsCollector initialized", this);
        }

        /// <summary>
        /// Update metrics collection
        /// </summary>
        public void UpdateMetrics()
        {
            if (!IsEnabled || !_enableMetricsCollection) return;

            float currentTime = Time.time;

            // Update frame-based metrics
            if (_trackPerformanceMetrics)
            {
                UpdatePerformanceMetrics();
            }

            // Update interval-based metrics
            if (currentTime - _lastMetricsUpdate >= _metricsUpdateInterval)
            {
                CollectMetricsSnapshot();
                _lastMetricsUpdate = currentTime;
            }
        }

        /// <summary>
        /// Record asset load event
        /// </summary>
        public void RecordAssetLoad(string assetKey, float loadTime, bool successful)
        {
            if (!_enableMetricsCollection) return;

            _currentMetrics.TotalLoadAttempts++;

            if (successful)
            {
                _currentMetrics.SuccessfulLoads++;
                _currentMetrics.TotalLoadTime += loadTime;
                _currentMetrics.AverageLoadTime = _currentMetrics.TotalLoadTime / _currentMetrics.SuccessfulLoads;

                if (loadTime > _currentMetrics.MaxLoadTime)
                    _currentMetrics.MaxLoadTime = loadTime;

                if (_currentMetrics.MinLoadTime == 0 || loadTime < _currentMetrics.MinLoadTime)
                    _currentMetrics.MinLoadTime = loadTime;
            }
            else
            {
                _currentMetrics.FailedLoads++;
            }

            // Update success rate
            _currentMetrics.LoadSuccessRate = (float)_currentMetrics.SuccessfulLoads / _currentMetrics.TotalLoadAttempts;

            // Update throughput
            if (_trackThroughputMetrics)
            {
                UpdateThroughputMetrics();
            }
        }

        /// <summary>
        /// Record asset unload event
        /// </summary>
        public void RecordAssetUnload(string assetKey, float unloadTime)
        {
            if (!_enableMetricsCollection) return;

            _currentMetrics.TotalUnloads++;
            _currentMetrics.TotalUnloadTime += unloadTime;
            _currentMetrics.AverageUnloadTime = _currentMetrics.TotalUnloadTime / _currentMetrics.TotalUnloads;

            if (unloadTime > _currentMetrics.MaxUnloadTime)
                _currentMetrics.MaxUnloadTime = unloadTime;
        }

        /// <summary>
        /// Record memory usage
        /// </summary>
        public void RecordMemoryUsage(long currentMemoryUsage, int loadedAssets)
        {
            if (!_enableMetricsCollection || !_trackMemoryMetrics) return;

            _currentMetrics.CurrentMemoryUsage = currentMemoryUsage;
            _currentMetrics.LoadedAssetCount = loadedAssets;

            if (currentMemoryUsage > _currentMetrics.PeakMemoryUsage)
                _currentMetrics.PeakMemoryUsage = currentMemoryUsage;

            // Calculate average memory per asset
            if (loadedAssets > 0)
            {
                _currentMetrics.AverageMemoryPerAsset = currentMemoryUsage / loadedAssets;
            }
        }

        /// <summary>
        /// Record queue metrics
        /// </summary>
        public void RecordQueueMetrics(int queuedLoads, int queuedUnloads, int concurrentLoads)
        {
            if (!_enableMetricsCollection) return;

            _currentMetrics.QueuedLoadRequests = queuedLoads;
            _currentMetrics.QueuedUnloadRequests = queuedUnloads;
            _currentMetrics.ConcurrentLoads = concurrentLoads;

            // Track queue size peaks
            if (queuedLoads > _currentMetrics.PeakLoadQueueSize)
                _currentMetrics.PeakLoadQueueSize = queuedLoads;

            if (queuedUnloads > _currentMetrics.PeakUnloadQueueSize)
                _currentMetrics.PeakUnloadQueueSize = queuedUnloads;
        }

        /// <summary>
        /// Get performance summary
        /// </summary>
        public StreamingPerformanceSummary GetPerformanceSummary()
        {
            return new StreamingPerformanceSummary
            {
                TotalRuntime = Time.time - _metricsStartTime,
                AverageLoadTime = _currentMetrics.AverageLoadTime,
                LoadSuccessRate = _currentMetrics.LoadSuccessRate,
                CurrentMemoryUsage = _currentMetrics.CurrentMemoryUsage,
                PeakMemoryUsage = _currentMetrics.PeakMemoryUsage,
                LoadThroughput = CalculateLoadThroughput(),
                AverageQueueWaitTime = _currentMetrics.AverageQueueWaitTime,
                FrameTimeImpact = CalculateFrameTimeImpact()
            };
        }

        /// <summary>
        /// Reset metrics
        /// </summary>
        public void ResetMetrics()
        {
            _currentMetrics = new StreamingMetricsData
            {
                StartTime = Time.time
            };

            _aggregatedMetrics = new StreamingMetricsData
            {
                StartTime = Time.time
            };

            _performanceHistory.Clear();

            if (_enableLogging)
                ChimeraLogger.Log("STREAMING", "Streaming metrics reset", this);
        }

        /// <summary>
        /// Set system enabled/disabled
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;

            if (!enabled)
            {
                _performanceHistory.Clear();
            }

            if (_enableLogging)
                ChimeraLogger.Log("STREAMING", $"StreamingMetricsCollector: {(enabled ? "enabled" : "disabled")}", this);
        }

        #region Private Methods

        /// <summary>
        /// Update performance metrics
        /// </summary>
        private void UpdatePerformanceMetrics()
        {
            float currentFrameTime = Time.realtimeSinceStartup;
            float deltaTime = currentFrameTime - _lastFrameTime;

            _frameCount++;
            _lastFrameTime = currentFrameTime;

            // Track frame time impact
            if (deltaTime > 0)
            {
                float fps = 1f / deltaTime;
                _currentMetrics.CurrentFPS = fps;

                if (fps < _currentMetrics.MinFPS || _currentMetrics.MinFPS == 0)
                    _currentMetrics.MinFPS = fps;

                if (fps > _currentMetrics.MaxFPS)
                    _currentMetrics.MaxFPS = fps;
            }
        }

        /// <summary>
        /// Collect metrics snapshot
        /// </summary>
        private void CollectMetricsSnapshot()
        {
            var snapshot = new StreamingPerformanceSnapshot
            {
                Timestamp = Time.time,
                LoadedAssets = _currentMetrics.LoadedAssetCount,
                QueuedLoads = _currentMetrics.QueuedLoadRequests,
                QueuedUnloads = _currentMetrics.QueuedUnloadRequests,
                MemoryUsage = _currentMetrics.CurrentMemoryUsage,
                AverageLoadTime = _currentMetrics.AverageLoadTime,
                LoadSuccessRate = _currentMetrics.LoadSuccessRate,
                CurrentFPS = _currentMetrics.CurrentFPS
            };

            _performanceHistory.Enqueue(snapshot);

            // Maintain history size limit
            while (_performanceHistory.Count > _maxMetricsHistory)
            {
                _performanceHistory.Dequeue();
            }

            OnMetricsSnapshot?.Invoke(snapshot);
            OnMetricsUpdated?.Invoke(_currentMetrics);

            // Update aggregated metrics
            UpdateAggregatedMetrics();
        }

        /// <summary>
        /// Update aggregated metrics
        /// </summary>
        private void UpdateAggregatedMetrics()
        {
            if (_performanceHistory.Count == 0) return;

            // Calculate averages over history
            float totalMemoryUsage = 0;
            float totalLoadTime = 0;
            float totalSuccessRate = 0;
            int snapshotCount = _performanceHistory.Count;

            foreach (var snapshot in _performanceHistory)
            {
                totalMemoryUsage += snapshot.MemoryUsage;
                totalLoadTime += snapshot.AverageLoadTime;
                totalSuccessRate += snapshot.LoadSuccessRate;
            }

            _aggregatedMetrics.AverageMemoryUsage = (long)(totalMemoryUsage / snapshotCount);
            _aggregatedMetrics.AverageLoadTime = totalLoadTime / snapshotCount;
            _aggregatedMetrics.AverageSuccessRate = totalSuccessRate / snapshotCount;
        }

        /// <summary>
        /// Update throughput metrics
        /// </summary>
        private void UpdateThroughputMetrics()
        {
            float timeSinceStart = Time.time - _metricsStartTime;
            if (timeSinceStart > 0)
            {
                _currentMetrics.LoadsPerSecond = _currentMetrics.SuccessfulLoads / timeSinceStart;
                _currentMetrics.UnloadsPerSecond = _currentMetrics.TotalUnloads / timeSinceStart;
            }
        }

        /// <summary>
        /// Calculate load throughput
        /// </summary>
        private float CalculateLoadThroughput()
        {
            if (_performanceHistory.Count < 2) return 0f;

            var recentSnapshots = new List<StreamingPerformanceSnapshot>(_performanceHistory);
            int recentCount = Mathf.Min(10, recentSnapshots.Count); // Last 10 snapshots

            if (recentCount < 2) return 0f;

            float timeSpan = recentSnapshots[recentSnapshots.Count - 1].Timestamp -
                           recentSnapshots[recentSnapshots.Count - recentCount].Timestamp;

            return timeSpan > 0 ? recentCount / timeSpan : 0f;
        }

        /// <summary>
        /// Calculate frame time impact
        /// </summary>
        private float CalculateFrameTimeImpact()
        {
            // Simplified calculation - in practice would measure streaming system impact
            return _currentMetrics.CurrentFPS > 0 ? (1000f / _currentMetrics.CurrentFPS) : 0f;
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Streaming metrics data
    /// </summary>
    [System.Serializable]
    public struct StreamingMetricsData
    {
        public float StartTime;
        public int TotalLoadAttempts;
        public int SuccessfulLoads;
        public int FailedLoads;
        public int TotalUnloads;
        public float TotalLoadTime;
        public float TotalUnloadTime;
        public float AverageLoadTime;
        public float AverageUnloadTime;
        public float MaxLoadTime;
        public float MinLoadTime;
        public float MaxUnloadTime;
        public float LoadSuccessRate;
        public float LoadsPerSecond;
        public float UnloadsPerSecond;
        public int LoadedAssetCount;
        public int QueuedLoadRequests;
        public int QueuedUnloadRequests;
        public int ConcurrentLoads;
        public int PeakLoadQueueSize;
        public int PeakUnloadQueueSize;
        public long CurrentMemoryUsage;
        public long PeakMemoryUsage;
        public long AverageMemoryUsage;
        public long AverageMemoryPerAsset;
        public float CurrentFPS;
        public float MinFPS;
        public float MaxFPS;
        public float AverageQueueWaitTime;
        public float AverageSuccessRate;
    }

    /// <summary>
    /// Performance snapshot
    /// </summary>
    [System.Serializable]
    public struct StreamingPerformanceSnapshot
    {
        public float Timestamp;
        public int LoadedAssets;
        public int QueuedLoads;
        public int QueuedUnloads;
        public long MemoryUsage;
        public float AverageLoadTime;
        public float LoadSuccessRate;
        public float CurrentFPS;
    }

    /// <summary>
    /// Performance summary
    /// </summary>
    [System.Serializable]
    public struct StreamingPerformanceSummary
    {
        public float TotalRuntime;
        public float AverageLoadTime;
        public float LoadSuccessRate;
        public long CurrentMemoryUsage;
        public long PeakMemoryUsage;
        public float LoadThroughput;
        public float AverageQueueWaitTime;
        public float FrameTimeImpact;
    }

    #endregion
}