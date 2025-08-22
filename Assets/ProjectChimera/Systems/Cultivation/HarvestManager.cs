using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Core.DependencyInjection;
using ProjectChimera.Data.Cultivation;
// using ProjectChimera.Data.Economy; // Temporarily decoupled
using ProjectChimera.Data.Environment;
using ProjectChimera.Core.Events;
using PlantGrowthStage = ProjectChimera.Data.Shared.PlantGrowthStage;
using ProjectChimera.Data.Genetics;
// using ProjectChimera.Systems.Economy; // Use ProjectChimera.Data.Economy for data types

namespace ProjectChimera.Systems.Cultivation
{
    /// <summary>
    /// PC-013-2: Harvest Manager - Handles plant harvesting and inventory integration
    /// Extracted from monolithic CultivationManager for Single Responsibility Principle
    /// </summary>
    public class HarvestManager : IHarvestManager
    {
        [Header("Events")]
        [SerializeField] private GameEventSO<PlantInstanceSO> _onPlantHarvested;
        
        // Dependencies
        private IPlantLifecycleManager _plantLifecycleManager;
        
        public bool IsInitialized { get; private set; }
        
        public HarvestManager(IPlantLifecycleManager plantLifecycleManager)
        {
            _plantLifecycleManager = plantLifecycleManager;
        }
        
        public void Initialize()
        {
            if (IsInitialized) return;
            
            Debug.Log("[HarvestManager] Initializing harvest management...");
            
            // Register with ServiceLocator for dependency injection
            try
            {
                var serviceLocator = ServiceLocator.Instance;
                serviceLocator.RegisterSingleton<IHarvestManager>(this);
                Debug.Log("[HarvestManager] Registered with ServiceLocator");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[HarvestManager] Failed to register with ServiceLocator: {ex.Message}");
            }
            
            IsInitialized = true;
        }
        
        public void Shutdown()
        {
            if (!IsInitialized) return;
            
            Debug.Log("[HarvestManager] Shutting down harvest management...");
            IsInitialized = false;
        }
        
        /// <summary>
        /// Processes harvest for a plant
        /// </summary>
        public void ProcessHarvest(PlantInstanceSO plant)
        {
            if (!IsInitialized)
            {
                Debug.LogError("[HarvestManager] Cannot process harvest: Manager not initialized.");
                return;
            }
            
            if (plant == null)
            {
                Debug.LogError("[HarvestManager] Cannot process harvest: Plant is null.");
                return;
            }
            
            // Validate plant is ready for harvest
            if (plant.CurrentGrowthStage != PlantGrowthStage.Harvest)
            {
                Debug.LogWarning($"[HarvestManager] Plant '{plant.PlantID}' is not ready for harvest (Stage: {plant.CurrentGrowthStage}).");
                return;
            }
            
            // Calculate harvest results
            float yieldAmount = plant.CalculateYieldPotential() * 100f; // Convert to grams
            float potency = plant.CalculatePotencyPotential();
            float qualityScore = CalculateQualityScore(plant);
            
            // Update lifecycle manager statistics
            if (_plantLifecycleManager is PlantLifecycleManager lifecycleManager)
            {
                lifecycleManager.UpdateYieldStatistics(yieldAmount);
            }
            
            Debug.Log($"[HarvestManager] Harvested plant '{plant.PlantID}': {yieldAmount:F1}g at {potency:F1}% potency, {qualityScore:F1}% quality");
            
            // Add to inventory
            AddHarvestToInventory(plant, yieldAmount, qualityScore);
            
            // Raise harvest event
            _onPlantHarvested?.Raise(plant);
        }
        
        /// <summary>
        /// PC-009-2: Adds harvested plant product to player inventory
        /// </summary>
        public void AddHarvestToInventory(PlantInstanceSO plant, float yieldAmount, float qualityScore)
        {
            if (!IsInitialized)
            {
                Debug.LogError("[HarvestManager] Cannot add harvest to inventory: Manager not initialized.");
                return;
            }
            
            try
            {
                // Economy integration ready for service interface implementation
                Debug.Log($"[HarvestManager] Harvest completed: {yieldAmount}g with quality {qualityScore:F1}% - inventory integration pending");
                
                // Generate unique batch ID from plant data
                string batchId = GenerateBatchId(plant);
                
                // Validate parameters
                yieldAmount = Mathf.Max(0f, yieldAmount);
                qualityScore = Mathf.Clamp(qualityScore, 0f, 100f);
                
                // Inventory integration ready for economy service implementation
                // bool added = tradingManager.PlayerInventory.AddHarvestedProduct(
                //     product: flowerProduct,
                //     quantity: yieldAmount,
                //     qualityScore: qualityScore,
                //     batchId: batchId
                // );
                
                // For now, always consider the harvest as successfully processed
                bool added = true;
                
                if (added)
                {
                    Debug.Log($"[HarvestManager] Successfully processed harvest: {yieldAmount:F1}g (Batch: {batchId}, Quality: {qualityScore:F1}%) - inventory integration pending");
                }
                else
                {
                    Debug.LogWarning($"[HarvestManager] Failed to process harvest");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[HarvestManager] Error adding harvest to inventory: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Harvests a plant by ID
        /// </summary>
        public bool HarvestPlant(string plantId)
        {
            if (!IsInitialized)
            {
                Debug.LogError("[HarvestManager] Cannot harvest plant: Manager not initialized.");
                return false;
            }
            
            var plant = _plantLifecycleManager.GetPlant(plantId);
            if (plant == null)
            {
                Debug.LogWarning($"[HarvestManager] Cannot harvest plant: Plant ID '{plantId}' not found.");
                return false;
            }
            
            // Process harvest
            ProcessHarvest(plant);
            
            // Remove plant from lifecycle manager
            return _plantLifecycleManager.RemovePlant(plantId, true);
        }
        
        /// <summary>
        /// Harvests all plants ready for harvest
        /// </summary>
        public int HarvestAllReadyPlants()
        {
            if (!IsInitialized)
            {
                Debug.LogError("[HarvestManager] Cannot harvest all ready plants: Manager not initialized.");
                return 0;
            }
            
            var harvestReadyPlants = _plantLifecycleManager.GetPlantsByStage(PlantGrowthStage.Harvest);
            int harvestedCount = 0;
            
            foreach (var plant in harvestReadyPlants)
            {
                if (HarvestPlant(plant.PlantID))
                {
                    harvestedCount++;
                }
            }
            
            Debug.Log($"[HarvestManager] Harvested {harvestedCount} plants ready for harvest.");
            return harvestedCount;
        }
        
        /// <summary>
        /// Gets harvest statistics
        /// </summary>
        public (int readyToHarvest, float totalYield, float avgQuality) GetHarvestStatistics()
        {
            if (!IsInitialized) return (0, 0f, 0f);
            
            var harvestReadyPlants = _plantLifecycleManager.GetPlantsByStage(PlantGrowthStage.Harvest);
            int readyCount = 0;
            float totalPotentialYield = 0f;
            float totalQuality = 0f;
            
            foreach (var plant in harvestReadyPlants)
            {
                readyCount++;
                totalPotentialYield += plant.CalculateYieldPotential() * 100f;
                totalQuality += CalculateQualityScore(plant);
            }
            
            float avgQuality = readyCount > 0 ? totalQuality / readyCount : 0f;
            
            return (readyCount, totalPotentialYield, avgQuality);
        }
        
        /// <summary>
        /// Calculates quality score for a plant
        /// </summary>
        private float CalculateQualityScore(PlantInstanceSO plant)
        {
            if (plant == null) return 0f;
            
            // Quality based on overall health, potency, and growth completion
            float healthScore = plant.OverallHealth * 40f; // 40% weight
            float potencyScore = plant.CalculatePotencyPotential() * 0.4f; // 40% weight  
            float completionScore = (plant.CurrentGrowthStage == PlantGrowthStage.Harvest) ? 20f : 0f; // 20% weight
            
            return Mathf.Clamp(healthScore + potencyScore + completionScore, 0f, 100f);
        }
        
        /// <summary>
        /// Generates a unique batch ID for harvested products
        /// </summary>
        private string GenerateBatchId(PlantInstanceSO plant)
        {
            if (plant == null) return $"UNKNOWN_{System.DateTime.Now:yyyyMMdd_HHmm}";
            
            string strainName = plant.Strain?.DisplayName ?? "Unknown";
            string plantId = plant.PlantID ?? "NoID";
            string timestamp = System.DateTime.Now.ToString("yyyyMMdd_HHmm");
            
            return $"{strainName}_{plantId}_{timestamp}";
        }
        
        /// <summary>
        /// Validates if a plant can be harvested
        /// </summary>
        public bool CanHarvestPlant(string plantId)
        {
            if (!IsInitialized) return false;
            
            var plant = _plantLifecycleManager.GetPlant(plantId);
            if (plant == null) return false;
            
            return plant.CurrentGrowthStage == PlantGrowthStage.Harvest && plant.OverallHealth > 0f;
        }
        
        /// <summary>
        /// Gets detailed harvest preview for a plant
        /// </summary>
        public (float yieldAmount, float potency, float quality) GetHarvestPreview(string plantId)
        {
            if (!IsInitialized) return (0f, 0f, 0f);
            
            var plant = _plantLifecycleManager.GetPlant(plantId);
            if (plant == null) return (0f, 0f, 0f);
            
            float yieldAmount = plant.CalculateYieldPotential() * 100f;
            float potency = plant.CalculatePotencyPotential();
            float quality = CalculateQualityScore(plant);
            
            return (yieldAmount, potency, quality);
        }
    }
}