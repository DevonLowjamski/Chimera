using System;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Data.Equipment;
using ProjectChimera.Data.Shared;

namespace ProjectChimera.Data.Simulation.HVAC
{
    /// <summary>
    /// Miscellaneous HVAC data structures
    /// Types that didn't fit into other categories
    /// </summary>



    public enum CollaborativeSessionType
    {
        Research,
        Development,
        Troubleshooting,
        Training
    }



    public enum EnvironmentalSkillLevel
    {
        Novice,
        Apprentice,
        Adept,
        Expert,
        Master
    }



    public class EnvironmentalInnovation
    {
        public string InnovationId;
        public string Title;
        public string Description;
        public System.DateTime DiscoveryDate;
        public string ZoneId;
        public InnovationType InnovationType;
    }



    public enum InnovationType
    {
        TechnologyIntegration,
        ProcessImprovement,
        EnergySaving,
        Sustainability
    }

    // HVACCertificationLevel enum moved to HVACCoreData.cs
    // PlantGrowthStage enum should be defined in ProjectChimera.Data.Shared



    [System.Serializable]
    public class PIDControllerSettings
    {
        [Range(0.1f, 2f)] public float TemperatureKp = 0.8f;
        [Range(0.01f, 0.5f)] public float TemperatureKi = 0.1f;
        [Range(0.01f, 0.2f)] public float TemperatureKd = 0.05f;
        
        [Range(0.1f, 2f)] public float HumidityKp = 0.6f;
        [Range(0.01f, 0.5f)] public float HumidityKi = 0.08f;
        [Range(0.01f, 0.2f)] public float HumidityKd = 0.03f;
        
        [Range(0.1f, 2f)] public float AirflowKp = 0.9f;
        [Range(0.01f, 0.5f)] public float AirflowKi = 0.12f;
        [Range(0.01f, 0.2f)] public float AirflowKd = 0.04f;
    }



    [System.Serializable]
    public class VPDOptimizationSettings
    {
        [Range(0.4f, 1.6f)] public float TargetVPD = 1.0f;
        [Range(0.1f, 0.5f)] public float VPDTolerance = 0.2f;
        public bool EnableDynamicVPD = true;
        public bool EnableGrowthStageAdjustment = true;
        public List<VPDSchedulePoint> VPDSchedule = new List<VPDSchedulePoint>();
    }



    [System.Serializable]
    public class VPDSchedulePoint
    {
        [Range(0, 24)] public int Hour;
        [Range(0.4f, 1.6f)] public float TargetVPD;
        public PlantGrowthStage GrowthStage;
    }



    [System.Serializable]
    public class ActiveHVACEquipment
    {
        public string EquipmentId;
        public EquipmentDataSO EquipmentData;
        public string ZoneId;
        public Vector3 Position;
        public HVACEquipmentStatus Status;
        [Range(0f, 1f)] public float PowerLevel;
        public float OperatingHours;
        [Range(0.1f, 1.5f)] public float EfficiencyRating = 1f;
        public MaintenanceStatus MaintenanceStatus;
        public System.DateTime InstallationDate;
        public System.DateTime LastMaintenanceDate;
        public List<HVACAlarm> EquipmentAlarms = new List<HVACAlarm>();
    }



    [System.Serializable]
    public class HVACControlParameters
    {
        public bool TemperatureControlEnabled = true;
        public bool HumidityControlEnabled = true;
        public bool AirflowControlEnabled = true;
        public bool CO2ControlEnabled = false;
        public bool VPDOptimizationEnabled = false;
        public VPDOptimizationSettings VPDSettings;
        public ControlStrategy ControlStrategy = ControlStrategy.PID;
        [Range(0.1f, 10f)] public float ControlGain = 1f;
        public bool EnableNightMode = true;
        public NightModeSettings NightModeSettings;
        public HVACOperationMode OperationMode = HVACOperationMode.Auto;
    }



    [System.Serializable]
    public class NightModeSettings
    {
        [Range(0, 23)] public int NightStartHour = 22;
        [Range(1, 23)] public int NightEndHour = 6;
        [Range(-10f, 0f)] public float TemperatureOffset = -2f;
        [Range(-20f, 0f)] public float HumidityOffset = -5f;
        [Range(0f, 1f)] public float AirflowReduction = 0.3f;
    }



    [System.Serializable]
    public class HVACControlLoop
    {
        public string ControlId;
        public string ZoneId;
        public HVACControlType ControlType;
        public PIDController PIDController;
        public bool IsActive;
        public float SetPoint;
        public float CurrentValue;
        public float ControlOutput;
        public float Tolerance;
        public ControlMode ControlMode = ControlMode.Automatic;
        public System.DateTime LastUpdate;
    }



    [System.Serializable]
    public class HVACAlarm
    {
        public string AlarmId;
        public string ZoneId;
        public string EquipmentId;
        public HVACAlarmType AlarmType;
        public HVACAlarmPriority Priority;
        public HVACAlarmStatus AlarmStatus;
        public string AlarmMessage;
        public float AlarmValue;
        public float ThresholdValue;
        public System.DateTime TriggerTime;
        public System.DateTime? AcknowledgeTime;
        public System.DateTime? ClearTime;
        public bool RequiresManualReset;
    }



    [System.Serializable]
    public class HVACMaintenanceSchedule
    {
        public string ScheduleId;
        public string EquipmentId;
        public MaintenanceType MaintenanceType;
        public System.DateTime ScheduledDate;
        public int EstimatedDuration; // Minutes
        public float MaintenanceCost;
        public MaintenancePriority Priority;
        public MaintenanceScheduleStatus Status;
        public string MaintenanceNotes;
        public List<string> RequiredParts = new List<string>();
        public List<string> RequiredSkills = new List<string>();
    }



    [System.Serializable]
    public class HVACEquipmentSnapshot
    {
        public string EquipmentId;
        public string EquipmentName;
        public HVACEquipmentStatus Status;
        public float PowerLevel;
        public float EfficiencyRating;
        public float EnergyConsumption;
        public MaintenanceStatus MaintenanceStatus;
    }



    [System.Serializable]
    public class HVACControlPerformance
    {
        public float TemperatureAccuracy;
        public float HumidityAccuracy;
        public float AirflowAccuracy;
        public float OverallPerformance;
        public int ControlLoopErrors;
        public System.TimeSpan ResponseTime;
    }



    [System.Serializable]
    public class EquipmentOptimization
    {
        public string EquipmentId;
        public float CurrentPowerLevel;
        public float OptimalPowerLevel;
        public float EnergySavings;
        public string OptimizationReason;
    }



    public class EnvironmentalPrediction
    {
        public float PredictionTime; // Minutes ahead
        public bool RequiresAction;
        public List<string> RecommendedActions = new List<string>();
        public float ConfidenceLevel;
        public EnvironmentalConditions PredictedConditions;
    }


    public class HeatingEquipmentSO : EquipmentDataSO
    {
        [Header("Heating Specifications")]
        [Range(1000f, 50000f)] public float HeatingCapacity = 5000f; // Watts
        [Range(0.8f, 1.2f)] public float EfficiencyRating = 0.95f;
        [Range(15f, 35f)] public float MinOperatingTemperature = 15f;
        [Range(15f, 35f)] public float MaxOperatingTemperature = 35f;
        public HeatingMethod HeatingMethod = HeatingMethod.Electric;
        public bool SupportsModulation = true;
    }


    public class CoolingEquipmentSO : EquipmentDataSO
    {
        [Header("Cooling Specifications")]
        [Range(1000f, 50000f)] public float CoolingCapacity = 5000f; // Watts
        [Range(2f, 6f)] public float COP = 3.5f; // Coefficient of Performance
        [Range(15f, 35f)] public float MinOperatingTemperature = 18f;
        [Range(15f, 35f)] public float MaxOperatingTemperature = 30f;
        public CoolingMethod CoolingMethod = CoolingMethod.AirConditioner;
        public bool SupportsVariableSpeed = true;
    }


    public class HumidificationEquipmentSO : EquipmentDataSO
    {
        [Header("Humidification Specifications")]
        [Range(1f, 50f)] public float HumidificationRate = 5f; // Liters per hour
        [Range(20f, 80f)] public float MinHumidity = 30f;
        [Range(20f, 80f)] public float MaxHumidity = 70f;
        public HumidificationMethod HumidificationMethod = HumidificationMethod.Ultrasonic;
        public bool RequiresWaterConnection = true;
    }


    public class DehumidificationEquipmentSO : EquipmentDataSO
    {
        [Header("Dehumidification Specifications")]
        [Range(1f, 50f)] public float DehumidificationRate = 3f; // Liters per hour
        [Range(20f, 80f)] public float OperatingHumidityRange = 60f;
        public DehumidificationMethod DehumidificationMethod = DehumidificationMethod.Refrigeration;
        public bool RequiresDrainConnection = true;
    }


    public class FanEquipmentSO : EquipmentDataSO
    {
        [Header("Fan Specifications")]
        [Range(100f, 10000f)] public float AirflowRate = 1000f; // CFM
        [Range(0.1f, 2f)] public float MaxAirVelocity = 1.5f;
        [Range(20f, 80f)] public float NoiseLevel = 40f; // dBA
        public FanType FanType = FanType.Axial;
        public bool SupportsVariableSpeed = true;
    }


    public class VentilationEquipmentSO : EquipmentDataSO
    {
        [Header("Ventilation Specifications")]
        [Range(100f, 10000f)] public float VentilationRate = 500f; // CFM
        [Range(0.5f, 10f)] public float AirChangesPerHour = 4f;
        public VentilationType VentilationType = VentilationType.Exhaust;
        public bool SupportsHeatRecovery = false;
    }



    public enum HVACControlType
    {
        Temperature,
        Humidity,
        Airflow,
        CO2,
        VPD
    }



    public enum ControlStrategy
    {
        OnOff,
        PID,
        Fuzzy,
        Model_Predictive,
        Adaptive
    }



    public enum HVACAlarmType
    {
        Temperature_High,
        Temperature_Low,
        Humidity_High,
        Humidity_Low,
        Equipment_Fault,
        Communication_Loss,
        Energy_Efficiency_Low,
        Maintenance_Required,
        Filter_Replacement
    }



    public enum HVACOperationMode
    {
        Off,
        Manual,
        Auto,
        Eco,
        Emergency
    }



    public enum HVACAlarmPriority
    {
        Low,
        Medium,
        High,
        Critical
    }



    public enum MaintenanceType
    {
        Routine,
        Preventive,
        Corrective,
        Emergency,
        Filter_Change,
        Calibration,
        Cleaning
    }



    public enum MaintenancePriority
    {
        Low,
        Normal,
        High,
        Critical
    }



    public enum ZonePriority
    {
        Low,
        Normal,
        High,
        Critical
    }



    public enum HeatingMethod
    {
        Electric,
        Gas,
        Heat_Pump,
        Radiant,
        Steam,
        Hot_Water
    }



    public enum CoolingMethod
    {
        AirConditioner,
        Evaporative,
        Chilled_Water,
        Heat_Pump,
        Refrigeration
    }



    public enum HumidificationMethod
    {
        Steam,
        Ultrasonic,
        Evaporative,
        Atomizing,
        Wetted_Media
    }



    public enum DehumidificationMethod
    {
        Refrigeration,
        Desiccant,
        Membrane,
        Condensation
    }



    public enum FanType
    {
        Axial,
        Centrifugal,
        Mixed_Flow,
        Cross_Flow,
        Inline
    }



    public enum VentilationType
    {
        Supply,
        Exhaust,
        Mixed,
        Heat_Recovery
    }


    
    [System.Serializable]
    public class ProfessionalNetworkingPlatform
    {
        private Dictionary<string, List<NetworkingConnection>> _networkConnections = new Dictionary<string, List<NetworkingConnection>>();
        
        public void Initialize(bool enableNetworking)
        {
            if (!enableNetworking) return;
            
            SetupNetworkingPlatform();
        }
        
        private void SetupNetworkingPlatform()
        {
            // Initialize networking platform
        }
    }


    
    [System.Serializable]
    public class CareerPathwayManager
    {
        private Dictionary<string, CareerPathway> _playerPathways = new Dictionary<string, CareerPathway>();
        
        public void Initialize(bool enableCareerPathways)
        {
            if (!enableCareerPathways) return;
            
            InitializeCareerPathways();
        }
        
        private void InitializeCareerPathways()
        {
            // Initialize career pathway options
        }
    }


    
    [System.Serializable]
    public class EnvironmentalKnowledgeNetwork
    {
        private List<EnvironmentalKnowledge> _sharedKnowledge = new List<EnvironmentalKnowledge>();
        
        public void Initialize(bool enableKnowledgeSharing)
        {
            if (!enableKnowledgeSharing) return;
            
            SetupKnowledgeNetwork();
        }
        
        public string ShareKnowledge(EnvironmentalKnowledge knowledge)
        {
            knowledge.KnowledgeId = System.Guid.NewGuid().ToString();
            knowledge.SharedDate = System.DateTime.Now;
            
            _sharedKnowledge.Add(knowledge);
            
            return knowledge.KnowledgeId;
        }
        
        private void SetupKnowledgeNetwork()
        {
            // Initialize knowledge sharing network
        }
    }


    
    [System.Serializable]
    public class HVACCertification
    {
        public string CertificationId;
        public HVACCertificationLevel Level;
        public string PlayerId;
        public string Title;
        public string Description;
        public System.DateTime CompletionDate;
        public float Score;
        public bool IsValid;
    }


    
    [System.Serializable]
    public class ProfessionalActivity
    {
        public string ActivityId;
        public string ActivityName;
        public ProfessionalActivityType ActivityType;
        public System.DateTime CompletedDate;
        public float DurationHours;
        public List<string> SkillsApplied = new List<string>();
    }


    
    public enum ProfessionalActivityType
    {
        Training,
        ProjectWork,
        Certification,
        Networking,
        Research,
        Innovation
    }


    
    [System.Serializable]
    public class ProfessionalInterests
    {
        public List<string> Industries = new List<string>();
        public List<string> SkillAreas = new List<string>();
        public List<string> CareerGoals = new List<string>();
        public ExperienceLevel PreferredLevel;
    }


    
    public enum ExperienceLevel
    {
        Entry,
        Intermediate,
        Senior,
        Expert,
        Executive
    }


    
    [System.Serializable]
    public class IndustryConnectionResult
    {
        public bool Success;
        public List<IndustryConnection> NewConnections = new List<IndustryConnection>();
        public int TotalConnections;
        public string Message;
    }


    
    [System.Serializable]
    public class IndustryConnection
    {
        public string ConnectionId;
        public string PlayerId;
        public string ProfessionalId;
        public System.DateTime ConnectionDate;
        public ConnectionStatus Status;
    }


    
    [System.Serializable]
    public class IndustryProfessional
    {
        public string ProfessionalId;
        public string Name;
        public string Industry;
        public List<string> Expertise = new List<string>();
        public ExperienceLevel Level;
        public bool IsAvailable;
    }


    
    [System.Serializable]
    public class NetworkingConnection
    {
        public string ConnectionId;
        public string PlayerId;
        public string ConnectedPlayerId;
        public System.DateTime ConnectionDate;
        public ConnectionType Type;
    }


    
    public enum ConnectionType
    {
        Peer,
        Mentor,
        Mentee,
        Colleague,
        Professional
    }


    
    [System.Serializable]
    public class CareerPathway
    {
        public string PathwayId;
        public string Title;
        public List<string> RequiredSkills = new List<string>();
        public List<string> OptionalSkills = new List<string>();
        public List<HVACCertificationLevel> RequiredCertifications = new List<HVACCertificationLevel>();
        public int EstimatedDurationMonths;
    }


    
    public enum PredictionTimeframe
    {
        NextHour,
        Next4Hours,
        Next12Hours,
        NextDay,
        NextWeek
    }


    
    [System.Serializable]
    public class OptimizationRecommendation
    {
        public string RecommendationId;
        public string Title;
        public string Description;
        public RecommendationPriority Priority;
        public float EstimatedSavings;
        public System.TimeSpan ImplementationTime;
        public float ImplementationCost;
    }


    
    public enum RecommendationPriority
    {
        Low,
        Medium,
        High,
        Critical
    }


    
    [System.Serializable]
    public class OptimizationObjectives
    {
        public bool MinimizeEnergyConsumption;
        public bool MaximizeComfort;
        public bool OptimizeAirQuality;
        public bool ReduceOperatingCosts;
        public float EnergyEfficiencyTarget;
        public float ComfortTargetScore;
    }


    
    [System.Serializable]
    public class EnvironmentalOptimizationResult
    {
        public string ZoneId;
        public float OptimizationScore;
        public float EnergyEfficiencyGain;
        public List<OptimizationRecommendation> Recommendations = new List<OptimizationRecommendation>();
        public System.DateTime OptimizationDate;
        public string Summary;
        
        // Properties that were referenced in the AtmosphericPhysicsDataStructures.cs but missing here
        public bool IsValid = true;
        public float ImprovementScore = 0f;
    }


    
    [System.Serializable]
    public class EnvironmentalKnowledge
    {
        public string KnowledgeId;
        public string Title;
        public string Description;
        public string Content;
        public string AuthorId;
        public System.DateTime SharedDate;
        public List<string> Tags = new List<string>();
        public int LikesCount;
        public int ViewsCount;
    }


    
    [System.Serializable]
    public class EnvironmentalCompetition
    {
        public string CompetitionId;
        public string Title;
        public string Description;
        public System.DateTime StartDate;
        public System.DateTime EndDate;
        public CompetitionType Type;
        public List<string> Rules = new List<string>();
        public List<CompetitionReward> Rewards = new List<CompetitionReward>();
    }


    
    public enum CompetitionType
    {
        EnergyEfficiency,
        InnovationChallenge,
        OptimizationRace,
        CollaborativeProject,
        KnowledgeSharing
    }


    
    [System.Serializable]
    public class CompetitionReward
    {
        public string RewardId;
        public string Title;
        public string Description;
        public RewardType Type;
        public float Value;
    }


    
    public enum RewardType
    {
        Points,
        Badge,
        Certification,
        Recognition,
        PrizeMoney
    }


    
    [System.Serializable]
    public class CollaborativeEnvironmentalConfig
    {
        public string SessionName;
        public string Description;
        public int MaxParticipants;
        public List<string> ResearchGoals = new List<string>();
        public CollaborationType Type;
        public System.TimeSpan Duration;
    }


    
    public enum CollaborationType
    {
        Research,
        Innovation,
        ProblemSolving,
        KnowledgeSharing,
        CompetitiveChallenge
    }


    
    [System.Serializable]
    public class CollaborativeSession
    {
        public string SessionId;
        public string SessionName;
        public string ProjectName; // Added missing ProjectName property
        public CollaborativeSessionType Type; // Added missing Type property
        public string Description;
        public int MaxParticipants;
        public System.DateTime StartTime;
        public SessionStatus Status;
        public List<string> Participants = new List<string>();
        public List<string> ResearchGoals = new List<string>();
        public float ProgressPercentage;
    }



    [System.Serializable]
    public class PlayerEnvironmentalProfile
    {
        public string PlayerId;
        public float EnvironmentalScore;
        public List<string> Achievements = new List<string>();
        public DateTime ProfileCreated;
        public int OptimizationLevel;
        public float SustainabilityRating;
        
        // Missing properties referenced in EnhancedEnvironmentalGamingManager.cs
        public string PlayerName;
        public EnvironmentalSkillLevel SkillLevel;
        public float ExperiencePoints;
        public List<string> CompletedChallenges = new List<string>();
        public List<string> ActiveCertifications = new List<string>();
        public List<string> Innovations = new List<string>();
    }
}
