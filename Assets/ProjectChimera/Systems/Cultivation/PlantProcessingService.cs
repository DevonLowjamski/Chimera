using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Cultivation
{
    /// <summary>
    /// BASIC: Simple plant processing service for Project Chimera's cultivation system.
    /// Focuses on essential plant updates without complex processors and batch systems.
    /// </summary>
    public class PlantProcessingService : MonoBehaviour
    {
        [Header("Basic Processing Settings")]
        [SerializeField] private bool _enableBasicProcessing = true;
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private float _updateInterval = 1.0f;
        [SerializeField] private int _maxPlantsPerUpdate = 50;

        // Basic processing state
        private readonly List<BasicPlantData> _plantsToProcess = new List<BasicPlantData>();
        private float _lastUpdateTime = 0f;
        private bool _isInitialized = false;

        /// <summary>
        /// Events for plant processing
        /// </summary>
        public event System.Action<string> OnPlantProcessed;
        public event System.Action<int> OnBatchProcessed;

        /// <summary>
        /// Initialize basic plant processing
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            _lastUpdateTime = Time.time;
            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("[PlantProcessingService] Initialized successfully");
            }
        }

        /// <summary>
        /// Update plant processing
        /// </summary>
        private void Update()
        {
            if (!_enableBasicProcessing || !_isInitialized) return;

            float currentTime = Time.time;
            if (currentTime - _lastUpdateTime >= _updateInterval)
            {
                ProcessPlantBatch();
                _lastUpdateTime = currentTime;
            }
        }

        /// <summary>
        /// Add plant to processing queue
        /// </summary>
        public void AddPlant(string plantId, Vector3 position, float health, float growthStage)
        {
            if (!_plantsToProcess.Exists(p => p.PlantId == plantId))
            {
                _plantsToProcess.Add(new BasicPlantData
                {
                    PlantId = plantId,
                    Position = position,
                    Health = health,
                    GrowthStage = growthStage,
                    LastProcessed = Time.time
                });

                if (_enableLogging)
                {
                    ChimeraLogger.Log($"[PlantProcessingService] Added plant {plantId} to processing queue");
                }
            }
        }

        /// <summary>
        /// Remove plant from processing
        /// </summary>
        public void RemovePlant(string plantId)
        {
            _plantsToProcess.RemoveAll(p => p.PlantId == plantId);

            if (_enableLogging)
            {
                ChimeraLogger.Log($"[PlantProcessingService] Removed plant {plantId} from processing");
            }
        }

        /// <summary>
        /// Process a batch of plants
        /// </summary>
        private void ProcessPlantBatch()
        {
            int plantsToProcess = Mathf.Min(_maxPlantsPerUpdate, _plantsToProcess.Count);
            int plantsProcessed = 0;

            for (int i = 0; i < plantsToProcess; i++)
            {
                var plant = _plantsToProcess[i];
                ProcessSinglePlant(plant);
                plantsProcessed++;
            }

            if (plantsProcessed > 0)
            {
                OnBatchProcessed?.Invoke(plantsProcessed);

                if (_enableLogging)
                {
                    ChimeraLogger.Log($"[PlantProcessingService] Processed {plantsProcessed} plants in batch");
                }
            }
        }

        /// <summary>
        /// Process a single plant
        /// </summary>
        private void ProcessSinglePlant(BasicPlantData plant)
        {
            // Simple plant processing logic
            float deltaTime = Time.time - plant.LastProcessed;

            // Basic health decay over time
            plant.Health = Mathf.Max(0f, plant.Health - deltaTime * 0.01f);

            // Basic growth progression
            plant.GrowthStage = Mathf.Min(1.0f, plant.GrowthStage + deltaTime * 0.001f);

            plant.LastProcessed = Time.time;

            OnPlantProcessed?.Invoke(plant.PlantId);
        }

        /// <summary>
        /// Get plant data
        /// </summary>
        public BasicPlantData GetPlantData(string plantId)
        {
            return _plantsToProcess.Find(p => p.PlantId == plantId);
        }

        /// <summary>
        /// Get all plant IDs
        /// </summary>
        public List<string> GetAllPlantIds()
        {
            return _plantsToProcess.ConvertAll(p => p.PlantId);
        }

        /// <summary>
        /// Get plants needing attention
        /// </summary>
        public List<string> GetPlantsNeedingAttention()
        {
            return _plantsToProcess.FindAll(p => p.Health < 0.5f).ConvertAll(p => p.PlantId);
        }

        /// <summary>
        /// Get processing statistics
        /// </summary>
        public ProcessingStats GetStats()
        {
            int totalPlants = _plantsToProcess.Count;
            int healthyPlants = _plantsToProcess.Count(p => p.Health > 0.8f);
            int unhealthyPlants = _plantsToProcess.Count(p => p.Health < 0.5f);
            int maturePlants = _plantsToProcess.Count(p => p.GrowthStage > 0.9f);

            return new ProcessingStats
            {
                TotalPlants = totalPlants,
                HealthyPlants = healthyPlants,
                UnhealthyPlants = unhealthyPlants,
                MaturePlants = maturePlants,
                IsProcessingEnabled = _enableBasicProcessing,
                IsInitialized = _isInitialized
            };
        }

        /// <summary>
        /// Clear all plants
        /// </summary>
        public void ClearAllPlants()
        {
            _plantsToProcess.Clear();

            if (_enableLogging)
            {
                ChimeraLogger.Log("[PlantProcessingService] Cleared all plants from processing");
            }
        }

        /// <summary>
        /// Set processing enabled state
        /// </summary>
        public void SetProcessingEnabled(bool enabled)
        {
            _enableBasicProcessing = enabled;

            if (!enabled)
            {
                // Could pause processing or clear queue
            }

            if (_enableLogging)
            {
                ChimeraLogger.Log($"[PlantProcessingService] Processing {(enabled ? "enabled" : "disabled")}");
            }
        }
    }

    /// <summary>
    /// Basic plant data for processing
    /// </summary>
    [System.Serializable]
    public class BasicPlantData
    {
        public string PlantId;
        public Vector3 Position;
        public float Health; // 0-1
        public float GrowthStage; // 0-1
        public float LastProcessed;
    }

    /// <summary>
    /// Processing statistics
    /// </summary>
    [System.Serializable]
    public struct ProcessingStats
    {
        public int TotalPlants;
        public int HealthyPlants;
        public int UnhealthyPlants;
        public int MaturePlants;
        public bool IsProcessingEnabled;
        public bool IsInitialized;
    }
}
