using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core.Foundation.Performance
{
    /// <summary>
    /// REFACTORED: Optimization Request Queue
    /// Single Responsibility: Optimization request queuing, prioritization, and processing coordination
    /// Extracted from FoundationPerformanceOptimizer for better separation of concerns
    /// </summary>
    public class OptimizationRequestQueue : MonoBehaviour
    {
        [Header("Queue Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private int _maxQueueSize = 100;
        [SerializeField] private float _requestTimeoutSeconds = 300f; // 5 minutes
        [SerializeField] private bool _enablePriorityProcessing = true;

        // Request storage
        private readonly Queue<OptimizationRequest> _requestQueue = new Queue<OptimizationRequest>();
        private readonly Dictionary<string, OptimizationRequest> _activeRequests = new Dictionary<string, OptimizationRequest>();
        private readonly List<OptimizationRequest> _completedRequests = new List<OptimizationRequest>();

        // State tracking
        private bool _isInitialized = false;
        private float _lastCleanupTime;
        private readonly float _cleanupInterval = 60f; // Clean up every minute

        // Statistics
        private OptimizationQueueStats _stats = new OptimizationQueueStats();

        // Events
        public event System.Action<OptimizationRequest> OnRequestQueued;
        public event System.Action<OptimizationRequest> OnRequestDequeued;
        public event System.Action<OptimizationRequest> OnRequestCompleted;
        public event System.Action<OptimizationRequest> OnRequestTimedOut;

        public bool IsInitialized => _isInitialized;
        public OptimizationQueueStats Stats => _stats;
        public int QueuedRequestCount => _requestQueue.Count;
        public int ActiveRequestCount => _activeRequests.Count;

        public void Initialize()
        {
            if (_isInitialized) return;

            _requestQueue.Clear();
            _activeRequests.Clear();
            _completedRequests.Clear();
            ResetStats();

            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("FOUNDATION", "Optimization Request Queue initialized", this);
            }
        }

        /// <summary>
        /// Queue optimization request
        /// </summary>
        public bool QueueRequest(OptimizationRequest request)
        {
            if (!_isInitialized)
            {
                if (_enableLogging)
                {
                    ChimeraLogger.LogWarning("FOUNDATION", "Cannot queue request - queue not initialized", this);
                }
                return false;
            }

            // OptimizationRequest is a struct; validate mandatory fields instead of null
            if (string.IsNullOrEmpty(request.SystemName))
            {
                if (_enableLogging)
                {
                    ChimeraLogger.LogWarning("FOUNDATION", "Cannot queue request with empty system name", this);
                }
                return false;
            }

            if (_requestQueue.Count >= _maxQueueSize)
            {
                if (_enableLogging)
                {
                    ChimeraLogger.LogWarning("FOUNDATION", $"Queue at maximum capacity ({_maxQueueSize})", this);
                }
                _stats.RequestsRejected++;
                return false;
            }

            // Check for duplicate requests
            if (HasPendingRequest(request.SystemName))
            {
                if (_enableLogging)
                {
                    ChimeraLogger.LogWarning("FOUNDATION", $"Request for {request.SystemName} already pending", this);
                }
                return false;
            }

            // Assign ID and timestamp
            request.RequestId = System.Guid.NewGuid().ToString();
            request.RequestTime = Time.time;

            _requestQueue.Enqueue(request);
            _stats.RequestsQueued++;
            _stats.LastQueueTime = Time.time;

            OnRequestQueued?.Invoke(request);

            if (_enableLogging)
            {
                ChimeraLogger.Log("FOUNDATION", $"Queued {request.Priority} optimization request for {request.SystemName} ({_requestQueue.Count} in queue)", this);
            }

            return true;
        }

        /// <summary>
        /// Get next request for processing
        /// </summary>
        public OptimizationRequest? GetNextRequest()
        {
            if (!_isInitialized || _requestQueue.Count == 0)
            {
                return null;
            }

            OptimizationRequest nextRequest;

            if (_enablePriorityProcessing)
            {
                nextRequest = GetHighestPriorityRequest();
            }
            else
            {
                nextRequest = _requestQueue.Dequeue();
            }

            if (!string.IsNullOrEmpty(nextRequest.SystemName))
            {
                // Move to active and mark processing start
                var active = nextRequest;
                active.ProcessingStartTime = Time.time;
                _activeRequests[active.RequestId] = active;

                _stats.RequestsProcessed++;
                OnRequestDequeued?.Invoke(_activeRequests[active.RequestId]);

                if (_enableLogging)
                {
                    ChimeraLogger.Log("FOUNDATION", $"Dequeued {nextRequest.Priority} request for {nextRequest.SystemName} ({_requestQueue.Count} remaining)", this);
                }
            }

            return nextRequest;
        }

        /// <summary>
        /// Mark request as completed
        /// </summary>
        public bool CompleteRequest(string requestId, bool success)
        {
            if (!_activeRequests.TryGetValue(requestId, out var request))
            {
                if (_enableLogging)
                {
                    ChimeraLogger.LogWarning("FOUNDATION", $"Request {requestId} not found in active requests", this);
                }
                return false;
            }

            _activeRequests.Remove(requestId);
            var completed = request;
            completed.CompletionTime = Time.time;
            completed.Success = success;
            completed.ProcessingDuration = completed.CompletionTime - completed.ProcessingStartTime;
            _completedRequests.Add(completed);

            if (success)
            {
                _stats.RequestsCompleted++;
            }
            else
            {
                _stats.RequestsFailed++;
            }

            OnRequestCompleted?.Invoke(completed);

            if (_enableLogging)
            {
                var result = success ? "completed" : "failed";
                ChimeraLogger.Log("FOUNDATION", $"Request for {request.SystemName} {result} in {completed.ProcessingDuration:F2}s", this);
            }

            return true;
        }

        /// <summary>
        /// Process queue maintenance (call periodically)
        /// </summary>
        public void ProcessQueueMaintenance()
        {
            if (!_isInitialized) return;

            if (Time.time - _lastCleanupTime >= _cleanupInterval)
            {
                CleanupTimedOutRequests();
                CleanupCompletedRequests();
                _lastCleanupTime = Time.time;
            }
        }

        /// <summary>
        /// Check if system has pending optimization request
        /// </summary>
        public bool HasPendingRequest(string systemName)
        {
            // Check queue
            foreach (var request in _requestQueue)
            {
                if (request.SystemName == systemName)
                    return true;
            }

            // Check active requests
            foreach (var request in _activeRequests.Values)
            {
                if (request.SystemName == systemName)
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Get pending requests for a system
        /// </summary>
        public List<OptimizationRequest> GetPendingRequests(string systemName)
        {
            var pending = new List<OptimizationRequest>();

            foreach (var request in _requestQueue)
            {
                if (request.SystemName == systemName)
                {
                    pending.Add(request);
                }
            }

            foreach (var request in _activeRequests.Values)
            {
                if (request.SystemName == systemName)
                {
                    pending.Add(request);
                }
            }

            return pending;
        }

        /// <summary>
        /// Get highest priority request from queue
        /// </summary>
        private OptimizationRequest GetHighestPriorityRequest()
        {
            var allRequests = _requestQueue.ToArray();
            _requestQueue.Clear();

            // Sort by priority (Critical = 0, High = 1, etc.)
            var sortedRequests = allRequests
                .OrderBy(r => (int)r.Priority)
                .ThenBy(r => r.RequestTime)
                .ToArray();

            var highestPriority = sortedRequests[0];

            // Re-queue the remaining requests
            for (int i = 1; i < sortedRequests.Length; i++)
            {
                _requestQueue.Enqueue(sortedRequests[i]);
            }

            return highestPriority;
        }

        /// <summary>
        /// Clean up timed out requests
        /// </summary>
        private void CleanupTimedOutRequests()
        {
            var currentTime = Time.time;
            var timedOutRequests = new List<OptimizationRequest>();

            foreach (var kvp in _activeRequests)
            {
                var req = kvp.Value;
                if (currentTime - req.ProcessingStartTime > _requestTimeoutSeconds)
                {
                    timedOutRequests.Add(req);
                }
            }

            foreach (var req in timedOutRequests)
            {
                _activeRequests.Remove(req.RequestId);
                var completed = req;
                completed.CompletionTime = currentTime;
                completed.Success = false;
                completed.ProcessingDuration = completed.CompletionTime - completed.ProcessingStartTime;
                _completedRequests.Add(completed);

                _stats.RequestsTimedOut++;
                OnRequestTimedOut?.Invoke(completed);

                if (_enableLogging)
                {
                    ChimeraLogger.LogWarning("FOUNDATION", $"Request for {completed.SystemName} timed out after {completed.ProcessingDuration:F1}s", this);
                }
            }
        }

        /// <summary>
        /// Clean up old completed requests
        /// </summary>
        private void CleanupCompletedRequests()
        {
            var currentTime = Time.time;
            var cutoffTime = currentTime - (_requestTimeoutSeconds * 2); // Keep completed requests for double the timeout

            var toRemove = _completedRequests.Where(r => r.CompletionTime < cutoffTime).ToList();

            foreach (var request in toRemove)
            {
                _completedRequests.Remove(request);
            }

            if (toRemove.Count > 0 && _enableLogging)
            {
                ChimeraLogger.Log("FOUNDATION", $"Cleaned up {toRemove.Count} old completed requests", this);
            }
        }

        /// <summary>
        /// Clear all requests
        /// </summary>
        public void ClearAllRequests()
        {
            var totalCleared = _requestQueue.Count + _activeRequests.Count;

            _requestQueue.Clear();
            _activeRequests.Clear();
            _completedRequests.Clear();

            if (_enableLogging && totalCleared > 0)
            {
                ChimeraLogger.Log("FOUNDATION", $"Cleared {totalCleared} requests from queue", this);
            }
        }

        /// <summary>
        /// Set maximum queue size
        /// </summary>
        public void SetMaxQueueSize(int maxSize)
        {
            _maxQueueSize = Mathf.Max(1, maxSize);

            if (_enableLogging)
            {
                ChimeraLogger.Log("FOUNDATION", $"Max queue size set to {_maxQueueSize}", this);
            }
        }

        /// <summary>
        /// Enable or disable priority processing
        /// </summary>
        public void SetPriorityProcessingEnabled(bool enabled)
        {
            _enablePriorityProcessing = enabled;

            if (_enableLogging)
            {
                ChimeraLogger.Log("FOUNDATION", $"Priority processing {(enabled ? "enabled" : "disabled")}", this);
            }
        }

        /// <summary>
        /// Get queue status summary
        /// </summary>
        public QueueStatus GetQueueStatus()
        {
            return new QueueStatus
            {
                QueuedRequests = _requestQueue.Count,
                ActiveRequests = _activeRequests.Count,
                CompletedRequests = _completedRequests.Count,
                CriticalRequests = _requestQueue.Count(r => r.Priority == OptimizationPriority.Critical),
                HighRequests = _requestQueue.Count(r => r.Priority == OptimizationPriority.High),
                MediumRequests = _requestQueue.Count(r => r.Priority == OptimizationPriority.Medium),
                LowRequests = _requestQueue.Count(r => r.Priority == OptimizationPriority.Low)
            };
        }

        /// <summary>
        /// Reset statistics
        /// </summary>
        private void ResetStats()
        {
            _stats = new OptimizationQueueStats
            {
                RequestsQueued = 0,
                RequestsProcessed = 0,
                RequestsCompleted = 0,
                RequestsFailed = 0,
                RequestsTimedOut = 0,
                RequestsRejected = 0,
                LastQueueTime = Time.time
            };
        }

        private void OnDestroy()
        {
            ClearAllRequests();
        }
    }





    /// <summary>
    /// Queue statistics
    /// </summary>
    [System.Serializable]
    public struct OptimizationQueueStats
    {
        public int RequestsQueued;
        public int RequestsProcessed;
        public int RequestsCompleted;
        public int RequestsFailed;
        public int RequestsTimedOut;
        public int RequestsRejected;
        public float LastQueueTime;
    }

    /// <summary>
    /// Queue status summary
    /// </summary>
    [System.Serializable]
    public struct QueueStatus
    {
        public int QueuedRequests;
        public int ActiveRequests;
        public int CompletedRequests;
        public int CriticalRequests;
        public int HighRequests;
        public int MediumRequests;
        public int LowRequests;
    }
}
