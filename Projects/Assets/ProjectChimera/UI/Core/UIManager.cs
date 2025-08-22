using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Collections;
using ProjectChimera.Core;
using ProjectChimera.Core.Events;
// using ProjectChimera.Data; // trimmed to avoid cross-assembly coupling
using ProjectChimera.UI.Core;

namespace ProjectChimera.UI.Core
{
    /// <summary>
    /// Central UI Manager for Project Chimera.
    /// Manages UI state, navigation, panels, and overall UI coordination.
    /// </summary>
    public class UIManager : DIChimeraManager
    {
        [Header("UI Configuration")]
        [SerializeField] private UIDesignSystemSO _designSystem;
        [SerializeField] private UIDocument _mainUIDocument;
        [SerializeField] private bool _enableUITransitions = true;
        [SerializeField] private float _defaultTransitionDuration = 0.25f;
        
        [Header("UI Events")]
        [SerializeField] private SimpleGameEventSO _onUIStateChanged;
        [SerializeField] private SimpleGameEventSO _onPanelOpened;
        [SerializeField] private SimpleGameEventSO _onPanelClosed;
        
        [Header("UI Panels")]
        [SerializeField] private List<UIPanel> _registeredPanels = new List<UIPanel>();
        
        [Header("Notification System")]
        [SerializeField] private NotificationManager _notificationManager;
        [SerializeField] private bool _enableGlobalNotifications = true;
        
        // UI State Management
        private Dictionary<string, UIPanel> _panelRegistry;
        private Stack<string> _panelHistory;
        private string _currentPanelId;
        private UIState _currentUIState;
        private bool _isTransitioning;
        
        // UI Document References
        private VisualElement _rootElement;
        private VisualElement _mainContainer;
        private VisualElement _overlayContainer;
        private VisualElement _modalContainer;
        
        // Properties
        public UIDesignSystemSO DesignSystem => _designSystem;
        public bool IsTransitioning => _isTransitioning;
        public UIState CurrentUIState => _currentUIState;
        public string CurrentPanelId => _currentPanelId;
        
        protected override void OnManagerInitialize()
        {
            InitializeUISystem();
            InitializeNotificationSystem();
            InitializePanelRegistry();
            SetupUIDocument();
            
            // Set initial UI state
            SetUIState(UIState.MainMenu);
            
            // LogInfo("UI Manager initialized successfully");
        }
        
        /// <summary>
        /// Initialize core UI system
        /// </summary>
        private void InitializeUISystem()
        {
            _panelRegistry = new Dictionary<string, UIPanel>();
            _panelHistory = new Stack<string>();
            _currentUIState = UIState.Loading;
            _isTransitioning = false;
            
            // Validate design system
            if (_designSystem == null)
            {
                LogError("UI Design System is not assigned! UI appearance will be inconsistent.");
            }
            // else if (!_designSystem.ValidateData())
            // {
                LogWarning("UI Design System validation failed. Some visual elements may not display correctly.");
            // }
        }
        
        /// <summary>
        /// Initialize enhanced notification system
        /// </summary>
        private void InitializeNotificationSystem()
        {
            if (!_enableGlobalNotifications)
                return;
                
            // Find or create NotificationManager
            if (_notificationManager == null)
            {
                _notificationManager = FindObjectOfType<NotificationManager>();
            }
            
            if (_notificationManager == null)
            {
                // Create NotificationManager GameObject
                var notificationGO = new GameObject("NotificationManager");
                _notificationManager = notificationGO.AddComponent<NotificationManager>();
                DontDestroyOnLoad(notificationGO);
                
                LogInfo("NotificationManager created automatically by UIManager");
            }
            
            // Subscribe to notification events for UI state management
            if (_notificationManager != null)
            {
                _notificationManager.OnNotificationShown += HandleNotificationShown;
                _notificationManager.OnNotificationDismissed += HandleNotificationDismissed;
                _notificationManager.OnQueueSizeChanged += HandleNotificationQueueChanged;
                
                LogInfo("Enhanced notification system initialized successfully");
            }
        }
        
        /// <summary>
        /// Initialize panel registry
        /// </summary>
        private void InitializePanelRegistry()
        {
            foreach (var panel in _registeredPanels)
            {
                if (panel != null)
                {
                    RegisterPanel(panel);
                }
            }
            
            // Find and register panels that aren't manually registered
            var foundPanels = FindObjectsByType<UIPanel>(FindObjectsSortMode.None);
            foreach (var panel in foundPanels)
            {
                if (!_panelRegistry.ContainsKey(panel.PanelId))
                {
                    RegisterPanel(panel);
                }
            }
            
            LogInfo($"Registered {_panelRegistry.Count} UI panels");
        }
        
        /// <summary>
        /// Setup UI Document and root elements
        /// </summary>
        private void SetupUIDocument()
        {
            if (_mainUIDocument == null)
            {
                LogError("Main UI Document is not assigned!");
                return;
            }
            
            _rootElement = _mainUIDocument.rootVisualElement;
            
            // Create main UI structure
            CreateUIStructure();
            
            // Apply design system styles
            ApplyDesignSystemStyles();
        }
        
        /// <summary>
        /// Create the main UI structure
        /// </summary>
        private void CreateUIStructure()
        {
            _rootElement.Clear();
            
            // Main container for standard UI
            _mainContainer = new VisualElement();
            _mainContainer.name = "main-container";
            _mainContainer.style.flexGrow = 1;
            _mainContainer.style.position = Position.Relative;
            _rootElement.Add(_mainContainer);
            
            // Overlay container for HUD elements
            _overlayContainer = new VisualElement();
            _overlayContainer.name = "overlay-container";
            _overlayContainer.style.position = Position.Absolute;
            _overlayContainer.style.top = 0;
            _overlayContainer.style.left = 0;
            _overlayContainer.style.right = 0;
            _overlayContainer.style.bottom = 0;
            // _overlayContainer.style.pointerEvents = PointerEvents.None; // PointerEvents not available in Unity UI Toolkit
            _rootElement.Add(_overlayContainer);
            
            // Modal container for modal dialogs
            _modalContainer = new VisualElement();
            _modalContainer.name = "modal-container";
            _modalContainer.style.position = Position.Absolute;
            _modalContainer.style.top = 0;
            _modalContainer.style.left = 0;
            _modalContainer.style.right = 0;
            _modalContainer.style.bottom = 0;
            _modalContainer.style.display = DisplayStyle.None;
            _rootElement.Add(_modalContainer);
        }
        
        /// <summary>
        /// Apply design system styles to root elements
        /// </summary>
        private void ApplyDesignSystemStyles()
        {
            if (_designSystem == null) return;
            
            var colors = _designSystem.ColorPalette;
            
            // Apply background color
            _rootElement.style.backgroundColor = colors.BackgroundDark;
            
            // Apply container styles
            _mainContainer.style.backgroundColor = Color.clear;
            _overlayContainer.style.backgroundColor = Color.clear;
            _modalContainer.style.backgroundColor = new Color(0f, 0f, 0f, 0.5f); // Semi-transparent overlay
        }
        
        /// <summary>
        /// Register a UI panel
        /// </summary>
        public void RegisterPanel(UIPanel panel)
        {
            if (panel == null)
            {
                LogWarning("Attempted to register null UI panel");
                return;
            }
            
            if (_panelRegistry.ContainsKey(panel.PanelId))
            {
                LogWarning($"Panel with ID '{panel.PanelId}' is already registered. Replacing existing panel.");
            }
            
            _panelRegistry[panel.PanelId] = panel;
            // UIPanel doesn't have Initialize method that takes UIManager parameter
            // Initialize is called during panel construction
            
            LogInfo($"Registered UI panel: {panel.PanelId}");
        }
        
        /// <summary>
        /// Unregister a UI panel
        /// </summary>
        public void UnregisterPanel(string panelId)
        {
            if (_panelRegistry.ContainsKey(panelId))
            {
                _panelRegistry.Remove(panelId);
                LogInfo($"Unregistered UI panel: {panelId}");
            }
        }
        
        /// <summary>
        /// Show a UI panel
        /// </summary>
        public void ShowPanel(string panelId, bool addToHistory = true)
        {
            if (_isTransitioning)
            {
                LogWarning($"Cannot show panel '{panelId}' - UI is currently transitioning");
                return;
            }
            
            if (!_panelRegistry.TryGetValue(panelId, out var panel))
            {
                LogError($"Panel '{panelId}' is not registered");
                return;
            }
            
            StartCoroutine(ShowPanelCoroutine(panel, addToHistory));
        }
        
        /// <summary>
        /// Show panel coroutine with transitions
        /// </summary>
        private IEnumerator ShowPanelCoroutine(UIPanel panel, bool addToHistory)
        {
            _isTransitioning = true;
            
            // Hide current panel if any
            if (!string.IsNullOrEmpty(_currentPanelId) && _panelRegistry.TryGetValue(_currentPanelId, out var currentPanel))
            {
                yield return StartCoroutine(currentPanel.HideCoroutine());
            }
            
            // Add to history if requested
            if (addToHistory && !string.IsNullOrEmpty(_currentPanelId))
            {
                _panelHistory.Push(_currentPanelId);
            }
            
            // Show new panel
            _currentPanelId = panel.PanelId;
            yield return StartCoroutine(panel.ShowCoroutine());
            
            _isTransitioning = false;
            
            // Raise events
            _onPanelOpened?.Raise();
            
            LogInfo($"Showed UI panel: {panel.PanelId}");
        }
        
        /// <summary>
        /// Hide current panel
        /// </summary>
        public void HideCurrentPanel()
        {
            if (string.IsNullOrEmpty(_currentPanelId)) return;
            
            HidePanel(_currentPanelId);
        }
        
        /// <summary>
        /// Hide a specific panel
        /// </summary>
        public void HidePanel(string panelId)
        {
            if (_isTransitioning)
            {
                LogWarning($"Cannot hide panel '{panelId}' - UI is currently transitioning");
                return;
            }
            
            if (!_panelRegistry.TryGetValue(panelId, out var panel))
            {
                LogError($"Panel '{panelId}' is not registered");
                return;
            }
            
            StartCoroutine(HidePanelCoroutine(panel));
        }
        
        /// <summary>
        /// Hide panel coroutine with transitions
        /// </summary>
        private IEnumerator HidePanelCoroutine(UIPanel panel)
        {
            _isTransitioning = true;
            
            yield return StartCoroutine(panel.HideCoroutine());
            
            if (_currentPanelId == panel.PanelId)
            {
                _currentPanelId = "";
            }
            
            _isTransitioning = false;
            
            // Raise events
            _onPanelClosed?.Raise();
            
            LogInfo($"Hid UI panel: {panel.PanelId}");
        }
        
        /// <summary>
        /// Go back to previous panel in history
        /// </summary>
        public void GoBack()
        {
            if (_panelHistory.Count > 0)
            {
                var previousPanelId = _panelHistory.Pop();
                ShowPanel(previousPanelId, false);
            }
            // else
            // {
                LogInfo("No previous panel in history");
            // }
        }
        
        /// <summary>
        /// Clear panel history
        /// </summary>
        public void ClearHistory()
        {
            _panelHistory.Clear();
        }
        
        /// <summary>
        /// Set UI state
        /// </summary>
        public void SetUIState(UIState newState)
        {
            if (_currentUIState == newState) return;
            
            var previousState = _currentUIState;
            _currentUIState = newState;
            
            // Handle state-specific logic
            HandleUIStateChange(previousState, newState);
            
            // Raise state change event
            _onUIStateChanged?.Raise();
            
            LogInfo($"UI State changed from {previousState} to {newState}");
        }
        
        /// <summary>
        /// Handle UI state changes
        /// </summary>
        private void HandleUIStateChange(UIState previousState, UIState newState)
        {
            switch (newState)
            {
                case UIState.MainMenu:
                    ShowPanel("main-menu");
                    break;
                    
                case UIState.Gameplay:
                    ShowPanel("gameplay-hud");
                    break;
                    
                case UIState.Paused:
                    ShowPanel("pause-menu");
                    break;
                    
                case UIState.Settings:
                    ShowPanel("settings-menu");
                    break;
                    
                case UIState.Loading:
                    ShowPanel("loading-screen");
                    break;
            }
        }
        
        /// <summary>
        /// Get panel by ID
        /// </summary>
        public UIPanel GetPanel(string panelId)
        {
            _panelRegistry.TryGetValue(panelId, out var panel);
            return panel;
        }
        
        /// <summary>
        /// Check if panel is registered
        /// </summary>
        public bool IsPanelRegistered(string panelId)
        {
            return _panelRegistry.ContainsKey(panelId);
        }
        
        /// <summary>
        /// Check if panel is currently visible
        /// </summary>
        public bool IsPanelVisible(string panelId)
        {
            if (_panelRegistry.TryGetValue(panelId, out var panel))
            {
                return panel.IsVisible;
            }
            return false;
        }
        
        /// <summary>
        /// Get main container for standard UI elements
        /// </summary>
        public VisualElement GetMainContainer()
        {
            return _mainContainer;
        }
        
        /// <summary>
        /// Get overlay container for HUD elements
        /// </summary>
        public VisualElement GetOverlayContainer()
        {
            return _overlayContainer;
        }
        
        /// <summary>
        /// Get modal container for modal dialogs
        /// </summary>
        public VisualElement GetModalContainer()
        {
            return _modalContainer;
        }
        
        /// <summary>
        /// Show modal dialog
        /// </summary>
        public void ShowModal(VisualElement modalContent)
        {
            _modalContainer.Clear();
            _modalContainer.Add(modalContent);
            _modalContainer.style.display = DisplayStyle.Flex;
        }
        
        /// <summary>
        /// Hide modal dialog
        /// </summary>
        public void HideModal()
        {
            _modalContainer.style.display = DisplayStyle.None;
            _modalContainer.Clear();
        }
        
        /// <summary>
        /// Apply design system styles to an element
        /// </summary>
        public void ApplyDesignSystemStyle(VisualElement element, UIStyleToken styleToken)
        {
            if (_designSystem == null || element == null) return;
            
            var colors = _designSystem.ColorPalette;
            var typography = _designSystem.Typography;
            var spacing = _designSystem.Spacing;
            
            switch (styleToken)
            {
                case UIStyleToken.PrimaryButton:
                    element.style.backgroundColor = colors.PrimaryGreen;
                    element.style.color = colors.TextOnPrimary;
                    element.style.borderTopLeftRadius = _designSystem.ComponentStyles.BorderRadiusMedium;
                    element.style.borderTopRightRadius = _designSystem.ComponentStyles.BorderRadiusMedium;
                    element.style.borderBottomLeftRadius = _designSystem.ComponentStyles.BorderRadiusMedium;
                    element.style.borderBottomRightRadius = _designSystem.ComponentStyles.BorderRadiusMedium;
                    element.style.paddingTop = spacing.GetSpacing(UISpacingScale.Small);
                    element.style.paddingBottom = spacing.GetSpacing(UISpacingScale.Small);
                    element.style.paddingLeft = spacing.GetSpacing(UISpacingScale.Medium);
                    element.style.paddingRight = spacing.GetSpacing(UISpacingScale.Medium);
                    break;
                    
                case UIStyleToken.SecondaryButton:
                    element.style.backgroundColor = Color.clear;
                    element.style.color = colors.PrimaryGreen;
                    element.style.borderTopColor = colors.PrimaryGreen;
                    element.style.borderRightColor = colors.PrimaryGreen;
                    element.style.borderBottomColor = colors.PrimaryGreen;
                    element.style.borderLeftColor = colors.PrimaryGreen;
                    element.style.borderTopWidth = _designSystem.ComponentStyles.BorderWidthThin;
                    element.style.borderRightWidth = _designSystem.ComponentStyles.BorderWidthThin;
                    element.style.borderBottomWidth = _designSystem.ComponentStyles.BorderWidthThin;
                    element.style.borderLeftWidth = _designSystem.ComponentStyles.BorderWidthThin;
                    break;
                    
                case UIStyleToken.Panel:
                    element.style.backgroundColor = colors.SurfaceDark;
                    element.style.borderTopLeftRadius = _designSystem.ComponentStyles.BorderRadiusLarge;
                    element.style.borderTopRightRadius = _designSystem.ComponentStyles.BorderRadiusLarge;
                    element.style.borderBottomLeftRadius = _designSystem.ComponentStyles.BorderRadiusLarge;
                    element.style.borderBottomRightRadius = _designSystem.ComponentStyles.BorderRadiusLarge;
                    element.style.paddingTop = spacing.GetSpacing(UISpacingScale.Large);
                    element.style.paddingBottom = spacing.GetSpacing(UISpacingScale.Large);
                    element.style.paddingLeft = spacing.GetSpacing(UISpacingScale.Large);
                    element.style.paddingRight = spacing.GetSpacing(UISpacingScale.Large);
                    break;
                    
                case UIStyleToken.HeaderText:
                    element.style.color = colors.TextPrimary;
                    element.style.fontSize = typography.GetFontSize(UIFontScale.HeadlineLarge);
                    break;
                    
                case UIStyleToken.BodyText:
                    element.style.color = colors.TextSecondary;
                    element.style.fontSize = typography.GetFontSize(UIFontScale.BodyMedium);
                    break;
            }
        }
        
        /// <summary>
        /// Close all open panels
        /// </summary>
        public void CloseAllPanels()
        {
            foreach (var panel in _panelRegistry.Values)
            {
                if (panel.IsVisible)
                {
                    panel.Hide();
                }
            }
            
            _currentPanelId = string.Empty;
            _panelHistory.Clear();
            
            LogInfo("All panels closed");
        }
        
        protected override void OnManagerShutdown()
        {
            // Clean up UI system
            CloseAllPanels();
            
            // Unsubscribe from notification events
            if (_notificationManager != null)
            {
                _notificationManager.OnNotificationShown -= HandleNotificationShown;
                _notificationManager.OnNotificationDismissed -= HandleNotificationDismissed;
                _notificationManager.OnQueueSizeChanged -= HandleNotificationQueueChanged;
            }
            
            // Clear event listeners
            if (_onUIStateChanged != null)
                _onUIStateChanged = null;
            if (_onPanelOpened != null)
                _onPanelOpened = null;
            if (_onPanelClosed != null)
                _onPanelClosed = null;
                
            // Clear references
            _registeredPanels.Clear();
            _currentPanelId = string.Empty;
            
            // LogInfo("UI Manager shutdown completed");
        }
        
        #region Notification Event Handlers
        
        /// <summary>
        /// Handle notification shown event
        /// </summary>
        private void HandleNotificationShown(NotificationData notification)
        {
            if (_enableGlobalNotifications)
            {
                LogInfo($"Notification shown: {notification.Message}");
                
                // Could trigger UI state changes based on critical notifications
                if (notification.Severity == NotificationSeverity.Critical)
                {
                    // Handle critical notifications that might need UI attention
                }
            }
        }
        
        /// <summary>
        /// Handle notification dismissed event
        /// </summary>
        private void HandleNotificationDismissed(NotificationData notification)
        {
            if (_enableGlobalNotifications)
            {
                LogInfo($"Notification dismissed: {notification.Message}");
            }
        }
        
        /// <summary>
        /// Handle notification queue size changes
        /// </summary>
        private void HandleNotificationQueueChanged(int queueSize)
        {
            if (_enableGlobalNotifications && queueSize > 10)
            {
                LogWarning($"Notification queue is getting large: {queueSize} notifications pending");
            }
        }
        
        #endregion
        
        #region Public Notification API
        
        /// <summary>
        /// Show global notification through UIManager
        /// </summary>
        public void ShowGlobalNotification(string message, NotificationSeverity severity = NotificationSeverity.Info)
        {
            if (_notificationManager != null && _enableGlobalNotifications)
            {
                _notificationManager.ShowNotification(message, severity);
            }
        }
        
        /// <summary>
        /// Show persistent notification through UIManager
        /// </summary>
        public void ShowPersistentNotification(string key, string message, NotificationSeverity severity = NotificationSeverity.Warning)
        {
            if (_notificationManager != null && _enableGlobalNotifications)
            {
                _notificationManager.ShowPersistentNotification(key, message, severity);
            }
        }
        
        /// <summary>
        /// Dismiss persistent notification through UIManager
        /// </summary>
        public void DismissPersistentNotification(string key)
        {
            if (_notificationManager != null && _enableGlobalNotifications)
            {
                _notificationManager.DismissNotification(key);
            }
        }
        
        #endregion
    }
    
    /// <summary>
    /// UI state enumeration
    /// </summary>
    public enum UIState
    {
        Loading,
        MainMenu,
        Gameplay,
        InGame,
        Game,
        Paused,
        Settings,
        Tutorial
    }
    
    /// <summary>
    /// UI style tokens for consistent styling
    /// </summary>
    public enum UIStyleToken
    {
        PrimaryButton,
        SecondaryButton,
        Panel,
        Card,
        HeaderText,
        BodyText,
        Caption,
        Input,
        Dropdown,
        Slider,
        Toggle
    }

    /// <summary>
    /// Types of notifications that can be displayed
    /// </summary>
    public enum NotificationType
    {
        Info,
        Success,
        Warning,
        Error,
        Achievement,
        Alert,
        Critical
    }
}