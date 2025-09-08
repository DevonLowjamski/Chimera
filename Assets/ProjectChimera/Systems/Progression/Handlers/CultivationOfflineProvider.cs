using ProjectChimera.Core.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;

namespace ProjectChimera.Systems.Progression
{
    /// <summary>
    /// Handles plant growth, harvest scheduling, and cultivation progression during offline periods
    /// </summary>
    public class CultivationOfflineProvider : IOfflineProgressionProvider
    {
        [Header("Cultivation Configuration")]
        [SerializeField] private float _baseGrowthRate = 1.0f;
        [SerializeField] private float _autoHarvestThreshold = 0.95f;
        [SerializeField] private int _maxPlantsToProcess = 100;
        [SerializeField] private bool _enableAutoHarvest = true;
        [SerializeField] private bool _enableAutoPlanting = false;
        
        private readonly List<OfflineProgressionEvent> _cultivationEvents = new List<OfflineProgressionEvent>();
        
        public string GetProviderId() => "cultivation_offline";
        public float GetPriority() => 0.9f;
        
        public async Task<OfflineProgressionCalculationResult> CalculateOfflineProgressionAsync(TimeSpan offlineTime)
        {
            await Task.Delay(50); // Simulate complex plant calculations
            
            var result = new OfflineProgressionCalculationResult();
            var hours = (float)offlineTime.TotalHours;
            
            try
            {
                // Simulate plant growth progression
                var plantData = await CalculatePlantGrowthAsync(hours);
                result.ProgressionData.Add("plant_growth", plantData);
                
                // Calculate harvests that would have occurred
                var harvestData = await CalculateOfflineHarvestsAsync(hours);
                result.ProgressionData.Add("harvests", harvestData);
                
                // Calculate resource generation from plants
                var resourceData = CalculatePlantResourceGeneration(hours, harvestData);
                foreach (var resource in resourceData)
                {
                    result.ResourceChanges[resource.Key] = resource.Value;
                }
                
                // Add cultivation events
                result.Events.AddRange(_cultivationEvents);
                _cultivationEvents.Clear();
                
                // Generate notifications
                if (harvestData.CompletedHarvests > 0)
                {
                    result.Notifications.Add($"{harvestData.CompletedHarvests} plants were automatically harvested while you were away");
                }
                
                if (plantData.NewGrowthStageTransitions > 0)
                {
                    result.Notifications.Add($"{plantData.NewGrowthStageTransitions} plants advanced to new growth stages");
                }
                
                ChimeraLogger.Log($"[CultivationOfflineProvider] Processed {hours:F1} hours of cultivation progression");
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.ErrorMessage = $"Cultivation calculation failed: {ex.Message}";
            }
            
            return result;
        }
        
        public async Task ApplyOfflineProgressionAsync(OfflineProgressionResult result)
        {
            await Task.Delay(30);
            
            if (result.ProgressionData.TryGetValue("plant_growth", out var growthObj) && growthObj is PlantGrowthData growthData)
            {
                await ApplyPlantGrowthProgressionAsync(growthData);
            }
            
            if (result.ProgressionData.TryGetValue("harvests", out var harvestObj) && harvestObj is HarvestData harvestData)
            {
                await ApplyHarvestProgressionAsync(harvestData);
            }
            
            ChimeraLogger.Log($"[CultivationOfflineProvider] Applied cultivation progression for session {result.SessionId}");
        }
        
        private async Task<PlantGrowthData> CalculatePlantGrowthAsync(float hours)
        {
            await Task.Delay(20);
            
            var growthData = new PlantGrowthData();
            
            // Simulate plant growth calculations
            var activePlants = Mathf.Min(_maxPlantsToProcess, UnityEngine.Random.Range(5, 25)); // Simulated active plants
            var growthProgress = hours * _baseGrowthRate * 0.1f; // 10% per hour base rate
            
            growthData.ProcessedPlants = activePlants;
            growthData.AverageGrowthProgress = growthProgress;
            growthData.NewGrowthStageTransitions = Mathf.FloorToInt(activePlants * growthProgress * 0.3f);
            
            // Calculate matured plants
            growthData.MaturedPlants = Mathf.FloorToInt(activePlants * Mathf.Clamp01(growthProgress - 0.8f));
            
            if (growthData.MaturedPlants > 0)
            {
                _cultivationEvents.Add(new OfflineProgressionEvent
                {
                    EventType = "plants_matured",
                    Title = "Plants Matured",
                    Description = $"{growthData.MaturedPlants} plants reached maturity while you were away",
                    Priority = EventPriority.Normal,
                    Timestamp = DateTime.UtcNow.AddHours(-hours * 0.5)
                });
            }
            
            return growthData;
        }
        
        private async Task<HarvestData> CalculateOfflineHarvestsAsync(float hours)
        {
            await Task.Delay(15);
            
            var harvestData = new HarvestData();
            
            if (!_enableAutoHarvest)
            {
                harvestData.AutoHarvestDisabled = true;
                return harvestData;
            }
            
            // Calculate plants ready for harvest
            var plantsReadyForHarvest = Mathf.FloorToInt(hours * 0.5f); // Simulate harvest readiness
            var actualHarvests = Mathf.Min(plantsReadyForHarvest, _maxPlantsToProcess / 4);
            
            harvestData.CompletedHarvests = actualHarvests;
            harvestData.TotalYield = actualHarvests * UnityEngine.Random.Range(15f, 35f);
            harvestData.QualityRating = UnityEngine.Random.Range(0.7f, 0.95f);
            
            // Calculate harvest timing
            for (int i = 0; i < actualHarvests; i++)
            {
                var harvestTime = DateTime.UtcNow.AddHours(-hours + (hours * i / actualHarvests));
                harvestData.HarvestTimes.Add(harvestTime);
            }
            
            if (actualHarvests > 0)
            {
                _cultivationEvents.Add(new OfflineProgressionEvent
                {
                    EventType = "auto_harvest_completed",
                    Title = "Automatic Harvest",
                    Description = $"Harvested {actualHarvests} mature plants (Total yield: {harvestData.TotalYield:F1})",
                    Priority = EventPriority.High,
                    Timestamp = harvestData.HarvestTimes.LastOrDefault()
                });
            }
            
            return harvestData;
        }
        
        private Dictionary<string, float> CalculatePlantResourceGeneration(float hours, HarvestData harvestData)
        {
            var resources = new Dictionary<string, float>();
            
            // Base resource generation from active plants
            resources["biomass"] = hours * 2.5f;
            resources["cultivation_experience"] = hours * 1.2f;
            
            // Harvest-specific resources
            if (harvestData.CompletedHarvests > 0)
            {
                resources["harvested_materials"] = harvestData.TotalYield;
                resources["quality_bonuses"] = harvestData.QualityRating * harvestData.CompletedHarvests * 10f;
                resources["harvest_experience"] = harvestData.CompletedHarvests * 25f;
            }
            
            // Efficiency bonuses for longer offline periods
            if (hours > 24f)
            {
                var efficiencyBonus = Math.Min(0.5f, (hours - 24f) * 0.02f);
                foreach (var key in resources.Keys.ToList())
                {
                    resources[key] *= (1f + efficiencyBonus);
                }
            }
            
            return resources;
        }
        
        private async Task ApplyPlantGrowthProgressionAsync(PlantGrowthData growthData)
        {
            await Task.Delay(10);
            // Apply growth progression to actual plant instances
            // This would integrate with the actual cultivation system
        }
        
        private async Task ApplyHarvestProgressionAsync(HarvestData harvestData)
        {
            await Task.Delay(15);
            // Apply harvest results to inventory and plant states
            // This would integrate with the actual harvest system
        }
    }
}
