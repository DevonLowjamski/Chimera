using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Cultivation.Components
{
    /// <summary>
    /// BASIC: Simple plant care component for Project Chimera's cultivation system.
    /// Focuses on essential plant care operations without complex lifecycle management and training.
    /// </summary>
    public class PlantCare : MonoBehaviour
    {
        [Header("Basic Plant Care Settings")]
        [SerializeField] private bool _enableBasicCare = true;
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private float _defaultWaterAmount = 0.5f;
        [SerializeField] private float _defaultNutrientAmount = 0.4f;

        // Basic plant care tracking
        private readonly Dictionary<string, PlantCareState> _plantCareStates = new Dictionary<string, PlantCareState>();
        private bool _isInitialized = false;

        /// <summary>
        /// Events for plant care operations
        /// </summary>
        public event System.Action<string, float> OnPlantWatered;
        public event System.Action<string, float> OnPlantFed;
        public event System.Action<string> OnCareNeeded;

        /// <summary>
        /// Initialize basic plant care
        /// </summary>
        public void Initialize()
        {
            if (_isInitialized) return;

            _isInitialized = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("[PlantCare] Initialized successfully");
            }
        }

        /// <summary>
        /// Add plant to care tracking
        /// </summary>
        public void AddPlant(string plantId)
        {
            if (!_plantCareStates.ContainsKey(plantId))
            {
                _plantCareStates[plantId] = new PlantCareState
                {
                    PlantId = plantId,
                    WaterLevel = 1.0f,
                    NutrientLevel = 1.0f,
                    Health = 1.0f,
                    LastWatered = Time.time,
                    LastFed = Time.time,
                    NeedsCare = false
                };

                if (_enableLogging)
                {
                    ChimeraLogger.Log($"[PlantCare] Added plant {plantId} to care tracking");
                }
            }
        }

        /// <summary>
        /// Remove plant from care tracking
        /// </summary>
        public void RemovePlant(string plantId)
        {
            if (_plantCareStates.Remove(plantId))
            {
                if (_enableLogging)
                {
                    ChimeraLogger.Log($"[PlantCare] Removed plant {plantId} from care tracking");
                }
            }
        }

        /// <summary>
        /// Water a plant
        /// </summary>
        public bool WaterPlant(string plantId, float waterAmount = 0f)
        {
            if (!_enableBasicCare || !_isInitialized || !_plantCareStates.ContainsKey(plantId))
            {
                return false;
            }

            if (waterAmount <= 0f)
            {
                waterAmount = _defaultWaterAmount;
            }

            var state = _plantCareStates[plantId];

            // Apply water with diminishing returns for overwatering
            float currentWater = state.WaterLevel;
            float newWaterLevel = Mathf.Min(1.0f, currentWater + waterAmount);

            // Reduced effectiveness if already well-watered
            if (currentWater > 0.8f)
            {
                newWaterLevel = Mathf.Min(1.0f, currentWater + waterAmount * 0.7f);
            }

            state.WaterLevel = newWaterLevel;
            state.LastWatered = Time.time;

            // Small health boost from proper watering
            if (currentWater < 0.3f)
            {
                state.Health = Mathf.Min(1.0f, state.Health + 0.05f);
            }

            OnPlantWatered?.Invoke(plantId, waterAmount);

            if (_enableLogging)
            {
                ChimeraLogger.Log($"[PlantCare] Watered plant {plantId} - Level: {newWaterLevel:F2}");
            }

            return true;
        }

        /// <summary>
        /// Feed a plant nutrients
        /// </summary>
        public bool FeedPlant(string plantId, float nutrientAmount = 0f)
        {
            if (!_enableBasicCare || !_isInitialized || !_plantCareStates.ContainsKey(plantId))
            {
                return false;
            }

            if (nutrientAmount <= 0f)
            {
                nutrientAmount = _defaultNutrientAmount;
            }

            var state = _plantCareStates[plantId];

            // Apply nutrients with diminishing returns for over-fertilizing
            float currentNutrients = state.NutrientLevel;
            float newNutrientLevel = Mathf.Min(1.0f, currentNutrients + nutrientAmount);

            // Reduced effectiveness if already well-fed
            if (currentNutrients > 0.8f)
            {
                newNutrientLevel = Mathf.Min(1.0f, currentNutrients + nutrientAmount * 0.7f);
            }

            state.NutrientLevel = newNutrientLevel;
            state.LastFed = Time.time;

            // Small health boost from proper feeding
            if (currentNutrients < 0.3f)
            {
                state.Health = Mathf.Min(1.0f, state.Health + 0.05f);
            }

            OnPlantFed?.Invoke(plantId, nutrientAmount);

            if (_enableLogging)
            {
                ChimeraLogger.Log($"[PlantCare] Fed plant {plantId} - Level: {newNutrientLevel:F2}");
            }

            return true;
        }

        /// <summary>
        /// Update plant care needs
        /// </summary>
        private void Update()
        {
            if (!_enableBasicCare || !_isInitialized) return;

            // Simple decay over time
            float deltaTime = Time.deltaTime;
            float decayRate = 0.01f * deltaTime; // Slow decay

            foreach (var kvp in _plantCareStates)
            {
                var state = kvp.Value;

                // Decay water and nutrients over time
                state.WaterLevel = Mathf.Max(0f, state.WaterLevel - decayRate);
                state.NutrientLevel = Mathf.Max(0f, state.NutrientLevel - decayRate * 0.7f); // Nutrients decay slower

                // Health affected by care levels
                if (state.WaterLevel < 0.2f || state.NutrientLevel < 0.2f)
                {
                    state.Health = Mathf.Max(0f, state.Health - decayRate * 2f);
                }

                // Check if care is needed
                bool needsCare = state.WaterLevel < 0.3f || state.NutrientLevel < 0.3f;
                if (needsCare != state.NeedsCare)
                {
                    state.NeedsCare = needsCare;
                    if (needsCare)
                    {
                        OnCareNeeded?.Invoke(state.PlantId);
                    }
                }
            }
        }

        /// <summary>
        /// Get plant care state
        /// </summary>
        public PlantCareState GetPlantState(string plantId)
        {
            return _plantCareStates.TryGetValue(plantId, out var state) ? state : null;
        }

        /// <summary>
        /// Get all plant IDs being cared for
        /// </summary>
        public List<string> GetAllPlantIds()
        {
            return new List<string>(_plantCareStates.Keys);
        }

        /// <summary>
        /// Get plants that need care
        /// </summary>
        public List<string> GetPlantsNeedingCare()
        {
            var needingCare = new List<string>();
            foreach (var state in _plantCareStates.Values)
            {
                if (state.NeedsCare)
                {
                    needingCare.Add(state.PlantId);
                }
            }
            return needingCare;
        }

        /// <summary>
        /// Get plant care statistics
        /// </summary>
        public PlantCareStats GetStats()
        {
            int totalPlants = _plantCareStates.Count;
            int plantsNeedingCare = GetPlantsNeedingCare().Count;
            int healthyPlants = _plantCareStates.Count(s => s.Value.Health > 0.8f);
            int unhealthyPlants = _plantCareStates.Count(s => s.Value.Health < 0.5f);

            return new PlantCareStats
            {
                TotalPlants = totalPlants,
                PlantsNeedingCare = plantsNeedingCare,
                HealthyPlants = healthyPlants,
                UnhealthyPlants = unhealthyPlants,
                IsCareEnabled = _enableBasicCare,
                IsInitialized = _isInitialized
            };
        }

        /// <summary>
        /// Clear all plant care data
        /// </summary>
        public void ClearAllPlants()
        {
            _plantCareStates.Clear();

            if (_enableLogging)
            {
                ChimeraLogger.Log("[PlantCare] Cleared all plant care data");
            }
        }
    }

    /// <summary>
    /// Plant care state
    /// </summary>
    [System.Serializable]
    public class PlantCareState
    {
        public string PlantId;
        public float WaterLevel; // 0-1
        public float NutrientLevel; // 0-1
        public float Health; // 0-1
        public float LastWatered;
        public float LastFed;
        public bool NeedsCare;
    }

    /// <summary>
    /// Plant care statistics
    /// </summary>
    [System.Serializable]
    public struct PlantCareStats
    {
        public int TotalPlants;
        public int PlantsNeedingCare;
        public int HealthyPlants;
        public int UnhealthyPlants;
        public bool IsCareEnabled;
        public bool IsInitialized;
    }
}
