using UnityEngine;
using ProjectChimera.Core.Logging;
using System;
using ProjectChimera.Data.Shared;

namespace ProjectChimera.Systems.Environment
{
    /// <summary>
    /// Simple Environmental Controller for Project Chimera
    /// Provides direct player control over environmental conditions through menus
    /// as described in the gameplay document. No complex automation or zones.
    /// </summary>
    public class EnvironmentalController : MonoBehaviour
    {
        [Header("Current Environmental Settings")]
        [SerializeField] private float _temperature = 25f; // °C
        [SerializeField] private float _humidity = 60f; // %
        [SerializeField] private float _co2Level = 800f; // ppm
        [SerializeField] private float _lightIntensity = 0.8f; // 0-1 scale

        [Header("Environmental Ranges")]
        [SerializeField] private Vector2 _temperatureRange = new Vector2(15f, 35f);
        [SerializeField] private Vector2 _humidityRange = new Vector2(30f, 80f);
        [SerializeField] private Vector2 _co2Range = new Vector2(300f, 1500f);
        [SerializeField] private Vector2 _lightRange = new Vector2(0f, 1f);

        // Current environmental conditions
        private EnvironmentalConditions _currentConditions;

        private void Awake()
        {
            InitializeEnvironmentalConditions();
        }

        private void InitializeEnvironmentalConditions()
        {
            _currentConditions = new EnvironmentalConditions
            {
                Temperature = _temperature,
                Humidity = _humidity,
                CO2Level = _co2Level,
                LightIntensity = _lightIntensity,
                Timestamp = DateTime.Now
            };

            ChimeraLogger.Log("ENV", "EnvironmentalController initialized", this);
        }

        /// <summary>
        /// Set temperature (called from player menu controls)
        /// </summary>
        public void SetTemperature(float temperature)
        {
            _temperature = Mathf.Clamp(temperature, _temperatureRange.x, _temperatureRange.y);
            _currentConditions.Temperature = _temperature;
            _currentConditions.Timestamp = DateTime.Now;

            ChimeraLogger.Log("ENV", $"Temperature set to {_temperature:F1}", this);
        }

        /// <summary>
        /// Set humidity (called from player menu controls)
        /// </summary>
        public void SetHumidity(float humidity)
        {
            _humidity = Mathf.Clamp(humidity, _humidityRange.x, _humidityRange.y);
            _currentConditions.Humidity = _humidity;
            _currentConditions.Timestamp = DateTime.Now;

            ChimeraLogger.Log("ENV", $"Humidity set to {_humidity:F1}", this);
        }

        /// <summary>
        /// Set CO2 level (called from player menu controls)
        /// </summary>
        public void SetCO2Level(float co2Level)
        {
            _co2Level = Mathf.Clamp(co2Level, _co2Range.x, _co2Range.y);
            _currentConditions.CO2Level = _co2Level;
            _currentConditions.Timestamp = DateTime.Now;

            ChimeraLogger.Log("ENV", $"CO2 set to {_co2Level:F0}ppm", this);
        }

        /// <summary>
        /// Set light intensity (called from player menu controls)
        /// </summary>
        public void SetLightIntensity(float intensity)
        {
            _lightIntensity = Mathf.Clamp(intensity, _lightRange.x, _lightRange.y);
            _currentConditions.LightIntensity = _lightIntensity;
            _currentConditions.Timestamp = DateTime.Now;

            ChimeraLogger.Log("ENV", $"Light intensity set to {_lightIntensity:F2}", this);
        }

        /// <summary>
        /// Get current environmental conditions for plant growth calculations
        /// </summary>
        public EnvironmentalConditions GetCurrentConditions()
        {
            return _currentConditions;
        }

        /// <summary>
        /// Reset to default optimal conditions
        /// </summary>
        public void ResetToOptimal()
        {
            _temperature = 25f;
            _humidity = 60f;
            _co2Level = 800f;
            _lightIntensity = 0.8f;

            InitializeEnvironmentalConditions();

            ChimeraLogger.Log("ENV", "Environmental settings reset to optimal", this);
        }

        /// <summary>
        /// Get environmental recommendations for optimal growth
        /// </summary>
        public string GetEnvironmentalRecommendations()
        {
            var recommendations = "Environmental Controls:\n";

            if (_temperature < 20f || _temperature > 30f)
                recommendations += "- Adjust temperature to 20-30°C range\n";

            if (_humidity < 40f || _humidity > 70f)
                recommendations += "- Adjust humidity to 40-70% range\n";

            if (_co2Level < 600f || _co2Level > 1200f)
                recommendations += "- Adjust CO2 to 600-1200ppm range\n";

            if (_lightIntensity < 0.5f)
                recommendations += "- Increase light intensity for better growth\n";

            return recommendations;
        }
    }
}
