using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Data.Cultivation;
// using ProjectChimera.Systems.Genetics; // Invalid namespace - genetics in ProjectChimera.Data.Genetics
using PlantGrowthStage = ProjectChimera.Data.Shared.PlantGrowthStage;

namespace ProjectChimera.Systems.Cultivation
{
    /// <summary>
    /// PC-013-3: Plant Processing Service - Handles plant updates, batch processing, and performance optimization
    /// Extracted from monolithic PlantManager for Single Responsibility Principle
    /// Focuses solely on plant processing, growth calculations, and performance optimization
    /// </summary>
    public class PlantProcessingService : IPlantProcessingService
    {
        [Header("Processing Configuration")]
        [SerializeField] private float _plantUpdateInterval = 1f;
        [SerializeField] private int _maxPlantsPerUpdate = 10;
        [SerializeField] private bool _enableDetailedLogging = false;
        
        [Header("Growth Configuration")]
        [SerializeField] private AnimationCurve _defaultGrowthCurve;
        [SerializeField] private float _globalGrowthModifier = 1f;
        [SerializeField] private bool _enableStressSystem = true;
        [SerializeField] private bool _enableGxEInteractions = true;
        
        [Header("Advanced Processing")]
        [SerializeField] private bool _enableAdvancedGenetics = true;
        [SerializeField] private float _traitExpressionCacheCleanupInterval = 60f;
        
        // Processing state
        private IPlantLifecycleService _plantLifecycleService;
        private PlantUpdateProcessor _updateProcessor;
        private List<PlantInstance> _plantsToUpdate = new List<PlantInstance>();
        private int _currentUpdateIndex = 0;
        private float _lastUpdateTime = 0f;
        private float _lastCacheCleanupTime = 0f;
        private int _plantsProcessedThisFrame = 0;
        
        public bool IsInitialized { get; private set; }
        
        public float GlobalGrowthModifier
        {
            get => _globalGrowthModifier;
            set => _globalGrowthModifier = Mathf.Clamp(value, 0.1f, 10f);
        }
        
        public PlantProcessingService() : this(null)
        {
        }
        
        public PlantProcessingService(IPlantLifecycleService plantLifecycleService)
        {
            _plantLifecycleService = plantLifecycleService;
        }
        
        public void Initialize()
        {
            if (IsInitialized) return;
            
            Debug.Log("[PlantProcessingService] Initializing plant processing system...");
            
            // Initialize enhanced update processor with advanced genetics support
            _updateProcessor = new PlantUpdateProcessor(_enableStressSystem, _enableGxEInteractions, _enableAdvancedGenetics);
            
            // Initialize default growth curve if not set
            if (_defaultGrowthCurve == null || _defaultGrowthCurve.keys.Length == 0)
            {
                InitializeDefaultGrowthCurve();
            }
            
            // Subscribe to plant lifecycle events
            if (_plantLifecycleService != null)
            {
                _plantLifecycleService.OnPlantAdded += OnPlantAdded;
            }
            
            _lastUpdateTime = Time.time;
            _lastCacheCleanupTime = Time.time;
            
            IsInitialized = true;
            Debug.Log($"[PlantProcessingService] Plant processing initialized (Advanced Genetics: {_enableAdvancedGenetics})");
        }
        
        public void Shutdown()
        {
            if (!IsInitialized) return;
            
            Debug.Log("[PlantProcessingService] Shutting down plant processing system...");
            
            // Unsubscribe from events
            if (_plantLifecycleService != null)
            {
                _plantLifecycleService.OnPlantAdded -= OnPlantAdded;
            }
            
            _plantsToUpdate.Clear();
            _updateProcessor = null;
            
            IsInitialized = false;
        }
        
        /// <summary>
        /// Main update method - processes plants in batches for optimal performance.
        /// </summary>
        public void UpdatePlants()
        {
            if (!IsInitialized) return;
            
            // Check if it's time for plant updates
            if (Time.time - _lastUpdateTime < _plantUpdateInterval)
                return;
            
            // Update plants list from lifecycle service
            UpdatePlantsToProcessList();
            
            if (_plantsToUpdate.Count == 0)
            {
                _lastUpdateTime = Time.time;
                return;
            }
            
            var timeManager = GameManager.Instance?.GetManager<TimeManager>();
            float deltaTime = timeManager?.GetScaledDeltaTime() ?? Time.deltaTime;
            
            // Determine optimal batch size based on plant count and performance
            int optimalBatchSize = CalculateOptimalBatchSize();
            int plantsToProcess = Mathf.Min(optimalBatchSize, _plantsToUpdate.Count);
            
            // Get the next batch of plants to process
            var plantsToProcessThisFrame = GetNextPlantsToProcess(plantsToProcess);
            
            if (plantsToProcessThisFrame.Count == 0)
            {
                _lastUpdateTime = Time.time;
                return;
            }
            
            // Use batch processing for improved performance if advanced genetics is enabled
            if (_enableAdvancedGenetics && plantsToProcessThisFrame.Count > 10)
            {
                ProcessPlantsBatch(plantsToProcessThisFrame, deltaTime);
            }
            else
            {
                ProcessPlantsIndividually(plantsToProcessThisFrame, deltaTime);
            }
            
            // Advance update index
            _currentUpdateIndex += plantsToProcess;
            
            // Reset index when we've processed all plants
            if (_currentUpdateIndex >= _plantsToUpdate.Count)
            {
                _currentUpdateIndex = 0;
                ValidateAndCleanupPlants();
            }
            
            _lastUpdateTime = Time.time;
            
            // Periodic cache cleanup
            if (_enableAdvancedGenetics && Time.time - _lastCacheCleanupTime >= _traitExpressionCacheCleanupInterval)
            {
                PerformCacheCleanup();
            }
        }
        
        /// <summary>
        /// Sets the global growth modifier for all plants.
        /// </summary>
        public void SetGlobalGrowthModifier(float modifier)
        {
            GlobalGrowthModifier = modifier;
            Debug.Log($"[PlantProcessingService] Global growth modifier set to {_globalGrowthModifier:F2}");
        }
        
        /// <summary>
        /// Calculates optimal batch size based on performance metrics and system resources.
        /// </summary>
        public int CalculateOptimalBatchSize()
        {
            int baseBatchSize = _maxPlantsPerUpdate;
            
            // Consider system capabilities
            if (_enableAdvancedGenetics && SystemInfo.supportsComputeShaders && SystemInfo.systemMemorySize > 8192)
            {
                baseBatchSize = Mathf.Min(100, baseBatchSize * 2); // Larger batches for powerful systems
            }
            
            // Adjust based on current plant count
            int totalPlants = _plantsToUpdate.Count;
            if (totalPlants > 1000)
            {
                baseBatchSize = Mathf.Max(5, baseBatchSize / 2); // Smaller batches for very large populations
            }
            else if (totalPlants < 50)
            {
                baseBatchSize = Mathf.Min(totalPlants, baseBatchSize * 2); // Larger batches for small populations
            }
            
            return baseBatchSize;
        }
        
        /// <summary>
        /// Gets the next batch of plants to process in this frame.
        /// </summary>
        public List<PlantInstance> GetNextPlantsToProcess(int count)
        {
            var plantsToProcess = new List<PlantInstance>();
            int endIndex = Mathf.Min(_currentUpdateIndex + count, _plantsToUpdate.Count);
            
            for (int i = _currentUpdateIndex; i < endIndex; i++)
            {
                var plant = _plantsToUpdate[i];
                if (plant != null && plant.IsActive)
                {
                    plantsToProcess.Add(plant);
                }
            }
            
            return plantsToProcess;
        }
        
        /// <summary>
        /// Performs cache cleanup and performance optimization.
        /// </summary>
        public void PerformCacheCleanup()
        {
            if (!IsInitialized) return;
            
            if (_updateProcessor != null)
            {
                _updateProcessor.ClearTraitExpressionCache();
            }
            
            _lastCacheCleanupTime = Time.time;
            
            if (_enableDetailedLogging)
            {
                Debug.Log("[PlantProcessingService] Performed cache cleanup for genetic calculations");
            }
        }
        
        /// <summary>
        /// Performs comprehensive performance optimization.
        /// </summary>
        public void PerformPerformanceOptimization()
        {
            if (!IsInitialized) return;
            
            if (_updateProcessor != null)
            {
                _updateProcessor.OptimizePerformance();
            }
            
            // Clean up inactive plants
            ValidateAndCleanupPlants();
            
            if (_enableDetailedLogging)
            {
                Debug.Log("[PlantProcessingService] Performed performance optimization");
            }
        }
        
        /// <summary>
        /// Forces an immediate update of all plants (for testing/debugging).
        /// </summary>
        public void ForceUpdateAllPlants()
        {
            if (!IsInitialized) return;
            
            UpdatePlantsToProcessList();
            
            if (_plantsToUpdate.Count == 0) return;
            
            var timeManager = GameManager.Instance?.GetManager<TimeManager>();
            float deltaTime = timeManager?.GetScaledDeltaTime() ?? Time.deltaTime;
            
            Debug.Log($"[PlantProcessingService] Force updating {_plantsToUpdate.Count} plants");
            
            if (_enableAdvancedGenetics && _plantsToUpdate.Count > 10)
            {
                ProcessPlantsBatch(_plantsToUpdate, deltaTime);
            }
            else
            {
                ProcessPlantsIndividually(_plantsToUpdate, deltaTime);
            }
        }
        
        /// <summary>
        /// Gets processing statistics.
        /// </summary>
        public (int totalPlants, int currentBatch, float lastUpdateTime, int optimalBatchSize) GetProcessingStats()
        {
            return (
                _plantsToUpdate.Count,
                _currentUpdateIndex,
                Time.time - _lastUpdateTime,
                CalculateOptimalBatchSize()
            );
        }
        
        /// <summary>
        /// Enables or disables advanced genetics processing.
        /// </summary>
        public void SetAdvancedGeneticsEnabled(bool enabled)
        {
            if (_enableAdvancedGenetics != enabled)
            {
                _enableAdvancedGenetics = enabled;
                
                // Reinitialize update processor with new setting
                if (_updateProcessor != null)
                {
                    _updateProcessor = new PlantUpdateProcessor(_enableStressSystem, _enableGxEInteractions, _enableAdvancedGenetics);
                }
                
                Debug.Log($"[PlantProcessingService] Advanced genetics {(enabled ? "enabled" : "disabled")}");
            }
        }
        
        /// <summary>
        /// Process comprehensive plant data for a specific plant
        /// </summary>
        public PlantProcessingResult ProcessPlantData(PlantInstance plant)
        {
            if (!IsInitialized || plant == null)
            {
                return new PlantProcessingResult
                {
                    PlantID = plant?.PlantID ?? "Unknown",
                    ProcessingSuccess = false,
                    ProcessingTimeMs = 0f,
                    ErrorMessage = "Service not initialized or plant is null"
                };
            }
            
            var startTime = System.DateTime.Now;
            
            try
            {
                // Get current time delta
                var timeManager = GameManager.Instance?.GetManager<TimeManager>();
                float deltaTime = timeManager?.GetScaledDeltaTime() ?? Time.deltaTime;
                
                // Process the plant using the update processor
                if (_updateProcessor != null)
                {
                    _updateProcessor.UpdatePlant(plant, deltaTime, _globalGrowthModifier);
                }
                
                // Calculate processing statistics
                var processingTime = (System.DateTime.Now - startTime).TotalMilliseconds;
                
                // Update processing counters
                _plantsProcessedThisFrame++;
                _lastUpdateTime = Time.time;
                
                return new PlantProcessingResult
                {
                    PlantID = plant.PlantID,
                    ProcessingSuccess = true,
                    ProcessingTimeMs = (float)processingTime,
                    HealthAfterProcessing = plant.CurrentHealth,
                    StressAfterProcessing = plant.StressLevel,
                    GrowthStageAfterProcessing = plant.CurrentGrowthStage,
                    DeltaTimeUsed = deltaTime,
                    GrowthModifierApplied = _globalGrowthModifier
                };
            }
            catch (System.Exception ex)
            {
                var processingTime = (System.DateTime.Now - startTime).TotalMilliseconds;
                
                Debug.LogError($"[PlantProcessingService] Error processing plant {plant.PlantID}: {ex.Message}");
                
                return new PlantProcessingResult
                {
                    PlantID = plant.PlantID,
                    ProcessingSuccess = false,
                    ProcessingTimeMs = (float)processingTime,
                    ErrorMessage = ex.Message
                };
            }
        }
        
        /// <summary>
        /// Updates the list of plants to process from the lifecycle service.
        /// </summary>
        private void UpdatePlantsToProcessList()
        {
            if (_plantLifecycleService == null) return;
            
            var trackedPlants = _plantLifecycleService.GetTrackedPlants();
            _plantsToUpdate = trackedPlants.Where(p => p != null && p.IsActive).ToList();
        }
        
        /// <summary>
        /// Processes plants using batch processing for optimal performance.
        /// </summary>
        private void ProcessPlantsBatch(List<PlantInstance> plants, float deltaTime)
        {
            if (_updateProcessor == null) return;
            
            try
            {
                _updateProcessor.UpdatePlantsBatch(plants, deltaTime, _globalGrowthModifier);
                
                if (_enableDetailedLogging)
                {
                    Debug.Log($"[PlantProcessingService] Batch processed {plants.Count} plants");
                }
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[PlantProcessingService] Error in batch processing: {ex.Message}");
                // Fallback to individual processing
                ProcessPlantsIndividually(plants, deltaTime);
            }
        }
        
        /// <summary>
        /// Processes plants individually for smaller batches or as fallback.
        /// </summary>
        private void ProcessPlantsIndividually(List<PlantInstance> plants, float deltaTime)
        {
            if (_updateProcessor == null) return;
            
            foreach (var plant in plants)
            {
                if (plant != null && plant.IsActive)
                {
                    try
                    {
                        _updateProcessor.UpdatePlant(plant, deltaTime, _globalGrowthModifier);
                    }
                    catch (System.Exception ex)
                    {
                        Debug.LogError($"[PlantProcessingService] Error processing plant {plant.PlantID}: {ex.Message}");
                    }
                }
            }
            
            if (_enableDetailedLogging)
            {
                Debug.Log($"[PlantProcessingService] Individually processed {plants.Count} plants");
            }
        }
        
        /// <summary>
        /// Validates and cleans up inactive plants from the processing list.
        /// </summary>
        private void ValidateAndCleanupPlants()
        {
            int removedCount = _plantsToUpdate.RemoveAll(p => p == null || !p.IsActive);
            
            if (removedCount > 0 && _enableDetailedLogging)
            {
                Debug.Log($"[PlantProcessingService] Cleaned up {removedCount} inactive plants");
            }
        }
        
        /// <summary>
        /// Initializes the default growth curve if none is provided.
        /// </summary>
        private void InitializeDefaultGrowthCurve()
        {
            _defaultGrowthCurve = new AnimationCurve();
            _defaultGrowthCurve.AddKey(0f, 0f);      // Start
            _defaultGrowthCurve.AddKey(0.25f, 0.1f); // Slow initial growth
            _defaultGrowthCurve.AddKey(0.5f, 0.4f);  // Accelerating growth
            _defaultGrowthCurve.AddKey(0.75f, 0.8f); // Peak growth
            _defaultGrowthCurve.AddKey(1f, 1f);      // Mature
            
            Debug.Log("[PlantProcessingService] Initialized default growth curve");
        }
        
        /// <summary>
        /// Handles new plants added to the lifecycle service.
        /// </summary>
        private void OnPlantAdded(PlantInstance plant)
        {
            if (plant != null && plant.IsActive)
            {
                if (!_plantsToUpdate.Contains(plant))
                {
                    _plantsToUpdate.Add(plant);
                    
                    if (_enableDetailedLogging)
                    {
                        Debug.Log($"[PlantProcessingService] Added plant {plant.PlantID} to processing queue");
                    }
                }
            }
        }
    }
    
    /// <summary>
    /// Plant processing result data structure
    /// </summary>
    [System.Serializable]
    public class PlantProcessingResult
    {
        public string PlantID;
        public bool ProcessingSuccess;
        public float ProcessingTimeMs;
        public float HealthAfterProcessing;
        public float StressAfterProcessing;
        public PlantGrowthStage GrowthStageAfterProcessing;
        public float DeltaTimeUsed;
        public float GrowthModifierApplied;
        public string ErrorMessage;
    }
}