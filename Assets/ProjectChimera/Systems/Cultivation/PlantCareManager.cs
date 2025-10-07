using ProjectChimera.Core.Logging;
using System.Collections.Generic;
using UnityEngine;
using ProjectChimera.Core;
// Migrated to unified ServiceContainer architecture
using ProjectChimera.Data.Shared;
using ProjectChimera.Data.Environment;

namespace ProjectChimera.Systems.Cultivation
{
    /// <summary>
    /// PC-013-2: Plant Care Manager - Handles plant watering, feeding, and training
    /// Extracted from monolithic CultivationManager for Single Responsibility Principle
    /// </summary>
    public class PlantCareManager : IPlantCareManager
    {
        // Dependencies
        private IPlantLifecycleManager _plantLifecycleManager;
        
        public bool IsInitialized { get; private set; }
        
        public PlantCareManager(IPlantLifecycleManager plantLifecycleManager)
        {
            _plantLifecycleManager = plantLifecycleManager;
        }
        
        public void Initialize()
        {
            if (IsInitialized) return;
            
            ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", null);
            
            // Register with ServiceContainer for dependency injection
            try
            {
                var serviceContainer = ServiceContainerFactory.Instance;
                serviceContainer?.RegisterSingleton<IPlantCareManager>(this);
                ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", null);
            }
            catch (System.Exception ex)
            {
                ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", null);
            }
            
            IsInitialized = true;
        }
        
        public void Shutdown()
        {
            if (!IsInitialized) return;
            
            ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", null);
            IsInitialized = false;
        }
        
        /// <summary>
        /// Waters a specific plant.
        /// </summary>
        public bool WaterPlant(string plantId, float waterAmount = 0.5f)
        {
            if (!IsInitialized)
            {
                ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", null);
                return false;
            }

            var plant = _plantLifecycleManager.GetPlant(plantId);
            if (plant == null)
            {
                ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", null);
                return false;
            }

            // Validate water amount
            waterAmount = Mathf.Clamp01(waterAmount);

            plant.Water(waterAmount);
            ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", null);

            return true;
        }

        /// <summary>
        /// Waters a specific plant with PlantInstance parameter overload.
        /// </summary>
        public bool Water(PlantInstance plantInstance, float waterAmount = 0.5f)
        {
            if (plantInstance == null) return false;
            return WaterPlant(plantInstance.PlantID, waterAmount);
        }
        
        /// <summary>
        /// Feeds nutrients to a specific plant.
        /// </summary>
        public bool FeedPlant(string plantId, float nutrientAmount = 0.4f)
        {
            if (!IsInitialized)
            {
                ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", null);
                return false;
            }

            var plant = _plantLifecycleManager.GetPlant(plantId);
            if (plant == null)
            {
                ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", null);
                return false;
            }

            // Validate nutrient amount
            nutrientAmount = Mathf.Clamp01(nutrientAmount);

            plant.Feed(nutrientAmount);
            ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", null);

            return true;
        }

        /// <summary>
        /// Feeds nutrients to a specific plant with PlantInstance parameter overload.
        /// </summary>
        public bool Feed(PlantInstance plantInstance, float nutrientAmount = 0.4f)
        {
            if (plantInstance == null) return false;
            return FeedPlant(plantInstance.PlantID, nutrientAmount);
        }
        
        /// <summary>
        /// Applies training to a specific plant.
        /// </summary>
        public bool TrainPlant(string plantId, string trainingType)
        {
            if (!IsInitialized)
            {
                ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", null);
                return false;
            }

            var plant = _plantLifecycleManager.GetPlant(plantId);
            if (plant == null)
            {
                ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", null);
                return false;
            }

            if (string.IsNullOrEmpty(trainingType))
            {
                ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", null);
                return false;
            }

            plant.ApplyTraining(trainingType);
            ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", null);

            return true;
        }

        /// <summary>
        /// Applies training to a specific plant with PlantInstance parameter overload.
        /// </summary>
        public bool ApplyTraining(PlantInstance plantInstance, string trainingType)
        {
            if (plantInstance == null) return false;
            return TrainPlant(plantInstance.PlantID, trainingType);
        }
        
        /// <summary>
        /// Waters all plants in the cultivation system.
        /// </summary>
        public void WaterAllPlants(float waterAmount = 0.5f)
        {
            if (!IsInitialized)
            {
                ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", null);
                return;
            }
            
            // Validate water amount
            waterAmount = Mathf.Clamp01(waterAmount);
            
            int wateredCount = 0;
            var allPlants = _plantLifecycleManager.GetAllPlants();
            
            foreach (var plant in allPlants)
            {
                if (plant.WaterLevel < 0.8f) // Only water if needed
                {
                    plant.Water(waterAmount);
                    wateredCount++;
                }
            }
            
            ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", null);
        }
        
        /// <summary>
        /// Feeds all plants in the cultivation system.
        /// </summary>
        public void FeedAllPlants(float nutrientAmount = 0.4f)
        {
            if (!IsInitialized)
            {
                ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", null);
                return;
            }
            
            // Validate nutrient amount
            nutrientAmount = Mathf.Clamp01(nutrientAmount);
            
            int fedCount = 0;
            var allPlants = _plantLifecycleManager.GetAllPlants();
            
            foreach (var plant in allPlants)
            {
                if (plant.NutrientLevel < 0.7f) // Only feed if needed
                {
                    plant.Feed(nutrientAmount);
                    fedCount++;
                }
            }
            
            ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", null);
        }
        
        /// <summary>
        /// Performs comprehensive care for plants needing attention
        /// </summary>
        public void CareForPlantsNeedingAttention()
        {
            if (!IsInitialized)
            {
                ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", null);
                return;
            }
            
            var plantsNeedingAttention = _plantLifecycleManager.GetPlantsNeedingAttention();
            int caredForCount = 0;
            
            foreach (var plant in plantsNeedingAttention)
            {
                bool careProvided = false;
                
                // Water if needed
                if (plant.WaterLevel < 0.3f)
                {
                    WaterPlant(plant.PlantID, 0.6f);
                    careProvided = true;
                }
                
                // Feed if needed
                if (plant.NutrientLevel < 0.3f)
                {
                    FeedPlant(plant.PlantID, 0.5f);
                    careProvided = true;
                }
                
                if (careProvided)
                {
                    caredForCount++;
                }
            }
            
            ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", null);
        }
        
        /// <summary>
        /// Gets care statistics for all plants
        /// </summary>
        public (int needsWater, int needsNutrients, int needsTraining) GetCareStatistics()
        {
            if (!IsInitialized) return (0, 0, 0);
            
            var allPlants = _plantLifecycleManager.GetAllPlants();
            int needsWater = 0;
            int needsNutrients = 0;
            int needsTraining = 0;
            
            foreach (var plant in allPlants)
            {
                if (plant.WaterLevel < 0.3f) needsWater++;
                if (plant.NutrientLevel < 0.3f) needsNutrients++;
                if (plant.StressLevel > 0.7f) needsTraining++;
            }
            
            return (needsWater, needsNutrients, needsTraining);
        }
    }
}