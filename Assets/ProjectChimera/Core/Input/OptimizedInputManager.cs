using UnityEngine;
using System;
using ProjectChimera.Core.Updates;

namespace ProjectChimera.Core.Input
{
    /// <summary>
    /// DEPRECATED: Use OptimizedInputService (Core.Input) + OptimizedInputManagerBridge (Systems.Input) instead
    /// This wrapper maintained for backward compatibility during migration
    /// Architecture violation: MonoBehaviour in Core layer
    /// </summary>
    [System.Obsolete("Use OptimizedInputService (Core.Input) + OptimizedInputManagerBridge (Systems.Input) instead")]
    public class OptimizedInputManager : MonoBehaviour, ITickable
    {
        [Header("Input Optimization Settings")]
        [SerializeField] private bool _enableInputOptimization = true;
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private float _inputPollingRate = 60f;
        [SerializeField] private float _mouseDeltaThreshold = 0.01f;
        [SerializeField] private int _maxInputEventsPerFrame = 50;

        [Header("Performance Settings")]
        [SerializeField] private bool _useInputBuffering = true;
        [SerializeField] private int _inputBufferSize = 100;
        [SerializeField] private bool _enableInputPrediction = true;
        [SerializeField] private float _predictionTimeWindow = 0.1f;

        [Header("Tick Settings")]
        [SerializeField] private float _tickInterval = 0.1f;

        private OptimizedInputService _service;
        private float _lastTickTime;

        public static OptimizedInputManager Instance { get; private set; }

        public bool IsInitialized => _service?.IsInitialized ?? false;
        public InputPerformanceStats Stats => _service?.Stats ?? new InputPerformanceStats();

        // Events
        public event Action<Vector2> OnOptimizedMouseMove
        {
            add { if (_service != null) _service.OnOptimizedMouseMove += value; }
            remove { if (_service != null) _service.OnOptimizedMouseMove -= value; }
        }

        public event Action<Vector2> OnMouseClick
        {
            add { if (_service != null) _service.OnMouseClick += value; }
            remove { if (_service != null) _service.OnMouseClick -= value; }
        }

        public event Action<Vector2> OnMouseDrag
        {
            add { if (_service != null) _service.OnMouseDrag += value; }
            remove { if (_service != null) _service.OnMouseDrag -= value; }
        }

        public event Action<float> OnScrollWheel
        {
            add { if (_service != null) _service.OnScrollWheel += value; }
            remove { if (_service != null) _service.OnScrollWheel -= value; }
        }

        public event Action<KeyCode> OnKeyPressed
        {
            add { if (_service != null) _service.OnKeyPressed += value; }
            remove { if (_service != null) _service.OnKeyPressed -= value; }
        }

        public event Action<KeyCode> OnKeyReleased
        {
            add { if (_service != null) _service.OnKeyReleased += value; }
            remove { if (_service != null) _service.OnKeyReleased -= value; }
        }

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                _service = new OptimizedInputService(
                    _enableInputOptimization,
                    _enableLogging,
                    _inputPollingRate,
                    _mouseDeltaThreshold,
                    _maxInputEventsPerFrame,
                    _useInputBuffering,
                    _inputBufferSize,
                    _enableInputPrediction,
                    _predictionTimeWindow
                );
                UpdateOrchestrator.Instance?.RegisterTickable(this);
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        public void Initialize()
            => _service?.Initialize(Time.unscaledTime, UnityEngine.Input.mousePosition);

        public void RegisterInputHandler(IInputHandler handler)
            => _service?.RegisterInputHandler(handler);

        public void UnregisterInputHandler(IInputHandler handler)
            => _service?.UnregisterInputHandler(handler);

        public Vector2 GetPredictedMousePosition(float deltaTime = 0.016f)
            => _service?.GetPredictedMousePosition(deltaTime) ?? Vector2.zero;

        public bool HasMouseMovedThisFrame()
            => _service?.HasMouseMovedThisFrame() ?? false;

        public InputState GetInputState(string inputName)
            => _service?.GetInputState(inputName) ?? new InputState();

        public void SetInputContext(InputContext context)
            => _service?.SetInputContext(context);

        public void SetInputEnabled(bool enabled)
            => _service?.SetInputEnabled(enabled);

        public int TickPriority => 50;
        public bool IsTickable => enabled && gameObject.activeInHierarchy;

        public void Tick(float deltaTime)
        {
            if (_service == null) return;

            _service.Tick(
                deltaTime,
                Time.unscaledTime,
                UnityEngine.Input.mousePosition,
                UnityEngine.Input.GetMouseButtonDown(0),
                UnityEngine.Input.GetMouseButtonUp(0),
                UnityEngine.Input.GetAxis("Mouse ScrollWheel"),
                _tickInterval,
                ref _lastTickTime
            );
        }

        private void OnDestroy()
        {
            _service?.Cleanup();
            UpdateOrchestrator.Instance?.UnregisterTickable(this);
        }
    }

    #region Data Structures

    /// <summary>
    /// Input event types
    /// </summary>
    public enum InputEventType
    {
        MouseMove,
        MouseClick,
        MouseRelease,
        MouseDrag,
        ScrollWheel,
        KeyPress,
        KeyRelease,
        TouchStart,
        TouchEnd,
        TouchMove
    }

    /// <summary>
    /// Input context for optimization
    /// </summary>
    public enum InputContext
    {
        Gameplay,
        UI,
        Both
    }

    /// <summary>
    /// Input event data structure
    /// </summary>
    [System.Serializable]
    public struct InputEvent
    {
        public InputEventType Type;
        public Vector2 MousePosition;
        public Vector2 MouseDelta;
        public float ScrollDelta;
        public KeyCode KeyCode;
        public float Timestamp;
    }

    /// <summary>
    /// Input state tracking
    /// </summary>
    [System.Serializable]
    public struct InputState
    {
        public bool IsActive;
        public float LastUpdateTime;
        public Vector2 LastPosition;
        public float LastValue;
    }

    /// <summary>
    /// Interface for input handlers
    /// </summary>
    public interface IInputHandler
    {
        void HandleInputEvent(InputEvent inputEvent);
    }

    #endregion
}
