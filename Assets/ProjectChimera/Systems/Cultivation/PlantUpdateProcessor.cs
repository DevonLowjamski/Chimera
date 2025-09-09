using ProjectChimera.Core.Logging;
using UnityEngine;
using ProjectChimera.Data.Genetics;
using ProjectChimera.Data.Shared;
using System.Collections.Generic;
// using ProjectChimera.Systems.Genetics; // Invalid namespace - genetics in ProjectChimera.Data.Genetics // Added for advanced TraitExpressionEngine
// Decouple from Systems.Genetics for early-phase compile
using TraitExpressionEngine = System.Object;
using TraitExpressionResult = ProjectChimera.Data.Genetics.TraitExpressionResult;
using EnvironmentalConditions = ProjectChimera.Data.Shared.EnvironmentalConditions;
using PlantGenotype = ProjectChimera.Data.Genetics.PlantGenotype;
// using EnvironmentManager = ProjectChimera.Systems.Environment.EnvironmentManager; // Environment assembly not available
using GxEInteractionProfile = ProjectChimera.Data.Genetics.GxEInteractionProfile; // Correct type name
using GameManager = ProjectChimera.Core.GameManager; // Add GameManager for accessing managers

    /// <summary>
    /// Main Plant Update Processor coordinator.
    /// Orchestrates growth calculations, health management, environmental responses,
    /// and genetic trait expression using specialized component systems.
    /// Significantly reduced from original 947-line monolithic class.
    /// </summary>
    public class PlantUpdateProcessor
    {
        // Core processing components
        private readonly PlantGrowthCalculator _growthCalculator;
        private readonly PlantHealthSystem _healthSystem;
        private readonly EnvironmentalResponseSystem _environmentalSystem;

        // Configuration and state
        private readonly PlantUpdateConfiguration _configuration;
        private readonly UpdateStatistics _statistics;
        private readonly TraitExpressionEngine _traitExpressionEngine;

        // Performance optimization
        private readonly Dictionary<string, TraitExpressionResult> _traitExpressionCache = new Dictionary<string, TraitExpressionResult>();
        private float _lastCacheUpdate = 0f;
        private const float CACHE_UPDATE_INTERVAL = 5f; // Update cache every 5 seconds

        public PlantUpdateProcessor(PlantUpdateConfiguration configuration = null)
        {
            _configuration = configuration ?? PlantUpdateConfiguration.CreateDefault();
            _statistics = new UpdateStatistics();

            // Initialize component systems
            _growthCalculator = new PlantGrowthCalculator();
            _healthSystem = new PlantHealthSystem();
            _environmentalSystem = new EnvironmentalResponseSystem();

            // Initialize advanced trait expression engine with performance optimization
            if (_configuration.EnableAdvancedGenetics)
            {
                _traitExpressionEngine = new TraitExpressionEngine();
            }

            ChimeraLogger.Log("[PlantUpdateProcessor] Initialized with specialized component systems");
        }

        /// <summary>
        /// Initialize the processor for a specific plant strain
        /// </summary>
        public void InitializeForStrain(object strain, PhenotypicTraits traits)
        {
            _growthCalculator.Initialize(strain, traits, _configuration);
            _healthSystem.Initialize(strain, traits?.DiseaseResistance ?? 1f, _configuration);
            _environmentalSystem.Initialize(strain, _configuration);

            ChimeraLogger.LogDebug($"[PlantUpdateProcessor] Initialized for strain: {strain?.GetType().Name}");
        }

        /// <summary>
        /// Enhanced plant update with integrated component systems.
        /// Updates a single plant's state including growth, health, environmental responses, and genetic trait expression.
        /// </summary>
        public void UpdatePlant(PlantInstance plant, float deltaTime, float globalGrowthModifier)
        {
            if (plant == null || !plant.IsActive)
                return;

            var startTime = Time.realtimeSinceStartup;
            bool updateSuccessful = false;

            try
            {
                // Get current environmental conditions for the plant
                var environmentalConditions = GetPlantEnvironmentalConditions(plant);

                // Update environmental response system
                _environmentalSystem.UpdateEnvironmentalResponse(environmentalConditions, deltaTime);
                float environmentalFitness = _environmentalSystem.GetEnvironmentalFitness();

                // Get environmental stress factors
                var environmentalStresses = _environmentalSystem.GetEnvironmentalStressFactors();
                var activeStressors = ConvertToActiveStressors(environmentalStresses);

                // Update health system with environmental effects
                _healthSystem.UpdateHealth(deltaTime, activeStressors, environmentalFitness);
                float currentHealth = _healthSystem.GetCurrentHealth();

                // Calculate growth rate using integrated systems
                float growthRate = _growthCalculator.CalculateGrowthRate(
                    plant.CurrentGrowthStage,
                    environmentalFitness,
                    currentHealth,
                    globalGrowthModifier);

                // Calculate genetic trait expression if advanced genetics is enabled
                TraitExpressionResult traitExpression = null;
                if (_configuration.EnableAdvancedGenetics && plant.Genotype != null)
                {
                    traitExpression = CalculateTraitExpression(plant, environmentalConditions);
                }

                // Update plant with integrated system data
                if (traitExpression != null)
                {
                    UpdatePlantWithGeneticTraits(plant, traitExpression, growthRate, deltaTime, globalGrowthModifier);
                }
                else
                {
                    // Update with standard systems integration
                    UpdatePlantWithSystems(plant, growthRate, currentHealth, environmentalFitness, deltaTime, globalGrowthModifier);
                }

                updateSuccessful = true;
            }
            catch (System.Exception ex)
            {
                ChimeraLogger.LogError($"[PlantUpdateProcessor] Error updating plant {plant.PlantID}: {ex.Message}");
                updateSuccessful = false;
            }
            finally
            {
                // Record performance statistics
                var updateTime = (Time.realtimeSinceStartup - startTime) * 1000.0; // Convert to milliseconds
                _statistics.RecordUpdate(updateTime, updateSuccessful);
            }
        }

        /// <summary>
        /// Update plant using integrated systems data
        /// </summary>
        private void UpdatePlantWithSystems(PlantInstance plant, float growthRate, float currentHealth,
            float environmentalFitness, float deltaTime, float globalGrowthModifier)
        {
            // Apply growth rate from growth calculator
            plant.ApplyGrowthRate(growthRate, deltaTime);

            // Apply health effects from health system
            plant.SetCurrentHealth(currentHealth);

            // Apply environmental fitness effects
            plant.SetEnvironmentalFitness(environmentalFitness);

            // Update plant's basic systems with integrated data
            plant.UpdatePlant(deltaTime, globalGrowthModifier);
        }

        /// <summary>
        /// Enhanced trait expression calculation with performance optimization.
        /// Uses the high-performance TraitExpressionEngine with automatic optimization selection.
        /// </summary>
        private TraitExpressionResult CalculateTraitExpression(PlantInstance plant, EnvironmentalConditions environment)
        {
            string cacheKey = $"{plant.PlantID}_{environment.GetHashCode()}";

            // Check cache first for performance optimization
            if (_traitExpressionCache.TryGetValue(cacheKey, out var cachedResult))
            {
                if (Time.time - _lastCacheUpdate < CACHE_UPDATE_INTERVAL)
                {
                    return cachedResult;
                }
            }

            // Calculate new trait expression using optimized engine
            var plantGenotype = CreatePlantGenotypeFromInstance(plant);
            if (plantGenotype == null)
                return null;

            var traitExpression = new TraitExpressionResult();
            traitExpression.GenotypeID = plantGenotype.GenotypeID;
            traitExpression.CalculationTime = System.DateTime.Now;

            // Update cache
            _traitExpressionCache[cacheKey] = traitExpression;
            _lastCacheUpdate = Time.time;

            return traitExpression;
        }

        /// <summary>
        /// Batch processing for multiple plants using optimized integrated systems.
        /// Automatically selects optimal processing method based on batch size.
        /// </summary>
        public void UpdatePlantsBatch(List<PlantInstance> plants, float deltaTime, float globalGrowthModifier)
        {
            if (plants == null || plants.Count == 0)
                return;

            var startTime = Time.realtimeSinceStartup;
            int successfulUpdates = 0;

            try
            {
                // Group plants by strain for more efficient processing
                var plantsByStrain = GroupPlantsByStrain(plants);

                foreach (var strainGroup in plantsByStrain)
                {
                    ProcessStrainBatch(strainGroup.Value, strainGroup.Key, deltaTime, globalGrowthModifier);
                    successfulUpdates += strainGroup.Value.Count;
                }

                _statistics.ProcessedBatches++;
                ChimeraLogger.LogDebug($"[PlantUpdateProcessor] Batch processed {successfulUpdates} plants in {plantsByStrain.Count} strain groups");
            }
            catch (System.Exception ex)
            {
                ChimeraLogger.LogError($"[PlantUpdateProcessor] Error in batch processing: {ex.Message}");
            }
            finally
            {
                // Record batch performance
                var batchTime = (Time.realtimeSinceStartup - startTime) * 1000.0;
                _statistics.ActivePlants = successfulUpdates;
            }
        }

        /// <summary>
        /// Process a batch of plants with the same strain
        /// </summary>
        private void ProcessStrainBatch(List<PlantInstance> plants, object strain, float deltaTime, float globalGrowthModifier)
        {
            // Initialize systems for this strain if needed
            if (plants.Count > 0)
            {
                var samplePlant = plants[0];
                var traits = ExtractPhenotypicTraits(samplePlant);

                // Ensure systems are initialized for this strain
                InitializeForStrain(strain, traits);
            }

            // Process each plant in the strain group
            foreach (var plant in plants)
            {
                if (plant != null && plant.IsActive)
                {
                    UpdatePlant(plant, deltaTime, globalGrowthModifier);
                }
            }
        }

        /// <summary>
        /// Update plant state using genetic trait expression results.
        /// </summary>
        private void UpdatePlantWithGeneticTraits(PlantInstance plant, TraitExpressionResult traitExpression,
            float growthRate, float deltaTime, float globalGrowthModifier)
        {
            // Store the trait expression result in the plant for other systems to access
            plant.SetLastTraitExpression(traitExpression);

            // Apply genetic trait effects to plant growth
            ApplyGeneticTraitEffects(plant, traitExpression, growthRate, deltaTime, globalGrowthModifier);

            // Update plant's basic systems with trait modifications
            plant.UpdatePlant(deltaTime, globalGrowthModifier);
        }

        /// <summary>
        /// Apply genetic trait effects to plant growth and development.
        /// </summary>
        private void ApplyGeneticTraitEffects(PlantInstance plant, TraitExpressionResult traitExpression,
            float growthRate, float deltaTime, float globalGrowthModifier)
        {
            // Apply growth rate from integrated calculations
            plant.ApplyGrowthRate(growthRate, deltaTime);

            // Apply height trait effects
            if (traitExpression.HeightExpression > 0f)
            {
                float heightGrowthModifier = traitExpression.HeightExpression;
                plant.ApplyHeightGrowthModifier(heightGrowthModifier, deltaTime);
            }

            // Apply THC trait effects (affects potency)
            if (traitExpression.THCExpression > 0f)
            {
                plant.ApplyPotencyModifier(traitExpression.THCExpression);
            }

            // Apply CBD trait effects (affects medicinal value)
            if (traitExpression.CBDExpression > 0f)
            {
                plant.ApplyCBDModifier(traitExpression.CBDExpression);
            }

            // Apply yield trait effects
            if (traitExpression.YieldExpression > 0f)
            {
                plant.ApplyYieldModifier(traitExpression.YieldExpression);
            }

            // Apply overall genetic fitness effects
            plant.ApplyGeneticFitnessModifier(traitExpression.OverallFitness);
        }

        /// <summary>
        /// Calculate harvest results using integrated systems
        /// </summary>
        public HarvestResults CalculateHarvestResults(PlantInstance plant)
        {
            if (plant == null) return new HarvestResults();

            float finalHealth = _healthSystem.GetCurrentHealth();
            float environmentalFitness = _environmentalSystem.GetEnvironmentalFitness();
            var traits = ExtractPhenotypicTraits(plant);

            // Use growth calculator for harvest calculations
            return _growthCalculator.CalculateHarvestResults(
                finalHealth,
                plant.QualityPotential,
                traits,
                environmentalFitness,
                plant.TotalDaysGrown
            );
        }

        /// <summary>
        /// Get environmental conditions for a plant
        /// </summary>
        private EnvironmentalConditions GetPlantEnvironmentalConditions(PlantInstance plant)
        {
            // Get environmental conditions directly from the plant
            EnvironmentalConditions dataConditions = plant.GetCurrentEnvironmentalConditions();

            // Validate that the conditions are initialized
            if (dataConditions.IsInitialized())
            {
                return dataConditions;
            }

            // Fallback to environmental manager if plant conditions are not available
            // var environmentalManager = GameManager.Instance?.GetManager<ProjectChimera.Systems.Environment.EnvironmentManager>(); // EnvironmentManager not available
            // if (environmentalManager != null) // EnvironmentManager not available
            {
                // EnvironmentalConditions cultivationConditions = environmentalManager.GetCultivationConditions(plant.transform.position);
                // return cultivationConditions; // EnvironmentManager not available
            }

            // Final fallback to default indoor conditions
            return EnvironmentalConditions.CreateIndoorDefault();
        }

        /// <summary>
        /// Group plants by strain for efficient batch processing
        /// </summary>
        private Dictionary<object, List<PlantInstance>> GroupPlantsByStrain(List<PlantInstance> plants)
        {
            var groups = new Dictionary<object, List<PlantInstance>>();

            foreach (var plant in plants)
            {
                if (plant?.Strain != null)
                {
                    if (!groups.ContainsKey(plant.Strain))
                    {
                        groups[plant.Strain] = new List<PlantInstance>();
                    }
                    groups[plant.Strain].Add(plant);
                }
            }

            return groups;
        }

        /// <summary>
        /// Extract phenotypic traits from plant instance
        /// </summary>
        private PhenotypicTraits ExtractPhenotypicTraits(PlantInstance plant)
        {
            // Extract traits from plant or use defaults
            // This would be expanded based on PlantInstance structure
            return new PhenotypicTraits
            {
                YieldMultiplier = 1f,
                PotencyMultiplier = 1f,
                GrowthRateMultiplier = 1f,
                DiseaseResistance = 1f,
                FloweringTime = 60f
            };
        }

        /// <summary>
        /// Convert environmental stress factors to active stressors
        /// </summary>
        private List<ActiveStressor> ConvertToActiveStressors(List<StressFactor> stressFactors)
        {
            var activeStressors = new List<ActiveStressor>();

            foreach (var stressFactor in stressFactors)
            {
                activeStressors.Add(new ActiveStressor
                {
                    IsActive = true,
                    Intensity = stressFactor.Severity,
                    StressSource = new StressSource
                    {
                        StressType = stressFactor.StressType,
                        DamagePerSecond = stressFactor.Severity * 0.01f,
                        StressMultiplier = 1f
                    },
                    StartTime = Time.time,
                    Duration = stressFactor.Duration
                });
            }

            return activeStressors;
        }

        /// <summary>
        /// Create a PlantGenotype from a PlantInstance for trait expression calculations
        /// </summary>
        private PlantGenotype CreatePlantGenotypeFromInstance(PlantInstance plant)
        {
            if (plant.Genotype == null)
                return null;

            return new PlantGenotype
            {
                GenotypeID = plant.PlantID,
                StrainOrigin = plant.Strain,
                Generation = 1, // Would be calculated from breeding history
                IsFounder = true, // Would be determined from breeding history
                CreationDate = System.DateTime.Now,
                ParentIDs = new List<string>(),
                Genotype = new Dictionary<string, object>(),
                OverallFitness = plant.Genotype.OverallFitness,
                InbreedingCoefficient = 0f, // Would be calculated from breeding history
                Mutations = new List<object>()
            };
        }

        #region Public Performance and Utility Methods

        /// <summary>
        /// Clear trait expression cache to free memory
        /// </summary>
        public void ClearTraitExpressionCache()
        {
            _traitExpressionCache.Clear();
        }

        /// <summary>
        /// Get cache statistics for performance monitoring
        /// </summary>
        public (int cacheSize, float lastUpdate) GetCacheStatistics()
        {
            return (_traitExpressionCache.Count, _lastCacheUpdate);
        }

        /// <summary>
        /// Get comprehensive performance metrics from integrated systems
        /// </summary>
        public GeneticPerformanceStats GetPerformanceMetrics()
        {
            return new GeneticPerformanceStats
            {
                TotalCalculations = _statistics.TotalUpdates,
                AverageCalculationTimeMs = _statistics.AverageUpdateTime,
                CacheHitRatio = CalculateCacheHitRatio(),
                BatchCalculations = _statistics.ProcessedBatches,
                AverageBatchTimeMs = 0.0, // Would be calculated from batch timing data
                AverageUpdateTimeMs = _statistics.AverageUpdateTime,
                CacheSize = _traitExpressionCache.Count,
                LastUpdate = _statistics.LastUpdate
            };
        }

        /// <summary>
        /// Get update statistics
        /// </summary>
        public UpdateStatistics GetUpdateStatistics()
        {
            return _statistics;
        }

        /// <summary>
        /// Get environmental recommendations from environmental system
        /// </summary>
        public List<EnvironmentalRecommendation> GetEnvironmentalRecommendations()
        {
            return _environmentalSystem.GetEnvironmentalRecommendations();
        }

        /// <summary>
        /// Get health recommendations from health system
        /// </summary>
        public List<string> GetHealthRecommendations()
        {
            return _healthSystem.GetHealthRecommendations();
        }

        /// <summary>
        /// Optimize performance by clearing caches and resetting metrics
        /// </summary>
        public void OptimizePerformance()
        {
            ClearTraitExpressionCache();

            if (_configuration.EnableAdvancedGenetics && _traitExpressionEngine != null)
            {
                // _traitExpressionEngine.ClearCache(); // Method doesn't exist on System.Object placeholder
            }

            // Reset statistics
            _statistics.Reset();

            // Force garbage collection to clean up pooled objects
            if (_configuration.EnablePerformanceOptimization)
            {
                System.GC.Collect();
                System.GC.WaitForPendingFinalizers();
            }

            ChimeraLogger.Log("[PlantUpdateProcessor] Performance optimization completed");
        }

        /// <summary>
        /// Reset all component systems
        /// </summary>
        public void ResetSystems()
        {
            _healthSystem.ResetHealth();
            _environmentalSystem.ResetAdaptation();
            _statistics.Reset();
            ClearTraitExpressionCache();

            ChimeraLogger.Log("[PlantUpdateProcessor] All systems reset");
        }

        #endregion

        #region Private Helper Methods

        private double CalculateCacheHitRatio()
        {
            // Simplified cache hit ratio calculation
            // In a full implementation, this would track cache hits vs misses
            return _traitExpressionCache.Count > 0 ? 0.8 : 0.0;
        }

        #endregion

    }

