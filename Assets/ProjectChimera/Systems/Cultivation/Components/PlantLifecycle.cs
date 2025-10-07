using ProjectChimera.Core.Logging;
using ProjectChimera.Data.Shared;
using ProjectChimera.Data.Genetics;
using ProjectChimera.Data.Cultivation;
using ProjectChimera.Data.Cultivation.Plant;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using PlantGrowthStage = ProjectChimera.Data.Shared.PlantGrowthStage;

namespace ProjectChimera.Systems.Cultivation
{
    /// <summary>
    /// Implementation for plant lifecycle management and growth tracking
    /// </summary>
    public class PlantLifecycle : IPlantLifecycle
    {
        private Dictionary<string, PlantInstanceSO> _activePlants = new Dictionary<string, PlantInstanceSO>();
        private int _totalPlantsGrown = 0;
        private int _totalPlantsHarvested = 0;
        private float _totalYieldHarvested = 0f;

        private bool _enableAutoGrowth = true;
        private float _timeAcceleration = 1f;
        private bool _isInitialized = false;

        // Events
        public event System.Action<string, PlantInstanceSO> OnPlantAdded;
        public event System.Action<string, string> OnPlantRemoved;
        public event System.Action<string, PlantGrowthStage> OnPlantStageChanged;

        public int ActivePlantCount => _activePlants.Count;
        public int TotalPlantsGrown => _totalPlantsGrown;
        public int TotalPlantsHarvested => _totalPlantsHarvested;
        public float TotalYieldHarvested => _totalYieldHarvested;

        public bool EnableAutoGrowth
        {
            get => _enableAutoGrowth;
            set => _enableAutoGrowth = value;
        }

        public float TimeAcceleration
        {
            get => _timeAcceleration;
            set => _timeAcceleration = Mathf.Max(0.1f, value);
        }



        public void Initialize()
        {
            _isInitialized = true;
            ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", null);
        }

        public void Shutdown()
        {
            _activePlants.Clear();
            _isInitialized = false;
            ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", null);
        }

        public string AddPlant(object species, Vector3 position, string zoneId = "")
        {
            if (!_isInitialized || species == null)
            {
                ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", null);
                return null;
            }

            try
            {
                // Create new plant instance
                var plantInstance = ScriptableObject.CreateInstance<PlantInstanceSO>();
                plantInstance.InitializeFromStrain(species as ProjectChimera.Data.Cultivation.Plant.PlantStrainSO);

                string plantId = plantInstance.PlantInstanceId;
                _activePlants[plantId] = plantInstance;
                _totalPlantsGrown++;

                OnPlantAdded?.Invoke(plantId, plantInstance);

                ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", null);
                return plantId;
            }
            catch (Exception ex)
            {
                ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", null);
                return null;
            }
        }

        public bool RemovePlant(string plantId, bool isHarvest = false)
        {
            if (!_isInitialized || string.IsNullOrEmpty(plantId))
            {
                return false;
            }

            try
            {
                if (_activePlants.TryGetValue(plantId, out var plant))
                {
                    if (isHarvest)
                    {
                        _totalPlantsHarvested++;
                        // Yield is updated by the harvest manager
                    }

                    _activePlants.Remove(plantId);
                    OnPlantRemoved?.Invoke(plantId, isHarvest ? "Harvested" : "Removed");

                    // Cleanup plant instance
                    if (plant != null)
                    {
                        ScriptableObject.DestroyImmediate(plant);
                    }

                    ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", null);
                    return true;
                }

                ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", null);
                return false;
            }
            catch (Exception ex)
            {
                ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", null);
                return false;
            }
        }

        public PlantInstanceSO GetPlant(string plantId)
        {
            if (string.IsNullOrEmpty(plantId))
            {
                return null;
            }

            _activePlants.TryGetValue(plantId, out var plant);
            return plant;
        }

        public IEnumerable<PlantInstanceSO> GetAllPlants()
        {
            return _activePlants.Values.ToList();
        }

        public IEnumerable<PlantInstanceSO> GetPlantsByStage(PlantGrowthStage stage)
        {
            return _activePlants.Values.Where(p => p.CurrentGrowthStage == stage).ToList();
        }

        public IEnumerable<PlantInstanceSO> GetPlantsNeedingAttention()
        {
            return _activePlants.Values.Where(p =>
                p.WaterLevel < 0.3f ||
                p.NutrientLevel < 0.3f ||
                p.CurrentHealth < 0.7f ||
                p.RequiresTraining
            ).ToList();
        }

        public void ProcessDailyGrowthForAllPlants()
        {
            if (!_enableAutoGrowth)
            {
                return;
            }

            foreach (var plant in _activePlants.Values.ToList())
            {
                ProcessPlantDailyGrowth(plant);
            }

            ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", null);
        }

        public void ForceGrowthUpdate()
        {
            ProcessDailyGrowthForAllPlants();
            ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", null);
        }

        public void ProcessOfflineGrowth(float offlineHours)
        {
            if (!_enableAutoGrowth || offlineHours <= 0f)
            {
                return;
            }

            float daysOffline = offlineHours / 24f;
            int plantsProcessed = 0;

            foreach (var plant in _activePlants.Values.ToList())
            {
                try
                {
                    // Update plant age
                    UpdatePlantAge(plant.PlantId, offlineHours);

                    // Process growth over offline period
                    float growthAmount = CalculateGrowthRate(plant) * daysOffline * _timeAcceleration;
                    plant.CurrentGrowthProgress += growthAmount;

                    // Check for stage advancement
                    while (plant.CurrentGrowthProgress >= 1.0f && plant.CurrentGrowthStage != PlantGrowthStage.Harvestable)
                    {
                        if (AdvancePlantGrowthStage(plant.PlantId))
                        {
                            plant.CurrentGrowthProgress = 0f;
                        }
                        else
                        {
                            break;
                        }
                    }

                    plantsProcessed++;
                }
                catch (Exception ex)
                {
                    ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", null);
                }
            }

            ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", null);
        }

        public float CalculateGrowthRate(PlantInstanceSO plant)
        {
            if (plant == null)
            {
                return 0f;
            }

            // Base growth rate depends on growth stage
            float baseRate = plant.CurrentGrowthStage switch
            {
                PlantGrowthStage.Seedling => 0.1f,   // Slow initial growth
                PlantGrowthStage.Vegetative => 0.15f, // Fast vegetative growth
                PlantGrowthStage.Flowering => 0.08f,  // Slower flowering growth
                PlantGrowthStage.Harvestable => 0f,   // No further growth
                _ => 0.05f // Default rate
            };

            // Apply health modifier
            float healthModifier = Mathf.Lerp(0.1f, 1.5f, plant.CurrentHealth);

            // Apply care modifiers
            float waterModifier = Mathf.Lerp(0.2f, 1.2f, plant.WaterLevel);
            float nutrientModifier = Mathf.Lerp(0.3f, 1.3f, plant.NutrientLevel);

            return baseRate * healthModifier * waterModifier * nutrientModifier;
        }

        public bool AdvancePlantGrowthStage(string plantId)
        {
            var plant = GetPlant(plantId);
            if (plant == null)
            {
                return false;
            }

            var currentStage = plant.CurrentGrowthStage;
            var nextStage = GetNextGrowthStage(currentStage);

            if (nextStage == currentStage)
            {
                return false; // Already at final stage
            }

            var previousStage = plant.CurrentGrowthStage;
                            plant.CurrentGrowthStage = nextStage;
                            plant.CurrentGrowthProgress = 0f;

            OnPlantStageChanged?.Invoke(plantId, nextStage);

            ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", null);
            return true;
        }

        public void UpdatePlantAge(string plantId, float hoursElapsed)
        {
            var plant = GetPlant(plantId);
            if (plant != null)
            {
                // plant.AgeInDays += hoursElapsed / 24f; // Property not available, using DaysSincePlanted instead
                plant.DaysSincePlanted += (int)(hoursElapsed / 24f);
            }
        }

        public void UpdateHarvestStatistics(float yieldAmount)
        {
            _totalYieldHarvested += yieldAmount;
        }

        private void ProcessPlantDailyGrowth(PlantInstanceSO plant)
        {
            if (plant == null)
            {
                return;
            }

            try
            {
                // Update age
                // plant.AgeInDays += 1f; // Property not available, using DaysSincePlanted instead
                plant.DaysSincePlanted += 1;

                // Calculate growth
                float growthRate = CalculateGrowthRate(plant);
                plant.CurrentGrowthProgress += growthRate;

                // Check for stage advancement
                if (plant.CurrentGrowthProgress >= 1.0f && plant.CurrentGrowthStage != PlantGrowthStage.Harvestable)
                {
                    AdvancePlantGrowthStage(plant.PlantId);
                }

                // Decay resources slightly
                plant.WaterLevel = Mathf.Max(0f, plant.WaterLevel - 0.1f);
                plant.NutrientLevel = Mathf.Max(0f, plant.NutrientLevel - 0.08f);
            }
            catch (Exception ex)
            {
                ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", null);
            }
        }

        private PlantGrowthStage GetNextGrowthStage(PlantGrowthStage currentStage)
        {
            return currentStage switch
            {
                PlantGrowthStage.Seedling => PlantGrowthStage.Vegetative,
                PlantGrowthStage.Vegetative => PlantGrowthStage.Flowering,
                PlantGrowthStage.Flowering => PlantGrowthStage.Harvestable,
                PlantGrowthStage.Harvestable => PlantGrowthStage.Harvestable, // Final stage
                _ => currentStage
            };
        }

        private ProjectChimera.Data.Shared.PlantGrowthStage ConvertToPlantGrowthStage(ProjectChimera.Data.Shared.PlantGrowthStage sharedStage)
        {
            return sharedStage switch
            {
                ProjectChimera.Data.Shared.PlantGrowthStage.Seed => ProjectChimera.Data.Shared.PlantGrowthStage.Seed,
                ProjectChimera.Data.Shared.PlantGrowthStage.Seedling => ProjectChimera.Data.Shared.PlantGrowthStage.Seedling,
                ProjectChimera.Data.Shared.PlantGrowthStage.Vegetative => ProjectChimera.Data.Shared.PlantGrowthStage.Vegetative,
                ProjectChimera.Data.Shared.PlantGrowthStage.Flowering => ProjectChimera.Data.Shared.PlantGrowthStage.Flowering,
                ProjectChimera.Data.Shared.PlantGrowthStage.Harvest => ProjectChimera.Data.Shared.PlantGrowthStage.Harvest,
                _ => ProjectChimera.Data.Shared.PlantGrowthStage.Seed
            };
        }
    }
}
