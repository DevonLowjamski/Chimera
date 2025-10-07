using UnityEngine;
using ProjectChimera.Core.Assets;
using ProjectChimera.Core.Updates;

namespace ProjectChimera.Systems.Assets
{
    /// <summary>
    /// REFACTORED: Thin MonoBehaviour wrapper for AssetCacheService with ITickable
    /// Bridges Unity lifecycle events to Core layer service
    /// Complies with clean architecture: Unity-specific code in Systems, business logic in Core
    /// Uses ITickable for centralized update management
    /// </summary>
    public class AssetCacheManagerBridge : MonoBehaviour, ITickable
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
        private static AssetCacheManagerBridge _instance;

        public static AssetCacheManagerBridge Instance => _instance;

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
            _service = new AssetCacheService(
                _enableLogging,
                _enableCaching,
                _maxCacheSize,
                _maxCacheMemoryBytes,
                _evictionStrategy,
                _cacheCleanupInterval,
                _unusedAssetTimeout
            );
            _service.Initialize();
        }

        #region ITickable Implementation

        public int TickPriority => ProjectChimera.Core.Updates.TickPriority.CacheManager;
        public bool IsTickable => enabled && gameObject.activeInHierarchy && (_service?.IsInitialized ?? false);

        public void Tick(float deltaTime)
        {
            if (_service != null && _service.ShouldPerformCleanup(Time.time))
            {
                _service.PerformCacheCleanup(Time.time);
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

        #region Public API (delegates to service)

        public bool IsInitialized => _service?.IsInitialized ?? false;
        public bool IsCachingEnabled => _service?.IsCachingEnabled ?? false;
        public AssetCacheManagerStats Stats => _service?.Stats ?? new AssetCacheManagerStats();
        public int CachedAssetCount => _service?.CachedAssetCount ?? 0;
        public long CacheMemoryUsage => _service?.CacheMemoryUsage ?? 0;
        public float CacheUtilization => _service?.CacheUtilization ?? 0f;
        public float MemoryUtilization => _service?.MemoryUtilization ?? 0f;

        public void Initialize() => _service?.Initialize();

        public bool CacheAsset(string address, object asset)
            => _service?.CacheAsset(address, asset, Time.time) ?? false;

        public T GetCachedAsset<T>(string address) where T : class
            => _service?.GetCachedAsset<T>(address, Time.time);

        public bool IsCached(string address)
            => _service?.IsCached(address) ?? false;

        public bool RemoveFromCache(string address)
            => _service?.RemoveFromCache(address) ?? false;

        public void ClearCache()
            => _service?.ClearCache();

        public CacheCleanupResult PerformCacheCleanup()
            => _service?.PerformCacheCleanup(Time.time) ?? new CacheCleanupResult();

        public RuntimeCachedAsset[] GetCachedAssets()
            => _service?.GetCachedAssets() ?? new RuntimeCachedAsset[0];

        // Events
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

        #endregion
    }
}
