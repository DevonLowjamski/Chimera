using UnityEngine;
using System.Threading.Tasks;
using System.Threading;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core
{
    /// <summary>
    /// Asset management service interfaces for dependency injection
    /// Eliminates Resources.Load anti-patterns
    /// </summary>

    public enum AssetLoadPriority
    {
        Low,
        Normal,
        High,
        Critical
    }

    [System.Serializable]
    public class AssetLoadRequest
    {
        public string AssetPath;
        public System.Type AssetType;
        public AssetLoadPriority Priority;
        public bool IsPersistent;
        public System.Action<UnityEngine.Object> OnComplete;
        public System.Action<string> OnError;
    }

    [System.Serializable]
    public class AssetCacheEntry
    {
        public UnityEngine.Object Asset;
        public string AssetPath;
        public System.Type AssetType;
        public bool IsPersistent;
        public System.DateTime LastAccessed;
        public int ReferenceCount;
    }

    public interface IAssetManager
    {
        bool IsInitialized { get; }
        int CachedAssetCount { get; }
        long CacheMemoryUsage { get; }

        // Enhanced async initialization
        void Initialize();
        Task InitializeAsync();
        Task InitializeAsync(CancellationToken cancellationToken);

        // Core async loading with cancellation support
        Task<T> LoadAssetAsync<T>(string assetPath) where T : UnityEngine.Object;
        Task<T> LoadAssetAsync<T>(string assetPath, AssetLoadPriority priority) where T : UnityEngine.Object;
        Task<T> LoadAssetAsync<T>(string assetPath, CancellationToken cancellationToken) where T : UnityEngine.Object;
        Task<T> LoadAssetAsync<T>(string assetPath, AssetLoadPriority priority, CancellationToken cancellationToken) where T : UnityEngine.Object;

        // Callback-based async (legacy compatibility)
        void LoadAssetAsync<T>(string assetPath, System.Action<T> onComplete, System.Action<string> onError = null) where T : UnityEngine.Object;

        // Batch loading operations
        Task<IList<T>> LoadAssetsAsync<T>(IList<string> assetPaths) where T : UnityEngine.Object;
        Task<IList<T>> LoadAssetsAsync<T>(IList<string> assetPaths, CancellationToken cancellationToken) where T : UnityEngine.Object;
        Task<IList<T>> LoadAssetsByLabelAsync<T>(string label) where T : UnityEngine.Object;
        Task<IList<T>> LoadAssetsByLabelAsync<T>(string label, CancellationToken cancellationToken) where T : UnityEngine.Object;

        // Asset existence validation
        Task<bool> HasAssetAsync(string assetPath);
        Task<bool> HasAssetAsync(string assetPath, CancellationToken cancellationToken);

        // Enhanced preloading
        void PreloadAssets(string[] assetPaths);
        Task PreloadAssetsAsync(string[] assetPaths);
        Task PreloadAssetsAsync(string[] assetPaths, CancellationToken cancellationToken);

        // Content updates and management
        Task<bool> CheckForContentUpdatesAsync();
        Task<bool> CheckForContentUpdatesAsync(CancellationToken cancellationToken);

        // Synchronous loading (legacy - deprecated)
        [System.Obsolete("Use LoadAssetAsync instead for better performance")]
        T LoadAsset<T>(string assetPath) where T : UnityEngine.Object;

        // Asset lifecycle management
        void UnloadAsset(string assetPath);
        void UnloadAsset<T>(T asset) where T : UnityEngine.Object;
        bool IsAssetLoaded(string assetPath);

        // Cache management
        void ClearCache();
        void ClearCache(bool persistentOnly);
        AssetCacheEntry[] GetCacheEntries();
    }

    public interface IAddressableManager
    {
        bool IsInitialized { get; }

        // Enhanced async initialization
        void Initialize();
        Task InitializeAsync();
        Task InitializeAsync(CancellationToken cancellationToken);

        // Core async loading with cancellation support
        Task<T> LoadAddressableAsync<T>(string address) where T : UnityEngine.Object;
        Task<T> LoadAddressableAsync<T>(string address, AssetLoadPriority priority) where T : UnityEngine.Object;
        Task<T> LoadAddressableAsync<T>(string address, CancellationToken cancellationToken) where T : UnityEngine.Object;
        Task<T> LoadAddressableAsync<T>(string address, AssetLoadPriority priority, CancellationToken cancellationToken) where T : UnityEngine.Object;

        // Callback-based async (legacy compatibility)
        void LoadAddressableAsync<T>(string address, System.Action<T> onComplete, System.Action<string> onError = null) where T : UnityEngine.Object;

        // Batch operations
        Task<IList<T>> LoadAddressablesAsync<T>(IList<string> addresses) where T : UnityEngine.Object;
        Task<IList<T>> LoadAddressablesAsync<T>(IList<string> addresses, CancellationToken cancellationToken) where T : UnityEngine.Object;

        // Enhanced preloading
        Task PreloadAddressablesAsync(string[] addresses);
        Task PreloadAddressablesAsync(string[] addresses, CancellationToken cancellationToken);

        // Addressable existence validation
        Task<bool> HasAddressableAsync(string address);
        Task<bool> HasAddressableAsync(string address, CancellationToken cancellationToken);

        // Content updates
        Task<bool> CheckForCatalogUpdatesAsync();
        Task<bool> CheckForCatalogUpdatesAsync(CancellationToken cancellationToken);
        Task UpdateCatalogsAsync();
        Task UpdateCatalogsAsync(CancellationToken cancellationToken);

        // Asset lifecycle management
        void ReleaseAddressable(string address);
        void ReleaseAddressable<T>(T asset) where T : UnityEngine.Object;
        bool IsAddressableLoaded(string address);

        // Cache management
        void ClearAddressableCache();
    }

    public interface IResourceManager
    {
        bool IsInitialized { get; }

        // Enhanced async initialization
        void Initialize();
        Task InitializeAsync();
        Task InitializeAsync(CancellationToken cancellationToken);

        // Legacy Resources.Load (deprecated)
        [System.Obsolete("Use LoadResourceAsync instead for better performance")]
        T LoadResource<T>(string resourcePath) where T : UnityEngine.Object;

        // Core async loading with cancellation support
        Task<T> LoadResourceAsync<T>(string resourcePath) where T : UnityEngine.Object;
        Task<T> LoadResourceAsync<T>(string resourcePath, CancellationToken cancellationToken) where T : UnityEngine.Object;

        // Batch operations
        Task<IList<T>> LoadResourcesAsync<T>(IList<string> resourcePaths) where T : UnityEngine.Object;
        Task<IList<T>> LoadResourcesAsync<T>(IList<string> resourcePaths, CancellationToken cancellationToken) where T : UnityEngine.Object;

        // Resource existence validation
        Task<bool> HasResourceAsync(string resourcePath);
        Task<bool> HasResourceAsync(string resourcePath, CancellationToken cancellationToken);

        // Resource lifecycle management
        void UnloadResource(string resourcePath);
        void UnloadAllResources();
        bool IsResourceLoaded(string resourcePath);
        string[] GetLoadedResourcePaths();
    }

    public interface IPrefabManager
    {
        bool IsInitialized { get; }

        // Enhanced async initialization
        void Initialize();
        Task InitializeAsync();
        Task InitializeAsync(CancellationToken cancellationToken);

        // Core async instantiation with cancellation support
        Task<GameObject> InstantiatePrefabAsync(string prefabPath);
        Task<GameObject> InstantiatePrefabAsync(string prefabPath, CancellationToken cancellationToken);
        Task<GameObject> InstantiatePrefabAsync(string prefabPath, Transform parent);
        Task<GameObject> InstantiatePrefabAsync(string prefabPath, Transform parent, CancellationToken cancellationToken);
        Task<GameObject> InstantiatePrefabAsync(string prefabPath, Vector3 position, Quaternion rotation);
        Task<GameObject> InstantiatePrefabAsync(string prefabPath, Vector3 position, Quaternion rotation, CancellationToken cancellationToken);

        // Batch instantiation
        Task<IList<GameObject>> InstantiatePrefabsAsync(IList<string> prefabPaths);
        Task<IList<GameObject>> InstantiatePrefabsAsync(IList<string> prefabPaths, CancellationToken cancellationToken);

        // Synchronous instantiation (legacy - deprecated)
        [System.Obsolete("Use InstantiatePrefabAsync instead for better performance")]
        GameObject InstantiatePrefab(string prefabPath);
        [System.Obsolete("Use InstantiatePrefabAsync instead for better performance")]
        GameObject InstantiatePrefab(string prefabPath, Transform parent);

        // Prefab existence validation
        Task<bool> HasPrefabAsync(string prefabPath);
        Task<bool> HasPrefabAsync(string prefabPath, CancellationToken cancellationToken);

        // Prefab registration and management
        void RegisterPrefab(string prefabPath, GameObject prefab);
        void UnregisterPrefab(string prefabPath);
        bool IsPrefabRegistered(string prefabPath);
        string[] GetRegisteredPrefabPaths();
    }
}