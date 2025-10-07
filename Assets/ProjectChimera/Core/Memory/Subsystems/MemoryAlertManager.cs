using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using Logger = ProjectChimera.Core.Logging.ChimeraLogger;

namespace ProjectChimera.Core.Memory.Subsystems
{
    /// <summary>
    /// REFACTORED: Memory Alert Manager - Focused memory threshold monitoring and alerting
    /// Handles memory pressure detection, threshold breaches, and emergency alerts
    /// Single Responsibility: Memory threshold monitoring and alerting
    /// </summary>
    public class MemoryAlertManager : MonoBehaviour
    {
        [Header("Alert Manager Settings")]
        [SerializeField] private bool _enableAlerts = true;
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private float _alertCheckInterval = 1f;
        [SerializeField] private float _alertCooldownTime = 5f;

        [Header("Memory Thresholds")]
        [SerializeField] private long _memoryWarningThreshold = 500 * 1024 * 1024; // 500MB
        [SerializeField] private long _memoryCriticalThreshold = 800 * 1024 * 1024; // 800MB
        [SerializeField] private long _memoryEmergencyThreshold = 1024 * 1024 * 1024; // 1GB

        [Header("Allocation Rate Thresholds")]
        [SerializeField] private long _allocationRateWarning = 10 * 1024 * 1024; // 10MB/s
        [SerializeField] private long _allocationRateCritical = 50 * 1024 * 1024; // 50MB/s

        [Header("GC Frequency Thresholds")]
        [SerializeField] private float _gcFrequencyWarning = 3f; // 3 GCs per second
        [SerializeField] private float _gcFrequencyCritical = 8f; // 8 GCs per second

        // System references
        private MemoryMonitor _memoryMonitor;
        private GCAnalyzer _gcAnalyzer;

        // Alert state
        private readonly Dictionary<MemoryAlertType, float> _lastAlertTimes = new Dictionary<MemoryAlertType, float>();
        private MemoryAlertLevel _currentAlertLevel = MemoryAlertLevel.Normal;
        private float _lastAlertCheck;

        // Statistics
        private MemoryAlertStats _stats = new MemoryAlertStats();

        // Properties
        public bool IsEnabled { get; private set; } = true;
        public MemoryAlertLevel CurrentAlertLevel => _currentAlertLevel;
        public MemoryAlertStats GetStats() => _stats;

        // Events
        public System.Action<MemoryAlert> OnMemoryAlert;
        public System.Action<MemoryAlertLevel, MemoryAlertLevel> OnAlertLevelChanged;
        public System.Action OnEmergencyMemoryCondition;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            InitializeSystemReferences();
            InitializeAlertTimes();

            if (_enableLogging)
                Logger.Log("MEMORY", "⚠️ MemoryAlertManager initialized", this);
        }

        /// <summary>
        /// Initialize system references using dependency injection
        /// </summary>
        private void InitializeSystemReferences()
        {
            // Use SafeResolve helper for clean dependency injection with fallback
            _memoryMonitor = DependencyResolutionHelper.SafeResolve<MemoryMonitor>(this, "MEMORY");
            _gcAnalyzer = DependencyResolutionHelper.SafeResolve<GCAnalyzer>(this, "MEMORY");

            if (_memoryMonitor == null)
            {
                Logger.LogError("MEMORY", "Critical dependency MemoryMonitor not found", this);
            }

            if (_gcAnalyzer == null)
            {
                Logger.LogError("MEMORY", "Critical dependency GCAnalyzer not found", this);
            }
        }

        /// <summary>
        /// Initialize alert times dictionary
        /// </summary>
        private void InitializeAlertTimes()
        {
            foreach (MemoryAlertType alertType in System.Enum.GetValues(typeof(MemoryAlertType)))
            {
                _lastAlertTimes[alertType] = 0f;
            }
        }

        /// <summary>
        /// Update memory alert checking (called from coordinator)
        /// </summary>
        public void UpdateMemoryAlerts()
        {
            if (!IsEnabled || !_enableAlerts) return;

            if (Time.time - _lastAlertCheck >= _alertCheckInterval)
            {
                CheckMemoryAlerts();
                _lastAlertCheck = Time.time;
            }
        }

        /// <summary>
        /// Force memory alert check
        /// </summary>
        public void ForceAlertCheck()
        {
            if (!IsEnabled) return;

            CheckMemoryAlerts();

            if (_enableLogging)
                Logger.Log("MEMORY", "Forced memory alert check", this);
        }

        /// <summary>
        /// Manually trigger alert
        /// </summary>
        public void TriggerAlert(MemoryAlertType alertType, string message, MemoryAlertLevel level)
        {
            if (!IsEnabled) return;

            var alert = new MemoryAlert
            {
                AlertType = alertType,
                Level = level,
                Message = message,
                Timestamp = Time.time,
                CurrentMemoryMB = _memoryMonitor?.GetCurrentMemoryUsage() / (1024f * 1024f) ?? 0f
            };

            ProcessAlert(alert);
        }

        /// <summary>
        /// Check if alert can be fired (cooldown check)
        /// </summary>
        public bool CanFireAlert(MemoryAlertType alertType)
        {
            if (!_lastAlertTimes.TryGetValue(alertType, out var lastTime))
                return true;

            return (Time.time - lastTime) >= _alertCooldownTime;
        }

        /// <summary>
        /// Get current memory pressure level
        /// </summary>
        public MemoryPressureLevel GetMemoryPressureLevel()
        {
            var currentMemory = _memoryMonitor?.GetCurrentMemoryUsage() ?? 0;

            if (currentMemory >= _memoryEmergencyThreshold)
                return MemoryPressureLevel.Emergency;
            else if (currentMemory >= _memoryCriticalThreshold)
                return MemoryPressureLevel.Critical;
            else if (currentMemory >= _memoryWarningThreshold)
                return MemoryPressureLevel.Warning;
            else
                return MemoryPressureLevel.Normal;
        }

        /// <summary>
        /// Set system enabled/disabled
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;

            if (!enabled)
            {
                _currentAlertLevel = MemoryAlertLevel.Normal;
                _stats = new MemoryAlertStats();
            }

            if (_enableLogging)
                Logger.Log("MEMORY", $"MemoryAlertManager: {(enabled ? "enabled" : "disabled")}", this);
        }

        #region Private Methods

        /// <summary>
        /// Check all memory alerts
        /// </summary>
        private void CheckMemoryAlerts()
        {
            CheckMemoryThresholds();
            CheckAllocationRateThresholds();
            CheckGCFrequencyThresholds();
            UpdateAlertLevel();
        }

        /// <summary>
        /// Check memory usage thresholds
        /// </summary>
        private void CheckMemoryThresholds()
        {
            var currentMemory = _memoryMonitor?.GetCurrentMemoryUsage() ?? 0;

            if (currentMemory >= _memoryEmergencyThreshold)
            {
                TriggerMemoryAlert(MemoryAlertType.MemoryEmergency,
                    $"Emergency memory condition: {currentMemory / (1024 * 1024):F2} MB",
                    MemoryAlertLevel.Emergency);

                OnEmergencyMemoryCondition?.Invoke();
            }
            else if (currentMemory >= _memoryCriticalThreshold)
            {
                TriggerMemoryAlert(MemoryAlertType.MemoryCritical,
                    $"Critical memory usage: {currentMemory / (1024 * 1024):F2} MB",
                    MemoryAlertLevel.Critical);
            }
            else if (currentMemory >= _memoryWarningThreshold)
            {
                TriggerMemoryAlert(MemoryAlertType.MemoryWarning,
                    $"High memory usage: {currentMemory / (1024 * 1024):F2} MB",
                    MemoryAlertLevel.Warning);
            }
        }

        /// <summary>
        /// Check allocation rate thresholds
        /// </summary>
        private void CheckAllocationRateThresholds()
        {
            if (_memoryMonitor == null) return;

            var memoryHistory = _memoryMonitor.GetMemoryHistory();
            if (memoryHistory.Length < 2) return;

            // Calculate allocation rate from recent snapshots
            int startIndex = System.Math.Max(0, memoryHistory.Length - 5);
            int length = memoryHistory.Length - startIndex;
            var recentSnapshots = new MemorySnapshot[length];
            System.Array.Copy(memoryHistory, startIndex, recentSnapshots, 0, length);

            if (recentSnapshots.Length < 2) return;

            var lastIdx = recentSnapshots.Length - 1;
            var timeDiff = recentSnapshots[lastIdx].Timestamp - recentSnapshots[0].Timestamp;
            var memoryDiff = recentSnapshots[lastIdx].TotalMemoryBytes - recentSnapshots[0].TotalMemoryBytes;

            if (timeDiff > 0)
            {
                var allocationRate = memoryDiff / timeDiff; // bytes per second

                if (allocationRate >= _allocationRateCritical)
                {
                    TriggerMemoryAlert(MemoryAlertType.AllocationRateCritical,
                        $"Critical allocation rate: {allocationRate / (1024 * 1024):F2} MB/s",
                        MemoryAlertLevel.Critical);
                }
                else if (allocationRate >= _allocationRateWarning)
                {
                    TriggerMemoryAlert(MemoryAlertType.AllocationRateHigh,
                        $"High allocation rate: {allocationRate / (1024 * 1024):F2} MB/s",
                        MemoryAlertLevel.Warning);
                }
            }
        }

        /// <summary>
        /// Check GC frequency thresholds
        /// </summary>
        private void CheckGCFrequencyThresholds()
        {
            if (_gcAnalyzer == null) return;

            var currentGCFrequency = _gcAnalyzer.CurrentGCFrequency;

            if (currentGCFrequency >= _gcFrequencyCritical)
            {
                TriggerMemoryAlert(MemoryAlertType.GCFrequencyCritical,
                    $"Critical GC frequency: {currentGCFrequency:F2} GCs/sec",
                    MemoryAlertLevel.Critical);
            }
            else if (currentGCFrequency >= _gcFrequencyWarning)
            {
                TriggerMemoryAlert(MemoryAlertType.GCFrequencyHigh,
                    $"High GC frequency: {currentGCFrequency:F2} GCs/sec",
                    MemoryAlertLevel.Warning);
            }
        }

        /// <summary>
        /// Trigger a memory alert
        /// </summary>
        private void TriggerMemoryAlert(MemoryAlertType alertType, string message, MemoryAlertLevel level)
        {
            if (!CanFireAlert(alertType)) return;

            var alert = new MemoryAlert
            {
                AlertType = alertType,
                Level = level,
                Message = message,
                Timestamp = Time.time,
                CurrentMemoryMB = _memoryMonitor?.GetCurrentMemoryUsage() / (1024f * 1024f) ?? 0f
            };

            ProcessAlert(alert);
        }

        /// <summary>
        /// Process and fire memory alert
        /// </summary>
        private void ProcessAlert(MemoryAlert alert)
        {
            _lastAlertTimes[alert.AlertType] = Time.time;
            _stats.TotalAlerts++;

            switch (alert.Level)
            {
                case MemoryAlertLevel.Warning:
                    _stats.WarningAlerts++;
                    break;
                case MemoryAlertLevel.Critical:
                    _stats.CriticalAlerts++;
                    break;
                case MemoryAlertLevel.Emergency:
                    _stats.EmergencyAlerts++;
                    break;
            }

            OnMemoryAlert?.Invoke(alert);

            if (_enableLogging)
            {
                if (alert.Level == MemoryAlertLevel.Emergency || alert.Level == MemoryAlertLevel.Critical)
                {
                    Logger.LogError("MEMORY", $"[{alert.Level}] {alert.Message}", this);
                }
                else
                {
                    Logger.LogWarning("MEMORY", $"[{alert.Level}] {alert.Message}", this);
                }
            }
        }

        /// <summary>
        /// Update current alert level
        /// </summary>
        private void UpdateAlertLevel()
        {
            var previousLevel = _currentAlertLevel;
            var pressureLevel = GetMemoryPressureLevel();
            var gcImpact = _gcAnalyzer?.GetGCPerformanceImpact() ?? GCPerformanceImpact.Low;

            // Determine alert level based on multiple factors
            _currentAlertLevel = DetermineAlertLevel(pressureLevel, gcImpact);

            if (_currentAlertLevel != previousLevel)
            {
                OnAlertLevelChanged?.Invoke(previousLevel, _currentAlertLevel);

                if (_enableLogging)
                    Logger.Log("MEMORY", $"Alert level changed: {previousLevel} -> {_currentAlertLevel}", this);
            }
        }

        /// <summary>
        /// Determine alert level from pressure and GC impact
        /// </summary>
        private MemoryAlertLevel DetermineAlertLevel(MemoryPressureLevel pressureLevel, GCPerformanceImpact gcImpact)
        {
            // Emergency takes priority
            if (pressureLevel == MemoryPressureLevel.Emergency)
                return MemoryAlertLevel.Emergency;

            // Critical conditions
            if (pressureLevel == MemoryPressureLevel.Critical || gcImpact == GCPerformanceImpact.Critical)
                return MemoryAlertLevel.Critical;

            // Warning conditions
            if (pressureLevel == MemoryPressureLevel.Warning || gcImpact >= GCPerformanceImpact.High)
                return MemoryAlertLevel.Warning;

            return MemoryAlertLevel.Normal;
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Memory alert types
    /// </summary>
    public enum MemoryAlertType
    {
        MemoryWarning,
        MemoryCritical,
        MemoryEmergency,
        AllocationRateHigh,
        AllocationRateCritical,
        GCFrequencyHigh,
        GCFrequencyCritical
    }

    /// <summary>
    /// Memory alert levels
    /// </summary>
    public enum MemoryAlertLevel
    {
        Normal,
        Warning,
        Critical,
        Emergency
    }

    /// <summary>
    /// Memory pressure levels
    /// </summary>
    public enum MemoryPressureLevel
    {
        Normal,
        Warning,
        Critical,
        Emergency
    }

    /// <summary>
    /// Memory alert data structure
    /// </summary>
    [System.Serializable]
    public struct MemoryAlert
    {
        public MemoryAlertType AlertType;
        public MemoryAlertLevel Level;
        public string Message;
        public float Timestamp;
        public float CurrentMemoryMB;
    }

    /// <summary>
    /// Memory alert statistics
    /// </summary>
    [System.Serializable]
    public struct MemoryAlertStats
    {
        public int TotalAlerts;
        public int WarningAlerts;
        public int CriticalAlerts;
        public int EmergencyAlerts;
    }

    #endregion
}
