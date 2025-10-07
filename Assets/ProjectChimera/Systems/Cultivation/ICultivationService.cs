using System.Collections.Generic;
using UnityEngine;
using ProjectChimera.Data.Shared;
using ProjectChimera.Data.Genetics;
using ProjectChimera.Data.Cultivation;
using ProjectChimera.Data.Cultivation.Plant;
using ProjectChimera.Data.Environment;
using EnvironmentalConditions = ProjectChimera.Data.Shared.EnvironmentalConditions;
using PlantGrowthStage = ProjectChimera.Data.Shared.PlantGrowthStage;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Cultivation
{
    /// <summary>
    /// PC-013-2: Interface for cultivation service components
    /// Defines contracts for modular cultivation system components
    /// </summary>
    public interface ICultivationService
    {
        bool IsInitialized { get; }
        void Initialize();
        void Shutdown();
    }

    /// <summary>
    /// Interface for plant lifecycle management
    /// </summary>
    public interface IPlantLifecycleManager : ICultivationService
    {
        PlantInstanceSO PlantSeed(string plantName, object strain, GenotypeDataSO genotype, Vector3 position, string zoneId = "default");
        bool RemovePlant(string plantId, bool isHarvest = false);
        PlantInstanceSO GetPlant(string plantId);
        IEnumerable<PlantInstanceSO> GetAllPlants();
        IEnumerable<PlantInstanceSO> GetPlantsByStage(PlantGrowthStage stage);
        IEnumerable<PlantInstanceSO> GetPlantsNeedingAttention();

        /// <summary>
        /// Sets dependencies for PlantLifecycleManager after creation to resolve circular dependency issues.
        /// </summary>
        /// <param name="environmentalManager">The environmental manager dependency</param>
        /// <param name="harvestManager">The harvest manager dependency</param>
        void SetDependencies(IEnvironmentalManager environmentalManager, IHarvestManager harvestManager);

        int ActivePlantCount { get; }
        int TotalPlantsGrown { get; }
        int TotalPlantsHarvested { get; }
        float TotalYieldHarvested { get; }
    }

    /// <summary>
    /// Interface for plant care and maintenance
    /// </summary>
    public interface IPlantCareManager : ICultivationService
    {
        bool WaterPlant(string plantId, float waterAmount = 0.5f);
        bool FeedPlant(string plantId, float nutrientAmount = 0.4f);
        bool TrainPlant(string plantId, string trainingType);
        void WaterAllPlants(float waterAmount = 0.5f);
        void FeedAllPlants(float nutrientAmount = 0.4f);
    }

    /// <summary>
    /// Interface for environmental management
    /// </summary>
    public interface IEnvironmentalManager : ICultivationService
    {
        void SetZoneEnvironment(string zoneId, EnvironmentalConditions environment);
        EnvironmentalConditions GetZoneEnvironment(string zoneId);
        EnvironmentalConditions GetEnvironmentForPlant(string plantId);
    }

    /// <summary>
    /// Interface for growth processing
    /// </summary>
    public interface IGrowthProcessor : ICultivationService
    {
        void ProcessDailyGrowthForAllPlants();
        void ForceGrowthUpdate();
        float AveragePlantHealth { get; }
        bool EnableAutoGrowth { get; set; }
        float TimeAcceleration { get; set; }
    }

    /// <summary>
    /// Interface for harvest management
    /// </summary>
    public interface ICultivationHarvestManager : ICultivationService
    {
        void ProcessHarvest(PlantInstanceSO plant);
        void AddHarvestToInventory(PlantInstanceSO plant, float yieldAmount, float qualityScore);
        bool HarvestPlant(string plantId);
    }
}
