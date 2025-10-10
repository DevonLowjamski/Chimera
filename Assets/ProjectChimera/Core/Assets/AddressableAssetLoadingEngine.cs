using UnityEngine;
#if UNITY_ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core.Assets
{
#if UNITY_ADDRESSABLES
    /// <summary>
    /// REFACTORED: Addressable Asset Loading Engine
    /// Single Responsibility: Core async loading functionality, handle management, and addressable operations
    /// Extracted from AddressablesAssetManager for better separation of concerns
    /// </summary>
    public class AddressableAssetLoadingEngine
    {
        [Header("Loading Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private bool _enableParallelLoading = true;
        [SerializeField] private int _maxConcurrentLoads = 10;
        [SerializeField] private float _loadTimeoutSeconds = 30f;

        // Active handles tracking
        private Dictionary<string, AsyncOperationHandle> _activeHandles = new Dictionary<string, AsyncOperationHandle>();
        private Dictionary<string, LoadingOperation> _activeOperations = new Dictionary<string, LoadingOperation>();
        private Queue<LoadRequest> _loadQueue = new Queue<LoadRequest>();

        // Loading statistics
        private LoadingStats _stats = new LoadingStats();

        // State tracking
        private bool _isInitialized = false;
        private SemaphoreSlim _loadSemaphore;

        // Events
        public event System.Action<string, object> OnAssetLoaded;
        public event System.Action<string, string> OnLoadFailed;
        public event System.Action<LoadingStats> OnStatsUpdated;
        public event System.Action<string, float> OnLoadProgress;

        public bool IsInitialized => _isInitialized;
        public LoadingStats Stats => _stats;
        public int ActiveLoads => _activeOperations.Count;
        public int QueuedLoads => _loadQueue.Count;

        public void Initialize()
        {
            if (_isInitialized) return;

            _activeHandles.Clear();
            _activeOperations.Clear();
            _loadQueue.Clear();
            ResetStats();

            _loadSemaphore = new SemaphoreSlim(_maxConcurrentLoads, _maxConcurrentLoads);
            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("ASSETS", "Addressable Asset Loading Engine initialized");
            }
        }
        /// <summary>
        /// Load asset asynchronously with full error handling
        /// </summary>
        public async Task<T> LoadAssetAsync<T>(string address) where T : UnityEngine.Object
        {
            if (!_isInitialized)
            {
                throw new InvalidOperationException("Loading engine not initialized");
            }
            var startTime = DateTime.Now;
            _stats.LoadsAttempted++;

            try
            {
                // Wait for available slot if parallel loading is limited
                if (_enableParallelLoading)
                {
                    await _loadSemaphore.WaitAsync();
                }
                // Create loading operation
                var operation = new LoadingOperation
                {
                    Address = address,
                    StartTime = startTime,
                    IsActive = true
                };
                _activeOperations[address] = operation;
                // Perform the actual load
                var result = await PerformLoadAsync<T>(address, operation);
                var loadTime = (float)(DateTime.Now - startTime).TotalMilliseconds;
                _stats.TotalLoadTime += loadTime;
                if (result != null)
                {
                    _stats.LoadsSucceeded++;
                    OnAssetLoaded?.Invoke(address, result);
                    if (_enableLogging)
                    {
                        ChimeraLogger.Log("ASSETS", $"Loaded asset '{address}' in {loadTime:F1}ms");
                    }
                }
                else
                {
                    _stats.LoadsFailed++;
                    OnLoadFailed?.Invoke(address, "Load returned null");
                }
                OnStatsUpdated?.Invoke(_stats);
                return result;
            }
            catch (Exception ex)
            {
                _stats.LoadsFailed++;
                var errorMessage = $"Exception loading '{address}': {ex.Message}";
                OnLoadFailed?.Invoke(address, errorMessage);
                if (_enableLogging)
                {
                    ChimeraLogger.LogError("ASSETS", errorMessage);
                }
                return null;
            }
            finally
            {
                _activeOperations.Remove(address);
                if (_enableParallelLoading)
                {
                    _loadSemaphore.Release();
                }
            }
        }
        /// <summary>
        /// Load asset with cancellation token support
        /// </summary>
        public async Task<T> LoadAssetAsync<T>(string address, CancellationToken cancellationToken) where T : UnityEngine.Object
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _stats.LoadsCancelled++;
                return null;
            }

            try
            {
                var loadTask = LoadAssetAsync<T>(address);
                var cancellationTask = Task.Delay(Timeout.Infinite, cancellationToken);

                var completedTask = await Task.WhenAny(loadTask, cancellationTask);

                if (completedTask == cancellationTask)
                {
                    _stats.LoadsCancelled++;
                    CancelLoad(address);
                    return null;
                }

                return await loadTask;
            }
            catch (OperationCanceledException)
            {
                _stats.LoadsCancelled++;
                CancelLoad(address);
                return null;
            }
        }
        /// <summary>
        /// Load asset with timeout
        /// </summary>
        public async Task<T> LoadAssetWithTimeoutAsync<T>(string address, float timeoutSeconds = -1) where T : UnityEngine.Object
        {
            var timeout = timeoutSeconds > 0 ? timeoutSeconds : _loadTimeoutSeconds;

            using (var cts = new CancellationTokenSource(TimeSpan.FromSeconds(timeout)))
            {
                try
                {
                    return await LoadAssetAsync<T>(address, cts.Token);
                }
                catch (OperationCanceledException)
                {
                    _stats.LoadsTimedOut++;

                    if (_enableLogging)
                    {
                        ChimeraLogger.LogWarning("ASSETS", $"Load timeout for '{address}' after {timeout:F1}s");
                    }

                    return null;
                }
            }
        }
        /// <summary>
        /// Load multiple assets in parallel
        /// </summary>
        public async Task<T[]> LoadAssetsAsync<T>(string[] addresses) where T : UnityEngine.Object
        {
            if (addresses == null || addresses.Length == 0)
            {
                return new T[0];
            }

            var loadTasks = new Task<T>[addresses.Length];

            for (int i = 0; i < addresses.Length; i++)
            {
                loadTasks[i] = LoadAssetAsync<T>(addresses[i]);
            }

            try
            {
                var results = await Task.WhenAll(loadTasks);
                _stats.BatchLoadsCompleted++;

                return results;
            }
            catch (Exception ex)
            {
                _stats.BatchLoadsFailed++;

                if (_enableLogging)
                {
                    ChimeraLogger.LogError("ASSETS", $"Batch load failed: {ex.Message}");
                }

                return new T[0];
            }
        }
        /// <summary>
        /// Load asset with callback pattern
        /// </summary>
        public async void LoadAssetAsync<T>(string address, System.Action<T> onComplete, System.Action<string> onError = null) where T : UnityEngine.Object
        {
            try
            {
                var asset = await LoadAssetAsync<T>(address);

                if (asset != null)
                {
                    onComplete?.Invoke(asset);
                }
                else
                {
                    onError?.Invoke($"Failed to load asset '{address}'");
                }
            }
            catch (Exception ex)
            {
                onError?.Invoke(ex.Message);
            }
        }
        /// <summary>
        /// Perform the actual Addressable load operation
        /// </summary>
        private async Task<T> PerformLoadAsync<T>(string address, LoadingOperation operation) where T : UnityEngine.Object
        {
            try
            {
                // Create Addressable handle
                var handle = Addressables.LoadAssetAsync<T>(address);
                _activeHandles[address] = handle;

                // Track progress if available
                while (!handle.IsDone)
                {
                    OnLoadProgress?.Invoke(address, handle.PercentComplete);
                    await Task.Yield();
                }

                OnLoadProgress?.Invoke(address, 1.0f);

                // Check result
                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    return handle.Result;
                }
                else
                {
                    var errorMessage = handle.OperationException?.Message ?? "Unknown load error";

                    if (_enableLogging)
                    {
                        ChimeraLogger.LogError("ASSETS", $"Addressable load failed for '{address}': {errorMessage}");
                    }

                    return null;
                }
            }
            catch (Exception ex)
            {
                if (_enableLogging)
                {
                    ChimeraLogger.LogError("ASSETS", $"Exception in PerformLoadAsync for '{address}': {ex.Message}");
                }

                return null;
            }
        }
        /// <summary>
        /// Cancel ongoing load operation
        /// </summary>
        public bool CancelLoad(string address)
        {
            if (_activeHandles.TryGetValue(address, out var handle))
            {
                try
                {
                    Addressables.Release(handle);
                    _activeHandles.Remove(address);
                    _activeOperations.Remove(address);

                    _stats.LoadsCancelled++;

                    if (_enableLogging)
                    {
                        ChimeraLogger.Log("ASSETS", $"Cancelled load for '{address}'");
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    if (_enableLogging)
                    {
                        ChimeraLogger.LogError("ASSETS", $"Error cancelling load for '{address}': {ex.Message}");
                    }
                }
            }

            return false;
        }
        /// <summary>
        /// Release specific asset handle
        /// </summary>
        public bool ReleaseAsset(string address)
        {
            if (_activeHandles.TryGetValue(address, out var handle))
            {
                try
                {
                    Addressables.Release(handle);
                    _activeHandles.Remove(address);

                    _stats.AssetsReleased++;

                    if (_enableLogging)
                    {
                        ChimeraLogger.Log("ASSETS", $"Released asset handle for '{address}'");
                    }

                    return true;
                }
                catch (Exception ex)
                {
                    if (_enableLogging)
                    {
                        ChimeraLogger.LogError("ASSETS", $"Error releasing asset '{address}': {ex.Message}");
                    }
                }
            }

            return false;
        }
        /// <summary>
        /// Release all active handles
        /// </summary>
        public void ReleaseAllAssets()
        {
            var handlesCopy = new Dictionary<string, AsyncOperationHandle>(_activeHandles);
            var releasedCount = 0;

            foreach (var kvp in handlesCopy)
            {
                try
                {
                    Addressables.Release(kvp.Value);
                    _activeHandles.Remove(kvp.Key);
                    releasedCount++;
                }
                catch (Exception ex)
                {
                    if (_enableLogging)
                    {
                        ChimeraLogger.LogError("ASSETS", $"Error releasing asset '{kvp.Key}': {ex.Message}");
                    }
                }
            }

            _activeOperations.Clear();
            _stats.AssetsReleased += releasedCount;

            if (_enableLogging)
            {
                ChimeraLogger.Log("ASSETS", $"Released {releasedCount} asset handles");
            }
        }
        /// <summary>
        /// Check if asset is currently loading
        /// </summary>
        public bool IsLoading(string address)
        {
            return _activeOperations.ContainsKey(address);
        }
        /// <summary>
        /// Get loading progress for specific asset
        /// </summary>
        public float GetLoadProgress(string address)
        {
            if (_activeHandles.TryGetValue(address, out var handle))
            {
                return handle.PercentComplete;
            }

            return _activeOperations.ContainsKey(address) ? 0f : -1f;
        }
        /// <summary>
        /// Get current loading operations summary
        /// </summary>
        public LoadingSummary GetLoadingSummary()
        {
            var activeOps = new List<LoadingOperationInfo>();

            foreach (var kvp in _activeOperations)
            {
                var operation = kvp.Value;
                var progress = GetLoadProgress(kvp.Key);

                activeOps.Add(new LoadingOperationInfo
                {
                    Address = operation.Address,
                    StartTime = operation.StartTime,
                    Progress = progress,
                    ElapsedTime = (float)(DateTime.Now - operation.StartTime).TotalSeconds
                });
            }

            return new LoadingSummary
            {
                ActiveOperations = activeOps,
                QueuedOperations = _loadQueue.Count,
                Stats = _stats,
                IsInitialized = _isInitialized,
                MaxConcurrentLoads = _maxConcurrentLoads
            };
        }
        /// <summary>
        /// Set loading engine parameters
        /// </summary>
        public void SetLoadingParameters(bool enableParallel, int maxConcurrent, float timeoutSeconds)
        {
            _enableParallelLoading = enableParallel;
            _maxConcurrentLoads = Mathf.Max(1, maxConcurrent);
            _loadTimeoutSeconds = Mathf.Max(1f, timeoutSeconds);

            // Recreate semaphore with new limit
            _loadSemaphore?.Dispose();
            _loadSemaphore = new SemaphoreSlim(_maxConcurrentLoads, _maxConcurrentLoads);

            if (_enableLogging)
            {
                ChimeraLogger.Log("ASSETS", $"Loading parameters updated: Parallel={enableParallel}, Max={maxConcurrent}, Timeout={timeoutSeconds:F1}s");
            }
        }
        /// <summary>
        /// Reset loading statistics
        /// </summary>
        private void ResetStats()
        {
            _stats = new LoadingStats
            {
                LoadsAttempted = 0,
                LoadsSucceeded = 0,
                LoadsFailed = 0,
                LoadsCancelled = 0,
                LoadsTimedOut = 0,
                BatchLoadsCompleted = 0,
                BatchLoadsFailed = 0,
                AssetsReleased = 0,
                TotalLoadTime = 0f
            };
        }
        /// <summary>
        /// Clean up resources
        /// </summary>
        public void Dispose()
        {
            ReleaseAllAssets();
            _loadSemaphore?.Dispose();

            if (_enableLogging)
            {
                ChimeraLogger.Log("ASSETS", "Addressable Asset Loading Engine disposed");
            }
        }
    }
#endif
}
