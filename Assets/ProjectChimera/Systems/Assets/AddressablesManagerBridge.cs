using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Core.Assets;
using ProjectChimera.Core.SimpleDI;

namespace ProjectChimera.Systems.Assets
{
    /// <summary>
    /// REFACTORED: Thin MonoBehaviour wrapper for AddressablesService
    /// Bridges Unity lifecycle events to Core layer service
    /// Complies with clean architecture: Unity-specific code in Systems, business logic in Core
    /// </summary>
    public class AddressablesManagerBridge : MonoBehaviour
    {
        [Header("Configuration")]
        [SerializeField] private bool _enableCaching = true;
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private int _maxCacheSize = 100;
        [SerializeField] private bool _preloadCriticalAssets = true;

        private IAssetManager _assetService;
        private static AddressablesManagerBridge _instance;

        public static AddressablesManagerBridge Instance => _instance;

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
            var service = new AddressablesService(_enableCaching, _enableLogging, _maxCacheSize, _preloadCriticalAssets);

            // Register with ServiceContainer (factory takes IServiceContainer parameter)
            ServiceContainerFactory.Instance?.RegisterSingleton<IAssetManager>(container => service);

            _assetService = service;

            // Initialize asynchronously
            _ = _assetService.InitializeAsync();
        }

        private void OnDestroy()
        {
            // Cleanup service resources - unload all cached assets properly
            if (_assetService != null)
            {
                var cacheEntries = _assetService.GetCacheEntries();
                foreach (var entry in cacheEntries)
                {
                    if (entry != null && !string.IsNullOrEmpty(entry.AssetPath))
                    {
                        _assetService.UnloadAsset(entry.AssetPath);
                    }
                }
                _assetService.ClearCache();
            }

            if (_instance == this)
            {
                _instance = null;
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && _assetService != null)
            {
                // Release non-critical assets to free memory when app is paused
                _assetService.ClearCache(persistentOnly: true);
            }
        }

        /// <summary>
        /// Public accessor for backward compatibility
        /// </summary>
        public IAssetManager AssetService => _assetService;
    }
}
