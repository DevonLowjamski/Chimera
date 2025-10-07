using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;

namespace ProjectChimera.Core.Input
{
    /// <summary>
    /// REFACTORED: Legacy Input Poller
    /// Single Responsibility: Polling Unity's legacy Input system at optimized intervals
    /// Extracted from OptimizedInputManager for better separation of concerns
    /// </summary>
    public class LegacyInputPoller : MonoBehaviour, ITickable
    {
        [Header("Polling Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private float _inputPollingRate = 60f; // Hz
        [SerializeField] private float _mouseDeltaThreshold = 0.01f;
        [SerializeField] private float _tickInterval = 0.1f;

        [Header("Input Tracking")]
        [SerializeField] private bool _trackMouseInput = true;
        [SerializeField] private bool _trackKeyboardInput = true;
        [SerializeField] private bool _trackScrollInput = true;

        // Input state tracking
        private readonly Dictionary<string, InputState> _inputStates = new Dictionary<string, InputState>();
        private Vector2 _lastMousePosition;
        private float _lastInputPollTime;
        private float _lastTickTime;

        // State tracking
        private bool _isInitialized = false;
        private bool _inputEnabled = true;

        // Statistics
        private LegacyInputPollerStats _stats = new LegacyInputPollerStats();

        // Events
        public event System.Action<InputEvent> OnInputEventDetected;
        public event System.Action<InputState> OnInputStateChanged;

        // ITickable implementation
        public int TickPriority => 50;
        public bool IsTickable => enabled && gameObject.activeInHierarchy && _inputEnabled;

        public bool IsInitialized => _isInitialized;
        public LegacyInputPollerStats Stats => _stats;
        public bool IsInputEnabled => _inputEnabled;

        public void Initialize()
        {
            if (_isInitialized) return;

            InitializeInputStates();
            _lastMousePosition = UnityEngine.Input.mousePosition;
            _lastInputPollTime = Time.unscaledTime;
            _lastTickTime = 0f;

            ResetStats();

            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("INPUT", "Legacy Input Poller initialized", this);
            }
        }

        public void Tick(float deltaTime)
        {
            if (!_isInitialized || !_inputEnabled) return;

            _lastTickTime += deltaTime;
            if (_lastTickTime >= _tickInterval)
            {
                _lastTickTime = 0f;
                UpdateInputPolling();
            }
        }

        /// <summary>
        /// Set input enabled/disabled
        /// </summary>
        public void SetInputEnabled(bool enabled)
        {
            _inputEnabled = enabled;

            if (_enableLogging)
            {
                ChimeraLogger.Log("INPUT", $"Legacy input polling {(enabled ? "enabled" : "disabled")}", this);
            }
        }

        /// <summary>
        /// Set polling rate
        /// </summary>
        public void SetPollingRate(float pollingRate)
        {
            _inputPollingRate = Mathf.Max(1f, pollingRate);

            if (_enableLogging)
            {
                ChimeraLogger.Log("INPUT", $"Input polling rate set to {_inputPollingRate} Hz", this);
            }
        }

        /// <summary>
        /// Get input state for device
        /// </summary>
        public InputState GetInputState(string inputDevice)
        {
            return _inputStates.TryGetValue(inputDevice, out var state) ? state : new InputState();
        }

        /// <summary>
        /// Get all input states
        /// </summary>
        public Dictionary<string, InputState> GetAllInputStates()
        {
            return new Dictionary<string, InputState>(_inputStates);
        }

        /// <summary>
        /// Set input tracking options
        /// </summary>
        public void SetInputTracking(bool mouse, bool keyboard, bool scroll)
        {
            _trackMouseInput = mouse;
            _trackKeyboardInput = keyboard;
            _trackScrollInput = scroll;

            if (_enableLogging)
            {
                ChimeraLogger.Log("INPUT", $"Input tracking updated: Mouse={mouse}, Keyboard={keyboard}, Scroll={scroll}", this);
            }
        }

        /// <summary>
        /// Force input poll
        /// </summary>
        [ContextMenu("Force Input Poll")]
        public void ForceInputPoll()
        {
            if (_isInitialized)
            {
                PollInputDevices();

                if (_enableLogging)
                {
                    ChimeraLogger.Log("INPUT", "Forced input poll completed", this);
                }
            }
        }

        #region Private Methods

        /// <summary>
        /// Initialize input state tracking
        /// </summary>
        private void InitializeInputStates()
        {
            _inputStates["Mouse"] = new InputState
            {
                IsActive = true,
                LastUpdateTime = Time.unscaledTime,
                LastPosition = UnityEngine.Input.mousePosition
            };

            _inputStates["Keyboard"] = new InputState
            {
                IsActive = true,
                LastUpdateTime = Time.unscaledTime
            };

            _inputStates["Scroll"] = new InputState
            {
                IsActive = true,
                LastUpdateTime = Time.unscaledTime
            };
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
                _stats.PollingCycles++;
            }
        }

        /// <summary>
        /// Poll input devices efficiently
        /// </summary>
        private void PollInputDevices()
        {
            var pollStartTime = Time.realtimeSinceStartup;

            // Mouse polling
            if (_trackMouseInput)
            {
                PollMouseInput();
            }

            // Keyboard polling
            if (_trackKeyboardInput)
            {
                PollKeyboardInput();
            }

            // Scroll polling
            if (_trackScrollInput)
            {
                PollScrollInput();
            }

            // Update statistics
            var pollTime = Time.realtimeSinceStartup - pollStartTime;
            _stats.TotalPollingTime += pollTime;
            _stats.AveragePollingTime = _stats.PollingCycles > 0 ? _stats.TotalPollingTime / _stats.PollingCycles : 0f;

            if (pollTime > _stats.MaxPollingTime)
                _stats.MaxPollingTime = pollTime;
        }

        /// <summary>
        /// Poll mouse input
        /// </summary>
        private void PollMouseInput()
        {
            var mouseState = _inputStates["Mouse"];
            Vector2 currentMousePos = UnityEngine.Input.mousePosition;
            Vector2 mouseDelta = currentMousePos - _lastMousePosition;

            // Mouse movement
            if (mouseDelta.magnitude > _mouseDeltaThreshold)
            {
                var inputEvent = new InputEvent
                {
                    Type = InputEventType.MouseMove,
                    MousePosition = currentMousePos,
                    MouseDelta = mouseDelta,
                    Timestamp = Time.unscaledTime
                };

                OnInputEventDetected?.Invoke(inputEvent);
                _stats.MouseEvents++;

                _lastMousePosition = currentMousePos;
            }

            // Mouse clicks
            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                var inputEvent = new InputEvent
                {
                    Type = InputEventType.MouseClick,
                    MousePosition = currentMousePos,
                    Timestamp = Time.unscaledTime
                };

                OnInputEventDetected?.Invoke(inputEvent);
                _stats.MouseEvents++;
            }

            if (UnityEngine.Input.GetMouseButtonUp(0))
            {
                var inputEvent = new InputEvent
                {
                    Type = InputEventType.MouseRelease,
                    MousePosition = currentMousePos,
                    Timestamp = Time.unscaledTime
                };

                OnInputEventDetected?.Invoke(inputEvent);
                _stats.MouseEvents++;
            }

            // Update mouse state
            mouseState.LastUpdateTime = Time.unscaledTime;
            mouseState.LastPosition = currentMousePos;
            _inputStates["Mouse"] = mouseState;

            OnInputStateChanged?.Invoke(mouseState);
        }

        /// <summary>
        /// Poll keyboard input (simplified - could be expanded for specific keys)
        /// </summary>
        private void PollKeyboardInput()
        {
            var keyboardState = _inputStates["Keyboard"];

            // Check for any key input (simplified approach)
            if (UnityEngine.Input.inputString.Length > 0)
            {
                foreach (char c in UnityEngine.Input.inputString)
                {
                    if (c != '\b' && c != '\n' && c != '\r') // Ignore backspace, newline, carriage return
                    {
                        var inputEvent = new InputEvent
                        {
                            Type = InputEventType.KeyPress,
                            KeyCode = (KeyCode)c,
                            Timestamp = Time.unscaledTime
                        };

                        OnInputEventDetected?.Invoke(inputEvent);
                        _stats.KeyboardEvents++;
                    }
                }
            }

            // Update keyboard state
            keyboardState.LastUpdateTime = Time.unscaledTime;
            _inputStates["Keyboard"] = keyboardState;

            OnInputStateChanged?.Invoke(keyboardState);
        }

        /// <summary>
        /// Poll scroll input
        /// </summary>
        private void PollScrollInput()
        {
            var scrollState = _inputStates["Scroll"];
            float scrollDelta = UnityEngine.Input.GetAxis("Mouse ScrollWheel");

            if (Mathf.Abs(scrollDelta) > 0.01f)
            {
                var inputEvent = new InputEvent
                {
                    Type = InputEventType.ScrollWheel,
                    ScrollDelta = scrollDelta,
                    MousePosition = UnityEngine.Input.mousePosition,
                    Timestamp = Time.unscaledTime
                };

                OnInputEventDetected?.Invoke(inputEvent);
                _stats.ScrollEvents++;

                // Update scroll state
                scrollState.LastValue = scrollDelta;
            }

            scrollState.LastUpdateTime = Time.unscaledTime;
            _inputStates["Scroll"] = scrollState;

            OnInputStateChanged?.Invoke(scrollState);
        }

        /// <summary>
        /// Reset statistics
        /// </summary>
        private void ResetStats()
        {
            _stats = new LegacyInputPollerStats
            {
                PollingCycles = 0,
                MouseEvents = 0,
                KeyboardEvents = 0,
                ScrollEvents = 0,
                TotalPollingTime = 0f,
                AveragePollingTime = 0f,
                MaxPollingTime = 0f
            };
        }

        #endregion

        private void OnDestroy()
        {
            UpdateOrchestrator.Instance?.UnregisterTickable(this);
        }
    }

    /// <summary>
    /// Legacy input poller statistics
    /// </summary>
    [System.Serializable]
    public struct LegacyInputPollerStats
    {
        public int PollingCycles;
        public int MouseEvents;
        public int KeyboardEvents;
        public int ScrollEvents;
        public float TotalPollingTime;
        public float AveragePollingTime;
        public float MaxPollingTime;
    }
}
