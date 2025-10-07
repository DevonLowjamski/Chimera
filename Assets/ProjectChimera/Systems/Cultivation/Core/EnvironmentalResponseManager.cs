using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using ProjectChimera.Data.Cultivation;
using ProjectChimera.Systems.Cultivation.Advanced;
using System;

namespace ProjectChimera.Systems.Cultivation.Core
{
    /// <summary>
    /// REFACTORED: Environmental Response Manager - Focused plant environmental adaptation
    /// Handles plant responses to environmental changes and stress factors
    /// Single Responsibility: Environmental response processing
    /// </summary>
    public class EnvironmentalResponseManager : MonoBehaviour
    {
        [Header("Environmental Response Settings")]
        [SerializeField] private bool _enableEnvironmentalResponse = true;
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private float _responseUpdateInterval = 2f;

        [Header("Response Sensitivity")]
        [SerializeField] private float _stressThreshold = 0.3f;
        [SerializeField] private float _recoveryRate = 0.1f;
        [SerializeField] private float _adaptationRate = 0.05f;
        [SerializeField] private float _maxStressAccumulation = 1f;

        [Header("Environmental Factors")]
        [SerializeField] private float _lightStressWeight = 1.2f;
        [SerializeField] private float _waterStressWeight = 1.5f;
        [SerializeField] private float _temperatureStressWeight = 1.3f;
        [SerializeField] private float _humidityStressWeight = 0.8f;

        // Response management
        private readonly Dictionary<string, EnvironmentalResponseData> _plantResponses = new Dictionary<string, EnvironmentalResponseData>();
        private readonly List<string> _plantsToProcess = new List<string>();
        private EnvironmentalResponseStats _stats = new EnvironmentalResponseStats();

        // Timing
        private float _lastResponseUpdate;

        // Properties
        public bool IsEnabled { get; private set; } = true;
        public EnvironmentalResponseStats GetStats() => _stats;

        // Events
        public System.Action<string, EnvironmentalStress> OnPlantStressChanged;
        public System.Action<string, float> OnAdaptationProgressChanged;
        public System.Action<string> OnPlantRecovered;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            _lastResponseUpdate = Time.time;
            _stats = new EnvironmentalResponseStats();

            if (_enableLogging)
                ChimeraLogger.Log("CULTIVATION", "🌡️ EnvironmentalResponseManager initialized", this);
        }

        /// <summary>
        /// Process environmental responses for all plants
        /// </summary>
        public void ProcessEnvironmentalResponse()
        {
            if (!IsEnabled || !_enableEnvironmentalResponse) return;

            float currentTime = Time.time;
            if (currentTime - _lastResponseUpdate < _responseUpdateInterval) return;

            _lastResponseUpdate = currentTime;
            var startTime = Time.realtimeSinceStartup;

            _plantsToProcess.Clear();
            foreach (var kvp in _plantResponses)
            {
                if (kvp.Value.IsActive)
                {
                    _plantsToProcess.Add(kvp.Key);
                }
            }

            foreach (var plantId in _plantsToProcess)
            {
                try
                {
                    ProcessPlantEnvironmentalResponse(plantId);
                    _stats.ResponsesProcessed++;
                }
                catch (System.Exception ex)
                {
                    _stats.ResponseErrors++;
                    if (_enableLogging)
                        ChimeraLogger.LogError("CULTIVATION", $"Environmental response error for plant {plantId}: {ex.Message}", this);
                }
            }

            // Update statistics
            var endTime = Time.realtimeSinceStartup;
            var responseTime = endTime - startTime;
            _stats.LastResponseTime = responseTime;
            _stats.AverageResponseTime = (_stats.AverageResponseTime + responseTime) / 2f;
            if (responseTime > _stats.MaxResponseTime) _stats.MaxResponseTime = responseTime;
        }

        /// <summary>
        /// Register plant for environmental response monitoring
        /// </summary>
        public bool RegisterPlantForResponse(AdvancedPlantInstance plant)
        {
            if (plant == null || string.IsNullOrEmpty(plant.PlantId))
                return false;

            if (_plantResponses.ContainsKey(plant.PlantId))
                return false;

            var responseData = new EnvironmentalResponseData
            {
                PlantId = plant.PlantId,
                IsActive = true,
                CurrentStressLevel = 0f,
                AdaptationProgress = 0f,
                LastEnvironmentalCheck = Time.time,
                StressAccumulation = 0f
            };

            _plantResponses[plant.PlantId] = responseData;
            _stats.ActiveResponses = _plantResponses.Count;

            if (_enableLogging)
                ChimeraLogger.Log("CULTIVATION", $"Registered plant for environmental response: {plant.PlantId}", this);

            return true;
        }

        /// <summary>
        /// Unregister plant from environmental response monitoring
        /// </summary>
        public bool UnregisterPlantFromResponse(string plantId)
        {
            if (!_plantResponses.ContainsKey(plantId))
                return false;

            _plantResponses.Remove(plantId);
            _stats.ActiveResponses = _plantResponses.Count;

            if (_enableLogging)
                ChimeraLogger.Log("CULTIVATION", $"Unregistered plant from environmental response: {plantId}", this);

            return true;
        }

        /// <summary>
        /// Get environmental stress level for plant
        /// </summary>
        public float GetPlantStressLevel(string plantId)
        {
            if (_plantResponses.TryGetValue(plantId, out var response))
            {
                return response.CurrentStressLevel;
            }
            return 0f;
        }

        /// <summary>
        /// Get adaptation progress for plant
        /// </summary>
        public float GetAdaptationProgress(string plantId)
        {
            if (_plantResponses.TryGetValue(plantId, out var response))
            {
                return response.AdaptationProgress;
            }
            return 0f;
        }

        /// <summary>
        /// Calculate environmental stress for plant
        /// </summary>
        public EnvironmentalStress CalculateEnvironmentalStress(AdvancedPlantInstance plant, EnvironmentalConditions conditions)
        {
            var stress = new EnvironmentalStress();

            // Calculate individual stress factors
            stress.LightStress = CalculateLightStress(plant, conditions.LightLevel);
            stress.WaterStress = CalculateWaterStress(plant, conditions.WaterLevel);
            stress.TemperatureStress = CalculateTemperatureStress(plant, conditions.Temperature);
            stress.HumidityStress = CalculateHumidityStress(plant, conditions.Humidity);

            // Calculate overall stress level
            stress.OverallStress = (stress.LightStress * _lightStressWeight +
                                  stress.WaterStress * _waterStressWeight +
                                  stress.TemperatureStress * _temperatureStressWeight +
                                  stress.HumidityStress * _humidityStressWeight) /
                                  (_lightStressWeight + _waterStressWeight + _temperatureStressWeight + _humidityStressWeight);

            stress.IsStressed = stress.OverallStress > _stressThreshold;
            return stress;
        }

        /// <summary>
        /// Set system enabled/disabled
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;
            if (_enableLogging)
                ChimeraLogger.Log("CULTIVATION", $"EnvironmentalResponseManager: {(enabled ? "enabled" : "disabled")}", this);
        }

        #region Private Methods

        /// <summary>
        /// Process environmental response for individual plant
        /// </summary>
        private void ProcessPlantEnvironmentalResponse(string plantId)
        {
            if (!_plantResponses.TryGetValue(plantId, out var response))
                return;

            // Get current environmental conditions
            var conditions = GetEnvironmentalConditions(plantId);

            // Get plant instance (would come from plant lifecycle manager)
            var plant = GetPlantInstance(plantId);
            if (plant == null) return;

            // Calculate current stress
            var stress = CalculateEnvironmentalStress(plant, conditions);
            var previousStressLevel = response.CurrentStressLevel;
            response.CurrentStressLevel = stress.OverallStress;

            // Process stress accumulation
            if (stress.IsStressed)
            {
                response.StressAccumulation += stress.OverallStress * Time.deltaTime;
                response.StressAccumulation = Mathf.Min(response.StressAccumulation, _maxStressAccumulation);
            }
            else
            {
                // Recovery from stress
                response.StressAccumulation -= _recoveryRate * Time.deltaTime;
                response.StressAccumulation = Mathf.Max(response.StressAccumulation, 0f);
            }

            // Process adaptation
            ProcessAdaptation(ref response, stress);

            // Apply effects to plant
            ApplyEnvironmentalEffects(plant, stress, response);

            // Update response data
            response.LastEnvironmentalCheck = Time.time;
            _plantResponses[plantId] = response;

            // Fire events
            if (Mathf.Abs(previousStressLevel - response.CurrentStressLevel) > 0.1f)
            {
                OnPlantStressChanged?.Invoke(plantId, stress);
            }

            if (!stress.IsStressed && previousStressLevel > _stressThreshold)
            {
                OnPlantRecovered?.Invoke(plantId);
            }
        }

        /// <summary>
        /// Process adaptation progress
        /// </summary>
        private void ProcessAdaptation(ref EnvironmentalResponseData response, EnvironmentalStress stress)
        {
            if (stress.IsStressed)
            {
                // Increase adaptation when under stress
                response.AdaptationProgress += _adaptationRate * Time.deltaTime;
                response.AdaptationProgress = Mathf.Min(response.AdaptationProgress, 1f);

                OnAdaptationProgressChanged?.Invoke(response.PlantId, response.AdaptationProgress);
            }
        }

        /// <summary>
        /// Apply environmental effects to plant
        /// </summary>
        private void ApplyEnvironmentalEffects(AdvancedPlantInstance plant, EnvironmentalStress stress, EnvironmentalResponseData response)
        {
            // Reduce health based on stress
            if (stress.IsStressed)
            {
                float healthReduction = stress.OverallStress * 0.1f * Time.deltaTime;
                plant.HealthPercentage -= healthReduction;
                plant.HealthPercentage = Mathf.Max(plant.HealthPercentage, 0f);
            }
            else if (response.StressAccumulation < 0.1f)
            {
                // Gradual health recovery when not stressed
                float healthRecovery = _recoveryRate * 0.5f * Time.deltaTime;
                plant.HealthPercentage += healthRecovery;
                plant.HealthPercentage = Mathf.Min(plant.HealthPercentage, 100f);
            }

            // Apply adaptation benefits
            if (response.AdaptationProgress > 0.5f)
            {
                plant.GrowthRateModifier = 1f + (response.AdaptationProgress * 0.2f);
            }
        }

        /// <summary>
        /// Get environmental conditions for plant
        /// </summary>
        private EnvironmentalConditions GetEnvironmentalConditions(string plantId)
        {
            // This would interface with environmental monitoring systems
            return new EnvironmentalConditions
            {
                LightLevel = 0.8f,
                WaterLevel = 0.7f,
                NutrientLevel = 0.6f,
                Temperature = 22f,
                Humidity = 0.65f
            };
        }

        /// <summary>
        /// Get plant instance
        /// </summary>
        private AdvancedPlantInstance GetPlantInstance(string plantId)
        {
            // This would come from the plant lifecycle manager
            return null; // Placeholder
        }

        /// <summary>
        /// Calculate light stress
        /// </summary>
        private float CalculateLightStress(AdvancedPlantInstance plant, float lightLevel)
        {
            float optimalLight = 0.8f; // Default optimal light level
            float deviation = Mathf.Abs(lightLevel - optimalLight) / optimalLight;
            return Mathf.Clamp01(deviation);
        }

        /// <summary>
        /// Calculate water stress
        /// </summary>
        private float CalculateWaterStress(AdvancedPlantInstance plant, float waterLevel)
        {
            float optimalWater = 0.7f; // Default optimal water level
            if (waterLevel < optimalWater)
            {
                return Mathf.Clamp01((optimalWater - waterLevel) / optimalWater);
            }
            else if (waterLevel > 0.9f)
            {
                return Mathf.Clamp01((waterLevel - 0.9f) / 0.1f); // Overwatering stress
            }
            return 0f;
        }

        /// <summary>
        /// Calculate temperature stress
        /// </summary>
        private float CalculateTemperatureStress(AdvancedPlantInstance plant, float temperature)
        {
            float optimalTemp = plant?.OptimalTemperature ?? 22f;
            float tolerance = 5f;
            float deviation = Mathf.Abs(temperature - optimalTemp);

            if (deviation <= tolerance) return 0f;

            return Mathf.Clamp01((deviation - tolerance) / (tolerance * 2));
        }

        /// <summary>
        /// Calculate humidity stress
        /// </summary>
        private float CalculateHumidityStress(AdvancedPlantInstance plant, float humidity)
        {
            float optimalHumidity = 0.6f; // Default optimal humidity
            float deviation = Mathf.Abs(humidity - optimalHumidity) / optimalHumidity;
            return Mathf.Clamp01(deviation * 0.5f); // Less impactful than other factors
        }

        #endregion
    }

    /// <summary>
    /// Environmental response data for individual plant
    /// </summary>
    [System.Serializable]
    public struct EnvironmentalResponseData
    {
        public string PlantId;
        public bool IsActive;
        public float CurrentStressLevel;
        public float AdaptationProgress;
        public float LastEnvironmentalCheck;
        public float StressAccumulation;
    }

    /// <summary>
    /// Environmental response statistics
    /// </summary>
    [System.Serializable]
    public struct EnvironmentalResponseStats
    {
        public int ActiveResponses;
        public int ResponsesProcessed;
        public int ResponseErrors;
        public float AverageResponseTime;
        public float MaxResponseTime;
        public float LastResponseTime;
    }

    /// <summary>
    /// Environmental stress data
    /// </summary>
    [System.Serializable]
    public struct EnvironmentalStress
    {
        public float LightStress;
        public float WaterStress;
        public float TemperatureStress;
        public float HumidityStress;
        public float OverallStress;
        public bool IsStressed;
    }
}