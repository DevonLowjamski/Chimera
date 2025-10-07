using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using ProjectChimera.Data.Cultivation;
using ProjectChimera.Systems.Cultivation.Advanced;
using System;

namespace ProjectChimera.Systems.Cultivation.Core
{
    /// <summary>
    /// REFACTORED: Growth Simulation Manager - Focused plant growth calculations
    /// Handles growth rates, timing, and progression simulation
    /// Single Responsibility: Growth simulation and calculation
    /// </summary>
    public class GrowthSimulationManager : MonoBehaviour
    {
        [Header("Growth Simulation Settings")]
        [SerializeField] private bool _enableGrowthSimulation = true;
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private float _simulationUpdateInterval = 0.5f;
        [SerializeField] private float _baseGrowthRate = 1f;

        [Header("Growth Modifiers")]
        [SerializeField] private float _lightGrowthMultiplier = 1.2f;
        [SerializeField] private float _waterGrowthMultiplier = 1.1f;
        [SerializeField] private float _nutrientGrowthMultiplier = 1.15f;
        [SerializeField] private float _temperatureOptimalRange = 5f;

        // Simulation state
        private readonly Dictionary<string, GrowthSimulationData> _growthSimulations = new Dictionary<string, GrowthSimulationData>();
        private readonly List<string> _activeSimulations = new List<string>();
        private GrowthSimulationStats _stats = new GrowthSimulationStats();

        // Timing
        private float _lastSimulationUpdate;
        private float _simulationDeltaTime;

        // Properties
        public bool IsEnabled { get; private set; } = true;
        public GrowthSimulationStats GetStats() => _stats;

        // Events
        public System.Action<string, float> OnGrowthProgressUpdated;
        public System.Action<string, GrowthStage> OnGrowthStageAdvanced;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            _lastSimulationUpdate = Time.time;
            _stats = new GrowthSimulationStats();

            if (_enableLogging)
                ChimeraLogger.Log("CULTIVATION", "🌿 GrowthSimulationManager initialized", this);
        }

        /// <summary>
        /// Process growth simulation for all plants
        /// </summary>
        public void ProcessGrowthSimulation()
        {
            if (!IsEnabled || !_enableGrowthSimulation) return;

            float currentTime = Time.time;
            _simulationDeltaTime = currentTime - _lastSimulationUpdate;

            if (_simulationDeltaTime < _simulationUpdateInterval) return;

            _lastSimulationUpdate = currentTime;
            var startTime = Time.realtimeSinceStartup;

            _activeSimulations.Clear();
            foreach (var kvp in _growthSimulations)
            {
                if (kvp.Value.IsActive)
                {
                    _activeSimulations.Add(kvp.Key);
                }
            }

            foreach (var plantId in _activeSimulations)
            {
                try
                {
                    ProcessPlantGrowthSimulation(plantId);
                    _stats.SimulationsProcessed++;
                }
                catch (System.Exception ex)
                {
                    _stats.SimulationErrors++;
                    if (_enableLogging)
                        ChimeraLogger.LogError("CULTIVATION", $"Growth simulation error for plant {plantId}: {ex.Message}", this);
                }
            }

            // Update statistics
            var endTime = Time.realtimeSinceStartup;
            var simulationTime = endTime - startTime;
            _stats.LastSimulationTime = simulationTime;
            _stats.AverageGrowthTime = (_stats.AverageGrowthTime + simulationTime) / 2f;
            if (simulationTime > _stats.MaxGrowthTime) _stats.MaxGrowthTime = simulationTime;
        }

        /// <summary>
        /// Register plant for growth simulation
        /// </summary>
        public bool RegisterPlantForSimulation(AdvancedPlantInstance plant)
        {
            if (plant == null || string.IsNullOrEmpty(plant.PlantId))
                return false;

            if (_growthSimulations.ContainsKey(plant.PlantId))
                return false;

            var simulationData = new GrowthSimulationData
            {
                PlantId = plant.PlantId,
                IsActive = true,
                CurrentGrowthStage = plant.GrowthStage,
                GrowthProgress = plant.GrowthProgress,
                BaseGrowthRate = _baseGrowthRate,
                LastUpdateTime = Time.time
            };

            _growthSimulations[plant.PlantId] = simulationData;
            _stats.ActiveSimulations = _growthSimulations.Count;

            if (_enableLogging)
                ChimeraLogger.Log("CULTIVATION", $"Registered plant for growth simulation: {plant.PlantId}", this);

            return true;
        }

        /// <summary>
        /// Unregister plant from growth simulation
        /// </summary>
        public bool UnregisterPlantFromSimulation(string plantId)
        {
            if (!_growthSimulations.ContainsKey(plantId))
                return false;

            _growthSimulations.Remove(plantId);
            _stats.ActiveSimulations = _growthSimulations.Count;

            if (_enableLogging)
                ChimeraLogger.Log("CULTIVATION", $"Unregistered plant from growth simulation: {plantId}", this);

            return true;
        }

        /// <summary>
        /// Calculate growth rate based on environmental factors
        /// </summary>
        public float CalculateGrowthRate(AdvancedPlantInstance plant, EnvironmentalConditions conditions)
        {
            if (plant == null) return 0f;

            float baseRate = _baseGrowthRate;
            float finalRate = baseRate;

            // Apply environmental modifiers
            finalRate *= CalculateLightModifier(conditions.LightLevel);
            finalRate *= CalculateWaterModifier(conditions.WaterLevel);
            finalRate *= CalculateNutrientModifier(conditions.NutrientLevel);
            finalRate *= CalculateTemperatureModifier(conditions.Temperature, plant.OptimalTemperature);

            // Apply plant-specific modifiers
            finalRate *= plant.GrowthRateModifier;

            // Apply growth stage modifiers
            finalRate *= GetGrowthStageModifier(plant.GrowthStage);

            return Mathf.Max(0f, finalRate);
        }

        /// <summary>
        /// Get growth progress for plant
        /// </summary>
        public float GetGrowthProgress(string plantId)
        {
            if (_growthSimulations.TryGetValue(plantId, out var simulation))
            {
                return simulation.GrowthProgress;
            }
            return 0f;
        }

        /// <summary>
        /// Set system enabled/disabled
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;
            if (_enableLogging)
                ChimeraLogger.Log("CULTIVATION", $"GrowthSimulationManager: {(enabled ? "enabled" : "disabled")}", this);
        }

        /// <summary>
        /// Set update interval
        /// </summary>
        public void SetUpdateInterval(float interval)
        {
            _simulationUpdateInterval = Mathf.Max(0.1f, interval);
        }

        #region Private Methods

        /// <summary>
        /// Process growth simulation for individual plant
        /// </summary>
        private void ProcessPlantGrowthSimulation(string plantId)
        {
            if (!_growthSimulations.TryGetValue(plantId, out var simulation))
                return;

            // Get environmental conditions (would come from environmental system)
            var conditions = GetEnvironmentalConditions(plantId);

            // Calculate growth rate
            float growthRate = simulation.BaseGrowthRate;
            growthRate *= CalculateEnvironmentalModifiers(conditions);
            growthRate *= GetGrowthStageModifier(simulation.CurrentGrowthStage);

            // Apply growth
            float growthIncrease = growthRate * _simulationDeltaTime;
            simulation.GrowthProgress += growthIncrease;
            simulation.LastUpdateTime = Time.time;

            // Check for growth stage advancement
            if (simulation.GrowthProgress >= 1.0f && simulation.CurrentGrowthStage != GrowthStage.Harvest)
            {
                var nextStage = GetNextGrowthStage(simulation.CurrentGrowthStage);
                if (nextStage != simulation.CurrentGrowthStage)
                {
                    simulation.CurrentGrowthStage = nextStage;
                    simulation.GrowthProgress = 0f;
                    OnGrowthStageAdvanced?.Invoke(plantId, nextStage);
                }
            }

            // Update simulation data
            _growthSimulations[plantId] = simulation;

            // Fire progress event
            OnGrowthProgressUpdated?.Invoke(plantId, simulation.GrowthProgress);
        }

        /// <summary>
        /// Get environmental conditions for plant
        /// </summary>
        private EnvironmentalConditions GetEnvironmentalConditions(string plantId)
        {
            // This would interface with environmental systems
            // For now, return default conditions
            return new EnvironmentalConditions
            {
                LightLevel = 0.8f,
                WaterLevel = 0.7f,
                NutrientLevel = 0.6f,
                Temperature = 22f,
                Humidity = 0.65f
            };
        }

        /// <summary>
        /// Calculate combined environmental modifiers
        /// </summary>
        private float CalculateEnvironmentalModifiers(EnvironmentalConditions conditions)
        {
            float modifier = 1f;
            modifier *= CalculateLightModifier(conditions.LightLevel);
            modifier *= CalculateWaterModifier(conditions.WaterLevel);
            modifier *= CalculateNutrientModifier(conditions.NutrientLevel);
            modifier *= CalculateTemperatureModifier(conditions.Temperature, 22f); // Default optimal temp
            return modifier;
        }

        /// <summary>
        /// Calculate light level modifier
        /// </summary>
        private float CalculateLightModifier(float lightLevel)
        {
            if (lightLevel >= 0.8f) return _lightGrowthMultiplier;
            if (lightLevel >= 0.5f) return 1f;
            if (lightLevel >= 0.3f) return 0.8f;
            return 0.5f;
        }

        /// <summary>
        /// Calculate water level modifier
        /// </summary>
        private float CalculateWaterModifier(float waterLevel)
        {
            if (waterLevel >= 0.7f) return _waterGrowthMultiplier;
            if (waterLevel >= 0.4f) return 1f;
            if (waterLevel >= 0.2f) return 0.7f;
            return 0.4f;
        }

        /// <summary>
        /// Calculate nutrient level modifier
        /// </summary>
        private float CalculateNutrientModifier(float nutrientLevel)
        {
            if (nutrientLevel >= 0.8f) return _nutrientGrowthMultiplier;
            if (nutrientLevel >= 0.5f) return 1f;
            if (nutrientLevel >= 0.3f) return 0.8f;
            return 0.6f;
        }

        /// <summary>
        /// Calculate temperature modifier
        /// </summary>
        private float CalculateTemperatureModifier(float currentTemp, float optimalTemp)
        {
            float deviation = Mathf.Abs(currentTemp - optimalTemp);
            if (deviation <= _temperatureOptimalRange) return 1f;
            if (deviation <= _temperatureOptimalRange * 2) return 0.8f;
            if (deviation <= _temperatureOptimalRange * 3) return 0.6f;
            return 0.4f;
        }

        /// <summary>
        /// Get growth stage modifier
        /// </summary>
        private float GetGrowthStageModifier(GrowthStage stage)
        {
            switch (stage)
            {
                case GrowthStage.Seedling: return 0.8f;
                case GrowthStage.Vegetative: return 1.2f;
                case GrowthStage.Flowering: return 1f;
                case GrowthStage.Harvest: return 0.5f;
                default: return 1f;
            }
        }

        /// <summary>
        /// Get next growth stage
        /// </summary>
        private GrowthStage GetNextGrowthStage(GrowthStage currentStage)
        {
            switch (currentStage)
            {
                case GrowthStage.Seedling: return GrowthStage.Vegetative;
                case GrowthStage.Vegetative: return GrowthStage.Flowering;
                case GrowthStage.Flowering: return GrowthStage.Harvest;
                case GrowthStage.Harvest: return GrowthStage.Harvest;
                default: return currentStage;
            }
        }

        #endregion
    }

    /// <summary>
    /// Growth simulation data for individual plant
    /// </summary>
    [System.Serializable]
    public struct GrowthSimulationData
    {
        public string PlantId;
        public bool IsActive;
        public GrowthStage CurrentGrowthStage;
        public float GrowthProgress;
        public float BaseGrowthRate;
        public float LastUpdateTime;
    }

    /// <summary>
    /// Growth simulation statistics
    /// </summary>
    [System.Serializable]
    public struct GrowthSimulationStats
    {
        public int ActiveSimulations;
        public int SimulationsProcessed;
        public int SimulationErrors;
        public float AverageGrowthTime;
        public float MaxGrowthTime;
        public float LastSimulationTime;
    }

    /// <summary>
    /// Environmental conditions for growth calculation
    /// </summary>
    [System.Serializable]
    public struct EnvironmentalConditions
    {
        public float LightLevel;
        public float WaterLevel;
        public float NutrientLevel;
        public float Temperature;
        public float Humidity;
    }
}