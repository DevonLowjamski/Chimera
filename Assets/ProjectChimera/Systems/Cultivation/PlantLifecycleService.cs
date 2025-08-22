using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Data.Cultivation;
using ProjectChimera.Data.Genetics;
using ProjectChimera.Core.Events;
using PlantGrowthStage = ProjectChimera.Data.Shared.PlantGrowthStage;

namespace ProjectChimera.Systems.Cultivation
{
    /// <summary>
    /// PC-013-3: Plant Lifecycle Service - Handles plant creation, registration, and lifecycle management
    /// Extracted from monolithic PlantManager for Single Responsibility Principle
    /// Focuses solely on plant lifecycle operations and basic plant management
    /// </summary>
    public class PlantLifecycleService : IPlantLifecycleService
    {
        [Header("Lifecycle Configuration")]
        [SerializeField] private bool _enableDetailedLogging = false;
        
        // Dependencies
        private CultivationManager _cultivationManager;
        private List<PlantInstance> _plantsToUpdate = new List<PlantInstance>();
        
        // Event channels
        [SerializeField] private SimpleGameEventSO _onPlantCreated;
        
        // Events for other systems to subscribe to
        public System.Action<PlantInstance> OnPlantAdded { get; set; }
        public System.Action<PlantInstance> OnPlantStageChanged { get; set; }
        public System.Action<PlantInstance> OnPlantHealthUpdated { get; set; }
        
        public bool IsInitialized { get; private set; }
        
        public PlantLifecycleService() : this(null)
        {
        }
        
        public PlantLifecycleService(CultivationManager cultivationManager)
        {
            _cultivationManager = cultivationManager;
        }
        
        public void Initialize()
        {
            if (IsInitialized) return;
            
            Debug.Log("[PlantLifecycleService] Initializing plant lifecycle management...");
            
            // Get reference to CultivationManager as single source of truth
            if (_cultivationManager == null)
            {
                _cultivationManager = GameManager.Instance?.GetManager<CultivationManager>();
            }
            
            if (_cultivationManager == null)
            {
                Debug.LogError("[PlantLifecycleService] CultivationManager not found! PlantLifecycleService requires CultivationManager as data source.");
                return;
            }
            
            IsInitialized = true;
            Debug.Log("[PlantLifecycleService] Plant lifecycle management initialized successfully.");
        }
        
        public void Shutdown()
        {
            if (!IsInitialized) return;
            
            Debug.Log("[PlantLifecycleService] Shutting down plant lifecycle management...");
            
            // Unsubscribe from all plant events
            foreach (var plant in _plantsToUpdate)
            {
                if (plant != null)
                {
                    UnsubscribeFromPlantEvents(plant);
                }
            }
            
            _plantsToUpdate.Clear();
            IsInitialized = false;
        }
        
        /// <summary>
        /// Creates a new plant instance from a strain definition.
        /// </summary>
        public PlantInstance CreatePlant(PlantStrainSO strain, Vector3 position, Transform parent = null)
        {
            if (!IsInitialized)
            {
                Debug.LogError("[PlantLifecycleService] Cannot create plant: Service not initialized");
                return null;
            }
            
            if (strain == null)
            {
                Debug.LogError("[PlantLifecycleService] Cannot create plant: strain is null");
                return null;
            }
            
            var plantInstance = PlantInstance.CreateFromStrain(strain, position, parent);
            RegisterPlant(plantInstance);
            
            Debug.Log($"[PlantLifecycleService] Created plant: {plantInstance.PlantID} (Strain: {strain.StrainName})");
            
            // Trigger plant created event
            _onPlantCreated?.Raise();
            
            return plantInstance;
        }
        
        /// <summary>
        /// Creates multiple plants from the same strain.
        /// </summary>
        public List<PlantInstance> CreatePlants(PlantStrainSO strain, List<Vector3> positions, Transform parent = null)
        {
            if (!IsInitialized)
            {
                Debug.LogError("[PlantLifecycleService] Cannot create plants: Service not initialized");
                return new List<PlantInstance>();
            }
            
            var plants = new List<PlantInstance>();
            
            foreach (var position in positions)
            {
                var plant = CreatePlant(strain, position, parent);
                if (plant != null)
                    plants.Add(plant);
            }
            
            Debug.Log($"[PlantLifecycleService] Created {plants.Count} plants from strain: {strain.StrainName}");
            return plants;
        }
        
        /// <summary>
        /// Registers an existing plant instance with the lifecycle service.
        /// </summary>
        public void RegisterPlant(PlantInstance plant)
        {
            if (!IsInitialized)
            {
                Debug.LogError("[PlantLifecycleService] Cannot register plant: Service not initialized");
                return;
            }
            
            if (plant == null)
            {
                Debug.LogError("[PlantLifecycleService] Cannot register null plant");
                return;
            }
            
            // Check if plant is already tracked by CultivationManager
            var existingPlant = _cultivationManager.GetPlant(plant.PlantID);
            if (existingPlant != null)
            {
                Debug.LogWarning($"[PlantLifecycleService] Plant {plant.PlantID} already registered in CultivationManager, skipping");
                return;
            }
            
            // Add to processing update list
            if (!_plantsToUpdate.Contains(plant))
            {
                _plantsToUpdate.Add(plant);
            }
            
            // Subscribe to plant events
            SubscribeToPlantEvents(plant);
            
            // Invoke OnPlantAdded event for other systems
            OnPlantAdded?.Invoke(plant);
            
            if (_enableDetailedLogging)
                Debug.Log($"[PlantLifecycleService] Registered plant: {plant.PlantID}");
        }
        
        /// <summary>
        /// Unregisters a plant from lifecycle management.
        /// </summary>
        public void UnregisterPlant(string plantID, PlantRemovalReason reason = PlantRemovalReason.Other)
        {
            if (!IsInitialized)
            {
                Debug.LogError("[PlantLifecycleService] Cannot unregister plant: Service not initialized");
                return;
            }
            
            // Find plant in processing list
            var plant = _plantsToUpdate.FirstOrDefault(p => p.PlantID == plantID);
            if (plant == null)
            {
                Debug.LogWarning($"[PlantLifecycleService] Attempted to unregister unknown plant: {plantID}");
                return;
            }
            
            // Unsubscribe from events
            UnsubscribeFromPlantEvents(plant);
            
            // Remove from processing list
            _plantsToUpdate.Remove(plant);
            
            Debug.Log($"[PlantLifecycleService] Unregistered plant: {plantID} (Reason: {reason})");
        }
        
        /// <summary>
        /// Gets a plant instance by ID - delegates to CultivationManager.
        /// </summary>
        public PlantInstanceSO GetPlant(string plantID)
        {
            if (!IsInitialized)
            {
                Debug.LogError("[PlantLifecycleService] Cannot get plant: Service not initialized");
                return null;
            }
            
            return _cultivationManager?.GetPlant(plantID);
        }
        
        /// <summary>
        /// Gets all active plants - delegates to CultivationManager.
        /// </summary>
        public IEnumerable<PlantInstanceSO> GetAllPlants()
        {
            if (!IsInitialized)
            {
                Debug.LogError("[PlantLifecycleService] Cannot get plants: Service not initialized");
                return System.Linq.Enumerable.Empty<PlantInstanceSO>();
            }
            
            return _cultivationManager?.GetAllPlants() ?? System.Linq.Enumerable.Empty<PlantInstanceSO>();
        }
        
        /// <summary>
        /// Gets all plants in a specific growth stage - delegates to CultivationManager.
        /// </summary>
        public IEnumerable<PlantInstanceSO> GetPlantsInStage(PlantGrowthStage stage)
        {
            if (!IsInitialized)
            {
                Debug.LogError("[PlantLifecycleService] Cannot get plants by stage: Service not initialized");
                return System.Linq.Enumerable.Empty<PlantInstanceSO>();
            }
            
            return _cultivationManager?.GetPlantsByStage(stage) ?? System.Linq.Enumerable.Empty<PlantInstanceSO>();
        }
        
        /// <summary>
        /// Gets all plants ready for harvest - delegates to CultivationManager.
        /// </summary>
        public IEnumerable<PlantInstanceSO> GetHarvestablePlants()
        {
            return GetPlantsInStage(PlantGrowthStage.Harvest);
        }
        
        /// <summary>
        /// Gets all plants that need attention - delegates to CultivationManager.
        /// </summary>
        public IEnumerable<PlantInstanceSO> GetPlantsNeedingAttention()
        {
            if (!IsInitialized)
            {
                Debug.LogError("[PlantLifecycleService] Cannot get plants needing attention: Service not initialized");
                return System.Linq.Enumerable.Empty<PlantInstanceSO>();
            }
            
            return _cultivationManager?.GetPlantsNeedingAttention() ?? System.Linq.Enumerable.Empty<PlantInstanceSO>();
        }
        
        /// <summary>
        /// Gets all plants currently tracked for processing (PlantInstance objects).
        /// </summary>
        public IEnumerable<PlantInstance> GetTrackedPlants()
        {
            return _plantsToUpdate.AsReadOnly();
        }
        
        /// <summary>
        /// Gets a tracked plant instance by ID.
        /// </summary>
        public PlantInstance GetTrackedPlant(string plantID)
        {
            return _plantsToUpdate.FirstOrDefault(p => p.PlantID == plantID);
        }
        
        /// <summary>
        /// Validates plant health and performs basic maintenance checks.
        /// </summary>
        public void ValidatePlantHealth()
        {
            if (!IsInitialized) return;
            
            var plantsToRemove = new List<PlantInstance>();
            
            foreach (var plant in _plantsToUpdate)
            {
                if (plant == null || !plant.IsActive)
                {
                    plantsToRemove.Add(plant);
                    continue;
                }
                
                // Check for critical health issues
                if (plant.CurrentHealth <= 0f)
                {
                    Debug.LogWarning($"[PlantLifecycleService] Plant {plant.PlantID} has died (Health: {plant.CurrentHealth})");
                    plantsToRemove.Add(plant);
                }
            }
            
            // Remove inactive or dead plants
            foreach (var plant in plantsToRemove)
            {
                if (plant != null)
                {
                    UnregisterPlant(plant.PlantID, PlantRemovalReason.Died);
                }
                else
                {
                    _plantsToUpdate.Remove(plant);
                }
            }
            
            if (plantsToRemove.Count > 0)
            {
                Debug.Log($"[PlantLifecycleService] Removed {plantsToRemove.Count} inactive/dead plants during validation");
            }
        }
        
        /// <summary>
        /// Gets lifecycle statistics.
        /// </summary>
        public (int tracked, int total, int harvestable, int needingAttention) GetLifecycleStats()
        {
            if (!IsInitialized)
                return (0, 0, 0, 0);
            
            int tracked = _plantsToUpdate.Count;
            int total = _cultivationManager?.ActivePlantCount ?? 0;
            int harvestable = GetHarvestablePlants().Count();
            int needingAttention = GetPlantsNeedingAttention().Count();
            
            return (tracked, total, harvestable, needingAttention);
        }
        
        /// <summary>
        /// Update plant lifecycle for a single plant
        /// </summary>
        public void UpdatePlantLifecycle(PlantInstance plant, float deltaTime)
        {
            if (!IsInitialized || plant == null || !plant.IsActive)
                return;
            
            var startTime = Time.realtimeSinceStartup;
            
            // Update plant's internal lifecycle
            plant.UpdatePlant(deltaTime);
            
            // Check for stage progression
            var currentStage = plant.CurrentGrowthStage;
            bool canProgress = CanProgressToNextStage(plant);
            
            if (canProgress)
            {
                if (plant.AdvanceGrowthStage())
                {
                    OnPlantGrowthStageChanged(plant);
                }
            }
            
            // Update health tracking
            if (plant.CurrentHealth <= 10f && plant.CurrentHealth > 0f)
            {
                Debug.LogWarning($"[PlantLifecycleService] Plant {plant.PlantID} health is critical: {plant.CurrentHealth:F1}");
            }
            
            // Track processing time
            var processingTime = (Time.realtimeSinceStartup - startTime) * 1000f;
            
            if (_enableDetailedLogging && processingTime > 1f)
            {
                Debug.Log($"[PlantLifecycleService] Updated lifecycle for {plant.PlantID} in {processingTime:F2}ms");
            }
        }
        
        /// <summary>
        /// Get comprehensive lifecycle data for a plant
        /// </summary>
        public PlantLifecycleData GetPlantLifecycleData(PlantInstance plant)
        {
            if (!IsInitialized || plant == null)
            {
                return new PlantLifecycleData
                {
                    PlantID = plant?.PlantID ?? "Unknown",
                    CurrentStage = PlantGrowthStage.Seed,
                    DaysInCurrentStage = 0f,
                    TotalLifespanDays = 0f,
                    HealthStatus = "Unknown",
                    IsActive = false
                };
            }
            
            return new PlantLifecycleData
            {
                PlantID = plant.PlantID,
                CurrentStage = plant.CurrentGrowthStage,
                DaysInCurrentStage = CalculateDaysInCurrentStage(plant),
                TotalLifespanDays = plant.DaysSincePlanted,
                HealthStatus = GetHealthStatus(plant.CurrentHealth),
                IsActive = plant.IsActive,
                StressLevel = plant.StressLevel,
                NextStageETA = CalculateNextStageETA(plant),
                LifecycleProgress = CalculateLifecycleProgress(plant)
            };
        }
        
        private bool CanProgressToNextStage(PlantInstance plant)
        {
            // Basic progression logic - can be enhanced with more sophisticated requirements
            if (plant.CurrentHealth < 30f) return false; // Too unhealthy to progress
            if (plant.StressLevel > 80f) return false; // Too stressed to progress
            
            // Stage-specific requirements
            switch (plant.CurrentGrowthStage)
            {
                case PlantGrowthStage.Seed:
                    return plant.DaysSincePlanted >= 1f;
                case PlantGrowthStage.Germination:
                    return plant.DaysSincePlanted >= 3f;
                case PlantGrowthStage.Seedling:
                    return plant.DaysSincePlanted >= 7f && plant.CurrentHealth > 50f;
                case PlantGrowthStage.Vegetative:
                    return plant.DaysSincePlanted >= 21f;
                case PlantGrowthStage.PreFlowering:
                    return plant.DaysSincePlanted >= 7f;
                case PlantGrowthStage.Flowering:
                    return plant.DaysSincePlanted >= 49f;
                case PlantGrowthStage.Ripening:
                    return plant.DaysSincePlanted >= 7f;
                default:
                    return false;
            }
        }
        
        private float CalculateDaysInCurrentStage(PlantInstance plant)
        {
            // This is a simplified calculation - would need stage entry tracking for accuracy
            return Mathf.Max(0f, plant.DaysSincePlanted * 0.1f); // Rough approximation
        }
        
        private string GetHealthStatus(float health)
        {
            if (health > 80f) return "Excellent";
            if (health > 60f) return "Good";
            if (health > 40f) return "Fair";
            if (health > 20f) return "Poor";
            if (health > 0f) return "Critical";
            return "Dead";
        }
        
        private float CalculateNextStageETA(PlantInstance plant)
        {
            // Simplified ETA calculation based on current stage
            switch (plant.CurrentGrowthStage)
            {
                case PlantGrowthStage.Seed: return 1f - (plant.DaysSincePlanted % 1f);
                case PlantGrowthStage.Germination: return 3f - (plant.DaysSincePlanted % 3f);
                case PlantGrowthStage.Seedling: return 7f - (plant.DaysSincePlanted % 7f);
                case PlantGrowthStage.Vegetative: return 21f - (plant.DaysSincePlanted % 21f);
                case PlantGrowthStage.PreFlowering: return 7f - (plant.DaysSincePlanted % 7f);
                case PlantGrowthStage.Flowering: return 49f - (plant.DaysSincePlanted % 49f);
                case PlantGrowthStage.Ripening: return 7f - (plant.DaysSincePlanted % 7f);
                default: return 0f;
            }
        }
        
        private float CalculateLifecycleProgress(PlantInstance plant)
        {
            // Estimate overall lifecycle progress (0-1)
            float totalLifecycleDays = 95f; // Approximate total lifecycle
            return Mathf.Clamp01(plant.DaysSincePlanted / totalLifecycleDays);
        }
        
        /// <summary>
        /// Subscribes to plant events for lifecycle tracking.
        /// </summary>
        private void SubscribeToPlantEvents(PlantInstance plant)
        {
            plant.OnGrowthStageChanged += OnPlantGrowthStageChanged;
            plant.OnHealthChanged += OnPlantHealthChanged;
            plant.OnPlantDied += OnPlantDied;
        }
        
        /// <summary>
        /// Unsubscribes from plant events.
        /// </summary>
        private void UnsubscribeFromPlantEvents(PlantInstance plant)
        {
            plant.OnGrowthStageChanged -= OnPlantGrowthStageChanged;
            plant.OnHealthChanged -= OnPlantHealthChanged;
            plant.OnPlantDied -= OnPlantDied;
        }
        
        /// <summary>
        /// Handles plant growth stage changes.
        /// </summary>
        private void OnPlantGrowthStageChanged(PlantInstance plant)
        {
            Debug.Log($"[PlantLifecycleService] Plant {plant.PlantID} advanced to {plant.CurrentGrowthStage}");
            OnPlantStageChanged?.Invoke(plant);
        }
        
        /// <summary>
        /// Handles plant health changes.
        /// </summary>
        private void OnPlantHealthChanged(PlantInstance plant)
        {
            if (_enableDetailedLogging)
                Debug.Log($"[PlantLifecycleService] Plant {plant.PlantID} health changed to {plant.CurrentHealth:F2}");
            
            OnPlantHealthUpdated?.Invoke(plant);
        }
        
        /// <summary>
        /// Handles plant death events.
        /// </summary>
        private void OnPlantDied(PlantInstance plant)
        {
            Debug.Log($"[PlantLifecycleService] Plant {plant.PlantID} died (Health: {plant.CurrentHealth:F2})");
            UnregisterPlant(plant.PlantID, PlantRemovalReason.Died);
        }
    }
    
    /// <summary>
    /// Plant lifecycle data structure
    /// </summary>
    [System.Serializable]
    public class PlantLifecycleData
    {
        public string PlantID;
        public string PlantId;  // Alias for compatibility
        public string InitialStrain;
        public System.DateTime PlantedDate;
        public System.DateTime? RemovalDate;
        public PlantRemovalReason RemovalReason;
        public PlantGrowthStage CurrentStage;
        public PlantGrowthStage FinalStage;
        public Vector3 Position;
        public List<StageTransition> StageTransitions = new List<StageTransition>();
        public List<HealthEvent> HealthEvents = new List<HealthEvent>();
        public float FinalYield;
        public float FinalQuality;
        public float DaysInCurrentStage;
        public float TotalLifespanDays;
        public string HealthStatus;
        public bool IsActive;
        public float StressLevel;
        public float NextStageETA;
        public float LifecycleProgress;
        
        public float TotalGrowthDays => RemovalDate.HasValue 
            ? (float)(RemovalDate.Value - PlantedDate).TotalDays 
            : (float)(System.DateTime.Now - PlantedDate).TotalDays;
    }
    
    /// <summary>
    /// Records growth stage transitions
    /// </summary>
    [System.Serializable]
    public struct StageTransition
    {
        public PlantGrowthStage FromStage;
        public PlantGrowthStage ToStage;
        public System.DateTime TransitionDate;
        public float PlantAge;
    }
    
    /// <summary>
    /// Records significant health events
    /// </summary>
    [System.Serializable]
    public struct HealthEvent
    {
        public System.DateTime EventDate;
        public string EventType;
        public float HealthBefore;
        public float HealthAfter;
        public string Description;
    }
}