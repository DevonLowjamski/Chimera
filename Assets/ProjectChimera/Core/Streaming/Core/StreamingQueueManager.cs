using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core.Streaming.Core
{
    /// <summary>
    /// REFACTORED: Streaming Queue Manager - Focused queue processing and load management
    /// Handles load/unload queues, concurrent load limiting, and async operations
    /// Single Responsibility: Queue management and concurrent load processing
    /// </summary>
    public class StreamingQueueManager : MonoBehaviour
    {
        [Header("Queue Management Settings")]
        [SerializeField] private bool _enableQueueProcessing = true;
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private int _maxConcurrentLoads = 3;
        [SerializeField] private float _loadTimeoutSeconds = 30f;

        [Header("Queue Priorities")]
        [SerializeField] private bool _prioritizeLoadQueue = true;
        [SerializeField] private int _maxProcessedPerFrame = 5;

        // Queue management
        private readonly Queue<StreamingRequest> _loadQueue = new Queue<StreamingRequest>();
        private readonly Queue<string> _unloadQueue = new Queue<string>();
        private readonly Dictionary<string, object> _loadingOperations = new Dictionary<string, object>();
        private readonly Dictionary<string, Coroutine> _loadCoroutines = new Dictionary<string, Coroutine>();

        // State tracking
        private int _currentConcurrentLoads = 0;
        private QueueManagerStats _stats = new QueueManagerStats();

        // Properties
        public bool IsEnabled { get; private set; } = true;
        public QueueManagerStats GetStats() => _stats;

        // Events
        public System.Action<string> OnAssetLoadStarted;
        public System.Action<string, object> OnAssetLoadCompleted;
        public System.Action<string> OnAssetLoadFailed;
        public System.Action<string> OnAssetUnloadStarted;
        public System.Action<string> OnAssetUnloadCompleted;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            _stats = new QueueManagerStats();

            if (_enableLogging)
                ChimeraLogger.Log("STREAMING", "⏳ StreamingQueueManager initialized", this);
        }

        /// <summary>
        /// Process both load and unload queues
        /// </summary>
        public void ProcessQueues()
        {
            if (!IsEnabled || !_enableQueueProcessing) return;

            int processedThisFrame = 0;

            // Process load queue with priority
            if (_prioritizeLoadQueue)
            {
                ProcessLoadQueue(ref processedThisFrame);
                ProcessUnloadQueue(ref processedThisFrame);
            }
            else
            {
                ProcessUnloadQueue(ref processedThisFrame);
                ProcessLoadQueue(ref processedThisFrame);
            }

            // Update statistics
            _stats.QueuedLoadRequests = _loadQueue.Count;
            _stats.QueuedUnloadRequests = _unloadQueue.Count;
            _stats.CurrentConcurrentLoads = _currentConcurrentLoads;
        }

        /// <summary>
        /// Queue asset for loading
        /// </summary>
        public bool QueueAssetLoad(string assetKey, StreamingPriority priority)
        {
            if (!_enableQueueProcessing || string.IsNullOrEmpty(assetKey))
                return false;

            // Check if already queued or loading
            if (IsAssetQueued(assetKey) || _loadingOperations.ContainsKey(assetKey))
            {
                if (_enableLogging)
                    ChimeraLogger.Log("STREAMING", $"Asset {assetKey} already queued or loading", this);
                return false;
            }

            var request = new StreamingRequest
            {
                AssetKey = assetKey,
                Priority = priority,
                RequestTime = Time.time
            };

            // Insert based on priority (higher priority first)
            if (priority == StreamingPriority.Critical)
            {
                var tempQueue = new Queue<StreamingRequest>();
                tempQueue.Enqueue(request);

                while (_loadQueue.Count > 0)
                {
                    tempQueue.Enqueue(_loadQueue.Dequeue());
                }

                while (tempQueue.Count > 0)
                {
                    _loadQueue.Enqueue(tempQueue.Dequeue());
                }
            }
            else
            {
                _loadQueue.Enqueue(request);
            }

            _stats.LoadRequests++;

            if (_enableLogging)
                ChimeraLogger.Log("STREAMING", $"Queued asset for loading: {assetKey} (Priority: {priority})", this);

            return true;
        }

        /// <summary>
        /// Queue asset for unloading
        /// </summary>
        public bool QueueAssetUnload(string assetKey)
        {
            if (!_enableQueueProcessing || string.IsNullOrEmpty(assetKey))
                return false;

            // Cancel any pending load request
            CancelLoadRequest(assetKey);

            _unloadQueue.Enqueue(assetKey);
            _stats.UnloadRequests++;

            if (_enableLogging)
                ChimeraLogger.Log("STREAMING", $"Queued asset for unloading: {assetKey}", this);

            return true;
        }

        /// <summary>
        /// Check if asset is queued for loading
        /// </summary>
        public bool IsAssetQueued(string assetKey)
        {
            foreach (var request in _loadQueue)
            {
                if (request.AssetKey == assetKey)
                    return true;
            }
            return false;
        }

        /// <summary>
        /// Cancel load request for asset
        /// </summary>
        public bool CancelLoadRequest(string assetKey)
        {
            // Remove from load queue
            var tempQueue = new Queue<StreamingRequest>();
            bool found = false;

            while (_loadQueue.Count > 0)
            {
                var request = _loadQueue.Dequeue();
                if (request.AssetKey == assetKey)
                {
                    found = true;
                    _stats.CancelledLoads++;
                }
                else
                {
                    tempQueue.Enqueue(request);
                }
            }

            while (tempQueue.Count > 0)
            {
                _loadQueue.Enqueue(tempQueue.Dequeue());
            }

            // Cancel active loading operation
            if (_loadingOperations.ContainsKey(assetKey))
            {
                if (_loadCoroutines.TryGetValue(assetKey, out var coroutine))
                {
                    StopCoroutine(coroutine);
                    _loadCoroutines.Remove(assetKey);
                }

                _loadingOperations.Remove(assetKey);
                _currentConcurrentLoads--;
                found = true;
                _stats.CancelledLoads++;

                if (_enableLogging)
                    ChimeraLogger.Log("STREAMING", $"Cancelled active load for asset: {assetKey}", this);
            }

            return found;
        }

        /// <summary>
        /// Clear all queues
        /// </summary>
        public void ClearAllQueues()
        {
            // Cancel all active loads
            foreach (var kvp in _loadCoroutines)
            {
                StopCoroutine(kvp.Value);
            }

            _loadQueue.Clear();
            _unloadQueue.Clear();
            _loadingOperations.Clear();
            _loadCoroutines.Clear();
            _currentConcurrentLoads = 0;

            if (_enableLogging)
                ChimeraLogger.Log("STREAMING", "All queues cleared", this);
        }

        /// <summary>
        /// Update concurrent load settings
        /// </summary>
        public void UpdateConcurrentLoadSettings(int maxConcurrentLoads)
        {
            _maxConcurrentLoads = maxConcurrentLoads;

            if (_enableLogging)
                ChimeraLogger.Log("STREAMING", $"Updated max concurrent loads to: {maxConcurrentLoads}", this);
        }

        /// <summary>
        /// Set system enabled/disabled
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;

            if (!enabled)
            {
                ClearAllQueues();
            }

            if (_enableLogging)
                ChimeraLogger.Log("STREAMING", $"StreamingQueueManager: {(enabled ? "enabled" : "disabled")}", this);
        }

        #region Private Methods

        /// <summary>
        /// Process load queue with concurrent load limiting
        /// </summary>
        private void ProcessLoadQueue(ref int processedThisFrame)
        {
            while (_loadQueue.Count > 0 &&
                   _currentConcurrentLoads < _maxConcurrentLoads &&
                   processedThisFrame < _maxProcessedPerFrame)
            {
                var request = _loadQueue.Dequeue();
                StartAssetLoad(request);
                processedThisFrame++;
            }
        }

        /// <summary>
        /// Process unload queue
        /// </summary>
        private void ProcessUnloadQueue(ref int processedThisFrame)
        {
            while (_unloadQueue.Count > 0 && processedThisFrame < _maxProcessedPerFrame)
            {
                string assetKey = _unloadQueue.Dequeue();
                StartAssetUnload(assetKey);
                processedThisFrame++;
            }
        }

        /// <summary>
        /// Start loading an asset
        /// </summary>
        private void StartAssetLoad(StreamingRequest request)
        {
            string assetKey = request.AssetKey;

            if (_loadingOperations.ContainsKey(assetKey))
                return;

            OnAssetLoadStarted?.Invoke(assetKey);

            // Start load coroutine with timeout
            var coroutine = StartCoroutine(LoadAssetCoroutine(request));
            _loadCoroutines[assetKey] = coroutine;
            _loadingOperations[assetKey] = null; // Placeholder
            _currentConcurrentLoads++;

            if (_enableLogging)
                ChimeraLogger.Log("STREAMING", $"Started loading asset: {assetKey}", this);
        }

        /// <summary>
        /// Start unloading an asset
        /// </summary>
        private void StartAssetUnload(string assetKey)
        {
            OnAssetUnloadStarted?.Invoke(assetKey);

            // Simulate immediate unload (in real implementation, would handle Addressables)
            StartCoroutine(UnloadAssetCoroutine(assetKey));

            if (_enableLogging)
                ChimeraLogger.Log("STREAMING", $"Started unloading asset: {assetKey}", this);
        }

        /// <summary>
        /// Coroutine for asset loading with timeout
        /// </summary>
        private IEnumerator LoadAssetCoroutine(StreamingRequest request)
        {
            string assetKey = request.AssetKey;
            float startTime = Time.time;

            // Simulate loading time (in real implementation, would use Addressables)
            yield return new WaitForSeconds(UnityEngine.Random.Range(0.1f, 0.5f));

            // Check for timeout before proceeding
            if (Time.time - startTime > _loadTimeoutSeconds)
            {
                HandleAssetLoadFailed(assetKey, "Load timeout");
                CleanupLoadOperation(assetKey);
                yield break;
            }

            try
            {
                // Simulate load completion (in real implementation, would have actual asset handle)
                object handle = new object(); // Placeholder handle

                OnAssetLoadCompleted?.Invoke(assetKey, handle);
                _stats.SuccessfulLoads++;
            }
            catch (System.Exception ex)
            {
                HandleAssetLoadFailed(assetKey, ex.Message);
            }

            CleanupLoadOperation(assetKey);
        }

        /// <summary>
        /// Coroutine for asset unloading
        /// </summary>
        private IEnumerator UnloadAssetCoroutine(string assetKey)
        {
            // Simulate unload time
            yield return new WaitForSeconds(0.1f);

            OnAssetUnloadCompleted?.Invoke(assetKey);
            _stats.SuccessfulUnloads++;

            if (_enableLogging)
                ChimeraLogger.Log("STREAMING", $"Completed unloading asset: {assetKey}", this);
        }

        /// <summary>
        /// Handle load failure
        /// </summary>
        private void HandleAssetLoadFailed(string assetKey, string error)
        {
            OnAssetLoadFailed?.Invoke(assetKey);
            _stats.FailedLoads++;

            if (_enableLogging)
                ChimeraLogger.LogError("STREAMING", $"Failed to load asset {assetKey}: {error}", this);

            CleanupLoadOperation(assetKey);
        }

        /// <summary>
        /// Cleanup load operation tracking
        /// </summary>
        private void CleanupLoadOperation(string assetKey)
        {
            _loadingOperations.Remove(assetKey);
            _loadCoroutines.Remove(assetKey);
            _currentConcurrentLoads--;
        }

        #endregion
    }

    /// <summary>
    /// Queue manager statistics
    /// </summary>
    [System.Serializable]
    public struct QueueManagerStats
    {
        public int LoadRequests;
        public int UnloadRequests;
        public int SuccessfulLoads;
        public int FailedLoads;
        public int SuccessfulUnloads;
        public int CancelledLoads;
        public int QueuedLoadRequests;
        public int QueuedUnloadRequests;
        public int CurrentConcurrentLoads;
    }
}
