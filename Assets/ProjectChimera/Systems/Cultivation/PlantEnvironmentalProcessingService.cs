using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Cultivation
{
    /// <summary>
    /// BASIC: Core environmental monitoring for Project Chimera's cultivation.
    /// Focuses solely on essential environmental tracking and simple stress effects.
    /// </summary>
    public class PlantEnvironmentalProcessingService : MonoBehaviour
    {
        [Header("Basic Environmental Settings")]
        [SerializeField] private bool _enableBasicMonitoring = true;
        [SerializeField] private float _updateInterval = 2f;
        [SerializeField] private bool _enableStressEffects = true;

        // Basic tracking
        private readonly Dictionary<string, float> _plantHealth = new Dictionary<string, float>();
        private float _lastUpdateTime = 0f;
        private bool _isInitialized = false;

        /// <summary>
        /// Initialize basic environmental processing
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            _isInitialized = true;
            _lastUpdateTime = Time.time;

            if (_enableBasicMonitoring)
            {
                ChimeraLogger.Log("[PlantEnvironmentalProcessingService] Initialized");
            }
        }

        /// <summary>
        /// Update environmental processing for a plant
        /// </summary>
        public void UpdatePlantEnvironment(PlantInstance plant, float deltaTime)
        {
            if (!_enableBasicMonitoring || !_isInitialized || plant == null) return;

            // Throttle updates to avoid performance issues
            if (Time.time - _lastUpdateTime < _updateInterval) return;

            _lastUpdateTime = Time.time;

            // Get basic environmental conditions
            var conditions = GetBasicEnvironmentalConditions(plant);

            // Calculate simple health impact
            float healthImpact = CalculateHealthImpact(conditions);

            // Store plant health
            _plantHealth[plant.PlantID] = Mathf.Max(0f, Mathf.Min(1f, healthImpact));

            // Apply basic stress if enabled
            if (_enableStressEffects && healthImpact < 0.7f)
            {
                ApplyBasicStress(plant, healthImpact);
            }
        }

        /// <summary>
        /// Get plant health status
        /// </summary>
        public float GetPlantHealth(string plantId)
        {
            return _plantHealth.GetValueOrDefault(plantId, 1f);
        }

        /// <summary>
        /// Check if plant environment is optimal
        /// </summary>
        public bool IsOptimalEnvironment(string plantId)
        {
            return GetPlantHealth(plantId) >= 0.8f;
        }

        /// <summary>
        /// Get environmental recommendation for a plant
        /// </summary>
        public string GetEnvironmentalRecommendation(string plantId)
        {
            float health = GetPlantHealth(plantId);

            if (health >= 0.8f) return "Environment optimal";
            if (health >= 0.6f) return "Environment acceptable";
            if (health >= 0.4f) return "Check temperature and humidity";
            return "Immediate environmental attention needed";
        }

        /// <summary>
        /// Clear all plant data
        /// </summary>
        public void ClearAllData()
        {
            _plantHealth.Clear();
            ChimeraLogger.Log("[PlantEnvironmentalProcessingService] Cleared all data");
        }

        /// <summary>
        /// Get monitoring statistics
        /// </summary>
        public EnvironmentalStats GetStatistics()
        {
            int totalPlants = _plantHealth.Count;
            int healthyPlants = _plantHealth.Count(kvp => kvp.Value >= 0.8f);
            int stressedPlants = _plantHealth.Count(kvp => kvp.Value < 0.6f);

            return new EnvironmentalStats
            {
                TotalPlants = totalPlants,
                HealthyPlants = healthyPlants,
                StressedPlants = stressedPlants,
                IsMonitoringEnabled = _enableBasicMonitoring
            };
        }

        #region Private Methods

        private BasicEnvironmentalConditions GetBasicEnvironmentalConditions(PlantInstance plant)
        {
            // Get basic conditions from plant or use defaults
            if (plant != null && plant.EnvironmentData != null)
            {
                return new BasicEnvironmentalConditions
                {
                    Temperature = plant.EnvironmentData.Temperature,
                    Humidity = plant.EnvironmentData.Humidity,
                    LightLevel = plant.EnvironmentData.LightIntensity
                };
            }

            // Default indoor conditions
            return new BasicEnvironmentalConditions
            {
                Temperature = 25f,
                Humidity = 50f,
                LightLevel = 500f
            };
        }

        private float CalculateHealthImpact(BasicEnvironmentalConditions conditions)
        {
            // Simple health calculation based on optimal ranges
            float tempScore = (conditions.Temperature >= 20f && conditions.Temperature <= 28f) ? 1f : 0.5f;
            float humidityScore = (conditions.Humidity >= 40f && conditions.Humidity <= 70f) ? 1f : 0.5f;
            float lightScore = (conditions.LightLevel >= 300f && conditions.LightLevel <= 800f) ? 1f : 0.5f;

            // Average the scores
            return (tempScore + humidityScore + lightScore) / 3f;
        }

        private void ApplyBasicStress(PlantInstance plant, float healthImpact)
        {
            if (plant == null) return;

            // Simple stress application
            float stressAmount = (1f - healthImpact) * 0.1f;
            plant.Health = Mathf.Max(0f, plant.Health - stressAmount);

            if (_enableBasicMonitoring)
            {
                ChimeraLogger.Log($"[PlantEnvironmentalProcessingService] Applied stress to {plant.PlantID}: {stressAmount:F2}");
            }
        }

        #endregion
    }

    /// <summary>
    /// Basic environmental conditions
    /// </summary>
    [System.Serializable]
    public struct BasicEnvironmentalConditions
    {
        public float Temperature;
        public float Humidity;
        public float LightLevel;
    }

    /// <summary>
    /// Environmental statistics
    /// </summary>
    [System.Serializable]
    public struct EnvironmentalStats
    {
        public int TotalPlants;
        public int HealthyPlants;
        public int StressedPlants;
        public bool IsMonitoringEnabled;
    }
}
