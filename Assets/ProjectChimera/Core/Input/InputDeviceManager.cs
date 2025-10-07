using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;

namespace ProjectChimera.Core.Input
{
    /// <summary>
    /// REFACTORED: Input Device Manager - Focused input device polling and state management
    /// Handles direct input device polling and raw input state tracking
    /// Single Responsibility: Input device polling and state management
    /// </summary>
    public class InputDeviceManager : MonoBehaviour, ITickable
    {
        [Header("Device Polling Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private float _inputPollingRate = 60f; // Hz
        [SerializeField] private float _mouseDeltaThreshold = 0.01f;

        // Input state tracking
        private readonly Dictionary<string, InputState> _inputStates = new Dictionary<string, InputState>();

        // Mouse state tracking
        private Vector2 _lastMousePosition;
        private Vector2 _currentMousePosition;
        private Vector2 _mouseDelta;
        private bool _mouseMovedThisFrame;

        // Timing
        private float _lastInputPollTime;

        // Statistics
        private InputDeviceStats _stats = new InputDeviceStats();

        // Properties
        public bool IsEnabled { get; private set; } = true;
        public Vector2 CurrentMousePosition => _currentMousePosition;
        public Vector2 MouseDelta => _mouseDelta;
        public bool MouseMovedThisFrame => _mouseMovedThisFrame;
        public InputDeviceStats Stats => _stats;

        // Events
        public System.Action<Vector2, Vector2> OnMousePositionChanged;
        public System.Action<KeyCode, bool> OnKeyStateChanged;
        public System.Action<int, bool> OnMouseButtonStateChanged;
        public System.Action<float> OnScrollWheelChanged;

        private void Start()
        {
            Initialize();
            UpdateOrchestrator.Instance.RegisterTickable(this);
        }

        private void OnDestroy()
        {
            if (UpdateOrchestrator.Instance != null)
            {
                UpdateOrchestrator.Instance.UnregisterTickable(this);
            }
        }

        #region ITickable Implementation

        /// <summary>
        /// Input polling priority - runs very early for responsive input
        /// </summary>
        public int TickPriority => -45;

        /// <summary>
        /// Whether this tickable should be updated this frame
        /// </summary>
        public bool IsTickable => IsEnabled && enabled;

        // Compatibility alias for older call sites
        public bool ShouldTick => IsTickable;

        public void Tick(float deltaTime)
        {
            UpdateInputPolling();
        }

        #endregion

        /// <summary>
        /// Initialize input device manager
        /// </summary>
        private void Initialize()
        {
            InitializeInputStates();
            _lastMousePosition = UnityEngine.Input.mousePosition;
            _currentMousePosition = _lastMousePosition;
            _lastInputPollTime = Time.unscaledTime;

            if (_enableLogging)
                ChimeraLogger.Log("INPUT", "🎮 InputDeviceManager initialized", this);
        }

        /// <summary>
        /// Initialize input state tracking
        /// </summary>
        private void InitializeInputStates()
        {
            _inputStates["Mouse"] = new InputState { IsActive = true };
            _inputStates["Keyboard"] = new InputState { IsActive = true };
            _inputStates["Touch"] = new InputState { IsActive = true };
        }

        /// <summary>
        /// Update input polling at controlled rate
        /// </summary>
        private void UpdateInputPolling()
        {
            float currentTime = Time.unscaledTime;
            float pollInterval = 1f / _inputPollingRate;

            if (currentTime - _lastInputPollTime >= pollInterval)
            {
                PollInputDevices();
                _lastInputPollTime = currentTime;
            }
        }

        /// <summary>
        /// Poll all input devices efficiently
        /// </summary>
        private void PollInputDevices()
        {
            PollMouseInput();
            PollKeyboardInput();
            PollMouseButtons();
            PollScrollWheel();

            _stats.InputPolls++;
        }

        /// <summary>
        /// Poll mouse input
        /// </summary>
        private void PollMouseInput()
        {
            var mouseState = _inputStates["Mouse"];
            mouseState.LastUpdateTime = Time.unscaledTime;

            _currentMousePosition = UnityEngine.Input.mousePosition;
            _mouseDelta = _currentMousePosition - _lastMousePosition;
            _mouseMovedThisFrame = _mouseDelta.magnitude > _mouseDeltaThreshold;

            if (_mouseMovedThisFrame)
            {
                OnMousePositionChanged?.Invoke(_currentMousePosition, _mouseDelta);
                _stats.MouseMovements++;
            }

            _lastMousePosition = _currentMousePosition;
            _inputStates["Mouse"] = mouseState;
        }

        /// <summary>
        /// Poll keyboard input (simplified - monitors common keys)
        /// </summary>
        private void PollKeyboardInput()
        {
            var keyboardState = _inputStates["Keyboard"];
            keyboardState.LastUpdateTime = Time.unscaledTime;

            // Poll common keys (can be expanded as needed)
            CheckKeyState(KeyCode.Escape);
            CheckKeyState(KeyCode.Tab);
            CheckKeyState(KeyCode.Space);
            CheckKeyState(KeyCode.Return);
            CheckKeyState(KeyCode.LeftShift);
            CheckKeyState(KeyCode.LeftControl);
            CheckKeyState(KeyCode.LeftAlt);

            _inputStates["Keyboard"] = keyboardState;
        }

        /// <summary>
        /// Check individual key state
        /// </summary>
        private void CheckKeyState(KeyCode keyCode)
        {
            if (UnityEngine.Input.GetKeyDown(keyCode))
            {
                OnKeyStateChanged?.Invoke(keyCode, true);
                _stats.KeyPresses++;
            }
            else if (UnityEngine.Input.GetKeyUp(keyCode))
            {
                OnKeyStateChanged?.Invoke(keyCode, false);
                _stats.KeyReleases++;
            }
        }

        /// <summary>
        /// Poll mouse buttons
        /// </summary>
        private void PollMouseButtons()
        {
            // Left mouse button (0)
            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                OnMouseButtonStateChanged?.Invoke(0, true);
                _stats.MouseClicks++;
            }
            else if (UnityEngine.Input.GetMouseButtonUp(0))
            {
                OnMouseButtonStateChanged?.Invoke(0, false);
            }

            // Right mouse button (1)
            if (UnityEngine.Input.GetMouseButtonDown(1))
            {
                OnMouseButtonStateChanged?.Invoke(1, true);
                _stats.MouseClicks++;
            }
            else if (UnityEngine.Input.GetMouseButtonUp(1))
            {
                OnMouseButtonStateChanged?.Invoke(1, false);
            }

            // Middle mouse button (2)
            if (UnityEngine.Input.GetMouseButtonDown(2))
            {
                OnMouseButtonStateChanged?.Invoke(2, true);
                _stats.MouseClicks++;
            }
            else if (UnityEngine.Input.GetMouseButtonUp(2))
            {
                OnMouseButtonStateChanged?.Invoke(2, false);
            }
        }

        /// <summary>
        /// Poll scroll wheel
        /// </summary>
        private void PollScrollWheel()
        {
            float scrollDelta = UnityEngine.Input.GetAxis("Mouse ScrollWheel");
            if (Mathf.Abs(scrollDelta) > 0.01f)
            {
                OnScrollWheelChanged?.Invoke(scrollDelta);
                _stats.ScrollEvents++;
            }
        }

        /// <summary>
        /// Get input state for device
        /// </summary>
        public InputState GetInputState(string deviceName)
        {
            return _inputStates.TryGetValue(deviceName, out var state) ? state : new InputState();
        }

        /// <summary>
        /// Set input device enabled/disabled
        /// </summary>
        public void SetDeviceEnabled(string deviceName, bool enabled)
        {
            if (_inputStates.TryGetValue(deviceName, out var state))
            {
                state.IsActive = enabled;
                _inputStates[deviceName] = state;

                if (_enableLogging)
                    ChimeraLogger.Log("INPUT", $"Device {deviceName}: {(enabled ? "enabled" : "disabled")}", this);
            }
        }

        /// <summary>
        /// Set polling rate
        /// </summary>
        public void SetPollingRate(float rateHz)
        {
            _inputPollingRate = Mathf.Clamp(rateHz, 10f, 120f);

            if (_enableLogging)
                ChimeraLogger.Log("INPUT", $"Polling rate set to {_inputPollingRate} Hz", this);
        }

        /// <summary>
        /// Set mouse delta threshold
        /// </summary>
        public void SetMouseDeltaThreshold(float threshold)
        {
            _mouseDeltaThreshold = Mathf.Max(0.001f, threshold);

            if (_enableLogging)
                ChimeraLogger.Log("INPUT", $"Mouse delta threshold set to {_mouseDeltaThreshold}", this);
        }

        /// <summary>
        /// Set system enabled/disabled
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;

            if (_enableLogging)
                ChimeraLogger.Log("INPUT", $"InputDeviceManager: {(enabled ? "enabled" : "disabled")}", this);
        }

        /// <summary>
        /// Reset statistics
        /// </summary>
        public void ResetStats()
        {
            _stats = new InputDeviceStats();

            if (_enableLogging)
                ChimeraLogger.Log("INPUT", "Device statistics reset", this);
        }
    }

    #region Data Structures

    /// <summary>
    /// Input device statistics
    /// </summary>
    [System.Serializable]
    public struct InputDeviceStats
    {
        public int InputPolls;
        public int MouseMovements;
        public int MouseClicks;
        public int KeyPresses;
        public int KeyReleases;
        public int ScrollEvents;
    }

    #endregion
}
