using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System;

namespace ProjectChimera.UI.Panels
{
    /// <summary>
    /// Type definitions and configuration classes for World Space Status System.
    /// Contains all data structures, enums, and configuration for 3D status displays.
    /// </summary>
    
    /// <summary>
    /// Types of status displays available in the world space UI system
    /// </summary>
    public enum StatusDisplayType
    {
        PlantHealth,
        FacilityStatus,
        EquipmentStatus
    }
    
    /// <summary>
    /// Comprehensive status data for various object types in the cannabis cultivation simulation
    /// </summary>
    [System.Serializable]
    public class StatusData
    {
        [Header("Universal Status")]
        public float Health = 100f;
        public string Name;
        public string OperationalStatus;
        
        [Header("Plant-Specific")]
        public string GrowthStage;
        public int? DaysToHarvest;
        public List<string> StressFactors;
        public float? WaterLevel;
        public float? NutrientLevel;
        
        [Header("Facility/Equipment")]
        public float? PowerLevel;
        public float? Temperature;
        public float? Humidity;
        public float? AirQuality;
        public float? Efficiency;
        public float? MaintenanceHours;
        public List<string> Alerts;
        
        [Header("Environmental")]
        public float? LightIntensity;
        public float? CO2Level;
        public float? pH;
        
        [Header("Economic")]
        public float? ProductionValue;
        public float? OperatingCost;
        
        public StatusData()
        {
            StressFactors = new List<string>();
            Alerts = new List<string>();
        }
        
        /// <summary>
        /// Creates status data for a cannabis plant
        /// </summary>
        public static StatusData CreatePlantStatus(string plantName, string growthStage, float health, 
                                                  int? daysToHarvest = null, List<string> stressFactors = null)
        {
            return new StatusData
            {
                Name = plantName,
                Health = health,
                GrowthStage = growthStage,
                DaysToHarvest = daysToHarvest,
                StressFactors = stressFactors != null ? stressFactors : new List<string>(),
                OperationalStatus = "Growing"
            };
        }
        
        /// <summary>
        /// Creates status data for a facility room
        /// </summary>
        public static StatusData CreateFacilityStatus(string facilityName, float powerLevel, 
                                                     float? temperature = null, float? humidity = null,
                                                     float? airQuality = null, List<string> alerts = null)
        {
            return new StatusData
            {
                Name = facilityName,
                PowerLevel = powerLevel,
                Temperature = temperature,
                Humidity = humidity,
                AirQuality = airQuality,
                Alerts = alerts != null ? alerts : new List<string>(),
                OperationalStatus = "Running",
                Health = Mathf.Min(100f, powerLevel)
            };
        }
        
        /// <summary>
        /// Creates status data for equipment
        /// </summary>
        public static StatusData CreateEquipmentStatus(string equipmentName, string operationalStatus,
                                                      float? efficiency = null, float? maintenanceHours = null,
                                                      List<string> alerts = null)
        {
            return new StatusData
            {
                Name = equipmentName,
                OperationalStatus = operationalStatus,
                Efficiency = efficiency,
                MaintenanceHours = maintenanceHours,
                Alerts = alerts != null ? alerts : new List<string>(),
                Health = efficiency.HasValue ? efficiency.Value : (operationalStatus == "Running" ? 100f : 50f)
            };
        }
    }
    
    /// <summary>
    /// Data container for active status displays
    /// </summary>
    public class StatusDisplayData
    {
        public GameObject Target { get; set; }
        public StatusDisplayType DisplayType { get; set; }
        public UIDocument UIDocument { get; set; }
        public StatusData StatusData { get; set; }
        public float LastUpdateTime { get; set; }
        public float CreationTime { get; set; }
        public bool IsVisible { get; set; } = true;
        
        /// <summary>
        /// Age of the display in seconds
        /// </summary>
        public float Age => Time.time - CreationTime;
        
        /// <summary>
        /// Time since last update in seconds
        /// </summary>
        public float TimeSinceUpdate => Time.time - LastUpdateTime;
    }
    
    /// <summary>
    /// Configuration for world space status display system
    /// </summary>
    [System.Serializable]
    public class WorldSpaceStatusConfig
    {
        [Header("Display Settings")]
        public bool billboardMode = true;
        public bool adaptiveScaling = true;
        public float statusScale = 1.0f;
        public AnimationCurve distanceScaleCurve = AnimationCurve.EaseInOut(0f, 1f, 1f, 0.5f);
        
        [Header("Performance")]
        public int poolSize = 50;
        public float updateFrequency = 0.1f;
        public int maxConcurrentUpdates = 10;
        
        [Header("UI Configuration")]
        public PanelSettings panelSettings;
        public int sortingOrder = 100;
        
        [Header("Plant Health Colors")]
        public Color healthyColor = Color.green;
        public Color warnColor = Color.yellow;
        public Color criticalColor = Color.red;
        
        [Header("Facility Status Colors")]
        public Color operationalColor = Color.cyan;
        public Color idleColor = Color.gray;
        public Color errorColor = Color.red;
        
        [Header("Equipment Status Colors")]
        public Color runningColor = Color.green;
        public Color maintenanceColor = Color.orange;
        public Color offlineColor = Color.red;
    }
    
    /// <summary>
    /// Status update event data
    /// </summary>
    public class StatusUpdateEventArgs : EventArgs
    {
        public GameObject Target { get; set; }
        public StatusDisplayType DisplayType { get; set; }
        public StatusData OldStatus { get; set; }
        public StatusData NewStatus { get; set; }
        public float UpdateTime { get; set; }
        
        public StatusUpdateEventArgs(GameObject target, StatusDisplayType displayType, 
                                   StatusData oldStatus, StatusData newStatus)
        {
            Target = target;
            DisplayType = displayType;
            OldStatus = oldStatus;
            NewStatus = newStatus;
            UpdateTime = Time.time;
        }
    }
    
    /// <summary>
    /// Interface for objects that can provide status data
    /// </summary>
    public interface IStatusProvider
    {
        StatusData GetCurrentStatus();
        StatusDisplayType GetDisplayType();
        bool ShouldShowStatus();
    }
    
    /// <summary>
    /// Interface for objects that can receive status update notifications
    /// </summary>
    public interface IStatusUpdateReceiver
    {
        void OnStatusUpdated(StatusUpdateEventArgs args);
    }
    
    /// <summary>
    /// Plant-specific status provider implementation
    /// </summary>
    public class PlantStatusProvider : MonoBehaviour, IStatusProvider
    {
        [Header("Plant Status")]
        [SerializeField] private string _plantName = "Cannabis Plant";
        [SerializeField] private string _currentGrowthStage = "Vegetative";
        [SerializeField] private float _health = 100f;
        [SerializeField] private int _daysToHarvest = 30;
        [SerializeField] private List<string> _currentStressors = new List<string>();
        
        public StatusData GetCurrentStatus()
        {
            return StatusData.CreatePlantStatus(_plantName, _currentGrowthStage, _health, 
                                              _daysToHarvest > 0 ? _daysToHarvest : null, 
                                              _currentStressors);
        }
        
        public StatusDisplayType GetDisplayType()
        {
            return StatusDisplayType.PlantHealth;
        }
        
        public bool ShouldShowStatus()
        {
            return gameObject.activeInHierarchy && _health > 0f;
        }
        
        /// <summary>
        /// Updates plant health and triggers status refresh
        /// </summary>
        public void UpdateHealth(float newHealth)
        {
            _health = Mathf.Clamp(newHealth, 0f, 100f);
        }
        
        /// <summary>
        /// Updates growth stage
        /// </summary>
        public void SetGrowthStage(string stage)
        {
            _currentGrowthStage = stage;
        }
        
        /// <summary>
        /// Adds a stress factor
        /// </summary>
        public void AddStressor(string stressor)
        {
            if (!_currentStressors.Contains(stressor))
            {
                _currentStressors.Add(stressor);
            }
        }
        
        /// <summary>
        /// Removes a stress factor
        /// </summary>
        public void RemoveStressor(string stressor)
        {
            _currentStressors.Remove(stressor);
        }
    }
    
    /// <summary>
    /// Facility-specific status provider implementation
    /// </summary>
    public class FacilityStatusProvider : MonoBehaviour, IStatusProvider
    {
        [Header("Facility Status")]
        [SerializeField] private string _facilityName = "Grow Room";
        [SerializeField] private float _powerLevel = 100f;
        [SerializeField] private float _temperature = 24f;
        [SerializeField] private float _humidity = 55f;
        [SerializeField] private float _airQuality = 85f;
        [SerializeField] private List<string> _currentAlerts = new List<string>();
        
        public StatusData GetCurrentStatus()
        {
            return StatusData.CreateFacilityStatus(_facilityName, _powerLevel, _temperature, 
                                                 _humidity, _airQuality, _currentAlerts);
        }
        
        public StatusDisplayType GetDisplayType()
        {
            return StatusDisplayType.FacilityStatus;
        }
        
        public bool ShouldShowStatus()
        {
            return gameObject.activeInHierarchy;
        }
        
        /// <summary>
        /// Updates environmental conditions
        /// </summary>
        public void UpdateEnvironment(float temperature, float humidity, float airQuality)
        {
            _temperature = temperature;
            _humidity = humidity;
            _airQuality = airQuality;
        }
        
        /// <summary>
        /// Adds an alert
        /// </summary>
        public void AddAlert(string alert)
        {
            if (!_currentAlerts.Contains(alert))
            {
                _currentAlerts.Add(alert);
            }
        }
        
        /// <summary>
        /// Removes an alert
        /// </summary>
        public void RemoveAlert(string alert)
        {
            _currentAlerts.Remove(alert);
        }
    }
}