using ProjectChimera.Core.Logging;
using ProjectChimera.Data.Shared;
using ProjectChimera.Data.Cultivation;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using PlantGrowthStage = ProjectChimera.Data.Shared.PlantGrowthStage;

namespace ProjectChimera.Systems.Cultivation
{
    /// <summary>
    /// Implementation for plant care operations and maintenance
    /// </summary>
    public class PlantCare : IPlantCare
    {
        private IPlantLifecycle _plantLifecycle;
        private IEnvironmentControl _environmentControl;
        private bool _isInitialized = false;

        public Action<string, float> OnPlantWatered { get; set; }
        public Action<string, float> OnPlantFed { get; set; }
        public Action<string, string> OnPlantTrained { get; set; }
        public Action<string, string> OnMaintenanceRequired { get; set; }

        public PlantCare(IPlantLifecycle plantLifecycle = null, IEnvironmentControl environmentControl = null)
        {
            _plantLifecycle = plantLifecycle;
            _environmentControl = environmentControl;
        }

        public void Initialize(IPlantLifecycle plantLifecycle = null)
        {
            if (plantLifecycle != null)
            {
                _plantLifecycle = plantLifecycle;
            }

            _isInitialized = true;
            ChimeraLogger.Log("[PlantCare] Plant care system initialized");
        }

        public void Shutdown()
        {
            _isInitialized = false;
            ChimeraLogger.Log("[PlantCare] Plant care system shutdown");
        }

        public bool WaterPlant(string plantId, float waterAmount = 0.5f)
        {
            if (!_isInitialized || _plantLifecycle == null)
            {
                ChimeraLogger.LogWarning("[PlantCare] Cannot water plant - system not initialized");
                return false;
            }

            var plant = _plantLifecycle.GetPlant(plantId);
            if (plant == null)
            {
                ChimeraLogger.LogWarning($"[PlantCare] Plant {plantId} not found for watering");
                return false;
            }

            try
            {
                // Apply water with diminishing returns for overwatering
                float currentWater = plant.WaterLevel;
                float newWaterLevel = Mathf.Min(1.0f, currentWater + waterAmount);

                // Check for overwatering
                if (currentWater > 0.9f)
                {
                    ChimeraLogger.LogWarning($"[PlantCare] Plant {plantId} may be overwatered (current: {currentWater:P0})");
                    newWaterLevel = Mathf.Min(1.0f, currentWater + waterAmount * 0.5f); // Reduced effectiveness when overwatering
                }

                plant.WaterLevel = newWaterLevel;

                // Slight health improvement from proper watering
                if (currentWater < 0.5f)
                {
                    plant.CurrentHealth = Mathf.Min(1.0f, plant.CurrentHealth + 0.02f);
                }

                OnPlantWatered?.Invoke(plantId, waterAmount);

                ChimeraLogger.Log($"[PlantCare] Watered plant {plantId} with {waterAmount:F2}L - New level: {newWaterLevel:P0}");
                return true;
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[PlantCare] Error watering plant {plantId}: {ex.Message}");
                return false;
            }
        }

        public bool FeedPlant(string plantId, float nutrientAmount = 0.4f)
        {
            if (!_isInitialized || _plantLifecycle == null)
            {
                ChimeraLogger.LogWarning("[PlantCare] Cannot feed plant - system not initialized");
                return false;
            }

            var plant = _plantLifecycle.GetPlant(plantId);
            if (plant == null)
            {
                ChimeraLogger.LogWarning($"[PlantCare] Plant {plantId} not found for feeding");
                return false;
            }

            try
            {
                // Apply nutrients with growth stage considerations
                float stageMultiplier = plant.CurrentGrowthStage switch
                {
                    PlantGrowthStage.Vegetative => 1.2f, // Veg stage needs more nutrients
                    PlantGrowthStage.Flowering => 1.0f,  // Normal feeding during flowering
                    PlantGrowthStage.Seedling => 0.6f,   // Less nutrients for seedlings
                    _ => 0.8f
                };

                float effectiveNutrients = nutrientAmount * stageMultiplier;
                float currentNutrients = plant.NutrientLevel;
                float newNutrientLevel = Mathf.Min(1.0f, currentNutrients + effectiveNutrients);

                // Check for nutrient burn
                if (currentNutrients > 0.8f)
                {
                    ChimeraLogger.LogWarning($"[PlantCare] Plant {plantId} may have nutrient burn (current: {currentNutrients:P0})");
                    newNutrientLevel = Mathf.Min(1.0f, currentNutrients + effectiveNutrients * 0.3f);
                }

                plant.NutrientLevel = newNutrientLevel;

                // Health improvement from proper feeding
                if (currentNutrients < 0.4f)
                {
                    plant.CurrentHealth = Mathf.Min(1.0f, plant.CurrentHealth + 0.03f);
                }

                OnPlantFed?.Invoke(plantId, nutrientAmount);

                ChimeraLogger.Log($"[PlantCare] Fed plant {plantId} with {nutrientAmount:F2}L nutrients - New level: {newNutrientLevel:P0}");
                return true;
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[PlantCare] Error feeding plant {plantId}: {ex.Message}");
                return false;
            }
        }

        public bool TrainPlant(string plantId, string trainingType)
        {
            if (!_isInitialized || _plantLifecycle == null)
            {
                ChimeraLogger.LogWarning("[PlantCare] Cannot train plant - system not initialized");
                return false;
            }

            var plant = _plantLifecycle.GetPlant(plantId);
            if (plant == null)
            {
                ChimeraLogger.LogWarning($"[PlantCare] Plant {plantId} not found for training");
                return false;
            }

            try
            {
                bool success = trainingType.ToLower() switch
                {
                    "lst" or "low_stress" => ApplyLowStressTraining(plantId),
                    "hst" or "high_stress" => ApplyHighStressTraining(plantId),
                    "defoliation" => ApplyDefoliation(plantId),
                    _ => false
                };

                if (success)
                {
                    OnPlantTrained?.Invoke(plantId, trainingType);
                    ChimeraLogger.Log($"[PlantCare] Applied {trainingType} training to plant {plantId}");
                }

                return success;
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[PlantCare] Error training plant {plantId}: {ex.Message}");
                return false;
            }
        }

        public void WaterAllPlants(float waterAmount = 0.5f)
        {
            if (!_isInitialized || _plantLifecycle == null)
            {
                return;
            }

            var plants = _plantLifecycle.GetAllPlants();
            int wateredCount = 0;

            foreach (var plant in plants)
            {
                if (plant.WaterLevel < 0.8f) // Only water if needed
                {
                    if (WaterPlant(plant.PlantId, waterAmount))
                    {
                        wateredCount++;
                    }
                }
            }

            ChimeraLogger.Log($"[PlantCare] Watered {wateredCount} plants with {waterAmount:F2}L each");
        }

        public void FeedAllPlants(float nutrientAmount = 0.4f)
        {
            if (!_isInitialized || _plantLifecycle == null)
            {
                return;
            }

            var plants = _plantLifecycle.GetAllPlants();
            int fedCount = 0;

            foreach (var plant in plants)
            {
                if (plant.NutrientLevel < 0.7f) // Only feed if needed
                {
                    if (FeedPlant(plant.PlantId, nutrientAmount))
                    {
                        fedCount++;
                    }
                }
            }

            ChimeraLogger.Log($"[PlantCare] Fed {fedCount} plants with {nutrientAmount:F2}L nutrients each");
        }

        public OfflineCareResult ProcessOfflinePlantCare(PlantInstanceSO plant, float offlineHours)
        {
            if (plant == null || offlineHours <= 0f)
            {
                return OfflineCareResult.None;
            }

            var result = new OfflineCareResult();

            try
            {
                bool needsWater = CheckPlantNeedsWater(plant, offlineHours);
                bool needsNutrients = CheckPlantNeedsNutrients(plant, offlineHours);

                // Check if automation systems can handle care
                bool autoWateringAvailable = _environmentControl?.IsAutoWateringEnabled() ?? false;
                bool autoFeedingAvailable = _environmentControl?.IsAutoFeedingEnabled() ?? false;

                if (needsWater && autoWateringAvailable)
                {
                    float waterRequired = CalculateWaterRequirement(plant, offlineHours);
                    plant.WaterLevel = Mathf.Min(1.0f, plant.WaterLevel + 0.8f); // Auto-watering to 80%
                    result.WaterUsed = waterRequired;
                    result.WasAutoCareApplied = true;
                }

                if (needsNutrients && autoFeedingAvailable)
                {
                    float nutrientsRequired = CalculateNutrientRequirement(plant, offlineHours);
                    plant.NutrientLevel = Mathf.Min(1.0f, plant.NutrientLevel + 0.7f); // Auto-feeding to 70%
                    result.NutrientsUsed = nutrientsRequired;
                    result.WasAutoCareApplied = true;
                }

                // Check for maintenance needs
                if (CheckPlantMaintenanceNeeds(plant, offlineHours))
                {
                    result.MaintenanceRequired = true;
                    result.MaintenanceNotes = GenerateMaintenanceNotes(plant);
                }

                return result;
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[PlantCare] Error processing offline care for plant {plant.PlantId}: {ex.Message}");
                return OfflineCareResult.None;
            }
        }

        public void ProcessAllPlantsOfflineCare(float offlineHours)
        {
            if (!_isInitialized || _plantLifecycle == null || offlineHours <= 0f)
            {
                return;
            }

            var plants = _plantLifecycle.GetAllPlants();
            int plantsProcessed = 0;
            float totalWaterUsed = 0f;
            float totalNutrientsUsed = 0f;

            foreach (var plant in plants)
            {
                try
                {
                    var careResult = ProcessOfflinePlantCare(plant, offlineHours);
                    totalWaterUsed += careResult.WaterUsed;
                    totalNutrientsUsed += careResult.NutrientsUsed;

                    if (careResult.MaintenanceRequired)
                    {
                        OnMaintenanceRequired?.Invoke(plant.PlantId, careResult.MaintenanceNotes);
                    }

                    plantsProcessed++;
                }
                catch (Exception ex)
                {
                    ChimeraLogger.LogError($"[PlantCare] Error processing offline care for plant {plant.PlantId}: {ex.Message}");
                }
            }

            ChimeraLogger.Log($"[PlantCare] Processed offline care for {plantsProcessed} plants - Water: {totalWaterUsed:F1}L, Nutrients: {totalNutrientsUsed:F1}L");
        }

        public bool CheckPlantNeedsWater(PlantInstanceSO plant, float offlineHours = 0f)
        {
            if (plant == null) return false;

            // Calculate water depletion rate based on plant size and environmental conditions
            float depletionRate = CalculateWaterDepletionRate(plant);
            float hoursUntilDry = plant.WaterLevel / depletionRate;

            // Plant needs water if it would run dry during offline period or is currently low
            return offlineHours > 0f ? offlineHours >= (hoursUntilDry * 0.8f) : plant.WaterLevel < 0.3f;
        }

        public bool CheckPlantNeedsNutrients(PlantInstanceSO plant, float offlineHours = 0f)
        {
            if (plant == null) return false;

            // Different growth stages have different nutrient consumption rates
            float consumptionRate = plant.CurrentGrowthStage switch
            {
                PlantGrowthStage.Vegetative => 0.02f, // High consumption during veg
                PlantGrowthStage.Flowering => 0.015f, // Moderate during flowering
                PlantGrowthStage.Seedling => 0.005f, // Low during seedling
                _ => 0.01f // Default rate
            };

            float hoursUntilDeficient = plant.NutrientLevel / consumptionRate;

            return offlineHours > 0f ? offlineHours >= (hoursUntilDeficient * 0.7f) : plant.NutrientLevel < 0.3f;
        }

        public bool CheckPlantMaintenanceNeeds(PlantInstanceSO plant, float offlineHours = 0f)
        {
            if (plant == null) return false;

            // Check for various maintenance needs
            bool needsPruning = plant.AgeInDays > 30 && plant.CurrentGrowthStage == PlantGrowthStage.Vegetative;
            bool hasHealthIssues = plant.CurrentHealth < 0.8f;
            bool needsTraining = plant.CurrentHeight > 60f && plant.CurrentGrowthStage == PlantGrowthStage.Vegetative;

            return needsPruning || hasHealthIssues || needsTraining;
        }

        public float CalculateWaterDepletionRate(PlantInstanceSO plant)
        {
            // Base rate varies by growth stage and plant size
            float baseRate = 0.01f; // 1% per hour base rate

            // Adjust for plant size (larger plants use more water)
            float sizeModifier = (plant.CurrentHeight / 100f) * 0.5f + 0.5f; // 0.5x to 1.5x based on height

            // Adjust for growth stage
            float stageModifier = plant.CurrentGrowthStage switch
            {
                PlantGrowthStage.Vegetative => 1.5f, // High water use during veg
                PlantGrowthStage.Flowering => 1.2f, // Moderate during flowering
                PlantGrowthStage.Seedling => 0.5f, // Low during seedling
                _ => 1.0f
            };

            return baseRate * sizeModifier * stageModifier;
        }

        public float CalculateWaterRequirement(PlantInstanceSO plant, float offlineHours)
        {
            float depletionRate = CalculateWaterDepletionRate(plant);
            return depletionRate * offlineHours * 2.0f; // 2L per depletion unit
        }

        public float CalculateNutrientRequirement(PlantInstanceSO plant, float offlineHours)
        {
            float consumptionRate = plant.CurrentGrowthStage switch
            {
                PlantGrowthStage.Vegetative => 0.02f,
                PlantGrowthStage.Flowering => 0.015f,
                PlantGrowthStage.Seedling => 0.005f,
                _ => 0.01f
            };

            return consumptionRate * offlineHours * 1.5f; // 1.5L per consumption unit
        }

        public float CalculateOverallPlantHealth(PlantInstanceSO plant)
        {
            if (plant == null) return 0f;

            float waterHealth = plant.WaterLevel > 0.2f ? 1.0f : plant.WaterLevel * 5f; // Penalty below 20%
            float nutrientHealth = plant.NutrientLevel > 0.2f ? 1.0f : plant.NutrientLevel * 5f; // Penalty below 20%
            float baseHealth = plant.CurrentHealth;

            return (waterHealth + nutrientHealth + baseHealth) / 3f;
        }

        public void UpdatePlantHealthBasedOnCare(PlantInstanceSO plant)
        {
            if (plant == null) return;

            float overallHealth = CalculateOverallPlantHealth(plant);
            plant.CurrentHealth = Mathf.Lerp(plant.CurrentHealth, overallHealth, 0.1f); // Gradual health changes
        }

        public List<string> GetPlantCareRecommendations(PlantInstanceSO plant)
        {
            var recommendations = new List<string>();

            if (plant == null) return recommendations;

            if (plant.WaterLevel < 0.3f)
                recommendations.Add("Water the plant - water level is low");

            if (plant.NutrientLevel < 0.3f)
                recommendations.Add("Feed the plant - nutrient level is low");

            if (plant.CurrentHealth < 0.7f)
                recommendations.Add("Check for health issues - plant health is declining");

            if (plant.CurrentHeight > 60f && plant.CurrentGrowthStage == PlantGrowthStage.Vegetative)
                recommendations.Add("Consider training - plant is getting tall");

            if (plant.AgeInDays > 30 && plant.CurrentGrowthStage == PlantGrowthStage.Vegetative)
                recommendations.Add("Pruning may be beneficial - mature vegetative plant");

            return recommendations;
        }

        public bool ApplyLowStressTraining(string plantId)
        {
            var plant = _plantLifecycle?.GetPlant(plantId);
            if (plant == null || plant.CurrentGrowthStage != PlantGrowthStage.Vegetative)
            {
                return false;
            }

            // LST increases yield potential but requires ongoing maintenance
            // plant.RequiresTraining = false; // Training completed - property not available
            // In a real implementation, this would modify yield potential

            ChimeraLogger.Log($"[PlantCare] Applied Low Stress Training to plant {plantId}");
            return true;
        }

        public bool ApplyHighStressTraining(string plantId)
        {
            var plant = _plantLifecycle?.GetPlant(plantId);
            if (plant == null || plant.CurrentGrowthStage != PlantGrowthStage.Vegetative)
            {
                return false;
            }

            // HST can stress the plant but increases yield if done correctly
            plant.CurrentHealth = Mathf.Max(0.5f, plant.CurrentHealth - 0.1f); // Temporary health reduction
            // plant.RequiresTraining = false; // Property not available

            ChimeraLogger.Log($"[PlantCare] Applied High Stress Training to plant {plantId}");
            return true;
        }

        public bool ApplyDefoliation(string plantId)
        {
            var plant = _plantLifecycle?.GetPlant(plantId);
            if (plant == null)
            {
                return false;
            }

            // Defoliation can be done in veg or early flower
            if (plant.CurrentGrowthStage != PlantGrowthStage.Vegetative && plant.CurrentGrowthStage != PlantGrowthStage.Flowering)
            {
                return false;
            }

            // Defoliation temporarily stresses the plant but can improve light penetration
            plant.CurrentHealth = Mathf.Max(0.6f, plant.CurrentHealth - 0.05f);

            ChimeraLogger.Log($"[PlantCare] Applied defoliation to plant {plantId}");
            return true;
        }

        public void SetDependencies(IPlantLifecycle plantLifecycle, IEnvironmentControl environmentControl)
        {
            _plantLifecycle = plantLifecycle;
            _environmentControl = environmentControl;
        }

        private string GenerateMaintenanceNotes(PlantInstanceSO plant)
        {
            var notes = new List<string>();

            if (plant.CurrentHealth < 0.8f)
                notes.Add("Low health detected");

            if (plant.AgeInDays > 30 && plant.CurrentGrowthStage == PlantGrowthStage.Vegetative)
                notes.Add("Pruning recommended");

            if (plant.CurrentHeight > 60f)
                notes.Add("Training recommended");

            return string.Join(", ", notes);
        }
    }
}
