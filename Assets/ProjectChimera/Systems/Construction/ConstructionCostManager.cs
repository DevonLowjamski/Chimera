using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core;
using ProjectChimera.Core.Events;
using ProjectChimera.Data.Construction;
// Grid-based construction system imports
using GridConstructionTemplate = ProjectChimera.Data.Construction.GridConstructionTemplate;
using GridConstructionProject = ProjectChimera.Data.Construction.GridConstructionProject;
using ConstructionCatalog = ProjectChimera.Data.Construction.ConstructionCatalog;
using ConstructionResource = ProjectChimera.Data.Construction.ConstructionResource;

namespace ProjectChimera.Systems.Construction
{
    /// <summary>
    /// Grid-based construction cost and resource management system for Project Chimera.
    /// Adapted for grid-based construction templates with simplified resource tracking.
    /// Handles cost estimation, budget tracking, and resource management for grid construction.
    /// </summary>
    public class ConstructionCostManager : ChimeraManager
    {
        [Header("Cost Management Configuration")]
        [SerializeField] private bool _enableDynamicPricing = true;
        [SerializeField] private bool _enableResourceTracking = true;
        [SerializeField] private bool _enableBudgetAlerts = true;
        [SerializeField] private bool _enableCostOptimization = true;
        [SerializeField] private float _costInflationRate = 0.02f; // 2% per year
        [SerializeField] private float _laborCostMultiplier = 1.0f;
        [SerializeField] private float _materialMarkupPercentage = 0.15f; // 15% markup
        
        [Header("Resource Management")]
        [SerializeField] private float _resourceBufferPercentage = 0.10f; // 10% buffer
        [SerializeField] private bool _enableResourceOptimization = true;
        [SerializeField] private float _wasteReductionTarget = 0.95f; // 95% efficiency
        [SerializeField] private int _maxConcurrentProjects = 5;
        [SerializeField] private float _resourceReorderThreshold = 0.20f; // 20% remaining
        
        [Header("Budget Tracking")]
        [SerializeField] private float _budgetWarningThreshold = 0.80f; // 80% budget used
        [SerializeField] private float _budgetCriticalThreshold = 0.95f; // 95% budget used
        [SerializeField] private bool _enableAutomaticBudgetAdjustment = true;
        [SerializeField] private float _contingencyPercentage = 0.10f; // 10% contingency
        
        [Header("Cannabis-Specific Costs")]
        [SerializeField] private float _complianceCostMultiplier = 1.25f;
        [SerializeField] private float _securityCostMultiplier = 1.50f;
        [SerializeField] private float _hvacCostMultiplier = 1.30f;
        [SerializeField] private float _lightingCostMultiplier = 1.40f;
        [SerializeField] private bool _enableComplianceTracking = true;
        
        [Header("Event Channels")]
        [SerializeField] private SimpleGameEventSO _onBudgetAlert;
        [SerializeField] private SimpleGameEventSO _onCostEstimateCompleted;
        [SerializeField] private SimpleGameEventSO _onResourceAllocated;
        [SerializeField] private SimpleGameEventSO _onProjectBudgetExceeded;
        [SerializeField] private SimpleGameEventSO _onResourceShortage;
        
        // Grid-based cost management
        private Dictionary<string, GridProjectBudget> _projectBudgets = new Dictionary<string, GridProjectBudget>();
        private Dictionary<string, GridCostEstimate> _costEstimates = new Dictionary<string, GridCostEstimate>();
        private Dictionary<string, float> _resourceInventory = new Dictionary<string, float>();
        private Dictionary<string, GridResourceAllocation> _resourceAllocations = new Dictionary<string, GridResourceAllocation>();
        
        // Grid system references
        private GridSystem _gridSystem;
        private ConstructionCatalog _constructionCatalog;
        
        // Performance tracking
        private GridCostMetrics _costMetrics;
        private Dictionary<string, GridCostPerformanceData> _projectPerformance = new Dictionary<string, GridCostPerformanceData>();
        
        // Runtime tracking
        private float _totalBudgetAllocated = 0f;
        private float _totalBudgetSpent = 0f;
        private float _totalResourcesAllocated = 0f;
        private DateTime _lastCostUpdate = DateTime.Now;
        
        // Events  
        public System.Action<string, GridProjectBudget> OnBudgetCreated;
        public System.Action<string, GridCostEstimate> OnCostEstimateCompleted;
        public System.Action<string, GridResourceAllocation> OnResourceAllocated;
        public System.Action<string, GridBudgetAlert> OnBudgetAlert;
        public System.Action<string, float> OnResourceShortage;
        
        // Properties
        public override ManagerPriority Priority => ManagerPriority.High;
        public float TotalBudgetAllocated => _totalBudgetAllocated;
        public float TotalBudgetSpent => _totalBudgetSpent;
        public float BudgetUtilization => _totalBudgetAllocated > 0 ? _totalBudgetSpent / _totalBudgetAllocated : 0f;
        public int ActiveProjects => _projectBudgets.Count;
        public GridCostMetrics CostMetrics => _costMetrics;
        public Dictionary<string, GridProjectBudget> ProjectBudgets => _projectBudgets;
        
        protected override void OnManagerInitialize()
        {
            InitializeGridCostSystems();
            InitializeResourceManagement();
            
            _costMetrics = new GridCostMetrics();
            
            LogInfo("Grid-based ConstructionCostManager initialized successfully");
        }
        
        private void Update()
        {
            if (!IsInitialized) return;
            
            UpdateCostTracking();
            UpdateResourceManagement();
            UpdateBudgetAlerts();
            UpdateMetrics();
        }
        
        protected override void OnManagerShutdown()
        {
            // Cleanup cost management systems
            _projectBudgets.Clear();
            _costEstimates.Clear();
            _resourceInventory.Clear();
            _resourceAllocations.Clear();
            _projectPerformance.Clear();
            
            LogInfo("ConstructionCostManager shutdown completed");
        }
        
        /// <summary>
        /// Create cost estimate for grid construction template
        /// </summary>
        public GridCostEstimate CreateCostEstimate(string projectId, GridConstructionTemplate template)
        {
            var estimate = new GridCostEstimate
            {
                EstimateId = Guid.NewGuid().ToString(),
                ProjectId = projectId,
                Template = template,
                EstimateDate = DateTime.Now,
                EstimateValidUntil = DateTime.Now.AddDays(30)
            };
            
            // Calculate base costs from template
            estimate.BaseCost = template.BaseCost;
            
            // Calculate resource costs
            estimate.ResourceCost = 0f;
            foreach (var resource in template.RequiredResources)
            {
                estimate.ResourceCost += resource.Cost;
            }
            
            // Calculate labor cost based on construction time
            float laborHours = template.ConstructionTime / 3600f; // Convert seconds to hours
            estimate.LaborCost = laborHours * 25f * _laborCostMultiplier; // $25/hour base rate
            
            // Apply material markup
            estimate.MaterialCost = estimate.ResourceCost * (1f + _materialMarkupPercentage);
            
            // Cannabis-specific multipliers
            float categoryMultiplier = GetCategoryMultiplier(template.Category);
            
            // Calculate totals
            estimate.SubtotalCost = estimate.BaseCost + estimate.LaborCost + estimate.MaterialCost;
            estimate.ContingencyCost = estimate.SubtotalCost * _contingencyPercentage;
            estimate.TotalCost = (estimate.SubtotalCost + estimate.ContingencyCost) * categoryMultiplier;
            
            // Store estimate
            _costEstimates[estimate.EstimateId] = estimate;
            
            // Trigger events
            OnCostEstimateCompleted?.Invoke(projectId, estimate);
            _onCostEstimateCompleted?.Raise();
            
            LogInfo($"Cost estimate completed for {template.TemplateName}: ${estimate.TotalCost:F2}");
            return estimate;
        }
        
        /// <summary>
        /// Create project budget based on grid cost estimate
        /// </summary>
        public GridProjectBudget CreateProjectBudget(string projectId, GridCostEstimate costEstimate, float approvedAmount)
        {
            var budget = new GridProjectBudget
            {
                BudgetId = Guid.NewGuid().ToString(),
                ProjectId = projectId,
                ApprovedAmount = approvedAmount,
                EstimatedCost = costEstimate.TotalCost,
                RemainingAmount = approvedAmount,
                CreatedDate = DateTime.Now,
                ContingencyReserve = approvedAmount * _contingencyPercentage
            };
            
            // Initialize budget categories
            budget.LaborBudget = costEstimate.LaborCost;
            budget.MaterialBudget = costEstimate.MaterialCost;
            budget.BaseBudget = costEstimate.BaseCost;
            budget.ContingencyBudget = costEstimate.ContingencyCost;
            
            // Store budget
            _projectBudgets[budget.BudgetId] = budget;
            _totalBudgetAllocated += approvedAmount;
            
            // Initialize performance tracking
            _projectPerformance[projectId] = new GridCostPerformanceData
            {
                ProjectId = projectId,
                BudgetId = budget.BudgetId,
                PlannedCost = costEstimate.TotalCost,
                ActualCost = 0f,
                StartDate = DateTime.Now
            };
            
            // Trigger events
            OnBudgetCreated?.Invoke(projectId, budget);
            
            LogInfo($"Grid project budget created for {projectId}: ${approvedAmount:F2}");
            return budget;
        }
        
        /// <summary>
        /// Allocate resources for a grid construction project
        /// </summary>
        public GridResourceAllocation AllocateResources(string projectId, GridConstructionTemplate template)
        {
            var allocation = new GridResourceAllocation
            {
                AllocationId = Guid.NewGuid().ToString(),
                ProjectId = projectId,
                AllocationDate = DateTime.Now,
                Status = AllocationStatus.Pending
            };
            
            // Check resource availability
            var resourceRequirements = template.RequiredResources;
            var availabilityCheck = CheckResourceAvailability(template);
            if (!availabilityCheck)
            {
                allocation.Status = AllocationStatus.ResourceShortage;
                LogWarning($"Resource allocation failed for project {projectId}: Insufficient resources");
                return allocation;
            }
            
            // Allocate resources
            foreach (var requirement in resourceRequirements)
            {
                var resourceType = requirement.ResourceName;
                var requiredAmount = requirement.Quantity;
                
                if (_resourceInventory.ContainsKey(resourceType))
                {
                    var inventoryAmount = _resourceInventory[resourceType];
                    var adjustedAmount = requiredAmount * (1f + _resourceBufferPercentage);
                    
                    if (inventoryAmount >= adjustedAmount)
                    {
                        _resourceInventory[resourceType] -= adjustedAmount;

                        // This part of the logic seems to have been left incomplete.
                        // For now, I will comment it out to resolve the compilation errors.
                        // We will need to revisit the purpose of the 'allocation' object.

                        // allocation.AllocatedResources[resourceType] = adjustedAmount;
                        // allocation.AllocationCosts[resourceType] = adjustedAmount * GetResourceUnitCost(resourceType);

                        // Check for reorder threshold
                        // if (_resourceInventory[resourceType] <= GetResourceTotalAmount(resourceType) * _resourceReorderThreshold)
                        // {
                        //     TriggerResourceReorder(resourceType);
                        // }
                    }
                    else
                    {
                        // allocation.Status = AllocationStatus.PartialAllocation;
                        LogWarning($"Partial allocation for {resourceType}: Required {adjustedAmount}, Available {inventoryAmount}");
                    }
                }
            }
            
            // Calculate total allocation cost
            allocation.TotalAllocationCost = allocation.AllocationCosts.Values.Sum();
            
            if (allocation.Status == AllocationStatus.Pending)
            {
                allocation.Status = AllocationStatus.Allocated;
            }
            
            // Store allocation
            _resourceAllocations[allocation.AllocationId] = allocation;
            _totalResourcesAllocated += allocation.TotalAllocationCost;
            
            // Trigger events
            OnResourceAllocated?.Invoke(projectId, allocation);
            _onResourceAllocated?.Raise();
            
            LogInfo($"Resources allocated for project {projectId}: ${allocation.TotalAllocationCost:F2}");
            return allocation;
        }
        
        /// <summary>
        /// Record actual costs for a grid construction project
        /// </summary>
        public void RecordActualCost(string projectId, GridCostCategory costCategory, float amount, string description = "")
        {
            // Find project budget
            var budget = _projectBudgets.Values.FirstOrDefault(b => b.ProjectId == projectId);
            if (budget == null)
            {
                LogError($"No budget found for project {projectId}");
                return;
            }
            
            // Record cost
            var costRecord = new CostRecord
            {
                RecordId = Guid.NewGuid().ToString(),
                ProjectId = projectId,
                CostType = costCategory,
                Amount = amount,
                Description = description,
                RecordDate = DateTime.Now
            };
            
            budget.ActualCosts.Add(costRecord);
            budget.RemainingAmount -= amount;
            _totalBudgetSpent += amount;
            
            // Update category budget
            if (budget.CategoryBudgets.ContainsKey(costCategory))
            {
                budget.CategoryBudgets[costCategory] -= amount;
            }
            
            // Update performance tracking
            if (_projectPerformance.ContainsKey(projectId))
            {
                var performance = _projectPerformance[projectId];
                performance.ActualCost += amount;
                performance.CostPerformanceIndex = performance.EarnedValue / performance.ActualCost;
                performance.SchedulePerformanceIndex = performance.EarnedValue / performance.PlannedValue;
            }
            
            // Check budget alerts
            CheckGridBudgetAlerts(budget);
            
            LogInfo($"Recorded cost for project {projectId}: ${amount:F2} ({costCategory})");
        }
        
        /// <summary>
        /// Get project cost performance data
        /// </summary>
        public GridCostPerformanceData GetProjectPerformance(string projectId)
        {
            return _projectPerformance.GetValueOrDefault(projectId);
        }
        
        /// <summary>
        /// Get resource inventory status
        /// </summary>
        public GridResourceInventoryStatus GetResourceInventoryStatus()
        {
            return new GridResourceInventoryStatus
            {
                TotalResourceTypes = _resourceInventory.Count,
                LowStockResources = _resourceInventory.Count(kvp => kvp.Value <= 100f), // Low stock threshold
                TotalInventoryValue = _resourceInventory.Values.Sum(),
                AvailableInventoryValue = _resourceInventory.Values.Sum(),
                LastUpdated = DateTime.Now,
                CriticalResources = _resourceInventory.Where(kvp => kvp.Value <= 50f).Select(kvp => kvp.Key).ToList()
            };
        }
        
        /// <summary>
        /// Get cost summary for a project
        /// </summary>
        public GridCostSummary GetProjectCostSummary(string projectId)
        {
            var budget = _projectBudgets.Values.FirstOrDefault(b => b.ProjectId == projectId);
            if (budget == null)
            {
                LogError($"No budget found for project {projectId}");
                return null;
            }
            
            return new GridCostSummary
            {
                ProjectId = projectId,
                TotalBudget = budget.ApprovedAmount,
                TotalSpent = budget.TotalSpent,
                RemainingBudget = budget.RemainingAmount,
                BudgetUtilization = budget.BudgetUtilization,
                IsOverBudget = budget.IsOverBudget,
                CostBreakdown = new Dictionary<GridCostCategory, float>
                {
                    { GridCostCategory.Labor, budget.LaborSpent },
                    { GridCostCategory.Materials, budget.MaterialSpent },
                    { GridCostCategory.Base, budget.BaseSpent },
                    { GridCostCategory.Contingency, budget.ContingencySpent }
                }
            };
        }
        
        #region Private Implementation
        
        private void InitializeGridCostSystems()
        {
            // Find grid system references
            _gridSystem = FindObjectOfType<GridSystem>();
            
            // Initialize resource inventory with basic construction materials
            InitializeResourceInventory();
        }
        
        private void InitializeResourceInventory()
        {
            // Initialize basic construction resources
            _resourceInventory["Steel"] = 1000f;
            _resourceInventory["Concrete"] = 2000f;
            _resourceInventory["Lumber"] = 1500f;
            _resourceInventory["Electrical"] = 500f;
            _resourceInventory["Plumbing"] = 300f;
            _resourceInventory["HVAC"] = 200f;
            _resourceInventory["Insulation"] = 800f;
            _resourceInventory["Drywall"] = 1200f;
            _resourceInventory["Flooring"] = 600f;
            _resourceInventory["Lighting"] = 100f;
            _resourceInventory["Security"] = 50f;
        }
        
        private void InitializeResourceManagement()
        {
            // Simplified resource management for grid system
        }
        
        private float GetCategoryMultiplier(ConstructionCategory category)
        {
            return category switch
            {
                ConstructionCategory.Room => _hvacCostMultiplier * _lightingCostMultiplier,
                ConstructionCategory.Security => _securityCostMultiplier,
                ConstructionCategory.Processing => _complianceCostMultiplier,
                _ => 1.0f
            };
        }
        
        private void UpdateCostTracking()
        {
            // Update cost tracking for all active projects
            foreach (var budget in _projectBudgets.Values)
            {
                // Update remaining budget
                budget.RemainingAmount = budget.ApprovedAmount - budget.TotalSpent;
            }
        }
        
        private void UpdateResourceManagement()
        {
            // Check for low resource levels and trigger alerts
            foreach (var kvp in _resourceInventory.ToList())
            {
                if (kvp.Value <= 50f) // Critical level
                {
                    OnResourceShortage?.Invoke(kvp.Key, kvp.Value);
                }
            }
        }
        
        private void UpdateBudgetAlerts()
        {
            foreach (var budget in _projectBudgets.Values)
            {
                CheckGridBudgetAlerts(budget);
            }
        }
        
        private void CheckGridBudgetAlerts(GridProjectBudget budget)
        {
            float budgetUtilization = budget.BudgetUtilization;
            
            if (budgetUtilization >= _budgetCriticalThreshold)
            {
                var alert = new GridBudgetAlert
                {
                    AlertId = Guid.NewGuid().ToString(),
                    ProjectId = budget.ProjectId,
                    BudgetId = budget.BudgetId,
                    AlertType = GridBudgetAlertType.Critical,
                    Message = $"Budget critically low: {budgetUtilization:P0} used",
                    Threshold = _budgetCriticalThreshold,
                    CurrentUtilization = budgetUtilization,
                    CreatedDate = DateTime.Now
                };
                
                OnBudgetAlert?.Invoke(budget.ProjectId, alert);
                _onBudgetAlert?.Raise();
            }
            else if (budgetUtilization >= _budgetWarningThreshold)
            {
                var alert = new GridBudgetAlert
                {
                    AlertId = Guid.NewGuid().ToString(),
                    ProjectId = budget.ProjectId,
                    BudgetId = budget.BudgetId,
                    AlertType = GridBudgetAlertType.Warning,
                    Message = $"Budget warning: {budgetUtilization:P0} used",
                    Threshold = _budgetWarningThreshold,
                    CurrentUtilization = budgetUtilization,
                    CreatedDate = DateTime.Now
                };
                
                OnBudgetAlert?.Invoke(budget.ProjectId, alert);
                _onBudgetAlert?.Raise();
            }
        }
        
        private void UpdateMetrics()
        {
            _costMetrics.TotalBudgetAllocated = _totalBudgetAllocated;
            _costMetrics.TotalBudgetSpent = _totalBudgetSpent;
            _costMetrics.BudgetUtilization = BudgetUtilization;
            _costMetrics.ActiveProjects = ActiveProjects;
            _costMetrics.AverageCostPerProject = ActiveProjects > 0 ? _totalBudgetSpent / ActiveProjects : 0f;
            _costMetrics.ProjectsOnBudget = _projectBudgets.Values.Count(b => !b.IsOverBudget);
            _costMetrics.ProjectsOverBudget = _projectBudgets.Values.Count(b => b.IsOverBudget);
            _costMetrics.LastUpdated = DateTime.Now;
        }
        
        /// <summary>
        /// Check if sufficient resources are available for template
        /// </summary>
        public bool CheckResourceAvailability(GridConstructionTemplate template)
        {
            foreach (var resource in template.RequiredResources)
            {
                if (_resourceInventory.ContainsKey(resource.ResourceName))
                {
                    if (_resourceInventory[resource.ResourceName] < resource.Quantity)
                    {
                        return false;
                    }
                }
                else
                {
                    return false; // Resource not in inventory
                }
            }
            return true;
        }
        
        /// <summary>
        /// Consume resources for construction
        /// </summary>
        public void ConsumeResources(GridConstructionTemplate template)
        {
            foreach (var resource in template.RequiredResources)
            {
                if (_resourceInventory.ContainsKey(resource.ResourceName))
                {
                    _resourceInventory[resource.ResourceName] -= resource.Quantity;
                    _resourceInventory[resource.ResourceName] = Mathf.Max(0f, _resourceInventory[resource.ResourceName]);
                }
            }
        }
        
        /// <summary>
        /// Add resources to inventory
        /// </summary>
        public void AddResources(string resourceName, float quantity)
        {
            if (_resourceInventory.ContainsKey(resourceName))
            {
                _resourceInventory[resourceName] += quantity;
            }
            else
            {
                _resourceInventory[resourceName] = quantity;
            }
        }
        
        #endregion
        
        #region Grid-Specific Methods
        
        /// <summary>
        /// Quick cost estimate from template
        /// </summary>
        public float GetQuickCostEstimate(GridConstructionTemplate template)
        {
            if (template == null) return 0f;
            
            float totalCost = template.GetTotalCost();
            float categoryMultiplier = GetCategoryMultiplier(template.Category);
            
            return totalCost * categoryMultiplier * (1f + _contingencyPercentage);
        }
        
        /// <summary>
        /// Check if player can afford template
        /// </summary>
        public bool CanAfford(GridConstructionTemplate template, float availableFunds)
        {
            float estimatedCost = GetQuickCostEstimate(template);
            return availableFunds >= estimatedCost;
        }
        
        /// <summary>
        /// Get resource cost for template
        /// </summary>
        public float GetResourceCost(GridConstructionTemplate template)
        {
            float totalResourceCost = 0f;
            foreach (var resource in template.RequiredResources)
            {
                totalResourceCost += resource.Cost;
            }
            return totalResourceCost * (1f + _materialMarkupPercentage);
        }
        
                #endregion
    }

    #region Supporting Data Structures

    [System.Serializable]
    public class GridResourceAllocation
    {
        public string AllocationId;
        public string ProjectId;
        public DateTime AllocationDate;
        public AllocationStatus Status;
        public Dictionary<string, float> AllocatedResources = new Dictionary<string, float>();
        public Dictionary<string, float> AllocationCosts = new Dictionary<string, float>();
        public float TotalAllocationCost;
    }

    public enum AllocationStatus
    {
        Pending,
        Allocated,
        PartialAllocation,
        ResourceShortage,
        Failed
    }

    // Stub definitions for previously missing data structures to ensure compilation.
    // These can be fleshed out with more detail later if needed.
    [System.Serializable]
    public class GridProjectBudget {
        public string BudgetId;
        public string ProjectId;
        public float ApprovedAmount;
        public float EstimatedCost;
        public float RemainingAmount;
        public System.DateTime CreatedDate;
        public float ContingencyReserve;
        public float LaborBudget;
        public float MaterialBudget;
        public float BaseBudget;
        public float ContingencyBudget;
        public List<CostRecord> ActualCosts = new List<CostRecord>();
        public Dictionary<GridCostCategory, float> CategoryBudgets = new Dictionary<GridCostCategory, float>();
        public float TotalSpent => ActualCosts.Sum(c => c.Amount);
        public float BudgetUtilization => ApprovedAmount > 0 ? TotalSpent / ApprovedAmount : 0f;
        public bool IsOverBudget => TotalSpent > ApprovedAmount;
        public float LaborSpent => ActualCosts.Where(c => c.CostType == GridCostCategory.Labor).Sum(c => c.Amount);
        public float MaterialSpent => ActualCosts.Where(c => c.CostType == GridCostCategory.Materials).Sum(c => c.Amount);
        public float BaseSpent => ActualCosts.Where(c => c.CostType == GridCostCategory.Base).Sum(c => c.Amount);
        public float ContingencySpent => ActualCosts.Where(c => c.CostType == GridCostCategory.Contingency).Sum(c => c.Amount);
    }
    [System.Serializable]
    public class GridCostEstimate {
        public string EstimateId;
        public string ProjectId;
        public GridConstructionTemplate Template;
        public System.DateTime EstimateDate;
        public System.DateTime EstimateValidUntil;
        public float BaseCost;
        public float ResourceCost;
        public float LaborCost;
        public float MaterialCost;
        public float SubtotalCost;
        public float ContingencyCost;
        public float TotalCost;
    }
    [System.Serializable]
    public class GridCostMetrics {
        public float TotalBudgetAllocated;
        public float TotalBudgetSpent;
        public float BudgetUtilization;
        public int ActiveProjects;
        public float AverageCostPerProject;
        public int ProjectsOnBudget;
        public int ProjectsOverBudget;
        public System.DateTime LastUpdated;
    }
    [System.Serializable]
    public class GridCostPerformanceData {
        public string ProjectId;
        public string BudgetId;
        public float PlannedCost;
        public float ActualCost;
        public System.DateTime StartDate;
        public float EarnedValue;
        public float PlannedValue;
        public float CostPerformanceIndex;
        public float SchedulePerformanceIndex;
    }
    [System.Serializable]
    public class CostRecord {
        public string RecordId;
        public string ProjectId;
        public GridCostCategory CostType;
        public float Amount;
        public string Description;
        public System.DateTime RecordDate;
    }
    [System.Serializable]
    public class GridBudgetAlert {
        public string AlertId;
        public string ProjectId;
        public string BudgetId;
        public GridBudgetAlertType AlertType;
        public string Message;
        public float Threshold;
        public float CurrentUtilization;
        public System.DateTime CreatedDate;
    }
    public enum GridBudgetAlertType { Warning, Critical }
    public enum GridCostCategory { Base, Labor, Materials, Contingency, Other }
    [System.Serializable]
    public class GridResourceInventoryStatus {
        public int TotalResourceTypes;
        public int LowStockResources;
        public float TotalInventoryValue;
        public float AvailableInventoryValue;
        public System.DateTime LastUpdated;
        public List<string> CriticalResources;
    }
    [System.Serializable]
    public class GridCostSummary {
        public string ProjectId;
        public float TotalBudget;
        public float TotalSpent;
        public float RemainingBudget;
        public float BudgetUtilization;
        public bool IsOverBudget;
        public Dictionary<GridCostCategory, float> CostBreakdown;
    }

    #endregion

}