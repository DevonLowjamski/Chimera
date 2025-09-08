using ProjectChimera.Data.Shared;
using ProjectChimera.Data.Genetics;
using ProjectChimera.Data.Cultivation;
using System;
using System.Collections.Generic;
using UnityEngine;
using PlantGrowthStage = ProjectChimera.Data.Shared.PlantGrowthStage;

namespace ProjectChimera.Systems.Cultivation
{
    /// <summary>
    /// Interface for plant lifecycle management and growth tracking
    /// </summary>
    public interface IPlantLifecycle
    {
        int ActivePlantCount { get; }
        int TotalPlantsGrown { get; }
        int TotalPlantsHarvested { get; }
        float TotalYieldHarvested { get; }

        bool EnableAutoGrowth { get; set; }
        float TimeAcceleration { get; set; }

        string AddPlant(object species, Vector3 position, string zoneId = "");
        bool RemovePlant(string plantId, bool isHarvest = false);

        PlantInstanceSO GetPlant(string plantId);
        IEnumerable<PlantInstanceSO> GetAllPlants();
        IEnumerable<PlantInstanceSO> GetPlantsByStage(PlantGrowthStage stage);
        IEnumerable<PlantInstanceSO> GetPlantsNeedingAttention();

        void ProcessDailyGrowthForAllPlants();
        void ForceGrowthUpdate();

        void ProcessOfflineGrowth(float offlineHours);

        // Growth calculations
        float CalculateGrowthRate(PlantInstanceSO plant);
        bool AdvancePlantGrowthStage(string plantId);
        void UpdatePlantAge(string plantId, float hoursElapsed);

        // Events
        event System.Action<string, PlantInstanceSO> OnPlantAdded;
        event System.Action<string, string> OnPlantRemoved;
        event System.Action<string, PlantGrowthStage> OnPlantStageChanged;

        void Initialize();
        void Shutdown();
    }
}
