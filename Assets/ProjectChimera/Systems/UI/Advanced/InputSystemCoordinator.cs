using ProjectChimera.Core.Logging;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System.Collections.Generic;
using System;

#if UNITY_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ProjectChimera.Systems.UI.Advanced
{
#if UNITY_INPUT_SYSTEM
    /// <summary>
    /// Main coordinator for the advanced input system integration.
    /// Orchestrates all input components and provides unified interface.
    /// </summary>
    [RequireComponent(typeof(PlayerInput))]
    [RequireComponent(typeof(InputNavigationCore))]
    public class InputSystemCoordinator : MonoBehaviour
    {
        [Header("System Configuration")]
        [SerializeField] private bool _enableMouseSupport = true;
        [SerializeField] private bool _enableTouchSupport = false;
        [SerializeField] private bool _autoInitialize = true;
        [SerializeField] private bool _debugMode = false;
        
        [Header("Component References")]
        [SerializeField] private InputNavigationCore _navigationCore;
        [SerializeField] private InputControllerSupport _controllerSupport;
        [SerializeField] private InputKeyboardHandler _keyboardHandler;
        [SerializeField] private InputAccessibilitySupport _accessibilitySupport;
        
        // System references
        private AdvancedMenuSystem _menuSystem;
        private VisualFeedbackIntegration _visualFeedback;
        private PlayerInput _playerInput;
        private UIDocument _uiDocument;
        
        // State management
        private bool _isInitialized = false;
        private Dictionary<string, object> _systemState = new Dictionary<string, object>();
        private List<IInputComponent> _inputComponents = new List<IInputComponent>();
        
        // Events
        public event Action OnSystemInitialized;
        public event Action OnSystemShutdown;
        public event Action<NavigationMode> OnNavigationModeChanged;
        public event Action<string> OnInputEvent;
        
        // Properties
        public bool IsInitialized => _isInitialized;
        public NavigationMode CurrentNavigationMode => _controllerSupport?.CurrentNavigationMode ?? NavigationMode.Mouse;
        public bool EnableMouseSupport { get => _enableMouseSupport; set => _enableMouseSupport = value; }
        public bool EnableTouchSupport { get => _enableTouchSupport; set => _enableTouchSupport = value; }
        public InputNavigationCore NavigationCore => _navigationCore;
        public InputControllerSupport ControllerSupport => _controllerSupport;
        public InputKeyboardHandler KeyboardHandler => _keyboardHandler;
        public InputAccessibilitySupport AccessibilitySupport => _accessibilitySupport;
        
        private void Awake()
        {
            if (_autoInitialize)
            {
                InitializeSystem();
            }
        }
        
        private void Start()
        {
            if (_isInitialized)
            {
                StartSystemOperations();
            }
        }
        
        private void Update()
        {
            if (_isInitialized)
            {
                UpdateInputSystem();
            }
        }
        
        /// <summary>
        /// Initialize the complete input system
        /// </summary>
        public void InitializeSystem()
        {
            if (_isInitialized)
            {
                ChimeraLogger.LogWarning("[InputSystemCoordinator] System already initialized");
                return;
            }
            
            ChimeraLogger.Log("[InputSystemCoordinator] Initializing input system...");
            
            // Get system references
            GatherSystemReferences();
            
            // Initialize components
            InitializeInputComponents();
            
            // Setup event connections
            SetupEventConnections();
            
            // Setup mouse and touch support
            SetupMouseAndTouchSupport();
            
            // Initialize system state
            InitializeSystemState();
            
            _isInitialized = true;
            OnSystemInitialized?.Invoke();
            
            ChimeraLogger.Log("[InputSystemCoordinator] Input system initialized successfully");
        }
        
        /// <summary>
        /// Shutdown the input system
        /// </summary>
        public void ShutdownSystem()
        {
            if (!_isInitialized)
                return;
            
            ChimeraLogger.Log("[InputSystemCoordinator] Shutting down input system...");
            
            // Cleanup components
            CleanupInputComponents();
            
            // Clear event connections
            CleanupEventConnections();
            
            // Clear system state
            _systemState.Clear();
            
            _isInitialized = false;
            OnSystemShutdown?.Invoke();
            
            ChimeraLogger.Log("[InputSystemCoordinator] Input system shutdown complete");
        }
        
        /// <summary>
        /// Refresh all input components and their configurations
        /// </summary>
        public void RefreshSystem()
        {
            if (!_isInitialized)
            {
                InitializeSystem();
                return;
            }
            
            ChimeraLogger.Log("[InputSystemCoordinator] Refreshing input system...");
            
            // Refresh navigation elements
            _navigationCore?.RefreshNavigableElements();
            
            // Update component states
            foreach (var component in _inputComponents)
            {
                component?.Refresh();
            }
            
            // Re-sync system state
            SyncSystemState();
        }
        
        /// <summary>
        /// Set the active navigation mode
        /// </summary>
        public void SetNavigationMode(NavigationMode mode)
        {
            if (_controllerSupport != null)
            {
                _controllerSupport.SwitchNavigationMode(mode);
            }
        }
        
        /// <summary>
        /// Enable or disable debug mode
        /// </summary>
        public void SetDebugMode(bool enabled)
        {
            _debugMode = enabled;
            
            if (_debugMode)
            {
                ChimeraLogger.Log("[InputSystemCoordinator] Debug mode enabled");
                StartCoroutine(DebugUpdateLoop());
            }
        }
        
        /// <summary>
        /// Get current input system status
        /// </summary>
        public InputSystemStatus GetSystemStatus()
        {
            return new InputSystemStatus
            {
                IsInitialized = _isInitialized,
                NavigationMode = CurrentNavigationMode,
                NavigableElementCount = _navigationCore?.NavigableElementCount ?? 0,
                IsControllerConnected = _controllerSupport?.IsControllerConnected ?? false,
                IsInSearchMode = _keyboardHandler?.IsInSearchMode ?? false,
                AccessibilityFeaturesActive = GetActiveAccessibilityFeatures()
            };
        }
        
        /// <summary>
        /// Register a custom input component
        /// </summary>
        public void RegisterInputComponent(IInputComponent component)
        {
            if (component == null || _inputComponents.Contains(component))
                return;
            
            _inputComponents.Add(component);
            component.Initialize();
            
            ChimeraLogger.Log($"[InputSystemCoordinator] Registered input component: {component.GetType().Name}");
        }
        
        /// <summary>
        /// Unregister an input component
        /// </summary>
        public void UnregisterInputComponent(IInputComponent component)
        {
            if (component == null || !_inputComponents.Remove(component))
                return;
            
            component.Cleanup();
            ChimeraLogger.Log($"[InputSystemCoordinator] Unregistered input component: {component.GetType().Name}");
        }
        
        private void GatherSystemReferences()
        {
            // Get required components
            _playerInput = GetComponent<PlayerInput>();
            _uiDocument = GetComponent<UIDocument>();
            _menuSystem = GetComponent<AdvancedMenuSystem>();
            _visualFeedback = GetComponent<VisualFeedbackIntegration>();
            
            // Get input components
            if (_navigationCore == null)
                _navigationCore = GetComponent<InputNavigationCore>();
            if (_controllerSupport == null)
                _controllerSupport = GetComponent<InputControllerSupport>();
            if (_keyboardHandler == null)
                _keyboardHandler = GetComponent<InputKeyboardHandler>();
            if (_accessibilitySupport == null)
                _accessibilitySupport = GetComponent<InputAccessibilitySupport>();
            
            // Validate required components
            if (_playerInput == null)
            {
                ChimeraLogger.LogError("[InputSystemCoordinator] PlayerInput component required");
                return;
            }
            
            if (_navigationCore == null)
            {
                ChimeraLogger.LogError("[InputSystemCoordinator] InputNavigationCore component required");
                return;
            }
        }
        
        private void InitializeInputComponents()
        {
            // Initialize core components
            if (_navigationCore != null)
            {
                _inputComponents.Add(_navigationCore as IInputComponent);
            }
            
            if (_controllerSupport != null)
            {
                _inputComponents.Add(_controllerSupport as IInputComponent);
            }
            
            if (_keyboardHandler != null)
            {
                _inputComponents.Add(_keyboardHandler as IInputComponent);
            }
            
            if (_accessibilitySupport != null)
            {
                _inputComponents.Add(_accessibilitySupport as IInputComponent);
            }
            
            // Initialize all components
            foreach (var component in _inputComponents)
            {
                component?.Initialize();
            }
        }
        
        private void SetupEventConnections()
        {
            // Connect navigation events
            if (_navigationCore != null)
            {
                _navigationCore.OnElementFocused += OnElementFocused;
                _navigationCore.OnElementSelected += OnElementSelected;
            }
            
            // Connect controller events
            if (_controllerSupport != null)
            {
                _controllerSupport.OnNavigationModeChanged += OnNavigationModeChangedInternal;
                _controllerSupport.OnControllerConnected += OnControllerConnected;
                _controllerSupport.OnControllerDisconnected += OnControllerDisconnected;
            }
            
            // Connect keyboard events
            if (_keyboardHandler != null)
            {
                _keyboardHandler.OnShortcutExecuted += OnShortcutExecuted;
                _keyboardHandler.OnSearchModeEntered += OnSearchModeEntered;
                _keyboardHandler.OnSearchModeExited += OnSearchModeExited;
            }
            
            // Connect accessibility events
            if (_accessibilitySupport != null)
            {
                _accessibilitySupport.OnHighContrastToggled += OnHighContrastToggled;
                _accessibilitySupport.OnScreenReaderAnnouncement += OnScreenReaderAnnouncement;
            }
            
            // Connect menu system events
            if (_menuSystem != null)
            {
                _menuSystem.OnMenuOpened += OnMenuOpened;
                _menuSystem.OnMenuClosed += OnMenuClosed;
            }
        }
        
        private void SetupMouseAndTouchSupport()
        {
            if (!_enableMouseSupport && !_enableTouchSupport)
                return;
            
            var root = _uiDocument?.rootVisualElement;
            if (root != null)
            {
                if (_enableMouseSupport)
                {
                    root.RegisterCallback<MouseEnterEvent>(OnMouseEnter);
                    root.RegisterCallback<MouseMoveEvent>(OnMouseMove);
                    root.RegisterCallback<ClickEvent>(OnMouseClick);
                }
                
                if (_enableTouchSupport)
                {
                    root.RegisterCallback<PointerDownEvent>(OnTouchStart);
                    root.RegisterCallback<PointerMoveEvent>(OnTouchMove);
                    root.RegisterCallback<PointerUpEvent>(OnTouchEnd);
                }
            }
        }
        
        private void InitializeSystemState()
        {
            _systemState["NavigationMode"] = NavigationMode.Mouse;
            _systemState["LastInputTime"] = Time.time;
            _systemState["ActiveComponents"] = _inputComponents.Count;
            _systemState["MenuOpen"] = false;
        }
        
        private void StartSystemOperations()
        {
            StartCoroutine(SystemUpdateLoop());
            
            if (_debugMode)
            {
                StartCoroutine(DebugUpdateLoop());
            }
        }
        
        private void UpdateInputSystem()
        {
            // Update system state
            _systemState["LastInputTime"] = Time.time;
            
            // Process any queued input events
            ProcessInputEvents();
        }
        
        private void ProcessInputEvents()
        {
            // This would handle any queued input events or state changes
            // For now, it's a placeholder for future expansion
        }
        
        private void SyncSystemState()
        {
            if (_controllerSupport != null)
            {
                _systemState["NavigationMode"] = _controllerSupport.CurrentNavigationMode;
                _systemState["ControllerConnected"] = _controllerSupport.IsControllerConnected;
            }
            
            if (_keyboardHandler != null)
            {
                _systemState["SearchMode"] = _keyboardHandler.IsInSearchMode;
                _systemState["SearchQuery"] = _keyboardHandler.SearchQuery;
            }
            
            if (_accessibilitySupport != null)
            {
                _systemState["HighContrast"] = _accessibilitySupport.IsHighContrastActive;
                _systemState["SlowMotion"] = _accessibilitySupport.IsSlowMotionActive;
            }
        }
        
        private List<string> GetActiveAccessibilityFeatures()
        {
            var features = new List<string>();
            
            if (_accessibilitySupport != null)
            {
                if (_accessibilitySupport.EnableScreenReader) features.Add("ScreenReader");
                if (_accessibilitySupport.IsHighContrastActive) features.Add("HighContrast");
                if (_accessibilitySupport.IsSlowMotionActive) features.Add("SlowMotion");
            }
            
            return features;
        }
        
        private IEnumerator SystemUpdateLoop()
        {
            while (_isInitialized)
            {
                yield return new WaitForSeconds(0.1f);
                
                // Periodic system maintenance
                SyncSystemState();
                
                // Update components
                foreach (var component in _inputComponents)
                {
                    component?.Update();
                }
            }
        }
        
        private IEnumerator DebugUpdateLoop()
        {
            while (_debugMode && _isInitialized)
            {
                yield return new WaitForSeconds(1f);
                
                var status = GetSystemStatus();
                ChimeraLogger.Log($"[InputSystemCoordinator] Status - Mode: {status.NavigationMode}, Elements: {status.NavigableElementCount}, Controller: {status.IsControllerConnected}");
            }
        }
        
        private void CleanupInputComponents()
        {
            foreach (var component in _inputComponents)
            {
                component?.Cleanup();
            }
            _inputComponents.Clear();
        }
        
        private void CleanupEventConnections()
        {
            // Cleanup navigation events
            if (_navigationCore != null)
            {
                _navigationCore.OnElementFocused -= OnElementFocused;
                _navigationCore.OnElementSelected -= OnElementSelected;
            }
            
            // Cleanup controller events
            if (_controllerSupport != null)
            {
                _controllerSupport.OnNavigationModeChanged -= OnNavigationModeChangedInternal;
                _controllerSupport.OnControllerConnected -= OnControllerConnected;
                _controllerSupport.OnControllerDisconnected -= OnControllerDisconnected;
            }
            
            // Cleanup keyboard events
            if (_keyboardHandler != null)
            {
                _keyboardHandler.OnShortcutExecuted -= OnShortcutExecuted;
                _keyboardHandler.OnSearchModeEntered -= OnSearchModeEntered;
                _keyboardHandler.OnSearchModeExited -= OnSearchModeExited;
            }
            
            // Cleanup accessibility events
            if (_accessibilitySupport != null)
            {
                _accessibilitySupport.OnHighContrastToggled -= OnHighContrastToggled;
                _accessibilitySupport.OnScreenReaderAnnouncement -= OnScreenReaderAnnouncement;
            }
        }
        
        // Event handlers
        private void OnElementFocused(VisualElement element)
        {
            OnInputEvent?.Invoke($"Element focused: {element?.name}");
        }
        
        private void OnElementSelected(VisualElement element)
        {
            OnInputEvent?.Invoke($"Element selected: {element?.name}");
        }
        
        private void OnNavigationModeChangedInternal(NavigationMode mode)
        {
            _systemState["NavigationMode"] = mode;
            OnNavigationModeChanged?.Invoke(mode);
            OnInputEvent?.Invoke($"Navigation mode changed: {mode}");
        }
        
        private void OnControllerConnected()
        {
            OnInputEvent?.Invoke("Controller connected");
        }
        
        private void OnControllerDisconnected()
        {
            OnInputEvent?.Invoke("Controller disconnected");
        }
        
        private void OnShortcutExecuted(string shortcut)
        {
            OnInputEvent?.Invoke($"Shortcut executed: {shortcut}");
        }
        
        private void OnSearchModeEntered()
        {
            OnInputEvent?.Invoke("Search mode entered");
        }
        
        private void OnSearchModeExited()
        {
            OnInputEvent?.Invoke("Search mode exited");
        }
        
        private void OnHighContrastToggled(bool enabled)
        {
            OnInputEvent?.Invoke($"High contrast: {(enabled ? "enabled" : "disabled")}");
        }
        
        private void OnScreenReaderAnnouncement(string message)
        {
            OnInputEvent?.Invoke($"Screen reader: {message}");
        }
        
        private void OnMenuOpened(string menuId)
        {
            _systemState["MenuOpen"] = true;
            RefreshSystem();
        }
        
        private void OnMenuClosed(string menuId)
        {
            _systemState["MenuOpen"] = false;
        }
        
        // Mouse and touch event handlers
        private void OnMouseEnter(MouseEnterEvent evt)
        {
            if (_controllerSupport != null && CurrentNavigationMode != NavigationMode.Mouse)
            {
                _controllerSupport.SwitchNavigationMode(NavigationMode.Mouse);
            }
        }
        
        private void OnMouseMove(MouseMoveEvent evt)
        {
            _systemState["LastInputTime"] = Time.time;
        }
        
        private void OnMouseClick(ClickEvent evt)
        {
            OnInputEvent?.Invoke($"Mouse click at {evt.position}");
        }
        
        private void OnTouchStart(PointerDownEvent evt)
        {
            if (_controllerSupport != null && CurrentNavigationMode != NavigationMode.Touch)
            {
                _controllerSupport.SwitchNavigationMode(NavigationMode.Touch);
            }
        }
        
        private void OnTouchMove(PointerMoveEvent evt)
        {
            _systemState["LastInputTime"] = Time.time;
        }
        
        private void OnTouchEnd(PointerUpEvent evt)
        {
            OnInputEvent?.Invoke($"Touch ended at {evt.position}");
        }
        
        private void OnDestroy()
        {
            ShutdownSystem();
        }
    }
    
    // Supporting interfaces and classes
    public interface IInputComponent
    {
        void Initialize();
        void Update();
        void Refresh();
        void Cleanup();
    }
    
    [System.Serializable]
    public class InputSystemStatus
    {
        public bool IsInitialized;
        public NavigationMode NavigationMode;
        public int NavigableElementCount;
        public bool IsControllerConnected;
        public bool IsInSearchMode;
        public List<string> AccessibilityFeaturesActive;
    }
#endif
}