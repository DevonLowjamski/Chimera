using UnityEngine;
using System;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Data.Cultivation.Plant
{
    /// <summary>
    /// PHASE 0 REFACTORED: Plant Sync Configuration Manager Coordinator
    /// Single Responsibility: Orchestrate configuration management, validation, and persistence
    /// BEFORE: 736 lines (massive SRP violation)
    /// AFTER: 4 files <500 lines each (PlantSyncDataStructures, PlantConfigurationValidator, PlantConfigurationPersistence, this coordinator)
    /// </summary>
    [Serializable]
    public class PlantSyncConfigurationManager
    {
        [Header("Configuration Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private bool _persistConfiguration = true;
        [SerializeField] private string _configurationKey = "PlantSyncConfig";

        // PHASE 0: Component-based architecture (SRP)
        private PlantConfigurationValidator _validator;
        private PlantConfigurationPersistence _persistence;

        // Core configuration
        [SerializeField] private PlantSyncConfiguration _configuration;
        private string _activeProfile = "Default";

        // Configuration state
        private bool _isInitialized = false;
        private bool _isDirty = false;
        private DateTime _lastConfigChange = DateTime.Now;

        // Statistics
        private ConfigurationStats _stats = new ConfigurationStats();

        // Events
        public event Action<PlantSyncConfiguration> OnConfigurationChanged;
        public event Action<string> OnProfileChanged;
        public event Action<ConfigurationValidationResult> OnValidationComplete;
        public event Action OnConfigurationSaved;

        // Public properties
        public bool IsInitialized => _isInitialized;
        public PlantSyncConfiguration Configuration => _configuration;
        public string ActiveProfile => _activeProfile;
        public bool IsDirty => _isDirty;
        public ConfigurationStats Stats => _stats;
        public DateTime LastConfigChange => _lastConfigChange;

        public void Initialize()
        {
            if (_isInitialized) return;

            // Initialize components
            _validator = new PlantConfigurationValidator(_enableLogging);
            _persistence = new PlantConfigurationPersistence(_configurationKey, _enableLogging);

            // Subscribe to events
            _validator.OnValidationComplete += result => OnValidationComplete?.Invoke(result);
            _persistence.OnConfigurationSaved += () => OnConfigurationSaved?.Invoke();

            LoadDefaultConfiguration();

            if (_persistConfiguration)
            {
                _configuration = _persistence.LoadConfiguration(_configuration, ref _stats);
            }

            _stats.Reset();
            _validator.ValidateConfiguration(_configuration);

            _isInitialized = true;
            _isDirty = false;

            if (_enableLogging)
            {
                ChimeraLogger.Log("PLANT", "Plant Sync Configuration Manager initialized", null);
            }
        }

        /// <summary>
        /// Update configuration with new values
        /// </summary>
        public ConfigurationUpdateResult UpdateConfiguration(PlantSyncConfiguration newConfiguration, bool validate = true)
        {
            if (!_isInitialized)
            {
                return ConfigurationUpdateResult.Failure("Configuration manager not initialized");
            }

            var validationResult = validate ? _validator.ValidateConfiguration(newConfiguration) : ConfigurationValidationResult.Success();

            if (!validationResult.IsValid)
            {
                return new ConfigurationUpdateResult
                {
                    Success = false,
                    ErrorMessage = $"Configuration validation failed: {string.Join(", ", validationResult.Errors)}",
                    ValidationResult = validationResult
                };
            }

            var oldConfiguration = _configuration;
            _configuration = newConfiguration;
            _isDirty = true;
            _lastConfigChange = DateTime.Now;
            _stats.ConfigurationUpdates++;

            OnConfigurationChanged?.Invoke(_configuration);

            if (_persistConfiguration)
            {
                _persistence.SaveConfiguration(_configuration, ref _stats);
            }

            if (_enableLogging)
            {
                ChimeraLogger.Log("PLANT", $"Configuration updated for profile '{_activeProfile}'", null);
            }

            return ConfigurationUpdateResult.CreateSuccess(oldConfiguration);
        }

        /// <summary>
        /// Update specific configuration parameter
        /// </summary>
        public bool UpdateParameter<T>(string parameterName, T value)
        {
            if (!_isInitialized) return false;

            var updated = false;
            var config = _configuration;

            switch (parameterName.ToLower())
            {
                case "autosync":
                    if (value is bool autoSync) { config.AutoSyncEnabled = autoSync; updated = true; }
                    break;
                case "syncfrequency":
                    if (value is float frequency) { config.SyncFrequency = Mathf.Max(0.1f, frequency); updated = true; }
                    break;
                case "validatedata":
                    if (value is bool validate) { config.ValidateDataIntegrity = validate; updated = true; }
                    break;
                case "enablelogging":
                    if (value is bool log) { config.EnableLogging = log; updated = true; }
                    break;
                case "batchsize":
                    if (value is int size) { config.BatchSize = Mathf.Max(1, size); updated = true; }
                    break;
                case "enablebatching":
                    if (value is bool batch) { config.EnableBatching = batch; updated = true; }
                    break;
                case "timeout":
                    if (value is float timeout) { config.OperationTimeoutSeconds = Mathf.Max(1f, timeout); updated = true; }
                    break;
                case "maxretry":
                    if (value is int retry) { config.MaxRetryAttempts = Mathf.Clamp(retry, 0, 10); updated = true; }
                    break;
            }

            if (updated)
            {
                _configuration = config;
                _isDirty = true;
                _lastConfigChange = DateTime.Now;
                _stats.ParameterUpdates++;
                OnConfigurationChanged?.Invoke(_configuration);

                if (_persistConfiguration)
                {
                    _persistence.SaveConfiguration(_configuration, ref _stats);
                }

                if (_enableLogging)
                {
                    ChimeraLogger.Log("PLANT", $"Parameter '{parameterName}' updated", null);
                }
            }

            return updated;
        }

        /// <summary>
        /// Load profile
        /// </summary>
        public bool LoadProfile(string profileName)
        {
            if (!_isInitialized) return false;

            var profile = _persistence.GetProfile(profileName);
            if (!profile.HasValue)
            {
                if (_enableLogging)
                {
                    ChimeraLogger.LogWarning("PLANT", $"Profile '{profileName}' not found", null);
                }
                return false;
            }

            _configuration = profile.Value;
            _activeProfile = profileName;
            _isDirty = false;
            _lastConfigChange = DateTime.Now;
            _stats.ProfileSwitches++;

            OnConfigurationChanged?.Invoke(_configuration);
            OnProfileChanged?.Invoke(_activeProfile);

            if (_persistConfiguration)
            {
                _persistence.SaveConfiguration(_configuration, ref _stats);
            }

            if (_enableLogging)
            {
                ChimeraLogger.Log("PLANT", $"Profile '{profileName}' loaded", null);
            }

            return true;
        }

        /// <summary>
        /// Save as profile
        /// </summary>
        public bool SaveAsProfile(string profileName)
        {
            if (!_isInitialized || string.IsNullOrWhiteSpace(profileName)) return false;

            _persistence.SetProfile(profileName, _configuration);
            _stats.ProfilesSaved++;

            if (_enableLogging)
            {
                ChimeraLogger.Log("PLANT", $"Configuration saved as profile '{profileName}'", null);
            }

            return true;
        }

        /// <summary>
        /// Delete profile
        /// </summary>
        public bool DeleteProfile(string profileName)
        {
            if (!_isInitialized) return false;

            if (_persistence.RemoveProfile(profileName))
            {
                _stats.ProfilesDeleted++;
                if (_activeProfile == profileName)
                {
                    LoadProfile("Default");
                }

                if (_enableLogging)
                {
                    ChimeraLogger.Log("PLANT", $"Profile '{profileName}' deleted", null);
                }

                return true;
            }

            return false;
        }

        /// <summary>
        /// Get available profiles
        /// </summary>
        public List<string> GetAvailableProfiles()
        {
            return _persistence?.GetAvailableProfiles() ?? new List<string>();
        }

        /// <summary>
        /// Validate configuration
        /// </summary>
        public ConfigurationValidationResult ValidateConfiguration(PlantSyncConfiguration? config = null)
        {
            var configToValidate = config ?? _configuration;
            _stats.ValidationAttempts++;
            return _validator.ValidateConfiguration(configToValidate);
        }

        /// <summary>
        /// Reset configuration to defaults
        /// </summary>
        public void ResetToDefaults()
        {
            LoadDefaultConfiguration();
            _isDirty = true;
            _lastConfigChange = DateTime.Now;
            _stats.ConfigurationResets++;

            OnConfigurationChanged?.Invoke(_configuration);

            if (_persistConfiguration)
            {
                _persistence.SaveConfiguration(_configuration, ref _stats);
            }

            if (_enableLogging)
            {
                ChimeraLogger.Log("PLANT", "Configuration reset to defaults", null);
            }
        }

        /// <summary>
        /// Get configuration summary
        /// </summary>
        public ConfigurationSummary GetConfigurationSummary()
        {
            return ConfigurationSummary.Create(
                _configuration,
                _activeProfile,
                GetAvailableProfiles(),
                _isDirty,
                _lastConfigChange,
                ValidateConfiguration(),
                _stats,
                _isInitialized
            );
        }

        /// <summary>
        /// Reset statistics
        /// </summary>
        private void ResetStats()
        {
            _stats.Reset();
        }

        /// <summary>
        /// Load default configuration
        /// </summary>
        private void LoadDefaultConfiguration()
        {
            _configuration = PlantSyncConfiguration.CreateDefault();
        }
    }
}

