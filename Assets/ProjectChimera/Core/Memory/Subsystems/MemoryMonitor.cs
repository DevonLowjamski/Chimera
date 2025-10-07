using UnityEngine;
using UnityEngine.Profiling;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;
using Logger = ProjectChimera.Core.Logging.ChimeraLogger;

namespace ProjectChimera.Core.Memory.Subsystems
{
    /// <summary>
    /// REFACTORED: Memory Monitor - Focused memory tracking and snapshot collection
    /// Handles memory usage monitoring, snapshot capture, and basic memory statistics
    /// Single Responsibility: Memory tracking and snapshot collection
    /// </summary>
    public class MemoryMonitor : MonoBehaviour
    {
        [Header("Memory Monitoring Settings")]
        [SerializeField] private bool _enableMonitoring = true;
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private float _snapshotInterval = 1f;
        [SerializeField] private int _historySize = 300; // 5 minutes at 1-second intervals

        [Header("Memory Categories")]
        [SerializeField] private bool _trackSystemMemory = true;
        [SerializeField] private bool _trackManagedMemory = true;
        [SerializeField] private bool _trackGraphicsMemory = true;
        [SerializeField] private bool _trackAudioMemory = true;

        // Memory tracking
        private readonly List<MemorySnapshot> _memoryHistory = new List<MemorySnapshot>();
        private readonly Dictionary<string, List<AllocationSnapshot>> _allocationHistory = new Dictionary<string, List<AllocationSnapshot>>();

        // Timing
        private float _lastSnapshotTime;

        // Statistics
        private MemoryMonitorStats _stats = new MemoryMonitorStats();

        // Properties
        public bool IsEnabled { get; private set; } = true;
        public int SnapshotCount => _memoryHistory.Count;
        public MemorySnapshot? LatestSnapshot => _memoryHistory.LastOrDefault();
        public MemoryMonitorStats GetStats() => _stats;

        // Events
        public System.Action<MemorySnapshot> OnMemorySnapshotCaptured;
        public System.Action<string, long> OnAllocationRecorded;
        public System.Action OnMemoryHistoryCleared;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (_enableLogging)
                Logger.Log("MEMORY", "📊 MemoryMonitor initialized", this);
        }

        /// <summary>
        /// Update memory monitoring (called from coordinator)
        /// </summary>
        public void UpdateMemoryMonitoring()
        {
            if (!IsEnabled || !_enableMonitoring) return;

            if (Time.time - _lastSnapshotTime >= _snapshotInterval)
            {
                CaptureMemorySnapshot();
            }
        }

        /// <summary>
        /// Capture memory snapshot
        /// </summary>
        public void CaptureMemorySnapshot()
        {
            if (!IsEnabled) return;

            var snapshot = CreateMemorySnapshot();
            AddSnapshotToHistory(snapshot);
            UpdateStatistics(snapshot);

            OnMemorySnapshotCaptured?.Invoke(snapshot);
            _lastSnapshotTime = Time.time;

            if (_enableLogging)
                Logger.Log("MEMORY", $"Memory snapshot captured: {snapshot.TotalMemoryMB:F2} MB", this);
        }

        /// <summary>
        /// Record memory allocation
        /// </summary>
        public void RecordAllocation(string category, long bytes)
        {
            if (!IsEnabled) return;

            if (!_allocationHistory.ContainsKey(category))
            {
                _allocationHistory[category] = new List<AllocationSnapshot>();
            }

            var allocation = new AllocationSnapshot
            {
                Category = category,
                Bytes = bytes,
                Timestamp = Time.time
            };

            _allocationHistory[category].Add(allocation);

            // Maintain history size
            if (_allocationHistory[category].Count > _historySize)
            {
                _allocationHistory[category].RemoveAt(0);
            }

            OnAllocationRecorded?.Invoke(category, bytes);
            _stats.TotalAllocations++;

            if (_enableLogging && bytes > 1024 * 1024) // Log allocations > 1MB
                Logger.Log("MEMORY", $"Large allocation recorded: {category} - {bytes / (1024 * 1024):F2} MB", this);
        }

        /// <summary>
        /// Get memory history within time range
        /// </summary>
        public MemorySnapshot[] GetMemoryHistory(float startTime, float endTime)
        {
            return _memoryHistory.Where(s => s.Timestamp >= startTime && s.Timestamp <= endTime).ToArray();
        }

        /// <summary>
        /// Get memory history (all snapshots)
        /// </summary>
        public MemorySnapshot[] GetMemoryHistory()
        {
            return _memoryHistory.ToArray();
        }

        /// <summary>
        /// Get allocation history for category
        /// </summary>
        public AllocationSnapshot[] GetAllocationHistory(string category)
        {
            if (_allocationHistory.TryGetValue(category, out var allocations))
                return allocations.ToArray();

            return new AllocationSnapshot[0];
        }

        /// <summary>
        /// Get all allocation categories
        /// </summary>
        public string[] GetAllocationCategories()
        {
            return _allocationHistory.Keys.ToArray();
        }

        /// <summary>
        /// Get current memory usage
        /// </summary>
        public long GetCurrentMemoryUsage()
        {
            return System.GC.GetTotalMemory(false);
        }

        /// <summary>
        /// Get peak memory usage from history
        /// </summary>
        public long GetPeakMemoryUsage()
        {
            if (_memoryHistory.Count == 0) return 0;
            return _memoryHistory.Max(s => s.TotalMemoryBytes);
        }

        /// <summary>
        /// Get average memory usage from history
        /// </summary>
        public float GetAverageMemoryUsage()
        {
            if (_memoryHistory.Count == 0) return 0;
            return (float)_memoryHistory.Average(s => (double)s.TotalMemoryBytes);
        }

        /// <summary>
        /// Clear memory history
        /// </summary>
        public void ClearHistory()
        {
            _memoryHistory.Clear();
            _allocationHistory.Clear();
            _stats = new MemoryMonitorStats();

            OnMemoryHistoryCleared?.Invoke();

            if (_enableLogging)
                Logger.Log("MEMORY", "Memory history cleared", this);
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
                Logger.Log("MEMORY", $"MemoryMonitor: {(enabled ? "enabled" : "disabled")}", this);
        }

        #region Private Methods

        /// <summary>
        /// Create memory snapshot
        /// </summary>
        private MemorySnapshot CreateMemorySnapshot()
        {
            var snapshot = new MemorySnapshot
            {
                Timestamp = Time.time,
                TotalMemoryBytes = System.GC.GetTotalMemory(false),
                SystemMemoryBytes = _trackSystemMemory ? GetSystemMemoryUsage() : 0,
                ManagedMemoryBytes = _trackManagedMemory ? GetManagedMemoryUsage() : 0,
                GraphicsMemoryBytes = _trackGraphicsMemory ? GetGraphicsMemoryUsage() : 0,
                AudioMemoryBytes = _trackAudioMemory ? GetAudioMemoryUsage() : 0
            };

            // Calculate derived values
            snapshot.TotalMemoryMB = snapshot.TotalMemoryBytes / (1024f * 1024f);

            return snapshot;
        }

        /// <summary>
        /// Add snapshot to history with size management
        /// </summary>
        private void AddSnapshotToHistory(MemorySnapshot snapshot)
        {
            _memoryHistory.Add(snapshot);

            // Maintain history size
            while (_memoryHistory.Count > _historySize)
            {
                _memoryHistory.RemoveAt(0);
            }
        }

        /// <summary>
        /// Update monitoring statistics
        /// </summary>
        private void UpdateStatistics(MemorySnapshot snapshot)
        {
            _stats.SnapshotsCaptured++;
            _stats.CurrentMemoryMB = snapshot.TotalMemoryMB;
            _stats.PeakMemoryMB = System.Math.Max(_stats.PeakMemoryMB, snapshot.TotalMemoryMB);

            // Calculate memory trend
            if (_memoryHistory.Count >= 2)
            {
                var previousSnapshot = _memoryHistory[_memoryHistory.Count - 2];
                var memoryChange = snapshot.TotalMemoryBytes - previousSnapshot.TotalMemoryBytes;
                _stats.LastMemoryChangeMB = memoryChange / (1024f * 1024f);
            }
        }

        /// <summary>
        /// Get system memory usage
        /// </summary>
        private long GetSystemMemoryUsage()
        {
            try
            {
                return Profiler.GetTotalAllocatedMemoryLong();
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Get managed memory usage
        /// </summary>
        private long GetManagedMemoryUsage()
        {
            return System.GC.GetTotalMemory(false);
        }

        /// <summary>
        /// Get graphics memory usage
        /// </summary>
        private long GetGraphicsMemoryUsage()
        {
            try
            {
                return Profiler.GetAllocatedMemoryForGraphicsDriver();
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Get audio memory usage
        /// </summary>
        private long GetAudioMemoryUsage()
        {
            try
            {
                // Unity doesn't provide direct audio memory API, so we estimate
                return 0; // Placeholder for audio memory calculation
            }
            catch
            {
                return 0;
            }
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Memory snapshot at a specific point in time
    /// </summary>
    [System.Serializable]
    public struct MemorySnapshot
    {
        public float Timestamp;
        public long TotalMemoryBytes;
        public long SystemMemoryBytes;
        public long ManagedMemoryBytes;
        public long GraphicsMemoryBytes;
        public long AudioMemoryBytes;
        public float TotalMemoryMB;
    }

    /// <summary>
    /// Memory allocation snapshot
    /// </summary>
    [System.Serializable]
    public struct AllocationSnapshot
    {
        public string Category;
        public long Bytes;
        public float Timestamp;
    }

    /// <summary>
    /// Memory monitor statistics
    /// </summary>
    [System.Serializable]
    public struct MemoryMonitorStats
    {
        public int SnapshotsCaptured;
        public int TotalAllocations;
        public float CurrentMemoryMB;
        public float PeakMemoryMB;
        public float LastMemoryChangeMB;
    }

    #endregion
}
