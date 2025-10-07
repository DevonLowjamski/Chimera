using UnityEngine;
using System.Threading.Tasks;
using ProjectChimera.Core.Assets;
using ProjectChimera.Core;
using ProjectChimera.Core.SimpleDI;

namespace ProjectChimera.Systems.Assets
{
    /// <summary>
    /// REFACTORED: Thin MonoBehaviour wrapper for AssetPreloadService
    /// Bridges Unity lifecycle events to Core layer service
    /// Complies with clean architecture: Unity-specific code in Systems, business logic in Core
    /// </summary>
    public class AssetPreloaderBridge : MonoBehaviour
    {
        [Header("Preloader Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private bool _enablePreloading = true;
        [SerializeField] private bool _preloadOnStartup = true;
        [SerializeField] private float _preloadTimeoutSeconds = 30f;

        [Header("Preload Strategy")]
        [SerializeField] private PreloadStrategy _preloadStrategy = PreloadStrategy.Critical;
        [SerializeField] private int _maxConcurrentPreloads = 5;
        [SerializeField] private bool _continueOnFailure = true;

        [Header("Critical Assets")]
        [SerializeField] private string[] _criticalAssets;

        [Header("Common Assets")]
        [SerializeField] private string[] _commonAssets;

        private AssetPreloadService _service;
        private static AssetPreloaderBridge _instance;

        public static AssetPreloaderBridge Instance => _instance;

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
            _service = new AssetPreloadService(
                _enableLogging,
                _enablePreloading,
                _preloadOnStartup,
                _preloadTimeoutSeconds,
                _preloadStrategy,
                _maxConcurrentPreloads,
                _continueOnFailure,
                _criticalAssets,
                _commonAssets
            );

            var assetManager = ServiceContainerFactory.Instance?.TryResolve<IAssetManager>();
            _service.Initialize(assetManager);

            if (_preloadOnStartup && _enablePreloading)
            {
                _ = StartPreloadingAsync();
            }
        }

        #region Public API (delegates to service)

        public bool IsInitialized => _service?.IsInitialized ?? false;
        public bool IsPreloadingEnabled => _service?.IsPreloadingEnabled ?? false;
        public bool IsPreloadingInProgress => _service?.IsPreloadingInProgress ?? false;
        public AssetPreloaderStats Stats => _service?.Stats ?? new AssetPreloaderStats();
        public int PreloadedAssetCount => _service?.PreloadedAssetCount ?? 0;

        public Task<PreloadSession> StartPreloadingAsync()
            => _service?.StartPreloadingAsync(Time.time, Time.realtimeSinceStartup) ?? Task.FromResult(new PreloadSession());

        public Task<AssetPreloadResult[]> PreloadAssetsAsync(string[] assetAddresses)
            => _service?.PreloadAssetsAsync(assetAddresses) ?? Task.FromResult(new AssetPreloadResult[0]);

        public bool IsAssetPreloaded(string address)
            => _service?.IsAssetPreloaded(address) ?? false;

        public T GetPreloadedAsset<T>(string address) where T : class
            => _service?.GetPreloadedAsset<T>(address);

        public void ClearPreloadedAssets()
            => _service?.ClearPreloadedAssets();

        public void Initialize(IAssetManager assetManager = null)
            => _service?.Initialize(assetManager);

        // Events
        public event System.Action<string> OnAssetPreloaded
        {
            add { if (_service != null) _service.OnAssetPreloaded += value; }
            remove { if (_service != null) _service.OnAssetPreloaded -= value; }
        }

        public event System.Action<string, string> OnAssetPreloadFailed
        {
            add { if (_service != null) _service.OnAssetPreloadFailed += value; }
            remove { if (_service != null) _service.OnAssetPreloadFailed -= value; }
        }

        public event System.Action<PreloadProgressStatus> OnPreloadProgressChanged
        {
            add { if (_service != null) _service.OnPreloadProgressChanged += value; }
            remove { if (_service != null) _service.OnPreloadProgressChanged -= value; }
        }

        public event System.Action<PreloadSession> OnPreloadSessionCompleted
        {
            add { if (_service != null) _service.OnPreloadSessionCompleted += value; }
            remove { if (_service != null) _service.OnPreloadSessionCompleted -= value; }
        }

        #endregion
    }
}
