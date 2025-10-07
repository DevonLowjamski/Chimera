using UnityEngine;
using ProjectChimera.Core.Pooling;
using ProjectChimera.Systems.Cultivation.Pooling;
using ProjectChimera.Data.Cultivation.Plant;
using System.Collections.Generic;
using ProjectChimera.Core;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;

namespace ProjectChimera.Systems.Cultivation
{
    /// <summary>
    /// PERFORMANCE: Integration service connecting cultivation system with object pooling
    /// Replaces direct GameObject instantiation with pooled objects for better performance
    /// Week 9 Day 4-5: Object Pooling System Implementation
    /// </summary>
    public class CultivationPoolingIntegration : MonoBehaviour, ITickable
    {
        [Header("Pooling Integration Settings")]
        [SerializeField] private bool _enablePooling = true;
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private float _poolCleanupInterval = 60f;

        // Pool components
        private PlantObjectPool _plantPool;
        private PoolPerformanceMonitor _performanceMonitor;

        // Plant tracking
        private readonly Dictionary<string, PlantInstanceComponent> _activePlants = new Dictionary<string, PlantInstanceComponent>();
        private readonly Dictionary<string, int> _plantPrefabMapping = new Dictionary<string, int>();

        // Performance tracking
        private float _lastCleanupTime;
        private int _totalPlantsSpawned = 0;
        private int _totalPlantsRecycled = 0;

        public bool IsInitialized { get; private set; }

        // ITickable implementation
        public int TickPriority => 25; // Medium priority for pooling management
        public bool IsTickable => IsInitialized && enabled && gameObject.activeInHierarchy;

        /// <summary>
        /// Initialize pooling integration
        /// </summary>
        public void Initialize()
        {
            if (IsInitialized) return;

            InitializePoolComponents();
            RegisterWithServices();

            IsInitialized = true;
            _lastCleanupTime = Time.time;

            if (_enableLogging)
            {
                ChimeraLogger.Log("CULTIVATION", "CultivationPoolingIntegration initialized", this);
            }
        }

        /// <summary>
        /// Spawn plant using pooling system
        /// </summary>
        public PlantInstanceComponent SpawnPlant(PlantInstance plantData, Vector3 position, string strainName = null)
        {
            if (!IsInitialized || !_enablePooling)
            {
                return SpawnPlantDirect(plantData, position);
            }

            var startTime = Time.realtimeSinceStartup;

            // Get plant from pool
            PlantInstanceComponent plantComponent;
            if (!string.IsNullOrEmpty(strainName))
            {
                plantComponent = _plantPool.GetPlantByStrain(strainName, plantData);
            }
            else
            {
                int prefabIndex = GetPrefabIndexForPlant(plantData);
                plantComponent = _plantPool.GetPlant(prefabIndex, plantData);
            }

            if (plantComponent != null)
            {
                // Set position
                plantComponent.transform.position = position;

                // Track active plant
                _activePlants[plantData.PlantId] = plantComponent;
                _totalPlantsSpawned++;

                // Record performance
                var spawnTime = Time.realtimeSinceStartup - startTime;
                _performanceMonitor?.RecordPoolOperation("PlantPool", PoolOperationType.Get, spawnTime);

                if (_enableLogging)
                {
                    ChimeraLogger.Log("CULTIVATION", $"Spawned pooled plant {plantData.PlantId} at {position}", this);
                }

                return plantComponent;
            }
            else
            {
                // Fallback to direct instantiation
                if (_enableLogging)
                {
                    ChimeraLogger.LogWarning("CULTIVATION", "Pooled spawn failed, falling back to direct instantiation", this);
                }
                return SpawnPlantDirect(plantData, position);
            }
        }

        /// <summary>
        /// Despawn plant using pooling system
        /// </summary>
        public void DespawnPlant(string plantId)
        {
            if (!IsInitialized) return;

            if (_activePlants.TryGetValue(plantId, out var plantComponent))
            {
                if (_enablePooling)
                {
                    var startTime = Time.realtimeSinceStartup;

                    // Return to pool
                    _plantPool.ReturnPlant(plantComponent);
                    _totalPlantsRecycled++;

                    // Record performance
                    var returnTime = Time.realtimeSinceStartup - startTime;
                    _performanceMonitor?.RecordPoolOperation("PlantPool", PoolOperationType.Return, returnTime);

                    if (_enableLogging)
                    {
                        ChimeraLogger.Log("CULTIVATION", $"Returned plant {plantId} to pool", this);
                    }
                }
                else
                {
                    // Direct destruction
                    Destroy(plantComponent.gameObject);
                }

                _activePlants.Remove(plantId);
            }
        }

        /// <summary>
        /// Batch spawn multiple plants efficiently
        /// </summary>
        public List<PlantInstanceComponent> BatchSpawnPlants(List<PlantInstance> plantDataList, List<Vector3> positions)
        {
            var spawnedPlants = new List<PlantInstanceComponent>();

            if (plantDataList.Count != positions.Count)
            {
                ChimeraLogger.LogError("CULTIVATION", "BatchSpawnPlants: positions count does not match plant data count", this);
                return spawnedPlants;
            }

            // Pre-warm pools if needed
            if (_enablePooling)
            {
                _plantPool.PrewarmPools(plantDataList.Count);
            }

            // Spawn plants
            for (int i = 0; i < plantDataList.Count; i++)
            {
                var plantComponent = SpawnPlant(plantDataList[i], positions[i]);
                if (plantComponent != null)
                {
                    spawnedPlants.Add(plantComponent);
                }
            }

            if (_enableLogging)
            {
                ChimeraLogger.Log("CULTIVATION", $"Batch spawned {spawnedPlants.Count} plants", this);
            }

            return spawnedPlants;
        }

        /// <summary>
        /// Get pooling statistics
        /// </summary>
        public CultivationPoolingStats GetPoolingStats()
        {
            var stats = new CultivationPoolingStats
            {
                IsPoolingEnabled = _enablePooling,
                TotalPlantsSpawned = _totalPlantsSpawned,
                TotalPlantsRecycled = _totalPlantsRecycled,
                ActivePlantsCount = _activePlants.Count,
                PoolingEfficiency = _totalPlantsSpawned > 0 ? (float)_totalPlantsRecycled / _totalPlantsSpawned : 0f
            };

            if (_plantPool != null)
            {
                stats.PlantPoolStats = _plantPool.GetPoolStats();
            }

            if (_performanceMonitor != null)
            {
                stats.PerformanceReport = _performanceMonitor.GetPerformanceReport();
            }

            return stats;
        }

        /// <summary>
        /// Tick implementation for cleanup and monitoring
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (!IsInitialized) return;

            // Periodic cleanup
            if (Time.time - _lastCleanupTime >= _poolCleanupInterval)
            {
                PerformCleanup();
                _lastCleanupTime = Time.time;
            }
        }

        /// <summary>
        /// Enable or disable pooling at runtime
        /// </summary>
        public void SetPoolingEnabled(bool enabled)
        {
            _enablePooling = enabled;

            if (_enableLogging)
            {
                ChimeraLogger.Log("CULTIVATION", $"Pooling {(enabled ? "enabled" : "disabled")}", this);
            }
        }

        #region Private Methods

        /// <summary>
        /// Initialize pool components
        /// </summary>
        private void InitializePoolComponents()
        {
            // Get or create plant pool
            _plantPool = GetComponent<PlantObjectPool>();
            if (_plantPool == null)
            {
                _plantPool = gameObject.AddComponent<PlantObjectPool>();
            }
            _plantPool.Initialize();

            // Get or create performance monitor via ServiceContainer
            _performanceMonitor = ServiceContainerFactory.Instance?.TryResolve<PoolPerformanceMonitor>();

            if (_performanceMonitor == null)
            {
                var monitorGO = new GameObject("PoolPerformanceMonitor");
                monitorGO.transform.SetParent(transform);
                _performanceMonitor = monitorGO.AddComponent<PoolPerformanceMonitor>();

                // Register with ServiceContainer for future resolution
                ServiceContainerFactory.Instance?.RegisterSingleton<PoolPerformanceMonitor>(_performanceMonitor);
            }
            _performanceMonitor.Initialize();
        }

        /// <summary>
        /// Register with service systems
        /// </summary>
        private void RegisterWithServices()
        {
            // Register with UpdateOrchestrator if available
            var updateOrchestrator = ServiceContainerFactory.Instance?.TryResolve<IUpdateOrchestrator>();
            updateOrchestrator?.RegisterTickable(this);

            // Register with PoolManager
            if (PoolManager.Instance != null && PoolManager.Instance.IsInitialized)
            {
                // Could register custom pools here if needed
            }
        }

        /// <summary>
        /// Spawn plant directly without pooling (fallback)
        /// </summary>
        private PlantInstanceComponent SpawnPlantDirect(PlantInstance plantData, Vector3 position)
        {
            // This would use the original plant spawning logic
            // For now, return null as a placeholder
            if (_enableLogging)
            {
                ChimeraLogger.LogWarning("CULTIVATION", $"Direct spawn used for plant {plantData?.PlantId} at {position}", this);
            }

            return null; // Placeholder - would implement actual spawning logic
        }

        /// <summary>
        /// Get prefab index for plant data
        /// </summary>
        private int GetPrefabIndexForPlant(PlantInstance plantData)
        {
            // Simple mapping based on strain or growth stage
            if (_plantPrefabMapping.ContainsKey(plantData.StrainName))
            {
                return _plantPrefabMapping[plantData.StrainName];
            }

            // Default prefab index
            return 0;
        }

        /// <summary>
        /// Perform periodic cleanup
        /// </summary>
        private void PerformCleanup()
        {
            // Remove any null references from active plants
            var keysToRemove = new List<string>();
            foreach (var kvp in _activePlants)
            {
                if (kvp.Value == null)
                {
                    keysToRemove.Add(kvp.Key);
                }
            }

            foreach (var key in keysToRemove)
            {
                _activePlants.Remove(key);
            }

            if (_enableLogging && keysToRemove.Count > 0)
            {
                ChimeraLogger.Log("CULTIVATION", $"Cleanup removed {keysToRemove.Count} null plant references", this);
            }
        }

        #endregion

        private void OnDestroy()
        {
            // Clean up active plants
            if (_enablePooling && _plantPool != null)
            {
                foreach (var plant in _activePlants.Values)
                {
                    if (plant != null)
                    {
                        _plantPool.ReturnPlant(plant);
                    }
                }
            }

            _activePlants.Clear();
        }
    }

    /// <summary>
    /// Statistics for cultivation pooling integration
    /// </summary>
    [System.Serializable]
    public struct CultivationPoolingStats
    {
        public bool IsPoolingEnabled;
        public int TotalPlantsSpawned;
        public int TotalPlantsRecycled;
        public int ActivePlantsCount;
        public float PoolingEfficiency;
        public PlantPoolStats PlantPoolStats;
        public PoolPerformanceReport PerformanceReport;
    }
}
