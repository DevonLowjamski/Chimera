using System.Collections.Generic;
using UnityEngine;
using ProjectChimera.Data.Cultivation;
using ProjectChimera.Data.Genetics;
using ProjectChimera.Data.Environment;
// using ProjectChimera.Systems.Genetics; // Invalid namespace - genetics in ProjectChimera.Data.Genetics
using EnvironmentalConditions = ProjectChimera.Data.Shared.EnvironmentalConditions;
using PlantGrowthStage = ProjectChimera.Data.Shared.PlantGrowthStage;
using EnvironmentalStressSO = ProjectChimera.Data.Simulation.EnvironmentalStressSO;

namespace ProjectChimera.Systems.Cultivation
{
    /// <summary>
    /// PC-013-3: Base interface for all plant management services
    /// Defines common contracts for plant service components
    /// </summary>
    public interface IPlantService
    {
        bool IsInitialized { get; }
        void Initialize();
        void Shutdown();
    }
    
    /// <summary>
    /// PC013-4a: Interface for plant growth management service
    /// Handles growth calculations, stage transitions, and growth-related operations
    /// </summary>
    public interface IPlantGrowthService : IPlantService
    {
        float GlobalGrowthModifier { get; set; }
        float BaseGrowthRate { get; set; }
        
        void UpdatePlantGrowth(PlantInstance plant, float deltaTime);
        void UpdatePlantGrowthBatch(List<PlantInstance> plants, float deltaTime);
        float CalculateGrowthRate(PlantInstance plant, float deltaTime);
        bool ForceStageProgression(PlantInstance plant);
        GrowthStageRequirements GetStageRequirements(PlantGrowthStage stage);
        GrowthServiceStatistics GetGrowthStatistics();
    }
    
    /// <summary>
    /// PC013-4b: Interface for plant environmental processing service
    /// Handles environmental adaptation, stress application, and environmental fitness calculations
    /// </summary>
    public interface IPlantEnvironmentalProcessingService : IPlantService
    {
        bool EnableStressSystem { get; set; }
        bool EnableGxEInteractions { get; set; }
        float StressRecoveryRate { get; set; }
        
        void UpdatePlantEnvironmentalProcessing(PlantInstance plant, float deltaTime);
        void UpdatePlantEnvironmentalProcessingBatch(List<PlantInstance> plants, float deltaTime);
        float CalculateEnvironmentalFitness(PlantInstance plant, EnvironmentalConditions conditions);
        void ApplyEnvironmentalStress(PlantInstance plant, EnvironmentalStressSO stressSource, float intensity);
        void RemoveEnvironmentalStress(PlantInstance plant, EnvironmentalStressSO stressSource);
        float GetPlantEnvironmentalFitness(string plantId);
        List<ActiveStressor> GetPlantActiveStressors(string plantId);
        EnvironmentalProcessingStatistics GetEnvironmentalStatistics();
    }

    /// <summary>
    /// Interface for plant lifecycle management service
    /// </summary>
    public interface IPlantLifecycleService : IPlantService
    {
        PlantInstance CreatePlant(PlantStrainSO strain, Vector3 position, Transform parent = null);
        List<PlantInstance> CreatePlants(PlantStrainSO strain, List<Vector3> positions, Transform parent = null);
        void RegisterPlant(PlantInstance plant);
        void UnregisterPlant(string plantID, PlantRemovalReason reason = PlantRemovalReason.Other);
        PlantInstanceSO GetPlant(string plantID);
        IEnumerable<PlantInstanceSO> GetAllPlants();
        IEnumerable<PlantInstanceSO> GetPlantsInStage(PlantGrowthStage stage);
        IEnumerable<PlantInstanceSO> GetHarvestablePlants();
        IEnumerable<PlantInstanceSO> GetPlantsNeedingAttention();
        
        // New methods needed by environmental service
        IEnumerable<PlantInstance> GetTrackedPlants();
        PlantInstance GetTrackedPlant(string plantID);
        
        // Event handlers
        System.Action<PlantInstance> OnPlantAdded { get; set; }
        System.Action<PlantInstance> OnPlantStageChanged { get; set; }
        System.Action<PlantInstance> OnPlantHealthUpdated { get; set; }
    }

    /// <summary>
    /// Interface for plant processing and update service
    /// </summary>
    public interface IPlantProcessingService : IPlantService
    {
        void UpdatePlants();
        void SetGlobalGrowthModifier(float modifier);
        float GlobalGrowthModifier { get; set; }
        void PerformCacheCleanup();
        void PerformPerformanceOptimization();
        int CalculateOptimalBatchSize();
        List<PlantInstance> GetNextPlantsToProcess(int count);
    }

    /// <summary>
    /// Interface for plant achievement and event tracking service
    /// </summary>
    public interface IPlantAchievementService : IPlantService
    {
        void TrackPlantCreation(PlantInstance plant);
        void TrackPlantHarvest(PlantInstance plant, SystemsHarvestResults results);
        void TrackPlantDeath(PlantInstance plant);
        void TrackPlantHealthChange(PlantInstance plant);
        void TrackPlantGrowthStageChange(PlantInstance plant);
        bool EnableAchievementTracking { get; set; }
        
        // Achievement statistics
        int TotalPlantsCreated { get; }
        int TotalPlantsHarvested { get; }
        float TotalYieldHarvested { get; }
        float HighestQualityAchieved { get; }
        int HealthyPlantsCount { get; }
        int StrainDiversity { get; }
        
        // Achievement statistics method
        PlantAchievementStats GetAchievementStats();
    }

    /// <summary>
    /// Interface for plant genetics and genetic performance service
    /// </summary>
    public interface IPlantGeneticsService : IPlantService
    {
        GeneticPerformanceStats GetGeneticPerformanceStats();
        void SetAdvancedGeneticsEnabled(bool enabled);
        GeneticDiversityStats CalculateGeneticDiversityStats();
        bool AdvancedGeneticsEnabled { get; set; }
        
        // Genetic monitoring
        void RecordGeneticCalculation(float calculationTime);
        void RecordBatchUpdate(int plantCount, GeneticPerformanceStats stats);
    }

    /// <summary>
    /// Interface for plant harvest management service
    /// </summary>
    public interface IPlantHarvestService : IPlantService
    {
        SystemsHarvestResults HarvestPlant(string plantID);
        float CalculateExpectedYield(PlantInstance plantInstance);
        float GetStageYieldModifier(PlantGrowthStage stage);
        bool EnableYieldVariability { get; set; }
        bool EnablePostHarvestProcessing { get; set; }
        float HarvestQualityMultiplier { get; set; }
        
        // Harvest events
        System.Action<PlantInstance> OnPlantHarvested { get; set; }
    }
    
    /// <summary>
    /// PC013-4c: Interface for plant yield calculation service
    /// Handles harvest yield computations, quality assessments, and yield predictions
    /// </summary>
    public interface IPlantYieldCalculationService : IPlantService
    {
        bool EnableYieldVariability { get; set; }
        bool EnablePostHarvestProcessing { get; set; }
        float HarvestQualityMultiplier { get; set; }
        
        SystemsHarvestResults HarvestPlant(string plantID);
        float CalculateExpectedYield(PlantInstance plantInstance);
        float GetStageYieldModifier(PlantGrowthStage stage);
        YieldCalculationStatistics GetYieldStatistics();
    }

    /// <summary>
    /// Interface for plant statistics and reporting service
    /// </summary>
    public interface IPlantStatisticsService : IPlantService
    {
        PlantManagerStatistics GetStatistics();
        EnhancedPlantManagerStatistics GetEnhancedStatistics();
        void UpdateStatistics();
        
        // Statistics properties
        int ActivePlantCount { get; }
        float AverageHealth { get; }
        float AverageStress { get; }
        int UnhealthyPlants { get; }
        int HighStressPlants { get; }
    }

    /// <summary>
    /// Interface for plant environmental adaptation service
    /// </summary>
    public interface IPlantEnvironmentalService : IPlantService
    {
        void UpdateEnvironmentalAdaptation(EnvironmentalConditions conditions);
        void UpdateEnvironmentalAdaptation(string plantID, EnvironmentalConditions conditions);
        void ApplyEnvironmentalStress(EnvironmentalStressSO stressSource, float intensity);
        void UpdateEnvironmentalConditions(EnvironmentalConditions newConditions);
        
        // Environmental settings
        bool EnableEnvironmentalStress { get; set; }
        float StressRecoveryRate { get; set; }
    }

    /// <summary>
    /// Plant removal reason enumeration
    /// </summary>
    public enum PlantRemovalReason
    {
        Harvested,
        Died,
        Removed,
        Other
    }

    /// <summary>
    /// Cultivation event tracker for achievement and progression systems.
    /// </summary>
    public class CultivationEventTracker
    {
        private Dictionary<string, int> _plantCounts = new Dictionary<string, int>();
        private Dictionary<string, float> _harvestTotals = new Dictionary<string, float>();
        private List<PlantInstance> _healthyPlants = new List<PlantInstance>();
        private int _totalPlantsCreated = 0;
        private int _totalPlantsHarvested = 0;
        private int _totalPlantDeaths = 0;
        private float _totalYieldHarvested = 0f;
        private float _highestQualityAchieved = 0f;

        public void OnPlantCreated(PlantInstance plant)
        {
            _totalPlantsCreated++;
            
            if (plant.Strain != null)
            {
                string strainName = plant.Strain.StrainName;
                if (_plantCounts.ContainsKey(strainName))
                    _plantCounts[strainName]++;
                else
                    _plantCounts[strainName] = 1;
            }
        }

        public void OnPlantHarvested(PlantInstance plant, SystemsHarvestResults harvestResults)
        {
            _totalPlantsHarvested++;
            _totalYieldHarvested += harvestResults.TotalYieldGrams;
            
            if (harvestResults.QualityScore > _highestQualityAchieved)
                _highestQualityAchieved = harvestResults.QualityScore;
            
            if (plant.Strain != null)
            {
                string strainName = plant.Strain.StrainName;
                if (_harvestTotals.ContainsKey(strainName))
                    _harvestTotals[strainName] += harvestResults.TotalYieldGrams;
                else
                    _harvestTotals[strainName] = harvestResults.TotalYieldGrams;
            }
        }

        public void OnPlantDied(PlantInstance plant)
        {
            _totalPlantDeaths++;
            _healthyPlants.Remove(plant);
        }

        public void OnPlantHealthChanged(PlantInstance plant)
        {
            if (plant.CurrentHealth > 0.8f && !_healthyPlants.Contains(plant))
            {
                _healthyPlants.Add(plant);
            }
            else if (plant.CurrentHealth <= 0.8f && _healthyPlants.Contains(plant))
            {
                _healthyPlants.Remove(plant);
            }
        }

        public void OnPlantGrowthStageChanged(PlantInstance plant)
        {
            // Track growth milestones for achievements
            if (plant.CurrentGrowthStage == PlantGrowthStage.Flowering && plant.CurrentHealth > 0.9f)
            {
                // High-health flowering achievement
            }
        }
        
        public void ProcessPlantAchievements(PlantInstance plant)
        {
            // Process any pending achievement checks for this plant
            OnPlantHealthChanged(plant);
            OnPlantGrowthStageChanged(plant);
        }

        // Properties for accessing tracked data
        public int TotalPlantsCreated => _totalPlantsCreated;
        public int TotalPlantsHarvested => _totalPlantsHarvested;
        public float TotalYieldHarvested => _totalYieldHarvested;
        public float HighestQualityAchieved => _highestQualityAchieved;
        public int HealthyPlantsCount => _healthyPlants.Count;
        public int StrainDiversity => _plantCounts.Count;
    }

    /// <summary>
    /// Harvest results structure for Systems layer
    /// </summary>
    [System.Serializable]
    public class SystemsHarvestResults
    {
        public string PlantID;
        public float TotalYieldGrams;
        public float QualityScore;
        public CannabinoidProfile Cannabinoids;
        public TerpeneProfile Terpenes;
        public int FloweringDays;
        public float FinalHealth;
        public System.DateTime HarvestDate;
    }

    /// <summary>
    /// Harvest results structure for achievement tracking
    /// </summary>
    [System.Serializable]
    public class HarvestResults
    {
        public string PlantID;
        public float TotalYield;
        public float TotalYieldGrams; // Alias for compatibility
        public float QualityScore;
        public CannabinoidProfile Cannabinoids;
        public TerpeneProfile Terpenes;
        public int FloweringDays;
        public float FinalHealth;
        public System.DateTime HarvestDate;
        
        // Constructor to sync the two yield fields
        public HarvestResults()
        {
            // Ensure both fields stay in sync
        }
    }

    /// <summary>
    /// Genetic performance statistics structure
    /// </summary>
    [System.Serializable]
    public class GeneticPerformanceStats
    {
        public long TotalCalculations;
        public double AverageCalculationTimeMs;
        public double CacheHitRatio;
        public long BatchCalculations;
        public double AverageBatchTimeMs;
        public double AverageUpdateTimeMs;
        
        public override string ToString()
        {
            return $"Calcs: {TotalCalculations}, Avg: {AverageCalculationTimeMs:F2}ms, Cache: {CacheHitRatio:F2}, Batch: {BatchCalculations}, BatchAvg: {AverageBatchTimeMs:F2}ms";
        }
    }

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
        public int TotalPlants;
        public int[] PlantsByStage = new int[System.Enum.GetValues(typeof(PlantGrowthStage)).Length];
        public float AverageHealth;
        public float AverageStress;
        public int UnhealthyPlants;
        public int HighStressPlants;
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
    
    /// <summary>
    /// Plant achievement statistics structure
    /// </summary>
    [System.Serializable]
    public class PlantAchievementStats
    {
        public int TotalPlantsCreated;
        public int TotalPlantsHarvested;
        public int TotalPlantDeaths;
        public float TotalYieldHarvested;
        public float HighestQualityAchieved;
        public int HealthyPlantsCount;
        public int StrainDiversity;
        public float SurvivalRate;
        public float AverageYieldPerPlant;
        public float AverageQuality;
        public float AverageYield;
        public int PerfectQualityCount;
        public int HighYieldCount;
        public System.DateTime LastHarvestDate;
        
        public override string ToString()
        {
            return $"Created: {TotalPlantsCreated}, Harvested: {TotalPlantsHarvested}, Deaths: {TotalPlantDeaths}, " +
                   $"Yield: {TotalYieldHarvested:F0}g, Quality: {HighestQualityAchieved:F2}, " +
                   $"Healthy: {HealthyPlantsCount}, Strains: {StrainDiversity}, Survival: {SurvivalRate:F2}";
        }
    }
}