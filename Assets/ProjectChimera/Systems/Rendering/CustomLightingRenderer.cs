using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core;
using ProjectChimera.Systems.Rendering.Core;
using CoreGrowLight = ProjectChimera.Systems.Rendering.Core.GrowLight;

namespace ProjectChimera.Systems.Rendering
{
    /// <summary>
    /// REFACTORED: Custom Lighting Renderer - Legacy wrapper
    /// Now delegates to LightingCore for focused responsibility
    /// Maintains backward compatibility while providing improved architecture
    /// </summary>
    public class CustomLightingRenderer : MonoBehaviour
    {
        [Header("Legacy Lighting Settings")]
        [SerializeField] private bool _enableCustomLighting = true;
        [SerializeField] private bool _enableVolumetricLighting = false;
        [SerializeField] private bool _enableGrowLightOptimization = true;
        [SerializeField] private float _lightingUpdateInterval = 0.1f;

        [Header("Legacy Grow Light System")]
        [SerializeField] private int _maxGrowLights = 100;
        [SerializeField] private float _growLightRange = 10f;
        [SerializeField] private Color _growLightColor = new Color(1f, 0.8f, 0.6f, 1f);
        [SerializeField] private float _growLightIntensity = 2f;
        [SerializeField] private bool _enableLightCulling = true;

        [Header("Legacy Performance Settings")]
        [SerializeField] private int _maxDynamicLights = 50;
        [SerializeField] private float _lightCullingDistance = 100f;
        [SerializeField] private bool _enableLightLOD = true;
        [SerializeField] private float _lightLODDistance = 50f;

        // Core lighting system (delegation target)
        private LightingCore _lightingCore;

        // Legacy properties for backward compatibility
        public bool IsInitialized { get; private set; }
        public int ActiveLightCount => _lightingCore?.GetCombinedStats().ActiveLights ?? 0;
        public int GrowLightCount => _lightingCore?.GetCombinedStats().TotalGrowLights ?? 0;
        public LightingPerformanceStats Stats => _lightingCore?.GetCombinedStats() ?? new LightingPerformanceStats();

        // Legacy events for backward compatibility
        public System.Action<ProjectChimera.Systems.Rendering.GrowLight> OnGrowLightCreated;
        public System.Action<ProjectChimera.Systems.Rendering.GrowLight> OnGrowLightDestroyed;

        /// <summary>
        /// Initialize custom lighting renderer - delegates to LightingCore
        /// </summary>
        public void Initialize(bool enableVolumetric = false)
        {
            if (IsInitialized) return;

            _enableVolumetricLighting = enableVolumetric;

            // Initialize LightingCore
            InitializeLightingCore();

            IsInitialized = true;

            ChimeraLogger.Log("RENDERING", "✅ CustomLightingRenderer initialized (delegating to LightingCore)", this);
        }

        /// <summary>
        /// Add grow light - delegates to LightingCore
        /// </summary>
        public ProjectChimera.Systems.Rendering.GrowLight AddGrowLight(Vector3 position, Color color, float intensity, float range)
        {
            if (_lightingCore == null) return null;
            CoreGrowLight coreLight = _lightingCore.AddGrowLight(position, color, intensity, range);
            return ConvertFromCoreGrowLight(coreLight);
        }

        /// <summary>
        /// Remove grow light - delegates to LightingCore
        /// </summary>
        public void RemoveGrowLight(ProjectChimera.Systems.Rendering.GrowLight growLight)
        {
            _lightingCore?.RemoveGrowLight(ConvertToCoreGrowLight(growLight));
        }

        /// <summary>
        /// Update grow light - delegates to LightingCore
        /// </summary>
        public void UpdateGrowLight(ProjectChimera.Systems.Rendering.GrowLight growLight, Vector3? position = null, Color? color = null,
                                   float? intensity = null, float? range = null)
        {
            _lightingCore?.UpdateGrowLight(ConvertToCoreGrowLight(growLight), position, color, intensity, range);
        }

        /// <summary>
        /// Create lighting zone - delegates to LightingCore
        /// </summary>
        public int CreateLightingZone(Bounds bounds, LightingZoneType zoneType)
        {
            return _lightingCore?.CreateLightingZone(bounds, zoneType) ?? -1;
        }

        /// <summary>
        /// Update lighting - delegates to LightingCore (called every frame)
        /// </summary>
        public void UpdateLighting()
        {
            if (!IsInitialized || !_enableCustomLighting || _lightingCore == null) return;

            // LightingCore handles its own update timing through ITickable
            // This method maintained for backward compatibility
        }

        /// <summary>
        /// Force lighting optimization - delegates to LightingCore
        /// </summary>
        public void OptimizeLighting()
        {
            if (!IsInitialized) return;
            _lightingCore?.ForceLightingOptimization();
        }

        /// <summary>
        /// Set volumetric lighting enabled - delegates to LightingCore
        /// </summary>
        public void SetVolumetricLightingEnabled(bool enabled)
        {
            _enableVolumetricLighting = enabled;
            _lightingCore?.SetVolumetricLightingEnabled(enabled);
        }

        /// <summary>
        /// Get lighting statistics - delegates to LightingCore
        /// </summary>
        public LightingPerformanceStats GetLightingStats()
        {
            return _lightingCore?.GetCombinedStats() ?? new LightingPerformanceStats();
        }

        #region Private Methods

        private void InitializeLightingCore()
        {
            // Create LightingCore GameObject
            var coreGO = new GameObject("LightingCore");
            coreGO.transform.SetParent(transform);
            _lightingCore = coreGO.AddComponent<LightingCore>();

            // Configure LightingCore with our legacy settings
            if (_enableVolumetricLighting)
            {
                _lightingCore.SetVolumetricLightingEnabled(true);
            }

            // Subscribe to LightingCore events (optional - for legacy event forwarding)
            _lightingCore.OnGrowLightCreated += OnLegacyGrowLightCreated;
            _lightingCore.OnGrowLightDestroyed += OnLegacyGrowLightDestroyed;
            _lightingCore.OnStatsUpdated += OnLegacyStatsUpdated;
        }

        // Legacy event handlers for backward compatibility
        private void OnLegacyGrowLightCreated(CoreGrowLight light)
        {
            // Legacy event handling if needed - convert from Core to legacy format
            OnGrowLightCreated?.Invoke(ConvertFromCoreGrowLight(light));
        }

        private void OnLegacyGrowLightDestroyed(CoreGrowLight light)
        {
            // Legacy event handling if needed - convert from Core to legacy format
            OnGrowLightDestroyed?.Invoke(ConvertFromCoreGrowLight(light));
        }

        private void OnLegacyStatsUpdated(LightingPerformanceStats stats)
        {
            // Legacy stats handling if needed
        }

        // Legacy methods removed - functionality moved to LightingCore subsystems

        #endregion

        #region Type Conversion Methods

        /// <summary>
        /// Convert Core GrowLight to legacy format
        /// </summary>
        private ProjectChimera.Systems.Rendering.GrowLight ConvertFromCoreGrowLight(CoreGrowLight coreLight)
        {
            if (coreLight == null) return null;

            // Create a legacy wrapper with Core reference
            return new ProjectChimera.Systems.Rendering.GrowLight
            {
                Position = coreLight.Position,
                Color = coreLight.Color,
                Intensity = coreLight.Intensity,
                Range = coreLight.Range,
                IsActive = coreLight.IsActive,
                LightType = coreLight.LightType,
                ShadowType = coreLight.ShadowType,
                CoreReference = coreLight
            };
        }

        /// <summary>
        /// Convert legacy GrowLight to Core format
        /// </summary>
        private CoreGrowLight ConvertToCoreGrowLight(ProjectChimera.Systems.Rendering.GrowLight legacyLight)
        {
            if (legacyLight == null) return null;

            // This is tricky since we need the actual Core object, not just data
            // For now, return the internally stored CoreGrowLight
            // In practice, the legacy system should maintain a mapping
            return legacyLight.CoreReference as CoreGrowLight;
        }

        #endregion

        private void OnDestroy()
        {
            // LightingCore handles its own cleanup
        }
    }

    #region Legacy Data Structure Compatibility

    /// <summary>
    /// LEGACY: GrowLight wrapper - use LightingCore for new implementations
    /// </summary>
    [System.Serializable]
    public class LegacyGrowLight
    {
        public Vector3 Position;
        public Color Color;
        public float Intensity;
        public float Range;
        public bool IsActive;
        public LightType LightType;
        public LightShadows ShadowType;

        // Internal reference to Core object for conversion
        internal CoreGrowLight CoreReference;
    }

    #endregion
}