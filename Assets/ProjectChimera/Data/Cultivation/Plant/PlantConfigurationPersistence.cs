using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Data.Cultivation.Plant
{
    /// <summary>
    /// PHASE 0 REFACTORED: Plant Configuration Persistence
    /// Single Responsibility: Save/load configurations and manage profiles
    /// Extracted from PlantSyncConfigurationManager (736 lines → 4 files <500 lines each)
    /// </summary>
    public class PlantConfigurationPersistence
    {
        private readonly Dictionary<string, PlantSyncConfiguration> _profiles;
        private readonly string _configurationKey;
        private readonly bool _enableLogging;

        public event Action OnConfigurationSaved;

        public PlantConfigurationPersistence(string configurationKey, bool enableLogging)
        {
            _profiles = new Dictionary<string, PlantSyncConfiguration>();
            _configurationKey = configurationKey;
            _enableLogging = enableLogging;

            InitializeDefaultProfiles();
        }

        /// <summary>
        /// Initialize default profiles
        /// </summary>
        private void InitializeDefaultProfiles()
        {
            // Default profile
            _profiles["Default"] = PlantSyncConfiguration.CreateDefault();

            // Performance profile
            _profiles["Performance"] = new PlantSyncConfiguration
            {
                AutoSyncEnabled = true,
                SyncFrequency = 0.5f,
                ValidateDataIntegrity = false,
                EnableLogging = false,
                DefaultSyncDirection = SyncDirection.FromComponentsToData,
                BatchSize = 20,
                EnableBatching = true,
                OperationTimeoutSeconds = 10f,
                MaxRetryAttempts = 1,
                RetryDelayMultiplier = 1.5f,
                EnablePerformanceTracking = true,
                PerformanceAlertThreshold = 50f
            };

            // Debug profile
            _profiles["Debug"] = new PlantSyncConfiguration
            {
                AutoSyncEnabled = true,
                SyncFrequency = 2f,
                ValidateDataIntegrity = true,
                EnableLogging = true,
                DefaultSyncDirection = SyncDirection.Bidirectional,
                BatchSize = 5,
                EnableBatching = false,
                OperationTimeoutSeconds = 60f,
                MaxRetryAttempts = 5,
                RetryDelayMultiplier = 2f,
                EnablePerformanceTracking = true,
                PerformanceAlertThreshold = 200f
            };
        }

        /// <summary>
        /// Save configuration
        /// </summary>
        public bool SaveConfiguration(PlantSyncConfiguration config, ref ConfigurationStats stats)
        {
            try
            {
                var configJson = JsonUtility.ToJson(config, true);
                PlayerPrefs.SetString(_configurationKey, configJson);
                PlayerPrefs.Save();

                stats.ConfigurationSaves++;
                OnConfigurationSaved?.Invoke();

                if (_enableLogging)
                {
                    ChimeraLogger.Log("PLANT", "Configuration saved to persistent storage", null);
                }
                return true;
            }
            catch (Exception ex)
            {
                stats.SaveFailures++;
                if (_enableLogging)
                {
                    ChimeraLogger.LogError("PLANT", $"Failed to save configuration: {ex.Message}", null);
                }
                return false;
            }
        }

        /// <summary>
        /// Load configuration
        /// </summary>
        public PlantSyncConfiguration LoadConfiguration(PlantSyncConfiguration defaultConfig, ref ConfigurationStats stats)
        {
            try
            {
                if (PlayerPrefs.HasKey(_configurationKey))
                {
                    var configJson = PlayerPrefs.GetString(_configurationKey);
                    if (!string.IsNullOrEmpty(configJson))
                    {
                        var loadedConfig = JsonUtility.FromJson<PlantSyncConfiguration>(configJson);
                        stats.ConfigurationLoads++;
                        if (_enableLogging)
                        {
                            ChimeraLogger.Log("PLANT", "Configuration loaded from persistent storage", null);
                        }
                        return loadedConfig;
                    }
                }
            }
            catch (Exception ex)
            {
                stats.LoadFailures++;
                if (_enableLogging)
                {
                    ChimeraLogger.LogError("PLANT", $"Failed to load configuration: {ex.Message}. Loading defaults.", null);
                }
            }
            return defaultConfig;
        }

        /// <summary>
        /// Save all profiles
        /// </summary>
        public bool SaveProfiles(Dictionary<string, PlantSyncConfiguration> profiles)
        {
            try
            {
                var profileData = SerializableProfiles.Create(profiles);
                var profilesJson = JsonUtility.ToJson(profileData, true);
                PlayerPrefs.SetString(_configurationKey + "_Profiles", profilesJson);
                PlayerPrefs.Save();

                if (_enableLogging)
                {
                    ChimeraLogger.Log("PLANT", "Configuration profiles saved", null);
                }
                return true;
            }
            catch (Exception ex)
            {
                if (_enableLogging)
                {
                    ChimeraLogger.LogError("PLANT", $"Failed to save profiles: {ex.Message}", null);
                }
                return false;
            }
        }

        /// <summary>
        /// Load all profiles
        /// </summary>
        public Dictionary<string, PlantSyncConfiguration> LoadProfiles()
        {
            try
            {
                if (PlayerPrefs.HasKey(_configurationKey + "_Profiles"))
                {
                    var profilesJson = PlayerPrefs.GetString(_configurationKey + "_Profiles");
                    if (!string.IsNullOrEmpty(profilesJson))
                    {
                        var profileData = JsonUtility.FromJson<SerializableProfiles>(profilesJson);
                        if (profileData.Profiles != null)
                        {
                            if (_enableLogging)
                            {
                                ChimeraLogger.Log("PLANT", $"Configuration profiles loaded ({profileData.Profiles.Count})", null);
                            }
                            return profileData.Profiles;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                if (_enableLogging)
                {
                    ChimeraLogger.LogError("PLANT", $"Failed to load profiles: {ex.Message}. Returning defaults.", null);
                }
            }
            return new Dictionary<string, PlantSyncConfiguration>(_profiles);
        }

        /// <summary>
        /// Get available profiles
        /// </summary>
        public List<string> GetAvailableProfiles()
        {
            return _profiles.Keys.ToList();
        }

        /// <summary>
        /// Get profile by name
        /// </summary>
        public PlantSyncConfiguration? GetProfile(string profileName)
        {
            return _profiles.TryGetValue(profileName, out var profile) ? profile : (PlantSyncConfiguration?)null;
        }

        /// <summary>
        /// Add or update profile
        /// </summary>
        public void SetProfile(string profileName, PlantSyncConfiguration config)
        {
            _profiles[profileName] = config;
        }

        /// <summary>
        /// Remove profile
        /// </summary>
        public bool RemoveProfile(string profileName)
        {
            if (profileName == "Default")
                return false; // Cannot remove default profile

            return _profiles.Remove(profileName);
        }
    }
}

