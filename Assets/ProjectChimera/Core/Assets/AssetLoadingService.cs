#if UNITY_ADDRESSABLES
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
#endif
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;
using System;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core.Assets
{
#if UNITY_ADDRESSABLES
    /// <summary>
    /// REFACTORED: Asset Loading Service (POCO - Unity-independent core)
    /// Single Responsibility: Core async loading, handle management, and addressables operations
    /// Extracted from AssetLoadingEngine for clean architecture compliance
    /// </summary>
    public class AssetLoadingService
    {
        private readonly bool _enableLogging;
        private readonly bool _enableDetailedLogging;
        private readonly float _loadTimeoutSeconds;
        private readonly int _maxConcurrentLoads;

        private readonly Dictionary<string, object> _activeHandles = new Dictionary<string, object>();
        private readonly Dictionary<string, AsyncOperationHandle> _operationHandles = new Dictionary<string, AsyncOperationHandle>();
        private readonly Queue<EngineLoadRequest> _loadQueue = new Queue<EngineLoadRequest>();
        private readonly HashSet<string> _currentlyLoading = new HashSet<string>();

        private AssetLoadingEngineStats _stats = new AssetLoadingEngineStats();
        private bool _isInitialized = false;
        private int _activeConcurrentLoads = 0;

        public event Action<string, object> OnAssetLoaded;
        public event Action<string, string> OnAssetLoadFailed;
        public event Action<EngineLoadRequest> OnLoadRequestQueued;
        public event Action<EngineLoadRequest> OnLoadRequestStarted;

        public bool IsInitialized => _isInitialized;
        public AssetLoadingEngineStats Stats => _stats;
        public int ActiveHandleCount => _activeHandles.Count;
        public int QueuedLoadCount => _loadQueue.Count;

        public AssetLoadingService(
            bool enableLogging = false,
            bool enableDetailedLogging = false,
            float loadTimeoutSeconds = 30f,
            int maxConcurrentLoads = 10)
        {
            _enableLogging = enableLogging;
            _enableDetailedLogging = enableDetailedLogging;
            _loadTimeoutSeconds = loadTimeoutSeconds;
            _maxConcurrentLoads = maxConcurrentLoads;
        }

        public void Initialize()
        {
            if (_isInitialized) return;
            _activeHandles.Clear();
            _operationHandles.Clear();
            _loadQueue.Clear();
            _currentlyLoading.Clear();
            ResetStats();
            _isInitialized = true;
            if (_enableLogging)
                ChimeraLogger.Log("ASSETS", "Asset Loading Service initialized");
        }

        public async Task<T> LoadAssetAsync<T>(string address, float realtimeSinceStartup) where T : UnityEngine.Object
        {
            if (!_isInitialized)
            {
                if (_enableLogging)
                    ChimeraLogger.LogWarning("ASSETS", "Loading service not initialized, initializing now...");
                Initialize();
            }

            if (string.IsNullOrEmpty(address))
            {
                if (_enableLogging)
                    ChimeraLogger.LogWarning("ASSETS", "Cannot load asset with null or empty address");
                return null;
            }

            var loadStartTime = realtimeSinceStartup;

            try
            {
                if (_currentlyLoading.Contains(address))
                {
                    if (_enableDetailedLogging)
                        ChimeraLogger.Log("ASSETS", $"Asset {address} already loading, waiting...");
                    return await WaitForAssetLoad<T>(address, realtimeSinceStartup);
                }

                if (_activeHandles.TryGetValue(address, out var existingHandle))
                {
                    if (existingHandle is AsyncOperationHandle<T> typedHandle && typedHandle.IsValid())
                    {
                        if (_enableDetailedLogging)
                            ChimeraLogger.Log("ASSETS", $"Reusing existing handle for {address}");
                        return typedHandle.Result;
                    }
                }

                if (_activeConcurrentLoads >= _maxConcurrentLoads)
                    return await QueueLoad<T>(address, realtimeSinceStartup);

                return await PerformLoad<T>(address, loadStartTime, realtimeSinceStartup);
            }
            catch (Exception ex)
            {
                _stats.LoadErrors++;
                var loadTime = realtimeSinceStartup - loadStartTime;
                UpdateLoadStats(loadTime, false);
                if (_enableLogging)
                    ChimeraLogger.LogError("ASSETS", $"Exception loading asset '{address}': {ex.Message}");
                OnAssetLoadFailed?.Invoke(address, ex.Message);
                return null;
            }
        }

        public async Task<T> LoadAssetAsync<T>(string address, CancellationToken cancellationToken, float realtimeSinceStartup) where T : UnityEngine.Object
        {
            if (cancellationToken.IsCancellationRequested)
                return null;

            using (var timeoutCts = new CancellationTokenSource(TimeSpan.FromSeconds(_loadTimeoutSeconds)))
            using (var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token))
            {
                return await LoadAssetAsync<T>(address, realtimeSinceStartup);
            }
        }

        public void ReleaseAsset(string address)
        {
            if (_activeHandles.TryGetValue(address, out var handle))
            {
                if (handle is AsyncOperationHandle asyncHandle && asyncHandle.IsValid())
                {
                    Addressables.Release(asyncHandle);
                }
                _activeHandles.Remove(address);
            }

            if (_operationHandles.TryGetValue(address, out var opHandle))
            {
                if (opHandle.IsValid())
                {
                    Addressables.Release(opHandle);
                }
                _operationHandles.Remove(address);
            }

            _currentlyLoading.Remove(address);
            if (_enableDetailedLogging)
                ChimeraLogger.Log("ASSETS", $"Released asset handle: {address}");
        }

        public void ReleaseAllAssets()
        {
            foreach (var handle in _operationHandles.Values)
            {
                if (handle.IsValid())
                    Addressables.Release(handle);
            }
            _activeHandles.Clear();
            _operationHandles.Clear();
            _currentlyLoading.Clear();
            _activeConcurrentLoads = 0;
            if (_enableLogging)
                ChimeraLogger.Log("ASSETS", "Released all asset handles");
        }

        #region Private Methods

        private async Task<T> PerformLoad<T>(string address, float loadStartTime, float currentTime) where T : UnityEngine.Object
        {
            _currentlyLoading.Add(address);
            _activeConcurrentLoads++;

            try
            {
                var handle = Addressables.LoadAssetAsync<T>(address);
                _activeHandles[address] = handle;
                _operationHandles[address] = handle;

                OnLoadRequestStarted?.Invoke(new EngineLoadRequest { Address = address, AssetType = typeof(T) });

                await handle.Task;

                var loadTime = currentTime - loadStartTime;

                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    _stats.AssetsLoaded++;
                    UpdateLoadStats(loadTime, true);
                    OnAssetLoaded?.Invoke(address, handle.Result);
                    if (_enableDetailedLogging)
                        ChimeraLogger.Log("ASSETS", $"Loaded {address} in {loadTime:F2}s");
                    return handle.Result;
                }
                else
                {
                    _stats.LoadErrors++;
                    UpdateLoadStats(loadTime, false);
                    var error = handle.OperationException?.Message ?? "Unknown error";
                    if (_enableLogging)
                        ChimeraLogger.LogError("ASSETS", $"Failed to load {address}: {error}");
                    OnAssetLoadFailed?.Invoke(address, error);
                    Addressables.Release(handle);
                    _activeHandles.Remove(address);
                    _operationHandles.Remove(address);
                    return null;
                }
            }
            finally
            {
                _currentlyLoading.Remove(address);
                _activeConcurrentLoads--;
                ProcessLoadQueue(currentTime);
            }
        }

        private async Task<T> QueueLoad<T>(string address, float realtimeSinceStartup) where T : UnityEngine.Object
        {
            var request = new EngineLoadRequest { Address = address, AssetType = typeof(T) };
            _loadQueue.Enqueue(request);
            OnLoadRequestQueued?.Invoke(request);
            if (_enableDetailedLogging)
                ChimeraLogger.Log("ASSETS", $"Queued load request for {address}");

            while (_loadQueue.Contains(request))
            {
                await Task.Delay(100);
            }

            return await LoadAssetAsync<T>(address, realtimeSinceStartup);
        }

        private async Task<T> WaitForAssetLoad<T>(string address, float realtimeSinceStartup) where T : UnityEngine.Object
        {
            int attempts = 0;
            const int maxAttempts = 300;

            while (_currentlyLoading.Contains(address) && attempts < maxAttempts)
            {
                await Task.Delay(100);
                attempts++;
            }

            if (_activeHandles.TryGetValue(address, out var handle))
            {
                if (handle is AsyncOperationHandle<T> typedHandle && typedHandle.IsValid())
                    return typedHandle.Result;
            }

            if (attempts >= maxAttempts)
            {
                if (_enableLogging)
                    ChimeraLogger.LogWarning("ASSETS", $"Timeout waiting for {address}");
            }

            return null;
        }

        private void ProcessLoadQueue(float currentTime)
        {
            while (_loadQueue.Count > 0 && _activeConcurrentLoads < _maxConcurrentLoads)
            {
                var request = _loadQueue.Dequeue();
                _ = LoadAssetAsync<UnityEngine.Object>(request.Address, currentTime);
            }
        }

        private void UpdateLoadStats(float loadTime, bool success)
        {
            if (success)
            {
                _stats.SuccessfulLoads++;
                _stats.TotalLoadTime += loadTime;
                _stats.AverageLoadTime = _stats.TotalLoadTime / _stats.SuccessfulLoads;
                if (loadTime < _stats.MinLoadTime || _stats.MinLoadTime == 0)
                    _stats.MinLoadTime = loadTime;
                if (loadTime > _stats.MaxLoadTime)
                    _stats.MaxLoadTime = loadTime;
            }
            else
            {
                _stats.FailedLoads++;
            }
        }

        private void ResetStats()
        {
            _stats = new AssetLoadingEngineStats();
        }

        #endregion
    }
#else
    /// <summary>
    /// Stub implementation when Addressables is not available
    /// </summary>
    public class AssetLoadingService
    {
        public bool IsInitialized => false;
        public AssetLoadingEngineStats Stats => new AssetLoadingEngineStats();
        public int ActiveHandleCount => 0;
        public int QueuedLoadCount => 0;

        public AssetLoadingService(bool enableLogging = false, bool enableDetailedLogging = false, float loadTimeoutSeconds = 30f, int maxConcurrentLoads = 10) { }
        public void Initialize() { }
        public Task<T> LoadAssetAsync<T>(string address, float realtimeSinceStartup) where T : UnityEngine.Object => Task.FromResult<T>(null);
        public Task<T> LoadAssetAsync<T>(string address, CancellationToken cancellationToken, float realtimeSinceStartup) where T : UnityEngine.Object => Task.FromResult<T>(null);
        public void ReleaseAsset(string address) { }
        public void ReleaseAllAssets() { }

        public event Action<string, object> OnAssetLoaded { add { } remove { } }
        public event Action<string, string> OnAssetLoadFailed { add { } remove { } }
        public event Action<EngineLoadRequest> OnLoadRequestQueued { add { } remove { } }
        public event Action<EngineLoadRequest> OnLoadRequestStarted { add { } remove { } }
    }
#endif

    [Serializable]
    public struct EngineLoadRequest
    {
        public string Address;
        public Type AssetType;
    }

    [Serializable]
    public struct AssetLoadingEngineStats
    {
        public int AssetsLoaded;
        public int SuccessfulLoads;
        public int FailedLoads;
        public int LoadErrors;
        public float TotalLoadTime;
        public float AverageLoadTime;
        public float MinLoadTime;
        public float MaxLoadTime;
    }
}
