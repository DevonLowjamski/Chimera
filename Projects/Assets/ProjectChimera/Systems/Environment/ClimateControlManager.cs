using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Data.Shared;


namespace ProjectChimera.Systems.Environment
{
    /// <summary>
    /// PC013-6a: Specialized service for climate control systems
    /// Extracted from monolithic EnvironmentalManager.cs to handle climate regulation,
    /// temperature control, humidity management, and atmospheric optimization.
    /// </summary>
    public class ClimateControlManager : ChimeraManager, IEnvironmentalService
    {
        [Header("Climate Control Configuration")]
        [SerializeField] private float _climateUpdateInterval = 5f; // 5 seconds
        [SerializeField] private bool _enableAutomaticClimateControl = true;
        [SerializeField] private float _climateSensitivity = 1f;
        
        [Header("Temperature Control")]
        [SerializeField] private float _targetTemperatureMin = 22f;
        [SerializeField] private float _targetTemperatureMax = 26f;
        [SerializeField] private float _temperatureToleranceRange = 2f;
        
        [Header("Humidity Control")]
        [SerializeField] private float _targetHumidityMin = 50f;
        [SerializeField] private float _targetHumidityMax = 60f;
        [SerializeField] private float _humidityToleranceRange = 5f;
        
        [Header("Atmospheric Control")]
        [SerializeField] private float _targetCO2Level = 1000f;
        [SerializeField] private float _co2ToleranceRange = 200f;
        [SerializeField] private float _airflowRate = 1f;
        
        // Climate tracking data
        private Dictionary<string, ClimateZoneData> _climateZones = new Dictionary<string, ClimateZoneData>();
        private Dictionary<string, ClimateControlHistory> _climateHistory = new Dictionary<string, ClimateControlHistory>();
        private Dictionary<string, ClimateAlert> _activeClimateAlerts = new Dictionary<string, ClimateAlert>();
        
        // Timing
        private float _lastClimateUpdate;
        
        public bool IsInitialized { get; private set; }
        
        // Properties
        public bool AutomaticClimateControl 
        { 
            get => _enableAutomaticClimateControl; 
            set => _enableAutomaticClimateControl = value; 
        }
        public int ActiveClimateZones => _climateZones.Count;
        public int ActiveAlerts => _activeClimateAlerts.Count;
        public float ClimateSensitivity 
        { 
            get => _climateSensitivity; 
            set => _climateSensitivity = Mathf.Clamp(value, 0.1f, 3f); 
        }
        
        // Events
        public System.Action<string, float> OnTemperatureChanged;
        public System.Action<string, float> OnHumidityChanged;
        public System.Action<string, float> OnCO2LevelChanged;
        public System.Action<string, float> OnAirflowChanged;
        public System.Action<string, ClimateAlert> OnClimateAlert;
        public System.Action<string, EnvironmentalConditions> OnClimateOptimized;
        
        public void Initialize()
        {
            if (IsInitialized) return;
            
            Debug.Log("[ClimateControlManager] Initializing climate control system...");
            
            _lastClimateUpdate = Time.time;
            
            IsInitialized = true;
            Debug.Log($"[ClimateControlManager] Initialized. Auto-control: {_enableAutomaticClimateControl}");
        }
        
        public void Shutdown()
        {
            Debug.Log("[ClimateControlManager] Shutting down climate control...");
            
            _climateZones.Clear();
            _climateHistory.Clear();
            _activeClimateAlerts.Clear();
            
            IsInitialized = false;
        }
        
        private void Update()
        {
            if (!IsInitialized || !_enableAutomaticClimateControl) return;
            
            if (Time.time - _lastClimateUpdate >= _climateUpdateInterval)
            {
                UpdateClimateControl();
                _lastClimateUpdate = Time.time;
            }
        }
        
        /// <summary>
        /// Creates a new climate control zone
        /// </summary>
        public bool CreateClimateZone(string zoneId, EnvironmentalConditions targetConditions)
        {
            if (string.IsNullOrEmpty(zoneId))
            {
                Debug.LogWarning("[ClimateControlManager] Cannot create zone with null or empty ID");
                return false;
            }
            
            if (_climateZones.ContainsKey(zoneId))
            {
                Debug.LogWarning($"[ClimateControlManager] Climate zone '{zoneId}' already exists");
                return false;
            }
            
            var zoneData = new ClimateZoneData
            {
                ZoneId = zoneId,
                TargetConditions = targetConditions,
                CurrentConditions = EnvironmentalConditions.CreateIndoorDefault(),
                IsActive = true,
                CreatedAt = System.DateTime.Now
            };
            
            _climateZones[zoneId] = zoneData;
            _climateHistory[zoneId] = new ClimateControlHistory();
            
            Debug.Log($"[ClimateControlManager] Created climate zone '{zoneId}'");
            return true;
        }
        
        /// <summary>
        /// Updates climate conditions for a specific zone
        /// </summary>
        public void SetZoneClimate(string zoneId, EnvironmentalConditions conditions)
        {
            if (!_climateZones.TryGetValue(zoneId, out ClimateZoneData zoneData))
            {
                Debug.LogWarning($"[ClimateControlManager] Climate zone '{zoneId}' not found");
                return;
            }
            
            var oldConditions = zoneData.CurrentConditions;
            zoneData.CurrentConditions = conditions;
            _climateZones[zoneId] = zoneData;
            
            // Record climate change
            RecordClimateChange(zoneId, oldConditions, conditions);
            
            // Check for alerts
            CheckClimateAlerts(zoneId, conditions);
            
            // Fire events for specific parameter changes
            if (Mathf.Abs(oldConditions.Temperature - conditions.Temperature) > 0.5f)
            {
                OnTemperatureChanged?.Invoke(zoneId, conditions.Temperature);
            }
            
            if (Mathf.Abs(oldConditions.Humidity - conditions.Humidity) > 2f)
            {
                OnHumidityChanged?.Invoke(zoneId, conditions.Humidity);
            }
            
            if (Mathf.Abs(oldConditions.CO2Level - conditions.CO2Level) > 50f)
            {
                OnCO2LevelChanged?.Invoke(zoneId, conditions.CO2Level);
            }
            
            Debug.Log($"[ClimateControlManager] Updated climate for zone '{zoneId}'");
        }
        
        /// <summary>
        /// Gets current climate conditions for a zone
        /// </summary>
        public EnvironmentalConditions GetZoneClimate(string zoneId)
        {
            if (_climateZones.TryGetValue(zoneId, out ClimateZoneData zoneData))
            {
                return zoneData.CurrentConditions;
            }
            
            Debug.LogWarning($"[ClimateControlManager] Climate zone '{zoneId}' not found, returning default");
            return EnvironmentalConditions.CreateIndoorDefault();
        }
        
        /// <summary>
        /// Optimizes climate conditions for plant growth
        /// </summary>
        public void OptimizeClimateForGrowth(string zoneId, PlantGrowthStage growthStage)
        {
            if (!_climateZones.TryGetValue(zoneId, out ClimateZoneData zoneData))
            {
                Debug.LogWarning($"[ClimateControlManager] Cannot optimize - zone '{zoneId}' not found");
                return;
            }
            
            var optimizedConditions = CalculateOptimalConditions(growthStage);
            var currentConditions = zoneData.CurrentConditions;
            
            // Gradually adjust conditions towards optimal
            var adjustedConditions = new EnvironmentalConditions
            {
                Temperature = Mathf.Lerp(currentConditions.Temperature, optimizedConditions.Temperature, 0.1f),
                Humidity = Mathf.Lerp(currentConditions.Humidity, optimizedConditions.Humidity, 0.1f),
                CO2Level = Mathf.Lerp(currentConditions.CO2Level, optimizedConditions.CO2Level, 0.1f),
                DailyLightIntegral = optimizedConditions.DailyLightIntegral,
                AirflowRate = Mathf.Lerp(currentConditions.AirflowRate, optimizedConditions.AirflowRate, 0.1f)
            };
            
            SetZoneClimate(zoneId, adjustedConditions);
            OnClimateOptimized?.Invoke(zoneId, adjustedConditions);
            
            Debug.Log($"[ClimateControlManager] Optimized climate for '{zoneId}' (Stage: {growthStage})");
        }
        
        /// <summary>
        /// Processes automatic climate control for all zones
        /// </summary>
        private void UpdateClimateControl()
        {
            foreach (var kvp in _climateZones)
            {
                string zoneId = kvp.Key;
                ClimateZoneData zoneData = kvp.Value;
                
                if (!zoneData.IsActive) continue;
                
                ProcessZoneClimateControl(zoneId, zoneData);
            }
        }
        
        /// <summary>
        /// Processes climate control for a specific zone
        /// </summary>
        private void ProcessZoneClimateControl(string zoneId, ClimateZoneData zoneData)
        {
            var current = zoneData.CurrentConditions;
            var target = zoneData.TargetConditions;
            var adjusted = current;
            
            // Temperature control
            if (current.Temperature < target.Temperature - _temperatureToleranceRange)
            {
                adjusted.Temperature = Mathf.Min(current.Temperature + (1f * _climateSensitivity), target.Temperature);
            }
            else if (current.Temperature > target.Temperature + _temperatureToleranceRange)
            {
                adjusted.Temperature = Mathf.Max(current.Temperature - (1f * _climateSensitivity), target.Temperature);
            }
            
            // Humidity control
            if (current.Humidity < target.Humidity - _humidityToleranceRange)
            {
                adjusted.Humidity = Mathf.Min(current.Humidity + (2f * _climateSensitivity), target.Humidity);
            }
            else if (current.Humidity > target.Humidity + _humidityToleranceRange)
            {
                adjusted.Humidity = Mathf.Max(current.Humidity - (2f * _climateSensitivity), target.Humidity);
            }
            
            // CO2 control
            if (current.CO2Level < target.CO2Level - _co2ToleranceRange)
            {
                adjusted.CO2Level = Mathf.Min(current.CO2Level + (50f * _climateSensitivity), target.CO2Level);
            }
            else if (current.CO2Level > target.CO2Level + _co2ToleranceRange)
            {
                adjusted.CO2Level = Mathf.Max(current.CO2Level - (25f * _climateSensitivity), target.CO2Level);
            }
            
            // Apply adjustments if significant
            if (!ConditionsAreEqual(current, adjusted))
            {
                SetZoneClimate(zoneId, adjusted);
            }
        }
        
        /// <summary>
        /// Calculates optimal conditions for a growth stage
        /// </summary>
        private EnvironmentalConditions CalculateOptimalConditions(PlantGrowthStage growthStage)
        {
            switch (growthStage)
            {
                case PlantGrowthStage.Seed:
                case PlantGrowthStage.Germination:
                    return new EnvironmentalConditions
                    {
                        Temperature = 24f,
                        Humidity = 65f,
                        CO2Level = 800f,
                        DailyLightIntegral = 15f,
                        AirflowRate = 0.5f
                    };
                    
                case PlantGrowthStage.Seedling:
                    return new EnvironmentalConditions
                    {
                        Temperature = 23f,
                        Humidity = 60f,
                        CO2Level = 900f,
                        DailyLightIntegral = 20f,
                        AirflowRate = 0.7f
                    };
                    
                case PlantGrowthStage.Vegetative:
                    return new EnvironmentalConditions
                    {
                        Temperature = 25f,
                        Humidity = 55f,
                        CO2Level = 1200f,
                        DailyLightIntegral = 35f,
                        AirflowRate = 1f
                    };
                    
                case PlantGrowthStage.Flowering:
                    return new EnvironmentalConditions
                    {
                        Temperature = 23f,
                        Humidity = 45f,
                        CO2Level = 1000f,
                        DailyLightIntegral = 40f,
                        AirflowRate = 1.2f
                    };
                    
                default:
                    return EnvironmentalConditions.CreateIndoorDefault();
            }
        }
        
        /// <summary>
        /// Checks for climate alerts and warnings
        /// </summary>
        private void CheckClimateAlerts(string zoneId, EnvironmentalConditions conditions)
        {
            List<string> issues = new List<string>();
            
            // Temperature alerts
            if (conditions.Temperature < _targetTemperatureMin - (_temperatureToleranceRange * 2))
            {
                issues.Add($"Critical low temperature: {conditions.Temperature:F1}°C");
            }
            else if (conditions.Temperature > _targetTemperatureMax + (_temperatureToleranceRange * 2))
            {
                issues.Add($"Critical high temperature: {conditions.Temperature:F1}°C");
            }
            
            // Humidity alerts
            if (conditions.Humidity < _targetHumidityMin - (_humidityToleranceRange * 2))
            {
                issues.Add($"Critical low humidity: {conditions.Humidity:F1}%");
            }
            else if (conditions.Humidity > _targetHumidityMax + (_humidityToleranceRange * 2))
            {
                issues.Add($"Critical high humidity: {conditions.Humidity:F1}%");
            }
            
            // CO2 alerts
            if (conditions.CO2Level < _targetCO2Level - (_co2ToleranceRange * 2))
            {
                issues.Add($"Low CO2 level: {conditions.CO2Level:F0}ppm");
            }
            
            // Create or clear alerts
            string alertKey = $"climate_{zoneId}";
            
            if (issues.Count > 0)
            {
                var alert = new ClimateAlert
                {
                    ZoneId = zoneId,
                    Issues = issues,
                    Severity = DetermineAlertSeverity(issues),
                    Timestamp = System.DateTime.Now
                };
                
                _activeClimateAlerts[alertKey] = alert;
                OnClimateAlert?.Invoke(zoneId, alert);
            }
            else if (_activeClimateAlerts.ContainsKey(alertKey))
            {
                _activeClimateAlerts.Remove(alertKey);
            }
        }
        
        /// <summary>
        /// Records climate change in history
        /// </summary>
        private void RecordClimateChange(string zoneId, EnvironmentalConditions oldConditions, EnvironmentalConditions newConditions)
        {
            if (!_climateHistory.TryGetValue(zoneId, out ClimateControlHistory history))
            {
                history = new ClimateControlHistory();
                _climateHistory[zoneId] = history;
            }
            
            history.AddReading(newConditions);
        }
        
        /// <summary>
        /// Determines alert severity based on number of issues
        /// </summary>
        private ClimateAlertSeverity DetermineAlertSeverity(List<string> issues)
        {
            if (issues.Count >= 3) return ClimateAlertSeverity.Critical;
            if (issues.Count >= 2) return ClimateAlertSeverity.High;
            return ClimateAlertSeverity.Medium;
        }
        
        /// <summary>
        /// Checks if two environmental conditions are functionally equal
        /// </summary>
        private bool ConditionsAreEqual(EnvironmentalConditions a, EnvironmentalConditions b)
        {
            return Mathf.Abs(a.Temperature - b.Temperature) < 0.1f &&
                   Mathf.Abs(a.Humidity - b.Humidity) < 0.5f &&
                   Mathf.Abs(a.CO2Level - b.CO2Level) < 10f &&
                   Mathf.Abs(a.AirflowRate - b.AirflowRate) < 0.1f;
        }
        
        /// <summary>
        /// Gets climate history for a zone
        /// </summary>
        public ClimateControlHistory GetClimateHistory(string zoneId)
        {
            return _climateHistory.TryGetValue(zoneId, out ClimateControlHistory history) 
                ? history 
                : new ClimateControlHistory();
        }
        
        /// <summary>
        /// Gets all active climate alerts
        /// </summary>
        public IReadOnlyDictionary<string, ClimateAlert> GetActiveAlerts()
        {
            return _activeClimateAlerts;
        }
        
        /// <summary>
        /// Clears alerts for a specific zone
        /// </summary>
        public void ClearZoneAlerts(string zoneId)
        {
            string alertKey = $"climate_{zoneId}";
            _activeClimateAlerts.Remove(alertKey);
        }
        
        #region ChimeraManager Implementation
        
        protected override void OnManagerInitialize()
        {
            // Manager-specific initialization (already implemented in Initialize method)
            Initialize();
        }
        
        protected override void OnManagerShutdown()
        {
            // Manager-specific shutdown logic (already implemented in Shutdown method)
            Shutdown();
        }
        
        #endregion
    }
    
    
    /// <summary>
    /// Climate zone data tracking
    /// </summary>
    [System.Serializable]
    public struct ClimateZoneData
    {
        public string ZoneId;
        public EnvironmentalConditions TargetConditions;
        public EnvironmentalConditions CurrentConditions;
        public bool IsActive;
        public System.DateTime CreatedAt;
    }
    
    /// <summary>
    /// Climate alert information
    /// </summary>
    [System.Serializable]
    public struct ClimateAlert
    {
        public string ZoneId;
        public List<string> Issues;
        public ClimateAlertSeverity Severity;
        public System.DateTime Timestamp;
    }
    
    /// <summary>
    /// Climate alert severity levels
    /// </summary>
    public enum ClimateAlertSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }
    
    /// <summary>
    /// Historical climate data tracking
    /// </summary>
    [System.Serializable]
    public class ClimateControlHistory
    {
        [SerializeField] private List<ClimateReading> _readings = new List<ClimateReading>();
        private const int MAX_READINGS = 500;
        
        public IReadOnlyList<ClimateReading> Readings => _readings.AsReadOnly();
        
        public void AddReading(EnvironmentalConditions conditions)
        {
            var reading = new ClimateReading
            {
                Temperature = conditions.Temperature,
                Humidity = conditions.Humidity,
                CO2Level = conditions.CO2Level,
                AirflowRate = conditions.AirflowRate,
                Timestamp = System.DateTime.Now
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
            var cutoff = System.DateTime.Now.AddHours(-lastHours);
            var recentReadings = _readings.Where(r => r.Timestamp >= cutoff);
            return recentReadings.Any() ? recentReadings.Average(r => r.Temperature) : 0f;
        }
        
        public float GetAverageHumidity(int lastHours = 24)
        {
            var cutoff = System.DateTime.Now.AddHours(-lastHours);
            var recentReadings = _readings.Where(r => r.Timestamp >= cutoff);
            return recentReadings.Any() ? recentReadings.Average(r => r.Humidity) : 0f;
        }
        
    }
    
    /// <summary>
    /// Individual climate reading
    /// </summary>
    [System.Serializable]
    public struct ClimateReading
    {
        public float Temperature;
        public float Humidity;
        public float CO2Level;
        public float AirflowRate;
        public System.DateTime Timestamp;
    }
}