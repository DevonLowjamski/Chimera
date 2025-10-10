using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;
using ProjectChimera.Data.Simulation.HVAC;
using ProjectChimera.Data.Shared;

namespace ProjectChimera.Systems.Construction.Utilities
{
    /// <summary>
    /// HVAC Integration system - manages climate control zones and equipment.
    ///
    /// GAMEPLAY PURPOSE - VIDEO GAME FIRST:
    /// ====================================
    /// "Perfect climate = perfect plants! Master temperature, humidity, and airflow."
    ///
    /// **Player Experience**:
    /// - Create climate zones (veg room, flower room, dry room)
    /// - Set target temperature, humidity, CO2 for each zone
    /// - Place HVAC equipment (AC, heaters, humidifiers, dehumidifiers, fans)
    /// - Watch real-time climate adjustment
    /// - VPD (Vapor Pressure Deficit) optimization for plant health
    ///
    /// **Strategic Depth**:
    /// - Different growth stages need different climates
    /// - Energy cost vs. precision trade-offs
    /// - Zone isolation prevents cross-contamination
    /// - Equipment sizing (too small = can't maintain, too large = wastes energy)
    ///
    /// **Construction Integration**:
    /// - Zones tied to room structure (walls define boundaries)
    /// - Equipment requires electrical power connection
    /// - Ducting/airflow pathways (simplified for Phase 1)
    /// - Visual climate overlay in cultivation mode
    ///
    /// INVISIBLE COMPLEXITY:
    /// Players see: "Flower Room: 78°F, 55% RH, VPD: 1.2 kPa (Optimal)" → simple!
    /// Behind scenes: PID control loops, thermal dynamics, equipment coordination.
    /// </summary>
    public class HVACIntegration : MonoBehaviour, ITickable
    {
        [Header("Dependencies")]
        [SerializeField] private ElectricalSystem _electricalSystem;

        [Header("Configuration")]
        [SerializeField] private float _updateIntervalSeconds = 60f; // Update every minute
        [SerializeField] private float _temperatureChangeRate = 2f;  // °F per minute (equipment impact)
        [SerializeField] private float _humidityChangeRate = 5f;     // % per minute (equipment impact)

        [Header("Defaults")]
        [SerializeField] private float _defaultTemperature = 72f;  // 72°F
        [SerializeField] private float _defaultHumidity = 55f;     // 55% RH
        [SerializeField] private float _defaultCO2 = 400f;         // 400 PPM (ambient)

        // Zone management
        private Dictionary<string, HVACZone> _zones = new Dictionary<string, HVACZone>();
        private Dictionary<string, HVACEquipment> _equipment = new Dictionary<string, HVACEquipment>();

        // Update timing
        private float _timeSinceLastUpdate = 0f;

        // ITickable properties
        public int TickPriority => 10; // Low priority - climate changes are gradual
        public bool IsTickable => enabled && gameObject.activeInHierarchy;

        // Events
        public event Action<string> OnZoneCreated;                           // zoneId
        public event Action<string> OnZoneRemoved;                           // zoneId
        public event Action<string, EnvironmentalConditions> OnZoneUpdated;  // zoneId, conditions
        public event Action<string> OnEquipmentAdded;                        // equipmentId
        public event Action<string, string> OnClimateAlert;                  // zoneId, alertMessage

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            // Get electrical system
            if (_electricalSystem == null)
            {
                var container = ServiceContainerFactory.Instance;
                _electricalSystem = container?.Resolve<ElectricalSystem>();
            }

            // Register with service container
            var serviceContainer = ServiceContainerFactory.Instance;
            serviceContainer?.RegisterSingleton<HVACIntegration>(this);

            // Register with UpdateOrchestrator for ITickable
            var orchestrator = serviceContainer?.Resolve<UpdateOrchestrator>();
            orchestrator?.RegisterTickable(this);

            ChimeraLogger.Log("HVAC",
                "HVAC integration initialized - climate control ready!", this);
        }

        public void Tick(float deltaTime)
        {
            // Update zones periodically
            _timeSinceLastUpdate += deltaTime;

            if (_timeSinceLastUpdate >= _updateIntervalSeconds)
            {
                UpdateAllZones();
                _timeSinceLastUpdate = 0f;
            }
        }

        #region Zone Management

        /// <summary>
        /// Creates a climate zone (room with environmental control).
        /// GAMEPLAY: Player creates zones in construction mode, names them (e.g., "Veg Room 1").
        /// </summary>
        public bool CreateZone(string zoneId, string zoneName, Vector3 location, float volumeCubicMeters = 100f)
        {
            if (_zones.ContainsKey(zoneId))
            {
                ChimeraLogger.LogWarning("HVAC",
                    $"Zone {zoneId} already exists", this);
                return false;
            }

            var zone = new HVACZone
            {
                ZoneId = zoneId,
                ZoneName = zoneName,
                ZoneSettings = new HVACZoneSettings
                {
                    DefaultTemperature = _defaultTemperature,
                    DefaultHumidity = _defaultHumidity,
                    DefaultCO2Level = _defaultCO2,
                    ZoneVolume = volumeCubicMeters,
                    EnableZoneIsolation = true,
                    EnableVPDControl = true,
                    ZonePriority = ZonePriority.Normal
                },
                CurrentConditions = new EnvironmentalConditions
                {
                    Temperature = _defaultTemperature,
                    Humidity = _defaultHumidity,
                    CO2Level = _defaultCO2,
                    AirVelocity = 0.3f
                },
                TargetConditions = new EnvironmentalConditions
                {
                    Temperature = _defaultTemperature,
                    Humidity = _defaultHumidity,
                    CO2Level = _defaultCO2,
                    AirVelocity = 0.3f
                },
                ZoneEquipment = new List<ActiveHVACEquipment>(),
                ControlParameters = new HVACControlParameters
                {
                    TemperatureTolerance = 2f,
                    HumidityTolerance = 5f,
                    AutoMode = true,
                    OperationMode = HVACOperationMode.Auto
                },
                ZoneStatus = HVACZoneStatus.Active,
                CreatedAt = DateTime.Now,
                LastUpdated = DateTime.Now
            };

            _zones[zoneId] = zone;
            OnZoneCreated?.Invoke(zoneId);

            ChimeraLogger.Log("HVAC",
                $"Zone created: {zoneName} ({volumeCubicMeters}m³) at {location}", this);

            return true;
        }

        /// <summary>
        /// Removes a climate zone.
        /// GAMEPLAY: Player demolishes room, zone is automatically removed.
        /// </summary>
        public bool RemoveZone(string zoneId)
        {
            if (!_zones.ContainsKey(zoneId))
                return false;

            var zone = _zones[zoneId];

            // Disconnect all equipment first
            foreach (var equipment in zone.ZoneEquipment.ToList())
            {
                RemoveEquipment(equipment.EquipmentId);
            }

            _zones.Remove(zoneId);
            OnZoneRemoved?.Invoke(zoneId);

            ChimeraLogger.Log("HVAC",
                $"Zone {zone.ZoneName} removed", this);

            return true;
        }

        /// <summary>
        /// Sets target climate for a zone.
        /// GAMEPLAY: Player adjusts sliders in zone settings UI.
        /// </summary>
        public void SetZoneTargets(string zoneId, float targetTemp, float targetHumidity, float targetCO2 = 400f)
        {
            if (!_zones.TryGetValue(zoneId, out var zone))
                return;

            zone.TargetConditions.Temperature = targetTemp;
            zone.TargetConditions.Humidity = targetHumidity;
            zone.TargetConditions.CO2Level = targetCO2;
            zone.LastUpdated = DateTime.Now;

            ChimeraLogger.Log("HVAC",
                $"Zone {zone.ZoneName} targets updated: {targetTemp}°F, {targetHumidity}% RH, {targetCO2} PPM CO2", this);
        }

        /// <summary>
        /// Gets zone info for UI display.
        /// </summary>
        public HVACZone GetZone(string zoneId)
        {
            return _zones.TryGetValue(zoneId, out var zone) ? zone : null;
        }

        /// <summary>
        /// Gets all zones.
        /// </summary>
        public List<HVACZone> GetAllZones()
        {
            return _zones.Values.ToList();
        }

        #endregion

        #region Equipment Management

        /// <summary>
        /// Adds HVAC equipment to a zone (AC, heater, humidifier, etc.).
        /// GAMEPLAY: Player places equipment in construction mode, assigns to zone.
        /// </summary>
        public bool AddEquipment(string equipmentId, string zoneId, HVACEquipmentType type,
            float capacity, Vector3 location, float powerLoadAmps)
        {
            if (!_zones.ContainsKey(zoneId))
            {
                ChimeraLogger.LogWarning("HVAC",
                    $"Cannot add equipment {equipmentId}: Zone {zoneId} not found", this);
                return false;
            }

            if (_equipment.ContainsKey(equipmentId))
            {
                ChimeraLogger.LogWarning("HVAC",
                    $"Equipment {equipmentId} already exists", this);
                return false;
            }

            // Check electrical power available
            if (_electricalSystem != null && !_electricalSystem.IsDevicePowered(equipmentId))
            {
                ChimeraLogger.LogWarning("HVAC",
                    $"⚠️ Equipment {equipmentId} requires electrical connection first!", this);
            }

            var equipment = new HVACEquipment
            {
                EquipmentId = equipmentId,
                ZoneId = zoneId,
                Type = type,
                Capacity = capacity,
                Location = location,
                PowerLoadAmps = powerLoadAmps,
                IsActive = true,
                CurrentLoad = 0f
            };

            _equipment[equipmentId] = equipment;

            // Add to zone
            var zone = _zones[zoneId];
            zone.ZoneEquipment.Add(new ActiveHVACEquipment
            {
                EquipmentId = equipmentId,
                EquipmentType = type.ToString(),
                IsActive = true,
                PowerConsumption = powerLoadAmps * 240f // Watts (240V * Amps)
            });

            OnEquipmentAdded?.Invoke(equipmentId);

            ChimeraLogger.Log("HVAC",
                $"Equipment {type} added to {zone.ZoneName}: {capacity} capacity, {powerLoadAmps}A load", this);

            return true;
        }

        /// <summary>
        /// Removes HVAC equipment.
        /// GAMEPLAY: Player demolishes equipment.
        /// </summary>
        public bool RemoveEquipment(string equipmentId)
        {
            if (!_equipment.TryGetValue(equipmentId, out var equipment))
                return false;

            // Remove from zone
            if (_zones.TryGetValue(equipment.ZoneId, out var zone))
            {
                zone.ZoneEquipment.RemoveAll(e => e.EquipmentId == equipmentId);
            }

            _equipment.Remove(equipmentId);

            ChimeraLogger.Log("HVAC",
                $"Equipment {equipmentId} removed", this);

            return true;
        }

        /// <summary>
        /// Gets equipment info.
        /// </summary>
        public HVACEquipment GetEquipment(string equipmentId)
        {
            return _equipment.TryGetValue(equipmentId, out var equipment) ? equipment : default;
        }

        #endregion

        #region Climate Simulation

        /// <summary>
        /// Updates all zones' climate conditions.
        /// Simplified simulation for Phase 1 - equipment adjusts conditions toward targets.
        /// </summary>
        private void UpdateAllZones()
        {
            foreach (var zone in _zones.Values)
            {
                UpdateZoneClimate(zone);
            }
        }

        /// <summary>
        /// Updates a single zone's climate using HVACClimateHelpers.
        /// </summary>
        private void UpdateZoneClimate(HVACZone zone)
        {
            HVACClimateHelpers.UpdateZoneClimate(
                zone, _temperatureChangeRate, _humidityChangeRate,
                _updateIntervalSeconds, OnClimateAlert);

            OnZoneUpdated?.Invoke(zone.ZoneId, zone.CurrentConditions);
        }

        /// <summary>
        /// Calculates VPD (Vapor Pressure Deficit) for a zone using HVACClimateHelpers.
        /// </summary>
        public float CalculateVPD(string zoneId)
        {
            if (!_zones.TryGetValue(zoneId, out var zone))
                return 0f;

            return HVACClimateHelpers.CalculateVPD(
                zone.CurrentConditions.Temperature,
                zone.CurrentConditions.Humidity);
        }

        #endregion

        #region Query Methods

        /// <summary>
        /// Gets zone climate summary for UI using HVACClimateHelpers.
        /// </summary>
        public ZoneClimateSummary GetZoneSummary(string zoneId)
        {
            if (!_zones.TryGetValue(zoneId, out var zone))
                return default;

            return HVACClimateHelpers.GenerateZoneSummary(zone);
        }

        /// <summary>
        /// Gets HVAC system statistics.
        /// </summary>
        public HVACStats GetStatistics()
        {
            return new HVACStats
            {
                TotalZones = _zones.Count,
                ActiveZones = _zones.Count(z => z.Value.IsActive),
                TotalEquipment = _equipment.Count,
                ActiveEquipment = _equipment.Count(e => e.Value.IsActive),
                OptimalZones = _zones.Count(z => HVACClimateHelpers.IsZoneOptimal(z.Value)),
                TotalPowerLoad = _equipment.Values.Sum(e => e.PowerLoadAmps)
            };
        }

        #endregion
    }

    #region Data Structures

    [Serializable]
    public struct HVACEquipment
    {
        public string EquipmentId;
        public string ZoneId;
        public HVACEquipmentType Type;
        public float Capacity;
        public Vector3 Location;
        public float PowerLoadAmps;
        public bool IsActive;
        public float CurrentLoad;
    }

    [Serializable]
    public enum HVACEquipmentType
    {
        AirConditioner,
        Heater,
        Humidifier,
        Dehumidifier,
        ExhaustFan,
        IntakeFan,
        CirculationFan,
        CO2Generator
    }

    [Serializable]
    public struct ZoneClimateSummary
    {
        public string ZoneId;
        public string ZoneName;
        public float CurrentTemp;
        public float TargetTemp;
        public float CurrentHumidity;
        public float TargetHumidity;
        public float CurrentCO2;
        public float TargetCO2;
        public float VPD;
        public HVACZoneStatus Status;
        public int EquipmentCount;
        public bool IsOptimal;
    }

    [Serializable]
    public struct HVACStats
    {
        public int TotalZones;
        public int ActiveZones;
        public int TotalEquipment;
        public int ActiveEquipment;
        public int OptimalZones;
        public float TotalPowerLoad;
    }

    [Serializable]
    public enum VPDOptimizationStatus
    {
        Optimal,
        Acceptable,
        TooLow,
        TooHigh
    }

    #endregion
}
