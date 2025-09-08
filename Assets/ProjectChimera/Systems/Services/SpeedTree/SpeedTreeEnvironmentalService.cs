using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Systems.Registry;

#if UNITY_SPEEDTREE
using SpeedTree;
#endif

namespace ProjectChimera.Systems.Services.SpeedTree
{
    /// <summary>
    /// PC014-5c: SpeedTree Environmental Response Service
    /// Handles environmental conditions, wind systems, stress visualization, and seasonal changes
    /// Decomposed from AdvancedSpeedTreeManager (360 lines target)
    /// </summary>
    public class SpeedTreeEnvironmentalService : MonoBehaviour, ITickable, ISpeedTreeEnvironmentalService
    {
        #region Properties

        public bool IsInitialized { get; private set; }

        #endregion

        #region Private Fields

        [Header("Environmental Configuration")]
        [SerializeField] private bool _enableEnvironmentalResponse = true;
        [SerializeField] private bool _enableSeasonalChanges = true;
        [SerializeField] private bool _enableStressVisualization = true;
        [SerializeField] private float _environmentalUpdateFrequency = 0.5f;

        [Header("Wind System")]
        [SerializeField] private ScriptableObject _windConfig;
        [SerializeField] private bool _enableWindAnimation = true;
        [SerializeField] private float _windStrength = 1.0f;
        [SerializeField] private Vector3 _windDirection = Vector3.right;

        [Header("Stress Visualization")]
        [SerializeField] private Gradient _healthGradient;
        [SerializeField] private Gradient _stressGradient;
        [SerializeField] private float _stressVisualizationIntensity = 1.0f;

        // Environmental Processing
        private Dictionary<int, object> _plantResponses = new Dictionary<int, object>();
        private Dictionary<int, object> _stressData = new Dictionary<int, object>();
        private List<int> _plantsNeedingUpdate = new List<int>();

        // Wind System
        private Dictionary<WindZone, object> _windZones = new Dictionary<WindZone, object>();
        private List<WindZone> _activeWindZones = new List<WindZone>();
        private float _currentWindStrength = 0f;
        private Vector3 _currentWindDirection = Vector3.zero;

        // Seasonal System
        private object _currentSeason;
        private float _seasonalTransitionProgress = 0f;
        private Dictionary<object, object> _seasonalEffects = new Dictionary<object, object>();

        // Update Timing
        private float _lastEnvironmentalUpdate = 0f;
        private float _lastSeasonalUpdate = 0f;

        // Shader Property IDs
        private int _healthPropertyId;
        private int _stressPropertyId;
        private int _windStrengthPropertyId;
        private int _seasonalPropertyId;

        #endregion

        #region Events

        public event Action<object> OnEnvironmentalConditionsChanged;
        public event Action<float> OnWindStrengthChanged;
        public event Action<int, float> OnPlantStressChanged;

        #endregion

        #region IService Implementation

        public void Initialize()
        {
            if (IsInitialized) return;

            ChimeraLogger.Log("Initializing SpeedTreeEnvironmentalService...");

            // Cache shader properties
            CacheShaderProperties();

            // Initialize environmental systems
            InitializeEnvironmentalSystem();
            InitializeWindSystem();
            InitializeSeasonalSystem();
            InitializeStressVisualization();

            // Register with ServiceRegistry
            ServiceContainerFactory.Instance.RegisterSingleton<ISpeedTreeEnvironmentalService>(this);

            IsInitialized = true;
            ChimeraLogger.Log("SpeedTreeEnvironmentalService initialized successfully");
        }

        public void Shutdown()
        {
            if (!IsInitialized) return;

            ChimeraLogger.Log("Shutting down SpeedTreeEnvironmentalService...");

            // Clear all collections
            _plantResponses.Clear();
            _stressData.Clear();
            _plantsNeedingUpdate.Clear();
            _windZones.Clear();
            _activeWindZones.Clear();
            _seasonalEffects.Clear();

            IsInitialized = false;
            ChimeraLogger.Log("SpeedTreeEnvironmentalService shutdown complete");
        }

        #endregion

        #region Environmental Response

        public void UpdateEnvironmentalResponse(int plantId, object conditions)
        {
            if (plantId <= 0 || conditions == null || !_enableEnvironmentalResponse) return;

            // Get or create response data
            if (!_plantResponses.TryGetValue(plantId, out var responseData))
            {
                responseData = new object(); // Placeholder
                _plantResponses[plantId] = responseData;
            }

            ChimeraLogger.Log($"Updated environmental response for plant {plantId}");
        }

        public void ApplyEnvironmentalConditions(int plantId, object conditions)
        {
            if (plantId <= 0 || conditions == null) return;

            // Update the plant's environmental response
            UpdateEnvironmentalResponse(plantId, conditions);

            // Trigger environmental change event
            OnEnvironmentalConditionsChanged?.Invoke(conditions);
        }

        public void UpdateSeasonalChanges(IEnumerable<int> plantIds)
        {
            if (!_enableSeasonalChanges) return;

            foreach (var plantId in plantIds)
            {
                ApplySeasonalEffects(plantId, _currentSeason);
            }
        }

        #endregion

        #region Wind System

        public void UpdateWindSystem()
        {
            if (!_enableWindAnimation) return;

            // Update global wind parameters
            UpdateGlobalWind();

            // Update all wind zones
            foreach (var windZone in _activeWindZones)
            {
                if (windZone != null)
                {
                    UpdateWindZone(windZone);
                }
            }

            // Apply wind to SpeedTree renderers
            ApplyWindToRenderers();
        }

        public void UpdateWindZone(WindZone windZone)
        {
            if (windZone == null) return;

            // Get or create wind settings for this zone
            if (!_windZones.TryGetValue(windZone, out var settings))
            {
                settings = CreateWindSettingsForZone(windZone);
                _windZones[windZone] = settings;
            }

            // Update wind zone parameters
            UpdateWindZoneParameters(windZone, settings);

            // Update SpeedTree wind settings
            ApplyWindSettings(settings);
        }

        public void ApplyWindSettings(object settings)
        {
            if (settings == null) return;

#if UNITY_SPEEDTREE
            // Apply wind settings to SpeedTree system
            if (_windConfig != null)
            {
                ApplyWindConfiguration(settings);
            }

            // Update current wind state
            _currentWindStrength = _windStrength;
            _currentWindDirection = _windDirection;

            OnWindStrengthChanged?.Invoke(_currentWindStrength);
#endif
        }

        public void SetWindEnabled(bool enabled)
        {
            _enableWindAnimation = enabled;

            if (!enabled)
            {
                // Disable wind on all renderers
#if UNITY_SPEEDTREE
                SetGlobalWindStrength(0f);
#else
                ChimeraLogger.Log("Wind disabled - SpeedTree not available");
#endif
            }
        }

        #endregion

        #region Stress Management

        public void UpdateStressVisualization(IEnumerable<int> plantIds)
        {
            if (!_enableStressVisualization) return;

            foreach (var plantId in plantIds)
            {
                if (_plantResponses.TryGetValue(plantId, out var responseData))
                {
                    UpdatePlantHealthVisualization(plantId, 1f);
                }
            }
        }

        public void UpdatePlantHealthVisualization(int plantId, float health)
        {
            if (plantId <= 0) return;

            // Get or create stress data
            if (!_stressData.TryGetValue(plantId, out var stressData))
            {
                stressData = new object(); // Placeholder
                _stressData[plantId] = stressData;
            }

            var healthLevel = Mathf.Clamp01(health);
            var stressLevel = 1f - healthLevel;

            // Apply visualization to renderer
            var stressFactor = stressLevel * _stressVisualizationIntensity;
            ApplyHealthVisualization(plantId, healthLevel, stressFactor);

            OnPlantStressChanged?.Invoke(plantId, stressLevel);
        }

        public void ApplyHealthVisualization(int plantId, float healthFactor, float stressFactor)
        {
            if (plantId <= 0) return;

            // Find the SpeedTree renderer for this instance
            var renderer = FindRendererForInstance(plantId);
            if (renderer == null) return;

            var materials = renderer.GetComponent<Renderer>()?.materials;
            if (materials == null) return;

            foreach (var material in materials)
            {
                // Apply health color
                if (material.HasProperty(_healthPropertyId))
                {
                    var healthColor = _healthGradient.Evaluate(healthFactor);
                    material.SetColor(_healthPropertyId, healthColor);
                }

                // Apply stress effects
                if (material.HasProperty(_stressPropertyId))
                {
                    var stressColor = _stressGradient.Evaluate(stressFactor);
                    material.SetColor(_stressPropertyId, stressColor);
                }
            }
        }

        #endregion

        #region Lighting System

        public void UpdatePlantLighting(int plantId, float intensity, Color color)
        {
            if (plantId <= 0) return;

            var renderer = FindRendererForInstance(plantId);
            if (renderer == null) return;

            var materials = renderer.GetComponent<Renderer>()?.materials;
            if (materials == null) return;

            foreach (var material in materials)
            {
                // Update lighting parameters
                if (material.HasProperty("_LightIntensity"))
                {
                    material.SetFloat("_LightIntensity", intensity);
                }

                if (material.HasProperty("_LightColor"))
                {
                    material.SetColor("_LightColor", color);
                }
            }
        }

        public void HandleLightingChange(object lightingConditions)
        {
            if (lightingConditions == null) return;

            // Update all plants with new lighting conditions
            foreach (var kvp in _plantResponses)
            {
                var plantId = kvp.Key;

                UpdatePlantLighting(plantId, 1.0f, Color.white);
            }
        }

        #endregion

        #region Private Helper Methods

        private void CacheShaderProperties()
        {
            _healthPropertyId = Shader.PropertyToID("_HealthColor");
            _stressPropertyId = Shader.PropertyToID("_StressColor");
            _windStrengthPropertyId = Shader.PropertyToID("_WindStrength");
            _seasonalPropertyId = Shader.PropertyToID("_SeasonalEffect");
        }

        private void InitializeEnvironmentalSystem()
        {
            _plantResponses.Clear();
            _plantsNeedingUpdate.Clear();

            // Initialize default gradients if not set
            if (_healthGradient == null)
            {
                _healthGradient = CreateDefaultHealthGradient();
            }

            if (_stressGradient == null)
            {
                _stressGradient = CreateDefaultStressGradient();
            }

            ChimeraLogger.Log("Environmental system initialized");
        }

        private void InitializeWindSystem()
        {
            _windZones.Clear();
            _activeWindZones.Clear();

            // Find all wind zones in the scene
            var windZones = /* TODO: ServiceContainer.GetAll<WindZone>() */ new WindZone[0];
            foreach (var windZone in windZones)
            {
                RegisterWindZone(windZone);
            }

            _currentWindStrength = _windStrength;
            _currentWindDirection = _windDirection;

            ChimeraLogger.Log($"Wind system initialized with {_windZones.Count} wind zones");
        }

        private void InitializeSeasonalSystem()
        {
            _seasonalEffects.Clear();

            // Initialize seasonal effects
            CreateSeasonalEffects();

            _currentSeason = Season.Spring;
            _seasonalTransitionProgress = 0f;

            ChimeraLogger.Log("Seasonal system initialized");
        }

        private void InitializeStressVisualization()
        {
            _stressData.Clear();
            ChimeraLogger.Log("Stress visualization system initialized");
        }

        private void UpdateTemperatureResponse(object data, float temperature)
        {
            // Calculate temperature stress (optimal range 20-26°C for cannabis)
            var optimalMin = 20f;
            var optimalMax = 26f;

            float temperatureStress;
            if (temperature < optimalMin)
            {
                temperatureStress = (optimalMin - temperature) / optimalMin;
            }
            else if (temperature > optimalMax)
            {
                temperatureStress = (temperature - optimalMax) / (40f - optimalMax);
            }
            else
            {
                temperatureStress = 0f;
            }

            temperatureStress = Mathf.Clamp01(temperatureStress);
            ChimeraLogger.Log($"Temperature stress calculated: {temperatureStress}");
        }

        private void UpdateHumidityResponse(object data, float humidity)
        {
            // Calculate humidity stress (optimal range 40-60% for cannabis)
            var optimalMin = 40f;
            var optimalMax = 60f;

            float humidityStress;
            if (humidity < optimalMin)
            {
                humidityStress = (optimalMin - humidity) / optimalMin;
            }
            else if (humidity > optimalMax)
            {
                humidityStress = (humidity - optimalMax) / (100f - optimalMax);
            }
            else
            {
                humidityStress = 0f;
            }

            humidityStress = Mathf.Clamp01(humidityStress);
            ChimeraLogger.Log($"Humidity stress calculated: {humidityStress}");
        }

        private void UpdateLightResponse(object data, float lightIntensity)
        {
            // Calculate light stress (optimal PPFD 400-700 for cannabis)
            var optimalMin = 400f;
            var optimalMax = 700f;

            float lightStress;
            if (lightIntensity < optimalMin)
            {
                lightStress = (optimalMin - lightIntensity) / optimalMin;
            }
            else if (lightIntensity > optimalMax)
            {
                lightStress = (lightIntensity - optimalMax) / (1200f - optimalMax);
            }
            else
            {
                lightStress = 0f;
            }

            lightStress = Mathf.Clamp01(lightStress);
            ChimeraLogger.Log($"Light stress calculated: {lightStress}");
        }

        private void UpdateNutrientResponse(object data, float nutrientLevel)
        {
            // Calculate nutrient stress
            var nutrientStress = Mathf.Clamp01(1f - (nutrientLevel / 100f));
            ChimeraLogger.Log($"Nutrient stress calculated: {nutrientStress}");
        }

        private void UpdateCO2Response(object data, float co2Level)
        {
            // Calculate CO2 stress (optimal 800-1200 ppm for cannabis)
            var optimalMin = 800f;
            var optimalMax = 1200f;

            float co2Stress;
            if (co2Level < optimalMin)
            {
                co2Stress = (optimalMin - co2Level) / optimalMin;
            }
            else if (co2Level > optimalMax)
            {
                co2Stress = (co2Level - optimalMax) / (2000f - optimalMax);
            }
            else
            {
                co2Stress = 0f;
            }

            co2Stress = Mathf.Clamp01(co2Stress);
            ChimeraLogger.Log($"CO2 stress calculated: {co2Stress}");
        }

        private float CalculateEnvironmentalStress(object data, object conditions)
        {
            // Weighted average of all stress factors - placeholder implementation
            var totalStress = 0.1f; // Default low stress
            return Mathf.Clamp01(totalStress);
        }

        private void UpdatePlantStress(int plantId, float stressLevel)
        {
            if (!_stressData.TryGetValue(plantId, out var stressData))
            {
                stressData = new object(); // Placeholder
                _stressData[plantId] = stressData;
            }

            ChimeraLogger.Log($"Updated stress level for plant {plantId}: {stressLevel}");
        }

        private void ApplyEnvironmentalEffects(int plantId, object data)
        {
            var renderer = FindRendererForInstance(plantId);
            if (renderer == null) return;

            // Apply environmental effects to materials
            var materials = renderer.GetComponent<Renderer>()?.materials;
            if (materials == null) return;

            foreach (var material in materials)
            {
                // Apply environmental effects - placeholder implementation
                ApplyTemperatureEffects(material, 0.1f);
                ApplyHumidityEffects(material, 0.1f);
                ApplyLightEffects(material, 0.1f);
            }
        }

        private void ApplySeasonalEffects(int plantId, object season)
        {
            if (!_seasonalEffects.TryGetValue(season, out var effects)) return;

            var renderer = FindRendererForInstance(plantId);
            if (renderer == null) return;

            var materials = renderer.GetComponent<Renderer>()?.materials;
            if (materials == null) return;

            foreach (var material in materials)
            {
                if (material.HasProperty(_seasonalPropertyId))
                {
                    material.SetFloat(_seasonalPropertyId, 1.0f);
                }

                if (material.HasProperty("_SeasonalColor"))
                {
                    material.SetColor("_SeasonalColor", Color.white);
                }
            }
        }

        private void UpdateGlobalWind()
        {
            // Update global wind parameters based on environmental conditions
            var windVariation = Mathf.Sin(Time.time * 0.5f) * 0.3f;
            _currentWindStrength = _windStrength + windVariation;

            // Slight direction variation
            var directionVariation = Mathf.Sin(Time.time * 0.2f) * 15f; // ±15 degrees
            var currentAngle = Mathf.Atan2(_windDirection.z, _windDirection.x) * Mathf.Rad2Deg + directionVariation;
            _currentWindDirection = new Vector3(
                Mathf.Cos(currentAngle * Mathf.Deg2Rad),
                0f,
                Mathf.Sin(currentAngle * Mathf.Deg2Rad)
            );
        }

        private void ApplyWindToRenderers()
        {
#if UNITY_SPEEDTREE
            // Apply current wind settings to all SpeedTree renderers
            SetGlobalWindStrength(_currentWindStrength);
            SetGlobalWindDirection(_currentWindDirection);
#endif
        }

        private object CreateWindSettingsForZone(WindZone windZone)
        {
            // Create wind settings - placeholder implementation
            return new object();
        }

        private void UpdateWindZoneParameters(WindZone windZone, object settings)
        {
            if (windZone == null || settings == null) return;

            ChimeraLogger.Log($"Updated wind zone parameters for: {windZone.name}");
        }

        private void RegisterWindZone(WindZone windZone)
        {
            if (!_activeWindZones.Contains(windZone))
            {
                _activeWindZones.Add(windZone);

                var settings = CreateWindSettingsForZone(windZone);
                _windZones[windZone] = settings;
            }
        }

#if UNITY_SPEEDTREE
        private void ApplyWindConfiguration(object settings)
        {
            if (settings == null) return;

            ChimeraLogger.Log("Applied wind configuration to SpeedTree system");
        }

        private void SetGlobalWindStrength(float strength)
        {
            // Set global wind strength for all SpeedTree renderers
            Shader.SetGlobalFloat(_windStrengthPropertyId, strength);
        }

        private void SetGlobalWindDirection(Vector3 direction)
        {
            // Set global wind direction for all SpeedTree renderers
            Shader.SetGlobalVector("_WindDirection", direction);
        }
#endif

        private Gradient CreateDefaultHealthGradient()
        {
            var gradient = new Gradient();
            var colorKeys = new GradientColorKey[]
            {
                new GradientColorKey(Color.red, 0f),      // Unhealthy
                new GradientColorKey(Color.yellow, 0.5f), // Moderate
                new GradientColorKey(Color.green, 1f)     // Healthy
            };
            var alphaKeys = new GradientAlphaKey[]
            {
                new GradientAlphaKey(1f, 0f),
                new GradientAlphaKey(1f, 1f)
            };
            gradient.SetKeys(colorKeys, alphaKeys);
            return gradient;
        }

        private Gradient CreateDefaultStressGradient()
        {
            var gradient = new Gradient();
            var colorKeys = new GradientColorKey[]
            {
                new GradientColorKey(Color.clear, 0f),    // No stress
                new GradientColorKey(Color.yellow, 0.5f), // Moderate stress
                new GradientColorKey(Color.red, 1f)       // High stress
            };
            var alphaKeys = new GradientAlphaKey[]
            {
                new GradientAlphaKey(0f, 0f),
                new GradientAlphaKey(1f, 1f)
            };
            gradient.SetKeys(colorKeys, alphaKeys);
            return gradient;
        }

        private void CreateSeasonalEffects()
        {
            _seasonalEffects[Season.Spring] = new SeasonalEffects
            {
                ColorTint = new Color(0.8f, 1f, 0.8f, 1f), // Light green tint
                IntensityModifier = 1.1f,
                GrowthModifier = 1.2f
            };

            _seasonalEffects[Season.Summer] = new SeasonalEffects
            {
                ColorTint = new Color(1f, 1f, 0.9f, 1f), // Bright, slightly yellow
                IntensityModifier = 1.3f,
                GrowthModifier = 1.5f
            };

            _seasonalEffects[Season.Autumn] = new SeasonalEffects
            {
                ColorTint = new Color(1f, 0.8f, 0.6f, 1f), // Orange/brown tint
                IntensityModifier = 0.8f,
                GrowthModifier = 0.7f
            };

            _seasonalEffects[Season.Winter] = new SeasonalEffects
            {
                ColorTint = new Color(0.7f, 0.8f, 0.9f, 1f), // Blue-ish tint
                IntensityModifier = 0.5f,
                GrowthModifier = 0.3f
            };
        }

        private float CalculateOverallStress(object data)
        {
            // Placeholder implementation
            return 0.1f; // Default low stress
        }

        private void ApplyTemperatureEffects(Material material, float temperatureStress)
        {
            if (material.HasProperty("_TemperatureStress"))
            {
                material.SetFloat("_TemperatureStress", temperatureStress);
            }
        }

        private void ApplyHumidityEffects(Material material, float humidityStress)
        {
            if (material.HasProperty("_HumidityStress"))
            {
                material.SetFloat("_HumidityStress", humidityStress);
            }
        }

        private void ApplyLightEffects(Material material, float lightStress)
        {
            if (material.HasProperty("_LightStress"))
            {
                material.SetFloat("_LightStress", lightStress);
            }
        }

        private GameObject FindRendererForInstance(int plantId)
        {
            // Find the SpeedTree renderer associated with this plant instance
            var renderers = /* TODO: ServiceContainer.GetAll<GameObject>() */ new GameObject[0];
            return renderers.FirstOrDefault(r =>
                r.name.Contains($"SpeedTree_Plant_{plantId}"));
        }

        private object FindPlantInstance(int plantId)
        {
            // This would typically be provided by a plant management service
            // For now, return null as a placeholder
            return null;
        }

        #endregion

        #region Unity Lifecycle

        private void Start()
        {
        // Register with UpdateOrchestrator
        UpdateOrchestrator.Instance?.RegisterTickable(this);
            Initialize();
        }

        private void OnDestroy()
        {
        // Unregister from UpdateOrchestrator
        UpdateOrchestrator.Instance?.UnregisterTickable(this);
            Shutdown();
        }

        public void Tick(float deltaTime)
        {
            if (!IsInitialized) return;

            var currentTime = Time.time;

            // Update environmental systems
            if (currentTime - _lastEnvironmentalUpdate >= _environmentalUpdateFrequency)
            {
                if (_plantsNeedingUpdate.Count > 0)
                {
                    UpdateStressVisualization(_plantsNeedingUpdate);
                }
                _lastEnvironmentalUpdate = currentTime;
            }

            // Update wind system
            if (_enableWindAnimation)
            {
                UpdateWindSystem();
            }

            // Update seasonal effects
            if (currentTime - _lastSeasonalUpdate >= 3600f) // Update every hour
            {
                if (_plantsNeedingUpdate.Count > 0)
                {
                    UpdateSeasonalChanges(_plantsNeedingUpdate);
                }
                _lastSeasonalUpdate = currentTime;
            }
        }

        #endregion

        #region ITickable Implementation

        // ITickable implementation properties
        public int Priority => TickPriority.SpeedTreeServices;
        public bool Enabled => enabled && gameObject.activeInHierarchy;

        public void OnRegistered()
        {
            ChimeraLogger.LogVerbose("[SpeedTreeEnvironmentalService] Registered with UpdateOrchestrator");
        }

        public void OnUnregistered()
        {
            ChimeraLogger.LogVerbose("[SpeedTreeEnvironmentalService] Unregistered from UpdateOrchestrator");
        }

        #endregion
    }

    #region Supporting Data Classes

    [System.Serializable]
    public class EnvironmentalResponseData
    {
        public int PlantId;
        public float TemperatureStress;
        public float HumidityStress;
        public float LightStress;
        public float NutrientStress;
        public float CO2Stress;
        public float LastUpdate;
    }

    [System.Serializable]
    public class StressVisualizationData
    {
        public int PlantId;
        public float HealthLevel;
        public float StressLevel;
        public Color CurrentHealthColor;
        public Color CurrentStressColor;
    }

    [System.Serializable]
    public class SpeedTreeWindSettings
    {
        public float Strength;
        public Vector3 Direction;
        public float Turbulence;
        public float Pulsation;
        public WindZone Zone;
    }

    [System.Serializable]
    public class SeasonalEffects
    {
        public Color ColorTint;
        public float IntensityModifier;
        public float GrowthModifier;
    }

    public enum Season
    {
        Spring,
        Summer,
        Autumn,
        Winter
    }

    #endregion
}
