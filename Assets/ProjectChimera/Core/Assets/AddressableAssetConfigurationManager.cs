using UnityEngine;
using System;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core.Assets
{
    /// <summary>
    /// PHASE 0 REFACTORED: Addressable Asset Configuration Manager Coordinator
    /// Single Responsibility: Orchestrate configuration components
    /// BEFORE: 859 lines (massive SRP violation)
    /// AFTER: 4 files <500 lines each (AssetConfigurationData, AssetConfigurationValidator, AssetConfigurationPersistence, this coordinator)
    /// </summary>
    public class AddressableAssetConfigurationManager
    {
        [Header("Configuration Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private bool _persistConfiguration = true;
        [SerializeField] private string _configurationPath = "AssetManagerConfig";
        [SerializeField] private bool _validateOnLoad = true;

        // PHASE 0: Component-based architecture (SRP)
        private AssetConfigurationValidator _validator;
        private AssetConfigurationPersistence _persistence;

        // Core configuration
        private AssetManagerConfiguration _configuration;
        private Dictionary<string, AssetManagerConfiguration> _configurationProfiles;
        private string _activeProfile = "Default";

        // State tracking
        private bool _isInitialized = false;
        private bool _isDirty = false;
        private DateTime _lastConfigChange = DateTime.Now;
        private List<ConfigurationIssue> _validationIssues = new List<ConfigurationIssue>();

        // Statistics
        private ConfigurationStats _stats;

        // Events
        public event Action<AssetManagerConfiguration> OnConfigurationChanged;
        public event Action<string> OnProfileChanged;
        public event Action<List<ConfigurationIssue>> OnValidationComplete;
        public event Action OnConfigurationSaved;

        // Public properties
        public bool IsInitialized => _isInitialized;
        public AssetManagerConfiguration Configuration => _configuration;
        public string ActiveProfile => _activeProfile;
        public bool IsDirty => _isDirty;
        public bool IsValid => _validationIssues.Count == 0;
        public List<ConfigurationIssue> ValidationIssues => new List<ConfigurationIssue>(_validationIssues);
        public ConfigurationStats Stats => _stats;

        /// <summary>
        /// Initialize configuration manager
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            // Initialize components
            _validator = new AssetConfigurationValidator();
            _persistence = new AssetConfigurationPersistence(_configurationPath, _enableLogging);
            _persistence.OnConfigurationSaved += HandleConfigurationSaved;

            // Load configuration
            _configuration = AssetManagerConfiguration.CreateDefault();
            _configurationProfiles = AssetConfigurationPersistence.CreateDefaultProfiles();

            if (_persistConfiguration)
            {
                LoadPersistedConfiguration();
            }

            if (_validateOnLoad)
            {
                ValidateConfiguration();
            }

            _stats = ConfigurationStats.CreateEmpty();
            _isInitialized = true;
            _isDirty = false;

            if (_enableLogging)
            {
                ChimeraLogger.Log("ASSETS", "Addressable Asset Configuration Manager initialized", null);
            }
        }

        /// <summary>
        /// Update configuration with validation
        /// </summary>
        public ConfigurationUpdateResult UpdateConfiguration(AssetManagerConfiguration newConfiguration)
        {
            if (!_isInitialized)
            {
                return new ConfigurationUpdateResult
                {
                    Success = false,
                    ErrorMessage = "Configuration manager not initialized"
                };
            }

            var validationResult = _validator.ValidateConfiguration(newConfiguration);

            if (validationResult.HasCriticalIssues)
            {
                return new ConfigurationUpdateResult
                {
                    Success = false,
                    ErrorMessage = "Configuration has critical validation issues",
                    ValidationIssues = validationResult.Issues
                };
            }

            var previousConfig = _configuration;
            _configuration = newConfiguration;
            _isDirty = true;
            _lastConfigChange = DateTime.Now;
            _stats.ConfigurationUpdates++;

            OnConfigurationChanged?.Invoke(_configuration);

            if (_persistConfiguration)
            {
                SaveConfiguration();
            }

            if (_enableLogging)
            {
                ChimeraLogger.Log("ASSETS", $"Configuration updated for profile '{_activeProfile}'", null);
            }

            return new ConfigurationUpdateResult
            {
                Success = true,
                PreviousConfiguration = previousConfig,
                ValidationIssues = validationResult.Issues
            };
        }

        /// <summary>
        /// Update individual configuration parameter
        /// </summary>
        public bool UpdateParameter(string parameterName, object value)
        {
            if (!_isInitialized) return false;

            var updated = false;
            var config = _configuration;

            switch (parameterName.ToLower())
            {
                case "enablecaching":
                    if (value is bool caching) { config.EnableCaching = caching; updated = true; }
                    break;
                case "maxcachesize":
                    if (value is int cacheSize) { config.MaxCacheSize = Mathf.Max(1, cacheSize); updated = true; }
                    break;
                case "maxmemoryusage":
                    if (value is long memoryUsage) { config.MaxMemoryUsage = Math.Max(1024L, memoryUsage); updated = true; }
                    else if (value is float memF) { config.MaxMemoryUsage = (long)Mathf.Max(1024f, memF); updated = true; }
                    break;
                case "preloadcriticalassets":
                    if (value is bool preload) { config.PreloadCriticalAssets = preload; updated = true; }
                    break;
                case "enableparallelloading":
                    if (value is bool parallel) { config.EnableParallelLoading = parallel; updated = true; }
                    break;
                case "maxconcurrentloads":
                    if (value is int maxConcurrent) { config.MaxConcurrentLoads = Mathf.Max(1, maxConcurrent); updated = true; }
                    break;
                case "loadtimeoutseconds":
                    if (value is float timeout) { config.LoadTimeoutSeconds = Mathf.Max(1f, timeout); updated = true; }
                    break;
                case "autoreleaseenabled":
                    if (value is bool autoRelease) { config.AutoReleaseEnabled = autoRelease; updated = true; }
                    break;
                case "autoreleaseinterval":
                    if (value is float interval) { config.AutoReleaseInterval = Mathf.Max(1f, interval); updated = true; }
                    break;
                case "maxretainedhandles":
                    if (value is int maxHandles) { config.MaxRetainedHandles = Mathf.Max(1, maxHandles); updated = true; }
                    break;
                case "enabledetailedstatistics":
                    if (value is bool stats) { config.EnableDetailedStatistics = stats; updated = true; }
                    break;
                case "statisticsupdateinterval":
                    if (value is float statInterval) { config.StatisticsUpdateInterval = Mathf.Max(0.1f, statInterval); updated = true; }
                    break;
                case "maxperformancehistoryentries":
                    if (value is int maxHistory) { config.MaxPerformanceHistoryEntries = Mathf.Max(1, maxHistory); updated = true; }
                    break;
                case "enablelogging":
                    if (value is bool logging) { config.EnableLogging = logging; updated = true; }
                    break;
            }

            if (updated)
            {
                _configuration = config;
                _isDirty = true;
                _stats.ParameterUpdates++;

                OnConfigurationChanged?.Invoke(_configuration);

                if (_persistConfiguration)
                {
                    SaveConfiguration();
                }

                if (_enableLogging)
                {
                    ChimeraLogger.Log("ASSETS", $"Parameter '{parameterName}' updated", null);
                }
            }

            return updated;
        }

        /// <summary>
        /// Create new profile
        /// </summary>
        public bool CreateProfile(string profileName, AssetManagerConfiguration configuration)
        {
            if (!_isInitialized || string.IsNullOrEmpty(profileName) || _configurationProfiles.ContainsKey(profileName))
                return false;

            _configurationProfiles[profileName] = configuration;
            _stats.ProfilesCreated++;

            if (_persistConfiguration)
            {
                _persistence.SaveProfiles(_configurationProfiles);
            }

            if (_enableLogging)
            {
                ChimeraLogger.Log("ASSETS", $"Profile '{profileName}' created", null);
            }

            return true;
        }

        /// <summary>
        /// Load profile
        /// </summary>
        public bool LoadProfile(string profileName)
        {
            if (!_isInitialized || !_configurationProfiles.ContainsKey(profileName))
                return false;

            _configuration = _configurationProfiles[profileName];
            _activeProfile = profileName;
            _isDirty = true;
            _lastConfigChange = DateTime.Now;
            _stats.ProfileSwitches++;

            OnProfileChanged?.Invoke(_activeProfile);
            OnConfigurationChanged?.Invoke(_configuration);

            if (_persistConfiguration)
            {
                SaveConfiguration();
            }

            if (_enableLogging)
            {
                ChimeraLogger.Log("ASSETS", $"Profile '{profileName}' loaded", null);
            }

            return true;
        }

        /// <summary>
        /// Save current configuration as profile
        /// </summary>
        public bool SaveAsProfile(string profileName)
        {
            if (!_isInitialized || string.IsNullOrEmpty(profileName))
                return false;

            _configurationProfiles[profileName] = _configuration;
            _stats.ProfilesSaved++;

            if (_persistConfiguration)
            {
                _persistence.SaveProfiles(_configurationProfiles);
            }

            if (_enableLogging)
            {
                ChimeraLogger.Log("ASSETS", $"Configuration saved as profile '{profileName}'", null);
            }

            return true;
        }

        /// <summary>
        /// Delete profile
        /// </summary>
        public bool DeleteProfile(string profileName)
        {
            if (!_isInitialized || profileName == "Default" || !_configurationProfiles.ContainsKey(profileName))
                return false;

            _configurationProfiles.Remove(profileName);
            _stats.ProfilesDeleted++;

            if (_activeProfile == profileName)
            {
                LoadProfile("Default");
            }

            if (_persistConfiguration)
            {
                _persistence.SaveProfiles(_configurationProfiles);
            }

            if (_enableLogging)
            {
                ChimeraLogger.Log("ASSETS", $"Profile '{profileName}' deleted", null);
            }

            return true;
        }

        /// <summary>
        /// Get available profiles
        /// </summary>
        public List<string> GetAvailableProfiles()
        {
            return new List<string>(_configurationProfiles.Keys);
        }

        /// <summary>
        /// Validate configuration
        /// </summary>
        public ConfigurationValidationResult ValidateConfiguration(AssetManagerConfiguration? configToValidate = null)
        {
            if (!_isInitialized)
                return new ConfigurationValidationResult(new List<ConfigurationIssue>());

            var config = configToValidate ?? _configuration;
            var result = _validator.ValidateConfiguration(config);

            _validationIssues = result.Issues;
            _stats.ValidationAttempts++;

            OnValidationComplete?.Invoke(_validationIssues);

            if (_enableLogging && _validationIssues.Count > 0)
            {
                ChimeraLogger.LogWarning("ASSETS", $"Configuration validation found {_validationIssues.Count} issues", null);
            }

            return result;
        }

        /// <summary>
        /// Reset configuration to defaults
        /// </summary>
        public void ResetToDefaults()
        {
            if (!_isInitialized) return;

            _configuration = AssetManagerConfiguration.CreateDefault();
            _isDirty = true;
            _lastConfigChange = DateTime.Now;
            _stats.ConfigurationResets++;

            OnConfigurationChanged?.Invoke(_configuration);

            if (_persistConfiguration)
            {
                SaveConfiguration();
            }

            if (_enableLogging)
            {
                ChimeraLogger.Log("ASSETS", "Configuration reset to defaults", null);
            }
        }

        /// <summary>
        /// Get configuration summary
        /// </summary>
        public ConfigurationSummary GetConfigurationSummary()
        {
            return new ConfigurationSummary
            {
                CurrentConfiguration = _configuration,
                ActiveProfile = _activeProfile,
                AvailableProfiles = GetAvailableProfiles(),
                IsDirty = _isDirty,
                IsValid = IsValid,
                ValidationIssues = _validationIssues,
                LastConfigChange = _lastConfigChange,
                Stats = _stats
            };
        }

        /// <summary>
        /// Set persistence enabled
        /// </summary>
        public void SetPersistenceEnabled(bool enabled)
        {
            _persistConfiguration = enabled;

            if (_enableLogging)
            {
                ChimeraLogger.Log("ASSETS", $"Configuration persistence {(enabled ? "enabled" : "disabled")}", null);
            }
        }

        #region Private Methods

        /// <summary>
        /// Save configuration to persistent storage
        /// </summary>
        private void SaveConfiguration()
        {
            if (_persistence.SaveConfiguration(_configuration))
            {
                _isDirty = false;
                _stats.ConfigurationSaves++;
            }
            else
            {
                _stats.SaveFailures++;
            }
        }

        /// <summary>
        /// Load configuration from persistent storage
        /// </summary>
        private void LoadPersistedConfiguration()
        {
            var (success, config) = _persistence.LoadConfiguration();
            if (success && config.HasValue)
            {
                _configuration = config.Value;
                _stats.ConfigurationLoads++;
            }
            else
            {
                _stats.LoadFailures++;
            }
        }

        /// <summary>
        /// Handle configuration saved event
        /// </summary>
        private void HandleConfigurationSaved()
        {
            OnConfigurationSaved?.Invoke();
        }

        #endregion
    }
}

