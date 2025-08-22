using UnityEngine;
using System;
using System.Collections.Generic;
using ProjectChimera.Data.Environment;
using ProjectChimera.Data.Shared;

namespace ProjectChimera.Data.Facilities
{
    /// <summary>
    /// Core facility data structures for cultivation facility management.
    /// Supports complex construction, equipment systems, and utility networks.
    /// </summary>
    
    /// <summary>
    /// Complete facility instance with all rooms and infrastructure.
    /// </summary>
    [System.Serializable]
    public class FacilityInstance
    {
        public string FacilityId;
        public string FacilityName;
        public FacilityType FacilityType;
        public Vector3 FacilitySize;
        public List<CultivationRoom> Rooms = new List<CultivationRoom>();
        public List<UtilityConnection> UtilityConnections = new List<UtilityConnection>();
        public FacilityInfrastructure Infrastructure;
        public float ConstructionProgress = 0f;
        public bool IsOperational = false;
        public DateTime ConstructionStarted;
        public DateTime LastInspection;
        public FacilityLicense License;
    }
    
    /// <summary>
    /// Individual cultivation room with environmental controls and equipment.
    /// </summary>
    [System.Serializable]
    public class CultivationRoom
    {
        public string RoomId;
        public string RoomName;
        public RoomType RoomType;
        public Vector3 Position;
        public Vector3 Size;
        public List<EquipmentInstance> Equipment = new List<EquipmentInstance>();
        public RoomEnvironmentController EnvironmentController;
        public List<PlantPosition> PlantPositions = new List<PlantPosition>();
        public ConstructionStatus ConstructionStatus;
        public SecurityLevel SecurityLevel;
        public bool IsClimateControlled = true;
        public float PowerConsumption;
        public string AssignedEnvironmentId;
    }
    
    /// <summary>
    /// Equipment instance with operational status and configuration.
    /// </summary>
    [System.Serializable]
    public class EquipmentInstance
    {
        public string EquipmentId;
        public string EquipmentName;
        public FacilityEquipmentDataSO EquipmentData;
        public Vector3 Position;
        public Vector3 Rotation;
        public EquipmentStatus Status;
        public float OperationalLevel = 1f;
        public float EfficiencyRating = 1f;
        public DateTime InstallationDate;
        public DateTime LastMaintenance;
        public float PowerConsumption;
        public List<string> ConnectedUtilities = new List<string>();
        public EquipmentSchedule Schedule;
        public Dictionary<string, float> RuntimeParameters = new Dictionary<string, float>();
    }
    
    /// <summary>
    /// Room environment controller for managing climate systems.
    /// </summary>
    [System.Serializable]
    public class RoomEnvironmentController
    {
        public string ControllerId;
        public bool AutomationEnabled = true;
        public EnvironmentalTargets Targets;
        public List<EnvironmentSensor> Sensors = new List<EnvironmentSensor>();
        public List<string> ControlledEquipmentIds = new List<string>();
        public ControllerStatus Status;
        public float ResponseTime = 30f;
        public bool EmergencyShutdownEnabled = true;
    }
    
    /// <summary>
    /// Plant position within cultivation room.
    /// </summary>
    [System.Serializable]
    public class PlantPosition
    {
        public string PositionId;
        public Vector3 Position;
        public bool IsOccupied = false;
        public string PlantInstanceId;
        public GrowingMediumType GrowingMedium;
        public float SpaceAllocation = 1f;
        public string ContainerType;
        public bool HasIrrigation = false;
        public bool HasDrainage = false;
    }
    
    /// <summary>
    /// Facility infrastructure systems.
    /// </summary>
    [System.Serializable]
    public class FacilityInfrastructure
    {
        public ElectricalSystem ElectricalSystem;
        public PlumbingSystem PlumbingSystem;
        public VentilationSystem VentilationSystem;
        public SecuritySystem SecuritySystem;
        public FireSafety FireSafety;
        public WasteManagement WasteManagement;
        public List<EmergencySystem> EmergencySystems = new List<EmergencySystem>();
    }
    
    /// <summary>
    /// Electrical system infrastructure.
    /// </summary>
    [System.Serializable]
    public class ElectricalSystem
    {
        public float TotalCapacity;         // kW
        public float CurrentLoad;           // kW
        public List<CircuitBreaker> Circuits = new List<CircuitBreaker>();
        public List<PowerOutlet> PowerOutlets = new List<PowerOutlet>();
        public BackupPowerSystem BackupPower;
        public bool HasGroundFaultProtection = true;
        public float PowerFactor = 0.95f;
        public List<EnergyMeter> EnergyMeters = new List<EnergyMeter>();
    }
    
    /// <summary>
    /// Plumbing and irrigation infrastructure.
    /// </summary>
    [System.Serializable]
    public class PlumbingSystem
    {
        public WaterSupply WaterSupply;
        public DrainageSystem DrainageSystem;
        public IrrigationNetwork IrrigationNetwork;
        public List<WaterFilter> WaterFilters = new List<WaterFilter>();
        public List<FlowMeter> FlowMeters = new List<FlowMeter>();
        public bool HasBackflowPrevention = true;
        public float WaterPressure = 50f;   // PSI
        public WaterQualityMonitoring WaterQuality;
    }
    
    /// <summary>
    /// HVAC and ventilation infrastructure.
    /// </summary>
    [System.Serializable]
    public class VentilationSystem
    {
        public List<AirHandler> AirHandlers = new List<AirHandler>();
        public List<ExhaustFan> ExhaustFans = new List<ExhaustFan>();
        public List<IntakeFan> IntakeFans = new List<IntakeFan>();
        public DuctworkNetwork Ductwork;
        public AirFiltrationSystem AirFiltration;
        public float AirExchangeRate = 6f;  // Changes per hour
        public bool HasHeatRecovery = false;
        public TemperatureZoning TemperatureZoning;
    }
    
    /// <summary>
    /// Utility connection for infrastructure services.
    /// </summary>
    [System.Serializable]
    public class UtilityConnection
    {
        public string ConnectionId;
        public UtilityType UtilityType;
        public string ProviderId;
        public float Capacity;
        public float CurrentUsage;
        public bool IsActive = true;
        public float MonthlyCost;
        public DateTime ConnectionDate;
        public MaintenanceSchedule MaintenanceSchedule;
    }
    
    /// <summary>
    /// Environmental targets for automation.
    /// </summary>
    [System.Serializable]
    public class EnvironmentalTargets
    {
        public Vector2 TemperatureRange = new Vector2(20f, 26f);
        public Vector2 HumidityRange = new Vector2(45f, 65f);
        public Vector2 CO2Range = new Vector2(800f, 1200f);
        public Vector2 LightIntensityRange = new Vector2(400f, 800f);
        public Vector2 AirVelocityRange = new Vector2(0.2f, 0.5f);
        public Vector2 VPDRange = new Vector2(0.8f, 1.2f);
        public LightSchedule LightSchedule;
        public bool EnableAdaptiveControl = true;
    }
    
    /// <summary>
    /// Environmental sensor data.
    /// </summary>
    [System.Serializable]
    public class EnvironmentSensor
    {
        public string SensorId;
        public string SensorName;
        public SensorType SensorType;
        public Vector3 Position;
        public float CurrentReading;
        public float Accuracy = 0.95f;
        public DateTime LastReading;
        public bool IsOnline = true;
        public float CalibrationOffset = 0f;
        public List<float> ReadingHistory = new List<float>();
    }
    
    /// <summary>
    /// Equipment operational schedule.
    /// </summary>
    [System.Serializable]
    public class EquipmentSchedule
    {
        public List<ScheduleEntry> Schedule = new List<ScheduleEntry>();
        public bool EnableSchedule = false;
        public ScheduleType ScheduleType;
        public string TimeZone = "UTC";
    }
    
    /// <summary>
    /// Individual schedule entry.
    /// </summary>
    [System.Serializable]
    public class ScheduleEntry
    {
        public TimeSpan StartTime;
        public TimeSpan EndTime;
        public List<DayOfWeek> ActiveDays = new List<DayOfWeek>();
        public float PowerLevel = 1f;
        public Dictionary<string, float> Parameters = new Dictionary<string, float>();
        public bool IsEnabled = true;
    }
    
    /// <summary>
    /// Facility license and compliance information.
    /// </summary>
    [System.Serializable]
    public class FacilityLicense
    {
        public string LicenseNumber;
        public LicenseType LicenseType;
        public DateTime IssueDate;
        public DateTime ExpirationDate;
        public string IssuingAuthority;
        public List<string> Endorsements = new List<string>();
        public ComplianceStatus ComplianceStatus;
        public List<Inspection> InspectionHistory = new List<Inspection>();
    }
    
    // Enums for facility management
    public enum FacilityType
    {
        IndoorCultivation,
        Greenhouse,
        OutdoorFarm,
        MixedFacility,
        ProcessingFacility,
        ResearchFacility,
        GrowRoom,
        ProcessingRoom,
        StorageRoom,
        UtilityRoom,
        Office
    }
    
    public enum RoomType
    {
        Propagation,
        Vegetative,
        Flowering,
        Drying,
        Curing,
        Trimming,
        Storage,
        Processing,
        Laboratory,
        Office,
        Maintenance,
        Security
    }
    
    public enum EquipmentStatus
    {
        Offline,
        Online,
        Maintenance,
        Error,
        Calibrating,
        Standby
    }
    
    public enum ConstructionStatus
    {
        Planned,
        UnderConstruction,
        Completed,
        Operational,
        UnderRenovation,
        Decommissioned
    }
    
    public enum SecurityLevel
    {
        Public,
        Restricted,
        Secured,
        HighSecurity,
        Maximum
    }
    
    public enum ControllerStatus
    {
        Inactive,
        Active,
        Emergency,
        Maintenance,
        Error
    }
    
    public enum GrowingMediumType
    {
        Soil,
        Coco,
        Rockwool,
        Perlite,
        Hydroton,
        DWC,
        NFT,
        Aeroponics
    }
    
    public enum UtilityType
    {
        Electricity,
        Water,
        NaturalGas,
        Internet,
        Telephone,
        Sewer,
        HVAC,
        Security
    }
    
    public enum SensorType
    {
        Temperature,
        Humidity,
        CO2,
        LightIntensity,
        AirVelocity,
        pH,
        EC,
        WaterLevel,
        Pressure,
        Motion,
        Smoke,
        Intrusion
    }
    
    public enum ScheduleType
    {
        Daily,
        Weekly,
        Monthly,
        Seasonal,
        Custom
    }
    
    public enum LicenseType
    {
        Cultivation,
        Processing,
        Distribution,
        Retail,
        Research,
        Medical,
        Adult
    }
    
    public enum ComplianceStatus
    {
        Compliant,
        MinorViolation,
        MajorViolation,
        Suspended,
        Revoked
    }
    
    // Additional supporting classes (placeholder implementations)
    [System.Serializable]
    public class CircuitBreaker
    {
        public string Id;
        public float Rating;
        public bool IsTripped = false;
    }
    
    [System.Serializable]
    public class PowerOutlet
    {
        public string Id;
        public Vector3 Position;
        public float Voltage = 120f;
    }
    
    [System.Serializable]
    public class BackupPowerSystem
    {
        public string SystemType = "UPS";
        public float Capacity;
        public float Runtime;
    }
    
    [System.Serializable]
    public class EnergyMeter
    {
        public string Id;
        public float CurrentUsage;
        public float TotalUsage;
    }
    
    [System.Serializable]
    public class WaterSupply
    {
        public string Source = "Municipal";
        public float Capacity;
        public float Pressure;
    }
    
    [System.Serializable]
    public class DrainageSystem
    {
        public string Type = "Gravity";
        public float Capacity;
    }
    
    [System.Serializable]
    public class IrrigationNetwork
    {
        public string Type = "Drip";
        public int Zones;
    }
    
    [System.Serializable]
    public class WaterFilter
    {
        public string Type;
        public float Rating;
    }
    
    [System.Serializable]
    public class FlowMeter
    {
        public string Id;
        public float CurrentFlow;
    }
    
    [System.Serializable]
    public class WaterQualityMonitoring
    {
        public float pH = 7f;
        public float EC = 1.2f;
        public float TDS = 800f;
    }
    
    [System.Serializable]
    public class AirHandler
    {
        public string Id;
        public float Capacity;
    }
    
    [System.Serializable]
    public class ExhaustFan
    {
        public string Id;
        public float CFM;
    }
    
    [System.Serializable]
    public class IntakeFan
    {
        public string Id;
        public float CFM;
    }
    
    [System.Serializable]
    public class DuctworkNetwork
    {
        public float TotalLength;
        public string Material = "Galvanized Steel";
    }
    
    [System.Serializable]
    public class AirFiltrationSystem
    {
        public string FilterType = "HEPA";
        public float Efficiency = 0.995f;
    }
    
    [System.Serializable]
    public class TemperatureZoning
    {
        public int NumberOfZones = 1;
        public bool IndividualControl = false;
    }
    
    [System.Serializable]
    public class MaintenanceSchedule
    {
        public string EquipmentId;
        public List<MaintenanceTask> Tasks = new List<MaintenanceTask>();
    }
    
    [System.Serializable]
    public class MaintenanceTask
    {
        public string TaskName;
        public TimeSpan Interval;
        public DateTime LastCompleted;
    }
    
    [System.Serializable]
    public class LightSchedule
    {
        public string ScheduleName = "Default Schedule";
        public TimeSpan LightsOn = TimeSpan.FromHours(6);
        public TimeSpan LightsOff = TimeSpan.FromHours(18);
        public float LightPeriodHours = 12f;
        public float DarkPeriodHours = 12f;
        public TimeSpan LightStartTime = TimeSpan.FromHours(6);
        public float IntensityRampTime = 30f;
        public bool IsActive = true;
        public bool AutoAdjust = false;
        
        // Compatibility properties for OnTime/OffTime access
        public TimeSpan OnTime => LightsOn;
        public TimeSpan OffTime => LightsOff;
    }
    
    [System.Serializable]
    public class SecuritySystem
    {
        public bool HasCameras = true;
        public bool HasAlarms = true;
        public bool HasAccessControl = true;
    }
    
    [System.Serializable]
    public class FireSafety
    {
        public bool HasSprinklers = true;
        public bool HasAlarms = true;
        public bool HasExtinguishers = true;
    }
    
    [System.Serializable]
    public class WasteManagement
    {
        public string DisposalMethod = "Licensed Hauler";
        public bool HasComposting = false;
    }
    
    [System.Serializable]
    public class EmergencySystem
    {
        public string SystemType;
        public bool IsActive = true;
    }
    
    [System.Serializable]
    public class Inspection
    {
        public DateTime InspectionDate;
        public string InspectorName;
        public string Result;
        public List<string> Violations = new List<string>();
    }
}
