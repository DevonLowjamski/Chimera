using UnityEngine;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core;
using ProjectChimera.Data.Camera;

namespace ProjectChimera.Systems.Camera
{
    /// <summary>
    /// SIMPLE: Basic camera state manager aligned with Project Chimera's hierarchical viewpoint system.
    /// Focuses on essential camera level management for facility, room, table, and plant views.
    /// </summary>
    public class CameraStateManager : MonoBehaviour
    {
        [Header("Basic Camera Settings")]
        [SerializeField] private CameraLevel _defaultLevel = CameraLevel.Facility;
        [SerializeField] private bool _enableLogging = true;

        // Basic camera state
        private CameraLevel _currentLevel;
        private Transform _focusTarget;
        private Transform _currentLevelAnchor;
        private bool _isInitialized = false;
        private bool _userControlActive = true;
        private bool _isLevelTransitioning = false;

        /// <summary>
        /// Events for state changes
        /// </summary>
        public event System.Action<CameraLevel> OnLevelChanged;
        public event System.Action<Transform> OnFocusTargetChanged;
        public event System.Action<Transform> OnLevelAnchorChanged;
        public event System.Action<bool> OnUserControlChanged;

        /// <summary>
        /// Initialize the basic camera state manager
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            _currentLevel = _defaultLevel;
            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("OTHER", "$1", this);
            }
        }

        /// <summary>
        /// Set camera level
        /// </summary>
        public void SetCameraLevel(CameraLevel level)
        {
            if (_currentLevel == level) return;

            var previousLevel = _currentLevel;
            _currentLevel = level;

            OnLevelChanged?.Invoke(level);

            if (_enableLogging)
            {
                ChimeraLogger.Log("OTHER", "$1", this);
            }
        }

        /// <summary>
        /// Get current camera level
        /// </summary>
        public CameraLevel GetCurrentLevel()
        {
            return _currentLevel;
        }

        /// <summary>
        /// Set focus target
        /// </summary>
        public void SetFocusTarget(Transform target)
        {
            if (_focusTarget == target) return;

            _focusTarget = target;
            OnFocusTargetChanged?.Invoke(target);

            if (_enableLogging)
            {
                string targetName = target != null ? target.name : "None";
                ChimeraLogger.Log("OTHER", "$1", this);
            }
        }

        /// <summary>
        /// Get current focus target
        /// </summary>
        public Transform GetFocusTarget()
        {
            return _focusTarget;
        }

        /// <summary>
        /// Clear focus target
        /// </summary>
        public void ClearFocusTarget()
        {
            SetFocusTarget(null);
        }

        /// <summary>
        /// Check if camera is at facility level
        /// </summary>
        public bool IsAtFacilityLevel()
        {
            return _currentLevel == CameraLevel.Facility;
        }

        /// <summary>
        /// Check if camera is at room level
        /// </summary>
        public bool IsAtRoomLevel()
        {
            return _currentLevel == CameraLevel.Room;
        }

        /// <summary>
        /// Check if camera is at table level
        /// </summary>
        public bool IsAtTableLevel()
        {
            return _currentLevel == CameraLevel.Bench;
        }

        /// <summary>
        /// Check if camera is at plant level
        /// </summary>
        public bool IsAtPlantLevel()
        {
            return _currentLevel == CameraLevel.Plant;
        }

        /// <summary>
        /// Zoom in to next level
        /// </summary>
        public void ZoomIn()
        {
            switch (_currentLevel)
            {
                case CameraLevel.Facility:
                    SetCameraLevel(CameraLevel.Room);
                    break;
                case CameraLevel.Room:
                    SetCameraLevel(CameraLevel.Bench);
                    break;
                case CameraLevel.Bench:
                    SetCameraLevel(CameraLevel.Plant);
                    break;
                case CameraLevel.Plant:
                    // Already at closest level
                    break;
            }
        }

        /// <summary>
        /// Zoom out to previous level
        /// </summary>
        public void ZoomOut()
        {
            switch (_currentLevel)
            {
                case CameraLevel.Facility:
                    // Already at furthest level
                    break;
                case CameraLevel.Room:
                    SetCameraLevel(CameraLevel.Facility);
                    break;
                case CameraLevel.Bench:
                    SetCameraLevel(CameraLevel.Room);
                    break;
                case CameraLevel.Plant:
                    SetCameraLevel(CameraLevel.Bench);
                    break;
            }
        }

        /// <summary>
        /// Reset to default level
        /// </summary>
        public void ResetToDefault()
        {
            SetCameraLevel(_defaultLevel);
            ClearFocusTarget();
        }

        /// <summary>
        /// Public properties for camera state access
        /// </summary>
        public Transform FocusTarget => _focusTarget;
        public bool UserControlActive => _userControlActive;
        public Transform CurrentLevelAnchor => _currentLevelAnchor;
        public bool IsLevelTransitioning => _isLevelTransitioning;
        public CameraLevel CurrentLevel => _currentLevel;

        /// <summary>
        /// Clear focus
        /// </summary>
        public void ClearFocus()
        {
            ClearFocusTarget();
        }

        /// <summary>
        /// Set user control enabled/disabled
        /// </summary>
        public void SetUserControlEnabled(bool enabled)
        {
            if (_userControlActive != enabled)
            {
                _userControlActive = enabled;
                OnUserControlChanged?.Invoke(enabled);
            }
        }

        /// <summary>
        /// Update camera state with camera and transform references
        /// </summary>
        public void UpdateCameraState(UnityEngine.Camera camera, Transform cameraTransform)
        {
            // Basic camera state update logic
            // Implementation would depend on specific camera behavior requirements
        }

        /// <summary>
        /// Additional camera state management methods required by AdvancedCameraController
        /// </summary>
        public bool ZoomOutOneLevel()
        {
            var originalLevel = _currentLevel;
            ZoomOut();
            return _currentLevel != originalLevel;
        }

        public float GetLevelDistance(CameraLevel level)
        {
            // Return default distance based on level
            switch (level)
            {
                case CameraLevel.Facility: return 50f;
                case CameraLevel.Room: return 25f;
                case CameraLevel.Bench: return 10f;
                case CameraLevel.Plant: return 5f;
                default: return 15f;
            }
        }

        public float GetLevelHeight(CameraLevel level)
        {
            // Return default height based on level
            switch (level)
            {
                case CameraLevel.Facility: return 30f;
                case CameraLevel.Room: return 15f;
                case CameraLevel.Bench: return 8f;
                case CameraLevel.Plant: return 3f;
                default: return 10f;
            }
        }

        public bool IsValidLevel(CameraLevel level)
        {
            return System.Enum.IsDefined(typeof(CameraLevel), level);
        }

        public int GetLevelDistance(CameraLevel fromLevel, CameraLevel toLevel)
        {
            return Mathf.Abs((int)fromLevel - (int)toLevel);
        }

        public bool IsValidLevelTransition(CameraLevel fromLevel, CameraLevel toLevel)
        {
            return IsValidLevel(fromLevel) && IsValidLevel(toLevel);
        }

        public void SetMovementSmoothing(bool enabled, float movementSmoothTime = -1f, float rotationSmoothTime = -1f)
        {
            // Implementation would set smoothing parameters
        }

        public void SetCameraBounds(Vector3 min, Vector3 max, bool useSoftBounds = false)
        {
            // Implementation would set camera bounds
        }

        public string GetLevelSemanticName(CameraLevel level)
        {
            switch (level)
            {
                case CameraLevel.Facility: return "Facility View";
                case CameraLevel.Room: return "Room View";
                case CameraLevel.Bench: return "Table View";
                case CameraLevel.Plant: return "Plant View";
                default: return level.ToString();
            }
        }

        public string GetLevelDescription(CameraLevel level)
        {
            switch (level)
            {
                case CameraLevel.Facility: return "Overview of the entire facility";
                case CameraLevel.Room: return "Focus on a specific room";
                case CameraLevel.Bench: return "Close view of cultivation table";
                case CameraLevel.Plant: return "Individual plant inspection";
                default: return "";
            }
        }

        public float GetLevelFieldOfView(CameraLevel level)
        {
            switch (level)
            {
                case CameraLevel.Facility: return 80f;
                case CameraLevel.Room: return 60f;
                case CameraLevel.Bench: return 45f;
                case CameraLevel.Plant: return 30f;
                default: return 60f;
            }
        }

        public bool IsLevelAvailable(CameraLevel level)
        {
            return IsValidLevel(level);
        }

        public Vector3 ApplyGlobalPositionOffset(Vector3 originalPosition)
        {
            return originalPosition; // No offset by default
        }

        public void SetCameraLevelConfiguration(object configuration)
        {
            // Implementation would apply configuration
        }

        /// <summary>
        /// Get camera state summary
        /// </summary>
        public CameraStateSummary GetStateSummary()
        {
            return new CameraStateSummary
            {
                CurrentLevel = _currentLevel,
                FocusTargetName = _focusTarget != null ? _focusTarget.name : "None",
                IsInitialized = _isInitialized,
                DefaultLevel = _defaultLevel
            };
        }
    }

    /// <summary>
    /// Camera state summary
    /// </summary>
    [System.Serializable]
    public class CameraStateSummary
    {
        public CameraLevel CurrentLevel;
        public string FocusTargetName;
        public bool IsInitialized;
        public CameraLevel DefaultLevel;
    }

    /// <summary>
    /// Camera snapshot for state persistence
    /// </summary>
    [System.Serializable]
    public class CameraSnapshot
    {
        public Vector3 position;
        public Quaternion rotation;
        public float fieldOfView;
        public CameraLevel cameraLevel;
        public bool userControlActive;
        public float timestamp;
        public Vector3 focusPosition;
        public string focusTargetName;
        public bool isTransitioning;

        public CameraSnapshot()
        {
            position = Vector3.zero;
            rotation = Quaternion.identity;
            fieldOfView = 60f;
            cameraLevel = CameraLevel.Facility;
            userControlActive = true;
            timestamp = 0f;
            focusPosition = Vector3.zero;
            focusTargetName = "";
            isTransitioning = false;
        }

        public CameraSnapshot(Vector3 pos, Quaternion rot, float fov, CameraLevel level, bool userControl)
        {
            position = pos;
            rotation = rot;
            fieldOfView = fov;
            cameraLevel = level;
            userControlActive = userControl;
            timestamp = UnityEngine.Time.time;
            focusPosition = Vector3.zero;
            focusTargetName = "";
            isTransitioning = false;
        }
    }
}
