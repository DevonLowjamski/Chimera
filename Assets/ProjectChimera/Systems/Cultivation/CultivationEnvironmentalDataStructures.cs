// REFACTORED: Cultivation Environmental Data Structures
// Extracted from CultivationEnvironmentalController for better separation of concerns

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProjectChimera.Data.Shared;

namespace ProjectChimera.Systems.Cultivation
{
    /// <summary>
    /// Environmental alert data
    /// </summary>
    public struct EnvironmentalAlert
    {
        public string ZoneId;
        public List<string> Issues;
        public EnvironmentalAlertSeverity Severity;
        public DateTime Timestamp;
    }

    /// <summary>
    /// Environmental alert severity levels
    /// </summary>
    public enum EnvironmentalAlertSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// Historical environmental data tracking
    /// </summary>
    [Serializable]
    public class EnvironmentalHistory
    {
        [SerializeField] private List<EnvironmentalReading> _readings = new List<EnvironmentalReading>();
        private const int MAX_READINGS = 1000;

        public IReadOnlyList<EnvironmentalReading> Readings => _readings.AsReadOnly();

        public void AddReading(EnvironmentalConditions environment)
        {
            var reading = new EnvironmentalReading
            {
                Temperature = environment.Temperature,
                Humidity = environment.Humidity,
                CO2Level = environment.CO2Level,
                DLI = environment.DailyLightIntegral,
                Timestamp = DateTime.Now
            };

            _readings.Add(reading);

            // Maintain maximum readings limit
            if (_readings.Count > MAX_READINGS)
            {
                _readings.RemoveAt(0);
            }
        }

        public float GetAverageTemperature(int lastHours = 24)
        {
            var cutoff = DateTime.Now.AddHours(-lastHours);
            var recentReadings = _readings.Where(r => r.Timestamp >= cutoff);
            return recentReadings.Any() ? recentReadings.Average(r => r.Temperature) : 0f;
        }

        public float GetAverageHumidity(int lastHours = 24)
        {
            var cutoff = DateTime.Now.AddHours(-lastHours);
            var recentReadings = _readings.Where(r => r.Timestamp >= cutoff);
            return recentReadings.Any() ? recentReadings.Average(r => r.Humidity) : 0f;
        }
    }

    /// <summary>
    /// Individual environmental reading
    /// </summary>
    [Serializable]
    public struct EnvironmentalReading
    {
        public float Temperature;
        public float Humidity;
        public float CO2Level;
        public float DLI;
        public DateTime Timestamp;
    }
}

