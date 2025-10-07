using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;

namespace ProjectChimera.Systems.UI.Performance
{
    /// <summary>
    /// REFACTORED: UI Performance Monitor - Focused UI performance tracking and optimization
    /// Single Responsibility: Monitoring UI performance metrics and providing optimization insights
    /// Extracted from OptimizedUIManager for better SRP compliance
    /// </summary>
    public class UIPerformanceMonitor : ITickable
    {
        private readonly bool _enableLogging;
        private readonly float _updateInterval;

        // Performance tracking
        private UIPerformanceStats _stats = new UIPerformanceStats();
        private readonly Queue<float> _frameTimeHistory = new Queue<float>();
        private readonly Queue<int> _updateCountHistory = new Queue<int>();
        private readonly int _maxHistorySize = 60; // 1 second at 60fps

        // Performance thresholds
        private readonly float _warningFrameTime = 16.67f; // 60fps threshold
        private readonly float _criticalFrameTime = 33.33f; // 30fps threshold
        private readonly int _maxUpdatesPerFrame = 20;

        // Timing
        private float _lastUpdateTime;
        private float _frameStartTime;
        private int _updatesThisFrame;

        // Events
        public event System.Action<UIPerformanceStats> OnPerformanceStatsUpdated;
        public event System.Action<PerformanceWarning> OnPerformanceWarning;

        public UIPerformanceMonitor(bool enableLogging = false, float updateInterval = 0.1f)
        {
            _enableLogging = enableLogging;
            _updateInterval = updateInterval;
            _frameStartTime = Time.realtimeSinceStartup;
        }

        // ITickable implementation
        public int TickPriority => ProjectChimera.Core.Updates.TickPriority.UIManager - 10; // Higher priority than UI
        public bool IsTickable => true;

        #region Performance Tracking

        public void Tick(float deltaTime)
        {
            if (Time.realtimeSinceStartup - _lastUpdateTime >= _updateInterval)
            {
                UpdatePerformanceStats();
                _lastUpdateTime = Time.realtimeSinceStartup;
            }

            // Track frame timing
            TrackFrameTiming(deltaTime);
        }

        /// <summary>
        /// Start tracking a UI operation
        /// </summary>
        public PerformanceTracker StartTracking(string operationName)
        {
            return new PerformanceTracker(operationName, this);
        }

        /// <summary>
        /// Record a UI update operation
        /// </summary>
        public void RecordUIUpdate(string panelName, float updateTime)
        {
            _stats.TotalUIUpdates++;
            _stats.TotalUpdateTime += updateTime;
            _updatesThisFrame++;

            // Track per-panel statistics
            if (!_stats.PanelUpdateTimes.ContainsKey(panelName))
            {
                _stats.PanelUpdateTimes[panelName] = new List<float>();
            }
            _stats.PanelUpdateTimes[panelName].Add(updateTime);

            // Maintain history size
            if (_stats.PanelUpdateTimes[panelName].Count > _maxHistorySize)
            {
                _stats.PanelUpdateTimes[panelName].RemoveAt(0);
            }

            // Check for performance warnings
            CheckPerformanceThresholds(updateTime);
        }

        /// <summary>
        /// Record canvas optimization
        /// </summary>
        public void RecordCanvasOptimization(int canvasCount, int culledCanvases)
        {
            _stats.ManagedCanvasCount = canvasCount;
            _stats.CulledCanvasCount = culledCanvases;
            _stats.CanvasOptimizationRatio = canvasCount > 0 ? (float)culledCanvases / canvasCount : 0f;
        }

        /// <summary>
        /// Record pooling statistics
        /// </summary>
        public void RecordPoolingStats(int pooledElements, int activeElements, int poolHits, int poolMisses)
        {
            _stats.PooledElementCount = pooledElements;
            _stats.ActiveElementCount = activeElements;
            _stats.PoolHitRate = (poolHits + poolMisses) > 0 ? (float)poolHits / (poolHits + poolMisses) : 0f;
        }

        #endregion

        #region Performance Analysis

        /// <summary>
        /// Update overall performance statistics
        /// </summary>
        private void UpdatePerformanceStats()
        {
            // Calculate average frame time
            if (_frameTimeHistory.Count > 0)
            {
                _stats.AverageFrameTime = _frameTimeHistory.Average();
                _stats.MinFrameTime = _frameTimeHistory.Min();
                _stats.MaxFrameTime = _frameTimeHistory.Max();
            }

            // Calculate average update count
            if (_updateCountHistory.Count > 0)
            {
                _stats.AverageUpdatesPerFrame = (float)_updateCountHistory.Average();
            }

            // Calculate overall efficiency
            _stats.UIEfficiencyScore = CalculateEfficiencyScore();

            // Emit events
            OnPerformanceStatsUpdated?.Invoke(_stats);

            if (_enableLogging && _stats.UIEfficiencyScore < 0.7f)
            {
                ChimeraLogger.LogWarning("UI_PERF",
                    $"UI performance below threshold: {_stats.UIEfficiencyScore:F2} efficiency", null);
            }
        }

        /// <summary>
        /// Calculate efficiency score based on multiple factors
        /// </summary>
        private float CalculateEfficiencyScore()
        {
            float score = 1.0f;

            // Penalize high frame times
            if (_stats.AverageFrameTime > _warningFrameTime)
            {
                score *= 0.8f;
            }
            if (_stats.AverageFrameTime > _criticalFrameTime)
            {
                score *= 0.5f;
            }

            // Reward good pooling performance
            if (_stats.PoolHitRate > 0.8f)
            {
                score *= 1.1f;
            }
            else if (_stats.PoolHitRate < 0.5f)
            {
                score *= 0.9f;
            }

            // Reward canvas optimization
            if (_stats.CanvasOptimizationRatio > 0.3f)
            {
                score *= 1.05f;
            }

            return Mathf.Clamp01(score);
        }

        /// <summary>
        /// Track frame timing for performance analysis
        /// </summary>
        private void TrackFrameTiming(float deltaTime)
        {
            var frameTime = deltaTime * 1000f; // Convert to milliseconds

            _frameTimeHistory.Enqueue(frameTime);
            _updateCountHistory.Enqueue(_updatesThisFrame);

            // Maintain history size
            while (_frameTimeHistory.Count > _maxHistorySize)
            {
                _frameTimeHistory.Dequeue();
            }
            while (_updateCountHistory.Count > _maxHistorySize)
            {
                _updateCountHistory.Dequeue();
            }

            // Reset frame counters
            _updatesThisFrame = 0;
        }

        /// <summary>
        /// Check performance thresholds and emit warnings
        /// </summary>
        private void CheckPerformanceThresholds(float updateTime)
        {
            // Check for excessive update time
            if (updateTime > 5.0f) // 5ms threshold
            {
                var warning = new PerformanceWarning
                {
                    Type = PerformanceWarningType.SlowUpdate,
                    Message = $"UI update took {updateTime:F2}ms",
                    Value = updateTime,
                    Timestamp = Time.realtimeSinceStartup
                };
                OnPerformanceWarning?.Invoke(warning);
            }

            // Check for too many updates per frame
            if (_updatesThisFrame > _maxUpdatesPerFrame)
            {
                var warning = new PerformanceWarning
                {
                    Type = PerformanceWarningType.TooManyUpdates,
                    Message = $"Too many UI updates this frame: {_updatesThisFrame}",
                    Value = _updatesThisFrame,
                    Timestamp = Time.realtimeSinceStartup
                };
                OnPerformanceWarning?.Invoke(warning);
            }
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get current performance statistics
        /// </summary>
        public UIPerformanceStats GetPerformanceStats()
        {
            return _stats;
        }

        /// <summary>
        /// Update performance tracking (for UIManagerCore compatibility)
        /// </summary>
        public void UpdatePerformanceTracking(float deltaTime)
        {
            // This method delegates to the normal Tick method
            Tick(deltaTime);
        }

        /// <summary>
        /// Get performance report as string
        /// </summary>
        public string GetPerformanceReport()
        {
            return $"UI Performance Report:\n" +
                   $"- Efficiency Score: {_stats.UIEfficiencyScore:F2}\n" +
                   $"- Average Frame Time: {_stats.AverageFrameTime:F2}ms\n" +
                   $"- Average Updates/Frame: {_stats.AverageUpdatesPerFrame:F1}\n" +
                   $"- Pool Hit Rate: {_stats.PoolHitRate:F2}\n" +
                   $"- Canvas Optimization: {_stats.CanvasOptimizationRatio:F2}\n" +
                   $"- Total UI Updates: {_stats.TotalUIUpdates}";
        }

        /// <summary>
        /// Reset performance statistics
        /// </summary>
        public void ResetStats()
        {
            _stats = new UIPerformanceStats();
            _frameTimeHistory.Clear();
            _updateCountHistory.Clear();
            _updatesThisFrame = 0;

            if (_enableLogging)
                ChimeraLogger.LogInfo("UI_PERF", "Performance statistics reset", null);
        }

        /// <summary>
        /// Get panel-specific performance data
        /// </summary>
        public Dictionary<string, float> GetPanelPerformanceData()
        {
            var result = new Dictionary<string, float>();
            foreach (var kvp in _stats.PanelUpdateTimes)
            {
                if (kvp.Value.Count > 0)
                {
                    result[kvp.Key] = kvp.Value.Average();
                }
            }
            return result;
        }

        #endregion

        #region ITickable Implementation

        public void OnRegistered()
        {
            if (_enableLogging)
                ChimeraLogger.LogInfo("UI_PERF", "UIPerformanceMonitor registered with UpdateOrchestrator", null);
        }

        public void OnUnregistered()
        {
            if (_enableLogging)
                ChimeraLogger.LogInfo("UI_PERF", "UIPerformanceMonitor unregistered from UpdateOrchestrator", null);
        }

        #endregion
    }

    /// <summary>
    /// Performance tracker for individual operations
    /// </summary>
    public class PerformanceTracker : System.IDisposable
    {
        private readonly string _operationName;
        private readonly UIPerformanceMonitor _monitor;
        private readonly float _startTime;

        public PerformanceTracker(string operationName, UIPerformanceMonitor monitor)
        {
            _operationName = operationName;
            _monitor = monitor;
            _startTime = Time.realtimeSinceStartup;
        }

        public void Dispose()
        {
            var duration = (Time.realtimeSinceStartup - _startTime) * 1000f; // Convert to milliseconds
            _monitor.RecordUIUpdate(_operationName, duration);
        }
    }

    /// <summary>
    /// UI performance statistics
    /// </summary>
    [System.Serializable]
    public class UIPerformanceStats
    {
        public float UIEfficiencyScore = 1.0f;
        public float AverageFrameTime = 0f;
        public float MinFrameTime = 0f;
        public float MaxFrameTime = 0f;
        public float AverageUpdatesPerFrame = 0f;
        public int TotalUIUpdates = 0;
        public float TotalUpdateTime = 0f;
        public float PoolHitRate = 0f;
        public int PooledElementCount = 0;
        public int ActiveElementCount = 0;
        public int ManagedCanvasCount = 0;
        public int CulledCanvasCount = 0;
        public float CanvasOptimizationRatio = 0f;
        public Dictionary<string, List<float>> PanelUpdateTimes = new Dictionary<string, List<float>>();
    }

    /// <summary>
    /// Performance warning types
    /// </summary>
    public enum PerformanceWarningType
    {
        SlowUpdate,
        TooManyUpdates,
        MemoryPressure,
        FrameDrops
    }

    /// <summary>
    /// Performance warning information
    /// </summary>
    [System.Serializable]
    public struct PerformanceWarning
    {
        public PerformanceWarningType Type;
        public string Message;
        public float Value;
        public float Timestamp;
    }
}
