using ProjectChimera.Core.Logging;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.Linq;
using System;

#if UNITY_INPUT_SYSTEM
using UnityEngine.InputSystem;
#endif

namespace ProjectChimera.Systems.UI.Advanced
{
#if UNITY_INPUT_SYSTEM
    /// <summary>
    /// Keyboard input handling and shortcut management for advanced menu systems.
    /// Handles keyboard navigation, shortcuts, and search functionality.
    /// </summary>
    [RequireComponent(typeof(InputNavigationCore))]
    public class InputKeyboardHandler : MonoBehaviour
    {
        [Header("Keyboard Configuration")]
        [SerializeField] private bool _enableKeyboardNavigation = true;
        [SerializeField] private bool _enableShortcuts = true;
        [SerializeField] private bool _enableSearchMode = true;
        [SerializeField] private bool _enableTabNavigation = true;
        
        [Header("Search Configuration")]
        [SerializeField] private float _searchTimeout = 3f;
        [SerializeField] private bool _searchCaseSensitive = false;
        [SerializeField] private bool _searchPartialMatch = true;
        
        [Header("Input Actions")]
        [SerializeField] private InputActionReference _menuToggleAction;
        [SerializeField] private InputActionReference _shortcutAction;
        [SerializeField] private InputActionReference _cancelAction;
        
        // System references
        private InputNavigationCore _navigationCore;
        private AdvancedMenuSystem _menuSystem;
        private PlayerInput _playerInput;
        
        // Search state
        private bool _isInSearchMode = false;
        private string _searchQuery = "";
        private float _lastSearchTime;
        private List<VisualElement> _searchResults = new List<VisualElement>();
        private int _currentSearchIndex = -1;
        
        // Shortcut mappings
        private Dictionary<string, System.Action> _shortcuts = new Dictionary<string, System.Action>();
        private Dictionary<InputAction, float> _actionCooldowns = new Dictionary<InputAction, float>();
        
        // Events
        public event Action<string> OnShortcutExecuted;
        public event Action<string> OnSearchQueryChanged;
        public event Action OnSearchModeEntered;
        public event Action OnSearchModeExited;
        
        // Properties
        public bool EnableKeyboardNavigation { get => _enableKeyboardNavigation; set => _enableKeyboardNavigation = value; }
        public bool EnableShortcuts { get => _enableShortcuts; set => _enableShortcuts = value; }
        public bool IsInSearchMode => _isInSearchMode;
        public string SearchQuery => _searchQuery;
        public int SearchResultCount => _searchResults.Count;
        
        private void Awake()
        {
            InitializeKeyboardHandler();
        }
        
        private void Start()
        {
            SetupInputActions();
            SetupDefaultShortcuts();
        }
        
        private void Update()
        {
            HandleKeyboardInput();
            UpdateSearchTimeout();
        }
        
        private void InitializeKeyboardHandler()
        {
            _navigationCore = GetComponent<InputNavigationCore>();
            _menuSystem = GetComponent<AdvancedMenuSystem>();
            _playerInput = GetComponent<PlayerInput>();
            
            if (_navigationCore == null)
            {
                ChimeraLogger.LogError("[InputKeyboardHandler] InputNavigationCore component required");
                enabled = false;
                return;
            }
            
            ChimeraLogger.Log("[InputKeyboardHandler] Keyboard handler initialized");
        }
        
        private void SetupInputActions()
        {
            if (!_enableKeyboardNavigation)
                return;
            
            // Setup input action callbacks
            if (_menuToggleAction != null)
            {
                _menuToggleAction.action.performed += OnMenuToggle;
                _menuToggleAction.action.Enable();
            }
            
            if (_shortcutAction != null)
            {
                _shortcutAction.action.performed += OnShortcut;
                _shortcutAction.action.Enable();
            }
            
            if (_cancelAction != null)
            {
                _cancelAction.action.performed += OnCancel;
                _cancelAction.action.Enable();
            }
        }
        
        private void SetupDefaultShortcuts()
        {
            // Common shortcuts
            RegisterShortcut("Ctrl+F", () => SetSearchMode(!_isInSearchMode), "Toggle search");
            RegisterShortcut("Escape", () => SetSearchMode(false), "Exit search");
            RegisterShortcut("Ctrl+1", () => SelectCategoryByIndex(0), "Select category 1");
            RegisterShortcut("Ctrl+2", () => SelectCategoryByIndex(1), "Select category 2");
            RegisterShortcut("Ctrl+3", () => SelectCategoryByIndex(2), "Select category 3");
            RegisterShortcut("Ctrl+4", () => SelectCategoryByIndex(3), "Select category 4");
            RegisterShortcut("Ctrl+5", () => SelectCategoryByIndex(4), "Select category 5");
            
            ChimeraLogger.Log($"[InputKeyboardHandler] Registered {_shortcuts.Count} default shortcuts");
        }
        
        /// <summary>
        /// Register a custom keyboard shortcut
        /// </summary>
        public void RegisterShortcut(string keyCombo, System.Action action, string description = "")
        {
            if (string.IsNullOrEmpty(keyCombo) || action == null)
                return;
            
            var normalizedCombo = NormalizeShortcutKey(keyCombo);
            _shortcuts[normalizedCombo] = action;
            
            ChimeraLogger.Log($"[InputKeyboardHandler] Registered shortcut: {keyCombo} - {description}");
        }
        
        /// <summary>
        /// Unregister a keyboard shortcut
        /// </summary>
        public void UnregisterShortcut(string keyCombo)
        {
            var normalizedCombo = NormalizeShortcutKey(keyCombo);
            if (_shortcuts.Remove(normalizedCombo))
            {
                ChimeraLogger.Log($"[InputKeyboardHandler] Unregistered shortcut: {keyCombo}");
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
                _lastSearchTime = Time.time;
                StartSearchMode();
                OnSearchModeEntered?.Invoke();
            }
            else
            {
                EndSearchMode();
                OnSearchModeExited?.Invoke();
            }
            
            OnSearchQueryChanged?.Invoke(_searchQuery);
        }
        
        /// <summary>
        /// Add character to search query
        /// </summary>
        public void AddToSearchQuery(char character)
        {
            if (!_isInSearchMode)
                return;
            
            _searchQuery += character;
            _lastSearchTime = Time.time;
            
            UpdateSearchResults();
            OnSearchQueryChanged?.Invoke(_searchQuery);
        }
        
        /// <summary>
        /// Remove last character from search query
        /// </summary>
        public void BackspaceSearchQuery()
        {
            if (!_isInSearchMode || _searchQuery.Length == 0)
                return;
            
            _searchQuery = _searchQuery.Substring(0, _searchQuery.Length - 1);
            _lastSearchTime = Time.time;
            
            UpdateSearchResults();
            OnSearchQueryChanged?.Invoke(_searchQuery);
        }
        
        /// <summary>
        /// Navigate to next search result
        /// </summary>
        public void NextSearchResult()
        {
            if (_searchResults.Count == 0)
                return;
            
            _currentSearchIndex = (_currentSearchIndex + 1) % _searchResults.Count;
            _navigationCore.SetFocus(_searchResults[_currentSearchIndex]);
        }
        
        /// <summary>
        /// Navigate to previous search result
        /// </summary>
        public void PreviousSearchResult()
        {
            if (_searchResults.Count == 0)
                return;
            
            _currentSearchIndex = (_currentSearchIndex - 1 + _searchResults.Count) % _searchResults.Count;
            _navigationCore.SetFocus(_searchResults[_currentSearchIndex]);
        }
        
        /// <summary>
        /// Get all registered shortcuts
        /// </summary>
        public Dictionary<string, System.Action> GetRegisteredShortcuts()
        {
            return new Dictionary<string, System.Action>(_shortcuts);
        }
        
        private void HandleKeyboardInput()
        {
            if (!_enableKeyboardNavigation)
                return;
            
            var keyboard = Keyboard.current;
            if (keyboard == null)
                return;
            
            // Handle search mode input
            if (_isInSearchMode)
            {
                HandleSearchInput(keyboard);
                return;
            }
            
            // Handle shortcuts
            if (_enableShortcuts)
            {
                HandleShortcuts(keyboard);
            }
        }
        
        private void HandleSearchInput(Keyboard keyboard)
        {
            // Handle alphanumeric input
            foreach (Key key in System.Enum.GetValues(typeof(Key)))
            {
                if (keyboard[key].wasPressedThisFrame)
                {
                    char character = KeyToChar(key, keyboard.shiftKey.isPressed);
                    if (character != '\0')
                    {
                        AddToSearchQuery(character);
                    }
                }
            }
            
            // Handle backspace
            if (keyboard.backspaceKey.wasPressedThisFrame)
            {
                BackspaceSearchQuery();
            }
            
            // Handle navigation within search results
            if (keyboard.downArrowKey.wasPressedThisFrame)
            {
                NextSearchResult();
            }
            else if (keyboard.upArrowKey.wasPressedThisFrame)
            {
                PreviousSearchResult();
            }
        }
        
        private void HandleShortcuts(Keyboard keyboard)
        {
            // Check for modifier combinations
            bool ctrl = keyboard.ctrlKey.isPressed;
            bool shift = keyboard.shiftKey.isPressed;
            bool alt = keyboard.altKey.isPressed;
            
            foreach (var shortcut in _shortcuts)
            {
                if (IsShortcutPressed(shortcut.Key, keyboard, ctrl, shift, alt))
                {
                    shortcut.Value?.Invoke();
                    OnShortcutExecuted?.Invoke(shortcut.Key);
                    break;
                }
            }
        }
        
        private void UpdateSearchTimeout()
        {
            if (_isInSearchMode && Time.time - _lastSearchTime > _searchTimeout)
            {
                SetSearchMode(false);
            }
        }
        
        private void UpdateSearchResults()
        {
            _searchResults.Clear();
            _currentSearchIndex = -1;
            
            if (string.IsNullOrEmpty(_searchQuery))
                return;
            
            // Search through all navigable elements
            var allElements = FindAllSearchableElements();
            
            foreach (var element in allElements)
            {
                if (ElementMatchesSearch(element, _searchQuery))
                {
                    _searchResults.Add(element);
                }
            }
            
            // Focus first result
            if (_searchResults.Count > 0)
            {
                _currentSearchIndex = 0;
                _navigationCore.SetFocus(_searchResults[0]);
            }
            
            ChimeraLogger.Log($"[InputKeyboardHandler] Search '{_searchQuery}' found {_searchResults.Count} results");
        }
        
        private List<VisualElement> FindAllSearchableElements()
        {
            var elements = new List<VisualElement>();
            
            var rootDocument = GetComponent<UIDocument>();
            if (rootDocument != null)
            {
                var root = rootDocument.rootVisualElement;
                FindSearchableElementsRecursive(root, elements);
            }
            
            return elements;
        }
        
        private void FindSearchableElementsRecursive(VisualElement element, List<VisualElement> results)
        {
            if (element == null)
                return;
            
            // Check if element is searchable
            if (element.focusable && !string.IsNullOrEmpty(element.name))
            {
                results.Add(element);
            }
            
            // Recursively search children
            foreach (var child in element.Children())
            {
                FindSearchableElementsRecursive(child, results);
            }
        }
        
        private bool ElementMatchesSearch(VisualElement element, string query)
        {
            if (element == null || string.IsNullOrEmpty(query))
                return false;
            
            var elementText = GetElementSearchText(element);
            if (string.IsNullOrEmpty(elementText))
                return false;
            
            var comparison = _searchCaseSensitive ? StringComparison.Ordinal : StringComparison.OrdinalIgnoreCase;
            
            if (_searchPartialMatch)
            {
                return elementText.IndexOf(query, comparison) >= 0;
            }
            else
            {
                return elementText.Equals(query, comparison);
            }
        }
        
        private string GetElementSearchText(VisualElement element)
        {
            // Try to get text from various sources
            if (element is Label label)
                return label.text;
            if (element is Button button)
                return button.text;
            if (element is TextField textField)
                return textField.value;
            
            // Fall back to element name
            return element.name;
        }
        
        private void StartSearchMode()
        {
            ChimeraLogger.Log("[InputKeyboardHandler] Search mode activated");
            // Additional UI changes could be made here
        }
        
        private void EndSearchMode()
        {
            _searchQuery = "";
            _searchResults.Clear();
            _currentSearchIndex = -1;
            ChimeraLogger.Log("[InputKeyboardHandler] Search mode deactivated");
        }
        
        private char KeyToChar(Key key, bool shift)
        {
            // Handle letters
            if (key >= Key.A && key <= Key.Z)
            {
                char baseChar = (char)('a' + (key - Key.A));
                return shift ? char.ToUpper(baseChar) : baseChar;
            }
            
            // Handle digits
            if (key >= Key.Digit0 && key <= Key.Digit9)
            {
                if (shift)
                {
                    // Shift+digit special characters
                    switch (key)
                    {
                        case Key.Digit1: return '!';
                        case Key.Digit2: return '@';
                        case Key.Digit3: return '#';
                        case Key.Digit4: return '$';
                        case Key.Digit5: return '%';
                        case Key.Digit6: return '^';
                        case Key.Digit7: return '&';
                        case Key.Digit8: return '*';
                        case Key.Digit9: return '(';
                        case Key.Digit0: return ')';
                    }
                }
                return (char)('0' + (key - Key.Digit0));
            }
            
            // Handle space
            if (key == Key.Space)
                return ' ';
            
            return '\0'; // Unsupported key
        }
        
        private string NormalizeShortcutKey(string keyCombo)
        {
            return keyCombo.ToLowerInvariant().Replace(" ", "");
        }
        
        private bool IsShortcutPressed(string shortcut, Keyboard keyboard, bool ctrl, bool shift, bool alt)
        {
            var normalized = NormalizeShortcutKey(shortcut);
            
            // Simple implementation - could be expanded for complex combinations
            if (normalized.Contains("ctrl+f") && ctrl && keyboard.fKey.wasPressedThisFrame)
                return true;
            if (normalized.Contains("escape") && keyboard.escapeKey.wasPressedThisFrame)
                return true;
            
            // Handle number shortcuts
            for (int i = 1; i <= 9; i++)
            {
                if (normalized.Contains($"ctrl+{i}") && ctrl && keyboard[(Key)(Key.Digit1 + i - 1)].wasPressedThisFrame)
                    return true;
            }
            
            return false;
        }
        
        private void SelectCategoryByIndex(int index)
        {
            // Find category elements and select by index
            var categoryElements = FindAllSearchableElements()
                .Where(e => e.ClassListContains("menu-category-item"))
                .ToList();
            
            if (index >= 0 && index < categoryElements.Count)
            {
                _navigationCore.SetFocus(categoryElements[index]);
                _navigationCore.SelectCurrentElement();
            }
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
        
        // Input action handlers
        private void OnMenuToggle(InputAction.CallbackContext context)
        {
            if (IsActionOnCooldown(_menuToggleAction.action))
                return;
            
            SetActionCooldown(_menuToggleAction.action, 0.3f);
            
            if (_menuSystem != null)
            {
                if (_menuSystem.IsMenuOpen())
                {
                    _menuSystem.CloseAllMenus();
                }
                else
                {
                    var screenCenter = new Vector3(Screen.width * 0.5f, Screen.height * 0.5f, 10f);
                    var worldPosition = Camera.main?.ScreenToWorldPoint(screenCenter) ?? Vector3.zero;
                    _menuSystem.OpenContextualMenu(worldPosition);
                }
            }
        }
        
        private void OnShortcut(InputAction.CallbackContext context)
        {
            // This is handled in the Update loop
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
            else if (_menuSystem != null && _menuSystem.IsMenuOpen())
            {
                _menuSystem.CloseAllMenus();
            }
        }
        
        private void OnDestroy()
        {
            // Cleanup input actions
            if (_menuToggleAction != null)
                _menuToggleAction.action.performed -= OnMenuToggle;
            if (_shortcutAction != null)
                _shortcutAction.action.performed -= OnShortcut;
            if (_cancelAction != null)
                _cancelAction.action.performed -= OnCancel;
            
            ChimeraLogger.Log("[InputKeyboardHandler] Keyboard handler cleanup complete");
        }
    }
#endif
}