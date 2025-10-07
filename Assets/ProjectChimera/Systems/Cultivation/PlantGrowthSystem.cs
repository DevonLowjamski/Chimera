using UnityEngine;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;
using ProjectChimera.Core;
using ProjectChimera.Data.Shared;
using System;

namespace ProjectChimera.Systems.Cultivation
{
    /// <summary>
    /// ENHANCED: Plant growth system with ITickable integration.
    /// Migrated from Update() to centralized tick system for better performance.
    /// Focuses on essential plant growth without complex calculators and yield calculations.
    /// </summary>
    public class PlantGrowthSystem : MonoBehaviour, ITickable
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

        // Additional plant properties needed by PlantInstance
        private float _daysSincePlanted = 0f;
        private float _plantSize = 1f;
        private float _yieldPotential = 100f;
        private float _qualityPotential = 80f;
        private bool _isHarvestable = false;

        /// <summary>
        /// Events for growth changes
        /// </summary>
        public event System.Action<string, PlantGrowthStage> OnStageChanged;
        public event System.Action<string, float> OnGrowthProgressChanged;
        public event System.Action<PlantGrowthStage, PlantGrowthStage> OnGrowthStageChanged;

        // Properties accessed by PlantInstance
        public PlantGrowthStage CurrentGrowthStage => _currentStage;
        public float GrowthProgress => _growthProgress;
        public float OverallGrowthProgress => (_growthProgress + (int)_currentStage) / 4f; // Assuming 4 stages
        public float DaysSincePlanted => _daysSincePlanted;
        public float PlantSize => _plantSize;
        public float YieldPotential => _yieldPotential;
        public float QualityPotential => _qualityPotential;
        public bool IsHarvestable => _isHarvestable;

        /// <summary>
        /// ITickable implementation - high priority for plant systems
        /// </summary>
        public int TickPriority => 100; // High priority plant system
        public bool IsTickable => _isInitialized && _enableBasicGrowth && isActiveAndEnabled;

        /// <summary>
        /// Initialize basic growth system and register with UpdateOrchestrator
        /// </summary>
        public void Initialize(string plantId, PlantGrowthStage initialStage = PlantGrowthStage.Seedling)
        {
            _plantId = plantId;
            _currentStage = initialStage;
            _growthProgress = 0f;
            _totalGrowthTime = 0f;
            _lastUpdateTime = Time.time;
            _isInitialized = true;

            // Register with centralized update system
            var orchestrator = ServiceContainerFactory.Instance.TryResolve<UpdateOrchestrator>();
            if (orchestrator != null)
            {
                orchestrator.RegisterTickable(this);
                if (_enableLogging)
                {
                    ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", this);
                }
            }
            else
            {
                // Fallback: Find orchestrator in scene if ServiceContainer unavailable
                var fallbackOrchestrator = ServiceContainerFactory.Instance.TryResolve<UpdateOrchestrator>();
                if (fallbackOrchestrator != null)
                {
                    fallbackOrchestrator.RegisterTickable(this);
                    if (_enableLogging)
                    {
                        ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", this);
                    }
                }
            }

            if (_enableLogging)
            {
                ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", this);
            }
        }

        /// <summary>
        /// ITickable implementation - update growth system
        /// </summary>
        public void Tick(float deltaTime)
        {
            if (!_enableBasicGrowth || !_isInitialized) return;

            float currentTime = Time.time;
            if (currentTime - _lastUpdateTime >= _updateInterval)
            {
                float actualDeltaTime = currentTime - _lastUpdateTime;
                UpdateGrowth(actualDeltaTime);
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
                ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", this);
            }

            // Check if plant is ready for harvest
            if (_currentStage == PlantGrowthStage.Mature)
            {
                if (_enableLogging)
                {
                    ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", this);
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
                ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", this);
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
                ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", this);
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
                ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", this);
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

        #region PlantInstance Compatibility Methods

        /// <summary>
        /// Set days since planted
        /// </summary>
        public void SetDaysSincePlanted(float days)
        {
            _daysSincePlanted = days;
        }

        /// <summary>
        /// Set growth progress
        /// </summary>
        public void SetGrowthProgress(float progress)
        {
            _growthProgress = Mathf.Clamp01(progress);
        }

        /// <summary>
        /// Update growth progress
        /// </summary>
        public void UpdateGrowthProgress(float deltaTime)
        {
            ProcessGrowth(deltaTime);
        }

        /// <summary>
        /// Advance growth stage
        /// </summary>
        public void AdvanceGrowthStage()
        {
            var oldStage = _currentStage;
            _currentStage = GetNextStage(_currentStage);
            _growthProgress = 0f;
            OnGrowthStageChanged?.Invoke(oldStage, _currentStage);
        }

        /// <summary>
        /// Apply growth rate modifier
        /// </summary>
        public void ApplyGrowthRate(float rateModifier)
        {
            _baseGrowthRate *= rateModifier;
        }

        /// <summary>
        /// Sprout the plant (seedling stage)
        /// </summary>
        public void Sprout()
        {
            _currentStage = PlantGrowthStage.Seedling;
            _growthProgress = 0f;
            _daysSincePlanted = 0f;
        }

        /// <summary>
        /// Get growth metrics
        /// </summary>
        public object GetGrowthMetrics()
        {
            return new
            {
                CurrentStage = _currentStage,
                GrowthProgress = _growthProgress,
                DaysSincePlanted = _daysSincePlanted,
                PlantSize = _plantSize,
                YieldPotential = _yieldPotential,
                QualityPotential = _qualityPotential
            };
        }

        /// <summary>
        /// Process growth (compatibility method)
        /// </summary>
        private void ProcessGrowth(float deltaTime)
        {
            UpdateGrowth(deltaTime);
        }

        #endregion

        /// <summary>
        /// Unity lifecycle - ensure proper cleanup
        /// </summary>
        private void OnDestroy()
        {
            // Unregister from UpdateOrchestrator if available
            var orchestrator = ServiceContainerFactory.Instance.TryResolve<UpdateOrchestrator>();
            if (orchestrator != null)
            {
                orchestrator.UnregisterTickable(this);
                if (_enableLogging)
                {
                    ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", this);
                }
            }
        }
    }

    /// <summary>
    /// Plant growth metrics for instance integration
    /// </summary>
    [System.Serializable]
    public class PlantGrowthMetrics
    {
        public PlantGrowthStage CurrentStage;
        public float GrowthProgress;
        public float TotalGrowthTime;
        public bool IsReadyForHarvest;
        public bool IsGrowthEnabled;
        public string PlantId;
        public DateTime LastUpdateTime;
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
