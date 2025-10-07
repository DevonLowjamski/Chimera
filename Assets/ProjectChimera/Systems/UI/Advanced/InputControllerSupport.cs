using UnityEngine;
using UnityEngine.UIElements;
using System.Collections;
using System;

#if UNITY_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ProjectChimera.Systems.UI.Advanced
{
#if UNITY_INPUT_SYSTEM
    /// <summary>
    /// Controller support for advanced input system.
    /// Handles gamepad input, controller cursor, and haptic feedback.
    /// </summary>
    [RequireComponent(typeof(InputNavigationCore))]
    public class InputControllerSupport : MonoBehaviour
    {
        [Header("Controller Configuration")]
        [SerializeField] private bool _enableControllerSupport = true;
        [SerializeField] private float _controllerNavigationDelay = 0.2f;
        [SerializeField] private float _controllerScrollSpeed = 300f;
        [SerializeField] private bool _enableControllerCursor = true;
        [SerializeField] private bool _enableHapticFeedback = true;
        
        [Header("Controller Input Actions")]
        [SerializeField] private InputActionReference _navigateAction;
        [SerializeField] private InputActionReference _selectAction;
        [SerializeField] private InputActionReference _cancelAction;
        [SerializeField] private InputActionReference _scrollAction;
        
        // System references
        private InputNavigationCore _navigationCore;
        private PlayerInput _playerInput;
        
        // Controller cursor
        private VisualElement _controllerCursor;
        private Vector2 _cursorPosition;
        private bool _cursorVisible = false;
        
        // Input tracking
        private Vector2 _lastNavigationInput;
        private float _navigationInputTime;
        private NavigationMode _currentNavigationMode = NavigationMode.Mouse;
        
        // Events
        public event Action<NavigationMode> OnNavigationModeChanged;
        public event Action OnControllerConnected;
        public event Action OnControllerDisconnected;
        
        // Properties
        public bool EnableControllerSupport { get => _enableControllerSupport; set => _enableControllerSupport = value; }
        public bool EnableControllerCursor { get => _enableControllerCursor; set => _enableControllerCursor = value; }
        public bool EnableHapticFeedback { get => _enableHapticFeedback; set => _enableHapticFeedback = value; }
        public NavigationMode CurrentNavigationMode => _currentNavigationMode;
        public bool IsControllerConnected => Gamepad.current != null;
        
        private void Awake()
        {
            InitializeController();
        }
        
        private void Start()
        {
            SetupInputActions();
            SetupControllerCursor();
            StartCoroutine(UpdateControllerSystem());
        }
        
        private void InitializeController()
        {
            _navigationCore = GetComponent<InputNavigationCore>();
            _playerInput = GetComponent<PlayerInput>();
            
            if (_navigationCore == null)
            {
                ChimeraLogger.LogInfo("InputControllerSupport", "$1");
                enabled = false;
                return;
            }
            
            if (_playerInput == null)
            {
                ChimeraLogger.LogInfo("InputControllerSupport", "$1");
                enabled = false;
                return;
            }
            
            ChimeraLogger.LogInfo("InputControllerSupport", "$1");
        }
        
        private void SetupInputActions()
        {
            if (!_enableControllerSupport)
                return;
            
            // Setup input action callbacks
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
            
            // Setup device change detection
            InputSystem.onDeviceChange += OnDeviceChanged;
        }
        
        private void SetupControllerCursor()
        {
            if (!_enableControllerCursor)
                return;
            
            var rootDocument = GetComponent<UIDocument>();
            if (rootDocument == null)
                return;
            
            var root = rootDocument.rootVisualElement;
            
            _controllerCursor = new VisualElement();
            _controllerCursor.name = "controller-cursor";
            _controllerCursor.AddToClassList("controller-cursor");
            _controllerCursor.style.position = Position.Absolute;
            _controllerCursor.style.width = 16f;
            _controllerCursor.style.height = 16f;
            _controllerCursor.style.borderTopLeftRadius = Length.Percent(50);
            _controllerCursor.style.borderTopRightRadius = Length.Percent(50);
            _controllerCursor.style.borderBottomLeftRadius = Length.Percent(50);
            _controllerCursor.style.borderBottomRightRadius = Length.Percent(50);
            _controllerCursor.style.backgroundColor = new Color(1f, 1f, 1f, 0.8f);
            _controllerCursor.style.borderLeftWidth = 2f;
            _controllerCursor.style.borderRightWidth = 2f;
            _controllerCursor.style.borderTopWidth = 2f;
            _controllerCursor.style.borderBottomWidth = 2f;
            _controllerCursor.style.borderLeftColor = Color.black;
            _controllerCursor.style.borderRightColor = Color.black;
            _controllerCursor.style.borderTopColor = Color.black;
            _controllerCursor.style.borderBottomColor = Color.black;
            _controllerCursor.style.display = DisplayStyle.None;
            
            root.Add(_controllerCursor);
            
            ChimeraLogger.LogInfo("InputControllerSupport", "$1");
        }
        
        /// <summary>
        /// Switch to a new navigation mode
        /// </summary>
        public void SwitchNavigationMode(NavigationMode newMode)
        {
            if (_currentNavigationMode == newMode)
                return;
            
            _currentNavigationMode = newMode;
            
            // Update cursor visibility
            if (newMode == NavigationMode.Controller && _enableControllerCursor)
            {
                ShowControllerCursor(true);
            }
            else if (newMode == NavigationMode.Mouse)
            {
                ShowControllerCursor(false);
            }
            
            OnNavigationModeChanged?.Invoke(newMode);
            ChimeraLogger.LogInfo("InputControllerSupport", "$1");
        }
        
        /// <summary>
        /// Show or hide the controller cursor
        /// </summary>
        public void ShowControllerCursor(bool visible)
        {
            if (_controllerCursor == null)
                return;
            
            _cursorVisible = visible;
            _controllerCursor.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
        
        /// <summary>
        /// Trigger haptic feedback on connected controller
        /// </summary>
        public void TriggerHapticFeedback(float intensity = 0.2f, float duration = 0.1f)
        {
            if (!_enableHapticFeedback)
                return;
            
            var gamepad = Gamepad.current;
            if (gamepad != null)
            {
                gamepad.SetMotorSpeeds(intensity, intensity);
                StartCoroutine(StopHapticFeedback(duration));
            }
        }
        
        /// <summary>
        /// Update controller cursor position
        /// </summary>
        public void UpdateCursorPosition(Vector2 screenPosition)
        {
            if (_controllerCursor == null)
                return;
            
            _cursorPosition = screenPosition;
            _cursorPosition.x = Mathf.Clamp(_cursorPosition.x, 0, Screen.width);
            _cursorPosition.y = Mathf.Clamp(_cursorPosition.y, 0, Screen.height);
            
            _controllerCursor.style.left = _cursorPosition.x;
            _controllerCursor.style.top = _cursorPosition.y;
        }
        
        private void OnNavigate(InputAction.CallbackContext context)
        {
            if (!_enableControllerSupport)
                return;
            
            var input = context.ReadValue<Vector2>();
            
            // Update navigation mode
            if (_currentNavigationMode != NavigationMode.Controller)
            {
                SwitchNavigationMode(NavigationMode.Controller);
            }
            
            // Handle navigation based on current mode
            HandleControllerNavigation(input);
        }
        
        private void OnSelect(InputAction.CallbackContext context)
        {
            if (!_enableControllerSupport)
                return;
            
            if (_currentNavigationMode != NavigationMode.Controller)
            {
                SwitchNavigationMode(NavigationMode.Controller);
            }
            
            _navigationCore.SelectCurrentElement();
            TriggerHapticFeedback(0.3f, 0.15f);
        }
        
        private void OnCancel(InputAction.CallbackContext context)
        {
            if (!_enableControllerSupport)
                return;
            
            // Handle cancel action - could be handled by parent system
            TriggerHapticFeedback(0.1f, 0.05f);
        }
        
        private void OnScroll(InputAction.CallbackContext context)
        {
            if (!_enableControllerSupport)
                return;
            
            var scrollDelta = context.ReadValue<Vector2>();
            HandleControllerScrolling(scrollDelta);
        }
        
        private void HandleControllerNavigation(Vector2 input)
        {
            if (input.magnitude < 0.3f)
                return;
            
            // Check for navigation delay
            if (Time.time - _navigationInputTime < _controllerNavigationDelay)
                return;
            
            _navigationInputTime = Time.time;
            _lastNavigationInput = input;
            
            // Handle cursor mode
            if (_enableControllerCursor && _currentNavigationMode == NavigationMode.Controller)
            {
                MoveCursor(input * _controllerScrollSpeed * Time.deltaTime);
            }
            else
            {
                // Handle discrete navigation
                if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
                {
                    if (input.x > 0.5f)
                        _navigationCore.NavigateRight();
                    else if (input.x < -0.5f)
                        _navigationCore.NavigateLeft();
                }
                else
                {
                    if (input.y > 0.5f)
                        _navigationCore.NavigateUp();
                    else if (input.y < -0.5f)
                        _navigationCore.NavigateDown();
                }
            }
        }
        
        private void HandleControllerScrolling(Vector2 scrollDelta)
        {
            // Find scrollable container
            var currentElement = _navigationCore.CurrentFocusedElement;
            var scrollView = currentElement?.GetFirstAncestorOfType<ScrollView>();
            if (scrollView != null)
            {
                var currentOffset = scrollView.scrollOffset;
                scrollView.scrollOffset = new Vector2(
                    currentOffset.x + scrollDelta.x,
                    currentOffset.y + scrollDelta.y
                );
            }
        }
        
        private void MoveCursor(Vector2 delta)
        {
            if (_controllerCursor == null)
                return;
            
            _cursorPosition += delta;
            UpdateCursorPosition(_cursorPosition);
            ShowControllerCursor(true);
            
            // Find element under cursor
            var elementUnderCursor = GetElementAtPosition(_cursorPosition);
            if (elementUnderCursor != null)
            {
                _navigationCore.SetFocus(elementUnderCursor);
            }
        }
        
        private VisualElement GetElementAtPosition(Vector2 position)
        {
            // This would need to be implemented with proper UI hit testing
            // For now, return null - would require access to UI hierarchy
            return null;
        }
        
        private IEnumerator StopHapticFeedback(float delay)
        {
            yield return new WaitForSeconds(delay);
            
            var gamepad = Gamepad.current;
            if (gamepad != null)
            {
                gamepad.SetMotorSpeeds(0f, 0f);
            }
        }
        
        private IEnumerator UpdateControllerSystem()
        {
            while (true)
            {
                yield return new WaitForSeconds(0.1f);
                
                // Update controller cursor if inactive
                if (_cursorVisible && _currentNavigationMode != NavigationMode.Controller)
                {
                    ShowControllerCursor(false);
                }
                
                // Check for controller input to auto-switch modes
                var gamepad = Gamepad.current;
                if (gamepad != null)
                {
                    // Check if any gamepad input is active
                    if (gamepad.leftStick.ReadValue().magnitude > 0.1f ||
                        gamepad.rightStick.ReadValue().magnitude > 0.1f ||
                        gamepad.buttonSouth.isPressed ||
                        gamepad.buttonEast.isPressed ||
                        gamepad.buttonWest.isPressed ||
                        gamepad.buttonNorth.isPressed)
                    {
                        if (_currentNavigationMode != NavigationMode.Controller)
                        {
                            SwitchNavigationMode(NavigationMode.Controller);
                        }
                    }
                }
            }
        }
        
        private void OnDeviceChanged(InputDevice device, InputDeviceChange change)
        {
            if (change == InputDeviceChange.Added)
            {
                if (device is Gamepad)
                {
                    ChimeraLogger.LogInfo("InputControllerSupport", "$1");
                    SwitchNavigationMode(NavigationMode.Controller);
                    OnControllerConnected?.Invoke();
                }
            }
            else if (change == InputDeviceChange.Removed)
            {
                if (device is Gamepad)
                {
                    ChimeraLogger.LogInfo("InputControllerSupport", "$1");
                    SwitchNavigationMode(NavigationMode.Keyboard);
                    OnControllerDisconnected?.Invoke();
                }
            }
        }
        
        private void OnDestroy()
        {
            // Cleanup input actions
            if (_navigateAction != null)
                _navigateAction.action.performed -= OnNavigate;
            if (_selectAction != null)
                _selectAction.action.performed -= OnSelect;
            if (_cancelAction != null)
                _cancelAction.action.performed -= OnCancel;
            if (_scrollAction != null)
                _scrollAction.action.performed -= OnScroll;
            
            InputSystem.onDeviceChange -= OnDeviceChanged;
            
            ChimeraLogger.LogInfo("InputControllerSupport", "$1");
        }
    }
    
    // Supporting enums
    public enum NavigationMode
    {
        Mouse,
        Keyboard,
        Controller,
        Touch
    }
#endif
}