using System;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Data.Cultivation.Plant
{
    /// <summary>
    /// PHASE 0 REFACTORED: Sync Trend Analyzer
    /// Single Responsibility: Analyze performance trends, moving averages, and generate performance grades
    /// Extracted from PlantSyncStatisticsTracker for better SRP compliance
    /// </summary>
    public class SyncTrendAnalyzer
    {
        private readonly bool _enableLogging;
        private readonly MovingAverageCalculator _syncTimeAverage;
        private readonly MovingAverageCalculator _successRateAverage;
        private readonly PerformanceTrendAnalyzer _trendAnalyzer;

        private readonly Action<TrendAnalysis> _onTrendAnalysisComplete;

        public float AverageSyncTime => _syncTimeAverage.Average;
        public float SuccessRate => _successRateAverage.Average;

        public SyncTrendAnalyzer(bool enableLogging, Action<TrendAnalysis> onTrendAnalysisComplete)
        {
            _enableLogging = enableLogging;
            _onTrendAnalysisComplete = onTrendAnalysisComplete;

            _syncTimeAverage = new MovingAverageCalculator(50);
            _successRateAverage = new MovingAverageCalculator(100);
            _trendAnalyzer = new PerformanceTrendAnalyzer();
        }

        /// <summary>
        /// Initialize trend analyzer
        /// </summary>
        public void Initialize()
        {
            _syncTimeAverage.Reset();
            _successRateAverage.Reset();
            _trendAnalyzer.Initialize();
        }

        /// <summary>
        /// Update moving averages with new values
        /// </summary>
        public void UpdateAverages(float syncTime, bool success)
        {
            _syncTimeAverage.AddValue(syncTime);
            _successRateAverage.AddValue(success ? 1f : 0f);
        }

        /// <summary>
        /// Perform trend analysis
        /// </summary>
        public TrendAnalysis AnalyzeTrends(SyncPerformanceEntry[] historyData)
        {
            if (historyData.Length < 10)
            {
                return new TrendAnalysis
                {
                    IsValid = false,
                    ErrorMessage = "Insufficient data for trend analysis"
                };
            }

            var analysis = _trendAnalyzer.AnalyzeTrends(historyData);
            _onTrendAnalysisComplete?.Invoke(analysis);

            if (_enableLogging)
            {
                ChimeraLogger.Log("PLANT", $"Trend analysis: {analysis.SyncTimeTrend}, Performance: {analysis.OverallPerformanceGrade}");
            }

            return analysis;
        }

        /// <summary>
        /// Get current performance grade
        /// </summary>
        public PerformanceGrade GetCurrentPerformanceGrade(PlantSyncPerformanceStats stats)
        {
            if (stats.TotalOperations < 10)
                return PerformanceGrade.Unknown;

            var successRate = stats.SuccessRate;
            var avgSyncTime = _syncTimeAverage.Average;

            if (successRate >= 0.95f && avgSyncTime <= 10f)
                return PerformanceGrade.Excellent;

            if (successRate >= 0.9f && avgSyncTime <= 25f)
                return PerformanceGrade.Good;

            if (successRate >= 0.8f && avgSyncTime <= 50f)
                return PerformanceGrade.Fair;

            return PerformanceGrade.Poor;
        }

        /// <summary>
        /// Reset trend analyzer
        /// </summary>
        public void Reset()
        {
            _syncTimeAverage.Reset();
            _successRateAverage.Reset();
            _trendAnalyzer.Reset();
        }
    }

    /// <summary>
    /// Moving average calculator
    /// </summary>
    [Serializable]
    public class MovingAverageCalculator
    {
        private Queue<float> _values;
        private int _windowSize;
        private float _sum;

        public float Average => _values.Count > 0 ? _sum / _values.Count : 0f;
        public int Count => _values.Count;

        public MovingAverageCalculator(int windowSize)
        {
            _windowSize = windowSize;
            _values = new Queue<float>();
            _sum = 0f;
        }

        public void AddValue(float value)
        {
            _values.Enqueue(value);
            _sum += value;

            while (_values.Count > _windowSize)
            {
                _sum -= _values.Dequeue();
            }
        }

        public void Reset()
        {
            _values.Clear();
            _sum = 0f;
        }
    }

    /// <summary>
    /// Performance trend analyzer
    /// </summary>
    [Serializable]
    public class PerformanceTrendAnalyzer
    {
        private List<float> _recentSyncTimes = new List<float>();
        private List<float> _recentSuccessRates = new List<float>();

        public void Initialize()
        {
            _recentSyncTimes.Clear();
            _recentSuccessRates.Clear();
        }

        public TrendAnalysis AnalyzeTrends(SyncPerformanceEntry[] historyData)
        {
            if (historyData.Length < 10)
            {
                return new TrendAnalysis { IsValid = false, ErrorMessage = "Insufficient data" };
            }

            var recentData = historyData.TakeLast(50).ToArray();
            var olderData = historyData.Take(historyData.Length - 50).TakeLast(50).ToArray();

            var recentAvgTime = recentData.Average(e => e.SyncTime);
            var olderAvgTime = olderData.Length > 0 ? olderData.Average(e => e.SyncTime) : recentAvgTime;

            var recentSuccessRate = recentData.Average(e => e.Success ? 1f : 0f);
            var olderSuccessRate = olderData.Length > 0 ? olderData.Average(e => e.Success ? 1f : 0f) : recentSuccessRate;

            return new TrendAnalysis
            {
                IsValid = true,
                SyncTimeTrend = ClassifyTrend(recentAvgTime, olderAvgTime),
                SuccessRateTrend = ClassifyTrend(recentSuccessRate, olderSuccessRate),
                RecentAverageSyncTime = recentAvgTime,
                RecentSuccessRate = recentSuccessRate,
                PerformanceChangePercent = ((recentAvgTime - olderAvgTime) / olderAvgTime) * 100f,
                OverallPerformanceGrade = ClassifyPerformance(recentAvgTime, recentSuccessRate),
                AnalysisTime = DateTime.Now
            };
        }

        public void Reset()
        {
            _recentSyncTimes.Clear();
            _recentSuccessRates.Clear();
        }

        private TrendDirection ClassifyTrend(float recent, float older)
        {
            var changePercent = Math.Abs((recent - older) / older) * 100f;

            if (changePercent < 5f)
                return TrendDirection.Stable;

            return recent > older ? TrendDirection.Increasing : TrendDirection.Decreasing;
        }

        private PerformanceGrade ClassifyPerformance(float avgTime, float successRate)
        {
            if (successRate >= 0.95f && avgTime <= 10f)
                return PerformanceGrade.Excellent;

            if (successRate >= 0.9f && avgTime <= 25f)
                return PerformanceGrade.Good;

            if (successRate >= 0.8f && avgTime <= 50f)
                return PerformanceGrade.Fair;

            return PerformanceGrade.Poor;
        }
    }
}

