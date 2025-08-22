using UnityEngine;
using System;
using System.Collections.Generic;

namespace ProjectChimera.Data.Save
{
    /// <summary>
    /// Data Transfer Objects for UI System Save/Load Operations
    /// These DTOs capture UI state including mode settings, level configurations,
    /// panel states, user preferences, and interface customizations.
    /// </summary>
    
    /// <summary>
    /// Main UI state DTO for the user interface system
    /// </summary>
    [System.Serializable]
    public class UIStateDTO
    {
        [Header("UI Mode and Level")]
        public UIModeStateDTO UIModeState;
        public UILevelStateDTO UILevelState;
        
        [Header("Panel States")]
        public List<UIPanelStateDTO> PanelStates = new List<UIPanelStateDTO>();
        
        [Header("Window Management")]
        public WindowManagerStateDTO WindowManagerState;
        
        [Header("User Preferences")]
        public UIUserPreferencesDTO UserPreferences;
        
        [Header("Camera and View")]
        public UICameraStateDTO CameraState;
        public UIViewStateDTO ViewState;
        
        [Header("Navigation and Menu")]
        public UINavigationStateDTO NavigationState;
        public UIMenuStateDTO MenuState;
        
        [Header("HUD and Overlays")]
        public UIHUDStateDTO HUDState;
        public List<UIOverlayStateDTO> OverlayStates = new List<UIOverlayStateDTO>();
        
        [Header("Interactive Elements")]
        public UIInteractionStateDTO InteractionState;
        public UIControlStateDTO ControlState;
        
        [Header("Theme and Visual")]
        public UIThemeStateDTO ThemeState;
        public UIAnimationStateDTO AnimationState;
        
        [Header("Localization")]
        public UILocalizationStateDTO LocalizationState;
        
        [Header("System Configuration")]
        public bool EnableUISystem = true;
        public bool EnableUIAnimations = true;
        public bool EnableTooltips = true;
        public bool EnableSoundEffects = true;
        public float UIScaleFactor = 1.0f;
        
        [Header("Panel States")]
        public List<UIPanelStateDTO> UIPanelStates = new List<UIPanelStateDTO>();
        
        [Header("Window Management")]
        public UIWindowManagementDTO WindowManagement;
        
        [Header("Save Metadata")]
        public DateTime SaveTimestamp;
        public string SaveVersion = "1.0";
    }
    
    /// <summary>
    /// DTO for UI mode state (Design Mode, Play Mode, etc.)
    /// </summary>
    [System.Serializable]
    public class UIModeStateDTO
    {
        [Header("Current Mode")]
        public string CurrentMode = "PlayMode"; // "DesignMode", "PlayMode", "BuildMode", "ManageMode"
        public string PreviousMode;
        public DateTime ModeChangedTime;
        
        [Header("Mode Settings")]
        public Dictionary<string, UIModeConfigDTO> ModeConfigurations = new Dictionary<string, UIModeConfigDTO>();
        
        [Header("Mode Transitions")]
        public bool AllowModeTransitions = true;
        public List<string> AvailableModes = new List<string>();
        public List<string> RestrictedModes = new List<string>();
        
        [Header("Mode History")]
        public List<UIModeTransitionDTO> ModeHistory = new List<UIModeTransitionDTO>();
        
        [Header("Mode Preferences")]
        public string PreferredStartupMode = "PlayMode";
        public bool RememberLastMode = true;
        public bool ShowModeTransitionAnimations = true;
        public DateTime LastModeChange;
        public Dictionary<string, object> ModePreferences = new Dictionary<string, object>();
    }
    
    /// <summary>
    /// DTO for UI level state (interface complexity level)
    /// </summary>
    [System.Serializable]
    public class UILevelStateDTO
    {
        [Header("Current Level")]
        public string CurrentLevel = "Intermediate"; // "Beginner", "Intermediate", "Advanced", "Expert"
        public int LevelIndex = 1; // 0-3 for the four levels
        
        [Header("Level Features")]
        public List<string> EnabledFeatures = new List<string>();
        public List<string> HiddenFeatures = new List<string>();
        public List<string> SimplifiedFeatures = new List<string>();
        
        [Header("Level Configurations")]
        public Dictionary<string, UILevelConfigDTO> LevelConfigurations = new Dictionary<string, UILevelConfigDTO>();
        
        [Header("Adaptive UI")]
        public bool EnableAdaptiveUI = true;
        public float UserExperienceLevel = 0.5f; // 0-1, influences automatic level adjustments
        public DateTime LastLevelChange;
        
        [Header("Tutorial Integration")]
        public bool ShowLevelSpecificTutorials = true;
        public List<string> CompletedLevelTutorials = new List<string>();
        
        [Header("User Progression")]
        public Dictionary<string, float> FeatureUsageStats = new Dictionary<string, float>();
        public int TimeInCurrentLevel = 0; // minutes
        
        [Header("Level Options")]
        public List<string> AvailableLevels = new List<string>();
        public bool ShowAdvancedControls = false;
        public bool ShowTooltips = true;
        public int UIComplexity = 1;
    }
    
    /// <summary>
    /// DTO for individual UI panel states
    /// </summary>
    [System.Serializable]
    public class UIPanelStateDTO
    {
        [Header("Panel Identity")]
        public string PanelId;
        public string PanelName;
        public string PanelType; // "Window", "Popup", "Sidebar", "Toolbar", "HUD"
        
        [Header("Visibility and State")]
        public bool IsVisible = true;
        public bool IsMinimized = false;
        public bool IsMaximized = false;
        public bool IsPinned = false;
        public bool IsLocked = false;
        
        [Header("Position and Size")]
        public Vector2 Position;
        public Vector2 Size;
        public Vector2 MinSize;
        public Vector2 MaxSize;
        
        [Header("Docking and Layout")]
        public string DockState; // "Floating", "Docked", "Tabbed", "Auto"
        public string DockPosition; // "Left", "Right", "Top", "Bottom", "Center"
        public int TabOrder = 0;
        public bool AllowDocking = true;
        
        [Header("Content State")]
        public Dictionary<string, object> ContentData = new Dictionary<string, object>();
        public string ActiveTab;
        public List<string> VisibleTabs = new List<string>();
        
        [Header("User Interactions")]
        public DateTime LastInteracted;
        public DateTime LastAccessed;
        public int InteractionCount = 0;
        public float TimeSpentOpen = 0f; // minutes
        public int ZOrder = 0;
        
        [Header("Panel Preferences")]
        public float Opacity = 1.0f;
        public bool EnableTransparency = false;
        public bool AutoHide = false;
        public float AutoHideDelay = 5.0f;
    }
    
    /// <summary>
    /// DTO for window manager state
    /// </summary>
    [System.Serializable]
    public class WindowManagerStateDTO
    {
        [Header("Window Layout")]
        public string CurrentLayout = "Default";
        public List<string> SavedLayouts = new List<string>();
        public Dictionary<string, UILayoutConfigDTO> LayoutConfigurations = new Dictionary<string, UILayoutConfigDTO>();
        
        [Header("Window Management")]
        public List<string> OpenWindows = new List<string>();
        public string ActiveWindow;
        public string FocusedWindow;
        
        [Header("Multi-Monitor Support")]
        public bool EnableMultiMonitor = false;
        public List<UIMonitorConfigDTO> MonitorConfigurations = new List<UIMonitorConfigDTO>();
        
        [Header("Window Behavior")]
        public bool EnableWindowSnapping = true;
        public bool EnableWindowGrouping = true;
        public bool RememberWindowPositions = true;
        public bool AutoSaveLayout = true;
        
        [Header("Performance")]
        public int MaxOpenWindows = 20;
        public bool EnableWindowVirtualization = true;
        public bool OptimizeOffscreenWindows = true;
    }
    
    /// <summary>
    /// DTO for UI user preferences
    /// </summary>
    [System.Serializable]
    public class UIUserPreferencesDTO
    {
        [Header("Visual Preferences")]
        public string ColorScheme = "Default"; // "Default", "Dark", "Light", "HighContrast"
        public string FontSizeCategory = "Medium"; // "Small", "Medium", "Large", "ExtraLarge"
        public string FontFamily = "Default";
        public bool EnableAnimations = true;
        public float AnimationSpeed = 1.0f;
        
        [Header("Interaction Preferences")]
        public string ClickBehavior = "Single"; // "Single", "Double"
        public bool EnableDragAndDrop = true;
        public bool EnableContextMenus = true;
        public float TooltipDelay = 0.5f;
        public bool EnableKeyboardShortcuts = true;
        
        [Header("Layout Preferences")]
        public bool CompactMode = false;
        public bool ShowStatusBar = true;
        public bool ShowToolbar = true;
        public bool AutoHidePanels = false;
        public string DefaultPanelPosition = "Right";
        
        [Header("Notification Preferences")]
        public bool EnableNotifications = true;
        public bool ShowNotifications = true;
        public bool EnableSoundNotifications = true;
        public bool EnablePopupNotifications = true;
        public float NotificationDuration = 5.0f;
        
        [Header("Accessibility")]
        public bool HighContrastMode = false;
        public bool LargeTextMode = false;
        public bool ScreenReaderSupport = false;
        public bool ReducedMotion = false;
        public bool EnableVoiceCommands = false;
        
        [Header("Performance Preferences")]
        public bool EnableVSync = true;
        public int TargetFrameRate = 60;
        public bool EnableUIBatching = true;
        public bool ReduceUIComplexity = false;
        
        [Header("User Interface Settings")]
        public string Theme = "Default";
        public float FontSize = 14f;
        public float UIScale = 1f;
        public bool AutoSavePanelStates = true;
        public DateTime LastPreferencesUpdate;
    }
    
    /// <summary>
    /// DTO for UI camera state
    /// </summary>
    [System.Serializable]
    public class UICameraStateDTO
    {
        [Header("Camera Settings")]
        public Vector3 Position;
        public Quaternion Rotation;
        public float FieldOfView = 60f;
        public float ZoomLevel = 1.0f;
        
        [Header("Camera Behavior")]
        public string CameraMode = "Free"; // "Free", "Follow", "Fixed", "Orbital"
        public string MovementType = "Smooth"; // "Smooth", "Instant", "Eased"
        public float MovementSpeed = 5.0f;
        public float RotationSpeed = 90.0f;
        
        [Header("View Settings")]
        public bool OrthographicView = false;
        public float OrthographicSize = 10f;
        public Vector2 ClampBounds;
        public bool EnableBoundsClamping = false;
        
        [Header("Camera Presets")]
        public List<UICameraPresetDTO> SavedPresets = new List<UICameraPresetDTO>();
        public string ActivePreset;
        
        [Header("User Controls")]
        public bool EnableUserControl = true;
        public bool EnableMouseOrbit = true;
        public bool EnableKeyboardMovement = true;
        public bool EnableZoomControl = true;
    }
    
    /// <summary>
    /// DTO for UI view state
    /// </summary>
    [System.Serializable]
    public class UIViewStateDTO
    {
        [Header("Current View")]
        public string CurrentView = "MainView"; // "MainView", "DesignView", "ManagementView"
        public string PreviousView;
        public DateTime ViewChangedTime;
        
        [Header("View Configuration")]
        public Dictionary<string, UIViewConfigDTO> ViewConfigurations = new Dictionary<string, UIViewConfigDTO>();
        
        [Header("View History")]
        public List<UIViewTransitionDTO> ViewHistory = new List<UIViewTransitionDTO>();
        
        [Header("View Settings")]
        public bool EnableViewTransitions = true;
        public float TransitionDuration = 0.3f;
        public string TransitionStyle = "Fade"; // "Fade", "Slide", "Zoom", "None"
        
        [Header("Multi-View Support")]
        public bool EnableSplitView = false;
        public List<string> ActiveViews = new List<string>();
        public string PrimaryView;
    }
    
    /// <summary>
    /// DTO for UI navigation state
    /// </summary>
    [System.Serializable]
    public class UINavigationStateDTO
    {
        [Header("Navigation Stack")]
        public List<UINavigationItemDTO> NavigationStack = new List<UINavigationItemDTO>();
        public int CurrentNavigationIndex = 0;
        
        [Header("Breadcrumbs")]
        public List<UIBreadcrumbDTO> Breadcrumbs = new List<UIBreadcrumbDTO>();
        public bool EnableBreadcrumbs = true;
        
        [Header("Navigation History")]
        public List<UINavigationHistoryDTO> NavigationHistory = new List<UINavigationHistoryDTO>();
        public int MaxHistoryItems = 50;
        
        [Header("Quick Navigation")]
        public List<UIQuickAccessDTO> QuickAccessItems = new List<UIQuickAccessDTO>();
        public bool EnableQuickAccess = true;
        
        [Header("Navigation Behavior")]
        public bool EnableBackForwardNavigation = true;
        public bool RememberNavigationState = true;
        public bool EnableKeyboardNavigation = true;
    }
    
    /// <summary>
    /// DTO for UI menu state
    /// </summary>
    [System.Serializable]
    public class UIMenuStateDTO
    {
        [Header("Menu Configuration")]
        public List<UIMenuItemDTO> MenuItems = new List<UIMenuItemDTO>();
        public Dictionary<string, bool> MenuVisibility = new Dictionary<string, bool>();
        
        [Header("Context Menus")]
        public List<UIContextMenuDTO> ContextMenus = new List<UIContextMenuDTO>();
        public bool EnableContextMenus = true;
        
        [Header("Menu Behavior")]
        public bool EnableMenuAnimations = true;
        public float MenuAnimationSpeed = 1.0f;
        public bool EnableMenuSounds = true;
        public bool AutoCloseMenus = true;
        
        [Header("Custom Menus")]
        public List<UICustomMenuDTO> CustomMenus = new List<UICustomMenuDTO>();
        public bool AllowMenuCustomization = true;
        
        [Header("Recent Items")]
        public List<UIRecentItemDTO> RecentMenuItems = new List<UIRecentItemDTO>();
        public int MaxRecentItems = 10;
    }
    
    /// <summary>
    /// DTO for HUD state
    /// </summary>
    [System.Serializable]
    public class UIHUDStateDTO
    {
        [Header("HUD Elements")]
        public List<UIHUDElementDTO> HUDElements = new List<UIHUDElementDTO>();
        
        [Header("HUD Configuration")]
        public bool ShowHUD = true;
        public float HUDOpacity = 0.8f;
        public string HUDLayout = "Default";
        
        [Header("Information Display")]
        public bool ShowResourceCounters = true;
        public bool ShowStatusIndicators = true;
        public bool ShowPerformanceMetrics = false;
        public bool ShowDebugInfo = false;
        
        [Header("HUD Customization")]
        public List<UIHUDCustomizationDTO> HUDCustomizations = new List<UIHUDCustomizationDTO>();
        public bool AllowHUDCustomization = true;
        
        [Header("Auto-Hide Settings")]
        public bool EnableAutoHide = false;
        public float AutoHideDelay = 3.0f;
        public List<string> AlwaysVisibleElements = new List<string>();
    }
    
    /// <summary>
    /// DTO for UI overlay states
    /// </summary>
    [System.Serializable]
    public class UIOverlayStateDTO
    {
        [Header("Overlay Identity")]
        public string OverlayId;
        public string OverlayName;
        public string OverlayType; // "Modal", "NonModal", "Tooltip", "Notification"
        
        [Header("Overlay State")]
        public bool IsVisible = false;
        public bool IsModal = false;
        public float ZOrder = 0f;
        
        [Header("Position and Animation")]
        public Vector2 Position;
        public Vector2 Size;
        public string AnimationType = "Fade"; // "Fade", "Slide", "Scale", "None"
        public float AnimationDuration = 0.3f;
        
        [Header("Content")]
        public Dictionary<string, object> OverlayData = new Dictionary<string, object>();
        public DateTime ShowTime;
        public float AutoHideDelay = 0f; // 0 = no auto-hide
        
        [Header("User Interaction")]
        public bool AllowUserDismiss = true;
        public bool DismissOnBackgroundClick = true;
        public bool ShowCloseButton = true;
    }
    
    /// <summary>
    /// DTO for UI interaction state
    /// </summary>
    [System.Serializable]
    public class UIInteractionStateDTO
    {
        [Header("Current Interactions")]
        public List<UIActiveInteractionDTO> ActiveInteractions = new List<UIActiveInteractionDTO>();
        
        [Header("Input State")]
        public bool MouseOverUI = false;
        public Vector2 LastMousePosition;
        public bool KeyboardFocusOnUI = false;
        public string FocusedElement;
        
        [Header("Drag and Drop")]
        public bool IsDragging = false;
        public string DraggedElement;
        public Vector2 DragStartPosition;
        public Dictionary<string, object> DragData = new Dictionary<string, object>();
        
        [Header("Selection State")]
        public List<string> SelectedElements = new List<string>();
        public string PrimarySelection;
        public bool MultiSelectEnabled = true;
        
        [Header("Hover State")]
        public string HoveredElement;
        public DateTime HoverStartTime;
        public bool ShowHoverEffects = true;
    }
    
    /// <summary>
    /// DTO for UI control state
    /// </summary>
    [System.Serializable]
    public class UIControlStateDTO
    {
        [Header("Control Values")]
        public Dictionary<string, object> ControlValues = new Dictionary<string, object>();
        
        [Header("Form State")]
        public Dictionary<string, UIFormStateDTO> FormStates = new Dictionary<string, UIFormStateDTO>();
        
        [Header("Validation State")]
        public Dictionary<string, UIValidationStateDTO> ValidationStates = new Dictionary<string, UIValidationStateDTO>();
        
        [Header("Control Behavior")]
        public bool EnableRealTimeValidation = true;
        public bool EnableAutoSave = false;
        public float AutoSaveInterval = 30f; // seconds
        
        [Header("Input History")]
        public Dictionary<string, List<object>> InputHistory = new Dictionary<string, List<object>>();
        public int MaxHistoryItems = 20;
    }
    
    /// <summary>
    /// DTO for UI theme state
    /// </summary>
    [System.Serializable]
    public class UIThemeStateDTO
    {
        [Header("Current Theme")]
        public string CurrentTheme = "Default";
        public string ThemeVariant = "Standard"; // "Standard", "Dark", "Light", "HighContrast"
        
        [Header("Theme Customization")]
        public Dictionary<string, Color> CustomColors = new Dictionary<string, Color>();
        public Dictionary<string, float> CustomValues = new Dictionary<string, float>();
        public Dictionary<string, string> CustomTextures = new Dictionary<string, string>();
        
        [Header("Theme Settings")]
        public bool EnableThemeTransitions = true;
        public float ThemeTransitionDuration = 0.5f;
        public bool ApplyThemeToAllElements = true;
        
        [Header("Available Themes")]
        public List<string> AvailableThemes = new List<string>();
        public bool AllowThemeCustomization = true;
    }
    
    /// <summary>
    /// DTO for UI animation state
    /// </summary>
    [System.Serializable]
    public class UIAnimationStateDTO
    {
        [Header("Animation Settings")]
        public bool EnableAnimations = true;
        public float GlobalAnimationSpeed = 1.0f;
        public string AnimationQuality = "High"; // "Low", "Medium", "High"
        
        [Header("Active Animations")]
        public List<UIActiveAnimationDTO> ActiveAnimations = new List<UIActiveAnimationDTO>();
        
        [Header("Animation Preferences")]
        public bool EnableTransitionAnimations = true;
        public bool EnableHoverAnimations = true;
        public bool EnableLoadingAnimations = true;
        public bool EnableParticleEffects = true;
        
        [Header("Performance")]
        public int MaxConcurrentAnimations = 10;
        public bool EnableAnimationBatching = true;
        public bool PauseAnimationsWhenInactive = true;
    }
    
    /// <summary>
    /// DTO for UI localization state
    /// </summary>
    [System.Serializable]
    public class UILocalizationStateDTO
    {
        [Header("Language Settings")]
        public string CurrentLanguage = "en-US";
        public string FallbackLanguage = "en-US";
        public List<string> AvailableLanguages = new List<string>();
        
        [Header("Regional Settings")]
        public string CurrentRegion = "US";
        public string DateFormat = "MM/dd/yyyy";
        public string TimeFormat = "12-hour";
        public string NumberFormat = "US";
        public string CurrencyFormat = "USD";
        
        [Header("Text Settings")]
        public string TextDirection = "LeftToRight"; // "LeftToRight", "RightToLeft"
        public bool EnableRightToLeftSupport = false;
        public float TextScale = 1.0f;
        
        [Header("Localization Cache")]
        public Dictionary<string, string> CachedTranslations = new Dictionary<string, string>();
        public DateTime LastLocalizationUpdate;
    }

    // Supporting DTOs for complex nested structures

    [System.Serializable]
    public class UIModeConfigDTO
    {
        public string ModeName;
        public List<string> EnabledPanels = new List<string>();
        public List<string> HiddenPanels = new List<string>();
        public Dictionary<string, object> ModeSettings = new Dictionary<string, object>();
        public bool IsDefault = false;
    }

    [System.Serializable]
    public class UIModeTransitionDTO
    {
        public string FromMode;
        public string ToMode;
        public DateTime TransitionTime;
        public string Reason;
        public float TransitionDuration;
    }

    [System.Serializable]
    public class UILevelConfigDTO
    {
        public string LevelName;
        public List<string> AvailableFeatures = new List<string>();
        public List<string> SimplifiedFeatures = new List<string>();
        public Dictionary<string, object> LevelSettings = new Dictionary<string, object>();
        public string Description;
    }

    [System.Serializable]
    public class UILayoutConfigDTO
    {
        public string LayoutName;
        public List<UIPanelLayoutDTO> PanelLayouts = new List<UIPanelLayoutDTO>();
        public Dictionary<string, object> LayoutSettings = new Dictionary<string, object>();
        public bool IsDefault = false;
        public DateTime CreatedDate;
    }

    [System.Serializable]
    public class UIPanelLayoutDTO
    {
        public string PanelId;
        public Vector2 Position;
        public Vector2 Size;
        public bool IsVisible;
        public string DockState;
        public int ZOrder;
    }

    [System.Serializable]
    public class UIMonitorConfigDTO
    {
        public int MonitorIndex;
        public Vector2 Resolution;
        public Vector2 Position;
        public bool IsPrimary;
        public string MonitorName;
        public List<string> AssignedWindows = new List<string>();
    }

    [System.Serializable]
    public class UICameraPresetDTO
    {
        public string PresetName;
        public Vector3 Position;
        public Quaternion Rotation;
        public float FieldOfView;
        public bool IsOrthographic;
        public string Description;
    }

    [System.Serializable]
    public class UIViewConfigDTO
    {
        public string ViewName;
        public List<string> EnabledPanels = new List<string>();
        public string CameraPreset;
        public Dictionary<string, object> ViewSettings = new Dictionary<string, object>();
        public bool AllowCustomization = true;
    }

    [System.Serializable]
    public class UIViewTransitionDTO
    {
        public string FromView;
        public string ToView;
        public DateTime TransitionTime;
        public float TransitionDuration;
        public string TransitionType;
    }

    [System.Serializable]
    public class UINavigationItemDTO
    {
        public string ItemId;
        public string ItemName;
        public string ItemType;
        public string TargetView;
        public Dictionary<string, object> NavigationData = new Dictionary<string, object>();
        public DateTime AccessTime;
    }

    [System.Serializable]
    public class UIBreadcrumbDTO
    {
        public string ItemName;
        public string ItemId;
        public string TargetView;
        public bool IsClickable = true;
        public int Level;
    }

    [System.Serializable]
    public class UINavigationHistoryDTO
    {
        public string ViewName;
        public DateTime AccessTime;
        public Dictionary<string, object> ViewState = new Dictionary<string, object>();
        public float TimeSpent; // seconds
    }

    [System.Serializable]
    public class UIQuickAccessDTO
    {
        public string ItemId;
        public string ItemName;
        public string IconName;
        public string TargetView;
        public int Priority;
        public int UsageCount;
        public DateTime LastUsed;
    }

    [System.Serializable]
    public class UIMenuItemDTO
    {
        public string MenuId;
        public string MenuName;
        public string IconName;
        public bool IsVisible = true;
        public bool IsEnabled = true;
        public List<string> SubMenuItems = new List<string>();
        public string Action;
        public string KeyboardShortcut;
    }

    [System.Serializable]
    public class UIContextMenuDTO
    {
        public string ContextId;
        public string TargetElement;
        public List<UIMenuItemDTO> MenuItems = new List<UIMenuItemDTO>();
        public bool IsEnabled = true;
    }

    [System.Serializable]
    public class UICustomMenuDTO
    {
        public string MenuId;
        public string MenuName;
        public List<UIMenuItemDTO> CustomItems = new List<UIMenuItemDTO>();
        public string MenuPosition;
        public bool IsUserCreated = true;
        public DateTime CreatedDate;
    }

    [System.Serializable]
    public class UIRecentItemDTO
    {
        public string ItemId;
        public string ItemName;
        public string ItemType;
        public DateTime LastAccessed;
        public int AccessCount;
        public string IconName;
    }

    [System.Serializable]
    public class UIHUDElementDTO
    {
        public string ElementId;
        public string ElementName;
        public string ElementType;
        public Vector2 Position;
        public Vector2 Size;
        public bool IsVisible = true;
        public float Opacity = 1.0f;
        public Dictionary<string, object> ElementData = new Dictionary<string, object>();
    }

    [System.Serializable]
    public class UIHUDCustomizationDTO
    {
        public string ElementId;
        public Vector2 CustomPosition;
        public Vector2 CustomSize;
        public bool IsCustomized = false;
        public Dictionary<string, object> CustomProperties = new Dictionary<string, object>();
    }

    [System.Serializable]
    public class UIActiveInteractionDTO
    {
        public string InteractionId;
        public string InteractionType;
        public string TargetElement;
        public DateTime StartTime;
        public Dictionary<string, object> InteractionData = new Dictionary<string, object>();
    }

    [System.Serializable]
    public class UIFormStateDTO
    {
        public string FormId;
        public Dictionary<string, object> FieldValues = new Dictionary<string, object>();
        public bool IsDirty = false;
        public bool IsValid = true;
        public DateTime LastModified;
        public List<string> ModifiedFields = new List<string>();
    }

    [System.Serializable]
    public class UIValidationStateDTO
    {
        public string ControlId;
        public bool IsValid = true;
        public List<string> ErrorMessages = new List<string>();
        public List<string> WarningMessages = new List<string>();
        public DateTime LastValidated;
    }

    [System.Serializable]
    public class UIActiveAnimationDTO
    {
        public string AnimationId;
        public string TargetElement;
        public string AnimationType;
        public float Duration;
        public float ElapsedTime;
        public bool IsLooping = false;
        public Dictionary<string, object> AnimationData = new Dictionary<string, object>();
    }

    /// <summary>
    /// Result DTO for UI save operations
    /// </summary>
    [System.Serializable]
    public class UISaveResult
    {
        public bool Success;
        public DateTime SaveTime;
        public string ErrorMessage;
        public long DataSizeBytes;
        public TimeSpan SaveDuration;
        public int PanelsSaved;
        public int PreferencesSaved;
        public int LayoutsSaved;
        public string SaveVersion;
    }

    /// <summary>
    /// Result DTO for UI load operations
    /// </summary>
    [System.Serializable]
    public class UILoadResult
    {
        public bool Success;
        public DateTime LoadTime;
        public string ErrorMessage;
        public TimeSpan LoadDuration;
        public int PanelsLoaded;
        public int PreferencesLoaded;
        public int LayoutsLoaded;
        public bool RequiredMigration;
        public string LoadedVersion;
        public UIStateDTO UIState;
    }

    /// <summary>
    /// DTO for UI system validation
    /// </summary>
    [System.Serializable]
    public class UIValidationResult
    {
        public bool IsValid;
        public DateTime ValidationTime;
        public List<string> Errors = new List<string>();
        public List<string> Warnings = new List<string>();
        
        [Header("UI State Validation")]
        public bool UIModeValid;
        public bool UILevelValid;
        public bool PanelStatesValid;
        
        [Header("User Preferences Validation")]
        public bool PreferencesValid;
        public bool ThemeValid;
        public bool LocalizationValid;
        
        [Header("Layout Validation")]
        public bool WindowLayoutValid;
        public bool PanelLayoutValid;
        public bool NavigationValid;
        
        [Header("Data Integrity")]
        public int TotalPanels;
        public int ValidPanels;
        public int TotalLayouts;
        public int ValidLayouts;
        public float DataIntegrityScore;
    }
    
    /// <summary>
    /// DTO for UI window management and organization
    /// </summary>
    [System.Serializable]
    public class UIWindowManagementDTO
    {
        [Header("Active Windows")]
        public List<string> ActiveWindows = new List<string>();
        public string FocusedWindow;
        public DateTime LastWindowFocus;
        
        [Header("Window Stack")]
        public List<string> WindowStack = new List<string>();
        public int MaxOpenWindows = 10;
        public bool AllowOverlapping = true;
        
        [Header("Window Layout")]
        public string WindowLayoutName;
        public Dictionary<string, Vector2> WindowPositions = new Dictionary<string, Vector2>();
        public Dictionary<string, Vector2> WindowSizes = new Dictionary<string, Vector2>();
        public Dictionary<string, int> WindowZOrders = new Dictionary<string, int>();
        
        [Header("Window Behavior")]
        public bool AutoArrangeWindows = false;
        public bool SnapToGrid = true;
        public bool RememberWindowStates = true;
        public float GridSize = 10f;
    }
}