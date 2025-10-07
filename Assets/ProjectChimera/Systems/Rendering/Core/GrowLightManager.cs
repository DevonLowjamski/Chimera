using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Rendering.Core
{
    /// <summary>
    /// REFACTORED: Grow Light Manager
    /// Focused component for managing grow light creation, updates, and lifecycle
    /// </summary>
    public class GrowLightManager : MonoBehaviour
    {
        [Header("Grow Light Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private int _maxGrowLights = 100;
        [SerializeField] private float _growLightRange = 10f;
        [SerializeField] private Color _growLightColor = new Color(1f, 0.8f, 0.6f, 1f);
        [SerializeField] private float _growLightIntensity = 2f;
        [SerializeField] private bool _enableLightLOD = true;
        [SerializeField] private float _lightLODDistance = 50f;

        // Grow light management
        private readonly List<GrowLight> _growLights = new List<GrowLight>();
        private readonly Queue<Light> _lightPool = new Queue<Light>();
        private readonly List<Light> _activeLights = new List<Light>();

        // Performance tracking
        private GrowLightStats _stats = new GrowLightStats();
        private UnityEngine.Camera _mainCamera;

        // Properties
        public bool IsEnabled { get; private set; } = true;
        public int GrowLightCount => _growLights.Count;
        public int ActiveLightCount => _activeLights.Count;

        // Events
        public System.Action<GrowLight> OnGrowLightCreated;
        public System.Action<GrowLight> OnGrowLightDestroyed;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            _mainCamera = UnityEngine.Camera.main;
            InitializeLightPool();
            ResetStats();

            if (_enableLogging)
                ChimeraLogger.Log("RENDERING", "✅ GrowLightManager initialized", this);
        }

        /// <summary>
        /// Add grow light to the system
        /// </summary>
        public GrowLight AddGrowLight(Vector3 position, Color color, float intensity, float range)
        {
            if (!IsEnabled) return null;
            if (_growLights.Count >= _maxGrowLights)
            {
                if (_enableLogging)
                    ChimeraLogger.LogWarning("RENDERING", $"Maximum grow lights reached: {_maxGrowLights}", this);
                return null;
            }

            var growLight = new GrowLight
            {
                ID = _growLights.Count,
                Position = position,
                Color = color,
                Intensity = intensity,
                Range = range,
                IsActive = true,
                LightObject = CreateGrowLightObject(position, color, intensity, range)
            };

            _growLights.Add(growLight);
            _stats.GrowLightsCreated++;

            OnGrowLightCreated?.Invoke(growLight);

            if (_enableLogging)
                ChimeraLogger.Log("RENDERING", $"Created grow light {growLight.ID} at {position}", this);

            return growLight;
        }

        /// <summary>
        /// Remove grow light from the system
        /// </summary>
        public void RemoveGrowLight(GrowLight growLight)
        {
            if (!IsEnabled || growLight == null) return;

            if (_growLights.Contains(growLight))
            {
                if (growLight.LightObject != null)
                {
                    ReturnLightToPool(growLight.LightObject);
                }

                _growLights.Remove(growLight);
                _stats.GrowLightsDestroyed++;

                OnGrowLightDestroyed?.Invoke(growLight);

                if (_enableLogging)
                    ChimeraLogger.Log("RENDERING", $"Removed grow light {growLight.ID}", this);
            }
        }

        /// <summary>
        /// Update grow light parameters
        /// </summary>
        public void UpdateGrowLight(GrowLight growLight, Vector3? position = null, Color? color = null,
                                   float? intensity = null, float? range = null)
        {
            if (!IsEnabled || growLight == null) return;

            bool updated = false;

            if (position.HasValue) { growLight.Position = position.Value; updated = true; }
            if (color.HasValue) { growLight.Color = color.Value; updated = true; }
            if (intensity.HasValue) { growLight.Intensity = intensity.Value; updated = true; }
            if (range.HasValue) { growLight.Range = range.Value; updated = true; }

            if (updated)
            {
                UpdateLightObject(growLight);
                _stats.LightUpdates++;
            }
        }

        /// <summary>
        /// Update all grow lights (called by LightingCore)
        /// </summary>
        public void UpdateGrowLights()
        {
            if (!IsEnabled) return;

            Vector3 cameraPos = _mainCamera != null ? _mainCamera.transform.position : Vector3.zero;

            foreach (var growLight in _growLights)
            {
                if (!growLight.IsActive) continue;

                // Distance-based LOD adjustment
                if (_enableLightLOD)
                {
                    UpdateLightLOD(growLight, cameraPos);
                }
            }

            UpdateStats();
        }

        /// <summary>
        /// Get grow light statistics
        /// </summary>
        public GrowLightStats GetStats()
        {
            return _stats;
        }

        /// <summary>
        /// Get all grow lights
        /// </summary>
        public GrowLight[] GetGrowLights()
        {
            return _growLights.ToArray();
        }

        /// <summary>
        /// Set manager enabled/disabled
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;

            foreach (var growLight in _growLights)
            {
                if (growLight.LightObject != null)
                {
                    growLight.LightObject.SetActive(enabled && growLight.IsActive);
                }
            }

            if (_enableLogging)
                ChimeraLogger.Log("RENDERING", $"GrowLightManager: {(enabled ? "enabled" : "disabled")}", this);
        }

        private void InitializeLightPool()
        {
            for (int i = 0; i < _maxGrowLights; i++)
            {
                var lightObj = CreateLightObject();
                lightObj.SetActive(false);
                _lightPool.Enqueue(lightObj.GetComponent<Light>());
            }
        }

        private GameObject CreateGrowLightObject(Vector3 position, Color color, float intensity, float range)
        {
            Light light = GetLightFromPool();
            if (light == null) return null;

            light.transform.position = position;
            light.type = LightType.Point;
            light.color = color;
            light.intensity = intensity;
            light.range = range;
            light.shadows = LightShadows.Soft;
            light.gameObject.SetActive(true);

            _activeLights.Add(light);
            return light.gameObject;
        }

        private GameObject CreateLightObject()
        {
            var lightObj = new GameObject("PooledGrowLight");
            lightObj.transform.SetParent(transform);

            var light = lightObj.AddComponent<Light>();
            light.type = LightType.Point;
            light.cullingMask = -1; // Default to all layers

            return lightObj;
        }

        private Light GetLightFromPool()
        {
            if (_lightPool.Count > 0)
            {
                return _lightPool.Dequeue();
            }
            return null;
        }

        private void ReturnLightToPool(GameObject lightObject)
        {
            if (lightObject == null) return;

            var light = lightObject.GetComponent<Light>();
            if (light != null)
            {
                light.gameObject.SetActive(false);
                _activeLights.Remove(light);
                _lightPool.Enqueue(light);
            }
        }

        private void UpdateLightObject(GrowLight growLight)
        {
            if (growLight.LightObject == null) return;

            var light = growLight.LightObject.GetComponent<Light>();
            if (light != null)
            {
                light.transform.position = growLight.Position;
                light.color = growLight.Color;
                light.intensity = growLight.Intensity;
                light.range = growLight.Range;
            }
        }

        private void UpdateLightLOD(GrowLight growLight, Vector3 cameraPos)
        {
            if (growLight.LightObject == null) return;

            float distance = Vector3.Distance(cameraPos, growLight.Position);
            float lodFactor = Mathf.Clamp01(1f - (distance / _lightLODDistance));

            var light = growLight.LightObject.GetComponent<Light>();
            if (light != null)
            {
                light.intensity = growLight.Intensity * lodFactor;
            }
        }

        private void UpdateStats()
        {
            _stats.ActiveLights = _activeLights.Count;
            _stats.TotalGrowLights = _growLights.Count;
        }

        private void ResetStats()
        {
            _stats = new GrowLightStats
            {
                ActiveLights = 0,
                TotalGrowLights = 0,
                GrowLightsCreated = 0,
                GrowLightsDestroyed = 0,
                LightUpdates = 0
            };
        }
    }

    /// <summary>
    /// Grow light statistics
    /// </summary>
    [System.Serializable]
    public struct GrowLightStats
    {
        public int ActiveLights;
        public int TotalGrowLights;
        public int GrowLightsCreated;
        public int GrowLightsDestroyed;
        public int LightUpdates;
    }
}
