using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core;
using ProjectChimera.Core.SimpleDI;

namespace ProjectChimera.Systems.UI.Performance
{
    /// <summary>
    /// REFACTORED: UI Frame Profiler - Focused frame-by-frame UI performance profiling
    /// Handles frame data collection, timing analysis, and performance trend tracking
    /// Single Responsibility: UI frame profiling and timing analysis
    /// </summary>
    public class UIFrameProfiler : MonoBehaviour
    {
        [Header("Frame Profiling Settings")]
        [SerializeField] private bool _enableProfiling = true;
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private int _maxFrameHistory = 300; // 5 seconds at 60fps
        [SerializeField] private bool _enableDetailedProfiling = true;

        [Header("Profiling Thresholds")]
        [SerializeField] private float _slowFrameThreshold = 20f; // 20ms
        [SerializeField] private float _verySlowFrameThreshold = 33f; // 33ms (30fps)
        [SerializeField] private int _consecutiveSlowFramesAlert = 5;

        [Header("Sampling Settings")]
        [SerializeField] private bool _enableFrameTimeSmoothing = true;
        [SerializeField] private float _smoothingFactor = 0.1f;
        [SerializeField] private bool _trackMemoryPerFrame = true;

        // Frame data tracking
        private readonly Queue<UIFrameData> _frameHistory = new Queue<UIFrameData>();
        private readonly List<float> _frameTimeBuffer = new List<float>();

        // Timing tracking
        private float _lastFrameTime;
        private float _frameStartTime;
        private int _consecutiveSlowFrames;

        // Memory tracking
        private long _frameStartMemory;
        private bool _trackingFrame = false;

        // Statistics
        private UIFrameProfilerStats _stats = new UIFrameProfilerStats();

        // Properties
        public bool IsEnabled { get; private set; } = true;
        public bool IsProfiling { get; private set; } = false;
        public UIFrameProfilerStats GetStats() => _stats;

        // Events
        public System.Action<UIFrameData> OnFrameProfiled;
        public System.Action<float> OnSlowFrameDetected;
        public System.Action OnConsecutiveSlowFramesDetected;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            _stats = new UIFrameProfilerStats();
            _lastFrameTime = Time.realtimeSinceStartup;

            if (_enableLogging)
                ChimeraLogger.Log("UI", "⏱️ UIFrameProfiler initialized", this);
        }

        /// <summary>
        /// Start frame profiling
        /// </summary>
        public void StartProfiling()
        {
            if (!IsEnabled || IsProfiling) return;

            IsProfiling = true;
            _consecutiveSlowFrames = 0;

            if (_enableLogging)
                ChimeraLogger.Log("UI", "Started UI frame profiling", this);
        }

        /// <summary>
        /// Stop frame profiling
        /// </summary>
        public void StopProfiling()
        {
            if (!IsProfiling) return;

            IsProfiling = false;

            if (_enableLogging)
                ChimeraLogger.Log("UI", "Stopped UI frame profiling", this);
        }

        /// <summary>
        /// Process frame data - called once per frame
        /// </summary>
        public void ProcessFrameData()
        {
            if (!IsEnabled || !IsProfiling || !_enableProfiling) return;

            float currentTime = Time.realtimeSinceStartup;
            float frameTime = (currentTime - _lastFrameTime) * 1000f; // Convert to milliseconds

            if (_enableFrameTimeSmoothing)
            {
                frameTime = SmoothFrameTime(frameTime);
            }

            // Create frame data
            var frameData = new UIFrameData
            {
                Timestamp = Time.time,
                FrameTime = frameTime,
                UITime = EstimateUITime(),
                DrawCalls = EstimateUIDrawCalls(),
                MemoryDelta = CalculateMemoryDelta()
            };

            // Add to history
            _frameHistory.Enqueue(frameData);

            // Maintain history size limit
            while (_frameHistory.Count > _maxFrameHistory)
            {
                _frameHistory.Dequeue();
            }

            // Update statistics
            UpdateFrameStatistics(frameData);

            // Check for performance issues
            CheckFramePerformance(frameData);

            OnFrameProfiled?.Invoke(frameData);

            _lastFrameTime = currentTime;
        }

        /// <summary>
        /// Begin UI frame timing
        /// </summary>
        public void BeginFrameTiming()
        {
            if (!IsEnabled || !IsProfiling) return;

            _frameStartTime = Time.realtimeSinceStartup;
            _trackingFrame = true;

            if (_trackMemoryPerFrame)
            {
                _frameStartMemory = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemory();
            }
        }

        /// <summary>
        /// End UI frame timing
        /// </summary>
        public void EndFrameTiming()
        {
            if (!IsEnabled || !IsProfiling || !_trackingFrame) return;

            _trackingFrame = false;
        }

        /// <summary>
        /// Get frame history
        /// </summary>
        public UIFrameData[] GetFrameHistory()
        {
            return _frameHistory.ToArray();
        }

        /// <summary>
        /// Get recent frame data
        /// </summary>
        public UIFrameData[] GetRecentFrames(int count)
        {
            var recentFrames = new List<UIFrameData>(_frameHistory);
            int startIndex = Mathf.Max(0, recentFrames.Count - count);
            return recentFrames.Skip(startIndex).ToArray();
        }

        /// <summary>
        /// Get average frame time over recent history
        /// </summary>
        public float GetAverageFrameTime(int frameCount = 60)
        {
            var recentFrames = GetRecentFrames(frameCount);
            if (recentFrames.Length == 0) return 0f;

            return recentFrames.Average(f => f.FrameTime);
        }

        /// <summary>
        /// Get frame time percentiles
        /// </summary>
        public FrameTimePercentiles GetFrameTimePercentiles()
        {
            if (_frameHistory.Count == 0)
                return new FrameTimePercentiles();

            var frameTimes = _frameHistory.Select(f => f.FrameTime).OrderBy(t => t).ToArray();

            return new FrameTimePercentiles
            {
                P50 = GetPercentile(frameTimes, 0.5f),
                P90 = GetPercentile(frameTimes, 0.9f),
                P95 = GetPercentile(frameTimes, 0.95f),
                P99 = GetPercentile(frameTimes, 0.99f)
            };
        }

        /// <summary>
        /// Get performance trend
        /// </summary>
        public PerformanceTrend GetPerformanceTrend()
        {
            if (_frameHistory.Count < 30) return PerformanceTrend.Stable;

            var recentFrames = GetRecentFrames(30);
            var earlierFrames = _frameHistory.Skip(Math.Max(0, _frameHistory.Count - 60)).Take(30).ToArray();

            if (earlierFrames.Length < 30) return PerformanceTrend.Stable;

            float recentAverage = recentFrames.Average(f => f.FrameTime);
            float earlierAverage = earlierFrames.Average(f => f.FrameTime);

            float percentChange = (recentAverage - earlierAverage) / earlierAverage;

            if (percentChange > 0.1f) return PerformanceTrend.Degrading;
            else if (percentChange < -0.1f) return PerformanceTrend.Improving;
            else return PerformanceTrend.Stable;
        }

        /// <summary>
        /// Reset profile data
        /// </summary>
        public void ResetProfileData()
        {
            _frameHistory.Clear();
            _frameTimeBuffer.Clear();
            _consecutiveSlowFrames = 0;
            _stats = new UIFrameProfilerStats();

            if (_enableLogging)
                ChimeraLogger.Log("UI", "Reset UI frame profile data", this);
        }

        /// <summary>
        /// Set system enabled/disabled
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;

            if (!enabled)
            {
                StopProfiling();
                ResetProfileData();
            }

            if (_enableLogging)
                ChimeraLogger.Log("UI", $"UIFrameProfiler: {(enabled ? "enabled" : "disabled")}", this);
        }

        #region Private Methods

        /// <summary>
        /// Smooth frame time using exponential moving average
        /// </summary>
        private float SmoothFrameTime(float frameTime)
        {
            _frameTimeBuffer.Add(frameTime);

            // Keep only recent samples for smoothing
            if (_frameTimeBuffer.Count > 10)
            {
                _frameTimeBuffer.RemoveAt(0);
            }

            // Apply exponential smoothing
            if (_frameTimeBuffer.Count == 1)
                return frameTime;

            float smoothedTime = _frameTimeBuffer[0];
            for (int i = 1; i < _frameTimeBuffer.Count; i++)
            {
                smoothedTime = smoothedTime * (1f - _smoothingFactor) + _frameTimeBuffer[i] * _smoothingFactor;
            }

            return smoothedTime;
        }

        /// <summary>
        /// Estimate UI processing time for current frame
        /// </summary>
        private float EstimateUITime()
        {
            if (!_trackingFrame) return 0f;

            float uiTime = (Time.realtimeSinceStartup - _frameStartTime) * 1000f;
            return Mathf.Clamp(uiTime, 0f, 50f); // Clamp to reasonable range
        }

        /// <summary>
        /// Estimate UI draw calls for current frame
        /// </summary>
        private int EstimateUIDrawCalls()
        {
            // Simplified estimation - in reality would use actual profiling data
            // Use GameObjectRegistry for canvas tracking
            var registry = ServiceContainerFactory.Instance?.TryResolve<ProjectChimera.Core.Performance.IGameObjectRegistry>();
            var canvases = registry?.GetAll<Canvas>() ?? new Canvas[0];
            int totalDrawCalls = 0;

            foreach (var canvas in canvases)
            {
                if (canvas.enabled && canvas.gameObject.activeInHierarchy)
                {
                    var graphics = canvas.GetComponentsInChildren<UnityEngine.UI.Graphic>();
                    var materials = new HashSet<Material>();

                    foreach (var graphic in graphics)
                    {
                        if (graphic.material != null)
                        {
                            materials.Add(graphic.material);
                        }
                    }

                    totalDrawCalls += materials.Count;
                }
            }

            return totalDrawCalls;
        }

        /// <summary>
        /// Calculate memory delta for current frame
        /// </summary>
        private long CalculateMemoryDelta()
        {
            if (!_trackMemoryPerFrame || !_trackingFrame) return 0;

            long currentMemory = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemory();
            return currentMemory - _frameStartMemory;
        }

        /// <summary>
        /// Update frame statistics
        /// </summary>
        private void UpdateFrameStatistics(UIFrameData frameData)
        {
            _stats.FramesProcessed++;
            _stats.TotalFrameTime += frameData.FrameTime;

            if (_stats.FramesProcessed > 0)
            {
                _stats.AverageFrameTime = _stats.TotalFrameTime / _stats.FramesProcessed;
            }

            if (frameData.FrameTime > _stats.MaxFrameTime)
            {
                _stats.MaxFrameTime = frameData.FrameTime;
            }

            if (_stats.MinFrameTime == 0 || frameData.FrameTime < _stats.MinFrameTime)
            {
                _stats.MinFrameTime = frameData.FrameTime;
            }

            if (frameData.FrameTime > _slowFrameThreshold)
            {
                _stats.SlowFrames++;
            }

            if (frameData.FrameTime > _verySlowFrameThreshold)
            {
                _stats.VerySlowFrames++;
            }
        }

        /// <summary>
        /// Check frame performance and detect issues
        /// </summary>
        private void CheckFramePerformance(UIFrameData frameData)
        {
            if (frameData.FrameTime > _slowFrameThreshold)
            {
                _consecutiveSlowFrames++;
                OnSlowFrameDetected?.Invoke(frameData.FrameTime);

                if (_consecutiveSlowFrames >= _consecutiveSlowFramesAlert)
                {
                    OnConsecutiveSlowFramesDetected?.Invoke();

                    if (_enableLogging)
                        ChimeraLogger.LogWarning("UI", $"Detected {_consecutiveSlowFrames} consecutive slow frames", this);
                }
            }
            else
            {
                _consecutiveSlowFrames = 0;
            }
        }

        /// <summary>
        /// Get percentile value from sorted array
        /// </summary>
        private float GetPercentile(float[] sortedValues, float percentile)
        {
            if (sortedValues.Length == 0) return 0f;

            float index = percentile * (sortedValues.Length - 1);
            int lowerIndex = Mathf.FloorToInt(index);
            int upperIndex = Mathf.CeilToInt(index);

            if (lowerIndex == upperIndex)
                return sortedValues[lowerIndex];

            float weight = index - lowerIndex;
            return sortedValues[lowerIndex] * (1f - weight) + sortedValues[upperIndex] * weight;
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Frame time percentiles
    /// </summary>
    [System.Serializable]
    public struct FrameTimePercentiles
    {
        public float P50; // Median
        public float P90;
        public float P95;
        public float P99;
    }

    /// <summary>
    /// Performance trend enumeration
    /// </summary>
    public enum PerformanceTrend
    {
        Improving,
        Stable,
        Degrading
    }

    /// <summary>
    /// UI frame profiler statistics
    /// </summary>
    [System.Serializable]
    public struct UIFrameProfilerStats
    {
        public int FramesProcessed;
        public float TotalFrameTime;
        public float AverageFrameTime;
        public float MinFrameTime;
        public float MaxFrameTime;
        public int SlowFrames;
        public int VerySlowFrames;
    }

    #endregion
}