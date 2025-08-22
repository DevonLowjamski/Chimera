using UnityEngine;
using ProjectChimera.Data.Cultivation;
using ProjectChimera.Data.Genetics;
// using ProjectChimera.Systems.Genetics; // Invalid namespace - genetics in ProjectChimera.Data.Genetics
using ProjectChimera.Core;
using System.Collections.Generic;
using System.Linq;
using EnvironmentalConditions = ProjectChimera.Data.Shared.EnvironmentalConditions;
using PlantGrowthStage = ProjectChimera.Data.Shared.PlantGrowthStage;

namespace ProjectChimera.Systems.Cultivation
{
    /// <summary>
    /// PC013-4c: Plant Yield Calculation Service - Handles harvest yield computations
    /// Extracted from monolithic PlantManager for Single Responsibility Principle
    /// Focuses solely on yield calculations, harvest processing, and quality assessments
    /// </summary>
    public class PlantYieldCalculationService : IPlantYieldCalculationService
    {
        [Header("Yield Calculation Configuration")]
        [SerializeField] private bool _enableYieldVariability = true;
        [SerializeField] private bool _enablePostHarvestProcessing = true;
        [SerializeField] private bool _enableGeneticYieldFactors = true;
        [SerializeField] private bool _enableEnvironmentalYieldFactors = true;
        [SerializeField] private bool _enableDetailedLogging = false;
        
        [Header("Harvest Quality Settings")]
        [SerializeField] private float _harvestQualityMultiplier = 1.0f;
        [SerializeField] private float _baseQualityThreshold = 0.5f;
        [SerializeField] private float _perfectQualityThreshold = 0.95f;
        [SerializeField] private float _highQualityThreshold = 0.85f;
        
        [Header("Yield Calculation Parameters")]
        [SerializeField] private float _defaultBaseYieldGrams = 50f;
        [SerializeField] private float _maxYieldMultiplier = 3.0f;
        [SerializeField] private float _minYieldMultiplier = 0.1f;
        [SerializeField] private float _yieldVariabilityRange = 0.2f;
        
        [Header("Performance Settings")]
        [SerializeField] private bool _enableYieldCaching = true;
        [SerializeField] private float _cacheRefreshInterval = 5.0f;
        
        // Dependencies
        private IPlantEnvironmentalProcessingService _environmentalService;
        private IPlantGeneticsService _geneticsService;
        // Optional dependency removed for early-phase compile stability
        private object _traitExpressionEngine;
        
        // Yield calculation tracking
        private Dictionary<string, float> _cachedYieldCalculations = new Dictionary<string, float>();
        private Dictionary<string, float> _lastYieldCalculationTime = new Dictionary<string, float>();
        private Dictionary<string, YieldCalculationData> _yieldCalculationHistory = new Dictionary<string, YieldCalculationData>();
        
        // Performance tracking
        private int _yieldCalculationsPerformed = 0;
        private float _totalYieldCalculationTime = 0f;
        private int _harvestsProcessed = 0;
        private float _totalHarvestProcessingTime = 0f;
        
        public bool IsInitialized { get; private set; }
        
        public bool EnableYieldVariability
        {
            get => _enableYieldVariability;
            set => _enableYieldVariability = value;
        }
        
        public bool EnablePostHarvestProcessing
        {
            get => _enablePostHarvestProcessing;
            set => _enablePostHarvestProcessing = value;
        }
        
        public float HarvestQualityMultiplier
        {
            get => _harvestQualityMultiplier;
            set => _harvestQualityMultiplier = Mathf.Max(0f, value);
        }
        
        public PlantYieldCalculationService(IPlantEnvironmentalProcessingService environmentalService = null, 
                                          IPlantGeneticsService geneticsService = null)
        {
            _environmentalService = environmentalService;
            _geneticsService = geneticsService;
        }
        
        public void Initialize()
        {
            if (IsInitialized)
            {
                Debug.LogWarning("[PlantYieldCalculationService] Already initialized");
                return;
            }
            
            // Initialize trait expression engine for genetic yield factors
            if (_enableGeneticYieldFactors)
            {
                _traitExpressionEngine = new object();
            }
            
            IsInitialized = true;
            
            if (_enableDetailedLogging)
            {
                Debug.Log("[PlantYieldCalculationService] Initialized successfully");
            }
        }
        
        public void Shutdown()
        {
            if (!IsInitialized) return;
            
            _cachedYieldCalculations.Clear();
            _lastYieldCalculationTime.Clear();
            _yieldCalculationHistory.Clear();
            _traitExpressionEngine = null;
            
            IsInitialized = false;
            
            if (_enableDetailedLogging)
            {
                Debug.Log("[PlantYieldCalculationService] Shutdown completed");
            }
        }
        
        /// <summary>
        /// Processes a plant harvest and returns comprehensive harvest results.
        /// </summary>
        public SystemsHarvestResults HarvestPlant(string plantID)
        {
            if (!IsInitialized)
            {
                Debug.LogError("[PlantYieldCalculationService] Service not initialized");
                return null;
            }
            
            var startTime = Time.realtimeSinceStartup;
            
            // Find the plant instance (this would need to be injected or accessed via PlantManager)
            var plantInstance = FindPlantInstance(plantID);
            if (plantInstance == null)
            {
                // Use warning instead of error for testing scenarios (when GameManager is not available)
                if (GameManager.Instance == null)
                {
                    Debug.LogWarning($"[PlantYieldCalculationService] Cannot harvest unknown plant: {plantID} (service in testing mode)");
                }
                else
                {
                    Debug.LogError($"[PlantYieldCalculationService] Cannot harvest unknown plant: {plantID}");
                }
                return null;
            }
            
            // Validate harvest readiness
            if (!IsReadyForHarvest(plantInstance))
            {
                Debug.LogWarning($"[PlantYieldCalculationService] Plant {plantID} is not ready for harvest (Stage: {plantInstance.CurrentGrowthStage})");
                return null;
            }
            
            // Calculate final yield
            float finalYield = CalculateFinalHarvestYield(plantInstance);
            
            // Calculate quality score
            float qualityScore = CalculateHarvestQuality(plantInstance);
            
            // Generate cannabinoid and terpene profiles
            var cannabinoidProfile = GenerateCannabinoidProfile(plantInstance);
            var terpeneProfile = GenerateTerpeneProfile(plantInstance);
            
            // Create harvest results
            var harvestResults = new SystemsHarvestResults
            {
                PlantID = plantID,
                TotalYieldGrams = finalYield,
                QualityScore = qualityScore,
                Cannabinoids = cannabinoidProfile,
                Terpenes = terpeneProfile,
                FloweringDays = CalculateFloweringDays(plantInstance),
                FinalHealth = plantInstance.CurrentHealth,
                HarvestDate = System.DateTime.Now
            };
            
            // Apply post-harvest processing if enabled
            if (_enablePostHarvestProcessing)
            {
                ApplyPostHarvestProcessing(harvestResults, plantInstance);
            }
            
            // Update tracking data
            UpdateHarvestTracking(plantInstance, harvestResults);
            
            // Performance tracking
            _harvestsProcessed++;
            _totalHarvestProcessingTime += Time.realtimeSinceStartup - startTime;
            
            if (_enableDetailedLogging)
            {
                Debug.Log($"[PlantYieldCalculationService] Harvested plant {plantID}: {finalYield:F1}g yield, {qualityScore:F2} quality");
            }
            
            return harvestResults;
        }
        
        /// <summary>
        /// Calculates the expected yield for a plant instance based on current conditions (interface method).
        /// </summary>
        public float CalculateExpectedYield(PlantInstance plantInstance)
        {
            var yieldData = CalculateExpectedYieldData(plantInstance);
            return yieldData.EstimatedYield;
        }
        
        /// <summary>
        /// Calculates the expected yield data for a plant instance based on current conditions.
        /// </summary>
        public PlantYieldData CalculateExpectedYieldData(PlantInstance plantInstance)
        {
            if (!IsInitialized || plantInstance == null)
            {
                if (_enableDetailedLogging)
                {
                    Debug.LogWarning("[PlantYieldCalculationService] Cannot calculate expected yield for null plant instance");
                }
                return new PlantYieldData
                {
                    BaseYield = 0f,
                    EstimatedYield = 0f,
                    QualityScore = 0f,
                    HealthModifier = 0f,
                    StageModifier = 0f,
                    EnvironmentalModifier = 0f,
                    GeneticModifier = 0f,
                    StressModifier = 0f,
                    GrowthStage = PlantGrowthStage.Seed
                };
            }
            
            var startTime = Time.realtimeSinceStartup;
            
            // Check cache first
            if (_enableYieldCaching && TryGetCachedYield(plantInstance.PlantID, out float cachedYield))
            {
                return new PlantYieldData
                {
                    BaseYield = cachedYield,
                    EstimatedYield = cachedYield,
                    QualityScore = CalculateHarvestQuality(plantInstance),
                    HealthModifier = 1f,
                    StageModifier = 1f,
                    EnvironmentalModifier = 1f,
                    GeneticModifier = 1f,
                    StressModifier = 1f,
                    GrowthStage = plantInstance.CurrentGrowthStage
                };
            }
            
            // Get base yield from strain data
            float baseYield = GetBaseYieldFromStrain(plantInstance);
            
            // Apply health modifier
            float healthModifier = CalculateHealthYieldModifier(plantInstance);
            
            // Apply growth stage modifier
            float stageModifier = GetStageYieldModifier(plantInstance.CurrentGrowthStage);
            
            // Apply environmental factors if enabled
            float environmentalModifier = 1f;
            if (_enableEnvironmentalYieldFactors && _environmentalService != null)
            {
                environmentalModifier = CalculateEnvironmentalYieldModifier(plantInstance);
            }
            
            // Apply genetic factors if enabled
            float geneticModifier = 1f;
            if (_enableGeneticYieldFactors)
            {
                geneticModifier = CalculateGeneticYieldModifier(plantInstance);
            }
            
            // Apply stress modifier
            float stressModifier = CalculateStressYieldModifier(plantInstance);
            
            // Calculate final expected yield
            float expectedYield = baseYield * healthModifier * stageModifier * environmentalModifier * geneticModifier * stressModifier;
            
            // Apply harvest quality multiplier if enabled
            if (_enableYieldVariability)
            {
                expectedYield *= _harvestQualityMultiplier;
            }
            
            // Clamp to reasonable bounds
            expectedYield = Mathf.Clamp(expectedYield, baseYield * _minYieldMultiplier, baseYield * _maxYieldMultiplier);
            
            // Update cache
            if (_enableYieldCaching)
            {
                UpdateYieldCache(plantInstance.PlantID, expectedYield);
            }
            
            // Performance tracking
            _yieldCalculationsPerformed++;
            _totalYieldCalculationTime += Time.realtimeSinceStartup - startTime;
            
            if (_enableDetailedLogging)
            {
                Debug.Log($"[PlantYieldCalculationService] Expected yield for plant {plantInstance.PlantID}: {expectedYield:F1}g (Base: {baseYield:F1}g, Health: {healthModifier:F2}, Stage: {stageModifier:F2}, Env: {environmentalModifier:F2}, Genetic: {geneticModifier:F2}, Stress: {stressModifier:F2})");
            }
            
            // Create comprehensive yield data
            var yieldData = new PlantYieldData
            {
                BaseYield = baseYield,
                EstimatedYield = Mathf.Max(0f, expectedYield),
                QualityScore = CalculateHarvestQuality(plantInstance),
                HealthModifier = healthModifier,
                StageModifier = stageModifier,
                EnvironmentalModifier = environmentalModifier,
                GeneticModifier = geneticModifier,
                StressModifier = stressModifier,
                GrowthStage = plantInstance.CurrentGrowthStage
            };
            
            return yieldData;
        }
        
        /// <summary>
        /// Gets the yield modifier based on growth stage.
        /// </summary>
        public float GetStageYieldModifier(PlantGrowthStage stage)
        {
            return stage switch
            {
                PlantGrowthStage.Seed => 0f,
                PlantGrowthStage.Germination => 0f,
                PlantGrowthStage.Seedling => 0.1f,
                PlantGrowthStage.Vegetative => 0.3f,
                PlantGrowthStage.PreFlowering => 0.5f,
                PlantGrowthStage.Flowering => 0.8f,
                PlantGrowthStage.Ripening => 0.95f,
                PlantGrowthStage.Harvestable => 1f,
                PlantGrowthStage.Harvest => 0f,
                PlantGrowthStage.Drying => 0f,
                PlantGrowthStage.Curing => 0f,
                _ => 0.5f // Default fallback
            };
        }
        
        /// <summary>
        /// Gets comprehensive yield calculation statistics.
        /// </summary>
        public YieldCalculationStatistics GetYieldStatistics()
        {
            return new YieldCalculationStatistics
            {
                TotalYieldCalculations = _yieldCalculationsPerformed,
                AverageYieldCalculationTime = _yieldCalculationsPerformed > 0 ? (_totalYieldCalculationTime / _yieldCalculationsPerformed) * 1000f : 0f,
                TotalHarvestsProcessed = _harvestsProcessed,
                AverageHarvestProcessingTime = _harvestsProcessed > 0 ? (_totalHarvestProcessingTime / _harvestsProcessed) * 1000f : 0f,
                CachedYieldCalculations = _cachedYieldCalculations.Count,
                YieldVariabilityEnabled = _enableYieldVariability,
                PostHarvestProcessingEnabled = _enablePostHarvestProcessing,
                GeneticYieldFactorsEnabled = _enableGeneticYieldFactors,
                EnvironmentalYieldFactorsEnabled = _enableEnvironmentalYieldFactors
            };
        }
        
        #region Private Helper Methods
        
        private PlantInstance FindPlantInstance(string plantID)
        {
            // This would need to be implemented by accessing the plant registry
            // For now, return null - this requires dependency injection of plant registry
            return null;
        }
        
        private bool IsReadyForHarvest(PlantInstance plantInstance)
        {
            return plantInstance.CurrentGrowthStage == PlantGrowthStage.Harvest || 
                   plantInstance.CurrentGrowthStage == PlantGrowthStage.Harvestable;
        }
        
        private float GetBaseYieldFromStrain(PlantInstance plantInstance)
        {
            if (plantInstance.Strain != null)
            {
                return plantInstance.Strain.BaseYieldGrams;
            }
            
            if (plantInstance.GeneticProfile?.BaseSpecies != null)
            {
                // Use the average of the yield range as base yield
                var yieldRange = plantInstance.GeneticProfile.BaseSpecies.YieldPerPlantRange;
                return (yieldRange.x + yieldRange.y) / 2f;
            }
            
            return _defaultBaseYieldGrams;
        }
        
        private float CalculateHealthYieldModifier(PlantInstance plantInstance)
        {
            // Healthy plants produce more yield
            float healthRatio = plantInstance.CurrentHealth / 100f;
            return Mathf.Lerp(0.1f, 1.2f, healthRatio);
        }
        
        private float CalculateStressYieldModifier(PlantInstance plantInstance)
        {
            // Stressed plants produce less yield
            float stressRatio = plantInstance.StressLevel / 100f;
            return Mathf.Lerp(1.0f, 0.3f, stressRatio);
        }
        
        private float CalculateEnvironmentalYieldModifier(PlantInstance plantInstance)
        {
            if (_environmentalService == null) return 1f;
            
            // Get environmental fitness from environmental service
            float environmentalFitness = _environmentalService.GetPlantEnvironmentalFitness(plantInstance.PlantID);
            
            // Convert fitness to yield modifier
            return Mathf.Lerp(0.5f, 1.5f, environmentalFitness);
        }
        
        private float CalculateGeneticYieldModifier(PlantInstance plantInstance)
        {
            if (_traitExpressionEngine == null || plantInstance.GeneticProfile == null) return 1f;
            
            // Use strain data for genetic yield influence
            if (plantInstance.GeneticProfile.BaseSpecies != null)
            {
                // Use the yield range to calculate potential modifier
                var yieldRange = plantInstance.GeneticProfile.BaseSpecies.YieldPerPlantRange;
                float yieldPotentialNormalized = (yieldRange.y - yieldRange.x) / yieldRange.y;
                float strainYieldModifier = 0.8f + (yieldPotentialNormalized * 0.4f); // Range 0.8 to 1.2
                return Mathf.Clamp(strainYieldModifier, 0.5f, 2.0f);
            }
            
            return 1f;
        }
        
        private float CalculateFinalHarvestYield(PlantInstance plantInstance)
        {
            float expectedYield = CalculateExpectedYield(plantInstance);
            
            // Apply yield variability if enabled
            if (_enableYieldVariability)
            {
                float variability = UnityEngine.Random.Range(-_yieldVariabilityRange, _yieldVariabilityRange);
                expectedYield *= (1f + variability);
            }
            
            return Mathf.Max(0f, expectedYield);
        }
        
        private float CalculateHarvestQuality(PlantInstance plantInstance)
        {
            // Base quality from plant health
            float healthQuality = plantInstance.CurrentHealth / 100f;
            
            // Environmental quality factor
            float environmentalQuality = 1f;
            if (_environmentalService != null)
            {
                environmentalQuality = _environmentalService.GetPlantEnvironmentalFitness(plantInstance.PlantID);
            }
            
            // Stress quality penalty
            float stressQuality = 1f - (plantInstance.StressLevel / 100f);
            
            // Genetic quality factor
            float geneticQuality = 1f;
            if (plantInstance.GeneticProfile?.BaseSpecies != null)
            {
                // Use THC and CBD potential ranges to estimate quality potential
                var thcRange = plantInstance.GeneticProfile.BaseSpecies.ThcPotentialRange;
                var cbdRange = plantInstance.GeneticProfile.BaseSpecies.CbdPotentialRange;
                float potencyPotential = (thcRange.y + cbdRange.y) / 35f; // Normalize to 0-1 range
                geneticQuality = Mathf.Clamp01(potencyPotential);
            }
            
            // Combine factors
            float overallQuality = (healthQuality * 0.3f + environmentalQuality * 0.25f + stressQuality * 0.25f + geneticQuality * 0.2f);
            
            return Mathf.Clamp01(overallQuality);
        }
        
        private CannabinoidProfile GenerateCannabinoidProfile(PlantInstance plantInstance)
        {
            // Generate cannabinoid profile based on genetics and environmental factors
            var profile = new CannabinoidProfile();
            
            if (plantInstance.Strain != null)
            {
                var strain = plantInstance.Strain;
                profile.ThcPercentage = strain.THCContent();
                profile.CbdPercentage = strain.CBDContent();
                // Add other cannabinoids as needed
            }
            
            return profile;
        }
        
        private TerpeneProfile GenerateTerpeneProfile(PlantInstance plantInstance)
        {
            // Generate terpene profile based on genetics and environmental factors
            var profile = new TerpeneProfile();
            
            if (plantInstance.GeneticProfile?.BaseSpecies != null)
            {
                // Use species data to generate terpene profile
                // This would be expanded based on actual terpene data structure
            }
            
            return profile;
        }
        
        private int CalculateFloweringDays(PlantInstance plantInstance)
        {
            // Calculate days spent in flowering stage
            // Use days since planted as approximation for now
            return plantInstance.DaysSincePlanted;
        }
        
        private void ApplyPostHarvestProcessing(SystemsHarvestResults harvestResults, PlantInstance plantInstance)
        {
            // Apply post-harvest processing effects (drying, curing, etc.)
            // This could modify yield and quality based on processing methods
        }
        
        private void UpdateHarvestTracking(PlantInstance plantInstance, SystemsHarvestResults harvestResults)
        {
            var plantId = plantInstance.PlantID;
            _yieldCalculationHistory[plantId] = new YieldCalculationData
            {
                PlantID = plantId,
                FinalYield = harvestResults.TotalYieldGrams,
                QualityScore = harvestResults.QualityScore,
                HarvestDate = harvestResults.HarvestDate,
                FloweringDays = harvestResults.FloweringDays
            };
        }
        
        private bool TryGetCachedYield(string plantId, out float cachedYield)
        {
            cachedYield = 0f;
            
            if (!_cachedYieldCalculations.ContainsKey(plantId))
                return false;
            
            // Check if cache is still valid
            float lastCalculationTime = _lastYieldCalculationTime.GetValueOrDefault(plantId, 0f);
            if (Time.time - lastCalculationTime > _cacheRefreshInterval)
            {
                _cachedYieldCalculations.Remove(plantId);
                _lastYieldCalculationTime.Remove(plantId);
                return false;
            }
            
            cachedYield = _cachedYieldCalculations[plantId];
            return true;
        }
        
        private void UpdateYieldCache(string plantId, float yieldValue)
        {
            _cachedYieldCalculations[plantId] = yieldValue;
            _lastYieldCalculationTime[plantId] = Time.time;
        }
        
        #endregion
    }
    
    /// <summary>
    /// Plant yield data structure for expected yield calculations
    /// </summary>
    [System.Serializable]
    public class PlantYieldData
    {
        public float BaseYield;
        public float EstimatedYield;
        public float QualityScore;
        public float HealthModifier;
        public float StageModifier;
        public float EnvironmentalModifier;
        public float GeneticModifier;
        public float StressModifier;
        public PlantGrowthStage GrowthStage;
    }
    
    /// <summary>
    /// Yield calculation data structure for tracking harvest history.
    /// </summary>
    [System.Serializable]
    public class YieldCalculationData
    {
        public string PlantID;
        public float FinalYield;
        public float QualityScore;
        public System.DateTime HarvestDate;
        public int FloweringDays;
    }
    
    /// <summary>
    /// Yield calculation statistics structure.
    /// </summary>
    [System.Serializable]
    public class YieldCalculationStatistics
    {
        public int TotalYieldCalculations;
        public float AverageYieldCalculationTime; // milliseconds
        public int TotalHarvestsProcessed;
        public float AverageHarvestProcessingTime; // milliseconds
        public int CachedYieldCalculations;
        public bool YieldVariabilityEnabled;
        public bool PostHarvestProcessingEnabled;
        public bool GeneticYieldFactorsEnabled;
        public bool EnvironmentalYieldFactorsEnabled;
        
        public override string ToString()
        {
            return $"Yield Stats: {TotalYieldCalculations} calcs, {AverageYieldCalculationTime:F2}ms avg, {TotalHarvestsProcessed} harvests, {AverageHarvestProcessingTime:F2}ms harvest avg";
        }
    }
}