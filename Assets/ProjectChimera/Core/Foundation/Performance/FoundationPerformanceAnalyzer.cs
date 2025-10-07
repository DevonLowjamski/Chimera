using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using System.Linq;

namespace ProjectChimera.Core.Foundation.Performance
{
    /// <summary>
    /// REFACTORED: Foundation Performance Analyzer - Focused performance analysis and trend detection
    /// Handles performance trend analysis, category determination, and optimization recommendations
    /// Single Responsibility: Performance analysis and trend detection
    /// </summary>
    public class FoundationPerformanceAnalyzer : MonoBehaviour
    {
        [Header("Analysis Settings")]
        [SerializeField] private bool _enableAnalysis = true;
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private float _analysisInterval = 5f;

        [Header("Performance Thresholds")]
        [SerializeField] private float _excellentThreshold = 0.85f; // 85%
        [SerializeField] private float _goodThreshold = 0.70f; // 70%
        [SerializeField] private float _acceptableThreshold = 0.55f; // 55%
        [SerializeField] private float _poorThreshold = 0.40f; // 40%

        [Header("Analysis Configuration")]
        [SerializeField] private int _trendAnalysisPeriod = 10; // Number of measurements for trend
        [SerializeField] private float _significantChangeThreshold = 0.1f; // 10% change
        [SerializeField] private int _consecutivePoorThreshold = 3;

        // Analysis tracking
        private readonly Dictionary<string, List<float>> _performanceHistory = new Dictionary<string, List<float>>();
        private readonly Dictionary<string, PerformanceAnalysisResult> _analysisResults = new Dictionary<string, PerformanceAnalysisResult>();
        private float _overallPerformanceScore = 1.0f;

        // Timing
        private float _lastAnalysis;

        // Properties
        public bool IsEnabled { get; private set; } = true;

        // Events
        public System.Action<string, PerformanceAnalysisResult> OnPerformanceAnalyzed;
        public System.Action<string> OnOptimizationRecommended;
        public System.Action<string, PerformanceTrend> OnTrendDetected;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (_enableLogging)
                ChimeraLogger.Log("FOUNDATION", "🔍 FoundationPerformanceAnalyzer initialized", this);
        }

        /// <summary>
        /// Analyze performance data
        /// </summary>
        public void AnalyzePerformanceData()
        {
            if (!IsEnabled || !_enableAnalysis) return;

            if (Time.time - _lastAnalysis < _analysisInterval) return;

            // Analyze all tracked systems
            foreach (var systemName in _performanceHistory.Keys.ToArray())
            {
                AnalyzeSystemPerformance(systemName);
            }

            _lastAnalysis = Time.time;
        }

        /// <summary>
        /// Notify about system performance update
        /// </summary>
        public void NotifySystemPerformanceUpdated(string systemName, float performanceScore)
        {
            if (!IsEnabled) return;

            // Add to performance history
            if (!_performanceHistory.ContainsKey(systemName))
            {
                _performanceHistory[systemName] = new List<float>();
            }

            _performanceHistory[systemName].Add(performanceScore);

            // Maintain history size
            if (_performanceHistory[systemName].Count > _trendAnalysisPeriod * 2)
            {
                _performanceHistory[systemName].RemoveAt(0);
            }

            // Trigger immediate analysis if significant change
            if (IsSignificantChange(systemName, performanceScore))
            {
                AnalyzeSystemPerformance(systemName);
            }
        }

        /// <summary>
        /// Notify about overall performance change
        /// </summary>
        public void NotifyOverallPerformanceChanged(float overallScore)
        {
            _overallPerformanceScore = overallScore;
        }

        /// <summary>
        /// Get performance category for overall score
        /// </summary>
        public PerformanceCategory GetPerformanceCategory()
        {
            return GetPerformanceCategory(_overallPerformanceScore);
        }

        /// <summary>
        /// Get performance category for specific score
        /// </summary>
        public PerformanceCategory GetPerformanceCategory(float score)
        {
            if (score >= _excellentThreshold)
                return PerformanceCategory.Excellent;
            else if (score >= _goodThreshold)
                return PerformanceCategory.Good;
            else if (score >= _acceptableThreshold)
                return PerformanceCategory.Acceptable;
            else
                return PerformanceCategory.Poor;
        }

        /// <summary>
        /// Get poor performing systems
        /// </summary>
        public string[] GetPoorPerformingSystems()
        {
            var poorSystems = new List<string>();

            foreach (var kvp in _performanceHistory)
            {
                if (kvp.Value.Count > 0)
                {
                    var latestScore = kvp.Value.Last();
                    if (latestScore < _poorThreshold)
                    {
                        poorSystems.Add(kvp.Key);
                    }
                }
            }

            return poorSystems.ToArray();
        }

        /// <summary>
        /// Get analysis result for specific system
        /// </summary>
        public PerformanceAnalysisResult GetAnalysisResult(string systemName)
        {
            _analysisResults.TryGetValue(systemName, out var result);
            return result;
        }

        /// <summary>
        /// Get all analysis results
        /// </summary>
        public Dictionary<string, PerformanceAnalysisResult> GetAllAnalysisResults()
        {
            return new Dictionary<string, PerformanceAnalysisResult>(_analysisResults);
        }

        /// <summary>
        /// Reset analysis data
        /// </summary>
        public void ResetAnalysis()
        {
            _performanceHistory.Clear();
            _analysisResults.Clear();

            if (_enableLogging)
                ChimeraLogger.Log("FOUNDATION", "Performance analysis data reset", this);
        }

        /// <summary>
        /// Set system enabled/disabled
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;

            if (!enabled)
            {
                ResetAnalysis();
            }

            if (_enableLogging)
                ChimeraLogger.Log("FOUNDATION", $"FoundationPerformanceAnalyzer: {(enabled ? "enabled" : "disabled")}", this);
        }

        #region Private Methods

        /// <summary>
        /// Analyze performance for specific system
        /// </summary>
        private void AnalyzeSystemPerformance(string systemName)
        {
            if (!_performanceHistory.TryGetValue(systemName, out var history) || history.Count == 0)
                return;

            var currentScore = history.Last();
            var trend = CalculatePerformanceTrend(history);
            var category = GetPerformanceCategory(currentScore);
            var recommendations = GenerateRecommendations(systemName, history, trend, category);

            var analysisResult = new PerformanceAnalysisResult
            {
                SystemName = systemName,
                CurrentScore = currentScore,
                Trend = trend,
                Category = category,
                Recommendations = recommendations,
                AnalysisTime = Time.time,
                ConsecutivePoorPerformance = CalculateConsecutivePoorPerformance(history),
                ScoreVariability = CalculateScoreVariability(history)
            };

            _analysisResults[systemName] = analysisResult;
            OnPerformanceAnalyzed?.Invoke(systemName, analysisResult);

            // Check if optimization should be recommended
            if (ShouldRecommendOptimization(analysisResult))
            {
                OnOptimizationRecommended?.Invoke(systemName);
            }

            // Fire trend detection event
            if (trend != PerformanceTrend.Stable)
            {
                OnTrendDetected?.Invoke(systemName, trend);
            }

            if (_enableLogging)
                ChimeraLogger.Log("FOUNDATION", $"Analyzed performance for {systemName}: {category} ({currentScore:P2}) - Trend: {trend}", this);
        }

        /// <summary>
        /// Calculate performance trend
        /// </summary>
        private PerformanceTrend CalculatePerformanceTrend(List<float> history)
        {
            if (history.Count < 3) return PerformanceTrend.Stable;

            var recentCount = Mathf.Min(_trendAnalysisPeriod, history.Count);
            var recentValues = history.Skip(history.Count - recentCount).ToArray();

            if (recentValues.Length < 3) return PerformanceTrend.Stable;

            // Simple linear regression to determine trend
            var slope = CalculateLinearSlope(recentValues);

            if (slope > _significantChangeThreshold / recentCount)
                return PerformanceTrend.Improving;
            else if (slope < -_significantChangeThreshold / recentCount)
                return PerformanceTrend.Declining;
            else
                return PerformanceTrend.Stable;
        }

        /// <summary>
        /// Calculate linear slope for trend analysis
        /// </summary>
        private float CalculateLinearSlope(float[] values)
        {
            if (values.Length < 2) return 0f;

            float sumX = 0f, sumY = 0f, sumXY = 0f, sumXX = 0f;
            int n = values.Length;

            for (int i = 0; i < n; i++)
            {
                float x = i;
                float y = values[i];
                sumX += x;
                sumY += y;
                sumXY += x * y;
                sumXX += x * x;
            }

            return (n * sumXY - sumX * sumY) / (n * sumXX - sumX * sumX);
        }

        /// <summary>
        /// Calculate consecutive poor performance count
        /// </summary>
        private int CalculateConsecutivePoorPerformance(List<float> history)
        {
            int consecutiveCount = 0;

            for (int i = history.Count - 1; i >= 0; i--)
            {
                if (history[i] < _poorThreshold)
                {
                    consecutiveCount++;
                }
                else
                {
                    break;
                }
            }

            return consecutiveCount;
        }

        /// <summary>
        /// Calculate score variability
        /// </summary>
        private float CalculateScoreVariability(List<float> history)
        {
            if (history.Count < 2) return 0f;

            var mean = history.Average();
            var variance = history.Sum(x => Mathf.Pow(x - mean, 2)) / history.Count;
            return Mathf.Sqrt(variance);
        }

        /// <summary>
        /// Generate recommendations based on analysis
        /// </summary>
        private List<string> GenerateRecommendations(string systemName, List<float> history, PerformanceTrend trend, PerformanceCategory category)
        {
            var recommendations = new List<string>();

            // Category-based recommendations
            switch (category)
            {
                case PerformanceCategory.Poor:
                    recommendations.Add("System performance is poor - immediate attention required");
                    recommendations.Add("Consider system restart or reinitialization");
                    break;
                case PerformanceCategory.Acceptable:
                    recommendations.Add("System performance is acceptable but could be improved");
                    break;
            }

            // Trend-based recommendations
            switch (trend)
            {
                case PerformanceTrend.Declining:
                    recommendations.Add("Performance is declining - investigate potential causes");
                    recommendations.Add("Monitor system resource usage and bottlenecks");
                    break;
                case PerformanceTrend.Improving:
                    recommendations.Add("Performance is improving - continue current optimization efforts");
                    break;
            }

            // Consecutive poor performance
            var consecutivePoor = CalculateConsecutivePoorPerformance(history);
            if (consecutivePoor >= _consecutivePoorThreshold)
            {
                recommendations.Add($"System has poor performance for {consecutivePoor} consecutive measurements");
                recommendations.Add("Consider escalating to system recovery procedures");
            }

            // High variability
            var variability = CalculateScoreVariability(history);
            if (variability > 0.2f) // 20% variability threshold
            {
                recommendations.Add("Performance shows high variability - investigate unstable conditions");
            }

            return recommendations;
        }

        /// <summary>
        /// Check if significant performance change occurred
        /// </summary>
        private bool IsSignificantChange(string systemName, float currentScore)
        {
            if (!_performanceHistory.TryGetValue(systemName, out var history) || history.Count == 0)
                return false;

            var previousScore = history.Last();
            return Mathf.Abs(currentScore - previousScore) > _significantChangeThreshold;
        }

        /// <summary>
        /// Check if optimization should be recommended
        /// </summary>
        private bool ShouldRecommendOptimization(PerformanceAnalysisResult result)
        {
            return result.Category == PerformanceCategory.Poor ||
                   result.Trend == PerformanceTrend.Declining ||
                   result.ConsecutivePoorPerformance >= _consecutivePoorThreshold;
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Performance analysis result
    /// </summary>
    [System.Serializable]
    public struct PerformanceAnalysisResult
    {
        public string SystemName;
        public float CurrentScore;
        public PerformanceTrend Trend;
        public PerformanceCategory Category;
        public List<string> Recommendations;
        public float AnalysisTime;
        public int ConsecutivePoorPerformance;
        public float ScoreVariability;
    }

    #endregion
}