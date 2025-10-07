using UnityEngine;
using UnityEngine.Profiling;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core;
using ProjectChimera.Core.Updates;
using ProjectChimera.Core.Memory.Subsystems;

namespace ProjectChimera.Core.Memory
{
    /// <summary>
    /// REFACTORED: Memory Profiler - Legacy wrapper for backward compatibility
    /// Delegates to specialized MemoryCore for all memory profiling and monitoring
    /// Single Responsibility: Backward compatibility delegation
    /// </summary>
    public class MemoryProfiler : MonoBehaviour, ITickable
    {
        [Header("Legacy Compatibility Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private bool _enableLegacyMode = true;

        // Delegation target - the actual memory management system
        private MemoryCore _memoryCore;

        // Legacy properties for backward compatibility
        public bool IsInitialized => _memoryCore != null;
        public MemorySystemHealth SystemHealth => _memoryCore?.SystemHealth ?? MemorySystemHealth.Healthy;

        // Legacy events for backward compatibility
        public System.Action<MemorySnapshot> OnMemorySnapshotCaptured;
        public System.Action<GCSnapshot> OnGCEventDetected;
        public System.Action<MemoryAlert> OnMemoryAlert;

        // Legacy singleton pattern for backward compatibility
        private static MemoryProfiler _instance;
        public static MemoryProfiler Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = ServiceContainerFactory.Instance?.TryResolve<MemoryProfiler>();
                    if (_instance == null)
                    {
                        var go = new GameObject("MemoryProfiler");
                        _instance = go.AddComponent<MemoryProfiler>();
                        DontDestroyOnLoad(go);
                    }
                }
                return _instance;
            }
        }

        public int TickPriority => 80;
        public bool IsTickable => enabled && gameObject.activeInHierarchy;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeMemoryCore();
                ProjectChimera.Core.Updates.UpdateOrchestrator.Instance?.RegisterTickable(this);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Initialize the memory core system
        /// </summary>
        private void InitializeMemoryCore()
        {
            _memoryCore = GetComponent<MemoryCore>();
            if (_memoryCore == null)
            {
                _memoryCore = gameObject.AddComponent<MemoryCore>();
            }

            // Connect legacy events to the new core
            ConnectLegacyEvents();
        }

        /// <summary>
        /// Connect legacy events to the new memory core
        /// </summary>
        private void ConnectLegacyEvents()
        {
            if (_memoryCore != null)
            {
                _memoryCore.OnMemorySnapshotCaptured += (snapshot) => OnMemorySnapshotCaptured?.Invoke(snapshot);
                _memoryCore.OnGCEventDetected += (gcSnapshot) => OnGCEventDetected?.Invoke(gcSnapshot);
                _memoryCore.OnMemoryAlert += (alert) => OnMemoryAlert?.Invoke(alert);
            }
        }

        /// <summary>
        /// Initialize profiler (Legacy method - delegates to MemoryCore)
        /// </summary>
        public void Initialize()
        {
            if (_enableLogging)
                ChimeraLogger.Log("MEMORY", "🔄 MemoryProfiler (Legacy) initialization delegated to MemoryCore", this);
        }

        public void Tick(float deltaTime)
        {
            if (!_enableLegacyMode) return;

            // Delegate to memory core - it handles all coordination
            // The core system automatically runs coordination via its own Tick method
            if (_enableLogging && Time.frameCount % 300 == 0) // Log every 5 seconds at 60fps
                ChimeraLogger.Log("MEMORY", "Legacy profiler tick delegated to MemoryCore", this);
        }

        /// <summary>
        /// Capture memory snapshot (Legacy method - delegates to MemoryCore)
        /// </summary>
        public void CaptureMemorySnapshot()
        {
            _memoryCore?.CaptureMemorySnapshot();

            if (_enableLogging)
                ChimeraLogger.Log("MEMORY", "Memory snapshot captured via legacy method", this);
        }

        /// <summary>
        /// Record allocation (Legacy method - delegates to MemoryCore)
        /// </summary>
        public void RecordAllocation(string category, long bytes)
        {
            _memoryCore?.RecordAllocation(category, bytes);

            if (_enableLogging && bytes > 1024 * 1024) // Log allocations > 1MB
                ChimeraLogger.Log("MEMORY", $"Large allocation recorded via legacy method: {category} - {bytes / (1024 * 1024):F2} MB", this);
        }

        /// <summary>
        /// Get memory statistics (Legacy method - delegates to MemoryCore)
        /// </summary>
        public MemoryProfilerStats GetMemoryStatistics()
        {
            return _memoryCore?.Stats ?? new MemoryProfilerStats();
        }

        /// <summary>
        /// Get memory history (Legacy method - delegates to MemoryCore)
        /// </summary>
        public MemorySnapshot[] GetMemoryHistory()
        {
            return _memoryCore?.GetMemoryHistory() ?? new MemorySnapshot[0];
        }

        /// <summary>
        /// Get GC history (Legacy method - delegates to MemoryCore)
        /// </summary>
        public GCSnapshot[] GetGCHistory()
        {
            return _memoryCore?.GetGCHistory() ?? new GCSnapshot[0];
        }

        /// <summary>
        /// Get current memory usage (Legacy method - delegates to MemoryCore)
        /// </summary>
        public long GetCurrentMemoryUsage()
        {
            return _memoryCore?.CurrentMemoryUsage ?? System.GC.GetTotalMemory(false);
        }

        /// <summary>
        /// Get memory pressure level (Legacy method - delegates to MemoryCore)
        /// </summary>
        public MemoryPressureLevel GetMemoryPressureLevel()
        {
            return _memoryCore?.GetMemoryPressureLevel() ?? MemoryPressureLevel.Normal;
        }

        /// <summary>
        /// Get GC performance impact (Legacy method - delegates to MemoryCore)
        /// </summary>
        public GCPerformanceImpact GetGCPerformanceImpact()
        {
            return _memoryCore?.GetGCPerformanceImpact() ?? GCPerformanceImpact.Low;
        }

        /// <summary>
        /// Clear history (Legacy method - delegates to MemoryCore)
        /// </summary>
        public void ClearHistory()
        {
            _memoryCore?.ClearHistory();

            if (_enableLogging)
                ChimeraLogger.Log("MEMORY", "Memory history cleared via legacy method", this);
        }

        /// <summary>
        /// Force alert check (Legacy method - delegates to MemoryCore)
        /// </summary>
        public void ForceAlertCheck()
        {
            _memoryCore?.ForceAlertCheck();

            if (_enableLogging)
                ChimeraLogger.Log("MEMORY", "Alert check forced via legacy method", this);
        }

        /// <summary>
        /// Check memory patterns (Legacy method - now handled by MemoryCore automatically)
        /// </summary>
        [System.Obsolete("Use MemoryCore automatic pattern analysis instead", false)]
        public void CheckMemoryPatterns()
        {
            if (_enableLogging)
                ChimeraLogger.Log("MEMORY", "Legacy CheckMemoryPatterns called - now handled automatically by MemoryCore", this);
        }

        /// <summary>
        /// Analyze memory patterns (Legacy method - now handled by MemoryCore automatically)
        /// </summary>
        [System.Obsolete("Use MemoryCore automatic pattern analysis instead", false)]
        public void AnalyzeMemoryPatterns()
        {
            if (_enableLogging)
                ChimeraLogger.Log("MEMORY", "Legacy AnalyzeMemoryPatterns called - now handled automatically by MemoryCore", this);
        }

        /// <summary>
        /// Check memory thresholds (Legacy method - now handled by MemoryAlertManager automatically)
        /// </summary>
        [System.Obsolete("Use MemoryAlertManager automatic threshold monitoring instead", false)]
        public void CheckMemoryThresholds()
        {
            if (_enableLogging)
                ChimeraLogger.Log("MEMORY", "Legacy CheckMemoryThresholds called - now handled automatically by MemoryAlertManager", this);
        }

        /// <summary>
        /// Check GC activity (Legacy method - now handled by GCAnalyzer automatically)
        /// </summary>
        [System.Obsolete("Use GCAnalyzer automatic GC tracking instead", false)]
        public void CheckGCActivity()
        {
            if (_enableLogging)
                ChimeraLogger.Log("MEMORY", "Legacy CheckGCActivity called - now handled automatically by GCAnalyzer", this);
        }

        #region Private Methods - Legacy Compatibility Helpers

        /// <summary>
        /// Legacy method - no longer needed as MemoryCore handles all coordination
        /// </summary>
        [System.Obsolete("Use MemoryCore instead", false)]
        private void UpdateProfiling()
        {
            // All profiling is now handled by MemoryCore
        }

        /// <summary>
        /// Legacy method - no longer needed as MemoryMonitor handles snapshots
        /// </summary>
        [System.Obsolete("Use MemoryMonitor instead", false)]
        private MemorySnapshot CreateMemorySnapshot()
        {
            // Now handled by MemoryMonitor
            return new MemorySnapshot();
        }

        /// <summary>
        /// Legacy method - no longer needed as GCAnalyzer handles GC tracking
        /// </summary>
        [System.Obsolete("Use GCAnalyzer instead", false)]
        private void RecordGCEvent(int generation, int count)
        {
            // Now handled by GCAnalyzer
        }

        /// <summary>
        /// Legacy method - no longer needed as MemoryAlertManager handles threshold checking
        /// </summary>
        [System.Obsolete("Use MemoryAlertManager instead", false)]
        private void CheckMemoryThresholds(MemorySnapshot snapshot)
        {
            // Now handled by MemoryAlertManager
        }

        #endregion

        private void OnDestroy()
        {
            UpdateOrchestrator.Instance?.UnregisterTickable(this);
        }
    }

    #region Data Structures - Legacy Compatibility

    /// <summary>
    /// Legacy memory health levels (use MemorySystemHealth from subsystems instead)
    /// </summary>
    public enum LegacyMemoryHealth
    {
        Healthy,
        Warning,
        Critical,
        Emergency
    }

    /// <summary>
    /// Legacy GC generation info
    /// </summary>
    [System.Serializable]
    [System.Obsolete("Use GCSnapshot from subsystems instead")]
    public struct LegacyGCInfo
    {
        public int generation;
        public int collectionCount;
        public float timestamp;
    }

    /// <summary>
    /// Legacy allocation info
    /// </summary>
    [System.Serializable]
    [System.Obsolete("Use AllocationSnapshot from subsystems instead")]
    public struct LegacyAllocationInfo
    {
        public string category;
        public long bytes;
        public float timestamp;
    }

    #endregion
}