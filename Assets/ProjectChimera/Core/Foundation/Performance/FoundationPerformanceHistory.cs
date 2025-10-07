using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using System.Linq;

namespace ProjectChimera.Core.Foundation.Performance
{
    /// <summary>
    /// REFACTORED: Foundation Performance History - Focused performance history tracking and retention
    /// Handles performance data historical tracking, snapshot creation, and trend analysis over time
    /// Single Responsibility: Performance history tracking and retention
    /// </summary>
    public class FoundationPerformanceHistory : MonoBehaviour
    {
        [Header("History Settings")]
        [SerializeField] private bool _enableHistoryTracking = true;
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private float _snapshotInterval = 60f; // 1 minute intervals

        [Header("History Configuration")]
        [SerializeField] private int _maxSnapshotRetention = 120; // 2 hours of snapshots
        [SerializeField] private int _maxSystemHistoryEntries = 50; // Per system
        [SerializeField] private bool _enableDataCompression = true;
        [SerializeField] private bool _enableTrendCalculation = true;

        // System references
        private FoundationPerformanceMetrics _performanceMetrics;
        private FoundationPerformanceAnalyzer _performanceAnalyzer;

        // History tracking
        private readonly List<PerformanceSnapshot> _performanceHistory = new List<PerformanceSnapshot>();
        private readonly Dictionary<string, List<SystemPerformanceHistoryEntry>> _systemHistory = new Dictionary<string, List<SystemPerformanceHistoryEntry>>();
        private float _lastSnapshotTime;

        // Properties
        public bool IsEnabled { get; private set; } = true;

        // Events
        public System.Action<PerformanceSnapshot> OnSnapshotCreated;
        public System.Action<string, SystemPerformanceHistoryEntry> OnSystemHistoryUpdated;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            InitializeSystemReferences();

            if (_enableLogging)
                ChimeraLogger.Log("FOUNDATION", "📈 FoundationPerformanceHistory initialized", this);
        }

        /// <summary>
        /// Initialize system references using dependency injection
        /// </summary>
        private void InitializeSystemReferences()
        {
            // Use SafeResolve helper for clean dependency injection with fallback
            _performanceMetrics = DependencyResolutionHelper.SafeResolve<FoundationPerformanceMetrics>(this, "FOUNDATION");
            _performanceAnalyzer = DependencyResolutionHelper.SafeResolve<FoundationPerformanceAnalyzer>(this, "FOUNDATION");

            if (_performanceMetrics == null)
            {
                ChimeraLogger.LogWarning("FOUNDATION", "FoundationPerformanceMetrics not found - performance tracking may be limited", this);
            }

            if (_performanceAnalyzer == null)
            {
                ChimeraLogger.LogWarning("FOUNDATION", "FoundationPerformanceAnalyzer not found - analysis features may be limited", this);
            }
        }

        /// <summary>
        /// Record performance snapshot
        /// </summary>
        public void RecordPerformanceSnapshot()
        {
            if (!IsEnabled || !_enableHistoryTracking) return;

            if (Time.time - _lastSnapshotTime < _snapshotInterval) return;

            CreatePerformanceSnapshot();
            _lastSnapshotTime = Time.time;
        }

        /// <summary>
        /// Record system performance update
        /// </summary>
        public void RecordSystemPerformanceUpdate(string systemName, float performanceScore)
        {
            if (!IsEnabled || !_enableHistoryTracking) return;

            RecordSystemHistoryEntry(systemName, performanceScore);
        }

        /// <summary>
        /// Get performance history
        /// </summary>
        public PerformanceSnapshot[] GetPerformanceHistory()
        {
            return _performanceHistory.ToArray();
        }

        /// <summary>
        /// Get performance history within time range
        /// </summary>
        public PerformanceSnapshot[] GetPerformanceHistory(float startTime, float endTime)
        {
            return _performanceHistory.Where(s => s.Timestamp >= startTime && s.Timestamp <= endTime).ToArray();
        }

        /// <summary>
        /// Get system performance history
        /// </summary>
        public SystemPerformanceHistoryEntry[] GetSystemHistory(string systemName)
        {
            if (_systemHistory.TryGetValue(systemName, out var history))
                return history.ToArray();

            return new SystemPerformanceHistoryEntry[0];
        }

        /// <summary>
        /// Get system performance history within time range
        /// </summary>
        public SystemPerformanceHistoryEntry[] GetSystemHistory(string systemName, float startTime, float endTime)
        {
            if (_systemHistory.TryGetValue(systemName, out var history))
            {
                return history.Where(e => e.Timestamp >= startTime && e.Timestamp <= endTime).ToArray();
            }

            return new SystemPerformanceHistoryEntry[0];
        }

        /// <summary>
        /// Get all system names with history
        /// </summary>
        public string[] GetSystemsWithHistory()
        {
            return _systemHistory.Keys.ToArray();
        }

        /// <summary>
        /// Get performance trend for system over time period
        /// </summary>
        public PerformanceTrend GetSystemTrend(string systemName, float timePeriod)
        {
            if (!_enableTrendCalculation) return PerformanceTrend.Stable;

            var cutoffTime = Time.time - timePeriod;
            var recentHistory = GetSystemHistory(systemName, cutoffTime, Time.time);

            if (recentHistory.Length < 3) return PerformanceTrend.Stable;

            var scores = recentHistory.Select(h => h.PerformanceScore).ToArray();
            return CalculateTrend(scores);
        }

        /// <summary>
        /// Get overall performance trend
        /// </summary>
        public PerformanceTrend GetOverallTrend(float timePeriod)
        {
            if (!_enableTrendCalculation) return PerformanceTrend.Stable;

            var cutoffTime = Time.time - timePeriod;
            var recentSnapshots = GetPerformanceHistory(cutoffTime, Time.time);

            if (recentSnapshots.Length < 3) return PerformanceTrend.Stable;

            var scores = recentSnapshots.Select(s => s.OverallPerformanceScore).ToArray();
            return CalculateTrend(scores);
        }

        /// <summary>
        /// Clear all history
        /// </summary>
        public void ClearHistory()
        {
            _performanceHistory.Clear();
            _systemHistory.Clear();
            _lastSnapshotTime = 0f;

            if (_enableLogging)
                ChimeraLogger.Log("FOUNDATION", "Performance history cleared", this);
        }

        /// <summary>
        /// Clear history older than specified time
        /// </summary>
        public void ClearHistoryOlderThan(float ageInSeconds)
        {
            var cutoffTime = Time.time - ageInSeconds;

            // Clear old performance snapshots
            _performanceHistory.RemoveAll(s => s.Timestamp < cutoffTime);

            // Clear old system history entries
            foreach (var systemName in _systemHistory.Keys.ToArray())
            {
                _systemHistory[systemName].RemoveAll(e => e.Timestamp < cutoffTime);
                if (_systemHistory[systemName].Count == 0)
                {
                    _systemHistory.Remove(systemName);
                }
            }

            if (_enableLogging)
                ChimeraLogger.Log("FOUNDATION", $"Performance history older than {ageInSeconds}s cleared", this);
        }

        /// <summary>
        /// Set system enabled/disabled
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;

            if (!enabled)
            {
                ClearHistory();
            }

            if (_enableLogging)
                ChimeraLogger.Log("FOUNDATION", $"FoundationPerformanceHistory: {(enabled ? "enabled" : "disabled")}", this);
        }

        #region Private Methods

        /// <summary>
        /// Create performance snapshot
        /// </summary>
        private void CreatePerformanceSnapshot()
        {
            var snapshot = new PerformanceSnapshot
            {
                Timestamp = Time.time,
                OverallPerformanceScore = _performanceMetrics?.GetOverallPerformanceScore() ?? 1.0f,
                SystemCount = _performanceMetrics?.GetAllSystemPerformance()?.Length ?? 0,
                SystemSnapshots = CreateSystemSnapshots()
            };

            // Add snapshot to history
            _performanceHistory.Add(snapshot);

            // Maintain history size
            if (_performanceHistory.Count > _maxSnapshotRetention)
            {
                _performanceHistory.RemoveAt(0);
            }

            OnSnapshotCreated?.Invoke(snapshot);

            if (_enableLogging)
                ChimeraLogger.Log("FOUNDATION", $"Performance snapshot created - Overall: {snapshot.OverallPerformanceScore:P2}, Systems: {snapshot.SystemCount}", this);
        }

        /// <summary>
        /// Create system snapshots
        /// </summary>
        private SystemPerformanceSnapshot[] CreateSystemSnapshots()
        {
            if (_performanceMetrics == null) return new SystemPerformanceSnapshot[0];

            var systemPerformanceData = _performanceMetrics.GetAllSystemPerformance();
            if (systemPerformanceData == null) return new SystemPerformanceSnapshot[0];

            var snapshots = new List<SystemPerformanceSnapshot>();

            foreach (var perfData in systemPerformanceData)
            {
                var snapshot = new SystemPerformanceSnapshot
                {
                    SystemName = perfData.SystemName,
                    PerformanceScore = perfData.PerformanceScore,
                    Trend = ProjectChimera.Core.Foundation.PerformanceTrend.Stable,
                    ConsecutivePoorPerformance = perfData.ConsecutivePoorPerformance,
                    Category = GetPerformanceCategory(perfData.PerformanceScore)
                };

                snapshots.Add(snapshot);
            }

            return snapshots.ToArray();
        }

        /// <summary>
        /// Record system history entry
        /// </summary>
        private void RecordSystemHistoryEntry(string systemName, float performanceScore)
        {
            if (!_systemHistory.ContainsKey(systemName))
            {
                _systemHistory[systemName] = new List<SystemPerformanceHistoryEntry>();
            }

            var entry = new SystemPerformanceHistoryEntry
            {
                Timestamp = Time.time,
                PerformanceScore = performanceScore,
                Category = GetPerformanceCategory(performanceScore)
            };

            _systemHistory[systemName].Add(entry);

            // Maintain history size per system
            if (_systemHistory[systemName].Count > _maxSystemHistoryEntries)
            {
                _systemHistory[systemName].RemoveAt(0);
            }

            OnSystemHistoryUpdated?.Invoke(systemName, entry);
        }

        /// <summary>
        /// Calculate trend from performance scores
        /// </summary>
        private ProjectChimera.Core.Foundation.Performance.PerformanceTrend CalculateTrend(float[] scores)
        {
            if (scores.Length < 3) return ProjectChimera.Core.Foundation.Performance.PerformanceTrend.Stable;

            // Simple linear regression to determine trend
            var slope = CalculateLinearSlope(scores);
            const float significantChangeThreshold = 0.05f;

            if (slope > significantChangeThreshold / scores.Length)
                return ProjectChimera.Core.Foundation.Performance.PerformanceTrend.Improving;
            else if (slope < -significantChangeThreshold / scores.Length)
                return ProjectChimera.Core.Foundation.Performance.PerformanceTrend.Declining;
            else
                return ProjectChimera.Core.Foundation.Performance.PerformanceTrend.Stable;
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
        /// Get performance category for score
        /// </summary>
        private PerformanceCategory GetPerformanceCategory(float score)
        {
            if (score >= 0.85f) return PerformanceCategory.Excellent;
            if (score >= 0.70f) return PerformanceCategory.Good;
            if (score >= 0.55f) return PerformanceCategory.Acceptable;
            return PerformanceCategory.Poor;
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Performance snapshot at a specific point in time
    /// </summary>
    [System.Serializable]
    public struct PerformanceSnapshot
    {
        public float Timestamp;
        public float OverallPerformanceScore;
        public int SystemCount;
        public SystemPerformanceSnapshot[] SystemSnapshots;
    }

    /// <summary>
    /// System performance snapshot
    /// </summary>
    [System.Serializable]
    public struct SystemPerformanceSnapshot
    {
        public string SystemName;
        public float PerformanceScore;
        public ProjectChimera.Core.Foundation.PerformanceTrend Trend;
        public int ConsecutivePoorPerformance;
        public PerformanceCategory Category;
    }

    /// <summary>
    /// System performance history entry
    /// </summary>
    [System.Serializable]
    public struct SystemPerformanceHistoryEntry
    {
        public float Timestamp;
        public float PerformanceScore;
        public PerformanceCategory Category;
    }

    #endregion
}
