using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core;
using ProjectChimera.UI.Core;
using ProjectChimera.UI.Events;
using ProjectChimera.UI.Components;
using ProjectChimera.Core.Events;

namespace ProjectChimera.UI.Panels
{
    /// <summary>
    /// Settings panel for Project Chimera.
    /// Provides comprehensive game configuration options and preferences.
    /// </summary>
    public class SettingsPanel : UIPanel
    {
        [Header("Settings Events")]
        [SerializeField] private SimpleGameEventSO _onSettingsChanged;
        [SerializeField] private SimpleGameEventSO _onSettingsSaved;
        [SerializeField] private UIButtonClickEventSO _onSettingsButtonClicked;
        
        [Header("Audio")]
        [SerializeField] private AudioClip _settingsChangeSound;
        
        // Main containers
        private VisualElement _headerContainer;
        private VisualElement _contentContainer;
        private VisualElement _footerContainer;
        private VisualElement _categoryContainer;
        private VisualElement _settingsContainer;
        
        // Header elements
        private Label _titleLabel;
        private Button _closeButton;
        
        // Category navigation
        private List<Button> _categoryButtons;
        private VisualElement _categoryIndicator;
        
        // Footer elements
        private Button _resetButton;
        private Button _applyButton;
        private Button _saveButton;
        private Button _cancelButton;
        
        // Settings sections
        private VisualElement _gameplaySettings;
        private VisualElement _graphicsSettings;
        private VisualElement _audioSettings;
        private VisualElement _controlsSettings;
        private VisualElement _accessibilitySettings;
        
        // Current settings state
        private SettingsCategory _currentCategory = SettingsCategory.Gameplay;
        private Dictionary<string, object> _currentSettings;
        private Dictionary<string, object> _originalSettings;
        private bool _hasUnsavedChanges = false;
        
        // Managers
        // private SettingsManager _settingsManager;
        
        protected override void SetupUIElements()
        {
            base.SetupUIElements();
            
            // Get settings manager
            // _settingsManager = GameManager.Instance?.GetManager<SettingsManager>();
            
            // Initialize settings state
            _currentSettings = new Dictionary<string, object>();
            _originalSettings = new Dictionary<string, object>();
            _categoryButtons = new List<Button>();
            
            LoadCurrentSettings();
            
            CreateSettingsLayout();
            CreateHeader();
            CreateCategoryNavigation();
            CreateSettingsSections();
            CreateFooter();
            
            ShowCategory(_currentCategory);
        }
        
        protected override void BindUIEvents()
        {
            base.BindUIEvents();
            
            // Header buttons
            _closeButton?.RegisterCallback<ClickEvent>(OnCloseClicked);
            
            // Footer buttons
            _resetButton?.RegisterCallback<ClickEvent>(OnResetClicked);
            _applyButton?.RegisterCallback<ClickEvent>(OnApplyClicked);
            _saveButton?.RegisterCallback<ClickEvent>(OnSaveClicked);
            _cancelButton?.RegisterCallback<ClickEvent>(OnCancelClicked);
            
            // Category buttons
            BindCategoryButtons();
            
            // Settings controls
            BindSettingsControls();
        }
        
        /// <summary>
        /// Create the main settings layout
        /// </summary>
        private void CreateSettingsLayout()
        {
            _rootElement.Clear();
            
            // Main container
            var mainContainer = new VisualElement();
            mainContainer.name = "settings-main-container";
            mainContainer.style.flexGrow = 1;
            mainContainer.style.backgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.95f);
            mainContainer.style.flexDirection = FlexDirection.Column;
            
            // Content area
            var contentArea = new VisualElement();
            contentArea.name = "settings-content-area";
            contentArea.style.flexGrow = 1;
            contentArea.style.flexDirection = FlexDirection.Row;
            contentArea.style.maxWidth = 1000;
            contentArea.style.alignSelf = Align.Center;
            contentArea.style.marginTop = 50;
            contentArea.style.marginBottom = 50;
            contentArea.style.marginLeft = 50;
            contentArea.style.marginRight = 50;
            
            // Header
            _headerContainer = new VisualElement();
            _headerContainer.name = "settings-header";
            _headerContainer.style.height = 60;
            // _headerContainer.style.backgroundColor = _uiManager.DesignSystem.ColorPalette.SurfaceDark;
            _headerContainer.style.flexDirection = FlexDirection.Row;
            _headerContainer.style.alignItems = Align.Center;
            _headerContainer.style.justifyContent = Justify.SpaceBetween;
            _headerContainer.style.paddingLeft = 24;
            _headerContainer.style.paddingRight = 24;
            _headerContainer.style.borderTopLeftRadius = 12;
            _headerContainer.style.borderTopRightRadius = 12;
            
            // Content container
            _contentContainer = new VisualElement();
            _contentContainer.name = "settings-content";
            _contentContainer.style.flexGrow = 1;
            // _contentContainer.style.backgroundColor = _uiManager.DesignSystem.ColorPalette.BackgroundMedium;
            _contentContainer.style.flexDirection = FlexDirection.Row;
            
            // Footer
            _footerContainer = new VisualElement();
            _footerContainer.name = "settings-footer";
            _footerContainer.style.height = 60;
            // _footerContainer.style.backgroundColor = _uiManager.DesignSystem.ColorPalette.SurfaceDark;
            _footerContainer.style.flexDirection = FlexDirection.Row;
            _footerContainer.style.alignItems = Align.Center;
            _footerContainer.style.justifyContent = Justify.SpaceBetween;
            _footerContainer.style.paddingLeft = 24;
            _footerContainer.style.paddingRight = 24;
            _footerContainer.style.borderBottomLeftRadius = 12;
            _footerContainer.style.borderBottomRightRadius = 12;
            
            contentArea.Add(_headerContainer);
            contentArea.Add(_contentContainer);
            contentArea.Add(_footerContainer);
            
            mainContainer.Add(contentArea);
            _rootElement.Add(mainContainer);
        }
        
        /// <summary>
        /// Create the header section
        /// </summary>
        private void CreateHeader()
        {
            // Title
            _titleLabel = new Label("Settings");
            _titleLabel.name = "settings-title";
            _titleLabel.style.fontSize = 24;
            // _titleLabel.style.color = _uiManager.DesignSystem.ColorPalette.TextPrimary;
            _titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            
            // Close button
            _closeButton = new Button();
            _closeButton.name = "settings-close-button";
            _closeButton.text = "✕";
            _closeButton.style.width = 32;
            _closeButton.style.height = 32;
            _closeButton.style.fontSize = 18;
            _closeButton.style.backgroundColor = Color.clear;
            _closeButton.style.borderTopWidth = 0;
            _closeButton.style.borderRightWidth = 0;
            _closeButton.style.borderBottomWidth = 0;
            _closeButton.style.borderLeftWidth = 0;
            // _closeButton.style.color = _uiManager.DesignSystem.ColorPalette.TextSecondary;
            
            _headerContainer.Add(_titleLabel);
            _headerContainer.Add(_closeButton);
        }
        
        /// <summary>
        /// Create category navigation
        /// </summary>
        private void CreateCategoryNavigation()
        {
            _categoryContainer = new VisualElement();
            _categoryContainer.name = "category-container";
            _categoryContainer.style.width = 200;
            // _categoryContainer.style.backgroundColor = _uiManager.DesignSystem.ColorPalette.SurfaceDark;
            _categoryContainer.style.paddingTop = 24;
            _categoryContainer.style.paddingBottom = 24;
            _categoryContainer.style.paddingLeft = 16;
            _categoryContainer.style.paddingRight = 16;
            
            var categories = new[]
            {
                ("Gameplay", SettingsCategory.Gameplay),
                ("Graphics", SettingsCategory.Graphics),
                ("Audio", SettingsCategory.Audio),
                ("Controls", SettingsCategory.Controls),
                ("Accessibility", SettingsCategory.Accessibility)
            };
            
            foreach (var (name, category) in categories)
            {
                var button = CreateCategoryButton(name, category);
                _categoryButtons.Add(button);
                _categoryContainer.Add(button);
            }
            
            // Category indicator
            _categoryIndicator = new VisualElement();
            _categoryIndicator.name = "category-indicator";
            _categoryIndicator.style.position = Position.Absolute;
            _categoryIndicator.style.width = 4;
            _categoryIndicator.style.height = 40;
            // _categoryIndicator.style.backgroundColor = _uiManager.DesignSystem.ColorPalette.PrimaryGreen;
            _categoryIndicator.style.left = 0;
            _categoryIndicator.style.borderTopRightRadius = 2;
            _categoryIndicator.style.borderBottomRightRadius = 2;
            
            _categoryContainer.Add(_categoryIndicator);
            _contentContainer.Add(_categoryContainer);
        }
        
        /// <summary>
        /// Create a category button
        /// </summary>
        private Button CreateCategoryButton(string text, SettingsCategory category)
        {
            var button = new Button(() => ShowCategory(category));
            button.name = $"category-{category.ToString().ToLower()}-button";
            button.text = text;
            button.style.height = 40;
            button.style.marginBottom = 8;
            button.style.backgroundColor = Color.clear;
            button.style.borderTopWidth = 0;
            button.style.borderRightWidth = 0;
            button.style.borderBottomWidth = 0;
            button.style.borderLeftWidth = 0;
            // button.style.color = _uiManager.DesignSystem.ColorPalette.TextSecondary;
            button.style.unityTextAlign = TextAnchor.MiddleLeft;
            button.style.paddingLeft = 16;
            button.style.borderTopLeftRadius = 6;
            button.style.borderTopRightRadius = 6;
            button.style.borderBottomLeftRadius = 6;
            button.style.borderBottomRightRadius = 6;
            
            return button;
        }
        
        /// <summary>
        /// Create settings sections
        /// </summary>
        private void CreateSettingsSections()
        {
            _settingsContainer = new VisualElement();
            _settingsContainer.name = "settings-container";
            _settingsContainer.style.flexGrow = 1;
            _settingsContainer.style.paddingTop = 24;
            _settingsContainer.style.paddingBottom = 24;
            _settingsContainer.style.paddingLeft = 24;
            _settingsContainer.style.paddingRight = 24;
            
            CreateGameplaySettings();
            CreateGraphicsSettings();
            CreateAudioSettings();
            CreateControlsSettings();
            CreateAccessibilitySettings();
            
            _contentContainer.Add(_settingsContainer);
        }
        
        /// <summary>
        /// Create gameplay settings section
        /// </summary>
        private void CreateGameplaySettings()
        {
            _gameplaySettings = CreateSettingsSection("Gameplay Settings");
            
            // Auto-save
            var autoSaveToggle = CreateToggleSetting("Auto-save", "Automatically save game progress", "auto_save", true);
            _gameplaySettings.Add(autoSaveToggle);
            
            // Save interval
            var saveIntervalSlider = CreateSliderSetting("Save Interval", "Minutes between auto-saves", "save_interval", 5f, 1f, 30f, "minutes");
            _gameplaySettings.Add(saveIntervalSlider);
            
            // Game speed
            var gameSpeedSlider = CreateSliderSetting("Game Speed", "Default time acceleration", "game_speed", 1f, 0.5f, 5f, "x");
            _gameplaySettings.Add(gameSpeedSlider);
            
            // Tutorial hints
            var tutorialToggle = CreateToggleSetting("Tutorial Hints", "Show helpful gameplay hints", "tutorial_hints", true);
            _gameplaySettings.Add(tutorialToggle);
            
            // Notifications
            var notificationsToggle = CreateToggleSetting("Notifications", "Enable in-game notifications", "notifications", true);
            _gameplaySettings.Add(notificationsToggle);
            
            _settingsContainer.Add(_gameplaySettings);
        }
        
        /// <summary>
        /// Create graphics settings section
        /// </summary>
        private void CreateGraphicsSettings()
        {
            _graphicsSettings = CreateSettingsSection("Graphics Settings");
            
            // Resolution dropdown
            var resolutionDropdown = CreateDropdownSetting("Resolution", "Screen resolution", "resolution", 
                new[] { "1920x1080", "1680x1050", "1440x900", "1280x720" }, 0);
            _graphicsSettings.Add(resolutionDropdown);
            
            // Fullscreen toggle
            var fullscreenToggle = CreateToggleSetting("Fullscreen", "Enable fullscreen mode", "fullscreen", true);
            _graphicsSettings.Add(fullscreenToggle);
            
            // Graphics quality
            var qualityDropdown = CreateDropdownSetting("Graphics Quality", "Overall graphics quality", "quality",
                new[] { "Low", "Medium", "High", "Ultra" }, 2);
            _graphicsSettings.Add(qualityDropdown);
            
            // VSync
            var vsyncToggle = CreateToggleSetting("VSync", "Synchronize with monitor refresh rate", "vsync", true);
            _graphicsSettings.Add(vsyncToggle);
            
            // Frame rate limit
            var frameRateSlider = CreateSliderSetting("Frame Rate Limit", "Maximum frames per second", "frame_rate_limit", 60f, 30f, 144f, "fps");
            _graphicsSettings.Add(frameRateSlider);
            
            _settingsContainer.Add(_graphicsSettings);
        }
        
        /// <summary>
        /// Create audio settings section
        /// </summary>
        private void CreateAudioSettings()
        {
            _audioSettings = CreateSettingsSection("Audio Settings");
            
            // Master volume
            var masterVolumeSlider = CreateSliderSetting("Master Volume", "Overall audio volume", "master_volume", 80f, 0f, 100f, "%");
            _audioSettings.Add(masterVolumeSlider);
            
            // Music volume
            var musicVolumeSlider = CreateSliderSetting("Music Volume", "Background music volume", "music_volume", 70f, 0f, 100f, "%");
            _audioSettings.Add(musicVolumeSlider);
            
            // SFX volume
            var sfxVolumeSlider = CreateSliderSetting("Sound Effects", "Sound effects volume", "sfx_volume", 85f, 0f, 100f, "%");
            _audioSettings.Add(sfxVolumeSlider);
            
            // UI sounds
            var uiSoundsToggle = CreateToggleSetting("UI Sounds", "Enable user interface sounds", "ui_sounds", true);
            _audioSettings.Add(uiSoundsToggle);
            
            // Audio quality
            var audioQualityDropdown = CreateDropdownSetting("Audio Quality", "Audio sample rate and quality", "audio_quality",
                new[] { "Low (22kHz)", "Medium (44kHz)", "High (48kHz)" }, 1);
            _audioSettings.Add(audioQualityDropdown);
            
            _settingsContainer.Add(_audioSettings);
        }
        
        /// <summary>
        /// Create controls settings section
        /// </summary>
        private void CreateControlsSettings()
        {
            _controlsSettings = CreateSettingsSection("Controls Settings");
            
            // Mouse sensitivity
            var mouseSensitivitySlider = CreateSliderSetting("Mouse Sensitivity", "Camera movement sensitivity", "mouse_sensitivity", 50f, 10f, 100f, "%");
            _controlsSettings.Add(mouseSensitivitySlider);
            
            // Invert mouse
            var invertMouseToggle = CreateToggleSetting("Invert Mouse Y", "Invert vertical mouse movement", "invert_mouse", false);
            _controlsSettings.Add(invertMouseToggle);
            
            // Scroll speed
            var scrollSpeedSlider = CreateSliderSetting("Scroll Speed", "Mouse wheel scroll speed", "scroll_speed", 50f, 10f, 100f, "%");
            _controlsSettings.Add(scrollSpeedSlider);
            
            // Edge scrolling
            var edgeScrollingToggle = CreateToggleSetting("Edge Scrolling", "Scroll camera at screen edges", "edge_scrolling", true);
            _controlsSettings.Add(edgeScrollingToggle);
            
            _settingsContainer.Add(_controlsSettings);
        }
        
        /// <summary>
        /// Create accessibility settings section
        /// </summary>
        private void CreateAccessibilitySettings()
        {
            _accessibilitySettings = CreateSettingsSection("Accessibility Settings");
            
            // Color blind support
            var colorBlindDropdown = CreateDropdownSetting("Color Blind Support", "Color vision assistance", "color_blind_support",
                new[] { "None", "Protanopia", "Deuteranopia", "Tritanopia" }, 0);
            _accessibilitySettings.Add(colorBlindDropdown);
            
            // Text size
            var textSizeSlider = CreateSliderSetting("Text Size", "User interface text size", "text_size", 100f, 75f, 150f, "%");
            _accessibilitySettings.Add(textSizeSlider);
            
            // High contrast
            var highContrastToggle = CreateToggleSetting("High Contrast", "Enable high contrast mode", "high_contrast", false);
            _accessibilitySettings.Add(highContrastToggle);
            
            // Reduced motion
            var reducedMotionToggle = CreateToggleSetting("Reduced Motion", "Minimize animations and effects", "reduced_motion", false);
            _accessibilitySettings.Add(reducedMotionToggle);
            
            _settingsContainer.Add(_accessibilitySettings);
        }
        
        /// <summary>
        /// Create a settings section container
        /// </summary>
        private VisualElement CreateSettingsSection(string title)
        {
            var section = new VisualElement();
            section.name = title.ToLower().Replace(" ", "-") + "-section";
            section.style.display = DisplayStyle.None;
            
            var titleLabel = new Label(title);
            titleLabel.style.fontSize = 18;
            // titleLabel.style.color = _uiManager.DesignSystem.ColorPalette.TextPrimary;
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.marginBottom = 20;
            
            section.Add(titleLabel);
            return section;
        }
        
        /// <summary>
        /// Create a toggle setting
        /// </summary>
        private VisualElement CreateToggleSetting(string label, string description, string key, bool defaultValue)
        {
            var container = new VisualElement();
            container.name = key + "-setting";
            container.style.flexDirection = FlexDirection.Row;
            container.style.justifyContent = Justify.SpaceBetween;
            container.style.alignItems = Align.Center;
            container.style.marginBottom = 16;
            container.style.paddingTop = 12;
            container.style.paddingBottom = 12;
            container.style.paddingLeft = 16;
            container.style.paddingRight = 16;
            // container.style.backgroundColor = _uiManager.DesignSystem.ColorPalette.SurfaceDark;
            container.style.borderTopLeftRadius = 8;
            container.style.borderTopRightRadius = 8;
            container.style.borderBottomLeftRadius = 8;
            container.style.borderBottomRightRadius = 8;
            
            var labelContainer = new VisualElement();
            labelContainer.style.flexGrow = 1;
            
            var labelElement = new Label(label);
            labelElement.style.fontSize = 14;
            // labelElement.style.color = _uiManager.DesignSystem.ColorPalette.TextPrimary;
            labelElement.style.unityFontStyleAndWeight = FontStyle.Bold;
            
            var descElement = new Label(description);
            descElement.style.fontSize = 12;
            // descElement.style.color = _uiManager.DesignSystem.ColorPalette.TextSecondary;
            descElement.style.marginTop = 4;
            
            labelContainer.Add(labelElement);
            labelContainer.Add(descElement);
            
            var toggle = new Toggle();
            toggle.name = key + "-toggle";
            toggle.value = GetSettingValue<bool>(key, defaultValue);
            toggle.RegisterCallback<ChangeEvent<bool>>(evt => OnSettingChanged(key, evt.newValue));
            
            container.Add(labelContainer);
            container.Add(toggle);
            
            return container;
        }
        
        /// <summary>
        /// Create a slider setting
        /// </summary>
        private VisualElement CreateSliderSetting(string label, string description, string key, float defaultValue, float min, float max, string unit)
        {
            var container = new VisualElement();
            container.name = key + "-setting";
            container.style.marginBottom = 16;
            container.style.paddingTop = 12;
            container.style.paddingBottom = 12;
            container.style.paddingLeft = 16;
            container.style.paddingRight = 16;
            // container.style.backgroundColor = _uiManager.DesignSystem.ColorPalette.SurfaceDark;
            container.style.borderTopLeftRadius = 8;
            container.style.borderTopRightRadius = 8;
            container.style.borderBottomLeftRadius = 8;
            container.style.borderBottomRightRadius = 8;
            
            var headerContainer = new VisualElement();
            headerContainer.style.flexDirection = FlexDirection.Row;
            headerContainer.style.justifyContent = Justify.SpaceBetween;
            headerContainer.style.alignItems = Align.Center;
            headerContainer.style.marginBottom = 8;
            
            var labelElement = new Label(label);
            labelElement.style.fontSize = 14;
            // labelElement.style.color = _uiManager.DesignSystem.ColorPalette.TextPrimary;
            labelElement.style.unityFontStyleAndWeight = FontStyle.Bold;
            
            var valueLabel = new Label($"{GetSettingValue(key, defaultValue):F0} {unit}");
            valueLabel.name = key + "-value-label";
            valueLabel.style.fontSize = 14;
            // valueLabel.style.color = _uiManager.DesignSystem.ColorPalette.AccentGold;
            
            headerContainer.Add(labelElement);
            headerContainer.Add(valueLabel);
            
            var descElement = new Label(description);
            descElement.style.fontSize = 12;
            // descElement.style.color = _uiManager.DesignSystem.ColorPalette.TextSecondary;
            descElement.style.marginBottom = 8;
            
            var slider = new Slider(min, max);
            slider.name = key + "-slider";
            slider.value = GetSettingValue(key, defaultValue);
            slider.RegisterCallback<ChangeEvent<float>>(evt => {
                OnSettingChanged(key, evt.newValue);
                valueLabel.text = $"{evt.newValue:F0} {unit}";
            });
            
            container.Add(headerContainer);
            container.Add(descElement);
            container.Add(slider);
            
            return container;
        }
        
        /// <summary>
        /// Create a dropdown setting
        /// </summary>
        private VisualElement CreateDropdownSetting(string label, string description, string key, string[] options, int defaultIndex)
        {
            var container = new VisualElement();
            container.name = key + "-setting";
            container.style.marginBottom = 16;
            container.style.paddingTop = 12;
            container.style.paddingBottom = 12;
            container.style.paddingLeft = 16;
            container.style.paddingRight = 16;
            // container.style.backgroundColor = _uiManager.DesignSystem.ColorPalette.SurfaceDark;
            container.style.borderTopLeftRadius = 8;
            container.style.borderTopRightRadius = 8;
            container.style.borderBottomLeftRadius = 8;
            container.style.borderBottomRightRadius = 8;
            
            var labelElement = new Label(label);
            labelElement.style.fontSize = 14;
            // labelElement.style.color = _uiManager.DesignSystem.ColorPalette.TextPrimary;
            labelElement.style.unityFontStyleAndWeight = FontStyle.Bold;
            labelElement.style.marginBottom = 4;
            
            var descElement = new Label(description);
            descElement.style.fontSize = 12;
            // descElement.style.color = _uiManager.DesignSystem.ColorPalette.TextSecondary;
            descElement.style.marginBottom = 8;
            
            var dropdown = new DropdownField(options.ToList(), GetSettingValue(key, defaultIndex));
            dropdown.name = key + "-dropdown";
            dropdown.RegisterCallback<ChangeEvent<string>>(evt => {
                var index = System.Array.IndexOf(options, evt.newValue);
                OnSettingChanged(key, index);
            });
            
            container.Add(labelElement);
            container.Add(descElement);
            container.Add(dropdown);
            
            return container;
        }
        
        /// <summary>
        /// Create the footer section
        /// </summary>
        private void CreateFooter()
        {
            // Left side - Reset button
            _resetButton = new Button();
            _resetButton.name = "settings-reset-button";
            _resetButton.text = "Reset to Defaults";
            // _uiManager.ApplyDesignSystemStyle(_resetButton, UIStyleToken.SecondaryButton);
            
            // Right side - Action buttons
            var actionContainer = new VisualElement();
            actionContainer.style.flexDirection = FlexDirection.Row;
            
            _cancelButton = new Button();
            _cancelButton.name = "settings-cancel-button";
            _cancelButton.text = "Cancel";
            _cancelButton.style.marginRight = 8;
            // _uiManager.ApplyDesignSystemStyle(_cancelButton, UIStyleToken.SecondaryButton);
            
            _applyButton = new Button();
            _applyButton.name = "settings-apply-button";
            _applyButton.text = "Apply";
            _applyButton.style.marginRight = 8;
            // _uiManager.ApplyDesignSystemStyle(_applyButton, UIStyleToken.SecondaryButton);
            
            _saveButton = new Button();
            _saveButton.name = "settings-save-button";
            _saveButton.text = "Save";
            // _uiManager.ApplyDesignSystemStyle(_saveButton, UIStyleToken.PrimaryButton);
            
            actionContainer.Add(_cancelButton);
            actionContainer.Add(_applyButton);
            actionContainer.Add(_saveButton);
            
            _footerContainer.Add(_resetButton);
            _footerContainer.Add(actionContainer);
        }
        
        /// <summary>
        /// Load current settings values
        /// </summary>
        private void LoadCurrentSettings()
        {
            // if (_settingsManager != null)
            // {
                // Load from settings manager
                // This would integrate with the actual settings system
            // }
            
            // For now, use default values
            _currentSettings.Clear();
            _originalSettings.Clear();
            
            // Copy current to original for change detection
            foreach (var kvp in _currentSettings)
            {
                _originalSettings[kvp.Key] = kvp.Value;
            }
        }
        
        /// <summary>
        /// Get setting value with default fallback
        /// </summary>
        private T GetSettingValue<T>(string key, T defaultValue)
        {
            if (_currentSettings.TryGetValue(key, out var value))
            {
                return (T)value;
            }
            return defaultValue;
        }
        
        /// <summary>
        /// Show settings category
        /// </summary>
        private void ShowCategory(SettingsCategory category)
        {
            _currentCategory = category;
            
            // Hide all sections
            _gameplaySettings.style.display = DisplayStyle.None;
            _graphicsSettings.style.display = DisplayStyle.None;
            _audioSettings.style.display = DisplayStyle.None;
            _controlsSettings.style.display = DisplayStyle.None;
            _accessibilitySettings.style.display = DisplayStyle.None;
            
            // Show selected section
            switch (category)
            {
                case SettingsCategory.Gameplay:
                    _gameplaySettings.style.display = DisplayStyle.Flex;
                    break;
                case SettingsCategory.Graphics:
                    _graphicsSettings.style.display = DisplayStyle.Flex;
                    break;
                case SettingsCategory.Audio:
                    _audioSettings.style.display = DisplayStyle.Flex;
                    break;
                case SettingsCategory.Controls:
                    _controlsSettings.style.display = DisplayStyle.Flex;
                    break;
                case SettingsCategory.Accessibility:
                    _accessibilitySettings.style.display = DisplayStyle.Flex;
                    break;
            }
            
            // Update category button states
            UpdateCategoryButtons();
            
            // Update category indicator position
            UpdateCategoryIndicator();
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
                    // button.style.backgroundColor = _uiManager.DesignSystem.ColorPalette.PrimaryGreen;
                    // button.style.color = _uiManager.DesignSystem.ColorPalette.TextOnPrimary;
                }
                // else
                // {
                    button.style.backgroundColor = Color.clear;
                    // button.style.color = _uiManager.DesignSystem.ColorPalette.TextSecondary;
                // }
            }
        }
        
        /// <summary>
        /// Update category indicator position
        /// </summary>
        private void UpdateCategoryIndicator()
        {
            var buttonIndex = (int)_currentCategory;
            var topOffset = 24 + (buttonIndex * 48); // Account for padding and button spacing
            _categoryIndicator.style.top = topOffset;
        }
        
        /// <summary>
        /// Bind category button events
        /// </summary>
        private void BindCategoryButtons()
        {
            for (int i = 0; i < _categoryButtons.Count; i++)
            {
                var button = _categoryButtons[i];
                var category = (SettingsCategory)i;
                
                button.RegisterCallback<ClickEvent>(evt => {
                    ShowCategory(category);
                    PlaySettingsChangeSound();
                });
            }
        }
        
        /// <summary>
        /// Bind settings control events
        /// </summary>
        private void BindSettingsControls()
        {
            // This would bind events for all settings controls
            // Already handled in individual creation methods
        }
        
        /// <summary>
        /// Handle setting value change
        /// </summary>
        private void OnSettingChanged(string key, object value)
        {
            _currentSettings[key] = value;
            _hasUnsavedChanges = true;
            
            UpdateFooterButtons();
            PlaySettingsChangeSound();
            
            _onSettingsChanged?.Raise();
        }
        
        /// <summary>
        /// Update footer button states
        /// </summary>
        private void UpdateFooterButtons()
        {
            _applyButton.SetEnabled(_hasUnsavedChanges);
            _saveButton.SetEnabled(_hasUnsavedChanges);
            
            if (_hasUnsavedChanges)
            {
                _applyButton.style.opacity = 1f;
                _saveButton.style.opacity = 1f;
            }
            // else
            // {
                _applyButton.style.opacity = 0.5f;
                _saveButton.style.opacity = 0.5f;
            // }
        }
        
        /// <summary>
        /// Play settings change sound
        /// </summary>
        private void PlaySettingsChangeSound()
        {
            if (_settingsChangeSound != null)
            {
                // This would integrate with the audio system
                // AudioSource.PlayClipAtPoint(_settingsChangeSound, Vector3.zero);
            }
        }
        
        // Event handlers
        private void OnCloseClicked(ClickEvent evt)
        {
            if (_hasUnsavedChanges)
            {
                ShowUnsavedChangesDialog();
            }
            // else
            // {
                Hide();
            // }
        }
        
        private void OnResetClicked(ClickEvent evt)
        {
            ShowResetConfirmationDialog();
        }
        
        private void OnApplyClicked(ClickEvent evt)
        {
            ApplySettings();
        }
        
        private void OnSaveClicked(ClickEvent evt)
        {
            SaveSettings();
        }
        
        private void OnCancelClicked(ClickEvent evt)
        {
            if (_hasUnsavedChanges)
            {
                ShowUnsavedChangesDialog();
            }
            // else
            // {
                Hide();
            // }
        }
        
        /// <summary>
        /// Apply current settings
        /// </summary>
        private void ApplySettings()
        {
            // if (_settingsManager != null)
            // {
                // Apply settings through manager
                foreach (var kvp in _currentSettings)
                {
                    // _settingsManager.SetSetting(kvp.Key, kvp.Value);
                }
            // }
            
            _hasUnsavedChanges = false;
            UpdateFooterButtons();
            
            LogInfo("Settings applied");
        }
        
        /// <summary>
        /// Save current settings
        /// </summary>
        private void SaveSettings()
        {
            ApplySettings();
            
            // if (_settingsManager != null)
            // {
                // _settingsManager.SaveSettings();
            // }
            
            // Update original settings
            _originalSettings.Clear();
            foreach (var kvp in _currentSettings)
            {
                _originalSettings[kvp.Key] = kvp.Value;
            }
            
            _onSettingsSaved?.Raise();
            LogInfo("Settings saved");
            
            Hide();
        }
        
        /// <summary>
        /// Show unsaved changes dialog
        /// </summary>
        private void ShowUnsavedChangesDialog()
        {
            // Create modal dialog for unsaved changes
            // Implementation similar to main menu exit confirmation
        }
        
        /// <summary>
        /// Show reset confirmation dialog
        /// </summary>
        private void ShowResetConfirmationDialog()
        {
            // Create modal dialog for reset confirmation
            // Implementation similar to main menu exit confirmation
        }
        
        protected override void OnAfterShow()
        {
            base.OnAfterShow();
            LoadCurrentSettings();
            UpdateFooterButtons();
        }
    }
    
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
}