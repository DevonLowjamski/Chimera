using UnityEngine;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;
using ProjectChimera.Data.Shared;
using ProjectChimera.Core.Updates;
using PlantInstance = ProjectChimera.Data.Cultivation.Plant.PlantInstance;

namespace ProjectChimera.Systems.Cultivation
{
    /// <summary>
    /// BASIC: Simple plant updating for Project Chimera's cultivation system.
    /// Focuses on essential plant updates without complex processing systems.
    /// </summary>
    public class PlantUpdateProcessor : MonoBehaviour, ITickable
    {
        [Header("Basic Update Settings")]
        [SerializeField] private bool _enableBasicUpdates = true;
        [SerializeField] private float _updateInterval = 1f; // seconds
        [SerializeField] private bool _enableLogging = true;

        // Basic plant tracking
        private readonly List<PlantInstance> _trackedPlants = new List<PlantInstance>();
        private float _lastUpdateTime = 0f;
        private bool _isInitialized = false;

        /// <summary>
        /// Events for plant updates
        /// </summary>
        public event System.Action<PlantInstance> OnPlantUpdated;
        public event System.Action<PlantInstance> OnPlantHealthChanged;

        /// <summary>
        /// Initialize basic plant processor
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            _isInitialized = true;
            _lastUpdateTime = Time.time;

            if (_enableLogging)
            {
                ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", this);
            }
        }

        /// <summary>
        /// Update all tracked plants
        /// </summary>
    [SerializeField] private float _tickInterval = 0.1f; // Configurable update frequency
    private float _lastTickTime;

    public int TickPriority => 50; // Lower priority for complex updates
    public bool IsTickable => enabled && gameObject.activeInHierarchy;

    public void Tick(float deltaTime)
    {
        _lastTickTime += deltaTime;
        if (_lastTickTime >= _tickInterval)
        {
            _lastTickTime = 0f;
                if (!_enableBasicUpdates || !_isInitialized) return;
    
                // Throttle updates to avoid performance issues
                if (Time.time - _lastUpdateTime < _updateInterval) return;
    
                _lastUpdateTime = Time.time;
    
                // Update all tracked plants
                for (int i = _trackedPlants.Count - 1; i >= 0; i--)
                {
                    var plant = _trackedPlants[i];
                    if (plant != null && plant.IsActive)
                    {
                        UpdatePlant(plant, _updateInterval);
                    }
                    else
                    {
                        // Remove inactive plants
                        _trackedPlants.RemoveAt(i);
                    }
                }
        }
    }

    private void Awake()
    {
        UpdateOrchestrator.Instance.RegisterTickable(this);
    }

    private void OnDestroy()
    {
        UpdateOrchestrator.Instance.UnregisterTickable(this);
    }

        /// <summary>
        /// Update a single plant
        /// </summary>
        public void UpdatePlant(PlantInstance plant, float deltaTime)
        {
            if (plant == null) return;

            float previousHealth = plant.Health;

            // Basic plant growth and aging
            plant.AgeInDays += Mathf.RoundToInt(deltaTime / 86400f); // Convert to days

            // Basic health decay over time (plants need care)
            float healthDecay = 0.001f * deltaTime; // Slow decay
            plant.Health = Mathf.Max(0f, plant.Health - healthDecay);

            // Basic growth based on health and time
            if (plant.Health > 0.5f && plant.AgeInDays < 90f) // 90 days max growth
            {
                // Growth affects growth progress, not stage enum
                // Use a property like GrowthProgress instead of GrowthStage enum
            }

            // Notify listeners
            OnPlantUpdated?.Invoke(plant);

            if (Mathf.Abs(plant.Health - previousHealth) > 0.01f)
            {
                OnPlantHealthChanged?.Invoke(plant);
            }

            if (_enableLogging && Random.value < 0.01f) // Log occasionally to avoid spam
            {
                ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", this);
            }
        }

        /// <summary>
        /// Add plant to tracking
        /// </summary>
        public void TrackPlant(PlantInstance plant)
        {
            if (plant != null && !_trackedPlants.Contains(plant))
            {
                _trackedPlants.Add(plant);

                if (_enableLogging)
                {
                    ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", this);
                }
            }
        }

        /// <summary>
        /// Remove plant from tracking
        /// </summary>
        public void UntrackPlant(PlantInstance plant)
        {
            if (plant != null)
            {
                _trackedPlants.Remove(plant);

                if (_enableLogging)
                {
                    ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", this);
                }
            }
        }

        /// <summary>
        /// Get all tracked plants
        /// </summary>
        public List<PlantInstance> GetTrackedPlants()
        {
            return new List<PlantInstance>(_trackedPlants);
        }

        /// <summary>
        /// Get plant count
        /// </summary>
        public int GetPlantCount()
        {
            return _trackedPlants.Count;
        }

        /// <summary>
        /// Clear all tracked plants
        /// </summary>
        public void ClearAllPlants()
        {
            _trackedPlants.Clear();

            if (_enableLogging)
            {
                ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", this);
            }
        }

        /// <summary>
        /// Apply care to a plant (watering, nutrients, etc.)
        /// </summary>
        public void ApplyCare(PlantInstance plant, PlantCareType careType)
        {
            if (plant == null) return;

            switch (careType)
            {
                case PlantCareType.Watering:
                    plant.LastWatering = System.DateTime.Now;
                    plant.Health = Mathf.Min(1f, plant.Health + 0.1f);
                    break;

                case PlantCareType.Fertilizer:
                    plant.LastFeeding = System.DateTime.Now;
                    plant.Health = Mathf.Min(1f, plant.Health + 0.05f);
                    break;

                case PlantCareType.Pruning:
                    // Slight health boost from pruning
                    plant.Health = Mathf.Min(1f, plant.Health + 0.02f);
                    break;
            }

            OnPlantHealthChanged?.Invoke(plant);

            if (_enableLogging)
            {
                ChimeraLogger.Log("CULTIVATION", "Cultivation system operation", this);
            }
        }

        /// <summary>
        /// Get update statistics
        /// </summary>
        public PlantUpdateStats GetUpdateStats()
        {
            int healthyPlants = _trackedPlants.FindAll(p => p.Health > 0.7f).Count;
            int stressedPlants = _trackedPlants.FindAll(p => p.Health < 0.5f).Count;
            float averageHealth = _trackedPlants.Count > 0 ?
                _trackedPlants.Average(p => p.Health) : 0f;

            return new PlantUpdateStats
            {
                TotalPlants = _trackedPlants.Count,
                HealthyPlants = healthyPlants,
                StressedPlants = stressedPlants,
                AverageHealth = averageHealth,
                IsUpdateEnabled = _enableBasicUpdates,
                UpdateInterval = _updateInterval
            };
        }
    }

    /// <summary>
    /// Plant care types
    /// </summary>
    public enum PlantCareType
    {
        Watering,
        Fertilizer,
        Pruning
    }

    /// <summary>
    /// Plant update statistics
    /// </summary>
    [System.Serializable]
    public struct PlantUpdateStats
    {
        public int TotalPlants;
        public int HealthyPlants;
        public int StressedPlants;
        public float AverageHealth;
        public bool IsUpdateEnabled;
        public float UpdateInterval;
    }
}
