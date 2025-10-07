using System.Collections.Generic;
using System.Linq;
using System;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core.Streaming
{
    /// <summary>
    /// REFACTORED: Streaming Performance Service (POCO - Unity-independent core)
    /// Single Responsibility: Performance monitoring, analysis, and optimization logic
    /// Extracted from StreamingPerformanceMonitor for clean architecture compliance
    /// </summary>
    public class StreamingPerformanceService
    {
        private readonly bool _enableMonitoring;
        private readonly bool _enableAutoOptimization;
        private readonly bool _enableLogging;
        private readonly float _monitoringInterval;
        private readonly float _targetFrameRate;
        private readonly float _acceptableFrameRate;
        private readonly float _criticalFrameRate;
        private readonly long _memoryWarningThreshold;
        private readonly float _optimizationReactionTime;
        private readonly float _maxStreamingRadiusReduction;
        private readonly float _maxLODDistanceReduction;

        // Performance tracking
        private readonly Queue<float> _frameTimeHistory = new Queue<float>();
        private readonly Queue<long> _memoryHistory = new Queue<long>();
        private readonly List<PerformanceSnapshot> _performanceSnapshots = new List<PerformanceSnapshot>();

        // Optimization state
        private StreamingOptimizationState _currentState = StreamingOptimizationState.Optimal;
        private float _lastOptimizationTime;
        private float _streamingRadiusMultiplier = 1f;
        private float _lodDistanceMultiplier = 1f;

        // References (interfaces for testability)
        private AssetStreamingManager _streamingManager;
        private LODManager _lodManager;

        // Statistics
        private StreamingPerformanceStats _stats = new StreamingPerformanceStats();

        public StreamingOptimizationState CurrentState => _currentState;
        public float StreamingRadiusMultiplier => _streamingRadiusMultiplier;
        public float LODDistanceMultiplier => _lodDistanceMultiplier;

        public enum StreamingOptimizationState
        {
            Optimal,
            Warning,
            Degraded,
            Critical
        }

        public StreamingPerformanceService(
            bool enableMonitoring = true,
            bool enableAutoOptimization = true,
            bool enableLogging = true,
            float monitoringInterval = 1f,
            float targetFrameRate = 60f,
            float acceptableFrameRate = 45f,
            float criticalFrameRate = 30f,
            long memoryWarningThreshold = 400 * 1024 * 1024,
            float optimizationReactionTime = 2f,
            float maxStreamingRadiusReduction = 0.5f,
            float maxLODDistanceReduction = 0.3f)
        {
            _enableMonitoring = enableMonitoring;
            _enableAutoOptimization = enableAutoOptimization;
            _enableLogging = enableLogging;
            _monitoringInterval = monitoringInterval;
            _targetFrameRate = targetFrameRate;
            _acceptableFrameRate = acceptableFrameRate;
            _criticalFrameRate = criticalFrameRate;
            _memoryWarningThreshold = memoryWarningThreshold;
            _optimizationReactionTime = optimizationReactionTime;
            _maxStreamingRadiusReduction = maxStreamingRadiusReduction;
            _maxLODDistanceReduction = maxLODDistanceReduction;
        }

        public void Initialize(float currentTime, AssetStreamingManager streamingManager, LODManager lodManager)
        {
            _streamingManager = streamingManager;
            _lodManager = lodManager;
            _lastOptimizationTime = currentTime;

            if (_enableLogging)
            {
                ChimeraLogger.LogInfo("StreamingPerformanceMonitor", "Operation completed");
            }
        }

        public void Tick(float deltaTime, float currentTime, float unscaledDeltaTime)
        {
            if (!_enableMonitoring) return;

            UpdatePerformanceTracking(unscaledDeltaTime);

            if (currentTime - _lastOptimizationTime >= _monitoringInterval)
            {
                AnalyzePerformance(currentTime);
                if (_enableAutoOptimization)
                    OptimizeStreaming(deltaTime);
                _lastOptimizationTime = currentTime;
            }
        }

        public StreamingPerformanceStats GetStats()
        {
            _stats.CurrentFrameRate = _frameTimeHistory.Count > 0 ? 1f / _frameTimeHistory.Average() : 0f;
            _stats.AverageFrameTime = _frameTimeHistory.Count > 0 ? _frameTimeHistory.Average() : 0f;
            _stats.CurrentMemoryUsage = _memoryHistory.Count > 0 ? _memoryHistory.Last() : 0L;
            _stats.OptimizationState = _currentState;
            _stats.StreamingRadiusMultiplier = _streamingRadiusMultiplier;
            _stats.LODDistanceMultiplier = _lodDistanceMultiplier;

            if (_streamingManager != null)
            {
                var streamingStats = _streamingManager.GetStats();
                _stats.LoadedAssets = streamingStats.LoadedAssets;
                _stats.LoadingAssets = streamingStats.LoadingAssets;
            }

            if (_lodManager != null)
            {
                var lodStats = _lodManager.GetStats();
                _stats.VisibleObjects = lodStats.VisibleObjects;
                _stats.RegisteredLODObjects = lodStats.RegisteredObjects;
            }

            return _stats;
        }

        public void ForceOptimization(StreamingOptimizationState targetState, float deltaTime)
        {
            _currentState = targetState;
            OptimizeStreaming(deltaTime);

            if (_enableLogging)
            {
                ChimeraLogger.LogInfo("StreamingPerformanceMonitor", "Operation completed");
            }
        }

        public void ResetOptimizations()
        {
            _streamingRadiusMultiplier = 1f;
            _lodDistanceMultiplier = 1f;
            _currentState = StreamingOptimizationState.Optimal;

            ApplyOptimizations();

            if (_enableLogging)
            {
                ChimeraLogger.LogInfo("StreamingPerformanceMonitor", "Operation completed");
            }
        }

        public List<PerformanceRecommendation> GetRecommendations()
        {
            var recommendations = new List<PerformanceRecommendation>();

            float avgFrameRate = _frameTimeHistory.Count > 0 ? 1f / _frameTimeHistory.Average() : 0f;
            long currentMemory = _memoryHistory.Count > 0 ? _memoryHistory.Last() : 0L;

            if (avgFrameRate < _criticalFrameRate)
            {
                recommendations.Add(new PerformanceRecommendation
                {
                    Priority = RecommendationPriority.Critical,
                    Category = "Performance",
                    Title = "Critical Frame Rate",
                    Description = $"Frame rate ({avgFrameRate:F1} FPS) is critically low. Consider reducing streaming distance and LOD quality.",
                    Impact = "Gameplay Experience"
                });
            }
            else if (avgFrameRate < _acceptableFrameRate)
            {
                recommendations.Add(new PerformanceRecommendation
                {
                    Priority = RecommendationPriority.High,
                    Category = "Performance",
                    Title = "Low Frame Rate",
                    Description = $"Frame rate ({avgFrameRate:F1} FPS) is below acceptable threshold. Consider optimizations.",
                    Impact = "User Experience"
                });
            }

            if (currentMemory > _memoryWarningThreshold)
            {
                recommendations.Add(new PerformanceRecommendation
                {
                    Priority = RecommendationPriority.High,
                    Category = "Memory",
                    Title = "High Memory Usage",
                    Description = $"Memory usage ({currentMemory / 1024 / 1024}MB) is high. Consider reducing asset cache size.",
                    Impact = "System Stability"
                });
            }

            if (_streamingManager != null)
            {
                var streamingStats = _streamingManager.GetStats();
                if (streamingStats.LoadingAssets > 10)
                {
                    recommendations.Add(new PerformanceRecommendation
                    {
                        Priority = RecommendationPriority.Medium,
                        Category = "Streaming",
                        Title = "High Asset Loading",
                        Description = $"{streamingStats.LoadingAssets} assets currently loading. Consider reducing streaming aggressiveness.",
                        Impact = "Loading Performance"
                    });
                }
            }

            return recommendations;
        }

        public void Cleanup()
        {
            _frameTimeHistory.Clear();
            _memoryHistory.Clear();
            _performanceSnapshots.Clear();
        }

        #region Private Methods

        private void UpdatePerformanceTracking(float unscaledDeltaTime)
        {
            _frameTimeHistory.Enqueue(unscaledDeltaTime);
            if (_frameTimeHistory.Count > 60)
            {
                _frameTimeHistory.Dequeue();
            }

            _memoryHistory.Enqueue(System.GC.GetTotalMemory(false));
            if (_memoryHistory.Count > 60)
            {
                _memoryHistory.Dequeue();
            }
        }

        private void AnalyzePerformance(float currentTime)
        {
            float avgFrameRate = _frameTimeHistory.Count > 0 ? 1f / _frameTimeHistory.Average() : 0f;
            long currentMemory = _memoryHistory.Count > 0 ? _memoryHistory.Last() : 0L;

            var snapshot = new PerformanceSnapshot
            {
                Timestamp = currentTime,
                FrameRate = avgFrameRate,
                MemoryUsage = currentMemory,
                LoadedAssets = _streamingManager?.GetStats().LoadedAssets ?? 0,
                VisibleObjects = _lodManager?.GetStats().VisibleObjects ?? 0
            };

            _performanceSnapshots.Add(snapshot);
            if (_performanceSnapshots.Count > 300)
            {
                _performanceSnapshots.RemoveAt(0);
            }

            StreamingOptimizationState newState = DetermineOptimizationState(avgFrameRate, currentMemory);

            if (newState != _currentState)
            {
                _currentState = newState;
                _stats.StateChanges++;

                if (_enableLogging)
                {
                    ChimeraLogger.LogInfo("StreamingPerformanceMonitor", "Operation completed");
                }
            }
        }

        private StreamingOptimizationState DetermineOptimizationState(float frameRate, long memoryUsage)
        {
            if (frameRate < _criticalFrameRate || memoryUsage > _memoryWarningThreshold * 1.5f)
            {
                return StreamingOptimizationState.Critical;
            }

            if (frameRate < _acceptableFrameRate || memoryUsage > _memoryWarningThreshold)
            {
                return StreamingOptimizationState.Degraded;
            }

            if (frameRate < _targetFrameRate * 0.9f || memoryUsage > _memoryWarningThreshold * 0.8f)
            {
                return StreamingOptimizationState.Warning;
            }

            if (_currentState != StreamingOptimizationState.Optimal &&
                frameRate > _targetFrameRate * 1.1f &&
                memoryUsage < _memoryWarningThreshold * 0.6f)
            {
                return StreamingOptimizationState.Optimal;
            }

            return _currentState;
        }

        private void OptimizeStreaming(float deltaTime)
        {
            float targetStreamingMultiplier = 1f;
            float targetLODMultiplier = 1f;

            switch (_currentState)
            {
                case StreamingOptimizationState.Critical:
                    targetStreamingMultiplier = 1f - _maxStreamingRadiusReduction;
                    targetLODMultiplier = 1f - _maxLODDistanceReduction;
                    break;

                case StreamingOptimizationState.Degraded:
                    targetStreamingMultiplier = 1f - (_maxStreamingRadiusReduction * 0.7f);
                    targetLODMultiplier = 1f - (_maxLODDistanceReduction * 0.7f);
                    break;

                case StreamingOptimizationState.Warning:
                    targetStreamingMultiplier = 1f - (_maxStreamingRadiusReduction * 0.3f);
                    targetLODMultiplier = 1f - (_maxLODDistanceReduction * 0.3f);
                    break;

                case StreamingOptimizationState.Optimal:
                    targetStreamingMultiplier = 1f;
                    targetLODMultiplier = 1f;
                    break;
            }

            float adjustmentSpeed = 2f * deltaTime;
            _streamingRadiusMultiplier = Lerp(_streamingRadiusMultiplier, targetStreamingMultiplier, adjustmentSpeed);
            _lodDistanceMultiplier = Lerp(_lodDistanceMultiplier, targetLODMultiplier, adjustmentSpeed);

            ApplyOptimizations();
            _stats.OptimizationChanges++;
        }

        private void ApplyOptimizations()
        {
            if (_enableLogging && (_streamingRadiusMultiplier != 1f || _lodDistanceMultiplier != 1f))
            {
                ChimeraLogger.LogInfo("StreamingPerformanceMonitor", "Operation completed");
            }
        }

        private bool IsPerformanceStable(int sampleCount = 10)
        {
            if (_performanceSnapshots.Count < sampleCount) return false;

            var recentSnapshots = _performanceSnapshots.TakeLast(sampleCount).ToArray();
            float frameRateVariance = CalculateVariance(recentSnapshots.Select(s => s.FrameRate));

            return frameRateVariance < 5f;
        }

        private float CalculateVariance(IEnumerable<float> values)
        {
            var valueList = values.ToList();
            if (valueList.Count == 0) return 0f;

            float mean = valueList.Average();
            float sumSquaredDeviations = valueList.Sum(x => (x - mean) * (x - mean));
            return sumSquaredDeviations / valueList.Count;
        }

        private float Lerp(float a, float b, float t)
        {
            return a + (b - a) * Math.Min(Math.Max(t, 0f), 1f);
        }

        #endregion
    }
}
