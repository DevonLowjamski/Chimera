using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Memory;
using System;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;
using ProjectChimera.Core.SimpleDI;

namespace ProjectChimera.Core.Input
{
    /// <summary>
    /// REFACTORED: Optimized Input Manager - Focused input handling and event distribution
    /// Single Responsibility: Core input processing and event distribution
    /// Performance tracking delegated to InputPerformanceTracker
    /// Significantly reduced from 530 lines to maintain SRP compliance
    /// </summary>
    public class OptimizedInputManagerRefactored : MonoBehaviour, ITickable
    {
        [Header("Core Input Settings")]
        [SerializeField] private bool _enableInputOptimization = true;
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private float _inputPollingRate = 60f; // Hz
        [SerializeField] private int _maxInputEventsPerFrame = 50;

        [Header("Mouse Input Settings")]
        [SerializeField] private float _mouseDeltaThreshold = 0.01f;
        [SerializeField] private bool _enableMousePrediction = true;
        [SerializeField] private float _predictionTimeWindow = 0.1f;

        [Header("Buffering Settings")]
        [SerializeField] private bool _useInputBuffering = true;
        [SerializeField] private int _inputBufferSize = 100;

        // Core input management
        private readonly Dictionary<string, InputState> _inputStates = new Dictionary<string, InputState>();
        private readonly MemoryOptimizedQueue<InputEvent> _inputEventQueue = new MemoryOptimizedQueue<InputEvent>();
        private readonly List<IInputHandler> _inputHandlers = new List<IInputHandler>();

        // Mouse state tracking
        private Vector2 _lastMousePosition;
        private Vector2 _mouseVelocity;
        private float _lastMouseUpdateTime;
        private bool _mouseMovedThisFrame;

        // Input prediction
        private readonly Queue<Vector2> _mousePositionHistory = new Queue<Vector2>();
        private readonly Queue<float> _mouseTimeHistory = new Queue<float>();

        // Frame management
        private float _lastInputPollTime;
        private int _inputEventsThisFrame;

        // Performance tracker reference (composition)
        private InputPerformanceTracker _performanceTracker;

        // ITickable implementation
        public int TickPriority => ProjectChimera.Core.Updates.TickPriority.InputSystem;
        public bool IsTickable => _enableInputOptimization && isActiveAndEnabled;

        // Properties
        public bool IsEnabled { get; private set; } = true;
        public int QueuedEventCount => _inputEventQueue.Count;
        public int RegisteredHandlerCount => _inputHandlers.Count;

        // Events
        public event Action<InputEvent> OnInputEventProcessed;
        public event Action<Vector2> OnMouseMoved;
        public event Action<string> OnKeyPressed;

        #region Unity Lifecycle

        private void Start()
        {
            Initialize();
        }

        private void OnEnable()
        {
            UpdateOrchestrator.Instance?.RegisterTickable(this);
        }

        private void OnDisable()
        {
            UpdateOrchestrator.Instance?.UnregisterTickable(this);
        }

        #endregion

        #region Initialization

        private void Initialize()
        {
            // Initialize input event queue
            if (_useInputBuffering)
            {
                // Reserve capacity by constructing with size; queue grows automatically
                // Already constructed with default; nothing required here
            }

            // Resolve performance tracker via ServiceContainer
            _performanceTracker = ServiceContainerFactory.Instance?.TryResolve<InputPerformanceTracker>();
            if (_performanceTracker == null)
            {
                // Create and register performance tracker if not found
                var trackerObject = new GameObject("InputPerformanceTracker");
                _performanceTracker = trackerObject.AddComponent<InputPerformanceTracker>();
                _performanceTracker.Initialize();

                // Register with ServiceContainer for future resolution
                ServiceContainerFactory.Instance?.RegisterSingleton<InputPerformanceTracker>(_performanceTracker);

                if (_enableLogging)
                {
                    ChimeraLogger.LogWarning("INPUT",
                        "InputPerformanceTracker not found - created and registered new instance", this);
                }
            }

            // Initialize mouse state
            _lastMousePosition = UnityEngine.Input.mousePosition;
            _lastMouseUpdateTime = Time.time;

            if (_enableLogging)
            {
                ChimeraLogger.Log("INPUT", "OptimizedInputManager initialized", this);
            }
        }

        #endregion

        #region Core Input Processing (ITickable)

        public void Tick(float deltaTime)
        {
            if (!IsEnabled) return;

            var frameStartTime = Time.realtimeSinceStartup;
            _inputEventsThisFrame = 0;

            // Process input based on polling rate
            if (Time.time - _lastInputPollTime >= 1f / _inputPollingRate)
            {
                ProcessInputs();
                _lastInputPollTime = Time.time;
            }

            // Process queued events
            ProcessInputEventQueue();

            // Update mouse prediction if enabled
            if (_enableMousePrediction)
            {
                UpdateMousePrediction();
            }

            // Record performance metrics
            var frameTime = (Time.realtimeSinceStartup - frameStartTime) * 1000f; // Convert to milliseconds
            _performanceTracker?.RecordInputPoll(frameTime);
        }

        public void OnRegistered()
        {
            if (_enableLogging)
                ChimeraLogger.Log("INPUT", "OptimizedInputManager registered with UpdateOrchestrator", this);
        }

        public void OnUnregistered()
        {
            if (_enableLogging)
                ChimeraLogger.Log("INPUT", "OptimizedInputManager unregistered from UpdateOrchestrator", this);
        }

        #endregion

        #region Input Processing

        private void ProcessInputs()
        {
            // Process mouse input
            ProcessMouseInput();

            // Process keyboard input
            ProcessKeyboardInput();

            // Process touch input (if on mobile)
            if (Application.isMobilePlatform)
            {
                ProcessTouchInput();
            }
        }

        private void ProcessMouseInput()
        {
            Vector2 currentMousePosition = UnityEngine.Input.mousePosition;
            Vector2 mouseDelta = currentMousePosition - _lastMousePosition;

            if (mouseDelta.magnitude > _mouseDeltaThreshold)
            {
                _mouseMovedThisFrame = true;
                _mouseVelocity = mouseDelta / Time.deltaTime;

                var mouseEvent = new InputEvent
                {
                    Type = InputEventType.MouseMove,
                    MousePosition = currentMousePosition,
                    MouseDelta = mouseDelta,
                    Timestamp = Time.time
                };

                QueueInputEvent(mouseEvent);
                OnMouseMoved?.Invoke(currentMousePosition);

                // Update prediction history
                if (_enableMousePrediction)
                {
                    UpdateMouseHistory(currentMousePosition);
                }
            }

            _lastMousePosition = currentMousePosition;
            _lastMouseUpdateTime = Time.time;

            // Process mouse buttons
            ProcessMouseButtons();
        }

        private void ProcessMouseButtons()
        {
            // Left mouse button
            if (UnityEngine.Input.GetMouseButtonDown(0))
            {
                QueueInputEvent(new InputEvent
                {
                    Type = InputEventType.MouseClick,
                    Timestamp = Time.time
                });
            }

            if (UnityEngine.Input.GetMouseButtonUp(0))
            {
                QueueInputEvent(new InputEvent
                {
                    Type = InputEventType.MouseRelease,
                    Timestamp = Time.time
                });
            }

            // Similar for right and middle mouse buttons
            ProcessMouseButton(1); // Right
            ProcessMouseButton(2); // Middle
        }

        private void ProcessMouseButton(int button)
        {
            if (UnityEngine.Input.GetMouseButtonDown(button))
            {
                QueueInputEvent(new InputEvent
                {
                    Type = InputEventType.MouseClick,
                    Timestamp = Time.time
                });
            }

            if (UnityEngine.Input.GetMouseButtonUp(button))
            {
                QueueInputEvent(new InputEvent
                {
                    Type = InputEventType.MouseRelease,
                    Timestamp = Time.time
                });
            }
        }

        private void ProcessKeyboardInput()
        {
            if (UnityEngine.Input.inputString.Length > 0)
            {
                foreach (char c in UnityEngine.Input.inputString)
                {
                    if (c != '\b' && c != '\n' && c != '\r') // Ignore backspace, newline, return
                    {
                        QueueInputEvent(new InputEvent
                        {
                            Type = InputEventType.KeyPress,
                            KeyCode = (KeyCode)c,
                            Timestamp = Time.time
                        });

                        OnKeyPressed?.Invoke(c.ToString());
                    }
                }
            }
        }

        private void ProcessTouchInput()
        {
            foreach (Touch touch in UnityEngine.Input.touches)
            {
                var touchEvent = new InputEvent
                {
                    Type = GetTouchEventType(touch.phase),
                    MousePosition = touch.position,
                    MouseDelta = touch.deltaPosition,
                    Timestamp = Time.time
                };

                QueueInputEvent(touchEvent);
            }
        }

        private InputEventType GetTouchEventType(TouchPhase phase)
        {
            return phase switch
            {
                TouchPhase.Began => InputEventType.TouchStart,
                TouchPhase.Moved => InputEventType.TouchMove,
                TouchPhase.Ended => InputEventType.TouchEnd,
                TouchPhase.Canceled => InputEventType.TouchMove,
                _ => InputEventType.TouchMove
            };
        }

        #endregion

        #region Event Queue Management

        private void QueueInputEvent(InputEvent inputEvent)
        {
            if (_inputEventsThisFrame >= _maxInputEventsPerFrame)
            {
                if (_enableLogging)
                    ChimeraLogger.LogWarning("INPUT", "Max input events per frame exceeded", this);
                return;
            }

            if (_useInputBuffering)
            {
                _inputEventQueue.Enqueue(inputEvent);
            }
            else
            {
                ProcessInputEvent(inputEvent);
            }

            _inputEventsThisFrame++;
        }

        private void ProcessInputEventQueue()
        {
            if (!_useInputBuffering) return;

            var eventsProcessed = 0;
            var processingStartTime = Time.realtimeSinceStartup;

            while (_inputEventQueue.Count > 0 && eventsProcessed < _maxInputEventsPerFrame)
            {
                var inputEvent = _inputEventQueue.Dequeue();
                ProcessInputEvent(inputEvent);
                eventsProcessed++;
            }

            // Record event processing performance
            if (eventsProcessed > 0)
            {
                var processingTime = (Time.realtimeSinceStartup - processingStartTime) * 1000f;
                _performanceTracker?.RecordEventProcessing(eventsProcessed, processingTime);
            }
        }

        private void ProcessInputEvent(InputEvent inputEvent)
        {
            // Distribute event to registered handlers
            foreach (var handler in _inputHandlers)
            {
                if (handler != null)
                {
                    handler.HandleInputEvent(inputEvent);
                }
            }

            OnInputEventProcessed?.Invoke(inputEvent);
        }

        #endregion

        #region Mouse Prediction

        private void UpdateMouseHistory(Vector2 position)
        {
            _mousePositionHistory.Enqueue(position);
            _mouseTimeHistory.Enqueue(Time.time);

            // Maintain history within time window
            while (_mouseTimeHistory.Count > 0 && Time.time - _mouseTimeHistory.Peek() > _predictionTimeWindow)
            {
                _mousePositionHistory.Dequeue();
                _mouseTimeHistory.Dequeue();
            }
        }

        private void UpdateMousePrediction()
        {
            if (_mousePositionHistory.Count >= 2)
            {
                // Simple linear prediction based on recent movement
                var positions = _mousePositionHistory.ToArray();
                var times = _mouseTimeHistory.ToArray();

                var lastPos = positions[positions.Length - 1];
                var secondLastPos = positions[positions.Length - 2];
                var lastTime = times[times.Length - 1];
                var secondLastTime = times[times.Length - 2];

                var velocity = (lastPos - secondLastPos) / (lastTime - secondLastTime);
                // Prediction logic can be used by other systems that request it
            }
        }

        #endregion

        #region Handler Management

        public void RegisterInputHandler(IInputHandler handler)
        {
            if (handler != null && !_inputHandlers.Contains(handler))
            {
                _inputHandlers.Add(handler);

                if (_enableLogging)
                    ChimeraLogger.Log("INPUT", $"Registered input handler: {handler.GetType().Name}", this);
            }
        }

        public void UnregisterInputHandler(IInputHandler handler)
        {
            if (handler != null && _inputHandlers.Remove(handler))
            {
                if (_enableLogging)
                    ChimeraLogger.Log("INPUT", $"Unregistered input handler: {handler.GetType().Name}", this);
            }
        }

        #endregion

        #region Status and Configuration

        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;

            if (_enableLogging)
                ChimeraLogger.Log("INPUT", $"Input optimization {(enabled ? "enabled" : "disabled")}", this);
        }

        public InputSystemStatus GetStatus()
        {
            return new InputSystemStatus
            {
                IsEnabled = IsEnabled,
                QueuedEvents = _inputEventQueue.Count,
                RegisteredHandlers = _inputHandlers.Count,
                MousePosition = _lastMousePosition,
                MouseVelocity = _mouseVelocity,
                EventsThisFrame = _inputEventsThisFrame
            };
        }

        #endregion
    }

    /// <summary>
    /// Input system status information
    /// </summary>
    [System.Serializable]
    public struct InputSystemStatus
    {
        public bool IsEnabled;
        public int QueuedEvents;
        public int RegisteredHandlers;
        public Vector2 MousePosition;
        public Vector2 MouseVelocity;
        public int EventsThisFrame;
    }
}
