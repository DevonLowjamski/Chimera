using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using ProjectChimera.Data.Shared;

namespace ProjectChimera.Systems.Cultivation
{
    /// <summary>
    /// BASIC: Simple plant growth service for Project Chimera's cultivation system.
    /// Focuses on essential plant growth without complex calculators and genetic factors.
    /// </summary>
    public class PlantGrowthService : MonoBehaviour
    {
        [Header("Basic Growth Settings")]
        [SerializeField] private bool _enableBasicGrowth = true;
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private float _baseGrowthRate = 1.0f;
        [SerializeField] private float _updateInterval = 1.0f;

        // Basic growth tracking
        private readonly Dictionary<string, PlantGrowthData> _plantGrowthData = new Dictionary<string, PlantGrowthData>();
        private float _lastUpdateTime = 0f;
        private bool _isInitialized = false;

        /// <summary>
        /// Events for growth operations
        /// </summary>
        public event System.Action<string, PlantGrowthStage> OnGrowthStageChanged;
        public event System.Action<string, float> OnGrowthProgressUpdated;

        /// <summary>
        /// Initialize basic growth service
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            _isInitialized = true;
            _lastUpdateTime = Time.time;

            if (_enableLogging)
            {
                ChimeraLogger.Log("[PlantGrowthService] Initialized successfully");
            }
        }

        /// <summary>
        /// Update plant growth
        /// </summary>
        private void Update()
        {
            if (!_enableBasicGrowth || !_isInitialized) return;

            float currentTime = Time.time;
            if (currentTime - _lastUpdateTime >= _updateInterval)
            {
                UpdateAllPlantGrowth(currentTime - _lastUpdateTime);
                _lastUpdateTime = currentTime;
            }
        }

        /// <summary>
        /// Add plant to growth tracking
        /// </summary>
        public void AddPlant(string plantId, PlantGrowthStage initialStage = PlantGrowthStage.Seedling)
        {
            if (!_plantGrowthData.ContainsKey(plantId))
            {
                _plantGrowthData[plantId] = new PlantGrowthData
                {
                    PlantId = plantId,
                    CurrentStage = initialStage,
                    GrowthProgress = 0f,
                    Age = 0f,
                    LastUpdateTime = Time.time
                };

                if (_enableLogging)
                {
                    ChimeraLogger.Log($"[PlantGrowthService] Added plant {plantId} in {initialStage} stage");
                }
            }
        }

        /// <summary>
        /// Remove plant from growth tracking
        /// </summary>
        public void RemovePlant(string plantId)
        {
            if (_plantGrowthData.Remove(plantId))
            {
                if (_enableLogging)
                {
                    ChimeraLogger.Log($"[PlantGrowthService] Removed plant {plantId}");
                }
            }
        }

        /// <summary>
        /// Update specific plant growth
        /// </summary>
        public void UpdatePlantGrowth(string plantId, float deltaTime)
        {
            if (!_plantGrowthData.ContainsKey(plantId)) return;

            var data = _plantGrowthData[plantId];
            var previousStage = data.CurrentStage;

            // Simple growth calculation
            float growthAmount = _baseGrowthRate * deltaTime * GetStageMultiplier(data.CurrentStage);
            data.GrowthProgress += growthAmount;
            data.Age += deltaTime;

            // Check for stage progression
            if (data.GrowthProgress >= 1.0f)
            {
                data.GrowthProgress = 0f;
                data.CurrentStage = GetNextStage(data.CurrentStage);

                if (data.CurrentStage != previousStage)
                {
                    OnGrowthStageChanged?.Invoke(plantId, data.CurrentStage);

                    if (_enableLogging)
                    {
                        ChimeraLogger.Log($"[PlantGrowthService] Plant {plantId} progressed to {data.CurrentStage}");
                    }
                }
            }

            OnGrowthProgressUpdated?.Invoke(plantId, data.GrowthProgress);
        }

        /// <summary>
        /// Get plant growth data
        /// </summary>
        public PlantGrowthData GetPlantData(string plantId)
        {
            return _plantGrowthData.TryGetValue(plantId, out var data) ? data : null;
        }

        /// <summary>
        /// Get plant growth stage
        /// </summary>
        public PlantGrowthStage GetPlantStage(string plantId)
        {
            var data = GetPlantData(plantId);
            return data != null ? data.CurrentStage : PlantGrowthStage.Seedling;
        }

        /// <summary>
        /// Get plant growth progress (0-1)
        /// </summary>
        public float GetPlantProgress(string plantId)
        {
            var data = GetPlantData(plantId);
            return data != null ? data.GrowthProgress : 0f;
        }

        /// <summary>
        /// Manually advance plant stage
        /// </summary>
        public void AdvancePlantStage(string plantId)
        {
            if (_plantGrowthData.ContainsKey(plantId))
            {
                var data = _plantGrowthData[plantId];
                var nextStage = GetNextStage(data.CurrentStage);

                if (nextStage != data.CurrentStage)
                {
                    data.CurrentStage = nextStage;
                    data.GrowthProgress = 0f;

                    OnGrowthStageChanged?.Invoke(plantId, data.CurrentStage);

                    if (_enableLogging)
                    {
                        ChimeraLogger.Log($"[PlantGrowthService] Manually advanced {plantId} to {data.CurrentStage}");
                    }
                }
            }
        }

        /// <summary>
        /// Get all plant IDs
        /// </summary>
        public List<string> GetAllPlantIds()
        {
            return new List<string>(_plantGrowthData.Keys);
        }

        /// <summary>
        /// Clear all plant data
        /// </summary>
        public void ClearAllPlants()
        {
            _plantGrowthData.Clear();

            if (_enableLogging)
            {
                ChimeraLogger.Log("[PlantGrowthService] Cleared all plant data");
            }
        }

        /// <summary>
        /// Get growth service statistics
        /// </summary>
        public GrowthServiceStats GetStats()
        {
            int totalPlants = _plantGrowthData.Count;
            int seedlings = _plantGrowthData.Count(p => p.Value.CurrentStage == PlantGrowthStage.Seedling);
            int vegetative = _plantGrowthData.Count(p => p.Value.CurrentStage == PlantGrowthStage.Vegetative);
            int flowering = _plantGrowthData.Count(p => p.Value.CurrentStage == PlantGrowthStage.Flowering);
            int mature = _plantGrowthData.Count(p => p.Value.CurrentStage == PlantGrowthStage.Mature);

            return new GrowthServiceStats
            {
                TotalPlants = totalPlants,
                Seedlings = seedlings,
                Vegetative = vegetative,
                Flowering = flowering,
                Mature = mature,
                IsGrowthEnabled = _enableBasicGrowth
            };
        }

        #region Private Methods

        private void UpdateAllPlantGrowth(float deltaTime)
        {
            foreach (var plantId in new List<string>(_plantGrowthData.Keys))
            {
                UpdatePlantGrowth(plantId, deltaTime);
            }
        }

        private float GetStageMultiplier(PlantGrowthStage stage)
        {
            // Different stages have different growth rates
            switch (stage)
            {
                case PlantGrowthStage.Seedling: return 0.5f;
                case PlantGrowthStage.Vegetative: return 1.0f;
                case PlantGrowthStage.Flowering: return 0.8f;
                case PlantGrowthStage.Mature: return 0.2f;
                default: return 1.0f;
            }
        }

        private PlantGrowthStage GetNextStage(PlantGrowthStage currentStage)
        {
            switch (currentStage)
            {
                case PlantGrowthStage.Seedling: return PlantGrowthStage.Vegetative;
                case PlantGrowthStage.Vegetative: return PlantGrowthStage.Flowering;
                case PlantGrowthStage.Flowering: return PlantGrowthStage.Mature;
                case PlantGrowthStage.Mature: return PlantGrowthStage.Mature;
                default: return currentStage;
            }
        }

        #endregion
    }

    /// <summary>
    /// Plant growth data
    /// </summary>
    [System.Serializable]
    public class PlantGrowthData
    {
        public string PlantId;
        public PlantGrowthStage CurrentStage;
        public float GrowthProgress; // 0-1 within current stage
        public float Age; // total age in seconds
        public float LastUpdateTime;
    }

    /// <summary>
    /// Growth service statistics
    /// </summary>
    [System.Serializable]
    public struct GrowthServiceStats
    {
        public int TotalPlants;
        public int Seedlings;
        public int Vegetative;
        public int Flowering;
        public int Mature;
        public bool IsGrowthEnabled;
    }
}
