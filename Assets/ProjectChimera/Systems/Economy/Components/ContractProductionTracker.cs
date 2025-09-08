using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;
using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Data.Economy;
using ProjectChimera.Data.Shared;
using System.Collections.Generic;
using System.Linq;
using System;

namespace ProjectChimera.Systems.Economy.Components
{
    /// <summary>
    /// Handles plant production registration, allocation, and availability tracking
    /// for Project Chimera's game economy system.
    /// </summary>
    public class ContractProductionTracker : MonoBehaviour, ITickable
    {
        [Header("Production Tracking Configuration")]
        [SerializeField] private bool _enableProductionTracking = true;
        [SerializeField] private bool _enableAutoAllocation = true;
        [SerializeField] private float _trackingUpdateInterval = 30f;
        [SerializeField] private int _maxProductionHistorySize = 1000;
        [SerializeField] private bool _enableStrainGrouping = true;

        // Production tracking state
        private Dictionary<string, List<PlantProductionRecord>> _contractProduction = new Dictionary<string, List<PlantProductionRecord>>();
        private List<PlantProductionRecord> _unallocatedProduction = new List<PlantProductionRecord>();
        private Dictionary<StrainType, List<PlantProductionRecord>> _strainProduction = new Dictionary<StrainType, List<PlantProductionRecord>>();
        private List<PlantProductionRecord> _allProduction = new List<PlantProductionRecord>();

        // Allocation tracking
        private Dictionary<string, List<string>> _contractAllocations = new Dictionary<string, List<string>>(); // contractId -> plantIds
        private Dictionary<string, string> _plantToContractMapping = new Dictionary<string, string>(); // plantId -> contractId

        // Performance tracking
        private float _lastTrackingUpdate;
        private int _totalPlantsProcessed;

        // Events
        public System.Action<PlantProductionRecord> OnPlantProductionRegistered;
        public System.Action<string, List<PlantProductionRecord>> OnProductionAllocated;
        public System.Action<PlantProductionRecord> OnProductionDeallocated;

        // Properties
        public int TotalPlantsTracked => _allProduction.Count;
        public int UnallocatedPlantsCount => _unallocatedProduction.Count;
        public bool ProductionTrackingEnabled => _enableProductionTracking;
        public bool AutoAllocationEnabled => _enableAutoAllocation;

            public void Tick(float deltaTime)
    {
            if (_enableProductionTracking && Time.time - _lastTrackingUpdate >= _trackingUpdateInterval)
            {
                PerformMaintenanceTasks();
                _lastTrackingUpdate = Time.time;

    }
        }

        public void Initialize()
        {
            LogInfo("Contract production tracker initialized for game economy");
        }

        #region Plant Production Registration

        /// <summary>
        /// Register harvested plants that could fulfill contracts
        /// </summary>
        public void RegisterHarvestedPlant(string plantId, StrainType strainType, float quantity, float quality)
        {
            if (!_enableProductionTracking) return;

            if (string.IsNullOrEmpty(plantId) || quantity <= 0f)
            {
                LogWarning($"Invalid plant data provided: ID={plantId}, Quantity={quantity}");
                return;
            }

            var record = new PlantProductionRecord
            {
                PlantId = plantId,
                StrainType = strainType,
                Quantity = (int)quantity,
                Quality = QualityGradeExtensions.FromFloat(quality),
                HarvestDate = DateTime.Now,
                IsAllocated = false
            };

            // Add to all production tracking
            _allProduction.Add(record);
            _unallocatedProduction.Add(record);

            // Add to strain-specific tracking
            if (_enableStrainGrouping)
            {
                if (!_strainProduction.ContainsKey(strainType))
                {
                    _strainProduction[strainType] = new List<PlantProductionRecord>();
                }
                _strainProduction[strainType].Add(record);
            }

            _totalPlantsProcessed++;

            // Auto-allocate to contracts if enabled
            if (_enableAutoAllocation)
            {
                AutoAllocateToContracts(record);
            }

            OnPlantProductionRegistered?.Invoke(record);
            LogInfo($"Plant production registered: {plantId} - {strainType}, Quality: {quality:P1}, Quantity: {quantity}g");
        }

        /// <summary>
        /// Register multiple harvested plants at once
        /// </summary>
        public void RegisterHarvestedPlants(List<PlantProductionData> plants)
        {
            if (plants == null || plants.Count == 0) return;

            foreach (var plantData in plants)
            {
                RegisterHarvestedPlant(plantData.PlantId, plantData.StrainType, (float)plantData.Quantity, plantData.Quality.ToFloat());
            }

            LogInfo($"Registered {plants.Count} plant productions in batch");
        }

        /// <summary>
        /// Remove a plant production record (e.g., if used elsewhere)
        /// </summary>
        public bool RemovePlantProduction(string plantId)
        {
            // Find and remove from all tracking collections
            var record = _allProduction.FirstOrDefault(p => p.PlantId == plantId);
            if (record == null) return false;

            // Remove from main collections
            _allProduction.Remove(record);
            _unallocatedProduction.Remove(record);

            // Remove from strain tracking
            if (_strainProduction.ContainsKey(record.StrainType))
            {
                _strainProduction[record.StrainType].Remove(record);
            }

            // Remove from contract allocations if allocated
            if (record.IsAllocated && _plantToContractMapping.TryGetValue(plantId, out var contractId))
            {
                DeallocateFromContract(plantId, contractId);
            }

            LogInfo($"Plant production removed: {plantId}");
            return true;
        }

        #endregion

        #region Contract Allocation

        /// <summary>
        /// Allocate available production to contracts
        /// </summary>
        public void AllocateToContract(string contractId, List<string> plantIds)
        {
            if (string.IsNullOrEmpty(contractId) || plantIds == null || plantIds.Count == 0)
            {
                LogWarning("Invalid contract allocation data provided");
                return;
            }

            var allocatedRecords = new List<PlantProductionRecord>();

            foreach (var plantId in plantIds)
            {
                var record = _unallocatedProduction.FirstOrDefault(p => p.PlantId == plantId);
                if (record == null)
                {
                    LogWarning($"Plant {plantId} not found in unallocated production");
                    continue;
                }

                // Allocate the plant
                record.IsAllocated = true;
                record.AllocationDate = DateTime.Now;

                // Add to contract production tracking
                if (!_contractProduction.ContainsKey(contractId))
                {
                    _contractProduction[contractId] = new List<PlantProductionRecord>();
                    _contractAllocations[contractId] = new List<string>();
                }

                _contractProduction[contractId].Add(record);
                _contractAllocations[contractId].Add(plantId);
                _plantToContractMapping[plantId] = contractId;

                // Remove from unallocated
                _unallocatedProduction.Remove(record);
                allocatedRecords.Add(record);
            }

            if (allocatedRecords.Count > 0)
            {
                OnProductionAllocated?.Invoke(contractId, allocatedRecords);
                LogInfo($"Allocated {allocatedRecords.Count} plants to contract {contractId}");
            }
        }

        /// <summary>
        /// Auto-allocate a plant to suitable contracts
        /// </summary>
        private void AutoAllocateToContracts(PlantProductionRecord record)
        {
            // This would typically check active contracts and auto-assign
            // For now, just keep in unallocated pool
            LogInfo($"Plant {record.PlantId} added to unallocated production pool for auto-allocation");
        }

        /// <summary>
        /// Deallocate a plant from a contract
        /// </summary>
        public bool DeallocateFromContract(string plantId, string contractId)
        {
            if (!_plantToContractMapping.TryGetValue(plantId, out var mappedContractId) || mappedContractId != contractId)
            {
                LogWarning($"Plant {plantId} is not allocated to contract {contractId}");
                return false;
            }

            // Find the record
            var record = _contractProduction[contractId].FirstOrDefault(p => p.PlantId == plantId);
            if (record == null) return false;

            // Deallocate
            record.IsAllocated = false;
            record.AllocationDate = DateTime.MinValue;

            // Remove from contract tracking
            _contractProduction[contractId].Remove(record);
            _contractAllocations[contractId].Remove(plantId);
            _plantToContractMapping.Remove(plantId);

            // Add back to unallocated
            _unallocatedProduction.Add(record);

            OnProductionDeallocated?.Invoke(record);
            LogInfo($"Plant {plantId} deallocated from contract {contractId}");

            return true;
        }

        /// <summary>
        /// Get all production records for a specific contract
        /// </summary>
        public List<PlantProductionRecord> GetContractProduction(string contractId)
        {
            return _contractProduction.TryGetValue(contractId, out var production)
                ? new List<PlantProductionRecord>(production)
                : new List<PlantProductionRecord>();
        }

        /// <summary>
        /// Remove all production tracking for a contract (when completed)
        /// </summary>
        public void ClearContractProduction(string contractId)
        {
            if (_contractProduction.ContainsKey(contractId))
            {
                var plantIds = _contractAllocations[contractId].ToList();

                // Remove mapping entries
                foreach (var plantId in plantIds)
                {
                    _plantToContractMapping.Remove(plantId);
                }

                _contractProduction.Remove(contractId);
                _contractAllocations.Remove(contractId);

                LogInfo($"Cleared production tracking for contract {contractId}");
            }
        }

        #endregion

        #region Production Queries

        /// <summary>
        /// Get available production for a specific strain type
        /// </summary>
        public List<PlantProductionRecord> GetAvailableProduction(StrainType strainType, float minimumQuality = 0f)
        {
            return _unallocatedProduction
                .Where(p => p.StrainType == strainType && p.Quality.IsGreaterThanOrEqualFloat(minimumQuality))
                .OrderByDescending(p => p.Quality)
                .ToList();
        }

        /// <summary>
        /// Get available production that can fulfill contract requirements
        /// </summary>
        public List<PlantProductionRecord> GetAvailableProductionForContract(ActiveContractSO contract)
        {
            if (contract == null) return new List<PlantProductionRecord>();

            return _unallocatedProduction
                .Where(p => p.StrainType == contract.RequiredStrain && p.Quality.IsGreaterThanOrEqualFloat(contract.MinimumQuality))
                .OrderByDescending(p => p.Quality)
                .ToList();
        }

        /// <summary>
        /// Get production summary by strain type
        /// </summary>
        public Dictionary<StrainType, ProductionSummary> GetProductionSummaryByStrain()
        {
            var summary = new Dictionary<StrainType, ProductionSummary>();

            foreach (var kvp in _strainProduction)
            {
                var strainType = kvp.Key;
                var production = kvp.Value;

                summary[strainType] = new ProductionSummary
                {
                    StrainType = strainType,
                    TotalPlants = production.Count,
                    AllocatedPlants = production.Count(p => p.IsAllocated),
                    UnallocatedPlants = production.Count(p => !p.IsAllocated),
                    TotalQuantity = production.Sum(p => p.Quantity),
                    AverageQuality = production.Count > 0 ? QualityGradeExtensions.FromFloat(production.Average(p => p.Quality.ToFloat())) : QualityGrade.BelowStandard,
                    BestQuality = production.Count > 0 ? production.Select(p => p.Quality).Max() : QualityGrade.BelowStandard
                };
            }

            return summary;
        }

        /// <summary>
        /// Get overall production statistics
        /// </summary>
        public ProductionStatistics GetProductionStatistics()
        {
            var stats = new ProductionStatistics
            {
                TotalPlantsProcessed = _totalPlantsProcessed,
                TotalPlantsTracked = _allProduction.Count,
                UnallocatedPlants = _unallocatedProduction.Count,
                AllocatedPlants = _allProduction.Count(p => p.IsAllocated)
            };

            if (_allProduction.Count > 0)
            {
                stats.TotalQuantityProduced = _allProduction.Sum(p => p.Quantity);
                stats.AverageQuality = QualityGradeExtensions.FromFloat(_allProduction.Average(p => p.Quality.ToFloat()));
                stats.BestQuality = _allProduction.Select(p => p.Quality).Max();
                stats.WorstQuality = _allProduction.Select(p => p.Quality).Min();
            }

            stats.ActiveStrainTypes = _strainProduction.Keys.ToList();
            stats.ContractsWithProduction = _contractProduction.Keys.Count;

            return stats;
        }

        /// <summary>
        /// Find best plants for a contract based on quality and quantity needs
        /// </summary>
        public List<PlantProductionRecord> FindBestPlantsForContract(ActiveContractSO contract, float targetQuantity)
        {
            var availablePlants = GetAvailableProductionForContract(contract);
            var selectedPlants = new List<PlantProductionRecord>();
            float currentQuantity = 0f;

            // Sort by quality (descending) and select best plants up to target quantity
            foreach (var plant in availablePlants.OrderByDescending(p => p.Quality))
            {
                selectedPlants.Add(plant);
                currentQuantity += plant.Quantity;

                if (currentQuantity >= targetQuantity)
                    break;
            }

            return selectedPlants;
        }

        #endregion

        #region Maintenance and Cleanup

        /// <summary>
        /// Perform periodic maintenance tasks
        /// </summary>
        private void PerformMaintenanceTasks()
        {
            // Clean up old production records
            CleanupOldProduction();

            // Validate allocation consistency
            ValidateAllocationConsistency();

            LogInfo($"Maintenance completed - Tracking {_allProduction.Count} plants, {_unallocatedProduction.Count} unallocated");
        }

        /// <summary>
        /// Clean up old production records to prevent memory buildup
        /// </summary>
        private void CleanupOldProduction()
        {
            if (_allProduction.Count <= _maxProductionHistorySize) return;

            var cutoffDate = DateTime.Now.AddDays(-30); // Keep 30 days of history
            var toRemove = _allProduction
                .Where(p => p.HarvestDate < cutoffDate && !p.IsAllocated)
                .OrderBy(p => p.HarvestDate)
                .Take(_allProduction.Count - _maxProductionHistorySize)
                .ToList();

            foreach (var record in toRemove)
            {
                RemovePlantProduction(record.PlantId);
            }

            if (toRemove.Count > 0)
            {
                LogInfo($"Cleaned up {toRemove.Count} old production records");
            }
        }

        /// <summary>
        /// Validate that allocation tracking is consistent
        /// </summary>
        private void ValidateAllocationConsistency()
        {
            var inconsistencies = 0;

            // Check if allocated plants are properly mapped
            foreach (var record in _allProduction.Where(p => p.IsAllocated))
            {
                if (!_plantToContractMapping.ContainsKey(record.PlantId))
                {
                    LogWarning($"Allocated plant {record.PlantId} has no contract mapping");
                    record.IsAllocated = false; // Fix the inconsistency
                    _unallocatedProduction.Add(record);
                    inconsistencies++;
                }
            }

            if (inconsistencies > 0)
            {
                LogWarning($"Fixed {inconsistencies} allocation inconsistencies");
            }
        }

        #endregion

        #region Plant Production Helpers

        public PlantProductionRecord GetPlantRecord(string plantId)
        {
            return _allProduction.FirstOrDefault(p => p.PlantId == plantId);
        }

        public bool IsPlantAllocated(string plantId)
        {
            var record = GetPlantRecord(plantId);
            return record?.IsAllocated ?? false;
        }

        public string GetPlantAllocation(string plantId)
        {
            return _plantToContractMapping.TryGetValue(plantId, out var contractId) ? contractId : null;
        }

        #endregion

        private void LogInfo(string message)
        {
            ChimeraLogger.Log($"[ContractProductionTracker] {message}");
        }

        private void LogWarning(string message)
        {
            ChimeraLogger.LogWarning($"[ContractProductionTracker] {message}");
        }

    // ITickable implementation
    public int Priority => 0;
    public bool Enabled => enabled && gameObject.activeInHierarchy;

    public virtual void OnRegistered()
    {
        // Override in derived classes if needed
    }

    public virtual void OnUnregistered()
    {
        // Override in derived classes if needed
    }

}
}
