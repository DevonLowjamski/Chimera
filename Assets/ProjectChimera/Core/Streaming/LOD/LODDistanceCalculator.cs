using UnityEngine;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core.Streaming.LOD
{
    /// <summary>
    /// REFACTORED: LOD Distance Calculator
    /// Focused component for calculating LOD levels based on distance and object properties
    /// </summary>
    public class LODDistanceCalculator : MonoBehaviour
    {
        [Header("Distance Settings")]
        [SerializeField] private float[] _lodDistances = { 25f, 50f, 100f, 200f };
        [SerializeField] private float _cullingDistance = 300f;
        [SerializeField] private bool _useScreenSpaceLOD = false;
        [SerializeField] private float _screenSpaceThreshold = 0.1f;

        [Header("Quality Profiles")]
        [SerializeField] private LODQualityProfile[] _qualityProfiles;
        [SerializeField] private int _currentProfileIndex = 1; // Medium quality by default

        [Header("Advanced Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private bool _useFrustumCulling = true;
        [SerializeField] private AnimationCurve _distanceCurve = AnimationCurve.Linear(0f, 0f, 1f, 1f);

        // Cached camera reference for screen space calculations
        private UnityEngine.Camera _mainCamera;

        // Properties
        public float[] LODDistances => _lodDistances;
        public float CullingDistance => _cullingDistance;
        public LODQualityProfile CurrentProfile => _qualityProfiles != null && _currentProfileIndex < _qualityProfiles.Length
            ? _qualityProfiles[_currentProfileIndex] : default;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            _mainCamera = UnityEngine.Camera.main;

            if (_qualityProfiles == null || _qualityProfiles.Length == 0)
            {
                InitializeDefaultProfiles();
            }

            ApplyQualityProfile(_currentProfileIndex);

            if (_enableLogging)
                ChimeraLogger.Log("LOD", "✅ LOD Distance Calculator initialized", this);
        }

        /// <summary>
        /// Calculate LOD level for an object based on distance and properties
        /// </summary>
        public int CalculateObjectLOD(LODObject lodObject, Transform lodCenter)
        {
            if (lodObject.GameObject == null || lodCenter == null)
                return GetMaxLODLevel();

            // Calculate basic distance
            float distance = Vector3.Distance(lodCenter.position, lodObject.Transform.position);

            // Apply custom bias
            distance /= lodObject.CustomBias;

            // Apply object type specific adjustments
            distance = ApplyObjectTypeAdjustment(distance, lodObject.ObjectType);

            // Screen space LOD calculation (if enabled)
            if (_useScreenSpaceLOD && _mainCamera != null)
            {
                float screenSize = CalculateScreenSpaceSize(lodObject.GameObject, _mainCamera);
                if (screenSize < _screenSpaceThreshold)
                {
                    distance *= 2f; // Increase effective distance for small screen objects
                }
            }

            // Frustum culling check
            if (_useFrustumCulling && _mainCamera != null)
            {
                if (!IsInCameraFrustum(lodObject.GameObject, _mainCamera))
                {
                    return GetMaxLODLevel(); // Use lowest LOD for objects outside frustum
                }
            }

            // Apply distance curve
            float normalizedDistance = distance / _cullingDistance;
            float curveValue = _distanceCurve.Evaluate(normalizedDistance);
            distance = curveValue * _cullingDistance;

            // Determine LOD level
            return CalculateLODFromDistance(distance);
        }

        /// <summary>
        /// Check if object should be culled based on distance
        /// </summary>
        public bool ShouldCullObject(LODObject lodObject, Transform lodCenter)
        {
            if (lodObject.GameObject == null || lodCenter == null)
                return true;

            float distance = Vector3.Distance(lodCenter.position, lodObject.Transform.position);
            distance /= lodObject.CustomBias;

            return distance > _cullingDistance;
        }

        /// <summary>
        /// Set quality profile
        /// </summary>
        public void SetQualityProfile(int profileIndex)
        {
            if (_qualityProfiles != null && profileIndex >= 0 && profileIndex < _qualityProfiles.Length)
            {
                _currentProfileIndex = profileIndex;
                ApplyQualityProfile(profileIndex);

                if (_enableLogging)
                    ChimeraLogger.Log("LOD", $"Quality profile changed to: {_qualityProfiles[profileIndex].ProfileName}", this);
            }
        }

        /// <summary>
        /// Set custom LOD distances
        /// </summary>
        public void SetLODDistances(float[] distances)
        {
            if (distances != null && distances.Length > 0)
            {
                _lodDistances = distances;
                if (_enableLogging)
                    ChimeraLogger.Log("LOD", $"LOD distances updated: [{string.Join(", ", distances)}]", this);
            }
        }

        /// <summary>
        /// Set culling distance
        /// </summary>
        public void SetCullingDistance(float distance)
        {
            _cullingDistance = distance;
            if (_enableLogging)
                ChimeraLogger.Log("LOD", $"Culling distance set to: {distance}", this);
        }

        /// <summary>
        /// Get maximum LOD level (lowest quality)
        /// </summary>
        public int GetMaxLODLevel()
        {
            return _lodDistances.Length;
        }

        private int CalculateLODFromDistance(float distance)
        {
            for (int i = 0; i < _lodDistances.Length; i++)
            {
                if (distance <= _lodDistances[i])
                {
                    return i;
                }
            }
            return _lodDistances.Length; // Beyond all LOD levels
        }

        private float ApplyObjectTypeAdjustment(float distance, LODObjectType objectType)
        {
            switch (objectType)
            {
                case LODObjectType.Plant:
                    return distance * 0.8f; // Plants get better LOD at distance
                case LODObjectType.Building:
                    return distance * 1.2f; // Buildings can use lower LOD sooner
                case LODObjectType.Equipment:
                    return distance * 0.9f; // Equipment gets slightly better LOD
                case LODObjectType.UI:
                    return distance * 0.5f; // UI elements get much better LOD
                case LODObjectType.Effect:
                    return distance * 1.5f; // Effects can degrade quickly
                default:
                    return distance;
            }
        }

        private float CalculateScreenSpaceSize(GameObject obj, Camera camera)
        {
            var renderer = obj.GetComponent<Renderer>();
            if (renderer == null) return 0f;

            var bounds = renderer.bounds;
            var screenPoint = camera.WorldToScreenPoint(bounds.center);

            if (screenPoint.z <= 0) return 0f; // Behind camera

            var size = bounds.size.magnitude;
            var distance = Vector3.Distance(camera.transform.position, bounds.center);

            return size / distance;
        }

        private bool IsInCameraFrustum(GameObject obj, Camera camera)
        {
            var renderer = obj.GetComponent<Renderer>();
            if (renderer == null) return true; // Assume visible if no renderer

            var frustumPlanes = GeometryUtility.CalculateFrustumPlanes(camera);
            return GeometryUtility.TestPlanesAABB(frustumPlanes, renderer.bounds);
        }

        private void ApplyQualityProfile(int profileIndex)
        {
            if (_qualityProfiles == null || profileIndex >= _qualityProfiles.Length) return;

            var profile = _qualityProfiles[profileIndex];
            if (profile.LODDistances != null && profile.LODDistances.Length > 0)
            {
                _lodDistances = profile.LODDistances;
                _cullingDistance = profile.CullingDistance;
            }
        }

        private void InitializeDefaultProfiles()
        {
            _qualityProfiles = new LODQualityProfile[]
            {
                new LODQualityProfile
                {
                    ProfileName = "Low Quality",
                    LODDistances = new float[] { 15f, 30f, 60f },
                    CullingDistance = 100f
                },
                new LODQualityProfile
                {
                    ProfileName = "Medium Quality",
                    LODDistances = new float[] { 25f, 50f, 100f },
                    CullingDistance = 200f
                },
                new LODQualityProfile
                {
                    ProfileName = "High Quality",
                    LODDistances = new float[] { 40f, 80f, 150f },
                    CullingDistance = 300f
                },
                new LODQualityProfile
                {
                    ProfileName = "Ultra Quality",
                    LODDistances = new float[] { 60f, 120f, 200f },
                    CullingDistance = 400f
                }
            };
        }

        /// <summary>
        /// Get distance calculation performance stats
        /// </summary>
        public LODDistanceStats GetPerformanceStats()
        {
            return new LODDistanceStats
            {
                ActiveLODDistances = _lodDistances,
                CullingDistance = _cullingDistance,
                CurrentProfileName = CurrentProfile.ProfileName,
                UseScreenSpaceLOD = _useScreenSpaceLOD,
                UseFrustumCulling = _useFrustumCulling
            };
        }
    }

    /// <summary>
    /// LOD quality profile data structure
    /// </summary>
    [System.Serializable]
    public struct LODQualityProfile
    {
        public string ProfileName;
        public float[] LODDistances;
        public float CullingDistance;
    }

    /// <summary>
    /// LOD distance calculation performance statistics
    /// </summary>
    [System.Serializable]
    public struct LODDistanceStats
    {
        public float[] ActiveLODDistances;
        public float CullingDistance;
        public string CurrentProfileName;
        public bool UseScreenSpaceLOD;
        public bool UseFrustumCulling;
    }
}