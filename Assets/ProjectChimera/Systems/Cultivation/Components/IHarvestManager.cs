using ProjectChimera.Data.Shared;
using ProjectChimera.Data.Cultivation;
using ProjectChimera.Data.Cultivation.Plant;
using System;
using System.Collections.Generic;
using PlantGrowthStage = ProjectChimera.Data.Shared.PlantGrowthStage;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Cultivation
{
    /// <summary>
    /// Interface for harvest management and processing
    /// </summary>
    public interface IHarvestManager
    {
        float TotalYieldHarvested { get; }
        int TotalPlantsHarvested { get; }
        
        // Core harvest operations
        bool HarvestPlant(string plantId);
        HarvestResult ProcessHarvest(PlantInstanceSO plant);
        
        // Harvest validation
        bool IsPlantReadyForHarvest(string plantId);
        bool IsPlantOverripe(string plantId);
        float CalculateOptimalHarvestTime(PlantInstanceSO plant);
        
        // Yield calculations
        float CalculateExpectedYield(PlantInstanceSO plant);
        float CalculateYieldQuality(PlantInstanceSO plant);
        HarvestQuality DetermineHarvestQuality(PlantInstanceSO plant);
        
        // Offline harvest processing
        void ProcessOfflineHarvestChecks(float offlineHours);
        List<string> GetPlantsReadyForHarvest();
        void ScheduleAutomaticHarvest(string plantId, float hoursUntilHarvest);
        
        // Harvest analytics
        HarvestStatistics GetHarvestStatistics();
        float GetAverageYieldPerPlant();
        Dictionary<HarvestQuality, int> GetYieldQualityDistribution();
        
        // Post-harvest processing
        void ProcessHarvestedMaterial(string plantId, float yieldAmount, HarvestQuality quality);
        void UpdateInventoryWithHarvest(HarvestResult harvestResult);
        
        // Events
        Action<string, HarvestResult> OnPlantHarvested { get; set; }
        Action<string> OnHarvestReady { get; set; }
        Action<string> OnPlantOverripe { get; set; }
        Action<HarvestStatistics> OnHarvestStatisticsUpdated { get; set; }
        
        void Initialize(IPlantLifecycle plantLifecycle = null);
        void Shutdown();
    }

    /// <summary>
    /// Result data structure for harvest operations
    /// </summary>
    public struct HarvestResult
    {
        public bool Success;
        public string PlantId;
        public float YieldAmount; // Grams
        public HarvestQuality Quality;
        public float QualityScore; // 0.0 to 1.0
        public string ErrorMessage;
        public DateTime HarvestTime;
        
        public static HarvestResult CreateSuccess(string plantId, float yieldAmount, HarvestQuality quality, float qualityScore)
        {
            return new HarvestResult
            {
                Success = true,
                PlantId = plantId,
                YieldAmount = yieldAmount,
                Quality = quality,
                QualityScore = qualityScore,
                ErrorMessage = string.Empty,
                HarvestTime = DateTime.Now
            };
        }

        public static HarvestResult CreateFailure(string plantId, string errorMessage)
        {
            return new HarvestResult
            {
                Success = false,
                PlantId = plantId,
                YieldAmount = 0f,
                Quality = HarvestQuality.Poor,
                QualityScore = 0f,
                ErrorMessage = errorMessage,
                HarvestTime = DateTime.Now
            };
        }
    }

    /// <summary>
    /// Harvest quality enumeration
    /// </summary>
    public enum HarvestQuality
    {
        Poor = 0,
        Fair = 1,
        Good = 2,
        Excellent = 3,
        Premium = 4
    }

    /// <summary>
    /// Harvest statistics data structure
    /// </summary>
    public struct HarvestStatistics
    {
        public int TotalPlantsHarvested;
        public float TotalYieldHarvested;
        public float AverageYieldPerPlant;
        public float AverageQualityScore;
        public Dictionary<HarvestQuality, int> QualityDistribution;
        public DateTime LastHarvestTime;
        public string BestPerformingStrain;
        public float BestYieldAchieved;
    }
}
