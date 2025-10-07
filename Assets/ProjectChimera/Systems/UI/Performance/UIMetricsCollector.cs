using UnityEngine;
using UnityEngine.Profiling;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core;
using ProjectChimera.Core.SimpleDI;
using ProjectChimera.Core.Memory;
using ProjectChimera.Core.SimpleDI;

namespace ProjectChimera.Systems.UI.Performance
{
    /// <summary>
    /// REFACTORED: UI Metrics Collector - Focused UI metrics collection and tracking
    /// Handles frame time monitoring, memory usage tracking, and UI component counting
    /// Single Responsibility: UI metrics collection and measurement
    /// </summary>
    public class UIMetricsCollector : MonoBehaviour
    {
        [Header("Metrics Collection Settings")]
        [SerializeField] private bool _enableMetricsCollection = true;
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private float _metricsUpdateInterval = 0.1f;
        [SerializeField] private int _maxMetricsHistory = 300;

        [Header("Performance Targets")]
        [SerializeField] private float _targetFrameTime = 16.67f; // 60 FPS
        [SerializeField] private float _maxUIUpdateTime = 2f; // 2ms per frame for UI
        [SerializeField] private long _maxUIMemoryUsage = 50 * 1024 * 1024; // 50MB

        // Metrics tracking
        private UIMetrics _currentMetrics = new UIMetrics();
        private readonly Queue<UIMetricsSnapshot> _metricsHistory = new Queue<UIMetricsSnapshot>();

        // Frame timing
        private float _lastMetricsUpdate;
        private float _frameTimeAccumulator;
        private int _frameCount;
        private float _lastFrameTime;

        // Memory tracking
        private long _baselineMemory;
        private bool _memoryBaselineSet = false;

        // Component tracking
        private readonly Dictionary<System.Type, int> _componentCounts = new Dictionary<System.Type, int>();

        // Statistics
        private UIMetricsStats _stats = new UIMetricsStats();

        // Properties
        public bool IsEnabled { get; private set; } = true;
        public bool IsMonitoring { get; private set; } = false;
        public UIMetrics GetMetrics() => _currentMetrics;
        public UIMetricsStats GetStats() => _stats;

        // Events
        public System.Action<float> OnFrameTimeChanged;
        public System.Action<long> OnMemoryUsageChanged;
        public System.Action<UIMetricsSnapshot> OnMetricsSnapshot;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            _stats = new UIMetricsStats();
            _lastMetricsUpdate = Time.time;
            _lastFrameTime = Time.realtimeSinceStartup;

            if (_enableLogging)
                ChimeraLogger.Log("UI", "📊 UIMetricsCollector initialized", this);
        }

        /// <summary>
        /// Start metrics collection
        /// </summary>
        public void StartMonitoring()
        {
            if (!IsEnabled || IsMonitoring) return;

            IsMonitoring = true;
            SetMemoryBaseline();

            if (_enableLogging)
                ChimeraLogger.Log("UI", "Started UI metrics collection", this);
        }

        /// <summary>
        /// Stop metrics collection
        /// </summary>
        public void StopMonitoring()
        {
            if (!IsMonitoring) return;

            IsMonitoring = false;

            if (_enableLogging)
                ChimeraLogger.Log("UI", "Stopped UI metrics collection", this);
        }

        /// <summary>
        /// Collect current UI metrics
        /// </summary>
        public void CollectMetrics()
        {
            if (!IsEnabled || !IsMonitoring || !_enableMetricsCollection) return;

            if (Time.time - _lastMetricsUpdate < _metricsUpdateInterval) return;

            // Collect frame timing metrics
            CollectFrameMetrics();

            // Collect memory metrics
            CollectMemoryMetrics();

            // Collect UI component metrics
            CollectComponentMetrics();

            // Create metrics snapshot
            CreateMetricsSnapshot();

            _lastMetricsUpdate = Time.time;
            _stats.MetricsCollected++;
        }

        /// <summary>
        /// Get metrics history
        /// </summary>
        public UIMetricsSnapshot[] GetMetricsHistory()
        {
            return _metricsHistory.ToArray();
        }

        /// <summary>
        /// Get average frame time over recent history
        /// </summary>
        public float GetAverageFrameTime()
        {
            if (_metricsHistory.Count == 0) return _currentMetrics.FrameTime;

            float total = 0f;
            foreach (var snapshot in _metricsHistory)
            {
                total += snapshot.FrameTime;
            }
            return total / _metricsHistory.Count;
        }

        /// <summary>
        /// Get memory usage trend
        /// </summary>
        public MemoryTrend GetMemoryTrend()
        {
            if (_metricsHistory.Count < 10) return MemoryTrend.Stable;

            var recentSnapshots = new List<UIMetricsSnapshot>(_metricsHistory);
            int recentCount = Mathf.Min(10, recentSnapshots.Count);

            if (recentCount < 2) return MemoryTrend.Stable;

            long startMemory = recentSnapshots[recentSnapshots.Count - recentCount].MemoryUsage;
            long endMemory = recentSnapshots[recentSnapshots.Count - 1].MemoryUsage;

            float percentChange = (float)(endMemory - startMemory) / startMemory;

            if (percentChange > 0.1f) return MemoryTrend.Increasing;
            else if (percentChange < -0.1f) return MemoryTrend.Decreasing;
            else return MemoryTrend.Stable;
        }

        /// <summary>
        /// Check if performance targets are being met
        /// </summary>
        public bool IsPerformanceTargetMet()
        {
            return _currentMetrics.FrameTime <= _targetFrameTime &&
                   _currentMetrics.UIUpdateTime <= _maxUIUpdateTime &&
                   _currentMetrics.MemoryUsage <= _maxUIMemoryUsage;
        }

        /// <summary>
        /// Reset metrics data
        /// </summary>
        public void ResetMetrics()
        {
            _currentMetrics = new UIMetrics();
            _metricsHistory.Clear();
            _componentCounts.Clear();
            _frameTimeAccumulator = 0f;
            _frameCount = 0;
            _memoryBaselineSet = false;

            if (_enableLogging)
                ChimeraLogger.Log("UI", "Reset UI metrics data", this);
        }

        /// <summary>
        /// Set system enabled/disabled
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;

            if (!enabled)
            {
                StopMonitoring();
                ResetMetrics();
            }

            if (_enableLogging)
                ChimeraLogger.Log("UI", $"UIMetricsCollector: {(enabled ? "enabled" : "disabled")}", this);
        }

        #region Private Methods

        /// <summary>
        /// Set memory baseline for relative measurements
        /// </summary>
        private void SetMemoryBaseline()
        {
            if (!_memoryBaselineSet)
            {
                _baselineMemory = Profiler.GetTotalAllocatedMemoryLong();
                _memoryBaselineSet = true;

                if (_enableLogging)
                    ChimeraLogger.Log("UI", $"Set UI memory baseline: {_baselineMemory / (1024 * 1024)}MB", this);
            }
        }

        /// <summary>
        /// Collect frame timing metrics
        /// </summary>
        private void CollectFrameMetrics()
        {
            float currentFrameTime = Time.realtimeSinceStartup;
            float deltaTime = currentFrameTime - _lastFrameTime;

            if (deltaTime > 0)
            {
                _frameTimeAccumulator += deltaTime * 1000f; // Convert to milliseconds
                _frameCount++;

                if (_frameCount > 0)
                {
                    float averageFrameTime = _frameTimeAccumulator / _frameCount;

                    if (Mathf.Abs(averageFrameTime - _currentMetrics.FrameTime) > 0.5f)
                    {
                        _currentMetrics.FrameTime = averageFrameTime;
                        OnFrameTimeChanged?.Invoke(averageFrameTime);
                    }
                }

                // Reset accumulator periodically
                if (_frameCount >= 60)
                {
                    _frameTimeAccumulator = 0f;
                    _frameCount = 0;
                }
            }

            _lastFrameTime = currentFrameTime;
        }

        /// <summary>
        /// Collect memory usage metrics
        /// </summary>
        private void CollectMemoryMetrics()
        {
            if (!_memoryBaselineSet) return;

            long currentMemory = Profiler.GetTotalAllocatedMemoryLong();
            long uiMemoryUsage = currentMemory - _baselineMemory;

            if (uiMemoryUsage != _currentMetrics.MemoryUsage)
            {
                _currentMetrics.MemoryUsage = uiMemoryUsage;
                OnMemoryUsageChanged?.Invoke(uiMemoryUsage);
            }

            // Track memory statistics
            if (uiMemoryUsage > _stats.PeakMemoryUsage)
            {
                _stats.PeakMemoryUsage = uiMemoryUsage;
            }
        }

        /// <summary>
        /// Collect UI component metrics
        /// </summary>
        private void CollectComponentMetrics()
        {
            _componentCounts.Clear();

            // Count active UI components
            // Use GameObjectRegistry for canvas tracking
            var registry = ServiceContainerFactory.Instance?.TryResolve<ProjectChimera.Core.Performance.IGameObjectRegistry>();
            var canvases = registry?.GetAll<Canvas>() ?? new Canvas[0];
            _currentMetrics.ActiveCanvases = canvases.Length;

            int totalComponents = 0;
            int drawCalls = 0;

            foreach (var canvas in canvases)
            {
                if (!canvas.enabled) continue;

                var components = canvas.GetComponentsInChildren<UnityEngine.UI.Graphic>();
                totalComponents += components.Length;

                // Estimate draw calls (simplified)
                drawCalls += EstimateDrawCalls(canvas);

                // Count component types
                foreach (var component in components)
                {
                    var type = component.GetType();
                    if (!_componentCounts.ContainsKey(type))
                    {
                        _componentCounts[type] = 0;
                    }
                    _componentCounts[type]++;
                }
            }

            _currentMetrics.ActiveComponents = totalComponents;
            _currentMetrics.UIDrawCalls = drawCalls;

            // Estimate UI update time based on component count
            _currentMetrics.UIUpdateTime = EstimateUIUpdateTime(totalComponents);
            _currentMetrics.TotalUpdateTime = _currentMetrics.UIUpdateTime;
        }

        /// <summary>
        /// Estimate draw calls for a canvas
        /// </summary>
        private int EstimateDrawCalls(Canvas canvas)
        {
            // Simplified draw call estimation
            var graphics = canvas.GetComponentsInChildren<UnityEngine.UI.Graphic>();
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

        /// <summary>
        /// Estimate UI update time based on component count
        /// </summary>
        private float EstimateUIUpdateTime(int componentCount)
        {
            // Simplified estimation: assume each component takes ~0.01ms to update
            return componentCount * 0.01f;
        }

        /// <summary>
        /// Create metrics snapshot
        /// </summary>
        private void CreateMetricsSnapshot()
        {
            var snapshot = new UIMetricsSnapshot
            {
                Timestamp = Time.time,
                FrameTime = _currentMetrics.FrameTime,
                UIUpdateTime = _currentMetrics.UIUpdateTime,
                MemoryUsage = _currentMetrics.MemoryUsage,
                ActiveComponents = _currentMetrics.ActiveComponents,
                UIDrawCalls = _currentMetrics.UIDrawCalls,
                ActiveCanvases = _currentMetrics.ActiveCanvases
            };

            _metricsHistory.Enqueue(snapshot);

            // Maintain history size limit
            while (_metricsHistory.Count > _maxMetricsHistory)
            {
                _metricsHistory.Dequeue();
            }

            OnMetricsSnapshot?.Invoke(snapshot);
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// UI metrics data structure
    /// </summary>
    [System.Serializable]
    public struct UIMetrics
    {
        public float FrameTime;
        public float UIUpdateTime;
        public long MemoryUsage;
        public int ActiveComponents;
        public int UIDrawCalls;
        public int ActiveCanvases;
        public float TotalUpdateTime;
    }

    /// <summary>
    /// UI metrics snapshot
    /// </summary>
    [System.Serializable]
    public struct UIMetricsSnapshot
    {
        public float Timestamp;
        public float FrameTime;
        public float UIUpdateTime;
        public long MemoryUsage;
        public int ActiveComponents;
        public int UIDrawCalls;
        public int ActiveCanvases;
    }

    /// <summary>
    /// Memory trend enumeration
    /// </summary>
    public enum MemoryTrend
    {
        Stable,
        Increasing,
        Decreasing
    }

    /// <summary>
    /// UI metrics statistics
    /// </summary>
    [System.Serializable]
    public struct UIMetricsStats
    {
        public int MetricsCollected;
        public long PeakMemoryUsage;
        public float BestFrameTime;
        public float WorstFrameTime;
        public int TotalSnapshots;
    }

    #endregion
}