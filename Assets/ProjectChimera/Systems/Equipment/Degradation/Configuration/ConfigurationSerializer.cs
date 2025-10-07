using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using ProjectChimera.Core.Logging;
using Newtonsoft.Json;

namespace ProjectChimera.Systems.Equipment.Degradation.Configuration
{
    /// <summary>
    /// PHASE 0 REFACTORED: Configuration Serializer
    /// Single Responsibility: Handle JSON serialization and deserialization of configuration data
    /// Extracted from ConfigurationPersistenceManager for better SRP compliance
    /// </summary>
    public class ConfigurationSerializer
    {
        private readonly bool _enableLogging;
        private readonly string _configFilePath;
        private PersistenceStatistics _stats;
        private readonly Action<string> _onConfigurationSaved;
        private readonly Action<string> _onConfigurationLoaded;
        private readonly Action<string> _onConfigurationError;

        public ConfigurationSerializer(
            bool enableLogging,
            string configFilePath,
            PersistenceStatistics stats,
            Action<string> onConfigurationSaved,
            Action<string> onConfigurationLoaded,
            Action<string> onConfigurationError)
        {
            _enableLogging = enableLogging;
            _configFilePath = configFilePath;
            _stats = stats;
            _onConfigurationSaved = onConfigurationSaved;
            _onConfigurationLoaded = onConfigurationLoaded;
            _onConfigurationError = onConfigurationError;
        }

        /// <summary>
        /// Save configuration profiles to disk
        /// </summary>
        public bool SaveConfiguration(Dictionary<string, CostConfigurationProfile> profiles)
        {
            if (profiles == null)
                return false;

            try
            {
                var startTime = DateTime.Now;

                // Serialize configuration data
                var configData = new ConfigurationData
                {
                    Profiles = profiles,
                    SavedAt = DateTime.Now,
                    Version = "1.0"
                };

                var jsonSettings = new JsonSerializerSettings
                {
                    Formatting = Formatting.Indented,
                    DateFormatHandling = DateFormatHandling.IsoDateFormat
                };

                var jsonContent = JsonConvert.SerializeObject(configData, jsonSettings);

                // Write to file with atomic operation
                var tempFilePath = _configFilePath + ".tmp";
                File.WriteAllText(tempFilePath, jsonContent);

                if (File.Exists(_configFilePath))
                    File.Delete(_configFilePath);

                File.Move(tempFilePath, _configFilePath);

                var saveTime = (DateTime.Now - startTime).TotalMilliseconds;
                _stats.TotalSaves++;
                _stats.TotalSaveTime += saveTime;
                _stats.LastSaveTime = DateTime.Now;

                _onConfigurationSaved?.Invoke(_configFilePath);

                if (_enableLogging)
                    ChimeraLogger.LogInfo("CONFIG_PERSIST", $"Configuration saved to {_configFilePath} ({saveTime:F1}ms)", null);

                return true;
            }
            catch (Exception ex)
            {
                _stats.SaveErrors++;
                var errorMessage = $"Failed to save configuration: {ex.Message}";

                _onConfigurationError?.Invoke(errorMessage);

                if (_enableLogging)
                    ChimeraLogger.LogError("CONFIG_PERSIST", errorMessage, null);

                return false;
            }
        }

        /// <summary>
        /// Load configuration profiles from disk
        /// </summary>
        public Dictionary<string, CostConfigurationProfile> LoadConfiguration()
        {
            if (!File.Exists(_configFilePath))
                return new Dictionary<string, CostConfigurationProfile>();

            try
            {
                var startTime = DateTime.Now;

                var jsonContent = File.ReadAllText(_configFilePath);

                var jsonSettings = new JsonSerializerSettings
                {
                    DateFormatHandling = DateFormatHandling.IsoDateFormat
                };

                var configData = JsonConvert.DeserializeObject<ConfigurationData>(jsonContent, jsonSettings);

                var loadTime = (DateTime.Now - startTime).TotalMilliseconds;
                _stats.TotalLoads++;
                _stats.TotalLoadTime += loadTime;
                _stats.LastLoadTime = DateTime.Now;

                _onConfigurationLoaded?.Invoke(_configFilePath);

                if (_enableLogging)
                {
                    ChimeraLogger.LogInfo("CONFIG_PERSIST",
                        $"Configuration loaded from {_configFilePath} ({loadTime:F1}ms): {configData.Profiles.Count} profiles", null);
                }

                return configData.Profiles ?? new Dictionary<string, CostConfigurationProfile>();
            }
            catch (Exception ex)
            {
                _stats.LoadErrors++;
                var errorMessage = $"Failed to load configuration: {ex.Message}";

                _onConfigurationError?.Invoke(errorMessage);

                if (_enableLogging)
                    ChimeraLogger.LogError("CONFIG_PERSIST", errorMessage, null);

                return new Dictionary<string, CostConfigurationProfile>();
            }
        }

        /// <summary>
        /// Simple string compression simulation
        /// </summary>
        public string CompressString(string input)
        {
            // This is a placeholder - in real implementation, use GZip, LZ4, etc.
            return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(input));
        }

        /// <summary>
        /// Simple string decompression simulation
        /// </summary>
        public string DecompressString(string compressed)
        {
            // This is a placeholder - in real implementation, use GZip, LZ4, etc.
            try
            {
                var bytes = Convert.FromBase64String(compressed);
                return System.Text.Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                return compressed; // Fallback if not compressed
            }
        }
    }
}

