using UnityEngine;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core;
using ProjectChimera.Core.Updates;
using System.Collections.Generic;

namespace ProjectChimera.Systems.Services.SpeedTree.Environmental
{
    /// <summary>
    /// Modular SpeedTree Environmental Service - Main orchestrator
    /// Coordinates environmental responses, wind, seasonal changes, and stress visualization
    /// </summary>
    public class SpeedTreeEnvironmentalService : MonoBehaviour, ITickable
    {
        #region Properties

        public bool IsInitialized { get; private set; }
        public int TickPriority => ProjectChimera.Core.Updates.TickPriority.SpeedTreeServices;
        public bool IsTickable => _enableEnvironmentalResponse && gameObject.activeInHierarchy;

        #endregion

        #region Private Fields

        [Header("Service Configuration")]
        [SerializeField] private bool _enableEnvironmentalResponse = true;
        [SerializeField] private bool _enableSeasonalChanges = true;
        [SerializeField] private bool _enableStressVisualization = true;
        [SerializeField] private float _environmentalUpdateFrequency = 0.5f;

        [Header("Environmental Conditions")]
        [SerializeField] private float _currentTemperature = 22f;
        [SerializeField] private float _currentHumidity = 60f;
        [SerializeField] private float _currentLightIntensity = 500f;
        [SerializeField] private float _currentWindSpeed = 2f;
        [SerializeField] private Vector3 _currentWindDirection = Vector3.right;

        // Modular system components
        private EnvironmentalResponseSystem _environmentalSystem;
        private WindSystem _windSystem;
        private SeasonalSystem _seasonalSystem;
        private StressVisualizationSystem _stressVisualizationSystem;

        // Update timing
        private float _lastEnvironmentalUpdate = 0f;
        private float _lastSeasonalUpdate = 0f;

        #endregion

        #region Events

        public event System.Action<object> OnEnvironmentalConditionsChanged;
        public event System.Action<float> OnWindStrengthChanged;
        public event System.Action<int, float> OnPlantStressChanged;

        #endregion

        #region IService Implementation

        public void Initialize()
        {
            if (IsInitialized) return;

            ChimeraLogger.Log("SPEEDTREE/ENV", "Initializing environmental service", this);

            // Initialize modular components
            InitializeModularSystems();

            // Set initial environmental conditions
            UpdateEnvironmentalConditions(_currentTemperature, _currentHumidity, _currentLightIntensity, _currentWindSpeed, _currentWindDirection);

            IsInitialized = true;
            ChimeraLogger.Log("SPEEDTREE/ENV", "Environmental service initialized", this);
        }

        #endregion

        #region Unity Methods

        private void Awake()
        {
            Initialize();
        }

        #endregion

        #region ITickable Implementation

        public void Tick(float deltaTime)
        {
            if (!IsInitialized) return;

            // Update environmental systems
            UpdateEnvironmentalSystems(deltaTime);

            // Process environmental responses for known plants (none tracked here yet)
            var plants = _environmentalSystem?.GetPlantsNeedingUpdate();
            if (plants != null)
            {
                // Gather current environmental conditions snapshot
                var conditions = new ProjectChimera.Data.Shared.EnvironmentalConditions
                {
                    Temperature = _currentTemperature,
                    Humidity = _currentHumidity,
                    LightIntensity = _currentLightIntensity,
                    CO2Level = 1200f
                };

                _environmentalSystem.ProcessEnvironmentalResponses(plants, conditions);
            }

            // Update wind system
            _windSystem?.Tick(deltaTime);

            // Update seasonal system
            _seasonalSystem?.Tick(deltaTime);

            // Update stress visualization
            _stressVisualizationSystem?.Tick(deltaTime);
        }

        #endregion

        #region Modular System Initialization

        /// <summary>
        /// Initialize all modular systems
        /// </summary>
        private void InitializeModularSystems()
        {
            // Environmental response system
            _environmentalSystem = new EnvironmentalResponseSystem();
            _environmentalSystem.Initialize();

            // Wind system
            _windSystem = new WindSystem();
            _windSystem.Initialize();

            // Seasonal system
            _seasonalSystem = new SeasonalSystem();
            _seasonalSystem.Initialize();

            // Stress visualization system
            _stressVisualizationSystem = new StressVisualizationSystem();
            _stressVisualizationSystem.Initialize();

            ChimeraLogger.Log("SPEEDTREE/ENV", "Registered environmental systems", this);
        }

        #endregion

        #region Public API Methods

        /// <summary>
        /// Register a plant for environmental monitoring
        /// </summary>
        public void RegisterPlant(int plantId, float initialHealth = 1f, float initialStress = 0f)
        {
            _environmentalSystem?.RegisterPlant(plantId);
            _stressVisualizationSystem?.RegisterPlant(plantId);
        }

        /// <summary>
        /// Unregister a plant from environmental monitoring
        /// </summary>
        public void UnregisterPlant(int plantId)
        {
            _environmentalSystem?.UnregisterPlant(plantId);
            _stressVisualizationSystem?.UnregisterPlant(plantId);
        }

        /// <summary>
        /// Update environmental conditions
        /// </summary>
        public void UpdateEnvironmentalConditions(float temperature, float humidity, float lightIntensity, float windSpeed, Vector3 windDirection)
        {
            _currentTemperature = temperature;
            _currentHumidity = humidity;
            _currentLightIntensity = lightIntensity;
            _currentWindSpeed = windSpeed;
            _currentWindDirection = windDirection;

            // Update environmental response system
            _environmentalSystem?.UpdateEnvironmentalConditions(temperature, humidity, lightIntensity, windSpeed, windDirection);

            // Update wind system
            _windSystem?.SetWindStrength(windSpeed);
            _windSystem?.SetWindDirection(windDirection);

            // Notify listeners
            OnEnvironmentalConditionsChanged?.Invoke(this);
            OnWindStrengthChanged?.Invoke(windSpeed);
        }

        /// <summary>
        /// Update plant health and stress
        /// </summary>
        public void UpdatePlantHealth(int plantId, float health, float stress)
        {
            // Break down stress into components for visualization
            float temperatureStress = stress * 0.3f; // Approximate distribution
            float humidityStress = stress * 0.3f;
            float lightStress = stress * 0.4f;
            _stressVisualizationSystem?.UpdatePlantStress(plantId, stress, temperatureStress, humidityStress, lightStress);
            OnPlantStressChanged?.Invoke(plantId, stress);
        }

        /// <summary>
        /// Get environmental response for a plant
        /// </summary>
        public EnvironmentalResponseData GetPlantResponse(int plantId)
        {
            return _environmentalSystem?.GetPlantResponse(plantId);
        }

        /// <summary>
        /// Get current wind state
        /// </summary>
        public WindState GetWindState()
        {
            return _windSystem?.GetCurrentWindState() ?? new WindState();
        }

        /// <summary>
        /// Get current seasonal state
        /// </summary>
        public SeasonalState GetSeasonalState()
        {
            return _seasonalSystem?.GetCurrentSeasonalState() ?? new SeasonalState();
        }

        /// <summary>
        /// Get stress visualization statistics
        /// </summary>
        public StressVisualizationStatistics GetStressStatistics()
        {
            return _stressVisualizationSystem?.GetStatistics();
        }

        /// <summary>
        /// Set seasonal manually
        /// </summary>
        public void SetSeason(Season season)
        {
            _seasonalSystem?.SetSeason(season);
        }

        /// <summary>
        /// Create a wind gust
        /// </summary>
        public void CreateWindGust(float strength, float duration)
        {
            _windSystem?.CreateWindGust(strength, duration);
        }

        /// <summary>
        /// Get comprehensive environmental status
        /// </summary>
        public EnvironmentalStatus GetEnvironmentalStatus()
        {
            return new EnvironmentalStatus
            {
                Temperature = _currentTemperature,
                Humidity = _currentHumidity,
                LightIntensity = _currentLightIntensity,
                WindSpeed = _currentWindSpeed,
                WindDirection = _currentWindDirection,
                CurrentSeason = _seasonalSystem?.GetCurrentSeasonalState().CurrentSeason ?? Season.Spring,
                WindState = _windSystem?.GetCurrentWindState() ?? new WindState(),
                EnvironmentalStats = _environmentalSystem?.GetStatistics(),
                StressStats = _stressVisualizationSystem?.GetStatistics(),
                SeasonalConditions = _seasonalSystem?.GetSeasonalConditions() ?? new SeasonalConditions()
            };
        }

        #endregion

        #region Configuration Methods

        /// <summary>
        /// Set environmental update frequency
        /// </summary>
        public void SetUpdateFrequency(float frequency)
        {
            _environmentalUpdateFrequency = frequency;
            if (_environmentalSystem != null)
                _environmentalSystem.UpdateFrequency = frequency;
        }

        /// <summary>
        /// Enable/disable environmental response
        /// </summary>
        public void SetEnvironmentalResponse(bool enabled)
        {
            _enableEnvironmentalResponse = enabled;
            if (_environmentalSystem != null)
                _environmentalSystem.EnableEnvironmentalResponse = enabled;
        }

        /// <summary>
        /// Enable/disable seasonal changes
        /// </summary>
        public void SetSeasonalChanges(bool enabled)
        {
            _enableSeasonalChanges = enabled;
            if (_seasonalSystem != null)
                _seasonalSystem.EnableSeasonalChanges = enabled;
        }

        /// <summary>
        /// Enable/disable stress visualization
        /// </summary>
        public void SetStressVisualization(bool enabled)
        {
            _enableStressVisualization = enabled;
            if (_stressVisualizationSystem != null)
                _stressVisualizationSystem.EnableStressVisualization = enabled;
        }

        #endregion

        #region Update Methods

        /// <summary>
        /// Update environmental systems
        /// </summary>
        private void UpdateEnvironmentalSystems(float deltaTime)
        {
            if (Time.time - _lastEnvironmentalUpdate > _environmentalUpdateFrequency)
            {
                _lastEnvironmentalUpdate = Time.time;

                // Update environmental conditions from scene
                UpdateConditionsFromScene();
            }
        }

        /// <summary>
        /// Update environmental conditions from scene (placeholder)
        /// </summary>
        private void UpdateConditionsFromScene()
        {
            // In a real implementation, this would get conditions from
            // weather systems, time of day, location, etc.
            // For now, maintain current conditions or add slight variations
        }

        #endregion

        #region Cleanup

        private void OnDestroy()
        {
            // Clean up resources
            if (_environmentalSystem != null)
                _environmentalSystem.ResetResponses();

            if (_stressVisualizationSystem != null)
                _stressVisualizationSystem.ResetVisualization();

            ChimeraLogger.Log("SPEEDTREE/ENV", "Reset environmental systems", this);
        }

        #endregion

        // ISpeedTreeEnvironmentalService implementation
        public void Shutdown()
        {
            // Stub implementation
        }

        public void UpdateEnvironment(float deltaTime)
        {
            // Stub implementation - delegate to existing Tick method
            Tick(deltaTime);
        }

        public EnvironmentalResponseData GetEnvironmentalResponse(int plantId)
        {
            return new EnvironmentalResponseData();
        }

        public void UpdateWindSettings(float windSpeed, Vector3 windDirection, float turbulence)
        {
            // Stub implementation
        }

        public void EnableStressVisualization(bool enabled)
        {
            // Stub implementation
        }

        public SeasonalEffects GetSeasonalEffects()
        {
            return new SeasonalEffects();
        }
    }

    /// <summary>
    /// Comprehensive environmental status
    /// </summary>
    [System.Serializable]
    public class EnvironmentalStatus
    {
        public float Temperature;
        public float Humidity;
        public float LightIntensity;
        public float WindSpeed;
        public Vector3 WindDirection;
        public Season CurrentSeason;
        public WindState WindState;
        public EnvironmentalStatistics EnvironmentalStats;
        public StressVisualizationStatistics StressStats;
        public SeasonalConditions SeasonalConditions;

        public string GetStatusSummary()
        {
            return $"Temp: {Temperature:F1}°C | Humidity: {Humidity:F1}% | Light: {LightIntensity:F0} lux | Wind: {WindSpeed:F1} m/s | Season: {CurrentSeason}";
        }
    }
}
