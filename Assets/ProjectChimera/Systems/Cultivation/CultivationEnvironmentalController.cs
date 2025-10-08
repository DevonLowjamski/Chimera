using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Data.Shared;
using ProjectChimera.Data.Cultivation;
using ProjectChimera.Data.Cultivation.Plant;
using ProjectChimera.Core.Events;
using EnvironmentalConditions = ProjectChimera.Data.Shared.EnvironmentalConditions;
using PlantGrowthStage = ProjectChimera.Data.Shared.PlantGrowthStage;
// using ProjectChimera.Systems.Environment; // Environment assembly not available

namespace ProjectChimera.Systems.Cultivation
{
    /// <summary>
    /// PC013-5b: Specialized service for cultivation environmental control
    /// Extracted from monolithic CultivationManager.cs to handle environmental
    /// effects on plants, automatic environmental adjustments, and environmental optimization.
    /// </summary>
    public class CultivationEnvironmentalController : MonoBehaviour, ITickable, ICultivationService
    {
        [Header("Environmental Control Configuration")]
        [SerializeField] private bool _enableAutoEnvironmentalControl = true;
        [SerializeField] private float _environmentalUpdateInterval = 300f; // 5 minutes
        [SerializeField] private float _environmentalSensitivity = 1f;

        [Header("Environmental Thresholds")]
        [SerializeField] private float _optimalTemperatureMin = 20f;
        [SerializeField] private float _optimalTemperatureMax = 26f;
        [SerializeField] private float _optimalHumidityMin = 40f;
        [SerializeField] private float _optimalHumidityMax = 60f;
        [SerializeField] private float _optimalCO2Min = 800f;
        [SerializeField] private float _optimalCO2Max = 1200f;

        // Dependencies
        // private EnvironmentManager _environmentManager; // EnvironmentManager not available
        private CultivationZoneManager _zoneManager;

        // Environmental tracking
        private Dictionary<string, EnvironmentalHistory> _environmentalHistory = new Dictionary<string, EnvironmentalHistory>();
        private Dictionary<string, EnvironmentalAlert> _activeAlerts = new Dictionary<string, EnvironmentalAlert>();
        private float _lastEnvironmentalUpdate;

        public bool IsInitialized { get; private set; }

        // Properties
        public bool AutoEnvironmentalControl
        {
            get => _enableAutoEnvironmentalControl;
            set => _enableAutoEnvironmentalControl = value;
        }
        public int ActiveAlertCount => _activeAlerts.Count;
        public float EnvironmentalSensitivity
        {
            get => _environmentalSensitivity;
            set => _environmentalSensitivity = Mathf.Clamp(value, 0.1f, 3f);
        }

        // Events
        public System.Action<string, EnvironmentalAlert> OnEnvironmentalAlert;
        public System.Action<string, EnvironmentalConditions> OnEnvironmentalAdjustment;
        public System.Action<string> OnEnvironmentalOptimization;

        public void Initialize()
        {
            if (IsInitialized) return;

            ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", this);

            // Get dependencies
            // _environmentManager = GameManager.Instance?.GetManager<EnvironmentManager>(); // EnvironmentManager not available
            _zoneManager = ServiceContainerFactory.Instance.TryResolve<CultivationZoneManager>();

            // if (_environmentManager == null) // EnvironmentManager not available
            {
                // ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", this); // EnvironmentManager not available
            }

            if (_zoneManager == null)
            {
                ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", this);
            }

            _lastEnvironmentalUpdate = Time.time;

            IsInitialized = true;
            ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", this);
        }

        public void Shutdown()
        {
            ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", this);

            _environmentalHistory.Clear();
            _activeAlerts.Clear();

            IsInitialized = false;
        }

            public void Tick(float deltaTime)
    {
            if (!IsInitialized || !_enableAutoEnvironmentalControl) return;

            if (Time.time - _lastEnvironmentalUpdate >= _environmentalUpdateInterval)
            {
                UpdateEnvironmentalControl();
                _lastEnvironmentalUpdate = Time.time;

    }
        }

        /// <summary>
        /// Processes environmental effects on plants in a specific zone
        /// </summary>
        public void ProcessEnvironmentalEffects(string zoneId, List<PlantInstanceSO> plants)
        {
            if (!IsInitialized || plants == null || plants.Count == 0) return;

            EnvironmentalConditions environment = GetZoneEnvironment(zoneId);

            foreach (var plant in plants)
            {
                if (plant == null) continue;

                ApplyEnvironmentalEffectsToPlant(plant, environment);
            }

            // Record environmental data
            RecordEnvironmentalData(zoneId, environment);

            // Check for environmental issues
            CheckEnvironmentalAlerts(zoneId, environment);
        }

        /// <summary>
        /// Applies environmental effects to a single plant
        /// </summary>
        private void ApplyEnvironmentalEffectsToPlant(PlantInstanceSO plant, EnvironmentalConditions environment)
        {
            // Temperature effects
            float temperatureStress = CalculateTemperatureStress(environment.Temperature);

            // Humidity effects
            float humidityStress = CalculateHumidityStress(environment.Humidity);

            // Light effects
            float lightStress = CalculateLightStress(environment.DailyLightIntegral);

            // CO2 effects
            float co2Stress = CalculateCO2Stress(environment.CO2Level);

            // Combined environmental stress
            float totalStress = (temperatureStress + humidityStress + lightStress + co2Stress) / 4f;
            totalStress *= _environmentalSensitivity;

            // Apply stress to plant (this would be handled by PlantManager in practice)
            ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", this);
        }

        /// <summary>
        /// Automatically adjusts environmental conditions for optimal plant growth
        /// </summary>
        public void OptimizeEnvironmentalConditions(string zoneId)
        {
            if (!IsInitialized) return;

            EnvironmentalConditions currentEnvironment = GetZoneEnvironment(zoneId);
            EnvironmentalConditions optimizedEnvironment = currentEnvironment;

            // Optimize temperature
            if (currentEnvironment.Temperature < _optimalTemperatureMin)
            {
                optimizedEnvironment.Temperature = _optimalTemperatureMin + 1f;
            }
            else if (currentEnvironment.Temperature > _optimalTemperatureMax)
            {
                optimizedEnvironment.Temperature = _optimalTemperatureMax - 1f;
            }

            // Optimize humidity
            if (currentEnvironment.Humidity < _optimalHumidityMin)
            {
                optimizedEnvironment.Humidity = _optimalHumidityMin + 5f;
            }
            else if (currentEnvironment.Humidity > _optimalHumidityMax)
            {
                optimizedEnvironment.Humidity = _optimalHumidityMax - 5f;
            }

            // Optimize CO2
            if (currentEnvironment.CO2Level < _optimalCO2Min)
            {
                optimizedEnvironment.CO2Level = _optimalCO2Min + 50f;
            }
            else if (currentEnvironment.CO2Level > _optimalCO2Max)
            {
                optimizedEnvironment.CO2Level = _optimalCO2Max - 50f;
            }

            // Apply optimized environment
            if (_zoneManager != null)
            {
                _zoneManager.SetZoneEnvironment(zoneId, optimizedEnvironment);
                OnEnvironmentalAdjustment?.Invoke(zoneId, optimizedEnvironment);
            }

            OnEnvironmentalOptimization?.Invoke(zoneId);
            ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", this);
        }

        /// <summary>
        /// Updates environmental control for all zones
        /// </summary>
        private void UpdateEnvironmentalControl()
        {
            if (_zoneManager == null) return;

            foreach (string zoneId in _zoneManager.ZoneIds)
            {
                var environment = GetZoneEnvironment(zoneId);
                CheckEnvironmentalAlerts(zoneId, environment);

                // Auto-optimize if needed
                if (ShouldAutoOptimize(zoneId, environment))
                {
                    OptimizeEnvironmentalConditions(zoneId);
                }
            }
        }

        /// <summary>
        /// Determines if a zone needs automatic optimization
        /// </summary>
        private bool ShouldAutoOptimize(string zoneId, EnvironmentalConditions environment)
        {
            float temperatureStress = CalculateTemperatureStress(environment.Temperature);
            float humidityStress = CalculateHumidityStress(environment.Humidity);
            float co2Stress = CalculateCO2Stress(environment.CO2Level);

            float totalStress = (temperatureStress + humidityStress + co2Stress) / 3f;

            return totalStress > 0.3f; // Optimize if stress is above threshold
        }

        /// <summary>
        /// Checks for environmental alerts and warnings
        /// </summary>
        private void CheckEnvironmentalAlerts(string zoneId, EnvironmentalConditions environment)
        {
            List<string> issues = new List<string>();

            // Temperature alerts
            if (environment.Temperature < _optimalTemperatureMin - 5f || environment.Temperature > _optimalTemperatureMax + 5f)
            {
                issues.Add($"Critical temperature: {environment.Temperature:F1}°C");
            }

            // Humidity alerts
            if (environment.Humidity < _optimalHumidityMin - 10f || environment.Humidity > _optimalHumidityMax + 10f)
            {
                issues.Add($"Critical humidity: {environment.Humidity:F1}%");
            }

            // CO2 alerts
            if (environment.CO2Level < _optimalCO2Min - 200f || environment.CO2Level > _optimalCO2Max + 300f)
            {
                issues.Add($"Critical CO2: {environment.CO2Level:F0}ppm");
            }

            // Create or clear alerts
            string alertKey = $"zone_{zoneId}";

            if (issues.Count > 0)
            {
                var alert = new EnvironmentalAlert
                {
                    ZoneId = zoneId,
                    Issues = issues,
                    Severity = DetermineAlertSeverity(issues),
                    Timestamp = System.DateTime.Now
                };

                _activeAlerts[alertKey] = alert;
                OnEnvironmentalAlert?.Invoke(zoneId, alert);
            }
            else if (_activeAlerts.ContainsKey(alertKey))
            {
                _activeAlerts.Remove(alertKey);
            }
        }

        /// <summary>
        /// Records environmental data for historical tracking
        /// </summary>
        private void RecordEnvironmentalData(string zoneId, EnvironmentalConditions environment)
        {
            if (!_environmentalHistory.ContainsKey(zoneId))
            {
                _environmentalHistory[zoneId] = new EnvironmentalHistory();
            }

            var history = _environmentalHistory[zoneId];
            history.AddReading(environment);
        }

        /// <summary>
        /// Gets environmental conditions for a zone
        /// </summary>
        private EnvironmentalConditions GetZoneEnvironment(string zoneId)
        {
            if (_zoneManager != null)
            {
                return _zoneManager.GetZoneEnvironment(zoneId);
            }

            // Fallback to default environment
            return EnvironmentalConditions.CreateIndoorDefault();
        }

        #region Stress Calculation Methods

        private float CalculateTemperatureStress(float temperature)
        {
            if (temperature >= _optimalTemperatureMin && temperature <= _optimalTemperatureMax)
                return 0f;

            float distanceFromOptimal = Mathf.Min(
                Mathf.Abs(temperature - _optimalTemperatureMin),
                Mathf.Abs(temperature - _optimalTemperatureMax)
            );

            return Mathf.Clamp01(distanceFromOptimal / 10f); // Stress increases with distance from optimal
        }

        private float CalculateHumidityStress(float humidity)
        {
            if (humidity >= _optimalHumidityMin && humidity <= _optimalHumidityMax)
                return 0f;

            float distanceFromOptimal = Mathf.Min(
                Mathf.Abs(humidity - _optimalHumidityMin),
                Mathf.Abs(humidity - _optimalHumidityMax)
            );

            return Mathf.Clamp01(distanceFromOptimal / 20f);
        }

        private float CalculateLightStress(float dli)
        {
            float optimalDLI = 30f; // Optimal DLI for cannabis
            float distanceFromOptimal = Mathf.Abs(dli - optimalDLI);
            return Mathf.Clamp01(distanceFromOptimal / 20f);
        }

        private float CalculateCO2Stress(float co2Level)
        {
            if (co2Level >= _optimalCO2Min && co2Level <= _optimalCO2Max)
                return 0f;

            float distanceFromOptimal = Mathf.Min(
                Mathf.Abs(co2Level - _optimalCO2Min),
                Mathf.Abs(co2Level - _optimalCO2Max)
            );

            return Mathf.Clamp01(distanceFromOptimal / 400f);
        }

        #endregion

        #region Helper Methods

        private EnvironmentalAlertSeverity DetermineAlertSeverity(List<string> issues)
        {
            if (issues.Count >= 3) return EnvironmentalAlertSeverity.Critical;
            if (issues.Count >= 2) return EnvironmentalAlertSeverity.High;
            return EnvironmentalAlertSeverity.Medium;
        }

        #endregion

        /// <summary>
        /// Gets environmental history for a zone
        /// </summary>
        public EnvironmentalHistory GetEnvironmentalHistory(string zoneId)
        {
            return _environmentalHistory.TryGetValue(zoneId, out EnvironmentalHistory history)
                ? history
                : new EnvironmentalHistory();
        }

        /// <summary>
        /// Gets all active environmental alerts
        /// </summary>
        public IReadOnlyDictionary<string, EnvironmentalAlert> GetActiveAlerts()
        {
            return _activeAlerts;
        }

        /// <summary>
        /// Clears all environmental alerts for a zone
        /// </summary>
        public void ClearZoneAlerts(string zoneId)
        {
            string alertKey = $"zone_{zoneId}";
            _activeAlerts.Remove(alertKey);
        }

    // ITickable implementation
    public int TickPriority => ProjectChimera.Core.Updates.TickPriority.EnvironmentalManager;
    public bool IsTickable => enabled && gameObject.activeInHierarchy;

    public virtual void OnRegistered()
    {
        // Override in derived classes if needed
    }

    public virtual void OnUnregistered()
    {
        // Override in derived classes if needed
    }
}
}
