using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Data.Equipment;
using ProjectChimera.Data.Shared;

namespace ProjectChimera.Data.Simulation.HVAC
{
    /// <summary>
    /// HVAC Zone data structures
    /// Refactored from HVACDataStructures.cs for Single Responsibility Principle
    /// </summary>



    public class EnvironmentalZone
    {
        public string ZoneId;
        public EnvironmentalConditions CurrentConditions;
    }



    [System.Serializable]
    public class HVACZoneSettings
    {
        [Range(15f, 35f)] public float DefaultTemperature = 24f;
        [Range(20f, 80f)] public float DefaultHumidity = 55f;
        [Range(0.1f, 2f)] public float DefaultAirflow = 0.3f;
        [Range(300f, 1500f)] public float DefaultCO2Level = 800f;
        [Range(10f, 1000f)] public float ZoneVolume = 100f; // Cubic meters
        public bool EnableZoneIsolation = true;
        public bool EnableVPDControl = true;
        public ZonePriority ZonePriority = ZonePriority.Normal;
    }



    [System.Serializable]
    public class HVACZone
    {
        public string ZoneId;
        public string ZoneName;
        public HVACZoneSettings ZoneSettings;
        public EnvironmentalConditions CurrentConditions;
        public EnvironmentalConditions TargetConditions;
        public List<ActiveHVACEquipment> ZoneEquipment = new List<ActiveHVACEquipment>();
        public HVACControlParameters ControlParameters;
        public HVACZoneStatus ZoneStatus;
        public System.DateTime CreatedAt;
        public System.DateTime LastUpdated;
        
        // Compatibility properties for HVACSystemManager
        public bool IsActive 
        { 
            get => ZoneStatus == HVACZoneStatus.Active || ZoneStatus == HVACZoneStatus.Heating || ZoneStatus == HVACZoneStatus.Cooling; 
        }
        
        public HVACOperationMode OperationMode 
        { 
            get => ControlParameters?.OperationMode ?? HVACOperationMode.Manual; 
        }
    }



    [System.Serializable]
    public class HVACZoneSnapshot
    {
        public string ZoneId;
        public string ZoneName;
        public System.DateTime Timestamp;
        public EnvironmentalConditions CurrentConditions;
        public EnvironmentalConditions TargetConditions;
        public HVACZoneStatus ZoneStatus;
        public List<HVACEquipmentSnapshot> EquipmentStatus = new List<HVACEquipmentSnapshot>();
        public HVACControlPerformance ControlPerformance;
        public float EnergyEfficiency;
        public VPDOptimizationStatus VPDOptimal;
    }


    public enum HVACZoneStatus
    {
        Active,
        Standby,
        Heating,
        Cooling,
        Maintenance,
        Alarm,
        Offline
    }

    [System.Serializable]
    public enum ZonePriority
    {
        Low,
        Normal,
        High,
        Critical
    }

    [System.Serializable]
    public class ActiveHVACEquipment
    {
        public string EquipmentId;
        public string EquipmentType;
        public bool IsActive;
        public float PowerConsumption;
    }

    [System.Serializable]
    public class HVACControlParameters
    {
        public float TemperatureTolerance;
        public float HumidityTolerance;
        public bool AutoMode;
        public HVACOperationMode OperationMode = HVACOperationMode.Auto;
    }

    [System.Serializable]
    public enum HVACOperationMode
    {
        Auto,
        Manual,
        Cooling,
        Heating,
        Ventilation,
        Dehumidification
    }

    [System.Serializable]
    public class HVACEquipmentSnapshot
    {
        public string EquipmentId;
        public float CurrentLoad;
        public bool IsOperational;
        public DateTime LastMaintenance;
    }

    [System.Serializable]
    public class HVACControlPerformance
    {
        public float EfficiencyRating;
        public float ResponseTime;
        public int AdjustmentCount;
    }
}
