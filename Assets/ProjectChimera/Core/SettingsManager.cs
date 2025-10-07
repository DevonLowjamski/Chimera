using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core
{
    /// <summary>
    /// BASIC: Simple settings management for Project Chimera.
    /// Focuses on essential settings without complex categories and validation systems.
    /// </summary>
    public class SettingsManager : MonoBehaviour
    {
        [Header("Basic Settings")]
        [SerializeField] private bool _enableAutoSave = true;
        [SerializeField] private bool _enableLogging = true;

        // Basic settings storage
        private readonly Dictionary<string, object> _settings = new Dictionary<string, object>();

        /// <summary>
        /// Events for settings changes
        /// </summary>
        public event System.Action<string> OnSettingChanged;
        public event System.Action OnSettingsLoaded;

        /// <summary>
        /// Initialize basic settings
        /// </summary>
        public void Initialize()
        {
            LoadDefaultSettings();
            LoadSettings();

            if (_enableLogging)
            {
                ChimeraLogger.LogInfo("SettingsManager", "$1");
            }
        }

        /// <summary>
        /// Set a setting value
        /// </summary>
        public void SetSetting(string key, object value)
        {
            _settings[key] = value;
            OnSettingChanged?.Invoke(key);

            if (_enableAutoSave)
            {
                SaveSettings();
            }

            if (_enableLogging)
            {
                ChimeraLogger.LogInfo("SettingsManager", "$1");
            }
        }

        /// <summary>
        /// Get a setting value
        /// </summary>
        public T GetSetting<T>(string key, T defaultValue = default)
        {
            if (_settings.TryGetValue(key, out object value))
            {
                try
                {
                    return (T)value;
                }
                catch
                {
                    if (_enableLogging)
                    {
                        ChimeraLogger.LogInfo("SettingsManager", "$1");
                    }
                    return defaultValue;
                }
            }

            return defaultValue;
        }

        /// <summary>
        /// Check if setting exists
        /// </summary>
        public bool HasSetting(string key)
        {
            return _settings.ContainsKey(key);
        }

        /// <summary>
        /// Remove a setting
        /// </summary>
        public void RemoveSetting(string key)
        {
            if (_settings.Remove(key))
            {
                OnSettingChanged?.Invoke(key);

                if (_enableAutoSave)
                {
                    SaveSettings();
                }

                if (_enableLogging)
                {
                    ChimeraLogger.LogInfo("SettingsManager", "$1");
                }
            }
        }

        /// <summary>
        /// Get all setting keys
        /// </summary>
        public List<string> GetAllSettingKeys()
        {
            return new List<string>(_settings.Keys);
        }

        /// <summary>
        /// Clear all settings
        /// </summary>
        public void ClearAllSettings()
        {
            _settings.Clear();

            if (_enableLogging)
            {
                ChimeraLogger.LogInfo("SettingsManager", "$1");
            }
        }

        /// <summary>
        /// Reset to default settings
        /// </summary>
        public void ResetToDefaults()
        {
            _settings.Clear();
            LoadDefaultSettings();

            if (_enableLogging)
            {
                ChimeraLogger.LogInfo("SettingsManager", "$1");
            }
        }

        /// <summary>
        /// Save settings to file
        /// </summary>
        public void SaveSettings()
        {
            try
            {
                // Convert settings to JSON-serializable format
                var serializableSettings = new Dictionary<string, string>();
                foreach (var kvp in _settings)
                {
                    serializableSettings[kvp.Key] = kvp.Value?.ToString() ?? "";
                }

                string json = JsonUtility.ToJson(new SettingsData { Settings = serializableSettings });
                string filePath = System.IO.Path.Combine(Application.persistentDataPath, "settings.json");
                System.IO.File.WriteAllText(filePath, json);

                if (_enableLogging)
                {
                    ChimeraLogger.LogInfo("SettingsManager", "$1");
                }
            }
            catch (System.Exception ex)
            {
                if (_enableLogging)
                {
                    ChimeraLogger.LogInfo("SettingsManager", "$1");
                }
            }
        }

        /// <summary>
        /// Load settings from file
        /// </summary>
        public void LoadSettings()
        {
            try
            {
                string filePath = System.IO.Path.Combine(Application.persistentDataPath, "settings.json");
                if (System.IO.File.Exists(filePath))
                {
                    string json = System.IO.File.ReadAllText(filePath);
                    var data = JsonUtility.FromJson<SettingsData>(json);

                    if (data?.Settings != null)
                    {
                        foreach (var kvp in data.Settings)
                        {
                            // Try to parse common types
                            if (bool.TryParse(kvp.Value, out bool boolValue))
                                _settings[kvp.Key] = boolValue;
                            else if (int.TryParse(kvp.Value, out int intValue))
                                _settings[kvp.Key] = intValue;
                            else if (float.TryParse(kvp.Value, out float floatValue))
                                _settings[kvp.Key] = floatValue;
                            else
                                _settings[kvp.Key] = kvp.Value;
                        }
                    }

                    if (_enableLogging)
                    {
                        ChimeraLogger.LogInfo("SettingsManager", "$1");
                    }
                }
            }
            catch (System.Exception ex)
            {
                if (_enableLogging)
                {
                    ChimeraLogger.LogInfo("SettingsManager", "$1");
                }
            }

            OnSettingsLoaded?.Invoke();
        }

        /// <summary>
        /// Get settings statistics
        /// </summary>
        public SettingsStats GetStats()
        {
            return new SettingsStats
            {
                TotalSettings = _settings.Count,
                IsAutoSaveEnabled = _enableAutoSave
            };
        }

        #region Private Methods

        private void LoadDefaultSettings()
        {
            // Set some basic default settings
            SetSetting("audio.master_volume", 1.0f);
            SetSetting("audio.music_volume", 0.8f);
            SetSetting("audio.sfx_volume", 1.0f);
            SetSetting("graphics.quality", 1.0f);
            SetSetting("graphics.fullscreen", true);
            SetSetting("gameplay.autosave", true);
            SetSetting("gameplay.show_tooltips", true);
            SetSetting("controls.mouse_sensitivity", 1.0f);
        }

        #endregion
    }

    /// <summary>
    /// Settings data for serialization
    /// </summary>
    [System.Serializable]
    public class SettingsData
    {
        public Dictionary<string, string> Settings = new Dictionary<string, string>();
    }

    /// <summary>
    /// Settings statistics
    /// </summary>
    [System.Serializable]
    public struct SettingsStats
    {
        public int TotalSettings;
        public bool IsAutoSaveEnabled;
    }
}
