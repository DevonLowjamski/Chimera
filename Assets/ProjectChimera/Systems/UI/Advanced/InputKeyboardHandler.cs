using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;

namespace ProjectChimera.Systems.UI.Advanced
{
    /// <summary>
    /// BASIC: Simple keyboard input handling for Project Chimera's UI system.
    /// Focuses on essential keyboard operations without complex shortcut management and search functionality.
    /// </summary>
    public class InputKeyboardHandler : MonoBehaviour, ITickable
    {
        [Header("Basic Keyboard Settings")]
        [SerializeField] private bool _enableBasicKeyboard = true;
        [SerializeField] private KeyCode _menuToggleKey = KeyCode.Escape;
        [SerializeField] private KeyCode _confirmKey = KeyCode.Return;
        [SerializeField] private KeyCode _cancelKey = KeyCode.Escape;
        [SerializeField] private float _keyRepeatDelay = 0.2f;

        // Basic keyboard state
        private readonly Dictionary<KeyCode, float> _keyPressTimes = new Dictionary<KeyCode, float>();
        private readonly Dictionary<KeyCode, System.Action> _keyActions = new Dictionary<KeyCode, System.Action>();
        private bool _isInitialized = false;

        /// <summary>
        /// Events for keyboard actions
        /// </summary>
        public event System.Action<KeyCode> OnKeyPressed;
        public event System.Action OnMenuToggle;
        public event System.Action OnConfirm;
        public event System.Action OnCancel;

        /// <summary>
        /// Initialize basic keyboard handling
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            // Setup default key actions
            _keyActions[_menuToggleKey] = () => OnMenuToggle?.Invoke();
            _keyActions[_confirmKey] = () => OnConfirm?.Invoke();
            _keyActions[_cancelKey] = () => OnCancel?.Invoke();

            _isInitialized = true;
        }

        /// <summary>
        /// Update keyboard input processing
        /// </summary>
    [SerializeField] private float _tickInterval = 0.1f; // Configurable update frequency
    private float _lastTickTime;

    public int TickPriority => 50; // Lower priority for complex updates
    public bool IsTickable => enabled && gameObject.activeInHierarchy;

    public void Tick(float deltaTime)
    {
        _lastTickTime += deltaTime;
        if (_lastTickTime < _tickInterval) return;
        _lastTickTime = 0f;

        if (!_enableBasicKeyboard || !_isInitialized) return;

        foreach (var kvp in _keyActions)
        {
            var key = kvp.Key;
            if (Input.GetKeyDown(key))
            {
                _keyPressTimes[key] = Time.time;
                OnKeyPressed?.Invoke(key);
                kvp.Value?.Invoke();
            }
            else if (Input.GetKey(key) && _keyPressTimes.ContainsKey(key))
            {
                var timeSincePress = Time.time - _keyPressTimes[key];
                if (timeSincePress >= _keyRepeatDelay)
                {
                    _keyPressTimes[key] = Time.time;
                    OnKeyPressed?.Invoke(key);
                    kvp.Value?.Invoke();
                }
            }
        }

        var keysToRemove = new List<KeyCode>();
        foreach (var press in _keyPressTimes)
        {
            if (!Input.GetKey(press.Key))
            {
                keysToRemove.Add(press.Key);
            }
        }
        foreach (var key in keysToRemove)
        {
            _keyPressTimes.Remove(key);
        }
    }

    private void OnEnable()
    {
        ProjectChimera.Core.Updates.UpdateOrchestrator.Instance?.RegisterTickable(this);
    }

    private void OnDisable()
    {
        ProjectChimera.Core.Updates.UpdateOrchestrator.Instance?.UnregisterTickable(this);
    }

        /// <summary>
        /// Register a custom key action
        /// </summary>
        public void RegisterKeyAction(KeyCode key, System.Action action)
        {
            _keyActions[key] = action;
        }

        /// <summary>
        /// Unregister a key action
        /// </summary>
        public void UnregisterKeyAction(KeyCode key)
        {
            _keyActions.Remove(key);
            _keyPressTimes.Remove(key);
        }

        /// <summary>
        /// Check if a key is currently pressed
        /// </summary>
        public bool IsKeyPressed(KeyCode key)
        {
            return Input.GetKey(key);
        }

        /// <summary>
        /// Check if a key was just pressed
        /// </summary>
        public bool IsKeyPressedDown(KeyCode key)
        {
            return Input.GetKeyDown(key);
        }

        /// <summary>
        /// Get all registered keys
        /// </summary>
        public List<KeyCode> GetRegisteredKeys()
        {
            return new List<KeyCode>(_keyActions.Keys);
        }

        /// <summary>
        /// Clear all key actions
        /// </summary>
        public void ClearAllKeyActions()
        {
            _keyActions.Clear();
            _keyPressTimes.Clear();
        }

        /// <summary>
        /// Get keyboard input statistics
        /// </summary>
        public KeyboardStats GetKeyboardStats()
        {
            return new KeyboardStats
            {
                RegisteredKeys = _keyActions.Count,
                ActiveKeys = _keyPressTimes.Count,
                IsKeyboardEnabled = _enableBasicKeyboard,
                MenuToggleKey = _menuToggleKey,
                ConfirmKey = _confirmKey,
                CancelKey = _cancelKey
            };
        }

        /// <summary>
        /// Handle menu navigation keys
        /// </summary>
        public void HandleNavigationKeys(out bool up, out bool down, out bool left, out bool right)
        {
            up = Input.GetKeyDown(KeyCode.UpArrow) || Input.GetKeyDown(KeyCode.W);
            down = Input.GetKeyDown(KeyCode.DownArrow) || Input.GetKeyDown(KeyCode.S);
            left = Input.GetKeyDown(KeyCode.LeftArrow) || Input.GetKeyDown(KeyCode.A);
            right = Input.GetKeyDown(KeyCode.RightArrow) || Input.GetKeyDown(KeyCode.D);
        }

        /// <summary>
        /// Handle tab navigation
        /// </summary>
        public void HandleTabNavigation(out bool next, out bool previous)
        {
            next = Input.GetKeyDown(KeyCode.Tab) && !Input.GetKey(KeyCode.LeftShift);
            previous = Input.GetKeyDown(KeyCode.Tab) && Input.GetKey(KeyCode.LeftShift);
        }
    }

    /// <summary>
    /// Keyboard statistics
    /// </summary>
    [System.Serializable]
    public struct KeyboardStats
    {
        public int RegisteredKeys;
        public int ActiveKeys;
        public bool IsKeyboardEnabled;
        public KeyCode MenuToggleKey;
        public KeyCode ConfirmKey;
        public KeyCode CancelKey;
    }
}
