using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using System.Linq;

namespace ProjectChimera.Core.Foundation.Recovery
{
    /// <summary>
    /// REFACTORED: Foundation Recovery Queue - Focused recovery queue management and processing
    /// Handles recovery request queuing, scheduling, and priority management
    /// Single Responsibility: Recovery queue management and processing
    /// </summary>
    public class FoundationRecoveryQueue : MonoBehaviour
    {
        [Header("Queue Settings")]
        [SerializeField] private bool _enableQueueProcessing = true;
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private int _maxQueueSize = 50;
        [SerializeField] private float _queueProcessInterval = 1f;

        [Header("Recovery Timing")]
        [SerializeField] private float _recoveryAttemptInterval = 30f;
        [SerializeField] private int _maxRecoveryAttempts = 3;
        [SerializeField] private float _recoveryBackoffMultiplier = 2f;
        [SerializeField] private float _maxRecoveryInterval = 300f; // 5 minutes max

        // Queue management
        private readonly Queue<RecoveryRequest> _recoveryQueue = new Queue<RecoveryRequest>();
        private readonly Dictionary<string, RecoveryData> _recoveryData = new Dictionary<string, RecoveryData>();
        private readonly HashSet<string> _systemsUnderRecovery = new HashSet<string>();

        // Timing
        private float _lastQueueProcess;

        // Properties
        public bool IsEnabled { get; private set; } = true;
        public int QueueCount => _recoveryQueue.Count;
        public bool HasActiveRecoveries => _systemsUnderRecovery.Count > 0;

        // Events
        public System.Action<string, RecoveryTrigger> OnRecoveryQueued;
        public System.Action<string, RecoveryTrigger> OnRecoveryDequeued;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (_enableLogging)
                ChimeraLogger.Log("FOUNDATION", "📋 FoundationRecoveryQueue initialized", this);
        }

        /// <summary>
        /// Process recovery queue
        /// </summary>
        public void ProcessRecoveryQueue()
        {
            if (!IsEnabled || !_enableQueueProcessing) return;

            if (Time.time - _lastQueueProcess < _queueProcessInterval) return;

            while (_recoveryQueue.Count > 0)
            {
                var request = _recoveryQueue.Peek();

                if (ShouldProcessRecoveryRequest(request))
                {
                    var processedRequest = _recoveryQueue.Dequeue();
                    ProcessRecoveryRequest(processedRequest);
                }
                else
                {
                    break; // Wait for timing conditions to be met
                }
            }

            _lastQueueProcess = Time.time;
        }

        /// <summary>
        /// Queue recovery for specific system
        /// </summary>
        public bool QueueRecovery(string systemName, RecoveryTrigger trigger)
        {
            if (!IsEnabled || string.IsNullOrEmpty(systemName)) return false;

            if (_systemsUnderRecovery.Contains(systemName))
            {
                if (_enableLogging)
                    ChimeraLogger.LogWarning("FOUNDATION", $"System already under recovery: {systemName}", this);
                return false;
            }

            if (_recoveryQueue.Count >= _maxQueueSize)
            {
                if (_enableLogging)
                    ChimeraLogger.LogWarning("FOUNDATION", $"Recovery queue full, dropping request for: {systemName}", this);
                return false;
            }

            // Check if system should be queued based on timing and attempt limits
            if (!ShouldAttemptRecovery(systemName))
            {
                if (_enableLogging)
                    ChimeraLogger.LogWarning("FOUNDATION", $"Recovery not ready for: {systemName}", this);
                return false;
            }

            var request = new RecoveryRequest
            {
                SystemName = systemName,
                Trigger = trigger,
                QueueTime = Time.time,
                Priority = GetRecoveryPriority(trigger)
            };

            _recoveryQueue.Enqueue(request);
            OnRecoveryQueued?.Invoke(systemName, trigger);

            if (_enableLogging)
                ChimeraLogger.Log("FOUNDATION", $"Queued recovery for {systemName} (trigger: {trigger})", this);

            return true;
        }

        /// <summary>
        /// Mark system as under recovery
        /// </summary>
        public void MarkSystemUnderRecovery(string systemName)
        {
            _systemsUnderRecovery.Add(systemName);

            // Update recovery data
            if (!_recoveryData.TryGetValue(systemName, out var data))
            {
                data = new RecoveryData
                {
                    SystemName = systemName,
                    FirstAttemptTime = Time.time
                };
            }

            data.AttemptCount++;
            data.LastAttemptTime = Time.time;
            data.Status = RecoveryStatus.InProgress;

            _recoveryData[systemName] = data;
        }

        /// <summary>
        /// Mark system recovery completed
        /// </summary>
        public void MarkSystemRecoveryCompleted(string systemName, bool success)
        {
            _systemsUnderRecovery.Remove(systemName);

            if (_recoveryData.TryGetValue(systemName, out var data))
            {
                data.Status = success ? RecoveryStatus.Successful : RecoveryStatus.Failed;
                data.LastCompletionTime = Time.time;
                _recoveryData[systemName] = data;
            }
        }

        /// <summary>
        /// Check if system is in queue
        /// </summary>
        public bool IsSystemInQueue(string systemName)
        {
            return _recoveryQueue.Any(r => r.SystemName == systemName);
        }

        /// <summary>
        /// Check if system is under recovery
        /// </summary>
        public bool IsSystemUnderRecovery(string systemName)
        {
            return _systemsUnderRecovery.Contains(systemName);
        }

        /// <summary>
        /// Get recovery status for system
        /// </summary>
        public RecoveryStatus GetRecoveryStatus(string systemName)
        {
            if (_systemsUnderRecovery.Contains(systemName))
                return RecoveryStatus.InProgress;

            if (_recoveryData.TryGetValue(systemName, out var data))
                return data.Status;

            return RecoveryStatus.None;
        }

        /// <summary>
        /// Get recovery data for system
        /// </summary>
        public RecoveryData GetRecoveryData(string systemName)
        {
            _recoveryData.TryGetValue(systemName, out var data);
            return data;
        }

        /// <summary>
        /// Get systems currently under recovery
        /// </summary>
        public string[] GetSystemsUnderRecovery()
        {
            return _systemsUnderRecovery.ToArray();
        }

        /// <summary>
        /// Clear recovery queue
        /// </summary>
        public void ClearQueue()
        {
            _recoveryQueue.Clear();
            _systemsUnderRecovery.Clear();
            _recoveryData.Clear();

            if (_enableLogging)
                ChimeraLogger.Log("FOUNDATION", "Recovery queue cleared", this);
        }

        /// <summary>
        /// Set system enabled/disabled
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;

            if (!enabled)
            {
                ClearQueue();
            }

            if (_enableLogging)
                ChimeraLogger.Log("FOUNDATION", $"FoundationRecoveryQueue: {(enabled ? "enabled" : "disabled")}", this);
        }

        #region Private Methods

        /// <summary>
        /// Check if recovery request should be processed
        /// </summary>
        private bool ShouldProcessRecoveryRequest(RecoveryRequest request)
        {
            return !_systemsUnderRecovery.Contains(request.SystemName);
        }

        /// <summary>
        /// Process a recovery request
        /// </summary>
        private void ProcessRecoveryRequest(RecoveryRequest request)
        {
            MarkSystemUnderRecovery(request.SystemName);
            OnRecoveryDequeued?.Invoke(request.SystemName, request.Trigger);

            if (_enableLogging)
                ChimeraLogger.Log("FOUNDATION", $"Processing recovery request for {request.SystemName}", this);
        }

        /// <summary>
        /// Check if system should attempt recovery based on timing and limits
        /// </summary>
        private bool ShouldAttemptRecovery(string systemName)
        {
            if (!_recoveryData.TryGetValue(systemName, out var data))
                return true; // First attempt

            // Check attempt limits
            if (data.AttemptCount >= _maxRecoveryAttempts)
                return false;

            // Check time since last attempt with exponential backoff
            float timeSinceLastAttempt = Time.time - data.LastAttemptTime;
            float requiredInterval = _recoveryAttemptInterval * Mathf.Pow(_recoveryBackoffMultiplier, data.AttemptCount);
            requiredInterval = Mathf.Min(requiredInterval, _maxRecoveryInterval);

            return timeSinceLastAttempt >= requiredInterval;
        }

        /// <summary>
        /// Get recovery priority based on trigger
        /// </summary>
        private int GetRecoveryPriority(RecoveryTrigger trigger)
        {
            return trigger switch
            {
                RecoveryTrigger.CriticalFailure => 1, // Highest priority
                RecoveryTrigger.HealthAlert => 2,
                RecoveryTrigger.Manual => 3,
                RecoveryTrigger.HealthCheck => 4,
                _ => 5 // Lowest priority
            };
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Recovery request structure
    /// </summary>
    [System.Serializable]
    public struct RecoveryRequest
    {
        public string SystemName;
        public RecoveryTrigger Trigger;
        public float QueueTime;
        public int Priority;
    }

    #endregion
}