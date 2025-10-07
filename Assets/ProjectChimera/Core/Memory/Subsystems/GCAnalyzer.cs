using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core.Memory.Subsystems
{
    /// <summary>
    /// REFACTORED: GC Analyzer - Focused garbage collection tracking and analysis
    /// Handles GC event detection, frequency analysis, and performance impact assessment
    /// Single Responsibility: Garbage collection analysis and tracking
    /// </summary>
    public class GCAnalyzer : MonoBehaviour
    {
        [Header("GC Analysis Settings")]
        [SerializeField] private bool _enableGCTracking = true;
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private float _gcCheckInterval = 0.1f; // Check every 100ms
        [SerializeField] private int _gcHistorySize = 100;

        [Header("Performance Thresholds")]
        [SerializeField] private float _gcFrequencyWarningThreshold = 5f; // 5 GCs per second
        [SerializeField] private float _gcFrequencyCriticalThreshold = 10f; // 10 GCs per second
        [SerializeField] private int _consecutiveGCWarningThreshold = 3;

        // GC tracking
        private readonly List<GCSnapshot> _gcHistory = new List<GCSnapshot>();
        private readonly Dictionary<int, int> _lastGCCounts = new Dictionary<int, int>();
        private readonly Queue<float> _recentGCTimes = new Queue<float>();

        // Timing
        private float _lastGCCheck;

        // Statistics
        private GCAnalyzerStats _stats = new GCAnalyzerStats();

        // Properties
        public bool IsEnabled { get; private set; } = true;
        public int GCEventCount => _gcHistory.Count;
        public float CurrentGCFrequency => CalculateCurrentGCFrequency();
        public GCAnalyzerStats GetStats() => _stats;

        // Events
        public System.Action<GCSnapshot> OnGCEventDetected;
        public System.Action<int, int> OnGCFrequencyWarning;
        public System.Action<GCPerformanceImpact> OnGCPerformanceImpact;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            InitializeGCCounters();

            if (_enableLogging)
                ChimeraLogger.Log("MEMORY", "🗑️ GCAnalyzer initialized", this);
        }

        /// <summary>
        /// Update GC analysis (called from coordinator)
        /// </summary>
        public void UpdateGCAnalysis()
        {
            if (!IsEnabled || !_enableGCTracking) return;

            if (Time.time - _lastGCCheck >= _gcCheckInterval)
            {
                CheckGCActivity();
                _lastGCCheck = Time.time;
            }
        }

        /// <summary>
        /// Force GC analysis update
        /// </summary>
        public void ForceGCAnalysis()
        {
            if (!IsEnabled) return;

            CheckGCActivity();

            if (_enableLogging)
                ChimeraLogger.Log("MEMORY", "Forced GC analysis update", this);
        }

        /// <summary>
        /// Get GC history within time range
        /// </summary>
        public GCSnapshot[] GetGCHistory(float startTime, float endTime)
        {
            return _gcHistory.Where(gc => gc.Timestamp >= startTime && gc.Timestamp <= endTime).ToArray();
        }

        /// <summary>
        /// Get GC history for specific generation
        /// </summary>
        public GCSnapshot[] GetGCHistoryForGeneration(int generation)
        {
            return _gcHistory.Where(gc => gc.Generation == generation).ToArray();
        }

        /// <summary>
        /// Get recent GC frequency (GCs per second)
        /// </summary>
        public float GetRecentGCFrequency(float timeWindow = 10f)
        {
            var cutoffTime = Time.time - timeWindow;
            var recentGCs = _gcHistory.Count(gc => gc.Timestamp >= cutoffTime);
            return recentGCs / timeWindow;
        }

        /// <summary>
        /// Get GC performance impact assessment
        /// </summary>
        public GCPerformanceImpact GetGCPerformanceImpact()
        {
            var recentGCFrequency = GetRecentGCFrequency(5f); // Last 5 seconds
            var consecutiveGCs = CountConsecutiveRecentGCs();

            if (recentGCFrequency >= _gcFrequencyCriticalThreshold)
                return GCPerformanceImpact.Critical;
            else if (recentGCFrequency >= _gcFrequencyWarningThreshold)
                return GCPerformanceImpact.High;
            else if (consecutiveGCs >= _consecutiveGCWarningThreshold)
                return GCPerformanceImpact.Medium;
            else
                return GCPerformanceImpact.Low;
        }

        /// <summary>
        /// Predict next GC based on allocation patterns
        /// </summary>
        public float PredictNextGCTime()
        {
            if (_gcHistory.Count < 3) return -1f;

            // Simple prediction based on average GC interval
            var recentGCs = _gcHistory.TakeLast(5).ToArray();
            if (recentGCs.Length < 2) return -1f;

            var intervals = new List<float>();
            for (int i = 1; i < recentGCs.Length; i++)
            {
                intervals.Add(recentGCs[i].Timestamp - recentGCs[i - 1].Timestamp);
            }

            var averageInterval = intervals.Average();
            return _gcHistory.Last().Timestamp + averageInterval;
        }

        /// <summary>
        /// Clear GC history
        /// </summary>
        public void ClearGCHistory()
        {
            _gcHistory.Clear();
            _recentGCTimes.Clear();
            _stats = new GCAnalyzerStats();

            if (_enableLogging)
                ChimeraLogger.Log("MEMORY", "GC history cleared", this);
        }

        /// <summary>
        /// Set system enabled/disabled
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;

            if (!enabled)
            {
                ClearGCHistory();
            }

            if (_enableLogging)
                ChimeraLogger.Log("MEMORY", $"GCAnalyzer: {(enabled ? "enabled" : "disabled")}", this);
        }

        #region Private Methods

        /// <summary>
        /// Initialize GC counters for all generations
        /// </summary>
        private void InitializeGCCounters()
        {
            for (int generation = 0; generation <= 2; generation++)
            {
                _lastGCCounts[generation] = System.GC.CollectionCount(generation);
            }
        }

        /// <summary>
        /// Check for GC activity
        /// </summary>
        private void CheckGCActivity()
        {
            var currentTime = Time.time;
            bool gcDetected = false;

            // Check each generation for new GC events
            for (int generation = 0; generation <= 2; generation++)
            {
                var currentCount = System.GC.CollectionCount(generation);
                var lastCount = _lastGCCounts[generation];

                if (currentCount > lastCount)
                {
                    var gcEvents = currentCount - lastCount;
                    for (int i = 0; i < gcEvents; i++)
                    {
                        RecordGCEvent(generation, currentTime);
                        gcDetected = true;
                    }

                    _lastGCCounts[generation] = currentCount;
                }
            }

            if (gcDetected)
            {
                UpdateGCStatistics();
                CheckGCPerformanceImpact(currentTime);
            }
        }

        /// <summary>
        /// Record a GC event
        /// </summary>
        private void RecordGCEvent(int generation, float timestamp)
        {
            var gcSnapshot = new GCSnapshot
            {
                Generation = generation,
                Timestamp = timestamp,
                MemoryBeforeGC = System.GC.GetTotalMemory(false),
                MemoryAfterGC = System.GC.GetTotalMemory(true)
            };

            _gcHistory.Add(gcSnapshot);
            _recentGCTimes.Enqueue(timestamp);

            // Maintain history size
            if (_gcHistory.Count > _gcHistorySize)
            {
                _gcHistory.RemoveAt(0);
            }

            // Maintain recent GC times (last 10 seconds)
            while (_recentGCTimes.Count > 0 && timestamp - _recentGCTimes.Peek() > 10f)
            {
                _recentGCTimes.Dequeue();
            }

            OnGCEventDetected?.Invoke(gcSnapshot);

            if (_enableLogging)
            {
                var memoryFreed = gcSnapshot.MemoryBeforeGC - gcSnapshot.MemoryAfterGC;
                ChimeraLogger.Log("MEMORY",
                    $"GC Gen{generation} detected - Memory freed: {memoryFreed / (1024 * 1024):F2} MB",
                    this);
            }
        }

        /// <summary>
        /// Update GC statistics
        /// </summary>
        private void UpdateGCStatistics()
        {
            _stats.TotalGCEvents = _gcHistory.Count;
            _stats.Gen0Collections = _gcHistory.Count(gc => gc.Generation == 0);
            _stats.Gen1Collections = _gcHistory.Count(gc => gc.Generation == 1);
            _stats.Gen2Collections = _gcHistory.Count(gc => gc.Generation == 2);
            _stats.CurrentGCFrequency = CalculateCurrentGCFrequency();

            if (_gcHistory.Count > 0)
            {
                var lastGC = _gcHistory.Last();
                _stats.LastGCGeneration = lastGC.Generation;
                _stats.LastGCTime = lastGC.Timestamp;
            }
        }

        /// <summary>
        /// Calculate current GC frequency
        /// </summary>
        private float CalculateCurrentGCFrequency()
        {
            if (_recentGCTimes.Count < 2) return 0f;

            var timeSpan = Time.time - _recentGCTimes.Peek();
            return timeSpan > 0 ? _recentGCTimes.Count / timeSpan : 0f;
        }

        /// <summary>
        /// Check GC performance impact
        /// </summary>
        private void CheckGCPerformanceImpact(float currentTime)
        {
            var recentFrequency = GetRecentGCFrequency(1f); // Last 1 second

            // Check for high frequency GC
            if (recentFrequency >= _gcFrequencyWarningThreshold)
            {
                OnGCFrequencyWarning?.Invoke((int)recentFrequency, _gcHistory.Count);

                if (_enableLogging)
                    ChimeraLogger.LogWarning("MEMORY", $"High GC frequency detected: {recentFrequency:F2} GCs/sec", this);
            }

            // Assess overall performance impact
            var impact = GetGCPerformanceImpact();
            if (impact >= GCPerformanceImpact.Medium)
            {
                OnGCPerformanceImpact?.Invoke(impact);
            }
        }

        /// <summary>
        /// Count consecutive recent GCs (within last 5 seconds)
        /// </summary>
        private int CountConsecutiveRecentGCs()
        {
            var cutoffTime = Time.time - 5f;
            var recentGCs = _gcHistory.Where(gc => gc.Timestamp >= cutoffTime).OrderBy(gc => gc.Timestamp).ToArray();

            if (recentGCs.Length < 2) return recentGCs.Length;

            int consecutiveCount = 1;
            for (int i = 1; i < recentGCs.Length; i++)
            {
                var timeDiff = recentGCs[i].Timestamp - recentGCs[i - 1].Timestamp;
                if (timeDiff < 1f) // Less than 1 second apart
                {
                    consecutiveCount++;
                }
                else
                {
                    consecutiveCount = 1;
                }
            }

            return consecutiveCount;
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Garbage collection snapshot
    /// </summary>
    [System.Serializable]
    public struct GCSnapshot
    {
        public int Generation;
        public float Timestamp;
        public long MemoryBeforeGC;
        public long MemoryAfterGC;
    }

    /// <summary>
    /// GC performance impact levels
    /// </summary>
    public enum GCPerformanceImpact
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// GC analyzer statistics
    /// </summary>
    [System.Serializable]
    public struct GCAnalyzerStats
    {
        public int TotalGCEvents;
        public int Gen0Collections;
        public int Gen1Collections;
        public int Gen2Collections;
        public float CurrentGCFrequency;
        public int LastGCGeneration;
        public float LastGCTime;
    }

    #endregion
}