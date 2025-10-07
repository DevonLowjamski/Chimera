using UnityEngine;
using System;
using ProjectChimera.Core.Logging;
using ProjectChimera.Data.Shared;

namespace ProjectChimera.Systems.Cultivation
{
    /// <summary>
    /// SIMPLE: Basic plant environmental system aligned with Project Chimera's cultivation vision.
    /// Focuses on essential environmental monitoring for plant care.
    /// </summary>
    public class PlantEnvironmentalSystem : MonoBehaviour
    {
        [Header("Basic Environmental Settings")]
        [SerializeField] private bool _enableMonitoring = true;
        [SerializeField] private float _optimalTemperature = 25f;
        [SerializeField] private float _optimalHumidity = 60f;
        [SerializeField] private bool _enableLogging = true;

        // Basic environmental state
        private EnvironmentalConditions _currentConditions;
        private float _environmentalFitness = 1f;
        private bool _isInitialized = false;

        // Events
        public event Action<EnvironmentalConditions> OnEnvironmentChanged;
        public event Action<float> OnFitnessChanged;

        /// <summary>
        /// Initialize the environmental system
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            _currentConditions = new EnvironmentalConditions
            {
                Temperature = _optimalTemperature,
                Humidity = _optimalHumidity,
                LightIntensity = 500f,
                CO2Level = 400f
            };

            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", this);
            }
        }

        /// <summary>
        /// Update environmental monitoring
        /// </summary>
        public void UpdateEnvironmentalMonitoring(float deltaTime)
        {
            if (!_enableMonitoring || !_isInitialized) return;

            // Simple environmental monitoring - could be expanded to read from sensors
            // For now, maintain optimal conditions
            UpdateEnvironmentalFitness();
        }

        /// <summary>
        /// Update environmental conditions
        /// </summary>
        public void UpdateEnvironmentalConditions(EnvironmentalConditions newConditions)
        {
            // Removed null check - structs cannot be null

            _currentConditions = newConditions;
            UpdateEnvironmentalFitness();

            OnEnvironmentChanged?.Invoke(_currentConditions);

            if (_enableLogging)
            {
                ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", this);
            }
        }

        /// <summary>
        /// Get current environmental conditions
        /// </summary>
        public EnvironmentalConditions GetCurrentConditions()
        {
            return _currentConditions;
        }

        /// <summary>
        /// Get environmental fitness (0-1 scale)
        /// </summary>
        public float GetEnvironmentalFitness()
        {
            return _environmentalFitness;
        }

        /// <summary>
        /// Check if environment is optimal for plant growth
        /// </summary>
        public bool IsEnvironmentOptimal()
        {
            return _environmentalFitness > 0.8f;
        }

        /// <summary>
        /// Check if plant is stressed by environment
        /// </summary>
        public bool IsPlantStressed()
        {
            return _environmentalFitness < 0.5f;
        }

        /// <summary>
        /// Get environmental recommendations
        /// </summary>
        public string GetEnvironmentalRecommendation()
        {
            if (_environmentalFitness > 0.8f)
                return "Environment is optimal";

            if (_currentConditions.Temperature < _optimalTemperature - 5f)
                return "Increase temperature";
            else if (_currentConditions.Temperature > _optimalTemperature + 5f)
                return "Decrease temperature";
            else if (_currentConditions.Humidity < _optimalHumidity - 10f)
                return "Increase humidity";
            else if (_currentConditions.Humidity > _optimalHumidity + 10f)
                return "Decrease humidity";

            return "Monitor environmental conditions";
        }

        /// <summary>
        /// Set optimal environmental parameters
        /// </summary>
        public void SetOptimalConditions(float temperature, float humidity)
        {
            _optimalTemperature = temperature;
            _optimalHumidity = humidity;

            UpdateEnvironmentalFitness();

            if (_enableLogging)
            {
                ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", this);
            }
        }

        #region Private Methods

        private void UpdateEnvironmentalFitness()
        {
            // Removed null check - structs cannot be null

            // Simple fitness calculation based on optimal ranges
            float tempFitness = CalculateTemperatureFitness(_currentConditions.Temperature);
            float humidityFitness = CalculateHumidityFitness(_currentConditions.Humidity);
            float lightFitness = CalculateLightFitness(_currentConditions.LightIntensity);

            // Average fitness across factors
            float newFitness = (tempFitness + humidityFitness + lightFitness) / 3f;

            if (Mathf.Abs(newFitness - _environmentalFitness) > 0.01f)
            {
                _environmentalFitness = newFitness;
                OnFitnessChanged?.Invoke(_environmentalFitness);
            }
        }

        private float CalculateTemperatureFitness(float temperature)
        {
            float range = 10f; // Acceptable temperature range
            return Mathf.Clamp01(1f - Mathf.Abs(temperature - _optimalTemperature) / range);
        }

        private float CalculateHumidityFitness(float humidity)
        {
            float range = 20f; // Acceptable humidity range
            return Mathf.Clamp01(1f - Mathf.Abs(humidity - _optimalHumidity) / range);
        }

        private float CalculateLightFitness(float lightIntensity)
        {
            // Simple light fitness - optimal range 300-800 µmol/m²/s
            if (lightIntensity >= 300f && lightIntensity <= 800f)
                return 1f;
            else if (lightIntensity >= 200f && lightIntensity <= 1000f)
                return 0.7f;
            else
                return 0.3f;
        }

        #endregion
        /// <summary>
        /// Properties required by PlantInstance integration
        /// </summary>
        public EnvironmentalConditions CurrentEnvironment => _currentConditions;
        public float EnvironmentalFitness => _environmentalFitness;

        /// <summary>
        /// Additional methods for PlantInstance integration
        /// </summary>
        public EnvironmentalConditions GetCurrentEnvironmentalConditions()
        {
            return _currentConditions;
        }

        public void ProcessAdaptation(float deltaTime)
        {
            // Simple adaptation processing - can be expanded for biological accuracy
            if (_currentConditions.Temperature < _optimalTemperature - 2f ||
                _currentConditions.Temperature > _optimalTemperature + 2f)
            {
                _environmentalFitness *= 0.99f; // Slight fitness reduction over time
            }
        }

        public void SetEnvironmentalFitness(float fitness)
        {
            _environmentalFitness = Mathf.Clamp01(fitness);
            OnFitnessChanged?.Invoke(_environmentalFitness);
        }

        public PlantEnvironmentalMetrics GetEnvironmentalMetrics()
        {
            return new PlantEnvironmentalMetrics
            {
                Temperature = _currentConditions.Temperature,
                Humidity = _currentConditions.Humidity,
                LightIntensity = _currentConditions.LightIntensity,
                EnvironmentalStress = 1f - _environmentalFitness,
                OptimalConditions = _environmentalFitness > 0.8f,
                LastUpdateTime = System.DateTime.Now
            };
        }

        public void ProcessEnvironmentalConditions(float deltaTime)
        {
            UpdateEnvironmentalMonitoring(deltaTime);
            ProcessAdaptation(deltaTime);
        }
    }

    /// <summary>
    /// Plant environmental metrics for instance integration
    /// </summary>
    [System.Serializable]
    public class PlantEnvironmentalMetrics
    {
        public float Temperature;
        public float Humidity;
        public float LightIntensity;
        public float EnvironmentalStress;
        public bool OptimalConditions;
        public string PlantId;
        public System.DateTime LastUpdateTime;
    }
}
