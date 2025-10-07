using ProjectChimera.Core.Logging;
using ProjectChimera.Data.Shared;
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
    /// Implementation for harvest management and processing service
    /// </summary>
    public class HarvestProcessingService : IHarvestManager
    {
        private IPlantLifecycle _plantLifecycle;
        private float _totalYieldHarvested = 0f;
        private int _totalPlantsHarvested = 0;
        private List<HarvestResult> _harvestHistory = new List<HarvestResult>();
        private bool _isInitialized = false;

        public float TotalYieldHarvested => _totalYieldHarvested;
        public int TotalPlantsHarvested => _totalPlantsHarvested;

        public Action<string, HarvestResult> OnPlantHarvested { get; set; }
        public Action<string> OnHarvestReady { get; set; }
        public Action<string> OnPlantOverripe { get; set; }
        public Action<HarvestStatistics> OnHarvestStatisticsUpdated { get; set; }

        public HarvestProcessingService(IPlantLifecycle plantLifecycle = null)
        {
            _plantLifecycle = plantLifecycle;
        }

        public void Initialize(IPlantLifecycle plantLifecycle = null)
        {
            if (plantLifecycle != null)
            {
                _plantLifecycle = plantLifecycle;
            }

            _isInitialized = true;
            ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", null);
        }

        public void Shutdown()
        {
            _harvestHistory.Clear();
            _isInitialized = false;
            ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", null);
        }

        public bool HarvestPlant(string plantId)
        {
            if (!_isInitialized || _plantLifecycle == null)
            {
                ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", null);
                return false;
            }

            var plant = _plantLifecycle.GetPlant(plantId);
            if (plant == null)
            {
                ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", null);
                return false;
            }

            if (!IsPlantReadyForHarvest(plantId))
            {
                ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", null);
                return false;
            }

            try
            {
                var harvestResult = ProcessHarvest(plant);

                if (harvestResult.Success)
                {
                    // Remove plant from lifecycle manager
                    _plantLifecycle.RemovePlant(plantId, true);

                    // Update statistics
                    _totalYieldHarvested += harvestResult.YieldAmount;
                    _totalPlantsHarvested++;

                    // Record harvest
                    _harvestHistory.Add(harvestResult);

                    // Trigger events
                    OnPlantHarvested?.Invoke(plantId, harvestResult);

                    // Update lifecycle statistics
                    if (_plantLifecycle is PlantLifecycle lifecycle)
                    {
                        lifecycle.UpdateHarvestStatistics(harvestResult.YieldAmount);
                    }

                    ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", null);
                    return true;
                }
                else
                {
                    ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", null);
                    return false;
                }
            }
            catch (Exception ex)
            {
                ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", null);
                return false;
            }
        }

        public HarvestResult ProcessHarvest(PlantInstanceSO plant)
        {
            if (plant == null)
            {
                return HarvestResult.CreateFailure("", "Plant is null");
            }

            if (plant.CurrentGrowthStage != PlantGrowthStage.Harvestable)
            {
                return HarvestResult.CreateFailure(plant.PlantId, "Plant is not in harvestable stage");
            }

            try
            {
                // Calculate yield based on plant characteristics
                float expectedYield = CalculateExpectedYield(plant);
                float actualYield = expectedYield * UnityEngine.Random.Range(0.8f, 1.2f); // Add some variance

                // Determine quality based on plant health and care
                var quality = DetermineHarvestQuality(plant);
                float qualityScore = CalculateYieldQuality(plant);

                // Apply quality modifier to yield
                float qualityModifier = quality switch
                {
                    HarvestQuality.Premium => 1.3f,
                    HarvestQuality.Excellent => 1.15f,
                    HarvestQuality.Good => 1.0f,
                    HarvestQuality.Fair => 0.85f,
                    HarvestQuality.Poor => 0.6f,
                    _ => 1.0f
                };

                actualYield *= qualityModifier;

                var result = HarvestResult.CreateSuccess(plant.PlantId, actualYield, quality, qualityScore);

                // Process the harvested material
                ProcessHarvestedMaterial(plant.PlantId, actualYield, quality);

                return result;
            }
            catch (Exception ex)
            {
                ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", null);
                return HarvestResult.CreateFailure(plant.PlantId, $"Harvest processing error: {ex.Message}");
            }
        }

        public bool IsPlantReadyForHarvest(string plantId)
        {
            var plant = _plantLifecycle?.GetPlant(plantId);
            return plant?.CurrentGrowthStage == PlantGrowthStage.Harvestable;
        }

        public bool IsPlantOverripe(string plantId)
        {
            var plant = _plantLifecycle?.GetPlant(plantId);
            if (plant == null || plant.CurrentGrowthStage != PlantGrowthStage.Harvestable)
            {
                return false;
            }

            // Plant is overripe if it's been harvestable for more than 14 days
            return plant.DaysSincePlanted > 90; // Assuming 76 days to reach harvestable + 14 days overripe threshold
        }

        public float CalculateOptimalHarvestTime(PlantInstanceSO plant)
        {
            if (plant == null)
            {
                return 0f;
            }

            // Optimal harvest time is species-dependent
            // For now, use a simplified calculation based on growth stage and health
            float baseHarvestTime = 76f; // Average days to harvest

            // Adjust based on plant health (healthier plants mature faster)
            float healthModifier = Mathf.Lerp(0.9f, 1.1f, plant.CurrentHealth);

            return baseHarvestTime * healthModifier;
        }

        public float CalculateExpectedYield(PlantInstanceSO plant)
        {
            if (plant == null)
            {
                return 0f;
            }

            // Base yield calculation
            float baseYield = 50f; // Base 50 grams per plant

            // Size modifier based on plant height and health
            float sizeModifier = (plant.CurrentHeight / 100f) * 0.5f + 0.5f; // 0.5x to 1.5x based on height
            float healthModifier = Mathf.Lerp(0.3f, 1.5f, plant.CurrentHealth);

            // Care quality modifier
            float careModifier = (plant.WaterLevel + plant.NutrientLevel) / 2f;
            careModifier = Mathf.Lerp(0.4f, 1.3f, careModifier);

            return baseYield * sizeModifier * healthModifier * careModifier;
        }

        public float CalculateYieldQuality(PlantInstanceSO plant)
        {
            if (plant == null)
            {
                return 0f;
            }

            // Quality is based on overall plant care and health
            float healthScore = plant.CurrentHealth;
            float careScore = (plant.WaterLevel + plant.NutrientLevel) / 2f;

            // Environmental stress factor (simplified)
            float environmentScore = 0.8f; // Would calculate from environmental conditions

            return (healthScore + careScore + environmentScore) / 3f;
        }

        public HarvestQuality DetermineHarvestQuality(PlantInstanceSO plant)
        {
            float qualityScore = CalculateYieldQuality(plant);

            return qualityScore switch
            {
                >= 0.9f => HarvestQuality.Premium,
                >= 0.8f => HarvestQuality.Excellent,
                >= 0.65f => HarvestQuality.Good,
                >= 0.4f => HarvestQuality.Fair,
                _ => HarvestQuality.Poor
            };
        }

        public void ProcessOfflineHarvestChecks(float offlineHours)
        {
            if (!_isInitialized || _plantLifecycle == null || offlineHours <= 0f)
            {
                return;
            }

            try
            {
                // Check for plants that became ready for harvest during offline period
                var harvestReadyPlants = GetPlantsReadyForHarvest();
                if (harvestReadyPlants.Any())
                {
                    int harvestableCount = harvestReadyPlants.Count;
                    ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", null);

                    // Check for overripe plants
                    foreach (var plantId in harvestReadyPlants)
                    {
                        if (IsPlantOverripe(plantId))
                        {
                            OnPlantOverripe?.Invoke(plantId);
                            ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", null);
                        }
                        else
                        {
                            OnHarvestReady?.Invoke(plantId);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", null);
            }
        }

        public List<string> GetPlantsReadyForHarvest()
        {
            if (_plantLifecycle == null)
            {
                return new List<string>();
            }

            var harvestablePlants = _plantLifecycle.GetPlantsByStage(PlantGrowthStage.Harvestable);
            return harvestablePlants.Select(p => p.PlantId).ToList();
        }

        public void ScheduleAutomaticHarvest(string plantId, float hoursUntilHarvest)
        {
            // This would integrate with a scheduling system
            // For now, just log the schedule
            ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", null);
        }

        public HarvestStatistics GetHarvestStatistics()
        {
            try
            {
                var qualityDistribution = new Dictionary<HarvestQuality, int>();

                // Initialize quality distribution
                foreach (HarvestQuality quality in Enum.GetValues(typeof(HarvestQuality)))
                {
                    qualityDistribution[quality] = 0;
                }

                // Count harvests by quality
                foreach (var harvest in _harvestHistory)
                {
                    if (qualityDistribution.ContainsKey(harvest.Quality))
                    {
                        qualityDistribution[harvest.Quality]++;
                    }
                }

                var statistics = new HarvestStatistics
                {
                    TotalPlantsHarvested = _totalPlantsHarvested,
                    TotalYieldHarvested = _totalYieldHarvested,
                    AverageYieldPerPlant = GetAverageYieldPerPlant(),
                    AverageQualityScore = _harvestHistory.Any() ? _harvestHistory.Average(h => h.QualityScore) : 0f,
                    QualityDistribution = qualityDistribution,
                    LastHarvestTime = _harvestHistory.Any() ? _harvestHistory.Last().HarvestTime : DateTime.MinValue,
                    BestPerformingStrain = "Unknown", // Would track by strain
                    BestYieldAchieved = _harvestHistory.Any() ? _harvestHistory.Max(h => h.YieldAmount) : 0f
                };

                return statistics;
            }
            catch (Exception ex)
            {
                ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", null);
                return new HarvestStatistics();
            }
        }

        public float GetAverageYieldPerPlant()
        {
            if (_totalPlantsHarvested == 0)
            {
                return 0f;
            }

            return _totalYieldHarvested / _totalPlantsHarvested;
        }

        public Dictionary<HarvestQuality, int> GetYieldQualityDistribution()
        {
            var distribution = new Dictionary<HarvestQuality, int>();

            // Initialize all quality levels
            foreach (HarvestQuality quality in Enum.GetValues(typeof(HarvestQuality)))
            {
                distribution[quality] = 0;
            }

            // Count harvests by quality
            foreach (var harvest in _harvestHistory)
            {
                if (distribution.ContainsKey(harvest.Quality))
                {
                    distribution[harvest.Quality]++;
                }
            }

            return distribution;
        }

        public void ProcessHarvestedMaterial(string plantId, float yieldAmount, HarvestQuality quality)
        {
            try
            {
                // This would integrate with inventory system
                // For now, just log the processed material
                ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", null);

                // Update inventory would happen here
                // UpdateInventoryWithHarvest(harvestResult);
            }
            catch (Exception ex)
            {
                ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", null);
            }
        }

        public void UpdateInventoryWithHarvest(HarvestResult harvestResult)
        {
            if (!harvestResult.Success)
            {
                return;
            }

            try
            {
                // This would update the inventory system with harvested materials
                // Implementation would depend on the inventory system design
                ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", null);
            }
            catch (Exception ex)
            {
                ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", null);
            }
        }

        public void SetDependencies(IPlantLifecycle plantLifecycle)
        {
            _plantLifecycle = plantLifecycle;
        }

        public void ClearHarvestHistory()
        {
            _harvestHistory.Clear();
            ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", null);
        }

        public List<HarvestResult> GetRecentHarvests(int count = 10)
        {
            return _harvestHistory.TakeLast(count).ToList();
        }

        public void TriggerStatisticsUpdate()
        {
            var statistics = GetHarvestStatistics();
            OnHarvestStatisticsUpdated?.Invoke(statistics);
        }
    }
}
