using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using ProjectChimera.Data.Shared;

namespace ProjectChimera.Systems.Cultivation
{
    /// <summary>
    /// BASIC: Simple environmental response for Project Chimera's cultivation system.
    /// Focuses on essential environmental reactions without complex GxE interactions.
    /// </summary>
    public class EnvironmentalResponseSystem : MonoBehaviour
    {
        [Header("Basic Environmental Settings")]
        [SerializeField] private bool _enableBasicResponses = true;
        [SerializeField] private float _stressThreshold = 0.7f;
        [SerializeField] private bool _enableLogging = true;

        // Basic environmental tracking
        private EnvironmentalConditions _currentConditions;
        private float _environmentalFitness = 1f;
        private bool _isInitialized = false;

        /// <summary>
        /// Events for environmental changes
        /// </summary>
        public event System.Action<float> OnFitnessChanged;
        public event System.Action OnStressDetected;

        /// <summary>
        /// Initialize basic environmental response
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            _isInitialized = true;
            _currentConditions = new EnvironmentalConditions
            {
                Temperature = 25f,
                Humidity = 60f,
                LightIntensity = 500f
            };

            if (_enableLogging)
            {
                ChimeraLogger.Log("[EnvironmentalResponseSystem] Initialized successfully");
            }
        }

        /// <summary>
        /// Update environmental conditions
        /// </summary>
        public void UpdateConditions(EnvironmentalConditions conditions)
        {
            if (!_enableBasicResponses || !_isInitialized) return;

            _currentConditions = conditions;
            float newFitness = CalculateFitness(conditions);

            if (Mathf.Abs(newFitness - _environmentalFitness) > 0.01f)
            {
                _environmentalFitness = newFitness;
                OnFitnessChanged?.Invoke(_environmentalFitness);

                if (_environmentalFitness < _stressThreshold)
                {
                    OnStressDetected?.Invoke();
                }

                if (_enableLogging)
                {
                    ChimeraLogger.Log($"[EnvironmentalResponseSystem] Fitness updated to {_environmentalFitness:F2}");
                }
            }
        }

        /// <summary>
        /// Get current environmental fitness
        /// </summary>
        public float GetEnvironmentalFitness()
        {
            return _environmentalFitness;
        }

        /// <summary>
        /// Check if environment is causing stress
        /// </summary>
        public bool IsStressfulEnvironment()
        {
            return _environmentalFitness < _stressThreshold;
        }

        /// <summary>
        /// Get current conditions
        /// </summary>
        public EnvironmentalConditions GetCurrentConditions()
        {
            return _currentConditions;
        }

        /// <summary>
        /// Get environmental recommendations
        /// </summary>
        public string GetEnvironmentalRecommendation()
        {
            if (_environmentalFitness >= 0.9f) return "Environment optimal";
            if (_environmentalFitness >= 0.7f) return "Environment acceptable";
            if (_environmentalFitness >= 0.5f) return "Monitor environmental conditions";
            return "Environmental conditions need attention";
        }

        /// <summary>
        /// Get environmental status
        /// </summary>
        public EnvironmentalStatus GetStatus()
        {
            return new EnvironmentalStatus
            {
                CurrentConditions = _currentConditions,
                EnvironmentalFitness = _environmentalFitness,
                IsStressful = IsStressfulEnvironment(),
                Recommendation = GetEnvironmentalRecommendation()
            };
        }

        /// <summary>
        /// Reset environmental state
        /// </summary>
        public void Reset()
        {
            _environmentalFitness = 1f;
            _currentConditions = new EnvironmentalConditions
            {
                Temperature = 25f,
                Humidity = 60f,
                LightIntensity = 500f
            };

            if (_enableLogging)
            {
                ChimeraLogger.Log("[EnvironmentalResponseSystem] Reset to default conditions");
            }
        }

        #region Private Methods

        private float CalculateFitness(EnvironmentalConditions conditions)
        {
            // Simple fitness calculation based on optimal ranges
            float tempScore = IsOptimalTemperature(conditions.Temperature) ? 1f : 0.5f;
            float humidityScore = IsOptimalHumidity(conditions.Humidity) ? 1f : 0.5f;
            float lightScore = IsOptimalLight(conditions.LightIntensity) ? 1f : 0.5f;

            // Average the scores
            return (tempScore + humidityScore + lightScore) / 3f;
        }

        private bool IsOptimalTemperature(float temperature)
        {
            return temperature >= 20f && temperature <= 28f;
        }

        private bool IsOptimalHumidity(float humidity)
        {
            return humidity >= 40f && humidity <= 70f;
        }

        private bool IsOptimalLight(float lightIntensity)
        {
            return lightIntensity >= 300f && lightIntensity <= 800f;
        }

        #endregion
    }

    /// <summary>
    /// Environmental status
    /// </summary>
    [System.Serializable]
    public struct EnvironmentalStatus
    {
        public EnvironmentalConditions CurrentConditions;
        public float EnvironmentalFitness;
        public bool IsStressful;
        public string Recommendation;
    }
}
