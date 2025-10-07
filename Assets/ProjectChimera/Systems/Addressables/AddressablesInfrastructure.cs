using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceLocations;
using ProjectChimera.Core;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Addressables
{
    /// <summary>
    /// REAL ADDRESSABLES: Infrastructure for Unity Addressables system integration
    /// Provides additional utility functions and batch operations for Addressables
    /// Works with the main AddressablesAssetManager for advanced scenarios
    /// </summary>
    public class AddressablesInfrastructure : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private bool _enableBatchPreloading = true;
        [SerializeField] private int _maxConcurrentLoads = 10;

        // Batch loading management
        private readonly Dictionary<string, AsyncOperationHandle> _batchHandles = new Dictionary<string, AsyncOperationHandle>();
        private readonly Queue<string> _loadQueue = new Queue<string>();
        private int _currentConcurrentLoads = 0;
        private bool _isInitialized = false;

        // Events
        public event Action<string, UnityEngine.Object> OnAssetLoaded;
        public event Action<string, string> OnAssetLoadFailed;
        public event Action<string, int, int> OnBatchProgress; // batchId, loaded, total

        // Properties
        public bool IsInitialized => _isInitialized;
        public int ActiveBatchOperations => _batchHandles.Count;
        public int QueuedLoads => _loadQueue.Count;

        #region Unity Lifecycle

        private void Start()
        {
            InitializeAsync();
        }

        private void OnDestroy()
        {
            Shutdown();
        }

        #endregion

        #region Initialization

        public async void InitializeAsync()
        {
            if (_isInitialized) return;

            try
            {
                // Mark initialized – we rely on Unity Addressables static API directly
                _isInitialized = true;

                if (_enableLogging)
                    ChimeraLogger.Log("ASSETS", "AddressablesInfrastructure initialized", this);
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError("ASSETS", $"Failed to initialize AddressablesInfrastructure: {ex.Message}", this);
            }
        }

        private void Shutdown()
        {
            // Cancel all batch operations
            foreach (var handle in _batchHandles.Values)
            {
                if (handle.IsValid())
                {
                    UnityEngine.AddressableAssets.Addressables.Release(handle);
                }
            }

            _batchHandles.Clear();
            _loadQueue.Clear();

            if (_enableLogging)
                ChimeraLogger.Log("ASSETS", "AddressablesInfrastructure shutdown completed", this);
        }

        #endregion

        #region Batch Loading

        /// <summary>
        /// Load multiple assets in a batch with progress tracking
        /// </summary>
        public async Task<Dictionary<string, T>> LoadAssetBatchAsync<T>(IList<string> addresses, string batchId = null) where T : UnityEngine.Object
        {
            if (!_isInitialized)
            {
                ChimeraLogger.LogWarning("ASSETS", "Infrastructure not initialized", this);
                return new Dictionary<string, T>();
            }

            if (string.IsNullOrEmpty(batchId))
                batchId = Guid.NewGuid().ToString();

            var results = new Dictionary<string, T>();
            var tasks = new List<Task<T>>();
            var addressList = new List<string>();

            // Create load tasks for all addresses
            foreach (var address in addresses)
            {
                if (!string.IsNullOrEmpty(address))
                {
                    addressList.Add(address);
                    tasks.Add(LoadSingleAssetForBatch<T>(address, batchId));
                }
            }

            if (tasks.Count == 0)
                return results;

            // Execute all loads and track progress
            var loadedCount = 0;
            var totalCount = tasks.Count;

            while (loadedCount < totalCount)
            {
                var completedTask = await Task.WhenAny(tasks);
                var index = tasks.IndexOf(completedTask);
                var address = addressList[index];
                var result = await completedTask;

                if (result != null)
                {
                    results[address] = result;
                }

                loadedCount++;
                OnBatchProgress?.Invoke(batchId, loadedCount, totalCount);

                tasks.RemoveAt(index);
                addressList.RemoveAt(index);
            }

            if (_enableLogging)
                ChimeraLogger.Log("ASSETS", $"Batch '{batchId}' completed: {results.Count}/{totalCount} assets loaded", this);

            return results;
        }

        /// <summary>
        /// Load assets by multiple labels in batch
        /// </summary>
        public async Task<Dictionary<string, List<T>>> LoadAssetsByLabelsAsync<T>(IList<string> labels) where T : UnityEngine.Object
        {
            var results = new Dictionary<string, List<T>>();

            foreach (var label in labels)
            {
                try
                {
                    var assets = await LoadAssetsByLabelAsync<T>(label);
                    results[label] = new List<T>(assets);
                }
                catch (Exception ex)
                {
                    ChimeraLogger.LogError("ASSETS", $"Failed to load assets with label '{label}': {ex.Message}", this);
                    results[label] = new List<T>();
                }
            }

            return results;
        }

        /// <summary>
        /// Load assets by a single label
        /// </summary>
        public async Task<IList<T>> LoadAssetsByLabelAsync<T>(string label) where T : UnityEngine.Object
        {
            try
            {
                var handle = UnityEngine.AddressableAssets.Addressables.LoadAssetsAsync<T>(label, null);
                await handle.Task;

                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    return handle.Result;
                }
                else
                {
                    ChimeraLogger.LogError("ASSETS", $"Failed to load assets with label '{label}': {handle.OperationException?.Message}", this);
                    UnityEngine.AddressableAssets.Addressables.Release(handle);
                    return new List<T>();
                }
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError("ASSETS", $"Exception loading assets with label '{label}': {ex.Message}", this);
                return new List<T>();
            }
        }

        /// <summary>
        /// Helper method for batch loading individual assets
        /// </summary>
        private async Task<T> LoadSingleAssetForBatch<T>(string address, string batchId) where T : UnityEngine.Object
        {
            try
            {
                // Use the main asset manager for actual loading
                // Direct Addressables call
                var handle = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<T>(address);
                await handle.Task;

                if (handle.Status == AsyncOperationStatus.Succeeded)
                {
                    OnAssetLoaded?.Invoke(address, handle.Result);
                    return handle.Result;
                }
                else
                {
                    UnityEngine.AddressableAssets.Addressables.Release(handle);
                }

                OnAssetLoadFailed?.Invoke(address, $"Failed to load asset in batch '{batchId}'");
                return null;
            }
            catch (Exception ex)
            {
                OnAssetLoadFailed?.Invoke(address, ex.Message);
                return null;
            }
        }

        #endregion

        #region Asset Validation

        /// <summary>
        /// Validate that all assets in a list exist
        /// </summary>
        public async Task<Dictionary<string, bool>> ValidateAssetsExistAsync(IList<string> addresses)
        {
            var results = new Dictionary<string, bool>();

            foreach (var address in addresses)
            {
                try
                {
                    var locations = await UnityEngine.AddressableAssets.Addressables.LoadResourceLocationsAsync(address).Task;
                    results[address] = locations != null && locations.Count > 0;
                }
                catch
                {
                    results[address] = false;
                }
            }

            return results;
        }

        /// <summary>
        /// Get addressable asset information
        /// </summary>
        public async Task<AddressableAssetInfo> GetAssetInfoAsync(string address)
        {
            try
            {
                var locations = await UnityEngine.AddressableAssets.Addressables.LoadResourceLocationsAsync(address).Task;

                if (locations != null && locations.Count > 0)
                {
                    var location = locations[0];
                    return new AddressableAssetInfo
                    {
                        Address = address,
                        PrimaryKey = location.PrimaryKey,
                        ResourceType = location.ResourceType?.Name ?? "Unknown",
                        Provider = location.ProviderId,
                        Exists = true
                    };
                }
                else
                {
                    return new AddressableAssetInfo
                    {
                        Address = address,
                        Exists = false
                    };
                }
            }
            catch (Exception ex)
            {
                return new AddressableAssetInfo
                {
                    Address = address,
                    Exists = false,
                    ErrorMessage = ex.Message
                };
            }
        }

        #endregion

        #region Content Management

        /// <summary>
        /// Download and cache content for specific labels
        /// </summary>
        public async Task<bool> DownloadContentAsync(string label, Action<float> progressCallback = null)
        {
            try
            {
                var sizeHandle = UnityEngine.AddressableAssets.Addressables.GetDownloadSizeAsync(label);
                await sizeHandle.Task;

                var downloadSize = sizeHandle.Result;
                UnityEngine.AddressableAssets.Addressables.Release(sizeHandle);

                if (downloadSize > 0)
                {
                    ChimeraLogger.Log("ASSETS", $"Downloading {downloadSize} bytes for label '{label}'", this);

                    var downloadHandle = UnityEngine.AddressableAssets.Addressables.DownloadDependenciesAsync(label);

                    while (!downloadHandle.IsDone)
                    {
                        progressCallback?.Invoke(downloadHandle.PercentComplete);
                        await Task.Delay(100);
                    }

                    await downloadHandle.Task;

                    if (downloadHandle.Status == AsyncOperationStatus.Succeeded)
                    {
                        ChimeraLogger.Log("ASSETS", $"Successfully downloaded content for label '{label}'", this);
                        UnityEngine.AddressableAssets.Addressables.Release(downloadHandle);
                        return true;
                    }
                    else
                    {
                        ChimeraLogger.LogError("ASSETS", $"Failed to download content for label '{label}': {downloadHandle.OperationException?.Message}", this);
                        UnityEngine.AddressableAssets.Addressables.Release(downloadHandle);
                        return false;
                    }
                }
                else
                {
                    // No download needed
                    return true;
                }
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError("ASSETS", $"Exception downloading content for label '{label}': {ex.Message}", this);
                return false;
            }
        }

        #endregion

        #region Statistics

        /// <summary>
        /// Get infrastructure statistics
        /// </summary>
        public AddressablesInfrastructureStats GetStats()
        {
            return new AddressablesInfrastructureStats
            {
                IsInitialized = _isInitialized,
                ActiveBatchOperations = _batchHandles.Count,
                QueuedLoads = _loadQueue.Count,
                CurrentConcurrentLoads = _currentConcurrentLoads,
                MaxConcurrentLoads = _maxConcurrentLoads
            };
        }

        #endregion
    }

    /// <summary>
    /// Information about an addressable asset
    /// </summary>
    [System.Serializable]
    public struct AddressableAssetInfo
    {
        public string Address;
        public string PrimaryKey;
        public string ResourceType;
        public string Provider;
        public bool Exists;
        public string ErrorMessage;
    }

    /// <summary>
    /// Infrastructure statistics
    /// </summary>
    [System.Serializable]
    public struct AddressablesInfrastructureStats
    {
        public bool IsInitialized;
        public int ActiveBatchOperations;
        public int QueuedLoads;
        public int CurrentConcurrentLoads;
        public int MaxConcurrentLoads;
    }
}
