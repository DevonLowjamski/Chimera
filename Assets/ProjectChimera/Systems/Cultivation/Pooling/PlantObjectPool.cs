using UnityEngine;
using ProjectChimera.Core.Pooling;
using ProjectChimera.Systems.Cultivation;
using ProjectChimera.Data.Cultivation.Plant;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;

namespace ProjectChimera.Systems.Cultivation.Pooling
{
    /// <summary>
    /// PERFORMANCE: Specialized object pool for plant GameObjects
    /// Optimizes plant instantiation/destruction for large cultivation operations
    /// Week 9 Day 4-5: Object Pooling System Implementation
    /// </summary>
    public class PlantObjectPool : MonoBehaviour, ITickable
    {
        [Header("Plant Pool Settings")]
        [SerializeField] private GameObject[] _plantPrefabs;
        [SerializeField] private int _initialPoolSizePerPrefab = 20;
        [SerializeField] private int _maxPoolSizePerPrefab = 100;
        [SerializeField] private bool _expandablePools = true;
        [SerializeField] private bool _enableLogging = true;

        // Pool storage
        private ObjectPool<PlantInstanceComponent>[] _plantPools;
        private Transform _poolParent;
        private bool _isInitialized;

        // Statistics
        private int _totalGets = 0;
        private int _totalReturns = 0;
        private float _averageGetTime = 0f;

        /// <summary>
        /// Initialize plant object pools
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            // Create pool parent
            _poolParent = new GameObject("PlantPools").transform;
            _poolParent.SetParent(transform);

            // Initialize pools for each prefab
            _plantPools = new ObjectPool<PlantInstanceComponent>[_plantPrefabs.Length];

            for (int i = 0; i < _plantPrefabs.Length; i++)
            {
                if (_plantPrefabs[i] != null)
                {
                    CreatePlantPool(i);
                }
            }

            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("CULTIVATION", "PlantObjectPool initialized", this);
            }
        }

        /// <summary>
        /// Get plant object from appropriate pool
        /// </summary>
        public PlantInstanceComponent GetPlant(int prefabIndex, PlantInstance plantData)
        {
            if (!_isInitialized || prefabIndex < 0 || prefabIndex >= _plantPools.Length)
                return null;

            var startTime = Time.realtimeSinceStartup;
            var pooledPlant = _plantPools[prefabIndex].Get();

            if (pooledPlant != null)
            {
                // Initialize with plant data
                pooledPlant.Initialize(plantData);

                // Reset transform
                pooledPlant.transform.position = plantData.Position;
                pooledPlant.transform.rotation = Quaternion.identity;
                pooledPlant.transform.localScale = Vector3.one;

                // Update statistics
                _totalGets++;
                var getTime = Time.realtimeSinceStartup - startTime;
                _averageGetTime = (_averageGetTime * (_totalGets - 1) + getTime) / _totalGets;

                if (_enableLogging)
                {
                    ChimeraLogger.Log("CULTIVATION", $"Got plant from pool (prefabIndex={prefabIndex})", this);
                }
            }

            return pooledPlant;
        }

        /// <summary>
        /// Return plant object to pool
        /// </summary>
        public void ReturnPlant(PlantInstanceComponent plant)
        {
            if (!_isInitialized || plant == null) return;

            // Find appropriate pool
            for (int i = 0; i < _plantPools.Length; i++)
            {
                _plantPools[i].Return(plant);
                _totalReturns++;

                if (_enableLogging)
                {
                    ChimeraLogger.Log("CULTIVATION", "Returned plant to pool", this);
                }
                break;
            }
        }

        /// <summary>
        /// Get plant by strain name
        /// </summary>
        public PlantInstanceComponent GetPlantByStrain(string strainName, PlantInstance plantData)
        {
            // Find prefab index by strain name (simplified logic)
            int prefabIndex = GetPrefabIndexByStrain(strainName);
            return GetPlant(prefabIndex, plantData);
        }

        /// <summary>
        /// Get plant by plant ID (for streaming manager)
        /// </summary>
        public GameObject GetPlant(string plantId)
        {
            // For streaming, we'll get the first available plant prefab
            // In a full implementation, this would map plantId to specific prefab
            if (!_isInitialized || _plantPools == null || _plantPools.Length == 0) return null;

            var plantComponent = GetPlant(0, null); // Get from first pool
            return plantComponent?.gameObject;
        }

        /// <summary>
        /// Return plant by plant ID (for streaming manager)
        /// </summary>
        public void ReturnPlant(string plantId)
        {
            // In a full implementation, this would find the plant by ID and return it
            // For now, this is a placeholder that logs the request
            if (_enableLogging)
            {
                ChimeraLogger.Log("CULTIVATION", $"Return plant requested for ID: {plantId}", this);
            }
        }

        /// <summary>
        /// Pre-warm pools with additional objects
        /// </summary>
        public void PrewarmPools(int additionalObjects)
        {
            if (!_isInitialized) return;

            for (int i = 0; i < _plantPools.Length; i++)
            {
                if (_plantPools[i] != null)
                {
                    var currentSize = _plantPools[i].CountInactive + _plantPools[i].CountActive;
                    var targetSize = currentSize + additionalObjects;
                    _plantPools[i].Resize(Mathf.Min(targetSize, _maxPoolSizePerPrefab));
                }
            }

            if (_enableLogging)
            {
                ChimeraLogger.Log("CULTIVATION", "Prewarmed plant pools", this);
            }
        }

        /// <summary>
        /// Get pool statistics
        /// </summary>
        public PlantPoolStats GetPoolStats()
        {
            var stats = new PlantPoolStats
            {
                TotalGets = _totalGets,
                TotalReturns = _totalReturns,
                AverageGetTime = _averageGetTime,
                PoolDetails = new PlantPoolDetail[_plantPools?.Length ?? 0]
            };

            if (_plantPools != null)
            {
                for (int i = 0; i < _plantPools.Length; i++)
                {
                    if (_plantPools[i] != null)
                    {
                        stats.PoolDetails[i] = new PlantPoolDetail
                        {
                            PrefabIndex = i,
                            CountInactive = _plantPools[i].CountInactive,
                            CountActive = _plantPools[i].CountActive,
                            CountAll = _plantPools[i].CountAll
                        };
                    }
                }
            }

            return stats;
        }

        /// <summary>
        /// Clear all plant pools
        /// </summary>
        public void ClearAllPools()
        {
            if (_plantPools != null)
            {
                for (int i = 0; i < _plantPools.Length; i++)
                {
                    _plantPools[i]?.Clear();
                }
            }

            if (_enableLogging)
            {
                ChimeraLogger.Log("CULTIVATION", "Cleared all plant pools", this);
            }
        }

        #region Private Methods

        /// <summary>
        /// Create pool for specific plant prefab
        /// </summary>
        private void CreatePlantPool(int prefabIndex)
        {
            var prefab = _plantPrefabs[prefabIndex];
            var poolParent = new GameObject($"PlantPool_{prefab.name}").transform;
            poolParent.SetParent(_poolParent);

            _plantPools[prefabIndex] = new ObjectPool<PlantInstanceComponent>(
                prefab,
                _initialPoolSizePerPrefab,
                _maxPoolSizePerPrefab,
                _expandablePools,
                poolParent,
                createFunc: () => Instantiate(prefab),
                onGetAction: OnPlantGet,
                onReturnAction: OnPlantReturn,
                onDestroyAction: OnPlantDestroy
            );
        }

        /// <summary>
        /// Get prefab index by strain name (simplified)
        /// </summary>
        private int GetPrefabIndexByStrain(string strainName)
        {
            // Simplified logic - in real implementation, this would match strain to appropriate prefab
            return 0; // Default to first prefab
        }

        /// <summary>
        /// Called when plant is retrieved from pool
        /// </summary>
        private void OnPlantGet(PlantInstanceComponent plant)
        {
            if (plant != null)
            {
                plant.gameObject.SetActive(true);

                // Reset any poolable components
                if (plant is IPoolable poolable)
                {
                    poolable.OnGetFromPool();
                }
            }
        }

        /// <summary>
        /// Called when plant is returned to pool
        /// </summary>
        private void OnPlantReturn(PlantInstanceComponent plant)
        {
            if (plant != null)
            {
                plant.gameObject.SetActive(false);

                // Reset plant state
                plant.ResetForPool();

                // Handle poolable interface
                if (plant is IPoolable poolable)
                {
                    poolable.OnReturnToPool();
                }
            }
        }

        /// <summary>
        /// Called when plant is destroyed from pool
        /// </summary>
        private void OnPlantDestroy(PlantInstanceComponent plant)
        {
            // Clean up any resources before destruction
            if (plant != null)
            {
                plant.CleanupForDestroy();
            }
        }

        #endregion

    public int TickPriority => 100;
    public bool IsTickable => enabled && gameObject.activeInHierarchy;

    public void Tick(float deltaTime)
    {
            if (!_isInitialized) return;

            // Optional: Performance monitoring or dynamic pool sizing
    }

    private void Awake()
    {
        UpdateOrchestrator.Instance.RegisterTickable(this);
    }

    private void OnDestroy()
    {
        UpdateOrchestrator.Instance.UnregisterTickable(this);
        ClearAllPools();
    }
    }

    /// <summary>
    /// Statistics for plant object pools
    /// </summary>
    [System.Serializable]
    public struct PlantPoolStats
    {
        public int TotalGets;
        public int TotalReturns;
        public float AverageGetTime;
        public PlantPoolDetail[] PoolDetails;
    }

    /// <summary>
    /// Detail for individual plant pool
    /// </summary>
    [System.Serializable]
    public struct PlantPoolDetail
    {
        public int PrefabIndex;
        public int CountInactive;
        public int CountActive;
        public int CountAll;
    }
}
