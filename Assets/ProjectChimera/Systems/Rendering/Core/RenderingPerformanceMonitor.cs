using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;
using UnityEngine.Profiling;

namespace ProjectChimera.Systems.Rendering.Core
{
    /// <summary>
    /// REFACTORED: Rendering Performance Monitor
    /// Focused component for tracking and reporting rendering performance metrics
    /// </summary>
    public class RenderingPerformanceMonitor : MonoBehaviour, ITickable
    {
        [Header("Performance Monitoring Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private bool _enableDetailedTracking = true;
        [SerializeField] private float _updateInterval = 1f;
        [SerializeField] private int _frameHistorySize = 300;
        [SerializeField] private float _targetFrameRate = 60f;

        // Performance tracking
        private readonly Queue<float> _frameTimeHistory = new Queue<float>();
        private readonly Queue<float> _cpuFrameTimeHistory = new Queue<float>();
        private readonly Queue<float> _gpuFrameTimeHistory = new Queue<float>();

        // Statistics
        private RenderingPerformanceStats _currentStats = new RenderingPerformanceStats();
        private float _lastUpdateTime;
        private int _droppedFramesCount;

        // Memory tracking
        private long _previousMemoryUsage;
        private float _lastMemoryCheck;

        // Properties
        public bool IsEnabled { get; private set; } = true;
        public RenderingPerformanceStats CurrentStats => _currentStats;
        public float TargetFrameRate => _targetFrameRate;
        public bool IsPerformingWell => _currentStats.IsPerformingWell;

        // Events
        public System.Action<RenderingPerformanceStats> OnPerformanceUpdate;
        public System.Action<float> OnFrameRateDrop;
        public System.Action OnPerformanceIssueDetected;

        // ITickable Implementation
        public int TickPriority => -3; // Early monitoring for rendering systems
        public bool IsTickable => IsEnabled;

        private void Start()
        {
            Initialize();
            UpdateOrchestrator.Instance.RegisterTickable(this);
        }

        private void Initialize()
        {
            ResetStats();
            _lastUpdateTime = Time.unscaledTime;
            _lastMemoryCheck = Time.unscaledTime;

            if (_enableLogging)
                ChimeraLogger.Log("RENDERING", "✅ Rendering Performance Monitor initialized", this);
        }

        public void Tick(float deltaTime)
        {
            if (!IsEnabled) return;

            TrackFramePerformance();

            if (Time.unscaledTime - _lastUpdateTime >= _updateInterval)
            {
                UpdatePerformanceStats();
                _lastUpdateTime = Time.unscaledTime;
            }
        }

        /// <summary>
        /// Update performance statistics
        /// </summary>
        public void UpdatePerformanceStats()
        {
            if (!IsEnabled) return;

            CalculateFrameTimeStats();
            UpdateMemoryStats();
            CheckPerformanceIssues();

            OnPerformanceUpdate?.Invoke(_currentStats);

            if (_enableLogging)
            {
                LogPerformanceStats();
            }
        }

        /// <summary>
        /// Get detailed performance report
        /// </summary>
        public RenderingPerformanceReport GetDetailedReport()
        {
            return new RenderingPerformanceReport
            {
                BasicStats = _currentStats,
                FrameTimeHistory = _frameTimeHistory.ToArray(),
                CPUFrameTimeHistory = _cpuFrameTimeHistory.ToArray(),
                GPUFrameTimeHistory = _gpuFrameTimeHistory.ToArray(),
                PerformanceAnalysis = AnalyzePerformance(),
                ReportTimestamp = System.DateTime.Now
            };
        }

        /// <summary>
        /// Reset all performance statistics
        /// </summary>
        public void ResetStats()
        {
            _frameTimeHistory.Clear();
            _cpuFrameTimeHistory.Clear();
            _gpuFrameTimeHistory.Clear();
            _droppedFramesCount = 0;

            _currentStats = new RenderingPerformanceStats
            {
                AverageFrameTime = 0f,
                MinFrameTime = 0f,
                MaxFrameTime = 0f,
                DroppedFrames = 0,
                GPUMemoryUsage = 0f,
                CPUFrameTime = 0f,
                GPUFrameTime = 0f,
                DrawCalls = 0,
                Triangles = 0,
                IsPerformingWell = true
            };

            if (_enableLogging)
                ChimeraLogger.Log("RENDERING", "Performance statistics reset", this);
        }

        private void OnDestroy()
        {
            if (UpdateOrchestrator.Instance != null)
            {
                UpdateOrchestrator.Instance.UnregisterTickable(this);
            }
        }

        /// <summary>
        /// Set performance monitoring enabled/disabled
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;

            if (!enabled)
            {
                _frameTimeHistory.Clear();
                _cpuFrameTimeHistory.Clear();
                _gpuFrameTimeHistory.Clear();
            }

            if (_enableLogging)
                ChimeraLogger.Log("RENDERING", $"Performance monitoring: {(enabled ? "enabled" : "disabled")}", this);
        }

        /// <summary>
        /// Set target frame rate for performance evaluation
        /// </summary>
        public void SetTargetFrameRate(float targetFPS)
        {
            _targetFrameRate = targetFPS;

            if (_enableLogging)
                ChimeraLogger.Log("RENDERING", $"Target frame rate set to: {targetFPS} FPS", this);
        }

        /// <summary>
        /// Check if current performance meets target
        /// </summary>
        public bool IsPerformanceMeetingTarget()
        {
            if (_frameTimeHistory.Count == 0) return true;

            float targetFrameTime = 1f / _targetFrameRate;
            return _currentStats.AverageFrameTime <= targetFrameTime * 1.1f; // 10% tolerance
        }

        /// <summary>
        /// Get performance grade based on metrics
        /// </summary>
        public PerformanceGrade GetPerformanceGrade()
        {
            if (!IsPerformanceMeetingTarget()) return PerformanceGrade.Poor;

            float targetFrameTime = 1f / _targetFrameRate;
            float efficiency = targetFrameTime / _currentStats.AverageFrameTime;

            if (efficiency >= 1.5f) return PerformanceGrade.Excellent;
            if (efficiency >= 1.2f) return PerformanceGrade.Good;
            if (efficiency >= 1.0f) return PerformanceGrade.Fair;
            return PerformanceGrade.Poor;
        }

        private void TrackFramePerformance()
        {
            float frameTime = Time.unscaledDeltaTime;

            // Track frame time
            _frameTimeHistory.Enqueue(frameTime);
            if (_frameTimeHistory.Count > _frameHistorySize)
            {
                _frameTimeHistory.Dequeue();
            }

            // Track CPU frame time (approximated)
            float cpuFrameTime = Time.deltaTime - Time.smoothDeltaTime;
            _cpuFrameTimeHistory.Enqueue(cpuFrameTime);
            if (_cpuFrameTimeHistory.Count > _frameHistorySize)
            {
                _cpuFrameTimeHistory.Dequeue();
            }

            // Track GPU frame time (approximated)
            float gpuFrameTime = Time.smoothDeltaTime;
            _gpuFrameTimeHistory.Enqueue(gpuFrameTime);
            if (_gpuFrameTimeHistory.Count > _frameHistorySize)
            {
                _gpuFrameTimeHistory.Dequeue();
            }

            // Check for dropped frames
            float targetFrameTime = 1f / _targetFrameRate;
            if (frameTime > targetFrameTime * 1.5f) // 50% worse than target
            {
                _droppedFramesCount++;
                OnFrameRateDrop?.Invoke(1f / frameTime);
            }
        }

        private void CalculateFrameTimeStats()
        {
            if (_frameTimeHistory.Count == 0) return;

            var frameTimes = _frameTimeHistory.ToArray();
            _currentStats.AverageFrameTime = frameTimes.Average();
            _currentStats.MinFrameTime = frameTimes.Min();
            _currentStats.MaxFrameTime = frameTimes.Max();

            if (_cpuFrameTimeHistory.Count > 0)
            {
                _currentStats.CPUFrameTime = _cpuFrameTimeHistory.Average();
            }

            if (_gpuFrameTimeHistory.Count > 0)
            {
                _currentStats.GPUFrameTime = _gpuFrameTimeHistory.Average();
            }

            _currentStats.DroppedFrames = _droppedFramesCount;
        }

        private void UpdateMemoryStats()
        {
            if (Time.unscaledTime - _lastMemoryCheck < 0.5f) return; // Check every 500ms

            // GPU Memory (approximated - using total allocated memory as fallback)
            long totalReservedMemory = Profiler.GetTotalAllocatedMemory(); // No parameters needed
            _currentStats.GPUMemoryUsage = totalReservedMemory / (1024f * 1024f); // Convert to MB

            // Draw calls and triangles (would need actual render stats in real implementation)
            _currentStats.DrawCalls = GetEstimatedDrawCalls();
            _currentStats.Triangles = GetEstimatedTriangles();

            _lastMemoryCheck = Time.unscaledTime;
        }

        private void CheckPerformanceIssues()
        {
            bool performingWell = true;

            // Check frame rate
            float targetFrameTime = 1f / _targetFrameRate;
            if (_currentStats.AverageFrameTime > targetFrameTime * 1.2f)
            {
                performingWell = false;
            }

            // Check dropped frames
            if (_currentStats.DroppedFrames > 10) // More than 10 dropped frames
            {
                performingWell = false;
            }

            // Check memory usage (if GPU memory > 1GB, consider it high)
            if (_currentStats.GPUMemoryUsage > 1024f)
            {
                performingWell = false;
            }

            bool wasPerformingWell = _currentStats.IsPerformingWell;
            _currentStats.IsPerformingWell = performingWell;

            if (wasPerformingWell && !performingWell)
            {
                OnPerformanceIssueDetected?.Invoke();

                if (_enableLogging)
                    ChimeraLogger.Log("RENDERING", "⚠️ Performance issues detected", this);
            }
        }

        private string AnalyzePerformance()
        {
            var issues = new List<string>();

            if (_currentStats.AverageFrameTime > 1f / _targetFrameRate * 1.2f)
            {
                issues.Add("Frame rate below target");
            }

            if (_currentStats.DroppedFrames > 5)
            {
                issues.Add($"High dropped frame count: {_currentStats.DroppedFrames}");
            }

            if (_currentStats.GPUMemoryUsage > 1024f)
            {
                issues.Add($"High GPU memory usage: {_currentStats.GPUMemoryUsage:F1} MB");
            }

            if (_currentStats.DrawCalls > 2000)
            {
                issues.Add($"High draw call count: {_currentStats.DrawCalls}");
            }

            return issues.Count > 0 ? string.Join(", ", issues) : "Performance within acceptable ranges";
        }

        private int GetEstimatedDrawCalls()
        {
            // In a real implementation, this would get actual draw call count from rendering statistics
            return Random.Range(500, 1500); // Placeholder
        }

        private int GetEstimatedTriangles()
        {
            // In a real implementation, this would get actual triangle count from rendering statistics
            return Random.Range(50000, 200000); // Placeholder
        }

        private void LogPerformanceStats()
        {
            ChimeraLogger.Log("RENDERING",
                $"Performance - FPS: {1f / _currentStats.AverageFrameTime:F1}, " +
                $"Frame Time: {_currentStats.AverageFrameTime * 1000f:F1}ms, " +
                $"GPU Memory: {_currentStats.GPUMemoryUsage:F1}MB, " +
                $"Draw Calls: {_currentStats.DrawCalls}", this);
        }
    }

    /// <summary>
    /// Detailed rendering performance report
    /// </summary>
    [System.Serializable]
    public struct RenderingPerformanceReport
    {
        public RenderingPerformanceStats BasicStats;
        public float[] FrameTimeHistory;
        public float[] CPUFrameTimeHistory;
        public float[] GPUFrameTimeHistory;
        public string PerformanceAnalysis;
        public System.DateTime ReportTimestamp;
    }

    /// <summary>
    /// Performance grade enumeration
    /// </summary>
    public enum PerformanceGrade
    {
        Excellent,
        Good,
        Fair,
        Poor
    }
}
