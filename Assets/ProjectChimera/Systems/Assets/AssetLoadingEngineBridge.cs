using UnityEngine;
using System.Threading.Tasks;
using System.Threading;
using ProjectChimera.Core.Assets;

namespace ProjectChimera.Systems.Assets
{
    /// <summary>
    /// REFACTORED: Thin MonoBehaviour wrapper for AssetLoadingService
    /// Bridges Unity lifecycle events to Core layer service
    /// Complies with clean architecture: Unity-specific code in Systems, business logic in Core
    /// </summary>
    public class AssetLoadingEngineBridge : MonoBehaviour
    {
        [Header("Loading Engine Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private bool _enableDetailedLogging = false;
        [SerializeField] private float _loadTimeoutSeconds = 30f;
        [SerializeField] private int _maxConcurrentLoads = 10;

        private AssetLoadingService _service;
        private static AssetLoadingEngineBridge _instance;

        public static AssetLoadingEngineBridge Instance => _instance;

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
            _service = new AssetLoadingService(
                _enableLogging,
                _enableDetailedLogging,
                _loadTimeoutSeconds,
                _maxConcurrentLoads
            );
            _service.Initialize();
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
        public AssetLoadingEngineStats Stats => _service?.Stats ?? new AssetLoadingEngineStats();
        public int ActiveHandleCount => _service?.ActiveHandleCount ?? 0;
        public int QueuedLoadCount => _service?.QueuedLoadCount ?? 0;

        public void Initialize() => _service?.Initialize();

        public Task<T> LoadAssetAsync<T>(string address) where T : UnityEngine.Object
            => _service?.LoadAssetAsync<T>(address, Time.realtimeSinceStartup) ?? Task.FromResult<T>(null);

        public Task<T> LoadAssetAsync<T>(string address, CancellationToken cancellationToken) where T : UnityEngine.Object
            => _service?.LoadAssetAsync<T>(address, cancellationToken, Time.realtimeSinceStartup) ?? Task.FromResult<T>(null);

        public void ReleaseAsset(string address)
            => _service?.ReleaseAsset(address);

        public void ReleaseAllAssets()
            => _service?.ReleaseAllAssets();

        // Events
        public event System.Action<string, object> OnAssetLoaded
        {
            add { if (_service != null) _service.OnAssetLoaded += value; }
            remove { if (_service != null) _service.OnAssetLoaded -= value; }
        }

        public event System.Action<string, string> OnAssetLoadFailed
        {
            add { if (_service != null) _service.OnAssetLoadFailed += value; }
            remove { if (_service != null) _service.OnAssetLoadFailed -= value; }
        }

        public event System.Action<EngineLoadRequest> OnLoadRequestQueued
        {
            add { if (_service != null) _service.OnLoadRequestQueued += value; }
            remove { if (_service != null) _service.OnLoadRequestQueued -= value; }
        }

        public event System.Action<EngineLoadRequest> OnLoadRequestStarted
        {
            add { if (_service != null) _service.OnLoadRequestStarted += value; }
            remove { if (_service != null) _service.OnLoadRequestStarted -= value; }
        }

        #endregion
    }
}
