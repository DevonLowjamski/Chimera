using UnityEngine;
using System;
using System.Collections.Generic;

namespace ProjectChimera.Data.Save
{
    /// <summary>
    /// Data Transfer Objects for Construction System Save/Load Operations
    /// These DTOs capture the state of the grid-based construction system,
    /// including rooms, equipment, utilities, and construction projects.
    /// </summary>
    
    /// <summary>
    /// Main construction state DTO for the entire construction system
    /// </summary>
    [System.Serializable]
    public class ConstructionStateDTO
    {
        [Header("Grid System State")]
        public GridSystemStateDTO GridState;
        
        [Header("Active Construction Projects")]
        public List<ConstructionProjectDTO> ActiveProjects = new List<ConstructionProjectDTO>();
        public List<ConstructionProjectDTO> CompletedProjects = new List<ConstructionProjectDTO>();
        public Dictionary<string, Vector3Int> ReservedPositions = new Dictionary<string, Vector3Int>();
        
        /// <summary>
        /// Backward compatibility for 2D reserved positions (deprecated)
        /// </summary>
        [System.Obsolete("Use ReservedPositions for 3D coordinates")]
        public Dictionary<string, Vector2Int> ReservedPositions2D = new Dictionary<string, Vector2Int>();
        
        [Header("Placed Objects")]
        public List<PlacedObjectDTO> PlacedObjects = new List<PlacedObjectDTO>();
        public List<RoomDTO> Rooms = new List<RoomDTO>();
        public List<UtilitySystemDTO> UtilitySystems = new List<UtilitySystemDTO>();
        
        [Header("Construction Metrics")]
        public ConstructionMetricsDTO Metrics;
        public ConstructionCostSummaryDTO CostSummary;
        
        [Header("System Configuration")]
        public bool EnableConstructionSystem = true;
        public int MaxBuildingLimit = 100;
        public bool EnableCostTracking = true;
        public bool EnableResourceTracking = true;
        public bool EnableProgressTracking = true;
        public float AutoSaveInterval = 300f;
        
        [Header("Grid System")]
        public GridSystemStateDTO GridSystem;
        
        [Header("Save Metadata")]
        public DateTime SaveTimestamp;
        public string SaveVersion = "1.0";
    }
    
    /// <summary>
    /// DTO for grid system state and configuration
    /// </summary>
    [System.Serializable]
    public class GridSystemStateDTO
    {
        [Header("Grid Configuration")]
        public int GridSizeX = 100;
        public int GridSizeY = 100;
        public float CellSize = 1.0f;
        public Vector3 GridOffset = Vector3.zero;
        public bool IsGridVisible = false;
        public Vector3 GridOrigin;
        public Vector2 GridDimensions;
        public float GridSize = 1.0f;
        public float GridHeight = 0.01f;
        
        [Header("Grid Settings")]
        public bool SnapToGrid = true;
        public bool ShowGrid = true;
        public Color GridColor = new Color(0.5f, 0.5f, 0.5f, 0.3f);
        public bool ShowGridLines = true;
        public bool ShowGridBounds = true;
        public float PlacementTolerance = 0.1f;
        public bool ValidatePlacement = true;
        public bool PreventOverlap = true;
        
        [Header("Grid Cells State")]
        public List<GridCellDTO> GridCells = new List<GridCellDTO>();
        public int TotalCells;
        public int OccupiedCells;
        public int AvailableCells;
        
        [Header("Grid Statistics")]
        public DateTime LastUpdated;
        public float GridUtilization; // Percentage of cells occupied
    }
    
    /// <summary>
    /// DTO for individual grid cell state
    /// </summary>
    [System.Serializable]
    public class GridCellDTO
    {
        public Vector3Int GridCoordinate;
        
        /// <summary>
        /// Backward compatibility for 2D grid coordinates (deprecated)
        /// </summary>
        [System.Obsolete("Use GridCoordinate for 3D coordinates")]
        public Vector2Int GridCoordinate2D 
        { 
            get => new Vector2Int(GridCoordinate.x, GridCoordinate.y); 
            set => GridCoordinate = new Vector3Int(value.x, value.y, 0); 
        }
        public Vector3 WorldPosition;
        public bool IsOccupied;
        public string OccupyingObjectId;
        public string CellType; // "Standard", "Foundation", "Utility", "Restricted"
        public bool IsValid;
        public Dictionary<string, object> CellProperties = new Dictionary<string, object>();
    }
    
    /// <summary>
    /// DTO for construction projects
    /// </summary>
    [System.Serializable]
    public class ConstructionProjectDTO
    {
        [Header("Project Identity")]
        public string ProjectId;
        public string ProjectName;
        public string TemplateName;
        public string ProjectType; // "Construction", "Renovation", "Demolition"
        
        [Header("Project Location")]
        public Vector3Int GridCoordinate;
        public Vector3 WorldPosition;
        public int Rotation;
        public Vector3Int GridSize;
        
        /// <summary>
        /// Backward compatibility for 2D coordinates (deprecated)
        /// </summary>
        [System.Obsolete("Use GridCoordinate for 3D coordinates")]
        public Vector2Int GridCoordinate2D 
        { 
            get => new Vector2Int(GridCoordinate.x, GridCoordinate.y); 
            set => GridCoordinate = new Vector3Int(value.x, value.y, 0); 
        }
        
        /// <summary>
        /// Backward compatibility for 2D grid size (deprecated)
        /// </summary>
        [System.Obsolete("Use GridSize for 3D coordinates")]
        public Vector2Int GridSize2D 
        { 
            get => new Vector2Int(GridSize.x, GridSize.y); 
            set => GridSize = new Vector3Int(value.x, value.y, 1); 
        }
        
        [Header("Project Status")]
        public string Status; // "Planning", "InProgress", "Completed", "Cancelled", "OnHold"
        public float Progress;
        public DateTime StartTime;
        public DateTime EstimatedCompletion;
        public DateTime ActualCompletion;
        
        [Header("Project Resources")]
        public List<ConstructionResourceDTO> RequiredResources = new List<ConstructionResourceDTO>();
        public List<ConstructionResourceDTO> AllocatedResources = new List<ConstructionResourceDTO>();
        public List<string> AssignedWorkers = new List<string>();
        
        [Header("Project Costs")]
        public float EstimatedCost;
        public float ActualCost;
        public string BudgetId;
        public List<ConstructionCostRecordDTO> CostRecords = new List<ConstructionCostRecordDTO>();
        
        [Header("Project Template Data")]
        public ConstructionTemplateDTO TemplateData;
        
        [Header("Project Events")]
        public List<ConstructionEventDTO> ProjectEvents = new List<ConstructionEventDTO>();
    }
    
    /// <summary>
    /// DTO for construction template information
    /// </summary>
    [System.Serializable]
    public class ConstructionTemplateDTO
    {
        public string TemplateName;
        public string Description;
        public string Category; // "Structure", "Room", "Equipment", "Utility"
        public Vector3Int GridSize;
        
        /// <summary>
        /// Backward compatibility for 2D grid size (deprecated)
        /// </summary>
        [System.Obsolete("Use GridSize for 3D coordinates")]
        public Vector2Int GridSize2D 
        { 
            get => new Vector2Int(GridSize.x, GridSize.y); 
            set => GridSize = new Vector3Int(value.x, value.y, 1); 
        }
        public bool CanBeRotated;
        public float PlacementHeight;
        public float ConstructionTime;
        public int RequiredSkillLevel;
        public List<string> RequiredUnlocks = new List<string>();
        public float BaseCost;
        public float MaintenanceCost;
        public float OperationalValue;
        public bool RequiresFoundation;
        public bool RequiresUtilities;
        public bool AllowsOverlap;
        public List<string> CanConnectTo = new List<string>();
    }
    
    /// <summary>
    /// DTO for construction resources
    /// </summary>
    [System.Serializable]
    public class ConstructionResourceDTO
    {
        public string ResourceName;
        public int Quantity;
        public float UnitCost;
        public float TotalCost;
        public string ResourceType; // "Material", "Labor", "Equipment", "Service"
        public bool IsAvailable;
        public DateTime LastUpdated;
    }
    
    /// <summary>
    /// DTO for placed objects in the construction system
    /// </summary>
    [System.Serializable]
    public class PlacedObjectDTO
    {
        [Header("Object Identity")]
        public string ObjectId;
        public string ObjectName;
        public string ObjectType; // "Structure", "Equipment", "Decoration", "Utility"
        public string TemplateName;
        public string PrefabName;
        
        [Header("Object Placement")]
        public Vector3 Position;
        public Quaternion Rotation;
        public Vector3 Scale = Vector3.one;
        public Vector3Int GridCoordinate;
        public Vector3 WorldPosition;
        public Vector3Int GridSize;
        
        /// <summary>
        /// Backward compatibility for 2D coordinates (deprecated)
        /// </summary>
        [System.Obsolete("Use GridCoordinate for 3D coordinates")]
        public Vector2Int GridCoordinate2D 
        { 
            get => new Vector2Int(GridCoordinate.x, GridCoordinate.y); 
            set => GridCoordinate = new Vector3Int(value.x, value.y, 0); 
        }
        
        /// <summary>
        /// Backward compatibility for 2D grid size (deprecated)
        /// </summary>
        [System.Obsolete("Use GridSize for 3D coordinates")]
        public Vector2Int GridSize2D 
        { 
            get => new Vector2Int(GridSize.x, GridSize.y); 
            set => GridSize = new Vector3Int(value.x, value.y, 1); 
        }
        public bool IsSnappedToGrid;
        
        [Header("Object State")]
        public bool IsActive;
        public bool IsOperational;
        public float Health = 1.0f;
        public DateTime PlacementDate;
        public DateTime LastMaintenance;
        
        [Header("Object Properties")]
        public Dictionary<string, object> ObjectData = new Dictionary<string, object>();
        public Dictionary<string, object> ObjectProperties = new Dictionary<string, object>();
        public List<string> ConnectedUtilities = new List<string>();
        public List<string> ConnectedObjects = new List<string>();
        
        [Header("Equipment-Specific Data")]
        public EquipmentInstanceDTO EquipmentData; // Null if not equipment
    }
    
    /// <summary>
    /// DTO for equipment instances
    /// </summary>
    [System.Serializable]
    public class EquipmentInstanceDTO
    {
        [Header("Equipment Identity")]
        public string EquipmentId;
        public string EquipmentName;
        public string Category; // "Lighting", "HVAC", "Irrigation", "Processing"
        public string EquipmentType; // "LED_Light", "Exhaust_Fan", "Drip_System"
        public string Manufacturer;
        public string ModelNumber;
        
        [Header("Equipment State")]
        public string Status; // "Offline", "Online", "Maintenance", "Error"
        public float OperationalLevel = 1.0f;
        public float EfficiencyRating = 1.0f;
        public DateTime InstallationDate;
        public DateTime LastMaintenance;
        public float OperatingHours;
        
        [Header("Equipment Performance")]
        public float PowerConsumption;
        public float WaterConsumption;
        public float HeatGeneration;
        public float NoiseLevel;
        public Dictionary<string, float> RuntimeParameters = new Dictionary<string, float>();
        
        [Header("Equipment Schedule")]
        public EquipmentScheduleDTO Schedule;
        
        [Header("Equipment Maintenance")]
        public List<MaintenanceRecordDTO> MaintenanceHistory = new List<MaintenanceRecordDTO>();
        public float Reliability = 0.95f;
        public float NextMaintenanceDue; // Days until next maintenance
        
        [Header("Environmental Effects")]
        public List<EnvironmentalEffectDTO> EnvironmentalEffects = new List<EnvironmentalEffectDTO>();
        public float EffectiveRange = 2.0f;
    }
    
    /// <summary>
    /// DTO for equipment schedules
    /// </summary>
    [System.Serializable]
    public class EquipmentScheduleDTO
    {
        public bool EnableSchedule = false;
        public string ScheduleType; // "Daily", "Weekly", "Monthly", "Custom"
        public string TimeZone = "UTC";
        public List<ScheduleEntryDTO> ScheduleEntries = new List<ScheduleEntryDTO>();
    }
    
    /// <summary>
    /// DTO for schedule entries
    /// </summary>
    [System.Serializable]
    public class ScheduleEntryDTO
    {
        public TimeSpan StartTime;
        public TimeSpan EndTime;
        public List<DayOfWeek> ActiveDays = new List<DayOfWeek>();
        public float PowerLevel = 1.0f;
        public Dictionary<string, float> Parameters = new Dictionary<string, float>();
        public bool IsEnabled = true;
    }
    
    /// <summary>
    /// DTO for rooms in the facility
    /// </summary>
    [System.Serializable]
    public class RoomDTO
    {
        [Header("Room Identity")]
        public string RoomId;
        public string RoomName;
        public string RoomType; // "Propagation", "Vegetative", "Flowering", "Drying"
        public string FacilityId;
        
        [Header("Room Physical Properties")]
        public Vector3 Position;
        public Vector3 Size;
        public Vector3 RoomSize;
        public Vector3Int GridPosition;
        public Vector3Int GridSize;
        
        /// <summary>
        /// Backward compatibility for 2D coordinates (deprecated)
        /// </summary>
        [System.Obsolete("Use GridPosition for 3D coordinates")]
        public Vector2Int GridPosition2D 
        { 
            get => new Vector2Int(GridPosition.x, GridPosition.y); 
            set => GridPosition = new Vector3Int(value.x, value.y, 0); 
        }
        
        /// <summary>
        /// Backward compatibility for 2D grid size (deprecated)
        /// </summary>
        [System.Obsolete("Use GridSize for 3D coordinates")]
        public Vector2Int GridSize2D 
        { 
            get => new Vector2Int(GridSize.x, GridSize.y); 
            set => GridSize = new Vector3Int(value.x, value.y, 1); 
        }
        
        [Header("Room Configuration")]
        public List<string> InstalledEquipmentIds = new List<string>();
        public List<string> ConnectedUtilityIds = new List<string>();
        public int MaxPlantCapacity;
        public int MaxCapacity;
        public int CurrentPlantCount;
        public int CurrentOccupancy;
        public float PowerRequirement;
        
        [Header("Room Environment")]
        public EnvironmentalTargetsDTO EnvironmentalTargets;
        public ConstructionEnvironmentalConditionsDTO CurrentConditions;
        public ConstructionEnvironmentalConditionsDTO EnvironmentalConditions;
        public bool IsClimateControlled = true;
        public bool HasAutomation = false;
        
        [Header("Room Status")]
        public string ConstructionStatus; // "Planned", "UnderConstruction", "Completed"
        public string SecurityLevel; // "Public", "Restricted", "Secured"
        public bool IsActive = true;
        public bool IsOperational = false;
        public DateTime ConstructionDate;
        public DateTime LastInspection;
        
        [Header("Room Costs")]
        public float ConstructionCost;
        public float OperationalCost;
        public float MaintenanceCost;
        public float PowerConsumption;
    }
    
    /// <summary>
    /// DTO for utility systems
    /// </summary>
    [System.Serializable]
    public class UtilitySystemDTO
    {
        [Header("Utility Identity")]
        public string UtilityId;
        public string UtilityName;
        public string UtilityType; // "Electricity", "Water", "HVAC", "Internet"
        public string SystemType; // "Primary", "Secondary", "Backup"
        
        [Header("Utility Capacity")]
        public float TotalCapacity;
        public float CurrentLoad;
        public float MaxCapacity;
        public float UtilizationPercentage;
        
        [Header("Utility Network")]
        public List<UtilityConnectionDTO> Connections = new List<UtilityConnectionDTO>();
        public List<string> ConnectedRoomIds = new List<string>();
        public List<string> ConnectedEquipmentIds = new List<string>();
        
        [Header("Utility Status")]
        public bool IsActive = true;
        public bool IsOperational = true;
        public string Status; // "Normal", "Warning", "Critical", "Offline"
        public DateTime InstallationDate;
        public DateTime LastMaintenance;
        
        [Header("Utility Performance")]
        public float Efficiency = 1.0f;
        public float ReliabilityScore = 0.95f;
        public List<UtilityEventDTO> EventHistory = new List<UtilityEventDTO>();
        
        [Header("Utility Costs")]
        public float InstallationCost;
        public float OperationalCost;
        public float MaintenanceCost;
        public float MonthlyCost;
    }
    
    /// <summary>
    /// DTO for utility connections
    /// </summary>
    [System.Serializable]
    public class UtilityConnectionDTO
    {
        public string ConnectionId;
        public string FromUtilityId;
        public string ToObjectId; // Room or Equipment ID
        public string ConnectionType; // "Direct", "Switched", "Regulated"
        public float Capacity;
        public float CurrentUsage;
        public bool IsActive = true;
        public DateTime ConnectionDate;
        public Dictionary<string, object> ConnectionProperties = new Dictionary<string, object>();
    }
    
    /// <summary>
    /// DTO for environmental targets and conditions
    /// </summary>
    [System.Serializable]
    public class EnvironmentalTargetsDTO
    {
        public Vector2 TemperatureRange = new Vector2(20f, 26f);
        public Vector2 HumidityRange = new Vector2(45f, 65f);
        public Vector2 CO2Range = new Vector2(800f, 1200f);
        public Vector2 LightIntensityRange = new Vector2(400f, 800f);
        public Vector2 AirVelocityRange = new Vector2(0.2f, 0.5f);
        public Vector2 VPDRange = new Vector2(0.8f, 1.2f);
        public bool EnableAdaptiveControl = true;
        public Dictionary<string, Vector2> CustomTargets = new Dictionary<string, Vector2>();
    }
    
    /// <summary>
    /// DTO for current environmental conditions in construction/rooms
    /// </summary>
    [System.Serializable]
    public class ConstructionEnvironmentalConditionsDTO
    {
        public float Temperature;
        public float Humidity;
        public float CO2Level;
        public float LightIntensity;
        public float AirCirculation;
        public float pH;
        public float EC; // Electrical Conductivity
        public Dictionary<string, float> NutrientLevels = new Dictionary<string, float>();
        public DateTime ReadingTimestamp;
        public Dictionary<string, float> CustomReadings = new Dictionary<string, float>();
    }
    
    /// <summary>
    /// DTO for environmental effects
    /// </summary>
    [System.Serializable]
    public class EnvironmentalEffectDTO
    {
        public string EffectType; // "Temperature", "Humidity", "LightIntensity"
        public float EffectMagnitude;
        public string EffectDirection; // "Increase", "Decrease", "Maintain"
        public float EffectRange = 2.0f;
        public bool IsActive = true;
        public Dictionary<string, object> EffectParameters = new Dictionary<string, object>();
    }
    
    /// <summary>
    /// DTO for construction cost tracking
    /// </summary>
    [System.Serializable]
    public class ConstructionCostRecordDTO
    {
        public string RecordId;
        public string ProjectId;
        public string CostCategory; // "Labor", "Materials", "Equipment", "Permits"
        public float Amount;
        public string Description;
        public DateTime RecordDate;
        public string AuthorizedBy;
        public bool IsApproved = false;
    }
    
    /// <summary>
    /// DTO for construction events and history
    /// </summary>
    [System.Serializable]
    public class ConstructionEventDTO
    {
        public string EventId;
        public string EventType; // "ProjectStarted", "ProjectCompleted", "ObjectPlaced"
        public DateTime EventTime;
        public string ProjectId;
        public string ObjectId;
        public string Description;
        public Dictionary<string, object> EventData = new Dictionary<string, object>();
        public bool WasSuccessful = true;
        public string ErrorMessage;
    }
    
    /// <summary>
    /// DTO for utility events
    /// </summary>
    [System.Serializable]
    public class UtilityEventDTO
    {
        public string EventId;
        public string EventType; // "Connected", "Disconnected", "Overload", "Maintenance"
        public DateTime EventTime;
        public string UtilityId;
        public string Description;
        public string Severity; // "Info", "Warning", "Critical"
        public bool IsResolved = false;
        public Dictionary<string, object> EventData = new Dictionary<string, object>();
    }
    
    /// <summary>
    /// DTO for maintenance records
    /// </summary>
    [System.Serializable]
    public class MaintenanceRecordDTO
    {
        public string RecordId;
        public DateTime MaintenanceDate;
        public string MaintenanceType; // "Routine", "Preventive", "Corrective", "Emergency"
        public string Description;
        public float Cost;
        public string TechnicianName;
        public bool WasSuccessful = true;
        public List<string> PartsReplaced = new List<string>();
        public float DowntimeHours;
        public string NextMaintenanceScheduled;
    }
    
    /// <summary>
    /// DTO for construction metrics and statistics
    /// </summary>
    [System.Serializable]
    public class ConstructionMetricsDTO
    {
        [Header("Project Statistics")]
        public int TotalProjectsStarted;
        public int TotalProjectsCompleted;
        public int TotalProjectsCancelled;
        public int ActiveProjects;
        public float AverageProjectDuration; // Days
        public float ProjectSuccessRate;
        
        [Header("Cost Statistics")]
        public float TotalBudgetAllocated;
        public float TotalBudgetSpent;
        public float BudgetUtilization;
        public float AverageCostPerProject;
        public float TotalSavings;
        public int ProjectsOnBudget;
        public int ProjectsOverBudget;
        
        [Header("Object Placement")]
        public int TotalObjectsPlaced;
        public int TotalObjectsBuilt;
        public int TotalObjectsRemoved;
        public int TotalRoomsBuilt;
        public int ActiveObjects;
        public float GridUtilization;
        public float TotalConstructionCost;
        public DateTime LastConstructionDate;
        public float ConstructionExperience;
        
        [Header("Performance Metrics")]
        public float AverageConstructionTime;
        public float ConstructionEfficiency;
        public int EquipmentInstalled;
        public int RoomsConstructed;
        public int UtilitySystemsInstalled;
        
        [Header("Maintenance Statistics")]
        public int TotalMaintenanceEvents;
        public float AverageMaintenanceCost;
        public float EquipmentUptime;
        public int CriticalFailures;
        
        [Header("Temporal Data")]
        public DateTime LastUpdated;
        public DateTime FirstProject;
        public DateTime LastCompletedProject;
    }
    
    /// <summary>
    /// DTO for construction cost summary
    /// </summary>
    [System.Serializable]
    public class ConstructionCostSummaryDTO
    {
        [Header("Total Costs")]
        public float TotalConstructionCosts;
        public float TotalEquipmentCosts;
        public float TotalUtilityCosts;
        public float TotalMaintenanceCosts;
        public float TotalOperationalCosts;
        
        [Header("Cost Breakdown")]
        public Dictionary<string, float> CostsByCategory = new Dictionary<string, float>();
        public Dictionary<string, float> CostsByProject = new Dictionary<string, float>();
        public Dictionary<string, float> CostsByTimeperiod = new Dictionary<string, float>();
        
        [Header("Budget Performance")]
        public float TotalBudget;
        public float RemainingBudget;
        public float BudgetVariance;
        public bool IsOverBudget;
        
        [Header("Financial Metrics")]
        public float ROI; // Return on Investment
        public float CostPerSquareMeter;
        public float CostPerPlantCapacity;
        public DateTime LastCalculated;
    }
    
    /// <summary>
    /// Result DTO for construction save operations
    /// </summary>
    [System.Serializable]
    public class ConstructionSaveResult
    {
        public bool Success;
        public DateTime SaveTime;
        public string ErrorMessage;
        public long DataSizeBytes;
        public TimeSpan SaveDuration;
        public int ProjectsSaved;
        public int ObjectsSaved;
        public int RoomsSaved;
        public int UtilitySystemsSaved;
        public string SaveVersion;
    }
    
    /// <summary>
    /// Result DTO for construction load operations
    /// </summary>
    [System.Serializable]
    public class ConstructionLoadResult
    {
        public bool Success;
        public DateTime LoadTime;
        public string ErrorMessage;
        public TimeSpan LoadDuration;
        public int ProjectsLoaded;
        public int ObjectsLoaded;
        public int RoomsLoaded;
        public int UtilitySystemsLoaded;
        public bool RequiredMigration;
        public string LoadedVersion;
        public ConstructionStateDTO ConstructionState;
    }
    
    /// <summary>
    /// DTO for construction system validation
    /// </summary>
    [System.Serializable]
    public class ConstructionValidationResult
    {
        public bool IsValid;
        public DateTime ValidationTime;
        public List<string> Errors = new List<string>();
        public List<string> Warnings = new List<string>();
        
        [Header("Grid Validation")]
        public bool GridSystemValid;
        public bool GridCellsValid;
        public bool PlacementValid;
        
        [Header("Project Validation")]
        public bool ProjectsValid;
        public bool CostDataValid;
        public bool ResourcesValid;
        
        [Header("Object Validation")]
        public bool PlacedObjectsValid;
        public bool EquipmentValid;
        public bool RoomsValid;
        public bool UtilitiesValid;
        
        [Header("Data Integrity")]
        public int TotalProjects;
        public int ValidProjects;
        public int TotalObjects;
        public int ValidObjects;
        public float DataIntegrityScore;
    }
}