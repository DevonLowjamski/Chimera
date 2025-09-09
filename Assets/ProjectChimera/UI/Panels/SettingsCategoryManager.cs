using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using System;

namespace ProjectChimera.UI.Panels
{
    /// <summary>
    /// Manages settings categories, navigation, and section visibility.
    /// Handles category buttons, section switching, and content organization.
    /// </summary>
    public class SettingsCategoryManager
    {
        // Category management
        private List<Button> _categoryButtons;
        private VisualElement _categoryContainer;
        private VisualElement _categoryIndicator;
        private VisualElement _settingsContainer;
        
        // Settings sections
        private Dictionary<SettingsCategory, VisualElement> _settingSections;
        private SettingsCategory _currentCategory = SettingsCategory.Gameplay;
        
        // Configuration
        private SettingsConfiguration _configuration;
        private SettingsState _state;
        
        // Events
        public event Action<SettingsCategory> OnCategoryChanged;
        public event Action<string> OnSoundRequested;
        
        public SettingsCategory CurrentCategory => _currentCategory;
        
        /// <summary>
        /// Initialize the category manager
        /// </summary>
        public void Initialize(VisualElement contentContainer, SettingsConfiguration config, SettingsState state)
        {
            _configuration = config;
            _state = state;
            _categoryButtons = new List<Button>();
            _settingSections = new Dictionary<SettingsCategory, VisualElement>();
            
            CreateCategoryNavigation(contentContainer);
            CreateSettingsContainer(contentContainer);
            CreateAllSettingSections();
            
            ShowCategory(_currentCategory);
        }
        
        /// <summary>
        /// Create category navigation
        /// </summary>
        private void CreateCategoryNavigation(VisualElement contentContainer)
        {
            _categoryContainer = SettingsLayoutManager.CreateCategoryContainer();
            
            var categories = GetCategoryInfos();
            
            foreach (var categoryInfo in categories)
            {
                var button = CreateCategoryButton(categoryInfo);
                _categoryButtons.Add(button);
                _categoryContainer.Add(button);
            }
            
            // Category indicator
            _categoryIndicator = SettingsLayoutManager.CreateCategoryIndicator();
            _categoryContainer.Add(_categoryIndicator);
            
            contentContainer.Add(_categoryContainer);
        }
        
        /// <summary>
        /// Create settings container
        /// </summary>
        private void CreateSettingsContainer(VisualElement contentContainer)
        {
            _settingsContainer = SettingsLayoutManager.CreateSettingsContainer();
            contentContainer.Add(_settingsContainer);
        }
        
        /// <summary>
        /// Get category information
        /// </summary>
        private CategoryInfo[] GetCategoryInfos()
        {
            return new[]
            {
                new CategoryInfo("Gameplay", SettingsCategory.Gameplay, "Game mechanics and behavior"),
                new CategoryInfo("Graphics", SettingsCategory.Graphics, "Visual quality and display"),
                new CategoryInfo("Audio", SettingsCategory.Audio, "Sound and music settings"),
                new CategoryInfo("Controls", SettingsCategory.Controls, "Input and control preferences"),
                new CategoryInfo("Accessibility", SettingsCategory.Accessibility, "Accessibility and usability")
            };
        }
        
        /// <summary>
        /// Create a category button
        /// </summary>
        private Button CreateCategoryButton(CategoryInfo categoryInfo)
        {
            var button = new Button(() => ShowCategory(categoryInfo.category));
            button.name = $"category-{categoryInfo.category.ToString().ToLower()}-button";
            button.text = categoryInfo.name;
            button.tooltip = categoryInfo.description;
            button.style.height = 40;
            button.style.marginBottom = 8;
            button.style.backgroundColor = Color.clear;
            button.style.borderTopWidth = 0;
            button.style.borderRightWidth = 0;
            button.style.borderBottomWidth = 0;
            button.style.borderLeftWidth = 0;
            button.style.unityTextAlign = TextAnchor.MiddleLeft;
            button.style.paddingLeft = 16;
            button.style.borderTopLeftRadius = 6;
            button.style.borderTopRightRadius = 6;
            button.style.borderBottomLeftRadius = 6;
            button.style.borderBottomRightRadius = 6;
            
            button.SetEnabled(categoryInfo.isEnabled);
            
            return button;
        }
        
        /// <summary>
        /// Create all settings sections
        /// </summary>
        private void CreateAllSettingSections()
        {
            _settingSections[SettingsCategory.Gameplay] = CreateGameplaySettings();
            _settingSections[SettingsCategory.Graphics] = CreateGraphicsSettings();
            _settingSections[SettingsCategory.Audio] = CreateAudioSettings();
            _settingSections[SettingsCategory.Controls] = CreateControlsSettings();
            _settingSections[SettingsCategory.Accessibility] = CreateAccessibilitySettings();
            
            // Add all sections to container
            foreach (var section in _settingSections.Values)
            {
                _settingsContainer.Add(section);
            }
        }
        
        /// <summary>
        /// Create gameplay settings section
        /// </summary>
        private VisualElement CreateGameplaySettings()
        {
            var section = SettingsLayoutManager.CreateSettingsSection("Gameplay Settings");
            var defaults = _configuration.gameplayDefaults;
            
            // Auto-save
            var autoSaveToggle = SettingsControlFactory.CreateToggleSetting(
                "Auto-save", "Automatically save game progress", "auto_save", 
                defaults.autoSave, OnSettingChanged);
            section.Add(autoSaveToggle);
            
            // Save interval
            var saveIntervalSlider = SettingsControlFactory.CreateSliderSetting(
                "Save Interval", "Minutes between auto-saves", "save_interval", 
                defaults.saveInterval, 1f, 30f, "minutes", OnSettingChanged);
            section.Add(saveIntervalSlider);
            
            // Game speed
            var gameSpeedSlider = SettingsControlFactory.CreateSliderSetting(
                "Game Speed", "Default time acceleration", "game_speed", 
                defaults.gameSpeed, 0.5f, 5f, "x", OnSettingChanged);
            section.Add(gameSpeedSlider);
            
            // Tutorial hints
            var tutorialToggle = SettingsControlFactory.CreateToggleSetting(
                "Tutorial Hints", "Show helpful gameplay hints", "tutorial_hints", 
                defaults.tutorialHints, OnSettingChanged);
            section.Add(tutorialToggle);
            
            // Notifications
            var notificationsToggle = SettingsControlFactory.CreateToggleSetting(
                "Notifications", "Enable in-game notifications", "notifications", 
                defaults.notifications, OnSettingChanged);
            section.Add(notificationsToggle);
            
            return section;
        }
        
        /// <summary>
        /// Create graphics settings section
        /// </summary>
        private VisualElement CreateGraphicsSettings()
        {
            var section = SettingsLayoutManager.CreateSettingsSection("Graphics Settings");
            var defaults = _configuration.graphicsDefaults;
            
            // Resolution dropdown
            var resolutionDropdown = SettingsControlFactory.CreateDropdownSetting(
                "Resolution", "Screen resolution", "resolution", 
                defaults.resolutionOptions, defaults.defaultResolution, OnSettingChanged);
            section.Add(resolutionDropdown);
            
            // Fullscreen toggle
            var fullscreenToggle = SettingsControlFactory.CreateToggleSetting(
                "Fullscreen", "Enable fullscreen mode", "fullscreen", 
                defaults.fullscreen, OnSettingChanged);
            section.Add(fullscreenToggle);
            
            // Graphics quality
            var qualityDropdown = SettingsControlFactory.CreateDropdownSetting(
                "Graphics Quality", "Overall graphics quality", "quality",
                defaults.qualityOptions, defaults.defaultQuality, OnSettingChanged);
            section.Add(qualityDropdown);
            
            // VSync
            var vsyncToggle = SettingsControlFactory.CreateToggleSetting(
                "VSync", "Synchronize with monitor refresh rate", "vsync", 
                defaults.vsync, OnSettingChanged);
            section.Add(vsyncToggle);
            
            // Frame rate limit
            var frameRateSlider = SettingsControlFactory.CreateSliderSetting(
                "Frame Rate Limit", "Maximum frames per second", "frame_rate_limit", 
                defaults.frameRateLimit, 30f, 144f, "fps", OnSettingChanged);
            section.Add(frameRateSlider);
            
            return section;
        }
        
        /// <summary>
        /// Create audio settings section
        /// </summary>
        private VisualElement CreateAudioSettings()
        {
            var section = SettingsLayoutManager.CreateSettingsSection("Audio Settings");
            var defaults = _configuration.audioDefaults;
            
            // Master volume
            var masterVolumeSlider = SettingsControlFactory.CreateSliderSetting(
                "Master Volume", "Overall audio volume", "master_volume", 
                defaults.masterVolume, 0f, 100f, "%", OnSettingChanged);
            section.Add(masterVolumeSlider);
            
            // Music volume
            var musicVolumeSlider = SettingsControlFactory.CreateSliderSetting(
                "Music Volume", "Background music volume", "music_volume", 
                defaults.musicVolume, 0f, 100f, "%", OnSettingChanged);
            section.Add(musicVolumeSlider);
            
            // SFX volume
            var sfxVolumeSlider = SettingsControlFactory.CreateSliderSetting(
                "Sound Effects", "Sound effects volume", "sfx_volume", 
                defaults.sfxVolume, 0f, 100f, "%", OnSettingChanged);
            section.Add(sfxVolumeSlider);
            
            // UI sounds
            var uiSoundsToggle = SettingsControlFactory.CreateToggleSetting(
                "UI Sounds", "Enable user interface sounds", "ui_sounds", 
                defaults.uiSounds, OnSettingChanged);
            section.Add(uiSoundsToggle);
            
            // Audio quality
            var audioQualityDropdown = SettingsControlFactory.CreateDropdownSetting(
                "Audio Quality", "Audio sample rate and quality", "audio_quality",
                defaults.audioQualityOptions, defaults.defaultAudioQuality, OnSettingChanged);
            section.Add(audioQualityDropdown);
            
            return section;
        }
        
        /// <summary>
        /// Create controls settings section
        /// </summary>
        private VisualElement CreateControlsSettings()
        {
            var section = SettingsLayoutManager.CreateSettingsSection("Controls Settings");
            var defaults = _configuration.controlsDefaults;
            
            // Mouse sensitivity
            var mouseSensitivitySlider = SettingsControlFactory.CreateSliderSetting(
                "Mouse Sensitivity", "Camera movement sensitivity", "mouse_sensitivity", 
                defaults.mouseSensitivity, 10f, 100f, "%", OnSettingChanged);
            section.Add(mouseSensitivitySlider);
            
            // Invert mouse
            var invertMouseToggle = SettingsControlFactory.CreateToggleSetting(
                "Invert Mouse Y", "Invert vertical mouse movement", "invert_mouse", 
                defaults.invertMouse, OnSettingChanged);
            section.Add(invertMouseToggle);
            
            // Scroll speed
            var scrollSpeedSlider = SettingsControlFactory.CreateSliderSetting(
                "Scroll Speed", "Mouse wheel scroll speed", "scroll_speed", 
                defaults.scrollSpeed, 10f, 100f, "%", OnSettingChanged);
            section.Add(scrollSpeedSlider);
            
            // Edge scrolling
            var edgeScrollingToggle = SettingsControlFactory.CreateToggleSetting(
                "Edge Scrolling", "Scroll camera at screen edges", "edge_scrolling", 
                defaults.edgeScrolling, OnSettingChanged);
            section.Add(edgeScrollingToggle);
            
            return section;
        }
        
        /// <summary>
        /// Create accessibility settings section
        /// </summary>
        private VisualElement CreateAccessibilitySettings()
        {
            var section = SettingsLayoutManager.CreateSettingsSection("Accessibility Settings");
            var defaults = _configuration.accessibilityDefaults;
            
            // Color blind support
            var colorBlindDropdown = SettingsControlFactory.CreateDropdownSetting(
                "Color Blind Support", "Color vision assistance", "color_blind_support",
                defaults.colorBlindOptions, defaults.defaultColorBlindSupport, OnSettingChanged);
            section.Add(colorBlindDropdown);
            
            // Text size
            var textSizeSlider = SettingsControlFactory.CreateSliderSetting(
                "Text Size", "User interface text size", "text_size", 
                defaults.textSize, 75f, 150f, "%", OnSettingChanged);
            section.Add(textSizeSlider);
            
            // High contrast
            var highContrastToggle = SettingsControlFactory.CreateToggleSetting(
                "High Contrast", "Enable high contrast mode", "high_contrast", 
                defaults.highContrast, OnSettingChanged);
            section.Add(highContrastToggle);
            
            // Reduced motion
            var reducedMotionToggle = SettingsControlFactory.CreateToggleSetting(
                "Reduced Motion", "Minimize animations and effects", "reduced_motion", 
                defaults.reducedMotion, OnSettingChanged);
            section.Add(reducedMotionToggle);
            
            return section;
        }
        
        /// <summary>
        /// Show settings category
        /// </summary>
        public void ShowCategory(SettingsCategory category)
        {
            if (_currentCategory == category) return;
            
            _currentCategory = category;
            _state.currentCategory = category;
            
            // Hide all sections
            foreach (var section in _settingSections.Values)
            {
                section.style.display = DisplayStyle.None;
            }
            
            // Show selected section
            if (_settingSections.TryGetValue(category, out var selectedSection))
            {
                selectedSection.style.display = DisplayStyle.Flex;
            }
            
            // Update visual states
            UpdateCategoryButtons();
            UpdateCategoryIndicator();
            
            // Play sound
            OnSoundRequested?.Invoke("ui_category_change");
            
            // Notify listeners
            OnCategoryChanged?.Invoke(category);
        }
        
        /// <summary>
        /// Update category button visual states
        /// </summary>
        private void UpdateCategoryButtons()
        {
            for (int i = 0; i < _categoryButtons.Count; i++)
            {
                var button = _categoryButtons[i];
                var isSelected = (SettingsCategory)i == _currentCategory;
                
                if (isSelected)
                {
                    // Selected styling - would integrate with design system
                    // button.style.backgroundColor = designSystem.PrimaryColor;
                }
                else
                {
                    button.style.backgroundColor = Color.clear;
                }
            }
        }
        
        /// <summary>
        /// Update category indicator position
        /// </summary>
        private void UpdateCategoryIndicator()
        {
            SettingsLayoutManager.UpdateCategoryIndicatorPosition(_categoryIndicator, _currentCategory);
        }
        
        /// <summary>
        /// Handle setting value changes
        /// </summary>
        private void OnSettingChanged(string key, object value)
        {
            _state.SetValue(key, value, _currentCategory);
            OnSoundRequested?.Invoke("ui_setting_change");
        }
        
        /// <summary>
        /// Get section for category
        /// </summary>
        public VisualElement GetSection(SettingsCategory category)
        {
            return _settingSections.TryGetValue(category, out var section) ? section : null;
        }
        
        /// <summary>
        /// Update all setting controls to reflect current values
        /// </summary>
        public void RefreshAllControls()
        {
            foreach (var section in _settingSections.Values)
            {
                RefreshControlsInSection(section);
            }
        }
        
        /// <summary>
        /// Refresh controls in a specific section
        /// </summary>
        private void RefreshControlsInSection(VisualElement section)
        {
            // Find all toggles
            var toggles = section.Query<Toggle>().ToList();
            foreach (var toggle in toggles)
            {
                if (toggle.name.EndsWith("-toggle"))
                {
                    var key = toggle.name.Replace("-toggle", "");
                    toggle.value = _state.GetValue(key, toggle.value);
                }
            }
            
            // Find all sliders
            var sliders = section.Query<Slider>().ToList();
            foreach (var slider in sliders)
            {
                if (slider.name.EndsWith("-slider"))
                {
                    var key = slider.name.Replace("-slider", "");
                    slider.value = _state.GetValue(key, slider.value);
                }
            }
            
            // Find all dropdowns
            var dropdowns = section.Query<DropdownField>().ToList();
            foreach (var dropdown in dropdowns)
            {
                if (dropdown.name.EndsWith("-dropdown"))
                {
                    var key = dropdown.name.Replace("-dropdown", "");
                    var index = _state.GetValue(key, dropdown.index);
                    if (index >= 0 && index < dropdown.choices.Count)
                    {
                        dropdown.index = index;
                    }
                }
            }
        }
        
        /// <summary>
        /// Enable or disable a category
        /// </summary>
        public void SetCategoryEnabled(SettingsCategory category, bool enabled)
        {
            var buttonIndex = (int)category;
            if (buttonIndex >= 0 && buttonIndex < _categoryButtons.Count)
            {
                _categoryButtons[buttonIndex].SetEnabled(enabled);
            }
        }
        
        /// <summary>
        /// Get all categories
        /// </summary>
        public SettingsCategory[] GetAllCategories()
        {
            return (SettingsCategory[])Enum.GetValues(typeof(SettingsCategory));
        }
    }
}