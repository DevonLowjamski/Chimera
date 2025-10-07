using UnityEngine;
#if UNITY_ADDRESSABLES
using UnityEngine.AddressableAssets;
#endif
using System.Collections.Generic;
using System.Linq;

using ProjectChimera.Core.Logging;
using Logger = ProjectChimera.Core.Logging.ChimeraLogger;

namespace ProjectChimera.Core.Assets
{
#if UNITY_ADDRESSABLES
    /// <summary>
    /// REFACTORED: Asset management and lifecycle operations
    /// Part 2 of 2: Unloading, caching, and cleanup
    /// Kept under 500 lines per clean architecture guidelines
    /// </summary>
    public partial class AddressablesService : IAssetManager
    {
        #region Asset Management

        public void UnloadAsset(string assetPath)
        {
            ReleaseAsset(assetPath);
        }

        public void UnloadAsset<T>(T asset) where T : UnityEngine.Object
        {
            if (asset == null) return;

            var entry = _assetCache.FirstOrDefault(kvp => kvp.Value.Asset == asset);
            if (entry.Key != null)
            {
                ReleaseAsset(entry.Key);
            }
        }

        /// <summary>
        /// Release a specific asset
        /// </summary>
        public void ReleaseAsset(string address)
        {
            if (_activeHandles.TryGetValue(address, out var handle))
            {
                Addressables.Release(handle);
                _activeHandles.Remove(address);
            }

            if (_assetCache.ContainsKey(address))
            {
                _assetCache.Remove(address);
            }
        }

        /// <summary>
        /// Release all loaded assets
        /// </summary>
        public void ReleaseAllAssets()
        {
            foreach (var handle in _activeHandles.Values)
            {
                Addressables.Release(handle);
            }
            _activeHandles.Clear();
            _assetCache.Clear();

            if (_enableLogging)
                Logger.Log("ASSETS", "Released all assets");
        }

        /// <summary>
        /// Release non-critical assets to free memory
        /// </summary>
        public void ReleaseNonCriticalAssets()
        {
            var assetsToRelease = _assetCache.Keys
                .Where(k => !_preloadedAssets.Contains(k))
                .ToList();

            foreach (var assetKey in assetsToRelease)
            {
                ReleaseAsset(assetKey);
            }

            if (_enableLogging)
                Logger.Log("ASSETS", $"Released {assetsToRelease.Count} non-critical assets");
        }

        #endregion

        #region Cache Management

        public void ClearCache()
        {
            _assetCache.Clear();
            if (_enableLogging)
                Logger.Log("ASSETS", "Cleared asset cache");
        }

        public void ClearCache(bool persistentOnly)
        {
            if (persistentOnly)
            {
                var keysToRemove = _assetCache.Keys.Where(k => !_preloadedAssets.Contains(k)).ToList();
                foreach (var key in keysToRemove)
                {
                    _assetCache.Remove(key);
                }
            }
            else
            {
                ClearCache();
            }
        }

        public AssetCacheEntry[] GetCacheEntries()
        {
            return _assetCache.Values.ToArray();
        }

        #endregion

        #region Cleanup

        /// <summary>
        /// Cleanup all resources
        /// </summary>
        public void Cleanup()
        {
            ReleaseAllAssets();
            _preloadedAssets.Clear();
            _isInitialized = false;
        }

        #endregion
    }
#else
    /// <summary>
    /// Stub implementation when Addressables is not available - Management part
    /// </summary>
    public partial class AddressablesService : IAssetManager
    {
        public void UnloadAsset(string assetPath) { }
        public void UnloadAsset<T>(T asset) where T : UnityEngine.Object { }
        public void ReleaseAsset(string address) { }
        public void ReleaseAllAssets() { }
        public void ReleaseNonCriticalAssets() { }
        public void ClearCache() { }
        public void ClearCache(bool persistentOnly) { }
        public AssetCacheEntry[] GetCacheEntries() => new AssetCacheEntry[0];
        public void Cleanup() { }
    }
#endif
}
