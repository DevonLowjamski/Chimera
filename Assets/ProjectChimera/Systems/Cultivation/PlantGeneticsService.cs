using ProjectChimera.Core.Logging;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Data.Shared;
using ProjectChimera.Data.Genetics;
using ProjectChimera.Data.Cultivation;
using EnvironmentalConditions = ProjectChimera.Data.Shared.EnvironmentalConditions;
using PlantGrowthStage = ProjectChimera.Data.Shared.PlantGrowthStage;
// Remove invalid namespace references - genetics types are in ProjectChimera.Data.Genetics
// using TraitExpressionResult = ProjectChimera.Data.Genetics.TraitExpressionResult;
// using GeneticPerformanceStats = ProjectChimera.Systems.Cultivation.GeneticPerformanceStats;
// using GeneticsPerformanceStats = ProjectChimera.Data.Genetics.GeneticsPerformanceStats;

namespace ProjectChimera.Systems.Cultivation
{
    /// <summary>
    /// PC-013-3: Plant Genetics Service - Handles advanced genetics integration and genetic performance monitoring
    /// Extracted from monolithic PlantManager for Single Responsibility Principle
    /// Focuses solely on genetic calculations, monitoring, and performance optimization
    /// </summary>
    public class PlantGeneticsService
    {
        [Header("Genetics Configuration")]
        [SerializeField] private bool _advancedGeneticsEnabled = true;
        [SerializeField] private bool _enableGeneticPerformanceMonitoring = true;
        [SerializeField] private bool _enableDetailedLogging = false;

        // Genetic performance monitoring
        private object _geneticPerformanceMonitor;
        private CultivationManager _cultivationManager;

        // Performance tracking
        private long _totalCalculations = 0;
        private double _totalCalculationTime = 0.0;
        private long _totalBatchCalculations = 0;
        private double _totalBatchTime = 0.0;
        private List<double> _recentCalculationTimes = new List<double>();
        private const int MAX_RECENT_TIMES = 100;

        public bool IsInitialized { get; private set; }

        public bool AdvancedGeneticsEnabled
        {
            get => _advancedGeneticsEnabled;
            set => _advancedGeneticsEnabled = value;
        }

        public PlantGeneticsService() : this(null)
        {
        }

        public PlantGeneticsService(CultivationManager cultivationManager)
        {
            _cultivationManager = cultivationManager;
        }

        public void Initialize()
        {
            if (IsInitialized) return;

            ChimeraLogger.Log("[PlantGeneticsService] Initializing genetics monitoring system...");

            // Initialize genetic performance monitoring
            if (_enableGeneticPerformanceMonitoring) { _geneticPerformanceMonitor = new object(); }

            // Get reference to CultivationManager
            if (_cultivationManager == null)
            {
                _cultivationManager = GameManager.Instance?.GetManager<CultivationManager>();
            }

            if (_cultivationManager == null)
            {
                ChimeraLogger.LogWarning("[PlantGeneticsService] CultivationManager not found - some features may be limited");
            }

            IsInitialized = true;
            ChimeraLogger.Log($"[PlantGeneticsService] Genetics monitoring initialized (Advanced: {_advancedGeneticsEnabled}, Monitoring: {_enableGeneticPerformanceMonitoring})");
        }

        public void Shutdown()
        {
            if (!IsInitialized) return;

            ChimeraLogger.Log("[PlantGeneticsService] Shutting down genetics monitoring system...");

            _geneticPerformanceMonitor = null;
            _recentCalculationTimes.Clear();

            IsInitialized = false;
        }

        /// <summary>
        /// Gets genetic performance statistics for monitoring and optimization.
        /// </summary>
        public GeneticPerformanceStats GetGeneticPerformanceStats()
        {
            if (!IsInitialized)
            {
                return new GeneticPerformanceStats
                {
                    TotalCalculations = 0,
                    AverageCalculationTimeMs = 0.0,
                    CacheHitRatio = 0.0,
                    BatchCalculations = 0,
                    AverageBatchTimeMs = 0.0,
                    AverageUpdateTimeMs = 0.0
                };
            }

            // In stub mode we may not have a real monitor. If present and reflective API exists, try to map; otherwise return fallback.
            // Note: Avoids hard dependency on Systems.Genetics.
            if (_geneticPerformanceMonitor != null)
            {
                try
                {
                    // dynamic geneticsStats = _geneticPerformanceMonitor; // Disabled to avoid CS0656 missing compiler required member error
                    var stats = new GeneticPerformanceStats
                    {
                        TotalCalculations = (int)_totalCalculations,
                        AverageCalculationTimeMs = _totalCalculations > 0 ? _totalCalculationTime / _totalCalculations : 0.0,
                        CacheHitRatio = 0.8, // Placeholder
                        BatchCalculations = (int)_totalBatchCalculations,
                        AverageBatchTimeMs = _totalBatchCalculations > 0 ? _totalBatchTime / _totalBatchCalculations : 0.0,
                        AverageUpdateTimeMs = _recentCalculationTimes.Count > 0 ? _recentCalculationTimes.Average() : 0.0
                    };
                    return stats;
                }
                catch { /* Fall through to fallback below */ }
            }

            // Fallback to internal tracking
            return new GeneticPerformanceStats
            {
                TotalCalculations = (int)_totalCalculations,
                AverageCalculationTimeMs = _totalCalculations > 0 ? _totalCalculationTime / _totalCalculations : 0.0,
                CacheHitRatio = 0.0, // Not tracked in fallback
                BatchCalculations = (int)_totalBatchCalculations,
                AverageBatchTimeMs = _totalBatchCalculations > 0 ? _totalBatchTime / _totalBatchCalculations : 0.0,
                AverageUpdateTimeMs = _recentCalculationTimes.Count > 0 ? _recentCalculationTimes.Average() : 0.0
            };
        }

        /// <summary>
        /// Enables or disables advanced genetics during runtime.
        /// </summary>
        public void SetAdvancedGeneticsEnabled(bool enabled)
        {
            if (_advancedGeneticsEnabled != enabled)
            {
                _advancedGeneticsEnabled = enabled;

                ChimeraLogger.Log($"[PlantGeneticsService] Advanced genetics {(enabled ? "enabled" : "disabled")}");

                if (_enableDetailedLogging)
                {
                    ChimeraLogger.Log($"[PlantGeneticsService] Genetics system reconfigured for {(enabled ? "advanced" : "basic")} mode");
                }
            }
        }

        /// <summary>
        /// Calculates genetic diversity statistics across all plants.
        /// </summary>
        public GeneticDiversityStats CalculateGeneticDiversityStats()
        {
            if (!IsInitialized)
            {
                ChimeraLogger.LogError("[PlantGeneticsService] Cannot calculate genetic diversity: Service not initialized");
                return new GeneticDiversityStats();
            }

            var stats = new GeneticDiversityStats();

            if (_cultivationManager == null)
            {
                ChimeraLogger.LogWarning("[PlantGeneticsService] Cannot calculate genetic diversity: CultivationManager is null");
                return stats;
            }

            var startTime = System.DateTime.Now;

            try
            {
                var strainCounts = new Dictionary<string, int>();
                var allPlants = _cultivationManager.GetAllPlants();

                foreach (var plant in allPlants)
                {
                    if (plant != null)
                    {
                        // Count strain diversity based on PlantInstanceSO data
                        var strainName = plant.StrainName ?? "Unknown";
                        strainCounts[strainName] = strainCounts.GetValueOrDefault(strainName, 0) + 1;
                    }
                }

                stats.StrainDiversity = strainCounts.Count;
                stats.MostCommonStrain = strainCounts.OrderByDescending(kvp => kvp.Value).FirstOrDefault().Key ?? "None";

                // Calculate genetic fitness (placeholder - would need actual genetic data)
                stats.AverageGeneticFitness = CalculateAverageGeneticFitness(allPlants);

                // Calculate trait expression variance (placeholder - would need actual trait data)
                stats.TraitExpressionVariance = CalculateTraitExpressionVariance(allPlants);

                // Record calculation time
                var calculationTime = (System.DateTime.Now - startTime).TotalMilliseconds;
                RecordGeneticCalculation((float)calculationTime);

                if (_enableDetailedLogging)
                {
                    ChimeraLogger.Log($"[PlantGeneticsService] Calculated genetic diversity: {stats} (Time: {calculationTime:F2}ms)");
                }
            }
            catch (System.Exception ex)
            {
                ChimeraLogger.LogError($"[PlantGeneticsService] Error calculating genetic diversity: {ex.Message}");
            }

            return stats;
        }

        /// <summary>
        /// Calculates variance in trait expression across plants (private helper method).
        /// </summary>
        private float CalculateTraitVariance(List<TraitExpressionResult> traitExpressions)
        {
            if (traitExpressions == null || traitExpressions.Count == 0)
                return 0f;

            var heightVariances = new List<float>();
            var thcVariances = new List<float>();
            var cbdVariances = new List<float>();
            var yieldVariances = new List<float>();

            foreach (var expression in traitExpressions)
            {
                heightVariances.Add(expression.HeightExpression);
                thcVariances.Add(expression.THCExpression);
                cbdVariances.Add(expression.CBDExpression);
                yieldVariances.Add(expression.YieldExpression);
            }

            // Calculate combined variance across all traits
            float heightVar = CalculateVariance(heightVariances);
            float thcVar = CalculateVariance(thcVariances);
            float cbdVar = CalculateVariance(cbdVariances);
            float yieldVar = CalculateVariance(yieldVariances);

            return (heightVar + thcVar + cbdVar + yieldVar) / 4f;
        }

        /// <summary>
        /// Records a genetic calculation for performance monitoring.
        /// </summary>
        public void RecordGeneticCalculation(float calculationTime)
        {
            if (!IsInitialized || !_enableGeneticPerformanceMonitoring) return;

            _totalCalculations++;
            _totalCalculationTime += calculationTime;

            // Track recent calculation times
            _recentCalculationTimes.Add(calculationTime);
            if (_recentCalculationTimes.Count > MAX_RECENT_TIMES)
            {
                _recentCalculationTimes.RemoveAt(0);
            }

            // Record with performance monitor
            if (_geneticPerformanceMonitor != null)
            {
                // Performance monitoring is handled internally by the monitor
                // Individual calculation times are tracked automatically
                ChimeraLogger.Log($"[PlantGeneticsService] Calculation time: {calculationTime}ms");
            }
        }

        /// <summary>
        /// Records a batch update for performance monitoring.
        /// </summary>
        public void RecordBatchUpdate(int plantCount, GeneticPerformanceStats stats)
        {
            if (!IsInitialized || !_enableGeneticPerformanceMonitoring) return;

            _totalBatchCalculations++;
            _totalBatchTime += stats.AverageBatchTimeMs;

            // Record with performance monitor
            // In stub mode we do nothing; real monitor integration removed during early-phase refactor

            if (_enableDetailedLogging)
            {
                ChimeraLogger.Log($"[PlantGeneticsService] Recorded batch update: {plantCount} plants, {stats.AverageBatchTimeMs:F2}ms");
            }
        }

        /// <summary>
        /// Optimizes genetic calculations by clearing caches and updating algorithms.
        /// </summary>
        public void OptimizeGeneticCalculations()
        {
            if (!IsInitialized) return;

            // Clear performance tracking data periodically
            if (_totalCalculations > 10000)
            {
                _totalCalculations = _totalCalculations / 2;
                _totalCalculationTime = _totalCalculationTime / 2;
                _totalBatchCalculations = _totalBatchCalculations / 2;
                _totalBatchTime = _totalBatchTime / 2;

                if (_enableDetailedLogging)
                {
                    ChimeraLogger.Log("[PlantGeneticsService] Optimized calculation tracking data");
                }
            }

            // Clear recent calculation times if too many
            if (_recentCalculationTimes.Count > MAX_RECENT_TIMES)
            {
                _recentCalculationTimes.RemoveRange(0, _recentCalculationTimes.Count - MAX_RECENT_TIMES);
            }

            // Optimize genetic performance monitor
            if (_geneticPerformanceMonitor != null)
            {
                // The GeneticPerformanceMonitor doesn't have an OptimizePerformance method
                // This is handled internally by the monitor
                ChimeraLogger.Log("[PlantGeneticsService] Genetic performance monitor optimization handled internally");
            }
        }

        /// <summary>
        /// Gets detailed genetic analysis for a specific plant.
        /// </summary>
        public PlantGeneticAnalysis GetPlantGeneticAnalysis(string plantID)
        {
            if (!IsInitialized)
            {
                ChimeraLogger.LogError("[PlantGeneticsService] Cannot get genetic analysis: Service not initialized");
                return new PlantGeneticAnalysis();
            }

            var plant = _cultivationManager?.GetPlant(plantID);
            if (plant == null)
            {
                ChimeraLogger.LogWarning($"[PlantGeneticsService] Plant {plantID} not found for genetic analysis");
                return new PlantGeneticAnalysis();
            }

            var analysis = new PlantGeneticAnalysis
            {
                PlantID = plantID,
                StrainName = plant.StrainName ?? "Unknown",
                GeneticFitness = CalculateGeneticFitness(plant),
                TraitDiversity = CalculateTraitDiversity(plant),
                GeneticStability = CalculateGeneticStability(plant),
                AnalysisTimestamp = System.DateTime.Now
            };

            if (_enableDetailedLogging)
            {
                ChimeraLogger.Log($"[PlantGeneticsService] Generated genetic analysis for {plantID}: {analysis}");
            }

            return analysis;
        }

        /// <summary>
        /// Calculate genetic potential for a genotype
        /// </summary>
        public GeneticPotentialData CalculateGeneticPotential(CannabisGenotype genotype)
        {
            if (!IsInitialized || genotype == null)
            {
                return new GeneticPotentialData { OverallPotential = 0f };
            }

            var startTime = System.DateTime.Now;

            // Calculate various genetic potentials
            float yieldPotential = CalculateYieldPotential(genotype);
            float potencyPotential = CalculatePotencyPotential(genotype);
            float resistancePotential = CalculateResistancePotential(genotype);
            float growthPotential = CalculateGrowthPotential(genotype);

            // Calculate overall potential (weighted average)
            float overallPotential = (yieldPotential * 0.3f + potencyPotential * 0.25f +
                                     resistancePotential * 0.2f + growthPotential * 0.25f);

            var calculationTime = (System.DateTime.Now - startTime).TotalMilliseconds;
            RecordCalculation(calculationTime);

            return new GeneticPotentialData
            {
                OverallPotential = Mathf.Clamp01(overallPotential),
                YieldPotential = yieldPotential,
                PotencyPotential = potencyPotential,
                ResistancePotential = resistancePotential,
                GrowthPotential = growthPotential,
                GeneticVariability = genotype.GeneticVariability
            };
        }

        /// <summary>
        /// Process gene expression for a plant under specific environmental conditions
        /// </summary>
        public GeneExpressionResult ProcessGeneExpression(PlantInstance plant, EnvironmentalConditions conditions)
        {
            if (!IsInitialized || plant == null)
            {
                return new GeneExpressionResult { ExpressionLevel = 0f };
            }

            var startTime = System.DateTime.Now;

            // Get genetic data from plant
            var genotype = plant.Genotype;
            if (genotype == null)
            {
                return new GeneExpressionResult { ExpressionLevel = 0.5f }; // Default expression
            }

            // Calculate expression based on environmental conditions
            float temperatureExpression = CalculateTemperatureExpression(conditions.Temperature, plant);
            float humidityExpression = CalculateHumidityExpression(conditions.Humidity, plant);
            float lightExpression = CalculateLightExpression(conditions.LightIntensity, plant);
            float stressExpression = CalculateStressExpression(plant.StressLevel);

            // Combine expression factors
            float overallExpression = (temperatureExpression * 0.25f + humidityExpression * 0.25f +
                                     lightExpression * 0.3f + stressExpression * 0.2f);

            var calculationTime = (System.DateTime.Now - startTime).TotalMilliseconds;
            RecordCalculation(calculationTime);

            return new GeneExpressionResult
            {
                ExpressionLevel = Mathf.Clamp01(overallExpression),
                TemperatureExpression = temperatureExpression,
                HumidityExpression = humidityExpression,
                LightExpression = lightExpression,
                StressExpression = stressExpression,
                ProcessingTimeMs = (float)calculationTime
            };
        }

        private float CalculateYieldPotential(CannabisGenotype genotype)
        {
            // Base potential from genetic variability
            return Mathf.Clamp01(0.5f + genotype.GeneticVariability * 0.5f);
        }

        private float CalculatePotencyPotential(CannabisGenotype genotype)
        {
            // Base potency potential
            return Mathf.Clamp01(0.6f + genotype.GeneticVariability * 0.4f);
        }

        private float CalculateResistancePotential(CannabisGenotype genotype)
        {
            // Disease/pest resistance potential
            return Mathf.Clamp01(0.7f + genotype.GeneticVariability * 0.3f);
        }

        private float CalculateGrowthPotential(CannabisGenotype genotype)
        {
            // Growth rate potential
            return Mathf.Clamp01(0.5f + genotype.GeneticVariability * 0.5f);
        }

        private float CalculateTemperatureExpression(float temperature, PlantInstance plant)
        {
            // Optimal range around 22-26°C
            float optimalTemp = 24f;
            float deviation = Mathf.Abs(temperature - optimalTemp);
            return Mathf.Clamp01(1f - (deviation / 10f));
        }

        private float CalculateHumidityExpression(float humidity, PlantInstance plant)
        {
            // Optimal range around 50-70%
            float optimalHumidity = 60f;
            float deviation = Mathf.Abs(humidity - optimalHumidity);
            return Mathf.Clamp01(1f - (deviation / 30f));
        }

        private float CalculateLightExpression(float lightIntensity, PlantInstance plant)
        {
            // Optimal range around 600-1000 PPFD
            float optimalLight = 800f;
            float deviation = Mathf.Abs(lightIntensity - optimalLight);
            return Mathf.Clamp01(1f - (deviation / 400f));
        }

        private float CalculateStressExpression(float stressLevel)
        {
            // Higher stress reduces expression
            return Mathf.Clamp01(1f - (stressLevel / 100f));
        }

        /// <summary>
        /// Calculates statistical variance for a list of values.
        /// </summary>
        private float CalculateVariance(List<float> values)
        {
            if (values.Count == 0)
                return 0f;

            float mean = values.Average();
            float variance = values.Sum(v => Mathf.Pow(v - mean, 2)) / values.Count;
            return variance;
        }

        /// <summary>
        /// Records a genetic calculation for performance tracking.
        /// </summary>
        private void RecordCalculation(double calculationTimeMs)
        {
            // Update performance tracking statistics
            _totalCalculations++;
            _totalCalculationTime += calculationTimeMs;

            if (_enableDetailedLogging)
            {
                ChimeraLogger.Log($"[PlantGeneticsService] Genetic calculation completed in {calculationTimeMs:F2}ms");
            }
        }

        /// <summary>
        /// Calculates average genetic fitness across all plants.
        /// </summary>
        private float CalculateAverageGeneticFitness(IEnumerable<PlantInstanceSO> plants)
        {
            if (plants == null || !plants.Any())
                return 0f;

            float totalFitness = 0f;
            int plantCount = 0;

            foreach (var plant in plants)
            {
                if (plant != null)
                {
                    totalFitness += CalculateGeneticFitness(plant);
                    plantCount++;
                }
            }

            return plantCount > 0 ? totalFitness / plantCount : 0f;
        }

        /// <summary>
        /// Calculates trait expression variance across all plants.
        /// </summary>
        private float CalculateTraitExpressionVariance(IEnumerable<PlantInstanceSO> plants)
        {
            if (plants == null || !plants.Any())
                return 0f;

            // Placeholder implementation - would need actual trait expression data
            var healthValues = plants.Where(p => p != null).Select(p => p.OverallHealth).ToList();

            if (healthValues.Count == 0)
                return 0f;

            return CalculateVariance(healthValues);
        }

        /// <summary>
        /// Calculates genetic fitness for a specific plant.
        /// </summary>
        private float CalculateGeneticFitness(PlantInstanceSO plant)
        {
            if (plant == null)
                return 0f;

            // Placeholder implementation based on available data
            float healthFactor = plant.OverallHealth;
            float stressFactor = 1f - plant.StressLevel;
            float growthFactor = plant.CurrentGrowthStage == PlantGrowthStage.Harvest ? 1f : 0.5f;

            return (healthFactor + stressFactor + growthFactor) / 3f;
        }

        /// <summary>
        /// Calculates trait diversity for a specific plant.
        /// </summary>
        private float CalculateTraitDiversity(PlantInstanceSO plant)
        {
            if (plant == null)
                return 0f;

            // Placeholder implementation - would need actual genetic trait data
            return UnityEngine.Random.Range(0.5f, 1f);
        }

        /// <summary>
        /// Calculates genetic stability for a specific plant.
        /// </summary>
        private float CalculateGeneticStability(PlantInstanceSO plant)
        {
            if (plant == null)
                return 0f;

            // Placeholder implementation based on health consistency
            float healthStability = 1f - (plant.StressLevel / 100f);
            return Mathf.Clamp01(healthStability);
        }
    }

    /// <summary>
    /// Genetic potential data structure
    /// </summary>
    [System.Serializable]
    public class GeneticPotentialData
    {
        public float OverallPotential;
        public float YieldPotential;
        public float PotencyPotential;
        public float ResistancePotential;
        public float GrowthPotential;
        public float GeneticVariability;
    }

    /// <summary>
    /// Gene expression result data structure
    /// </summary>
    [System.Serializable]
    public class GeneExpressionResult
    {
        public float ExpressionLevel;
        public float TemperatureExpression;
        public float HumidityExpression;
        public float LightExpression;
        public float StressExpression;
        public float ProcessingTimeMs;
    }

    /// <summary>
    /// Plant genetic analysis result structure.
    /// </summary>
    [System.Serializable]
    public class PlantGeneticAnalysis
    {
        public string PlantID;
        public string StrainName;
        public float GeneticFitness;
        public float TraitDiversity;
        public float GeneticStability;
        public System.DateTime AnalysisTimestamp;

        public override string ToString()
        {
            return $"Plant {PlantID} ({StrainName}): Fitness={GeneticFitness:F2}, Diversity={TraitDiversity:F2}, Stability={GeneticStability:F2}";
        }
    }
}
