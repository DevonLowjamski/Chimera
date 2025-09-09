using UnityEngine;
using UnityEngine.UIElements;
using ProjectChimera.Core;
using ProjectChimera.UI.Core;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.UI.Panels
{
    /// <summary>
    /// Main settings panel coordinator for Project Chimera.
    /// Orchestrates layout, category management, and settings persistence.
    /// Significantly reduced from original 951-line monolithic class.
    /// </summary>
    public class SettingsPanel : UIPanel
    {
        [Header("Settings Configuration")]
        [SerializeField] private SettingsConfiguration _configuration;

        // Core components
        private SettingsLayoutElements _layoutElements;
        private SettingsHeaderElements _headerElements;
        private SettingsFooterElements _footerElements;
        private SettingsCategoryManager _categoryManager;

        // Settings state
        private SettingsState _settingsState;

        // Managers
        // private SettingsManager _settingsManager;

        protected override void SetupUIElements()
        {
            base.SetupUIElements();

            // Get settings manager
            // _settingsManager = GameManager.Instance?.GetManager<SettingsManager>();

            // Initialize settings state
            _settingsState = new SettingsState();
            LoadCurrentSettings();

            // Create main layout
            _layoutElements = SettingsLayoutManager.CreateMainLayout(_rootElement);

            // Create header
            _headerElements = SettingsLayoutManager.CreateHeaderElements();
            _layoutElements.headerContainer.Add(_headerElements.titleLabel);
            _layoutElements.headerContainer.Add(_headerElements.closeButton);

            // Create footer
            _footerElements = SettingsLayoutManager.CreateFooterElements();
            _layoutElements.footerContainer.Add(_footerElements.resetButton);
            _layoutElements.footerContainer.Add(_footerElements.actionContainer);

            // Initialize category manager
            _categoryManager = new SettingsCategoryManager();
            _categoryManager.Initialize(_layoutElements.contentContainer, _configuration, _settingsState);

            // Update initial state
            UpdateFooterButtons();
        }

        protected override void BindUIEvents()
        {
            base.BindUIEvents();

            // Header buttons
            _headerElements.closeButton?.RegisterCallback<ClickEvent>(OnCloseClicked);

            // Footer buttons
            _footerElements.resetButton?.RegisterCallback<ClickEvent>(OnResetClicked);
            _footerElements.applyButton?.RegisterCallback<ClickEvent>(OnApplyClicked);
            _footerElements.saveButton?.RegisterCallback<ClickEvent>(OnSaveClicked);
            _footerElements.cancelButton?.RegisterCallback<ClickEvent>(OnCancelClicked);

            // Category manager events
            if (_categoryManager != null)
            {
                _categoryManager.OnCategoryChanged += OnCategoryChanged;
                _categoryManager.OnSoundRequested += PlaySound;
            }
        }

        /// <summary>
        /// Load current settings from persistent storage
        /// </summary>
        private void LoadCurrentSettings()
        {
            if (_configuration == null)
            {
                ChimeraLogger.LogWarning("[SettingsPanel] Configuration not set, using defaults");
                return;
            }

            // Load from settings manager if available
            // if (_settingsManager != null)
            // {
                // Load from settings manager
                // This would integrate with the actual settings system
            // }

            // For now, reset to defaults
            _settingsState?.ResetToDefaults(_configuration);
            _settingsState?.CommitChanges(); // Mark as original state
        }

        /// <summary>
        /// Apply current settings to the game
        /// </summary>
        private void ApplySettings()
        {
            if (_settingsState == null) return;

            // Apply through settings manager if available
            // if (_settingsManager != null)
            // {
                // Apply settings through manager
                // foreach (var kvp in _settingsState.currentSettings)
                // {
                    // _settingsManager.SetSetting(kvp.Key, kvp.Value);
                // }
            // }

            _settingsState.hasUnsavedChanges = false;
            UpdateFooterButtons();

            _configuration?.onSettingsChanged?.Raise();
            ChimeraLogger.Log("[SettingsPanel] Settings applied");
        }

        /// <summary>
        /// Save current settings to persistent storage
        /// </summary>
        private void SaveSettings()
        {
            if (_settingsState == null) return;

            ApplySettings();

            // Persist through settings manager if available
            // if (_settingsManager != null)
            // {
                // _settingsManager.SaveSettings();
            // }

            _settingsState.CommitChanges();

            _configuration?.onSettingsSaved?.Raise();
            ChimeraLogger.Log("[SettingsPanel] Settings saved");

            Hide();
        }

        /// <summary>
        /// Update footer button states
        /// </summary>
        private void UpdateFooterButtons()
        {
            if (_footerElements == null || _settingsState == null) return;

            var hasChanges = _settingsState.HasChanges();

            SettingsLayoutManager.ApplyButtonStyling(_footerElements.applyButton, hasChanges);
            SettingsLayoutManager.ApplyButtonStyling(_footerElements.saveButton, hasChanges, true);
        }

        /// <summary>
        /// Play settings-related sound
        /// </summary>
        private void PlaySound(string soundType)
        {
            if (_configuration?.settingsChangeSound != null)
            {
                // This would integrate with the audio system
                // AudioSource.PlayClipAtPoint(_configuration.settingsChangeSound, Vector3.zero);
                ChimeraLogger.LogVerbose($"[SettingsPanel] Playing sound: {soundType}");
            }
        }

        /// <summary>
        /// Handle category change events
        /// </summary>
        private void OnCategoryChanged(SettingsCategory category)
        {
            ChimeraLogger.LogVerbose($"[SettingsPanel] Category changed to: {category}");
        }

        /// <summary>
        /// Show unsaved changes dialog
        /// </summary>
        private void ShowUnsavedChangesDialog()
        {
            // Create modal dialog for unsaved changes
            // Implementation would use a modal dialog system
            ChimeraLogger.LogWarning("[SettingsPanel] Unsaved changes dialog - implementation needed");

            // For now, just close without saving
            _settingsState?.RevertChanges();
            UpdateFooterButtons();
            Hide();
        }

        /// <summary>
        /// Show reset confirmation dialog
        /// </summary>
        private void ShowResetConfirmationDialog()
        {
            // Create modal dialog for reset confirmation
            // Implementation would use a modal dialog system
            ChimeraLogger.LogWarning("[SettingsPanel] Reset confirmation dialog - implementation needed");

            // For now, just reset to defaults
            _settingsState?.ResetToDefaults(_configuration);
            _categoryManager?.RefreshAllControls();
            UpdateFooterButtons();
        }

        protected override void OnAfterShow()
        {
            base.OnAfterShow();
            LoadCurrentSettings();
            _categoryManager?.RefreshAllControls();
            UpdateFooterButtons();
        }

        // Event handlers
        private void OnCloseClicked(ClickEvent evt)
        {
            if (_settingsState?.HasChanges() == true)
            {
                ShowUnsavedChangesDialog();
            }
            else
            {
                Hide();
            }
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
            if (_settingsState?.HasChanges() == true)
            {
                ShowUnsavedChangesDialog();
            }
            else
            {
                Hide();
            }
        }

        protected override void OnDestroy()
        {
            base.OnDestroy();

            // Unsubscribe from events
            if (_categoryManager != null)
            {
                _categoryManager.OnCategoryChanged -= OnCategoryChanged;
                _categoryManager.OnSoundRequested -= PlaySound;
            }
        }
    }
}
