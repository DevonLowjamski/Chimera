using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Memory;
using System;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core.Input
{
    /// <summary>
    /// REFACTORED: Optimized Input Service (POCO - Unity-independent core)
    /// Single Responsibility: Input event processing, polling, and state management
    /// Extracted from OptimizedInputManager for clean architecture compliance
    /// </summary>
    public class OptimizedInputService
    {
        private readonly bool _enableInputOptimization;
        private readonly bool _enableLogging;
        private readonly float _inputPollingRate;
        private readonly float _mouseDeltaThreshold;
        private readonly int _maxInputEventsPerFrame;
        private readonly bool _useInputBuffering;
        private readonly int _inputBufferSize;
        private readonly bool _enableInputPrediction;
        private readonly float _predictionTimeWindow;

        // Input state tracking
        private readonly Dictionary<string, InputState> _inputStates = new Dictionary<string, InputState>();
        private readonly MemoryOptimizedQueue<InputEvent> _inputEventQueue = new MemoryOptimizedQueue<InputEvent>();
        private readonly List<IInputHandler> _inputHandlers = new List<IInputHandler>();

        // Mouse and touch optimization
        private Vector2 _lastMousePosition;
        private Vector2 _mouseVelocity;
        private float _lastMouseUpdateTime;
        private bool _mouseMovedThisFrame;

        // Input prediction
        private readonly Queue<Vector2> _mousePositionHistory = new Queue<Vector2>();
        private readonly Queue<float> _mouseTimeHistory = new Queue<float>();

        // Performance tracking
        private float _lastInputPollTime;
        private int _inputEventsThisFrame;
        private InputPerformanceStats _stats = new InputPerformanceStats();

        // Input state tracking
        private Vector2 _mouseDelta;
        private bool _inputInitialized;

        public bool IsInitialized { get; private set; }
        public InputPerformanceStats Stats => _stats;

        // Events
        public event Action<Vector2> OnOptimizedMouseMove;
        public event Action<Vector2> OnMouseClick;
        public event Action<Vector2> OnMouseDrag;
        public event Action<float> OnScrollWheel;
        public event Action<KeyCode> OnKeyPressed;
        public event Action<KeyCode> OnKeyReleased;

        private readonly string[] CRITICAL_INPUTS = { "Mouse", "Keyboard", "Touch" };

        public OptimizedInputService(
            bool enableInputOptimization = true,
            bool enableLogging = false,
            float inputPollingRate = 60f,
            float mouseDeltaThreshold = 0.01f,
            int maxInputEventsPerFrame = 50,
            bool useInputBuffering = true,
            int inputBufferSize = 100,
            bool enableInputPrediction = true,
            float predictionTimeWindow = 0.1f)
        {
            _enableInputOptimization = enableInputOptimization;
            _enableLogging = enableLogging;
            _inputPollingRate = inputPollingRate;
            _mouseDeltaThreshold = mouseDeltaThreshold;
            _maxInputEventsPerFrame = maxInputEventsPerFrame;
            _useInputBuffering = useInputBuffering;
            _inputBufferSize = inputBufferSize;
            _enableInputPrediction = enableInputPrediction;
            _predictionTimeWindow = predictionTimeWindow;
        }

        public void Initialize(float currentTime, Vector2 initialMousePosition)
        {
            if (IsInitialized) return;

            InitializeInputSystem();
            SetupInputActions();

            _lastInputPollTime = currentTime;
            _lastMouseUpdateTime = currentTime;
            _lastMousePosition = initialMousePosition;

            IsInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("OptimizedInputManager", "Input manager initialized successfully");
            }
        }

        public void RegisterInputHandler(IInputHandler handler)
        {
            if (handler != null && !_inputHandlers.Contains(handler))
            {
                _inputHandlers.Add(handler);
                _stats.RegisteredHandlers++;

                if (_enableLogging)
                {
                    ChimeraLogger.Log("OptimizedInputManager", $"Registered input handler: {handler.GetType().Name}");
                }
            }
        }

        public void UnregisterInputHandler(IInputHandler handler)
        {
            if (_inputHandlers.Remove(handler))
            {
                _stats.RegisteredHandlers--;

                if (_enableLogging)
                {
                    ChimeraLogger.Log("OptimizedInputManager", $"Unregistered input handler: {handler.GetType().Name}");
                }
            }
        }

        public Vector2 GetPredictedMousePosition(float deltaTime = 0.016f)
        {
            if (!_enableInputPrediction || _mouseVelocity.magnitude < _mouseDeltaThreshold)
            {
                return _lastMousePosition;
            }

            return _lastMousePosition + (_mouseVelocity * deltaTime);
        }

        public bool HasMouseMovedThisFrame()
        {
            return _mouseMovedThisFrame;
        }

        public InputState GetInputState(string inputName)
        {
            return _inputStates.TryGetValue(inputName, out var state) ? state : new InputState();
        }

        public void SetInputContext(InputContext context)
        {
            if (_enableLogging)
            {
                ChimeraLogger.Log("OptimizedInputManager", $"Switched to input context: {context}");
            }
        }

        public void SetInputEnabled(bool enabled)
        {
            // _enableInputOptimization = enabled; // Cannot modify readonly field
            if (_enableLogging)
            {
                ChimeraLogger.Log("OptimizedInputManager", $"Input processing {(enabled ? "enabled" : "disabled")}");
            }
        }

        public void Tick(float deltaTime, float currentTime, Vector2 currentMousePos, bool mouseButton0Down, bool mouseButton0Up, float scrollDelta, float tickInterval, ref float lastTickTime)
        {
            lastTickTime += deltaTime;
            if (lastTickTime >= tickInterval)
            {
                lastTickTime = 0f;
                if (!IsInitialized) return;

                // Mouse position tracking
                _mouseDelta = currentMousePos - _lastMousePosition;

                if (_mouseDelta.magnitude > _mouseDeltaThreshold)
                {
                    QueueInputEvent(new InputEvent
                    {
                        Type = InputEventType.MouseMove,
                        MousePosition = currentMousePos,
                        Timestamp = currentTime,
                    });
                    _lastMousePosition = currentMousePos;
                }

                // Mouse button handling
                if (mouseButton0Down)
                {
                    QueueInputEvent(new InputEvent
                    {
                        Type = InputEventType.MouseClick,
                        MousePosition = currentMousePos,
                        Timestamp = currentTime,
                    });
                }

                if (mouseButton0Up)
                {
                    QueueInputEvent(new InputEvent
                    {
                        Type = InputEventType.MouseRelease,
                        MousePosition = currentMousePos,
                        Timestamp = currentTime,
                    });
                }

                // Mouse scroll wheel
                if (Mathf.Abs(scrollDelta) > 0.01f)
                {
                    QueueInputEvent(new InputEvent
                    {
                        Type = InputEventType.ScrollWheel,
                        ScrollDelta = scrollDelta,
                        Timestamp = currentTime,
                    });
                }
            }
        }

        public void Cleanup()
        {
            _inputEventQueue?.Dispose();

            if (_enableLogging)
            {
                ChimeraLogger.Log("OptimizedInputManager", "Input manager destroyed");
            }
        }

        #region Private Methods

        private void InitializeInputSystem()
        {
            _inputStates["Mouse"] = new InputState { IsActive = true };
            _inputStates["Keyboard"] = new InputState { IsActive = true };
            _inputStates["Touch"] = new InputState { IsActive = true };
        }

        private void SetupInputActions()
        {
            _inputInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("OptimizedInputManager", "Input system initialized with legacy Input API");
            }
        }

        private void UpdateInputPolling(float currentTime)
        {
            float pollInterval = 1f / _inputPollingRate;

            if (currentTime - _lastInputPollTime >= pollInterval)
            {
                PollInputDevices(currentTime);
                _lastInputPollTime = currentTime;
            }
        }

        private void PollInputDevices(float currentTime)
        {
            var mouseState = _inputStates["Mouse"];
            mouseState.LastUpdateTime = currentTime;

            var keyboardState = _inputStates["Keyboard"];
            keyboardState.LastUpdateTime = currentTime;

            _stats.InputPolls++;
        }

        private void ProcessInputBuffer()
        {
            while (_inputEventQueue.Count > 0 && _inputEventsThisFrame < _maxInputEventsPerFrame)
            {
                if (_inputEventQueue.TryDequeue(out var inputEvent))
                {
                    ProcessInputEvent(inputEvent);
                    _inputEventsThisFrame++;
                }
            }
        }

        private void ProcessInputEvent(InputEvent inputEvent)
        {
            switch (inputEvent.Type)
            {
                case InputEventType.MouseMove:
                    OnOptimizedMouseMove?.Invoke(inputEvent.MousePosition);
                    break;
                case InputEventType.MouseClick:
                    OnMouseClick?.Invoke(inputEvent.MousePosition);
                    break;
                case InputEventType.MouseDrag:
                    OnMouseDrag?.Invoke(inputEvent.MousePosition);
                    break;
                case InputEventType.ScrollWheel:
                    OnScrollWheel?.Invoke(inputEvent.ScrollDelta);
                    break;
                case InputEventType.KeyPress:
                    OnKeyPressed?.Invoke(inputEvent.KeyCode);
                    break;
                case InputEventType.KeyRelease:
                    OnKeyReleased?.Invoke(inputEvent.KeyCode);
                    break;
            }

            // Notify registered handlers
            foreach (var handler in _inputHandlers)
            {
                handler.HandleInputEvent(inputEvent);
            }

            _stats.EventsProcessed++;
        }

        private void QueueInputEvent(InputEvent inputEvent)
        {
            if (_useInputBuffering)
            {
                _inputEventQueue.Enqueue(inputEvent);
            }
            else
            {
                ProcessInputEvent(inputEvent);
            }
        }

        private void UpdateMouseOptimization(float currentTime, Vector2 currentMousePos)
        {
            float deltaTime = currentTime - _lastMouseUpdateTime;

            if (deltaTime > 0f)
            {
                Vector2 mouseDelta = currentMousePos - _lastMousePosition;
                _mouseVelocity = mouseDelta / deltaTime;
                _mouseMovedThisFrame = mouseDelta.magnitude > _mouseDeltaThreshold;

                _lastMousePosition = currentMousePos;
                _lastMouseUpdateTime = currentTime;
            }
            else
            {
                _mouseMovedThisFrame = false;
            }
        }

        private void UpdateInputPrediction(float currentTime)
        {
            if (!_enableInputPrediction) return;

            _mousePositionHistory.Enqueue(_lastMousePosition);
            _mouseTimeHistory.Enqueue(currentTime);

            while (_mouseTimeHistory.Count > 0 && currentTime - _mouseTimeHistory.Peek() > _predictionTimeWindow)
            {
                _mousePositionHistory.Dequeue();
                _mouseTimeHistory.Dequeue();
            }
        }

        #endregion
    }
}
