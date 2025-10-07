using UnityEngine;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;

namespace ProjectChimera.Core.Memory.Subsystems
{
    /// <summary>
    /// REFACTORED: Memory Core - Central coordination for memory management subsystems
    /// Coordinates memory monitoring, GC analysis, alert management, and optimization recommendations
    /// Single Responsibility: Central memory system coordination
    /// </summary>
    public class MemoryCore : MonoBehaviour, ITickable
    {
        [Header("Core Settings")]
        [SerializeField] private bool _enableMemoryCoordination = true;
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private float _updateInterval = 1f;

        // Subsystem references
        private MemoryMonitor _memoryMonitor;
        private GCAnalyzer _gcAnalyzer;
        private MemoryAlertManager _alertManager;

        // Timing
        private float _lastUpdate;

        // System health
        private MemorySystemHealth _systemHealth = MemorySystemHealth.Healthy;
        private MemoryProfilerStats _stats = new MemoryProfilerStats();

        // Properties
        public bool IsEnabled { get; private set; } = true;
        public MemorySystemHealth SystemHealth => _systemHealth;
        public MemoryProfilerStats Stats => _stats;
        public long CurrentMemoryUsage => _memoryMonitor?.GetCurrentMemoryUsage() ?? 0;

        // Events for backward compatibility
        public System.Action<MemorySnapshot> OnMemorySnapshotCaptured;
        public System.Action<GCSnapshot> OnGCEventDetected;
        public System.Action<MemoryAlert> OnMemoryAlert;
        public System.Action<MemorySystemHealth> OnHealthChanged;

        public int TickPriority => 80;
        public bool IsTickable => enabled && gameObject.activeInHierarchy;

        private void Start()
        {
            Initialize();
            UpdateOrchestrator.Instance?.RegisterTickable(this);
        }

        private void Initialize()
        {
            InitializeSubsystems();
            ConnectEventHandlers();

            if (_enableLogging)
                ChimeraLogger.Log("MEMORY", "⚡ MemoryCore initialized", this);
        }

        /// <summary>
        /// Initialize all memory subsystems
        /// </summary>
        private void InitializeSubsystems()
        {
            // Get or create subsystem components
            _memoryMonitor = GetOrCreateComponent<MemoryMonitor>();
            _gcAnalyzer = GetOrCreateComponent<GCAnalyzer>();
            _alertManager = GetOrCreateComponent<MemoryAlertManager>();

            // Configure subsystems
            _memoryMonitor?.SetEnabled(_enableMemoryCoordination);
            _gcAnalyzer?.SetEnabled(_enableMemoryCoordination);
            _alertManager?.SetEnabled(_enableMemoryCoordination);
        }

        /// <summary>
        /// Connect event handlers between subsystems
        /// </summary>
        private void ConnectEventHandlers()
        {
            if (_memoryMonitor != null)
            {
                _memoryMonitor.OnMemorySnapshotCaptured += HandleMemorySnapshotCaptured;
            }

            if (_gcAnalyzer != null)
            {
                _gcAnalyzer.OnGCEventDetected += HandleGCEventDetected;
                _gcAnalyzer.OnGCPerformanceImpact += HandleGCPerformanceImpact;
            }

            if (_alertManager != null)
            {
                _alertManager.OnMemoryAlert += HandleMemoryAlert;
                _alertManager.OnAlertLevelChanged += HandleAlertLevelChanged;
                _alertManager.OnEmergencyMemoryCondition += HandleEmergencyMemoryCondition;
            }
        }

        /// <summary>
        /// Get or create subsystem component
        /// </summary>
        private T GetOrCreateComponent<T>() where T : Component
        {
            var component = GetComponent<T>();
            if (component == null)
            {
                component = gameObject.AddComponent<T>();
            }
            return component;
        }

        public void Tick(float deltaTime)
        {
            if (!IsEnabled || !_enableMemoryCoordination) return;

            if (Time.time - _lastUpdate < _updateInterval) return;

            ProcessMemoryCoordination();
            UpdateSystemHealth();
            UpdateStatistics();

            _lastUpdate = Time.time;
        }

        /// <summary>
        /// Process memory system coordination
        /// </summary>
        private void ProcessMemoryCoordination()
        {
            // Update memory monitoring
            _memoryMonitor?.UpdateMemoryMonitoring();

            // Update GC analysis
            _gcAnalyzer?.UpdateGCAnalysis();

            // Update memory alerts
            _alertManager?.UpdateMemoryAlerts();
        }

        /// <summary>
        /// Update system health based on subsystem status
        /// </summary>
        private void UpdateSystemHealth()
        {
            var previousHealth = _systemHealth;
            _systemHealth = DetermineSystemHealth();

            if (_systemHealth != previousHealth)
            {
                OnHealthChanged?.Invoke(_systemHealth);

                if (_enableLogging)
                    ChimeraLogger.Log("MEMORY", $"System health changed: {previousHealth} -> {_systemHealth}", this);
            }
        }

        /// <summary>
        /// Determine overall system health
        /// </summary>
        private MemorySystemHealth DetermineSystemHealth()
        {
            var alertLevel = _alertManager?.CurrentAlertLevel ?? MemoryAlertLevel.Normal;
            var gcImpact = _gcAnalyzer?.GetGCPerformanceImpact() ?? GCPerformanceImpact.Low;

            if (alertLevel == MemoryAlertLevel.Emergency)
                return MemorySystemHealth.Emergency;
            else if (alertLevel == MemoryAlertLevel.Critical || gcImpact == GCPerformanceImpact.Critical)
                return MemorySystemHealth.Critical;
            else if (alertLevel == MemoryAlertLevel.Warning || gcImpact >= GCPerformanceImpact.High)
                return MemorySystemHealth.Warning;
            else
                return MemorySystemHealth.Healthy;
        }

        /// <summary>
        /// Update coordination statistics
        /// </summary>
        private void UpdateStatistics()
        {
            _stats.SystemHealth = _systemHealth;
            _stats.CurrentMemoryMB = CurrentMemoryUsage / (1024f * 1024f);
            _stats.SnapshotCount = _memoryMonitor?.SnapshotCount ?? 0;
            _stats.GCEventCount = _gcAnalyzer?.GCEventCount ?? 0;
            _stats.AlertCount = _alertManager?.GetStats().TotalAlerts ?? 0;
        }

        /// <summary>
        /// Capture memory snapshot
        /// </summary>
        public void CaptureMemorySnapshot()
        {
            _memoryMonitor?.CaptureMemorySnapshot();
        }

        /// <summary>
        /// Record memory allocation
        /// </summary>
        public void RecordAllocation(string category, long bytes)
        {
            _memoryMonitor?.RecordAllocation(category, bytes);
        }

        /// <summary>
        /// Force GC analysis
        /// </summary>
        public void ForceGCAnalysis()
        {
            _gcAnalyzer?.ForceGCAnalysis();
        }

        /// <summary>
        /// Force alert check
        /// </summary>
        public void ForceAlertCheck()
        {
            _alertManager?.ForceAlertCheck();
        }

        /// <summary>
        /// Get memory history
        /// </summary>
        public MemorySnapshot[] GetMemoryHistory()
        {
            return _memoryMonitor?.GetMemoryHistory() ?? new MemorySnapshot[0];
        }

        /// <summary>
        /// Get GC history
        /// </summary>
        public GCSnapshot[] GetGCHistory()
        {
            return _gcAnalyzer?.GetGCHistory(0f, float.MaxValue) ?? new GCSnapshot[0];
        }

        /// <summary>
        /// Get memory pressure level
        /// </summary>
        public MemoryPressureLevel GetMemoryPressureLevel()
        {
            return _alertManager?.GetMemoryPressureLevel() ?? MemoryPressureLevel.Normal;
        }

        /// <summary>
        /// Get GC performance impact
        /// </summary>
        public GCPerformanceImpact GetGCPerformanceImpact()
        {
            return _gcAnalyzer?.GetGCPerformanceImpact() ?? GCPerformanceImpact.Low;
        }

        /// <summary>
        /// Clear all history
        /// </summary>
        public void ClearHistory()
        {
            _memoryMonitor?.ClearHistory();
            _gcAnalyzer?.ClearGCHistory();

            if (_enableLogging)
                ChimeraLogger.Log("MEMORY", "All memory history cleared", this);
        }

        /// <summary>
        /// Set system enabled/disabled
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;

            // Update all subsystems
            _memoryMonitor?.SetEnabled(enabled);
            _gcAnalyzer?.SetEnabled(enabled);
            _alertManager?.SetEnabled(enabled);

            if (_enableLogging)
                ChimeraLogger.Log("MEMORY", $"MemoryCore: {(enabled ? "enabled" : "disabled")}", this);
        }

        #region Event Handlers

        private void HandleMemorySnapshotCaptured(MemorySnapshot snapshot)
        {
            OnMemorySnapshotCaptured?.Invoke(snapshot);
        }

        private void HandleGCEventDetected(GCSnapshot gcSnapshot)
        {
            OnGCEventDetected?.Invoke(gcSnapshot);
        }

        private void HandleGCPerformanceImpact(GCPerformanceImpact impact)
        {
            if (impact >= GCPerformanceImpact.High && _enableLogging)
                ChimeraLogger.LogWarning("MEMORY", $"High GC performance impact detected: {impact}", this);
        }

        private void HandleMemoryAlert(MemoryAlert alert)
        {
            OnMemoryAlert?.Invoke(alert);
        }

        private void HandleAlertLevelChanged(MemoryAlertLevel previousLevel, MemoryAlertLevel currentLevel)
        {
            if (_enableLogging)
                ChimeraLogger.Log("MEMORY", $"Memory alert level changed: {previousLevel} -> {currentLevel}", this);
        }

        private void HandleEmergencyMemoryCondition()
        {
            if (_enableLogging)
                ChimeraLogger.LogError("MEMORY", "EMERGENCY: Critical memory condition detected!", this);

            // Emergency response could be implemented here
            // For example: force GC, reduce quality settings, etc.
        }

        #endregion

        private void OnDestroy()
        {
            UpdateOrchestrator.Instance?.UnregisterTickable(this);
        }
    }

    #region Data Structures

    /// <summary>
    /// Memory system health enumeration
    /// </summary>
    public enum MemorySystemHealth
    {
        Healthy,
        Warning,
        Critical,
        Emergency,
        Failed
    }

    /// <summary>
    /// Memory profiler statistics (legacy compatibility)
    /// </summary>
    [System.Serializable]
    public struct MemoryProfilerStats
    {
        public MemorySystemHealth SystemHealth;
        public float CurrentMemoryMB;
        public int SnapshotCount;
        public int GCEventCount;
        public int AlertCount;
    }

    #endregion
}