using UnityEngine;
using System;
using ProjectChimera.Core.Input;
using ProjectChimera.Core.Updates;

namespace ProjectChimera.Systems.InputManagement
{
    /// <summary>
    /// REFACTORED: Thin MonoBehaviour wrapper for OptimizedInputService
    /// Bridges Unity lifecycle events and ITickable to Core layer service
    /// Complies with clean architecture: Unity-specific code in Systems, business logic in Core
    /// Note: Namespace changed from Systems.Input to Systems.InputManagement to avoid conflict with UnityEngine.Input
    /// </summary>
    public class OptimizedInputManagerBridge : MonoBehaviour, ITickable
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

        private static OptimizedInputManagerBridge _instance;
        public static OptimizedInputManagerBridge Instance => _instance;

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
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeService();
                UpdateOrchestrator.Instance?.RegisterTickable(this);
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void InitializeService()
        {
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
        }

        #region ITickable Implementation

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

        #endregion

        #region Public API (delegates to service)

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

        #endregion

        private void OnDestroy()
        {
            _service?.Cleanup();
            UpdateOrchestrator.Instance?.UnregisterTickable(this);

            if (_instance == this)
                _instance = null;
        }
    }
}
