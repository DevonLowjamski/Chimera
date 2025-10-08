using UnityEngine;
using ProjectChimera.Core.Memory;
using ProjectChimera.Data.Cultivation.Plant;
using ProjectChimera.Data.Shared;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;

namespace ProjectChimera.Systems.Cultivation
{
    /// <summary>
    /// MEMORY: Memory-optimized cultivation manager using efficient data structures
    /// Demonstrates memory optimization techniques in cultivation system
    /// Week 10: Memory & GC Optimization
    /// </summary>
    public class MemoryOptimizedCultivationManager : MonoBehaviour, ITickable
    {
        [Header("Memory Optimization Settings")]
        [SerializeField] private bool _enableMemoryOptimization = true;
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private int _initialPlantCapacity = 100;
        [SerializeField] private int _maxCachedEventStrings = 50;

        // Memory-optimized collections
        private MemoryOptimizedList<PlantInstance> _plants;
        private MemoryOptimizedDictionary<string, int> _plantIdToIndex;
        private MemoryOptimizedQueue<CultivationEvent> _eventQueue;
        private MemoryPool<CultivationEvent> _eventPool;
        private MemoryPool<PlantUpdateData> _updateDataPool;

        // String optimization
        private readonly Dictionary<PlantGrowthStage, string> _stageStrings = new Dictionary<PlantGrowthStage, string>();
        private readonly Dictionary<CultivationEventType, string> _eventTypeStrings = new Dictionary<CultivationEventType, string>();

        // Performance tracking
        private int _totalPlantUpdates = 0;
        private int _totalEventsProcessed = 0;
        private float _lastMemoryUsage = 0f;

        public int PlantCount => _plants?.Count ?? 0;
        public int QueuedEvents => _eventQueue?.Count ?? 0;

        public bool IsInitialized { get; private set; }

        /// <summary>
        /// Initialize memory-optimized cultivation manager
        /// </summary>
        public void Initialize()
        {
            if (IsInitialized) return;

            InitializeMemoryOptimizedCollections();
            InitializeStringOptimizations();
            RegisterWithMemoryProfiler();

            IsInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("CULTIVATION", "MemoryOptimizedCultivationManager initialized", this);
            }
        }

        /// <summary>
        /// Add plant with memory optimization
        /// </summary>
        public void AddPlant(PlantInstance plant)
        {
            if (!IsInitialized || plant == null) return;

            // Optimize plant ID string
            plant.PlantId = StringOptimizer.Intern(plant.PlantId);
            plant.StrainName = StringOptimizer.Intern(plant.StrainName);

            int plantIndex = _plants.Count;
            _plants.Add(plant);
            _plantIdToIndex.Add(plant.PlantId, plantIndex);

            // Queue plant added event
            QueueEvent(CultivationEventType.PlantAdded, plant.PlantId, "Plant added to cultivation system");

            if (_enableLogging)
            {
                var logMessage = StringOptimizer.Format("Added plant {0} ({1})", plant.PlantId, plant.StrainName);
                ChimeraLogger.Log("CULTIVATION", logMessage, this);
            }
        }

        /// <summary>
        /// Remove plant with memory optimization
        /// </summary>
        public bool RemovePlant(string plantId)
        {
            if (!IsInitialized || string.IsNullOrEmpty(plantId)) return false;

            plantId = StringOptimizer.Intern(plantId);

            if (_plantIdToIndex.TryGetValue(plantId, out int index))
            {
                _plants.RemoveAt(index);
                _plantIdToIndex.Remove(plantId);

                // Rebuild index map (only if needed)
                RebuildPlantIndex();

                QueueEvent(CultivationEventType.PlantRemoved, plantId, "Plant removed from cultivation system");

                if (_enableLogging)
                {
                    ChimeraLogger.Log("CULTIVATION", $"Removed plant {plantId}", this);
                }
                return true;
            }

            return false;
        }

        /// <summary>
        /// Update plants with memory-optimized processing
        /// </summary>
        public void UpdatePlants(float deltaTime)
        {
            if (!IsInitialized) return;

            // Process in batches to avoid large temporary allocations
            const int batchSize = 20;
            int plantsProcessed = 0;

            for (int i = 0; i < _plants.Count; i += batchSize)
            {
                int batchEnd = Mathf.Min(i + batchSize, _plants.Count);

                for (int j = i; j < batchEnd; j++)
                {
                    UpdateSinglePlant(_plants[j], deltaTime);
                    plantsProcessed++;
                }

                // Yield control periodically to prevent frame drops
                if (plantsProcessed % batchSize == 0 && Time.unscaledDeltaTime > 0.016f)
                {
                    // Could yield here in a coroutine implementation
                    break;
                }
            }

            _totalPlantUpdates += plantsProcessed;

            // Process events
            ProcessEventQueue();

            // Memory management
            if (Time.frameCount % 300 == 0) // Every ~5 seconds at 60fps
            {
                PerformMemoryMaintenance();
            }
        }

        /// <summary>
        /// Get plant by ID with optimized lookup
        /// </summary>
        public PlantInstance GetPlant(string plantId)
        {
            if (!IsInitialized || string.IsNullOrEmpty(plantId)) return null;

            plantId = StringOptimizer.Intern(plantId);

            if (_plantIdToIndex.TryGetValue(plantId, out int index) && index < _plants.Count)
            {
                return _plants[index];
            }

            return null;
        }

        /// <summary>
        /// Get plants by growth stage with memory optimization
        /// </summary>
        public MemoryOptimizedList<PlantInstance> GetPlantsByStage(PlantGrowthStage stage)
        {
            var result = new MemoryOptimizedList<PlantInstance>();

            if (!IsInitialized) return result;

            for (int i = 0; i < _plants.Count; i++)
            {
                if (_plants[i].GrowthStage == stage)
                {
                    result.Add(_plants[i]);
                }
            }

            return result;
        }

        /// <summary>
        /// Queue cultivation event with object pooling
        /// </summary>
        public void QueueEvent(CultivationEventType eventType, string plantId, string description)
        {
            if (!IsInitialized) return;

            var cultEvent = _eventPool.Get();
            cultEvent.EventType = eventType;
            cultEvent.PlantId = StringOptimizer.Intern(plantId);
            cultEvent.Description = StringOptimizer.Intern(description);
            cultEvent.Timestamp = Time.time;

            _eventQueue.Enqueue(cultEvent);
        }

        /// <summary>
        /// Get memory optimization statistics
        /// </summary>
        public MemoryOptimizedCultivationStats GetStats()
        {
            var currentMemory = System.GC.GetTotalMemory(false) / 1024f / 1024f; // MB

            return new MemoryOptimizedCultivationStats
            {
                PlantCount = _plants?.Count ?? 0,
                PlantCapacity = _plants?.Capacity ?? 0,
                QueuedEvents = _eventQueue?.Count ?? 0,
                TotalPlantUpdates = _totalPlantUpdates,
                TotalEventsProcessed = _totalEventsProcessed,
                CurrentMemoryUsage = currentMemory,
                MemoryDelta = currentMemory - _lastMemoryUsage,
                StringCacheStats = StringOptimizer.GetStats(),
                MemoryOptimizationEnabled = _enableMemoryOptimization
            };
        }

        /// <summary>
        /// Perform memory cleanup and optimization
        /// </summary>
        public void OptimizeMemory()
        {
            if (!IsInitialized) return;

            // Trim excess capacity from collections
            _plants?.TrimExcess();

            // Clear old event data
            ProcessEventQueue();

            // Trim string caches
            StringOptimizer.TrimCaches();

            // Record memory state
            RecordMemoryUsage();

            if (_enableLogging)
            {
                ChimeraLogger.Log("PERFORMANCE", "OptimizeMemory completed for cultivation manager", this);
            }
        }

        #region Private Methods

        /// <summary>
        /// Initialize memory-optimized data structures
        /// </summary>
        private void InitializeMemoryOptimizedCollections()
        {
            _plants = new MemoryOptimizedList<PlantInstance>(_initialPlantCapacity);
            _plantIdToIndex = new MemoryOptimizedDictionary<string, int>();
            _eventQueue = new MemoryOptimizedQueue<CultivationEvent>();

            _eventPool = new MemoryPool<CultivationEvent>(
                maxSize: 100,
                factory: () => new CultivationEvent(),
                resetAction: ResetEvent
            );

            _updateDataPool = new MemoryPool<PlantUpdateData>(
                maxSize: 50,
                factory: () => new PlantUpdateData(),
                resetAction: ResetUpdateData
            );
        }

        /// <summary>
        /// Initialize string optimizations
        /// </summary>
        private void InitializeStringOptimizations()
        {
            // Pre-intern common stage strings
            foreach (PlantGrowthStage stage in System.Enum.GetValues(typeof(PlantGrowthStage)))
            {
                _stageStrings[stage] = StringOptimizer.Intern(stage.ToString());
            }

            // Pre-intern common event type strings
            foreach (CultivationEventType eventType in System.Enum.GetValues(typeof(CultivationEventType)))
            {
                _eventTypeStrings[eventType] = StringOptimizer.Intern(eventType.ToString());
            }
        }

        /// <summary>
        /// Register with memory profiler for tracking
        /// </summary>
        private void RegisterWithMemoryProfiler()
        {
            if (MemoryProfiler.Instance != null)
            {
                MemoryProfiler.Instance.Initialize();
            }
        }

        /// <summary>
        /// Update single plant with memory optimization
        /// </summary>
        private void UpdateSinglePlant(PlantInstance plant, float deltaTime)
        {
            if (plant == null) return;

            // Use pooled update data
            var updateData = _updateDataPool.Get();

            // Perform plant update logic (simplified)
            updateData.DeltaTime = deltaTime;
            updateData.PlantId = plant.PlantId;

            // Simulate growth
            plant.Age += deltaTime;

            if (plant.Health > 0.1f)
            {
                plant.Height += deltaTime * 0.5f; // Simple growth
                plant.Biomass += deltaTime * 0.3f;
            }

            // Return to pool
            _updateDataPool.Return(updateData);
        }

        /// <summary>
        /// Process queued events
        /// </summary>
        private void ProcessEventQueue()
        {
            int processed = 0;
            const int maxProcessPerFrame = 10;

            while (_eventQueue.Count > 0 && processed < maxProcessPerFrame)
            {
                if (_eventQueue.TryDequeue(out var cultEvent))
                {
                    ProcessSingleEvent(cultEvent);
                    _eventPool.Return(cultEvent);
                    processed++;
                    _totalEventsProcessed++;
                }
            }
        }

        /// <summary>
        /// Process single cultivation event
        /// </summary>
        private void ProcessSingleEvent(CultivationEvent cultEvent)
        {
            // Event processing logic would go here
            // This is a simplified implementation

            if (_enableLogging && cultEvent.EventType == CultivationEventType.PlantAdded)
            {
                var message = StringOptimizer.Format("Processed event: {0} for plant {1}",
                    _eventTypeStrings[cultEvent.EventType], cultEvent.PlantId);
                ChimeraLogger.Log("CULTIVATION", message, this);
            }
        }

        /// <summary>
        /// Rebuild plant index after removal
        /// </summary>
        private void RebuildPlantIndex()
        {
            _plantIdToIndex.Clear();

            for (int i = 0; i < _plants.Count; i++)
            {
                _plantIdToIndex[_plants[i].PlantId] = i;
            }
        }

        /// <summary>
        /// Perform periodic memory maintenance
        /// </summary>
        private void PerformMemoryMaintenance()
        {
            // Record current memory usage
            RecordMemoryUsage();

            // Trim collections if they're oversized
            if (_plants.Capacity > _plants.Count * 2)
            {
                _plants.TrimExcess();
            }

            // Notify memory profiler of cultivation allocations
            if (MemoryProfiler.Instance != null)
            {
                long estimatedAllocation = _plants.Count * 500; // Estimated bytes per plant
                MemoryProfiler.Instance.RecordAllocation("Cultivation", estimatedAllocation);
            }
        }

        /// <summary>
        /// Record current memory usage
        /// </summary>
        private void RecordMemoryUsage()
        {
            _lastMemoryUsage = System.GC.GetTotalMemory(false) / 1024f / 1024f; // MB
        }

        /// <summary>
        /// Reset cultivation event for pooling
        /// </summary>
        private void ResetEvent(CultivationEvent cultEvent)
        {
            cultEvent.EventType = CultivationEventType.PlantAdded;
            cultEvent.PlantId = null;
            cultEvent.Description = null;
            cultEvent.Timestamp = 0f;
        }

        /// <summary>
        /// Reset update data for pooling
        /// </summary>
        private void ResetUpdateData(PlantUpdateData updateData)
        {
            updateData.PlantId = null;
            updateData.DeltaTime = 0f;
        }

        #endregion

    public int TickPriority => 100;
    public bool IsTickable => enabled && gameObject.activeInHierarchy;

    public void Tick(float deltaTime)
    {
            if (IsInitialized)
                UpdatePlants(deltaTime);
    }

    private void Awake()
    {
        UpdateOrchestrator.Instance.RegisterTickable(this);
    }

    private void OnDestroy()
    {
        UpdateOrchestrator.Instance.UnregisterTickable(this);
        _plants?.Dispose();
        _plantIdToIndex?.Dispose();
        _eventQueue?.Dispose();
        _eventPool?.Dispose();
        _updateDataPool?.Dispose();
    }
    }

    #region Data Structures

    /// <summary>
    /// Cultivation event for memory optimization
    /// </summary>
}
