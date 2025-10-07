using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Core.SimpleDI;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;

namespace ProjectChimera.Core.Assets
{
    /// <summary>
    /// DEPRECATED: Use AssetReleaseService (Core.Assets) + AssetReleaseManagerBridge (Systems.Assets) instead
    /// This wrapper maintained for backward compatibility during migration
    /// Architecture violation: MonoBehaviour in Core layer
    /// PHASE 0: Migrated to ITickable pattern for zero-tolerance compliance
    /// </summary>
    [System.Obsolete("Use AssetReleaseService (Core.Assets) + AssetReleaseManagerBridge (Systems.Assets) instead")]
    public class AssetReleaseManager : MonoBehaviour, ITickable
    {
        [Header("Release Manager Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private bool _enableAutomaticCleanup = true;
        [SerializeField] private float _cleanupInterval = 60f;
        [SerializeField] private float _unusedAssetTimeout = 300f;

        [Header("Memory Management")]
        [SerializeField] private bool _enableMemoryPressureCleanup = true;
        [SerializeField] private long _memoryPressureThreshold = 536870912;
        [SerializeField] private bool _forceGCAfterCleanup = true;

        [Header("Release Strategy")]
        [SerializeField] private ReleaseStrategy _releaseStrategy = ReleaseStrategy.TimeoutBased;
        [SerializeField] private int _maxReleasesPerFrame = 10;

        private AssetReleaseService _service;

        public bool IsInitialized => _service?.IsInitialized ?? false;
        public AssetReleaseManagerStats Stats => _service?.Stats ?? new AssetReleaseManagerStats();
        public int TrackedAssetCount => _service?.TrackedAssetCount ?? 0;
        public int QueuedReleaseCount => _service?.QueuedReleaseCount ?? 0;

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

        private void Awake()
        {
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

            // PHASE 0: Register with UpdateOrchestrator
            UpdateOrchestrator.Instance?.RegisterTickable(this);
        }

        private void OnDestroy()
        {
            // PHASE 0: Unregister from UpdateOrchestrator
            UpdateOrchestrator.Instance?.UnregisterTickable(this);
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && _service != null)
            {
                _service.ReleaseNonCriticalAssets(Time.time);
            }
        }

        #region ITickable Implementation

        public int TickPriority => ProjectChimera.Core.Updates.TickPriority.CacheManager + 10;
        public bool IsTickable => enabled && gameObject.activeInHierarchy;

        public void Tick(float deltaTime)
        {
            if (_service == null || !_service.IsInitialized) return;

            var currentTime = Time.time;

            if (_service.ShouldRunAutomaticCleanup(currentTime))
            {
                _service.PerformAutomaticCleanup(currentTime);
            }

            if (_service.ShouldCheckMemoryPressure(currentTime))
            {
                _service.CheckMemoryPressure(currentTime);
                _service.UpdateMemoryCheckTime(currentTime);
            }
        }

        public void OnRegistered() { }
        public void OnUnregistered() { }

        #endregion

        #region Public API

        public void Initialize(IAssetManager assetManager = null, AssetCacheManager cacheManager = null)
            => _service?.Initialize(assetManager, cacheManager);

        public void TrackAsset(string address, object asset)
            => _service?.TrackAsset(address, asset, Time.time);

        public void UpdateAssetAccess(string address)
            => _service?.UpdateAssetAccess(address, Time.time);

        public void IncrementReferenceCount(string address)
            => _service?.IncrementReferenceCount(address);

        public void DecrementReferenceCount(string address)
            => _service?.DecrementReferenceCount(address);

        public void ProtectAsset(string address)
            => _service?.ProtectAsset(address);

        public void UnprotectAsset(string address)
            => _service?.UnprotectAsset(address);

        public bool ReleaseAsset(string address)
            => _service?.ReleaseAsset(address) ?? false;

        public CleanupResult ReleaseNonCriticalAssets()
            => _service?.ReleaseNonCriticalAssets(Time.time) ?? new CleanupResult();

        public CleanupResult ReleaseAllAssets()
            => _service?.ReleaseAllAssets() ?? new CleanupResult();

        public CleanupResult PerformAutomaticCleanup()
            => _service?.PerformAutomaticCleanup(Time.time) ?? new CleanupResult();

        public bool CheckMemoryPressure()
            => _service?.CheckMemoryPressure(Time.time) ?? false;

        public void SetReleaseParameters(bool enableAutoCleanup, float cleanupInterval, float unusedTimeout, ReleaseStrategy strategy)
        {
            // Parameters are immutable in service - would need to recreate service
            // For backward compatibility, just log warning
            if (_enableLogging)
            {
                ChimeraLogger.LogWarning("AssetRelease", "SetReleaseParameters not supported in refactored service. Set values in Inspector before runtime.", this);
            }
        }

        #endregion
    }
}
