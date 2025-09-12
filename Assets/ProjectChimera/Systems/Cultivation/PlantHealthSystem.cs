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

        /// <summary>
        /// Events for health changes
        /// </summary>
        public event System.Action<float> OnHealthChanged;
        public event System.Action OnPlantDied;

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
                ChimeraLogger.Log("[PlantHealthSystem] Initialized successfully");
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
                    ChimeraLogger.Log("[PlantHealthSystem] Plant has died");
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
                ChimeraLogger.Log($"[PlantHealthSystem] Applied care: +{careAmount:F1}, Health: {_currentHealth:F1}/{_maxHealth:F1}");
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
                ChimeraLogger.Log($"[PlantHealthSystem] Plant watered: {waterAmount:F1}");
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
                ChimeraLogger.Log($"[PlantHealthSystem] Plant fed: {nutrientAmount:F1}");
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
                ChimeraLogger.Log("[PlantHealthSystem] Health reset to maximum");
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
                ChimeraLogger.Log($"[PlantHealthSystem] Max health set to {_maxHealth:F1}");
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
    }

    /// <summary>
    /// Health statistics
    /// </summary>
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
