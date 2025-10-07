using UnityEngine;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;

namespace ProjectChimera.Core.Foundation.Recovery
{
    /// <summary>
    /// REFACTORED: Foundation Recovery Core - Central coordination for foundation recovery subsystems
    /// Coordinates recovery strategies, queue processing, history tracking, and health monitoring
    /// Single Responsibility: Central recovery system coordination
    /// </summary>
    public class FoundationRecoveryCore : MonoBehaviour, ITickable
    {
        [Header("Recovery Core Settings")]
        [SerializeField] private bool _enableRecovery = true;
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private float _updateInterval = 1f;

        // Subsystem references
        private FoundationRecoveryStrategy _recoveryStrategy;
        private FoundationRecoveryQueue _recoveryQueue;
        private FoundationRecoveryHistory _recoveryHistory;
        private FoundationRecoveryMonitor _recoveryMonitor;
        private FoundationRecoveryStatistics _recoveryStatistics;

        // Timing
        private float _lastUpdate;

        // Properties
        public bool IsEnabled { get; private set; } = true;
        public bool IsRecoveryActive => _recoveryQueue?.HasActiveRecoveries ?? false;

        // Events
        public System.Action<string, RecoveryStrategy> OnRecoveryStarted;
        public System.Action<string, bool> OnRecoveryCompleted;
        public System.Action<string, string> OnRecoveryFailed;

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

            if (_enableLogging)
                ChimeraLogger.Log("FOUNDATION", "🔧 FoundationRecoveryCore initialized", this);
        }

        /// <summary>
        /// Initialize all recovery subsystems
        /// </summary>
        private void InitializeSubsystems()
        {
            // Get or create subsystem components
            _recoveryStrategy = GetOrCreateComponent<FoundationRecoveryStrategy>();
            _recoveryQueue = GetOrCreateComponent<FoundationRecoveryQueue>();
            _recoveryHistory = GetOrCreateComponent<FoundationRecoveryHistory>();
            _recoveryMonitor = GetOrCreateComponent<FoundationRecoveryMonitor>();
            _recoveryStatistics = GetOrCreateComponent<FoundationRecoveryStatistics>();

            // Configure subsystems
            _recoveryStrategy?.SetEnabled(_enableRecovery);
            _recoveryQueue?.SetEnabled(_enableRecovery);
            _recoveryHistory?.SetEnabled(_enableRecovery);
            _recoveryMonitor?.SetEnabled(_enableRecovery);
            _recoveryStatistics?.SetEnabled(_enableRecovery);

            // Connect event handlers
            ConnectEventHandlers();
        }

        /// <summary>
        /// Connect inter-subsystem event handlers
        /// </summary>
        private void ConnectEventHandlers()
        {
            if (_recoveryMonitor != null)
            {
                _recoveryMonitor.OnHealthAlert += HandleHealthAlert;
                _recoveryMonitor.OnCriticalFailure += HandleCriticalFailure;
            }

            if (_recoveryQueue != null)
            {
                _recoveryQueue.OnRecoveryQueued += HandleRecoveryQueued;
            }

            if (_recoveryStrategy != null)
            {
                _recoveryStrategy.OnRecoveryStarted += HandleRecoveryStarted;
                _recoveryStrategy.OnRecoveryCompleted += HandleRecoveryCompleted;
                _recoveryStrategy.OnRecoveryFailed += HandleRecoveryFailed;
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
            if (!IsEnabled || !_enableRecovery) return;

            if (Time.time - _lastUpdate < _updateInterval) return;

            // Coordinate subsystem updates
            ProcessRecoveryCoordination();

            _lastUpdate = Time.time;
        }

        /// <summary>
        /// Coordinate recovery operations across subsystems
        /// </summary>
        private void ProcessRecoveryCoordination()
        {
            // Update recovery monitoring
            _recoveryMonitor?.ProcessHealthChecks();

            // Process recovery queue
            _recoveryQueue?.ProcessRecoveryQueue();

            // Update recovery statistics
            _recoveryStatistics?.UpdateStatistics();

            // Check for systems needing attention
            CheckForSystemsNeedingRecovery();
        }

        /// <summary>
        /// Check for systems that need recovery attention
        /// </summary>
        private void CheckForSystemsNeedingRecovery()
        {
            if (_recoveryMonitor == null || _recoveryQueue == null) return;

            var criticalSystems = _recoveryMonitor.GetCriticalSystems();
            foreach (var systemName in criticalSystems)
            {
                if (!_recoveryQueue.IsSystemInQueue(systemName) &&
                    !_recoveryQueue.IsSystemUnderRecovery(systemName))
                {
                    _recoveryQueue.QueueRecovery(systemName, RecoveryTrigger.HealthCheck);
                }
            }
        }

        /// <summary>
        /// Trigger manual recovery for specific system
        /// </summary>
        public bool TriggerRecovery(string systemName)
        {
            if (!IsEnabled || _recoveryQueue == null)
                return false;

            return _recoveryQueue.QueueRecovery(systemName, RecoveryTrigger.Manual);
        }

        /// <summary>
        /// Get recovery status for system
        /// </summary>
        public RecoveryStatus GetRecoveryStatus(string systemName)
        {
            return _recoveryQueue?.GetRecoveryStatus(systemName) ?? RecoveryStatus.None;
        }

        /// <summary>
        /// Get recovery history for system
        /// </summary>
        public RecoveryAttempt[] GetRecoveryHistory(string systemName)
        {
            return _recoveryHistory?.GetRecoveryHistory(systemName) ?? new RecoveryAttempt[0];
        }

        /// <summary>
        /// Get recovery statistics
        /// </summary>
        public RecoveryManagerStats GetRecoveryStats()
        {
            return _recoveryStatistics?.GetStats() ?? new RecoveryManagerStats();
        }

        /// <summary>
        /// Set system enabled/disabled
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;

            // Update all subsystems
            _recoveryStrategy?.SetEnabled(enabled);
            _recoveryQueue?.SetEnabled(enabled);
            _recoveryHistory?.SetEnabled(enabled);
            _recoveryMonitor?.SetEnabled(enabled);
            _recoveryStatistics?.SetEnabled(enabled);

            if (_enableLogging)
                ChimeraLogger.Log("FOUNDATION", $"FoundationRecoveryCore: {(enabled ? "enabled" : "disabled")}", this);
        }

        #region Event Handlers

        private void HandleHealthAlert(string systemName, SystemHealth health)
        {
            if (_recoveryQueue != null && (health == SystemHealth.Critical || health == SystemHealth.Failed))
            {
                _recoveryQueue.QueueRecovery(systemName, RecoveryTrigger.HealthAlert);
            }
        }

        private void HandleCriticalFailure(string systemName)
        {
            if (_recoveryQueue != null)
            {
                _recoveryQueue.QueueRecovery(systemName, RecoveryTrigger.CriticalFailure);
            }
        }

        private void HandleRecoveryQueued(string systemName, RecoveryTrigger trigger)
        {
            if (_recoveryStrategy != null)
            {
                _recoveryStrategy.StartRecovery(systemName, trigger);
            }
        }

        private void HandleRecoveryStarted(string systemName, RecoveryStrategy strategy)
        {
            _recoveryHistory?.RecordRecoveryStart(systemName, strategy);
            OnRecoveryStarted?.Invoke(systemName, strategy);
        }

        private void HandleRecoveryCompleted(string systemName, bool success, RecoveryStrategy strategy, float duration)
        {
            _recoveryHistory?.RecordRecoveryCompletion(systemName, strategy, success, duration, null);
            _recoveryStatistics?.RecordRecoveryResult(success);
            OnRecoveryCompleted?.Invoke(systemName, success);
        }

        private void HandleRecoveryFailed(string systemName, RecoveryStrategy strategy, string errorMessage)
        {
            _recoveryHistory?.RecordRecoveryCompletion(systemName, strategy, false, 0f, errorMessage);
            _recoveryStatistics?.RecordRecoveryResult(false);
            OnRecoveryFailed?.Invoke(systemName, errorMessage);
        }

        #endregion

        private void OnDestroy()
        {
            UpdateOrchestrator.Instance?.UnregisterTickable(this);
        }
    }
}
