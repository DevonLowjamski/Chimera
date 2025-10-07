using UnityEngine;
using System.Collections.Generic;
using System.Diagnostics;
using ProjectChimera.Shared;
// Note: Avoid direct using of Updates to prevent namespace resolution issues in some build setups
using System.Linq;
using System;

namespace ProjectChimera.Core.Performance
{
    /// <summary>
    /// PERFORMANCE: Advanced performance monitoring system for Project Chimera
    /// Provides comprehensive metrics collection, analysis, and optimization recommendations
    /// Migrated to ITickable for centralized update management
    /// </summary>
    public class AdvancedPerformanceMonitor : MonoBehaviour, ProjectChimera.Core.Updates.ITickable
    {
        [Header("Performance Monitoring Settings")]
        [SerializeField] private bool _enableMonitoring = true;
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private float _samplingInterval = 0.1f;
        [SerializeField] private int _maxSampleHistory = 300; // 30 seconds at 0.1s intervals

        [Header("Performance Thresholds")]
        [SerializeField] private float _targetFrameTime = 16.67f; // 60 FPS
        [SerializeField] private float _warningFrameTime = 20f;   // 50 FPS
        [SerializeField] private float _criticalFrameTime = 33.33f; // 30 FPS
        [SerializeField] private long _maxMemoryUsage = 500 * 1024 * 1024; // 500 MB

        [Header("System Monitoring")]
        [SerializeField] private bool _monitorUpdateOrchestrator = true;
        [SerializeField] private bool _monitorMemoryUsage = true;
        [SerializeField] private bool _monitorRenderingPerformance = true;
        [SerializeField] private bool _monitorSystemLoadTimes = true;

        // Performance data
        private readonly Queue<FramePerformanceData> _frameHistory = new Queue<FramePerformanceData>();
        private readonly Dictionary<string, SystemPerformanceData> _systemMetrics = new Dictionary<string, SystemPerformanceData>();
        private readonly Stopwatch _frameStopwatch = new Stopwatch();

        // Current frame data
        private FramePerformanceData _currentFrameData;
        private float _lastSampleTime;
        private int _framesSinceLastSample;

        // Performance analysis
        private PerformanceAnalysis _lastAnalysis;
        private float _lastAnalysisTime;

        // Events
        public event System.Action<PerformanceAnalysis> OnPerformanceAnalyzed;
        public event System.Action<PerformanceWarning> OnPerformanceWarning;


        public bool IsMonitoring => _enableMonitoring;
        public PerformanceAnalysis LastAnalysis => _lastAnalysis;

        private void Awake()
        {
            InitializeMonitoring();

            // Register with UpdateOrchestrator for centralized update management
            ProjectChimera.Core.Updates.UpdateOrchestrator.Instance.RegisterTickable(this);
        }

        private void OnDestroy()
        {
            // Unregister from UpdateOrchestrator
            if (ProjectChimera.Core.Updates.UpdateOrchestrator.Instance != null)
            {
                ProjectChimera.Core.Updates.UpdateOrchestrator.Instance.UnregisterTickable(this);
            }
        }

        private void InitializeMonitoring()
        {
            _frameStopwatch.Start();
            _lastSampleTime = Time.time;

            if (_enableLogging)
            {
                SharedLogger.Log("PERFORMANCE", "Advanced performance monitoring initialized", this);
            }
        }

        #region ITickable Implementation

        /// <summary>
        /// ITickable implementation - called by UpdateOrchestrator
        /// Performance monitoring has high priority to run early in the frame
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (!_enableMonitoring) return;

            CollectFrameMetrics(deltaTime);

            // Sample data at specified intervals
            if (Time.time - _lastSampleTime >= _samplingInterval)
            {
                ProcessPerformanceSample();
                _lastSampleTime = Time.time;
            }

            // Analyze performance periodically
            if (Time.time - _lastAnalysisTime >= 1.0f) // Analyze every second
            {
                AnalyzePerformance();
                _lastAnalysisTime = Time.time;
            }
        }

        /// <summary>
        /// High priority for performance monitoring (runs early in frame)
        /// </summary>
        public int TickPriority => -50; // System priority

        /// <summary>
        /// Should tick when monitoring is enabled
        /// </summary>
        public bool IsTickable => _enableMonitoring && enabled;

        #endregion

        private void CollectFrameMetrics(float deltaTime)
        {
            if (_currentFrameData == null)
            {
                _currentFrameData = new FramePerformanceData
                {
                    Timestamp = Time.time,
                    FrameCount = Time.frameCount
                };
            }

            // Frame time metrics
            _currentFrameData.FrameTime = deltaTime * 1000f; // Convert to milliseconds
            _currentFrameData.FPS = 1f / deltaTime;

            // Memory metrics
            if (_monitorMemoryUsage)
            {
                _currentFrameData.GCMemory = GC.GetTotalMemory(false);
                _currentFrameData.UnityMemory = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
            }

            // Rendering metrics - simplified due to API limitations
            if (_monitorRenderingPerformance)
            {
                // Note: Direct profiler stat access not available in this Unity version
                // Using placeholder values - rendering stats collection disabled
                _currentFrameData.DrawCalls = 0; // Placeholder
                _currentFrameData.Triangles = 0; // Placeholder
                _currentFrameData.Vertices = 0;  // Placeholder
            }

            // Update orchestrator metrics
            // Update orchestrator monitoring disabled due to assembly separation

            _framesSinceLastSample++;
        }


        private void ProcessPerformanceSample()
        {
            if (_currentFrameData == null) return;

            // Finalize current frame data
            _currentFrameData.SampleFrameCount = _framesSinceLastSample;
            _currentFrameData.AverageFrameTime = _frameHistory.Count > 0 ?
                _frameHistory.Average(f => f.FrameTime) : _currentFrameData.FrameTime;

            // Add to history
            _frameHistory.Enqueue(_currentFrameData);

            // Maintain history size
            while (_frameHistory.Count > _maxSampleHistory)
            {
                _frameHistory.Dequeue();
            }

            // Check for performance warnings
            CheckPerformanceWarnings(_currentFrameData);

            // Reset for next sample
            _currentFrameData = null;
            _framesSinceLastSample = 0;
        }

        private void CheckPerformanceWarnings(FramePerformanceData frameData)
        {
            var warnings = new List<PerformanceWarning>();

            // Frame time warnings
            if (frameData.FrameTime > _criticalFrameTime)
            {
                warnings.Add(new PerformanceWarning
                {
                    Type = PerformanceWarningType.CriticalFrameTime,
                    Message = $"Critical frame time: {frameData.FrameTime:F2}ms (target: {_targetFrameTime:F2}ms)",
                    Value = frameData.FrameTime,
                    Threshold = _criticalFrameTime,
                    Severity = PerformanceSeverity.Critical
                });
            }
            else if (frameData.FrameTime > _warningFrameTime)
            {
                warnings.Add(new PerformanceWarning
                {
                    Type = PerformanceWarningType.HighFrameTime,
                    Message = $"High frame time: {frameData.FrameTime:F2}ms (target: {_targetFrameTime:F2}ms)",
                    Value = frameData.FrameTime,
                    Threshold = _warningFrameTime,
                    Severity = PerformanceSeverity.Warning
                });
            }

            // Memory warnings
            if (frameData.GCMemory > _maxMemoryUsage)
            {
                warnings.Add(new PerformanceWarning
                {
                    Type = PerformanceWarningType.HighMemoryUsage,
                    Message = $"High memory usage: {frameData.GCMemory / (1024 * 1024):F1}MB",
                    Value = frameData.GCMemory,
                    Threshold = _maxMemoryUsage,
                    Severity = PerformanceSeverity.Warning
                });
            }

            // Notify about warnings
            foreach (var warning in warnings)
            {
                OnPerformanceWarning?.Invoke(warning);

                if (_enableLogging)
                {
                    var logLevel = warning.Severity == PerformanceSeverity.Critical ? "ERROR" : "WARNING";
                    SharedLogger.Log("PERFORMANCE", $"{logLevel}: {warning.Message}", this);
                }
            }
        }

        private void AnalyzePerformance()
        {
            if (_frameHistory.Count < 10) return; // Need sufficient data

            var recentFrames = _frameHistory.TakeLast(60).ToList(); // Last 6 seconds

            var analysis = new PerformanceAnalysis
            {
                Timestamp = Time.time,
                SampleCount = recentFrames.Count,

                // Frame time analysis
                AverageFrameTime = recentFrames.Average(f => f.FrameTime),
                MinFrameTime = recentFrames.Min(f => f.FrameTime),
                MaxFrameTime = recentFrames.Max(f => f.FrameTime),
                FrameTimeStdDev = CalculateStandardDeviation(recentFrames.Select(f => f.FrameTime)),

                // FPS analysis
                AverageFPS = recentFrames.Average(f => f.FPS),
                MinFPS = recentFrames.Min(f => f.FPS),
                MaxFPS = recentFrames.Max(f => f.FPS),

                // Memory analysis
                AverageGCMemory = recentFrames.Average(f => f.GCMemory),
                MaxGCMemory = recentFrames.Max(f => f.GCMemory),
                AverageUnityMemory = recentFrames.Average(f => f.UnityMemory),
                MaxUnityMemory = recentFrames.Max(f => f.UnityMemory),

                // Rendering analysis
                AverageDrawCalls = recentFrames.Average(f => f.DrawCalls),
                MaxDrawCalls = recentFrames.Max(f => f.DrawCalls),
                AverageTriangles = recentFrames.Average(f => f.Triangles),
                MaxTriangles = recentFrames.Max(f => f.Triangles),

                // System health
                PerformanceHealth = CalculatePerformanceHealth(recentFrames),
                Recommendations = GenerateOptimizationRecommendations(recentFrames)
            };

            _lastAnalysis = analysis;
            OnPerformanceAnalyzed?.Invoke(analysis);

            if (_enableLogging)
            {
                LogPerformanceAnalysis(analysis);
            }
        }

        private float CalculateStandardDeviation(IEnumerable<float> values)
        {
            var enumerable = values.ToList();
            var average = enumerable.Average();
            var sumOfSquaresOfDifferences = enumerable.Select(val => (val - average) * (val - average)).Sum();
            return Mathf.Sqrt(sumOfSquaresOfDifferences / enumerable.Count);
        }

        private PerformanceHealth CalculatePerformanceHealth(List<FramePerformanceData> frames)
        {
            var avgFrameTime = frames.Average(f => f.FrameTime);
            var avgMemory = frames.Average(f => f.GCMemory);
            var frameTimeConsistency = 1f - (CalculateStandardDeviation(frames.Select(f => f.FrameTime)) / avgFrameTime);

            var frameTimeScore = Mathf.InverseLerp(_criticalFrameTime, _targetFrameTime, avgFrameTime);
            var memoryScore = (float)Mathf.InverseLerp(_maxMemoryUsage, _maxMemoryUsage * 0.5f, (float)avgMemory);
            var consistencyScore = Mathf.Clamp01(frameTimeConsistency);

            var overallScore = (float)(frameTimeScore * 0.5f + memoryScore * 0.3f + consistencyScore * 0.2f);

            return new PerformanceHealth
            {
                OverallScore = overallScore,
                FrameTimeScore = frameTimeScore,
                MemoryScore = memoryScore,
                ConsistencyScore = consistencyScore,
                HealthLevel = overallScore > 0.8f ? PerformanceLevel.Excellent :
                            overallScore > 0.6f ? PerformanceLevel.Good :
                            overallScore > 0.4f ? PerformanceLevel.Fair :
                            overallScore > 0.2f ? PerformanceLevel.Poor : PerformanceLevel.Critical
            };
        }

        private List<string> GenerateOptimizationRecommendations(List<FramePerformanceData> frames)
        {
            var recommendations = new List<string>();
            var avgFrameTime = frames.Average(f => f.FrameTime);
            var avgDrawCalls = frames.Average(f => f.DrawCalls);
            var avgTriangles = frames.Average(f => f.Triangles);
            var avgMemory = frames.Average(f => f.GCMemory);

            if (avgFrameTime > _warningFrameTime)
            {
                recommendations.Add("Consider reducing Update() method complexity or frequency");
                recommendations.Add("Review Update() implementations for optimization opportunities");
            }

            if (avgDrawCalls > 1000)
            {
                recommendations.Add("High draw call count - consider batching similar objects");
                recommendations.Add("Review material usage and consider texture atlasing");
            }

            if (avgTriangles > 100000)
            {
                recommendations.Add("High triangle count - implement LOD system for distant objects");
                recommendations.Add("Consider mesh optimization and decimation");
            }

            if (avgMemory > _maxMemoryUsage * 0.8f)
            {
                recommendations.Add("High memory usage - review object pooling implementation");
                recommendations.Add("Consider more aggressive asset unloading");
            }

            var frameTimeVariation = CalculateStandardDeviation(frames.Select(f => f.FrameTime));
            if (frameTimeVariation > 5f)
            {
                recommendations.Add("High frame time variation - investigate frame spikes");
                recommendations.Add("Consider spreading expensive operations across multiple frames");
            }

            return recommendations;
        }

        private void LogPerformanceAnalysis(PerformanceAnalysis analysis)
        {
            SharedLogger.Log("PERFORMANCE",
                $"Analysis: FPS={analysis.AverageFPS:F1} FrameTime={analysis.AverageFrameTime:F2}ms " +
                $"Memory={analysis.AverageGCMemory / (1024*1024):F1}MB Health={analysis.PerformanceHealth.HealthLevel}",
                this);

            if (analysis.Recommendations.Any())
            {
                SharedLogger.Log("PERFORMANCE",
                    $"Recommendations: {string.Join(", ", analysis.Recommendations.Take(2))}",
                    this);
            }
        }

        /// <summary>
        /// Get current performance metrics
        /// </summary>
        public FramePerformanceData GetCurrentMetrics()
        {
            return _frameHistory.LastOrDefault();
        }

        /// <summary>
        /// Get performance history
        /// </summary>
        public IEnumerable<FramePerformanceData> GetPerformanceHistory(int sampleCount = 60)
        {
            return _frameHistory.TakeLast(sampleCount);
        }

        /// <summary>
        /// Force immediate performance analysis
        /// </summary>
        public PerformanceAnalysis AnalyzeNow()
        {
            AnalyzePerformance();
            return _lastAnalysis;
        }

        /// <summary>
        /// Reset performance history
        /// </summary>
        [ContextMenu("Reset Performance History")]
        public void ResetHistory()
        {
            _frameHistory.Clear();
            _systemMetrics.Clear();

            if (_enableLogging)
            {
                SharedLogger.Log("PERFORMANCE", "Performance history reset", this);
            }
        }

        /// <summary>
        /// Export performance data for analysis
        /// </summary>
        public PerformanceReport ExportPerformanceReport()
        {
            return new PerformanceReport
            {
                GeneratedAt = DateTime.Now,
                MonitoringDuration = Time.time,
                TotalSamples = _frameHistory.Count,
                RecentAnalysis = _lastAnalysis,
                PerformanceHistory = _frameHistory.ToList(),
                SystemMetrics = new Dictionary<string, SystemPerformanceData>(_systemMetrics)
            };
        }
    }
}
