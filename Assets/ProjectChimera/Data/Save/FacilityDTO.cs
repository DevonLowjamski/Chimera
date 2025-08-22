using UnityEngine;
using System;
using System.Collections.Generic;

namespace ProjectChimera.Data.Save
{
    /// <summary>
    /// Data Transfer Objects for Facility System Save/Load Operations
    /// These DTOs capture the state of the current FacilityManager architecture
    /// and are designed for efficient serialization and state persistence.
    /// </summary>
    
    /// <summary>
    /// Main facility state DTO for the current facility management system
    /// </summary>
    [System.Serializable]
    public class FacilityStateDTO
    {
        [Header("Current Facility State")]
        public string CurrentFacilityId;
        public int CurrentTierIndex;
        public string CurrentTierName;
        public bool IsLoadingScene;
        
        [Header("Owned Facilities")]
        public List<OwnedFacilityDTO> OwnedFacilities = new List<OwnedFacilityDTO>();
        
        [Header("Progression Data")]
        public FacilityProgressionDTO ProgressionData;
        
        [Header("Scene Integration")]
        public List<FacilitySceneMappingDTO> SceneMappings = new List<FacilitySceneMappingDTO>();
        
        [Header("System Configuration")]
        public bool EnableProgressionSystem = true;
        public bool EnableMultipleFacilities = true;
        public float FacilityEvaluationInterval = 30f;
        public string DefaultFacilityScene = "04_Warehouse_Small_Bay";
        public bool PreloadNextTierScene = true;
        public float SceneTransitionTimeout = 15f;
        
        [Header("Save Metadata")]
        public DateTime SaveTimestamp;
        public string SaveVersion = "1.0";
    }
    
    /// <summary>
    /// DTO for individual owned facility instances
    /// </summary>
    [System.Serializable]
    public class OwnedFacilityDTO
    {
        public string FacilityId;
        public string FacilityName;
        public string TierName;
        public int TierLevel;
        public string SceneName;
        public DateTime PurchaseDate;
        public bool IsActive;
        public bool IsOperational;
        
        [Header("Financial Data")]
        public float TotalInvestment;
        public float CurrentValue;
        public float TotalRevenue;
        public float MaintenanceLevel;
        public DateTime LastMaintenance;
        
        [Header("Operational Data")]
        public int TotalPlantsGrown;
        public float AverageYield;
        public string Notes;
        
        [Header("Facility Tier Data")]
        public FacilityTierDataDTO TierData;
    }
    
    /// <summary>
    /// DTO for facility tier information
    /// </summary>
    [System.Serializable]
    public class FacilityTierDataDTO
    {
        public string TierName;
        public int TierLevel;
        public string Description;
        public string SceneName;
        public Vector3 FacilitySize;
        
        [Header("Requirements")]
        public float RequiredCapital;
        public float RequiredExperience;
        public int RequiredPlants;
        public int RequiredHarvests;
        
        [Header("Capacity")]
        public int MaxRooms;
        public int MaxPlants;
        public float MaxPowerConsumption;
        public bool SupportsAutomation;
        public bool SupportsAdvancedEquipment;
        
        [Header("Features")]
        public List<string> AvailableFeatures = new List<string>();
        public List<string> UnlockedEquipment = new List<string>();
    }
    
    /// <summary>
    /// DTO for facility progression tracking
    /// </summary>
    [System.Serializable]
    public class FacilityProgressionDTO
    {
        [Header("Player Progress")]
        public float Capital;
        public float Experience;
        public int TotalPlants;
        public int TotalHarvests;
        public int TotalUpgrades;
        public int UnlockedTiers;
        
        [Header("Statistics")]
        public float TotalRevenue;
        public float TotalExpenses;
        public float AverageYield;
        public float BestQuality;
        public DateTime FirstFacilityPurchase;
        public DateTime LastUpgrade;
        
        [Header("Achievements")]
        public List<string> CompletedMilestones = new List<string>();
        public Dictionary<string, float> CustomMetrics = new Dictionary<string, float>();
    }
    
    /// <summary>
    /// DTO for facility scene mapping information
    /// </summary>
    [System.Serializable]
    public class FacilitySceneMappingDTO
    {
        public string TierName;
        public string SceneName;
        public int BuildIndex;
        public string Description;
        public float LoadingEstimateSeconds;
        public bool IsAvailable;
        public DateTime LastLoaded;
    }
    
    /// <summary>
    /// DTO for facility upgrade requirements and validation
    /// </summary>
    [System.Serializable]
    public class FacilityUpgradeRequirementsDTO
    {
        public string TargetTierName;
        public float RequiredCapital;
        public float RequiredExperience;
        public int RequiredPlants;
        public int RequiredHarvests;
        
        [Header("Validation")]
        public bool CanUpgrade;
        public List<string> FailureReasons = new List<string>();
        public DateTime LastValidated;
    }
    
    /// <summary>
    /// DTO for facility event history and notifications
    /// </summary>
    [System.Serializable]
    public class FacilityEventHistoryDTO
    {
        public List<FacilityEventDTO> Events = new List<FacilityEventDTO>();
        public DateTime LastEventCheck;
        public int TotalUpgrades;
        public int TotalPurchases;
        public int TotalSales;
        public int TotalSwitches;
    }
    
    /// <summary>
    /// DTO for individual facility events
    /// </summary>
    [System.Serializable]
    public class FacilityEventDTO
    {
        public string EventId;
        public string EventType; // "Upgraded", "Purchased", "Sold", "Switched", etc.
        public DateTime EventTime;
        public string FacilityId;
        public string FacilityName;
        public string Description;
        public float CostAmount;
        public bool WasSuccessful;
        public Dictionary<string, string> EventData = new Dictionary<string, string>();
    }
    
    /// <summary>
    /// DTO for facility switching information and history
    /// </summary>
    [System.Serializable]
    public class FacilitySwitchHistoryDTO
    {
        public List<FacilitySwitchRecordDTO> SwitchHistory = new List<FacilitySwitchRecordDTO>();
        public string LastSwitchedToFacilityId;
        public DateTime LastSwitchTime;
        public float TotalSwitchTime;
        public int TotalSwitches;
    }
    
    /// <summary>
    /// DTO for individual facility switch records
    /// </summary>
    [System.Serializable]
    public class FacilitySwitchRecordDTO
    {
        public DateTime StartTime;
        public DateTime EndTime;
        public string FromFacilityId;
        public string ToFacilityId;
        public float SwitchDuration;
        public bool WasSuccessful;
        public string ErrorMessage;
        public float SwitchCost;
    }
    
    /// <summary>
    /// DTO for facility portfolio management data
    /// </summary>
    [System.Serializable]
    public class FacilityPortfolioDTO
    {
        [Header("Portfolio Summary")]
        public float TotalPortfolioValue;
        public float TotalInvestment;
        public float PortfolioROI;
        public int TotalFacilities;
        public DateTime LastValuation;
        
        [Header("Portfolio Performance")]
        public float MonthlyRevenue;
        public float MonthlyExpenses;
        public float NetIncome;
        public float GrowthRate;
        
        [Header("Risk Management")]
        public float DiversificationIndex;
        public string RiskLevel; // "Low", "Medium", "High"
        public List<string> RiskFactors = new List<string>();
        
        [Header("Market Data")]
        public Dictionary<string, float> MarketValues = new Dictionary<string, float>();
        public Dictionary<string, float> AppreciationRates = new Dictionary<string, float>();
    }
    
    /// <summary>
    /// DTO for facility automation and control settings
    /// </summary>
    [System.Serializable]
    public class FacilityAutomationDTO
    {
        [Header("Automation Settings")]
        public bool AutomaticUpgradeNotifications = true;
        public bool AutomaticValueUpdates = true;
        public bool AutomaticMaintenanceReminders = true;
        public float AutoEvaluationInterval = 30f;
        
        [Header("Notification Preferences")]
        public bool NotifyOnUpgradeAvailable = true;
        public bool NotifyOnRequirementsNotMet = false;
        public bool NotifyOnFacilityPurchased = true;
        public bool NotifyOnFacilitySold = true;
        public bool NotifyOnValueUpdated = false;
        
        [Header("Auto-Management")]
        public bool AutoAcceptUpgrades = false;
        public bool AutoSellUnprofitableFacilities = false;
        public float AutoSellThreshold = -0.2f; // -20% ROI
        public bool AutoMaintenance = true;
        public float MaintenanceThreshold = 0.7f;
    }
    
    /// <summary>
    /// Result DTO for facility save operations
    /// </summary>
    [System.Serializable]
    public class FacilitySaveResult
    {
        public bool Success;
        public DateTime SaveTime;
        public string ErrorMessage;
        public long DataSizeBytes;
        public TimeSpan SaveDuration;
        public int FacilitiesSaved;
        public string SaveVersion;
    }
    
    /// <summary>
    /// Result DTO for facility load operations
    /// </summary>
    [System.Serializable]
    public class FacilityLoadResult
    {
        public bool Success;
        public DateTime LoadTime;
        public string ErrorMessage;
        public TimeSpan LoadDuration;
        public int FacilitiesLoaded;
        public bool RequiredMigration;
        public string LoadedVersion;
        public FacilityStateDTO FacilityState;
    }
    
    /// <summary>
    /// DTO for facility system validation and integrity checks
    /// </summary>
    [System.Serializable]
    public class FacilityValidationResult
    {
        public bool IsValid;
        public DateTime ValidationTime;
        public List<string> Errors = new List<string>();
        public List<string> Warnings = new List<string>();
        public Dictionary<string, object> ValidationData = new Dictionary<string, object>();
        
        [Header("Facility Checks")]
        public bool HasValidCurrentFacility;
        public bool AllFacilitiesHaveValidTiers;
        public bool ProgressionDataValid;
        public bool SceneMappingsValid;
        public bool FinancialDataValid;
        
        [Header("Data Integrity")]
        public int TotalFacilities;
        public int ValidFacilities;
        public int InvalidFacilities;
        public float DataIntegrityScore;
    }
    
    /// <summary>
    /// DTO for facility scene integration and management
    /// </summary>
    [System.Serializable]
    public class FacilitySceneIntegrationDTO
    {
        [Header("Scene Management")]
        public string CurrentScene;
        public List<string> LoadedScenes = new List<string>();
        public Dictionary<string, string> SceneTransitions = new Dictionary<string, string>();
        public DateTime LastSceneChange;
        
        [Header("Scene Loading")]
        public bool IsLoadingScene;
        public float LoadingProgress;
        public string TargetScene;
        public bool PreloadNextScene;
        
        [Header("Scene Configuration")]
        public Dictionary<string, object> SceneSettings = new Dictionary<string, object>();
        public List<string> AvailableScenes = new List<string>();
        public string DefaultScene;
    }
}