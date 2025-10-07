using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core.Memory.GCOptimization
{
    /// <summary>
    /// REFACTORED: GC Memory Pressure Monitor - Focused memory pressure detection and allocation tracking
    /// Handles memory pressure calculation, allocation rate monitoring, and threshold detection
    /// Single Responsibility: Memory pressure monitoring and allocation tracking
    /// </summary>
    public class GCMemoryPressureMonitor : MonoBehaviour
    {
        [Header("Memory Pressure Settings")]
        [SerializeField] private bool _enableMonitoring = true;
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private float _monitoringInterval = 1f;

        [Header("Pressure Calculation")]
        [SerializeField] private int _allocationHistorySize = 10;
        [SerializeField] private float _pressureAveragingWindow = 5f; // seconds

        // Memory tracking
        private long _lastAllocatedMemory;
        private float _lastMonitoringTime;
        private readonly Queue<AllocationSample> _allocationHistory = new Queue<AllocationSample>();
        private readonly Queue<MemoryPressureSample> _pressureHistory = new Queue<MemoryPressureSample>();

        // Statistics
        private MemoryPressureStats _stats = new MemoryPressureStats();

        // Properties
        public bool IsEnabled { get; private set; } = true;
        public float CurrentMemoryPressure => CalculateCurrentMemoryPressure();
        public long CurrentAllocationRate => CalculateCurrentAllocationRate();
        public MemoryPressureStats GetStats() => _stats;

        // Events
        public System.Action<float> OnMemoryPressureChanged;
        public System.Action<long> OnHighAllocationRate;
        public System.Action<MemoryPressureLevel> OnPressureLevelChanged;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            _lastAllocatedMemory = System.GC.GetTotalMemory(false);
            _lastMonitoringTime = Time.realtimeSinceStartup;

            if (_enableLogging)
                ChimeraLogger.Log("GC", "🔍 GCMemoryPressureMonitor initialized", this);
        }

        /// <summary>
        /// Update memory pressure monitoring (called from coordinator)
        /// </summary>
        public void UpdateMonitoring()
        {
            if (!IsEnabled || !_enableMonitoring) return;

            if (Time.realtimeSinceStartup - _lastMonitoringTime >= _monitoringInterval)
            {
                UpdateMemoryPressure();
                UpdateAllocationTracking();
                UpdateStatistics();
                CheckPressureLevels();

                _lastMonitoringTime = Time.realtimeSinceStartup;
            }
        }

        /// <summary>
        /// Force memory pressure update
        /// </summary>
        public void ForceUpdate()
        {
            if (!IsEnabled) return;

            UpdateMemoryPressure();
            UpdateAllocationTracking();
            UpdateStatistics();

            if (_enableLogging)
                ChimeraLogger.Log("GC", "Forced memory pressure update", this);
        }

        /// <summary>
        /// Get detailed memory pressure information
        /// </summary>
        public MemoryPressureInfo GetPressureInfo()
        {
            return new MemoryPressureInfo
            {
                CurrentPressure = CurrentMemoryPressure,
                AllocationRate = CurrentAllocationRate,
                PressureLevel = GetMemoryPressureLevel(),
                TotalMemoryMB = System.GC.GetTotalMemory(false) / (1024f * 1024f),
                SystemMemoryMB = SystemInfo.systemMemorySize,
                AveragePressure = CalculateAverageMemoryPressure(),
                PeakPressure = GetPeakMemoryPressure()
            };
        }

        /// <summary>
        /// Get memory pressure level
        /// </summary>
        public MemoryPressureLevel GetMemoryPressureLevel()
        {
            var pressure = CurrentMemoryPressure;

            if (pressure >= 0.9f)
                return MemoryPressureLevel.Critical;
            else if (pressure >= 0.7f)
                return MemoryPressureLevel.High;
            else if (pressure >= 0.5f)
                return MemoryPressureLevel.Medium;
            else
                return MemoryPressureLevel.Low;
        }

        /// <summary>
        /// Get allocation history
        /// </summary>
        public AllocationSample[] GetAllocationHistory()
        {
            return _allocationHistory.ToArray();
        }

        /// <summary>
        /// Get pressure history
        /// </summary>
        public MemoryPressureSample[] GetPressureHistory()
        {
            return _pressureHistory.ToArray();
        }

        /// <summary>
        /// Clear monitoring history
        /// </summary>
        public void ClearHistory()
        {
            _allocationHistory.Clear();
            _pressureHistory.Clear();
            _stats = new MemoryPressureStats();

            if (_enableLogging)
                ChimeraLogger.Log("GC", "Memory pressure history cleared", this);
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
                ChimeraLogger.Log("GC", $"GCMemoryPressureMonitor: {(enabled ? "enabled" : "disabled")}", this);
        }

        #region Private Methods

        /// <summary>
        /// Update memory pressure calculation
        /// </summary>
        private void UpdateMemoryPressure()
        {
            var currentPressure = CalculateCurrentMemoryPressure();
            var currentTime = Time.realtimeSinceStartup;

            var pressureSample = new MemoryPressureSample
            {
                Timestamp = currentTime,
                Pressure = currentPressure,
                TotalMemoryBytes = System.GC.GetTotalMemory(false)
            };

            _pressureHistory.Enqueue(pressureSample);

            // Maintain history size
            while (_pressureHistory.Count > 0 &&
                   currentTime - _pressureHistory.Peek().Timestamp > _pressureAveragingWindow)
            {
                _pressureHistory.Dequeue();
            }

            OnMemoryPressureChanged?.Invoke(currentPressure);
        }

        /// <summary>
        /// Update allocation tracking
        /// </summary>
        private void UpdateAllocationTracking()
        {
            var currentMemory = System.GC.GetTotalMemory(false);
            var currentTime = Time.realtimeSinceStartup;
            var timeDelta = currentTime - _lastMonitoringTime;

            if (timeDelta > 0 && _lastAllocatedMemory > 0)
            {
                var memoryDelta = currentMemory - _lastAllocatedMemory;
                var allocationRate = (long)(memoryDelta / timeDelta);

                var allocationSample = new AllocationSample
                {
                    Timestamp = currentTime,
                    AllocationRate = allocationRate,
                    MemoryDelta = memoryDelta,
                    TotalMemory = currentMemory
                };

                _allocationHistory.Enqueue(allocationSample);

                // Maintain history size
                if (_allocationHistory.Count > _allocationHistorySize)
                {
                    _allocationHistory.Dequeue();
                }

                // Check for high allocation rate
                if (allocationRate > 25 * 1024 * 1024) // > 25MB/s
                {
                    OnHighAllocationRate?.Invoke(allocationRate);
                }
            }

            _lastAllocatedMemory = currentMemory;
        }

        /// <summary>
        /// Calculate current memory pressure
        /// </summary>
        private float CalculateCurrentMemoryPressure()
        {
            long totalMemory = System.GC.GetTotalMemory(false);
            long systemMemory = SystemInfo.systemMemorySize * 1024L * 1024L;
            return Mathf.Clamp01((float)totalMemory / systemMemory);
        }

        /// <summary>
        /// Calculate current allocation rate
        /// </summary>
        private long CalculateCurrentAllocationRate()
        {
            if (_allocationHistory.Count < 2) return 0;

            var recentSamples = _allocationHistory.TakeLast(3).ToArray();
            var averageRate = recentSamples.Average(s => s.AllocationRate);
            return (long)averageRate;
        }

        /// <summary>
        /// Calculate average memory pressure over time window
        /// </summary>
        private float CalculateAverageMemoryPressure()
        {
            if (_pressureHistory.Count == 0) return 0f;
            return _pressureHistory.Average(p => p.Pressure);
        }

        /// <summary>
        /// Get peak memory pressure
        /// </summary>
        private float GetPeakMemoryPressure()
        {
            if (_pressureHistory.Count == 0) return 0f;
            return _pressureHistory.Max(p => p.Pressure);
        }

        /// <summary>
        /// Update monitoring statistics
        /// </summary>
        private void UpdateStatistics()
        {
            _stats.CurrentPressure = CurrentMemoryPressure;
            _stats.CurrentAllocationRate = CurrentAllocationRate;
            _stats.AveragePressure = CalculateAverageMemoryPressure();
            _stats.PeakPressure = GetPeakMemoryPressure();
            _stats.SampleCount = _pressureHistory.Count;
            _stats.AllocationSampleCount = _allocationHistory.Count;
        }

        /// <summary>
        /// Check pressure levels and fire events
        /// </summary>
        private void CheckPressureLevels()
        {
            var currentLevel = GetMemoryPressureLevel();
            var previousLevel = _stats.LastPressureLevel;

            if (currentLevel != previousLevel)
            {
                OnPressureLevelChanged?.Invoke(currentLevel);
                _stats.LastPressureLevel = currentLevel;

                if (_enableLogging)
                    ChimeraLogger.Log("GC", $"Memory pressure level changed: {previousLevel} -> {currentLevel}", this);
            }
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Memory pressure levels
    /// </summary>
    public enum MemoryPressureLevel
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// Allocation sample data
    /// </summary>
    [System.Serializable]
    public struct AllocationSample
    {
        public float Timestamp;
        public long AllocationRate; // bytes per second
        public long MemoryDelta;
        public long TotalMemory;
    }

    /// <summary>
    /// Memory pressure sample data
    /// </summary>
    [System.Serializable]
    public struct MemoryPressureSample
    {
        public float Timestamp;
        public float Pressure; // 0.0 to 1.0
        public long TotalMemoryBytes;
    }

    /// <summary>
    /// Memory pressure information
    /// </summary>
    [System.Serializable]
    public struct MemoryPressureInfo
    {
        public float CurrentPressure;
        public long AllocationRate;
        public MemoryPressureLevel PressureLevel;
        public float TotalMemoryMB;
        public int SystemMemoryMB;
        public float AveragePressure;
        public float PeakPressure;
    }

    /// <summary>
    /// Memory pressure statistics
    /// </summary>
    [System.Serializable]
    public struct MemoryPressureStats
    {
        public float CurrentPressure;
        public long CurrentAllocationRate;
        public float AveragePressure;
        public float PeakPressure;
        public int SampleCount;
        public int AllocationSampleCount;
        public MemoryPressureLevel LastPressureLevel;
    }

    #endregion
}