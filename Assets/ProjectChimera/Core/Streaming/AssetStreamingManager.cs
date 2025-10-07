using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;
using ProjectChimera.Core.Streaming.Core;
using System;
using StreamingPriority = ProjectChimera.Core.Streaming.Core.StreamingPriority;
using AssetLoadState = ProjectChimera.Core.Streaming.Core.AssetLoadState;

namespace ProjectChimera.Core.Streaming
{
    /// <summary>
    /// REFACTORED: Asset Streaming Manager - Legacy wrapper for backward compatibility
    /// Delegates to StreamingCore for focused streaming subsystem coordination
    /// Maintains existing API while utilizing Single Responsibility Principle architecture
    /// </summary>
    public class AssetStreamingManager : MonoBehaviour, ITickable
    {
        [Header("Legacy Wrapper Settings")]
        [SerializeField] private bool _enableStreaming = true;
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private float _streamingRadius = 100f;
        [SerializeField] private float _unloadRadius = 150f;
        [SerializeField] private int _maxConcurrentLoads = 3;

        // Core streaming system (delegation target)
        private StreamingCore _streamingCore;
        private StreamingStats _cachedStats = new StreamingStats();

        // Legacy wrapper state
        private Transform _streamingCenter;
        private float _lastStreamingUpdate;
        [SerializeField] private float _streamingUpdateInterval = 0.5f;
        private readonly Dictionary<string, StreamedAsset> _streamedAssets = new Dictionary<string, StreamedAsset>();
        private StreamingStats _stats = new StreamingStats();

        private static AssetStreamingManager _instance;
        public static AssetStreamingManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    // Try to resolve from ServiceContainer first
                    var serviceContainer = ServiceContainerFactory.Instance;
                    _instance = serviceContainer?.TryResolve<AssetStreamingManager>();

                    if (_instance == null)
                    {
                        var go = new GameObject("AssetStreamingManager");
                        _instance = go.AddComponent<AssetStreamingManager>();
                        DontDestroyOnLoad(go);

                        // Register with ServiceContainer for future resolution
                        serviceContainer?.RegisterSingleton<AssetStreamingManager>(_instance);
                    }
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Initialize streaming manager
        /// </summary>
        public void Initialize()
        {
            // Default streaming center to main camera
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

            _lastStreamingUpdate = Time.time;

            // Ensure core is available
            InitializeStreamingCore();

            if (_enableLogging)
            {
                ChimeraLogger.LogInfo("AssetStreamingManager", "Operation completed");
            }
        }

    public int TickPriority => 100;
    public bool IsTickable => enabled && gameObject.activeInHierarchy;

    public void Tick(float deltaTime)
    {
            if (!_enableStreaming) return;

            if (Time.time - _lastStreamingUpdate >= _streamingUpdateInterval)
            {
                UpdateStreaming();
                _lastStreamingUpdate = Time.time;
            }

            ProcessLoadQueue();
            ProcessUnloadQueue();
            UpdateAssetLifetimes();
    }


    private void OnDestroy()
    {
        ProjectChimera.Core.Updates.UpdateOrchestrator.Instance?.UnregisterTickable((ProjectChimera.Core.Updates.ITickable)this);
    }

    /// <summary>
    /// Set the streaming center (usually main camera)
    /// </summary>
    public void SetStreamingCenter(Transform center)
    {
        _streamingCenter = center;
    }

    /// <summary>
    /// Register asset for streaming
    /// </summary>
    public void RegisterAsset(string assetKey, Vector3 position, StreamingPriority priority = StreamingPriority.Medium, string[] tags = null)
    {
        if (string.IsNullOrEmpty(assetKey)) return;

        if (_streamedAssets.ContainsKey(assetKey))
        {
            // Update existing asset
            var existing = _streamedAssets[assetKey];
            existing.Position = position;
            existing.Priority = priority;
            existing.LastAccessTime = Time.time;
            return;
        }

        var streamedAsset = new StreamedAsset
        {
            AssetKey = assetKey,
            Position = position,
            Priority = priority,
            Tags = tags ?? new string[0],
            RegistrationTime = Time.time,
            LastAccessTime = Time.time,
            LoadState = AssetLoadState.Unloaded
        };

            _streamedAssets[assetKey] = streamedAsset;
            _stats.RegisteredAssets++;
            // Delegate to core
            _streamingCore?.RegisterAsset(assetKey, position, priority, tags);

            if (_enableLogging)
            {
                ChimeraLogger.LogInfo("AssetStreamingManager", "Operation completed");
            }
        }

        /// <summary>
        /// Unregister asset from streaming - delegates to StreamingCore
        /// </summary>
        public void UnregisterAsset(string assetKey)
        {
            if (_streamingCore != null)
            {
                _streamingCore.UnregisterAsset(assetKey);
            }
        }

        /// <summary>
        /// Request immediate load of asset - delegates to StreamingCore
        /// </summary>
        public T LoadAssetImmediate<T>(string assetKey) where T : UnityEngine.Object
        {
            // Set to critical priority and request through core
            if (_streamingCore != null)
            {
                _streamingCore.RegisterAsset(assetKey, Vector3.zero, StreamingPriority.Critical);
                return _streamingCore.GetLoadedAsset<T>(assetKey);
            }
            return null;
        }

        /// <summary>
        /// Get loaded asset if available - delegates to StreamingCore
        /// </summary>
        public T GetLoadedAsset<T>(string assetKey) where T : UnityEngine.Object
        {
            if (_streamingCore != null)
            {
                return _streamingCore.GetLoadedAsset<T>(assetKey);
            }
            return null;
        }

        /// <summary>
        /// Check if asset is loaded - delegates to StreamingCore
        /// </summary>
        public bool IsAssetLoaded(string assetKey)
        {
            if (_streamingCore != null)
            {
                return _streamingCore.IsAssetLoaded(assetKey);
            }
            return false;
        }

        /// <summary>
        /// Force unload asset - delegates to StreamingCore
        /// </summary>
        public void UnloadAsset(string assetKey)
        {
            if (_streamingCore != null)
            {
                _streamingCore.UnloadAsset(assetKey);
            }
        }

        /// <summary>
        /// Get streaming statistics - delegates to StreamingCore
        /// </summary>
        public StreamingStats GetStats()
        {
            if (_streamingCore != null)
            {
                return _streamingCore.GetCombinedStats();
            }
            return _cachedStats;
        }

        /// <summary>
        /// Clear all streaming data - delegates to StreamingCore
        /// </summary>
        public void ClearAll()
        {
            if (_streamingCore != null)
            {
                _streamingCore.ClearAll();
            }
        }

        /// <summary>
        /// Initialize core streaming system
        /// </summary>
        private void InitializeStreamingCore()
        {
            if (_streamingCore == null)
            {
                var coreGO = new GameObject("StreamingCore");
                coreGO.transform.SetParent(transform);
                _streamingCore = coreGO.AddComponent<StreamingCore>();
            }

            // Setup event delegation
            _streamingCore.OnAssetLoaded += (assetKey) => { /* Handle asset loaded */ };
            _streamingCore.OnAssetUnloaded += (assetKey) => { /* Handle asset unloaded */ };
            _streamingCore.OnMetricsUpdated += (stats) => {
                _cachedStats = stats;
            };

            // Apply initial settings
            _streamingCore.UpdateStreamingSettings(_streamingRadius, _unloadRadius, _maxConcurrentLoads);
        }

        // Legacy no-op wrappers maintained for backward compatibility
        private void UpdateStreaming()
        {
            if (_streamingCore != null && _streamingCenter != null)
            {
                _streamingCore.SetStreamingCenter(_streamingCenter);
            }
        }

        private void ProcessLoadQueue() { /* handled by StreamingCore */ }
        private void ProcessUnloadQueue() { /* handled by StreamingCore */ }
        private void UpdateAssetLifetimes() { /* handled by StreamingCore */ }

        /// <summary>
        /// Update streaming settings
        /// </summary>
        public void UpdateStreamingSettings(float streamingRadius, float unloadRadius, int maxConcurrentLoads)
        {
            _streamingRadius = streamingRadius;
            _unloadRadius = unloadRadius;
            _maxConcurrentLoads = maxConcurrentLoads;

            if (_streamingCore != null)
            {
                _streamingCore.UpdateStreamingSettings(streamingRadius, unloadRadius, maxConcurrentLoads);
            }
        }
    }

    // Legacy data structures removed (use Core/Streaming/StreamedAsset.cs)
}
