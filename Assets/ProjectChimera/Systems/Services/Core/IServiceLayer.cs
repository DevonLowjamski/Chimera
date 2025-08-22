using System;
using UnityEngine;
using ProjectChimera.Data.Genetics;
using ProjectChimera.Data.Shared;

namespace ProjectChimera.Systems.Services.Core
{
    /// <summary>
    /// Core service layer interfaces for Phase 2.1 UI to Systems Integration
    /// These interfaces provide a service layer between UI and Manager layers
    /// </summary>

    /// <summary>
    /// Base interface for all service layer components
    /// </summary>
    public interface IServiceLayer
    {
        bool IsInitialized { get; }
        void Initialize();
        void Shutdown();
    }

    /// <summary>
    /// Service interface for Construction pillar operations
    /// Handles facility building, equipment placement, and schematic management
    /// </summary>
    public interface IConstructionService : IServiceLayer
    {
        // Building Operations
        bool CanPlaceStructure(string structureId, Vector3Int gridPosition);
        bool PlaceStructure(string structureId, Vector3Int gridPosition);
        bool RemoveStructure(Vector3Int gridPosition);
        bool CanAffordStructure(string structureId);
        
        // Equipment Operations
        bool CanPlaceEquipment(string equipmentId, Vector3Int gridPosition);
        bool PlaceEquipment(string equipmentId, Vector3Int gridPosition);
        bool RemoveEquipment(Vector3Int gridPosition);
        bool CanAffordEquipment(string equipmentId);
        
        // Utility Operations
        bool InstallUtility(string utilityType, Vector3Int startPosition, Vector3Int endPosition);
        bool RemoveUtility(Vector3Int position);
        bool CanAffordUtility(string utilityType, float length);
        
        // Schematic Operations
        bool CanApplySchematic(string schematicId, Vector3Int position);
        bool ApplySchematic(string schematicId, Vector3Int position);
        bool SaveSchematic(string name, Vector3Int startPosition, Vector3Int endPosition);
        
        // Query Operations
        float GetStructureCost(string structureId);
        float GetEquipmentCost(string equipmentId);
        float GetUtilityCost(string utilityType, float length);
        bool IsPositionOccupied(Vector3Int gridPosition);
    }

    /// <summary>
    /// Service interface for Cultivation pillar operations
    /// Handles plant lifecycle, care, and environmental management
    /// </summary>
    public interface ICultivationService : IServiceLayer
    {
        // Plant Operations
        bool CanPlantSeed(string strainId, Vector3Int gridPosition);
        bool PlantSeed(string strainId, Vector3Int gridPosition, string plantName = null);
        bool RemovePlant(string plantId);
        bool HarvestPlant(string plantId);
        
        // Plant Care Operations
        bool WaterPlant(string plantId);
        bool FeedPlant(string plantId);
        bool TrainPlant(string plantId, string trainingType);
        bool PrunePlant(string plantId);
        
        // Environmental Operations
        bool SetEnvironmentalConditions(string zoneId, EnvironmentalConditions conditions);
        EnvironmentalConditions GetEnvironmentalConditions(string zoneId);
        bool CanAdjustEnvironment(string zoneId);
        
        // Query Operations
        int GetPlantCount();
        int GetHealthyPlantCount();
        int GetHarvestReadyPlantCount();
        float GetAverageHealthScore();
        bool HasPlantsNeedingAttention();
    }

    /// <summary>
    /// Service interface for Genetics pillar operations
    /// Handles breeding, strain management, and genetic research
    /// </summary>
    public interface IGeneticsService : IServiceLayer
    {
        // Breeding Operations
        bool CanBreedPlants(string parentId1, string parentId2);
        bool BreedPlants(string parentId1, string parentId2, out string newStrainId);
        bool CanCreateTissueCulture(string plantId);
        bool CreateTissueCulture(string plantId, string cultureName);
        
        // Micropropagation Operations
        bool CanMicropropagate(string cultureId);
        bool Micropropagate(string cultureId, int quantity, out string[] seedIds);
        
        // Strain Management
        PlantStrainSO GetStrain(string strainId);
        PlantStrainSO[] GetAvailableStrains();
        bool IsStrainUnlocked(string strainId);
        bool HasStrain(string strainId);
        
        // Seed Bank Operations
        bool HasSeeds(string strainId);
        int GetSeedCount(string strainId);
        bool CanAffordSeeds(string strainId, int quantity);
        bool PurchaseSeeds(string strainId, int quantity);
        
        // Research Operations
        bool CanResearchTrait(string traitId);
        bool ResearchTrait(string traitId);
        bool IsTraitDiscovered(string traitId);
        
        // Query Operations
        int GetDiscoveredTraitCount();
        int GetAvailableStrainCount();
        float GetBreedingSuccessRate(string parentId1, string parentId2);
    }

    /// <summary>
    /// Service validation results for command execution
    /// </summary>
    public struct ServiceValidationResult
    {
        public bool IsValid;
        public string ErrorMessage;
        public string RequiredResource;
        public float RequiredAmount;
        
        public static ServiceValidationResult Success() => new ServiceValidationResult { IsValid = true };
        public static ServiceValidationResult Failure(string error) => new ServiceValidationResult { IsValid = false, ErrorMessage = error };
        public static ServiceValidationResult InsufficientResources(string resource, float amount) => 
            new ServiceValidationResult { IsValid = false, ErrorMessage = $"Insufficient {resource}", RequiredResource = resource, RequiredAmount = amount };
    }

    /// <summary>
    /// Service operation results for UI feedback
    /// </summary>
    public struct ServiceOperationResult
    {
        public bool Success;
        public string Message;
        public object Data;
        
        public static ServiceOperationResult Succeeded(string message = null, object data = null) => 
            new ServiceOperationResult { Success = true, Message = message, Data = data };
        public static ServiceOperationResult Failed(string message) => 
            new ServiceOperationResult { Success = false, Message = message };
    }
}