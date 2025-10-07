using UnityEngine;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core;
using ProjectChimera.Data.Camera;

namespace ProjectChimera.Systems.Camera
{
    /// <summary>
    /// SIMPLE: Basic camera positioning aligned with Project Chimera's hierarchical viewpoint system.
    /// Focuses on essential camera level management without complex transitions.
    /// </summary>
    public class CameraTransitionManager : MonoBehaviour
    {
        [Header("Basic Camera Settings")]
        [SerializeField] private bool _enableBasicCamera = true;
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private UnityEngine.Camera _mainCamera;

        // Basic camera state
        private CameraLevel _currentLevel = CameraLevel.Facility;
        private bool _isInitialized = false;
        private bool _isTransitioning = false;

        /// <summary>
        /// Events for level changes and transitions
        /// </summary>
        public event System.Action<CameraLevel> OnLevelChanged;
        public event System.Action<bool> OnTransitionStateChanged;
        public event System.Action<TransitionType> OnTransitionCompleted;

        /// <summary>
        /// Initialize basic camera management
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            if (_mainCamera == null)
            {
                _mainCamera = UnityEngine.Camera.main;
            }

            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.LogInfo("CameraTransitionManager", "$1");
            }
        }

        /// <summary>
        /// Set camera position instantly
        /// </summary>
        public void SetCameraPosition(Vector3 position, Quaternion rotation)
        {
            if (!_enableBasicCamera || _mainCamera == null) return;

            _mainCamera.transform.position = position;
            _mainCamera.transform.rotation = rotation;

            if (_enableLogging)
            {
                ChimeraLogger.LogInfo("CameraTransitionManager", "$1");
            }
        }

        /// <summary>
        /// Set camera level
        /// </summary>
        public void SetCameraLevel(CameraLevel level)
        {
            if (_currentLevel == level) return;

            _currentLevel = level;
            OnLevelChanged?.Invoke(level);

            if (_enableLogging)
            {
                ChimeraLogger.LogInfo("CameraTransitionManager", "$1");
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
        /// Get camera position
        /// </summary>
        public Vector3 GetCameraPosition()
        {
            return _mainCamera != null ? _mainCamera.transform.position : Vector3.zero;
        }

        /// <summary>
        /// Get camera rotation
        /// </summary>
        public Quaternion GetCameraRotation()
        {
            return _mainCamera != null ? _mainCamera.transform.rotation : Quaternion.identity;
        }

        /// <summary>
        /// Set camera field of view
        /// </summary>
        public void SetFieldOfView(float fov)
        {
            if (_mainCamera == null) return;

            _mainCamera.fieldOfView = Mathf.Clamp(fov, 10f, 120f);

            if (_enableLogging)
            {
                ChimeraLogger.LogInfo("CameraTransitionManager", "$1");
            }
        }

        /// <summary>
        /// Get camera field of view
        /// </summary>
        public float GetFieldOfView()
        {
            return _mainCamera != null ? _mainCamera.fieldOfView : 60f;
        }

        /// <summary>
        /// Check if transition is in progress
        /// </summary>
        public bool IsTransitioning
        {
            get { return _isTransitioning; }
        }

        /// <summary>
        /// Reset camera to default position
        /// </summary>
        public void ResetToDefault()
        {
            SetCameraLevel(CameraLevel.Facility);
            SetCameraPosition(new Vector3(0, 10, -10), Quaternion.Euler(30, 0, 0));
            SetFieldOfView(60f);

            if (_enableLogging)
            {
                ChimeraLogger.LogInfo("CameraTransitionManager", "$1");
            }
        }

        /// <summary>
        /// Enable or disable basic camera
        /// </summary>
        public void SetCameraEnabled(bool enabled)
        {
            _enableBasicCamera = enabled;

            if (_mainCamera != null)
            {
                _mainCamera.enabled = enabled;
            }

            if (_enableLogging)
            {
                ChimeraLogger.LogInfo("CameraTransitionManager", "$1");
            }
        }

        /// <summary>
        /// Get camera statistics
        /// </summary>
        public CameraStatistics GetCameraStatistics()
        {
            return new CameraStatistics
            {
                IsInitialized = _isInitialized,
                CurrentLevel = _currentLevel,
                Position = GetCameraPosition(),
                Rotation = GetCameraRotation(),
                FieldOfView = GetFieldOfView(),
                IsEnabled = _enableBasicCamera && (_mainCamera != null ? _mainCamera.enabled : false)
            };
        }

        /// <summary>
        /// Zoom to specified camera level
        /// </summary>
        public bool ZoomTo(CameraLevel targetLevel)
        {
            SetCameraLevel(targetLevel);
            return true;
        }

        /// <summary>
        /// Zoom to specified camera level with anchor
        /// </summary>
        public bool ZoomTo(CameraLevel targetLevel, Transform anchor)
        {
            SetCameraLevel(targetLevel);
            return true;
        }

        /// <summary>
        /// Zoom to specified camera level with custom position
        /// </summary>
        public bool ZoomTo(CameraLevel targetLevel, Vector3 customPosition)
        {
            SetCameraLevel(targetLevel);
            SetCameraPosition(customPosition, GetCameraRotation());
            return true;
        }

        /// <summary>
        /// Get optimal transition duration between levels
        /// </summary>
        public float GetOptimalTransitionDuration(CameraLevel fromLevel, CameraLevel toLevel)
        {
            return 1f; // Default transition duration
        }

        /// <summary>
        /// Orbit camera around target
        /// </summary>
        public void OrbitAroundTarget(float yaw, float pitch, float duration = -1f)
        {
            if (_mainCamera == null) return;

            var currentRotation = _mainCamera.transform.rotation;
            var targetRotation = Quaternion.Euler(pitch, yaw, 0);
            _mainCamera.transform.rotation = targetRotation;

            if (_enableLogging)
            {
                ChimeraLogger.LogInfo("CameraTransitionManager", "$1");
            }
        }

        /// <summary>
        /// Focus camera on target
        /// </summary>
        public bool FocusOnTarget(Transform target)
        {
            if (target == null || _mainCamera == null) return false;

            var direction = (target.position - _mainCamera.transform.position).normalized;
            var targetRotation = Quaternion.LookRotation(direction);
            _mainCamera.transform.rotation = targetRotation;

            if (_enableLogging)
            {
                ChimeraLogger.LogInfo("CameraTransitionManager", "$1");
            }
            return true;
        }

        /// <summary>
        /// Move camera to specific position and rotation
        /// </summary>
        public void MoveCameraToPosition(Vector3 position, Quaternion rotation, float duration = -1f)
        {
            SetCameraPosition(position, rotation);
        }

        /// <summary>
        /// Get level transition speed
        /// </summary>
        public float GetLevelTransitionSpeed(CameraLevel level)
        {
            return 1f; // Default transition speed
        }

        /// <summary>
        /// Focus camera on specific position
        /// </summary>
        public bool FocusOnPosition(Vector3 position, Transform anchorReference = null)
        {
            if (_mainCamera == null) return false;

            var direction = (position - _mainCamera.transform.position).normalized;
            var targetRotation = Quaternion.LookRotation(direction);
            _mainCamera.transform.rotation = targetRotation;

            if (_enableLogging)
            {
                ChimeraLogger.LogInfo("CameraTransitionManager", "$1");
            }
            return true;
        }
    }

    // CameraLevel enum is defined in ProjectChimera.Data.Camera namespace

    /// <summary>
    /// Basic camera statistics
    /// </summary>
    [System.Serializable]
    public class CameraStatistics
    {
        public bool IsInitialized;
        public CameraLevel CurrentLevel;
        public Vector3 Position;
        public Quaternion Rotation;
        public float FieldOfView;
        public bool IsEnabled;
    }

    /// <summary>
    /// Camera transition type enumeration
    /// </summary>
    public enum TransitionType
    {
        None,
        Pan,
        Zoom,
        Focus,
        Level
    }

    /// <summary>
    /// Camera transition information structure
    /// </summary>
    [System.Serializable]
    public struct CameraTransitionInfo
    {
        public Vector3 TargetPosition;
        public Quaternion TargetRotation;
        public float TargetFieldOfView;
        public TransitionType Type;
        public float Duration;
        public bool IsValid;

        public CameraTransitionInfo(Vector3 position, Quaternion rotation, float fov = 60f, TransitionType type = TransitionType.Focus, float duration = 1f)
        {
            TargetPosition = position;
            TargetRotation = rotation;
            TargetFieldOfView = fov;
            Type = type;
            Duration = duration;
            IsValid = true;
        }

        public static CameraTransitionInfo Invalid => new CameraTransitionInfo { IsValid = false };
    }
}
