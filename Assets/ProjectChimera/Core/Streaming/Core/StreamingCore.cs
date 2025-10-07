using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;
using System;

namespace ProjectChimera.Core.Streaming.Core
{
    /// <summary>
    /// REFACTORED: Streaming Core - Central coordination for streaming subsystems
    /// Manages asset streaming, priority calculation, load queue processing, and memory management
    /// Follows Single Responsibility Principle with focused subsystem coordination
    /// </summary>
    public class StreamingCore : MonoBehaviour, ITickable
    {
        [Header("Core Streaming Settings")]
        [SerializeField] private bool _enableStreaming = true;
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private float _streamingUpdateInterval = 0.5f;

        // Core subsystems
        private AssetRegistrationManager _assetRegistrationManager;
        private StreamingPriorityCalculator _priorityCalculator;
        private StreamingQueueManager _queueManager;
        private StreamingMemoryManager _memoryManager;
        private StreamingMetricsCollector _metricsCollector;

        // Streaming center reference
        private Transform _streamingCenter;

        // Properties
        public bool IsEnabled { get; private set; } = true;
        public int TickPriority => 100; // Lower priority for streaming
        public bool IsTickable => enabled && gameObject.activeInHierarchy && IsEnabled && _enableStreaming;

        // Statistics aggregation
        public StreamingStats GetCombinedStats()
        {
            var stats = new StreamingStats();

            if (_assetRegistrationManager != null)
            {
                var registrationStats = _assetRegistrationManager.GetStats();
                stats.RegisteredAssets = registrationStats.RegisteredAssets;
                stats.LoadedAssets = registrationStats.LoadedAssets;
                stats.LoadingAssets = registrationStats.LoadingAssets;
            }

            if (_queueManager != null)
            {
                var queueStats = _queueManager.GetStats();
                stats.LoadRequests = queueStats.LoadRequests;
                stats.FailedLoads = queueStats.FailedLoads;
            }

            if (_memoryManager != null)
            {
                var memoryStats = _memoryManager.GetStats();
                stats.CurrentMemoryUsage = memoryStats.CurrentMemoryUsage;
                stats.UnloadedAssets = memoryStats.UnloadedAssets;
            }

            return stats;
        }

        // Events
        public System.Action<string> OnAssetLoaded;
        public System.Action<string> OnAssetUnloaded;
        public System.Action<StreamingStats> OnMetricsUpdated;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (_enableLogging)
                ChimeraLogger.Log("STREAMING", "🔄 Initializing StreamingCore subsystems...", this);

            // Initialize subsystems in dependency order
            InitializeAssetRegistrationManager();
            InitializePriorityCalculator();
            InitializeQueueManager();
            InitializeMemoryManager();
            InitializeMetricsCollector();

            // Set default streaming center
            SetDefaultStreamingCenter();

            // Register with UpdateOrchestrator
            UpdateOrchestrator.Instance?.RegisterTickable(this);

            if (_enableLogging)
                ChimeraLogger.Log("STREAMING", "✅ StreamingCore initialized with all subsystems", this);
        }

        private void InitializeAssetRegistrationManager()
        {
            var registrationGO = new GameObject("AssetRegistrationManager");
            registrationGO.transform.SetParent(transform);
            _assetRegistrationManager = registrationGO.AddComponent<AssetRegistrationManager>();

            _assetRegistrationManager.OnAssetLoaded += (assetKey) => OnAssetLoaded?.Invoke(assetKey);
            _assetRegistrationManager.OnAssetUnloaded += (assetKey) => OnAssetUnloaded?.Invoke(assetKey);
        }

        private void InitializePriorityCalculator()
        {
            var priorityGO = new GameObject("StreamingPriorityCalculator");
            priorityGO.transform.SetParent(transform);
            _priorityCalculator = priorityGO.AddComponent<StreamingPriorityCalculator>();
        }

        private void InitializeQueueManager()
        {
            var queueGO = new GameObject("StreamingQueueManager");
            queueGO.transform.SetParent(transform);
            _queueManager = queueGO.AddComponent<StreamingQueueManager>();
        }

        private void InitializeMemoryManager()
        {
            var memoryGO = new GameObject("StreamingMemoryManager");
            memoryGO.transform.SetParent(transform);
            _memoryManager = memoryGO.AddComponent<StreamingMemoryManager>();
        }

        private void InitializeMetricsCollector()
        {
            var metricsGO = new GameObject("StreamingMetricsCollector");
            metricsGO.transform.SetParent(transform);
            _metricsCollector = metricsGO.AddComponent<StreamingMetricsCollector>();
        }

        private void SetDefaultStreamingCenter()
        {
            if (_streamingCenter == null)
            {
                var mainCamera = UnityEngine.Camera.main;
                if (mainCamera != null)
                {
                    _streamingCenter = mainCamera.transform;
                }
                else
                {
                    _streamingCenter = transform;
                }
            }
        }

        public void Tick(float deltaTime)
        {
            if (!IsEnabled || !_enableStreaming) return;

            // Coordinate subsystem updates
            if (_assetRegistrationManager != null && _streamingCenter != null)
            {
                UpdateAssetStreaming();
            }

            if (_queueManager != null)
            {
                _queueManager.ProcessQueues();
            }

            if (_memoryManager != null)
            {
                _memoryManager.UpdateMemoryManagement();
            }

            if (_metricsCollector != null)
            {
                _metricsCollector.UpdateMetrics();
            }

            // Update combined metrics
            OnMetricsUpdated?.Invoke(GetCombinedStats());
        }

        /// <summary>
        /// Update asset streaming based on distance and priority
        /// </summary>
        private void UpdateAssetStreaming()
        {
            if (_streamingCenter == null) return;

            Vector3 centerPosition = _streamingCenter.position;
            var registeredAssets = _assetRegistrationManager.GetRegisteredAssets();

            foreach (var kvp in registeredAssets)
            {
                var assetKey = kvp.Key;
                var asset = kvp.Value;

                // Calculate distance and priority
                float distance = Vector3.Distance(centerPosition, asset.Position);
                var effectivePriority = _priorityCalculator.CalculateEffectivePriority(asset, distance);

                // Determine streaming actions
                bool shouldBeLoaded = _priorityCalculator.ShouldAssetBeLoaded(distance, effectivePriority);
                bool isLoaded = asset.LoadState == AssetLoadState.Loaded;
                bool isLoading = asset.LoadState == AssetLoadState.Loading;

                // Queue appropriate actions
                if (shouldBeLoaded && !isLoaded && !isLoading)
                {
                    _queueManager.QueueAssetLoad(assetKey, effectivePriority);
                }
                else if (!shouldBeLoaded && isLoaded)
                {
                    _queueManager.QueueAssetUnload(assetKey);
                }

                // Update asset distance
                _assetRegistrationManager.UpdateAssetDistance(assetKey, distance);
            }
        }

        /// <summary>
        /// Set streaming center (usually main camera)
        /// </summary>
        public void SetStreamingCenter(Transform center)
        {
            _streamingCenter = center;
            if (_enableLogging)
                ChimeraLogger.Log("STREAMING", $"Streaming center set to: {center?.name}", this);
        }

        /// <summary>
        /// Register asset for streaming through AssetRegistrationManager
        /// </summary>
        public void RegisterAsset(string assetKey, Vector3 position, StreamingPriority priority = StreamingPriority.Medium, string[] tags = null)
        {
            _assetRegistrationManager?.RegisterAsset(assetKey, position, priority, tags);
        }

        /// <summary>
        /// Unregister asset through AssetRegistrationManager
        /// </summary>
        public void UnregisterAsset(string assetKey)
        {
            _assetRegistrationManager?.UnregisterAsset(assetKey);
        }

        /// <summary>
        /// Get loaded asset through AssetRegistrationManager
        /// </summary>
        public T GetLoadedAsset<T>(string assetKey) where T : UnityEngine.Object
        {
            return _assetRegistrationManager?.GetLoadedAsset<T>(assetKey);
        }

        /// <summary>
        /// Check if asset is loaded through AssetRegistrationManager
        /// </summary>
        public bool IsAssetLoaded(string assetKey)
        {
            return _assetRegistrationManager?.IsAssetLoaded(assetKey) ?? false;
        }

        /// <summary>
        /// Force unload asset through appropriate managers
        /// </summary>
        public void UnloadAsset(string assetKey)
        {
            _queueManager?.QueueAssetUnload(assetKey);
        }

        /// <summary>
        /// Clear all streaming data through all subsystems
        /// </summary>
        public void ClearAll()
        {
            _queueManager?.ClearAllQueues();
            _assetRegistrationManager?.ClearAllAssets();
            _memoryManager?.ClearMemoryCache();

            if (_enableLogging)
                ChimeraLogger.Log("STREAMING", "All streaming data cleared", this);
        }

        /// <summary>
        /// Set system enabled/disabled
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;

            if (_assetRegistrationManager != null) _assetRegistrationManager.SetEnabled(enabled);
            if (_priorityCalculator != null) _priorityCalculator.SetEnabled(enabled);
            if (_queueManager != null) _queueManager.SetEnabled(enabled);
            if (_memoryManager != null) _memoryManager.SetEnabled(enabled);
            if (_metricsCollector != null) _metricsCollector.SetEnabled(enabled);

            if (_enableLogging)
                ChimeraLogger.Log("STREAMING", $"StreamingCore: {(enabled ? "enabled" : "disabled")}", this);
        }

        /// <summary>
        /// Update streaming settings across subsystems
        /// </summary>
        public void UpdateStreamingSettings(float streamingRadius, float unloadRadius, int maxConcurrentLoads)
        {
            _priorityCalculator?.UpdateRadiusSettings(streamingRadius, unloadRadius);
            _queueManager?.UpdateConcurrentLoadSettings(maxConcurrentLoads);

            if (_enableLogging)
                ChimeraLogger.Log("STREAMING", $"Updated streaming settings: radius={streamingRadius}, unload={unloadRadius}, concurrent={maxConcurrentLoads}", this);
        }

        private void OnDestroy()
        {
            UpdateOrchestrator.Instance?.UnregisterTickable(this);
        }
    }

    #region Data Structures

    /// <summary>
    /// Priority levels for streaming operations
    /// </summary>
    public enum StreamingPriority
    {
        VeryLow,
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// Asset load states
    /// </summary>
    public enum AssetLoadState
    {
        Unloaded,
        Loading,
        Loaded,
        Failed
    }

    /// <summary>
    /// Streaming request
    /// </summary>
    [System.Serializable]
    public struct StreamingRequest
    {
        public string AssetKey;
        public StreamingPriority Priority;
        public float RequestTime;
    }

    /// <summary>
    /// Streaming statistics
    /// </summary>
    [System.Serializable]
    public struct StreamingStats
    {
        public int RegisteredAssets;
        public int LoadedAssets;
        public int LoadingAssets;
        public int UnloadedAssets;
        public int LoadRequests;
        public int FailedLoads;
        public long CurrentMemoryUsage;
    }

    #endregion
}
