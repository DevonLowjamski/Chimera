using ProjectChimera.Data.Construction;
using ProjectChimera.Data.Economy;
using System.Collections.Generic;
using UnityEngine;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Construction
{
    /// <summary>
    /// Interface for cost calculation and pricing logic
    /// </summary>
    public interface ICostCalculator
    {
        float HeightCostMultiplier { get; set; }
        float FoundationCostMultiplier { get; set; }
        bool EnableBulkDiscounts { get; set; }
        float BulkDiscountThreshold { get; set; }
        float BulkDiscountRate { get; set; }
        
        Dictionary<PlaceableType, CostProfile> BaseCosts { get; }
        Dictionary<Vector3Int, float> PositionCostModifiers { get; }
        
        CostCalculationResult CalculatePlacementCost(GridPlaceable placeable, Vector3Int gridPosition);
        CostEstimate GetCostEstimate(GridPlaceable placeable, Vector3Int gridPosition);
        
        float CalculateHeightModifier(int height);
        float CalculateBulkDiscount(int quantity);
        float CalculatePositionModifier(Vector3Int gridPosition);
        
        bool RequiresFoundation(Vector3Int gridPosition);
        void SetPositionCostModifier(Vector3Int position, float modifier);
        void RemovePositionCostModifier(Vector3Int position);
        
        void InitializeBaseCosts();
        void UpdateCostProfile(PlaceableType type, CostProfile profile);
        CostProfile GetCostProfile(PlaceableType type);
        
        void Initialize(float heightCostMultiplier, float foundationCostMultiplier, 
                       bool enableBulkDiscounts, float bulkDiscountThreshold, float bulkDiscountRate);
        void Shutdown();
    }

    [System.Serializable]
    public class CostProfile
    {
        public float baseCost = 100f;
        public List<ResourceCost> resourceCosts = new List<ResourceCost>();
        public bool scalableWithSize = true;
        public float complexityMultiplier = 1f;
    }

    [System.Serializable]
    public class ResourceCost
    {
        public string resourceId;
        public int quantity;
        public bool isRequired = true;
    }

    public struct CostCalculationResult
    {
        public float TotalCost;
        public List<ResourceCost> ResourceCosts;
        public Dictionary<string, float> Breakdown;
    }

    public struct CostEstimate
    {
        public float TotalCost;
        public List<ResourceCost> ResourceCosts;
        public Dictionary<string, float> CostBreakdown;
        public float HeightModifier;
        public float FoundationModifier;
        public float BulkDiscount;
        public float PositionModifier;
    }
}
