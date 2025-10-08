using ProjectChimera.Core.Logging;
using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectChimera.Core;
using System.Linq;
using Logger = ProjectChimera.Core.Logging.ChimeraLogger;

namespace ProjectChimera.Systems.Services.SpeedTree.Environmental
{
    /// <summary>
    /// Wind System for SpeedTree Plants
    /// Manages wind animations, wind zones, and wind effects on plants
    /// in the cannabis cultivation simulation.
    /// </summary>
    public class WindSystem : MonoBehaviour
    {
        [Header("Wind System Configuration")]
        [SerializeField] private bool _enableWindAnimation = true;
        [SerializeField] private float _globalWindStrength = 1.0f;
        [SerializeField] private Vector3 _globalWindDirection = Vector3.right;
        [SerializeField] private float _windVariationFrequency = 0.1f;
        [SerializeField] private float _windGustFrequency = 2.0f;

        // Wind zone management
        private Dictionary<WindZone, WindZoneSettings> _windZones = new Dictionary<WindZone, WindZoneSettings>();
        private List<WindZone> _activeWindZones = new List<WindZone>();

        // Global wind state
        private float _currentWindStrength = 0f;
        private Vector3 _currentWindDirection = Vector3.zero;
        private float _windVariationTimer = 0f;
        private float _windGustTimer = 0f;
        private float _gustStrength = 0f;

        // Shader property IDs for wind effects
        private int _windStrengthPropertyId;
        private int _windDirectionPropertyId;
        private int _windGustPropertyId;

        #region Public Events
        public event Action<float> OnWindStrengthChanged;
        public event Action<Vector3> OnWindDirectionChanged;
        public event Action<float> OnWindGust;
        #endregion

        #region Initialization
        public void Initialize()
        {
            Logger.Log("SPEEDTREE/WIND", "Initialize", this);

            // Cache shader property IDs
            CacheShaderProperties();

            // Initialize wind parameters
            _currentWindStrength = _globalWindStrength;
            _currentWindDirection = _globalWindDirection.normalized;

            // Find existing wind zones
            FindExistingWindZones();

            Logger.Log("SPEEDTREE/WIND", "Initialized", this);
        }

        public void Shutdown()
        {
            Logger.Log("SPEEDTREE/WIND", "Shutdown", this);

            _windZones.Clear();
            _activeWindZones.Clear();

            Logger.Log("SPEEDTREE/WIND", "Shutdown Complete", this);
        }

        private void CacheShaderProperties()
        {
            _windStrengthPropertyId = Shader.PropertyToID("_WindStrength");
            _windDirectionPropertyId = Shader.PropertyToID("_WindDirection");
            _windGustPropertyId = Shader.PropertyToID("_WindGust");
        }

        private void FindExistingWindZones()
        {
            // Primary: Try GameObjectRegistry for registered WindZones
            var registry = ServiceContainerFactory.Instance?.TryResolve<ProjectChimera.Core.Performance.IGameObjectRegistry>();
            var windZones = registry?.GetAll<WindZone>();

            if (windZones != null && windZones.Any())
            {
                Logger.Log("SPEEDTREE/WIND", "Resolved WindZones from GameObjectRegistry", this);
            }
            else
            {
                Logger.Log("SPEEDTREE/WIND", "Using registered WindZones", this);
            }

            foreach (var windZone in windZones)
            {
                RegisterWindZone(windZone);
            }
        }
        #endregion

        #region Wind Zone Management

        /// <summary>
        /// Registers a wind zone with the system
        /// </summary>
        public void RegisterWindZone(WindZone windZone)
        {
            if (windZone == null || _windZones.ContainsKey(windZone)) return;

            try
            {
                var settings = new WindZoneSettings(windZone);
                _windZones[windZone] = settings;
                _activeWindZones.Add(windZone);

                Logger.Log("SPEEDTREE/WIND", "Registered wind zone", this);
            }
            catch (Exception ex)
            {
                Logger.LogWarning("SPEEDTREE/WIND", "RegisterWindZone failed", this);
            }
        }

        /// <summary>
        /// Unregisters a wind zone from the system
        /// </summary>
        public void UnregisterWindZone(WindZone windZone)
        {
            if (windZone == null) return;

            _windZones.Remove(windZone);
            _activeWindZones.Remove(windZone);

            Logger.Log("SPEEDTREE/WIND", "Unregistered wind zone", this);
        }

        /// <summary>
        /// Updates a specific wind zone
        /// </summary>
        public void UpdateWindZone(WindZone windZone)
        {
            if (windZone == null || !_windZones.TryGetValue(windZone, out var settings)) return;

            try
            {
                // Update zone-specific wind parameters
                UpdateWindZoneParameters(windZone, settings);

                // Apply wind effects to SpeedTree renderers in range
                ApplyWindToZoneRenderers(windZone, settings);
            }
            catch (Exception ex)
            {
                Logger.Log("SPEEDTREE/WIND", "Wind gust", this);
            }
        }

        #endregion

        #region Global Wind System

        /// <summary>
        /// Updates the global wind system
        /// </summary>
        public void UpdateGlobalWind()
        {
            if (!_enableWindAnimation) return;

            try
            {
                // Update wind variation
                _windVariationTimer += Time.deltaTime;
                if (_windVariationTimer >= _windVariationFrequency)
                {
                    UpdateWindVariation();
                    _windVariationTimer = 0f;
                }

                // Update wind gusts
                _windGustTimer += Time.deltaTime;
                if (_windGustTimer >= _windGustFrequency)
                {
                    UpdateWindGusts();
                    _windGustTimer = 0f;
                }

                // Apply global wind to all SpeedTree renderers
                ApplyGlobalWindToRenderers();
            }
            catch (Exception ex)
            {
                Logger.Log("SPEEDTREE/WIND", "Registered discovered Renderers", this);
            }
        }

        private void UpdateWindVariation()
        {
            // Add natural wind variation
            float variationStrength = UnityEngine.Random.Range(0.8f, 1.2f);
            float newStrength = _globalWindStrength * variationStrength;

            // Slight direction variation
            float angleVariation = UnityEngine.Random.Range(-10f, 10f);
            Vector3 newDirection = Quaternion.Euler(0, angleVariation, 0) * _globalWindDirection;

            // Update if significant change
            if (Mathf.Abs(newStrength - _currentWindStrength) > 0.1f)
            {
                _currentWindStrength = newStrength;
                OnWindStrengthChanged?.Invoke(_currentWindStrength);
            }

            if (Vector3.Angle(newDirection, _currentWindDirection) > 5f)
            {
                _currentWindDirection = newDirection.normalized;
                OnWindDirectionChanged?.Invoke(_currentWindDirection);
            }
        }

        private void UpdateWindGusts()
        {
            // Occasional wind gusts
            if (UnityEngine.Random.value < 0.1f) // 10% chance per gust interval
            {
                float gustStrength = UnityEngine.Random.Range(1.5f, 3.0f);
                OnWindGust?.Invoke(gustStrength);

                Logger.LogWarning("SPEEDTREE/WIND", "ApplyWindToRenderer failed", this);
            }
        }

        #endregion

        #region Wind Application

        private void ApplyGlobalWindToRenderers()
        {
            // Primary: Try GameObjectRegistry for registered Renderers
            var registry = ServiceContainerFactory.Instance?.TryResolve<ProjectChimera.Core.Performance.IGameObjectRegistry>();
            var allRenderers = registry?.GetAll<Renderer>();

            if (allRenderers == null || !allRenderers.Any())
            {
                Logger.LogWarning("SPEEDTREE/WIND", "No Renderers found - ensure they are registered with GameObjectRegistry in Awake()", this);
                return;
            }

            // Filter for SpeedTree renderers
            var renderers = allRenderers
                .Where(r => r.sharedMaterial != null &&
                           r.sharedMaterial.shader != null &&
                           r.sharedMaterial.shader.name.Contains("SpeedTree"));

            foreach (var renderer in renderers)
            {
                ApplyWindToRenderer(renderer, _currentWindStrength, _currentWindDirection);
            }
        }

        private void ApplyWindToZoneRenderers(WindZone windZone, WindZoneSettings settings)
        {
            if (windZone == null) return;

            // Primary: Try GameObjectRegistry for registered Renderers
            var registry = ServiceContainerFactory.Instance?.TryResolve<ProjectChimera.Core.Performance.IGameObjectRegistry>();
            var allRenderers = registry?.GetAll<Renderer>();

            if (allRenderers == null || !allRenderers.Any())
            {
                Logger.LogWarning("SPEEDTREE/WIND", "No Renderers found for wind zone - ensure they are registered with GameObjectRegistry", this);
                return;
            }

            // Find renderers within wind zone range
            var renderers = allRenderers
                .Where(r => r.sharedMaterial != null &&
                           r.sharedMaterial.shader != null &&
                           r.sharedMaterial.shader.name.Contains("SpeedTree") &&
                           IsRendererInWindZone(r, windZone));

            foreach (var renderer in renderers)
            {
                float zoneStrength = settings.Strength * _currentWindStrength;
                Vector3 zoneDirection = settings.Direction != Vector3.zero ?
                    settings.Direction : _currentWindDirection;

                ApplyWindToRenderer(renderer, zoneStrength, zoneDirection);
            }
        }

        private void ApplyWindToRenderer(Renderer renderer, float strength, Vector3 direction)
        {
            if (renderer == null || renderer.sharedMaterial == null) return;

            try
            {
                var material = renderer.material;

                // Apply wind strength
                if (material.HasProperty(_windStrengthPropertyId))
                {
                    material.SetFloat(_windStrengthPropertyId, strength);
                }

                // Apply wind direction
                if (material.HasProperty(_windDirectionPropertyId))
                {
                    material.SetVector(_windDirectionPropertyId, direction);
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning("SPEEDTREE/WIND", "ApplyWindToRenderer exception", this);
            }
        }

        private bool IsRendererInWindZone(Renderer renderer, WindZone windZone)
        {
            if (renderer == null || windZone == null) return false;

            // Simple distance check (could be enhanced with more sophisticated zone detection)
            float distance = Vector3.Distance(renderer.transform.position, windZone.transform.position);
            return distance <= windZone.radius;
        }

        private void UpdateWindZoneParameters(WindZone windZone, WindZoneSettings settings)
        {
            // Update zone-specific wind parameters based on zone settings
            settings.Strength = windZone.windMain;
            settings.Direction = windZone.transform.forward;

            // Apply zone-specific variations
            settings.Strength *= UnityEngine.Random.Range(0.9f, 1.1f);
        }

        private WindZoneSettings CreateWindSettingsForZone(WindZone windZone)
        {
            return new WindZoneSettings(windZone);
        }

        #endregion

        #region Public Interface

        /// <summary>
        /// Register a plant for wind effects
        /// </summary>
        public void RegisterPlant(int plantId)
        {
            if (plantId <= 0) return;

            // For this implementation, wind effects are applied globally to all SpeedTree renderers
            // Individual plant registration could be used for more specific wind effects
            Logger.Log("SPEEDTREE/WIND", $"Registered plant {plantId} for wind effects", this);
        }

        /// <summary>
        /// Unregister a plant from wind effects
        /// </summary>
        public void UnregisterPlant(int plantId)
        {
            if (plantId <= 0) return;

            // For this implementation, wind effects are applied globally
            // Individual plant unregistration could be used for more specific control
            Logger.Log("SPEEDTREE/WIND", $"Unregistered plant {plantId} from wind effects", this);
        }

        /// <summary>
        /// Update wind effects for specified plants
        /// </summary>
        public void UpdateWindEffects(int plantId, float windStrength, Vector3 windDirection)
        {
            if (plantId <= 0) return;

            try
            {
                // Find renderers for this specific plant and apply wind effects
                // For now, we'll apply to all SpeedTree renderers as a global effect
                var registry = ServiceContainerFactory.Instance?.TryResolve<ProjectChimera.Core.Performance.IGameObjectRegistry>();
                var allRenderers = registry?.GetAll<Renderer>();

                if (allRenderers == null || !allRenderers.Any())
                {
                    Logger.LogWarning("SPEEDTREE/WIND", "No Renderers found for plant wind - ensure they are registered with GameObjectRegistry", this);
                    return;
                }

                var speedTreeRenderers = allRenderers
                    .Where(r => r.sharedMaterial != null &&
                               r.sharedMaterial.shader != null &&
                               r.sharedMaterial.shader.name.Contains("SpeedTree"));

                foreach (var renderer in speedTreeRenderers)
                {
                    ApplyWindToRenderer(renderer, windStrength, windDirection);
                }

                Logger.Log("SPEEDTREE/WIND", $"Updated wind effects for plant {plantId}", this);
            }
            catch (Exception ex)
            {
                Logger.LogWarning("SPEEDTREE/WIND", $"Failed to update wind effects for plant {plantId}", this);
            }
        }

        /// <summary>
        /// Get wind system statistics
        /// </summary>
        public WindStatistics GetStatistics()
        {
            return new WindStatistics
            {
                GlobalWindStrength = _globalWindStrength,
                CurrentWindStrength = _currentWindStrength,
                WindDirection = _currentWindDirection,
                ActiveWindZones = _activeWindZones.Count,
                WindAnimationEnabled = _enableWindAnimation,
                UpdateFrequency = _windVariationFrequency
            };
        }

        /// <summary>
        /// Enable or disable wind animation
        /// </summary>
        public bool EnableWindAnimation
        {
            get => _enableWindAnimation;
            set => _enableWindAnimation = value;
        }

        /// <summary>
        /// Reset wind system to default state
        /// </summary>
        public void ResetWind()
        {
            _currentWindStrength = _globalWindStrength;
            _currentWindDirection = _globalWindDirection.normalized;
            _windVariationTimer = 0f;
            _windGustTimer = 0f;

            Logger.Log("SPEEDTREE/WIND", "Reset wind system to default state", this);
        }

        /// <summary>
        /// Sets the global wind strength
        /// </summary>
        public void SetWindStrength(float strength)
        {
            _globalWindStrength = Mathf.Max(0f, strength);
            _currentWindStrength = _globalWindStrength;
            OnWindStrengthChanged?.Invoke(_currentWindStrength);
        }

        /// <summary>
        /// Sets the global wind direction
        /// </summary>
        public void SetWindDirection(Vector3 direction)
        {
            _globalWindDirection = direction.normalized;
            _currentWindDirection = _globalWindDirection;
            OnWindDirectionChanged?.Invoke(_currentWindDirection);
        }

        /// <summary>
        /// Gets the current wind strength
        /// </summary>
        public float GetCurrentWindStrength()
        {
            return _currentWindStrength;
        }

        /// <summary>
        /// Gets the current wind direction
        /// </summary>
        public Vector3 GetCurrentWindDirection()
        {
            return _currentWindDirection;
        }

        /// <summary>
        /// Gets all active wind zones
        /// </summary>
        public IReadOnlyList<WindZone> GetActiveWindZones()
        {
            return _activeWindZones.AsReadOnly();
        }

        #endregion

        #region Update Loop

        public void Tick(float deltaTime)
        {
            if (!_enableWindAnimation) return;

            try
            {
                UpdateGlobalWind();

                // Update all active wind zones
                foreach (var windZone in _activeWindZones.ToArray())
                {
                    if (windZone != null)
                    {
                        UpdateWindZone(windZone);
                    }
                    else
                    {
                        // Clean up destroyed wind zones
                        _activeWindZones.Remove(windZone);
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarning("SPEEDTREE/WIND", "Tick exception", this);
            }
        }

        /// <summary>
        /// Get current wind state
        /// </summary>
        public WindState GetCurrentWindState()
        {
            return new WindState
            {
                CurrentStrength = _currentWindStrength,
                CurrentDirection = _currentWindDirection,
                GustStrength = _gustStrength
            };
        }

        /// <summary>
        /// Create a wind gust effect
        /// </summary>
        public void CreateWindGust(float gustStrength = 2.0f, float gustDuration = 1.5f)
        {
            StartCoroutine(CreateWindGustCoroutine(gustStrength, gustDuration));
        }

        private System.Collections.IEnumerator CreateWindGustCoroutine(float gustStrength, float gustDuration)
        {
            float originalStrength = _currentWindStrength;
            float targetStrength = originalStrength * gustStrength;
            float elapsedTime = 0f;

            // Ramp up
            while (elapsedTime < gustDuration * 0.3f)
            {
                float progress = elapsedTime / (gustDuration * 0.3f);
                _currentWindStrength = Mathf.Lerp(originalStrength, targetStrength, progress);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            // Hold
            _currentWindStrength = targetStrength;
            yield return new WaitForSeconds(gustDuration * 0.4f);

            // Ramp down
            elapsedTime = 0f;
            while (elapsedTime < gustDuration * 0.3f)
            {
                float progress = elapsedTime / (gustDuration * 0.3f);
                _currentWindStrength = Mathf.Lerp(targetStrength, originalStrength, progress);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            _currentWindStrength = originalStrength;
            OnWindGust?.Invoke(gustStrength);
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Settings for individual wind zones
    /// </summary>
    [Serializable]
