using UnityEngine;
using System;
using System.Collections.Generic;

namespace ProjectChimera.Data.Save
{
    /// <summary>
    /// Comprehensive save data structures for Project Chimera.
    /// Defines serializable data transfer objects (DTOs) for all game systems,
    /// enabling efficient and reliable save/load operations while maintaining
    /// data integrity and version compatibility.
    /// </summary>
    
    /// <summary>
    /// Main save game data container
    /// </summary>
    [System.Serializable]
    public class SaveGameData
    {
        [Header("Save Meta Information")]
        public string SlotName;
        public string Description;
        public DateTime SaveTimestamp;
        public string GameVersion;
        public string SaveSystemVersion;
        public TimeSpan PlayTime;
        
        [Header("New DTO-Based System Data")]
        public FacilityStateDTO FacilityData;
        public ConstructionStateDTO ConstructionData;
        public CultivationStateDTO PlantsData;
        public EconomyStateDTO EconomyStateData;
        public ProgressionStateDTO ProgressionStateData;
        public UIStateDTO UIData;
        public ContractsStateDTO ContractsData;
        
        [Header("Legacy Game Data - Deprecated")]
        public PlayerSaveData PlayerData;
        public CultivationSaveData CultivationData;
        public EconomySaveData EconomyData;
        public EnvironmentSaveData EnvironmentData;
        public ProgressionSaveData ProgressionData;
        public ObjectiveSaveData ObjectiveData;
        public EventSaveData EventData;
        public GameSettingsSaveData SettingsData;
        
        [Header("Additional Data")]
        public Dictionary<string, object> CustomData = new Dictionary<string, object>();
        public byte[] Checksum; // For data integrity verification
    }
    
    /// <summary>
    /// Version management data structures
    /// </summary>
    [System.Serializable]
    public class SaveVersionInfo
    {
        public string Version;
        public DateTime IntroducedDate;
        public string Description;
        public List<string> MajorChanges = new List<string>();
        public bool IsBreakingChange = false;
    }
    
    [System.Serializable]
    public class SaveVersionHistoryWrapper
    {
        public List<SaveVersionInfo> VersionHistory = new List<SaveVersionInfo>();
    }
    
    [System.Serializable]
    public class SaveVersionMetadata
    {
        public string SlotName;
        public string Version;
        public string GameVersion;
        public DateTime CreatedDate;
        public string Description;
        public Dictionary<string, object> Metadata = new Dictionary<string, object>();
    }
    
    [System.Serializable]
    public class VersionMigrationResult
    {
        public bool Success;
        public string ErrorMessage;
        public SaveGameData MigratedData;
        public bool RequiredMigration = false;
        public string OriginalVersion;
        public string TargetVersion;
    }

    /// <summary>
    /// Data structure for offline progression processing
    /// </summary>
    [System.Serializable]
    public class OfflineProgressionData
    {
        public float OfflineHours;
        public DateTime ProcessingTime;
        public Dictionary<string, OfflineProgressionResult> SystemResults = new Dictionary<string, OfflineProgressionResult>();
    }

    /// <summary>
    /// Result of offline progression processing for a specific system
    /// </summary>
    [System.Serializable]
    public class OfflineProgressionResult
    {
        public string SystemName;
        public bool Success;
        public float ProcessedHours;
        public string Description;
        public string ErrorMessage;
        public Dictionary<string, object> ResultData = new Dictionary<string, object>();
    }
    
    /// <summary>
    /// Player profile and progression data
    /// </summary>
    [System.Serializable]
    public class PlayerSaveData
    {
        public string PlayerId;
        public string PlayerName;
        public int Level;
        public float Experience;
        public int SkillPoints;
        public float Currency;
        public float Reputation;
        public TimeSpan TotalPlayTime;
        public DateTime CreationDate;
        public DateTime LastPlayDate;
        public Dictionary<string, object> PlayerPreferences = new Dictionary<string, object>();
        public List<string> UnlockedAchievements = new List<string>();
        public PlayerStatsSaveData Statistics;
    }
    
    /// <summary>
    /// Player statistics tracking
    /// </summary>
    [System.Serializable]
    public class PlayerStatsSaveData
    {
        public int TotalPlantsGrown;
        public int TotalHarvests;
        public float TotalYieldHarvested;
        public float TotalRevenue;
        public int TotalTrades;
        public int EventsCompleted;
        public int ObjectivesCompleted;
        public float HighestQualityAchieved;
        public TimeSpan LongestPlaySession;
        public DateTime FirstPlantGrown;
        public DateTime FirstHarvest;
    }
    
    /// <summary>
    /// Cultivation system save data
    /// </summary>
    [System.Serializable]
    public class CultivationSaveData
    {
        public List<PlantSaveData> ActivePlants = new List<PlantSaveData>();
        public List<HarvestSaveData> CompletedHarvests = new List<HarvestSaveData>();
        public List<string> UnlockedStrains = new List<string>();
        public List<GeneticCrossSaveData> BreedingHistory = new List<GeneticCrossSaveData>();
        public PlantStatisticsSaveData PlantStatistics;
        public CultivationSettingsSaveData Settings;
    }
    
    /// <summary>
    /// Individual plant save data
    /// </summary>
    [System.Serializable]
    public class PlantSaveData
    {
        public string PlantId;
        public string PlantName;
        public string StrainId;
        public Vector3 Position;
        public string GrowthStage;
        public float Health;
        public float Age;
        public float StressLevel;
        public float GrowthProgress;
        public Dictionary<string, float> GeneticTraits = new Dictionary<string, float>();
        public List<string> AppliedStressors = new List<string>();
        public EnvironmentalConditionsSaveData EnvironmentalHistory;
        public DateTime PlantedDate;
        public DateTime LastCareDate;
        public string FacilityLocation;
    }
    
    /// <summary>
    /// Harvest record save data
    /// </summary>
    [System.Serializable]
    public class HarvestSaveData
    {
        public string HarvestId;
        public string PlantId;
        public string StrainName;
        public DateTime HarvestDate;
        public float YieldGrams;
        public float QualityScore;
        public float PotencyTHC;
        public float PotencyCBD;
        public List<string> TerpeneProfile = new List<string>();
        public Dictionary<string, float> CannabinoidsProfile = new Dictionary<string, float>();
        public string ProcessingMethod;
        public float MarketValue;
        public bool WasSold;
        public DateTime SoldDate;
        public float SoldPrice;
    }
    
    /// <summary>
    /// Genetic breeding history
    /// </summary>
    [System.Serializable]
    public class GeneticCrossSaveData
    {
        public string CrossId;
        public string Parent1StrainId;
        public string Parent2StrainId;
        public string OffspringStrainId;
        public DateTime CrossDate;
        public List<string> InheritedTraits = new List<string>();
        public List<string> NewMutations = new List<string>();
        public int GenerationNumber;
        public bool IsStable;
        public float SuccessRate;
    }
    
    /// <summary>
    /// Plant cultivation statistics
    /// </summary>
    [System.Serializable]
    public class PlantStatisticsSaveData
    {
        public int TotalPlantsGrown;
        public int TotalHarvests;
        public float AverageYield;
        public float BestQuality;
        public float AverageGrowthTime;
        public int PlantsLost;
        public Dictionary<string, int> StrainCounts = new Dictionary<string, int>();
        public Dictionary<string, float> StrainYields = new Dictionary<string, float>();
        public Dictionary<string, int> CauseOfDeathStats = new Dictionary<string, int>();
    }
    
    /// <summary>
    /// Cultivation system settings
    /// </summary>
    [System.Serializable]
    public class CultivationSettingsSaveData
    {
        public bool AutoWatering;
        public bool AutoNutrients;
        public bool AutoHarvest;
        public float GlobalGrowthModifier;
        public bool RealisticGrowthCycles;
        public bool EnvironmentalStress;
        public Dictionary<string, float> AutomationThresholds = new Dictionary<string, float>();
    }
    
    /// <summary>
    /// Economic system save data
    /// </summary>
    [System.Serializable]
    public class EconomySaveData
    {
        public float CurrentCurrency;
        public float TotalRevenue;
        public float TotalExpenses;
        public List<MarketTransactionSaveData> MarketHistory = new List<MarketTransactionSaveData>();
        public List<InvestmentSaveData> Investments = new List<InvestmentSaveData>();
        public List<ContractSaveData> Contracts = new List<ContractSaveData>();
        public EconomicStatisticsSaveData Statistics;
        public MarketConditionsSaveData CurrentMarketConditions;
    }
    
    /// <summary>
    /// Market transaction record
    /// </summary>
    [System.Serializable]
    public class MarketTransactionSaveData
    {
        public string TransactionId;
        public DateTime TransactionDate;
        public string TransactionType; // Buy, Sell, Trade
        public string ItemType;
        public string ItemId;
        public float Quantity;
        public float UnitPrice;
        public float TotalValue;
        public string TradingPartner;
        public float QualityRating;
        public bool WasProfitable;
        public float ProfitMargin;
    }
    
    /// <summary>
    /// Investment save data
    /// </summary>
    [System.Serializable]
    public class InvestmentSaveData
    {
        public string InvestmentId;
        public string InvestmentType;
        public float InitialInvestment;
        public float CurrentValue;
        public DateTime StartDate;
        public DateTime MaturityDate;
        public float ExpectedReturn;
        public float ActualReturn;
        public string RiskLevel;
        public bool IsActive;
    }
    
    /// <summary>
    /// Contract save data
    /// </summary>
    [System.Serializable]
    public class ContractSaveData
    {
        public string ContractId;
        public string ContractType;
        public string ClientName;
        public DateTime StartDate;
        public DateTime EndDate;
        public Dictionary<string, object> Terms = new Dictionary<string, object>();
        public float ContractValue;
        public float CompletionPercentage;
        public bool IsActive;
        public List<string> RequiredDeliverables = new List<string>();
        public List<string> CompletedDeliverables = new List<string>();
    }
    
    /// <summary>
    /// Economic statistics
    /// </summary>
    [System.Serializable]
    public class EconomicStatisticsSaveData
    {
        public float LifetimeRevenue;
        public float LifetimeExpenses;
        public float NetWorth;
        public int TotalTransactions;
        public float AverageTransactionValue;
        public float BestSingleSale;
        public float WorstLoss;
        public Dictionary<string, float> RevenueByCategory = new Dictionary<string, float>();
        public Dictionary<string, float> ExpensesByCategory = new Dictionary<string, float>();
    }
    
    /// <summary>
    /// Current market conditions
    /// </summary>
    [System.Serializable]
    public class MarketConditionsSaveData
    {
        public Dictionary<string, float> ItemPrices = new Dictionary<string, float>();
        public Dictionary<string, float> DemandLevels = new Dictionary<string, float>();
        public Dictionary<string, float> SupplyLevels = new Dictionary<string, float>();
        public Dictionary<string, float> PriceTrends = new Dictionary<string, float>();
        public string OverallMarketCondition; // Bull, Bear, Stable
        public List<string> ActiveMarketEvents = new List<string>();
    }
    
    /// <summary>
    /// Environmental and facility save data
    /// </summary>
    [System.Serializable]
    public class EnvironmentSaveData
    {
        public FacilitySaveData FacilityConfiguration;
        public List<EquipmentSaveData> EquipmentStates = new List<EquipmentSaveData>();
        public List<EnvironmentalReadingSaveData> EnvironmentalHistory = new List<EnvironmentalReadingSaveData>();
        public List<AutomationRuleSaveData> AutomationRules = new List<AutomationRuleSaveData>();
        public EnvironmentalStatisticsSaveData Statistics;
    }
    
    /// <summary>
    /// Facility configuration save data
    /// </summary>
    [System.Serializable]
    public class FacilitySaveData
    {
        public string FacilityId;
        public string FacilityName;
        public Vector3 FacilitySize;
        public List<RoomSaveData> Rooms = new List<RoomSaveData>();
        public string FacilityType;
        public DateTime ConstructionDate;
        public float TotalValue;
        public Dictionary<string, object> FacilitySettings = new Dictionary<string, object>();
    }
    
    /// <summary>
    /// Individual room save data
    /// </summary>
    [System.Serializable]
    public class RoomSaveData
    {
        public string RoomId;
        public string RoomName;
        public string RoomType;
        public Vector3 RoomSize;
        public Vector3 RoomPosition;
        public List<string> InstalledEquipment = new List<string>();
        public EnvironmentalConditionsSaveData CurrentConditions;
        public EnvironmentalConditionsSaveData TargetConditions;
        public int MaxPlantCapacity;
        public int CurrentPlantCount;
        public bool IsActive;
    }
    
    /// <summary>
    /// Equipment state save data
    /// </summary>
    [System.Serializable]
    public class EquipmentSaveData
    {
        public string EquipmentId;
        public string EquipmentName;
        public string EquipmentType;
        public string RoomId;
        public Vector3 Position;
        public bool IsActive;
        public bool IsOnline;
        public float EfficiencyRating;
        public float PowerConsumption;
        public Dictionary<string, object> Settings = new Dictionary<string, object>();
        public List<MaintenanceRecordSaveData> MaintenanceHistory = new List<MaintenanceRecordSaveData>();
        public DateTime InstallationDate;
        public DateTime LastMaintenanceDate;
        public float OperatingHours;
    }
    
    /// <summary>
    /// Environmental conditions save data
    /// </summary>
    [System.Serializable]
    public class EnvironmentalConditionsSaveData
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
    }
    
    /// <summary>
    /// Environmental reading history
    /// </summary>
    [System.Serializable]
    public class EnvironmentalReadingSaveData
    {
        public DateTime ReadingTime;
        public string RoomId;
        public EnvironmentalConditionsSaveData Conditions;
        public Dictionary<string, bool> AlertsTriggered = new Dictionary<string, bool>();
    }
    
    /// <summary>
    /// Automation rule save data
    /// </summary>
    [System.Serializable]
    public class AutomationRuleSaveData
    {
        public string RuleId;
        public string RuleName;
        public string RoomId;
        public string TriggerType;
        public Dictionary<string, object> TriggerConditions = new Dictionary<string, object>();
        public string ActionType;
        public Dictionary<string, object> ActionParameters = new Dictionary<string, object>();
        public bool IsActive;
        public int TimesTriggered;
        public DateTime LastTriggered;
        public DateTime CreatedDate;
    }
    
    /// <summary>
    /// Equipment maintenance record
    /// </summary>
    [System.Serializable]
    public class MaintenanceRecordSaveData
    {
        public DateTime MaintenanceDate;
        public string MaintenanceType;
        public string Description;
        public float Cost;
        public string TechnicianName;
        public bool WasSuccessful;
        public List<string> PartsReplaced = new List<string>();
        public float DowntimeHours;
    }
    
    /// <summary>
    /// Environmental statistics
    /// </summary>
    [System.Serializable]
    public class EnvironmentalStatisticsSaveData
    {
        public float AverageTemperature;
        public float AverageHumidity;
        public float TotalPowerConsumption;
        public float EquipmentUptime;
        public int MaintenanceEvents;
        public float EnvironmentalStability;
        public Dictionary<string, float> OptimalConditionsPercentage = new Dictionary<string, float>();
    }
    
    /// <summary>
    /// Player progression save data
    /// </summary>
    [System.Serializable]
    public class ProgressionSaveData
    {
        public int PlayerLevel;
        public float CurrentExperience;
        public int AvailableSkillPoints;
        public List<string> UnlockedSkills = new List<string>();
        public List<string> CompletedResearch = new List<string>();
        public List<string> AvailableResearch = new List<string>();
        public List<AchievementSaveData> Achievements = new List<AchievementSaveData>();
        public Dictionary<string, int> SkillLevels = new Dictionary<string, int>();
        public ProgressionStatisticsSaveData Statistics;
    }
    
    /// <summary>
    /// Achievement save data
    /// </summary>
    [System.Serializable]
    public class AchievementSaveData
    {
        public string AchievementId;
        public string AchievementName;
        public DateTime UnlockedDate;
        public float Progress;
        public bool IsCompleted;
        public Dictionary<string, object> AchievementData = new Dictionary<string, object>();
    }
    
    /// <summary>
    /// Progression statistics
    /// </summary>
    [System.Serializable]
    public class ProgressionStatisticsSaveData
    {
        public TimeSpan TotalExperienceGained;
        public int TotalSkillPointsEarned;
        public int TotalSkillPointsSpent;
        public int ResearchProjectsCompleted;
        public int AchievementsUnlocked;
        public DateTime FirstAchievement;
        public DateTime LastLevelUp;
        public Dictionary<string, DateTime> SkillUnlockDates = new Dictionary<string, DateTime>();
    }
    
    /// <summary>
    /// Objectives and challenges save data
    /// </summary>
    [System.Serializable]
    public class ObjectiveSaveData
    {
        public List<ObjectiveProgressSaveData> ActiveObjectives = new List<ObjectiveProgressSaveData>();
        public List<string> CompletedObjectives = new List<string>();
        public List<ChallengeSaveData> DailyChallenges = new List<ChallengeSaveData>();
        public List<ObjectiveHistorySaveData> ObjectiveHistory = new List<ObjectiveHistorySaveData>();
        public int TotalObjectivesCompleted;
        public int TotalChallengesCompleted;
        public DateTime LastChallengeRefresh;
        public ObjectiveStatisticsSaveData Statistics;
    }
    
    /// <summary>
    /// Active objective progress
    /// </summary>
    [System.Serializable]
    public class ObjectiveProgressSaveData
    {
        public string ObjectiveId;
        public string ObjectiveType;
        public float CurrentProgress;
        public float TargetProgress;
        public DateTime StartTime;
        public DateTime ExpirationTime;
        public string Difficulty;
        public bool IsCompleted;
    }
    
    /// <summary>
    /// Challenge save data
    /// </summary>
    [System.Serializable]
    public class ChallengeSaveData
    {
        public string ChallengeId;
        public string ChallengeType;
        public float CurrentProgress;
        public float TargetProgress;
        public DateTime StartDate;
        public bool IsCompleted;
        public List<string> RewardsEarned = new List<string>();
    }
    
    /// <summary>
    /// Objective completion history
    /// </summary>
    [System.Serializable]
    public class ObjectiveHistorySaveData
    {
        public string ObjectiveId;
        public DateTime CompletionDate;
        public TimeSpan CompletionTime;
        public string Difficulty;
        public List<string> RewardsReceived = new List<string>();
        public bool WasSkipped;
    }
    
    /// <summary>
    /// Objective system statistics
    /// </summary>
    [System.Serializable]
    public class ObjectiveStatisticsSaveData
    {
        public int TotalObjectivesStarted;
        public int TotalObjectivesCompleted;
        public int TotalObjectivesSkipped;
        public float AverageCompletionTime;
        public Dictionary<string, int> ObjectivesByDifficulty = new Dictionary<string, int>();
        public Dictionary<string, int> ObjectivesByCategory = new Dictionary<string, int>();
        public DateTime FirstObjectiveCompleted;
        public DateTime LastObjectiveCompleted;
    }
    
    /// <summary>
    /// Random events save data
    /// </summary>
    [System.Serializable]
    public class EventSaveData
    {
        public List<ActiveEventSaveData> ActiveEvents = new List<ActiveEventSaveData>();
        public List<EventHistorySaveData> EventHistory = new List<EventHistorySaveData>();
        public Dictionary<string, string> PlayerChoices = new Dictionary<string, string>();
        public EventStatisticsSaveData EventStatistics;
        public float PlayerReputation;
        public DateTime LastEventTime;
    }
    
    /// <summary>
    /// Active event save data
    /// </summary>
    [System.Serializable]
    public class ActiveEventSaveData
    {
        public string EventId;
        public string EventType;
        public DateTime StartTime;
        public DateTime ExpirationTime;
        public bool HasTimeLimit;
        public Dictionary<string, object> EventData = new Dictionary<string, object>();
        public List<string> AvailableChoices = new List<string>();
    }
    
    /// <summary>
    /// Event completion history
    /// </summary>
    [System.Serializable]
    public class EventHistorySaveData
    {
        public string EventId;
        public DateTime EventDate;
        public string EventType;
        public string ChoiceMade;
        public List<string> ConsequencesApplied = new List<string>();
        public float ReputationChange;
        public bool WasSuccessful;
    }
    
    /// <summary>
    /// Event system statistics
    /// </summary>
    [System.Serializable]
    public class EventStatisticsSaveData
    {
        public int TotalEventsTriggered;
        public int TotalEventsCompleted;
        public int TotalEventsIgnored;
        public float PlayerReputation;
        public Dictionary<string, int> EventsByCategory = new Dictionary<string, int>();
        public Dictionary<string, int> ChoicesByType = new Dictionary<string, int>();
        public float AverageDecisionTime;
        public DateTime FirstEvent;
        public DateTime LastEvent;
    }
    
    /// <summary>
    /// Game settings save data
    /// </summary>
    [System.Serializable]
    public class GameSettingsSaveData
    {
        public Dictionary<string, object> GameplaySettings = new Dictionary<string, object>();
        public Dictionary<string, object> UISettings = new Dictionary<string, object>();
        public Dictionary<string, object> AudioSettings = new Dictionary<string, object>();
        public Dictionary<string, object> GraphicsSettings = new Dictionary<string, object>();
        public Dictionary<string, object> AccessibilitySettings = new Dictionary<string, object>();
        public string Language;
        public bool FirstTimePlaying;
        public DateTime SettingsLastModified;
    }
    
    // Save system operation results
    
    /// <summary>
    /// Result of a save operation
    /// </summary>
    [System.Serializable]
    public class SaveResult
    {
        public bool Success;
        public string SlotName;
        public DateTime SaveTime;
        public long FileSizeBytes;
        public string ErrorMessage;
        public TimeSpan SaveDuration;
    }
    
    /// <summary>
    /// Result of a load operation
    /// </summary>
    [System.Serializable]
    public class LoadResult
    {
        public bool Success;
        public DateTime LoadTime;
        public SaveGameData GameData;
        public string ErrorMessage;
        public TimeSpan LoadDuration;
        public bool RequiredMigration;
    }
    
    /// <summary>
    /// Result of loading save data from file
    /// </summary>
    [System.Serializable]
    public class SaveDataResult
    {
        public bool Success;
        public SaveGameData GameData;
        public string ErrorMessage;
    }
    
    /// <summary>
    /// Data validation result
    /// </summary>
    [System.Serializable]
    public class ValidationResult
    {
        public bool IsValid;
        public string ErrorMessage;
        public List<string> Warnings = new List<string>();
        public float CorruptionPercentage;
    }
    
    /// <summary>
    /// Save slot information for UI display
    /// </summary>
    [System.Serializable]
    public class SaveSlotData
    {
        public string SlotName;
        public string Description;
        public DateTime LastSaveTime;
        public TimeSpan PlayTime;
        public int PlayerLevel;
        public int TotalPlants;
        public float Currency;
        public bool IsAutoSave;
        public long FileSizeBytes;
        public bool IsCorrupted;
    }
    
    /// <summary>
    /// Detailed save slot information
    /// </summary>
    [System.Serializable]
    public class SaveSlotInfo
    {
        public SaveSlotData SlotData;
        public long FileSizeBytes;
        public bool IsCorrupted;
        public bool HasBackup;
        public bool CanLoad;
        public Texture2D Screenshot;
        public ValidationResult ValidationStatus;
    }
    
    /// <summary>
    /// Save system configuration
    /// </summary>
    [System.Serializable]
    public class SaveSystemConfig
    {
        public string SystemVersion;
        public int MaxSaveSlots;
        public bool EnableAutoSave;
        public float AutoSaveInterval;
        public bool EnableCompression;
        public bool EnableEncryption;
        public bool EnableCloudSync;
        public string EncryptionKey;
        public int CompressionLevel;
    }
    
    /// <summary>
    /// Save system performance metrics
    /// </summary>
    [System.Serializable]
    public class SaveMetrics
    {
        public int TotalSaves;
        public int TotalLoads;
        public int FailedSaves;
        public int FailedLoads;
        public DateTime LastSaveTime;
        public DateTime LastLoadTime;
        public TimeSpan AverageSaveTime;
        public TimeSpan AverageLoadTime;
        public long TotalDataSaved;
        public long TotalDataLoaded;
        public int AutoSavesPerformed;
        public int BackupsCreated;
    }
    
    // Enums for save system
    
    /// <summary>
    /// Types of save operations
    /// </summary>
    public enum SaveType
    {
        Manual,
        Auto,
        Quick,
        Checkpoint,
        Emergency
    }
    
    /// <summary>
    /// Save file format types
    /// </summary>
    public enum SaveFormat
    {
        Binary,
        JSON,
        XML,
        Compressed,
        Encrypted
    }
    
    /// <summary>
    /// Data corruption levels
    /// </summary>
    public enum CorruptionLevel
    {
        None,
        Minor,
        Moderate,
        Severe,
        Total
    }
    
    /// <summary>
    /// Save system error types
    /// </summary>
    public enum SaveErrorType
    {
        None,
        FileNotFound,
        AccessDenied,
        InsufficientSpace,
        CorruptedData,
        VersionMismatch,
        SerializationError,
        UnknownError
    }
}