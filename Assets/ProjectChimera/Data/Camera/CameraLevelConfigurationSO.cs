using UnityEngine;
using ProjectChimera.Shared;

namespace ProjectChimera.Data.Camera
{
    /// <summary>
    /// ScriptableObject configuration for camera level semantics and offsets
    /// Phase 3 implementation - allows per-scene camera behavior customization
    /// </summary>
    [CreateAssetMenu(fileName = "New Camera Level Configuration", menuName = "Project Chimera/Camera/Level Configuration", order = 300)]
    public class CameraLevelConfigurationSO : ChimeraDataSO
    {
        [Header("Configuration Metadata")]
        [SerializeField] private string _configurationName = "Default";
        [SerializeField, TextArea(2, 4)] private string _description = "Camera level configuration for scene";
        [SerializeField] private SceneType _targetSceneType = SceneType.Facility;
        
        [Header("Facility Level Configuration")]
        [SerializeField] private CameraLevelSettings _facilityLevelSettings = new CameraLevelSettings
        {
            Distance = 25f,
            Height = 20f,
            FieldOfView = 60f,
            PitchConstraints = new Vector2(-10f, 45f),
            TransitionSpeed = 2f,
            SemanticName = "Facility Overview",
            Description = "Wide view of entire facility"
        };
        
        [Header("Room Level Configuration")]
        [SerializeField] private CameraLevelSettings _roomLevelSettings = new CameraLevelSettings
        {
            Distance = 15f,
            Height = 12f,
            FieldOfView = 50f,
            PitchConstraints = new Vector2(-5f, 60f),
            TransitionSpeed = 1.8f,
            SemanticName = "Room View",
            Description = "Individual room or greenhouse view"
        };
        
        [Header("Bench Level Configuration")]
        [SerializeField] private CameraLevelSettings _benchLevelSettings = new CameraLevelSettings
        {
            Distance = 8f,
            Height = 5f,
            FieldOfView = 45f,
            PitchConstraints = new Vector2(0f, 70f),
            TransitionSpeed = 1.5f,
            SemanticName = "Bench View",
            Description = "Growing bench or table level view"
        };
        
        [Header("Plant Level Configuration")]
        [SerializeField] private CameraLevelSettings _plantLevelSettings = new CameraLevelSettings
        {
            Distance = 3f,
            Height = 2f,
            FieldOfView = 35f,
            PitchConstraints = new Vector2(10f, 80f),
            TransitionSpeed = 1.2f,
            SemanticName = "Plant Detail",
            Description = "Individual plant inspection view"
        };
        
        [Header("Advanced Configuration")]
        [SerializeField] private bool _enableLevelLocking = false;
        [SerializeField] private CameraLevel[] _availableLevels = { CameraLevel.Facility, CameraLevel.Room, CameraLevel.Bench, CameraLevel.Plant };
        [SerializeField] private LayerMask _facilityLevelLayers = -1;
        [SerializeField] private LayerMask _roomLevelLayers = -1;
        [SerializeField] private LayerMask _benchLevelLayers = -1;
        [SerializeField] private LayerMask _plantLevelLayers = -1;
        
        [Header("Scene-Specific Offsets")]
        [SerializeField] private Vector3 _globalPositionOffset = Vector3.zero;
        [SerializeField] private Vector3 _globalRotationOffset = Vector3.zero;
        [SerializeField] private float _globalScaleMultiplier = 1f;
        
        /// <summary>
        /// Get camera level settings for specified level
        /// </summary>
        public CameraLevelSettings GetLevelSettings(CameraLevel level)
        {
            return level switch
            {
                CameraLevel.Facility => _facilityLevelSettings,
                CameraLevel.Room => _roomLevelSettings,
                CameraLevel.Bench => _benchLevelSettings,
                CameraLevel.Plant => _plantLevelSettings,
                _ => _facilityLevelSettings
            };
        }
        
        /// <summary>
        /// Get layer mask for specified camera level
        /// </summary>
        public LayerMask GetLevelLayerMask(CameraLevel level)
        {
            return level switch
            {
                CameraLevel.Facility => _facilityLevelLayers,
                CameraLevel.Room => _roomLevelLayers,
                CameraLevel.Bench => _benchLevelLayers,
                CameraLevel.Plant => _plantLevelLayers,
                _ => _facilityLevelLayers
            };
        }
        
        /// <summary>
        /// Check if a camera level is available in this configuration
        /// </summary>
        public bool IsLevelAvailable(CameraLevel level)
        {
            if (!_enableLevelLocking) return true;
            
            foreach (var availableLevel in _availableLevels)
            {
                if (availableLevel == level) return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Get all available camera levels for this configuration
        /// </summary>
        public CameraLevel[] GetAvailableLevels()
        {
            return _enableLevelLocking ? _availableLevels : new[] { CameraLevel.Facility, CameraLevel.Room, CameraLevel.Bench, CameraLevel.Plant };
        }
        
        /// <summary>
        /// Apply global offsets to a position
        /// </summary>
        public Vector3 ApplyGlobalPositionOffset(Vector3 originalPosition)
        {
            return originalPosition + _globalPositionOffset;
        }
        
        /// <summary>
        /// Apply global rotation offsets to a rotation
        /// </summary>
        public Quaternion ApplyGlobalRotationOffset(Quaternion originalRotation)
        {
            return originalRotation * Quaternion.Euler(_globalRotationOffset);
        }
        
        /// <summary>
        /// Apply global scale multiplier to a distance
        /// </summary>
        public float ApplyGlobalScaleMultiplier(float originalDistance)
        {
            return originalDistance * _globalScaleMultiplier;
        }
        
        /// <summary>
        /// Get semantic information for a camera level
        /// </summary>
        public string GetLevelSemanticName(CameraLevel level)
        {
            return GetLevelSettings(level).SemanticName;
        }
        
        /// <summary>
        /// Get description for a camera level
        /// </summary>
        public string GetLevelDescription(CameraLevel level)
        {
            return GetLevelSettings(level).Description;
        }
        
        /// <summary>
        /// Configuration metadata properties
        /// </summary>
        public string ConfigurationName => _configurationName;
        public string Description => _description;
        public SceneType TargetSceneType => _targetSceneType;
        public bool EnableLevelLocking => _enableLevelLocking;
        public Vector3 GlobalPositionOffset => _globalPositionOffset;
        public Vector3 GlobalRotationOffset => _globalRotationOffset;
        public float GlobalScaleMultiplier => _globalScaleMultiplier;
        
        protected override void OnValidate()
        {
            base.OnValidate();
            
            // Validate configuration settings
            ValidateConfiguration();
        }
        
        private void ValidateConfiguration()
        {
            // Ensure distances are positive and in proper hierarchy
            var facilityDist = _facilityLevelSettings.Distance;
            var roomDist = _roomLevelSettings.Distance;
            var benchDist = _benchLevelSettings.Distance;
            var plantDist = _plantLevelSettings.Distance;
            
            if (facilityDist <= roomDist || roomDist <= benchDist || benchDist <= plantDist)
            {
                Debug.LogWarning($"[CameraLevelConfigurationSO] {name}: Level distances should decrease from Facility to Plant");
            }
            
            // Ensure heights are positive
            if (_facilityLevelSettings.Height <= 0 || _roomLevelSettings.Height <= 0 || 
                _benchLevelSettings.Height <= 0 || _plantLevelSettings.Height <= 0)
            {
                Debug.LogWarning($"[CameraLevelConfigurationSO] {name}: All level heights should be positive");
            }
            
            // Ensure field of view values are reasonable
            foreach (var level in new[] { CameraLevel.Facility, CameraLevel.Room, CameraLevel.Bench, CameraLevel.Plant })
            {
                var settings = GetLevelSettings(level);
                if (settings.FieldOfView <= 0 || settings.FieldOfView >= 180)
                {
                    Debug.LogWarning($"[CameraLevelConfigurationSO] {name}: Field of view for {level} should be between 0 and 180 degrees");
                }
            }
            
            // Validate global scale multiplier
            if (_globalScaleMultiplier <= 0)
            {
                Debug.LogWarning($"[CameraLevelConfigurationSO] {name}: Global scale multiplier should be positive");
                _globalScaleMultiplier = 1f;
            }
            
            // Validate available levels
            if (_enableLevelLocking && (_availableLevels == null || _availableLevels.Length == 0))
            {
                Debug.LogWarning($"[CameraLevelConfigurationSO] {name}: Level locking enabled but no available levels specified");
            }
        }
    }
    
    /// <summary>
    /// Settings for a specific camera level
    /// </summary>
    [System.Serializable]
    public struct CameraLevelSettings
    {
        [Header("Positioning")]
        public float Distance;
        public float Height;
        public float FieldOfView;
        
        [Header("Constraints")]
        public Vector2 PitchConstraints; // x = min, y = max pitch in degrees
        
        [Header("Animation")]
        public float TransitionSpeed;
        
        [Header("Semantics")]
        public string SemanticName;
        [TextArea(2, 3)]
        public string Description;
        
        /// <summary>
        /// Check if the settings are valid
        /// </summary>
        public bool IsValid => Distance > 0 && Height > 0 && FieldOfView > 0 && TransitionSpeed > 0;
        
        /// <summary>
        /// Get pitch constraint for this level (clamped to valid range)
        /// </summary>
        public Vector2 GetValidPitchConstraints()
        {
            return new Vector2(
                Mathf.Clamp(PitchConstraints.x, -90f, 90f),
                Mathf.Clamp(PitchConstraints.y, -90f, 90f)
            );
        }
    }
    
    /// <summary>
    /// Scene types for camera configuration
    /// </summary>
    public enum SceneType
    {
        Facility = 0,       // Main facility/greenhouse scenes
        Laboratory = 1,     // Laboratory/research scenes
        Genetics = 2,       // Genetics lab scenes
        Outdoor = 3,        // Outdoor cultivation scenes
        Processing = 4,     // Post-harvest processing scenes
        Tutorial = 5        // Tutorial/training scenes
    }
    
    /// <summary>
    /// Camera level hierarchy for contextual zoom and interaction
    /// Phase 3 implementation - defines hierarchical camera semantics
    /// </summary>
    public enum CameraLevel
    {
        Facility = 0,   // Wide overview of entire facility
        Room = 1,       // Individual room/greenhouse view
        Bench = 2,      // Growing bench/table level
        Plant = 3       // Individual plant detail view
    }
}