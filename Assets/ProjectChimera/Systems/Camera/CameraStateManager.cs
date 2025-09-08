using ProjectChimera.Core.Logging;
using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core;
using ProjectChimera.Core.Updates;
using ProjectChimera.Data.Camera;

namespace ProjectChimera.Systems.Camera
{
    /// <summary>
    /// Manages core camera state including position, rotation, focus targets, and camera levels.
    /// Handles state tracking and history management. Works with CameraStatePersistence for save/load.
    /// </summary>
    [System.Serializable]
    public class CameraStateManager : MonoBehaviour, ITickable
    {
        [Header("State Configuration")]
        [SerializeField] private bool _enableStatePersistence = true;
        [SerializeField] private float _stateUpdateInterval = 0.1f;
        [SerializeField] private int _maxHistoryEntries = 20;

        [Header("Default Camera State")]
        [SerializeField] private CameraLevel _defaultCameraLevel = CameraLevel.Facility;
        [SerializeField] private Vector3 _defaultPosition = new Vector3(0, 10, -10);
        [SerializeField] private Vector3 _defaultRotation = new Vector3(30, 0, 0);
        [SerializeField] private float _defaultFieldOfView = 60f;

        // Core camera state
        private Transform _focusTarget;
        private Vector3 _targetPosition;
        private Quaternion _targetRotation;
        private float _targetFieldOfView;
        private bool _isTransitioning = false;
        private bool _userControlActive = true;

        // Movement and smoothing state
        private Vector3 _movementVelocity = Vector3.zero;
        private Vector3 _rotationVelocity = Vector3.zero;
        private Vector3 _followVelocity = Vector3.zero;
        private Vector3 _currentInertia = Vector3.zero;
        private Vector3 _targetMovement = Vector3.zero;

        // Camera level state
        private CameraLevel _currentCameraLevel;
        private CameraLevel _previousCameraLevel;
        private Transform _levelAnchor;
        private Dictionary<CameraLevel, Transform> _levelAnchors = new Dictionary<CameraLevel, Transform>();
        private List<CameraLevel> _levelHistory = new List<CameraLevel>();
        private bool _isLevelTransitioning = false;

        // State history manager
        private CameraStateHistory _stateHistory;

        // State change tracking
        private float _lastStateUpdateTime;
        private Vector3 _lastPosition;
        private Quaternion _lastRotation;
        private CameraLevel _lastLevel;

        // Events
        public System.Action<CameraSnapshot> OnStateChanged;
        public System.Action<Transform> OnFocusTargetChanged;
        public System.Action<CameraLevel, CameraLevel> OnCameraLevelChanged;
        public System.Action<CameraLevel> OnLevelTransitionStarted;
        public System.Action<CameraLevel> OnLevelTransitionCompleted;
        public System.Action<Transform> OnLevelAnchorChanged;
        public System.Action<bool> OnTransitionStateChanged;
        public System.Action<bool> OnUserControlChanged;

        [System.Serializable]
        public struct CameraSnapshot
        {
            public Vector3 position;
            public Quaternion rotation;
            public float fieldOfView;
            public CameraLevel cameraLevel;
            public Vector3 focusPosition;
            public string focusTargetName;
            public float timestamp;
            public bool isTransitioning;
            public bool userControlActive;

            public CameraSnapshot(Transform cameraTransform, CameraStateManager stateManager)
            {
                position = cameraTransform.position;
                rotation = cameraTransform.rotation;
                fieldOfView = UnityEngine.Camera.main ? UnityEngine.Camera.main.fieldOfView : 60f;
                cameraLevel = stateManager._currentCameraLevel;
                focusPosition = stateManager._focusTarget ? stateManager._focusTarget.position : Vector3.zero;
                focusTargetName = stateManager._focusTarget ? stateManager._focusTarget.name : "";
                timestamp = Time.time;
                isTransitioning = stateManager._isTransitioning;
                userControlActive = stateManager._userControlActive;
            }
        }

        // Properties
        public Transform FocusTarget => _focusTarget;
        public Vector3 TargetPosition => _targetPosition;
        public Quaternion TargetRotation => _targetRotation;
        public float TargetFieldOfView => _targetFieldOfView;
        public bool IsTransitioning => _isTransitioning;
        public bool UserControlActive => _userControlActive;
        public CameraLevel CurrentCameraLevel => _currentCameraLevel;
        public CameraLevel PreviousCameraLevel => _previousCameraLevel;
        public Transform CurrentLevelAnchor => _levelAnchor;
        public CameraLevel CurrentLevel => _currentCameraLevel;
        public bool IsLevelTransitioning => _isLevelTransitioning;
        public List<CameraLevel> LevelHistory => new List<CameraLevel>(_levelHistory);
        public Vector3 MovementVelocity => _movementVelocity;
        public Vector3 RotationVelocity => _rotationVelocity;
        public Vector3 CurrentInertia => _currentInertia;

        private void Awake()
        {
            InitializeState();
        }

        private void Start()
        {
            LoadPersistedState();
            
            // Register with UpdateOrchestrator
            UpdateOrchestrator.Instance.RegisterTickable(this);
        }

        #region ITickable Implementation
        
        public int Priority => TickPriority.CameraEffects;
        public bool Enabled => enabled;
        
        public void Tick(float deltaTime)
        {
            UpdateStateTracking();
        }
        
        #endregion

        #region Initialization

        private void InitializeState()
        {
            // Initialize camera level state
            _currentCameraLevel = _defaultCameraLevel;
            _previousCameraLevel = _defaultCameraLevel;

            // Initialize target transforms
            _targetPosition = _defaultPosition;
            _targetRotation = Quaternion.Euler(_defaultRotation);
            _targetFieldOfView = _defaultFieldOfView;

            // Initialize user control
            _userControlActive = true;

            // Initialize state history
            _stateHistory = new CameraStateHistory(_maxHistoryEntries);
            _stateHistory.OnStateRecorded += (state) => OnStateChanged?.Invoke(state);

            // Add initial state to history
            if (UnityEngine.Camera.main != null)
            {
                RecordCurrentState();
            }
        }

        private void LoadPersistedState()
        {
            if (!_enableStatePersistence) return;

            // Load persisted camera state using CameraStatePersistence utility
            if (CameraStatePersistence.HasSavedState())
            {
                var savedState = CameraStatePersistence.LoadState(_defaultPosition, _defaultRotation, _defaultFieldOfView, _defaultCameraLevel);
                ApplyState(savedState);
            }
        }

        #endregion

        #region State Management

        /// <summary>
        /// Set the camera's focus target
        /// </summary>
        public void SetFocusTarget(Transform target)
        {
            if (_focusTarget == target) return;

            Transform previousTarget = _focusTarget;
            _focusTarget = target;

            RecordCurrentState();
            OnFocusTargetChanged?.Invoke(target);
        }

        /// <summary>
        /// Clear the current focus target
        /// </summary>
        public void ClearFocusTarget()
        {
            SetFocusTarget(null);
        }

        /// <summary>
        /// Set target camera position and rotation
        /// </summary>
        public void SetTargetTransform(Vector3 position, Quaternion rotation)
        {
            _targetPosition = position;
            _targetRotation = rotation;
            RecordCurrentState();
        }

        /// <summary>
        /// Set target field of view
        /// </summary>
        public void SetTargetFieldOfView(float fov)
        {
            _targetFieldOfView = Mathf.Clamp(fov, 10f, 120f);
        }

        /// <summary>
        /// Set camera level and update state accordingly
        /// </summary>
        public void SetCameraLevel(CameraLevel level, Transform anchor = null)
        {
            if (_currentCameraLevel == level && _levelAnchor == anchor) return;

            _previousCameraLevel = _currentCameraLevel;
            _currentCameraLevel = level;

            if (anchor != null)
            {
                SetLevelAnchor(level, anchor);
            }

            // Add to level history
            if (_levelHistory.Count == 0 || _levelHistory[_levelHistory.Count - 1] != level)
            {
                _levelHistory.Add(level);

                // Limit history size
                if (_levelHistory.Count > _maxHistoryEntries)
                {
                    _levelHistory.RemoveAt(0);
                }
            }

            RecordCurrentState();
            OnCameraLevelChanged?.Invoke(level, _previousCameraLevel);
        }

        /// <summary>
        /// Set level-specific anchor transform
        /// </summary>
        public void SetLevelAnchor(CameraLevel level, Transform anchor)
        {
            _levelAnchors[level] = anchor;
            
            if (level == _currentCameraLevel)
            {
                _levelAnchor = anchor;
                OnLevelAnchorChanged?.Invoke(anchor);
            }
        }

        /// <summary>
        /// Get anchor for specific camera level
        /// </summary>
        public Transform GetLevelAnchor(CameraLevel level)
        {
            return _levelAnchors.TryGetValue(level, out Transform anchor) ? anchor : null;
        }

        /// <summary>
        /// Set transition state
        /// </summary>
        public void SetTransitionState(bool isTransitioning)
        {
            if (_isTransitioning == isTransitioning) return;

            _isTransitioning = isTransitioning;
            OnTransitionStateChanged?.Invoke(isTransitioning);

            if (isTransitioning)
            {
                OnLevelTransitionStarted?.Invoke(_currentCameraLevel);
            }
            else
            {
                OnLevelTransitionCompleted?.Invoke(_currentCameraLevel);
            }
        }

        /// <summary>
        /// Set level transition state
        /// </summary>
        public void SetLevelTransitionState(bool isTransitioning)
        {
            _isLevelTransitioning = isTransitioning;
        }

        /// <summary>
        /// Set user control state
        /// </summary>
        public void SetUserControlActive(bool active)
        {
            if (_userControlActive == active) return;

            _userControlActive = active;
            OnUserControlChanged?.Invoke(active);
        }

        #endregion

        #region Movement State

        /// <summary>
        /// Update movement velocity for smoothing calculations
        /// </summary>
        public void SetMovementVelocity(Vector3 velocity)
        {
            _movementVelocity = velocity;
        }

        /// <summary>
        /// Update rotation velocity for smoothing calculations
        /// </summary>
        public void SetRotationVelocity(Vector3 velocity)
        {
            _rotationVelocity = velocity;
        }

        /// <summary>
        /// Update follow velocity for target following
        /// </summary>
        public void SetFollowVelocity(Vector3 velocity)
        {
            _followVelocity = velocity;
        }

        /// <summary>
        /// Set current inertia for movement physics
        /// </summary>
        public void SetCurrentInertia(Vector3 inertia)
        {
            _currentInertia = inertia;
        }

        /// <summary>
        /// Set target movement for next frame
        /// </summary>
        public void SetTargetMovement(Vector3 movement)
        {
            _targetMovement = movement;
        }

        #endregion

        #region State History

        /// <summary>
        /// Record current camera state to history
        /// </summary>
        public void RecordCurrentState()
        {
            if (UnityEngine.Camera.main == null) return;

            var snapshot = new CameraSnapshot(UnityEngine.Camera.main.transform, this);
            _stateHistory.RecordState(snapshot);
        }

        /// <summary>
        /// Undo to previous camera state
        /// </summary>
        public bool UndoState()
        {
            if (_stateHistory.TryUndo(out var state))
            {
                ApplyState(state);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Redo to next camera state
        /// </summary>
        public bool RedoState()
        {
            if (_stateHistory.TryRedo(out var state))
            {
                ApplyState(state);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Get current state snapshot
        /// </summary>
        public CameraSnapshot GetCurrentState()
        {
            if (UnityEngine.Camera.main == null)
                return new CameraSnapshot();

            return new CameraSnapshot(UnityEngine.Camera.main.transform, this);
        }

        /// <summary>
        /// Apply camera state from snapshot
        /// </summary>
        public void ApplyState(CameraSnapshot state)
        {
            if (CameraStatePersistence.ValidateState(state))
            {
                _targetPosition = state.position;
                _targetRotation = state.rotation;
                _targetFieldOfView = state.fieldOfView;
                _currentCameraLevel = state.cameraLevel;
                _isTransitioning = state.isTransitioning;
                _userControlActive = state.userControlActive;

                // Try to restore focus target by name
                if (!string.IsNullOrEmpty(state.focusTargetName))
                {
                    GameObject targetObject = /* TODO: Replace GameObject.Find */ null;
                    if (targetObject != null)
                    {
                        SetFocusTarget(targetObject.transform);
                    }
                }
            }
        }

        #endregion

        #region State Persistence

        /// <summary>
        /// Save current state to persistent storage
        /// </summary>
        public void SaveStateToPersistence()
        {
            if (!_enableStatePersistence) return;

            var state = GetCurrentState();
            CameraStatePersistence.SaveState(state);
        }

        #endregion


        #region State Tracking

        private void UpdateStateTracking()
        {
            if (Time.time - _lastStateUpdateTime < _stateUpdateInterval) return;

            var camera = UnityEngine.Camera.main;
            if (camera == null) return;

            // Check for significant state changes
            bool positionChanged = Vector3.Distance(camera.transform.position, _lastPosition) > 0.1f;
            bool rotationChanged = Quaternion.Angle(camera.transform.rotation, _lastRotation) > 1f;
            bool levelChanged = _currentCameraLevel != _lastLevel;

            if (positionChanged || rotationChanged || levelChanged)
            {
                _lastPosition = camera.transform.position;
                _lastRotation = camera.transform.rotation;
                _lastLevel = _currentCameraLevel;
                _lastStateUpdateTime = Time.time;

                // Automatically save state periodically
                if (_enableStatePersistence)
                {
                    SaveStateToPersistence();
                }
            }
        }

        #endregion

        #region Public Interface

        /// <summary>
        /// Reset camera state to defaults
        /// </summary>
        public void ResetToDefaults()
        {
            var defaultState = new CameraSnapshot
            {
                position = _defaultPosition,
                rotation = Quaternion.Euler(_defaultRotation),
                fieldOfView = _defaultFieldOfView,
                cameraLevel = _defaultCameraLevel,
                focusPosition = Vector3.zero,
                focusTargetName = "",
                timestamp = Time.time,
                isTransitioning = false,
                userControlActive = true
            };

            ApplyState(defaultState);
            RecordCurrentState();
        }

        /// <summary>
        /// Clear state history
        /// </summary>
        public void ClearHistory()
        {
            _stateHistory.ClearHistory();
        }

        /// <summary>
        /// Get state summary for debugging
        /// </summary>
        public string GetStateSummary()
        {
            return $"Level: {_currentCameraLevel}, Target: {(_focusTarget ? _focusTarget.name : "None")}, " +
                   $"Position: {_targetPosition}, Transitioning: {_isTransitioning}, " +
                   $"History Entries: {_stateHistory.HistoryCount}";
        }

        #endregion

        private void OnDisable()
        {
            if (_enableStatePersistence)
            {
                SaveStateToPersistence();
            }
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus && _enableStatePersistence)
            {
                SaveStateToPersistence();
            }
        }
        
        #region Public API for AdvancedCameraController Orchestrator
        
        /// <summary>
        /// Update camera state with current camera and transform
        /// </summary>
        public void UpdateCameraState(UnityEngine.Camera camera, Transform cameraTransform)
        {
            if (camera != null && cameraTransform != null)
            {
                // Update internal state based on current camera state
                _targetFieldOfView = camera.fieldOfView;
            }
        }
        
        /// <summary>
        /// Clear focus target
        /// </summary>
        public void ClearFocus()
        {
            ClearFocusTarget();
        }
        
        /// <summary>
        /// Get distance for a camera level
        /// </summary>
        public float GetLevelDistance(CameraLevel level)
        {
            // Implementation would return configured distance for level
            return level switch
            {
                CameraLevel.Plant => 3f,
                CameraLevel.Bench => 8f,
                CameraLevel.Room => 15f,
                CameraLevel.Facility => 25f,
                _ => 15f
            };
        }
        
        /// <summary>
        /// Get height for a camera level
        /// </summary>
        public float GetLevelHeight(CameraLevel level)
        {
            // Implementation would return configured height for level
            return level switch
            {
                CameraLevel.Plant => 2f,
                CameraLevel.Bench => 5f,
                CameraLevel.Room => 12f,
                CameraLevel.Facility => 20f,
                _ => 10f
            };
        }
        
        /// <summary>
        /// Check if level is valid
        /// </summary>
        public bool IsValidLevel(CameraLevel level)
        {
            return System.Enum.IsDefined(typeof(CameraLevel), level);
        }
        
        /// <summary>
        /// Zoom out one level
        /// </summary>
        public bool ZoomOutOneLevel()
        {
            // Implementation would zoom to next level up
            var currentIndex = (int)_currentCameraLevel;
            var nextLevel = (CameraLevel)UnityEngine.Mathf.Min(currentIndex + 1, System.Enum.GetValues(typeof(CameraLevel)).Length - 1);
            
            if (nextLevel != _currentCameraLevel)
            {
                SetCameraLevel(nextLevel);
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Get distance between two camera levels
        /// </summary>
        public int GetLevelDistance(CameraLevel fromLevel, CameraLevel toLevel)
        {
            return UnityEngine.Mathf.Abs((int)fromLevel - (int)toLevel);
        }
        
        /// <summary>
        /// Check if level transition is valid
        /// </summary>
        public bool IsValidLevelTransition(CameraLevel fromLevel, CameraLevel toLevel)
        {
            return IsValidLevel(fromLevel) && IsValidLevel(toLevel);
        }
        
        /// <summary>
        /// Set movement smoothing parameters
        /// </summary>
        public void SetMovementSmoothing(bool enabled, float movementSmoothTime = -1f, float rotationSmoothTime = -1f)
        {
            // Implementation would set smoothing parameters
            ChimeraLogger.Log($"[CameraStateManager] Movement smoothing set: {enabled}");
        }
        
        /// <summary>
        /// Set camera bounds
        /// </summary>
        public void SetCameraBounds(Vector3 min, Vector3 max, bool useSoftBounds = false)
        {
            // Implementation would set camera bounds
            ChimeraLogger.Log($"[CameraStateManager] Camera bounds set: {min} to {max}");
        }
        
        /// <summary>
        /// Set user control enabled state
        /// </summary>
        public void SetUserControlEnabled(bool enabled)
        {
            SetUserControlActive(enabled);
        }
        
        /// <summary>
        /// Apply global position offset
        /// </summary>
        public Vector3 ApplyGlobalPositionOffset(Vector3 originalPosition)
        {
            return originalPosition; // Implementation would apply any global offset
        }
        
        /// <summary>
        /// Set camera level configuration
        /// </summary>
        public void SetCameraLevelConfiguration(CameraLevelConfigurationSO configuration)
        {
            // Implementation would apply configuration
            ChimeraLogger.Log($"[CameraStateManager] Camera level configuration set");
        }
        
        /// <summary>
        /// Set camera level
        /// </summary>
        public void SetCameraLevel(CameraLevel level)
        {
            if (IsValidLevel(level))
            {
                _currentCameraLevel = level;
            }
        }
        
        /// <summary>
        /// Get level semantic name
        /// </summary>
        public string GetLevelSemanticName(CameraLevel level)
        {
            return level.ToString();
        }
        
        /// <summary>
        /// Get level description
        /// </summary>
        public string GetLevelDescription(CameraLevel level)
        {
            return level switch
            {
                CameraLevel.Plant => "Individual plant closeup view",
                CameraLevel.Bench => "Table/bench level view",
                CameraLevel.Room => "Room overview",
                CameraLevel.Facility => "Facility wide overview",
                _ => "Unknown level"
            };
        }
        
        /// <summary>
        /// Get level field of view
        /// </summary>
        public float GetLevelFieldOfView(CameraLevel level)
        {
            return level switch
            {
                CameraLevel.Plant => 45f,
                CameraLevel.Bench => 55f,
                CameraLevel.Room => 65f,
                CameraLevel.Facility => 75f,
                _ => 60f
            };
        }
        
        /// <summary>
        /// Check if level is available
        /// </summary>
        public bool IsLevelAvailable(CameraLevel level)
        {
            return IsValidLevel(level);
        }
        
        private void OnDestroy()
        {
            if (UpdateOrchestrator.Instance != null)
            {
                UpdateOrchestrator.Instance.UnregisterTickable(this);
            }
        }
        
        #endregion
    }
}