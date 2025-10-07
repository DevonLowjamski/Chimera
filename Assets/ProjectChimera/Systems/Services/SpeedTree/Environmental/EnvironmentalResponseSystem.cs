using ProjectChimera.Core.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProjectChimera.Core;
using EnvironmentalConditions = ProjectChimera.Data.Shared.EnvironmentalConditions;
using Logger = ProjectChimera.Core.Logging.ChimeraLogger;

namespace ProjectChimera.Systems.Services.SpeedTree.Environmental
{
    /// <summary>
    /// Environmental Response System for SpeedTree Plants
    /// Handles how individual plants respond to environmental conditions
    /// in the cannabis cultivation simulation.
    /// </summary>
    public class EnvironmentalResponseSystem : MonoBehaviour
    {
        [Header("Environmental Response Configuration")]
        [SerializeField] private bool _enableEnvironmentalResponse = true;
        [SerializeField] private float _responseUpdateFrequency = 0.5f;
        [SerializeField] private float _adaptationRate = 0.1f;

        // Plant response tracking
        private Dictionary<int, PlantEnvironmentalResponse> _plantResponses = new Dictionary<int, PlantEnvironmentalResponse>();
        private Dictionary<int, EnvironmentalConditions> _lastKnownConditions = new Dictionary<int, EnvironmentalConditions>();
        private List<int> _plantsNeedingUpdate = new List<int>();

        // Update timing
        private float _lastResponseUpdate = 0f;

        #region Public Events
        public event Action<int, EnvironmentalConditions> OnPlantEnvironmentalResponse;
        public event Action<int, float> OnPlantAdaptationProgress;
        #endregion

        #region Initialization
        public void Initialize()
        {
            Logger.Log("SPEEDTREE/ENV", "EnvironmentalResponseSystem Initialize", this);
            _plantResponses.Clear();
            _lastKnownConditions.Clear();
            _plantsNeedingUpdate.Clear();
            Logger.Log("SPEEDTREE/ENV", "EnvironmentalResponseSystem Initialized", this);
        }

        public void Shutdown()
        {
            Logger.Log("SPEEDTREE/ENV", "EnvironmentalResponseSystem Shutdown", this);
            _plantResponses.Clear();
            _lastKnownConditions.Clear();
            _plantsNeedingUpdate.Clear();
            Logger.Log("SPEEDTREE/ENV", "EnvironmentalResponseSystem Shutdown Complete", this);
        }
        #endregion

        #region Core Environmental Response Logic

        /// <summary>
        /// Updates environmental response for a specific plant
        /// </summary>
        public void UpdatePlantEnvironmentalResponse(int plantId, EnvironmentalConditions conditions)
        {
            if (plantId <= 0 || !_enableEnvironmentalResponse) return;

            try
            {
                // Get or create response data for this plant
                if (!_plantResponses.TryGetValue(plantId, out var response))
                {
                    response = new PlantEnvironmentalResponse(plantId);
                    _plantResponses[plantId] = response;
                }

                // Check if conditions have changed significantly
                if (HasEnvironmentalConditionsChanged(plantId, conditions))
                {
                    // Calculate plant response to new conditions
                    var responseData = CalculateEnvironmentalResponse(plantId, conditions, response);

                    // Update response data
                    response.UpdateResponse(responseData);
                    _plantResponses[plantId] = response;
                    _lastKnownConditions[plantId] = conditions;

                    // Mark plant for visual updates
                    if (!_plantsNeedingUpdate.Contains(plantId))
                    {
                        _plantsNeedingUpdate.Add(plantId);
                    }

                    // Trigger events
                    OnPlantEnvironmentalResponse?.Invoke(plantId, conditions);

                    Logger.Log("SPEEDTREE/ENV", $"Plant {plantId} response updated", this);
                }
            }
            catch (Exception)
            {
                Logger.LogWarning("SPEEDTREE/ENV", "UpdatePlantEnvironmentalResponse exception", this);
            }
        }

        /// <summary>
        /// Applies environmental conditions to a plant
        /// </summary>
        public void ApplyEnvironmentalConditions(int plantId, EnvironmentalConditions conditions)
        {
            if (plantId <= 0) return;

            try
            {
                UpdatePlantEnvironmentalResponse(plantId, conditions);

                // Calculate adaptation progress
                float adaptationProgress = CalculateAdaptationProgress(plantId, conditions);
                OnPlantAdaptationProgress?.Invoke(plantId, adaptationProgress);

                Logger.Log("SPEEDTREE/ENV", $"Applied env conditions to plant {plantId}", this);
            }
            catch (Exception)
            {
                Logger.LogWarning("SPEEDTREE/ENV", "ApplyEnvironmentalConditions exception", this);
            }
        }

        /// <summary>
        /// Processes environmental responses for multiple plants
        /// </summary>
        public void ProcessEnvironmentalResponses(IEnumerable<int> plantIds, EnvironmentalConditions conditions)
        {
            if (plantIds == null) return;

            foreach (var plantId in plantIds)
            {
                ApplyEnvironmentalConditions(plantId, conditions);
            }
        }

        #endregion

        #region Response Calculation Logic

        private PlantResponseData CalculateEnvironmentalResponse(int plantId, EnvironmentalConditions conditions, PlantEnvironmentalResponse response)
        {
            var responseData = new PlantResponseData();

            // Temperature response
            responseData.TemperatureStress = CalculateTemperatureStress(conditions.Temperature, response.BaselineTemperature);
            responseData.TemperatureAdaptation = CalculateTemperatureAdaptation(conditions.Temperature, response);

            // Humidity response
            responseData.HumidityStress = CalculateHumidityStress(conditions.Humidity, response.BaselineHumidity);
            responseData.HumidityAdaptation = CalculateHumidityAdaptation(conditions.Humidity, response);

            // Light response
            responseData.LightStress = CalculateLightStress(conditions.LightIntensity, response.BaselineLight);
            responseData.LightAdaptation = CalculateLightAdaptation(conditions.LightIntensity, response);

            // CO2 response
            responseData.CO2Stress = CalculateCO2Stress(conditions.CO2Level, response.BaselineCO2);
            responseData.CO2Adaptation = CalculateCO2Adaptation(conditions.CO2Level, response);

            // Overall stress calculation
            responseData.OverallStress = CalculateOverallStress(responseData);
            responseData.AdaptationRate = _adaptationRate;

            return responseData;
        }

        private float CalculateTemperatureStress(float currentTemp, float baselineTemp)
        {
            float delta = Mathf.Abs(currentTemp - baselineTemp);
            // Optimal temperature range for cannabis: 20-28°C
            if (currentTemp >= 20f && currentTemp <= 28f) return 0f;
            return Mathf.Clamp(delta / 10f, 0f, 1f); // Normalize to 0-1 range
        }

        private float CalculateHumidityStress(float currentHumidity, float baselineHumidity)
        {
            float delta = Mathf.Abs(currentHumidity - baselineHumidity);
            // Optimal humidity range for cannabis: 40-60%
            if (currentHumidity >= 40f && currentHumidity <= 60f) return 0f;
            return Mathf.Clamp(delta / 20f, 0f, 1f); // Normalize to 0-1 range
        }

        private float CalculateLightStress(float currentLight, float baselineLight)
        {
            float delta = Mathf.Abs(currentLight - baselineLight);
            // Light stress based on deviation from baseline
            return Mathf.Clamp(delta / baselineLight, 0f, 1f);
        }

        private float CalculateCO2Stress(float currentCO2, float baselineCO2)
        {
            float delta = Mathf.Abs(currentCO2 - baselineCO2);
            // Optimal CO2 range: 800-1500 ppm
            if (currentCO2 >= 800f && currentCO2 <= 1500f) return 0f;
            return Mathf.Clamp(delta / 500f, 0f, 1f); // Normalize to 0-1 range
        }

        private float CalculateTemperatureAdaptation(float currentTemp, PlantEnvironmentalResponse response)
        {
            // Simple adaptation model - plants gradually adapt to new conditions
            float adaptationFactor = response.AdaptationFactor;
            if (currentTemp >= 20f && currentTemp <= 28f)
            {
                adaptationFactor = Mathf.Min(adaptationFactor + _adaptationRate * Time.deltaTime, 1f);
            }
            return adaptationFactor;
        }

        private float CalculateHumidityAdaptation(float currentHumidity, PlantEnvironmentalResponse response)
        {
            float adaptationFactor = response.AdaptationFactor;
            if (currentHumidity >= 40f && currentHumidity <= 60f)
            {
                adaptationFactor = Mathf.Min(adaptationFactor + _adaptationRate * Time.deltaTime, 1f);
            }
            return adaptationFactor;
        }

        private float CalculateLightAdaptation(float currentLight, PlantEnvironmentalResponse response)
        {
            // Light adaptation is faster
            float adaptationFactor = response.AdaptationFactor;
            adaptationFactor = Mathf.Min(adaptationFactor + (_adaptationRate * 2f) * Time.deltaTime, 1f);
            return adaptationFactor;
        }

        private float CalculateCO2Adaptation(float currentCO2, PlantEnvironmentalResponse response)
        {
            float adaptationFactor = response.AdaptationFactor;
            if (currentCO2 >= 800f && currentCO2 <= 1500f)
            {
                adaptationFactor = Mathf.Min(adaptationFactor + (_adaptationRate * 0.5f) * Time.deltaTime, 1f);
            }
            return adaptationFactor;
        }

        private float CalculateOverallStress(PlantResponseData responseData)
        {
            // Weighted average of all stress factors
            float totalStress = responseData.TemperatureStress * 0.3f +
                               responseData.HumidityStress * 0.25f +
                               responseData.LightStress * 0.25f +
                               responseData.CO2Stress * 0.2f;

            return Mathf.Clamp(totalStress, 0f, 1f);
        }

        private float CalculateAdaptationProgress(int plantId, EnvironmentalConditions conditions)
        {
            if (!_plantResponses.TryGetValue(plantId, out var response))
            {
                return 0f;
            }

            // Calculate overall adaptation based on how well conditions match optimal ranges
            float tempScore = conditions.Temperature >= 20f && conditions.Temperature <= 28f ? 1f : 0f;
            float humidityScore = conditions.Humidity >= 40f && conditions.Humidity <= 60f ? 1f : 0f;
            float co2Score = conditions.CO2Level >= 800f && conditions.CO2Level <= 1500f ? 1f : 0f;

            return (tempScore + humidityScore + co2Score) / 3f * response.AdaptationFactor;
        }

        #endregion

        #region Helper Methods

        private bool HasEnvironmentalConditionsChanged(int plantId, EnvironmentalConditions conditions)
        {
            if (!_lastKnownConditions.TryGetValue(plantId, out var lastConditions))
            {
                return true; // First time seeing conditions for this plant
            }

            // Check for significant changes
            const float threshold = 0.1f; // 10% change threshold

            return Mathf.Abs(conditions.Temperature - lastConditions.Temperature) > threshold ||
                   Mathf.Abs(conditions.Humidity - lastConditions.Humidity) > threshold ||
                   Mathf.Abs(conditions.LightIntensity - lastConditions.LightIntensity) > threshold ||
                   Mathf.Abs(conditions.CO2Level - lastConditions.CO2Level) > threshold;
        }

        /// <summary>
        /// Gets the current response data for a plant
        /// </summary>
        public PlantResponseData GetPlantResponseData(int plantId)
        {
            if (_plantResponses.TryGetValue(plantId, out var response))
            {
                return response.CurrentResponse;
            }
            return new PlantResponseData();
        }

        /// <summary>
        /// Gets all plants that need response updates
        /// </summary>
        public IReadOnlyList<int> GetPlantsNeedingUpdate()
        {
            return _plantsNeedingUpdate.AsReadOnly();
        }

        /// <summary>
        /// Clears the update queue
        /// </summary>
        public void ClearUpdateQueue()
        {
            _plantsNeedingUpdate.Clear();
        }

        #endregion

        #region Update Loop

        public void Tick(float deltaTime)
        {
            if (!_enableEnvironmentalResponse) return;

            _lastResponseUpdate += deltaTime;

            if (_lastResponseUpdate >= _responseUpdateFrequency)
            {
                // Process plants needing updates
                foreach (var plantId in _plantsNeedingUpdate.ToArray())
                {
                    if (_lastKnownConditions.TryGetValue(plantId, out var conditions))
                    {
                        UpdatePlantEnvironmentalResponse(plantId, conditions);
                    }
                }

                _lastResponseUpdate = 0f;
            }
        }

        #endregion

        #region Missing Methods for SpeedTreeEnvironmentalService Integration

        /// <summary>
        /// Register a plant for environmental monitoring
        /// </summary>
        public void RegisterPlant(int plantId)
        {
            if (plantId <= 0) return;

            if (!_plantResponses.ContainsKey(plantId))
            {
                var response = new PlantEnvironmentalResponse(plantId);
                _plantResponses[plantId] = response;
                Logger.Log("SPEEDTREE/ENV", $"Registered plant {plantId} for environmental monitoring", this);
            }
        }

        /// <summary>
        /// Unregister a plant from environmental monitoring
        /// </summary>
        public void UnregisterPlant(int plantId)
        {
            if (_plantResponses.ContainsKey(plantId))
            {
                _plantResponses.Remove(plantId);
                _lastKnownConditions.Remove(plantId);
                _plantsNeedingUpdate.Remove(plantId);
                Logger.Log("SPEEDTREE/ENV", $"Unregistered plant {plantId} from environmental monitoring", this);
            }
        }

        /// <summary>
        /// Update environmental conditions for all registered plants
        /// </summary>
        public void UpdateEnvironmentalConditions(float temperature, float humidity, float lightIntensity, float windSpeed, Vector3 windDirection)
        {
            var conditions = new EnvironmentalConditions
            {
                Temperature = temperature,
                Humidity = humidity,
                LightIntensity = lightIntensity,
                CO2Level = 1200f // Default CO2 level
            };

            // Apply to all registered plants
            foreach (var plantId in _plantResponses.Keys.ToArray())
            {
                ApplyEnvironmentalConditions(plantId, conditions);
            }
        }

        /// <summary>
        /// Get plant response data for SpeedTreeEnvironmentalService
        /// </summary>
        public EnvironmentalResponseData GetPlantResponse(int plantId)
        {
            if (_plantResponses.TryGetValue(plantId, out var response))
            {
                var responseData = new EnvironmentalResponseData();
                responseData.PlantId = plantId;
                responseData.HealthResponse = 1f - response.CurrentResponse.OverallStress;
                responseData.GrowthResponse = 1f - response.CurrentResponse.OverallStress;
                responseData.StressResponse = response.CurrentResponse.OverallStress;
                responseData.TemperatureInfluence = response.CurrentResponse.TemperatureStress;
                responseData.HumidityInfluence = response.CurrentResponse.HumidityStress;
                responseData.LightInfluence = response.CurrentResponse.LightStress;
                responseData.OverallResponseStrength = 1f - response.CurrentResponse.OverallStress;
                responseData.AdaptationRate = response.AdaptationFactor;
                responseData.IsStressed = response.CurrentResponse.OverallStress > 0.5f;
                return responseData;
            }
            return new EnvironmentalResponseData { PlantId = plantId };
        }

        /// <summary>
        /// Get environmental statistics
        /// </summary>
        public EnvironmentalStatistics GetStatistics()
        {
            return new EnvironmentalStatistics
            {
                TotalPlantsMonitored = _plantResponses.Count,
                PlantsStressed = _plantResponses.Values.Count(r => r.CurrentResponse.OverallStress > 0.5f),
                SystemsEnabled = _enableEnvironmentalResponse
            };
        }

        /// <summary>
        /// Reset all environmental responses
        /// </summary>
        public void ResetResponses()
        {
            _plantResponses.Clear();
            _lastKnownConditions.Clear();
            _plantsNeedingUpdate.Clear();
            Logger.Log("SPEEDTREE/ENV", "Reset all environmental responses", this);
        }

        /// <summary>
        /// Update frequency property for external configuration
        /// </summary>
        public float UpdateFrequency
        {
            get { return _responseUpdateFrequency; }
            set { _responseUpdateFrequency = value; }
        }

        /// <summary>
        /// Enable/disable environmental response property
        /// </summary>
        public bool EnableEnvironmentalResponse
        {
            get { return _enableEnvironmentalResponse; }
            set { _enableEnvironmentalResponse = value; }
        }

        #endregion
    }

    #region Data Structures


    /// <summary>
    /// Data structure for plant environmental response
    /// </summary>
    [Serializable]
    public struct PlantResponseData
    {
        public float TemperatureStress;
        public float HumidityStress;
        public float LightStress;
        public float CO2Stress;
        public float OverallStress;
        public float TemperatureAdaptation;
        public float HumidityAdaptation;
        public float LightAdaptation;
        public float CO2Adaptation;
        public float AdaptationRate;
    }

    /// <summary>
    /// Tracks environmental response data for individual plants
    /// </summary>
    [Serializable]
    public class PlantEnvironmentalResponse
    {
        public int PlantId;
        public float BaselineTemperature = 24f; // Optimal cannabis temperature
        public float BaselineHumidity = 50f;   // Optimal cannabis humidity
        public float BaselineLight = 1f;       // Baseline light intensity
        public float BaselineCO2 = 1200f;      // Optimal CO2 level
        public float AdaptationFactor = 0f;    // How well adapted (0-1)
        public PlantResponseData CurrentResponse;

        public PlantEnvironmentalResponse(int plantId)
        {
            PlantId = plantId;
            CurrentResponse = new PlantResponseData();
        }

        public void UpdateResponse(PlantResponseData newResponse)
        {
            CurrentResponse = newResponse;

            // Update adaptation factor based on overall stress
            if (CurrentResponse.OverallStress < 0.3f) // Low stress
            {
                AdaptationFactor = Mathf.Min(AdaptationFactor + 0.01f, 1f);
            }
            else if (CurrentResponse.OverallStress > 0.7f) // High stress
            {
                AdaptationFactor = Mathf.Max(AdaptationFactor - 0.02f, 0f);
            }
        }
    }

    #endregion
}
