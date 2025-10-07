using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core.Input
{
    /// <summary>
    /// REFACTORED: Mouse Optimization Engine
    /// Single Responsibility: Mouse movement tracking, velocity calculation, and prediction
    /// Extracted from OptimizedInputManager for better separation of concerns
    /// </summary>
    public class MouseOptimizationEngine : MonoBehaviour
    {
        [Header("Mouse Optimization Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private float _mouseDeltaThreshold = 0.01f;
        [SerializeField] private bool _enableInputPrediction = true;
        [SerializeField] private float _predictionTimeWindow = 0.1f;
        [SerializeField] private int _maxHistoryEntries = 20;

        // Mouse state tracking
        private Vector2 _lastMousePosition;
        private Vector2 _mouseVelocity;
        private Vector2 _acceleratedVelocity;
        private float _lastMouseUpdateTime;
        private bool _mouseMovedThisFrame;

        // Input prediction
        private readonly Queue<Vector2> _mousePositionHistory = new Queue<Vector2>();
        private readonly Queue<float> _mouseTimeHistory = new Queue<float>();

        // Statistics
        private MouseOptimizationStats _stats = new MouseOptimizationStats();

        // State tracking
        private bool _isInitialized = false;

        // Events
        public event System.Action<Vector2, Vector2> OnMouseMoved; // position, velocity
        public event System.Action<Vector2> OnMousePredictionUpdated;

        public bool IsInitialized => _isInitialized;
        public MouseOptimizationStats Stats => _stats;
        public Vector2 LastMousePosition => _lastMousePosition;
        public Vector2 MouseVelocity => _mouseVelocity;
        public bool MouseMovedThisFrame => _mouseMovedThisFrame;

        public void Initialize()
        {
            if (_isInitialized) return;

            _lastMousePosition = UnityEngine.Input.mousePosition;
            _lastMouseUpdateTime = Time.unscaledTime;
            _mouseVelocity = Vector2.zero;
            _acceleratedVelocity = Vector2.zero;
            _mouseMovedThisFrame = false;

            _mousePositionHistory.Clear();
            _mouseTimeHistory.Clear();
            ResetStats();

            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("INPUT", "Mouse Optimization Engine initialized", this);
            }
        }

        /// <summary>
        /// Update mouse optimization system
        /// </summary>
        public void UpdateMouseOptimization()
        {
            if (!_isInitialized) return;

            Vector2 currentMousePos = UnityEngine.Input.mousePosition;
            float currentTime = Time.unscaledTime;
            float deltaTime = currentTime - _lastMouseUpdateTime;

            if (deltaTime > 0f)
            {
                Vector2 mouseDelta = currentMousePos - _lastMousePosition;
                bool moved = mouseDelta.magnitude > _mouseDeltaThreshold;

                if (moved)
                {
                    // Calculate velocity
                    Vector2 newVelocity = mouseDelta / deltaTime;

                    // Smooth velocity to reduce jitter
                    _mouseVelocity = Vector2.Lerp(_mouseVelocity, newVelocity, 0.7f);

                    // Calculate acceleration for advanced prediction
                    _acceleratedVelocity = (newVelocity - _mouseVelocity) / deltaTime;

                    _mouseMovedThisFrame = true;
                    _stats.MovementUpdates++;

                    // Fire movement event
                    OnMouseMoved?.Invoke(currentMousePos, _mouseVelocity);

                    // Update prediction system
                    UpdateInputPrediction(currentMousePos, currentTime);
                }
                else
                {
                    _mouseMovedThisFrame = false;
                }

                _lastMousePosition = currentMousePos;
                _lastMouseUpdateTime = currentTime;
                _stats.UpdateCycles++;
            }
        }

        /// <summary>
        /// Get predicted mouse position
        /// </summary>
        public Vector2 GetPredictedMousePosition(float deltaTime = 0.016f)
        {
            if (!_isInitialized)
            {
                return Vector2.zero;
            }

            if (!_enableInputPrediction || _mouseVelocity.magnitude < _mouseDeltaThreshold)
            {
                return _lastMousePosition;
            }

            // Simple linear prediction
            Vector2 linearPrediction = _lastMousePosition + (_mouseVelocity * deltaTime);

            // Advanced prediction with acceleration (for smoother prediction)
            Vector2 acceleratedPrediction = linearPrediction + (0.5f * _acceleratedVelocity * deltaTime * deltaTime);

            // Blend predictions based on velocity magnitude
            float velocityFactor = Mathf.Clamp01(_mouseVelocity.magnitude / 1000f);
            Vector2 prediction = Vector2.Lerp(linearPrediction, acceleratedPrediction, velocityFactor);

            _stats.PredictionRequests++;
            return prediction;
        }

        /// <summary>
        /// Get smoothed mouse velocity
        /// </summary>
        public Vector2 GetSmoothedMouseVelocity()
        {
            if (!_isInitialized) return Vector2.zero;

            // Calculate average velocity from recent history
            if (_mousePositionHistory.Count < 2) return _mouseVelocity;

            var positions = _mousePositionHistory.ToArray();
            var times = _mouseTimeHistory.ToArray();

            Vector2 avgVelocity = Vector2.zero;
            int validSamples = 0;

            for (int i = 1; i < positions.Length; i++)
            {
                float dt = times[i] - times[i - 1];
                if (dt > 0f)
                {
                    avgVelocity += (positions[i] - positions[i - 1]) / dt;
                    validSamples++;
                }
            }

            return validSamples > 0 ? avgVelocity / validSamples : _mouseVelocity;
        }

        /// <summary>
        /// Check if mouse is moving significantly
        /// </summary>
        public bool IsMouseMovingSignificantly(float threshold = -1f)
        {
            if (threshold < 0f) threshold = _mouseDeltaThreshold * 10f;
            return _mouseVelocity.magnitude > threshold;
        }

        /// <summary>
        /// Get mouse movement direction
        /// </summary>
        public Vector2 GetMouseMovementDirection()
        {
            if (_mouseVelocity.magnitude < _mouseDeltaThreshold)
                return Vector2.zero;

            return _mouseVelocity.normalized;
        }

        /// <summary>
        /// Set optimization parameters
        /// </summary>
        public void SetOptimizationParameters(float deltaThreshold, bool enablePrediction, float predictionWindow)
        {
            _mouseDeltaThreshold = Mathf.Max(0.001f, deltaThreshold);
            _enableInputPrediction = enablePrediction;
            _predictionTimeWindow = Mathf.Max(0.01f, predictionWindow);

            if (_enableLogging)
            {
                ChimeraLogger.Log("INPUT", $"Mouse optimization parameters updated: Threshold={_mouseDeltaThreshold:F3}, Prediction={_enableInputPrediction}, Window={_predictionTimeWindow:F2}s", this);
            }
        }

        /// <summary>
        /// Get mouse optimization status
        /// </summary>
        public MouseOptimizationStatus GetOptimizationStatus()
        {
            return new MouseOptimizationStatus
            {
                CurrentPosition = _lastMousePosition,
                Velocity = _mouseVelocity,
                IsMoving = _mouseMovedThisFrame,
                PredictionEnabled = _enableInputPrediction,
                HistoryEntries = _mousePositionHistory.Count,
                VelocityMagnitude = _mouseVelocity.magnitude,
                MovementDirection = GetMouseMovementDirection()
            };
        }

        /// <summary>
        /// Update input prediction system
        /// </summary>
        private void UpdateInputPrediction(Vector2 currentPosition, float currentTime)
        {
            if (!_enableInputPrediction) return;

            // Add current position to history
            _mousePositionHistory.Enqueue(currentPosition);
            _mouseTimeHistory.Enqueue(currentTime);

            // Remove old entries outside prediction window
            while (_mouseTimeHistory.Count > 0 && currentTime - _mouseTimeHistory.Peek() > _predictionTimeWindow)
            {
                _mousePositionHistory.Dequeue();
                _mouseTimeHistory.Dequeue();
            }

            // Limit history size
            while (_mousePositionHistory.Count > _maxHistoryEntries)
            {
                _mousePositionHistory.Dequeue();
                _mouseTimeHistory.Dequeue();
            }

            // Fire prediction update event
            var prediction = GetPredictedMousePosition();
            OnMousePredictionUpdated?.Invoke(prediction);

            _stats.PredictionUpdates++;
        }

        /// <summary>
        /// Reset statistics
        /// </summary>
        private void ResetStats()
        {
            _stats = new MouseOptimizationStats
            {
                UpdateCycles = 0,
                MovementUpdates = 0,
                PredictionRequests = 0,
                PredictionUpdates = 0
            };
        }

        /// <summary>
        /// Force mouse state refresh
        /// </summary>
        [ContextMenu("Force Mouse State Refresh")]
        public void ForceMouseStateRefresh()
        {
            if (_isInitialized)
            {
                UpdateMouseOptimization();

                if (_enableLogging)
                {
                    ChimeraLogger.Log("INPUT", $"Mouse state refreshed: Position={_lastMousePosition}, Velocity={_mouseVelocity.magnitude:F2}", this);
                }
            }
        }
    }

    /// <summary>
    /// Mouse optimization statistics
    /// </summary>
    [System.Serializable]
    public struct MouseOptimizationStats
    {
        public int UpdateCycles;
        public int MovementUpdates;
        public int PredictionRequests;
        public int PredictionUpdates;
    }

    /// <summary>
    /// Mouse optimization status
    /// </summary>
    [System.Serializable]
    public struct MouseOptimizationStatus
    {
        public Vector2 CurrentPosition;
        public Vector2 Velocity;
        public bool IsMoving;
        public bool PredictionEnabled;
        public int HistoryEntries;
        public float VelocityMagnitude;
        public Vector2 MovementDirection;
    }
}
