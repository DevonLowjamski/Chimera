using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectChimera.Core.Events;
using ProjectChimera.UI.Events;

namespace ProjectChimera.UI.Panels
{
    /// <summary>
    /// Data structures and enums for the Settings system.
    /// Contains all configuration data classes and UI event handling structures.
    /// </summary>
    
    /// <summary>
    /// Settings category enumeration
    /// </summary>
    public enum SettingsCategory
    {
        Gameplay = 0,
        Graphics = 1,
        Audio = 2,
        Controls = 3,
        Accessibility = 4
    }
    
    /// <summary>
    /// Settings configuration data
    /// </summary>
    [System.Serializable]
    public class SettingsConfiguration
    {
        [Header("Settings Events")]
        public SimpleGameEventSO onSettingsChanged;
        public SimpleGameEventSO onSettingsSaved;
        public UIButtonClickEventSO onSettingsButtonClicked;
        
        [Header("Audio")]
        public AudioClip settingsChangeSound;
        
        [Header("Default Values")]
        public GameplaySettingsDefaults gameplayDefaults = new GameplaySettingsDefaults();
        public GraphicsSettingsDefaults graphicsDefaults = new GraphicsSettingsDefaults();
        public AudioSettingsDefaults audioDefaults = new AudioSettingsDefaults();
        public ControlsSettingsDefaults controlsDefaults = new ControlsSettingsDefaults();
        public AccessibilitySettingsDefaults accessibilityDefaults = new AccessibilitySettingsDefaults();
    }
    
    /// <summary>
    /// Gameplay settings default values
    /// </summary>
    [System.Serializable]
    public class GameplaySettingsDefaults
    {
        public bool autoSave = true;
        public float saveInterval = 5f;
        public float gameSpeed = 1f;
        public bool tutorialHints = true;
        public bool notifications = true;
    }
    
    /// <summary>
    /// Graphics settings default values
    /// </summary>
    [System.Serializable]
    public class GraphicsSettingsDefaults
    {
        public string[] resolutionOptions = { "1920x1080", "1680x1050", "1440x900", "1280x720" };
        public int defaultResolution = 0;
        public bool fullscreen = true;
        public string[] qualityOptions = { "Low", "Medium", "High", "Ultra" };
        public int defaultQuality = 2;
        public bool vsync = true;
        public float frameRateLimit = 60f;
    }
    
    /// <summary>
    /// Audio settings default values
    /// </summary>
    [System.Serializable]
    public class AudioSettingsDefaults
    {
        public float masterVolume = 80f;
        public float musicVolume = 70f;
        public float sfxVolume = 85f;
        public bool uiSounds = true;
        public string[] audioQualityOptions = { "Low (22kHz)", "Medium (44kHz)", "High (48kHz)" };
        public int defaultAudioQuality = 1;
    }
    
    /// <summary>
    /// Controls settings default values
    /// </summary>
    [System.Serializable]
    public class ControlsSettingsDefaults
    {
        public float mouseSensitivity = 50f;
        public bool invertMouse = false;
        public float scrollSpeed = 50f;
        public bool edgeScrolling = true;
    }
    
    /// <summary>
    /// Accessibility settings default values
    /// </summary>
    [System.Serializable]
    public class AccessibilitySettingsDefaults
    {
        public string[] colorBlindOptions = { "None", "Protanopia", "Deuteranopia", "Tritanopia" };
        public int defaultColorBlindSupport = 0;
        public float textSize = 100f;
        public bool highContrast = false;
        public bool reducedMotion = false;
    }
    
    /// <summary>
    /// Settings change event data
    /// </summary>
    public class SettingChangeEventData
    {
        public string key;
        public object oldValue;
        public object newValue;
        public SettingsCategory category;
        public DateTime timestamp = DateTime.Now;
    }
    
    /// <summary>
    /// Settings validation result
    /// </summary>
    public class SettingValidationResult
    {
        public bool isValid = true;
        public string errorMessage = "";
        public object correctedValue;
        
        public static SettingValidationResult Valid() => new SettingValidationResult { isValid = true };
        public static SettingValidationResult Invalid(string error) => new SettingValidationResult { isValid = false, errorMessage = error };
        public static SettingValidationResult Corrected(object correctedValue, string message = "")
        {
            return new SettingValidationResult 
            { 
                isValid = true, 
                correctedValue = correctedValue, 
                errorMessage = message 
            };
        }
    }
    
    /// <summary>
    /// Settings state tracking
    /// </summary>
    public class SettingsState
    {
        public Dictionary<string, object> currentSettings = new Dictionary<string, object>();
        public Dictionary<string, object> originalSettings = new Dictionary<string, object>();
        public SettingsCategory currentCategory = SettingsCategory.Gameplay;
        public bool hasUnsavedChanges = false;
        public List<SettingChangeEventData> changeHistory = new List<SettingChangeEventData>();
        
        /// <summary>
        /// Check if settings have been modified
        /// </summary>
        public bool HasChanges()
        {
            return hasUnsavedChanges;
        }
        
        /// <summary>
        /// Get setting value with type safety
        /// </summary>
        public T GetValue<T>(string key, T defaultValue)
        {
            if (currentSettings.TryGetValue(key, out var value) && value is T)
            {
                return (T)value;
            }
            return defaultValue;
        }
        
        /// <summary>
        /// Set setting value with change tracking
        /// </summary>
        public void SetValue(string key, object value, SettingsCategory category)
        {
            var oldValue = currentSettings.TryGetValue(key, out var existing) ? existing : null;
            currentSettings[key] = value;
            
            var isChanged = !Equals(oldValue, value);
            if (isChanged)
            {
                hasUnsavedChanges = true;
                changeHistory.Add(new SettingChangeEventData
                {
                    key = key,
                    oldValue = oldValue,
                    newValue = value,
                    category = category
                });
            }
        }
        
        /// <summary>
        /// Save current state as original
        /// </summary>
        public void CommitChanges()
        {
            originalSettings.Clear();
            foreach (var kvp in currentSettings)
            {
                originalSettings[kvp.Key] = kvp.Value;
            }
            hasUnsavedChanges = false;
            changeHistory.Clear();
        }
        
        /// <summary>
        /// Revert to original settings
        /// </summary>
        public void RevertChanges()
        {
            currentSettings.Clear();
            foreach (var kvp in originalSettings)
            {
                currentSettings[kvp.Key] = kvp.Value;
            }
            hasUnsavedChanges = false;
            changeHistory.Clear();
        }
        
        /// <summary>
        /// Reset all settings to defaults
        /// </summary>
        public void ResetToDefaults(SettingsConfiguration config)
        {
            currentSettings.Clear();
            
            // Load defaults from configuration
            LoadGameplayDefaults(config.gameplayDefaults);
            LoadGraphicsDefaults(config.graphicsDefaults);
            LoadAudioDefaults(config.audioDefaults);
            LoadControlsDefaults(config.controlsDefaults);
            LoadAccessibilityDefaults(config.accessibilityDefaults);
            
            hasUnsavedChanges = true;
        }
        
        private void LoadGameplayDefaults(GameplaySettingsDefaults defaults)
        {
            currentSettings["auto_save"] = defaults.autoSave;
            currentSettings["save_interval"] = defaults.saveInterval;
            currentSettings["game_speed"] = defaults.gameSpeed;
            currentSettings["tutorial_hints"] = defaults.tutorialHints;
            currentSettings["notifications"] = defaults.notifications;
        }
        
        private void LoadGraphicsDefaults(GraphicsSettingsDefaults defaults)
        {
            currentSettings["resolution"] = defaults.defaultResolution;
            currentSettings["fullscreen"] = defaults.fullscreen;
            currentSettings["quality"] = defaults.defaultQuality;
            currentSettings["vsync"] = defaults.vsync;
            currentSettings["frame_rate_limit"] = defaults.frameRateLimit;
        }
        
        private void LoadAudioDefaults(AudioSettingsDefaults defaults)
        {
            currentSettings["master_volume"] = defaults.masterVolume;
            currentSettings["music_volume"] = defaults.musicVolume;
            currentSettings["sfx_volume"] = defaults.sfxVolume;
            currentSettings["ui_sounds"] = defaults.uiSounds;
            currentSettings["audio_quality"] = defaults.defaultAudioQuality;
        }
        
        private void LoadControlsDefaults(ControlsSettingsDefaults defaults)
        {
            currentSettings["mouse_sensitivity"] = defaults.mouseSensitivity;
            currentSettings["invert_mouse"] = defaults.invertMouse;
            currentSettings["scroll_speed"] = defaults.scrollSpeed;
            currentSettings["edge_scrolling"] = defaults.edgeScrolling;
        }
        
        private void LoadAccessibilityDefaults(AccessibilitySettingsDefaults defaults)
        {
            currentSettings["color_blind_support"] = defaults.defaultColorBlindSupport;
            currentSettings["text_size"] = defaults.textSize;
            currentSettings["high_contrast"] = defaults.highContrast;
            currentSettings["reduced_motion"] = defaults.reducedMotion;
        }
    }
    
    /// <summary>
    /// Settings category information
    /// </summary>
    public class CategoryInfo
    {
        public string name;
        public SettingsCategory category;
        public string description;
        public bool isEnabled = true;
        
        public CategoryInfo(string name, SettingsCategory category, string description = "")
        {
            this.name = name;
            this.category = category;
            this.description = description;
        }
    }
}