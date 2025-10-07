using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core.Memory.GCOptimization
{
    /// <summary>
    /// REFACTORED: GC Executor - Focused garbage collection execution and timing management
    /// Handles GC execution, timing coordination, cooldown management, and result tracking
    /// Single Responsibility: GC execution and timing coordination
    /// </summary>
    public class GCExecutor : MonoBehaviour
    {
        [Header("Execution Settings")]
        [SerializeField] private bool _enableExecution = true;
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private float _gcCooldownPeriod = 5f; // Minimum time between GCs

        [Header("Timing Settings")]
        [SerializeField] private float _idleGCDelay = 2f; // Delay before GC during idle
        [SerializeField] private float _sceneTransitionGCDelay = 0.5f;
        [SerializeField] private float _pressureGCDelay = 0.1f; // Small delay during gameplay

        [Header("Performance Limits")]
        [SerializeField] private float _maxExecutionTimePerFrame = 0.003f; // 3ms
        [SerializeField] private int _maxConcurrentGCs = 1;

        // Execution tracking
        private float _lastGCTime;
        private readonly Queue<GCExecutionRequest> _gcQueue = new Queue<GCExecutionRequest>();
        private int _activeGCCount = 0;

        // Statistics
        private GCExecutorStats _stats = new GCExecutorStats();
        private readonly List<GCResult> _recentResults = new List<GCResult>();

        // Properties
        public bool IsEnabled { get; private set; } = true;
        public bool CanExecuteGC => CanPerformGC() && _activeGCCount < _maxConcurrentGCs;
        public float TimeSinceLastGC => Time.realtimeSinceStartup - _lastGCTime;
        public GCExecutorStats GetStats() => _stats;

        // Events
        public System.Action<GCResult> OnGCCompleted;
        public System.Action<GCExecutionRequest> OnGCQueued;
        public System.Action<string> OnGCSkipped;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            _lastGCTime = Time.realtimeSinceStartup;

            if (_enableLogging)
                ChimeraLogger.Log("GC", "⚙️ GCExecutor initialized", this);
        }

        /// <summary>
        /// Execute garbage collection immediately with specified mode
        /// </summary>
        public GCResult ExecuteGC(GCExecutionMode mode, string reason, GCPriority priority = GCPriority.Medium)
        {
            if (!IsEnabled || !_enableExecution)
            {
                return new GCResult
                {
                    WasExecuted = false,
                    Reason = "Executor disabled",
                    RequestedMode = mode
                };
            }

            if (!CanExecuteGC)
            {
                var skipReason = _activeGCCount >= _maxConcurrentGCs ? "Max concurrent GCs reached" : "Cooldown active";
                OnGCSkipped?.Invoke(skipReason);

                return new GCResult
                {
                    WasExecuted = false,
                    Reason = skipReason,
                    RequestedMode = mode
                };
            }

            return PerformGC(mode, reason, priority);
        }

        /// <summary>
        /// Queue garbage collection for delayed execution
        /// </summary>
        public void QueueGC(GCExecutionMode mode, string reason, GCTriggerType triggerType, GCPriority priority = GCPriority.Medium)
        {
            if (!IsEnabled || !_enableExecution) return;

            var delay = GetDelayForTriggerType(triggerType);
            var request = new GCExecutionRequest
            {
                Mode = mode,
                Reason = reason,
                TriggerType = triggerType,
                Priority = priority,
                QueueTime = Time.realtimeSinceStartup,
                ScheduledTime = Time.realtimeSinceStartup + delay
            };

            _gcQueue.Enqueue(request);
            OnGCQueued?.Invoke(request);

            StartCoroutine(DelayedGCCoroutine(request));

            if (_enableLogging)
                ChimeraLogger.Log("GC", $"Queued GC: {reason} (delay: {delay:F2}s)", this);
        }

        /// <summary>
        /// Process queued GC requests
        /// </summary>
        public void ProcessGCQueue()
        {
            if (!IsEnabled || !_enableExecution || _gcQueue.Count == 0) return;

            var currentTime = Time.realtimeSinceStartup;
            var processedRequests = new List<GCExecutionRequest>();

            // Process requests that are due
            foreach (var request in _gcQueue)
            {
                if (currentTime >= request.ScheduledTime && CanExecuteGC)
                {
                    var result = PerformGC(request.Mode, request.Reason, request.Priority);
                    processedRequests.Add(request);

                    // Respect frame time budget
                    if (result.Duration > _maxExecutionTimePerFrame)
                        break;
                }
            }

            // Remove processed requests
            foreach (var processed in processedRequests)
            {
                // Note: This is a simplified removal - in practice, you'd need a more efficient structure
                var tempQueue = new Queue<GCExecutionRequest>();
                while (_gcQueue.Count > 0)
                {
                    var item = _gcQueue.Dequeue();
                    if (!processedRequests.Contains(item))
                        tempQueue.Enqueue(item);
                }
                while (tempQueue.Count > 0)
                    _gcQueue.Enqueue(tempQueue.Dequeue());
            }
        }

        /// <summary>
        /// Get recent GC results
        /// </summary>
        public GCResult[] GetRecentResults(int count = 10)
        {
            var results = new GCResult[Mathf.Min(count, _recentResults.Count)];
            for (int i = 0; i < results.Length; i++)
            {
                results[i] = _recentResults[_recentResults.Count - 1 - i];
            }
            return results;
        }

        /// <summary>
        /// Clear execution history
        /// </summary>
        public void ClearHistory()
        {
            _recentResults.Clear();
            _gcQueue.Clear();
            _stats = new GCExecutorStats();

            if (_enableLogging)
                ChimeraLogger.Log("GC", "GC execution history cleared", this);
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
                ChimeraLogger.Log("GC", $"GCExecutor: {(enabled ? "enabled" : "disabled")}", this);
        }

        #region Private Methods

        /// <summary>
        /// Check if GC can be performed now
        /// </summary>
        private bool CanPerformGC()
        {
            return Time.realtimeSinceStartup - _lastGCTime >= _gcCooldownPeriod;
        }

        /// <summary>
        /// Perform garbage collection with timing
        /// </summary>
        private GCResult PerformGC(GCExecutionMode mode, string reason, GCPriority priority)
        {
            _activeGCCount++;
            var startTime = Time.realtimeSinceStartup;
            var memoryBefore = System.GC.GetTotalMemory(false);

            try
            {
                // Execute GC based on mode
                switch (mode)
                {
                    case GCExecutionMode.Fast:
                        System.GC.Collect();
                        break;

                    case GCExecutionMode.Standard:
                        System.GC.Collect();
                        System.GC.WaitForPendingFinalizers();
                        break;

                    case GCExecutionMode.Thorough:
                        System.GC.Collect();
                        System.GC.WaitForPendingFinalizers();
                        System.GC.Collect();
                        break;
                }

                var endTime = Time.realtimeSinceStartup;
                var memoryAfter = System.GC.GetTotalMemory(false);

                var result = new GCResult
                {
                    WasExecuted = true,
                    Duration = endTime - startTime,
                    MemoryFreed = memoryBefore - memoryAfter,
                    Reason = reason,
                    RequestedMode = mode,
                    Priority = priority,
                    Timestamp = endTime,
                    MemoryBefore = memoryBefore,
                    MemoryAfter = memoryAfter
                };

                ProcessGCResult(result);
                return result;
            }
            finally
            {
                _activeGCCount--;
            }
        }

        /// <summary>
        /// Process and record GC result
        /// </summary>
        private void ProcessGCResult(GCResult result)
        {
            _lastGCTime = result.Timestamp;

            // Update statistics
            _stats.TotalExecutions++;
            _stats.TotalDuration += result.Duration;
            _stats.TotalMemoryFreed += result.MemoryFreed;

            switch (result.RequestedMode)
            {
                case GCExecutionMode.Fast:
                    _stats.FastModeExecutions++;
                    break;
                case GCExecutionMode.Standard:
                    _stats.StandardModeExecutions++;
                    break;
                case GCExecutionMode.Thorough:
                    _stats.ThoroughModeExecutions++;
                    break;
            }

            // Record recent result
            _recentResults.Add(result);
            if (_recentResults.Count > 20)
                _recentResults.RemoveAt(0);

            OnGCCompleted?.Invoke(result);

            if (_enableLogging)
            {
                ChimeraLogger.Log("GC",
                    $"GC completed: {result.Reason} - Mode: {result.RequestedMode}, " +
                    $"Duration: {result.Duration * 1000:F2}ms, " +
                    $"Memory freed: {result.MemoryFreed / (1024 * 1024):F2} MB", this);
            }
        }

        /// <summary>
        /// Get delay for trigger type
        /// </summary>
        private float GetDelayForTriggerType(GCTriggerType triggerType)
        {
            switch (triggerType)
            {
                case GCTriggerType.Idle:
                    return _idleGCDelay;
                case GCTriggerType.SceneTransition:
                    return _sceneTransitionGCDelay;
                case GCTriggerType.MemoryPressure:
                    return _pressureGCDelay;
                default:
                    return 0f;
            }
        }

        /// <summary>
        /// Coroutine for delayed GC execution
        /// </summary>
        private IEnumerator DelayedGCCoroutine(GCExecutionRequest request)
        {
            var delay = request.ScheduledTime - Time.realtimeSinceStartup;
            if (delay > 0)
            {
                yield return new WaitForSecondsRealtime(delay);
            }

            if (IsEnabled && CanExecuteGC)
            {
                PerformGC(request.Mode, request.Reason, request.Priority);
            }
            else if (_enableLogging)
            {
                ChimeraLogger.Log("GC", $"Delayed GC skipped: {request.Reason}", this);
            }
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// GC execution request
    /// </summary>
    [System.Serializable]
    public struct GCExecutionRequest
    {
        public GCExecutionMode Mode;
        public string Reason;
        public GCTriggerType TriggerType;
        public GCPriority Priority;
        public float QueueTime;
        public float ScheduledTime;
    }

    /// <summary>
    /// GC execution result (enhanced from original)
    /// </summary>
    [System.Serializable]
    public struct GCResult
    {
        public bool WasExecuted;
        public float Duration;
        public long MemoryFreed;
        public string Reason;
        public GCExecutionMode RequestedMode;
        public GCPriority Priority;
        public float Timestamp;
        public long MemoryBefore;
        public long MemoryAfter;
    }

    /// <summary>
    /// GC executor statistics
    /// </summary>
    [System.Serializable]
    public struct GCExecutorStats
    {
        public int TotalExecutions;
        public float TotalDuration;
        public long TotalMemoryFreed;
        public int FastModeExecutions;
        public int StandardModeExecutions;
        public int ThoroughModeExecutions;
        public float AverageExecutionTime;
        public float LastExecutionTime;
    }

    #endregion
}