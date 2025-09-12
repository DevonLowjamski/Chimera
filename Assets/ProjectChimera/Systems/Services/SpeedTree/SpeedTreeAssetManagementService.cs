using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Services.SpeedTree
{
    /// <summary>
    /// BASIC: Simple SpeedTree asset management service for Project Chimera.
    /// Focuses on essential asset loading without complex SpeedTree configurations and strain databases.
    /// </summary>
    public class SpeedTreeAssetManagementService : MonoBehaviour
    {
        [Header("Basic Asset Settings")]
        [SerializeField] private bool _enableBasicAssetManagement = true;
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private List<GameObject> _plantPrefabs = new List<GameObject>();

        // Basic asset tracking
        private readonly Dictionary<string, GameObject> _loadedAssets = new Dictionary<string, GameObject>();
        private readonly List<GameObject> _activeInstances = new List<GameObject>();
        private bool _isInitialized = false;

        /// <summary>
        /// Events for asset management
        /// </summary>
        public event System.Action<GameObject> OnAssetLoaded;
        public event System.Action<GameObject> OnAssetUnloaded;
        public event System.Action<GameObject> OnInstanceCreated;

        /// <summary>
        /// Initialize basic asset management
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("[SpeedTreeAssetManagementService] Initialized successfully");
            }
        }

        /// <summary>
        /// Load plant asset by name
        /// </summary>
        public GameObject LoadPlantAsset(string assetName)
        {
            if (!_enableBasicAssetManagement || !_isInitialized) return null;

            // Check if already loaded
            if (_loadedAssets.TryGetValue(assetName, out var existingAsset))
            {
                return existingAsset;
            }

            // Try to find in prefabs list
            foreach (var prefab in _plantPrefabs)
            {
                if (prefab != null && prefab.name == assetName)
                {
                    _loadedAssets[assetName] = prefab;
                    OnAssetLoaded?.Invoke(prefab);

                    if (_enableLogging)
                    {
                        ChimeraLogger.Log($"[SpeedTreeAssetManagementService] Loaded asset: {assetName}");
                    }

                    return prefab;
                }
            }

            // Try to load from Resources
            var loadedAsset = Resources.Load<GameObject>(assetName);
            if (loadedAsset != null)
            {
                _loadedAssets[assetName] = loadedAsset;
                OnAssetLoaded?.Invoke(loadedAsset);

                if (_enableLogging)
                {
                    ChimeraLogger.Log($"[SpeedTreeAssetManagementService] Loaded asset from Resources: {assetName}");
                }

                return loadedAsset;
            }

            if (_enableLogging)
            {
                ChimeraLogger.LogWarning($"[SpeedTreeAssetManagementService] Asset not found: {assetName}");
            }

            return null;
        }

        /// <summary>
        /// Unload plant asset
        /// </summary>
        public void UnloadPlantAsset(string assetName)
        {
            if (_loadedAssets.Remove(assetName))
            {
                OnAssetUnloaded?.Invoke(null); // Could pass the asset if we stored it

                if (_enableLogging)
                {
                    ChimeraLogger.Log($"[SpeedTreeAssetManagementService] Unloaded asset: {assetName}");
                }
            }
        }

        /// <summary>
        /// Create plant instance
        /// </summary>
        public GameObject CreatePlantInstance(string assetName, Vector3 position, Quaternion rotation)
        {
            var prefab = LoadPlantAsset(assetName);
            if (prefab == null) return null;

            var instance = Instantiate(prefab, position, rotation);
            instance.name = $"{assetName}_Instance";
            _activeInstances.Add(instance);

            OnInstanceCreated?.Invoke(instance);

            if (_enableLogging)
            {
                ChimeraLogger.Log($"[SpeedTreeAssetManagementService] Created instance: {instance.name}");
            }

            return instance;
        }

        /// <summary>
        /// Destroy plant instance
        /// </summary>
        public void DestroyPlantInstance(GameObject instance)
        {
            if (instance != null && _activeInstances.Remove(instance))
            {
                Destroy(instance);

                if (_enableLogging)
                {
                    ChimeraLogger.Log($"[SpeedTreeAssetManagementService] Destroyed instance: {instance.name}");
                }
            }
        }

        /// <summary>
        /// Get loaded asset by name
        /// </summary>
        public GameObject GetLoadedAsset(string assetName)
        {
            return _loadedAssets.TryGetValue(assetName, out var asset) ? asset : null;
        }

        /// <summary>
        /// Get all loaded asset names
        /// </summary>
        public List<string> GetLoadedAssetNames()
        {
            return new List<string>(_loadedAssets.Keys);
        }

        /// <summary>
        /// Get all active instances
        /// </summary>
        public List<GameObject> GetActiveInstances()
        {
            return new List<GameObject>(_activeInstances);
        }

        /// <summary>
        /// Clear all assets and instances
        /// </summary>
        public void ClearAllAssets()
        {
            // Destroy all instances
            foreach (var instance in _activeInstances.ToArray())
            {
                if (instance != null)
                {
                    Destroy(instance);
                }
            }
            _activeInstances.Clear();

            // Clear loaded assets
            _loadedAssets.Clear();

            if (_enableLogging)
            {
                ChimeraLogger.Log("[SpeedTreeAssetManagementService] Cleared all assets and instances");
            }
        }

        /// <summary>
        /// Set asset management enabled state
        /// </summary>
        public void SetAssetManagementEnabled(bool enabled)
        {
            _enableBasicAssetManagement = enabled;

            if (!enabled)
            {
                ClearAllAssets();
            }

            if (_enableLogging)
            {
                ChimeraLogger.Log($"[SpeedTreeAssetManagementService] Asset management {(enabled ? "enabled" : "disabled")}");
            }
        }

        /// <summary>
        /// Get asset management statistics
        /// </summary>
        public AssetManagementStats GetStats()
        {
            return new AssetManagementStats
            {
                LoadedAssets = _loadedAssets.Count,
                ActiveInstances = _activeInstances.Count,
                IsManagementEnabled = _enableBasicAssetManagement,
                IsInitialized = _isInitialized
            };
        }
    }

    /// <summary>
    /// Asset management statistics
    /// </summary>
    [System.Serializable]
    public struct AssetManagementStats
    {
        public int LoadedAssets;
        public int ActiveInstances;
        public bool IsManagementEnabled;
        public bool IsInitialized;
    }
}
