using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Data.Shared;
using ProjectChimera.Data.Genetics;
using ProjectChimera.Data.Cultivation;
using PlantGrowthStage = ProjectChimera.Data.Shared.PlantGrowthStage;

namespace ProjectChimera.Systems.Cultivation
{
    /// <summary>
    /// PC013-5c: Specialized service for cultivation plant registration and tracking
    /// Extracted from monolithic CultivationManager.cs to handle plant lifecycle,
    /// registration, statistics tracking, and plant state management.
    /// </summary>
    public class CultivationPlantTracker : MonoBehaviour, ITickable, ICultivationService
    {
        [Header("Plant Tracking Configuration")]
        [SerializeField] private int _maxTrackedPlants = 1000;
        [SerializeField] private bool _enableDetailedTracking = true;
        [SerializeField] private float _statisticsUpdateInterval = 60f; // 1 minute

        // Plant tracking data
        private Dictionary<string, PlantInstanceSO> _activePlants = new Dictionary<string, PlantInstanceSO>();
        private Dictionary<string, Vector3> _plantPositions = new Dictionary<string, Vector3>();
        private Dictionary<string, PlantLifecycleData> _plantLifecycleData = new Dictionary<string, PlantLifecycleData>();
        private Dictionary<PlantGrowthStage, List<string>> _plantsByStage = new Dictionary<PlantGrowthStage, List<string>>();

        // Statistics tracking
        private CultivationStatistics _statistics = new CultivationStatistics();
        private float _lastStatisticsUpdate;

        // Dependencies
        private CultivationZoneManager _zoneManager;

        public bool IsInitialized { get; private set; }

        // Properties
        public int ActivePlantCount => _activePlants.Count;
        public int MaxTrackedPlants => _maxTrackedPlants;
        public CultivationStatistics Statistics => _statistics;
        public bool DetailedTrackingEnabled
        {
            get => _enableDetailedTracking;
            set => _enableDetailedTracking = value;
        }

        // Events
        public System.Action<PlantInstanceSO> OnPlantRegistered;
        public System.Action<string, PlantInstanceSO> OnPlantRemoved;
        public System.Action<PlantInstanceSO, PlantGrowthStage, PlantGrowthStage> OnPlantStageChanged;
        public System.Action<PlantInstanceSO> OnPlantHealthCritical;
        public System.Action<CultivationStatistics> OnStatisticsUpdated;

        public void Initialize()
        {
            if (IsInitialized) return;

            ChimeraLogger.Log("[CultivationPlantTracker] Initializing plant tracking system...");

            // Initialize plant stage tracking
            foreach (PlantGrowthStage stage in System.Enum.GetValues(typeof(PlantGrowthStage)))
            {
                _plantsByStage[stage] = new List<string>();
            }

            // Get dependencies
            _zoneManager = ServiceContainerFactory.Instance?.TryResolve<CultivationZoneManager>();
            if (_zoneManager == null)
            {
                ChimeraLogger.LogWarning("[CultivationPlantTracker] CultivationZoneManager not found - zone integration will be limited");
            }

            _lastStatisticsUpdate = Time.time;

            IsInitialized = true;
            ChimeraLogger.Log($"[CultivationPlantTracker] Initialized. Max plants: {_maxTrackedPlants}, Detailed tracking: {_enableDetailedTracking}");
        }

        public void Shutdown()
        {
            ChimeraLogger.Log("[CultivationPlantTracker] Shutting down plant tracking...");

            // Save plant states before shutdown
            SaveAllPlantStates();

            _activePlants.Clear();
            _plantPositions.Clear();
            _plantLifecycleData.Clear();

            foreach (var stageList in _plantsByStage.Values)
            {
                stageList.Clear();
            }

            IsInitialized = false;
        }

            public void Tick(float deltaTime)
    {
            if (!IsInitialized) return;

            // Update statistics periodically
            if (Time.time - _lastStatisticsUpdate >= _statisticsUpdateInterval)
            {
                UpdateStatistics();
                _lastStatisticsUpdate = Time.time;

    }

            // Monitor plant health
            if (_enableDetailedTracking)
            {
                MonitorPlantHealth();
            }
        }

        /// <summary>
        /// Registers a new plant in the tracking system
        /// </summary>
        public bool RegisterPlant(PlantInstanceSO plant, Vector3 position, string zoneId = "default")
        {
            if (plant == null)
            {
                ChimeraLogger.LogWarning("[CultivationPlantTracker] Cannot register null plant");
                return false;
            }

            if (_activePlants.ContainsKey(plant.PlantID))
            {
                ChimeraLogger.LogWarning($"[CultivationPlantTracker] Plant {plant.PlantID} already registered");
                return false;
            }

            if (_activePlants.Count >= _maxTrackedPlants)
            {
                ChimeraLogger.LogWarning($"[CultivationPlantTracker] Maximum plant limit ({_maxTrackedPlants}) reached");
                return false;
            }

            // Register plant
            _activePlants[plant.PlantID] = plant;
            _plantPositions[plant.PlantID] = position;

            // Add to stage tracking
            AddPlantToStageTracking(plant.PlantID, (PlantGrowthStage)plant.CurrentGrowthStage);

            // Create lifecycle data
            if (_enableDetailedTracking)
            {
                _plantLifecycleData[plant.PlantID] = new PlantLifecycleData
                {
                    PlantId = plant.PlantID,
                    PlantedDate = System.DateTime.Now,
                    InitialStrain = plant.Strain?.name ?? "Unknown",
                    CurrentStage = (PlantGrowthStage)plant.CurrentGrowthStage,
                    Position = position
                };
            }

            // Add to zone
            if (_zoneManager != null)
            {
                _zoneManager.AddPlantToZone(plant.PlantID, zoneId);
            }

            // Update statistics
            _statistics.TotalPlantsGrown++;

            OnPlantRegistered?.Invoke(plant);
            ChimeraLogger.Log($"[CultivationPlantTracker] Registered plant {plant.PlantName} at {position}");

            return true;
        }

        /// <summary>
        /// Removes a plant from the tracking system
        /// </summary>
        public bool RemovePlant(string plantId, PlantRemovalReason reason = PlantRemovalReason.Harvested)
        {
            if (!_activePlants.TryGetValue(plantId, out PlantInstanceSO plant))
            {
                ChimeraLogger.LogWarning($"[CultivationPlantTracker] Plant {plantId} not found");
                return false;
            }

            // Remove from stage tracking
            RemovePlantFromStageTracking(plantId);

            // Update lifecycle data
            if (_enableDetailedTracking && _plantLifecycleData.ContainsKey(plantId))
            {
                var lifecycleData = _plantLifecycleData[plantId];
                lifecycleData.RemovalDate = System.DateTime.Now;
                lifecycleData.RemovalReason = reason;
                lifecycleData.FinalStage = (PlantGrowthStage)plant.CurrentGrowthStage;

                // Archive completed lifecycle data
                ArchiveLifecycleData(lifecycleData);
                _plantLifecycleData.Remove(plantId);
            }

            // Remove from zone
            if (_zoneManager != null)
            {
                _zoneManager.RemovePlantFromAllZones(plantId);
            }

            // Update statistics based on removal reason
            UpdateStatisticsForRemoval(plant, reason);

            // Remove from collections
            _activePlants.Remove(plantId);
            _plantPositions.Remove(plantId);

            OnPlantRemoved?.Invoke(plantId, plant);
            ChimeraLogger.Log($"[CultivationPlantTracker] Removed plant {plant.PlantName} (Reason: {reason})");

            return true;
        }

        /// <summary>
        /// Updates plant stage tracking when a plant changes growth stage
        /// </summary>
        public void UpdatePlantStage(string plantId, PlantGrowthStage newStage)
        {
            if (!_activePlants.TryGetValue(plantId, out PlantInstanceSO plant))
            {
                ChimeraLogger.LogWarning($"[CultivationPlantTracker] Plant {plantId} not found for stage update");
                return;
            }

            var oldStage = (PlantGrowthStage)plant.CurrentGrowthStage;

            if (oldStage == newStage) return; // No change

            // Update stage tracking
            RemovePlantFromStageTracking(plantId, oldStage);
            AddPlantToStageTracking(plantId, newStage);

            // Update lifecycle data
            if (_enableDetailedTracking && _plantLifecycleData.ContainsKey(plantId))
            {
                var lifecycleData = _plantLifecycleData[plantId];
                lifecycleData.StageTransitions.Add(new StageTransition
                {
                    FromStage = oldStage,
                    ToStage = newStage,
                    TransitionDate = System.DateTime.Now,
                    PlantAge = plant.AgeInDays
                });
                lifecycleData.CurrentStage = newStage;
            }

            OnPlantStageChanged?.Invoke(plant, oldStage, newStage);
            ChimeraLogger.Log($"[CultivationPlantTracker] Plant {plant.PlantName} stage changed: {oldStage} -> {newStage}");
        }

        /// <summary>
        /// Gets a specific plant by ID
        /// </summary>
        public PlantInstanceSO GetPlant(string plantId)
        {
            return _activePlants.TryGetValue(plantId, out PlantInstanceSO plant) ? plant : null;
        }

        /// <summary>
        /// Gets all active plants
        /// </summary>
        public IEnumerable<PlantInstanceSO> GetAllPlants()
        {
            return _activePlants.Values;
        }

        /// <summary>
        /// Gets plants by growth stage
        /// </summary>
        public IEnumerable<PlantInstanceSO> GetPlantsByStage(PlantGrowthStage stage)
        {
            if (!_plantsByStage.TryGetValue(stage, out List<string> plantIds))
                return System.Linq.Enumerable.Empty<PlantInstanceSO>();

            return plantIds.Select(id => _activePlants.TryGetValue(id, out PlantInstanceSO plant) ? plant : null)
                          .Where(plant => plant != null);
        }

        /// <summary>
        /// Gets plants that need attention (low health, pests, etc.)
        /// </summary>
        public IEnumerable<PlantInstanceSO> GetPlantsNeedingAttention()
        {
            return _activePlants.Values.Where(plant =>
                plant.CurrentHealth < 0.7f ||
                plant.StressLevel > 0.6f ||
                plant.WaterLevel < 0.3f ||
                plant.NutrientLevel < 0.3f
            );
        }

        /// <summary>
        /// Gets plants ready for harvest
        /// </summary>
        public IEnumerable<PlantInstanceSO> GetPlantsReadyForHarvest()
        {
            return GetPlantsByStage(PlantGrowthStage.Harvestable);
        }

        /// <summary>
        /// Gets plant position
        /// </summary>
        public Vector3 GetPlantPosition(string plantId)
        {
            return _plantPositions.TryGetValue(plantId, out Vector3 position) ? position : Vector3.zero;
        }

        /// <summary>
        /// Updates plant position
        /// </summary>
        public void UpdatePlantPosition(string plantId, Vector3 newPosition)
        {
            if (_activePlants.ContainsKey(plantId))
            {
                _plantPositions[plantId] = newPosition;

                if (_enableDetailedTracking && _plantLifecycleData.ContainsKey(plantId))
                {
                    _plantLifecycleData[plantId].Position = newPosition;
                }
            }
        }

        /// <summary>
        /// Gets lifecycle data for a plant
        /// </summary>
        public PlantLifecycleData GetPlantLifecycleData(string plantId)
        {
            return _plantLifecycleData.TryGetValue(plantId, out PlantLifecycleData data) ? data : null;
        }

        /// <summary>
        /// Gets plants by strain
        /// </summary>
        public IEnumerable<PlantInstanceSO> GetPlantsByStrain(object strain)
        {
            return _activePlants.Values.Where(plant => plant.Strain == strain);
        }

        /// <summary>
        /// Gets plants in a specific zone
        /// </summary>
        public IEnumerable<PlantInstanceSO> GetPlantsInZone(string zoneId)
        {
            if (_zoneManager == null) return System.Linq.Enumerable.Empty<PlantInstanceSO>();

            var plantIds = _zoneManager.GetPlantsInZone(zoneId);
            return plantIds.Select(id => GetPlant(id)).Where(plant => plant != null);
        }

        #region Private Methods

        private void AddPlantToStageTracking(string plantId, PlantGrowthStage stage)
        {
            if (_plantsByStage.ContainsKey(stage))
            {
                _plantsByStage[stage].Add(plantId);
            }
        }

        private void RemovePlantFromStageTracking(string plantId, PlantGrowthStage? specificStage = null)
        {
            if (specificStage.HasValue)
            {
                if (_plantsByStage.ContainsKey(specificStage.Value))
                {
                    _plantsByStage[specificStage.Value].Remove(plantId);
                }
            }
            else
            {
                // Remove from all stages
                foreach (var stageList in _plantsByStage.Values)
                {
                    stageList.Remove(plantId);
                }
            }
        }

        private void UpdateStatistics()
        {
            _statistics.ActivePlantCount = _activePlants.Count;

            if (_activePlants.Count > 0)
            {
                _statistics.AverageHealth = _activePlants.Values.Average(p => p.CurrentHealth);
                _statistics.AverageAge = _activePlants.Values.Average(p => p.AgeInDays);
            }
            else
            {
                _statistics.AverageHealth = 0f;
                _statistics.AverageAge = 0f;
            }

            // Update stage distribution
            _statistics.StageDistribution.Clear();
            foreach (var kvp in _plantsByStage)
            {
                _statistics.StageDistribution[kvp.Key] = kvp.Value.Count;
            }

            _statistics.LastUpdated = System.DateTime.Now;
            OnStatisticsUpdated?.Invoke(_statistics);
        }

        private void MonitorPlantHealth()
        {
            foreach (var plant in _activePlants.Values)
            {
                if (plant.CurrentHealth < 0.3f || plant.StressLevel > 0.8f)
                {
                    OnPlantHealthCritical?.Invoke(plant);
                }
            }
        }

        private void UpdateStatisticsForRemoval(PlantInstanceSO plant, PlantRemovalReason reason)
        {
            switch (reason)
            {
                case PlantRemovalReason.Harvested:
                    _statistics.TotalPlantsHarvested++;
                    // Add yield data if available
                    break;
                case PlantRemovalReason.Died:
                    _statistics.TotalPlantDeaths++;
                    break;
                case PlantRemovalReason.Removed:
                    _statistics.TotalPlantsRemoved++;
                    break;
            }
        }

        private void ArchiveLifecycleData(PlantLifecycleData data)
        {
            // In a full implementation, this would save to a database or file
            ChimeraLogger.Log($"[CultivationPlantTracker] Archived lifecycle data for plant {data.PlantId}");
        }

        private void SaveAllPlantStates()
        {
            ChimeraLogger.Log($"[CultivationPlantTracker] Saving states for {_activePlants.Count} plants");
            // Implementation would save plant states to persistent storage
        }

        #endregion

    // ITickable implementation
    public int Priority => 0;
    public bool Enabled => enabled && gameObject.activeInHierarchy;

    public virtual void OnRegistered()
    {
        // Override in derived classes if needed
    }

    public virtual void OnUnregistered()
    {
        // Override in derived classes if needed
    }

}


    /// <summary>
    /// Comprehensive cultivation statistics
    /// </summary>
    [System.Serializable]
    public class CultivationStatistics
    {
        public int ActivePlantCount;
        public int TotalPlantsGrown;
        public int TotalPlantsHarvested;
        public int TotalPlantDeaths;
        public int TotalPlantsRemoved;
        public float TotalYieldHarvested;
        public float AverageHealth;
        public float AverageAge;
        public Dictionary<PlantGrowthStage, int> StageDistribution = new Dictionary<PlantGrowthStage, int>();
        public System.DateTime LastUpdated;

        public float SuccessRate => TotalPlantsGrown > 0 ? (float)TotalPlantsHarvested / TotalPlantsGrown : 0f;
        public float MortalityRate => TotalPlantsGrown > 0 ? (float)TotalPlantDeaths / TotalPlantsGrown : 0f;
    }
}
