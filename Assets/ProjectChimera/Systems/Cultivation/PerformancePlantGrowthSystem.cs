using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;
using ProjectChimera.Core;
using ProjectChimera.Data.Shared;
using ProjectChimera.Data.Cultivation.Plant;
using ProjectChimera.Systems.Cultivation.Jobs;

namespace ProjectChimera.Systems.Cultivation
{
    /// <summary>
    /// PERFORMANCE: High-performance plant growth system using Unity Job System and Burst compilation
    /// Replaces traditional Update() loops with efficient parallel processing
    /// Week 9 Day 1-3: Jobs System & Performance Foundations
    /// </summary>
    public class PerformancePlantGrowthSystem : MonoBehaviour, ITickable
    {
        [Header("Performance Settings")]
        [SerializeField] private bool _useJobSystem = true;
        [SerializeField] private bool _enablePerformanceMetrics = true;
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private int _maxPlantsPerUpdate = 100;

        [Header("Growth Settings")]
        [SerializeField] private float _growthTickRate = 0.1f; // 10 times per second
        [SerializeField] private float _environmentalUpdateRate = 1.0f; // Once per second
        [SerializeField] private bool _enableStageTransitions = true;

        // Job system integration
        private PlantJobSystemIntegration _jobIntegration;

        // Plant management
        private readonly Dictionary<string, PlantInstance> _activePlants = new Dictionary<string, PlantInstance>();
        private readonly Dictionary<string, GameObject> _plantGameObjects = new Dictionary<string, GameObject>();

        // Timing
        private float _lastGrowthTick = 0f;
        private float _lastEnvironmentalUpdate = 0f;

        // Performance tracking
        private float _updateTimeAccumulator = 0f;
        private int _updateSampleCount = 0;
        private float _averageUpdateTime = 0f;

        // Events
        public event System.Action<PlantInstance, PlantGrowthStage, PlantGrowthStage> OnPlantStageChanged;
        public event System.Action<PlantInstance, float> OnPlantGrowthUpdated;
        public event System.Action<PlantInstance, float> OnPlantHealthChanged;

        public bool IsInitialized { get; private set; }
        public int ActivePlantCount => _activePlants.Count;

        // ITickable implementation
        public int TickPriority => 90; // High priority for plant growth
        public bool IsTickable => IsInitialized && enabled && gameObject.activeInHierarchy;

        /// <summary>
        /// Initialize the performance plant growth system
        /// </summary>
        public void Initialize()
        {
            if (IsInitialized) return;

            if (_useJobSystem)
            {
                InitializeJobSystem();
            }

            // Register with UpdateOrchestrator
            var updateOrchestrator = ServiceContainerFactory.Instance?.TryResolve<IUpdateOrchestrator>();
            updateOrchestrator?.RegisterTickable(this);

            IsInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("CULTIVATION", "PerformancePlantGrowthSystem initialized", this);
            }
        }

        /// <summary>
        /// ITickable implementation - high-performance growth updates
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (!IsInitialized) return;

            float startTime = Time.realtimeSinceStartup;

            // Check if it's time for a growth tick
            if (Time.time - _lastGrowthTick >= _growthTickRate)
            {
                ProcessGrowthTick(deltaTime);
                _lastGrowthTick = Time.time;
            }

            // Check if it's time for environmental updates
            if (Time.time - _lastEnvironmentalUpdate >= _environmentalUpdateRate)
            {
                ProcessEnvironmentalUpdates();
                _lastEnvironmentalUpdate = Time.time;
            }

            // Update performance metrics
            if (_enablePerformanceMetrics)
            {
                UpdatePerformanceMetrics(Time.realtimeSinceStartup - startTime);
            }
        }

        /// <summary>
        /// Add a plant to the performance growth system
        /// </summary>
        public async Task<bool> AddPlant(PlantInstance plantInstance, GameObject plantGameObject = null)
        {
            if (plantInstance == null || string.IsNullOrEmpty(plantInstance.PlantId)) return false;

            string plantId = plantInstance.PlantId;

            // Add to local tracking
            _activePlants[plantId] = plantInstance;
            if (plantGameObject != null)
            {
                _plantGameObjects[plantId] = plantGameObject;
            }

            // Register with job system if enabled
            bool jobSystemSuccess = true;
            if (_useJobSystem && _jobIntegration != null)
            {
                jobSystemSuccess = await _jobIntegration.RegisterPlant(plantInstance, plantGameObject);
            }

            if (_enableLogging)
            {
                ChimeraLogger.Log("CULTIVATION", $"Added plant {plantId}", this);
            }

            return jobSystemSuccess;
        }

        /// <summary>
        /// Remove a plant from the performance growth system
        /// </summary>
        public bool RemovePlant(string plantId)
        {
            if (string.IsNullOrEmpty(plantId)) return false;

            // Remove from local tracking
            bool removed = _activePlants.Remove(plantId);
            _plantGameObjects.Remove(plantId);

            // Unregister from job system if enabled
            if (_useJobSystem && _jobIntegration != null)
            {
                _jobIntegration.UnregisterPlant(plantId);
            }

            if (_enableLogging && removed)
            {
                ChimeraLogger.Log("CULTIVATION", $"Removed plant {plantId}", this);
            }

            return removed;
        }

        /// <summary>
        /// Update environmental conditions for a plant
        /// </summary>
        public bool UpdatePlantEnvironment(string plantId, EnvironmentalConditions conditions)
        {
            if (!_activePlants.ContainsKey(plantId)) return false;

            // Update job system if enabled
            if (_useJobSystem && _jobIntegration != null)
            {
                return _jobIntegration.UpdatePlantEnvironment(plantId, conditions);
            }

            // Fallback to direct update
            var plant = _activePlants[plantId];
            plant.Temperature = conditions.Temperature;
            plant.Humidity = conditions.Humidity;
            plant.LightIntensity = conditions.LightIntensity;
            plant.CO2Level = conditions.CO2Level;

            return true;
        }

        /// <summary>
        /// Get plant by ID
        /// </summary>
        public PlantInstance GetPlant(string plantId)
        {
            return _activePlants.TryGetValue(plantId, out var plant) ? plant : null;
        }

        /// <summary>
        /// Get all active plants
        /// </summary>
        public IEnumerable<PlantInstance> GetAllPlants()
        {
            return _activePlants.Values;
        }

        /// <summary>
        /// Get performance statistics
        /// </summary>
        public PlantGrowthSystemStats GetPerformanceStats()
        {
            var jobSystemStats = _jobIntegration?.GetPerformanceStats() ?? default;

            return new PlantGrowthSystemStats
            {
                ActivePlants = _activePlants.Count,
                UseJobSystem = _useJobSystem,
                AverageUpdateTime = _averageUpdateTime,
                GrowthTickRate = _growthTickRate,
                EnvironmentalUpdateRate = _environmentalUpdateRate,
                JobSystemStats = jobSystemStats
            };
        }

        #region Private Methods

        /// <summary>
        /// Initialize job system components
        /// </summary>
        private void InitializeJobSystem()
        {
            var integrationGO = new GameObject("PlantJobSystemIntegration");
            integrationGO.transform.SetParent(transform);
            _jobIntegration = integrationGO.AddComponent<PlantJobSystemIntegration>();
            _jobIntegration.Initialize();
        }

        /// <summary>
        /// Process a growth tick using job system or fallback
        /// </summary>
        private async void ProcessGrowthTick(float deltaTime)
        {
            if (_useJobSystem && _jobIntegration != null)
            {
                // Sync data back from job system
                await _jobIntegration.SyncPlantsFromJobSystem();

                // Process stage transitions and events
                ProcessPlantEvents();
            }
            else
            {
                // Fallback to traditional processing
                ProcessGrowthTraditional(deltaTime);
            }
        }

        /// <summary>
        /// Process environmental updates
        /// </summary>
        private void ProcessEnvironmentalUpdates()
        {
            // Update environmental conditions for all plants
            // This could be optimized with spatial queries or zone-based updates

            foreach (var kvp in _activePlants)
            {
                var plant = kvp.Value;
                var gameObject = _plantGameObjects.TryGetValue(kvp.Key, out var go) ? go : null;

                if (gameObject != null)
                {
                    // Get environmental data from the plant's position
                    var conditions = GetEnvironmentalConditionsAt(gameObject.transform.position);
                    UpdatePlantEnvironment(kvp.Key, conditions);
                }
            }
        }

        /// <summary>
        /// Process plant events (stage changes, etc.)
        /// </summary>
        private void ProcessPlantEvents()
        {
            foreach (var kvp in _activePlants)
            {
                var plant = kvp.Value;
                var previousStage = plant.GrowthStage; // Store for comparison

                // Check for stage transitions
                if (_enableStageTransitions)
                {
                    var newStage = CalculateNewGrowthStage(plant);
                    if (newStage != previousStage)
                    {
                        plant.GrowthStage = newStage;
                        OnPlantStageChanged?.Invoke(plant, previousStage, newStage);
                    }
                }

                // Fire growth and health events
                OnPlantGrowthUpdated?.Invoke(plant, plant.Height);
                OnPlantHealthChanged?.Invoke(plant, plant.Health);
            }
        }

        /// <summary>
        /// Traditional growth processing fallback
        /// </summary>
        private void ProcessGrowthTraditional(float deltaTime)
        {
            foreach (var kvp in _activePlants)
            {
                var plant = kvp.Value;

                // Simple growth calculation
                float growthRate = CalculateGrowthRate(plant);
                plant.Height += growthRate * deltaTime;
                plant.Age += deltaTime / 86400f; // Convert seconds to days

                // Simple health calculation
                plant.Health = Mathf.Clamp01(plant.Health + (0.1f - plant.Stress) * deltaTime);
            }
        }

        /// <summary>
        /// Calculate growth rate for traditional processing
        /// </summary>
        private float CalculateGrowthRate(PlantInstance plant)
        {
            float baseRate = 0.1f; // cm per second
            float healthModifier = Mathf.Lerp(0.1f, 1.2f, plant.Health);
            float stageModifier = plant.GrowthStage switch
            {
                PlantGrowthStage.Seedling => 0.5f,
                PlantGrowthStage.Vegetative => 1.0f,
                PlantGrowthStage.Flowering => 0.3f,
                _ => 0.1f
            };

            return baseRate * healthModifier * stageModifier;
        }

        /// <summary>
        /// Calculate new growth stage for a plant
        /// </summary>
        private PlantGrowthStage CalculateNewGrowthStage(PlantInstance plant)
        {
            switch (plant.GrowthStage)
            {
                case PlantGrowthStage.Seedling:
                    return plant.Age >= 14f ? PlantGrowthStage.Vegetative : PlantGrowthStage.Seedling;
                case PlantGrowthStage.Vegetative:
                    return plant.Age >= 44f ? PlantGrowthStage.Flowering : PlantGrowthStage.Vegetative;
                case PlantGrowthStage.Flowering:
                    return PlantGrowthStage.Flowering; // Stay in flowering
                default:
                    return plant.GrowthStage;
            }
        }

        /// <summary>
        /// Get environmental conditions at a specific position
        /// </summary>
        private EnvironmentalConditions GetEnvironmentalConditionsAt(Vector3 position)
        {
            // This would typically query an environmental manager or zone system
            // For now, return default conditions
            return new EnvironmentalConditions
            {
                Temperature = 24f,
                Humidity = 60f,
                LightIntensity = 100f,
                CO2Level = 400f
            };
        }

        /// <summary>
        /// Update performance metrics
        /// </summary>
        private void UpdatePerformanceMetrics(float updateTime)
        {
            _updateTimeAccumulator += updateTime;
            _updateSampleCount++;

            if (_updateSampleCount >= 60) // Average over 60 samples
            {
                _averageUpdateTime = _updateTimeAccumulator / _updateSampleCount;
                _updateTimeAccumulator = 0f;
                _updateSampleCount = 0;

                if (_enableLogging && _averageUpdateTime > 0.01f) // Log if over 10ms
                {
                    ChimeraLogger.LogWarning("PERFORMANCE", $"Avg update time {_averageUpdateTime * 1000f:F2}ms over last 60 ticks", this);
                }
            }
        }

        #endregion

        private void OnDestroy()
        {
            if (_jobIntegration != null)
            {
                Destroy(_jobIntegration.gameObject);
            }
        }
    }

    /// <summary>
    /// Performance statistics for the plant growth system
    /// </summary>
    [System.Serializable]
    public struct PlantGrowthSystemStats
    {
        public int ActivePlants;
        public bool UseJobSystem;
        public float AverageUpdateTime;
        public float GrowthTickRate;
        public float EnvironmentalUpdateRate;
        public PlantJobSystemStats JobSystemStats;
    }
}
