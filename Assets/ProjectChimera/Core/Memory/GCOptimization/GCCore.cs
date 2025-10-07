using UnityEngine;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;
using Logger = ProjectChimera.Core.Logging.ChimeraLogger;

namespace ProjectChimera.Core.Memory.GCOptimization
{
    /// <summary>
    /// REFACTORED: GC Core - Central coordination for GC optimization subsystems
    /// Coordinates GC strategy management, memory pressure monitoring, idle detection, and execution
    /// Single Responsibility: Central GC optimization system coordination
    /// </summary>
    public class GCCore : MonoBehaviour, ITickable
    {
        [Header("Core Settings")]
        [SerializeField] private bool _enableGCOptimization = true;
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private float _updateInterval = 1f;

        // Subsystem references
        private GCStrategyManager _strategyManager;
        private GCMemoryPressureMonitor _pressureMonitor;
        private GCIdleStateMonitor _idleStateMonitor;
        private GCExecutor _gcExecutor;

        // Timing
        private float _lastUpdate;

        // System health
        private GCOptimizationHealth _systemHealth = GCOptimizationHealth.Healthy;
        private GCOptimizationStats _stats = new GCOptimizationStats();

        // Properties
        public bool IsEnabled { get; private set; } = true;
        public GCOptimizationHealth SystemHealth => _systemHealth;
        public GCOptimizationStats Stats => _stats;

        // Events for backward compatibility
        public System.Action<GCResult> OnGCCompleted;
        public System.Action<GCOptimizationHealth> OnHealthChanged;
        public System.Action OnIdleStateChanged;

        public int TickPriority => 100;
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
                Logger.Log("GC", "⚡ GCCore initialized", this);
        }

        /// <summary>
        /// Initialize all GC optimization subsystems
        /// </summary>
        private void InitializeSubsystems()
        {
            // Get or create subsystem components
            _strategyManager = GetOrCreateComponent<GCStrategyManager>();
            _pressureMonitor = GetOrCreateComponent<GCMemoryPressureMonitor>();
            _idleStateMonitor = GetOrCreateComponent<GCIdleStateMonitor>();
            _gcExecutor = GetOrCreateComponent<GCExecutor>();

            // Configure subsystems
            _strategyManager?.SetEnabled(_enableGCOptimization);
            _pressureMonitor?.SetEnabled(_enableGCOptimization);
            _idleStateMonitor?.SetEnabled(_enableGCOptimization);
            _gcExecutor?.SetEnabled(_enableGCOptimization);
        }

        /// <summary>
        /// Connect event handlers between subsystems
        /// </summary>
        private void ConnectEventHandlers()
        {
            if (_strategyManager != null)
            {
                _strategyManager.OnStrategyChanged += HandleStrategyChanged;
                _strategyManager.OnGCRecommended += HandleGCRecommended;
            }

            if (_pressureMonitor != null)
            {
                _pressureMonitor.OnMemoryPressureChanged += HandleMemoryPressureChanged;
                _pressureMonitor.OnHighAllocationRate += HandleHighAllocationRate;
                _pressureMonitor.OnPressureLevelChanged += HandlePressureLevelChanged;
            }

            if (_idleStateMonitor != null)
            {
                _idleStateMonitor.OnIdleStateEntered += HandleIdleStateEntered;
                _idleStateMonitor.OnIdleStateExited += HandleIdleStateExited;
                _idleStateMonitor.OnSceneTransitionStarted += HandleSceneTransitionStarted;
                _idleStateMonitor.OnSceneTransitionEnded += HandleSceneTransitionEnded;
            }

            if (_gcExecutor != null)
            {
                _gcExecutor.OnGCCompleted += HandleGCCompleted;
                _gcExecutor.OnGCQueued += HandleGCQueued;
                _gcExecutor.OnGCSkipped += HandleGCSkipped;
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
            if (!IsEnabled || !_enableGCOptimization) return;

            if (Time.time - _lastUpdate < _updateInterval) return;

            ProcessGCCoordination();
            UpdateSystemHealth();
            UpdateStatistics();

            _lastUpdate = Time.time;
        }

        /// <summary>
        /// Process GC optimization coordination
        /// </summary>
        private void ProcessGCCoordination()
        {
            // Update all subsystems
            _pressureMonitor?.UpdateMonitoring();
            _idleStateMonitor?.UpdateIdleMonitoring();
            _gcExecutor?.ProcessGCQueue();

            // Evaluate GC needs based on subsystem state
            EvaluateGCNeeds();
        }

        /// <summary>
        /// Evaluate if GC is needed and trigger if appropriate
        /// </summary>
        private void EvaluateGCNeeds()
        {
            if (_strategyManager == null || _pressureMonitor == null) return;

            var pressureInfo = _pressureMonitor.GetPressureInfo();
            var decision = _strategyManager.EvaluateGCNeed(
                pressureInfo.CurrentPressure,
                pressureInfo.AllocationRate,
                (long)(pressureInfo.TotalMemoryMB * 1024 * 1024)
            );

            if (decision.ShouldPerformGC && _gcExecutor?.CanExecuteGC == true)
            {
                var idleInfo = _idleStateMonitor?.GetIdleStateInfo();
                var context = new GCContext
                {
                    Type = DetermineGCTriggerType(decision, idleInfo ?? new IdleStateInfo()),
                    MemoryPressure = pressureInfo.CurrentPressure,
                    AllocationRate = pressureInfo.AllocationRate,
                    IsIdle = idleInfo?.IsIdle ?? false,
                    IsSceneTransition = idleInfo?.IsSceneTransitioning ?? false
                };

                var mode = _strategyManager.GetRecommendedGCMode(context);

                if (context.IsIdle || context.IsSceneTransition)
                {
                    // Queue for delayed execution during idle/transitions
                    _gcExecutor.QueueGC(mode, decision.Reason, context.Type, decision.Priority);
                }
                else if (decision.Priority >= GCPriority.High)
                {
                    // Execute immediately for high priority
                    _gcExecutor.ExecuteGC(mode, decision.Reason, decision.Priority);
                }
                else
                {
                    // Queue with small delay to avoid interrupting gameplay
                    _gcExecutor.QueueGC(mode, decision.Reason, context.Type, decision.Priority);
                }
            }
        }

        /// <summary>
        /// Determine GC trigger type from decision and state
        /// </summary>
        private GCTriggerType DetermineGCTriggerType(GCDecision decision, IdleStateInfo idleInfo)
        {
            if (idleInfo.IsIdle)
                return GCTriggerType.Idle;
            else if (idleInfo.IsSceneTransitioning)
                return GCTriggerType.SceneTransition;
            else if (decision.Reason.Contains("allocation", System.StringComparison.OrdinalIgnoreCase))
                return GCTriggerType.AllocationRate;
            else
                return GCTriggerType.MemoryPressure;
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
                    Logger.Log("GC", $"System health changed: {previousHealth} -> {_systemHealth}", this);
            }
        }

        /// <summary>
        /// Determine overall system health
        /// </summary>
        private GCOptimizationHealth DetermineSystemHealth()
        {
            var pressureLevel = _pressureMonitor?.GetMemoryPressureLevel() ?? MemoryPressureLevel.Low;
            var canExecuteGC = _gcExecutor?.CanExecuteGC ?? true;

            if (pressureLevel == MemoryPressureLevel.Critical && !canExecuteGC)
                return GCOptimizationHealth.Critical;
            else if (pressureLevel == MemoryPressureLevel.High)
                return GCOptimizationHealth.Warning;
            else if (!canExecuteGC)
                return GCOptimizationHealth.Degraded;
            else
                return GCOptimizationHealth.Healthy;
        }

        /// <summary>
        /// Update coordination statistics
        /// </summary>
        private void UpdateStatistics()
        {
            var pressureInfo = _pressureMonitor?.GetPressureInfo() ?? new MemoryPressureInfo();
            var idleInfo = _idleStateMonitor?.GetIdleStateInfo() ?? new IdleStateInfo();
            var executorStats = _gcExecutor?.GetStats() ?? new GCExecutorStats();
            var strategyConfig = _strategyManager?.GetStrategyConfig() ?? new GCStrategyConfig();

            _stats.SystemHealth = _systemHealth;
            _stats.Strategy = strategyConfig.Strategy;
            _stats.CurrentMemoryPressure = pressureInfo.CurrentPressure;
            _stats.AllocationRate = pressureInfo.AllocationRate;
            _stats.IsIdle = idleInfo.IsIdle;
            _stats.IsSceneTransitioning = idleInfo.IsSceneTransitioning;
            _stats.TotalGCExecutions = executorStats.TotalExecutions;
            _stats.TotalGCTime = executorStats.TotalDuration;
            _stats.TotalMemoryFreed = executorStats.TotalMemoryFreed;
        }

        /// <summary>
        /// Public API methods for backward compatibility
        /// </summary>
        public GCResult ForceOptimizedGC(bool waitForFinalizers = true)
        {
            var mode = waitForFinalizers ? GCExecutionMode.Thorough : GCExecutionMode.Standard;
            return _gcExecutor?.ExecuteGC(mode, "Manual Force", GCPriority.High) ??
                   new GCResult { WasExecuted = false, Reason = "Executor not available" };
        }

        public float GetMemoryPressure()
        {
            return _pressureMonitor?.CurrentMemoryPressure ?? 0f;
        }

        public void SetGCStrategy(GCStrategy strategy)
        {
            _strategyManager?.SetStrategy(strategy);
        }

        public void NotifyIdle()
        {
            _idleStateMonitor?.SetIdleState(true, "External notification");
        }

        public void NotifyActive()
        {
            _idleStateMonitor?.SetIdleState(false, "External notification");
        }

        public void NotifySceneTransitionStart()
        {
            _idleStateMonitor?.NotifySceneTransitionStart();
        }

        public void NotifySceneTransitionEnd()
        {
            _idleStateMonitor?.NotifySceneTransitionEnd();
        }

        /// <summary>
        /// Set system enabled/disabled
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;

            // Update all subsystems
            _strategyManager?.SetEnabled(enabled);
            _pressureMonitor?.SetEnabled(enabled);
            _idleStateMonitor?.SetEnabled(enabled);
            _gcExecutor?.SetEnabled(enabled);

            if (_enableLogging)
                Logger.Log("GC", $"GCCore: {(enabled ? "enabled" : "disabled")}", this);
        }

        #region Event Handlers

        private void HandleStrategyChanged(GCStrategy previousStrategy, GCStrategy currentStrategy)
        {
            if (_enableLogging)
                Logger.Log("GC", $"GC strategy changed: {previousStrategy} -> {currentStrategy}", this);
        }

        private void HandleGCRecommended(string reason)
        {
            if (_enableLogging)
                Logger.Log("GC", $"GC recommended: {reason}", this);
        }

        private void HandleMemoryPressureChanged(float pressure)
        {
            if (_enableLogging && pressure > 0.8f)
                Logger.LogWarning("GC", $"High memory pressure: {pressure:P1}", this);
        }

        private void HandleHighAllocationRate(long rate)
        {
            if (_enableLogging)
                Logger.LogWarning("GC", $"High allocation rate: {rate / (1024 * 1024):F2} MB/s", this);
        }

        private void HandlePressureLevelChanged(MemoryPressureLevel level)
        {
            if (_enableLogging)
                Logger.Log("GC", $"Memory pressure level changed to: {level}", this);
        }

        private void HandleIdleStateEntered()
        {
            OnIdleStateChanged?.Invoke();

            if (_enableLogging)
                Logger.Log("GC", "Application entered idle state", this);
        }

        private void HandleIdleStateExited()
        {
            OnIdleStateChanged?.Invoke();

            if (_enableLogging)
                Logger.Log("GC", "Application exited idle state", this);
        }

        private void HandleSceneTransitionStarted()
        {
            if (_enableLogging)
                Logger.Log("GC", "Scene transition started", this);
        }

        private void HandleSceneTransitionEnded()
        {
            if (_enableLogging)
                Logger.Log("GC", "Scene transition ended", this);
        }

        private void HandleGCCompleted(GCResult result)
        {
            OnGCCompleted?.Invoke(result);
        }

        private void HandleGCQueued(GCExecutionRequest request)
        {
            if (_enableLogging)
                Logger.Log("GC", $"GC queued: {request.Reason}", this);
        }

        private void HandleGCSkipped(string reason)
        {
            if (_enableLogging)
                Logger.Log("GC", $"GC skipped: {reason}", this);
        }

        #endregion

        private void OnDestroy()
        {
            UpdateOrchestrator.Instance?.UnregisterTickable(this);
        }
    }

    #region Data Structures

    /// <summary>
    /// GC optimization system health enumeration
    /// </summary>
    public enum GCOptimizationHealth
    {
        Healthy,
        Warning,
        Degraded,
        Critical
    }

    /// <summary>
    /// GC optimization statistics (enhanced from original)
    /// </summary>
    [System.Serializable]
    public struct GCOptimizationStats
    {
        public GCOptimizationHealth SystemHealth;
        public GCStrategy Strategy;
        public float CurrentMemoryPressure;
        public long AllocationRate;
        public bool IsIdle;
        public bool IsSceneTransitioning;
        public int TotalGCExecutions;
        public float TotalGCTime;
        public long TotalMemoryFreed;
        public float AverageGCTime;
        public float LastGCTime;
    }

    #endregion
}
