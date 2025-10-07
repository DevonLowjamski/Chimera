using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;

namespace ProjectChimera.Systems.Rendering.Core
{
    /// <summary>
    /// REFACTORED: Lighting Core - Central coordination for lighting subsystems
    /// Manages grow lights, volumetric lighting, performance optimization, and lighting zones
    /// Follows Single Responsibility Principle with focused subsystem coordination
    /// </summary>
    public class LightingCore : MonoBehaviour, ITickable
    {
        [Header("Core Lighting Settings")]
        [SerializeField] private bool _enableCustomLighting = true;
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private float _lightingUpdateInterval = 0.1f;

        // Core subsystems
        private GrowLightManager _growLightManager;
        private VolumetricLightingManager _volumetricLightingManager;
        private LightingPerformanceOptimizer _performanceOptimizer;
        private LightingZoneManager _zoneManager;

        // Properties
        public bool IsEnabled { get; private set; } = true;
        public int TickPriority => 15; // Medium-high priority for lighting
        public bool IsTickable => enabled && gameObject.activeInHierarchy && IsEnabled && _enableCustomLighting;

        // Statistics aggregation
        public LightingPerformanceStats GetCombinedStats()
        {
            var stats = new LightingPerformanceStats();

            if (_growLightManager != null)
            {
                var growStats = _growLightManager.GetStats();
                stats.ActiveLights = growStats.ActiveLights;
                stats.TotalGrowLights = growStats.TotalGrowLights;
                stats.GrowLightsCreated = growStats.GrowLightsCreated;
                stats.GrowLightsDestroyed = growStats.GrowLightsDestroyed;
                stats.LightUpdates = growStats.LightUpdates;
            }

            if (_zoneManager != null)
            {
                stats.LightingZones = _zoneManager.ActiveZoneCount;
            }

            if (_performanceOptimizer != null)
            {
                var perfStats = _performanceOptimizer.GetStats();
                stats.LightsActivated = perfStats.LightsActivated;
                stats.LightsCulled = perfStats.LightsCulled;
                stats.OptimizationEvents = perfStats.OptimizationEvents;
                stats.AverageFrameTime = perfStats.AverageFrameTime;
            }

            stats.LastUpdateTime = Time.time;
            return stats;
        }

        // Events
        public System.Action<GrowLight> OnGrowLightCreated;
        public System.Action<GrowLight> OnGrowLightDestroyed;
        public System.Action<LightingPerformanceStats> OnStatsUpdated;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            if (_enableLogging)
                ChimeraLogger.Log("RENDERING", "🔆 Initializing LightingCore subsystems...", this);

            // Initialize subsystems in dependency order
            InitializeGrowLightManager();
            InitializeVolumetricLightingManager();
            InitializeLightingZoneManager();
            InitializePerformanceOptimizer();

            // Register with UpdateOrchestrator
            UpdateOrchestrator.Instance?.RegisterTickable(this);

            if (_enableLogging)
                ChimeraLogger.Log("RENDERING", "✅ LightingCore initialized with all subsystems", this);
        }

        private void InitializeGrowLightManager()
        {
            var growLightGO = new GameObject("GrowLightManager");
            growLightGO.transform.SetParent(transform);
            _growLightManager = growLightGO.AddComponent<GrowLightManager>();

            _growLightManager.OnGrowLightCreated += (light) => OnGrowLightCreated?.Invoke(light);
            _growLightManager.OnGrowLightDestroyed += (light) => OnGrowLightDestroyed?.Invoke(light);
        }

        private void InitializeVolumetricLightingManager()
        {
            var volumetricGO = new GameObject("VolumetricLightingManager");
            volumetricGO.transform.SetParent(transform);
            _volumetricLightingManager = volumetricGO.AddComponent<VolumetricLightingManager>();
        }

        private void InitializeLightingZoneManager()
        {
            var zoneGO = new GameObject("LightingZoneManager");
            zoneGO.transform.SetParent(transform);
            _zoneManager = zoneGO.AddComponent<LightingZoneManager>();
        }

        private void InitializePerformanceOptimizer()
        {
            var optimizerGO = new GameObject("LightingPerformanceOptimizer");
            optimizerGO.transform.SetParent(transform);
            _performanceOptimizer = optimizerGO.AddComponent<LightingPerformanceOptimizer>();

            // Connect optimizer to other systems
            if (_growLightManager != null)
                _performanceOptimizer.SetGrowLightManager(_growLightManager);
            if (_volumetricLightingManager != null)
                _performanceOptimizer.SetVolumetricManager(_volumetricLightingManager);
        }

        public void Tick(float deltaTime)
        {
            if (!IsEnabled || !_enableCustomLighting) return;

            // Coordinate subsystem updates
            if (_growLightManager != null)
                _growLightManager.UpdateGrowLights();

            if (_zoneManager != null)
                _zoneManager.UpdateZones();

            if (_performanceOptimizer != null)
                _performanceOptimizer.OptimizeLighting();

            // Update stats
            OnStatsUpdated?.Invoke(GetCombinedStats());
        }

        /// <summary>
        /// Create grow light through GrowLightManager
        /// </summary>
        public GrowLight AddGrowLight(Vector3 position, Color color, float intensity, float range)
        {
            return _growLightManager?.AddGrowLight(position, color, intensity, range);
        }

        /// <summary>
        /// Remove grow light through GrowLightManager
        /// </summary>
        public void RemoveGrowLight(GrowLight growLight)
        {
            _growLightManager?.RemoveGrowLight(growLight);
        }

        /// <summary>
        /// Update grow light through GrowLightManager
        /// </summary>
        public void UpdateGrowLight(GrowLight growLight, Vector3? position = null, Color? color = null,
                                   float? intensity = null, float? range = null)
        {
            _growLightManager?.UpdateGrowLight(growLight, position, color, intensity, range);
        }

        /// <summary>
        /// Create lighting zone through ZoneManager
        /// </summary>
        public int CreateLightingZone(Bounds bounds, LightingZoneType zoneType)
        {
            return _zoneManager?.CreateLightingZone(bounds, zoneType) ?? -1;
        }

        /// <summary>
        /// Enable/disable volumetric lighting
        /// </summary>
        public void SetVolumetricLightingEnabled(bool enabled)
        {
            _volumetricLightingManager?.SetEnabled(enabled);
        }

        /// <summary>
        /// Force lighting optimization
        /// </summary>
        public void ForceLightingOptimization()
        {
            _performanceOptimizer?.ForceLightingOptimization();
        }

        /// <summary>
        /// Set system enabled/disabled
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;

            if (_growLightManager != null) _growLightManager.SetEnabled(enabled);
            if (_volumetricLightingManager != null) _volumetricLightingManager.SetEnabled(enabled);
            if (_zoneManager != null) _zoneManager.SetEnabled(enabled);
            if (_performanceOptimizer != null) _performanceOptimizer.SetEnabled(enabled);

            if (_enableLogging)
                ChimeraLogger.Log("RENDERING", $"LightingCore: {(enabled ? "enabled" : "disabled")}", this);
        }

        private void OnDestroy()
        {
            UpdateOrchestrator.Instance?.UnregisterTickable(this);
        }
    }

    #region Data Structures

    /// <summary>
    /// Lighting zone types
    /// </summary>
    public enum LightingZoneType
    {
        Indoor,
        Outdoor,
        Greenhouse,
        Laboratory
    }

    /// <summary>
    /// Grow light data structure
    /// </summary>
    [System.Serializable]
    public class GrowLight
    {
        public int ID;
        public Vector3 Position;
        public Color Color;
        public float Intensity;
        public float Range;
        public bool IsActive;
        public GameObject LightObject;
        public UnityEngine.LightType LightType = UnityEngine.LightType.Point;
        public UnityEngine.LightShadows ShadowType = UnityEngine.LightShadows.None;
    }

    /// <summary>
    /// Lighting zone data structure
    /// </summary>
    [System.Serializable]
    public class LightingZone
    {
        public int ID;
        public Bounds Bounds;
        public LightingZoneType ZoneType;
        public bool IsActive;
        public List<GrowLight> AssignedLights;
        public float LightingLevel;
        public float CreationTime;
        public int ActiveLightCount;
    }

    /// <summary>
    /// Combined lighting performance statistics
    /// </summary>
    [System.Serializable]
    public struct LightingPerformanceStats
    {
        public int ActiveLights;
        public int TotalGrowLights;
        public int GrowLightsCreated;
        public int GrowLightsDestroyed;
        public int LightsActivated;
        public int LightsCulled;
        public int LightUpdates;
        public int LightingZones;
        public int OptimizationEvents;
        public float AverageFrameTime;
        public float LastUpdateTime;
    }

    #endregion
}
