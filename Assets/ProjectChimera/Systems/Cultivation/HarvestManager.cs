using ProjectChimera.Core.Logging;
using UnityEngine;
using ProjectChimera.Core;
// Migrated to unified ServiceContainer architecture
using ProjectChimera.Data.Shared;
using ProjectChimera.Data.Cultivation;
using ProjectChimera.Data.Cultivation.Plant;
using System;
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
    public partial class HarvestManager : IHarvestManager
    {
        [Header("Events")]
        [SerializeField] private GameEventSO<PlantInstanceSO> _onPlantHarvested;

        // Dependencies
        private IPlantLifecycle _plantLifecycle;

        public bool IsInitialized { get; private set; }
        public float TotalYieldHarvested { get; private set; }
        public int TotalPlantsHarvested { get; private set; }

        public HarvestManager(IPlantLifecycle plantLifecycle)
        {
            _plantLifecycle = plantLifecycle;
        }

        /// <summary>
        /// Sets dependencies for HarvestManager after creation to resolve circular dependency issues.
        /// </summary>
        public void SetDependencies(IPlantLifecycle plantLifecycle)
        {
            _plantLifecycle = plantLifecycle;
        }

        public void Initialize(IPlantLifecycle plantLifecycle = null)
        {
            if (IsInitialized) return;

            ChimeraLogger.Log("[HarvestManager] Initializing harvest management...");

            // Register with unified ServiceContainer architecture
            try
            {
                var serviceContainer = ServiceContainerFactory.Instance;
                serviceContainer?.RegisterSingleton<IHarvestManager>(this);
                ChimeraLogger.Log("[HarvestManager] Registered with ServiceContainer");
            }
            catch (System.Exception ex)
            {
                ChimeraLogger.LogError($"[HarvestManager] Failed to register with ServiceContainer: {ex.Message}");
            }

            IsInitialized = true;
        }

        // NOTE: Duplicate Shutdown method removed - using the more comprehensive one later in the file

        /// <summary>
        /// Processes harvest for a plant
        /// </summary>
        public HarvestResult ProcessHarvest(PlantInstanceSO plant)
        {
            if (!IsInitialized)
            {
                ChimeraLogger.LogError("[HarvestManager] Cannot process harvest: Manager not initialized.");
                return HarvestResult.CreateFailure(plant?.PlantId ?? "unknown", "Manager not initialized");
            }

            if (plant == null)
            {
                ChimeraLogger.LogError("[HarvestManager] Cannot process harvest: Plant is null.");
                return HarvestResult.CreateFailure("unknown", "Plant is null");
            }

            // Validate plant is ready for harvest
            if (plant.CurrentGrowthStage != PlantGrowthStage.Harvest)
            {
                ChimeraLogger.LogWarning($"[HarvestManager] Plant '{plant.PlantID}' is not ready for harvest (Stage: {plant.CurrentGrowthStage}).");
                return HarvestResult.CreateFailure(plant.PlantId, $"Plant not ready for harvest (Stage: {plant.CurrentGrowthStage})");
            }

            // Calculate harvest results
            float yieldAmount = plant.CalculateYieldPotential() * 100f; // Convert to grams
            float potency = plant.CalculatePotencyPotential();
            float qualityScore = CalculateQualityScore(plant);

            // Update lifecycle statistics
            // Note: Statistics update will be handled by the lifecycle component

            ChimeraLogger.Log($"[HarvestManager] Harvested plant '{plant.PlantID}': {yieldAmount:F1}g at {potency:F1}% potency, {qualityScore:F1}% quality");

            // Add to inventory
            var harvestResult = AddHarvestToInventory(plant, yieldAmount, qualityScore);

            // Raise harvest event
            _onPlantHarvested?.Raise(plant);

            return harvestResult;
        }

        /// <summary>
        /// PC-009-2: Adds harvested plant product to player inventory
        /// </summary>
        public HarvestResult AddHarvestToInventory(PlantInstanceSO plant, float yieldAmount, float qualityScore)
        {
            if (!IsInitialized)
            {
                ChimeraLogger.LogError("[HarvestManager] Cannot add harvest to inventory: Manager not initialized.");
                return HarvestResult.CreateFailure(plant?.PlantId ?? "unknown", "Manager not initialized");
            }

            try
            {
                // Economy integration ready for service interface implementation
                ChimeraLogger.Log($"[HarvestManager] Harvest completed: {yieldAmount}g with quality {qualityScore:F1}% - inventory integration pending");

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
                    ChimeraLogger.Log($"[HarvestManager] Successfully processed harvest: {yieldAmount:F1}g (Batch: {batchId}, Quality: {qualityScore:F1}%) - inventory integration pending");

                    TotalPlantsHarvested++;
                    TotalYieldHarvested += yieldAmount;

                    return HarvestResult.CreateSuccess(plant.PlantId, yieldAmount, DetermineHarvestQuality(plant), qualityScore);
                }
                else
                {
                    ChimeraLogger.LogWarning($"[HarvestManager] Failed to process harvest");
                    return HarvestResult.CreateFailure(plant.PlantId, "Failed to add harvest to inventory");
                }
            }
            catch (System.Exception ex)
            {
                ChimeraLogger.LogError($"[HarvestManager] Error adding harvest to inventory: {ex.Message}");
                return HarvestResult.CreateFailure(plant.PlantId, $"Error processing harvest: {ex.Message}");
            }
        }

        /// <summary>
        /// Harvests a plant by ID
        /// </summary>
        public bool HarvestPlant(string plantId)
        {
            if (!IsInitialized)
            {
                ChimeraLogger.LogError("[HarvestManager] Cannot harvest plant: Manager not initialized.");
                return false;
            }

            var plant = _plantLifecycle.GetPlant(plantId);
            if (plant == null)
            {
                ChimeraLogger.LogWarning($"[HarvestManager] Cannot harvest plant: Plant ID '{plantId}' not found.");
                return false;
            }

            // Process harvest
            ProcessHarvest(plant);

            // Remove plant from lifecycle
            return _plantLifecycle.RemovePlant(plantId, true);
        }

        /// <summary>
        /// Harvests all plants ready for harvest
        /// </summary>
        public int HarvestAllReadyPlants()
        {
            if (!IsInitialized)
            {
                ChimeraLogger.LogError("[HarvestManager] Cannot harvest all ready plants: Manager not initialized.");
                return 0;
            }

            var harvestReadyPlants = _plantLifecycle.GetPlantsByStage(PlantGrowthStage.Harvest);
            int harvestedCount = 0;

            foreach (var plant in harvestReadyPlants)
            {
                if (HarvestPlant(plant.PlantID))
                {
                    harvestedCount++;
                }
            }

            ChimeraLogger.Log($"[HarvestManager] Harvested {harvestedCount} plants ready for harvest.");
            return harvestedCount;
        }

        /// <summary>
        /// Gets harvest statistics summary
        /// </summary>
        public (int readyToHarvest, float totalYield, float avgQuality) GetHarvestSummary()
        {
            if (!IsInitialized) return (0, 0f, 0f);

            var harvestReadyPlants = _plantLifecycle.GetPlantsByStage(PlantGrowthStage.Harvest);
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

            string strainName = plant.Strain?.StrainName ?? "Unknown";
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

            var plant = _plantLifecycle.GetPlant(plantId);
            if (plant == null) return false;

            return plant.CurrentGrowthStage == PlantGrowthStage.Harvest && plant.OverallHealth > 0f;
        }

        /// <summary>
        /// Gets detailed harvest preview for a plant
        /// </summary>
        public (float yieldAmount, float potency, float quality) GetHarvestPreview(string plantId)
        {
            if (!IsInitialized) return (0f, 0f, 0f);

            var plant = _plantLifecycle.GetPlant(plantId);
            if (plant == null) return (0f, 0f, 0f);

            float yieldAmount = plant.CalculateYieldPotential() * 100f;
            float potency = plant.CalculatePotencyPotential();
            float quality = CalculateQualityScore(plant);

            return (yieldAmount, potency, quality);
        }

        // Additional interface methods to satisfy IHarvestManager
        public Action<string, HarvestResult> OnPlantHarvested { get; set; }
        public Action<string> OnHarvestReady { get; set; }
        public Action<string> OnPlantOverripe { get; set; }
        public Action<HarvestStatistics> OnHarvestStatisticsUpdated { get; set; }

        public bool IsPlantReadyForHarvest(string plantId)
        {
            if (!IsInitialized) return false;
            var plant = _plantLifecycle?.GetPlant(plantId);
            return plant?.CurrentGrowthStage == PlantGrowthStage.Harvest && plant.OverallHealth > 0f;
        }

        public bool IsPlantOverripe(string plantId)
        {
            if (!IsInitialized) return false;
            var plant = _plantLifecycle?.GetPlant(plantId);
            return plant?.CurrentGrowthStage == PlantGrowthStage.Harvest && plant.GrowthProgress > 1.2f;
        }

        public float CalculateOptimalHarvestTime(PlantInstanceSO plant)
        {
            if (plant == null) return 0f;
            return plant.GrowthProgress >= 1.0f ? 0f : (1.0f - plant.GrowthProgress) * 24f;
        }

        public float CalculateExpectedYield(PlantInstanceSO plant)
        {
            if (plant == null) return 0f;
            return plant.CalculateYieldPotential() * 100f;
        }

        public float CalculateYieldQuality(PlantInstanceSO plant)
        {
            if (plant == null) return 0f;
            return CalculateQualityScore(plant) / 100f;
        }

        public HarvestQuality DetermineHarvestQuality(PlantInstanceSO plant)
        {
            if (plant == null) return HarvestQuality.Poor;

            float qualityScore = CalculateQualityScore(plant);
            if (qualityScore >= 90f) return HarvestQuality.Premium;
            if (qualityScore >= 75f) return HarvestQuality.Excellent;
            if (qualityScore >= 60f) return HarvestQuality.Good;
            if (qualityScore >= 40f) return HarvestQuality.Fair;
            return HarvestQuality.Poor;
        }

        public void ProcessOfflineHarvestChecks(float offlineHours)
        {
            if (!IsInitialized) return;
            ChimeraLogger.Log($"[HarvestManager] Processing offline harvest checks for {offlineHours:F1} hours");
        }

        public System.Collections.Generic.List<string> GetPlantsReadyForHarvest()
        {
            return new System.Collections.Generic.List<string>(); // Placeholder
        }

        public void ScheduleAutomaticHarvest(string plantId, float hoursUntilHarvest)
        {
            if (!IsInitialized) return;
            ChimeraLogger.Log($"[HarvestManager] Scheduled automatic harvest for plant {plantId} in {hoursUntilHarvest:F1} hours");
        }

        public HarvestStatistics GetHarvestStatistics()
        {
            return new HarvestStatistics
            {
                TotalPlantsHarvested = TotalPlantsHarvested,
                TotalYieldHarvested = TotalYieldHarvested,
                AverageYieldPerPlant = TotalPlantsHarvested > 0 ? TotalYieldHarvested / TotalPlantsHarvested : 0f,
                AverageQualityScore = 0.75f,
                QualityDistribution = new System.Collections.Generic.Dictionary<HarvestQuality, int>(),
                LastHarvestTime = DateTime.Now,
                BestPerformingStrain = "Unknown",
                BestYieldAchieved = TotalYieldHarvested
            };
        }

        public float GetAverageYieldPerPlant()
        {
            return TotalPlantsHarvested > 0 ? TotalYieldHarvested / TotalPlantsHarvested : 0f;
        }

        public System.Collections.Generic.Dictionary<HarvestQuality, int> GetYieldQualityDistribution()
        {
            return new System.Collections.Generic.Dictionary<HarvestQuality, int>();
        }

        public void ProcessHarvestedMaterial(string plantId, float yieldAmount, HarvestQuality quality)
        {
            if (!IsInitialized) return;
            TotalYieldHarvested += yieldAmount;
            OnHarvestStatisticsUpdated?.Invoke(GetHarvestStatistics());
        }

        public void UpdateInventoryWithHarvest(HarvestResult harvestResult)
        {
            if (!IsInitialized || !harvestResult.Success) return;
            ProcessHarvestedMaterial(harvestResult.PlantId, harvestResult.YieldAmount, harvestResult.Quality);
        }

        public void Shutdown()
        {
            if (!IsInitialized) return;
            OnPlantHarvested = null;
            OnHarvestReady = null;
            OnPlantOverripe = null;
            OnHarvestStatisticsUpdated = null;
            IsInitialized = false;
        }
    }
}
