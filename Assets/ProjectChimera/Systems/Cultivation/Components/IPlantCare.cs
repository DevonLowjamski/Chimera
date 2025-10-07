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
    /// Interface for plant care operations and maintenance
    /// </summary>
    public interface IPlantCare
    {
        // Individual plant care
        bool WaterPlant(string plantId, float waterAmount = 0.5f);
        bool FeedPlant(string plantId, float nutrientAmount = 0.4f);
        bool TrainPlant(string plantId, string trainingType);
        
        // Bulk plant care
        void WaterAllPlants(float waterAmount = 0.5f);
        void FeedAllPlants(float nutrientAmount = 0.4f);
        
        // Offline care processing
        OfflineCareResult ProcessOfflinePlantCare(PlantInstanceSO plant, float offlineHours);
        void ProcessAllPlantsOfflineCare(float offlineHours);
        
        // Care calculations and validation
        bool CheckPlantNeedsWater(PlantInstanceSO plant, float offlineHours = 0f);
        bool CheckPlantNeedsNutrients(PlantInstanceSO plant, float offlineHours = 0f);
        bool CheckPlantMaintenanceNeeds(PlantInstanceSO plant, float offlineHours = 0f);
        
        float CalculateWaterDepletionRate(PlantInstanceSO plant);
        float CalculateWaterRequirement(PlantInstanceSO plant, float offlineHours);
        float CalculateNutrientRequirement(PlantInstanceSO plant, float offlineHours);
        
        // Plant health monitoring
        float CalculateOverallPlantHealth(PlantInstanceSO plant);
        void UpdatePlantHealthBasedOnCare(PlantInstanceSO plant);
        List<string> GetPlantCareRecommendations(PlantInstanceSO plant);
        
        // Training operations
        bool ApplyLowStressTraining(string plantId);
        bool ApplyHighStressTraining(string plantId);
        bool ApplyDefoliation(string plantId);
        
        // Events
        Action<string, float> OnPlantWatered { get; set; }
        Action<string, float> OnPlantFed { get; set; }
        Action<string, string> OnPlantTrained { get; set; }
        Action<string, string> OnMaintenanceRequired { get; set; }
        
        void Initialize(IPlantLifecycle plantLifecycle = null);
        void Shutdown();
    }

    /// <summary>
    /// Result data structure for offline plant care operations
    /// </summary>
    public struct OfflineCareResult
    {
        public bool WasAutoCareApplied;
        public float WaterUsed; // Liters
        public float NutrientsUsed; // Liters
        public bool MaintenanceRequired;
        public string MaintenanceNotes;
        
        public static OfflineCareResult None => new OfflineCareResult
        {
            WasAutoCareApplied = false,
            WaterUsed = 0f,
            NutrientsUsed = 0f,
            MaintenanceRequired = false,
            MaintenanceNotes = string.Empty
        };
    }
}
