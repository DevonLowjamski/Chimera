using UnityEngine;
using ProjectChimera.Core.Logging;
using ProjectChimera.Data.Shared;

namespace ProjectChimera.Systems.Cultivation
{
    /// <summary>
    /// BASIC: Simple plant physiology for Project Chimera's cultivation system.
    /// Focuses on essential plant health and growth without complex calculations.
    /// </summary>
    public class PlantPhysiology : MonoBehaviour
    {
        [Header("Basic Plant Settings")]
        [SerializeField] private bool _enableBasicPhysiology = true;
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private float _healthDecayRate = 0.1f;
        [SerializeField] private float _waterConsumptionRate = 0.05f;
        [SerializeField] private float _nutrientConsumptionRate = 0.03f;

        // Basic plant state
        private string _plantId;
        private float _health = 1.0f; // 0-1
        private float _waterLevel = 1.0f; // 0-1
        private float _nutrientLevel = 1.0f; // 0-1
        private PlantGrowthStage _currentStage = PlantGrowthStage.Seedling;
        private bool _isInitialized = false;

        /// <summary>
        /// Events for plant physiology
        /// </summary>
        public event System.Action<string, float> OnHealthChanged;
        public event System.Action<string, PlantGrowthStage> OnStageChanged;
        public event System.Action<string> OnPlantDied;

        /// <summary>
        /// Initialize plant physiology
        /// </summary>
        public void Initialize(string plantId, PlantGrowthStage initialStage = PlantGrowthStage.Seedling)
        {
            _plantId = plantId;
            _currentStage = initialStage;
            _health = 1.0f;
            _waterLevel = 1.0f;
            _nutrientLevel = 1.0f;
            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log($"[PlantPhysiology] Initialized plant {plantId} in {initialStage} stage");
            }
        }

        /// <summary>
        /// Update plant physiology
        /// </summary>
        private void Update()
        {
            if (!_enableBasicPhysiology || !_isInitialized) return;

            // Simple health decay over time
            float deltaTime = Time.deltaTime;
            _health = Mathf.Max(0f, _health - _healthDecayRate * deltaTime * 0.01f); // Very slow decay

            // Consume water and nutrients
            _waterLevel = Mathf.Max(0f, _waterLevel - _waterConsumptionRate * deltaTime * 0.01f);
            _nutrientLevel = Mathf.Max(0f, _nutrientLevel - _nutrientConsumptionRate * deltaTime * 0.01f);

            // Health affected by water and nutrients
            if (_waterLevel < 0.2f || _nutrientLevel < 0.2f)
            {
                _health = Mathf.Max(0f, _health - 0.05f * deltaTime);
            }

            OnHealthChanged?.Invoke(_plantId, _health);

            // Check if plant died
            if (_health <= 0f)
            {
                OnPlantDied?.Invoke(_plantId);

                if (_enableLogging)
                {
                    ChimeraLogger.Log($"[PlantPhysiology] Plant {_plantId} died");
                }
            }
        }

        /// <summary>
        /// Water the plant
        /// </summary>
        public void WaterPlant(float amount = 1.0f)
        {
            if (!_isInitialized) return;

            _waterLevel = Mathf.Min(1.0f, _waterLevel + amount);

            if (_enableLogging)
            {
                ChimeraLogger.Log($"[PlantPhysiology] Plant {_plantId} watered, water level: {_waterLevel:F2}");
            }
        }

        /// <summary>
        /// Feed the plant nutrients
        /// </summary>
        public void FeedPlant(float amount = 1.0f)
        {
            if (!_isInitialized) return;

            _nutrientLevel = Mathf.Min(1.0f, _nutrientLevel + amount);

            if (_enableLogging)
            {
                ChimeraLogger.Log($"[PlantPhysiology] Plant {_plantId} fed, nutrient level: {_nutrientLevel:F2}");
            }
        }

        /// <summary>
        /// Heal the plant
        /// </summary>
        public void HealPlant(float amount = 0.5f)
        {
            if (!_isInitialized) return;

            _health = Mathf.Min(1.0f, _health + amount);
            OnHealthChanged?.Invoke(_plantId, _health);

            if (_enableLogging)
            {
                ChimeraLogger.Log($"[PlantPhysiology] Plant {_plantId} healed, health: {_health:F2}");
            }
        }

        /// <summary>
        /// Set plant growth stage
        /// </summary>
        public void SetGrowthStage(PlantGrowthStage newStage)
        {
            if (!_isInitialized || newStage == _currentStage) return;

            var oldStage = _currentStage;
            _currentStage = newStage;
            OnStageChanged?.Invoke(_plantId, newStage);

            if (_enableLogging)
            {
                ChimeraLogger.Log($"[PlantPhysiology] Plant {_plantId} stage changed from {oldStage} to {newStage}");
            }
        }

        /// <summary>
        /// Get plant health
        /// </summary>
        public float GetHealth()
        {
            return _health;
        }

        /// <summary>
        /// Get water level
        /// </summary>
        public float GetWaterLevel()
        {
            return _waterLevel;
        }

        /// <summary>
        /// Get nutrient level
        /// </summary>
        public float GetNutrientLevel()
        {
            return _nutrientLevel;
        }

        /// <summary>
        /// Get current growth stage
        /// </summary>
        public PlantGrowthStage GetGrowthStage()
        {
            return _currentStage;
        }

        /// <summary>
        /// Check if plant is alive
        /// </summary>
        public bool IsAlive()
        {
            return _isInitialized && _health > 0f;
        }

        /// <summary>
        /// Check if plant needs water
        /// </summary>
        public bool NeedsWater()
        {
            return _waterLevel < 0.3f;
        }

        /// <summary>
        /// Check if plant needs nutrients
        /// </summary>
        public bool NeedsNutrients()
        {
            return _nutrientLevel < 0.3f;
        }

        /// <summary>
        /// Get plant statistics
        /// </summary>
        public PlantStats GetStats()
        {
            return new PlantStats
            {
                PlantId = _plantId,
                Health = _health,
                WaterLevel = _waterLevel,
                NutrientLevel = _nutrientLevel,
                CurrentStage = _currentStage,
                IsAlive = IsAlive(),
                NeedsWater = NeedsWater(),
                NeedsNutrients = NeedsNutrients(),
                PhysiologyEnabled = _enableBasicPhysiology
            };
        }

        /// <summary>
        /// Reset plant to initial state
        /// </summary>
        public void ResetPlant()
        {
            _health = 1.0f;
            _waterLevel = 1.0f;
            _nutrientLevel = 1.0f;
            _currentStage = PlantGrowthStage.Seedling;

            if (_enableLogging)
            {
                ChimeraLogger.Log($"[PlantPhysiology] Plant {_plantId} reset to initial state");
            }
        }
    }

    /// <summary>
    /// Plant statistics
    /// </summary>
    [System.Serializable]
    public struct PlantStats
    {
        public string PlantId;
        public float Health;
        public float WaterLevel;
        public float NutrientLevel;
        public PlantGrowthStage CurrentStage;
        public bool IsAlive;
        public bool NeedsWater;
        public bool NeedsNutrients;
        public bool PhysiologyEnabled;
    }
}
