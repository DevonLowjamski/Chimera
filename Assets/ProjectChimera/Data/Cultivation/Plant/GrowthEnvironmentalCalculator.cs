// REFACTORED: Growth Environmental Calculator
// Extracted from PlantGrowthProcessor for better separation of concerns

using UnityEngine;
using ProjectChimera.Data.Shared;
using System;

namespace ProjectChimera.Data.Cultivation.Plant
{
    /// <summary>
    /// Handles environmental factor calculations for plant growth
    /// </summary>
    [System.Serializable]
    public class GrowthEnvironmentalCalculator
    {
        [SerializeField] private bool _environmentalInfluence = true;
        
        // Environmental factors
        [SerializeField] private EnvironmentalConditions _currentEnvironment;
        [SerializeField] private float _lightOptimalityFactor = 1f;
        [SerializeField] private float _temperatureOptimalityFactor = 1f;
        [SerializeField] private float _humidityOptimalityFactor = 1f;

        // Optimal ranges
        private const float OPTIMAL_LIGHT_MIN = 400f;
        private const float OPTIMAL_LIGHT_MAX = 800f;
        private const float OPTIMAL_TEMP_MIN = 20f;
        private const float OPTIMAL_TEMP_MAX = 28f;
        private const float OPTIMAL_HUMIDITY_MIN = 50f;
        private const float OPTIMAL_HUMIDITY_MAX = 70f;

        public EnvironmentalConditions CurrentEnvironment => _currentEnvironment;
        public float LightOptimalityFactor => _lightOptimalityFactor;
        public float TemperatureOptimalityFactor => _temperatureOptimalityFactor;
        public float HumidityOptimalityFactor => _humidityOptimalityFactor;

        public void Initialize()
        {
            InitializeEnvironmentalFactors();
        }

        public void SetEnvironmentalConditions(EnvironmentalConditions conditions)
        {
            _currentEnvironment = conditions;
            UpdateEnvironmentalFactors();
        }

        public void SetEnvironmentalInfluence(bool enabled)
        {
            _environmentalInfluence = enabled;
        }

        public float CalculateEnvironmentalFactor()
        {
            if (!_environmentalInfluence)
                return 1f;

            // Weighted average of environmental factors
            float lightWeight = 0.4f;
            float tempWeight = 0.35f;
            float humidityWeight = 0.25f;

            float totalFactor = (_lightOptimalityFactor * lightWeight) +
                               (_temperatureOptimalityFactor * tempWeight) +
                               (_humidityOptimalityFactor * humidityWeight);

            return Mathf.Clamp(totalFactor, 0.1f, 2f); // Min 10%, max 200% of base growth
        }

        private void UpdateEnvironmentalFactors()
        {
            if (_currentEnvironment == null)
            {
                InitializeEnvironmentalFactors();
                return;
            }

            // Light optimality (PPFD between 400-800 is optimal)
            _lightOptimalityFactor = CalculateLightOptimality(_currentEnvironment.LightIntensity);

            // Temperature optimality (20-28°C is optimal)
            _temperatureOptimalityFactor = CalculateTemperatureOptimality(_currentEnvironment.Temperature);

            // Humidity optimality (50-70% is optimal)
            _humidityOptimalityFactor = CalculateHumidityOptimality(_currentEnvironment.Humidity);
        }

        private void InitializeEnvironmentalFactors()
        {
            _lightOptimalityFactor = 1f;
            _temperatureOptimalityFactor = 1f;
            _humidityOptimalityFactor = 1f;
        }

        private float CalculateLightOptimality(float ppfd)
        {
            if (ppfd < OPTIMAL_LIGHT_MIN)
                return Mathf.Lerp(0.3f, 1f, ppfd / OPTIMAL_LIGHT_MIN);
            if (ppfd > OPTIMAL_LIGHT_MAX)
                return Mathf.Lerp(1f, 0.7f, (ppfd - OPTIMAL_LIGHT_MAX) / OPTIMAL_LIGHT_MAX);
            return 1f;
        }

        private float CalculateTemperatureOptimality(float temp)
        {
            if (temp < OPTIMAL_TEMP_MIN)
                return Mathf.Lerp(0.2f, 1f, temp / OPTIMAL_TEMP_MIN);
            if (temp > OPTIMAL_TEMP_MAX)
                return Mathf.Lerp(1f, 0.5f, (temp - OPTIMAL_TEMP_MAX) / OPTIMAL_TEMP_MAX);
            return 1f;
        }

        private float CalculateHumidityOptimality(float humidity)
        {
            if (humidity < OPTIMAL_HUMIDITY_MIN)
                return Mathf.Lerp(0.6f, 1f, humidity / OPTIMAL_HUMIDITY_MIN);
            if (humidity > OPTIMAL_HUMIDITY_MAX)
                return Mathf.Lerp(1f, 0.8f, (humidity - OPTIMAL_HUMIDITY_MAX) / 100f);
            return 1f;
        }
    }
}

