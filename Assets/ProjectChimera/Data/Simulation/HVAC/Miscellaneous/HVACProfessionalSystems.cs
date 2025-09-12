using UnityEngine;
using System.Collections.Generic;
using System.Linq;

namespace ProjectChimera.Data.Simulation.HVAC.Miscellaneous
{
    /// <summary>
    /// SIMPLE: Basic environmental monitoring aligned with Project Chimera's cultivation vision.
    /// Focuses on essential environmental tracking without complex professional systems.
    /// </summary>
    public class HVACProfessionalSystems : MonoBehaviour
    {
        [Header("Basic Environmental Settings")]
        [SerializeField] private bool _enableBasicMonitoring = true;
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private float _monitoringInterval = 5f;

        // Basic environmental tracking
        private readonly Dictionary<string, EnvironmentalData> _environmentalData = new Dictionary<string, EnvironmentalData>();
        private float _lastMonitoringTime = 0f;
        private bool _isInitialized = false;

        /// <summary>
        /// Initialize basic environmental monitoring
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            _isInitialized = true;

            if (_enableLogging)
            {
                Debug.Log("[HVACProfessionalSystems] Initialized successfully");
            }
        }

        /// <summary>
        /// Update environmental monitoring
        /// </summary>
        public void UpdateMonitoring()
        {
            if (!_enableBasicMonitoring) return;

            if (Time.time - _lastMonitoringTime >= _monitoringInterval)
            {
                _lastMonitoringTime = Time.time;
                MonitorEnvironment();
            }
        }

        /// <summary>
        /// Add environmental data point
        /// </summary>
        public void AddEnvironmentalData(string zoneId, float temperature, float humidity, float airQuality)
        {
            if (!_environmentalData.ContainsKey(zoneId))
            {
                _environmentalData[zoneId] = new EnvironmentalData();
            }

            var data = _environmentalData[zoneId];
            data.Temperature = temperature;
            data.Humidity = humidity;
            data.AirQuality = airQuality;
            data.LastUpdateTime = Time.time;

            if (_enableLogging)
            {
                Debug.Log($"[HVACProfessionalSystems] Updated zone {zoneId}: T={temperature:F1}°C, H={humidity:F1}%, AQ={airQuality:F1}");
            }
        }

        /// <summary>
        /// Get environmental data for a zone
        /// </summary>
        public EnvironmentalData GetEnvironmentalData(string zoneId)
        {
            return _environmentalData.TryGetValue(zoneId, out var data) ? data : null;
        }

        /// <summary>
        /// Get all zone data
        /// </summary>
        public Dictionary<string, EnvironmentalData> GetAllEnvironmentalData()
        {
            return new Dictionary<string, EnvironmentalData>(_environmentalData);
        }

        /// <summary>
        /// Check if environmental conditions are optimal for a zone
        /// </summary>
        public bool IsOptimalConditions(string zoneId)
        {
            var data = GetEnvironmentalData(zoneId);
            if (data == null) return false;

            // Basic optimal conditions check
            bool tempOptimal = data.Temperature >= 20f && data.Temperature <= 28f;
            bool humidityOptimal = data.Humidity >= 40f && data.Humidity <= 70f;
            bool airQualityOptimal = data.AirQuality >= 0.8f;

            return tempOptimal && humidityOptimal && airQualityOptimal;
        }

        /// <summary>
        /// Get environmental recommendations for a zone
        /// </summary>
        public EnvironmentalRecommendation GetRecommendation(string zoneId)
        {
            var data = GetEnvironmentalData(zoneId);
            if (data == null) return null;

            var recommendation = new EnvironmentalRecommendation();
            recommendation.ZoneId = zoneId;

            // Temperature recommendations
            if (data.Temperature < 20f)
            {
                recommendation.TemperatureAction = "Increase temperature";
            }
            else if (data.Temperature > 28f)
            {
                recommendation.TemperatureAction = "Decrease temperature";
            }
            else
            {
                recommendation.TemperatureAction = "Temperature optimal";
            }

            // Humidity recommendations
            if (data.Humidity < 40f)
            {
                recommendation.HumidityAction = "Increase humidity";
            }
            else if (data.Humidity > 70f)
            {
                recommendation.HumidityAction = "Decrease humidity";
            }
            else
            {
                recommendation.HumidityAction = "Humidity optimal";
            }

            // Air quality recommendations
            if (data.AirQuality < 0.8f)
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
        /// Clear all environmental data
        /// </summary>
        public void ClearAllData()
        {
            _environmentalData.Clear();

            if (_enableLogging)
            {
                Debug.Log("[HVACProfessionalSystems] Cleared all environmental data");
            }
        }

        /// <summary>
        /// Get monitoring statistics
        /// </summary>
        public MonitoringStatistics GetMonitoringStatistics()
        {
            int zonesNeedingAttention = _environmentalData.Count(kvp => !IsOptimalConditions(kvp.Key));

            return new MonitoringStatistics
            {
                TotalZones = _environmentalData.Count,
                ZonesNeedingAttention = zonesNeedingAttention,
                IsInitialized = _isInitialized,
                MonitoringEnabled = _enableBasicMonitoring,
                MonitoringInterval = _monitoringInterval
            };
        }

        #region Private Methods

        private void MonitorEnvironment()
        {
            // Basic monitoring - check all zones
            foreach (var zoneId in _environmentalData.Keys.ToList())
            {
                var data = _environmentalData[zoneId];
                if (Time.time - data.LastUpdateTime > _monitoringInterval * 2)
                {
                    if (_enableLogging)
                    {
                        Debug.LogWarning($"[HVACProfessionalSystems] Zone {zoneId} has stale data");
                    }
                }
            }
        }

        #endregion
    }

    /// <summary>
    /// Basic environmental data
    /// </summary>
    [System.Serializable]
    public class EnvironmentalData
    {
        public float Temperature;
        public float Humidity;
        public float AirQuality;
        public float LastUpdateTime;
    }

    // Note: EnvironmentalRecommendation class is defined in HVACMiscellaneousData.cs

    /// <summary>
    /// Monitoring statistics
    /// </summary>
    [System.Serializable]
    public class MonitoringStatistics
    {
        public int TotalZones;
        public int ZonesNeedingAttention;
        public bool IsInitialized;
        public bool MonitoringEnabled;
        public float MonitoringInterval;
    }
}
