using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using ProjectChimera.Data.Shared;

namespace ProjectChimera.Systems.Cultivation
{
    /// <summary>
    /// BASIC: Simple plant environmental service for Project Chimera's cultivation system.
    /// Focuses on essential environmental monitoring without complex stress systems.
    /// </summary>
    public class PlantEnvironmentalService : MonoBehaviour
    {
        [Header("Basic Environmental Settings")]
        [SerializeField] private bool _enableBasicMonitoring = true;
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private float _optimalTemperature = 25f;
        [SerializeField] private float _optimalHumidity = 60f;
        [SerializeField] private float _optimalLight = 500f;

        // Basic environmental tracking
        private readonly Dictionary<string, EnvironmentalConditions> _plantEnvironments = new Dictionary<string, EnvironmentalConditions>();
        private bool _isInitialized = false;

        /// <summary>
        /// Events for environmental changes
        /// </summary>
        public event System.Action<string, EnvironmentalConditions> OnEnvironmentUpdated;

        /// <summary>
        /// Initialize basic environmental service
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("[PlantEnvironmentalService] Initialized successfully");
            }
        }

        /// <summary>
        /// Update plant environmental conditions
        /// </summary>
        public void UpdatePlantEnvironment(string plantId, EnvironmentalConditions conditions)
        {
            if (!_enableBasicMonitoring || !_isInitialized) return;

            _plantEnvironments[plantId] = conditions;
            OnEnvironmentUpdated?.Invoke(plantId, conditions);

            if (_enableLogging && Random.value < 0.01f) // Log occasionally
            {
                ChimeraLogger.Log($"[PlantEnvironmentalService] Updated {plantId}: T={conditions.Temperature:F1}, H={conditions.Humidity:F1}, L={conditions.LightIntensity:F0}");
            }
        }

        /// <summary>
        /// Get plant environmental conditions
        /// </summary>
        public EnvironmentalConditions GetPlantEnvironment(string plantId)
        {
            return _plantEnvironments.TryGetValue(plantId, out var conditions) ? conditions : GetDefaultConditions();
        }

        /// <summary>
        /// Check if plant environment is optimal
        /// </summary>
        public bool IsEnvironmentOptimal(string plantId)
        {
            var conditions = GetPlantEnvironment(plantId);
            return IsTemperatureOptimal(conditions.Temperature) &&
                   IsHumidityOptimal(conditions.Humidity) &&
                   IsLightOptimal(conditions.LightIntensity);
        }

        /// <summary>
        /// Get environmental recommendations for a plant
        /// </summary>
        public string GetEnvironmentalRecommendation(string plantId)
        {
            var conditions = GetPlantEnvironment(plantId);
            var issues = new List<string>();

            if (!IsTemperatureOptimal(conditions.Temperature))
            {
                issues.Add("temperature");
            }

            if (!IsHumidityOptimal(conditions.Humidity))
            {
                issues.Add("humidity");
            }

            if (!IsLightOptimal(conditions.LightIntensity))
            {
                issues.Add("light");
            }

            if (issues.Count == 0)
            {
                return "Environment optimal";
            }
            else
            {
                return $"Check {string.Join(", ", issues)} levels";
            }
        }

        /// <summary>
        /// Get optimal environmental ranges
        /// </summary>
        public EnvironmentalRanges GetOptimalRanges()
        {
            return new EnvironmentalRanges
            {
                MinTemperature = _optimalTemperature - 5f,
                MaxTemperature = _optimalTemperature + 5f,
                MinHumidity = _optimalHumidity - 10f,
                MaxHumidity = _optimalHumidity + 10f,
                MinLight = _optimalLight - 200f,
                MaxLight = _optimalLight + 200f
            };
        }

        /// <summary>
        /// Remove plant from environmental tracking
        /// </summary>
        public void RemovePlant(string plantId)
        {
            _plantEnvironments.Remove(plantId);

            if (_enableLogging)
            {
                ChimeraLogger.Log($"[PlantEnvironmentalService] Removed plant {plantId} from tracking");
            }
        }

        /// <summary>
        /// Clear all plant environmental data
        /// </summary>
        public void ClearAllData()
        {
            _plantEnvironments.Clear();

            if (_enableLogging)
            {
                ChimeraLogger.Log("[PlantEnvironmentalService] Cleared all environmental data");
            }
        }

        /// <summary>
        /// Get environmental service statistics
        /// </summary>
        public EnvironmentalServiceStats GetStats()
        {
            int totalPlants = _plantEnvironments.Count;
            int optimalEnvironments = 0;

            foreach (var kvp in _plantEnvironments)
            {
                if (IsEnvironmentOptimal(kvp.Key))
                {
                    optimalEnvironments++;
                }
            }

            return new EnvironmentalServiceStats
            {
                TotalPlantsTracked = totalPlants,
                OptimalEnvironments = optimalEnvironments,
                SuboptimalEnvironments = totalPlants - optimalEnvironments,
                IsMonitoringEnabled = _enableBasicMonitoring
            };
        }

        #region Private Methods

        private EnvironmentalConditions GetDefaultConditions()
        {
            return new EnvironmentalConditions
            {
                Temperature = _optimalTemperature,
                Humidity = _optimalHumidity,
                LightIntensity = _optimalLight
            };
        }

        private bool IsTemperatureOptimal(float temperature)
        {
            float minTemp = _optimalTemperature - 5f;
            float maxTemp = _optimalTemperature + 5f;
            return temperature >= minTemp && temperature <= maxTemp;
        }

        private bool IsHumidityOptimal(float humidity)
        {
            float minHumidity = _optimalHumidity - 10f;
            float maxHumidity = _optimalHumidity + 10f;
            return humidity >= minHumidity && humidity <= maxHumidity;
        }

        private bool IsLightOptimal(float light)
        {
            float minLight = _optimalLight - 200f;
            float maxLight = _optimalLight + 200f;
            return light >= minLight && light <= maxLight;
        }

        #endregion
    }

    /// <summary>
    /// Environmental ranges
    /// </summary>
    [System.Serializable]
    public struct EnvironmentalRanges
    {
        public float MinTemperature;
        public float MaxTemperature;
        public float MinHumidity;
        public float MaxHumidity;
        public float MinLight;
        public float MaxLight;
    }

    /// <summary>
    /// Environmental service statistics
    /// </summary>
    [System.Serializable]
    public struct EnvironmentalServiceStats
    {
        public int TotalPlantsTracked;
        public int OptimalEnvironments;
        public int SuboptimalEnvironments;
        public bool IsMonitoringEnabled;
    }
}
