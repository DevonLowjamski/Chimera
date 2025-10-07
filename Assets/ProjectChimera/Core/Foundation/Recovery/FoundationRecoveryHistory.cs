using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using System.Linq;

namespace ProjectChimera.Core.Foundation.Recovery
{
    /// <summary>
    /// REFACTORED: Foundation Recovery History - Focused recovery history tracking and analysis
    /// Handles recovery attempt recording, history analysis, and pattern detection
    /// Single Responsibility: Recovery history tracking and analysis
    /// </summary>
    public class FoundationRecoveryHistory : MonoBehaviour
    {
        [Header("History Settings")]
        [SerializeField] private bool _enableHistoryTracking = true;
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private int _maxHistoryPerSystem = 20;
        [SerializeField] private float _historyRetentionDays = 7f;

        [Header("Analysis Settings")]
        [SerializeField] private bool _enablePatternAnalysis = true;
        [SerializeField] private int _minAttemptsForPattern = 5;
        [SerializeField] private float _patternThreshold = 0.7f; // 70% similarity for pattern detection

        // History tracking
        private readonly Dictionary<string, List<RecoveryAttempt>> _recoveryHistory = new Dictionary<string, List<RecoveryAttempt>>();
        private readonly Dictionary<string, RecoveryPattern> _recoveryPatterns = new Dictionary<string, RecoveryPattern>();

        // Statistics
        private RecoveryHistoryStats _stats = new RecoveryHistoryStats();

        // Properties
        public bool IsEnabled { get; private set; } = true;
        public RecoveryHistoryStats GetStats() => _stats;

        // Events
        public System.Action<string, RecoveryAttempt> OnRecoveryRecorded;
        public System.Action<string, RecoveryPattern> OnPatternDetected;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            _stats = new RecoveryHistoryStats();

            if (_enableLogging)
                ChimeraLogger.Log("FOUNDATION", "📚 FoundationRecoveryHistory initialized", this);
        }

        /// <summary>
        /// Record recovery start
        /// </summary>
        public void RecordRecoveryStart(string systemName, RecoveryStrategy strategy)
        {
            if (!IsEnabled || !_enableHistoryTracking) return;

            // This will be completed when recovery finishes
            // Just update internal tracking if needed
        }

        /// <summary>
        /// Record recovery completion
        /// </summary>
        public void RecordRecoveryCompletion(string systemName, RecoveryStrategy strategy, bool success, float duration, string errorMessage)
        {
            if (!IsEnabled || !_enableHistoryTracking) return;

            var attempt = new RecoveryAttempt
            {
                SystemName = systemName,
                Strategy = strategy,
                AttemptTime = Time.time,
                Success = success,
                Duration = duration,
                ErrorMessage = errorMessage
            };

            RecordRecoveryAttempt(attempt);
        }

        /// <summary>
        /// Get recovery history for specific system
        /// </summary>
        public RecoveryAttempt[] GetRecoveryHistory(string systemName)
        {
            if (_recoveryHistory.TryGetValue(systemName, out var history))
            {
                return history.ToArray();
            }
            return new RecoveryAttempt[0];
        }

        /// <summary>
        /// Get recovery history for all systems
        /// </summary>
        public Dictionary<string, RecoveryAttempt[]> GetAllRecoveryHistory()
        {
            var result = new Dictionary<string, RecoveryAttempt[]>();
            foreach (var kvp in _recoveryHistory)
            {
                result[kvp.Key] = kvp.Value.ToArray();
            }
            return result;
        }

        /// <summary>
        /// Get recovery pattern for specific system
        /// </summary>
        public RecoveryPattern GetRecoveryPattern(string systemName)
        {
            _recoveryPatterns.TryGetValue(systemName, out var pattern);
            return pattern;
        }

        /// <summary>
        /// Get success rate for specific system and strategy
        /// </summary>
        public float GetSuccessRate(string systemName, RecoveryStrategy? strategy = null)
        {
            if (!_recoveryHistory.TryGetValue(systemName, out var history))
                return 0f;

            var relevantAttempts = strategy.HasValue
                ? history.Where(a => a.Strategy == strategy.Value).ToArray()
                : history.ToArray();

            if (relevantAttempts.Length == 0) return 0f;

            int successCount = relevantAttempts.Count(a => a.Success);
            return (float)successCount / relevantAttempts.Length;
        }

        /// <summary>
        /// Get most successful strategy for system
        /// </summary>
        public RecoveryStrategy GetMostSuccessfulStrategy(string systemName)
        {
            if (!_recoveryHistory.TryGetValue(systemName, out var history))
                return RecoveryStrategy.Restart; // Default fallback

            var strategyStats = history
                .GroupBy(a => a.Strategy)
                .Select(g => new
                {
                    Strategy = g.Key,
                    SuccessRate = (float)g.Count(a => a.Success) / g.Count(),
                    AttemptCount = g.Count()
                })
                .Where(s => s.AttemptCount >= 2) // Require at least 2 attempts
                .OrderByDescending(s => s.SuccessRate)
                .ThenByDescending(s => s.AttemptCount)
                .FirstOrDefault();

            return strategyStats?.Strategy ?? RecoveryStrategy.Restart;
        }

        /// <summary>
        /// Check if strategy has been tried recently
        /// </summary>
        public bool HasTriedStrategyRecently(string systemName, RecoveryStrategy strategy, float withinSeconds = 300f)
        {
            if (!_recoveryHistory.TryGetValue(systemName, out var history))
                return false;

            float cutoffTime = Time.time - withinSeconds;
            return history.Any(a => a.Strategy == strategy && a.AttemptTime >= cutoffTime);
        }

        /// <summary>
        /// Clean old history entries
        /// </summary>
        public void CleanOldHistory()
        {
            float cutoffTime = Time.time - (_historyRetentionDays * 24f * 3600f);

            foreach (var systemName in _recoveryHistory.Keys.ToArray())
            {
                var history = _recoveryHistory[systemName];
                var filteredHistory = history.Where(a => a.AttemptTime >= cutoffTime).ToList();

                if (filteredHistory.Count == 0)
                {
                    _recoveryHistory.Remove(systemName);
                    _recoveryPatterns.Remove(systemName);
                }
                else
                {
                    _recoveryHistory[systemName] = filteredHistory;
                }
            }

            UpdateStatistics();

            if (_enableLogging)
                ChimeraLogger.Log("FOUNDATION", "Cleaned old recovery history entries", this);
        }

        /// <summary>
        /// Clear all history
        /// </summary>
        public void ClearHistory()
        {
            _recoveryHistory.Clear();
            _recoveryPatterns.Clear();
            _stats = new RecoveryHistoryStats();

            if (_enableLogging)
                ChimeraLogger.Log("FOUNDATION", "Recovery history cleared", this);
        }

        /// <summary>
        /// Set system enabled/disabled
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;

            if (_enableLogging)
                ChimeraLogger.Log("FOUNDATION", $"FoundationRecoveryHistory: {(enabled ? "enabled" : "disabled")}", this);
        }

        #region Private Methods

        /// <summary>
        /// Record recovery attempt
        /// </summary>
        private void RecordRecoveryAttempt(RecoveryAttempt attempt)
        {
            if (!_recoveryHistory.ContainsKey(attempt.SystemName))
            {
                _recoveryHistory[attempt.SystemName] = new List<RecoveryAttempt>();
            }

            _recoveryHistory[attempt.SystemName].Add(attempt);

            // Maintain history size limit
            if (_recoveryHistory[attempt.SystemName].Count > _maxHistoryPerSystem)
            {
                _recoveryHistory[attempt.SystemName].RemoveAt(0);
            }

            OnRecoveryRecorded?.Invoke(attempt.SystemName, attempt);

            // Update statistics
            UpdateStatistics();

            // Analyze patterns if enabled
            if (_enablePatternAnalysis)
            {
                AnalyzeRecoveryPatterns(attempt.SystemName);
            }

            if (_enableLogging)
                ChimeraLogger.Log("FOUNDATION", $"Recorded recovery attempt for {attempt.SystemName}: {attempt.Strategy} - {(attempt.Success ? "Success" : "Failed")}", this);
        }

        /// <summary>
        /// Analyze recovery patterns for system
        /// </summary>
        private void AnalyzeRecoveryPatterns(string systemName)
        {
            if (!_recoveryHistory.TryGetValue(systemName, out var history))
                return;

            if (history.Count < _minAttemptsForPattern)
                return;

            // Simple pattern analysis - look for recurring failure patterns
            var recentAttempts = history.TakeLast(10).ToArray();
            if (TryAnalyzeFailurePattern(recentAttempts, out var failurePattern))
            {
                _recoveryPatterns[systemName] = failurePattern;
                OnPatternDetected?.Invoke(systemName, failurePattern);
            }
        }

        /// <summary>
        /// Analyze failure patterns in recovery attempts
        /// </summary>
        private bool TryAnalyzeFailurePattern(RecoveryAttempt[] attempts, out RecoveryPattern pattern)
        {
            pattern = default;
            if (attempts.Length < _minAttemptsForPattern)
                return false;

            // Calculate success rate
            float successRate = (float)attempts.Count(a => a.Success) / attempts.Length;

            // Analyze strategy effectiveness
            var strategyStats = attempts
                .GroupBy(a => a.Strategy)
                .ToDictionary(
                    g => g.Key,
                    g => (float)g.Count(a => a.Success) / g.Count()
                );

            // Detect time-based patterns
            bool hasTimePattern = DetectTimePattern(attempts);

            pattern = new RecoveryPattern
            {
                SystemName = attempts.FirstOrDefault().SystemName,
                SuccessRate = successRate,
                StrategyEffectiveness = strategyStats,
                HasTimePattern = hasTimePattern,
                PatternConfidence = CalculatePatternConfidence(attempts),
                LastAnalysisTime = Time.time
            };

            return pattern.PatternConfidence >= _patternThreshold;
        }

        /// <summary>
        /// Detect time-based recovery patterns
        /// </summary>
        private bool DetectTimePattern(RecoveryAttempt[] attempts)
        {
            if (attempts.Length < 5) return false;

            // Look for clustering of failures at similar times
            var failureTimes = attempts
                .Where(a => !a.Success)
                .Select(a => a.AttemptTime % 86400f) // Time of day in seconds
                .OrderBy(t => t)
                .ToArray();

            if (failureTimes.Length < 3) return false;

            // Simple clustering analysis - check if failures happen within similar time windows
            for (int i = 0; i < failureTimes.Length - 2; i++)
            {
                float window = 3600f; // 1 hour window
                int clusteredCount = failureTimes.Count(t => Mathf.Abs(t - failureTimes[i]) <= window);
                if (clusteredCount >= 3) return true;
            }

            return false;
        }

        /// <summary>
        /// Calculate pattern confidence
        /// </summary>
        private float CalculatePatternConfidence(RecoveryAttempt[] attempts)
        {
            if (attempts.Length < _minAttemptsForPattern)
                return 0f;

            // Simple confidence calculation based on attempt count and consistency
            float countFactor = Mathf.Min(1f, (float)attempts.Length / 20f);
            float consistencyFactor = CalculateConsistency(attempts);

            return (countFactor + consistencyFactor) * 0.5f;
        }

        /// <summary>
        /// Calculate consistency in recovery attempts
        /// </summary>
        private float CalculateConsistency(RecoveryAttempt[] attempts)
        {
            if (attempts.Length < 2) return 0f;

            // Look for consistency in outcomes for similar strategies
            var strategyGroups = attempts.GroupBy(a => a.Strategy);
            float totalConsistency = 0f;
            int groupCount = 0;

            foreach (var group in strategyGroups)
            {
                if (group.Count() < 2) continue;

                var outcomes = group.Select(a => a.Success).ToArray();
                float successRate = (float)outcomes.Count(s => s) / outcomes.Length;

                // Consistency is higher when success rate is closer to 0 or 1
                float consistency = Mathf.Max(successRate, 1f - successRate);
                totalConsistency += consistency;
                groupCount++;
            }

            return groupCount > 0 ? totalConsistency / groupCount : 0f;
        }

        /// <summary>
        /// Update recovery statistics
        /// </summary>
        private void UpdateStatistics()
        {
            _stats.TotalSystems = _recoveryHistory.Count;
            _stats.TotalAttempts = _recoveryHistory.Values.SelectMany(h => h).Count();
            _stats.SuccessfulAttempts = _recoveryHistory.Values.SelectMany(h => h).Count(a => a.Success);
            _stats.DetectedPatterns = _recoveryPatterns.Count;

            if (_stats.TotalAttempts > 0)
            {
                _stats.OverallSuccessRate = (float)_stats.SuccessfulAttempts / _stats.TotalAttempts;
            }
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Recovery pattern structure
    /// </summary>
    [System.Serializable]
    public struct RecoveryPattern
    {
        public string SystemName;
        public float SuccessRate;
        public Dictionary<RecoveryStrategy, float> StrategyEffectiveness;
        public bool HasTimePattern;
        public float PatternConfidence;
        public float LastAnalysisTime;
    }

    /// <summary>
    /// Recovery history statistics
    /// </summary>
    [System.Serializable]
    public struct RecoveryHistoryStats
    {
        public int TotalSystems;
        public int TotalAttempts;
        public int SuccessfulAttempts;
        public int DetectedPatterns;
        public float OverallSuccessRate;
    }

    #endregion
}
