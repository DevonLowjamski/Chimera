using ProjectChimera.Core.Logging;
using UnityEngine;
using UnityEngine.UIElements;
using ProjectChimera.Systems.UI.Advanced;
using System.Collections.Generic;
using System.Collections;
using System;

#if UNITY_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ProjectChimera.Systems.UI.Advanced
{
#if UNITY_INPUT_SYSTEM
    /// <summary>
    /// Phase 2.3.4: Input System Integration
    /// Provides comprehensive input handling for advanced menu system including
    /// keyboard navigation, controller support, and accessibility features
    /// </summary>
    [RequireComponent(typeof(PlayerInput))]
    public class InputSystemIntegration : MonoBehaviour
    {
        [Header("Input Configuration")]
        [SerializeField] private bool _enableKeyboardNavigation = true;
        [SerializeField] private bool _enableControllerSupport = true;
        [SerializeField] private bool _enableMouseSupport = true;
        [SerializeField] private bool _enableTouchSupport = false;
        
        [Header("Keyboard Navigation")]
        [SerializeField] private bool _enableTabNavigation = true;
        [SerializeField] private bool _enableArrowNavigation = true;
        [SerializeField] private bool _enableShortcuts = true;
        [SerializeField] private bool _enableSearchMode = true;
        
        [Header("Controller Support")]
        [SerializeField] private float _controllerNavigationDelay = 0.2f;
        [SerializeField] private float _controllerScrollSpeed = 300f;
        [SerializeField] private bool _enableControllerCursor = true;
        [SerializeField] private bool _enableHapticFeedback = true;
        
        [Header("Accessibility")]
        [SerializeField] private bool _enableScreenReader = false;
        [SerializeField] private bool _enableHighContrastMode = false;
        [SerializeField] private bool _enableSlowMotionMode = false;
        [SerializeField, Range(0.5f, 3f)] private float _navigationSpeed = 1f;
        
        [Header("Input Actions")]
        [SerializeField] private InputActionReference _menuToggleAction;
        [SerializeField] private InputActionReference _navigateAction;
        [SerializeField] private InputActionReference _selectAction;
        [SerializeField] private InputActionReference _cancelAction;
        [SerializeField] private InputActionReference _scrollAction;
        [SerializeField] private InputActionReference _shortcutAction;
        
        // System references
        private AdvancedMenuSystem _menuSystem;
        private VisualFeedbackIntegration _visualFeedback;
        private PlayerInput _playerInput;
        
        // Navigation state
        private List<VisualElement> _navigableElements = new List<VisualElement>();
        private int _currentNavigationIndex = -1;
        private VisualElement _currentFocusedElement;
        private NavigationMode _currentNavigationMode = NavigationMode.Mouse;
        
        // Input tracking
        private Dictionary<InputAction, float> _actionCooldowns = new Dictionary<InputAction, float>();
        private Vector2 _lastNavigationInput;
        private float _navigationInputTime;
        private bool _isInSearchMode = false;
        private string _searchQuery = "";
        
        // Controller cursor
        private VisualElement _controllerCursor;
        private Vector2 _cursorPosition;
        private bool _cursorVisible = false;
        
        // Events
        public event Action<NavigationMode> OnNavigationModeChanged;
        public event Action<VisualElement> OnElementFocused;
        public event Action<string> OnShortcutExecuted;
        public event Action<string> OnSearchQueryChanged;
        
        private void Awake()
        {
            InitializeInputSystem();
        }
        
        private void Start()
        {
            SetupInputActions();
            SetupControllerCursor();
            StartCoroutine(UpdateInputSystem());
        }
        
        private void InitializeInputSystem()
        {
            _menuSystem = GetComponent<AdvancedMenuSystem>();
            _visualFeedback = GetComponent<VisualFeedbackIntegration>();
            _playerInput = GetComponent<PlayerInput>();
            
            if (_menuSystem == null)
            {
                ChimeraLogger.LogError("[InputSystemIntegration] AdvancedMenuSystem component required");
                enabled = false;
                return;
            }
            
            if (_playerInput == null)
            {
                ChimeraLogger.LogError("[InputSystemIntegration] PlayerInput component required");
                enabled = false;
                return;
            }
            
            // Subscribe to menu events
            _menuSystem.OnMenuOpened += OnMenuOpened;
            _menuSystem.OnMenuClosed += OnMenuClosed;
        }
        
        private void SetupInputActions()
        {
            // Setup input action callbacks
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
        }
        
        /// <summary>
        /// Register an element for keyboard/controller navigation
        /// </summary>
        public void RegisterNavigableElement(VisualElement element, int priority = 0)
        {
            if (element == null || _navigableElements.Contains(element))
                return;
            
            _navigableElements.Add(element);
            
            // Sort by priority and position
            _navigableElements.Sort((a, b) =>
            {
                var aPriority = GetElementPriority(a);
                var bPriority = GetElementPriority(b);
                
                if (aPriority != bPriority)
                    return bPriority.CompareTo(aPriority);
                
                // Sort by vertical position, then horizontal
                var aRect = a.layout;
                var bRect = b.layout;
                
                if (Mathf.Abs(aRect.y - bRect.y) > 5f)
                    return aRect.y.CompareTo(bRect.y);
                
                return aRect.x.CompareTo(bRect.x);
            });
            
            // Setup element for navigation
            SetupElementForNavigation(element);
        }
        
        /// <summary>
        /// Unregister an element from navigation
        /// </summary>
        public void UnregisterNavigableElement(VisualElement element)
        {
            _navigableElements.Remove(element);
            
            if (_currentFocusedElement == element)
            {
                _currentFocusedElement = null;
                _currentNavigationIndex = -1;
            }
        }
        
        /// <summary>
        /// Set focus to a specific element
        /// </summary>
        public void SetFocus(VisualElement element)
        {
            if (element == null)
                return;
            
            var index = _navigableElements.IndexOf(element);
            if (index >= 0)
            {
                SetNavigationIndex(index);
            }
        }
        
        /// <summary>
        /// Enable or disable search mode
        /// </summary>
        public void SetSearchMode(bool enabled)
        {
            if (_isInSearchMode == enabled)
                return;
            
            _isInSearchMode = enabled;
            
            if (_isInSearchMode)
            {
                _searchQuery = "";
                StartSearchMode();
            }
            else
            {
                EndSearchMode();
            }
            
            OnSearchQueryChanged?.Invoke(_searchQuery);
        }
        
        private void SetupElementForNavigation(VisualElement element)
        {
            // Add navigation styling
            element.AddToClassList("navigable-element");
            
            // Setup keyboard focus
            element.focusable = true;
            element.tabIndex = GetElementPriority(element);
            
            // Setup focus events
            element.RegisterCallback<FocusInEvent>(OnElementFocusIn);
            element.RegisterCallback<FocusOutEvent>(OnElementFocusOut);
            
            // Setup keyboard events
            element.RegisterCallback<KeyDownEvent>(OnElementKeyDown);
            
            // Setup mouse events for navigation mode switching
            element.RegisterCallback<MouseEnterEvent>(OnElementMouseEnter);
        }
        
        private int GetElementPriority(VisualElement element)
        {
            // Check if element has priority metadata
            if (element.userData is Dictionary<string, object> metadata)
            {
                if (metadata.TryGetValue("NavigationPriority", out var priority))
                {
                    return (int)priority;
                }
            }
            
            // Default priority based on element type
            if (element.ClassListContains("menu-category-item"))
                return 100;
            if (element.ClassListContains("menu-action-item"))
                return 80;
            if (element.ClassListContains("button"))
                return 60;
            
            return 50;
        }
        
        private void OnMenuToggle(InputAction.CallbackContext context)
        {
            if (IsActionOnCooldown(_menuToggleAction.action))
                return;
            
            SetActionCooldown(_menuToggleAction.action, 0.3f);
            
            if (_menuSystem.IsMenuOpen())
            {
                _menuSystem.CloseAllMenus();
            }
            else
            {
                // Open menu at center of screen
                var screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 10f);
                var worldPosition = Camera.main?.ScreenToWorldPoint(screenCenter) ?? Vector3.zero;
                _menuSystem.OpenContextualMenu(worldPosition);
            }
        }
        
        private void OnNavigate(InputAction.CallbackContext context)
        {
            if (!_menuSystem.IsMenuOpen())
                return;
            
            var input = context.ReadValue<Vector2>();
            
            // Update navigation mode
            if (_currentNavigationMode != NavigationMode.Controller)
            {
                SwitchNavigationMode(NavigationMode.Controller);
            }
            
            // Handle navigation based on current mode
            if (_enableControllerSupport && _currentNavigationMode == NavigationMode.Controller)
            {
                HandleControllerNavigation(input);
            }
            else if (_enableArrowNavigation && _currentNavigationMode == NavigationMode.Keyboard)
            {
                HandleKeyboardNavigation(input);
            }
        }
        
        private void OnSelect(InputAction.CallbackContext context)
        {
            if (!_menuSystem.IsMenuOpen())
                return;
            
            if (IsActionOnCooldown(_selectAction.action))
                return;
            
            SetActionCooldown(_selectAction.action, 0.2f);
            
            if (_currentFocusedElement != null)
            {
                ExecuteElementAction(_currentFocusedElement);
            }
        }
        
        private void OnCancel(InputAction.CallbackContext context)
        {
            if (IsActionOnCooldown(_cancelAction.action))
                return;
            
            SetActionCooldown(_cancelAction.action, 0.2f);
            
            if (_isInSearchMode)
            {
                SetSearchMode(false);
            }
            else if (_menuSystem.IsMenuOpen())
            {
                _menuSystem.CloseAllMenus();
            }
        }
        
        private void OnScroll(InputAction.CallbackContext context)
        {
            if (!_menuSystem.IsMenuOpen())
                return;
            
            var scrollDelta = context.ReadValue<Vector2>();
            HandleScrolling(scrollDelta);
        }
        
        private void OnShortcut(InputAction.CallbackContext context)
        {
            if (!_enableShortcuts)
                return;
            
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;
            
            // Check for common shortcuts
            if (keyboard.ctrlKey.isPressed)
            {
                if (keyboard.fKey.wasPressedThisFrame)
                {
                    SetSearchMode(!_isInSearchMode);
                    OnShortcutExecuted?.Invoke("Find");
                }
                else if (keyboard.escapeKey.wasPressedThisFrame)
                {
                    SetSearchMode(false);
                    OnShortcutExecuted?.Invoke("CancelSearch");
                }
            }
            
            // Number key shortcuts for quick category selection
            for (int i = 1; i <= 9; i++)
            {
                if (keyboard[(Key)(Key.Digit1 + i - 1)].wasPressedThisFrame)
                {
                    SelectCategoryByIndex(i - 1);
                    OnShortcutExecuted?.Invoke($"Category{i}");
                    break;
                }
            }
        }
        
        private void HandleControllerNavigation(Vector2 input)
        {
            if (input.magnitude < 0.3f)
                return;
            
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
                        NavigateNext();
                    else if (input.x < -0.5f)
                        NavigatePrevious();
                }
                else
                {
                    if (input.y > 0.5f)
                        NavigateUp();
                    else if (input.y < -0.5f)
                        NavigateDown();
                }
            }
        }
        
        private void HandleKeyboardNavigation(Vector2 input)
        {
            if (input.magnitude < 0.1f)
                return;
            
            if (Mathf.Abs(input.x) > Mathf.Abs(input.y))
            {
                if (input.x > 0.5f)
                    NavigateNext();
                else if (input.x < -0.5f)
                    NavigatePrevious();
            }
            else
            {
                if (input.y > 0.5f)
                    NavigateUp();
                else if (input.y < -0.5f)
                    NavigateDown();
            }
        }
        
        private void HandleScrolling(Vector2 scrollDelta)
        {
            // Find scrollable container
            var scrollView = _currentFocusedElement?.GetFirstAncestorOfType<ScrollView>();
            if (scrollView != null)
            {
                var currentOffset = scrollView.scrollOffset;
                scrollView.scrollOffset = new Vector2(
                    currentOffset.x + scrollDelta.x,
                    currentOffset.y + scrollDelta.y
                );
            }
        }
        
        private void NavigateNext()
        {
            if (_navigableElements.Count == 0)
                return;
            
            SetNavigationIndex((_currentNavigationIndex + 1) % _navigableElements.Count);
        }
        
        private void NavigatePrevious()
        {
            if (_navigableElements.Count == 0)
                return;
            
            SetNavigationIndex((_currentNavigationIndex - 1 + _navigableElements.Count) % _navigableElements.Count);
        }
        
        private void NavigateUp()
        {
            if (_navigableElements.Count == 0 || _currentNavigationIndex < 0)
                return;
            
            var currentElement = _navigableElements[_currentNavigationIndex];
            var currentRect = currentElement.layout;
            
            // Find element above
            VisualElement bestMatch = null;
            float bestDistance = float.MaxValue;
            
            foreach (var element in _navigableElements)
            {
                if (element == currentElement)
                    continue;
                
                var rect = element.layout;
                if (rect.y < currentRect.y) // Above current element
                {
                    float distance = Vector2.Distance(
                        new Vector2(rect.center.x, rect.center.y),
                        new Vector2(currentRect.center.x, currentRect.center.y)
                    );
                    
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestMatch = element;
                    }
                }
            }
            
            if (bestMatch != null)
            {
                var index = _navigableElements.IndexOf(bestMatch);
                SetNavigationIndex(index);
            }
        }
        
        private void NavigateDown()
        {
            if (_navigableElements.Count == 0 || _currentNavigationIndex < 0)
                return;
            
            var currentElement = _navigableElements[_currentNavigationIndex];
            var currentRect = currentElement.layout;
            
            // Find element below
            VisualElement bestMatch = null;
            float bestDistance = float.MaxValue;
            
            foreach (var element in _navigableElements)
            {
                if (element == currentElement)
                    continue;
                
                var rect = element.layout;
                if (rect.y > currentRect.y) // Below current element
                {
                    float distance = Vector2.Distance(
                        new Vector2(rect.center.x, rect.center.y),
                        new Vector2(currentRect.center.x, currentRect.center.y)
                    );
                    
                    if (distance < bestDistance)
                    {
                        bestDistance = distance;
                        bestMatch = element;
                    }
                }
            }
            
            if (bestMatch != null)
            {
                var index = _navigableElements.IndexOf(bestMatch);
                SetNavigationIndex(index);
            }
        }
        
        private void SetNavigationIndex(int index)
        {
            if (index < 0 || index >= _navigableElements.Count)
                return;
            
            // Remove focus from previous element
            if (_currentFocusedElement != null)
            {
                RemoveElementFocus(_currentFocusedElement);
            }
            
            // Set new focus
            _currentNavigationIndex = index;
            _currentFocusedElement = _navigableElements[index];
            
            ApplyElementFocus(_currentFocusedElement);
            OnElementFocused?.Invoke(_currentFocusedElement);
        }
        
        private void MoveCursor(Vector2 delta)
        {
            if (_controllerCursor == null)
                return;
            
            _cursorPosition += delta;
            _cursorPosition.x = Mathf.Clamp(_cursorPosition.x, 0, Screen.width);
            _cursorPosition.y = Mathf.Clamp(_cursorPosition.y, 0, Screen.height);
            
            _controllerCursor.style.left = _cursorPosition.x;
            _controllerCursor.style.top = _cursorPosition.y;
            
            ShowControllerCursor(true);
            
            // Find element under cursor
            var elementUnderCursor = GetElementAtPosition(_cursorPosition);
            if (elementUnderCursor != null && _navigableElements.Contains(elementUnderCursor))
            {
                var index = _navigableElements.IndexOf(elementUnderCursor);
                SetNavigationIndex(index);
            }
        }
        
        private void ShowControllerCursor(bool visible)
        {
            if (_controllerCursor == null)
                return;
            
            _cursorVisible = visible;
            _controllerCursor.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
        }
        
        private VisualElement GetElementAtPosition(Vector2 position)
        {
            // This would need to be implemented with proper UI hit testing
            // For now, return null
            return null;
        }
        
        private void ExecuteElementAction(VisualElement element)
        {
            // Trigger click event on element
            using (var clickEvent = ClickEvent.GetPooled())
            {
                clickEvent.target = element;
                element.SendEvent(clickEvent);
            }
            
            // Haptic feedback for controller
            if (_enableHapticFeedback && _currentNavigationMode == NavigationMode.Controller)
            {
                var gamepad = Gamepad.current;
                if (gamepad != null)
                {
                    gamepad.SetMotorSpeeds(0.2f, 0.2f);
                    StartCoroutine(StopHapticFeedback(0.1f));
                }
            }
        }
        
        private void SelectCategoryByIndex(int index)
        {
            var categoryElements = _navigableElements.Where(e => e.ClassListContains("menu-category-item")).ToList();
            
            if (index >= 0 && index < categoryElements.Count)
            {
                ExecuteElementAction(categoryElements[index]);
            }
        }
        
        private void ApplyElementFocus(VisualElement element)
        {
            element.AddToClassList("navigation-focused");
            element.style.borderLeftWidth = 3f;
            element.style.borderRightWidth = 3f;
            element.style.borderTopWidth = 3f;
            element.style.borderBottomWidth = 3f;
            element.style.borderLeftColor = Color.cyan;
            element.style.borderRightColor = Color.cyan;
            element.style.borderTopColor = Color.cyan;
            element.style.borderBottomColor = Color.cyan;
            
            element.Focus();
            
            // Scroll element into view if needed
            ScrollIntoView(element);
        }
        
        private void RemoveElementFocus(VisualElement element)
        {
            element.RemoveFromClassList("navigation-focused");
            element.style.borderLeftWidth = 0f;
            element.style.borderRightWidth = 0f;
            element.style.borderTopWidth = 0f;
            element.style.borderBottomWidth = 0f;
        }
        
        private void ScrollIntoView(VisualElement element)
        {
            var scrollView = element.GetFirstAncestorOfType<ScrollView>();
            if (scrollView != null)
            {
                scrollView.ScrollTo(element);
            }
        }
        
        private void SwitchNavigationMode(NavigationMode newMode)
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
        }
        
        private void StartSearchMode()
        {
            // Implementation for search mode UI
            ChimeraLogger.Log("[InputSystemIntegration] Search mode activated");
        }
        
        private void EndSearchMode()
        {
            _searchQuery = "";
            ChimeraLogger.Log("[InputSystemIntegration] Search mode deactivated");
        }
        
        private bool IsActionOnCooldown(InputAction action)
        {
            if (_actionCooldowns.TryGetValue(action, out var cooldown))
            {
                return Time.time < cooldown;
            }
            return false;
        }
        
        private void SetActionCooldown(InputAction action, float duration)
        {
            _actionCooldowns[action] = Time.time + duration;
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
        
        private System.Collections.IEnumerator UpdateInputSystem()
        {
            while (true)
            {
                yield return new WaitForSeconds(0.1f);
                
                // Update action cooldowns
                var expiredActions = new List<InputAction>();
                foreach (var kvp in _actionCooldowns)
                {
                    if (Time.time >= kvp.Value)
                    {
                        expiredActions.Add(kvp.Key);
                    }
                }
                
                foreach (var action in expiredActions)
                {
                    _actionCooldowns.Remove(action);
                }
                
                // Update controller cursor if inactive
                if (_cursorVisible && _currentNavigationMode != NavigationMode.Controller)
                {
                    ShowControllerCursor(false);
                }
            }
        }
        
        // Event handlers
        private void OnMenuOpened(string menuId)
        {
            // Refresh navigable elements when menu opens
            RefreshNavigableElements();
            
            // Auto-focus first element if using keyboard/controller
            if (_currentNavigationMode != NavigationMode.Mouse && _navigableElements.Count > 0)
            {
                SetNavigationIndex(0);
            }
        }
        
        private void OnMenuClosed(string menuId)
        {
            _navigableElements.Clear();
            _currentNavigationIndex = -1;
            _currentFocusedElement = null;
            ShowControllerCursor(false);
        }
        
        private void OnDeviceChanged(InputDevice device, InputDeviceChange change)
        {
            if (change == InputDeviceChange.Added)
            {
                if (device is Gamepad)
                {
                    ChimeraLogger.Log("[InputSystemIntegration] Gamepad connected");
                    SwitchNavigationMode(NavigationMode.Controller);
                }
                else if (device is Keyboard)
                {
                    ChimeraLogger.Log("[InputSystemIntegration] Keyboard connected");
                }
            }
            else if (change == InputDeviceChange.Removed)
            {
                if (device is Gamepad)
                {
                    ChimeraLogger.Log("[InputSystemIntegration] Gamepad disconnected");
                    SwitchNavigationMode(NavigationMode.Keyboard);
                }
            }
        }
        
        private void OnElementFocusIn(FocusInEvent evt)
        {
            var element = evt.target as VisualElement;
            if (element != null && _navigableElements.Contains(element))
            {
                var index = _navigableElements.IndexOf(element);
                SetNavigationIndex(index);
            }
        }
        
        private void OnElementFocusOut(FocusOutEvent evt)
        {
            var element = evt.target as VisualElement;
            if (element != null)
            {
                RemoveElementFocus(element);
            }
        }
        
        private void OnElementKeyDown(KeyDownEvent evt)
        {
            switch (evt.keyCode)
            {
                case KeyCode.Return:
                case KeyCode.KeypadEnter:
                case KeyCode.Space:
                    ExecuteElementAction(evt.target as VisualElement);
                    evt.StopPropagation();
                    break;
                    
                case KeyCode.Tab:
                    if (evt.shiftKey)
                        NavigatePrevious();
                    else
                        NavigateNext();
                    evt.StopPropagation();
                    break;
                    
                case KeyCode.Escape:
                    OnCancel(new InputAction.CallbackContext());
                    evt.StopPropagation();
                    break;
            }
        }
        
        private void OnElementMouseEnter(MouseEnterEvent evt)
        {
            // Switch to mouse mode when mouse is used
            if (_currentNavigationMode != NavigationMode.Mouse)
            {
                SwitchNavigationMode(NavigationMode.Mouse);
            }
        }
        
        private void RefreshNavigableElements()
        {
            _navigableElements.Clear();
            
            // Find all navigable elements in the current menu
            var rootDocument = GetComponent<UIDocument>();
            if (rootDocument != null)
            {
                var root = rootDocument.rootVisualElement;
                FindNavigableElements(root);
            }
        }
        
        private void FindNavigableElements(VisualElement root)
        {
            if (root == null)
                return;
            
            // Check if this element is navigable
            if (root.focusable && root.style.display == DisplayStyle.Flex)
            {
                RegisterNavigableElement(root);
            }
            
            // Recursively check children
            foreach (var child in root.Children())
            {
                FindNavigableElements(child);
            }
        }
        
        private void OnDestroy()
        {
            // Cleanup input actions
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
            
            InputSystem.onDeviceChange -= OnDeviceChanged;
        }
        
        // Public API
        public NavigationMode CurrentNavigationMode => _currentNavigationMode;
        public VisualElement CurrentFocusedElement => _currentFocusedElement;
        public int NavigableElementCount => _navigableElements.Count;
        public bool IsInSearchMode => _isInSearchMode;
        public string SearchQuery => _searchQuery;
    }
    
    // Supporting enums and structures
    public enum NavigationMode
    {
        Mouse,
        Keyboard,
        Controller,
        Touch
    }
#endif
}