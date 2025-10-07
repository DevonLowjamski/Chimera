using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core.Foundation.Performance
{
    /// <summary>
    /// REFACTORED: Optimization History Service (POCO - Unity-independent core)
    /// Single Responsibility: Optimization history recording, analysis, and reporting
    /// Extracted from OptimizationHistoryTracker for clean architecture compliance
    /// </summary>
    public class OptimizationHistoryService
    {
        private readonly bool _enableLogging;
        private readonly int _maxHistoryPerSystem;
        private readonly int _maxTotalSystems;
        private readonly float _historyRetentionDays;
        private readonly bool _enableTrendAnalysis;
        private readonly int _trendAnalysisSampleSize;
        private readonly float _analysisInterval;

        private readonly Dictionary<string, OptimizationHistory> _systemHistories = new Dictionary<string, OptimizationHistory>();
        private readonly Dictionary<string, OptimizationTrends> _systemTrends = new Dictionary<string, OptimizationTrends>();

        private float _lastAnalysisTime;
        private bool _isInitialized = false;
        private OptimizationHistoryStats _stats = new OptimizationHistoryStats();

        public event System.Action<string, OptimizationAttempt> OnOptimizationRecorded;
        public event System.Action<string, OptimizationTrends> OnTrendsUpdated;
        public event System.Action<OptimizationHistoryStats> OnStatsUpdated;

        public bool IsInitialized => _isInitialized;
        public OptimizationHistoryStats Stats => _stats;
        public int TrackedSystemCount => _systemHistories.Count;

        public OptimizationHistoryService(
            bool enableLogging = false,
            int maxHistoryPerSystem = 50,
            int maxTotalSystems = 100,
            float historyRetentionDays = 7f,
            bool enableTrendAnalysis = true,
            int trendAnalysisSampleSize = 10,
            float analysisInterval = 300f)
        {
            _enableLogging = enableLogging;
            _maxHistoryPerSystem = maxHistoryPerSystem;
            _maxTotalSystems = maxTotalSystems;
            _historyRetentionDays = historyRetentionDays;
            _enableTrendAnalysis = enableTrendAnalysis;
            _trendAnalysisSampleSize = trendAnalysisSampleSize;
            _analysisInterval = analysisInterval;
        }

        public void Initialize()
        {
            if (_isInitialized) return;

            _systemHistories.Clear();
            _systemTrends.Clear();
            ResetStats();

            _isInitialized = true;

            if (_enableLogging)
                ChimeraLogger.Log("FOUNDATION", "Optimization History Service initialized");
        }

        public void RecordOptimization(OptimizationRequest request, OptimizationStrategy strategy, bool success, float currentTime)
        {
            if (!_isInitialized || string.IsNullOrEmpty(request.SystemName))
            {
                if (_enableLogging)
                    ChimeraLogger.LogWarning("FOUNDATION", "Cannot record optimization - invalid parameters");
                return;
            }

            if (_systemHistories.Count >= _maxTotalSystems && !_systemHistories.ContainsKey(request.SystemName))
            {
                if (_enableLogging)
                    ChimeraLogger.LogWarning("FOUNDATION", $"Maximum tracked systems ({_maxTotalSystems}) reached");
                return;
            }

            var attempt = new OptimizationAttempt
            {
                AttemptId = System.Guid.NewGuid().ToString(),
                Strategy = strategy,
                AttemptTime = currentTime,
                Success = success,
                Reason = request.Reason,
                Priority = request.Priority,
                ProcessingDuration = request.ProcessingDuration,
                RequestId = request.RequestId
            };

            RecordAttempt(request.SystemName, attempt, currentTime);
        }

        public void RecordAttempt(string systemName, OptimizationAttempt attempt, float currentTime)
        {
            if (!_isInitialized || string.IsNullOrEmpty(systemName))
                return;

            if (!_systemHistories.TryGetValue(systemName, out var history))
            {
                history = new OptimizationHistory
                {
                    SystemName = systemName,
                    FirstOptimizationTime = currentTime,
                    OptimizationAttempts = new List<OptimizationAttempt>()
                };
                _systemHistories[systemName] = history;
            }

            history.OptimizationAttempts.Add(attempt);
            history.LastOptimizationTime = currentTime;
            history.TotalAttempts++;

            if (attempt.Success)
                history.SuccessfulAttempts++;

            MaintainHistorySize(history);

            _stats.TotalOptimizationsRecorded++;
            if (attempt.Success) _stats.TotalSuccessfulOptimizations++;
            _stats.LastRecordingTime = currentTime;

            OnOptimizationRecorded?.Invoke(systemName, attempt);

            if (_enableLogging)
            {
                var result = attempt.Success ? "successful" : "failed";
                ChimeraLogger.Log("FOUNDATION", $"Recorded {result} {attempt.Strategy} optimization for {systemName}");
            }
        }

        public void ProcessTrendAnalysis(float currentTime)
        {
            if (!_isInitialized || !_enableTrendAnalysis) return;

            if (currentTime - _lastAnalysisTime >= _analysisInterval)
            {
                AnalyzeTrendsForAllSystems();
                CleanupOldHistory(currentTime);
                _lastAnalysisTime = currentTime;
                _stats.LastAnalysisTime = currentTime;
            }
        }

        public OptimizationHistory GetHistory(string systemName)
        {
            if (_systemHistories.TryGetValue(systemName, out var history))
                return history;
            return null;
        }

        public OptimizationTrends GetTrends(string systemName)
        {
            if (_systemTrends.TryGetValue(systemName, out var trends))
                return trends;
            return null;
        }

        public List<OptimizationAttempt> GetRecentAttempts(string systemName, int count = 10)
        {
            if (_systemHistories.TryGetValue(systemName, out var history))
                return history.OptimizationAttempts.TakeLast(count).ToList();
            return new List<OptimizationAttempt>();
        }

        public float GetSuccessRate(string systemName)
        {
            if (_systemHistories.TryGetValue(systemName, out var history))
            {
                if (history.TotalAttempts == 0) return 0f;
                return (float)history.SuccessfulAttempts / history.TotalAttempts;
            }
            return 0f;
        }

        public OptimizationStrategy GetMostEffectiveStrategy(string systemName)
        {
            if (!_systemHistories.TryGetValue(systemName, out var history))
                return OptimizationStrategy.ConfigurationTuning;

            var strategyStats = new Dictionary<OptimizationStrategy, (int total, int successful)>();

            foreach (var attempt in history.OptimizationAttempts)
            {
                if (!strategyStats.ContainsKey(attempt.Strategy))
                    strategyStats[attempt.Strategy] = (0, 0);

                var stats = strategyStats[attempt.Strategy];
                stats.total++;
                if (attempt.Success) stats.successful++;
                strategyStats[attempt.Strategy] = stats;
            }

            var bestStrategy = strategyStats
                .Where(kvp => kvp.Value.total >= 3)
                .OrderByDescending(kvp => (float)kvp.Value.successful / kvp.Value.total)
                .FirstOrDefault();

            return bestStrategy.Key != default ? bestStrategy.Key : OptimizationStrategy.ConfigurationTuning;
        }

        public List<string> GetTrackedSystems()
        {
            return new List<string>(_systemHistories.Keys);
        }

        public void ClearAllHistory()
        {
            var totalCleared = _systemHistories.Values.Sum(h => h.OptimizationAttempts.Count);

            _systemHistories.Clear();
            _systemTrends.Clear();
            ResetStats();

            if (_enableLogging && totalCleared > 0)
                ChimeraLogger.Log("FOUNDATION", $"Cleared optimization history ({totalCleared} attempts)");
        }

        public OptimizationHistoryReport GenerateReport()
        {
            var totalAttempts = _systemHistories.Values.Sum(h => h.TotalAttempts);
            var report = new OptimizationHistoryReport
            {
                GenerationTime = _stats.LastRecordingTime,
                TrackedSystems = _systemHistories.Count,
                TotalOptimizations = totalAttempts,
                OverallSuccessRate = totalAttempts > 0 ? _systemHistories.Values.Sum(h => h.SuccessfulAttempts) / (float)totalAttempts : 0f,
                SystemReports = new Dictionary<string, SystemOptimizationReport>()
            };

            foreach (var kvp in _systemHistories)
            {
                var systemName = kvp.Key;
                var history = kvp.Value;
                var trends = _systemTrends.ContainsKey(systemName) ? _systemTrends[systemName] : null;

                var systemReport = new SystemOptimizationReport
                {
                    SystemName = systemName,
                    TotalAttempts = history.TotalAttempts,
                    SuccessfulAttempts = history.SuccessfulAttempts,
                    SuccessRate = GetSuccessRate(systemName),
                    MostEffectiveStrategy = GetMostEffectiveStrategy(systemName),
                    Trends = trends,
                    RecentAttempts = GetRecentAttempts(systemName, 5)
                };

                report.SystemReports[systemName] = systemReport;
            }

            return report;
        }

        #region Private Methods

        private void AnalyzeTrendsForAllSystems()
        {
            foreach (var kvp in _systemHistories)
            {
                var systemName = kvp.Key;
                var history = kvp.Value;

                if (history.OptimizationAttempts.Count >= _trendAnalysisSampleSize)
                {
                    var trends = AnalyzeTrends(history);
                    _systemTrends[systemName] = trends;
                    OnTrendsUpdated?.Invoke(systemName, trends);
                }
            }

            _stats.TrendAnalysesPerformed++;
            OnStatsUpdated?.Invoke(_stats);
        }

        private OptimizationTrends AnalyzeTrends(OptimizationHistory history)
        {
            var recentAttempts = history.OptimizationAttempts.TakeLast(_trendAnalysisSampleSize).ToList();
            var successRate = recentAttempts.Count(a => a.Success) / (float)recentAttempts.Count;

            var trendDirection = AnalyzeTrendDirection(recentAttempts);

            var avgProcessingTime = recentAttempts.Where(a => a.ProcessingDuration > 0).Any()
                ? recentAttempts.Where(a => a.ProcessingDuration > 0).Average(a => a.ProcessingDuration)
                : 0f;

            var mostUsedStrategy = recentAttempts.GroupBy(a => a.Strategy)
                .OrderByDescending(g => g.Count())
                .FirstOrDefault()?.Key ?? OptimizationStrategy.ConfigurationTuning;

            var strategyEffectiveness = CalculateStrategyEffectiveness(recentAttempts);

            return new OptimizationTrends
            {
                SystemName = history.SystemName,
                AnalysisTime = history.LastOptimizationTime,
                SampleSize = recentAttempts.Count,
                SuccessRate = successRate,
                TrendDirection = trendDirection,
                AverageProcessingTime = avgProcessingTime,
                MostUsedStrategy = mostUsedStrategy,
                StrategyEffectiveness = strategyEffectiveness
            };
        }

        private TrendDirection AnalyzeTrendDirection(List<OptimizationAttempt> attempts)
        {
            if (attempts.Count < 4) return TrendDirection.Stable;

            var halfPoint = attempts.Count / 2;
            var firstHalf = attempts.Take(halfPoint);
            var secondHalf = attempts.Skip(halfPoint);

            var firstHalfSuccess = firstHalf.Count(a => a.Success) / (float)firstHalf.Count();
            var secondHalfSuccess = secondHalf.Count(a => a.Success) / (float)secondHalf.Count();

            var difference = secondHalfSuccess - firstHalfSuccess;

            if (difference > 0.1f) return TrendDirection.Improving;
            if (difference < -0.1f) return TrendDirection.Declining;
            return TrendDirection.Stable;
        }

        private Dictionary<OptimizationStrategy, float> CalculateStrategyEffectiveness(List<OptimizationAttempt> attempts)
        {
            var effectiveness = new Dictionary<OptimizationStrategy, float>();

            var strategyGroups = attempts.GroupBy(a => a.Strategy);

            foreach (var group in strategyGroups)
            {
                var total = group.Count();
                var successful = group.Count(a => a.Success);
                effectiveness[group.Key] = total > 0 ? (float)successful / total : 0f;
            }

            return effectiveness;
        }

        private void MaintainHistorySize(OptimizationHistory history)
        {
            while (history.OptimizationAttempts.Count > _maxHistoryPerSystem)
                history.OptimizationAttempts.RemoveAt(0);
        }

        private void CleanupOldHistory(float currentTime)
        {
            var cutoffTime = currentTime - (_historyRetentionDays * 24 * 3600);
            var systemsToRemove = new List<string>();

            foreach (var kvp in _systemHistories)
            {
                var history = kvp.Value;

                history.OptimizationAttempts.RemoveAll(a => a.AttemptTime < cutoffTime);

                if (history.OptimizationAttempts.Count == 0)
                {
                    systemsToRemove.Add(kvp.Key);
                }
                else
                {
                    history.TotalAttempts = history.OptimizationAttempts.Count;
                    history.SuccessfulAttempts = history.OptimizationAttempts.Count(a => a.Success);
                }
            }

            foreach (var systemName in systemsToRemove)
            {
                _systemHistories.Remove(systemName);
                _systemTrends.Remove(systemName);
            }

            if (systemsToRemove.Count > 0 && _enableLogging)
                ChimeraLogger.Log("FOUNDATION", $"Cleaned up history for {systemsToRemove.Count} systems");
        }

        private void ResetStats()
        {
            _stats = new OptimizationHistoryStats
            {
                TotalOptimizationsRecorded = 0,
                TotalSuccessfulOptimizations = 0,
                TrendAnalysesPerformed = 0,
                LastRecordingTime = 0f,
                LastAnalysisTime = 0f
            };
        }

        #endregion
    }

    public enum TrendDirection
    {
        Improving,
        Stable,
        Declining
    }

    [System.Serializable]
    public class OptimizationHistory
    {
        public string SystemName;
        public float FirstOptimizationTime;
        public float LastOptimizationTime;
        public int TotalAttempts;
        public int SuccessfulAttempts;
        public List<OptimizationAttempt> OptimizationAttempts;
    }

    [System.Serializable]
    public class OptimizationTrends
    {
        public string SystemName;
        public float AnalysisTime;
        public int SampleSize;
        public float SuccessRate;
        public TrendDirection TrendDirection;
        public float AverageProcessingTime;
        public OptimizationStrategy MostUsedStrategy;
        public Dictionary<OptimizationStrategy, float> StrategyEffectiveness;
    }

    [System.Serializable]
    public struct OptimizationHistoryStats
    {
        public int TotalOptimizationsRecorded;
        public int TotalSuccessfulOptimizations;
        public int TrendAnalysesPerformed;
        public float LastRecordingTime;
        public float LastAnalysisTime;
    }

    [System.Serializable]
    public class OptimizationHistoryReport
    {
        public float GenerationTime;
        public int TrackedSystems;
        public int TotalOptimizations;
        public float OverallSuccessRate;
        public Dictionary<string, SystemOptimizationReport> SystemReports;
    }

    [System.Serializable]
    public class SystemOptimizationReport
    {
        public string SystemName;
        public int TotalAttempts;
        public int SuccessfulAttempts;
        public float SuccessRate;
        public OptimizationStrategy MostEffectiveStrategy;
        public OptimizationTrends Trends;
        public List<OptimizationAttempt> RecentAttempts;
    }
}
