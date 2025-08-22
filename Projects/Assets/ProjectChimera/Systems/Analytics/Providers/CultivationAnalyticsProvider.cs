using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProjectChimera.Systems.Analytics.Providers
{
    /// <summary>
    /// Analytics provider for cultivation-related metrics
    /// Integrates with CultivationManager to expose plant and harvest data
    /// </summary>
    public class CultivationAnalyticsProvider : AnalyticsProviderBase, ICultivationAnalyticsProvider
    {
        private ProjectChimera.Systems.Cultivation.CultivationManager _cultivationManager;
        
        // Cached values for performance
        private int _totalPlantsCount;
        private float _averageHealth;
        private float _totalYield;
        private float _facilityCapacity = 100f; // Default capacity

        protected override void Awake()
        {
            _providerName = "CultivationAnalytics";
            base.Awake();
            _enableDebugLogging = true;
        }

        #region Initialization

        public void Initialize(ProjectChimera.Systems.Cultivation.CultivationManager cultivationManager)
        {
            _cultivationManager = cultivationManager;
            
            if (_enableDebugLogging)
                Debug.Log("[CultivationAnalyticsProvider] Initialized with CultivationManager");
        }

        #endregion

        #region Metric Registration

        protected override void RegisterMetrics()
        {
            // Plant count metrics
            RegisterMetric("ActivePlants", "Active Plants", "plants", () => GetActivePlantCount());
            RegisterMetric("SeedlingCount", "Seedlings", "plants", () => GetPlantCountByStage("Seedling"));
            RegisterMetric("VegetativeCount", "Vegetative Plants", "plants", () => GetPlantCountByStage("Vegetative"));
            RegisterMetric("FloweringCount", "Flowering Plants", "plants", () => GetPlantCountByStage("Flowering"));
            RegisterMetric("HarvestReadyCount", "Harvest Ready", "plants", () => GetPlantCountByStage("HarvestReady"));

            // Health and quality metrics
            RegisterMetric("AveragePlantHealth", "Average Plant Health", "%", () => GetAveragePlantHealth());
            RegisterMetric("HealthyPlantsRatio", "Healthy Plants Ratio", "%", () => GetHealthyPlantsRatio());
            RegisterMetric("PlantMortalityRate", "Plant Mortality Rate", "%", () => GetPlantMortalityRate());

            // Yield metrics
            RegisterMetric("TotalYieldHarvested", "Total Yield Harvested", "g", () => GetTotalYieldHarvested());
            RegisterMetric("YieldPerHour", "Yield Per Hour", "g/hr", () => GetCurrentYieldRate());
            RegisterMetric("AverageYieldPerPlant", "Average Yield Per Plant", "g", () => GetAverageYieldPerPlant());
            RegisterMetric("YieldEfficiency", "Yield Efficiency", "%", () => GetYieldEfficiency());

            // Facility metrics
            RegisterMetric("FacilityUtilization", "Facility Utilization", "%", () => GetFacilityUtilization());
            RegisterMetric("GrowthSpaceUsed", "Growth Space Used", "%", () => GetGrowthSpaceUtilization());
            RegisterMetric("OptimalCapacityRatio", "Optimal Capacity Ratio", "%", () => GetOptimalCapacityRatio());

            // Operational metrics
            RegisterMetric("AverageGrowthTime", "Average Growth Time", "days", () => GetAverageGrowthTime());
            RegisterMetric("HarvestFrequency", "Harvest Frequency", "harvests/day", () => GetHarvestFrequency());
            RegisterMetric("CultivationEfficiency", "Cultivation Efficiency", "%", () => GetCultivationEfficiency());
        }

        #endregion

        #region ICultivationAnalyticsProvider Implementation

        public Dictionary<string, int> GetPlantCountByStage()
        {
            var stageCount = new Dictionary<string, int>
            {
                ["Seedling"] = GetPlantCountByStage("Seedling"),
                ["Vegetative"] = GetPlantCountByStage("Vegetative"),
                ["Flowering"] = GetPlantCountByStage("Flowering"),
                ["HarvestReady"] = GetPlantCountByStage("HarvestReady")
            };

            return stageCount;
        }

        public float GetAveragePlantHealth()
        {
            if (_cultivationManager == null)
                return _averageHealth; // Return cached/default value

            try
            {
                // In a real implementation, this would query the cultivation system
                // For now, simulate with reasonable values
                _averageHealth = Mathf.Lerp(_averageHealth, Random.Range(75f, 95f), Time.deltaTime * 0.1f);
                return RoundForDisplay(_averageHealth);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[CultivationAnalyticsProvider] Error getting average plant health: {ex.Message}");
                return _averageHealth;
            }
        }

        public float GetTotalYieldHarvested()
        {
            if (_cultivationManager == null)
                return _totalYield;

            try
            {
                // Simulate accumulating yield over time
                _totalYield += GetCurrentYieldRate() * Time.deltaTime / 3600f; // Convert to hourly rate
                return RoundForDisplay(_totalYield);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[CultivationAnalyticsProvider] Error getting total yield: {ex.Message}");
                return _totalYield;
            }
        }

        public float GetCurrentYieldRate()
        {
            if (_cultivationManager == null)
                return 0f;

            try
            {
                // Calculate yield rate based on plants ready for harvest
                var readyPlants = GetPlantCountByStage("HarvestReady");
                var averageYield = GetAverageYieldPerPlant();
                var harvestFreq = GetHarvestFrequency();
                
                return RoundForDisplay(readyPlants * averageYield * harvestFreq);
            }
            catch (System.Exception ex)
            {
                Debug.LogWarning($"[CultivationAnalyticsProvider] Error calculating yield rate: {ex.Message}");
                return 0f;
            }
        }

        public float GetFacilityUtilization()
        {
            var activePlants = GetActivePlantCount();
            return CalculatePercentage(activePlants, _facilityCapacity);
        }

        #endregion

        #region Helper Methods

        private int GetActivePlantCount()
        {
            if (_cultivationManager == null)
            {
                // Simulate plant count changes
                _totalPlantsCount = Mathf.RoundToInt(Mathf.Lerp(_totalPlantsCount, Random.Range(20, 80), Time.deltaTime * 0.05f));
                return _totalPlantsCount;
            }

            // In real implementation, would query cultivation manager
            return _totalPlantsCount;
        }

        private int GetPlantCountByStage(string stage)
        {
            var totalPlants = GetActivePlantCount();
            
            // Simulate distribution across growth stages
            return stage switch
            {
                "Seedling" => Mathf.RoundToInt(totalPlants * 0.25f),
                "Vegetative" => Mathf.RoundToInt(totalPlants * 0.35f),
                "Flowering" => Mathf.RoundToInt(totalPlants * 0.25f),
                "HarvestReady" => Mathf.RoundToInt(totalPlants * 0.15f),
                _ => 0
            };
        }

        private float GetHealthyPlantsRatio()
        {
            var avgHealth = GetAveragePlantHealth();
            return CalculatePercentage(avgHealth, 100f);
        }

        private float GetPlantMortalityRate()
        {
            // Simulate low mortality rate
            return RoundForDisplay(Random.Range(0.5f, 3.0f));
        }

        private float GetAverageYieldPerPlant()
        {
            // Simulate yield per plant based on health
            var healthMultiplier = GetAveragePlantHealth() / 100f;
            return RoundForDisplay(Random.Range(50f, 120f) * healthMultiplier);
        }

        private float GetYieldEfficiency()
        {
            // Efficiency based on yield vs optimal conditions
            var currentYield = GetCurrentYieldRate();
            var optimalYield = _facilityCapacity * 1.5f; // Optimal yield estimate
            return CalculatePercentage(currentYield, optimalYield);
        }

        private float GetGrowthSpaceUtilization()
        {
            // Similar to facility utilization but focused on physical space
            return GetFacilityUtilization() * Random.Range(0.85f, 1.15f);
        }

        private float GetOptimalCapacityRatio()
        {
            // Ratio of current operation to optimal capacity
            var utilizationScore = GetFacilityUtilization();
            var healthScore = GetAveragePlantHealth();
            return RoundForDisplay((utilizationScore + healthScore) / 2f);
        }

        private float GetAverageGrowthTime()
        {
            // Simulate growth time based on conditions
            var healthFactor = GetAveragePlantHealth() / 100f;
            var baseGrowthTime = 90f; // 90 days base
            return RoundForDisplay(baseGrowthTime / healthFactor);
        }

        private float GetHarvestFrequency()
        {
            // Harvests per day based on plant count and growth cycle
            var readyPlants = GetPlantCountByStage("HarvestReady");
            var growthCycleDays = GetAverageGrowthTime();
            return RoundForDisplay(readyPlants / growthCycleDays);
        }

        private float GetCultivationEfficiency()
        {
            // Overall cultivation efficiency score
            var healthScore = GetAveragePlantHealth();
            var utilizationScore = GetFacilityUtilization();
            var yieldScore = GetYieldEfficiency();
            
            return RoundForDisplay((healthScore + utilizationScore + yieldScore) / 3f);
        }

        #endregion

        #region Public Configuration

        public void SetFacilityCapacity(float capacity)
        {
            _facilityCapacity = capacity;
            
            if (_enableDebugLogging)
                Debug.Log($"[CultivationAnalyticsProvider] Facility capacity set to {capacity}");
        }

        public void SetDebugMode(bool enabled)
        {
            SetDebugLogging(enabled);
        }

        #endregion
    }
}