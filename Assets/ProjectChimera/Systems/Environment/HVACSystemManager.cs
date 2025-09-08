using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Data.Simulation;
using EnvironmentalConditions = ProjectChimera.Data.Shared.EnvironmentalConditions;
// using HVACOperationMode = ProjectChimera.Data.Simulation.HVACOperationMode; // Type not available
// using HVACZone = ProjectChimera.Data.Simulation.HVACZone; // Type not available

namespace ProjectChimera.Systems.Environment
{
    /// <summary>
    /// HVAC operation mode enum - local definition until Data.Simulation is available
    /// </summary>
    public enum HVACOperationMode
    {
        Auto,
        Manual,
        Off,
        Cooling,
        Heating,
        Ventilation
    }
    /// <summary>
    /// PC013-6b: Specialized service for HVAC system management
    /// Extracted from monolithic EnvironmentalManager.cs to handle heating,
    /// ventilation, and air conditioning systems with equipment management.
    /// </summary>
    public class HVACSystemManager : ChimeraManager, ITickable, IEnvironmentalService
    {
        [Header("HVAC Configuration")]
        [SerializeField] private float _hvacUpdateInterval = 10f; // 10 seconds
        [SerializeField] private bool _enableAutomaticHVAC = true;
        [SerializeField] private float _hvacEfficiency = 0.8f;
        [SerializeField] private int _maxHVACZones = 20;
        
        [Header("Heating System")]
        [SerializeField] private float _heatingPower = 2000f; // Watts
        [SerializeField] private float _heatingEfficiency = 0.9f;
        [SerializeField] private float _maxHeatingTemperature = 30f;
        
        [Header("Cooling System")]
        [SerializeField] private float _coolingPower = 1500f; // Watts
        [SerializeField] private float _coolingEfficiency = 0.85f;
        [SerializeField] private float _minCoolingTemperature = 18f;
        
        [Header("Ventilation System")]
        [SerializeField] private float _fanPower = 500f; // Watts
        [SerializeField] private float _maxAirflowRate = 10f; // CFM per square meter
        [SerializeField] private float _airFilterEfficiency = 0.95f;
        
        // HVAC tracking data
        private Dictionary<string, HVACZoneData> _hvacZones = new Dictionary<string, HVACZoneData>();
        private Dictionary<string, List<HVACEquipment>> _zoneEquipment = new Dictionary<string, List<HVACEquipment>>();
        private Dictionary<string, HVACPerformanceData> _performanceData = new Dictionary<string, HVACPerformanceData>();
        private Dictionary<string, HVACAlert> _activeHVACAlerts = new Dictionary<string, HVACAlert>();
        
        // Timing
        private float _lastHVACUpdate;
        
        public bool IsInitialized { get; private set; }
        
        // Properties
        public bool AutomaticHVAC 
        { 
            get => _enableAutomaticHVAC; 
            set => _enableAutomaticHVAC = value; 
        }
        public int ActiveHVACZones => _hvacZones.Count;
        public int ActiveAlerts => _activeHVACAlerts.Count;
        public float SystemEfficiency 
        { 
            get => _hvacEfficiency; 
            set => _hvacEfficiency = Mathf.Clamp(value, 0.1f, 1f); 
        }
        
        // Events
        public System.Action<string, HVACOperationMode> OnHVACModeChanged;
        public System.Action<string, float> OnPowerConsumptionChanged;
        public System.Action<string, HVACAlert> OnHVACAlert;
        public System.Action<string, EnvironmentalConditions> OnEnvironmentControlled;
        public System.Action<string, HVACEquipment> OnEquipmentStatusChanged;
        
        public void Initialize()
        {
            if (IsInitialized) return;
            
            ChimeraLogger.Log("[HVACSystemManager] Initializing HVAC systems...");
            
            _lastHVACUpdate = Time.time;
            
            IsInitialized = true;
            ChimeraLogger.Log($"[HVACSystemManager] Initialized. Auto-HVAC: {_enableAutomaticHVAC}, Max zones: {_maxHVACZones}");
        }
        
        public void Shutdown()
        {
            ChimeraLogger.Log("[HVACSystemManager] Shutting down HVAC systems...");
            
            // Turn off all equipment before shutdown
            foreach (var kvp in _zoneEquipment)
            {
                var equipmentList = kvp.Value;
                for (int i = 0; i < equipmentList.Count; i++)
                {
                    var equipment = equipmentList[i];
                    equipment.IsOperational = false;
                    equipmentList[i] = equipment;
                }
            }
            
            _hvacZones.Clear();
            _zoneEquipment.Clear();
            _performanceData.Clear();
            _activeHVACAlerts.Clear();
            
            IsInitialized = false;
        }
        
        public void Tick(float deltaTime)
        {
            if (!IsInitialized || !_enableAutomaticHVAC) return;
            
            if (Time.time - _lastHVACUpdate >= _hvacUpdateInterval)
            {
                UpdateHVACSystems();
                _lastHVACUpdate = Time.time;
            }
        }
        
        /// <summary>
        /// Creates a new HVAC zone
        /// </summary>
        public bool CreateHVACZone(string zoneId, float zoneArea, EnvironmentalConditions targetConditions)
        {
            if (string.IsNullOrEmpty(zoneId))
            {
                ChimeraLogger.LogWarning("[HVACSystemManager] Cannot create zone with null or empty ID");
                return false;
            }
            
            if (_hvacZones.ContainsKey(zoneId))
            {
                ChimeraLogger.LogWarning($"[HVACSystemManager] HVAC zone '{zoneId}' already exists");
                return false;
            }
            
            if (_hvacZones.Count >= _maxHVACZones)
            {
                ChimeraLogger.LogWarning($"[HVACSystemManager] Maximum HVAC zone limit ({_maxHVACZones}) reached");
                return false;
            }
            
            var hvacZone = new HVACZoneData
            {
                ZoneId = zoneId,
                ZoneName = $"HVAC_Zone_{zoneId.Substring(0, 8)}",
                ZoneSettings = HVACZoneSettingsData.Default.SettingsName,
                ZoneEquipment = "Standard",
                ControlParameters = HVACControlParametersData.Default.ParameterId,
                ZoneStatus = "Active",
                ZoneArea = zoneArea,
                TargetConditions = targetConditions,
                CurrentConditions = EnvironmentalConditions.CreateIndoorDefault(),
                OperationMode = HVACOperationMode.Auto,
                IsActive = true,
                CreatedAt = System.DateTime.Now,
                LastUpdated = System.DateTime.Now
            };
            
            _hvacZones[zoneId] = hvacZone;
            _zoneEquipment[zoneId] = new List<HVACEquipment>();
            _performanceData[zoneId] = new HVACPerformanceData();
            
            // Install default equipment based on zone area
            InstallDefaultEquipment(zoneId, zoneArea);
            
            ChimeraLogger.Log($"[HVACSystemManager] Created HVAC zone '{zoneId}' with area {zoneArea}m²");
            return true;
        }
        
        /// <summary>
        /// Installs HVAC equipment in a zone
        /// </summary>
        public bool InstallEquipment(string zoneId, HVACEquipmentType equipmentType, float capacity)
        {
            if (!_hvacZones.ContainsKey(zoneId))
            {
                ChimeraLogger.LogWarning($"[HVACSystemManager] Zone '{zoneId}' not found for equipment installation");
                return false;
            }
            
            var equipment = new HVACEquipment
            {
                EquipmentId = System.Guid.NewGuid().ToString(),
                Type = equipmentType,
                Capacity = capacity,
                PowerConsumption = CalculateEquipmentPower(equipmentType, capacity),
                Efficiency = CalculateEquipmentEfficiency(equipmentType),
                IsOperational = true,
                InstallationDate = System.DateTime.Now
            };
            
            _zoneEquipment[zoneId].Add(equipment);
            
            ChimeraLogger.Log($"[HVACSystemManager] Installed {equipmentType} equipment in zone '{zoneId}' (Capacity: {capacity})");
            return true;
        }
        
        /// <summary>
        /// Controls HVAC system for environmental management
        /// </summary>
        public void ControlEnvironment(string zoneId, EnvironmentalConditions targetConditions)
        {
            if (!_hvacZones.TryGetValue(zoneId, out HVACZoneData zone))
            {
                ChimeraLogger.LogWarning($"[HVACSystemManager] Zone '{zoneId}' not found for environmental control");
                return;
            }
            
            var currentConditions = zone.CurrentConditions;
            var newConditions = ProcessHVACControl(zoneId, currentConditions, targetConditions);
            
            // Update zone conditions
            zone.CurrentConditions = newConditions;
            zone.TargetConditions = targetConditions;
            _hvacZones[zoneId] = zone;
            
            // Update performance metrics
            UpdatePerformanceData(zoneId);
            
            // Check for alerts
            CheckHVACAlerts(zoneId);
            
            OnEnvironmentControlled?.Invoke(zoneId, newConditions);
            
            ChimeraLogger.Log($"[HVACSystemManager] Controlled environment for zone '{zoneId}'");
        }
        
        /// <summary>
        /// Processes HVAC control logic
        /// </summary>
        private EnvironmentalConditions ProcessHVACControl(string zoneId, EnvironmentalConditions current, EnvironmentalConditions target)
        {
            var equipment = _zoneEquipment[zoneId];
            var controlled = current;
            
            // Temperature control
            controlled.Temperature = ProcessTemperatureControl(equipment, current.Temperature, target.Temperature);
            
            // Humidity control (through ventilation and dehumidification)
            controlled.Humidity = ProcessHumidityControl(equipment, current.Humidity, target.Humidity);
            
            // Airflow control
            controlled.AirflowRate = ProcessAirflowControl(equipment, target.AirflowRate);
            
            // Air quality improvement
            controlled = ProcessAirQualityControl(equipment, controlled);
            
            return controlled;
        }
        
        /// <summary>
        /// Processes temperature control through heating/cooling
        /// </summary>
        private float ProcessTemperatureControl(List<HVACEquipment> equipment, float currentTemp, float targetTemp)
        {
            float tempDelta = targetTemp - currentTemp;
            float adjustment = 0f;
            
            if (tempDelta > 0.5f) // Need heating
            {
                var heaters = equipment.Where(e => e.Type == HVACEquipmentType.Heater && e.IsOperational);
                foreach (var heater in heaters)
                {
                    float heatingCapacity = heater.Capacity * heater.Efficiency * _hvacEfficiency;
                    adjustment += heatingCapacity * Time.deltaTime * 0.001f; // Convert to temperature change
                }
            }
            else if (tempDelta < -0.5f) // Need cooling
            {
                var coolers = equipment.Where(e => e.Type == HVACEquipmentType.AirConditioner && e.IsOperational);
                foreach (var cooler in coolers)
                {
                    float coolingCapacity = cooler.Capacity * cooler.Efficiency * _hvacEfficiency;
                    adjustment -= coolingCapacity * Time.deltaTime * 0.001f; // Convert to temperature change
                }
            }
            
            return Mathf.Clamp(currentTemp + adjustment, _minCoolingTemperature, _maxHeatingTemperature);
        }
        
        /// <summary>
        /// Processes humidity control through ventilation
        /// </summary>
        private float ProcessHumidityControl(List<HVACEquipment> equipment, float currentHumidity, float targetHumidity)
        {
            float humidityDelta = targetHumidity - currentHumidity;
            float adjustment = 0f;
            
            var ventilationSystems = equipment.Where(e => e.Type == HVACEquipmentType.Ventilator && e.IsOperational);
            foreach (var system in ventilationSystems)
            {
                float ventilationCapacity = system.Capacity * system.Efficiency;
                adjustment += Mathf.Sign(humidityDelta) * ventilationCapacity * Time.deltaTime * 0.01f;
            }
            
            return Mathf.Clamp(currentHumidity + adjustment, 20f, 80f);
        }
        
        /// <summary>
        /// Processes airflow control through fans and ventilation
        /// </summary>
        private float ProcessAirflowControl(List<HVACEquipment> equipment, float targetAirflow)
        {
            float totalAirflow = 0f;
            
            var airflowEquipment = equipment.Where(e => 
                (e.Type == HVACEquipmentType.Ventilator || e.Type == HVACEquipmentType.Fan) && 
                e.IsOperational);
                
            foreach (var fan in airflowEquipment)
            {
                totalAirflow += fan.Capacity * fan.Efficiency;
            }
            
            return Mathf.Min(totalAirflow, _maxAirflowRate);
        }
        
        /// <summary>
        /// Processes air quality control through filtration
        /// </summary>
        private EnvironmentalConditions ProcessAirQualityControl(List<HVACEquipment> equipment, EnvironmentalConditions conditions)
        {
            var filters = equipment.Where(e => e.Type == HVACEquipmentType.AirFilter && e.IsOperational);
            
            foreach (var filter in filters)
            {
                // Improve air quality through filtration
                float filteringEffectiveness = filter.Efficiency * _airFilterEfficiency;
                // Would affect air quality metrics if they existed in EnvironmentalConditions
            }
            
            return conditions;
        }
        
        /// <summary>
        /// Updates HVAC systems for all zones
        /// </summary>
        private void UpdateHVACSystems()
        {
            foreach (var kvp in _hvacZones)
            {
                string zoneId = kvp.Key;
                HVACZoneData zone = kvp.Value;
                
                if (!zone.IsActive) continue;
                
                // Auto-control environment if in auto mode
                if (zone.OperationMode == HVACOperationMode.Auto)
                {
                    ControlEnvironment(zoneId, zone.TargetConditions);
                }
                
                // Update equipment status and maintenance
                UpdateEquipmentStatus(zoneId);
            }
        }
        
        /// <summary>
        /// Updates equipment status and maintenance needs
        /// </summary>
        private void UpdateEquipmentStatus(string zoneId)
        {
            if (!_zoneEquipment.TryGetValue(zoneId, out List<HVACEquipment> equipment))
                return;
            
            for (int i = 0; i < equipment.Count; i++)
            {
                var item = equipment[i];
                
                // Simulate equipment degradation
                item.Efficiency = Mathf.Max(item.Efficiency - (Time.deltaTime * 0.00001f), 0.5f);
                
                // Check for maintenance needs
                var daysSinceInstallation = (System.DateTime.Now - item.InstallationDate).TotalDays;
                if (daysSinceInstallation > 90 && item.Efficiency < 0.8f) // 90 days
                {
                    item.MaintenanceRequired = true;
                }
                
                equipment[i] = item;
                OnEquipmentStatusChanged?.Invoke(zoneId, item);
            }
        }
        
        /// <summary>
        /// Updates performance data for a zone
        /// </summary>
        private void UpdatePerformanceData(string zoneId)
        {
            if (!_performanceData.TryGetValue(zoneId, out HVACPerformanceData data))
                return;
            
            var equipment = _zoneEquipment[zoneId];
            
            // Calculate total power consumption
            data.TotalPowerConsumption = equipment.Where(e => e.IsOperational).Sum(e => e.PowerConsumption);
            
            // Calculate average efficiency
            var operationalEquipment = equipment.Where(e => e.IsOperational).ToList();
            data.AverageEfficiency = operationalEquipment.Any() 
                ? operationalEquipment.Average(e => e.Efficiency) 
                : 0f;
            
            // Update runtime
            data.TotalRuntime += Time.deltaTime;
            data.LastUpdated = System.DateTime.Now;
            
            _performanceData[zoneId] = data;
            
            OnPowerConsumptionChanged?.Invoke(zoneId, data.TotalPowerConsumption);
        }
        
        /// <summary>
        /// Checks for HVAC alerts and issues
        /// </summary>
        private void CheckHVACAlerts(string zoneId)
        {
            List<string> issues = new List<string>();
            var equipment = _zoneEquipment[zoneId];
            var performanceData = _performanceData[zoneId];
            
            // Equipment alerts
            var failedEquipment = equipment.Where(e => !e.IsOperational).Count();
            if (failedEquipment > 0)
            {
                issues.Add($"{failedEquipment} equipment units offline");
            }
            
            var maintenanceNeeded = equipment.Where(e => e.MaintenanceRequired).Count();
            if (maintenanceNeeded > 0)
            {
                issues.Add($"{maintenanceNeeded} equipment units need maintenance");
            }
            
            // Performance alerts
            if (performanceData.AverageEfficiency < 0.7f)
            {
                issues.Add($"Low system efficiency: {performanceData.AverageEfficiency:F1}%");
            }
            
            if (performanceData.TotalPowerConsumption > 5000f) // High power consumption
            {
                issues.Add($"High power consumption: {performanceData.TotalPowerConsumption:F0}W");
            }
            
            // Create or clear alerts
            string alertKey = $"hvac_{zoneId}";
            
            if (issues.Count > 0)
            {
                var alert = new HVACAlert
                {
                    ZoneId = zoneId,
                    Issues = issues,
                    Severity = DetermineHVACAlertSeverity(issues),
                    Timestamp = System.DateTime.Now
                };
                
                _activeHVACAlerts[alertKey] = alert;
                OnHVACAlert?.Invoke(zoneId, alert);
            }
            else if (_activeHVACAlerts.ContainsKey(alertKey))
            {
                _activeHVACAlerts.Remove(alertKey);
            }
        }
        
        /// <summary>
        /// Installs default equipment for a new zone
        /// </summary>
        private void InstallDefaultEquipment(string zoneId, float zoneArea)
        {
            // Install basic equipment based on zone size
            float heatingCapacity = zoneArea * 100f; // 100W per m²
            float coolingCapacity = zoneArea * 80f;   // 80W per m²
            float ventilationCapacity = zoneArea * 5f; // 5 CFM per m²
            
            InstallEquipment(zoneId, HVACEquipmentType.Heater, heatingCapacity);
            InstallEquipment(zoneId, HVACEquipmentType.AirConditioner, coolingCapacity);
            InstallEquipment(zoneId, HVACEquipmentType.Ventilator, ventilationCapacity);
            InstallEquipment(zoneId, HVACEquipmentType.AirFilter, ventilationCapacity);
        }
        
        /// <summary>
        /// Calculates equipment power consumption
        /// </summary>
        private float CalculateEquipmentPower(HVACEquipmentType type, float capacity)
        {
            switch (type)
            {
                case HVACEquipmentType.Heater:
                    return capacity * 0.8f; // 80% of heating capacity as power
                case HVACEquipmentType.AirConditioner:
                    return capacity * 1.2f; // 120% of cooling capacity as power (COP consideration)
                case HVACEquipmentType.Ventilator:
                    return capacity * 0.1f; // 10% of airflow capacity as power
                case HVACEquipmentType.Fan:
                    return capacity * 0.05f; // 5% of airflow capacity as power
                case HVACEquipmentType.AirFilter:
                    return 50f; // Fixed power for filter
                default:
                    return 100f;
            }
        }
        
        /// <summary>
        /// Calculates equipment efficiency
        /// </summary>
        private float CalculateEquipmentEfficiency(HVACEquipmentType type)
        {
            switch (type)
            {
                case HVACEquipmentType.Heater:
                    return _heatingEfficiency;
                case HVACEquipmentType.AirConditioner:
                    return _coolingEfficiency;
                case HVACEquipmentType.Ventilator:
                case HVACEquipmentType.Fan:
                    return 0.9f;
                case HVACEquipmentType.AirFilter:
                    return _airFilterEfficiency;
                default:
                    return 0.8f;
            }
        }
        
        /// <summary>
        /// Determines HVAC alert severity
        /// </summary>
        private HVACAlertSeverity DetermineHVACAlertSeverity(List<string> issues)
        {
            if (issues.Any(i => i.Contains("offline"))) return HVACAlertSeverity.Critical;
            if (issues.Count >= 2) return HVACAlertSeverity.High;
            return HVACAlertSeverity.Medium;
        }
        
        /// <summary>
        /// Gets HVAC zone data
        /// </summary>
        public HVACZoneData GetHVACZone(string zoneId)
        {
            return _hvacZones.TryGetValue(zoneId, out HVACZoneData zone) 
                ? zone 
                : new HVACZoneData();
        }
        
        /// <summary>
        /// Gets performance data for a zone
        /// </summary>
        public HVACPerformanceData GetPerformanceData(string zoneId)
        {
            return _performanceData.TryGetValue(zoneId, out HVACPerformanceData data) 
                ? data 
                : new HVACPerformanceData();
        }
        
        /// <summary>
        /// Gets all active HVAC alerts
        /// </summary>
        public IReadOnlyDictionary<string, HVACAlert> GetActiveAlerts()
        {
            return _activeHVACAlerts;
        }
        
        /// <summary>
        /// Sets HVAC operation mode for a zone
        /// </summary>
        public void SetOperationMode(string zoneId, HVACOperationMode mode)
        {
            if (_hvacZones.TryGetValue(zoneId, out HVACZoneData zone))
            {
                zone.OperationMode = mode;
                _hvacZones[zoneId] = zone;
                OnHVACModeChanged?.Invoke(zoneId, mode);
            }
        }
        
        #region ChimeraManager Implementation
        
        protected override void OnManagerInitialize()
        {
            // Manager-specific initialization logic (already implemented in Initialize method)
        }
        
        protected override void OnManagerShutdown()
        {
            // Manager-specific shutdown logic (already implemented in Shutdown method)
        }
        
        protected override void Start()
        {
            base.Start();
            // Register with UpdateOrchestrator
            UpdateOrchestrator.Instance?.RegisterTickable(this);
        }

        protected override void OnDestroy()
        {
            // Unregister from UpdateOrchestrator
            UpdateOrchestrator.Instance?.UnregisterTickable(this);
            base.OnDestroy();
        }

        // ITickable implementation
        public int Priority => 0;
        public bool Enabled => enabled && gameObject.activeInHierarchy;
        
        public virtual void OnRegistered() 
        { 
            // Override in derived classes if needed
        }
        
        public virtual void OnUnregistered() 
        { 
            // Override in derived classes if needed
        }
        
        #endregion
    }
    
    /// <summary>
    /// HVAC zone data
    /// </summary>
    [System.Serializable]
    public struct HVACZoneData
    {
        public string ZoneId;
        public string ZoneName;
        public string ZoneSettings;
        public string ZoneEquipment;
        public string ControlParameters;
        public string ZoneStatus;
        public float ZoneArea;
        public EnvironmentalConditions TargetConditions;
        public EnvironmentalConditions CurrentConditions;
        public HVACOperationMode OperationMode;
        public bool IsActive;
        public System.DateTime CreatedAt;
        public System.DateTime LastUpdated;
    }
    
    /// <summary>
    /// HVAC equipment data
    /// </summary>
    [System.Serializable]
    public struct HVACEquipment
    {
        public string EquipmentId;
        public HVACEquipmentType Type;
        public float Capacity;
        public float PowerConsumption;
        public float Efficiency;
        public bool IsOperational;
        public bool MaintenanceRequired;
        public System.DateTime InstallationDate;
    }
    
    /// <summary>
    /// HVAC performance tracking
    /// </summary>
    [System.Serializable]
    public struct HVACPerformanceData
    {
        public float TotalPowerConsumption;
        public float AverageEfficiency;
        public float TotalRuntime;
        public System.DateTime LastUpdated;
    }
    
    /// <summary>
    /// HVAC alert information
    /// </summary>
    [System.Serializable]
    public struct HVACAlert
    {
        public string ZoneId;
        public List<string> Issues;
        public HVACAlertSeverity Severity;
        public System.DateTime Timestamp;
    }
    
    
    /// <summary>
    /// HVAC equipment types
    /// </summary>
    public enum HVACEquipmentType
    {
        Heater,
        AirConditioner,
        Ventilator,
        Fan,
        AirFilter,
        Humidifier,
        Dehumidifier
    }
    
    /// <summary>
    /// Active HVAC equipment reference structure
    /// </summary>
    [System.Serializable]
    public struct HVACEquipmentReference
    {
        public string EquipmentId;
        public HVACEquipmentType Type;
        public float Capacity;
        public bool IsOperational;
        public float PowerConsumption;
        
        public HVACEquipmentReference(HVACEquipment equipment)
        {
            EquipmentId = equipment.EquipmentId;
            Type = equipment.Type;
            Capacity = equipment.Capacity;
            IsOperational = equipment.IsOperational;
            PowerConsumption = equipment.PowerConsumption;
        }
    }
    
    /// <summary>
    /// HVAC alert severity levels
    /// </summary>
    public enum HVACAlertSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }
    
    /// <summary>
    /// HVAC zone settings data structure
    /// </summary>
    [System.Serializable]
    public struct HVACZoneSettingsData
    {
        public string SettingsId;
        public string SettingsName;
        public float TemperatureSetpoint;
        public float HumiditySetpoint;
        public HVACOperationMode DefaultMode;
        public bool AutoMode;
        
        public static HVACZoneSettingsData Default => new HVACZoneSettingsData
        {
            SettingsId = System.Guid.NewGuid().ToString(),
            SettingsName = "Default",
            TemperatureSetpoint = 22f,
            HumiditySetpoint = 55f,
            DefaultMode = HVACOperationMode.Auto,
            AutoMode = true
        };
    }
    
    /// <summary>
    /// HVAC control parameters
    /// </summary>
    [System.Serializable]
    public struct HVACControlParametersData
    {
        public string ParameterId;
        public float ProportionalGain;
        public float IntegralGain;
        public float DerivativeGain;
        public float DeadBand;
        public float ResponseTime;
        
        public static HVACControlParametersData Default => new HVACControlParametersData
        {
            ParameterId = System.Guid.NewGuid().ToString(),
            ProportionalGain = 1.0f,
            IntegralGain = 0.1f,
            DerivativeGain = 0.05f,
            DeadBand = 0.5f,
            ResponseTime = 5.0f
        };
    }
    
    /// <summary>
    /// HVAC zone status
    /// </summary>
    [System.Serializable]
    public struct HVACZoneStatusData
    {
        public string StatusId;
        public string CurrentStatus;
        public bool IsOperational;
        public float EfficiencyRating;
        public System.DateTime LastStatusUpdate;
        
        public static HVACZoneStatusData Active => new HVACZoneStatusData
        {
            StatusId = System.Guid.NewGuid().ToString(),
            CurrentStatus = "Active",
            IsOperational = true,
            EfficiencyRating = 1.0f,
            LastStatusUpdate = System.DateTime.Now
        };
    }
    
    /// <summary>
    /// Active HVAC equipment collection
    /// </summary>
    [System.Serializable]
    public struct ActiveHVACCollection
    {
        public System.Collections.Generic.List<HVACEquipment> Equipment;
        public float TotalPowerConsumption;
        public float AverageEfficiency;
        public int OperationalCount;
        
        public ActiveHVACCollection(System.Collections.Generic.List<HVACEquipment> equipment)
        {
            Equipment = equipment ?? new System.Collections.Generic.List<HVACEquipment>();
            TotalPowerConsumption = 0f;
            AverageEfficiency = 0f;
            OperationalCount = 0;
        }
    }
}