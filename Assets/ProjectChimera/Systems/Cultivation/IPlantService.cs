using ProjectChimera.Core.Logging;
using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core;
using PlantGrowthStage = ProjectChimera.Data.Shared.PlantGrowthStage;

namespace ProjectChimera.Systems.Cultivation
{
    /// <summary>
    /// Interface for plant lifecycle management service
    /// </summary>
    public interface IPlantLifecycleService : ICultivationService
    {
        bool CreatePlant(object strain, Vector3 position, string customName = null);
        bool RemovePlant(string plantId, PlantRemovalReason reason = PlantRemovalReason.Other);
        PlantInstance GetPlant(string plantId);
        List<PlantInstance> GetAllPlants();
        List<PlantInstance> GetPlantsByStage(PlantGrowthStage stage);
        List<PlantInstance> GetHealthyPlants(float minimumHealth = 0.7f);
        List<PlantInstance> GetUnhealthyPlants(float maximumHealth = 0.5f);
        void ProcessPlantGrowth(float deltaTime);
        void ForceGrowthUpdate();
        int PlantCount { get; }
        float AveragePlantHealth { get; }

        // Tracking methods for compatibility
        List<PlantInstance> GetTrackedPlants();
        PlantInstance GetTrackedPlant(string plantId);
        List<PlantInstance> GetHarvestablePlants();
        bool EnableAutoGrowth { get; set; }
        float TimeAcceleration { get; set; }
    }

    /// <summary>
    /// Interface for plant statistics and achievement tracking
    /// </summary>
    public interface IPlantStatisticsService : ICultivationService
    {
        // Core achievement statistics
        int TotalPlantsCreated { get; }
        int TotalPlantsHarvested { get; }
        float TotalYieldHarvested { get; }
        float HighestQualityAchieved { get; }
        int HealthyPlantsCount { get; }
        int StrainDiversity { get; }

        // Achievement statistics method
        object GetAchievementStats();
    }

    /// <summary>
    /// Interface for plant genetics and genetic performance service
    /// </summary>
    public interface IPlantGeneticsService : ICultivationService
    {
        // Genetic performance tracking
        object GetGeneticPerformanceStats();
        object GetGeneticDiversityStats();

        void ProcessGeneticCalculations(float deltaTime);
        void ProcessBatchGeneticCalculations(List<PlantInstance> plants);

        // Advanced genetics features
        bool AdvancedGeneticsEnabled { get; set; }
        float GeneticCalculationFrequency { get; set; }
    }

    /// <summary>
    /// Interface for cultivation plant management service
    /// </summary>
    public interface IPlantService : ICultivationService, IPlantLifecycleService, IPlantStatisticsService, IPlantGeneticsService
    {
        // Plant care operations
        bool WaterPlant(string plantId, float waterAmount = 0.5f);
        bool FeedPlant(string plantId, float nutrientAmount = 0.4f);
        bool TrainPlant(string plantId, string trainingType);
        void WaterAllPlants(float waterAmount = 0.5f);
        void FeedAllPlants(float nutrientAmount = 0.4f);

        // Plant lifecycle events
        void OnPlantCreated(PlantInstance plant);
        void OnPlantRemoved(string plantId, PlantInstance plant, PlantRemovalReason reason);
        void OnPlantGrowthStageChanged(PlantInstance plant, PlantGrowthStage previousStage);
        void OnPlantHealthChanged(PlantInstance plant, float previousHealth);
        void OnPlantHarvested(PlantInstance plant, HarvestResults harvestResults);

        // Statistics and performance
        PlantManagerStatistics GetStatistics();
        EnhancedPlantManagerStatistics GetEnhancedStatistics();

        // Plant validation and search
        bool IsPlantValid(string plantId);
        List<PlantInstance> FindPlantsByName(string name);
        List<PlantInstance> FindPlantsByStrain(string strainName);
        PlantInstance FindNearestPlant(Vector3 position, float maxDistance = 10f);

        // Bulk operations
        void ProcessAllPlants(System.Action<PlantInstance> action);
        List<PlantInstance> GetPlantsInRadius(Vector3 center, float radius);
        void RemoveAllDeadPlants();
        void WaterPlantsInRadius(Vector3 center, float radius, float waterAmount = 0.5f);

        // Events
        System.Action<PlantInstance> OnPlantCreatedEvent { get; set; }
        System.Action<string, PlantInstance, PlantRemovalReason> OnPlantRemovedEvent { get; set; }
        System.Action<PlantInstance, PlantGrowthStage, PlantGrowthStage> OnPlantGrowthStageChangedEvent { get; set; }
        System.Action<PlantInstance, float, float> OnPlantHealthChangedEvent { get; set; }
        System.Action<PlantInstance, HarvestResults> OnPlantHarvestedEvent { get; set; }
        System.Action<PlantManagerStatistics> OnStatisticsUpdatedEvent { get; set; }
    }

        // PlantAchievementStats defined in CultivationSystemTypes.cs

    /// <summary>
    /// Interface for cultivation zone management
    /// </summary>
    public interface ICultivationZoneService : ICultivationService
    {
        bool CreateZone(string zoneId, Vector3 center, Vector3 size);
        bool RemoveZone(string zoneId);
        CultivationZone GetZone(string zoneId);
        List<CultivationZone> GetAllZones();
        List<PlantInstance> GetPlantsInZone(string zoneId);
        string GetPlantZone(string plantId);
        bool MovePlantToZone(string plantId, string targetZoneId);
        int ZoneCount { get; }
        bool ZoneExists(string zoneId);
    }

    /// <summary>
    /// Zone data structure
    /// </summary>
    [System.Serializable]
    public class CultivationZone
    {
        public string ZoneId;
        public string Name;
        public Vector3 Center;
        public Vector3 Size;
        public List<string> PlantIds = new List<string>();
        public float AverageHealth;
        public int PlantCount => PlantIds.Count;
        public bool IsActive = true;
    }

    /// <summary>
    /// Enumeration for plant removal reasons
    /// </summary>
    public enum PlantRemovalReason
    {
        Harvested,
        Died,
        Removed,
        Other
    }

    // CultivationEventTracker moved to CultivationSystemTypes.cs to avoid duplication

        // HarvestResults defined in CultivationSystemTypes.cs

    // CannabinoidProfile and TerpeneProfile classes moved to SystemsHarvestResults.cs to avoid duplication


    /// <summary>
    /// Genetic diversity statistics structure
    /// </summary>
    [System.Serializable]
    public class GeneticDiversityStats
    {
        [Header("Genetic Diversity Metrics")]
        public int StrainDiversity;
        public string MostCommonStrain;
        public float AverageGeneticFitness;
        public float TraitExpressionVariance;

        public override string ToString()
        {
            return $"Diversity: {StrainDiversity}, Common: {MostCommonStrain}, Fitness: {AverageGeneticFitness:F2}, Variance: {TraitExpressionVariance:F3}";
        }
    }

    /// <summary>
    /// Statistics about all plants managed by the PlantManager
    /// </summary>
    [System.Serializable]
    public class PlantManagerStatistics
    {
        public int PlantCount;
        public float AverageHealth;
        public float AverageGrowthRate;
        public int HarvestedThisWeek;
        public float TotalYieldThisWeek;
        public float AverageYieldPerPlant;
        public int DiedThisWeek;
        public Dictionary<string, int> StrainCounts = new Dictionary<string, int>();
        public Dictionary<PlantGrowthStage, int> StageDistribution = new Dictionary<PlantGrowthStage, int>();

        // Additional properties for PlantStatisticsService compatibility
        public int TotalPlants => PlantCount;
        public float AverageStress;
        public int UnhealthyPlants;
        public int HighStressPlants;
        public Dictionary<PlantGrowthStage, int> PlantsByStage => StageDistribution;

        public float SurvivalRate => PlantCount > 0 ? (float)(PlantCount - DiedThisWeek) / PlantCount : 0f;
        public string MostCommonStrain => StrainCounts.Count > 0 ? StrainCounts.OrderByDescending(kv => kv.Value).First().Key : "None";

        public override string ToString()
        {
            return $"Plants: {PlantCount}, Avg Health: {AverageHealth:F1}, Harvested: {HarvestedThisWeek}, Survival: {SurvivalRate:P1}";
        }
    }

    /// <summary>
    /// Enhanced statistics including genetic performance data
    /// </summary>
    [System.Serializable]
    public class EnhancedPlantManagerStatistics : PlantManagerStatistics
    {
        public bool AdvancedGeneticsEnabled;
        public GeneticPerformanceStats GeneticStats;
        public GeneticDiversityStats GeneticDiversityStats;
    }
}
