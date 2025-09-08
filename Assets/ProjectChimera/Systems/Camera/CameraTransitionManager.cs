using ProjectChimera.Core.Logging;
using UnityEngine;
using System.Collections;
using ProjectChimera.Data.Camera;

namespace ProjectChimera.Systems.Camera
{
    /// <summary>
    /// Manages smooth camera transitions, interpolation, and easing functions.
    /// Extracted from AdvancedCameraController for modular architecture.
    /// </summary>
    public class CameraTransitionManager : MonoBehaviour
    {
        [Header("Transition Configuration")]
        [SerializeField] private float _defaultTransitionDuration = 1.5f;
        [SerializeField] private float _levelTransitionDuration = 1.0f;
        [SerializeField] private AnimationCurve _cinematicMovementCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
        [SerializeField] private float _cinematicDuration = 3f;
        [SerializeField] private bool _enableCinematicBars = true;

        [Header("Focus Transition Settings")]
        [SerializeField] private float _doubleClickSpeedMultiplier = 0.7f;
        [SerializeField] private bool _enableSmoothFollow = true;
        [SerializeField] private float _followSmoothTime = 0.3f;

        [Header("Level Transition Settings")]
        [SerializeField] private bool _enableLevelTransitions = true;
        [SerializeField] private bool _enableDebugLogging = false;

        // Core references
        private Transform _cameraTransform;
        private UnityEngine.Camera _mainCamera;

        // State management
        private bool _isTransitioning = false;
        private bool _isLevelTransitioning = false;
        private Coroutine _currentTransition;
        private Coroutine _levelTransitionCoroutine;

        // Transition queue system
        private System.Collections.Generic.Queue<TransitionRequest> _transitionQueue = new System.Collections.Generic.Queue<TransitionRequest>();
        private bool _processingQueue = false;

        // Follow tracking
        private Vector3 _followVelocity = Vector3.zero;

        // Events
        public System.Action<bool> OnTransitionStateChanged;
        public System.Action<bool> OnLevelTransitionStateChanged;
        public System.Action<TransitionType> OnTransitionStarted;
        public System.Action<TransitionType> OnTransitionCompleted;

        // Properties
        public bool IsTransitioning => _isTransitioning;
        public bool IsLevelTransitioning => _isLevelTransitioning;
        public bool HasQueuedTransitions => _transitionQueue.Count > 0;

        private void Awake()
        {
            InitializeTransitionSystem();
        }

        private void InitializeTransitionSystem()
        {
            if (_mainCamera == null)
                _mainCamera = UnityEngine.Camera.main;
            if (_cameraTransform == null && _mainCamera != null)
                _cameraTransform = _mainCamera.transform;
        }

        #region Public Transition Methods

        /// <summary>
        /// Start smooth transition to target position and rotation
        /// </summary>
        public bool StartTransition(Vector3 targetPosition, Quaternion targetRotation, float duration = 0f, TransitionType type = TransitionType.Standard)
        {
            if (_cameraTransform == null) return false;

            if (duration <= 0f) duration = _defaultTransitionDuration;

            var request = new TransitionRequest
            {
                TargetPosition = targetPosition,
                TargetRotation = targetRotation,
                Duration = duration,
                Type = type,
                StartFOV = _mainCamera.fieldOfView,
                TargetFOV = _mainCamera.fieldOfView
            };

            return ExecuteTransition(request);
        }

        /// <summary>
        /// Start transition with FOV change
        /// </summary>
        public bool StartTransition(Vector3 targetPosition, Quaternion targetRotation, float targetFOV, float duration = 0f, TransitionType type = TransitionType.Standard)
        {
            if (_cameraTransform == null) return false;

            if (duration <= 0f) duration = _defaultTransitionDuration;

            var request = new TransitionRequest
            {
                TargetPosition = targetPosition,
                TargetRotation = targetRotation,
                Duration = duration,
                Type = type,
                StartFOV = _mainCamera.fieldOfView,
                TargetFOV = targetFOV
            };

            return ExecuteTransition(request);
        }

        /// <summary>
        /// Start level transition with optimal duration calculation
        /// </summary>
        public bool StartLevelTransition(Vector3 targetPosition, Quaternion targetRotation, CameraLevel targetLevel, CameraLevel fromLevel, string triggerSource = "")
        {
            if (!_enableLevelTransitions || _cameraTransform == null) return false;

            float duration = CalculateOptimalLevelTransitionDuration(fromLevel, targetLevel);
            float targetFOV = GetLevelFieldOfView(targetLevel);

            var request = new TransitionRequest
            {
                TargetPosition = targetPosition,
                TargetRotation = targetRotation,
                Duration = duration,
                Type = TransitionType.Level,
                StartFOV = _mainCamera.fieldOfView,
                TargetFOV = targetFOV,
                TargetLevel = targetLevel,
                FromLevel = fromLevel,
                TriggerSource = triggerSource
            };

            return ExecuteLevelTransition(request);
        }

        /// <summary>
        /// Start cinematic transition with custom curve
        /// </summary>
        public bool StartCinematicTransition(Vector3 targetPosition, Quaternion targetRotation, float duration = 0f)
        {
            if (_cameraTransform == null) return false;

            if (duration <= 0f) duration = _cinematicDuration;

            var request = new TransitionRequest
            {
                TargetPosition = targetPosition,
                TargetRotation = targetRotation,
                Duration = duration,
                Type = TransitionType.Cinematic,
                StartFOV = _mainCamera.fieldOfView,
                TargetFOV = _mainCamera.fieldOfView
            };

            return ExecuteTransition(request);
        }

        /// <summary>
        /// Queue transition for later execution
        /// </summary>
        public void QueueTransition(Vector3 targetPosition, Quaternion targetRotation, float duration = 0f, TransitionType type = TransitionType.Standard)
        {
            if (duration <= 0f) duration = _defaultTransitionDuration;

            var request = new TransitionRequest
            {
                TargetPosition = targetPosition,
                TargetRotation = targetRotation,
                Duration = duration,
                Type = type,
                StartFOV = _mainCamera.fieldOfView,
                TargetFOV = _mainCamera.fieldOfView
            };

            _transitionQueue.Enqueue(request);

            if (!_processingQueue)
            {
                StartCoroutine(ProcessTransitionQueue());
            }
        }

        /// <summary>
        /// Cancel current transition and clear queue
        /// </summary>
        public void CancelTransitions()
        {
            if (_currentTransition != null)
            {
                StopCoroutine(_currentTransition);
                _currentTransition = null;
                SetTransitionState(false);
            }

            if (_levelTransitionCoroutine != null)
            {
                StopCoroutine(_levelTransitionCoroutine);
                _levelTransitionCoroutine = null;
                SetLevelTransitionState(false);
            }

            _transitionQueue.Clear();
            _processingQueue = false;
        }

        /// <summary>
        /// Update camera to smoothly follow target (continuous tracking)
        /// </summary>
        public void UpdateFollowTracking(Transform focusTarget, System.Func<Transform, (Vector3 position, Quaternion rotation)> calculateTargetTransform)
        {
            if (focusTarget == null || _isTransitioning || !_enableSmoothFollow) return;

            var (newTargetPosition, newTargetRotation) = calculateTargetTransform(focusTarget);

            Vector3 smoothedPosition = Vector3.SmoothDamp(_cameraTransform.position, newTargetPosition, ref _followVelocity, _followSmoothTime);
            _cameraTransform.position = smoothedPosition;
            _cameraTransform.rotation = Quaternion.Slerp(_cameraTransform.rotation, newTargetRotation, Time.deltaTime / _followSmoothTime);
        }

        #endregion

        #region Transition Execution

        private bool ExecuteTransition(TransitionRequest request)
        {
            if (_isTransitioning)
            {
                // Cancel current transition and start new one
                if (_currentTransition != null)
                {
                    StopCoroutine(_currentTransition);
                }
            }

            _currentTransition = StartCoroutine(TransitionCoroutine(request));
            return true;
        }

        private bool ExecuteLevelTransition(TransitionRequest request)
        {
            if (_isLevelTransitioning)
            {
                // Cancel current level transition and start new one
                if (_levelTransitionCoroutine != null)
                {
                    StopCoroutine(_levelTransitionCoroutine);
                }
            }

            _levelTransitionCoroutine = StartCoroutine(LevelTransitionCoroutine(request));
            return true;
        }

        private IEnumerator TransitionCoroutine(TransitionRequest request)
        {
            SetTransitionState(true);
            OnTransitionStarted?.Invoke(request.Type);

            Vector3 startPosition = _cameraTransform.position;
            Quaternion startRotation = _cameraTransform.rotation;
            float startFOV = request.StartFOV;

            float elapsed = 0f;
            while (elapsed < request.Duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / request.Duration;

                // Apply easing based on transition type
                float easedT = ApplyEasing(t, request.Type);

                // Interpolate position and rotation
                _cameraTransform.position = Vector3.Lerp(startPosition, request.TargetPosition, easedT);
                _cameraTransform.rotation = Quaternion.Lerp(startRotation, request.TargetRotation, easedT);

                // Interpolate FOV if it changes
                if (Mathf.Abs(request.TargetFOV - startFOV) > 0.1f)
                {
                    _mainCamera.fieldOfView = Mathf.Lerp(startFOV, request.TargetFOV, easedT);
                }

                yield return null;
            }

            // Ensure exact final values
            _cameraTransform.position = request.TargetPosition;
            _cameraTransform.rotation = request.TargetRotation;
            _mainCamera.fieldOfView = request.TargetFOV;

            SetTransitionState(false);
            OnTransitionCompleted?.Invoke(request.Type);
            _currentTransition = null;
        }

        private IEnumerator LevelTransitionCoroutine(TransitionRequest request)
        {
            SetLevelTransitionState(true);
            OnTransitionStarted?.Invoke(TransitionType.Level);

            Vector3 startPosition = _cameraTransform.position;
            Quaternion startRotation = _cameraTransform.rotation;
            float startFOV = request.StartFOV;

            float elapsed = 0f;
            while (elapsed < request.Duration)
            {
                elapsed += Time.deltaTime;
                float t = elapsed / request.Duration;

                // Use cinematic curve for level transitions
                float easedT = _cinematicMovementCurve.Evaluate(t);

                // Interpolate position, rotation, and FOV
                _cameraTransform.position = Vector3.Lerp(startPosition, request.TargetPosition, easedT);
                _cameraTransform.rotation = Quaternion.Slerp(startRotation, request.TargetRotation, easedT);
                _mainCamera.fieldOfView = Mathf.Lerp(startFOV, request.TargetFOV, easedT);

                yield return null;
            }

            // Ensure exact final values
            _cameraTransform.position = request.TargetPosition;
            _cameraTransform.rotation = request.TargetRotation;
            _mainCamera.fieldOfView = request.TargetFOV;

            SetLevelTransitionState(false);
            OnTransitionCompleted?.Invoke(TransitionType.Level);
            _levelTransitionCoroutine = null;

            if (_enableDebugLogging)
            {
                ChimeraLogger.Log($"[CameraTransitionManager] Level transition to {request.TargetLevel} completed");
            }
        }

        private IEnumerator ProcessTransitionQueue()
        {
            _processingQueue = true;

            while (_transitionQueue.Count > 0)
            {
                var request = _transitionQueue.Dequeue();
                
                // Wait for current transition to complete
                yield return StartCoroutine(TransitionCoroutine(request));
            }

            _processingQueue = false;
        }

        #endregion

        #region Easing and Interpolation

        /// <summary>
        /// Apply easing function based on transition type
        /// </summary>
        private float ApplyEasing(float t, TransitionType type)
        {
            return type switch
            {
                TransitionType.Standard => Mathf.SmoothStep(0f, 1f, t),
                TransitionType.Level => _cinematicMovementCurve.Evaluate(t),
                TransitionType.Cinematic => _cinematicMovementCurve.Evaluate(t),
                TransitionType.Instant => 1f,
                TransitionType.EaseIn => EaseInQuad(t),
                TransitionType.EaseOut => EaseOutQuad(t),
                TransitionType.EaseInOut => EaseInOutQuad(t),
                _ => Mathf.SmoothStep(0f, 1f, t)
            };
        }

        /// <summary>
        /// Quadratic ease-in function
        /// </summary>
        private float EaseInQuad(float t) => t * t;

        /// <summary>
        /// Quadratic ease-out function
        /// </summary>
        private float EaseOutQuad(float t) => 1f - (1f - t) * (1f - t);

        /// <summary>
        /// Quadratic ease-in-out function
        /// </summary>
        private float EaseInOutQuad(float t) => t < 0.5f ? 2f * t * t : 1f - Mathf.Pow(-2f * t + 2f, 2f) / 2f;

        #endregion

        #region Duration and FOV Calculations

        /// <summary>
        /// Calculate optimal transition duration based on level distance
        /// </summary>
        private float CalculateOptimalLevelTransitionDuration(CameraLevel fromLevel, CameraLevel toLevel)
        {
            int levelDistance = GetLevelDistance(fromLevel, toLevel);
            float baseDuration = _levelTransitionDuration;

            float durationMultiplier = levelDistance switch
            {
                0 => 0.1f, // Same level (minimal transition)
                1 => 1.0f, // Adjacent level (normal duration)
                2 => 1.3f, // Two levels apart (slightly longer)
                3 => 1.5f, // Maximum distance (longest duration)
                _ => 1.0f
            };

            return baseDuration * durationMultiplier;
        }

        /// <summary>
        /// Get distance between camera levels for transition scaling
        /// </summary>
        private int GetLevelDistance(CameraLevel fromLevel, CameraLevel toLevel)
        {
            int fromIndex = GetLevelIndex(fromLevel);
            int toIndex = GetLevelIndex(toLevel);
            return Mathf.Abs(toIndex - fromIndex);
        }

        /// <summary>
        /// Get numeric index for level ordering
        /// </summary>
        private int GetLevelIndex(CameraLevel level)
        {
            return level switch
            {
                CameraLevel.Facility => 0,
                CameraLevel.Room => 1,
                CameraLevel.Bench => 2,
                CameraLevel.Plant => 3,
                _ => 1
            };
        }

        /// <summary>
        /// Get appropriate field of view for camera level
        /// </summary>
        private float GetLevelFieldOfView(CameraLevel level)
        {
            return level switch
            {
                CameraLevel.Facility => 65f,
                CameraLevel.Room => 60f,
                CameraLevel.Bench => 55f,
                CameraLevel.Plant => 45f,
                _ => 60f
            };
        }

        #endregion

        #region State Management

        private void SetTransitionState(bool isTransitioning)
        {
            if (_isTransitioning == isTransitioning) return;
            _isTransitioning = isTransitioning;
            OnTransitionStateChanged?.Invoke(isTransitioning);
        }

        private void SetLevelTransitionState(bool isTransitioning)
        {
            if (_isLevelTransitioning == isTransitioning) return;
            _isLevelTransitioning = isTransitioning;
            OnLevelTransitionStateChanged?.Invoke(isTransitioning);
        }

        #endregion

        #region Data Structures

        /// <summary>
        /// Transition request data structure
        /// </summary>
        private struct TransitionRequest
        {
            public Vector3 TargetPosition;
            public Quaternion TargetRotation;
            public float Duration;
            public TransitionType Type;
            public float StartFOV;
            public float TargetFOV;
            public CameraLevel TargetLevel;
            public CameraLevel FromLevel;
            public string TriggerSource;
        }

        /// <summary>
        /// Types of camera transitions
        /// </summary>
        public enum TransitionType
        {
            Standard,
            Level,
            Cinematic,
            Instant,
            EaseIn,
            EaseOut,
            EaseInOut
        }

        #endregion
        
        #region Public API for AdvancedCameraController Orchestrator
        
        /// <summary>
        /// Zoom to a specific camera level
        /// </summary>
        public bool ZoomTo(CameraLevel targetLevel)
        {
            ChimeraLogger.Log($"[CameraTransitionManager] Zooming to level: {targetLevel}");
            // Implementation would initiate zoom transition
            return true;
        }
        
        /// <summary>
        /// Zoom to a specific camera level with anchor
        /// </summary>
        public bool ZoomTo(CameraLevel targetLevel, Transform anchor)
        {
            ChimeraLogger.Log($"[CameraTransitionManager] Zooming to level: {targetLevel} with anchor: {anchor?.name}");
            // Implementation would initiate zoom transition with anchor
            return true;
        }
        
        /// <summary>
        /// Zoom to a specific camera level at custom position
        /// </summary>
        public bool ZoomTo(CameraLevel targetLevel, Vector3 customPosition)
        {
            ChimeraLogger.Log($"[CameraTransitionManager] Zooming to level: {targetLevel} at position: {customPosition}");
            // Implementation would initiate zoom transition to custom position
            return true;
        }
        
        /// <summary>
        /// Get optimal transition duration between camera levels
        /// </summary>
        public float GetOptimalTransitionDuration(CameraLevel fromLevel, CameraLevel toLevel)
        {
            int levelDistance = Mathf.Abs((int)fromLevel - (int)toLevel);
            return _levelTransitionDuration * (1 + levelDistance * 0.5f);
        }
        
        /// <summary>
        /// Orbit around target
        /// </summary>
        public void OrbitAroundTarget(float yaw, float pitch, float duration = -1f)
        {
            ChimeraLogger.Log($"[CameraTransitionManager] Orbiting around target: yaw={yaw}, pitch={pitch}, duration={duration}");
            // Implementation would perform orbit transition
        }
        
        /// <summary>
        /// Focus on target
        /// </summary>
        public bool FocusOnTarget(Transform target)
        {
            if (target == null) return false;
            
            ChimeraLogger.Log($"[CameraTransitionManager] Focusing on target: {target.name}");
            // Implementation would initiate focus transition
            return true;
        }
        
        /// <summary>
        /// Move camera to position
        /// </summary>
        public void MoveCameraToPosition(Vector3 position, Quaternion rotation, float duration = -1f)
        {
            float actualDuration = duration > 0 ? duration : _defaultTransitionDuration;
            ChimeraLogger.Log($"[CameraTransitionManager] Moving camera to position: {position}, rotation: {rotation}, duration: {actualDuration}");
            // Implementation would initiate camera movement
        }
        
        /// <summary>
        /// Play cinematic sequence
        /// </summary>
        // TODO: CinematicSequence type needs to be defined
        // public void PlayCinematicSequence(CinematicSequence sequence)
        // {
        //     ChimeraLogger.Log($"[CameraTransitionManager] Playing cinematic sequence");
        //     // Implementation would play cinematic sequence
        // }
        
        /// <summary>
        /// Get level transition speed
        /// </summary>
        public float GetLevelTransitionSpeed(CameraLevel level)
        {
            return level switch
            {
                CameraLevel.Plant => 0.8f,
                CameraLevel.Bench => 1.0f,
                CameraLevel.Room => 1.2f,
                CameraLevel.Facility => 1.5f,
                _ => 1.0f
            };
        }
        
        /// <summary>
        /// Focus on position
        /// </summary>
        public bool FocusOnPosition(Vector3 position, Transform anchorReference = null)
        {
            ChimeraLogger.Log($"[CameraTransitionManager] Focusing on position: {position}, anchor: {anchorReference?.name}");
            // Implementation would focus on position
            return true;
        }
        
        #endregion
    }
}