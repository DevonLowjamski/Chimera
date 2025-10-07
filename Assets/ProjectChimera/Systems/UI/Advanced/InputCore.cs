using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System;

#if UNITY_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ProjectChimera.Systems.UI.Advanced
{
#if UNITY_INPUT_SYSTEM
    /// <summary>
    /// Core input system infrastructure and component coordination.
    /// Handles initialization, component orchestration, and base input management.
    /// </summary>
    [RequireComponent(typeof(PlayerInput))]
    public class InputCore : MonoBehaviour
    {
        [Header("Input Configuration")]
        [SerializeField] protected bool _enableKeyboardNavigation = true;
        [SerializeField] protected bool _enableControllerSupport = true;
        [SerializeField] protected bool _enableMouseSupport = true;
        [SerializeField] protected bool _enableTouchSupport = false;
        
        [Header("Input Actions")]
        [SerializeField] protected InputActionReference _menuToggleAction;
        [SerializeField] protected InputActionReference _navigateAction;
        [SerializeField] protected InputActionReference _selectAction;
        [SerializeField] protected InputActionReference _cancelAction;
        [SerializeField] protected InputActionReference _scrollAction;
        [SerializeField] protected InputActionReference _shortcutAction;

        // Core system references
        protected AdvancedMenuSystem _menuSystem;
        protected VisualFeedbackIntegration _visualFeedback;
        protected PlayerInput _playerInput;

        // Component references
        protected InputNavigationHandler _navigationHandler;
        protected InputActionProcessor _actionProcessor;
        protected InputControllerSupport _controllerSupport;
        protected InputAccessibilityManager _accessibilityManager;

        // Core state
        protected NavigationMode _currentNavigationMode = NavigationMode.Mouse;
        protected bool _isInitialized = false;

        // Events
        public event Action<NavigationMode> OnNavigationModeChanged;
        public event Action<bool> OnInputSystemStateChanged;

        protected virtual void Awake()
        {
            InitializeInputCore();
        }

        protected virtual void Start()
        {
            if (_isInitialized)
            {
                SetupInputComponents();
                StartCoroutine(UpdateInputCore());
            }
        }

        protected virtual void InitializeInputCore()
        {
            try
            {
                // Get core system references
                _menuSystem = GetComponent<AdvancedMenuSystem>();
                _visualFeedback = GetComponent<VisualFeedbackIntegration>();
                _playerInput = GetComponent<PlayerInput>();

                // Validate required components
                if (!ValidateRequiredComponents())
                {
                    return;
                }

                // Initialize components
                InitializeComponents();

                // Subscribe to menu events
                SubscribeToMenuEvents();

                _isInitialized = true;
                OnInputSystemStateChanged?.Invoke(true);

                LogInfo("Input core initialized successfully");
            }
            catch (System.Exception ex)
            {
                LogError($"Error during input core initialization: {ex.Message}");
                enabled = false;
            }
        }

        protected virtual bool ValidateRequiredComponents()
        {
            if (_menuSystem == null)
            {
                LogError("AdvancedMenuSystem component required");
                enabled = false;
                return false;
            }

            if (_playerInput == null)
            {
                LogError("PlayerInput component required");
                enabled = false;
                return false;
            }

            return true;
        }

        protected virtual void InitializeComponents()
        {
            // Initialize input components
            _navigationHandler = GetComponent<InputNavigationHandler>();
            if (_navigationHandler == null)
            {
                _navigationHandler = gameObject.AddComponent<InputNavigationHandler>();
            }
            _navigationHandler.Initialize(this);

            _actionProcessor = GetComponent<InputActionProcessor>();
            if (_actionProcessor == null)
            {
                _actionProcessor = gameObject.AddComponent<InputActionProcessor>();
            }
            _actionProcessor.Initialize(this);

            _controllerSupport = GetComponent<InputControllerSupport>();
            if (_controllerSupport == null)
            {
                _controllerSupport = gameObject.AddComponent<InputControllerSupport>();
            }
            _controllerSupport.Initialize(this);

            _accessibilityManager = GetComponent<InputAccessibilityManager>();
            if (_accessibilityManager == null)
            {
                _accessibilityManager = gameObject.AddComponent<InputAccessibilityManager>();
            }
            _accessibilityManager.Initialize(this);
        }

        protected virtual void SetupInputComponents()
        {
            // Setup input action callbacks
            SetupInputActions();

            // Setup device change detection
            InputSystem.onDeviceChange += OnDeviceChanged;

            LogInfo("Input components setup completed");
        }

        protected virtual void SetupInputActions()
        {
            if (_menuToggleAction != null)
            {
                _menuToggleAction.action.performed += OnMenuToggle;
                _menuToggleAction.action.Enable();
            }

            if (_navigateAction != null)
            {
                _navigateAction.action.performed += OnNavigate;
                _navigateAction.action.Enable();
            }

            if (_selectAction != null)
            {
                _selectAction.action.performed += OnSelect;
                _selectAction.action.Enable();
            }

            if (_cancelAction != null)
            {
                _cancelAction.action.performed += OnCancel;
                _cancelAction.action.Enable();
            }

            if (_scrollAction != null)
            {
                _scrollAction.action.performed += OnScroll;
                _scrollAction.action.Enable();
            }

            if (_shortcutAction != null)
            {
                _shortcutAction.action.performed += OnShortcut;
                _shortcutAction.action.Enable();
            }
        }

        protected virtual void SubscribeToMenuEvents()
        {
            if (_menuSystem != null)
            {
                _menuSystem.OnMenuOpened += OnMenuOpened;
                _menuSystem.OnMenuClosed += OnMenuClosed;
            }
        }

        protected virtual void UnsubscribeFromMenuEvents()
        {
            if (_menuSystem != null)
            {
                _menuSystem.OnMenuOpened -= OnMenuOpened;
                _menuSystem.OnMenuClosed -= OnMenuClosed;
            }
        }

        public virtual void SwitchNavigationMode(NavigationMode newMode)
        {
            if (_currentNavigationMode == newMode)
                return;

            var previousMode = _currentNavigationMode;
            _currentNavigationMode = newMode;

            // Notify components about mode change
            _navigationHandler?.OnNavigationModeChanged(newMode);
            _controllerSupport?.OnNavigationModeChanged(newMode);
            _accessibilityManager?.OnNavigationModeChanged(newMode);

            OnNavigationModeChanged?.Invoke(newMode);

            LogInfo($"Navigation mode changed from {previousMode} to {newMode}");
        }

        #region Input Action Handlers

        protected virtual void OnMenuToggle(InputAction.CallbackContext context)
        {
            _actionProcessor?.ProcessMenuToggle(context);
        }

        protected virtual void OnNavigate(InputAction.CallbackContext context)
        {
            _navigationHandler?.ProcessNavigation(context);
        }

        protected virtual void OnSelect(InputAction.CallbackContext context)
        {
            _actionProcessor?.ProcessSelection(context);
        }

        protected virtual void OnCancel(InputAction.CallbackContext context)
        {
            _actionProcessor?.ProcessCancel(context);
        }

        protected virtual void OnScroll(InputAction.CallbackContext context)
        {
            _navigationHandler?.ProcessScrolling(context);
        }

        protected virtual void OnShortcut(InputAction.CallbackContext context)
        {
            _actionProcessor?.ProcessShortcut(context);
        }

        #endregion

        #region Event Handlers

        protected virtual void OnMenuOpened(string menuId)
        {
            _navigationHandler?.OnMenuOpened(menuId);
            _controllerSupport?.OnMenuOpened(menuId);
            
            LogInfo($"Menu opened: {menuId}");
        }

        protected virtual void OnMenuClosed(string menuId)
        {
            _navigationHandler?.OnMenuClosed(menuId);
            _controllerSupport?.OnMenuClosed(menuId);
            
            LogInfo($"Menu closed: {menuId}");
        }

        protected virtual void OnDeviceChanged(InputDevice device, InputDeviceChange change)
        {
            if (change == InputDeviceChange.Added)
            {
                if (device is Gamepad)
                {
                    LogInfo("Gamepad connected");
                    if (_enableControllerSupport)
                    {
                        SwitchNavigationMode(NavigationMode.Controller);
                    }
                }
                else if (device is Keyboard)
                {
                    LogInfo("Keyboard connected");
                }
            }
            else if (change == InputDeviceChange.Removed)
            {
                if (device is Gamepad)
                {
                    LogInfo("Gamepad disconnected");
                    SwitchNavigationMode(NavigationMode.Keyboard);
                }
            }

            // Notify components about device changes
            _controllerSupport?.OnDeviceChanged(device, change);
            _accessibilityManager?.OnDeviceChanged(device, change);
        }

        #endregion

        protected virtual System.Collections.IEnumerator UpdateInputCore()
        {
            while (true)
            {
                yield return new WaitForSeconds(0.1f);

                // Update components
                _actionProcessor?.UpdateComponent();
                _controllerSupport?.UpdateComponent();
                _accessibilityManager?.UpdateComponent();
            }
        }

        protected virtual void OnDestroy()
        {
            // Cleanup input actions
            CleanupInputActions();

            // Unsubscribe from events
            UnsubscribeFromMenuEvents();
            InputSystem.onDeviceChange -= OnDeviceChanged;

            // Cleanup components
            _navigationHandler?.Cleanup();
            _actionProcessor?.Cleanup();
            _controllerSupport?.Cleanup();
            _accessibilityManager?.Cleanup();

            OnInputSystemStateChanged?.Invoke(false);
        }

        protected virtual void CleanupInputActions()
        {
            if (_menuToggleAction != null)
                _menuToggleAction.action.performed -= OnMenuToggle;
            if (_navigateAction != null)
                _navigateAction.action.performed -= OnNavigate;
            if (_selectAction != null)
                _selectAction.action.performed -= OnSelect;
            if (_cancelAction != null)
                _cancelAction.action.performed -= OnCancel;
            if (_scrollAction != null)
                _scrollAction.action.performed -= OnScroll;
            if (_shortcutAction != null)
                _shortcutAction.action.performed -= OnShortcut;
        }

        #region Public Properties

        public bool EnableKeyboardNavigation
        {
            get => _enableKeyboardNavigation;
            set => _enableKeyboardNavigation = value;
        }

        public bool EnableControllerSupport
        {
            get => _enableControllerSupport;
            set => _enableControllerSupport = value;
        }

        public bool EnableMouseSupport
        {
            get => _enableMouseSupport;
            set => _enableMouseSupport = value;
        }

        public bool EnableTouchSupport
        {
            get => _enableTouchSupport;
            set => _enableTouchSupport = value;
        }

        public NavigationMode CurrentNavigationMode => _currentNavigationMode;
        public bool IsInitialized => _isInitialized;
        public AdvancedMenuSystem MenuSystem => _menuSystem;
        public PlayerInput PlayerInput => _playerInput;

        public InputActionReference MenuToggleAction => _menuToggleAction;
        public InputActionReference NavigateAction => _navigateAction;
        public InputActionReference SelectAction => _selectAction;
        public InputActionReference CancelAction => _cancelAction;
        public InputActionReference ScrollAction => _scrollAction;
        public InputActionReference ShortcutAction => _shortcutAction;

        #endregion

        #region Logging Helpers

        protected void LogInfo(string message)
        {
            ChimeraLogger.LogInfo("InputCore", "$1");
        }

        protected void LogWarning(string message)
        {
            ChimeraLogger.LogInfo("InputCore", "$1");
        }

        protected void LogError(string message)
        {
            ChimeraLogger.LogInfo("InputCore", "$1");
        }

        #endregion
    }

    /// <summary>
    /// Navigation mode enumeration
    /// </summary>
    public enum NavigationMode
    {
        Mouse,
        Keyboard,
        Controller,
        Touch
    }
#endif
}