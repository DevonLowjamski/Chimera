using UnityEngine;
using ProjectChimera.Data.Shared;
using System.Collections.Generic;

namespace ProjectChimera.Data.Genetics
{
    /// <summary>
    /// Gene-Environment interaction profile for plant genetics data.
    /// This class defines how genetic traits respond to environmental conditions.
    /// Used as a component within PlantGeneticsData for trait expression calculations.
    /// </summary>
    [System.Serializable]
    public class GxEInteractionProfile
    {
        [Header("Basic Interaction Settings")]
        [SerializeField] private GxEInteractionType _interactionType = GxEInteractionType.Multiplicative;
        [SerializeField, Range(0f, 2f)] private float _environmentalSensitivity = 1f;
        [SerializeField, Range(0f, 1f)] private float _phenotypicPlasticity = 0.5f;

        [Header("Temperature Response")]
        [SerializeField] private EnvironmentalResponseData _temperatureResponse;
        
        [Header("Light Response")]
        [SerializeField] private EnvironmentalResponseData _lightResponse;
        
        [Header("Humidity Response")]
        [SerializeField] private EnvironmentalResponseData _humidityResponse;
        
        [Header("CO2 Response")]
        [SerializeField] private EnvironmentalResponseData _co2Response;
        
        [Header("Stress Tolerance")]
        [SerializeField, Range(-0.5f, 0.5f)] private float _heatStressModifier = 0f;
        [SerializeField, Range(-0.5f, 0.5f)] private float _coldStressModifier = 0f;
        [SerializeField, Range(-0.5f, 0.5f)] private float _droughtStressModifier = 0f;
        [SerializeField, Range(-0.5f, 0.5f)] private float _lightStressModifier = 0f;
        
        [Header("Adaptation")]
        [SerializeField] private bool _hasAdaptiveResponse = false;
        [SerializeField, Range(0f, 0.1f)] private float _adaptationRate = 0.01f;

        // Public Properties
        public GxEInteractionType InteractionType => _interactionType;
        public float EnvironmentalSensitivity => _environmentalSensitivity;
        public float PhenotypicPlasticity => _phenotypicPlasticity;
        public EnvironmentalResponseData TemperatureResponse => _temperatureResponse;
        public EnvironmentalResponseData LightResponse => _lightResponse;
        public EnvironmentalResponseData HumidityResponse => _humidityResponse;
        public EnvironmentalResponseData CO2Response => _co2Response;
        public float HeatStressModifier => _heatStressModifier;
        public float ColdStressModifier => _coldStressModifier;
        public float DroughtStressModifier => _droughtStressModifier;
        public float LightStressModifier => _lightStressModifier;
        public bool HasAdaptiveResponse => _hasAdaptiveResponse;
        public float AdaptationRate => _adaptationRate;

        /// <summary>
        /// Constructor with default values
        /// </summary>
        public GxEInteractionProfile()
        {
            _temperatureResponse = new EnvironmentalResponseData();
            _lightResponse = new EnvironmentalResponseData();
            _humidityResponse = new EnvironmentalResponseData();
            _co2Response = new EnvironmentalResponseData();
        }

        /// <summary>
        /// Calculates the environmental modifier for trait expression based on conditions.
        /// </summary>
        /// <param name="environment">Current environmental conditions</param>
        /// <param name="baseValue">Base genetic trait value</param>
        /// <returns>Modified trait value</returns>
        public float CalculateEnvironmentalModifier(EnvironmentalConditions environment, float baseValue)
        {

            float modifier = 1f;

            // Temperature effects
            if (_temperatureResponse != null)
            {
                modifier *= CalculateFactorResponse(_temperatureResponse, environment.Temperature);
            }

            // Light effects
            if (_lightResponse != null)
            {
                modifier *= CalculateFactorResponse(_lightResponse, environment.LightIntensity);
            }

            // Humidity effects
            if (_humidityResponse != null)
            {
                modifier *= CalculateFactorResponse(_humidityResponse, environment.Humidity);
            }

            // CO2 effects
            if (_co2Response != null)
            {
                modifier *= CalculateFactorResponse(_co2Response, environment.CO2Level);
            }

            // Apply environmental sensitivity
            modifier = Mathf.Lerp(1f, modifier, _environmentalSensitivity);

            // Apply phenotypic plasticity
            modifier = Mathf.Lerp(1f, modifier, _phenotypicPlasticity);

            // Calculate final value based on interaction type
            switch (_interactionType)
            {
                case GxEInteractionType.Additive:
                    return baseValue + (modifier - 1f);
                case GxEInteractionType.Multiplicative:
                    return baseValue * modifier;
                case GxEInteractionType.Threshold:
                    return modifier > 1.1f ? baseValue * modifier : baseValue;
                default:
                    return baseValue * modifier;
            }
        }

        /// <summary>
        /// Calculates stress response effects on trait expression.
        /// </summary>
        /// <param name="environment">Current environmental conditions</param>
        /// <returns>Stress modifier (0-1, where 1 is no stress)</returns>
        public float CalculateStressResponse(EnvironmentalConditions environment)
        {
            return CalculateStressResponse(environment, GetDefaultOptimalConditions());
        }

        /// <summary>
        /// Calculates stress response effects on trait expression.
        /// </summary>
        /// <param name="environment">Current environmental conditions</param>
        /// <param name="optimalConditions">Optimal environmental conditions for comparison</param>
        /// <returns>Stress modifier (0-1, where 1 is no stress)</returns>
        public float CalculateStressResponse(EnvironmentalConditions environment, EnvironmentalConditions optimalConditions)
        {
            float stressLevel = 0f;
            float stressFactors = 0f;

            // Temperature stress
            {
                float tempDiff = Mathf.Abs(environment.Temperature - optimalConditions.Temperature);
                float tempStress = tempDiff > 5f ? (tempDiff - 5f) / 15f : 0f; // Stress beyond 5°C difference
                stressLevel += tempStress * (tempDiff > 0 ? (environment.Temperature > optimalConditions.Temperature ? _heatStressModifier : _coldStressModifier) : 0f);
                stressFactors++;
            }

            // Light stress
            if (environment.LightIntensity < 200f) // Low light stress
            {
                stressLevel += (200f - environment.LightIntensity) / 200f * Mathf.Abs(_lightStressModifier);
                stressFactors++;
            }

            // Humidity stress (simplified)
            float humidityStress = Mathf.Abs(environment.Humidity - 55f) > 20f ? 0.1f : 0f;
            stressLevel += humidityStress;
            if (humidityStress > 0) stressFactors++;

            // Average stress if multiple factors
            if (stressFactors > 0)
            {
                stressLevel /= stressFactors;
            }

            return Mathf.Clamp01(1f - stressLevel);
        }

        /// <summary>
        /// Gets default optimal environmental conditions for stress calculations.
        /// </summary>
        private EnvironmentalConditions GetDefaultOptimalConditions()
        {
            return new EnvironmentalConditions
            {
                Temperature = 23f,
                Humidity = 55f,
                LightIntensity = 400f,
                CO2Level = 400f
            };
        }

        private float CalculateFactorResponse(EnvironmentalResponseData response, float value)
        {
            if (response == null) return 1f;

            float normalizedValue = value;
            
            // Simple response calculation - could use curves for more complexity
            if (value < response.OptimalMin)
            {
                float deficit = response.OptimalMin - value;
                float maxDeficit = response.OptimalMin - response.ToleranceMin;
                if (maxDeficit > 0)
                {
                    float deficitRatio = Mathf.Clamp01(deficit / maxDeficit);
                    return Mathf.Lerp(1f, response.MinResponse, deficitRatio);
                }
            }
            else if (value > response.OptimalMax)
            {
                float excess = value - response.OptimalMax;
                float maxExcess = response.ToleranceMax - response.OptimalMax;
                if (maxExcess > 0)
                {
                    float excessRatio = Mathf.Clamp01(excess / maxExcess);
                    return Mathf.Lerp(1f, response.MinResponse, excessRatio);
                }
            }

            return 1f; // Within optimal range
        }
    }

    /// <summary>
    /// Environmental response data for a specific factor
    /// </summary>
    [System.Serializable]
    public class EnvironmentalResponseData
    {
        [SerializeField] private float _optimalMin = 20f;
        [SerializeField] private float _optimalMax = 25f;
        [SerializeField] private float _toleranceMin = 15f;
        [SerializeField] private float _toleranceMax = 30f;
        [SerializeField, Range(0f, 1f)] private float _minResponse = 0.1f;

        public float OptimalMin => _optimalMin;
        public float OptimalMax => _optimalMax;
        public float ToleranceMin => _toleranceMin;
        public float ToleranceMax => _toleranceMax;
        public float MinResponse => _minResponse;

        public EnvironmentalResponseData()
        {
            _optimalMin = 20f;
            _optimalMax = 25f;
            _toleranceMin = 15f;
            _toleranceMax = 30f;
            _minResponse = 0.1f;
        }

        public EnvironmentalResponseData(float optimalMin, float optimalMax, float toleranceMin, float toleranceMax, float minResponse = 0.1f)
        {
            _optimalMin = optimalMin;
            _optimalMax = optimalMax;
            _toleranceMin = toleranceMin;
            _toleranceMax = toleranceMax;
            _minResponse = minResponse;
        }
    }
}