using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Events;
using ProjectChimera.Shared;
using ProjectChimera.Core.DependencyInjection;


namespace ProjectChimera.Core
{
    public class SettingsManager : DIChimeraManager, IGameStateListener
    {
        [Header("Settings Configuration")]
        [SerializeField] private SettingsManagerConfigSO _config;
        [SerializeField] private bool _autoApplySettings = true;
        [SerializeField] private bool _validateSettings = true;

        [Header("Settings Events")]
        [SerializeField] private StringGameEventSO _onSettingChanged;
        [SerializeField] private SimpleGameEventSO _onSettingsLoaded;
        [SerializeField] private SimpleGameEventSO _onSettingsReset;
        [SerializeField] private StringGameEventSO _onSettingsError;

        private readonly Dictionary<SettingsCategory, Dictionary<string, object>> _settings = new Dictionary<SettingsCategory, Dictionary<string, object>>();
        private readonly Dictionary<string, SettingDefinition> _settingDefinitions = new Dictionary<string, SettingDefinition>();
        private readonly List<ISettingsListener> _settingsListeners = new List<ISettingsListener>();

        private int _settingsChangedThisSession = 0;
        private float _lastSettingsAppliedTime = 0.0f;

        public int SettingsChangedThisSession => _settingsChangedThisSession;
        public float LastSettingsAppliedTime => _lastSettingsAppliedTime;
        public bool AutoApplySettings => _autoApplySettings;

        protected override void OnManagerInitialize()
        {
            LogDebug("Initializing Settings Manager");

            if (_config != null)
            {
                _autoApplySettings = _config.AutoApplySettings;
                _validateSettings = _config.ValidateSettings;
            }

            InitializeSettingsCategories();
            DefineDefaultSettings();
            LoadSettings();

            if (_autoApplySettings)
            {
                ApplyAllSettings();
            }

            _onSettingsLoaded?.Raise();
            LogDebug($"Settings Manager initialized - {_settingDefinitions.Count} settings defined");
        }

        protected override void OnManagerShutdown()
        {
            LogDebug("Shutting down Settings Manager");
            SaveSettings();
            _settings.Clear();
            _settingDefinitions.Clear();
            _settingsListeners.Clear();
            _settingsChangedThisSession = 0;
        }

        private void InitializeSettingsCategories()
        {
            foreach (SettingsCategory category in Enum.GetValues(typeof(SettingsCategory)))
            {
                _settings[category] = new Dictionary<string, object>();
            }
        }

        private void DefineDefaultSettings()
        {
            DefineSettingFloat("graphics.quality", 1.0f, 0.0f, 1.0f, SettingsCategory.Graphics, "Overall graphics quality");
            DefineSettingInt("graphics.resolution_width", 1920, 800, 3840, SettingsCategory.Graphics, "Screen resolution width");
            DefineSettingInt("graphics.resolution_height", 1080, 600, 2160, SettingsCategory.Graphics, "Screen resolution height");
            DefineSettingBool("graphics.fullscreen", true, SettingsCategory.Graphics, "Fullscreen mode");
            DefineSettingBool("graphics.vsync", true, SettingsCategory.Graphics, "Vertical synchronization");
            DefineSettingFloat("graphics.render_scale", 1.0f, 0.5f, 2.0f, SettingsCategory.Graphics, "Render scale multiplier");
            DefineSettingInt("graphics.target_framerate", 60, 30, 120, SettingsCategory.Graphics, "Target frame rate");

            DefineSettingFloat("audio.master_volume", 1.0f, 0.0f, 1.0f, SettingsCategory.Audio, "Master volume");
            DefineSettingFloat("audio.music_volume", 0.8f, 0.0f, 1.0f, SettingsCategory.Audio, "Music volume");
            DefineSettingFloat("audio.sfx_volume", 1.0f, 0.0f, 1.0f, SettingsCategory.Audio, "Sound effects volume");
            DefineSettingFloat("audio.ui_volume", 1.0f, 0.0f, 1.0f, SettingsCategory.Audio, "UI sound volume");
            DefineSettingBool("audio.mute_on_focus_loss", false, SettingsCategory.Audio, "Mute audio when game loses focus");

            DefineSettingFloat("gameplay.time_scale_default", 1.0f, 0.1f, 10.0f, SettingsCategory.Gameplay, "Default time scale");
            DefineSettingFloat("gameplay.time_scale_max", 100.0f, 1.0f, 1000.0f, SettingsCategory.Gameplay, "Maximum time scale");
            DefineSettingBool("gameplay.auto_save", true, SettingsCategory.Gameplay, "Enable auto-save");
            DefineSettingFloat("gameplay.auto_save_interval", 300.0f, 60.0f, 1800.0f, SettingsCategory.Gameplay, "Auto-save interval in seconds");
            DefineSettingBool("gameplay.pause_on_focus_loss", true, SettingsCategory.Gameplay, "Pause game when it loses focus");
            DefineSettingBool("gameplay.show_tooltips", true, SettingsCategory.Gameplay, "Show gameplay tooltips");

            DefineSettingFloat("controls.mouse_sensitivity", 1.0f, 0.1f, 5.0f, SettingsCategory.Controls, "Mouse sensitivity");
            DefineSettingBool("controls.invert_mouse_y", false, SettingsCategory.Controls, "Invert mouse Y axis");
            DefineSettingFloat("controls.scroll_speed", 1.0f, 0.1f, 3.0f, SettingsCategory.Controls, "Scroll wheel speed");
            DefineSettingBool("controls.edge_scrolling", true, SettingsCategory.Controls, "Enable edge scrolling");

            DefineSettingFloat("accessibility.ui_scale", 1.0f, 0.8f, 2.0f, SettingsCategory.Accessibility, "UI scale factor");
            DefineSettingBool("accessibility.high_contrast", false, SettingsCategory.Accessibility, "High contrast mode");
            DefineSettingBool("accessibility.colorblind_support", false, SettingsCategory.Accessibility, "Colorblind accessibility");
            DefineSettingFloat("accessibility.font_size_multiplier", 1.0f, 0.8f, 2.0f, SettingsCategory.Accessibility, "Font size multiplier");
            DefineSettingBool("accessibility.reduce_motion", false, SettingsCategory.Accessibility, "Reduce motion effects");

            DefineSettingBool("debug.show_fps", false, SettingsCategory.Debug, "Show FPS counter");
            DefineSettingBool("debug.show_memory", false, SettingsCategory.Debug, "Show memory usage");
            DefineSettingBool("debug.enable_logging", true, SettingsCategory.Debug, "Enable debug logging");
            DefineSettingInt("debug.log_level", 3, 0, 5, SettingsCategory.Debug, "Debug log level");
        }

        private void DefineSettingBool(string key, bool defaultValue, SettingsCategory category, string description)
        {
            var definition = new SettingDefinition
            {
                Key = key,
                Type = typeof(bool),
                DefaultValue = defaultValue,
                Category = category,
                Description = description
            };

            _settingDefinitions[key] = definition;
            _settings[category][key] = defaultValue;
        }

        private void DefineSettingInt(string key, int defaultValue, int minValue, int maxValue, SettingsCategory category, string description)
        {
            var definition = new SettingDefinition
            {
                Key = key,
                Type = typeof(int),
                DefaultValue = defaultValue,
                MinValue = minValue,
                MaxValue = maxValue,
                Category = category,
                Description = description
            };

            _settingDefinitions[key] = definition;
            _settings[category][key] = defaultValue;
        }

        private void DefineSettingFloat(string key, float defaultValue, float minValue, float maxValue, SettingsCategory category, string description)
        {
            var definition = new SettingDefinition
            {
                Key = key,
                Type = typeof(float),
                DefaultValue = defaultValue,
                MinValue = minValue,
                MaxValue = maxValue,
                Category = category,
                Description = description
            };

            _settingDefinitions[key] = definition;
            _settings[category][key] = defaultValue;
        }

        public bool GetBool(string key, bool defaultValue = false)
        {
            if (TryGetSetting(key, out object value) && value is bool boolValue)
            {
                return boolValue;
            }
            return defaultValue;
        }

        public int GetInt(string key, int defaultValue = 0)
        {
            if (TryGetSetting(key, out object value) && value is int intValue)
            {
                return intValue;
            }
            return defaultValue;
        }

        public float GetFloat(string key, float defaultValue = 0.0f)
        {
            if (TryGetSetting(key, out object value) && value is float floatValue)
            {
                return floatValue;
            }
            return defaultValue;
        }

        public string GetString(string key, string defaultValue = "")
        {
            if (TryGetSetting(key, out object value) && value is string stringValue)
            {
                return stringValue;
            }
            return defaultValue;
        }

        public void SetSetting(string key, object value, bool applyImmediately = true)
        {
            if (!_settingDefinitions.TryGetValue(key, out SettingDefinition definition))
            {
                LogWarning($"Unknown setting key: {key}");
                return;
            }
            
            object validatedValue = ValidateAndClampValue(value, definition);
            
            _settings[definition.Category][key] = validatedValue;
            _settingsChangedThisSession++;

            LogDebug($"Setting changed: {key} = {validatedValue}");
            
            NotifySettingsListeners(key, validatedValue);
            _onSettingChanged?.Raise($"{key}:{validatedValue}");
            
            if (applyImmediately && _autoApplySettings)
            {
                ApplySetting(key, validatedValue);
            }
        }

        private bool TryGetSetting(string key, out object value)
        {
            value = null;

            if (!_settingDefinitions.TryGetValue(key, out SettingDefinition definition))
            {
                return false;
            }

            return _settings[definition.Category].TryGetValue(key, out value);
        }

        private object ValidateAndClampValue(object value, SettingDefinition definition)
        {
            if (value == null) return definition.DefaultValue;

            try
            {
                object convertedValue = Convert.ChangeType(value, definition.Type);

                if (definition.Type == typeof(int))
                {
                    int intValue = (int)convertedValue;
                    if (definition.MinValue.HasValue) intValue = Mathf.Max(intValue, (int)definition.MinValue.Value);
                    if (definition.MaxValue.HasValue) intValue = Mathf.Min(intValue, (int)definition.MaxValue.Value);
                    return intValue;
                }
                else if (definition.Type == typeof(float))
                {
                    float floatValue = (float)convertedValue;
                    if (definition.MinValue.HasValue) floatValue = Mathf.Max(floatValue, definition.MinValue.Value);
                    if (definition.MaxValue.HasValue) floatValue = Mathf.Min(floatValue, definition.MaxValue.Value);
                    return floatValue;
                }

                return convertedValue;
            }
            catch (Exception e)
            {
                LogError($"Error validating setting value for {definition.Key}: {e.Message}");
                return definition.DefaultValue;
            }
        }

        private void ApplySetting(string key, object value)
        {
            try
            {
                switch (key)
                {
                    case "graphics.quality":
                        QualitySettings.SetQualityLevel(Mathf.RoundToInt(GetFloat(key) * (QualitySettings.names.Length - 1)));
                        break;
                    case "graphics.resolution_width":
                    case "graphics.resolution_height":
                    case "graphics.fullscreen":
                        ApplyResolutionSettings();
                        break;
                    case "graphics.vsync":
                        QualitySettings.vSyncCount = GetBool(key) ? 1 : 0;
                        break;
                    case "graphics.target_framerate":
                        Application.targetFrameRate = GetInt(key);
                        break;
                    
                    case "audio.master_volume":
                        AudioListener.volume = GetFloat(key);
                        break;
                    
                    case "gameplay.time_scale_default":
                        var timeManager = ServiceContainerFactory.Instance?.TryResolve<TimeManager>();
                        if (timeManager != null)
                        {
                            timeManager.ResetSpeedLevel();
                        }
                        break;
                }

                _lastSettingsAppliedTime = Time.time;
            }
            catch (Exception e)
            {
                LogError($"Error applying setting {key}: {e.Message}");
                _onSettingsError?.Raise($"Failed to apply {key}: {e.Message}");
            }
        }

        private void ApplyResolutionSettings()
        {
            int width = GetInt("graphics.resolution_width");
            int height = GetInt("graphics.resolution_height");
            bool fullscreen = GetBool("graphics.fullscreen");

            Screen.SetResolution(width, height, fullscreen);
        }
        
        public void ApplyAllSettings()
        {
            LogDebug("Applying all settings");

            foreach (var definition in _settingDefinitions.Values)
            {
                if (TryGetSetting(definition.Key, out object value))
                {
                    ApplySetting(definition.Key, value);
                }
            }

            _lastSettingsAppliedTime = Time.time;
            LogDebug("All settings applied");
        }

        private void LoadSettings()
        {
            LogDebug("Loading settings from PlayerPrefs");

            foreach (var definition in _settingDefinitions.Values)
            {
                string key = definition.Key;
                
                if (PlayerPrefs.HasKey(key))
                {
                    object value = null;

                    if (definition.Type == typeof(bool))
                    {
                        value = PlayerPrefs.GetInt(key, 0) == 1;
                    }
                    else if (definition.Type == typeof(int))
                    {
                        value = PlayerPrefs.GetInt(key, (int)definition.DefaultValue);
                    }
                    else if (definition.Type == typeof(float))
                    {
                        value = PlayerPrefs.GetFloat(key, (float)definition.DefaultValue);
                    }
                    else if (definition.Type == typeof(string))
                    {
                        value = PlayerPrefs.GetString(key, (string)definition.DefaultValue);
                    }

                    if (value != null)
                    {
                        _settings[definition.Category][key] = ValidateAndClampValue(value, definition);
                    }
                }
            }

            LogDebug("Settings loaded from PlayerPrefs");
        }

        private void SaveSettings()
        {
            LogDebug("Saving settings to PlayerPrefs");

            foreach (var definition in _settingDefinitions.Values)
            {
                string key = definition.Key;
                
                if (TryGetSetting(key, out object value))
                {
                    if (definition.Type == typeof(bool))
                    {
                        PlayerPrefs.SetInt(key, (bool)value ? 1 : 0);
                    }
                    else if (definition.Type == typeof(int))
                    {
                        PlayerPrefs.SetInt(key, (int)value);
                    }
                    else if (definition.Type == typeof(float))
                    {
                        PlayerPrefs.SetFloat(key, (float)value);
                    }
                    else if (definition.Type == typeof(string))
                    {
                        PlayerPrefs.SetString(key, (string)value);
                    }
                }
            }

            PlayerPrefs.Save();
            LogDebug("Settings saved to PlayerPrefs");
        }

        public void ResetAllSettings()
        {
            LogDebug("Resetting all settings to defaults");

            foreach (var definition in _settingDefinitions.Values)
            {
                _settings[definition.Category][definition.Key] = definition.DefaultValue;
            }

            _settingsChangedThisSession = 0;

            if (_autoApplySettings)
            {
                ApplyAllSettings();
            }

            _onSettingsReset?.Raise();
            LogDebug("All settings reset to defaults");
        }

        public void ResetCategorySettings(SettingsCategory category)
        {
            LogDebug($"Resetting {category} settings to defaults");

            var categoryDefinitions = _settingDefinitions.Values.Where(d => d.Category == category);
            foreach (var definition in categoryDefinitions)
            {
                _settings[category][definition.Key] = definition.DefaultValue;
            }

            if (_autoApplySettings)
            {
                ApplyAllSettings();
            }
        }

        public Dictionary<string, object> GetCategorySettings(SettingsCategory category)
        {
            return new Dictionary<string, object>(_settings[category]);
        }
        
        public List<SettingDefinition> GetCategoryDefinitions(SettingsCategory category)
        {
            return _settingDefinitions.Values.Where(d => d.Category == category).ToList();
        }

        public void RegisterSettingsListener(ISettingsListener listener)
        {
            if (listener != null && !_settingsListeners.Contains(listener))
            {
                _settingsListeners.Add(listener);
            }
        }

        public void UnregisterSettingsListener(ISettingsListener listener)
        {
            _settingsListeners.Remove(listener);
        }

        private void NotifySettingsListeners(string key, object value)
        {
            for (int i = _settingsListeners.Count - 1; i >= 0; i--)
            {
                try
                {
                    _settingsListeners[i]?.OnSettingChanged(key, value);
                }
                catch (Exception e)
                {
                    LogError($"Error notifying settings listener: {e.Message}");
                    _settingsListeners.RemoveAt(i);
                }
            }
        }

        /// <summary>
        /// Handles game state changes.
        /// </summary>
        public void OnGameStateChanged(GameState gameState)
        {
            switch (gameState)
            {
                case GameState.Loading:
                    LogDebug("Settings Manager: Game entering loading state");
                    break;
                case GameState.Error:
                    LogWarning("Settings Manager: Game entered error state");
                    break;
                case GameState.Shutdown:
                    LogDebug("Settings Manager: Game shutting down, saving settings");
                    SaveSettings();
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
                case GameState.Shutdown:
                    SaveSettings();
                    break;
            }
        }
    }

    public enum SettingsCategory
    {
        Graphics,
        Audio,
        Gameplay,
        Controls,
        Accessibility,
        Debug
    }

    [Serializable]
    public struct SettingDefinition
    {
        public string Key;
        public Type Type;
        public object DefaultValue;
        public float? MinValue;
        public float? MaxValue;
        public SettingsCategory Category;
        public string Description;
    }

    public interface ISettingsListener
    {
        void OnSettingChanged(string key, object value);
    }
    
    [CreateAssetMenu(fileName = "Settings Manager Config", menuName = "Project Chimera/Core/Settings Manager Config")]
    public class SettingsManagerConfigSO : ChimeraConfigSO
    {
        [Header("General Settings")]
        public bool AutoApplySettings = true;
        public bool ValidateSettings = true;
        public bool SaveOnChange = false;

        [Header("Performance Settings")]
        public bool EnableSettingsCache = true;
        public bool EnableSettingsEvents = true;
        
        [Range(0.1f, 5.0f)]
        public float SettingsApplyDelay = 0.1f;

        [Header("Debug Settings")]
        public bool LogSettingChanges = false;
        public bool EnableSettingsValidation = true;
    }
}
