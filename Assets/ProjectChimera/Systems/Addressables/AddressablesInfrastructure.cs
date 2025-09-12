using System;
using ProjectChimera.Core;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace ProjectChimera.Systems.Addressables
{
    /// <summary>
    /// SIMPLE: Basic asset loading system aligned with Project Chimera's asset management vision.
    /// Focuses on essential asset loading without complex caching or progress tracking.
    /// </summary>
    public class AddressablesInfrastructure : MonoBehaviour
    {
        [Header("Basic Settings")]
        [SerializeField] private bool _enableLogging = true;

        // Simple asset cache
        private readonly Dictionary<string, UnityEngine.Object> _assetCache = new Dictionary<string, UnityEngine.Object>();
        private bool _isInitialized = false;

        // Events
        public event Action<string, UnityEngine.Object> OnAssetLoaded;
        public event Action<string, string> OnAssetLoadFailed;

        // Properties
        public bool IsInitialized => _isInitialized;
        public int CachedAssetCount => _assetCache.Count;

        #region Unity Lifecycle

        private void Start()
        {
            Initialize();
        }

        private void OnDestroy()
        {
            Shutdown();
        }

        #endregion

        #region Initialization

        public void Initialize()
        {
            if (_isInitialized) return;

            _isInitialized = true;

            if (_enableLogging)
            {
                Debug.Log("[AddressablesInfrastructure] Initialized successfully");
            }
        }

        public void Shutdown()
        {
            _assetCache.Clear();
            _isInitialized = false;

            if (_enableLogging)
            {
                Debug.Log("[AddressablesInfrastructure] Shutdown completed");
            }
        }

        #endregion

        #region Asset Loading

        /// <summary>
        /// Load an asset by address
        /// </summary>
        public async Task<T> LoadAssetAsync<T>(string address) where T : UnityEngine.Object
        {
            if (!_isInitialized)
            {
                Debug.LogError("[AddressablesInfrastructure] Not initialized");
                return null;
            }

            // Check cache first
            if (_assetCache.TryGetValue(address, out var cachedAsset))
            {
                if (cachedAsset is T typedAsset)
                {
                    return typedAsset;
                }
            }

            try
            {
                // Simple Resources.Load fallback for now
                var asset = Resources.Load<T>(address);

                if (asset != null)
                {
                    _assetCache[address] = asset;
                    OnAssetLoaded?.Invoke(address, asset);

                    if (_enableLogging)
                    {
                        Debug.Log($"[AddressablesInfrastructure] Loaded asset: {address}");
                    }

                    return asset;
                }
                else
                {
                    OnAssetLoadFailed?.Invoke(address, "Asset not found");
                    Debug.LogError($"[AddressablesInfrastructure] Failed to load asset: {address}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                OnAssetLoadFailed?.Invoke(address, ex.Message);
                Debug.LogError($"[AddressablesInfrastructure] Error loading asset {address}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Synchronous asset loading
        /// </summary>
        public T LoadAsset<T>(string address) where T : UnityEngine.Object
        {
            if (!_isInitialized)
            {
                Debug.LogError("[AddressablesInfrastructure] Not initialized");
                return null;
            }

            // Check cache first
            if (_assetCache.TryGetValue(address, out var cachedAsset))
            {
                if (cachedAsset is T typedAsset)
                {
                    return typedAsset;
                }
            }

            try
            {
                var asset = Resources.Load<T>(address);

                if (asset != null)
                {
                    _assetCache[address] = asset;
                    OnAssetLoaded?.Invoke(address, asset);
                    return asset;
                }
                else
                {
                    OnAssetLoadFailed?.Invoke(address, "Asset not found");
                    Debug.LogError($"[AddressablesInfrastructure] Failed to load asset: {address}");
                    return null;
                }
            }
            catch (Exception ex)
            {
                OnAssetLoadFailed?.Invoke(address, ex.Message);
                Debug.LogError($"[AddressablesInfrastructure] Error loading asset {address}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Unload asset from cache
        /// </summary>
        public void UnloadAsset(string address)
        {
            if (_assetCache.Remove(address))
            {
                if (_enableLogging)
                {
                    Debug.Log($"[AddressablesInfrastructure] Unloaded asset: {address}");
                }
            }
        }

        /// <summary>
        /// Clear all cached assets
        /// </summary>
        public void ClearCache()
        {
            _assetCache.Clear();

            if (_enableLogging)
            {
                Debug.Log("[AddressablesInfrastructure] Cache cleared");
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Check if asset is cached
        /// </summary>
        public bool IsAssetCached(string address)
        {
            return _assetCache.ContainsKey(address);
        }

        /// <summary>
        /// Get asset from cache without loading
        /// </summary>
        public T GetCachedAsset<T>(string address) where T : UnityEngine.Object
        {
            if (_assetCache.TryGetValue(address, out var asset))
            {
                return asset as T;
            }
            return null;
        }

        #endregion
    }
}
