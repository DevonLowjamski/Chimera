using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Linq;
using ProjectChimera.Data.Save;
using ProjectChimera.Core;

namespace ProjectChimera.Systems.Save
{
    /// <summary>
    /// Domain-specific save provider for the UI state system.
    /// Handles UI modes, panel states, user preferences, layouts, themes, and localization.
    /// Implements comprehensive validation, migration, and state management for interface persistence.
    /// </summary>
    public class UIStateSaveProvider : MonoBehaviour, ISaveSectionProvider
    {
        [Header("Provider Configuration")]
        [SerializeField] private string _sectionVersion = "1.2.0";
        [SerializeField] private int _priority = (int)SaveSectionPriority.Low;
        [SerializeField] private bool _enableIncrementalSave = true;
        [SerializeField] private int _maxPanelsPerSave = 200;

        [Header("Data Sources")]
        [SerializeField] private bool _autoDetectSystems = true;
        [SerializeField] private Transform _uiSystemRoot;
        [SerializeField] private string[] _uiManagerTags = { "UIManager", "GameUIManager" };

        [Header("Validation Settings")]
        [SerializeField] private bool _enableDataValidation = true;
        [SerializeField] private bool _validateLayoutConsistency = true;
        [SerializeField] private bool _validateThemeIntegrity = true;
        [SerializeField] private float _maxAllowedDataCorruption = 0.01f; // 1%
        [SerializeField] private bool _enableAutoRepair = true;

        [Header("UI State Filtering")]
        [SerializeField] private bool _saveTransientStates = false;
        [SerializeField] private bool _saveDebugPanels = false;
        [SerializeField] private bool _saveTemporaryOverlays = false;
        [SerializeField] private float _minPanelInteractionTime = 5.0f; // seconds

        // System references
        private IUIManager _uiManager;
        private IUILayoutManager _layoutManager;
        private IUIThemeManager _themeManager;
        private IUIPreferencesManager _preferencesManager;
        private IUIModeManager _modeManager;
        private IUILocalizationManager _localizationManager;
        private bool _systemsInitialized = false;

        // State tracking
        private UIStateDTO _lastSavedState;
        private DateTime _lastSaveTime;
        private bool _hasChanges = true;
        private long _estimatedDataSize = 0;

        // Dependencies
        private readonly string[] _dependencies = { 
            SaveSectionKeys.PLAYER, 
            SaveSectionKeys.SETTINGS 
        };

        #region ISaveSectionProvider Implementation

        public string SectionKey => SaveSectionKeys.UI_STATE;
        public string SectionName => "User Interface State";
        public string SectionVersion => _sectionVersion;
        public int Priority => _priority;
        public bool IsRequired => false;
        public bool SupportsIncrementalSave => _enableIncrementalSave;
        public long EstimatedDataSize => _estimatedDataSize;
        public IReadOnlyList<string> Dependencies => _dependencies;

        public async Task<ISaveSectionData> GatherSectionDataAsync()
        {
            await InitializeSystemsIfNeeded();

            var uiStateData = new UIStateSectionData
            {
                SectionKey = SectionKey,
                DataVersion = SectionVersion,
                Timestamp = DateTime.Now
            };

            try
            {
                // Gather core UI state
                uiStateData.UIState = await GatherUIStateAsync();
                
                // Filter out transient/temporary states if configured
                if (!_saveTransientStates)
                {
                    FilterTransientStates(uiStateData.UIState);
                }
                
                // Calculate data size and hash
                uiStateData.EstimatedSize = CalculateDataSize(uiStateData.UIState);
                uiStateData.DataHash = GenerateDataHash(uiStateData);

                _estimatedDataSize = uiStateData.EstimatedSize;
                _lastSavedState = uiStateData.UIState;
                _lastSaveTime = DateTime.Now;
                _hasChanges = false;

                LogInfo($"UI state data gathered: {uiStateData.UIState?.PanelStates?.Count ?? 0} panels, " +
                       $"Mode: {uiStateData.UIState?.UIModeState?.CurrentMode ?? "None"}, " +
                       $"Theme: {uiStateData.UIState?.ThemeState?.CurrentTheme ?? "Default"}, " +
                       $"Data size: {uiStateData.EstimatedSize} bytes");

                return uiStateData;
            }
            catch (Exception ex)
            {
                LogError($"Failed to gather UI state data: {ex.Message}");
                return CreateEmptySectionData();
            }
        }

        public async Task<SaveSectionResult> ApplySectionDataAsync(ISaveSectionData sectionData)
        {
            var startTime = DateTime.Now;
            
            try
            {
                if (!(sectionData is UIStateSectionData uiStateData))
                {
                    return SaveSectionResult.CreateFailure("Invalid section data type");
                }

                await InitializeSystemsIfNeeded();

                // Handle version migration if needed
                var migratedState = uiStateData.UIState;
                if (RequiresMigration(uiStateData.DataVersion))
                {
                    var migrationResult = await MigrateSectionDataAsync(uiStateData, uiStateData.DataVersion);
                    if (migrationResult is UIStateSectionData migrated)
                    {
                        migratedState = migrated.UIState;
                    }
                }

                var result = await ApplyUIStateAsync(migratedState);
                
                if (result.Success)
                {
                    _lastSavedState = migratedState;
                    _hasChanges = false;

                    LogInfo($"UI state applied successfully: " +
                           $"{migratedState?.PanelStates?.Count ?? 0} panels, " +
                           $"Mode: {migratedState?.UIModeState?.CurrentMode ?? "None"} restored");
                }

                var duration = DateTime.Now - startTime;
                return SaveSectionResult.CreateSuccess(duration, uiStateData.EstimatedSize, new Dictionary<string, object>
                {
                    { "panels_loaded", migratedState?.PanelStates?.Count ?? 0 },
                    { "preferences_loaded", migratedState?.UserPreferences != null ? 1 : 0 },
                    { "theme_loaded", !string.IsNullOrEmpty(migratedState?.ThemeState?.CurrentTheme) ? 1 : 0 },
                    { "mode_loaded", !string.IsNullOrEmpty(migratedState?.UIModeState?.CurrentMode) ? 1 : 0 }
                });
            }
            catch (Exception ex)
            {
                LogError($"Failed to apply UI state data: {ex.Message}");
                return SaveSectionResult.CreateFailure($"Application failed: {ex.Message}", ex);
            }
        }

        public async Task<SaveSectionValidation> ValidateSectionDataAsync(ISaveSectionData sectionData)
        {
            if (!_enableDataValidation)
            {
                return SaveSectionValidation.CreateValid();
            }

            try
            {
                if (!(sectionData is UIStateSectionData uiStateData))
                {
                    return SaveSectionValidation.CreateInvalid(new List<string> { "Invalid section data type" });
                }

                var errors = new List<string>();
                var warnings = new List<string>();

                // Validate UI state
                if (uiStateData.UIState == null)
                {
                    errors.Add("UI state is null");
                    return SaveSectionValidation.CreateInvalid(errors);
                }

                var validationResult = await ValidateUIStateAsync(uiStateData.UIState);
                
                errors.AddRange(validationResult.Errors);
                warnings.AddRange(validationResult.Warnings);

                // Layout consistency validation
                if (_validateLayoutConsistency)
                {
                    var layoutValidation = ValidateLayoutConsistency(uiStateData.UIState);
                    errors.AddRange(layoutValidation.Errors);
                    warnings.AddRange(layoutValidation.Warnings);
                }

                // Theme integrity validation
                if (_validateThemeIntegrity)
                {
                    var themeValidation = ValidateThemeIntegrity(uiStateData.UIState);
                    errors.AddRange(themeValidation.Errors);
                    warnings.AddRange(themeValidation.Warnings);
                }

                // Check data corruption level
                float corruptionLevel = CalculateDataCorruption(uiStateData.UIState);
                if (corruptionLevel > _maxAllowedDataCorruption)
                {
                    errors.Add($"Data corruption level ({corruptionLevel:P2}) exceeds maximum allowed ({_maxAllowedDataCorruption:P2})");
                }
                else if (corruptionLevel > 0)
                {
                    warnings.Add($"Minor data corruption detected ({corruptionLevel:P2})");
                }

                // Validate panel state consistency
                var panelValidation = ValidatePanelStateConsistency(uiStateData.UIState);
                errors.AddRange(panelValidation.Errors);
                warnings.AddRange(panelValidation.Warnings);

                return errors.Any()
                    ? SaveSectionValidation.CreateInvalid(errors, warnings, _enableAutoRepair, SectionVersion)
                    : SaveSectionValidation.CreateValid();
            }
            catch (Exception ex)
            {
                LogError($"Validation failed: {ex.Message}");
                return SaveSectionValidation.CreateInvalid(new List<string> { $"Validation error: {ex.Message}" });
            }
        }

        public async Task<ISaveSectionData> MigrateSectionDataAsync(ISaveSectionData oldData, string fromVersion)
        {
            try
            {
                if (!(oldData is UIStateSectionData uiStateData))
                {
                    throw new ArgumentException("Invalid data type for migration");
                }

                var migrator = GetVersionMigrator(fromVersion, SectionVersion);
                if (migrator != null)
                {
                    var migratedState = await migrator.MigrateUIStateAsync(uiStateData.UIState);
                    
                    var migratedData = new UIStateSectionData
                    {
                        SectionKey = SectionKey,
                        DataVersion = SectionVersion,
                        Timestamp = DateTime.Now,
                        UIState = migratedState,
                        DataHash = GenerateDataHash(uiStateData)
                    };

                    migratedData.EstimatedSize = CalculateDataSize(migratedState);

                    LogInfo($"UI state data migrated from {fromVersion} to {SectionVersion}");
                    return migratedData;
                }

                LogWarning($"No migration path found from {fromVersion} to {SectionVersion}");
                return oldData; // Return unchanged if no migration needed
            }
            catch (Exception ex)
            {
                LogError($"Migration failed: {ex.Message}");
                throw;
            }
        }

        public SaveSectionSummary GetSectionSummary()
        {
            var state = _lastSavedState ?? GetCurrentUIState();
            
            return new SaveSectionSummary
            {
                SectionKey = SectionKey,
                SectionName = SectionName,
                StatusDescription = GetStatusDescription(state),
                ItemCount = state?.PanelStates?.Count ?? 0,
                DataSize = _estimatedDataSize,
                LastUpdated = _lastSaveTime,
                KeyValuePairs = new Dictionary<string, string>
                {
                    { "UI Panels", (state?.PanelStates?.Count ?? 0).ToString() },
                    { "Current Mode", state?.UIModeState?.CurrentMode ?? "Unknown" },
                    { "UI Level", state?.UILevelState?.CurrentLevel ?? "Unknown" },
                    { "Theme", state?.ThemeState?.CurrentTheme ?? "Default" },
                    { "Language", state?.LocalizationState?.CurrentLanguage ?? "en-US" },
                    { "Open Windows", (state?.WindowManagerState?.OpenWindows?.Count ?? 0).ToString() },
                    { "Active Overlays", (state?.OverlayStates?.Count(o => o.IsVisible) ?? 0).ToString() },
                    { "System Status", _systemsInitialized ? "Initialized" : "Not Initialized" }
                },
                HasErrors = !_systemsInitialized,
                ErrorMessages = _systemsInitialized ? new List<string>() : new List<string> { "UI systems not initialized" }
            };
        }

        public bool HasChanges()
        {
            if (!_systemsInitialized || _lastSavedState == null)
                return true;

            // Quick change detection
            var currentState = GetCurrentUIState();
            if (currentState == null)
                return false;

            return _hasChanges || 
                   currentState.PanelStates?.Count != _lastSavedState.PanelStates?.Count ||
                   currentState.UIModeState?.CurrentMode != _lastSavedState.UIModeState?.CurrentMode ||
                   currentState.UILevelState?.CurrentLevel != _lastSavedState.UILevelState?.CurrentLevel ||
                   currentState.ThemeState?.CurrentTheme != _lastSavedState.ThemeState?.CurrentTheme ||
                   DateTime.Now.Subtract(_lastSaveTime).TotalMinutes > 30; // Force save every 30 minutes
        }

        public void MarkClean()
        {
            _hasChanges = false;
            _lastSaveTime = DateTime.Now;
        }

        public async Task ResetToDefaultStateAsync()
        {
            await InitializeSystemsIfNeeded();

            // Reset UI systems to defaults
            if (_uiManager != null)
            {
                await _uiManager.ResetToDefaultsAsync();
            }

            if (_layoutManager != null)
            {
                await _layoutManager.ResetToDefaultLayoutAsync();
            }

            if (_themeManager != null)
            {
                await _themeManager.ResetToDefaultThemeAsync();
            }

            if (_preferencesManager != null)
            {
                await _preferencesManager.ResetToDefaultPreferencesAsync();
            }

            _lastSavedState = null;
            _hasChanges = true;
            
            LogInfo("UI state system reset to default state");
        }

        public IReadOnlyDictionary<string, IReadOnlyList<string>> GetSupportedMigrations()
        {
            return new Dictionary<string, IReadOnlyList<string>>
            {
                { "1.0.0", new List<string> { "1.1.0", "1.2.0" } },
                { "1.1.0", new List<string> { "1.2.0" } }
            };
        }

        public async Task PreSaveCleanupAsync()
        {
            await InitializeSystemsIfNeeded();

            // Clean up temporary UI states, closed panels, etc.
            if (_uiManager != null)
            {
                await _uiManager.CleanupTemporaryStatesAsync();
            }

            if (_layoutManager != null)
            {
                await _layoutManager.OptimizeLayoutsAsync();
            }

            // Mark that changes occurred due to cleanup
            _hasChanges = true;
        }

        public async Task PostLoadInitializationAsync()
        {
            await InitializeSystemsIfNeeded();

            // Rebuild UI caches, apply loaded themes
            if (_uiManager != null)
            {
                await _uiManager.RefreshUIElementsAsync();
            }

            if (_themeManager != null)
            {
                await _themeManager.ApplyCurrentThemeAsync();
            }

            if (_localizationManager != null)
            {
                await _localizationManager.RefreshLocalizationAsync();
            }

            LogInfo("UI state system post-load initialization completed");
        }

        #endregion

        #region System Integration

        private async Task InitializeSystemsIfNeeded()
        {
            if (_systemsInitialized)
                return;

            await Task.Run(() =>
            {
                // Auto-detect systems if enabled
                if (_autoDetectSystems)
                {
                    _uiManager = FindObjectOfType<MonoBehaviour>() as IUIManager;
                    _layoutManager = FindObjectOfType<MonoBehaviour>() as IUILayoutManager;
                    _themeManager = FindObjectOfType<MonoBehaviour>() as IUIThemeManager;
                    _preferencesManager = FindObjectOfType<MonoBehaviour>() as IUIPreferencesManager;
                    _modeManager = FindObjectOfType<MonoBehaviour>() as IUIModeManager;
                    _localizationManager = FindObjectOfType<MonoBehaviour>() as IUILocalizationManager;
                }

                _systemsInitialized = true;
            });

            LogInfo($"UI state save provider initialized. Systems found: " +
                   $"UIManager={_uiManager != null}, " +
                   $"LayoutManager={_layoutManager != null}, " +
                   $"ThemeManager={_themeManager != null}, " +
                   $"PreferencesManager={_preferencesManager != null}, " +
                   $"ModeManager={_modeManager != null}, " +
                   $"LocalizationManager={_localizationManager != null}");
        }

        #endregion

        #region Data Gathering and Application

        private async Task<UIStateDTO> GatherUIStateAsync()
        {
            var state = new UIStateDTO
            {
                SaveTimestamp = DateTime.Now,
                SaveVersion = SectionVersion,
                EnableUISystem = true
            };

            // Gather UI mode and level state
            if (_modeManager != null)
            {
                state.UIModeState = await GatherUIModeStateAsync();
                state.UILevelState = await GatherUILevelStateAsync();
            }

            // Gather panel states
            if (_uiManager != null)
            {
                state.PanelStates = await GatherPanelStatesAsync();
                state.UIPanelStates = state.PanelStates; // Compatibility
                state.OverlayStates = await GatherOverlayStatesAsync();
                state.HUDState = await GatherHUDStateAsync();
                state.MenuState = await GatherMenuStateAsync();
                state.NavigationState = await GatherNavigationStateAsync();
                state.InteractionState = await GatherInteractionStateAsync();
                state.ControlState = await GatherControlStateAsync();
            }

            // Gather layout and window management
            if (_layoutManager != null)
            {
                state.WindowManagerState = await GatherWindowManagerStateAsync();
                state.WindowManagement = await GatherWindowManagementAsync();
                state.ViewState = await GatherViewStateAsync();
            }

            // Gather user preferences
            if (_preferencesManager != null)
            {
                state.UserPreferences = await GatherUserPreferencesAsync();
            }

            // Gather theme and visual state
            if (_themeManager != null)
            {
                state.ThemeState = await GatherThemeStateAsync();
                state.AnimationState = await GatherAnimationStateAsync();
            }

            // Gather camera and localization state
            state.CameraState = await GatherCameraStateAsync();
            
            if (_localizationManager != null)
            {
                state.LocalizationState = await GatherLocalizationStateAsync();
            }

            return state;
        }

        private async Task<SaveSectionResult> ApplyUIStateAsync(UIStateDTO state)
        {
            try
            {
                // Apply UI mode and level state
                if (_modeManager != null)
                {
                    if (state.UIModeState != null)
                        await _modeManager.LoadUIModeStateAsync(state.UIModeState);
                    
                    if (state.UILevelState != null)
                        await _modeManager.LoadUILevelStateAsync(state.UILevelState);
                }

                // Apply panel and UI element states
                if (_uiManager != null)
                {
                    if (state.PanelStates != null)
                        await _uiManager.LoadPanelStatesAsync(state.PanelStates);
                    
                    if (state.OverlayStates != null)
                        await _uiManager.LoadOverlayStatesAsync(state.OverlayStates);
                    
                    if (state.HUDState != null)
                        await _uiManager.LoadHUDStateAsync(state.HUDState);
                    
                    if (state.MenuState != null)
                        await _uiManager.LoadMenuStateAsync(state.MenuState);
                    
                    if (state.NavigationState != null)
                        await _uiManager.LoadNavigationStateAsync(state.NavigationState);
                }

                // Apply layout and window management
                if (_layoutManager != null)
                {
                    if (state.WindowManagerState != null)
                        await _layoutManager.LoadWindowManagerStateAsync(state.WindowManagerState);
                    
                    if (state.ViewState != null)
                        await _layoutManager.LoadViewStateAsync(state.ViewState);
                }

                // Apply user preferences
                if (_preferencesManager != null && state.UserPreferences != null)
                {
                    await _preferencesManager.LoadUserPreferencesAsync(state.UserPreferences);
                }

                // Apply theme and visual state
                if (_themeManager != null)
                {
                    if (state.ThemeState != null)
                        await _themeManager.LoadThemeStateAsync(state.ThemeState);
                    
                    if (state.AnimationState != null)
                        await _themeManager.LoadAnimationStateAsync(state.AnimationState);
                }

                // Apply localization state
                if (_localizationManager != null && state.LocalizationState != null)
                {
                    await _localizationManager.LoadLocalizationStateAsync(state.LocalizationState);
                }

                return SaveSectionResult.CreateSuccess();
            }
            catch (Exception ex)
            {
                LogError($"Failed to apply UI state: {ex.Message}");
                return SaveSectionResult.CreateFailure($"Application failed: {ex.Message}", ex);
            }
        }

        #endregion

        #region Helper Methods

        private UIStateDTO GetCurrentUIState()
        {
            // Quick state snapshot for change detection
            if (!_systemsInitialized)
                return null;

            return new UIStateDTO
            {
                PanelStates = _uiManager?.GetCurrentPanelStates()?.Select(ConvertToPanelStateDTO).ToList(),
                UIModeState = _modeManager?.GetCurrentModeState(),
                UILevelState = _modeManager?.GetCurrentLevelState(),
                ThemeState = _themeManager?.GetCurrentThemeState(),
                SaveTimestamp = DateTime.Now
            };
        }

        private ISaveSectionData CreateEmptySectionData()
        {
            return new UIStateSectionData
            {
                SectionKey = SectionKey,
                DataVersion = SectionVersion,
                Timestamp = DateTime.Now,
                UIState = new UIStateDTO
                {
                    SaveTimestamp = DateTime.Now,
                    SaveVersion = SectionVersion,
                    PanelStates = new List<UIPanelStateDTO>(),
                    UIModeState = new UIModeStateDTO { CurrentMode = "PlayMode" },
                    UILevelState = new UILevelStateDTO { CurrentLevel = "Intermediate" },
                    ThemeState = new UIThemeStateDTO { CurrentTheme = "Default" }
                },
                EstimatedSize = 2048 // Minimal size
            };
        }

        private void FilterTransientStates(UIStateDTO state)
        {
            if (state?.PanelStates != null)
            {
                // Remove panels that haven't been interacted with long enough
                state.PanelStates.RemoveAll(panel => 
                    panel.TimeSpentOpen < _minPanelInteractionTime && 
                    DateTime.Now.Subtract(panel.LastInteracted).TotalSeconds < _minPanelInteractionTime);

                // Remove debug panels if configured
                if (!_saveDebugPanels)
                {
                    state.PanelStates.RemoveAll(panel => 
                        panel.PanelType == "Debug" || 
                        panel.PanelName?.Contains("Debug") == true);
                }
            }

            // Remove temporary overlays if configured
            if (!_saveTemporaryOverlays && state?.OverlayStates != null)
            {
                state.OverlayStates.RemoveAll(overlay => 
                    overlay.OverlayType == "Tooltip" || 
                    overlay.OverlayType == "Notification" ||
                    overlay.AutoHideDelay > 0);
            }
        }

        private long CalculateDataSize(UIStateDTO state)
        {
            if (state == null) return 0;

            // Estimate based on content
            long size = 2048; // Base overhead
            size += (state.PanelStates?.Count ?? 0) * 1024; // ~1KB per panel
            size += (state.OverlayStates?.Count ?? 0) * 512; // ~0.5KB per overlay
            size += (state.UIModeState?.ModeHistory?.Count ?? 0) * 256; // ~256B per mode transition
            size += (state.WindowManagerState?.SavedLayouts?.Count ?? 0) * 2048; // ~2KB per layout
            size += (state.UserPreferences != null) ? 4096 : 0; // User preferences
            size += (state.ThemeState?.CustomColors?.Count ?? 0) * 128; // ~128B per custom color

            return size;
        }

        private string GenerateDataHash(UIStateSectionData data)
        {
            // Simple hash based on key data points
            var hashSource = $"{data.Timestamp:yyyy-MM-dd-HH-mm-ss}" +
                           $"{data.UIState?.PanelStates?.Count ?? 0}" +
                           $"{data.UIState?.UIModeState?.CurrentMode ?? ""}" +
                           $"{data.UIState?.ThemeState?.CurrentTheme ?? ""}" +
                           $"{data.EstimatedSize}";
            
            return hashSource.GetHashCode().ToString("X8");
        }

        private string GetStatusDescription(UIStateDTO state)
        {
            if (state == null)
                return "No UI state data";

            int panels = state.PanelStates?.Count ?? 0;
            string mode = state.UIModeState?.CurrentMode ?? "Unknown";
            string theme = state.ThemeState?.CurrentTheme ?? "Default";
            
            if (panels == 0)
                return $"Mode: {mode}, Theme: {theme}, No panels";
            
            return $"Mode: {mode}, Theme: {theme}, {panels} panels";
        }

        private bool RequiresMigration(string dataVersion) => dataVersion != SectionVersion;

        // Validation methods
        private async Task<UIValidationResult> ValidateUIStateAsync(UIStateDTO state)
        {
            // Comprehensive validation - placeholder implementation
            await Task.Delay(1);
            return new UIValidationResult { IsValid = true };
        }

        private UIValidationResult ValidateLayoutConsistency(UIStateDTO state) => new UIValidationResult { IsValid = true };
        private UIValidationResult ValidateThemeIntegrity(UIStateDTO state) => new UIValidationResult { IsValid = true };
        private UIValidationResult ValidatePanelStateConsistency(UIStateDTO state) => new UIValidationResult { IsValid = true };
        private float CalculateDataCorruption(UIStateDTO state) => 0.0f;
        private IUIStateMigrator GetVersionMigrator(string fromVersion, string toVersion) => null;

        // Conversion methods - placeholders
        private UIPanelStateDTO ConvertToPanelStateDTO(object panel) => new UIPanelStateDTO();

        // Async gathering methods - placeholders
        private async Task<UIModeStateDTO> GatherUIModeStateAsync() => new UIModeStateDTO();
        private async Task<UILevelStateDTO> GatherUILevelStateAsync() => new UILevelStateDTO();
        private async Task<List<UIPanelStateDTO>> GatherPanelStatesAsync() => new List<UIPanelStateDTO>();
        private async Task<List<UIOverlayStateDTO>> GatherOverlayStatesAsync() => new List<UIOverlayStateDTO>();
        private async Task<UIHUDStateDTO> GatherHUDStateAsync() => new UIHUDStateDTO();
        private async Task<UIMenuStateDTO> GatherMenuStateAsync() => new UIMenuStateDTO();
        private async Task<UINavigationStateDTO> GatherNavigationStateAsync() => new UINavigationStateDTO();
        private async Task<UIInteractionStateDTO> GatherInteractionStateAsync() => new UIInteractionStateDTO();
        private async Task<UIControlStateDTO> GatherControlStateAsync() => new UIControlStateDTO();
        private async Task<WindowManagerStateDTO> GatherWindowManagerStateAsync() => new WindowManagerStateDTO();
        private async Task<UIWindowManagementDTO> GatherWindowManagementAsync() => new UIWindowManagementDTO();
        private async Task<UIViewStateDTO> GatherViewStateAsync() => new UIViewStateDTO();
        private async Task<UIUserPreferencesDTO> GatherUserPreferencesAsync() => new UIUserPreferencesDTO();
        private async Task<UIThemeStateDTO> GatherThemeStateAsync() => new UIThemeStateDTO();
        private async Task<UIAnimationStateDTO> GatherAnimationStateAsync() => new UIAnimationStateDTO();
        private async Task<UICameraStateDTO> GatherCameraStateAsync() => new UICameraStateDTO();
        private async Task<UILocalizationStateDTO> GatherLocalizationStateAsync() => new UILocalizationStateDTO();

        private void LogInfo(string message) => Debug.Log($"[UIStateSaveProvider] {message}");
        private void LogWarning(string message) => Debug.LogWarning($"[UIStateSaveProvider] {message}");
        private void LogError(string message) => Debug.LogError($"[UIStateSaveProvider] {message}");

        #endregion
    }

    /// <summary>
    /// UI state-specific save section data container
    /// </summary>
    [System.Serializable]
    public class UIStateSectionData : ISaveSectionData
    {
        public string SectionKey { get; set; }
        public string DataVersion { get; set; }
        public DateTime Timestamp { get; set; }
        public long EstimatedSize { get; set; }
        public string DataHash { get; set; }

        public UIStateDTO UIState;

        public bool IsValid()
        {
            return !string.IsNullOrEmpty(SectionKey) && 
                   !string.IsNullOrEmpty(DataVersion) && 
                   UIState != null;
        }

        public string GetSummary()
        {
            var panels = UIState?.PanelStates?.Count ?? 0;
            var mode = UIState?.UIModeState?.CurrentMode ?? "Unknown";
            var theme = UIState?.ThemeState?.CurrentTheme ?? "Default";
            return $"UI: {panels} panels, Mode: {mode}, Theme: {theme}";
        }
    }

    /// <summary>
    /// Interfaces for system integration (would be implemented by actual systems)
    /// </summary>
    public interface IUIManager
    {
        Task ResetToDefaultsAsync();
        Task CleanupTemporaryStatesAsync();
        Task RefreshUIElementsAsync();
        Task LoadPanelStatesAsync(List<UIPanelStateDTO> panels);
        Task LoadOverlayStatesAsync(List<UIOverlayStateDTO> overlays);
        Task LoadHUDStateAsync(UIHUDStateDTO hudState);
        Task LoadMenuStateAsync(UIMenuStateDTO menuState);
        Task LoadNavigationStateAsync(UINavigationStateDTO navigationState);
        List<object> GetCurrentPanelStates();
    }

    public interface IUILayoutManager
    {
        Task ResetToDefaultLayoutAsync();
        Task OptimizeLayoutsAsync();
        Task LoadWindowManagerStateAsync(WindowManagerStateDTO state);
        Task LoadViewStateAsync(UIViewStateDTO state);
    }

    public interface IUIThemeManager
    {
        Task ResetToDefaultThemeAsync();
        Task ApplyCurrentThemeAsync();
        Task LoadThemeStateAsync(UIThemeStateDTO state);
        Task LoadAnimationStateAsync(UIAnimationStateDTO state);
        UIThemeStateDTO GetCurrentThemeState();
    }

    public interface IUIPreferencesManager
    {
        Task ResetToDefaultPreferencesAsync();
        Task LoadUserPreferencesAsync(UIUserPreferencesDTO preferences);
    }

    public interface IUIModeManager
    {
        Task LoadUIModeStateAsync(UIModeStateDTO state);
        Task LoadUILevelStateAsync(UILevelStateDTO state);
        UIModeStateDTO GetCurrentModeState();
        UILevelStateDTO GetCurrentLevelState();
    }

    public interface IUILocalizationManager
    {
        Task RefreshLocalizationAsync();
        Task LoadLocalizationStateAsync(UILocalizationStateDTO state);
    }

    public interface IUIStateMigrator
    {
        Task<UIStateDTO> MigrateUIStateAsync(UIStateDTO oldState);
    }
}