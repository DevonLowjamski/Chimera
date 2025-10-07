using UnityEngine;
using ProjectChimera.Core.Updates;

namespace ProjectChimera.Core.Assets
{
    /// <summary>
    /// DEPRECATED: Use AssetCacheService (Core.Assets) + AssetCacheManagerBridge (Systems.Assets) instead
    /// This wrapper maintained for backward compatibility during migration
    /// Architecture violation: MonoBehaviour in Core layer
    /// PHASE 0: Migrated to ITickable pattern for zero-tolerance compliance
    /// </summary>
    [System.Obsolete("Use AssetCacheService (Core.Assets) + AssetCacheManagerBridge (Systems.Assets) instead")]
    public class AssetCacheManager : MonoBehaviour, ITickable
    {
        [Header("Cache Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private bool _enableCaching = true;
        [SerializeField] private int _maxCacheSize = 100;
        [SerializeField] private long _maxCacheMemoryBytes = 536870912;

        [Header("Cache Strategy")]
        [SerializeField] private CacheEvictionStrategy _evictionStrategy = CacheEvictionStrategy.LRU;
        [SerializeField] private float _cacheCleanupInterval = 60f;
        [SerializeField] private float _unusedAssetTimeout = 300f;

        private AssetCacheService _service;

        public bool IsInitialized => _service?.IsInitialized ?? false;
        public bool IsCachingEnabled => _service?.IsCachingEnabled ?? false;
        public AssetCacheManagerStats Stats => _service?.Stats ?? new AssetCacheManagerStats();
        public int CachedAssetCount => _service?.CachedAssetCount ?? 0;
        public long CacheMemoryUsage => _service?.CacheMemoryUsage ?? 0;
        public float CacheUtilization => _service?.CacheUtilization ?? 0f;
        public float MemoryUtilization => _service?.MemoryUtilization ?? 0f;

        public event System.Action<string, object> OnAssetCached
        {
            add { if (_service != null) _service.OnAssetCached += value; }
            remove { if (_service != null) _service.OnAssetCached -= value; }
        }

        public event System.Action<string> OnAssetEvicted
        {
            add { if (_service != null) _service.OnAssetEvicted += value; }
            remove { if (_service != null) _service.OnAssetEvicted -= value; }
        }

        public event System.Action<CacheCleanupResult> OnCacheCleanup
        {
            add { if (_service != null) _service.OnCacheCleanup += value; }
            remove { if (_service != null) _service.OnCacheCleanup -= value; }
        }

        private void Awake()
        {
            _service = new AssetCacheService(
                _enableLogging,
                _enableCaching,
                _maxCacheSize,
                _maxCacheMemoryBytes,
                _evictionStrategy,
                _cacheCleanupInterval,
                _unusedAssetTimeout
            );

            // PHASE 0: Register with UpdateOrchestrator
            UpdateOrchestrator.Instance?.RegisterTickable(this);
        }

        private void OnDestroy()
        {
            // PHASE 0: Unregister from UpdateOrchestrator
            UpdateOrchestrator.Instance?.UnregisterTickable(this);
        }

        #region ITickable Implementation

        public int TickPriority => ProjectChimera.Core.Updates.TickPriority.CacheManager;
        public bool IsTickable => enabled && gameObject.activeInHierarchy;

        public void Tick(float deltaTime)
        {
            if (_service != null && _service.ShouldPerformCleanup(Time.time))
            {
                _service.PerformCacheCleanup(Time.time);
            }
        }

        public void OnRegistered() { }
        public void OnUnregistered() { }

        #endregion

        #region Public API

        public void Initialize() => _service?.Initialize();
        public bool CacheAsset(string address, object asset) => _service?.CacheAsset(address, asset, Time.time) ?? false;
        public T GetCachedAsset<T>(string address) where T : class => _service?.GetCachedAsset<T>(address, Time.time);
        public bool IsCached(string address) => _service?.IsCached(address) ?? false;
        public bool RemoveFromCache(string address) => _service?.RemoveFromCache(address) ?? false;
        public void ClearCache() => _service?.ClearCache();
        public CacheCleanupResult PerformCacheCleanup() => _service?.PerformCacheCleanup(Time.time) ?? new CacheCleanupResult();
        public RuntimeCachedAsset[] GetCachedAssets() => _service?.GetCachedAssets() ?? new RuntimeCachedAsset[0];

        #endregion
    }
}
