using UnityEngine;
using ProjectChimera.Data.Cultivation;
using ProjectChimera.Data.Genetics;
// using ProjectChimera.Systems.Genetics; // Invalid namespace - genetics in ProjectChimera.Data.Genetics
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Events;
using PlantGrowthStage = ProjectChimera.Data.Shared.PlantGrowthStage;

namespace ProjectChimera.Systems.Cultivation
{
    /// <summary>
    /// PC013-4a: Plant Growth Service - Handles growth calculations and stage transitions
    /// Extracted from monolithic PlantManager for Single Responsibility Principle
    /// Focuses solely on plant growth, stage progression, and growth-related calculations
    /// </summary>
    public class PlantGrowthService : IPlantGrowthService
    {
        [Header("Growth Configuration")]
        [SerializeField] private float _baseGrowthRate = 1.0f;
        [SerializeField] private float _globalGrowthModifier = 1.0f;
        [SerializeField] private bool _enableGeneticGrowthFactors = true;
        [SerializeField] private bool _enableEnvironmentalGrowthFactors = true;
        [SerializeField] private bool _enableDetailedLogging = false;
        
        [Header("Growth Timing")]
        [SerializeField] private float _growthUpdateInterval = 1.0f; // Update every second
        [SerializeField] private float _stageProgressionThreshold = 0.1f; // Minimum progress needed for stage change
        
        // Dependencies
        private IPlantEnvironmentalService _environmentalService;
        private object _traitExpressionEngine; // stubbed to remove dependency
        private PlantGrowthCalculator _growthCalculator;
        
        // Growth tracking
        private float _lastGrowthUpdate = 0f;
        private Dictionary<string, float> _plantGrowthProgress = new Dictionary<string, float>();
        private Dictionary<string, PlantGrowthStage> _previousStages = new Dictionary<string, PlantGrowthStage>();
        private int _growthCalculationsPerformed = 0;
        private float _totalGrowthTimeProcessed = 0f;
        
        public bool IsInitialized { get; private set; }
        
        public float GlobalGrowthModifier
        {
            get => _globalGrowthModifier;
            set => _globalGrowthModifier = Mathf.Max(0f, value);
        }
        
        public float BaseGrowthRate
        {
            get => _baseGrowthRate;
            set => _baseGrowthRate = Mathf.Max(0f, value);
        }
        
        public PlantGrowthService(IPlantEnvironmentalService environmentalService = null)
        {
            _environmentalService = environmentalService;
        }
        
        public void Initialize()
        {
            if (IsInitialized)
            {
                Debug.LogWarning("[PlantGrowthService] Already initialized");
                return;
            }
            
            // Initialize growth calculator
            _growthCalculator = new PlantGrowthCalculator();
            
            // Initialize trait expression engine if genetics are enabled
            if (_enableGeneticGrowthFactors) { _traitExpressionEngine = new object(); }
            
            IsInitialized = true;
            
            if (_enableDetailedLogging)
            {
                Debug.Log("[PlantGrowthService] Initialized successfully");
            }
        }
        
        public void Shutdown()
        {
            if (!IsInitialized) return;
            
            _plantGrowthProgress.Clear();
            _previousStages.Clear();
            _traitExpressionEngine = null;
            _growthCalculator = null;
            
            IsInitialized = false;
            
            if (_enableDetailedLogging)
            {
                Debug.Log("[PlantGrowthService] Shutdown completed");
            }
        }
        
        /// <summary>
        /// Updates growth for a single plant instance.
        /// </summary>
        public void UpdatePlantGrowth(PlantInstance plant, float deltaTime)
        {
            if (!IsInitialized || plant == null || !plant.IsActive)
                return;
            
            var startTime = Time.realtimeSinceStartup;
            
            // Calculate growth rate based on multiple factors
            float growthRate = CalculateGrowthRate(plant, deltaTime);
            
            // Apply growth to the plant
            ApplyGrowthToPlant(plant, growthRate, deltaTime);
            
            // Check for stage progression
            CheckAndProgressStage(plant);
            
            // Update tracking data
            UpdateGrowthTracking(plant, deltaTime);
            
            // Performance tracking
            _growthCalculationsPerformed++;
            _totalGrowthTimeProcessed += Time.realtimeSinceStartup - startTime;
            
            if (_enableDetailedLogging)
            {
                Debug.Log($"[PlantGrowthService] Updated growth for plant {plant.PlantID}: Rate={growthRate:F4}, Stage={plant.CurrentGrowthStage}");
            }
        }
        
        /// <summary>
        /// Updates growth for multiple plants in a batch for performance optimization.
        /// </summary>
        public void UpdatePlantGrowthBatch(List<PlantInstance> plants, float deltaTime)
        {
            if (!IsInitialized || plants == null || plants.Count == 0)
                return;
            
            var startTime = Time.realtimeSinceStartup;
            
            // Batch process growth calculations for better performance
            foreach (var plant in plants.Where(p => p != null && p.IsActive))
            {
                UpdatePlantGrowth(plant, deltaTime);
            }
            
            if (_enableDetailedLogging)
            {
                var processingTime = Time.realtimeSinceStartup - startTime;
                Debug.Log($"[PlantGrowthService] Batch updated {plants.Count} plants in {processingTime:F4}s");
            }
        }
        
        /// <summary>
        /// Calculates the growth rate for a plant based on genetics, environment, and health.
        /// </summary>
        public float CalculateGrowthRate(PlantInstance plant, float deltaTime)
        {
            if (plant == null) return 0f;
            
            // Start with base growth rate
            float growthRate = _baseGrowthRate;
            
            // Apply global growth modifier
            growthRate *= _globalGrowthModifier;
            
            // Apply stage-specific growth rate
            growthRate *= GetStageGrowthMultiplier(plant.CurrentGrowthStage);
            
            // Apply health-based modifier
            growthRate *= CalculateHealthGrowthModifier(plant);
            
            // Apply environmental factors if enabled
            if (_enableEnvironmentalGrowthFactors && _environmentalService != null)
            {
                growthRate *= CalculateEnvironmentalGrowthModifier(plant);
            }
            
            // Apply genetic factors if enabled
            if (_enableGeneticGrowthFactors)
            {
                growthRate *= CalculateGeneticGrowthModifier(plant);
            }
            
            return Mathf.Max(0f, growthRate);
        }
        
        /// <summary>
        /// Forces a plant to advance to the next growth stage.
        /// </summary>
        public bool ForceStageProgression(PlantInstance plant)
        {
            if (!IsInitialized || plant == null) return false;
            
            var currentStage = plant.CurrentGrowthStage;
            var nextStage = GetNextGrowthStage(currentStage);
            
            if (nextStage != currentStage)
            {
                SetPlantGrowthStage(plant, nextStage);
                
                if (_enableDetailedLogging)
                {
                    Debug.Log($"[PlantGrowthService] Forced stage progression for plant {plant.PlantID}: {currentStage} -> {nextStage}");
                }
                
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Gets the optimal growth conditions for a specific growth stage.
        /// </summary>
        public GrowthStageRequirements GetStageRequirements(PlantGrowthStage stage)
        {
            return stage switch
            {
                PlantGrowthStage.Seed => new GrowthStageRequirements
                {
                    OptimalTemperature = 22f,
                    OptimalHumidity = 65f,
                    OptimalLightHours = 0f,
                    MinimumDuration = 1f,
                    NutrientNeeds = NutrientLevel.Low
                },
                PlantGrowthStage.Germination => new GrowthStageRequirements
                {
                    OptimalTemperature = 24f,
                    OptimalHumidity = 70f,
                    OptimalLightHours = 18f,
                    MinimumDuration = 3f,
                    NutrientNeeds = NutrientLevel.Low
                },
                PlantGrowthStage.Seedling => new GrowthStageRequirements
                {
                    OptimalTemperature = 23f,
                    OptimalHumidity = 65f,
                    OptimalLightHours = 18f,
                    MinimumDuration = 7f,
                    NutrientNeeds = NutrientLevel.Medium
                },
                PlantGrowthStage.Vegetative => new GrowthStageRequirements
                {
                    OptimalTemperature = 25f,
                    OptimalHumidity = 60f,
                    OptimalLightHours = 18f,
                    MinimumDuration = 21f,
                    NutrientNeeds = NutrientLevel.High
                },
                PlantGrowthStage.PreFlowering => new GrowthStageRequirements
                {
                    OptimalTemperature = 24f,
                    OptimalHumidity = 55f,
                    OptimalLightHours = 12f,
                    MinimumDuration = 7f,
                    NutrientNeeds = NutrientLevel.High
                },
                PlantGrowthStage.Flowering => new GrowthStageRequirements
                {
                    OptimalTemperature = 22f,
                    OptimalHumidity = 50f,
                    OptimalLightHours = 12f,
                    MinimumDuration = 49f,
                    NutrientNeeds = NutrientLevel.Medium
                },
                PlantGrowthStage.Ripening => new GrowthStageRequirements
                {
                    OptimalTemperature = 20f,
                    OptimalHumidity = 45f,
                    OptimalLightHours = 12f,
                    MinimumDuration = 7f,
                    NutrientNeeds = NutrientLevel.Low
                },
                _ => new GrowthStageRequirements
                {
                    OptimalTemperature = 22f,
                    OptimalHumidity = 55f,
                    OptimalLightHours = 12f,
                    MinimumDuration = 1f,
                    NutrientNeeds = NutrientLevel.Medium
                }
            };
        }
        
        /// <summary>
        /// Gets comprehensive growth statistics for monitoring and optimization.
        /// </summary>
        public GrowthServiceStatistics GetGrowthStatistics()
        {
            return new GrowthServiceStatistics
            {
                TotalGrowthCalculations = _growthCalculationsPerformed,
                AverageCalculationTime = _growthCalculationsPerformed > 0 ? (_totalGrowthTimeProcessed / _growthCalculationsPerformed) * 1000f : 0f,
                TrackedPlants = _plantGrowthProgress.Count,
                GlobalGrowthModifier = _globalGrowthModifier,
                BaseGrowthRate = _baseGrowthRate,
                GeneticFactorsEnabled = _enableGeneticGrowthFactors,
                EnvironmentalFactorsEnabled = _enableEnvironmentalGrowthFactors
            };
        }
        
        #region Private Helper Methods
        
        private void ApplyGrowthToPlant(PlantInstance plant, float growthRate, float deltaTime)
        {
            // Apply the calculated growth rate to the plant using its existing update method
            plant.UpdatePlant(deltaTime, growthRate);
            
            // Update our tracking
            var plantId = plant.PlantID;
            if (!_plantGrowthProgress.ContainsKey(plantId))
            {
                _plantGrowthProgress[plantId] = 0f;
            }
            _plantGrowthProgress[plantId] += growthRate * deltaTime;
        }
        
        private void CheckAndProgressStage(PlantInstance plant)
        {
            var currentStage = plant.CurrentGrowthStage;
            var stageRequirements = GetStageRequirements(currentStage);
            
            // Check if plant has met the minimum duration and growth requirements for stage progression
            // Note: Using simplified time check - would need to track stage duration internally
            if (true) // Placeholder - would implement proper stage duration tracking
            {
                var plantId = plant.PlantID;
                var growthProgress = _plantGrowthProgress.GetValueOrDefault(plantId, 0f);
                
                if (growthProgress >= _stageProgressionThreshold)
                {
                    var nextStage = GetNextGrowthStage(currentStage);
                    if (nextStage != currentStage)
                    {
                        // Use the existing AdvanceGrowthStage method
                        if (plant.AdvanceGrowthStage())
                        {
                            _plantGrowthProgress[plantId] = 0f; // Reset progress for new stage
                        }
                    }
                }
            }
        }
        
        private void SetPlantGrowthStage(PlantInstance plant, PlantGrowthStage newStage)
        {
            var oldStage = plant.CurrentGrowthStage;
            
            // Use the existing AdvanceGrowthStage method instead of direct setting
            if (plant.AdvanceGrowthStage())
            {
                // Update tracking
                _previousStages[plant.PlantID] = oldStage;
                
                if (_enableDetailedLogging)
                {
                    Debug.Log($"[PlantGrowthService] Plant {plant.PlantID} progressed from {oldStage} to {plant.CurrentGrowthStage}");
                }
            }
        }
        
        private void UpdateGrowthTracking(PlantInstance plant, float deltaTime)
        {
            // Update any additional tracking data as needed
            // This could include stage duration tracking, growth rate history, etc.
        }
        
        private float GetStageGrowthMultiplier(PlantGrowthStage stage)
        {
            return stage switch
            {
                PlantGrowthStage.Seed => 0.1f,
                PlantGrowthStage.Germination => 0.3f,
                PlantGrowthStage.Seedling => 0.5f,
                PlantGrowthStage.Vegetative => 1.0f, // Peak growth
                PlantGrowthStage.PreFlowering => 0.8f,
                PlantGrowthStage.Flowering => 0.6f,
                PlantGrowthStage.Ripening => 0.2f,
                PlantGrowthStage.Harvest => 0f,
                PlantGrowthStage.Harvestable => 0f,
                _ => 0.5f
            };
        }
        
        private float CalculateHealthGrowthModifier(PlantInstance plant)
        {
            // Healthy plants grow faster
            float health = plant.CurrentHealth;
            return Mathf.Lerp(0.1f, 1.2f, health / 100f);
        }
        
        private float CalculateEnvironmentalGrowthModifier(PlantInstance plant)
        {
            if (_environmentalService == null) return 1f;
            
            // Get environmental fitness from the environmental service
            // This would integrate with the existing environmental system
            return 1f; // Placeholder - would call _environmentalService.GetEnvironmentalFitness(plant)
        }
        
        private float CalculateGeneticGrowthModifier(PlantInstance plant)
        {
            if (_traitExpressionEngine == null || plant.Genotype == null) return 1f;
            
            // TODO: The TraitExpressionEngine expects PlantGenotype but PlantInstance.Genotype returns GenotypeDataSO
            // For now, we'll skip genetic calculations and return a default modifier
            // This needs to be resolved by either:
            // 1. Adding a conversion method from GenotypeDataSO to PlantGenotype
            // 2. Updating TraitExpressionEngine to accept GenotypeDataSO
            // 3. Using the strain data from plant.GeneticProfile instead
            
            // Use strain data as a fallback for genetic influence
            if (plant.GeneticProfile != null)
            {
                // Simple genetic modifier based on strain characteristics
                float strainGrowthModifier = plant.GeneticProfile.GrowthRateModifier;
                return Mathf.Clamp(strainGrowthModifier, 0.5f, 2.0f);
            }
            
            return 1f;
        }
        
        private PlantGrowthStage GetNextGrowthStage(PlantGrowthStage currentStage)
        {
            return currentStage switch
            {
                PlantGrowthStage.Seed => PlantGrowthStage.Germination,
                PlantGrowthStage.Germination => PlantGrowthStage.Seedling,
                PlantGrowthStage.Seedling => PlantGrowthStage.Vegetative,
                PlantGrowthStage.Vegetative => PlantGrowthStage.PreFlowering,
                PlantGrowthStage.PreFlowering => PlantGrowthStage.Flowering,
                PlantGrowthStage.Flowering => PlantGrowthStage.Ripening,
                PlantGrowthStage.Ripening => PlantGrowthStage.Harvest,
                PlantGrowthStage.Harvest => PlantGrowthStage.Harvestable,
                _ => currentStage // No progression available
            };
        }
        
        /// <summary>
        /// Process plant growth and return growth result data
        /// </summary>
        public PlantGrowthResult ProcessPlantGrowth(PlantInstance plant, float deltaTime)
        {
            if (!IsInitialized || plant == null)
            {
                return new PlantGrowthResult { GrowthRate = 0f, StageChanged = false };
            }
            
            var initialStage = plant.CurrentGrowthStage;
            var growthRate = CalculateGrowthRate(plant, deltaTime);
            
            // Apply growth
            ApplyGrowthToPlant(plant, growthRate, deltaTime);
            CheckAndProgressStage(plant);
            
            var stageChanged = plant.CurrentGrowthStage != initialStage;
            
            return new PlantGrowthResult
            {
                GrowthRate = growthRate,
                StageChanged = stageChanged,
                NewStage = plant.CurrentGrowthStage,
                ProgressPercent = _plantGrowthProgress.GetValueOrDefault(plant.PlantID, 0f)
            };
        }
        
        /// <summary>
        /// Calculate what the next stage transition should be
        /// </summary>
        public PlantGrowthStage CalculateStageTransition(PlantInstance plant, float deltaTime)
        {
            if (plant == null) return PlantGrowthStage.Seed;
            
            var currentStage = plant.CurrentGrowthStage;
            var stageRequirements = GetStageRequirements(currentStage);
            var plantId = plant.PlantID;
            var growthProgress = _plantGrowthProgress.GetValueOrDefault(plantId, 0f);
            
            // Check if ready for next stage
            if (growthProgress >= _stageProgressionThreshold)
            {
                return GetNextGrowthStage(currentStage);
            }
            
            return currentStage;
        }
        
        #endregion
    }
    
    /// <summary>
    /// Result data structure for plant growth processing
    /// </summary>
    [System.Serializable]
    public class PlantGrowthResult
    {
        public float GrowthRate;
        public bool StageChanged;
        public PlantGrowthStage NewStage;
        public float ProgressPercent;
    }
    
    /// <summary>
    /// Growth stage requirements data structure.
    /// </summary>
    [System.Serializable]
    public class GrowthStageRequirements
    {
        public float OptimalTemperature;
        public float OptimalHumidity;
        public float OptimalLightHours;
        public float MinimumDuration; // Days
        public NutrientLevel NutrientNeeds;
    }
    
    /// <summary>
    /// Nutrient level enumeration.
    /// </summary>
    public enum NutrientLevel
    {
        None,
        Low,
        Medium,
        High,
        Maximum
    }
    
    /// <summary>
    /// Growth service statistics structure.
    /// </summary>
    [System.Serializable]
    public class GrowthServiceStatistics
    {
        public int TotalGrowthCalculations;
        public float AverageCalculationTime; // milliseconds
        public int TrackedPlants;
        public float GlobalGrowthModifier;
        public float BaseGrowthRate;
        public bool GeneticFactorsEnabled;
        public bool EnvironmentalFactorsEnabled;
        
        public override string ToString()
        {
            return $"Growth Stats: {TotalGrowthCalculations} calcs, {AverageCalculationTime:F2}ms avg, {TrackedPlants} plants";
        }
    }
}