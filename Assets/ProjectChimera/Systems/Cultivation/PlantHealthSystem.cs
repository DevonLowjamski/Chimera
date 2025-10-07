using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core;

namespace ProjectChimera.Systems.Cultivation
{
    /// <summary>
    /// SIMPLE: Basic plant health system aligned with Project Chimera's cultivation vision.
    /// Focuses on essential plant health tracking for basic plant care mechanics.
    /// </summary>
    public class PlantHealthSystem : MonoBehaviour
    {
        [Header("Basic Health Settings")]
        [SerializeField] private bool _enableBasicHealth = true;
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private float _maxHealth = 100f;
        [SerializeField] private float _healthDecayRate = 1f; // Health lost per day without care

        // Basic health tracking
        private float _currentHealth = 100f;
        private float _lastCareTime = 0f;
        private bool _isInitialized = false;

        // Additional health properties needed by PlantInstance
        private float _waterLevel = 1f;
        private float _nutrientLevel = 1f;
        private float _stressLevel = 0f;

        /// <summary>
        /// Events for health changes
        /// </summary>
        public event System.Action<float> OnHealthChanged;
        public event System.Action OnPlantDied;

        // Properties accessed by PlantInstance
        public float CurrentHealth => _currentHealth;
        public float MaxHealth => _maxHealth;
        public float WaterLevel => _waterLevel;
        public float NutrientLevel => _nutrientLevel;
        public float StressLevel => _stressLevel;

        /// <summary>
        /// Initialize the basic health system
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            _currentHealth = _maxHealth;
            _lastCareTime = Time.time;
            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", this);
            }
        }

        /// <summary>
        /// Update plant health
        /// </summary>
        public void UpdateHealth(float deltaTime)
        {
            if (!_enableBasicHealth || !_isInitialized) return;

            // Simple health decay over time
            float timeSinceCare = Time.time - _lastCareTime;
            float healthLoss = (timeSinceCare / 86400f) * _healthDecayRate; // Convert to days

            _currentHealth = Mathf.Max(0f, _currentHealth - healthLoss);

            if (_currentHealth <= 0f)
            {
                OnPlantDied?.Invoke();
                if (_enableLogging)
                {
                    ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", this);
                }
            }
        }

        /// <summary>
        /// Apply care to the plant
        /// </summary>
        public void ApplyCare(float careAmount)
        {
            if (!_enableBasicHealth || !_isInitialized) return;

            _currentHealth = Mathf.Min(_maxHealth, _currentHealth + careAmount);
            _lastCareTime = Time.time;

            OnHealthChanged?.Invoke(_currentHealth);

            if (_enableLogging)
            {
                ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", this);
            }
        }

        /// <summary>
        /// Water the plant
        /// </summary>
        public void WaterPlant(float waterAmount)
        {
            ApplyCare(waterAmount * 10f); // Water provides care

            if (_enableLogging)
            {
                ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", this);
            }
        }

        /// <summary>
        /// Feed the plant nutrients
        /// </summary>
        public void FeedPlant(float nutrientAmount)
        {
            ApplyCare(nutrientAmount * 15f); // Nutrients provide more care

            if (_enableLogging)
            {
                ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", this);
            }
        }

        /// <summary>
        /// Get current health
        /// </summary>
        public float GetCurrentHealth()
        {
            return _currentHealth;
        }

        /// <summary>
        /// Get health percentage (0-1)
        /// </summary>
        public float GetHealthPercentage()
        {
            return _currentHealth / _maxHealth;
        }

        /// <summary>
        /// Check if plant is alive
        /// </summary>
        public bool IsAlive()
        {
            return _currentHealth > 0f;
        }

        /// <summary>
        /// Check if plant is healthy
        /// </summary>
        public bool IsHealthy()
        {
            return GetHealthPercentage() > 0.7f;
        }

        /// <summary>
        /// Get time since last care
        /// </summary>
        public float GetTimeSinceCare()
        {
            return Time.time - _lastCareTime;
        }

        /// <summary>
        /// Reset plant health
        /// </summary>
        public void ResetHealth()
        {
            _currentHealth = _maxHealth;
            _lastCareTime = Time.time;
            OnHealthChanged?.Invoke(_currentHealth);

            if (_enableLogging)
            {
                ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", this);
            }
        }

        /// <summary>
        /// Set maximum health
        /// </summary>
        public void SetMaxHealth(float maxHealth)
        {
            _maxHealth = Mathf.Max(1f, maxHealth);

            if (_enableLogging)
            {
                ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", this);
            }
        }

        /// <summary>
        /// Get health statistics
        /// </summary>
        public HealthStatistics GetHealthStatistics()
        {
            return new HealthStatistics
            {
                CurrentHealth = _currentHealth,
                MaxHealth = _maxHealth,
                HealthPercentage = GetHealthPercentage(),
                TimeSinceCare = GetTimeSinceCare(),
                IsAlive = IsAlive(),
                IsHealthy = IsHealthy()
            };
        }

        #region PlantInstance Compatibility Methods

        /// <summary>
        /// Set health value
        /// </summary>
        public void SetHealth(float health)
        {
            _currentHealth = Mathf.Clamp(health, 0f, _maxHealth);
            OnHealthChanged?.Invoke(_currentHealth);
        }

        /// <summary>
        /// Set water level
        /// </summary>
        public void SetWaterLevel(float waterLevel)
        {
            _waterLevel = Mathf.Clamp01(waterLevel);
        }

        /// <summary>
        /// Set nutrient level
        /// </summary>
        public void SetNutrientLevel(float nutrientLevel)
        {
            _nutrientLevel = Mathf.Clamp01(nutrientLevel);
        }

        /// <summary>
        /// Set stress level
        /// </summary>
        public void SetStressLevel(float stressLevel)
        {
            _stressLevel = Mathf.Max(0f, stressLevel);
        }

        // UpdateHealthStatus method moved to PlantInstance integration section (line 362)

        /// <summary>
        /// Apply health change
        /// </summary>
        public void ApplyHealthChange(float healthChange)
        {
            _currentHealth = Mathf.Clamp(_currentHealth + healthChange, 0f, _maxHealth);
            OnHealthChanged?.Invoke(_currentHealth);
        }

        /// <summary>
        /// Apply stress to the plant
        /// </summary>
        public void ApplyStress(float stressAmount)
        {
            _stressLevel += stressAmount;
            // Stress reduces health over time
            ApplyHealthChange(-stressAmount * 0.1f);
        }

        /// <summary>
        /// Remove stress from the plant
        /// </summary>
        public void RemoveStress(float stressAmount)
        {
            _stressLevel = Mathf.Max(0f, _stressLevel - stressAmount);
        }

        /// <summary>
        /// Get health metrics
        /// </summary>
        public object GetHealthMetrics()
        {
            return new
            {
                CurrentHealth = _currentHealth,
                MaxHealth = _maxHealth,
                WaterLevel = _waterLevel,
                NutrientLevel = _nutrientLevel,
                StressLevel = _stressLevel,
                HealthPercentage = GetHealthPercentage(),
                IsAlive = IsAlive(),
                IsHealthy = IsHealthy()
            };
        }

        #endregion
        /// <summary>
        /// Properties and methods required by PlantInstance integration
        /// </summary>
        public List<string> ActiveStressors { get; private set; } = new List<string>();

        /// <summary>
        /// Apply temperature stress to the plant
        /// </summary>
        public void ApplyTemperatureStress(float stressLevel, float deltaTime = 0f)
        {
            ApplyStress(stressLevel);
            ActiveStressors.Add($"Temperature ({stressLevel:F2})");
        }

        /// <summary>
        /// Apply light stress to the plant
        /// </summary>
        public void ApplyLightStress(float stressLevel, float deltaTime = 0f)
        {
            ApplyStress(stressLevel);
            ActiveStressors.Add($"Light ({stressLevel:F2})");
        }

        /// <summary>
        /// Apply water stress to the plant
        /// </summary>
        public void ApplyWaterStress(float stressLevel, float deltaTime = 0f)
        {
            ApplyStress(stressLevel);
            ActiveStressors.Add($"Water ({stressLevel:F2})");
        }

        /// <summary>
        /// Apply nutrient stress to the plant
        /// </summary>
        public void ApplyNutrientStress(float stressLevel, float deltaTime = 0f)
        {
            ApplyStress(stressLevel);
            ActiveStressors.Add($"Nutrient ({stressLevel:F2})");
        }

        /// <summary>
        /// Apply atmospheric stress to the plant
        /// </summary>
        public void ApplyAtmosphericStress(float stressLevel, float deltaTime = 0f)
        {
            ApplyStress(stressLevel);
            ActiveStressors.Add($"Atmospheric ({stressLevel:F2})");
        }

        // Private field for tracking last update time
        private System.DateTime _lastUpdateTime = System.DateTime.Now;

        /// <summary>
        /// Set current health directly
        /// </summary>
        public void SetCurrentHealth(float health)
        {
            _currentHealth = Mathf.Clamp(health, 0f, _maxHealth);
            _lastUpdateTime = System.DateTime.Now;
        }

        /// <summary>
        /// Update health status (required by PlantInstanceCore)
        /// </summary>
        public void UpdateHealthStatus(float deltaTime)
        {
            if (!_enableBasicHealth || !_isInitialized) return;

            // Clear old stressors each update
            ActiveStressors.Clear();

            // Update health based on time and environmental factors
            // This would be expanded for biological accuracy in Phase 2
            _lastUpdateTime = System.DateTime.Now;
        }
    }

    /// <summary>
    /// Plant health metrics for instance integration
    /// </summary>
    [System.Serializable]
    public class PlantHealthMetrics
    {
        public float CurrentHealth;
        public float MaxHealth;
        public float HealthPercentage;
        public float TimeSinceCare;
        public bool IsAlive;
        public bool IsHealthy;
        public string PlantId;
        public System.DateTime LastUpdateTime;
    }

    [System.Serializable]
    public class HealthStatistics
    {
        public float CurrentHealth;
        public float MaxHealth;
        public float HealthPercentage;
        public float TimeSinceCare;
        public bool IsAlive;
        public bool IsHealthy;
    }
}
