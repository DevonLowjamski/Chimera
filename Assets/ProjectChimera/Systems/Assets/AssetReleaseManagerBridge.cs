using UnityEngine;
using ProjectChimera.Core.Assets;
using ProjectChimera.Core;
using ProjectChimera.Core.SimpleDI;
using ProjectChimera.Core.Updates;

namespace ProjectChimera.Systems.Assets
{
    /// <summary>
    /// REFACTORED: Thin MonoBehaviour wrapper for AssetReleaseService with ITickable
    /// Bridges Unity lifecycle events to Core layer service
    /// Complies with clean architecture: Unity-specific code in Systems, business logic in Core
    /// Uses ITickable for centralized update management
    /// </summary>
    public class AssetReleaseManagerBridge : MonoBehaviour, ITickable
    {
        [Header("Release Manager Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private bool _enableAutomaticCleanup = true;
        [SerializeField] private float _cleanupInterval = 60f;
        [SerializeField] private float _unusedAssetTimeout = 300f;

        [Header("Memory Management")]
        [SerializeField] private bool _enableMemoryPressureCleanup = true;
        [SerializeField] private long _memoryPressureThreshold = 536870912; // 512MB
        [SerializeField] private bool _forceGCAfterCleanup = true;

        [Header("Release Strategy")]
        [SerializeField] private ReleaseStrategy _releaseStrategy = ReleaseStrategy.TimeoutBased;
        [SerializeField] private int _maxReleasesPerFrame = 10;

        private AssetReleaseService _service;
        private static AssetReleaseManagerBridge _instance;

        public static AssetReleaseManagerBridge Instance => _instance;

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeService();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void InitializeService()
        {
            // Create Core service (POCO - no Unity dependencies)
            _service = new AssetReleaseService(
                _enableLogging,
                _enableAutomaticCleanup,
                _cleanupInterval,
                _unusedAssetTimeout,
                _enableMemoryPressureCleanup,
                _memoryPressureThreshold,
                _forceGCAfterCleanup,
                _releaseStrategy,
                _maxReleasesPerFrame
            );

            // Resolve dependencies from ServiceContainer
            var assetManager = ServiceContainerFactory.Instance?.TryResolve<IAssetManager>();
            var cacheManager = ServiceContainerFactory.Instance?.TryResolve<AssetCacheManager>();

            _service.Initialize(assetManager, cacheManager);
        }

        #region ITickable Implementation

        public int TickPriority => ProjectChimera.Core.Updates.TickPriority.CacheManager;
        public bool IsTickable => enabled && gameObject.activeInHierarchy && (_service?.IsInitialized ?? false);

        public void Tick(float deltaTime)
        {
            if (_service == null || !_service.IsInitialized) return;

            var currentTime = Time.time;

            // Periodic automatic cleanup
            if (_service.ShouldRunAutomaticCleanup(currentTime))
            {
                _service.PerformAutomaticCleanup(currentTime);
            }

            // Periodic memory pressure check
            if (_service.ShouldCheckMemoryPressure(currentTime))
            {
                _service.CheckMemoryPressure(currentTime);
                _service.UpdateMemoryCheckTime(currentTime);
            }
        }

        private void OnEnable()
        {
            UpdateOrchestrator.Instance?.RegisterTickable(this);
        }

        private void OnDisable()
        {
            UpdateOrchestrator.Instance?.UnregisterTickable(this);
        }

        #endregion

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && _service != null)
            {
                // Release non-critical assets when app is paused
                _service.ReleaseNonCriticalAssets(Time.time);
            }
        }

        private void OnDestroy()
        {
            if (_service != null)
            {
                _service.ReleaseAllAssets();
            }

            if (_instance == this)
            {
                _instance = null;
            }
        }

        #region Public API (delegates to service)

        public bool IsInitialized => _service?.IsInitialized ?? false;
        public AssetReleaseManagerStats Stats => _service?.Stats ?? new AssetReleaseManagerStats();
        public int TrackedAssetCount => _service?.TrackedAssetCount ?? 0;
        public int QueuedReleaseCount => _service?.QueuedReleaseCount ?? 0;

        public void TrackAsset(string address, object asset)
        {
            _service?.TrackAsset(address, asset, Time.time);
        }

        public void UpdateAssetAccess(string address)
        {
            _service?.UpdateAssetAccess(address, Time.time);
        }

        public void IncrementReferenceCount(string address)
        {
            _service?.IncrementReferenceCount(address);
        }

        public void DecrementReferenceCount(string address)
        {
            _service?.DecrementReferenceCount(address);
        }

        public void ProtectAsset(string address)
        {
            _service?.ProtectAsset(address);
        }

        public void UnprotectAsset(string address)
        {
            _service?.UnprotectAsset(address);
        }

        public bool ReleaseAsset(string address)
        {
            return _service?.ReleaseAsset(address) ?? false;
        }

        public CleanupResult ReleaseNonCriticalAssets()
        {
            return _service?.ReleaseNonCriticalAssets(Time.time) ?? new CleanupResult();
        }

        public CleanupResult ReleaseAllAssets()
        {
            return _service?.ReleaseAllAssets() ?? new CleanupResult();
        }

        public CleanupResult PerformAutomaticCleanup()
        {
            return _service?.PerformAutomaticCleanup(Time.time) ?? new CleanupResult();
        }

        public bool CheckMemoryPressure()
        {
            return _service?.CheckMemoryPressure(Time.time) ?? false;
        }

        // Events
        public event System.Action<string> OnAssetReleased
        {
            add { if (_service != null) _service.OnAssetReleased += value; }
            remove { if (_service != null) _service.OnAssetReleased -= value; }
        }

        public event System.Action<CleanupResult> OnCleanupCompleted
        {
            add { if (_service != null) _service.OnCleanupCompleted += value; }
            remove { if (_service != null) _service.OnCleanupCompleted -= value; }
        }

        public event System.Action<MemoryPressureEvent> OnMemoryPressureDetected
        {
            add { if (_service != null) _service.OnMemoryPressureDetected += value; }
            remove { if (_service != null) _service.OnMemoryPressureDetected -= value; }
        }

        #endregion
    }
}
