using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Shared;

namespace ProjectChimera.Core.Performance
{
    /// <summary>
    /// REFACTORED: Performance Recommendation Service (POCO - Unity-independent core)
    /// Single Responsibility: Performance analysis and recommendation generation
    /// Extracted from PerformanceRecommendationEngine for clean architecture compliance
    /// </summary>
    public class PerformanceRecommendationService
    {
        private readonly bool _enableRecommendations;
        private readonly bool _enableLogging;
        private readonly float _analysisInterval;
        private readonly int _analysisHistorySize;
        private readonly float _frameTimeThreshold;
        private readonly long _memoryThreshold;
        private readonly int _drawCallThreshold;
        private readonly int _triangleThreshold;
        private readonly bool _prioritizeCriticalIssues;
        private readonly int _maxRecommendationsPerAnalysis;

        private AdvancedPerformanceMonitor _performanceMonitor;
        private MetricsCollectionFramework _metricsFramework;

        private float _lastAnalysisTime;
        private readonly Queue<PerformanceRecommendation> _recommendationHistory = new Queue<PerformanceRecommendation>();
        private readonly Dictionary<RecommendationType, float> _lastRecommendationTime = new Dictionary<RecommendationType, float>();
        private List<PerformanceRecommendation> _activeRecommendations = new List<PerformanceRecommendation>();

        public event System.Action<PerformanceRecommendation> OnRecommendationGenerated;
        public event System.Action<List<PerformanceRecommendation>> OnRecommendationSetUpdated;

        public bool IsEnabled => _enableRecommendations;

        public PerformanceRecommendationService(
            bool enableRecommendations = true,
            bool enableLogging = true,
            float analysisInterval = 5.0f,
            int analysisHistorySize = 60,
            float frameTimeThreshold = 20f,
            long memoryThreshold = 400 * 1024 * 1024,
            int drawCallThreshold = 1000,
            int triangleThreshold = 100000,
            bool prioritizeCriticalIssues = true,
            int maxRecommendationsPerAnalysis = 5)
        {
            _enableRecommendations = enableRecommendations;
            _enableLogging = enableLogging;
            _analysisInterval = analysisInterval;
            _analysisHistorySize = analysisHistorySize;
            _frameTimeThreshold = frameTimeThreshold;
            _memoryThreshold = memoryThreshold;
            _drawCallThreshold = drawCallThreshold;
            _triangleThreshold = triangleThreshold;
            _prioritizeCriticalIssues = prioritizeCriticalIssues;
            _maxRecommendationsPerAnalysis = maxRecommendationsPerAnalysis;
        }

        public void Initialize(AdvancedPerformanceMonitor performanceMonitor, MetricsCollectionFramework metricsFramework)
        {
            _performanceMonitor = performanceMonitor;
            _metricsFramework = metricsFramework;

            if (_enableLogging)
            {
                if (_performanceMonitor != null)
                    SharedLogger.Log("PERFORMANCE", "✅ Performance recommendation service initialized with monitor");
                else
                    SharedLogger.LogWarning("PERFORMANCE", "⚠️ Performance recommendation service initialized without monitor - recommendations may be limited");
            }
        }

        public void Tick(float deltaTime, float currentTime)
        {
            if (!_enableRecommendations) return;

            if (currentTime - _lastAnalysisTime >= _analysisInterval)
            {
                AnalyzePerformanceAndGenerateRecommendations(currentTime);
                _lastAnalysisTime = currentTime;
            }
        }

        public void AnalyzePerformanceAndGenerateRecommendations(float currentTime)
        {
            if (_performanceMonitor == null || !_performanceMonitor.IsMonitoring)
                return;

            var analysis = _performanceMonitor.LastAnalysis;
            var currentMetrics = _performanceMonitor.GetCurrentMetrics();

            if (analysis == null || currentMetrics == null)
                return;

            var newRecommendations = new List<PerformanceRecommendation>();

            AnalyzeFrameTimePerformance(analysis, newRecommendations);
            AnalyzeMemoryUsage(analysis, newRecommendations);
            AnalyzeRenderingPerformance(currentMetrics, newRecommendations);
            AnalyzeSystemPerformance(newRecommendations);

            var filteredRecommendations = FilterAndPrioritizeRecommendations(newRecommendations, currentTime);
            UpdateActiveRecommendations(filteredRecommendations);

            if (_enableLogging && filteredRecommendations.Any())
                SharedLogger.Log("PERFORMANCE", $"Generated {filteredRecommendations.Count} performance recommendations");
        }

        public List<PerformanceRecommendation> GetActiveRecommendations()
        {
            return new List<PerformanceRecommendation>(_activeRecommendations);
        }

        public List<PerformanceRecommendation> GetRecommendationHistory(int count = 20)
        {
            return _recommendationHistory.TakeLast(count).ToList();
        }

        public void ClearRecommendationHistory()
        {
            _recommendationHistory.Clear();
            _activeRecommendations.Clear();
            _lastRecommendationTime.Clear();

            if (_enableLogging)
                SharedLogger.Log("PERFORMANCE", "Recommendation history cleared");
        }

        #region Private Analysis Methods

        private void AnalyzeFrameTimePerformance(PerformanceAnalysis analysis, List<PerformanceRecommendation> recommendations)
        {
            if (analysis.AverageFrameTime > _frameTimeThreshold)
            {
                var severity = analysis.AverageFrameTime > _frameTimeThreshold * 2 ? RecommendationSeverity.Critical : RecommendationSeverity.High;

                recommendations.Add(new PerformanceRecommendation
                {
                    Type = RecommendationType.FrameTimeOptimization,
                    Severity = severity,
                    Title = "High Frame Time Detected",
                    Description = $"Average frame time is {analysis.AverageFrameTime:F2}ms (target: {_frameTimeThreshold:F2}ms)",
                    ActionableSteps = new List<string>
                    {
                        "Review Update() method implementations for optimization opportunities",
                        "Consider optimizing Update() methods for better performance",
                        "Implement frame time profiling to identify bottlenecks",
                        "Consider reducing update frequency for non-critical systems"
                    },
                    Impact = PerformanceImpact.High,
                    Effort = ImplementationEffort.Medium,
                    Value = analysis.AverageFrameTime
                });
            }

            if (analysis.FrameTimeStdDev > 5f)
            {
                recommendations.Add(new PerformanceRecommendation
                {
                    Type = RecommendationType.FrameTimeConsistency,
                    Severity = RecommendationSeverity.Medium,
                    Title = "Inconsistent Frame Times",
                    Description = $"Frame time variation is {analysis.FrameTimeStdDev:F2}ms indicating frame spikes",
                    ActionableSteps = new List<string>
                    {
                        "Spread expensive operations across multiple frames",
                        "Implement coroutines for heavy computational tasks",
                        "Review garbage collection patterns",
                        "Consider object pooling for frequently instantiated objects"
                    },
                    Impact = PerformanceImpact.Medium,
                    Effort = ImplementationEffort.Medium,
                    Value = analysis.FrameTimeStdDev
                });
            }
        }

        private void AnalyzeMemoryUsage(PerformanceAnalysis analysis, List<PerformanceRecommendation> recommendations)
        {
            if (analysis.AverageGCMemory > _memoryThreshold)
            {
                var severity = analysis.AverageGCMemory > _memoryThreshold * 2 ? RecommendationSeverity.Critical : RecommendationSeverity.High;

                recommendations.Add(new PerformanceRecommendation
                {
                    Type = RecommendationType.MemoryOptimization,
                    Severity = severity,
                    Title = "High Memory Usage",
                    Description = $"Average memory usage: {analysis.AverageGCMemory / (1024 * 1024):F1}MB",
                    ActionableSteps = new List<string>
                    {
                        "Implement object pooling for frequently created/destroyed objects",
                        "Review asset loading patterns for memory leaks",
                        "Consider more aggressive asset unloading strategies",
                        "Implement periodic garbage collection",
                        "Profile memory allocations to identify hotspots"
                    },
                    Impact = PerformanceImpact.High,
                    Effort = ImplementationEffort.High,
                    Value = (float)(analysis.AverageGCMemory / (1024 * 1024))
                });
            }

            if (analysis.MaxGCMemory > analysis.AverageGCMemory * 1.5)
            {
                recommendations.Add(new PerformanceRecommendation
                {
                    Type = RecommendationType.MemoryLeakPrevention,
                    Severity = RecommendationSeverity.Medium,
                    Title = "Memory Growth Pattern Detected",
                    Description = "Memory usage shows significant spikes indicating potential leaks",
                    ActionableSteps = new List<string>
                    {
                        "Implement memory profiling to identify allocation patterns",
                        "Review event subscription/unsubscription patterns",
                        "Check for proper disposal of IDisposable objects",
                        "Implement automatic memory monitoring and alerting"
                    },
                    Impact = PerformanceImpact.Medium,
                    Effort = ImplementationEffort.Medium,
                    Value = (float)(analysis.MaxGCMemory / (1024 * 1024))
                });
            }
        }

        private void AnalyzeRenderingPerformance(FramePerformanceData metrics, List<PerformanceRecommendation> recommendations)
        {
            if (metrics.DrawCalls > _drawCallThreshold)
            {
                recommendations.Add(new PerformanceRecommendation
                {
                    Type = RecommendationType.RenderingOptimization,
                    Severity = RecommendationSeverity.High,
                    Title = "High Draw Call Count",
                    Description = $"Draw calls: {metrics.DrawCalls} (recommended: <{_drawCallThreshold})",
                    ActionableSteps = new List<string>
                    {
                        "Implement static batching for static geometry",
                        "Use GPU instancing for repeated objects",
                        "Combine similar materials to reduce material switches",
                        "Consider texture atlasing to reduce draw calls",
                        "Implement frustum culling and occlusion culling"
                    },
                    Impact = PerformanceImpact.High,
                    Effort = ImplementationEffort.High,
                    Value = metrics.DrawCalls
                });
            }

            if (metrics.Triangles > _triangleThreshold)
            {
                recommendations.Add(new PerformanceRecommendation
                {
                    Type = RecommendationType.GeometryOptimization,
                    Severity = RecommendationSeverity.Medium,
                    Title = "High Triangle Count",
                    Description = $"Triangle count: {metrics.Triangles:N0} (recommended: <{_triangleThreshold:N0})",
                    ActionableSteps = new List<string>
                    {
                        "Implement Level of Detail (LOD) system",
                        "Optimize mesh complexity for distant objects",
                        "Consider mesh decimation for non-critical geometry",
                        "Implement distance-based object hiding",
                        "Review model complexity in art pipeline"
                    },
                    Impact = PerformanceImpact.Medium,
                    Effort = ImplementationEffort.High,
                    Value = metrics.Triangles
                });
            }
        }

        private void AnalyzeSystemPerformance(List<PerformanceRecommendation> recommendations)
        {
            if (_metricsFramework == null) return;

            var aggregatedMetrics = _metricsFramework.GetAggregatedMetrics();

            foreach (var systemMetric in aggregatedMetrics)
            {
                var systemName = systemMetric.Key;
                var metrics = systemMetric.Value;

                if (metrics.Statistics.TryGetValue("UpdateTime", out var updateTimeStats))
                {
                    if (updateTimeStats.Average > 2f)
                    {
                        recommendations.Add(new PerformanceRecommendation
                        {
                            Type = RecommendationType.SystemOptimization,
                            Severity = RecommendationSeverity.Medium,
                            Title = $"Slow {systemName} Performance",
                            Description = $"{systemName} average update time: {updateTimeStats.Average:F2}ms",
                            ActionableSteps = new List<string>
                            {
                                $"Profile {systemName} for performance bottlenecks",
                                "Consider reducing update frequency if appropriate",
                                "Implement caching for expensive calculations",
                                "Review algorithm complexity and data structures"
                            },
                            Impact = PerformanceImpact.Medium,
                            Effort = ImplementationEffort.Medium,
                            Value = updateTimeStats.Average
                        });
                    }
                }

                if (metrics.Statistics.TryGetValue("MemoryUsage", out var memoryStats))
                {
                    if (memoryStats.Average > 50f)
                    {
                        recommendations.Add(new PerformanceRecommendation
                        {
                            Type = RecommendationType.MemoryOptimization,
                            Severity = RecommendationSeverity.Medium,
                            Title = $"High Memory Usage in {systemName}",
                            Description = $"{systemName} using {memoryStats.Average:F1}MB on average",
                            ActionableSteps = new List<string>
                            {
                                $"Review {systemName} memory allocation patterns",
                                "Implement object pooling where appropriate",
                                "Check for memory leaks in the system",
                                "Consider more efficient data structures"
                            },
                            Impact = PerformanceImpact.Medium,
                            Effort = ImplementationEffort.Medium,
                            Value = memoryStats.Average
                        });
                    }
                }
            }
        }

        private List<PerformanceRecommendation> FilterAndPrioritizeRecommendations(List<PerformanceRecommendation> recommendations, float currentTime)
        {
            var filteredRecommendations = recommendations.Where(r =>
                !_lastRecommendationTime.ContainsKey(r.Type) ||
                currentTime - _lastRecommendationTime[r.Type] > 30f
            ).ToList();

            if (_prioritizeCriticalIssues)
            {
                filteredRecommendations = filteredRecommendations
                    .OrderBy(r => r.Severity)
                    .ThenByDescending(r => r.Impact)
                    .ThenBy(r => r.Effort)
                    .ToList();
            }

            if (filteredRecommendations.Count > _maxRecommendationsPerAnalysis)
                filteredRecommendations = filteredRecommendations.Take(_maxRecommendationsPerAnalysis).ToList();

            foreach (var recommendation in filteredRecommendations)
            {
                _lastRecommendationTime[recommendation.Type] = currentTime;
                recommendation.GeneratedAt = currentTime;
            }

            return filteredRecommendations;
        }

        private void UpdateActiveRecommendations(List<PerformanceRecommendation> newRecommendations)
        {
            foreach (var recommendation in newRecommendations)
            {
                _recommendationHistory.Enqueue(recommendation);
                OnRecommendationGenerated?.Invoke(recommendation);
            }

            while (_recommendationHistory.Count > _analysisHistorySize)
                _recommendationHistory.Dequeue();

            _activeRecommendations = _recommendationHistory.TakeLast(10).ToList();

            if (newRecommendations.Any())
                OnRecommendationSetUpdated?.Invoke(_activeRecommendations);
        }

        #endregion
    }

    #region Recommendation Data Structures

    [System.Serializable]
    public class PerformanceRecommendation
    {
        public RecommendationType Type;
        public RecommendationSeverity Severity;
        public string Title;
        public string Description;
        public List<string> ActionableSteps;
        public PerformanceImpact Impact;
        public ImplementationEffort Effort;
        public float Value;
        public float GeneratedAt;
    }

    public enum RecommendationType
    {
        FrameTimeOptimization,
        FrameTimeConsistency,
        MemoryOptimization,
        MemoryLeakPrevention,
        RenderingOptimization,
        GeometryOptimization,
        SystemOptimization,
        CodeArchitecture,
        AssetOptimization
    }

    public enum RecommendationSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }

    public enum PerformanceImpact
    {
        Low,
        Medium,
        High,
        Critical
    }

    public enum ImplementationEffort
    {
        Low,
        Medium,
        High,
        VeryHigh
    }

    #endregion
}
