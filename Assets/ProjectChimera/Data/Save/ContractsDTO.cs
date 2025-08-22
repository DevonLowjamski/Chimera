using UnityEngine;
using System;
using System.Collections.Generic;
using ProjectChimera.Data.Economy;

namespace ProjectChimera.Data.Save
{
    /// <summary>
    /// Comprehensive Data Transfer Objects for Contract System Save/Load Operations
    /// Phase 8 MVP - Contract state persistence
    /// </summary>
    
    /// <summary>
    /// Main contracts state DTO for the entire contract system
    /// </summary>
    [System.Serializable]
    public class ContractsStateDTO
    {
        [Header("Contract System State")]
        public ContractGenerationStateDTO GenerationState;
        public ContractTrackingStateDTO TrackingState;
        public ContractMarketStateDTO MarketState;
        
        [Header("Active Contracts")]
        public List<ContractInstanceDTO> ActiveContracts = new List<ContractInstanceDTO>();
        public List<ContractInstanceDTO> CompletedContracts = new List<ContractInstanceDTO>();
        public List<ContractInstanceDTO> FailedContracts = new List<ContractInstanceDTO>();
        
        [Header("Contract Templates")]
        public List<string> AvailableContractTemplateIds = new List<string>();
        public Dictionary<string, ContractTemplateStateDTO> TemplateStates = new Dictionary<string, ContractTemplateStateDTO>();
        
        [Header("Player Contract Stats")]
        public PlayerContractStatsDTO PlayerStats;
        
        [Header("System Configuration")]
        public bool EnableContractSystem = true;
        public bool EnableDynamicGeneration = true;
        public bool EnableQualityAssessment = true;
        public float ContractGenerationInterval = 300f; // 5 minutes
        public int MaxActiveContracts = 10;
        
        [Header("Save Metadata")]
        public DateTime SaveTimestamp;
        public string SaveVersion = "1.0";
    }
    
    /// <summary>
    /// DTO for contract instance persistence
    /// </summary>
    [System.Serializable]
    public class ContractInstanceDTO
    {
        [Header("Contract Identity")]
        public string ContractId;
        public string TemplateId;
        public string ContractTitle;
        public string Description;
        public string ClientName;
        public string ClientType;
        
        [Header("Contract Requirements")]
        public ContractRequirementsDTO Requirements;
        public ContractRewardsDTO Rewards;
        public ContractPenaltiesDTO Penalties;
        
        [Header("Contract State")]
        public ContractState Status;
        public float CompletionProgress; // 0.0 to 1.0
        public DateTime CreationTime;
        public DateTime AcceptanceTime;
        public DateTime StartTime;
        public DateTime DueTime;
        public DateTime CompletionTime;
        public bool IsAccepted;
        public bool IsActive;
        public bool IsCompleted;
        public bool IsFailed;
        
        [Header("Contract Progress")]
        public List<ContractDeliveryDTO> RequiredDeliveries = new List<ContractDeliveryDTO>();
        public List<ContractDeliveryDTO> CompletedDeliveries = new List<ContractDeliveryDTO>();
        public Dictionary<string, float> ProgressMetrics = new Dictionary<string, float>();
        
        [Header("Quality Assessment")]
        public ContractQualityAssessmentDTO QualityAssessment;
        public float QualityScore; // 0.0 to 1.0
        public bool QualityMeetsStandards;
        
        [Header("Financial")]
        public float BaseReward;
        public float BonusReward;
        public float PenaltyAmount;
        public float FinalPayout;
        public bool PayoutProcessed;
        public DateTime PayoutTime;
        
        [Header("Contract Events")]
        public List<ContractEventDTO> EventHistory = new List<ContractEventDTO>();
        
        [Header("Custom Data")]
        public Dictionary<string, object> CustomProperties = new Dictionary<string, object>();
    }
    
    /// <summary>
    /// DTO for contract requirements
    /// </summary>
    [System.Serializable]
    public class ContractRequirementsDTO
    {
        [Header("Product Requirements")]
        public List<ProductRequirementDTO> ProductRequirements = new List<ProductRequirementDTO>();
        
        [Header("Quality Requirements")]
        public float MinimumQualityScore = 0.7f;
        public List<string> RequiredTestResults = new List<string>();
        public bool RequiresCertification;
        public string CertificationType;
        
        [Header("Delivery Requirements")]
        public float TotalQuantity;
        public string QuantityUnit;
        public DateTime DeliveryDeadline;
        public string DeliveryLocation;
        public bool AllowPartialDelivery;
        public int MaxDeliveryBatches = 1;
        
        [Header("Special Requirements")]
        public List<string> SpecialConditions = new List<string>();
        public Dictionary<string, object> CustomRequirements = new Dictionary<string, object>();
    }
    
    /// <summary>
    /// DTO for individual product requirements
    /// </summary>
    [System.Serializable]
    public class ProductRequirementDTO
    {
        public string ProductId;
        public string ProductName;
        public string StrainId;
        public string StrainName;
        public float Quantity;
        public string QuantityUnit;
        public float MinimumQuality;
        public float MinimumPotency;
        public List<string> RequiredCharacteristics = new List<string>();
        public Dictionary<string, float> QualityThresholds = new Dictionary<string, float>();
        public bool IsFulfilled;
        public float FulfilledQuantity;
    }
    
    /// <summary>
    /// DTO for contract rewards
    /// </summary>
    [System.Serializable]
    public class ContractRewardsDTO
    {
        [Header("Monetary Rewards")]
        public float BaseCashReward;
        public float BonusCashReward;
        public float QualityBonus;
        public float TimelyDeliveryBonus;
        public float VolumeBonus;
        
        [Header("Experience and Skill Rewards")]
        public int SkillPointsReward;
        public int ExperiencePointsReward;
        public List<string> SkillBonuses = new List<string>();
        
        [Header("Reputation Rewards")]
        public float ReputationGain;
        public List<string> ReputationCategories = new List<string>();
        
        [Header("Unlock Rewards")]
        public List<string> UnlockedSchematicIds = new List<string>();
        public List<string> UnlockedContractTypes = new List<string>();
        public List<string> UnlockedClients = new List<string>();
        
        [Header("Item Rewards")]
        public List<ItemRewardDTO> ItemRewards = new List<ItemRewardDTO>();
        
        [Header("Custom Rewards")]
        public Dictionary<string, object> CustomRewards = new Dictionary<string, object>();
    }
    
    /// <summary>
    /// DTO for contract penalties
    /// </summary>
    [System.Serializable]
    public class ContractPenaltiesDTO
    {
        [Header("Financial Penalties")]
        public float LatePenalty;
        public float QualityPenalty;
        public float CancellationPenalty;
        public float FailurePenalty;
        
        [Header("Reputation Penalties")]
        public float ReputationLoss;
        public List<string> AffectedReputationCategories = new List<string>();
        
        [Header("Relationship Penalties")]
        public float ClientRelationshipPenalty;
        public bool TemporaryClientBan;
        public int BanDurationDays;
        
        [Header("Custom Penalties")]
        public Dictionary<string, object> CustomPenalties = new Dictionary<string, object>();
    }
    
    /// <summary>
    /// DTO for contract deliveries
    /// </summary>
    [System.Serializable]
    public class ContractDeliveryDTO
    {
        public string DeliveryId;
        public string ProductId;
        public string StrainId;
        public float Quantity;
        public string QuantityUnit;
        public float QualityScore;
        public DateTime DeliveryTime;
        public bool IsCompleted;
        public bool QualityApproved;
        public Dictionary<string, float> QualityMetrics = new Dictionary<string, float>();
        public string DeliveryNotes;
    }
    
    /// <summary>
    /// DTO for contract quality assessment
    /// </summary>
    [System.Serializable]
    public class ContractQualityAssessmentDTO
    {
        [Header("Overall Assessment")]
        public float OverallQualityScore; // 0.0 to 1.0
        public bool MeetsRequirements;
        public DateTime AssessmentTime;
        public string AssessmentNotes;
        
        [Header("Product Quality")]
        public List<ProductQualityAssessmentDTO> ProductAssessments = new List<ProductQualityAssessmentDTO>();
        
        [Header("Delivery Quality")]
        public float DeliveryTimelinessScore;
        public float PackagingQualityScore;
        public float DocumentationQualityScore;
        
        [Header("Quality Metrics")]
        public Dictionary<string, float> QualityMetrics = new Dictionary<string, float>();
        public List<string> QualityIssues = new List<string>();
        public List<string> QualityExcellences = new List<string>();
    }
    
    /// <summary>
    /// DTO for individual product quality assessment
    /// </summary>
    [System.Serializable]
    public class ProductQualityAssessmentDTO
    {
        public string ProductId;
        public string StrainId;
        public float QualityScore;
        public float Potency;
        public float Moisture;
        public float Trichomes;
        public float VisualAppeal;
        public float Aroma;
        public bool ContaminantFree;
        public List<string> TestResults = new List<string>();
        public Dictionary<string, float> CustomMetrics = new Dictionary<string, float>();
    }
    
    /// <summary>
    /// DTO for contract events
    /// </summary>
    [System.Serializable]
    public class ContractEventDTO
    {
        public string EventId;
        public DateTime EventTime;
        public ContractEventType EventType;
        public string EventDescription;
        public Dictionary<string, object> EventData = new Dictionary<string, object>();
        public string TriggeredBy; // Player, System, Client
    }
    
    /// <summary>
    /// DTO for item rewards
    /// </summary>
    [System.Serializable]
    public class ItemRewardDTO
    {
        public string ItemId;
        public string ItemName;
        public string ItemType;
        public int Quantity;
        public float ItemValue;
        public Dictionary<string, object> ItemProperties = new Dictionary<string, object>();
    }
    
    /// <summary>
    /// DTO for contract generation state
    /// </summary>
    [System.Serializable]
    public class ContractGenerationStateDTO
    {
        [Header("Generation Settings")]
        public bool IsGenerationActive = true;
        public float GenerationInterval = 300f; // seconds
        public DateTime LastGenerationTime;
        public DateTime NextGenerationTime;
        
        [Header("Generation Pool")]
        public List<string> AvailableTemplateIds = new List<string>();
        public Dictionary<string, float> TemplateWeights = new Dictionary<string, float>();
        public Dictionary<string, DateTime> LastUsedTimes = new Dictionary<string, DateTime>();
        
        [Header("Generation Constraints")]
        public int MaxActiveContracts = 10;
        public int CurrentActiveContracts;
        public float MinDifficultyLevel = 0.1f;
        public float MaxDifficultyLevel = 1.0f;
        public float PlayerSkillModifier = 1.0f;
        
        [Header("Market Influence")]
        public Dictionary<string, float> ProductDemandModifiers = new Dictionary<string, float>();
        public Dictionary<string, float> SeasonalModifiers = new Dictionary<string, float>();
        public float MarketVolatilityModifier = 1.0f;
        
        [Header("Generation Metrics")]
        public int TotalContractsGenerated;
        public int ContractsGeneratedToday;
        public DateTime LastMetricsReset;
    }
    
    /// <summary>
    /// DTO for contract tracking state
    /// </summary>
    [System.Serializable]
    public class ContractTrackingStateDTO
    {
        [Header("Tracking Configuration")]
        public bool IsTrackingActive = true;
        public float TrackingUpdateInterval = 10f; // seconds
        public DateTime LastTrackingUpdate;
        
        [Header("Active Contract Tracking")]
        public List<string> TrackedContractIds = new List<string>();
        public Dictionary<string, ContractProgressDTO> ContractProgress = new Dictionary<string, ContractProgressDTO>();
        
        [Header("Notification Settings")]
        public bool EnableDeadlineNotifications = true;
        public bool EnableProgressNotifications = true;
        public bool EnableQualityNotifications = true;
        public int DeadlineWarningHours = 24;
        
        [Header("Tracking Metrics")]
        public int TotalTrackedContracts;
        public int ActiveTrackedContracts;
        public DateTime LastNotificationSent;
    }
    
    /// <summary>
    /// DTO for contract progress tracking
    /// </summary>
    [System.Serializable]
    public class ContractProgressDTO
    {
        public string ContractId;
        public float OverallProgress; // 0.0 to 1.0
        public Dictionary<string, float> RequirementProgress = new Dictionary<string, float>();
        public List<string> CompletedRequirements = new List<string>();
        public List<string> PendingRequirements = new List<string>();
        public DateTime LastProgressUpdate;
        public bool IsOnTrack;
        public DateTime EstimatedCompletionTime;
    }
    
    /// <summary>
    /// DTO for contract market state
    /// </summary>
    [System.Serializable]
    public class ContractMarketStateDTO
    {
        [Header("Market Configuration")]
        public bool IsMarketActive = true;
        public float MarketDemandModifier = 1.0f;
        public float PriceInflationRate = 0.02f;
        
        [Header("Client Pool")]
        public List<ContractClientDTO> AvailableClients = new List<ContractClientDTO>();
        public Dictionary<string, ClientRelationshipDTO> ClientRelationships = new Dictionary<string, ClientRelationshipDTO>();
        
        [Header("Market Trends")]
        public Dictionary<string, float> ProductDemandTrends = new Dictionary<string, float>();
        public Dictionary<string, float> QualityPremiums = new Dictionary<string, float>();
        public DateTime LastMarketUpdate;
        
        [Header("Seasonal Factors")]
        public string CurrentSeason;
        public Dictionary<string, float> SeasonalDemandModifiers = new Dictionary<string, float>();
    }
    
    /// <summary>
    /// DTO for contract clients
    /// </summary>
    [System.Serializable]
    public class ContractClientDTO
    {
        public string ClientId;
        public string ClientName;
        public string ClientType; // "Dispensary", "Distributor", "Research", "Medical"
        public float ReputationScore;
        public float PaymentReliability;
        public float QualityStandards;
        public List<string> PreferredProductTypes = new List<string>();
        public List<string> PreferredStrains = new List<string>();
        public bool IsActive;
        public DateTime LastContractTime;
        public int TotalContractsCompleted;
        public float AverageContractValue;
    }
    
    /// <summary>
    /// DTO for client relationships
    /// </summary>
    [System.Serializable]
    public class ClientRelationshipDTO
    {
        public string ClientId;
        public float RelationshipScore; // 0.0 to 1.0
        public int ContractsCompleted;
        public int ContractsFailed;
        public float AverageQualityDelivered;
        public float PaymentHistory; // reliability score
        public DateTime LastInteraction;
        public bool IsBlacklisted;
        public List<string> RelationshipNotes = new List<string>();
    }
    
    /// <summary>
    /// DTO for player contract statistics
    /// </summary>
    [System.Serializable]
    public class PlayerContractStatsDTO
    {
        [Header("Contract Statistics")]
        public int TotalContractsAccepted;
        public int TotalContractsCompleted;
        public int TotalContractsFailed;
        public int TotalContractsCancelled;
        public float CompletionRate; // percentage
        public float AverageQualityScore;
        
        [Header("Financial Statistics")]
        public float TotalEarningsFromContracts;
        public float TotalPenaltiesPaid;
        public float AverageContractValue;
        public float HighestContractValue;
        
        [Header("Performance Metrics")]
        public float AverageDeliveryTime; // days
        public float OnTimeDeliveryRate; // percentage
        public float QualityConsistency; // score consistency
        public DateTime BestCompletionTime;
        public DateTime LastContractCompletion;
        
        [Header("Reputation Metrics")]
        public float OverallContractReputation;
        public Dictionary<string, float> ClientSpecificReputation = new Dictionary<string, float>();
        public List<string> Achievements = new List<string>();
        
        [Header("Recent Performance")]
        public List<float> RecentQualityScores = new List<float>(); // last 10 contracts
        public List<TimeSpan> RecentCompletionTimes = new List<TimeSpan>(); // last 10 contracts
        public DateTime LastStatsUpdate;
    }
    
    /// <summary>
    /// DTO for contract template state
    /// </summary>
    [System.Serializable]
    public class ContractTemplateStateDTO
    {
        public string TemplateId;
        public bool IsAvailable = true;
        public bool IsUnlocked = true;
        public float DifficultyModifier = 1.0f;
        public float RewardModifier = 1.0f;
        public DateTime LastUsed;
        public int TimesUsed;
        public float SuccessRate; // historical success rate for this template
        public Dictionary<string, object> TemplateSettings = new Dictionary<string, object>();
    }
    
    /// <summary>
    /// Contract event types for tracking
    /// </summary>
    public enum ContractEventType
    {
        ContractGenerated,
        ContractAccepted,
        ContractStarted,
        DeliveryMade,
        QualityAssessed,
        ContractCompleted,
        ContractFailed,
        ContractCancelled,
        PaymentProcessed,
        DeadlineWarning,
        QualityIssue,
        BonusEarned,
        PenaltyApplied
    }
    
    /// <summary>
    /// Contract state enumeration
    /// </summary>
    public enum ContractState
    {
        Available,
        Accepted,
        InProgress,
        AwaitingDelivery,
        UnderReview,
        Completed,
        Failed,
        Cancelled,
        Expired
    }
    
    /// <summary>
    /// Result DTO for contract save operations
    /// </summary>
    [System.Serializable]
    public class ContractSaveResult
    {
        public bool Success;
        public DateTime SaveTime;
        public string ErrorMessage;
        public long DataSizeBytes;
        public TimeSpan SaveDuration;
        public int ActiveContractsSaved;
        public int CompletedContractsSaved;
        public int ClientsSaved;
        public string SaveVersion;
    }
    
    /// <summary>
    /// Result DTO for contract load operations
    /// </summary>
    [System.Serializable]
    public class ContractLoadResult
    {
        public bool Success;
        public DateTime LoadTime;
        public string ErrorMessage;
        public TimeSpan LoadDuration;
        public int ActiveContractsLoaded;
        public int CompletedContractsLoaded;
        public int ClientsLoaded;
        public bool RequiredMigration;
        public string LoadedVersion;
        public ContractsStateDTO ContractsState;
    }
    
    /// <summary>
    /// DTO for contract system validation
    /// </summary>
    [System.Serializable]
    public class ContractValidationResult
    {
        public bool IsValid;
        public DateTime ValidationTime;
        public List<string> Errors = new List<string>();
        public List<string> Warnings = new List<string>();
        
        [Header("Contract Data Validation")]
        public bool ActiveContractsValid;
        public bool CompletedContractsValid;
        public bool ClientDataValid;
        public bool ProgressDataValid;
        
        [Header("Data Integrity")]
        public int TotalContracts;
        public int ValidContracts;
        public int TotalClients;
        public int ValidClients;
        public float DataIntegrityScore;
    }
}