using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using System;
using ProjectChimera.Shared;
using ProjectChimera.Core.Events;


namespace ProjectChimera.Core
{
    /// <summary>
    /// Manages all ScriptableObject data assets for Project Chimera.
    /// Provides centralized access, validation, and organization of game data.
    /// </summary>
    public class DataManager : DIChimeraManager, IGameStateListener
    {
        [Header("Data Configuration")]
        [SerializeField] private DataManagerConfigSO _config;
        [SerializeField] private bool _validateDataOnStartup = true;
        [SerializeField] private bool _enableDataCaching = true;
        [SerializeField] private bool _logDataOperations = false;

        [Header("Data Events")]
        [SerializeField] private SimpleGameEventSO _onDataLoaded;
        [SerializeField] private SimpleGameEventSO _onDataValidationComplete;
        [SerializeField] private StringGameEventSO _onDataError;

        // Data registries organized by type
        private readonly Dictionary<Type, List<ChimeraDataSO>> _dataRegistries = new Dictionary<Type, List<ChimeraDataSO>>();
        private readonly Dictionary<string, ChimeraDataSO> _dataByID = new Dictionary<string, ChimeraDataSO>();
        private readonly Dictionary<Type, Dictionary<string, ChimeraDataSO>> _dataByTypeAndID = new Dictionary<Type, Dictionary<string, ChimeraDataSO>>();
        
        // Configuration registries
        private readonly Dictionary<Type, List<ChimeraConfigSO>> _configRegistries = new Dictionary<Type, List<ChimeraConfigSO>>();
        private readonly Dictionary<string, ChimeraConfigSO> _configByID = new Dictionary<string, ChimeraConfigSO>();

        // Performance tracking
        private int _totalDataAssets = 0;
        private int _totalConfigAssets = 0;
        private float _lastValidationTime = 0.0f;

        /// <summary>
        /// Total number of data assets loaded.
        /// </summary>
        public int TotalDataAssets => _totalDataAssets;

        /// <summary>
        /// Total number of configuration assets loaded.
        /// </summary>
        public int TotalConfigAssets => _totalConfigAssets;

        /// <summary>
        /// Whether data caching is enabled for improved performance.
        /// </summary>
        public bool IsDataCachingEnabled => _enableDataCaching;

        /// <summary>
        /// Time when data validation was last performed.
        /// </summary>
        public float LastValidationTime => _lastValidationTime;

        protected override void OnManagerInitialize()
        {
            LogDebug("Initializing Data Manager");

            // Load configuration
            if (_config != null)
            {
                _validateDataOnStartup = _config.ValidateDataOnStartup;
                _enableDataCaching = _config.EnableDataCaching;
                _logDataOperations = _config.LogDataOperations;
            }

            // Load all data assets
            LoadAllDataAssets();

            // Validate data if enabled
            if (_validateDataOnStartup)
            {
                ValidateAllData();
            }

            _onDataLoaded?.Raise();
            LogDebug($"Data Manager initialized - {_totalDataAssets} data assets, {_totalConfigAssets} config assets loaded");
        }

        protected override void OnManagerShutdown()
        {
            LogDebug("Shutting down Data Manager");

            // Clear all registries
            _dataRegistries.Clear();
            _dataByID.Clear();
            _dataByTypeAndID.Clear();
            _configRegistries.Clear();
            _configByID.Clear();

            _totalDataAssets = 0;
            _totalConfigAssets = 0;
        }

        /// <summary>
        /// Loads all data assets from Resources folders.
        /// </summary>
        private void LoadAllDataAssets()
        {
            LogDebug("Loading all data assets");

            // Load all ChimeraDataSO assets
            var dataAssets = Resources.LoadAll<ChimeraDataSO>("");
            foreach (var dataAsset in dataAssets)
            {
                RegisterDataAsset(dataAsset);
            }

            // Load all ChimeraConfigSO assets
            var configAssets = Resources.LoadAll<ChimeraConfigSO>("");
            foreach (var configAsset in configAssets)
            {
                RegisterConfigAsset(configAsset);
            }

            LogDebug($"Loaded {_totalDataAssets} data assets and {_totalConfigAssets} config assets");
        }

        /// <summary>
        /// Registers a data asset in the appropriate registries.
        /// </summary>
        private void RegisterDataAsset(ChimeraDataSO dataAsset)
        {
            if (dataAsset == null) return;

            Type assetType = dataAsset.GetType();
            string assetID = dataAsset.UniqueID;

            // Register in type-based registry
            if (!_dataRegistries.ContainsKey(assetType))
            {
                _dataRegistries[assetType] = new List<ChimeraDataSO>();
            }
            _dataRegistries[assetType].Add(dataAsset);

            // Register in global ID registry
            if (_dataByID.ContainsKey(assetID))
            {
                LogWarning($"Duplicate data asset ID detected: {assetID}. Replacing previous entry.");
            }
            _dataByID[assetID] = dataAsset;

            // Register in type + ID registry
            if (!_dataByTypeAndID.ContainsKey(assetType))
            {
                _dataByTypeAndID[assetType] = new Dictionary<string, ChimeraDataSO>();
            }
            _dataByTypeAndID[assetType][assetID] = dataAsset;

            _totalDataAssets++;

            if (_logDataOperations)
            {
                LogDebug($"Registered data asset: {assetType.Name} - {assetID}");
            }
        }

        /// <summary>
        /// Registers a configuration asset in the appropriate registries.
        /// </summary>
        private void RegisterConfigAsset(ChimeraConfigSO configAsset)
        {
            if (configAsset == null) return;

            Type assetType = configAsset.GetType();
            string assetID = configAsset.UniqueID;

            // Register in type-based registry
            if (!_configRegistries.ContainsKey(assetType))
            {
                _configRegistries[assetType] = new List<ChimeraConfigSO>();
            }
            _configRegistries[assetType].Add(configAsset);

            // Register in global ID registry
            if (_configByID.ContainsKey(assetID))
            {
                LogWarning($"Duplicate config asset ID detected: {assetID}. Replacing previous entry.");
            }
            _configByID[assetID] = configAsset;

            _totalConfigAssets++;

            if (_logDataOperations)
            {
                LogDebug($"Registered config asset: {assetType.Name} - {assetID}");
            }
        }

        /// <summary>
        /// Gets a data asset by its unique ID.
        /// </summary>
        public T GetDataAsset<T>(string assetID) where T : ChimeraDataSO
        {
            if (string.IsNullOrEmpty(assetID))
            {
                LogWarning("Attempted to get data asset with null or empty ID");
                return null;
            }

            if (_dataByID.TryGetValue(assetID, out ChimeraDataSO asset))
            {
                if (asset is T typedAsset)
                {
                    return typedAsset;
                }
                else
                {
                    LogWarning($"Data asset {assetID} found but is not of type {typeof(T).Name}");
                }
            }
            else if (_logDataOperations)
            {
                LogDebug($"Data asset not found: {assetID}");
            }

            return null;
        }

        /// <summary>
        /// Gets all data assets of a specific type.
        /// </summary>
        public List<T> GetDataAssets<T>() where T : ChimeraDataSO
        {
            Type targetType = typeof(T);
            var results = new List<T>();

            if (_dataRegistries.TryGetValue(targetType, out List<ChimeraDataSO> assets))
            {
                foreach (var asset in assets)
                {
                    if (asset is T typedAsset)
                    {
                        results.Add(typedAsset);
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Gets all data assets of a specific type that match a predicate.
        /// </summary>
        public List<T> GetDataAssets<T>(Func<T, bool> predicate) where T : ChimeraDataSO
        {
            return GetDataAssets<T>().Where(predicate).ToList();
        }

        /// <summary>
        /// Gets a configuration asset by its unique ID.
        /// </summary>
        public T GetConfigAsset<T>(string assetID) where T : ChimeraConfigSO
        {
            if (string.IsNullOrEmpty(assetID))
            {
                LogWarning("Attempted to get config asset with null or empty ID");
                return null;
            }

            if (_configByID.TryGetValue(assetID, out ChimeraConfigSO asset))
            {
                if (asset is T typedAsset)
                {
                    return typedAsset;
                }
                else
                {
                    LogWarning($"Config asset {assetID} found but is not of type {typeof(T).Name}");
                }
            }
            else if (_logDataOperations)
            {
                LogDebug($"Config asset not found: {assetID}");
            }

            return null;
        }

        /// <summary>
        /// Gets all configuration assets of a specific type.
        /// </summary>
        public List<T> GetConfigAssets<T>() where T : ChimeraConfigSO
        {
            Type targetType = typeof(T);
            var results = new List<T>();

            if (_configRegistries.TryGetValue(targetType, out List<ChimeraConfigSO> assets))
            {
                foreach (var asset in assets)
                {
                    if (asset is T typedAsset)
                    {
                        results.Add(typedAsset);
                    }
                }
            }

            return results;
        }

        /// <summary>
        /// Gets the first configuration asset of a specific type.
        /// </summary>
        public T GetFirstConfigAsset<T>() where T : ChimeraConfigSO
        {
            var assets = GetConfigAssets<T>();
            return assets.Count > 0 ? assets[0] : null;
        }

        /// <summary>
        /// Checks if a data asset exists with the given ID.
        /// </summary>
        public bool HasDataAsset(string assetID)
        {
            return !string.IsNullOrEmpty(assetID) && _dataByID.ContainsKey(assetID);
        }

        /// <summary>
        /// Checks if a configuration asset exists with the given ID.
        /// </summary>
        public bool HasConfigAsset(string assetID)
        {
            return !string.IsNullOrEmpty(assetID) && _configByID.ContainsKey(assetID);
        }

        /// <summary>
        /// Validates all loaded data assets.
        /// </summary>
        public void ValidateAllData()
        {
            LogDebug("Validating all data assets");
            
            int validationErrors = 0;
            _lastValidationTime = Time.time;

            // Validate data assets
            foreach (var registry in _dataRegistries.Values)
            {
                foreach (var asset in registry)
                {
                    try
                    {
                        if (!asset.ValidateData())
                        {
                            LogError($"Data validation failed for asset: {asset.name} ({asset.GetType().Name})");
                            validationErrors++;
                        }
                    }
                    catch (Exception e)
                    {
                        LogError($"Exception during data validation for {asset.name}: {e.Message}");
                        validationErrors++;
                    }
                }
            }

            // Validate config assets
            foreach (var registry in _configRegistries.Values)
            {
                foreach (var asset in registry)
                {
                    try
                    {
                        if (!asset.ValidateData())
                        {
                            LogError($"Config validation failed for asset: {asset.name} ({asset.GetType().Name})");
                            validationErrors++;
                        }
                    }
                    catch (Exception e)
                    {
                        LogError($"Exception during config validation for {asset.name}: {e.Message}");
                        validationErrors++;
                    }
                }
            }

            if (validationErrors > 0)
            {
                string errorMessage = $"Data validation completed with {validationErrors} errors";
                LogWarning(errorMessage);
                _onDataError?.Raise(errorMessage);
            }
            else
            {
                LogDebug("Data validation completed successfully");
            }

            _onDataValidationComplete?.Raise();
        }

        /// <summary>
        /// Reloads all data assets from Resources.
        /// </summary>
        public void ReloadAllData()
        {
            LogDebug("Reloading all data assets");

            // Clear existing data
            _dataRegistries.Clear();
            _dataByID.Clear();
            _dataByTypeAndID.Clear();
            _configRegistries.Clear();
            _configByID.Clear();

            _totalDataAssets = 0;
            _totalConfigAssets = 0;

            // Reload all data
            LoadAllDataAssets();

            // Validate if enabled
            if (_validateDataOnStartup)
            {
                ValidateAllData();
            }

            _onDataLoaded?.Raise();
            LogDebug("Data reload completed");
        }

        /// <summary>
        /// Gets statistics about loaded data assets.
        /// </summary>
        public DataManagerStats GetStats()
        {
            return new DataManagerStats
            {
                TotalDataAssets = _totalDataAssets,
                TotalConfigAssets = _totalConfigAssets,
                DataTypeCount = _dataRegistries.Count,
                ConfigTypeCount = _configRegistries.Count,
                LastValidationTime = _lastValidationTime,
                CachingEnabled = _enableDataCaching
            };
        }

        /// <summary>
        /// Handles game state changes.
        /// </summary>
        public void OnGameStateChanged(GameState gameState)
        {
            switch (gameState)
            {
                case GameState.Loading:
                    LogDebug("Data Manager: Game entering loading state");
                    break;
                case GameState.Error:
                    LogWarning("Data Manager: Game entered error state, triggering data validation");
                    if (_config?.ValidateOnError == true)
                    {
                        ValidateAllData();
                    }
                    break;
                case GameState.Shutdown:
                    LogDebug("Data Manager: Game shutting down");
                    break;
            }
        }

        /// <summary>
        /// Handles game state changes with previous state context.
        /// </summary>
        public void OnGameStateChanged(GameState previousState, GameState newState)
        {
            OnGameStateChanged(newState);
            
            switch (newState)
            {
                case GameState.Loading:
                    // Could reload data if needed
                    break;
                case GameState.Error:
                    // Could trigger data validation
                    if (_config?.ValidateOnError == true)
                    {
                        ValidateAllData();
                    }
                    break;
            }
        }
    }

    /// <summary>
    /// Statistics for the Data Manager.
    /// </summary>
    [Serializable]
    public struct DataManagerStats
    {
        public int TotalDataAssets;
        public int TotalConfigAssets;
        public int DataTypeCount;
        public int ConfigTypeCount;
        public float LastValidationTime;
        public bool CachingEnabled;
    }

    /// <summary>
    /// Configuration for Data Manager behavior.
    /// </summary>
    [CreateAssetMenu(fileName = "Data Manager Config", menuName = "Project Chimera/Core/Data Manager Config")]
    public class DataManagerConfigSO : ChimeraConfigSO
    {
        [Header("Loading Settings")]
        public bool ValidateDataOnStartup = true;
        public bool EnableDataCaching = true;
        public bool LogDataOperations = false;

        [Header("Validation Settings")]
        public bool ValidateOnError = true;
        public bool EnableAsyncValidation = false;
        
        [Range(1.0f, 60.0f)]
        public float ValidationTimeoutSeconds = 10.0f;

        [Header("Performance Settings")]
        public bool EnableLazyLoading = false;
        public bool PreloadCriticalAssets = true;
        
        [Range(10, 1000)]
        public int MaxCachedAssets = 100;
    }
}