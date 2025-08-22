using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Data.Cultivation;
using PlantGrowthStage = ProjectChimera.Data.Shared.PlantGrowthStage;
using ProjectChimera.Data.Genetics;
using HarvestResults = ProjectChimera.Systems.Cultivation.HarvestResults;
using DataHarvestResults = ProjectChimera.Data.Cultivation.DataHarvestResults;

namespace ProjectChimera.Systems.Cultivation
{
    /// <summary>
    /// PC-013-3: Plant Harvest Service - Handles harvest operations and post-harvest processing
    /// Extracted from monolithic PlantManager for Single Responsibility Principle
    /// Focuses solely on plant harvesting, yield calculation, and harvest quality assessment
    /// </summary>
    public class PlantHarvestService : IPlantHarvestService
    {
        [Header("Harvest Configuration")]
        [SerializeField] private bool _enableYieldVariability = true;
        [SerializeField] private bool _enablePostHarvestProcessing = true;
        [SerializeField] private float _harvestQualityMultiplier = 1f;
        [SerializeField] private bool _enableDetailedLogging = false;
        
        // Dependencies
        private IPlantLifecycleService _plantLifecycleService;
        private CultivationManager _cultivationManager;
        
        // Harvest tracking
        private int _totalHarvests = 0;
        private float _totalYieldHarvested = 0f;
        private float _bestQualityScore = 0f;
        
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
            set => _harvestQualityMultiplier = Mathf.Clamp(value, 0.1f, 5f);
        }
        
        public System.Action<PlantInstance> OnPlantHarvested { get; set; }
        
        public PlantHarvestService() : this(null, null)
        {
        }
        
        public PlantHarvestService(IPlantLifecycleService plantLifecycleService, CultivationManager cultivationManager)
        {
            _plantLifecycleService = plantLifecycleService;
            _cultivationManager = cultivationManager;
        }
        
        public void Initialize()
        {
            if (IsInitialized) return;
            
            Debug.Log("[PlantHarvestService] Initializing harvest management system...");
            
            // Get dependencies if not provided
            if (_cultivationManager == null)
            {
                _cultivationManager = GameManager.Instance?.GetManager<CultivationManager>();
            }
            
            if (_cultivationManager == null)
            {
                Debug.LogError("[PlantHarvestService] CultivationManager not found - harvest operations will be limited");
                return;
            }
            
            IsInitialized = true;
            Debug.Log($"[PlantHarvestService] Harvest management initialized (Variability: {_enableYieldVariability}, PostProcessing: {_enablePostHarvestProcessing})");
        }
        
        public void Shutdown()
        {
            if (!IsInitialized) return;
            
            Debug.Log("[PlantHarvestService] Shutting down harvest management system...");
            
            // Log final harvest statistics
            if (_enableDetailedLogging)
            {
                Debug.Log($"[PlantHarvestService] Final harvest stats - Total: {_totalHarvests}, Yield: {_totalYieldHarvested:F1}g, Best Quality: {_bestQualityScore:F2}");
            }
            
            IsInitialized = false;
        }
        
        /// <summary>
        /// Harvests a plant and returns the harvest results.
        /// </summary>
        public SystemsHarvestResults HarvestPlant(string plantID)
        {
            if (!IsInitialized)
            {
                Debug.LogError("[PlantHarvestService] Cannot harvest plant: Service not initialized");
                return null;
            }
            
            var plant = _cultivationManager?.GetPlant(plantID);
            if (plant == null)
            {
                Debug.LogError($"[PlantHarvestService] Cannot harvest unknown plant: {plantID}");
                return null;
            }
            
            if (plant.CurrentGrowthStage != PlantGrowthStage.Harvest)
            {
                Debug.LogWarning($"[PlantHarvestService] Plant {plantID} is not ready for harvest (Stage: {plant.CurrentGrowthStage})");
                return null;
            }
            
            var startTime = System.DateTime.Now;
            
            try
            {
                // Perform harvest using CultivationManager
                var dataHarvestResults = plant.Harvest();
                
                // Convert Data layer HarvestResults to Systems layer HarvestResults
                var harvestResults = CreateSystemsHarvestResults(plant, dataHarvestResults);
                
                // Apply quality and yield modifiers
                ApplyHarvestModifiers(harvestResults, plant);
                
                // Get the PlantInstance for event tracking
                var plantInstance = _plantLifecycleService?.GetTrackedPlant(plantID);
                
                // Trigger harvest event
                if (plantInstance != null)
                {
                    OnPlantHarvested?.Invoke(plantInstance);
                }
                
                // Update harvest statistics
                UpdateHarvestStatistics(harvestResults);
                
                // Perform post-harvest processing if enabled
                if (_enablePostHarvestProcessing)
                {
                    ProcessPostHarvestOperations(harvestResults, plant);
                }
                
                // Unregister plant from lifecycle service
                _plantLifecycleService?.UnregisterPlant(plantID, PlantRemovalReason.Harvested);
                
                var harvestTime = (System.DateTime.Now - startTime).TotalMilliseconds;
                
                Debug.Log($"[PlantHarvestService] Harvested plant {plantID}: {harvestResults.TotalYieldGrams:F1}g yield, {harvestResults.QualityScore:F2} quality (Time: {harvestTime:F1}ms)");
                
                return harvestResults;
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[PlantHarvestService] Error harvesting plant {plantID}: {ex.Message}");
                return null;
            }
        }
        
        /// <summary>
        /// Calculates expected yield for a plant instance based on its current state.
        /// </summary>
        public float CalculateExpectedYield(PlantInstance plantInstance)
        {
            if (!IsInitialized)
            {
                Debug.LogError("[PlantHarvestService] Cannot calculate expected yield: Service not initialized");
                return 0f;
            }
            
            if (plantInstance == null)
            {
                Debug.LogWarning("[PlantHarvestService] Cannot calculate expected yield for null plant instance");
                return 0f;
            }
            
            try
            {
                // Get base yield from strain data
                float baseYield = plantInstance.Strain?.BaseYieldGrams ?? 50f; // Default 50g if strain data missing
                
                // Apply health modifier
                float healthModifier = plantInstance.Health / 100f;
                
                // Apply growth stage modifier
                float stageModifier = GetStageYieldModifier(plantInstance.CurrentGrowthStage);
                
                // Apply environmental stress modifier
                float stressModifier = 1f - (plantInstance.StressLevel / 100f);
                
                // Apply harvest quality multiplier
                float qualityModifier = _harvestQualityMultiplier;
                
                // Apply yield variability if enabled
                float variabilityModifier = 1f;
                if (_enableYieldVariability)
                {
                    variabilityModifier = UnityEngine.Random.Range(0.8f, 1.2f); // ±20% variability
                }
                
                // Calculate final expected yield
                float expectedYield = baseYield * healthModifier * stageModifier * stressModifier * qualityModifier * variabilityModifier;
                
                if (_enableDetailedLogging)
                {
                    Debug.Log($"[PlantHarvestService] Expected yield for {plantInstance.PlantID}: {expectedYield:F1}g " +
                             $"(Base: {baseYield:F1}g, Health: {healthModifier:F2}, Stage: {stageModifier:F2}, " +
                             $"Stress: {stressModifier:F2}, Quality: {qualityModifier:F2}, Variability: {variabilityModifier:F2})");
                }
                
                return Mathf.Max(0f, expectedYield);
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[PlantHarvestService] Error calculating expected yield for {plantInstance.PlantID}: {ex.Message}");
                return 0f;
            }
        }
        
        /// <summary>
        /// Gets yield modifier based on growth stage.
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
        /// Gets harvest statistics.
        /// </summary>
        public (int totalHarvests, float totalYield, float bestQuality, float averageYield) GetHarvestStats()
        {
            float averageYield = _totalHarvests > 0 ? _totalYieldHarvested / _totalHarvests : 0f;
            return (_totalHarvests, _totalYieldHarvested, _bestQualityScore, averageYield);
        }
        
        /// <summary>
        /// Processes multiple plants for harvest in batch.
        /// </summary>
        public System.Collections.Generic.List<SystemsHarvestResults> BatchHarvestPlants(System.Collections.Generic.List<string> plantIDs)
        {
            if (!IsInitialized)
            {
                Debug.LogError("[PlantHarvestService] Cannot batch harvest: Service not initialized");
                return new System.Collections.Generic.List<SystemsHarvestResults>();
            }
            
            var results = new System.Collections.Generic.List<SystemsHarvestResults>();
            var startTime = System.DateTime.Now;
            
            Debug.Log($"[PlantHarvestService] Starting batch harvest of {plantIDs.Count} plants");
            
            foreach (var plantID in plantIDs)
            {
                var result = HarvestPlant(plantID);
                if (result != null)
                {
                    results.Add(result);
                }
            }
            
            var batchTime = (System.DateTime.Now - startTime).TotalMilliseconds;
            Debug.Log($"[PlantHarvestService] Batch harvest completed: {results.Count}/{plantIDs.Count} plants harvested in {batchTime:F1}ms");
            
            return results;
        }
        
        /// <summary>
        /// Validates plants ready for harvest.
        /// </summary>
        public System.Collections.Generic.List<string> GetPlantsReadyForHarvest()
        {
            if (!IsInitialized)
            {
                Debug.LogError("[PlantHarvestService] Cannot get harvest-ready plants: Service not initialized");
                return new System.Collections.Generic.List<string>();
            }
            
            var readyPlants = new System.Collections.Generic.List<string>();
            var harvestablePlants = _plantLifecycleService?.GetHarvestablePlants();
            
            if (harvestablePlants != null)
            {
                foreach (var plant in harvestablePlants)
                {
                    if (plant != null && plant.CurrentGrowthStage == PlantGrowthStage.Harvest)
                    {
                        readyPlants.Add(plant.PlantID);
                    }
                }
            }
            
            return readyPlants;
        }
        
        /// <summary>
        /// Check if a plant is ready for harvest
        /// </summary>
        public bool IsPlantReadyForHarvest(PlantInstance plant)
        {
            if (!IsInitialized || plant == null)
            {
                return false;
            }
            
            // Check growth stage readiness
            bool stageReady = plant.CurrentGrowthStage == PlantGrowthStage.Harvest || 
                            plant.CurrentGrowthStage == PlantGrowthStage.Harvestable ||
                            plant.CurrentGrowthStage == PlantGrowthStage.Ripening;
            
            // Check minimum health requirements
            bool healthOk = plant.CurrentHealth > 10f; // Must have some health remaining
            
            // Check plant is active
            bool isActive = plant.IsActive;
            
            bool isReady = stageReady && healthOk && isActive;
            
            if (_enableDetailedLogging)
            {
                Debug.Log($"[PlantHarvestService] Plant {plant.PlantID} harvest readiness: Stage={stageReady}, Health={healthOk}, Active={isActive}, Ready={isReady}");
            }
            
            return isReady;
        }
        
        /// <summary>
        /// Creates Systems layer harvest results from Data layer harvest results.
        /// </summary>
        private SystemsHarvestResults CreateSystemsHarvestResults(PlantInstanceSO plant, DataHarvestResults dataResults)
        {
            var systemsResults = new SystemsHarvestResults
            {
                PlantID = dataResults.PlantID,
                TotalYieldGrams = dataResults.TotalYieldGrams,
                QualityScore = dataResults.QualityScore,
                Cannabinoids = new CannabinoidProfile(), // Create empty profile
                Terpenes = new TerpeneProfile(), // Create empty profile
                FloweringDays = (int)plant.DaysInCurrentStage,
                FinalHealth = plant.OverallHealth,
                HarvestDate = dataResults.HarvestDate
            };
            
            return systemsResults;
        }
        
        /// <summary>
        /// Applies harvest modifiers to the results.
        /// </summary>
        private void ApplyHarvestModifiers(SystemsHarvestResults results, PlantInstanceSO plant)
        {
            // Apply quality multiplier
            results.QualityScore = Mathf.Clamp01(results.QualityScore * _harvestQualityMultiplier);
            
            // Apply yield variability if enabled
            if (_enableYieldVariability)
            {
                float variabilityFactor = UnityEngine.Random.Range(0.9f, 1.1f); // ±10% variability
                results.TotalYieldGrams *= variabilityFactor;
            }
            
            // Apply health-based quality modifier
            float healthModifier = plant.OverallHealth / 100f;
            results.QualityScore *= healthModifier;
            
            // Apply stress-based quality modifier
            float stressModifier = 1f - (plant.StressLevel / 100f);
            results.QualityScore *= stressModifier;
            
            // Ensure quality stays within bounds
            results.QualityScore = Mathf.Clamp01(results.QualityScore);
        }
        
        /// <summary>
        /// Updates harvest statistics.
        /// </summary>
        private void UpdateHarvestStatistics(SystemsHarvestResults results)
        {
            _totalHarvests++;
            _totalYieldHarvested += results.TotalYieldGrams;
            
            if (results.QualityScore > _bestQualityScore)
            {
                _bestQualityScore = results.QualityScore;
            }
        }
        
        /// <summary>
        /// Processes post-harvest operations.
        /// </summary>
        private void ProcessPostHarvestOperations(SystemsHarvestResults results, PlantInstanceSO plant)
        {
            // Placeholder for post-harvest processing
            // This could include drying, curing, trimming, etc.
            
            if (_enableDetailedLogging)
            {
                Debug.Log($"[PlantHarvestService] Processing post-harvest operations for {results.PlantID}");
            }
            
            // Apply post-harvest quality adjustments
            float postHarvestQualityBonus = 0.02f; // +2% quality for proper post-harvest handling
            results.QualityScore = Mathf.Clamp01(results.QualityScore + postHarvestQualityBonus);
        }
    }
}