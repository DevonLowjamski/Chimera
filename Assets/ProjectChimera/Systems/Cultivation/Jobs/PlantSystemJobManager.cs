using UnityEngine;
using Unity.Collections;
using Unity.Jobs;
using System.Threading.Tasks;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;
using ProjectChimera.Data.Shared;

namespace ProjectChimera.Systems.Cultivation.Jobs
{
    /// <summary>
    /// PERFORMANCE: High-performance plant system job manager using Unity Job System
    /// Manages burst-compiled plant growth calculations for large numbers of plants
    /// Week 9 Day 1-3: Jobs System & Performance Foundations
    /// </summary>
    public class PlantSystemJobManager : MonoBehaviour, ITickable
    {
        [Header("Performance Settings")]
        [SerializeField] private int _maxPlantsPerFrame = 1000;
        [SerializeField] private int _jobBatchSize = 32;
        [SerializeField] private bool _enableAsyncProcessing = true;
        [SerializeField] private bool _enableLogging = true;

        // Native arrays for job data
        private NativeArray<PlantGrowthParameters> _growthParams;
        private NativeArray<EnvironmentalData> _environmentalData;
        private NativeArray<PlantGrowthData> _growthData;
        private NativeArray<PlantHealthData> _healthData;
        private NativeArray<PlantResourceData> _resourceData;
        private NativeArray<float> _deltaTimeArray;

        // Job management
        private JobHandle _currentJobHandle;
        private bool _jobRunning = false;
        private int _activePlantCount = 0;

        // Plant tracking
        private readonly Dictionary<string, int> _plantIndexMapping = new Dictionary<string, int>();
        private readonly List<string> _activePlantIds = new List<string>();

        // Performance metrics
        private float _lastUpdateTime = 0f;
        private int _updatesPerSecond = 0;
        private float _averageJobTime = 0f;

        public bool IsInitialized { get; private set; }

        /// <summary>
        /// Initialize the job system
        /// </summary>
        public void Initialize(int initialCapacity = 500)
        {
            if (IsInitialized) return;

            AllocateNativeArrays(initialCapacity);

            _deltaTimeArray = new NativeArray<float>(1, Allocator.Persistent);

            IsInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("CULTIVATION", $"PlantSystemJobManager initialized (capacity={initialCapacity})", this);
            }
        }

        /// <summary>
        /// ITickable implementation - called by UpdateOrchestrator
        /// </summary>
        public int TickPriority => ProjectChimera.Core.Updates.TickPriority.CultivationManager;
        public bool IsTickable => enabled && gameObject.activeInHierarchy && IsInitialized;
        public void Tick(float deltaTime)
        {
            if (!IsInitialized || _activePlantCount == 0) return;

            // Complete previous frame's job if still running
            if (_jobRunning)
            {
                _currentJobHandle.Complete();
                _jobRunning = false;
            }

            // Update delta time for this frame
            _deltaTimeArray[0] = deltaTime;

            // Schedule new growth job
            SchedulePlantGrowthJob();

            // Update performance metrics
            UpdatePerformanceMetrics();
        }

        /// <summary>
        /// Register a plant for job system processing
        /// </summary>
        public bool RegisterPlant(string plantId, PlantGrowthParameters parameters,
            EnvironmentalData environment, PlantGrowthData growth,
            PlantHealthData health, PlantResourceData resources)
        {
            if (!IsInitialized) return false;

            // Check if we need to expand arrays
            if (_activePlantCount >= _growthParams.Length)
            {
                ExpandNativeArrays();
            }

            int index = _activePlantCount;
            _plantIndexMapping[plantId] = index;
            _activePlantIds.Add(plantId);

            // Set plant data
            _growthParams[index] = parameters;
            _environmentalData[index] = environment;
            _growthData[index] = growth;
            _healthData[index] = health;
            _resourceData[index] = resources;

            _activePlantCount++;

            if (_enableLogging)
            {
                ChimeraLogger.Log("CULTIVATION", $"Registered plant {plantId}", this);
            }

            return true;
        }

        /// <summary>
        /// Unregister a plant from job system processing
        /// </summary>
        public bool UnregisterPlant(string plantId)
        {
            if (!_plantIndexMapping.TryGetValue(plantId, out int index)) return false;

            // Move last plant to this slot to avoid gaps
            int lastIndex = _activePlantCount - 1;
            if (index != lastIndex)
            {
                string lastPlantId = _activePlantIds[lastIndex];

                // Copy data from last slot to current slot
                _growthParams[index] = _growthParams[lastIndex];
                _environmentalData[index] = _environmentalData[lastIndex];
                _growthData[index] = _growthData[lastIndex];
                _healthData[index] = _healthData[lastIndex];
                _resourceData[index] = _resourceData[lastIndex];

                // Update mapping for moved plant
                _plantIndexMapping[lastPlantId] = index;
                _activePlantIds[index] = lastPlantId;
            }

            // Remove the unregistered plant
            _plantIndexMapping.Remove(plantId);
            _activePlantIds.RemoveAt(lastIndex);
            _activePlantCount--;

            if (_enableLogging)
            {
                ChimeraLogger.Log("CULTIVATION", $"Unregistered plant {plantId}", this);
            }

            return true;
        }

        /// <summary>
        /// Update environmental data for a specific plant
        /// </summary>
        public bool UpdatePlantEnvironment(string plantId, EnvironmentalData environment)
        {
            if (!_plantIndexMapping.TryGetValue(plantId, out int index)) return false;

            _environmentalData[index] = environment;
            return true;
        }

        /// <summary>
        /// Get current plant data (waits for job completion if needed)
        /// </summary>
        public async Task<PlantUpdateData?> GetPlantDataAsync(string plantId)
        {
            if (!_plantIndexMapping.TryGetValue(plantId, out int index)) return null;

            // Wait for current job to complete
            if (_jobRunning)
            {
                await Task.Run(() => _currentJobHandle.Complete());
                _jobRunning = false;
            }

            var data = new PlantUpdateData
            {
                growth = _growthData[index],
                health = _healthData[index],
                resources = _resourceData[index],
                environment = _environmentalData[index],
                parameters = _growthParams[index],
                plantId = index
            };

            return data;
        }

        /// <summary>
        /// Get all plant data (waits for job completion)
        /// </summary>
        public async Task<PlantUpdateData[]> GetAllPlantDataAsync()
        {
            if (_activePlantCount == 0) return new PlantUpdateData[0];

            // Wait for current job to complete
            if (_jobRunning)
            {
                await Task.Run(() => _currentJobHandle.Complete());
                _jobRunning = false;
            }

            var results = new PlantUpdateData[_activePlantCount];
            for (int i = 0; i < _activePlantCount; i++)
            {
                results[i] = new PlantUpdateData
                {
                    growth = _growthData[i],
                    health = _healthData[i],
                    resources = _resourceData[i],
                    environment = _environmentalData[i],
                    parameters = _growthParams[i],
                    plantId = i
                };
            }

            return results;
        }

        /// <summary>
        /// Schedule the plant growth job
        /// </summary>
        private void SchedulePlantGrowthJob()
        {
            if (_activePlantCount == 0 || _jobRunning) return;

            var growthJob = new PlantGrowthJob
            {
                deltaTimeArray = _deltaTimeArray,
                growthParams = _growthParams,
                environmentalData = _environmentalData,
                growthData = _growthData,
                healthData = _healthData,
                resourceData = _resourceData
            };

            _currentJobHandle = growthJob.Schedule(_activePlantCount, _jobBatchSize);
            _jobRunning = true;

            // Schedule batched jobs for better performance
            JobHandle.ScheduleBatchedJobs();
        }

        /// <summary>
        /// Allocate native arrays with specified capacity
        /// </summary>
        private void AllocateNativeArrays(int capacity)
        {
            _growthParams = new NativeArray<PlantGrowthParameters>(capacity, Allocator.Persistent);
            _environmentalData = new NativeArray<EnvironmentalData>(capacity, Allocator.Persistent);
            _growthData = new NativeArray<PlantGrowthData>(capacity, Allocator.Persistent);
            _healthData = new NativeArray<PlantHealthData>(capacity, Allocator.Persistent);
            _resourceData = new NativeArray<PlantResourceData>(capacity, Allocator.Persistent);
        }

        /// <summary>
        /// Expand native arrays when needed
        /// </summary>
        private void ExpandNativeArrays()
        {
            int newCapacity = _growthParams.Length * 2;

            var oldGrowthParams = _growthParams;
            var oldEnvironmentalData = _environmentalData;
            var oldGrowthData = _growthData;
            var oldHealthData = _healthData;
            var oldResourceData = _resourceData;

            AllocateNativeArrays(newCapacity);

            // Copy existing data
            for (int i = 0; i < _activePlantCount; i++)
            {
                _growthParams[i] = oldGrowthParams[i];
                _environmentalData[i] = oldEnvironmentalData[i];
                _growthData[i] = oldGrowthData[i];
                _healthData[i] = oldHealthData[i];
                _resourceData[i] = oldResourceData[i];
            }

            // Dispose old arrays
            oldGrowthParams.Dispose();
            oldEnvironmentalData.Dispose();
            oldGrowthData.Dispose();
            oldHealthData.Dispose();
            oldResourceData.Dispose();

            if (_enableLogging)
            {
                ChimeraLogger.Log("CULTIVATION", $"Expanded native arrays to capacity {newCapacity}", this);
            }
        }

        /// <summary>
        /// Update performance metrics
        /// </summary>
        private void UpdatePerformanceMetrics()
        {
            _updatesPerSecond++;

            if (Time.time - _lastUpdateTime >= 1f)
            {
                if (_enableLogging && _updatesPerSecond > 0)
                {
                    ChimeraLogger.Log("PERFORMANCE", $"UPS={_updatesPerSecond}, ActivePlants={_activePlantCount}", this);
                }

                _updatesPerSecond = 0;
                _lastUpdateTime = Time.time;
            }
        }

        /// <summary>
        /// Cleanup on destroy
        /// </summary>
        private void OnDestroy()
        {
            if (_jobRunning)
            {
                _currentJobHandle.Complete();
                _jobRunning = false;
            }

            if (IsInitialized)
            {
                _growthParams.Dispose();
                _environmentalData.Dispose();
                _growthData.Dispose();
                _healthData.Dispose();
                _resourceData.Dispose();
                _deltaTimeArray.Dispose();

                IsInitialized = false;

                if (_enableLogging)
                {
                    ChimeraLogger.Log("CULTIVATION", "Disposed native arrays for PlantSystemJobManager", this);
                }
            }
        }

        /// <summary>
        /// Get performance statistics
        /// </summary>
        public PlantJobManagerStats GetStats()
        {
            return new PlantJobManagerStats
            {
                ActivePlants = _activePlantCount,
                ArrayCapacity = _growthParams.IsCreated ? _growthParams.Length : 0,
                JobRunning = _jobRunning,
                UpdatesPerSecond = _updatesPerSecond,
                AverageJobTime = _averageJobTime
            };
        }
    }

    /// <summary>
    /// Performance statistics for PlantSystemJobManager
    /// </summary>
    [System.Serializable]
    public struct PlantJobManagerStats
    {
        public int ActivePlants;
        public int ArrayCapacity;
        public bool JobRunning;
        public int UpdatesPerSecond;
        public float AverageJobTime;
    }
}
