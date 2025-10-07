using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Rendering.Core
{
    /// <summary>
    /// REFACTORED: Lighting Performance Optimizer
    /// Focused component for optimizing lighting performance through culling, LOD, and quality adjustment
    /// </summary>
    public class LightingPerformanceOptimizer : MonoBehaviour
    {
        [Header("Performance Optimization Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private bool _enableLightCulling = true;
        [SerializeField] private float _lightCullingDistance = 100f;
        [SerializeField] private int _maxDynamicLights = 50;
        [SerializeField] private float _targetFrameTime = 16.67f; // 60 FPS
        [SerializeField] private float _shadowUpdateInterval = 0.2f;

        // System references
        private GrowLightManager _growLightManager;
        private VolumetricLightingManager _volumetricManager;
        private UnityEngine.Camera _mainCamera;

        // Performance tracking
        private PerformanceOptimizerStats _stats = new PerformanceOptimizerStats();
        private float _lastShadowUpdate;
        private int _lightUpdateIndex = 0;

        // Properties
        public bool IsEnabled { get; private set; } = true;
        public PerformanceOptimizerStats Stats => _stats;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            _mainCamera = UnityEngine.Camera.main;
            ResetStats();

            if (_enableLogging)
                ChimeraLogger.Log("RENDERING", "✅ LightingPerformanceOptimizer initialized", this);
        }

        /// <summary>
        /// Set grow light manager reference
        /// </summary>
        public void SetGrowLightManager(GrowLightManager growLightManager)
        {
            _growLightManager = growLightManager;
        }

        /// <summary>
        /// Set volumetric manager reference
        /// </summary>
        public void SetVolumetricManager(VolumetricLightingManager volumetricManager)
        {
            _volumetricManager = volumetricManager;
        }

        /// <summary>
        /// Optimize lighting performance (called by LightingCore)
        /// </summary>
        public void OptimizeLighting()
        {
            if (!IsEnabled) return;

            float currentFrameTime = Time.smoothDeltaTime * 1000f; // Convert to milliseconds
            _stats.AverageFrameTime = currentFrameTime;

            // Perform different optimization strategies based on performance
            if (currentFrameTime > _targetFrameTime * 1.5f) // 25ms = 40 FPS
            {
                PerformAggressiveOptimization();
            }
            else if (currentFrameTime > _targetFrameTime * 1.2f) // 20ms = 50 FPS
            {
                PerformModerateOptimization();
            }
            else
            {
                PerformLightOptimization();
            }

            // Update dynamic shadows at intervals
            if (Time.time - _lastShadowUpdate >= _shadowUpdateInterval)
            {
                UpdateDynamicShadows();
                _lastShadowUpdate = Time.time;
            }

            UpdateStats();
        }

        /// <summary>
        /// Force immediate lighting optimization
        /// </summary>
        public void ForceLightingOptimization()
        {
            if (!IsEnabled) return;

            PerformAggressiveOptimization();
            _stats.OptimizationEvents++;

            if (_enableLogging)
                ChimeraLogger.Log("RENDERING", "Forced lighting optimization executed", this);
        }

        /// <summary>
        /// Set maximum dynamic lights
        /// </summary>
        public void SetMaxDynamicLights(int maxLights)
        {
            _maxDynamicLights = Mathf.Max(1, maxLights);
        }

        /// <summary>
        /// Set light culling distance
        /// </summary>
        public void SetLightCullingDistance(float distance)
        {
            _lightCullingDistance = Mathf.Max(1f, distance);
        }

        /// <summary>
        /// Get performance statistics
        /// </summary>
        public PerformanceOptimizerStats GetStats()
        {
            return _stats;
        }

        /// <summary>
        /// Set optimizer enabled/disabled
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;

            if (_enableLogging)
                ChimeraLogger.Log("RENDERING", $"LightingPerformanceOptimizer: {(enabled ? "enabled" : "disabled")}", this);
        }

        private void PerformAggressiveOptimization()
        {
            // Disable volumetric lighting if active
            if (_volumetricManager != null && _volumetricManager.IsVolumetricEnabled)
            {
                _volumetricManager.SetVolumetricEnabled(false);
                _stats.VolumetricDisableEvents++;
            }

            // Reduce shadow quality
            ReduceShadowQuality();

            // Aggressively cull lights
            CullDistantLights(0.5f); // Use 50% of max lights

            _stats.AggressiveOptimizationEvents++;
        }

        private void PerformModerateOptimization()
        {
            // Reduce volumetric quality if enabled
            if (_volumetricManager != null && _volumetricManager.IsVolumetricEnabled)
            {
                _volumetricManager.SetVolumetricQuality(0.5f); // Half resolution
            }

            // Moderate light culling
            CullDistantLights(0.75f); // Use 75% of max lights

            _stats.ModerateOptimizationEvents++;
        }

        private void PerformLightOptimization()
        {
            // Standard light culling based on distance
            if (_enableLightCulling)
            {
                CullLightsByDistance();
            }

            _stats.StandardOptimizationEvents++;
        }

        private void CullLightsByDistance()
        {
            if (_growLightManager == null || _mainCamera == null) return;

            Vector3 cameraPos = _mainCamera.transform.position;
            var growLights = _growLightManager.GetGrowLights();

            foreach (var growLight in growLights)
            {
                float distance = Vector3.Distance(cameraPos, growLight.Position);
                bool shouldBeActive = distance <= _lightCullingDistance;

                if (growLight.IsActive != shouldBeActive)
                {
                    growLight.IsActive = shouldBeActive;

                    if (growLight.LightObject != null)
                    {
                        growLight.LightObject.SetActive(shouldBeActive);
                    }

                    if (shouldBeActive)
                        _stats.LightsActivated++;
                    else
                        _stats.LightsCulled++;
                }
            }
        }

        private void CullDistantLights(float maxLightsFactor = 1f)
        {
            if (_growLightManager == null || _mainCamera == null) return;

            Vector3 cameraPos = _mainCamera.transform.position;
            var growLights = _growLightManager.GetGrowLights();

            // Sort lights by distance
            var sortedLights = growLights
                .Where(light => light.LightObject != null)
                .OrderBy(light => Vector3.Distance(cameraPos, light.Position))
                .ToArray();

            int maxActiveLights = Mathf.RoundToInt(_maxDynamicLights * maxLightsFactor);

            // Activate closest lights, deactivate distant ones
            for (int i = 0; i < sortedLights.Length; i++)
            {
                var light = sortedLights[i];
                bool shouldBeActive = i < maxActiveLights;

                if (light.IsActive != shouldBeActive)
                {
                    light.IsActive = shouldBeActive;
                    light.LightObject.SetActive(shouldBeActive);

                    if (shouldBeActive)
                        _stats.LightsActivated++;
                    else
                        _stats.LightsCulled++;
                }
            }
        }

        private void UpdateDynamicShadows()
        {
            if (_growLightManager == null || _mainCamera == null) return;

            var growLights = _growLightManager.GetGrowLights();
            Vector3 cameraPos = _mainCamera.transform.position;

            // Stagger shadow updates across multiple frames
            int lightsPerFrame = Mathf.Max(1, growLights.Length / 5);

            for (int i = 0; i < lightsPerFrame && _lightUpdateIndex < growLights.Length; i++)
            {
                var growLight = growLights[_lightUpdateIndex];
                if (growLight.LightObject != null && growLight.LightObject.activeInHierarchy)
                {
                    var light = growLight.LightObject.GetComponent<Light>();
                    if (light != null)
                    {
                        // Update shadow settings based on distance
                        float distance = Vector3.Distance(cameraPos, growLight.Position);

                        if (distance < 25f) // Close
                        {
                            light.shadows = LightShadows.Soft;
                        }
                        else if (distance < 50f) // Medium
                        {
                            light.shadows = LightShadows.Hard;
                        }
                        else // Far
                        {
                            light.shadows = LightShadows.None;
                        }
                    }
                }

                _lightUpdateIndex++;
            }

            // Reset index when we've processed all lights
            if (_lightUpdateIndex >= growLights.Length)
            {
                _lightUpdateIndex = 0;
            }

            _stats.ShadowUpdates++;
        }

        private void ReduceShadowQuality()
        {
            if (_growLightManager == null) return;

            var growLights = _growLightManager.GetGrowLights();

            foreach (var growLight in growLights)
            {
                if (growLight.LightObject != null)
                {
                    var light = growLight.LightObject.GetComponent<Light>();
                    if (light != null && light.shadows == LightShadows.Soft)
                    {
                        light.shadows = LightShadows.Hard;
                    }
                }
            }

            QualitySettings.shadowResolution = ShadowResolution.Low;
            _stats.ShadowQualityReductions++;
        }

        private void UpdateStats()
        {
            if (_growLightManager != null)
            {
                _stats.ManagedLights = _growLightManager.GrowLightCount;
                _stats.ActiveManagedLights = _growLightManager.ActiveLightCount;
            }
        }

        private void ResetStats()
        {
            _stats = new PerformanceOptimizerStats
            {
                ManagedLights = 0,
                ActiveManagedLights = 0,
                LightsActivated = 0,
                LightsCulled = 0,
                OptimizationEvents = 0,
                AggressiveOptimizationEvents = 0,
                ModerateOptimizationEvents = 0,
                StandardOptimizationEvents = 0,
                VolumetricDisableEvents = 0,
                ShadowQualityReductions = 0,
                ShadowUpdates = 0,
                AverageFrameTime = 0f
            };
        }
    }

    /// <summary>
    /// Performance optimizer statistics
    /// </summary>
    [System.Serializable]
    public struct PerformanceOptimizerStats
    {
        public int ManagedLights;
        public int ActiveManagedLights;
        public int LightsActivated;
        public int LightsCulled;
        public int OptimizationEvents;
        public int AggressiveOptimizationEvents;
        public int ModerateOptimizationEvents;
        public int StandardOptimizationEvents;
        public int VolumetricDisableEvents;
        public int ShadowQualityReductions;
        public int ShadowUpdates;
        public float AverageFrameTime;
    }
}
