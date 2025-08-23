using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Core.DependencyInjection;
using ProjectChimera.Core.Events;
using ProjectChimera.Data.Cultivation;
using PlantGrowthStage = ProjectChimera.Data.Shared.PlantGrowthStage;
using ProjectChimera.Data.Genetics;
using ProjectChimera.Data.Environment;

namespace ProjectChimera.Systems.Cultivation
{
    /// <summary>
    /// PC-013-2: Plant Lifecycle Manager - Handles plant creation, tracking, and removal
    /// Extracted from monolithic CultivationManager for Single Responsibility Principle
    /// </summary>
    public class PlantLifecycleManager : IPlantLifecycleManager
    {
        [Header("Plant Lifecycle Configuration")]
        [SerializeField] private int _maxPlantsPerGrow = 50;
        
        [Header("Events")]
        [SerializeField] private GameEventSO<PlantInstanceSO> _onPlantPlanted;
        [SerializeField] private GameEventSO<PlantInstanceSO> _onPlantHarvested;
        [SerializeField] private GameEventSO<PlantInstanceSO> _onPlantStageChanged;
        [SerializeField] private GameEventSO<PlantInstanceSO> _onPlantHealthCritical;
        
        // Runtime data
        private Dictionary<string, PlantInstanceSO> _activePlants = new Dictionary<string, PlantInstanceSO>();
        private Dictionary<string, Vector3> _plantPositions = new Dictionary<string, Vector3>();
        
        // Statistics
        private int _totalPlantsGrown = 0;
        private int _totalPlantsHarvested = 0;
        private float _totalYieldHarvested = 0f;
        
        // Dependencies
        private IEnvironmentalManager _environmentalManager;
        private IHarvestManager _harvestManager;
        
        public bool IsInitialized { get; private set; }
        
        // Properties
        public int ActivePlantCount => _activePlants.Count;
        public int TotalPlantsGrown => _totalPlantsGrown;
        public int TotalPlantsHarvested => _totalPlantsHarvested;
        public float TotalYieldHarvested => _totalYieldHarvested;
        
        public PlantLifecycleManager(IEnvironmentalManager environmentalManager, IHarvestManager harvestManager)
        {
            _environmentalManager = environmentalManager;
            _harvestManager = harvestManager;
        }
        
        /// <summary>
        /// Sets dependencies for PlantLifecycleManager after creation to resolve circular dependency issues.
        /// Used when PlantLifecycleManager needs to be created before its dependencies are available.
        /// </summary>
        /// <param name="environmentalManager">The environmental manager dependency</param>
        /// <param name="harvestManager">The harvest manager dependency</param>
        public void SetDependencies(IEnvironmentalManager environmentalManager, IHarvestManager harvestManager)
        {
            _environmentalManager = environmentalManager ?? throw new System.ArgumentNullException(nameof(environmentalManager));
            _harvestManager = harvestManager ?? throw new System.ArgumentNullException(nameof(harvestManager));
            
            Debug.Log("[PlantLifecycleManager] Dependencies successfully injected via SetDependencies method");
        }
        
        public void Initialize()
        {
            if (IsInitialized) return;
            
            Debug.Log("[PlantLifecycleManager] Initializing plant lifecycle management...");
            
            // Register with ServiceLocator for dependency injection
            try
            {
                var serviceLocator = ServiceLocator.Instance;
                serviceLocator.RegisterSingleton<IPlantLifecycleManager>(this);
                Debug.Log("[PlantLifecycleManager] Registered with ServiceLocator");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[PlantLifecycleManager] Failed to register with ServiceLocator: {ex.Message}");
            }
            
            _activePlants.Clear();
            _plantPositions.Clear();
            
            IsInitialized = true;
            Debug.Log($"[PlantLifecycleManager] Initialized. Max plants: {_maxPlantsPerGrow}");
        }
        
        public void Shutdown()
        {
            if (!IsInitialized) return;
            
            Debug.Log("[PlantLifecycleManager] Shutting down plant lifecycle management...");
            
            // Save current plant states before shutdown
            SaveAllPlantStates();
            
            _activePlants.Clear();
            _plantPositions.Clear();
            
            IsInitialized = false;
        }
        
        /// <summary>
        /// Plants a new plant instance in the cultivation system.
        /// </summary>
        public PlantInstanceSO PlantSeed(string plantName, PlantStrainSO strain, GenotypeDataSO genotype, Vector3 position, string zoneId = "default")
        {
            if (!IsInitialized)
            {
                Debug.LogError("[PlantLifecycleManager] Cannot plant seed: Manager not initialized.");
                return null;
            }
            
            if (_activePlants.Count >= _maxPlantsPerGrow)
            {
                Debug.LogWarning($"[PlantLifecycleManager] Cannot plant '{plantName}': Maximum plant limit ({_maxPlantsPerGrow}) reached.");
                return null;
            }
            
            if (strain == null)
            {
                Debug.LogError($"[PlantLifecycleManager] Cannot plant '{plantName}': No strain specified.");
                return null;
            }
            
            // Generate unique plant ID
            string plantId = GenerateUniquePlantId();
            
            // Create new plant instance
            PlantInstanceSO newPlant = ScriptableObject.CreateInstance<PlantInstanceSO>();
            newPlant.name = $"Plant_{plantId}_{plantName}";
            newPlant.InitializePlant(plantId, plantName, strain, genotype, position);
            
            // Add to active plants
            _activePlants[plantId] = newPlant;
            _plantPositions[plantId] = position;
            
            _totalPlantsGrown++;
            
            // Raise planting event
            _onPlantPlanted?.Raise(newPlant);
            
            Debug.Log($"[PlantLifecycleManager] Planted '{plantName}' (ID: {plantId}) at position {position}");
            
            return newPlant;
        }
        
        /// <summary>
        /// Removes a plant from the cultivation system (harvest, death, etc.).
        /// </summary>
        public bool RemovePlant(string plantId, bool isHarvest = false)
        {
            if (!IsInitialized)
            {
                Debug.LogError("[PlantLifecycleManager] Cannot remove plant: Manager not initialized.");
                return false;
            }
            
            if (!_activePlants.ContainsKey(plantId))
            {
                Debug.LogWarning($"[PlantLifecycleManager] Cannot remove plant: Plant ID '{plantId}' not found.");
                return false;
            }
            
            PlantInstanceSO plant = _activePlants[plantId];
            
            if (isHarvest)
            {
                _harvestManager?.ProcessHarvest(plant);
                _totalPlantsHarvested++;
            }
            
            // Clean up
            _activePlants.Remove(plantId);
            _plantPositions.Remove(plantId);
            
            // Destroy the ScriptableObject instance
            if (Application.isPlaying)
            {
                Object.Destroy(plant);
            }
            else
            {
                Object.DestroyImmediate(plant);
            }
            
            Debug.Log($"[PlantLifecycleManager] Removed plant '{plantId}' (Harvest: {isHarvest})");
            
            return true;
        }
        
        /// <summary>
        /// Gets a plant instance by its ID.
        /// </summary>
        public PlantInstanceSO GetPlant(string plantId)
        {
            if (!IsInitialized) return null;
            
            _activePlants.TryGetValue(plantId, out PlantInstanceSO plant);
            return plant;
        }
        
        /// <summary>
        /// Gets all active plants.
        /// </summary>
        public IEnumerable<PlantInstanceSO> GetAllPlants()
        {
            return IsInitialized ? _activePlants.Values : System.Linq.Enumerable.Empty<PlantInstanceSO>();
        }
        
        /// <summary>
        /// Gets all plants in a specific growth stage.
        /// </summary>
        public IEnumerable<PlantInstanceSO> GetPlantsByStage(PlantGrowthStage stage)
        {
            if (!IsInitialized) return System.Linq.Enumerable.Empty<PlantInstanceSO>();
            
            return _activePlants.Values.Where(plant => plant.CurrentGrowthStage == stage);
        }
        
        /// <summary>
        /// Gets all plants that need attention (low health, resources, etc.).
        /// </summary>
        public IEnumerable<PlantInstanceSO> GetPlantsNeedingAttention()
        {
            if (!IsInitialized) return System.Linq.Enumerable.Empty<PlantInstanceSO>();
            
            return _activePlants.Values.Where(plant => 
                plant.OverallHealth < 0.5f || 
                plant.WaterLevel < 0.3f || 
                plant.NutrientLevel < 0.3f ||
                plant.StressLevel > 0.7f
            );
        }
        
        /// <summary>
        /// Triggers stage change event for a plant
        /// </summary>
        public void TriggerStageChangeEvent(PlantInstanceSO plant)
        {
            _onPlantStageChanged?.Raise(plant);
        }
        
        /// <summary>
        /// Triggers health critical event for a plant
        /// </summary>
        public void TriggerHealthCriticalEvent(PlantInstanceSO plant)
        {
            _onPlantHealthCritical?.Raise(plant);
        }
        
        /// <summary>
        /// Gets the position of a plant
        /// </summary>
        public Vector3 GetPlantPosition(string plantId)
        {
            return _plantPositions.TryGetValue(plantId, out Vector3 position) ? position : Vector3.zero;
        }
        
        /// <summary>
        /// Updates the yield statistics when a plant is harvested
        /// </summary>
        public void UpdateYieldStatistics(float yieldAmount)
        {
            _totalYieldHarvested += yieldAmount;
        }
        
        private string GenerateUniquePlantId()
        {
            string baseId;
            int counter = 1;
            
            do
            {
                baseId = $"plant_{System.DateTime.Now:yyyyMMdd}_{counter:D3}";
                counter++;
            }
            while (_activePlants.ContainsKey(baseId));
            
            return baseId;
        }
        
        private void SaveAllPlantStates()
        {
            // This would typically save to persistent storage
            // For now, just log the save operation
            Debug.Log($"[PlantLifecycleManager] Saving states for {_activePlants.Count} plants...");
        }
    }
}