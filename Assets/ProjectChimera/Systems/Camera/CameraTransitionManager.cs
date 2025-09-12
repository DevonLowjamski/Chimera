using UnityEngine;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core;

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
        [SerializeField] private Camera _mainCamera;

        // Basic camera state
        private CameraLevel _currentLevel = CameraLevel.Facility;
        private bool _isInitialized = false;

        /// <summary>
        /// Events for level changes
        /// </summary>
        public event System.Action<CameraLevel> OnLevelChanged;

        /// <summary>
        /// Initialize basic camera management
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            if (_mainCamera == null)
            {
                _mainCamera = Camera.main;
            }

            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("[CameraTransitionManager] Initialized successfully");
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
                ChimeraLogger.Log($"[CameraTransitionManager] Set camera position: {position}");
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
                ChimeraLogger.Log($"[CameraTransitionManager] Set camera level to: {level}");
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
                ChimeraLogger.Log($"[CameraTransitionManager] Set FOV to: {fov}");
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
        /// Reset camera to default position
        /// </summary>
        public void ResetToDefault()
        {
            SetCameraLevel(CameraLevel.Facility);
            SetCameraPosition(new Vector3(0, 10, -10), Quaternion.Euler(30, 0, 0));
            SetFieldOfView(60f);

            if (_enableLogging)
            {
                ChimeraLogger.Log("[CameraTransitionManager] Reset camera to default");
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
                ChimeraLogger.Log($"[CameraTransitionManager] Camera {(enabled ? "enabled" : "disabled")}");
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
    }

    /// <summary>
    /// Camera level enum
    /// </summary>
    public enum CameraLevel
    {
        Facility,
        Room,
        Table,
        Plant
    }

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
}
