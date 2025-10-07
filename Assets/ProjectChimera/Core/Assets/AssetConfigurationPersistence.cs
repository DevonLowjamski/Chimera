using UnityEngine;
using System;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core.Assets
{
    /// <summary>
    /// PHASE 0 REFACTORED: Asset Configuration Persistence
    /// Single Responsibility: Handle save/load of configuration and profiles
    /// Extracted from AddressableAssetConfigurationManager (859 lines → 4 files <500 lines each)
    /// </summary>
    public class AssetConfigurationPersistence
    {
        private readonly string _configurationPath;
        private readonly bool _enableLogging;

        public event Action OnConfigurationSaved;

        public AssetConfigurationPersistence(string configurationPath, bool enableLogging)
        {
            _configurationPath = configurationPath;
            _enableLogging = enableLogging;
        }

        /// <summary>
        /// Save configuration to persistent storage
        /// </summary>
        public bool SaveConfiguration(AssetManagerConfiguration configuration)
        {
            try
            {
                var configJson = JsonUtility.ToJson(configuration, true);
                PlayerPrefs.SetString(_configurationPath, configJson);
                PlayerPrefs.Save();

                OnConfigurationSaved?.Invoke();

                if (_enableLogging)
                {
                    ChimeraLogger.Log("ASSETS", "Configuration saved to persistent storage", null);
                }

                return true;
            }
            catch (Exception ex)
            {
                if (_enableLogging)
                {
                    ChimeraLogger.LogError("ASSETS", $"Failed to save configuration: {ex.Message}", null);
                }
                return false;
            }
        }

        /// <summary>
        /// Load configuration from persistent storage
        /// </summary>
        public (bool success, AssetManagerConfiguration? config) LoadConfiguration()
        {
            try
            {
                if (PlayerPrefs.HasKey(_configurationPath))
                {
                    var configJson = PlayerPrefs.GetString(_configurationPath);
                    if (!string.IsNullOrEmpty(configJson))
                    {
                        var config = JsonUtility.FromJson<AssetManagerConfiguration>(configJson);

                        if (_enableLogging)
                        {
                            ChimeraLogger.Log("ASSETS", "Configuration loaded from persistent storage", null);
                        }

                        return (true, config);
                    }
                }

                return (false, null);
            }
            catch (Exception ex)
            {
                if (_enableLogging)
                {
                    ChimeraLogger.LogError("ASSETS", $"Failed to load configuration: {ex.Message}", null);
                }

                return (false, null);
            }
        }

        /// <summary>
        /// Save profiles to persistent storage
        /// </summary>
        public bool SaveProfiles(Dictionary<string, AssetManagerConfiguration> profiles)
        {
            try
            {
                var profileData = new ProfileContainer { Profiles = profiles };
                var profilesJson = JsonUtility.ToJson(profileData, true);
                PlayerPrefs.SetString(_configurationPath + "_Profiles", profilesJson);
                PlayerPrefs.Save();

                if (_enableLogging)
                {
                    ChimeraLogger.Log("ASSETS", "Configuration profiles saved", null);
                }

                return true;
            }
            catch (Exception ex)
            {
                if (_enableLogging)
                {
                    ChimeraLogger.LogError("ASSETS", $"Failed to save profiles: {ex.Message}", null);
                }
                return false;
            }
        }

        /// <summary>
        /// Load profiles from persistent storage
        /// </summary>
        public (bool success, Dictionary<string, AssetManagerConfiguration>? profiles) LoadProfiles()
        {
            try
            {
                if (PlayerPrefs.HasKey(_configurationPath + "_Profiles"))
                {
                    var profilesJson = PlayerPrefs.GetString(_configurationPath + "_Profiles");
                    if (!string.IsNullOrEmpty(profilesJson))
                    {
                        var profileData = JsonUtility.FromJson<ProfileContainer>(profilesJson);

                        if (_enableLogging)
                        {
                            ChimeraLogger.Log("ASSETS", "Configuration profiles loaded", null);
                        }

                        return (true, profileData.Profiles);
                    }
                }

                return (false, null);
            }
            catch (Exception ex)
            {
                if (_enableLogging)
                {
                    ChimeraLogger.LogError("ASSETS", $"Failed to load profiles: {ex.Message}", null);
                }
                return (false, null);
            }
        }

        /// <summary>
        /// Check if configuration exists in persistent storage
        /// </summary>
        public bool HasPersistedConfiguration()
        {
            return PlayerPrefs.HasKey(_configurationPath);
        }

        /// <summary>
        /// Check if profiles exist in persistent storage
        /// </summary>
        public bool HasPersistedProfiles()
        {
            return PlayerPrefs.HasKey(_configurationPath + "_Profiles");
        }

        /// <summary>
        /// Delete persisted configuration
        /// </summary>
        public void DeletePersistedConfiguration()
        {
            if (PlayerPrefs.HasKey(_configurationPath))
            {
                PlayerPrefs.DeleteKey(_configurationPath);
                PlayerPrefs.Save();

                if (_enableLogging)
                {
                    ChimeraLogger.Log("ASSETS", "Persisted configuration deleted", null);
                }
            }
        }

        /// <summary>
        /// Delete persisted profiles
        /// </summary>
        public void DeletePersistedProfiles()
        {
            if (PlayerPrefs.HasKey(_configurationPath + "_Profiles"))
            {
                PlayerPrefs.DeleteKey(_configurationPath + "_Profiles");
                PlayerPrefs.Save();

                if (_enableLogging)
                {
                    ChimeraLogger.Log("ASSETS", "Persisted profiles deleted", null);
                }
            }
        }

        /// <summary>
        /// Initialize default profiles
        /// </summary>
        public static Dictionary<string, AssetManagerConfiguration> CreateDefaultProfiles()
        {
            var profiles = new Dictionary<string, AssetManagerConfiguration>();

            // Default profile
            profiles["Default"] = AssetManagerConfiguration.CreateDefault();

            // Performance optimized profile
            profiles["Performance"] = new AssetManagerConfiguration
            {
                EnableCaching = true,
                MaxCacheSize = 200,
                MaxMemoryUsage = 1024L * 1024L * 1024L, // 1GB
                PreloadCriticalAssets = true,
                EnableParallelLoading = true,
                MaxConcurrentLoads = 20,
                LoadTimeoutSeconds = 15f,
                AutoReleaseEnabled = true,
                AutoReleaseInterval = 30f,
                MaxRetainedHandles = 100,
                EnableDetailedStatistics = false,
                StatisticsUpdateInterval = 10f,
                MaxPerformanceHistoryEntries = 500,
                EnableLogging = false
            };

            // Memory conservative profile
            profiles["MemoryConservative"] = new AssetManagerConfiguration
            {
                EnableCaching = true,
                MaxCacheSize = 25,
                MaxMemoryUsage = 128L * 1024L * 1024L, // 128MB
                PreloadCriticalAssets = false,
                EnableParallelLoading = false,
                MaxConcurrentLoads = 3,
                LoadTimeoutSeconds = 60f,
                AutoReleaseEnabled = true,
                AutoReleaseInterval = 15f,
                MaxRetainedHandles = 10,
                EnableDetailedStatistics = true,
                StatisticsUpdateInterval = 2f,
                MaxPerformanceHistoryEntries = 200,
                EnableLogging = true
            };

            // Debug profile
            profiles["Debug"] = new AssetManagerConfiguration
            {
                EnableCaching = true,
                MaxCacheSize = 50,
                MaxMemoryUsage = 256L * 1024L * 1024L, // 256MB
                PreloadCriticalAssets = true,
                EnableParallelLoading = true,
                MaxConcurrentLoads = 5,
                LoadTimeoutSeconds = 120f,
                AutoReleaseEnabled = false,
                AutoReleaseInterval = 120f,
                MaxRetainedHandles = 200,
                EnableDetailedStatistics = true,
                StatisticsUpdateInterval = 1f,
                MaxPerformanceHistoryEntries = 2000,
                EnableLogging = true
            };

            return profiles;
        }
    }
}

