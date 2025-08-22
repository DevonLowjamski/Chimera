using UnityEngine;
using System;
using System.Collections.Generic;
using ProjectChimera.Core;

namespace ProjectChimera.Data.Construction
{
    /// <summary>
    /// Data structures for grid-based construction cost management.
    /// Simplified cost tracking designed for the new grid construction system.
    /// </summary>

    [System.Serializable]
    public class GridCostEstimate
    {
        public string EstimateId;
        public string ProjectId;
        public GridConstructionTemplate Template;
        public DateTime EstimateDate;
        public DateTime EstimateValidUntil;
        
        // Cost breakdown
        public float BaseCost;
        public float ResourceCost;
        public float LaborCost;
        public float MaterialCost;
        public float SubtotalCost;
        public float ContingencyCost;
        public float TotalCost;
        
        // Additional info
        public string Notes;
        public bool IsApproved;
    }

    [System.Serializable]
    public class GridProjectBudget
    {
        public string BudgetId;
        public string ProjectId;
        public float ApprovedAmount;
        public float EstimatedCost;
        public float RemainingAmount;
        public DateTime CreatedDate;
        
        // Budget categories
        public float LaborBudget;
        public float MaterialBudget;
        public float BaseBudget;
        public float ContingencyBudget;
        
        // Spending tracking
        public float LaborSpent;
        public float MaterialSpent;
        public float BaseSpent;
        public float ContingencySpent;
        
        public float ContingencyReserve;
        public List<GridCostRecord> CostRecords = new List<GridCostRecord>();
        
        // Properties
        public float TotalSpent => LaborSpent + MaterialSpent + BaseSpent + ContingencySpent;
        public float BudgetUtilization => ApprovedAmount > 0 ? TotalSpent / ApprovedAmount : 0f;
        public bool IsOverBudget => TotalSpent > ApprovedAmount;
    }

    [System.Serializable]
    public class GridCostRecord
    {
        public string RecordId;
        public string ProjectId;
        public GridCostCategory CostCategory;
        public float Amount;
        public string Description;
        public DateTime RecordDate;
        public string ResourceName;
        public float Quantity;
    }

    [System.Serializable]
    public class GridResourceAllocation
    {
        public string AllocationId;
        public string ProjectId;
        public Dictionary<string, float> AllocatedResources = new Dictionary<string, float>();
        public Dictionary<string, float> AllocationCosts = new Dictionary<string, float>();
        public float TotalAllocationCost;
        public DateTime AllocationDate;
        public GridAllocationStatus Status;
    }

    [System.Serializable]
    public class GridCostPerformanceData
    {
        public string ProjectId;
        public string BudgetId;
        public float PlannedCost;
        public float ActualCost;
        public DateTime StartDate;
        public DateTime LastUpdate;
        
        // Performance metrics
        public float CostVariance => PlannedCost - ActualCost;
        public float CostPerformanceIndex => ActualCost > 0 ? PlannedCost / ActualCost : 1f;
        public bool IsOnBudget => Math.Abs(CostVariance) <= PlannedCost * 0.05f; // Within 5%
    }

    [System.Serializable]
    public class GridCostMetrics
    {
        public float TotalBudgetAllocated;
        public float TotalBudgetSpent;
        public float BudgetUtilization;
        public int ActiveProjects;
        public float AverageCostPerProject;
        public DateTime LastUpdated;
        
        // Performance metrics
        public int ProjectsOnBudget;
        public int ProjectsOverBudget;
        public float AverageCostVariance;
        public float TotalSavings;
    }

    [System.Serializable]
    public class GridBudgetAlert
    {
        public string AlertId;
        public string ProjectId;
        public string BudgetId;
        public GridBudgetAlertType AlertType;
        public string Message;
        public float Threshold;
        public float CurrentUtilization;
        public DateTime CreatedDate;
        public bool IsAcknowledged;
    }

    [System.Serializable]
    public class GridResourceInventoryStatus
    {
        public int TotalResourceTypes;
        public int LowStockResources;
        public float TotalInventoryValue;
        public float AvailableInventoryValue;
        public DateTime LastUpdated;
        public List<string> CriticalResources = new List<string>();
    }

    // Enums for grid cost system
    public enum GridCostCategory
    {
        Labor,
        Materials,
        Base,
        Contingency,
        Equipment,
        Utilities,
        Permits,
        Other
    }

    public enum GridAllocationStatus
    {
        Pending,
        Allocated,
        PartialAllocation,
        ResourceShortage,
        Cancelled
    }

    public enum GridBudgetAlertType
    {
        Info,
        Warning,
        Critical,
        Exceeded
    }

    public enum GridEstimateStatus
    {
        Draft,
        InProgress,
        Completed,
        Approved,
        Rejected,
        Expired
    }
    
    [System.Serializable]
    public class GridCostSummary
    {
        public string ProjectId;
        public float TotalBudget;
        public float TotalSpent;
        public float RemainingBudget;
        public float BudgetUtilization;
        public bool IsOverBudget;
        public Dictionary<GridCostCategory, float> CostBreakdown = new Dictionary<GridCostCategory, float>();
        public DateTime LastUpdated;
    }
}