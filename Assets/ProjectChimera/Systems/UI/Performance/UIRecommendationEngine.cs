using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using System.Linq;

namespace ProjectChimera.Systems.UI.Performance
{
    /// <summary>
    /// REFACTORED: UI Recommendation Engine - Focused optimization recommendation generation
    /// Handles performance analysis, optimization suggestions, and improvement recommendations
    /// Single Responsibility: UI optimization recommendation generation
    /// </summary>
    public class UIRecommendationEngine : MonoBehaviour
    {
        [Header("Recommendation Settings")]
        [SerializeField] private bool _enableRecommendations = true;
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private float _recommendationUpdateInterval = 5f;
        [SerializeField] private int _maxRecommendations = 10;

        [Header("Performance Thresholds")]
        [SerializeField] private float _frameTimeThreshold = 20f; // 20ms
        [SerializeField] private long _memoryThreshold = 50 * 1024 * 1024; // 50MB
        [SerializeField] private int _componentCountThreshold = 500;
        [SerializeField] private int _drawCallThreshold = 30;

        [Header("Recommendation Priorities")]
        [SerializeField] private float _frameTimeWeight = 0.4f;
        [SerializeField] private float _memoryWeight = 0.3f;
        [SerializeField] private float _componentWeight = 0.2f;
        [SerializeField] private float _drawCallWeight = 0.1f;

        // Recommendation tracking
        private UIOptimizationRecommendations _currentRecommendations = new UIOptimizationRecommendations();
        private readonly Dictionary<UIOptimizationType, RecommendationScore> _recommendationScores = new Dictionary<UIOptimizationType, RecommendationScore>();
        private readonly List<string> _performanceIssues = new List<string>();

        // Timing
        private float _lastRecommendationUpdate;

        // Historical data
        private readonly Queue<UIMetrics> _metricsHistory = new Queue<UIMetrics>();
        private const int MAX_METRICS_HISTORY = 20;

        // Statistics
        private RecommendationEngineStats _stats = new RecommendationEngineStats();

        // Properties
        public bool IsEnabled { get; private set; } = true;
        public RecommendationEngineStats GetStats() => _stats;

        // Events
        public System.Action<UIOptimizationRecommendations> OnRecommendationsGenerated;
        public System.Action<UIOptimizationType, float> OnRecommendationScoreUpdated;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            _stats = new RecommendationEngineStats();
            InitializeRecommendationScores();

            if (_enableLogging)
                ChimeraLogger.Log("UI", "💡 UIRecommendationEngine initialized", this);
        }

        /// <summary>
        /// Generate optimization recommendations based on current metrics
        /// </summary>
        public void GenerateRecommendations(UIMetrics metrics)
        {
            if (!IsEnabled || !_enableRecommendations) return;

            if (Time.time - _lastRecommendationUpdate < _recommendationUpdateInterval) return;

            // Add metrics to history
            _metricsHistory.Enqueue(metrics);
            while (_metricsHistory.Count > MAX_METRICS_HISTORY)
            {
                _metricsHistory.Dequeue();
            }

            // Clear previous recommendations
            _performanceIssues.Clear();

            // Analyze current performance
            AnalyzePerformance(metrics);

            // Calculate recommendation scores
            CalculateRecommendationScores(metrics);

            // Generate recommendations
            var recommendations = CreateRecommendations();

            // Update current recommendations
            _currentRecommendations = recommendations;

            OnRecommendationsGenerated?.Invoke(recommendations);

            _lastRecommendationUpdate = Time.time;
            _stats.RecommendationsGenerated++;

            if (_enableLogging && recommendations.RecommendedOptimizations.Length > 0)
                ChimeraLogger.Log("UI", $"Generated {recommendations.RecommendedOptimizations.Length} UI optimization recommendations", this);
        }

        /// <summary>
        /// Get current recommendations
        /// </summary>
        public UIOptimizationRecommendations GetCurrentRecommendations()
        {
            return _currentRecommendations;
        }

        /// <summary>
        /// Get recommendation score for specific optimization
        /// </summary>
        public float GetRecommendationScore(UIOptimizationType optimization)
        {
            if (_recommendationScores.TryGetValue(optimization, out var score))
            {
                return score.Score;
            }
            return 0f;
        }

        /// <summary>
        /// Get all recommendation scores
        /// </summary>
        public Dictionary<UIOptimizationType, float> GetAllRecommendationScores()
        {
            var scores = new Dictionary<UIOptimizationType, float>();
            foreach (var kvp in _recommendationScores)
            {
                scores[kvp.Key] = kvp.Value.Score;
            }
            return scores;
        }

        /// <summary>
        /// Get performance issues
        /// </summary>
        public string[] GetPerformanceIssues()
        {
            return _performanceIssues.ToArray();
        }

        /// <summary>
        /// Get recommended optimization level
        /// </summary>
        public UIOptimizationLevel GetRecommendedOptimizationLevel(UIMetrics metrics)
        {
            float performanceScore = CalculateOverallPerformanceScore(metrics);

            if (performanceScore < 0.3f) return UIOptimizationLevel.Maximum;
            else if (performanceScore < 0.5f) return UIOptimizationLevel.Aggressive;
            else if (performanceScore < 0.7f) return UIOptimizationLevel.Balanced;
            else if (performanceScore < 0.9f) return UIOptimizationLevel.Conservative;
            else return UIOptimizationLevel.None;
        }

        /// <summary>
        /// Reset recommendations
        /// </summary>
        public void ResetRecommendations()
        {
            _currentRecommendations = new UIOptimizationRecommendations();
            _recommendationScores.Clear();
            _performanceIssues.Clear();
            _metricsHistory.Clear();
            InitializeRecommendationScores();

            if (_enableLogging)
                ChimeraLogger.Log("UI", "Reset UI recommendation data", this);
        }

        /// <summary>
        /// Set system enabled/disabled
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;

            if (!enabled)
            {
                ResetRecommendations();
            }

            if (_enableLogging)
                ChimeraLogger.Log("UI", $"UIRecommendationEngine: {(enabled ? "enabled" : "disabled")}", this);
        }

        #region Private Methods

        /// <summary>
        /// Initialize recommendation scores
        /// </summary>
        private void InitializeRecommendationScores()
        {
            foreach (UIOptimizationType optimization in System.Enum.GetValues(typeof(UIOptimizationType)))
            {
                _recommendationScores[optimization] = new RecommendationScore
                {
                    Optimization = optimization,
                    Score = 0f,
                    Confidence = 0f,
                    PotentialImprovement = GetBaselineImprovement(optimization)
                };
            }
        }

        /// <summary>
        /// Analyze current performance and identify issues
        /// </summary>
        private void AnalyzePerformance(UIMetrics metrics)
        {
            // Frame time analysis
            if (metrics.FrameTime > _frameTimeThreshold)
            {
                float severity = metrics.FrameTime / _frameTimeThreshold;
                _performanceIssues.Add($"Frame time exceeds target: {metrics.FrameTime:F1}ms (target: {_frameTimeThreshold}ms)");

                if (severity > 2f)
                {
                    _performanceIssues.Add("Critical frame time performance issue detected");
                }
            }

            // Memory analysis
            if (metrics.MemoryUsage > _memoryThreshold)
            {
                _performanceIssues.Add($"Memory usage exceeds threshold: {metrics.MemoryUsage / (1024 * 1024):F1}MB");
            }

            // Component count analysis
            if (metrics.ActiveComponents > _componentCountThreshold)
            {
                _performanceIssues.Add($"High UI component count: {metrics.ActiveComponents}");
            }

            // Draw call analysis
            if (metrics.UIDrawCalls > _drawCallThreshold)
            {
                _performanceIssues.Add($"High UI draw call count: {metrics.UIDrawCalls}");
            }

            // Trend analysis
            if (_metricsHistory.Count >= 5)
            {
                AnalyzePerformanceTrends();
            }
        }

        /// <summary>
        /// Analyze performance trends over time
        /// </summary>
        private void AnalyzePerformanceTrends()
        {
            var recentMetrics = _metricsHistory.ToArray();
            if (recentMetrics.Length < 3) return;

            // Check frame time trend
            var frameTimeTrend = CalculateTrend(recentMetrics.Select(m => m.FrameTime).ToArray());
            if (frameTimeTrend > 0.1f)
            {
                _performanceIssues.Add("Frame time is trending upward");
            }

            // Check memory trend
            var memoryTrend = CalculateTrend(recentMetrics.Select(m => (float)m.MemoryUsage).ToArray());
            if (memoryTrend > 0.1f)
            {
                _performanceIssues.Add("Memory usage is trending upward");
            }
        }

        /// <summary>
        /// Calculate recommendation scores for all optimizations
        /// </summary>
        private void CalculateRecommendationScores(UIMetrics metrics)
        {
            float performanceScore = CalculateOverallPerformanceScore(metrics);

            foreach (var optimization in _recommendationScores.Keys.ToArray())
            {
                var score = CalculateOptimizationScore(optimization, metrics, performanceScore);
                var updatedScore = _recommendationScores[optimization];
                updatedScore.Score = score.Score;
                updatedScore.Confidence = score.Confidence;
                _recommendationScores[optimization] = updatedScore;

                OnRecommendationScoreUpdated?.Invoke(optimization, score.Score);
            }
        }

        /// <summary>
        /// Calculate score for specific optimization
        /// </summary>
        private RecommendationScore CalculateOptimizationScore(UIOptimizationType optimization, UIMetrics metrics, float overallScore)
        {
            float score = 0f;
            float confidence = 0f;

            switch (optimization)
            {
                case UIOptimizationType.EnableUIPooling:
                    score = CalculatePoolingScore(metrics);
                    confidence = 0.8f;
                    break;

                case UIOptimizationType.ReduceUpdateFrequency:
                    score = CalculateUpdateFrequencyScore(metrics);
                    confidence = 0.7f;
                    break;

                case UIOptimizationType.EnableBatchedUpdates:
                    score = CalculateBatchingScore(metrics);
                    confidence = 0.9f;
                    break;

                case UIOptimizationType.EnableCanvasCulling:
                    score = CalculateCullingScore(metrics);
                    confidence = 0.6f;
                    break;

                case UIOptimizationType.ReduceMaxUpdatesPerFrame:
                    score = CalculateFrameLimitScore(metrics);
                    confidence = 0.8f;
                    break;

                case UIOptimizationType.OptimizeCanvasStructure:
                    score = CalculateCanvasOptimizationScore(metrics);
                    confidence = 0.5f;
                    break;

                case UIOptimizationType.ReduceUIAnimations:
                    score = CalculateAnimationScore(metrics);
                    confidence = 0.4f;
                    break;

                case UIOptimizationType.EnableAsyncUIUpdates:
                    score = CalculateAsyncScore(metrics);
                    confidence = 0.6f;
                    break;
            }

            return new RecommendationScore
            {
                Optimization = optimization,
                Score = Mathf.Clamp01(score),
                Confidence = confidence,
                PotentialImprovement = GetBaselineImprovement(optimization) * score
            };
        }

        /// <summary>
        /// Create final recommendations based on scores
        /// </summary>
        private UIOptimizationRecommendations CreateRecommendations()
        {
            var sortedRecommendations = _recommendationScores.Values
                .Where(r => r.Score > 0.3f) // Only include recommendations with decent scores
                .OrderByDescending(r => r.Score * r.Confidence)
                .Take(_maxRecommendations)
                .ToArray();

            var recommendations = new UIOptimizationRecommendations
            {
                RecommendedOptimizations = sortedRecommendations.Select(r => r.Optimization).ToArray(),
                SuggestedLevel = GetRecommendedOptimizationLevel(_metricsHistory.LastOrDefault()),
                PerformanceIssues = _performanceIssues.ToArray(),
                PotentialImprovement = sortedRecommendations.Sum(r => r.PotentialImprovement)
            };

            return recommendations;
        }

        /// <summary>
        /// Calculate overall performance score
        /// </summary>
        private float CalculateOverallPerformanceScore(UIMetrics metrics)
        {
            float frameTimeScore = Mathf.Clamp01(1f - (metrics.FrameTime / (_frameTimeThreshold * 2f)));
            float memoryScore = Mathf.Clamp01(1f - ((float)metrics.MemoryUsage / (_memoryThreshold * 2f)));
            float componentScore = Mathf.Clamp01(1f - ((float)metrics.ActiveComponents / (_componentCountThreshold * 2f)));
            float drawCallScore = Mathf.Clamp01(1f - ((float)metrics.UIDrawCalls / (_drawCallThreshold * 2f)));

            return frameTimeScore * _frameTimeWeight +
                   memoryScore * _memoryWeight +
                   componentScore * _componentWeight +
                   drawCallScore * _drawCallWeight;
        }

        /// <summary>
        /// Calculate trend from array of values
        /// </summary>
        private float CalculateTrend(float[] values)
        {
            if (values.Length < 2) return 0f;

            float firstHalf = values.Take(values.Length / 2).Average();
            float secondHalf = values.Skip(values.Length / 2).Average();

            return (secondHalf - firstHalf) / firstHalf;
        }

        /// <summary>
        /// Get baseline improvement estimate for optimization
        /// </summary>
        private float GetBaselineImprovement(UIOptimizationType optimization)
        {
            return optimization switch
            {
                UIOptimizationType.EnableUIPooling => 0.25f,
                UIOptimizationType.ReduceUpdateFrequency => 0.15f,
                UIOptimizationType.EnableBatchedUpdates => 0.30f,
                UIOptimizationType.EnableCanvasCulling => 0.20f,
                UIOptimizationType.ReduceMaxUpdatesPerFrame => 0.10f,
                UIOptimizationType.OptimizeCanvasStructure => 0.35f,
                UIOptimizationType.ReduceUIAnimations => 0.15f,
                UIOptimizationType.EnableAsyncUIUpdates => 0.20f,
                _ => 0.10f
            };
        }

        // Simplified scoring methods for different optimizations
        private float CalculatePoolingScore(UIMetrics metrics) => metrics.ActiveComponents > 100 ? 0.8f : 0.3f;
        private float CalculateUpdateFrequencyScore(UIMetrics metrics) => metrics.FrameTime > _frameTimeThreshold ? 0.7f : 0.2f;
        private float CalculateBatchingScore(UIMetrics metrics) => metrics.UIDrawCalls > 20 ? 0.9f : 0.4f;
        private float CalculateCullingScore(UIMetrics metrics) => metrics.ActiveCanvases > 3 ? 0.6f : 0.2f;
        private float CalculateFrameLimitScore(UIMetrics metrics) => metrics.FrameTime > _frameTimeThreshold * 1.5f ? 0.8f : 0.3f;
        private float CalculateCanvasOptimizationScore(UIMetrics metrics) => metrics.UIDrawCalls > 30 ? 0.9f : 0.3f;
        private float CalculateAnimationScore(UIMetrics metrics) => metrics.FrameTime > _frameTimeThreshold * 2f ? 0.5f : 0.1f;
        private float CalculateAsyncScore(UIMetrics metrics) => metrics.UIUpdateTime > 2f ? 0.6f : 0.2f;

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Recommendation score data
    /// </summary>
    [System.Serializable]
    public struct RecommendationScore
    {
        public UIOptimizationType Optimization;
        public float Score;
        public float Confidence;
        public float PotentialImprovement;
    }

    /// <summary>
    /// Recommendation engine statistics
    /// </summary>
    [System.Serializable]
    public struct RecommendationEngineStats
    {
        public int RecommendationsGenerated;
        public int TotalOptimizationsSuggested;
        public float AverageRecommendationScore;
        public int PerformanceIssuesIdentified;
    }

    #endregion
}