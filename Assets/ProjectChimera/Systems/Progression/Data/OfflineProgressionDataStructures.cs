using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace ProjectChimera.Systems.Progression
{
    /// <summary>
    /// Priority levels for offline progression events
    /// </summary>
    public enum EventPriority
    {
        Low = 0,
        Normal = 1,
        High = 2,
        Critical = 3
    }
    // ===== CULTIVATION DATA STRUCTURES =====
    
    [System.Serializable]
    public class PlantGrowthData
    {
        public int ProcessedPlants;
        public float AverageGrowthProgress;
        public int NewGrowthStageTransitions;
        public int MaturedPlants;
    }
    
    [System.Serializable]
    public class HarvestData
    {
        public int CompletedHarvests;
        public float TotalYield;
        public float QualityRating;
        public List<DateTime> HarvestTimes = new List<DateTime>();
        public bool AutoHarvestDisabled;
    }

    // ===== CONSTRUCTION DATA STRUCTURES =====
    
    [System.Serializable]
    public class ConstructionProjectData
    {
        public int ActiveProjects;
        public float AverageProgressMade;
        public float TotalWorkHours;
        public int StageAdvances;
    }
    
    [System.Serializable]
    public class BuildingCompletionData
    {
        public int CompletedBuildings;
        public float TotalConstructionTime;
        public List<string> CompletedBuildingTypes = new List<string>();
        public bool AutoConstructionDisabled;
    }

    // ===== ECONOMY DATA STRUCTURES =====
    
    [System.Serializable]
    public class MarketData
    {
        public Dictionary<string, float> PriceChanges = new Dictionary<string, float>();
        public Dictionary<string, float> CurrentPrices = new Dictionary<string, float>();
        public List<string> SignificantPriceChanges = new List<string>();
    }
    
    [System.Serializable]
    public class ContractFulfillmentData
    {
        public int CompletedContracts;
        public float TotalContractValue;
        public float ReputationChange;
        public List<string> CompletedContractTypes = new List<string>();
        public bool AutoFulfillmentDisabled;
    }
    
    [System.Serializable]
    public class PassiveIncomeData
    {
        public float BaseIncome;
        public float MarketBonus;
        public float EfficiencyBonus;
        public float TotalIncome;
    }

    // ===== EQUIPMENT DATA STRUCTURES =====
    
    [System.Serializable]
    public class EquipmentDegradationData
    {
        public int ProcessedEquipment;
        public float AverageDegradation;
        public int EquipmentNeedingMaintenance;
        public int CriticallyDegradedEquipment;
        public List<float> EquipmentConditions = new List<float>();
    }
    
    [System.Serializable]
    public class EquipmentProductionData
    {
        public Dictionary<string, float> ResourceProduction = new Dictionary<string, float>();
        public float TotalProductionValue;
        public float ProductionEfficiency;
        public bool ProductionDisabled;
    }
    
    [System.Serializable]
    public class MaintenanceRequirementData
    {
        public int EquipmentNeedingMaintenance;
        public int CriticalEquipmentCount;
        public float MaintenanceCost;
        public bool AutoMaintenancePerformed;
    }

    // ===== OFFLINE PROGRESSION RESULT STRUCTURES =====

    /// <summary>
    /// Result of offline progression calculations for a specific domain
    /// </summary>
    [System.Serializable]
    public class OfflineProgressionResult
    {
        public string Domain { get; set; }
        public bool Success { get; set; }
        public string ErrorMessage { get; set; }
        public TimeSpan OfflineTime { get; set; }
        public DateTime ProcessedUntil { get; set; }

        // Legacy compatibility properties
        public string Message
        {
            get => ErrorMessage;
            set => ErrorMessage = value;
        }
        public TimeSpan OfflineDuration
        {
            get => OfflineTime;
            set => OfflineTime = value;
        }
        public float ExperienceGained { get; set; }
        public float CurrencyEarned { get; set; }

        // Domain-specific data
        public PlantGrowthData PlantGrowth { get; set; }
        public HarvestData Harvest { get; set; }
        public ConstructionProjectData Construction { get; set; }
        public BuildingCompletionData BuildingCompletion { get; set; }
        public MarketData Market { get; set; }
        public ContractFulfillmentData ContractFulfillment { get; set; }
        public PassiveIncomeData PassiveIncome { get; set; }
        public EquipmentDegradationData EquipmentDegradation { get; set; }
        public EquipmentProductionData EquipmentProduction { get; set; }
        public MaintenanceRequirementData MaintenanceRequirement { get; set; }

        // Summary data
        public float TotalResourcesGenerated { get; set; }
        public float TotalCostsIncurred { get; set; }
        public float NetGain { get; set; }
        public List<string> ImportantEvents { get; set; } = new List<string>();

        // ProgressionData property for legacy compatibility
        public object ProgressionData
        {
            get
            {
                // Return a dictionary with all domain-specific data for TryGetValue/ContainsKey operations
                var data = new Dictionary<string, object>();

                if (PlantGrowth != null) data["plant_growth"] = PlantGrowth;
                if (Harvest != null) data["harvest"] = Harvest;
                if (Construction != null) data["construction_projects"] = Construction;
                if (BuildingCompletion != null) data["building_completion"] = BuildingCompletion;
                if (Market != null) data["market"] = Market;
                if (ContractFulfillment != null) data["contract_fulfillment"] = ContractFulfillment;
                if (PassiveIncome != null) data["passive_income"] = PassiveIncome;
                if (EquipmentDegradation != null) data["equipment_degradation"] = EquipmentDegradation;
                if (EquipmentProduction != null) data["equipment_production"] = EquipmentProduction;
                if (MaintenanceRequirement != null) data["maintenance_requirement"] = MaintenanceRequirement;

                return data;
            }
        }
    }

    /// <summary>
    /// Result of async offline progression calculations
    /// Used by IOfflineProgressionProvider implementations
    /// </summary>
    [System.Serializable]
    public class OfflineProgressionCalculationResult
    {
        public bool IsCompleted { get; set; }
        public bool IsSuccessful { get; set; }
        public string ErrorMessage { get; set; }
        public TimeSpan CalculationTime { get; set; }
        public DateTime CalculationStartTime { get; set; }
        public DateTime CalculationEndTime { get; set; }

        // Legacy compatibility property
        public bool Success
        {
            get => IsSuccessful;
            set => IsSuccessful = value;
        }

        // Results per domain
        public List<OfflineProgressionResult> DomainResults { get; set; } = new List<OfflineProgressionResult>();

        // Overall summary
        public float OverallProgress { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        /// <summary>
        /// Get result for specific domain
        /// </summary>
        public OfflineProgressionResult GetDomainResult(string domain)
        {
            return DomainResults.Find(r => r.Domain == domain);
        }

        /// <summary>
        /// Add domain result
        /// </summary>
        public void AddDomainResult(OfflineProgressionResult result)
        {
            var existing = GetDomainResult(result.Domain);
            if (existing != null)
            {
                DomainResults.Remove(existing);
            }
            DomainResults.Add(result);
        }
    }

    /// <summary>
    /// Event data structure for offline progression events
    /// </summary>
    [System.Serializable]
    public class OfflineProgressionEvent
    {
        public string EventType { get; set; }
        public string Domain { get; set; }
        public DateTime Timestamp { get; set; }
        public string Description { get; set; }
        public string Title { get; set; }
        public EventPriority Priority { get; set; } = EventPriority.Normal;
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();

        public OfflineProgressionEvent()
        {
            Timestamp = DateTime.Now;
        }

        public OfflineProgressionEvent(string eventType, string domain, string description)
        {
            EventType = eventType;
            Domain = domain;
            Description = description;
            Timestamp = DateTime.Now;
        }
    }
}
