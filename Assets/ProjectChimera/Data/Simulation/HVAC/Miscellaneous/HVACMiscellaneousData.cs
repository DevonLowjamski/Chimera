using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Shared;

namespace ProjectChimera.Data.Simulation.HVAC.Miscellaneous
{
    /// <summary>
    /// BASIC: Core HVAC data management for Project Chimera's environmental systems.
    /// Focuses on essential environmental data without complex coordination systems.
    /// </summary>
    public class HVACMiscellaneousData : MonoBehaviour
    {
        [Header("Basic Environmental Data")]
        [SerializeField] private bool _enableBasicData = true;
        [SerializeField] private bool _enableLogging = true;

        // Basic environmental tracking
        private readonly Dictionary<string, EnvironmentalZone> _environmentalZones = new Dictionary<string, EnvironmentalZone>();
        private bool _isInitialized = false;

        /// <summary>
        /// Initialize basic HVAC data system
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            _isInitialized = true;

            if (_enableLogging)
            {
                ProjectChimera.Shared.SharedLogger.Log("OTHER", "$1", this);
            }
        }

        /// <summary>
        /// Add or update environmental zone data
        /// </summary>
        public void UpdateZoneData(string zoneId, float temperature, float humidity, float airQuality)
        {
            if (!_enableBasicData || !_isInitialized) return;

            if (!_environmentalZones.ContainsKey(zoneId))
            {
                _environmentalZones[zoneId] = new EnvironmentalZone();
            }

            var zone = _environmentalZones[zoneId];
            zone.ZoneId = zoneId;
            zone.Temperature = temperature;
            zone.Humidity = humidity;
            zone.AirQuality = airQuality;
            zone.LastUpdateTime = Time.time;

            if (_enableLogging)
            {
                ProjectChimera.Shared.SharedLogger.Log("OTHER", "$1", this);
            }
        }

        /// <summary>
        /// Get environmental zone data
        /// </summary>
        public EnvironmentalZone GetZoneData(string zoneId)
        {
            return _environmentalZones.TryGetValue(zoneId, out var zone) ? zone : null;
        }

        /// <summary>
        /// Get all environmental zones
        /// </summary>
        public Dictionary<string, EnvironmentalZone> GetAllZones()
        {
            return new Dictionary<string, EnvironmentalZone>(_environmentalZones);
        }

        /// <summary>
        /// Check if zone conditions are optimal
        /// </summary>
        public bool IsOptimalConditions(string zoneId)
        {
            var zone = GetZoneData(zoneId);
            if (zone == null) return false;

            // Basic optimal conditions check
            bool tempOptimal = zone.Temperature >= 20f && zone.Temperature <= 28f;
            bool humidityOptimal = zone.Humidity >= 40f && zone.Humidity <= 70f;
            bool airQualityOptimal = zone.AirQuality >= 0.7f;

            return tempOptimal && humidityOptimal && airQualityOptimal;
        }

        /// <summary>
        /// Get environmental recommendations for a zone
        /// </summary>
        public EnvironmentalRecommendation GetRecommendation(string zoneId)
        {
            var zone = GetZoneData(zoneId);
            if (zone == null) return null;

            var recommendation = new EnvironmentalRecommendation();
            recommendation.ZoneId = zoneId;

            // Temperature recommendations
            if (zone.Temperature < 20f)
            {
                recommendation.TemperatureAction = "Increase temperature";
            }
            else if (zone.Temperature > 28f)
            {
                recommendation.TemperatureAction = "Decrease temperature";
            }
            else
            {
                recommendation.TemperatureAction = "Temperature optimal";
            }

            // Humidity recommendations
            if (zone.Humidity < 40f)
            {
                recommendation.HumidityAction = "Increase humidity";
            }
            else if (zone.Humidity > 70f)
            {
                recommendation.HumidityAction = "Decrease humidity";
            }
            else
            {
                recommendation.HumidityAction = "Humidity optimal";
            }

            // Air quality recommendations
            if (zone.AirQuality < 0.7f)
            {
                recommendation.AirQualityAction = "Improve air quality";
            }
            else
            {
                recommendation.AirQualityAction = "Air quality good";
            }

            return recommendation;
        }

        /// <summary>
        /// Clear all zone data
        /// </summary>
        public void ClearAllData()
        {
            _environmentalZones.Clear();

            if (_enableLogging)
            {
                ProjectChimera.Shared.SharedLogger.Log("OTHER", "$1", this);
            }
        }

        /// <summary>
        /// Get environmental statistics
        /// </summary>
        public EnvironmentalStatistics GetStatistics()
        {
            int totalZones = _environmentalZones.Count;
            int zonesNeedingAttention = _environmentalZones.Count(kvp => !IsOptimalConditions(kvp.Key));
            int staleZones = _environmentalZones.Count(kvp =>
                Time.time - kvp.Value.LastUpdateTime > 300f); // 5 minutes

            return new EnvironmentalStatistics
            {
                TotalZones = totalZones,
                ZonesNeedingAttention = zonesNeedingAttention,
                StaleZones = staleZones,
                IsSystemEnabled = _enableBasicData
            };
        }

        /// <summary>
        /// Remove zone data
        /// </summary>
        public bool RemoveZone(string zoneId)
        {
            if (_environmentalZones.Remove(zoneId))
            {
                if (_enableLogging)
                {
                    ProjectChimera.Shared.SharedLogger.Log("OTHER", "$1", this);
                }
                return true;
            }
            return false;
        }
    }

    /// <summary>
    /// Basic environmental zone data
    /// </summary>
    [System.Serializable]
    public class EnvironmentalZone
    {
        public string ZoneId;
        public float Temperature;
        public float Humidity;
        public float AirQuality;
        public float LastUpdateTime;
    }

    /// <summary>
    /// Environmental recommendation
    /// </summary>
    [System.Serializable]
    public class EnvironmentalRecommendation
    {
        public string ZoneId;
        public string TemperatureAction;
        public string HumidityAction;
        public string AirQualityAction;
        public int Priority = 1; // 0 = optimal, 1 = needs attention, 2 = critical
    }

    /// <summary>
    /// Environmental statistics
    /// </summary>
    [System.Serializable]
    public class EnvironmentalStatistics
    {
        public int TotalZones;
        public int ZonesNeedingAttention;
        public int StaleZones;
        public bool IsSystemEnabled;
    }
}
