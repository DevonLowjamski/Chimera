using ProjectChimera.Core.Logging;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Data.Shared;
using ProjectChimera.Data.Genetics;
using ProjectChimera.Core.Events;
using PlantGrowthStage = ProjectChimera.Data.Shared.PlantGrowthStage;

namespace ProjectChimera.Systems.Cultivation
{
    /// <summary>
    /// PC-013-3: Plant Statistics Service - Handles statistics collection, reporting, and analytics
    /// Extracted from monolithic PlantManager for Single Responsibility Principle
    /// Focuses solely on plant data aggregation, metrics calculation, and reporting
    /// </summary>
    public class PlantStatisticsService
    {
        [Header("Statistics Configuration")]
        [SerializeField] private bool _enableDetailedLogging = false;
        [SerializeField] private bool _enableRealTimeUpdates = true;
        [SerializeField] private float _statisticsUpdateInterval = 5f; // Update every 5 seconds

        // Dependencies
        private CultivationManager _cultivationManager;
        private IPlantLifecycleService _lifecycleService;
        private object _achievementService;
        private IPlantGeneticsService _geneticsService;

        // Cached statistics
        private object _cachedStats;
        private object _cachedEnhancedStats;
        private float _lastStatsUpdate = 0f;

        // Real-time tracking
        private int _activePlantCount = 0;
        private float _averageHealth = 0f;
        private float _averageStress = 0f;
        private int _unhealthyPlants = 0;
        private int _highStressPlants = 0;

        public bool IsInitialized { get; private set; }

        public int ActivePlantCount => _activePlantCount;
        public float AverageHealth => _averageHealth;
        public float AverageStress => _averageStress;
        public int UnhealthyPlants => _unhealthyPlants;
        public int HighStressPlants => _highStressPlants;

        public PlantStatisticsService() : this(null, null, null, null)
        {
        }

        public PlantStatisticsService(CultivationManager cultivationManager, IPlantLifecycleService lifecycleService,
                                    object achievementService, IPlantGeneticsService geneticsService)
        {
            _cultivationManager = cultivationManager;
            _lifecycleService = lifecycleService;
            _achievementService = achievementService;
            _geneticsService = geneticsService;
        }

        public void Initialize()
        {
            if (IsInitialized) return;

            ChimeraLogger.Log("[PlantStatisticsService] Initializing statistics collection system...");

            // Get dependencies if not provided
            if (_cultivationManager == null)
            {
                _cultivationManager = GameManager.Instance?.GetManager<CultivationManager>();
            }

            if (_cultivationManager == null)
            {
                ChimeraLogger.LogError("[PlantStatisticsService] CultivationManager not found - statistics will be limited");
                return;
            }

            // Initialize cached statistics
            _cachedStats = new PlantManagerStatistics();
            _cachedEnhancedStats = new EnhancedPlantManagerStatistics();

            // Perform initial statistics update
            UpdateStatistics();

            IsInitialized = true;
            ChimeraLogger.Log($"[PlantStatisticsService] Statistics collection initialized (Real-time: {_enableRealTimeUpdates}, Interval: {_statisticsUpdateInterval}s)");
        }

        public void Shutdown()
        {
            if (!IsInitialized) return;

            ChimeraLogger.Log("[PlantStatisticsService] Shutting down statistics collection system...");

            // Log final statistics
            if (_enableDetailedLogging)
            {
                var finalStats = GetStatistics();
                ChimeraLogger.Log($"[PlantStatisticsService] Final statistics collected");
            }

            IsInitialized = false;
        }

        /// <summary>
        /// Updates statistics from all data sources.
        /// </summary>
        public void UpdateStatistics()
        {
            if (!IsInitialized) return;

            // Check if update is needed
            if (_enableRealTimeUpdates && Time.time - _lastStatsUpdate < _statisticsUpdateInterval)
                return;

            var startTime = System.DateTime.Now;

            try
            {
                // Update basic statistics
                UpdateBasicStatistics();

                // Update enhanced statistics if genetics service is available
                if (_geneticsService != null)
                {
                    UpdateEnhancedStatistics();
                }

                _lastStatsUpdate = Time.time;

                var updateTime = (System.DateTime.Now - startTime).TotalMilliseconds;

                if (_enableDetailedLogging)
                {
                    ChimeraLogger.Log($"[PlantStatisticsService] Statistics updated: {_activePlantCount} plants, {_averageHealth:F2} avg health (Time: {updateTime:F1}ms)");
                }
            }
            catch (System.Exception ex)
            {
                ChimeraLogger.LogError($"[PlantStatisticsService] Error updating statistics: {ex.Message}");
            }
        }

        /// <summary>
        /// Gets comprehensive plant manager statistics.
        /// </summary>
        public PlantManagerStatistics GetStatistics()
        {
            if (!IsInitialized)
                return new PlantManagerStatistics();

            // Return cached statistics if recent
            if (_enableRealTimeUpdates && Time.time - _lastStatsUpdate < _statisticsUpdateInterval)
            {
                return _cachedStats as ProjectChimera.Systems.Cultivation.PlantManagerStatistics;
            }

            // Force update if needed
            UpdateStatistics();

            return _cachedStats as ProjectChimera.Systems.Cultivation.PlantManagerStatistics;
        }

        /// <summary>
        /// Gets enhanced statistics including genetic performance data.
        /// </summary>
        public EnhancedPlantManagerStatistics GetEnhancedStatistics()
        {
            if (!IsInitialized)
                return new EnhancedPlantManagerStatistics();

            // Return cached statistics if recent
            if (_enableRealTimeUpdates && Time.time - _lastStatsUpdate < _statisticsUpdateInterval)
            {
                return _cachedEnhancedStats as ProjectChimera.Systems.Cultivation.EnhancedPlantManagerStatistics;
            }

            // Force update if needed
            UpdateStatistics();

            return _cachedEnhancedStats as ProjectChimera.Systems.Cultivation.EnhancedPlantManagerStatistics;
        }

        /// <summary>
        /// Gets real-time plant health distribution.
        /// </summary>
        public PlantHealthDistribution GetHealthDistribution()
        {
            if (!IsInitialized)
                return new PlantHealthDistribution();

            var distribution = new PlantHealthDistribution();
            var allPlants = _cultivationManager?.GetAllPlants();

            if (allPlants == null)
                return distribution;

            foreach (var plant in allPlants)
            {
                if (plant == null) continue;

                float health = plant.OverallHealth;

                if (health >= 0.8f)
                    distribution.Excellent++;
                else if (health >= 0.6f)
                    distribution.Good++;
                else if (health >= 0.4f)
                    distribution.Fair++;
                else if (health >= 0.2f)
                    distribution.Poor++;
                else
                    distribution.Critical++;
            }

            return distribution;
        }

        /// <summary>
        /// Gets plant growth stage distribution.
        /// </summary>
        public PlantStageDistribution GetStageDistribution()
        {
            if (!IsInitialized)
                return new PlantStageDistribution();

            var distribution = new PlantStageDistribution();
            var allPlants = _cultivationManager?.GetAllPlants();

            if (allPlants == null)
                return distribution;

            foreach (var plant in allPlants)
            {
                if (plant == null) continue;

                switch (plant.CurrentGrowthStage)
                {
                    case PlantGrowthStage.Seed:
                    case PlantGrowthStage.Germination:
                        distribution.Germinating++;
                        break;
                    case PlantGrowthStage.Seedling:
                        distribution.Seedling++;
                        break;
                    case PlantGrowthStage.Vegetative:
                        distribution.Vegetative++;
                        break;
                    case PlantGrowthStage.PreFlowering:
                    case PlantGrowthStage.Flowering:
                        distribution.Flowering++;
                        break;
                    case PlantGrowthStage.Ripening:
                        distribution.Ripening++;
                        break;
                    case PlantGrowthStage.Harvest:
                    case PlantGrowthStage.Harvestable:
                        distribution.ReadyForHarvest++;
                        break;
                }
            }

            return distribution;
        }

        /// <summary>
        /// Gets performance metrics for the statistics system.
        /// </summary>
        public StatisticsPerformanceMetrics GetPerformanceMetrics()
        {
            return new StatisticsPerformanceMetrics
            {
                LastUpdateTime = _lastStatsUpdate,
                UpdateInterval = _statisticsUpdateInterval,
                RealTimeUpdatesEnabled = _enableRealTimeUpdates,
                CachedPlantCount = _activePlantCount,
                StatisticsAge = Time.time - _lastStatsUpdate
            };
        }

        /// <summary>
        /// Forces an immediate statistics update.
        /// </summary>
        public void ForceUpdateStatistics()
        {
            if (!IsInitialized) return;

            _lastStatsUpdate = 0f; // Force update
            UpdateStatistics();

            if (_enableDetailedLogging)
            {
                ChimeraLogger.Log("[PlantStatisticsService] Forced statistics update completed");
            }
        }

        /// <summary>
        /// Exports statistics to formatted string.
        /// </summary>
        public string ExportStatistics(bool includeEnhanced = false)
        {
            if (!IsInitialized)
                return "Statistics service not initialized";

            var stats = GetStatistics();
            var report = new System.Text.StringBuilder();

            report.AppendLine("=== Plant Manager Statistics ===");
            report.AppendLine($"Total Plants: {stats.TotalPlants}");
            report.AppendLine($"Average Health: {stats.AverageHealth:F2}");
            report.AppendLine($"Average Stress: {stats.AverageStress:F2}");
            report.AppendLine($"Unhealthy Plants: {stats.UnhealthyPlants}");
            report.AppendLine($"High Stress Plants: {stats.HighStressPlants}");

            // Add stage distribution
            report.AppendLine("\n=== Growth Stage Distribution ===");
            var stageDistribution = GetStageDistribution();
            report.AppendLine($"Germinating: {stageDistribution.Germinating}");
            report.AppendLine($"Seedling: {stageDistribution.Seedling}");
            report.AppendLine($"Vegetative: {stageDistribution.Vegetative}");
            report.AppendLine($"Flowering: {stageDistribution.Flowering}");
            report.AppendLine($"Ripening: {stageDistribution.Ripening}");
            report.AppendLine($"Ready for Harvest: {stageDistribution.ReadyForHarvest}");

            // Add health distribution
            report.AppendLine("\n=== Health Distribution ===");
            var healthDistribution = GetHealthDistribution();
            report.AppendLine($"Excellent (80-100%): {healthDistribution.Excellent}");
            report.AppendLine($"Good (60-79%): {healthDistribution.Good}");
            report.AppendLine($"Fair (40-59%): {healthDistribution.Fair}");
            report.AppendLine($"Poor (20-39%): {healthDistribution.Poor}");
            report.AppendLine($"Critical (0-19%): {healthDistribution.Critical}");

            // Add enhanced statistics if requested
            if (includeEnhanced && _geneticsService != null)
            {
                var enhancedStats = GetEnhancedStatistics();
                report.AppendLine("\n=== Enhanced Statistics ===");
                report.AppendLine($"Advanced Genetics: {enhancedStats.AdvancedGeneticsEnabled}");
                report.AppendLine($"Genetic Performance: {enhancedStats.GeneticStats}");
                report.AppendLine($"Genetic Diversity: {enhancedStats.GeneticDiversityStats}");
            }

            // Add achievement statistics if available
            if (_achievementService != null)
            {
                // TODO: Implement achievement stats when available
                report.AppendLine("\n=== Achievement Statistics ===");
                report.AppendLine($"Plants Created: 0"); // Placeholder
                report.AppendLine($"Plants Harvested: 0"); // Placeholder
                report.AppendLine($"Total Yield: 0.0g"); // Placeholder
                report.AppendLine($"Highest Quality: 0.00"); // Placeholder
                report.AppendLine($"Survival Rate: 0.00"); // Placeholder
            }

            report.AppendLine($"\nGenerated: {System.DateTime.Now:yyyy-MM-dd HH:mm:ss}");

            return report.ToString();
        }

        /// <summary>
        /// Gets achievement statistics for plant management
        /// </summary>
        public PlantAchievementStats GetAchievementStats()
        {
            return new PlantAchievementStats
            {
                TotalPlantsCreated = 0, // TODO: Implement tracking
                TotalPlantsHarvested = 0, // TODO: Implement tracking
                TotalYieldHarvested = 0f, // TODO: Implement tracking
                HighestQualityAchieved = 0f, // TODO: Implement tracking
                HealthyPlantsCount = _activePlantCount - _unhealthyPlants,
                StrainDiversity = 0, // TODO: Implement tracking
                SurvivalRate = 0f // TODO: Implement tracking
            };
        }

        /// <summary>
        /// Updates basic plant statistics.
        /// </summary>
        private void UpdateBasicStatistics()
        {
            var allPlants = _cultivationManager?.GetAllPlants();
            if (allPlants == null)
            {
                _activePlantCount = 0;
                _averageHealth = 0f;
                _averageStress = 0f;
                _unhealthyPlants = 0;
                _highStressPlants = 0;
                return;
            }

            float totalHealth = 0f;
            float totalStress = 0f;
            int healthyPlants = 0;
            int[] stageDistribution = new int[System.Enum.GetValues(typeof(PlantGrowthStage)).Length];

            _activePlantCount = 0;
            _unhealthyPlants = 0;
            _highStressPlants = 0;

            foreach (var plant in allPlants)
            {
                if (plant == null) continue;

                _activePlantCount++;
                stageDistribution[(int)plant.CurrentGrowthStage]++;

                float health = plant.OverallHealth;
                float stress = plant.StressLevel;

                totalHealth += health;
                totalStress += stress;

                if (health < 0.5f)
                    _unhealthyPlants++;
                else
                    healthyPlants++;

                if (stress > 0.7f)
                    _highStressPlants++;
            }

            // Calculate averages
            _averageHealth = _activePlantCount > 0 ? totalHealth / _activePlantCount : 0f;
            _averageStress = _activePlantCount > 0 ? totalStress / _activePlantCount : 0f;

            // Convert stage distribution array to dictionary
            var stageDistributionDict = new Dictionary<PlantGrowthStage, int>();
            var stages = System.Enum.GetValues(typeof(PlantGrowthStage));
            for (int i = 0; i < stageDistribution.Length && i < stages.Length; i++)
            {
                stageDistributionDict[(PlantGrowthStage)stages.GetValue(i)] = stageDistribution[i];
            }

            // Update cached statistics
            var stats = new ProjectChimera.Systems.Cultivation.PlantManagerStatistics();
            stats.PlantCount = _activePlantCount;
            stats.StageDistribution = stageDistributionDict;
            stats.AverageHealth = _averageHealth;
            stats.AverageStress = _averageStress;
            stats.UnhealthyPlants = _unhealthyPlants;
            stats.HighStressPlants = _highStressPlants;
            _cachedStats = stats;
        }

        /// <summary>
        /// Updates enhanced statistics including genetic data.
        /// </summary>
        private void UpdateEnhancedStatistics()
        {
            // Create enhanced statistics from cached basic statistics
            var cachedStats = _cachedStats as ProjectChimera.Systems.Cultivation.PlantManagerStatistics;
            if (cachedStats != null)
            {
                var enhancedStats = new ProjectChimera.Systems.Cultivation.EnhancedPlantManagerStatistics();
                enhancedStats.PlantCount = cachedStats.PlantCount;
                enhancedStats.StageDistribution = cachedStats.StageDistribution;
                enhancedStats.AverageHealth = cachedStats.AverageHealth;
                enhancedStats.AverageStress = cachedStats.AverageStress;
                enhancedStats.UnhealthyPlants = cachedStats.UnhealthyPlants;
                enhancedStats.HighStressPlants = cachedStats.HighStressPlants;
                _cachedEnhancedStats = enhancedStats;
            }

            // Add genetic statistics
            if (_geneticsService != null && _cachedEnhancedStats != null)
            {
                // TODO: Implement when genetics service interface is finalized
                // var enhancedStats = _cachedEnhancedStats as ProjectChimera.Systems.Cultivation.EnhancedPlantManagerStatistics;
                // enhancedStats.AdvancedGeneticsEnabled = _geneticsService.AdvancedGeneticsEnabled;
                // enhancedStats.GeneticStats = _geneticsService.GetGeneticPerformanceStats();
                // enhancedStats.GeneticDiversityStats = _geneticsService.CalculateGeneticDiversityStats();
            }
        }

        /// <summary>
        /// Calculate comprehensive statistics for a specific plant
        /// </summary>
        public PlantStatisticsData CalculatePlantStatistics(PlantInstance plant)
        {
            if (!IsInitialized || plant == null)
            {
                return new PlantStatisticsData
                {
                    PlantID = plant?.PlantID ?? "Unknown",
                    HealthPercentage = 0f,
                    StressLevel = 0f,
                    GrowthStage = PlantGrowthStage.Seed,
                    DaysAlive = 0f
                };
            }

            return new PlantStatisticsData
            {
                PlantID = plant.PlantID,
                HealthPercentage = plant.CurrentHealth,
                StressLevel = plant.StressLevel,
                GrowthStage = plant.CurrentGrowthStage,
                DaysAlive = plant.DaysSincePlanted,
                IsActive = plant.IsActive,
                OverallPerformance = CalculateOverallPerformance(plant),
                HealthCategory = GetHealthCategory(plant.CurrentHealth),
                StressCategory = GetStressCategory(plant.StressLevel)
            };
        }

        /// <summary>
        /// Get performance metrics for a specific plant
        /// </summary>
        public PlantPerformanceMetrics GetPlantPerformanceMetrics(PlantInstance plant)
        {
            if (!IsInitialized || plant == null)
            {
                return new PlantPerformanceMetrics
                {
                    PlantID = plant?.PlantID ?? "Unknown",
                    PerformanceScore = 0f,
                    EfficiencyRating = 0f,
                    GrowthRate = 0f,
                    HealthTrend = 0f
                };
            }

            return new PlantPerformanceMetrics
            {
                PlantID = plant.PlantID,
                PerformanceScore = CalculatePerformanceScore(plant),
                EfficiencyRating = CalculateEfficiencyRating(plant),
                GrowthRate = CalculateGrowthRate(plant),
                HealthTrend = CalculateHealthTrend(plant),
                StressResistance = CalculateStressResistance(plant),
                AdaptationLevel = CalculateAdaptationLevel(plant)
            };
        }

        private float CalculateOverallPerformance(PlantInstance plant)
        {
            float healthScore = plant.CurrentHealth / 100f;
            float stressScore = 1f - (plant.StressLevel / 100f);
            float stageScore = GetStageProgressScore(plant.CurrentGrowthStage);

            return (healthScore * 0.4f + stressScore * 0.3f + stageScore * 0.3f) * 100f;
        }

        private string GetHealthCategory(float health)
        {
            if (health >= 80f) return "Excellent";
            if (health >= 60f) return "Good";
            if (health >= 40f) return "Fair";
            if (health >= 20f) return "Poor";
            return "Critical";
        }

        private string GetStressCategory(float stress)
        {
            if (stress <= 20f) return "Minimal";
            if (stress <= 40f) return "Low";
            if (stress <= 60f) return "Moderate";
            if (stress <= 80f) return "High";
            return "Critical";
        }

        private float GetStageProgressScore(PlantGrowthStage stage)
        {
            return stage switch
            {
                PlantGrowthStage.Seed => 0.1f,
                PlantGrowthStage.Germination => 0.2f,
                PlantGrowthStage.Seedling => 0.3f,
                PlantGrowthStage.Vegetative => 0.5f,
                PlantGrowthStage.PreFlowering => 0.6f,
                PlantGrowthStage.Flowering => 0.8f,
                PlantGrowthStage.Ripening => 0.9f,
                PlantGrowthStage.Harvest => 1.0f,
                _ => 0.5f
            };
        }

        private float CalculatePerformanceScore(PlantInstance plant)
        {
            return CalculateOverallPerformance(plant);
        }

        private float CalculateEfficiencyRating(PlantInstance plant)
        {
            // Resource efficiency based on health vs days alive
            float timeEfficiency = plant.DaysSincePlanted > 0 ? plant.CurrentHealth / plant.DaysSincePlanted : 0f;
            return Mathf.Clamp01(timeEfficiency) * 100f;
        }

        private float CalculateGrowthRate(PlantInstance plant)
        {
            // Growth rate approximation based on stage and time
            float stageProgress = GetStageProgressScore(plant.CurrentGrowthStage);
            float timeRate = plant.DaysSincePlanted > 0 ? stageProgress / plant.DaysSincePlanted : 0f;
            return timeRate * 100f;
        }

        private float CalculateHealthTrend(PlantInstance plant)
        {
            // Simplified trend calculation - would need historical data for accuracy
            float currentHealth = plant.CurrentHealth;
            float stressImpact = plant.StressLevel;
            return currentHealth - stressImpact; // Positive = improving, negative = declining
        }

        private float CalculateStressResistance(PlantInstance plant)
        {
            // Higher resistance = better ability to maintain health under stress
            return plant.StressLevel > 0 ? (plant.CurrentHealth / plant.StressLevel) * 10f : 100f;
        }

        private float CalculateAdaptationLevel(PlantInstance plant)
        {
            // Adaptation based on how well plant maintains performance over time
            float ageBonus = Mathf.Min(plant.DaysSincePlanted / 30f, 1f); // Up to 30 days for full adaptation
            float healthMaintenance = plant.CurrentHealth / 100f;
            return (ageBonus * 0.3f + healthMaintenance * 0.7f) * 100f;
        }
    }

    /// <summary>
    /// Plant statistics data structure
    /// </summary>
    [System.Serializable]
    public class PlantStatisticsData
    {
        public string PlantID;
        public float HealthPercentage;
        public float StressLevel;
        public PlantGrowthStage GrowthStage;
        public float DaysAlive;
        public bool IsActive;
        public float OverallPerformance;
        public string HealthCategory;
        public string StressCategory;
    }

    /// <summary>
    /// Plant performance metrics structure
    /// </summary>
    [System.Serializable]
    public class PlantPerformanceMetrics
    {
        public string PlantID;
        public float PerformanceScore;
        public float EfficiencyRating;
        public float GrowthRate;
        public float HealthTrend;
        public float StressResistance;
        public float AdaptationLevel;
    }

    /// <summary>
    /// Plant health distribution statistics.
    /// </summary>
    [System.Serializable]
    public class PlantHealthDistribution
    {
        public int Excellent = 0;   // 80-100%
        public int Good = 0;        // 60-79%
        public int Fair = 0;        // 40-59%
        public int Poor = 0;        // 20-39%
        public int Critical = 0;    // 0-19%

        public int Total => Excellent + Good + Fair + Poor + Critical;

        public override string ToString()
        {
            return $"Health Distribution - Excellent: {Excellent}, Good: {Good}, Fair: {Fair}, Poor: {Poor}, Critical: {Critical}";
        }
    }

    /// <summary>
    /// Plant growth stage distribution statistics.
    /// </summary>
    [System.Serializable]
    public class PlantStageDistribution
    {
        public int Germinating = 0;
        public int Seedling = 0;
        public int Vegetative = 0;
        public int Flowering = 0;
        public int Ripening = 0;
        public int ReadyForHarvest = 0;

        public int Total => Germinating + Seedling + Vegetative + Flowering + Ripening + ReadyForHarvest;

        public override string ToString()
        {
            return $"Stage Distribution - Germinating: {Germinating}, Seedling: {Seedling}, Vegetative: {Vegetative}, Flowering: {Flowering}, Ripening: {Ripening}, Harvest: {ReadyForHarvest}";
        }
    }

    /// <summary>
    /// Statistics system performance metrics.
    /// </summary>
    [System.Serializable]
    public class StatisticsPerformanceMetrics
    {
        public float LastUpdateTime;
        public float UpdateInterval;
        public bool RealTimeUpdatesEnabled;
        public int CachedPlantCount;
        public float StatisticsAge;

        public override string ToString()
        {
            return $"Stats Performance - Last Update: {LastUpdateTime:F1}s, Interval: {UpdateInterval:F1}s, Age: {StatisticsAge:F1}s, Cached: {CachedPlantCount}";
        }
    }
}
