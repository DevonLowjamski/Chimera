using UnityEngine;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Services.SpeedTree.Performance
{
    /// <summary>
    /// BASIC: Simple memory manager for Project Chimera's performance system.
    /// Focuses on essential memory monitoring without complex asset tracking and optimization systems.
    /// </summary>
    public class MemoryManager : MonoBehaviour
    {
        [Header("Basic Memory Settings")]
        [SerializeField] private bool _enableBasicMonitoring = true;
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private float _memoryCheckInterval = 10f;
        [SerializeField] private float _memoryWarningThresholdMB = 512f;

        // Basic memory tracking
        private float _lastMemoryCheck = 0f;
        private float _peakMemoryUsage = 0f;
        private int _memoryChecksPerformed = 0;
        private bool _isInitialized = false;

        /// <summary>
        /// Events for memory management
        /// </summary>
        public event System.Action<float> OnMemoryWarning;
        public event System.Action<float> OnMemoryCheck;

        /// <summary>
        /// Initialize basic memory manager
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            _lastMemoryCheck = Time.time;
            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("[MemoryManager] Initialized successfully");
            }
        }

        /// <summary>
        /// Update memory monitoring
        /// </summary>
        private void Update()
        {
            if (!_enableBasicMonitoring || !_isInitialized) return;

            float currentTime = Time.time;
            if (currentTime - _lastMemoryCheck >= _memoryCheckInterval)
            {
                CheckMemoryUsage();
                _lastMemoryCheck = currentTime;
            }
        }

        /// <summary>
        /// Check current memory usage
        /// </summary>
        private void CheckMemoryUsage()
        {
            // Get current memory usage (Unity-specific)
            float currentMemoryMB = GetCurrentMemoryUsageMB();
            _memoryChecksPerformed++;

            // Track peak usage
            if (currentMemoryMB > _peakMemoryUsage)
            {
                _peakMemoryUsage = currentMemoryMB;
            }

            // Check for memory warnings
            if (currentMemoryMB > _memoryWarningThresholdMB)
            {
                OnMemoryWarning?.Invoke(currentMemoryMB);

                if (_enableLogging)
                {
                    ChimeraLogger.LogWarning($"[MemoryManager] High memory usage: {currentMemoryMB:F1}MB (threshold: {_memoryWarningThresholdMB:F1}MB)");
                }
            }

            OnMemoryCheck?.Invoke(currentMemoryMB);
        }

        /// <summary>
        /// Force garbage collection
        /// </summary>
        public void ForceGarbageCollection()
        {
            System.GC.Collect();
            Resources.UnloadUnusedAssets();

            if (_enableLogging)
            {
                ChimeraLogger.Log("[MemoryManager] Forced garbage collection and asset cleanup");
            }
        }

        /// <summary>
        /// Get current memory usage in MB
        /// </summary>
        public float GetCurrentMemoryUsageMB()
        {
            // Unity-specific memory monitoring
            return (float)UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong() / (1024f * 1024f);
        }

        /// <summary>
        /// Get total allocated memory in MB
        /// </summary>
        public float GetTotalAllocatedMemoryMB()
        {
            return (float)UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() / (1024f * 1024f);
        }

        /// <summary>
        /// Get memory statistics
        /// </summary>
        public MemoryStats GetStats()
        {
            return new MemoryStats
            {
                CurrentMemoryUsage = GetCurrentMemoryUsageMB(),
                TotalAllocatedMemory = GetTotalAllocatedMemoryMB(),
                PeakMemoryUsage = _peakMemoryUsage,
                MemoryChecksPerformed = _memoryChecksPerformed,
                WarningThreshold = _memoryWarningThresholdMB,
                IsMonitoringEnabled = _enableBasicMonitoring,
                IsInitialized = _isInitialized
            };
        }

        /// <summary>
        /// Set memory monitoring parameters
        /// </summary>
        public void SetMonitoringParameters(float checkInterval, float warningThreshold)
        {
            _memoryCheckInterval = Mathf.Max(1f, checkInterval);
            _memoryWarningThresholdMB = Mathf.Max(64f, warningThreshold);

            if (_enableLogging)
            {
                ChimeraLogger.Log($"[MemoryManager] Updated parameters - Interval: {_memoryCheckInterval:F1}s, Threshold: {_memoryWarningThresholdMB:F1}MB");
            }
        }

        /// <summary>
        /// Reset memory tracking
        /// </summary>
        public void ResetTracking()
        {
            _peakMemoryUsage = 0f;
            _memoryChecksPerformed = 0;
            _lastMemoryCheck = Time.time;

            if (_enableLogging)
            {
                ChimeraLogger.Log("[MemoryManager] Reset memory tracking");
            }
        }

        /// <summary>
        /// Check if memory usage is within acceptable limits
        /// </summary>
        public bool IsMemoryUsageAcceptable()
        {
            return GetCurrentMemoryUsageMB() <= _memoryWarningThresholdMB;
        }

        /// <summary>
        /// Get memory usage as percentage of warning threshold
        /// </summary>
        public float GetMemoryUsagePercentage()
        {
            return (GetCurrentMemoryUsageMB() / _memoryWarningThresholdMB) * 100f;
        }

        /// <summary>
        /// Set monitoring enabled state
        /// </summary>
        public void SetMonitoringEnabled(bool enabled)
        {
            _enableBasicMonitoring = enabled;

            if (!enabled)
            {
                ResetTracking();
            }

            if (_enableLogging)
            {
                ChimeraLogger.Log($"[MemoryManager] Monitoring {(enabled ? "enabled" : "disabled")}");
            }
        }
    }

    /// <summary>
    /// Memory statistics
    /// </summary>
    [System.Serializable]
    public struct MemoryStats
    {
        public float CurrentMemoryUsage;
        public float TotalAllocatedMemory;
        public float PeakMemoryUsage;
        public int MemoryChecksPerformed;
        public float WarningThreshold;
        public bool IsMonitoringEnabled;
        public bool IsInitialized;
    }
}
