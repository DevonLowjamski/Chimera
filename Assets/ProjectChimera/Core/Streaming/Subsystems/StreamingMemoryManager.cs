using UnityEngine;
using System.Collections;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Memory;
using ProjectChimera.Core.SimpleDI;
using Logger = ProjectChimera.Core.Logging.ChimeraLogger;

namespace ProjectChimera.Core.Streaming.Subsystems
{
    /// <summary>
    /// REFACTORED: Streaming Memory Manager - Focused memory optimization and garbage collection management
    /// Handles memory pressure monitoring, GC optimization, and memory-based performance adjustments
    /// Single Responsibility: Memory optimization and garbage collection management
    /// </summary>
    public class StreamingMemoryManager : MonoBehaviour
    {
        [Header("Memory Management Settings")]
        [SerializeField] private bool _enableMemoryManagement = true;
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private float _memoryCheckInterval = 2f;
        [SerializeField] private bool _enableAutoGC = true;

        [Header("Memory Thresholds")]
        [SerializeField] private long _memoryPressureThreshold = 500 * 1024 * 1024; // 500 MB
        [SerializeField] private long _criticalMemoryThreshold = 800 * 1024 * 1024; // 800 MB
        [SerializeField] private float _gcTriggerThreshold = 0.8f; // 80% memory usage

        [Header("GC Optimization")]
        [SerializeField] private bool _enableGCOptimization = true;
        [SerializeField] private float _gcCooldownTime = 10f;
        [SerializeField] private bool _enableFrameRateBasedGC = true;
        [SerializeField] private float _frameRateThresholdForGC = 30f;

        // System references
        private GCOptimizationManager _gcManager;
        private StreamingQualityManager _qualityManager;

        // Memory state
        private long _lastRecordedMemoryUsage;
        private float _lastMemoryCheck;
        private float _lastGCTime;
        private bool _isMemoryPressureDetected;

        // Statistics
        private MemoryManagerStats _stats = new MemoryManagerStats();

        // Properties
        public bool IsEnabled { get; private set; } = true;
        public bool IsMemoryPressureDetected => _isMemoryPressureDetected;
        public long CurrentMemoryUsage => GetCurrentMemoryUsage();
        public MemoryManagerStats GetStats() => _stats;

        // Events
        public System.Action<long> OnMemoryPressureDetected;
        public System.Action<long> OnCriticalMemoryReached;
        public System.Action OnGarbageCollectionTriggered;
        public System.Action<long, long> OnMemoryUsageChanged;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            InitializeSystemReferences();

            if (_enableLogging)
                Logger.Log("STREAMING", "🧠 StreamingMemoryManager initialized", this);
        }

        /// <summary>
        /// Initialize system references via ServiceContainer DI
        /// </summary>
        private void InitializeSystemReferences()
        {
            // Resolve via ServiceContainer with optional dependency pattern
            _gcManager = ServiceContainerFactory.Instance?.TryResolve<GCOptimizationManager>();
            _qualityManager = ServiceContainerFactory.Instance?.TryResolve<StreamingQualityManager>();

            if (_gcManager == null && _enableLogging)
                Logger.LogWarning("STREAMING", "GCOptimizationManager not registered - GC optimization disabled", this);

            if (_qualityManager == null && _enableLogging)
                Logger.LogWarning("STREAMING", "StreamingQualityManager not registered - quality management disabled", this);
        }

        /// <summary>
        /// Update memory management (called from coordinator)
        /// </summary>
        public void UpdateMemoryManagement()
        {
            if (!IsEnabled || !_enableMemoryManagement) return;

            if (Time.time - _lastMemoryCheck >= _memoryCheckInterval)
            {
                ProcessMemoryMonitoring();
                _lastMemoryCheck = Time.time;
            }

            if (_enableAutoGC && ShouldTriggerGarbageCollection())
            {
                TriggerGarbageCollection();
            }
        }

        /// <summary>
        /// Force garbage collection
        /// </summary>
        public void ForceGarbageCollection()
        {
            if (!IsEnabled) return;

            TriggerGarbageCollection();
            _stats.ForcedGCCollections++;

            if (_enableLogging)
                Logger.Log("STREAMING", "Forced garbage collection executed", this);
        }

        /// <summary>
        /// Optimize memory for streaming
        /// </summary>
        public void OptimizeStreamingMemory()
        {
            if (!IsEnabled || !_enableMemoryManagement) return;

            StartCoroutine(OptimizeStreamingMemoryCoroutine());
        }

        /// <summary>
        /// Get current memory usage in bytes
        /// </summary>
        public long GetCurrentMemoryUsage()
        {
            return System.GC.GetTotalMemory(false);
        }

        /// <summary>
        /// Get memory usage as percentage of threshold
        /// </summary>
        public float GetMemoryUsagePercentage()
        {
            var currentUsage = GetCurrentMemoryUsage();
            return (float)currentUsage / _memoryPressureThreshold;
        }

        /// <summary>
        /// Check if memory optimization is needed
        /// </summary>
        public bool IsMemoryOptimizationNeeded()
        {
            var currentUsage = GetCurrentMemoryUsage();
            return currentUsage >= _memoryPressureThreshold;
        }

        /// <summary>
        /// Get memory pressure level
        /// </summary>
        public MemoryPressureLevel GetMemoryPressureLevel()
        {
            var currentUsage = GetCurrentMemoryUsage();

            if (currentUsage >= _criticalMemoryThreshold)
                return MemoryPressureLevel.Critical;
            else if (currentUsage >= _memoryPressureThreshold)
                return MemoryPressureLevel.High;
            else if (currentUsage >= _memoryPressureThreshold * 0.7f)
                return MemoryPressureLevel.Medium;
            else
                return MemoryPressureLevel.Low;
        }

        /// <summary>
        /// Set system enabled/disabled
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;

            if (!enabled)
            {
                _isMemoryPressureDetected = false;
            }

            if (_enableLogging)
                Logger.Log("STREAMING", $"StreamingMemoryManager: {(enabled ? "enabled" : "disabled")}", this);
        }

        #region Private Methods

        /// <summary>
        /// Process memory monitoring and pressure detection
        /// </summary>
        private void ProcessMemoryMonitoring()
        {
            var currentUsage = GetCurrentMemoryUsage();
            var previousUsage = _lastRecordedMemoryUsage;
            _lastRecordedMemoryUsage = currentUsage;

            // Check for memory pressure
            var pressureLevel = GetMemoryPressureLevel();
            var wasPressureDetected = _isMemoryPressureDetected;
            _isMemoryPressureDetected = pressureLevel >= MemoryPressureLevel.Medium;

            // Fire events for memory usage changes
            if (System.Math.Abs(currentUsage - previousUsage) > 50 * 1024 * 1024) // 50 MB change
            {
                OnMemoryUsageChanged?.Invoke(previousUsage, currentUsage);
            }

            // Handle memory pressure detection
            if (_isMemoryPressureDetected && !wasPressureDetected)
            {
                HandleMemoryPressureDetected(currentUsage, pressureLevel);
            }

            // Handle critical memory situation
            if (pressureLevel == MemoryPressureLevel.Critical)
            {
                HandleCriticalMemoryReached(currentUsage);
            }

            // Update statistics
            _stats.MemoryChecks++;
            _stats.CurrentMemoryUsage = currentUsage;
            _stats.PeakMemoryUsage = System.Math.Max(_stats.PeakMemoryUsage, currentUsage);
        }

        /// <summary>
        /// Handle memory pressure detection
        /// </summary>
        private void HandleMemoryPressureDetected(long memoryUsage, MemoryPressureLevel level)
        {
            _stats.MemoryPressureEvents++;
            OnMemoryPressureDetected?.Invoke(memoryUsage);

            if (_enableLogging)
                Logger.LogWarning("STREAMING", $"Memory pressure detected: {memoryUsage / (1024 * 1024)} MB (Level: {level})", this);

            // Trigger quality downgrade if possible
            if (_qualityManager != null && level >= MemoryPressureLevel.High)
            {
                _qualityManager.ForceQualityDowngrade();
            }

            // Trigger garbage collection
            if (_enableAutoGC)
            {
                TriggerGarbageCollection();
            }
        }

        /// <summary>
        /// Handle critical memory situation
        /// </summary>
        private void HandleCriticalMemoryReached(long memoryUsage)
        {
            _stats.CriticalMemoryEvents++;
            OnCriticalMemoryReached?.Invoke(memoryUsage);

            if (_enableLogging)
                Logger.LogError("STREAMING", $"Critical memory reached: {memoryUsage / (1024 * 1024)} MB", this);

            // Emergency memory optimization
            StartCoroutine(EmergencyMemoryOptimization());
        }

        /// <summary>
        /// Check if garbage collection should be triggered
        /// </summary>
        private bool ShouldTriggerGarbageCollection()
        {
            // Check cooldown
            if (Time.time - _lastGCTime < _gcCooldownTime)
                return false;

            // Check memory usage threshold
            var memoryUsageRatio = GetMemoryUsagePercentage();
            if (memoryUsageRatio >= _gcTriggerThreshold)
                return true;

            // Check frame rate based trigger
            if (_enableFrameRateBasedGC)
            {
                var currentFrameRate = 1f / Time.deltaTime;
                if (currentFrameRate < _frameRateThresholdForGC && memoryUsageRatio > 0.6f)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Trigger garbage collection
        /// </summary>
        private void TriggerGarbageCollection()
        {
            _lastGCTime = Time.time;
            _stats.GCCollections++;

            if (_gcManager != null && _enableGCOptimization)
            {
                // Use legacy-compatible API on manager
                _gcManager.ForceOptimizedGC(true);
            }
            else
            {
                // Fallback manual GC
                System.GC.Collect();
                System.GC.WaitForPendingFinalizers();
            }

            OnGarbageCollectionTriggered?.Invoke();

            if (_enableLogging)
                Logger.Log("STREAMING", "Garbage collection triggered", this);
        }

        /// <summary>
        /// Optimize streaming memory coroutine
        /// </summary>
        private IEnumerator OptimizeStreamingMemoryCoroutine()
        {
            if (_enableLogging)
                Logger.Log("STREAMING", "Starting streaming memory optimization", this);

            var initialMemory = GetCurrentMemoryUsage();

            // Trigger garbage collection first
            TriggerGarbageCollection();
            yield return new WaitForEndOfFrame();

            // Additional optimization steps could be added here
            yield return new WaitForSeconds(0.1f);

            var finalMemory = GetCurrentMemoryUsage();
            var memoryFreed = initialMemory - finalMemory;

            _stats.MemoryOptimizations++;
            _stats.TotalMemoryFreed += System.Math.Max(0, memoryFreed);

            if (_enableLogging)
            {
                Logger.Log("STREAMING",
                    $"Streaming memory optimization completed. Memory freed: {memoryFreed / (1024 * 1024):F2} MB",
                    this);
            }
        }

        /// <summary>
        /// Emergency memory optimization coroutine
        /// </summary>
        private IEnumerator EmergencyMemoryOptimization()
        {
            if (_enableLogging)
                Logger.Log("STREAMING", "Starting emergency memory optimization", this);

            // Force quality to lowest setting
            if (_qualityManager != null)
            {
                while (_qualityManager.CurrentQualityIndex > 0)
                {
                    _qualityManager.ForceQualityDowngrade();
                    yield return new WaitForEndOfFrame();
                }
            }

            // Aggressive garbage collection
            for (int i = 0; i < 3; i++)
            {
                TriggerGarbageCollection();
                yield return new WaitForEndOfFrame();
            }

            if (_enableLogging)
                Logger.Log("STREAMING", "Emergency memory optimization completed", this);
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Memory pressure level enumeration
    /// </summary>
    public enum MemoryPressureLevel
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// Memory manager statistics
    /// </summary>
    [System.Serializable]
    public struct MemoryManagerStats
    {
        public int MemoryChecks;
        public int MemoryPressureEvents;
        public int CriticalMemoryEvents;
        public int GCCollections;
        public int ForcedGCCollections;
        public int MemoryOptimizations;
        public long CurrentMemoryUsage;
        public long PeakMemoryUsage;
        public long TotalMemoryFreed;
    }

    #endregion
}
