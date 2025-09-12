using UnityEngine;
using ProjectChimera.Core.Logging;
using ProjectChimera.Data.Shared;

namespace ProjectChimera.Systems.Cultivation
{
    /// <summary>
    /// BASIC: Simple plant growth system for Project Chimera's cultivation system.
    /// Focuses on essential plant growth without complex calculators and yield calculations.
    /// </summary>
    public class PlantGrowthSystem : MonoBehaviour
    {
        [Header("Basic Growth Settings")]
        [SerializeField] private bool _enableBasicGrowth = true;
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private float _baseGrowthRate = 1.0f;
        [SerializeField] private float _updateInterval = 1.0f;

        // Basic growth state
        private string _plantId;
        private PlantGrowthStage _currentStage = PlantGrowthStage.Seedling;
        private float _growthProgress = 0f; // 0-1 within current stage
        private float _totalGrowthTime = 0f;
        private bool _isInitialized = false;
        private float _lastUpdateTime = 0f;

        /// <summary>
        /// Events for growth changes
        /// </summary>
        public event System.Action<string, PlantGrowthStage> OnStageChanged;
        public event System.Action<string, float> OnGrowthProgressChanged;

        /// <summary>
        /// Initialize basic growth system
        /// </summary>
        public void Initialize(string plantId, PlantGrowthStage initialStage = PlantGrowthStage.Seedling)
        {
            _plantId = plantId;
            _currentStage = initialStage;
            _growthProgress = 0f;
            _totalGrowthTime = 0f;
            _lastUpdateTime = Time.time;
            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log($"[PlantGrowthSystem] Initialized plant {plantId} in {initialStage} stage");
            }
        }

        /// <summary>
        /// Update growth system
        /// </summary>
        private void Update()
        {
            if (!_enableBasicGrowth || !_isInitialized) return;

            float currentTime = Time.time;
            if (currentTime - _lastUpdateTime >= _updateInterval)
            {
                float deltaTime = currentTime - _lastUpdateTime;
                UpdateGrowth(deltaTime);
                _lastUpdateTime = currentTime;
            }
        }

        /// <summary>
        /// Update plant growth
        /// </summary>
        private void UpdateGrowth(float deltaTime)
        {
            _totalGrowthTime += deltaTime;

            // Calculate growth increment based on current stage
            float growthIncrement = _baseGrowthRate * deltaTime * GetStageMultiplier(_currentStage);
            _growthProgress += growthIncrement;

            OnGrowthProgressChanged?.Invoke(_plantId, _growthProgress);

            // Check for stage advancement
            if (_growthProgress >= 1.0f)
            {
                AdvanceStage();
            }
        }

        /// <summary>
        /// Advance to next growth stage
        /// </summary>
        private void AdvanceStage()
        {
            PlantGrowthStage oldStage = _currentStage;
            _currentStage = GetNextStage(_currentStage);
            _growthProgress = 0f; // Reset progress for new stage

            OnStageChanged?.Invoke(_plantId, _currentStage);

            if (_enableLogging)
            {
                ChimeraLogger.Log($"[PlantGrowthSystem] Plant {_plantId} advanced from {oldStage} to {_currentStage}");
            }

            // Check if plant is ready for harvest
            if (_currentStage == PlantGrowthStage.Mature)
            {
                if (_enableLogging)
                {
                    ChimeraLogger.Log($"[PlantGrowthSystem] Plant {_plantId} is ready for harvest");
                }
            }
        }

        /// <summary>
        /// Manually set growth stage
        /// </summary>
        public void SetGrowthStage(PlantGrowthStage newStage)
        {
            if (!_isInitialized || newStage == _currentStage) return;

            _currentStage = newStage;
            _growthProgress = 0f;

            OnStageChanged?.Invoke(_plantId, _currentStage);

            if (_enableLogging)
            {
                ChimeraLogger.Log($"[PlantGrowthSystem] Plant {_plantId} stage manually set to {newStage}");
            }
        }

        /// <summary>
        /// Get current growth stage
        /// </summary>
        public PlantGrowthStage GetCurrentStage()
        {
            return _currentStage;
        }

        /// <summary>
        /// Get growth progress (0-1)
        /// </summary>
        public float GetGrowthProgress()
        {
            return _growthProgress;
        }

        /// <summary>
        /// Get total growth time
        /// </summary>
        public float GetTotalGrowthTime()
        {
            return _totalGrowthTime;
        }

        /// <summary>
        /// Check if plant is ready for harvest
        /// </summary>
        public bool IsReadyForHarvest()
        {
            return _currentStage == PlantGrowthStage.Mature;
        }

        /// <summary>
        /// Harvest the plant
        /// </summary>
        public float Harvest()
        {
            if (!IsReadyForHarvest()) return 0f;

            // Simple yield calculation based on growth time and health
            float yield = Mathf.Clamp(_totalGrowthTime / 100f, 0.1f, 2.0f); // Basic yield

            if (_enableLogging)
            {
                ChimeraLogger.Log($"[PlantGrowthSystem] Plant {_plantId} harvested with yield {yield:F2}");
            }

            // Reset for potential re-growth or remove
            _growthProgress = 0f;
            _totalGrowthTime = 0f;

            return yield;
        }

        /// <summary>
        /// Get growth system statistics
        /// </summary>
        public GrowthSystemStats GetStats()
        {
            return new GrowthSystemStats
            {
                PlantId = _plantId,
                CurrentStage = _currentStage,
                GrowthProgress = _growthProgress,
                TotalGrowthTime = _totalGrowthTime,
                IsReadyForHarvest = IsReadyForHarvest(),
                IsGrowthEnabled = _enableBasicGrowth,
                IsInitialized = _isInitialized
            };
        }

        /// <summary>
        /// Reset growth system
        /// </summary>
        public void Reset()
        {
            _currentStage = PlantGrowthStage.Seedling;
            _growthProgress = 0f;
            _totalGrowthTime = 0f;
            _lastUpdateTime = Time.time;

            if (_enableLogging)
            {
                ChimeraLogger.Log($"[PlantGrowthSystem] Plant {_plantId} growth reset");
            }
        }

        #region Private Methods

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
                case PlantGrowthStage.Mature: return PlantGrowthStage.Mature; // Stay mature
                default: return currentStage;
            }
        }

        #endregion
    }

    /// <summary>
    /// Growth system statistics
    /// </summary>
    [System.Serializable]
    public struct GrowthSystemStats
    {
        public string PlantId;
        public PlantGrowthStage CurrentStage;
        public float GrowthProgress;
        public float TotalGrowthTime;
        public bool IsReadyForHarvest;
        public bool IsGrowthEnabled;
        public bool IsInitialized;
    }
}
