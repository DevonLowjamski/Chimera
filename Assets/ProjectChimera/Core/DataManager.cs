using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using Logger = ProjectChimera.Core.Logging.ChimeraLogger;

namespace ProjectChimera.Core
{
    /// <summary>
    /// BASIC: Simple data manager for Project Chimera's data assets.
    /// Focuses on essential data loading and access without complex registries and validation.
    /// </summary>
    public class DataManager : MonoBehaviour
    {
        [Header("Basic Data Settings")]
        [SerializeField] private bool _enableBasicLoading = true;
        [SerializeField] private bool _enableLogging = true;

        // Basic data storage
        private readonly Dictionary<string, ScriptableObject> _loadedData = new Dictionary<string, ScriptableObject>();
        private bool _isInitialized = false;

        /// <summary>
        /// Events for data operations
        /// </summary>
        public event System.Action<string> OnDataLoaded;
        public event System.Action<string, string> OnDataLoadError;

        /// <summary>
        /// Initialize basic data manager
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            if (_enableBasicLoading)
            {
                LoadBasicData();
            }

            _isInitialized = true;

            if (_enableLogging)
            {
                Logger.LogInfo("DataManager", "Initialized");
            }
        }

        /// <summary>
        /// Load a data asset by name
        /// </summary>
        public T LoadData<T>(string assetName) where T : ScriptableObject
        {
            if (!_enableBasicLoading || !_isInitialized) return null;

            // Check if already loaded
            if (_loadedData.ContainsKey(assetName))
            {
                return _loadedData[assetName] as T;
            }

            // Load from ServiceContainer IAssetManager (with Resources fallback)
            var serviceContainer = ServiceContainerFactory.Instance;
            var assetManager = serviceContainer.TryResolve<IAssetManager>();
            T data = null;

            if (assetManager != null)
            {
                data = assetManager.LoadAsset<T>(assetName);
            }
            else
            {
                // No fallback to Resources.Load - log warning and fail gracefully
                Logger.LogWarning("DATA", $"Asset manager not available for data asset: {assetName}. Configure AddressablesAssetManager.", this);
                data = null;
            }
            if (data != null)
            {
                _loadedData[assetName] = data;
                OnDataLoaded?.Invoke(assetName);

                if (_enableLogging)
                {
                    Logger.LogInfo("DataManager", $"Loaded data: {assetName}");
                }

                return data;
            }
            else
            {
                OnDataLoadError?.Invoke(assetName, "Data asset not found in Resources");

                if (_enableLogging)
                {
                    Logger.LogInfo("DataManager", $"Failed to load data: {assetName}");
                }

                return null;
            }
        }

        /// <summary>
        /// Unload data asset
        /// </summary>
        public void UnloadData(string assetName)
        {
            if (_loadedData.Remove(assetName))
            {
                if (_enableLogging)
                {
                    Logger.LogInfo("DataManager", $"Unloaded data: {assetName}");
                }
            }
        }

        /// <summary>
        /// Check if data is loaded
        /// </summary>
        public bool IsDataLoaded(string assetName)
        {
            return _loadedData.ContainsKey(assetName);
        }

        /// <summary>
        /// Get loaded data count
        /// </summary>
        public int GetLoadedDataCount()
        {
            return _loadedData.Count;
        }

        /// <summary>
        /// Get all loaded data names
        /// </summary>
        public List<string> GetLoadedDataNames()
        {
            return new List<string>(_loadedData.Keys);
        }

        /// <summary>
        /// Clear all loaded data
        /// </summary>
        public void ClearAllData()
        {
            _loadedData.Clear();

            if (_enableLogging)
            {
                Logger.LogInfo("DataManager", "Cleared all data");
            }
        }

        /// <summary>
        /// Get data manager statistics
        /// </summary>
        public DataManagerStats GetStats()
        {
            return new DataManagerStats
            {
                LoadedDataCount = _loadedData.Count,
                IsDataLoadingEnabled = _enableBasicLoading,
                IsInitialized = _isInitialized
            };
        }

        /// <summary>
        /// Load common data assets
        /// </summary>
        public void LoadCommonData()
        {
            if (!_enableBasicLoading || !_isInitialized) return;

            // Load some commonly used data assets
            string[] commonData = {
                "Data/Strains/BasicStrains",
                "Data/Equipment/BasicEquipment",
                "Data/Settings/DefaultSettings"
            };

            foreach (string assetPath in commonData)
            {
                LoadData<ScriptableObject>(assetPath);
            }

            if (_enableLogging)
            {
                Logger.LogInfo("DataManager", "Loaded common data");
            }
        }

        #region Private Methods

        private void LoadBasicData()
        {
            // Load any essential data assets that should be available at startup
            // This could be expanded based on game requirements
        }

        #endregion
    }

    /// <summary>
    /// Data manager statistics
    /// </summary>
    [System.Serializable]
    public struct DataManagerStats
    {
        public int LoadedDataCount;
        public bool IsDataLoadingEnabled;
        public bool IsInitialized;
    }
}
