using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Rendering.Environmental
{
    /// <summary>
    /// REFACTORED: Focused Environmental Performance Optimizer
    /// Handles only LOD management, culling, and environmental rendering optimization
    /// </summary>
    public class EnvironmentalPerformanceOptimizer : MonoBehaviour
    {
        [Header("Performance Settings")]
        [SerializeField] private bool _enableOptimization = true;
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private float _optimizationUpdateInterval = 1f;
        [SerializeField] private int _maxEnvironmentalObjects = 200;

        [Header("LOD Settings")]
        [SerializeField] private bool _enableLOD = true;
        [SerializeField] private float _lodDistance1 = 50f;
        [SerializeField] private float _lodDistance2 = 100f;
        [SerializeField] private float _lodDistance3 = 200f;

        [Header("Culling Settings")]
        [SerializeField] private bool _enableFrustumCulling = true;
        [SerializeField] private bool _enableDistanceCulling = true;
        [SerializeField] private float _maxRenderDistance = 300f;
        [SerializeField] private float _cullCheckInterval = 0.5f;

        [Header("Quality Settings")]
        [SerializeField] private EnvironmentalQualityLevel _qualityLevel = EnvironmentalQualityLevel.High;
        [SerializeField] private bool _adaptiveQuality = true;
        [SerializeField] private float _targetFrameRate = 60f;

        // Performance state
        private readonly List<EnvironmentalRenderable> _environmentalObjects = new List<EnvironmentalRenderable>();
        private readonly List<EnvironmentalRenderable> _visibleObjects = new List<EnvironmentalRenderable>();
        private UnityEngine.Camera _mainCamera;
        private float _lastOptimizationUpdate;
        private float _lastCullCheck;
        private int _frameCount;
        private float _frameTimeAccumulator;

        // Performance metrics
        private EnvironmentalPerformanceMetrics _performanceMetrics;

        // Properties
        public bool IsEnabled => _enableOptimization;
        public EnvironmentalQualityLevel CurrentQuality => _qualityLevel;
        public int ManagedObjectCount => _environmentalObjects.Count;
        public int VisibleObjectCount => _visibleObjects.Count;

        // Events
        public System.Action<EnvironmentalQualityLevel> OnQualityLevelChanged;
        public System.Action<EnvironmentalPerformanceMetrics> OnPerformanceMetricsUpdated;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            _mainCamera = UnityEngine.Camera.main;
            _performanceMetrics = new EnvironmentalPerformanceMetrics();

            ApplyQualitySettings();

            if (_enableLogging)
                ChimeraLogger.Log("RENDERING", "✅ Environmental performance optimizer initialized", this);
        }

        /// <summary>
        /// Update environmental optimizations - called by EnvironmentalRendererCore
        /// </summary>
        public void UpdateOptimizations(float deltaTime)
        {
            if (!_enableOptimization) return;

            UpdatePerformanceMetrics(deltaTime);

            if (Time.time - _lastOptimizationUpdate >= _optimizationUpdateInterval)
            {
                if (_adaptiveQuality)
                {
                    UpdateAdaptiveQuality();
                }

                if (_enableLOD)
                {
                    UpdateLODLevels();
                }

                _lastOptimizationUpdate = Time.time;
            }

            if (Time.time - _lastCullCheck >= _cullCheckInterval)
            {
                if (_enableFrustumCulling || _enableDistanceCulling)
                {
                    UpdateCulling();
                }
                _lastCullCheck = Time.time;
            }
        }

        /// <summary>
        /// Register environmental object for optimization
        /// </summary>
        public void RegisterEnvironmentalObject(EnvironmentalRenderable environmentalObject)
        {
            if (environmentalObject != null && !_environmentalObjects.Contains(environmentalObject))
            {
                _environmentalObjects.Add(environmentalObject);

                if (_environmentalObjects.Count > _maxEnvironmentalObjects)
                {
                    OptimizeObjectPool();
                }

                if (_enableLogging)
                    ChimeraLogger.Log("RENDERING", $"Registered environmental object: {environmentalObject.name}", this);
            }
        }

        /// <summary>
        /// Unregister environmental object from optimization
        /// </summary>
        public void UnregisterEnvironmentalObject(EnvironmentalRenderable environmentalObject)
        {
            if (_environmentalObjects.Remove(environmentalObject))
            {
                _visibleObjects.Remove(environmentalObject);

                if (_enableLogging)
                    ChimeraLogger.Log("RENDERING", $"Unregistered environmental object: {environmentalObject?.name}", this);
            }
        }

        /// <summary>
        /// Set environmental quality level
        /// </summary>
        public void SetQualityLevel(EnvironmentalQualityLevel qualityLevel)
        {
            if (_qualityLevel == qualityLevel) return;

            var previousQuality = _qualityLevel;
            _qualityLevel = qualityLevel;
            ApplyQualitySettings();

            OnQualityLevelChanged?.Invoke(_qualityLevel);

            if (_enableLogging)
                ChimeraLogger.Log("RENDERING", $"Environmental quality changed: {previousQuality} → {_qualityLevel}", this);
        }

        /// <summary>
        /// Enable/disable performance optimization
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            _enableOptimization = enabled;

            if (!enabled)
            {
                // Reset all objects to full visibility when disabled
                foreach (var obj in _environmentalObjects)
                {
                    obj?.SetVisible(true);
                    obj?.SetLODLevel(0);
                }
            }

            if (_enableLogging)
                ChimeraLogger.Log("RENDERING", $"Environmental optimization: {(enabled ? "enabled" : "disabled")}", this);
        }

        /// <summary>
        /// Force immediate optimization update
        /// </summary>
        public void ForceOptimizationUpdate()
        {
            if (!_enableOptimization) return;

            UpdateLODLevels();
            UpdateCulling();

            if (_enableLogging)
                ChimeraLogger.Log("RENDERING", "Forced environmental optimization update", this);
        }

        private void UpdatePerformanceMetrics(float deltaTime)
        {
            _frameCount++;
            _frameTimeAccumulator += deltaTime;

            if (_frameTimeAccumulator >= 1f)
            {
                _performanceMetrics.CurrentFPS = _frameCount / _frameTimeAccumulator;
                _performanceMetrics.AverageFrameTime = _frameTimeAccumulator / _frameCount * 1000f; // Convert to ms
                _performanceMetrics.VisibleObjectCount = _visibleObjects.Count;
                _performanceMetrics.TotalObjectCount = _environmentalObjects.Count;
                _performanceMetrics.CurrentQualityLevel = _qualityLevel;

                OnPerformanceMetricsUpdated?.Invoke(_performanceMetrics);

                _frameCount = 0;
                _frameTimeAccumulator = 0f;
            }
        }

        private void UpdateAdaptiveQuality()
        {
            var currentFPS = _performanceMetrics.CurrentFPS;

            if (currentFPS < _targetFrameRate * 0.8f && _qualityLevel > EnvironmentalQualityLevel.Low)
            {
                // Reduce quality if FPS is too low
                SetQualityLevel((EnvironmentalQualityLevel)((int)_qualityLevel - 1));
            }
            else if (currentFPS > _targetFrameRate * 1.1f && _qualityLevel < EnvironmentalQualityLevel.Ultra)
            {
                // Increase quality if FPS is stable and high
                SetQualityLevel((EnvironmentalQualityLevel)((int)_qualityLevel + 1));
            }
        }

        private void UpdateLODLevels()
        {
            if (_mainCamera == null) return;

            var cameraPosition = _mainCamera.transform.position;

            foreach (var obj in _environmentalObjects)
            {
                if (obj == null) continue;

                var distance = Vector3.Distance(cameraPosition, obj.transform.position);
                var lodLevel = CalculateLODLevel(distance);
                obj.SetLODLevel(lodLevel);
            }
        }

        private void UpdateCulling()
        {
            if (_mainCamera == null) return;

            _visibleObjects.Clear();
            var cameraPosition = _mainCamera.transform.position;
            var frustumPlanes = GeometryUtility.CalculateFrustumPlanes(_mainCamera);

            foreach (var obj in _environmentalObjects)
            {
                if (obj == null) continue;

                bool isVisible = true;

                // Distance culling
                if (_enableDistanceCulling)
                {
                    var distance = Vector3.Distance(cameraPosition, obj.transform.position);
                    if (distance > _maxRenderDistance)
                    {
                        isVisible = false;
                    }
                }

                // Frustum culling
                if (isVisible && _enableFrustumCulling)
                {
                    var bounds = obj.GetBounds();
                    if (!GeometryUtility.TestPlanesAABB(frustumPlanes, bounds))
                    {
                        isVisible = false;
                    }
                }

                obj.SetVisible(isVisible);
                if (isVisible)
                {
                    _visibleObjects.Add(obj);
                }
            }
        }

        private int CalculateLODLevel(float distance)
        {
            if (distance <= _lodDistance1) return 0;
            if (distance <= _lodDistance2) return 1;
            if (distance <= _lodDistance3) return 2;
            return 3;
        }

        private void ApplyQualitySettings()
        {
            switch (_qualityLevel)
            {
                case EnvironmentalQualityLevel.Low:
                    _maxEnvironmentalObjects = 50;
                    _maxRenderDistance = 150f;
                    _lodDistance1 = 25f;
                    _lodDistance2 = 50f;
                    _lodDistance3 = 100f;
                    break;

                case EnvironmentalQualityLevel.Medium:
                    _maxEnvironmentalObjects = 100;
                    _maxRenderDistance = 200f;
                    _lodDistance1 = 35f;
                    _lodDistance2 = 75f;
                    _lodDistance3 = 150f;
                    break;

                case EnvironmentalQualityLevel.High:
                    _maxEnvironmentalObjects = 200;
                    _maxRenderDistance = 300f;
                    _lodDistance1 = 50f;
                    _lodDistance2 = 100f;
                    _lodDistance3 = 200f;
                    break;

                case EnvironmentalQualityLevel.Ultra:
                    _maxEnvironmentalObjects = 500;
                    _maxRenderDistance = 500f;
                    _lodDistance1 = 75f;
                    _lodDistance2 = 150f;
                    _lodDistance3 = 300f;
                    break;
            }
        }

        private void OptimizeObjectPool()
        {
            // Remove distant objects when pool is full
            if (_mainCamera == null) return;

            var cameraPosition = _mainCamera.transform.position;
            var objectsWithDistance = new List<(EnvironmentalRenderable obj, float distance)>();

            foreach (var obj in _environmentalObjects)
            {
                if (obj != null)
                {
                    var distance = Vector3.Distance(cameraPosition, obj.transform.position);
                    objectsWithDistance.Add((obj, distance));
                }
            }

            // Sort by distance and remove the farthest objects
            objectsWithDistance.Sort((a, b) => b.distance.CompareTo(a.distance));
            var objectsToRemove = objectsWithDistance.Count - _maxEnvironmentalObjects;

            for (int i = 0; i < objectsToRemove; i++)
            {
                var objToRemove = objectsWithDistance[i].obj;
                _environmentalObjects.Remove(objToRemove);
                _visibleObjects.Remove(objToRemove);
            }
        }

        /// <summary>
        /// Get environmental optimization performance statistics
        /// </summary>
        public EnvironmentalOptimizationPerformanceStats GetPerformanceStats()
        {
            return new EnvironmentalOptimizationPerformanceStats
            {
                IsEnabled = _enableOptimization,
                QualityLevel = _qualityLevel,
                ManagedObjectCount = _environmentalObjects.Count,
                VisibleObjectCount = _visibleObjects.Count,
                MaxRenderDistance = _maxRenderDistance,
                CurrentFPS = _performanceMetrics.CurrentFPS,
                LODEnabled = _enableLOD,
                CullingEnabled = _enableFrustumCulling || _enableDistanceCulling
            };
        }
    }

    /// <summary>
    /// Interface for environmental objects that can be optimized
    /// </summary>
    public interface EnvironmentalRenderable
    {
        string name { get; }
        Transform transform { get; }
        void SetVisible(bool visible);
        void SetLODLevel(int lodLevel);
        Bounds GetBounds();
    }

    /// <summary>
    /// Environmental quality levels
    /// </summary>
    public enum EnvironmentalQualityLevel
    {
        Low = 0,
        Medium = 1,
        High = 2,
        Ultra = 3
    }

    /// <summary>
    /// Environmental performance metrics
    /// </summary>
    [System.Serializable]
    public struct EnvironmentalPerformanceMetrics
    {
        public float CurrentFPS;
        public float AverageFrameTime;
        public int VisibleObjectCount;
        public int TotalObjectCount;
        public EnvironmentalQualityLevel CurrentQualityLevel;
    }

    /// <summary>
    /// Environmental optimization performance statistics
    /// </summary>
    [System.Serializable]
    public struct EnvironmentalOptimizationPerformanceStats
    {
        public bool IsEnabled;
        public EnvironmentalQualityLevel QualityLevel;
        public int ManagedObjectCount;
        public int VisibleObjectCount;
        public float MaxRenderDistance;
        public float CurrentFPS;
        public bool LODEnabled;
        public bool CullingEnabled;
    }
}
