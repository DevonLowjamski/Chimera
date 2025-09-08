using System;
using System.Collections.Generic;

namespace ProjectChimera.Systems.Progression
{
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
}
