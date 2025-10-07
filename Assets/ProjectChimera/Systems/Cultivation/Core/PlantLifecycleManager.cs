using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using ProjectChimera.Data.Cultivation;
using ProjectChimera.Data.Shared;
using ProjectChimera.Systems.Cultivation.Advanced;
using System;

namespace ProjectChimera.Systems.Cultivation.Core
{
    /// <summary>
    /// REFACTORED: Plant Lifecycle Manager - Focused plant state and lifecycle management
    /// Handles plant registration, state transitions, and lifecycle events
    /// Single Responsibility: Plant lifecycle coordination
    /// </summary>
    public class PlantLifecycleManager : MonoBehaviour
    {
        [Header("Plant Lifecycle Settings")]
        [SerializeField] private bool _enableLifecycleManagement = true;
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private float _lifecycleUpdateInterval = 1f;

        // Plant management
        private readonly Dictionary<string, AdvancedPlantInstance> _managedPlants = new Dictionary<string, AdvancedPlantInstance>();
        private readonly List<AdvancedPlantInstance> _plantsToUpdate = new List<AdvancedPlantInstance>();
        private readonly HashSet<string> _plantsToRemove = new HashSet<string>();

        // Statistics
        private PlantLifecycleStats _stats = new PlantLifecycleStats();
        private float _lastUpdateTime;

        // Properties
        public bool IsEnabled { get; private set; } = true;
        public PlantLifecycleStats GetStats() => _stats;
        public Dictionary<string, AdvancedPlantInstance> GetManagedPlants() => new Dictionary<string, AdvancedPlantInstance>(_managedPlants);

        // Events
        public System.Action<AdvancedPlantInstance> OnPlantGrowthStageChanged;
        public System.Action<AdvancedPlantInstance> OnPlantHarvestReady;
        public System.Action<AdvancedPlantInstance> OnPlantStateChanged;
        public System.Action<string> OnPlantRemoved;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            _lastUpdateTime = Time.time;
            _stats = new PlantLifecycleStats();

            if (_enableLogging)
                ChimeraLogger.Log("CULTIVATION", "🌱 PlantLifecycleManager initialized", this);
        }

        /// <summary>
        /// Register plant for lifecycle management
        /// </summary>
        public bool RegisterPlant(AdvancedPlantInstance plant)
        {
            if (!_enableLifecycleManagement || plant == null || string.IsNullOrEmpty(plant.PlantId))
            {
                _stats.UpdateErrors++;
                return false;
            }

            if (_managedPlants.ContainsKey(plant.PlantId))
            {
                if (_enableLogging)
                    ChimeraLogger.Log("CULTIVATION", $"Plant {plant.PlantId} already registered", this);
                return false;
            }

            _managedPlants[plant.PlantId] = plant;
            _stats.RegisteredPlants++;
            _stats.ManagedPlants = _managedPlants.Count;

            // Setup plant lifecycle events
            SetupPlantEvents(plant);

            if (_enableLogging)
                ChimeraLogger.Log("CULTIVATION", $"Registered plant: {plant.PlantId}", this);

            return true;
        }

        /// <summary>
        /// Unregister plant from lifecycle management
        /// </summary>
        public bool UnregisterPlant(string plantId)
        {
            if (!_managedPlants.ContainsKey(plantId))
                return false;

            var plant = _managedPlants[plantId];
            CleanupPlantEvents(plant);

            _managedPlants.Remove(plantId);
            _stats.ManagedPlants = _managedPlants.Count;

            OnPlantRemoved?.Invoke(plantId);

            if (_enableLogging)
                ChimeraLogger.Log("CULTIVATION", $"Unregistered plant: {plantId}", this);

            return true;
        }

        /// <summary>
        /// Update all managed plants
        /// </summary>
        public void UpdatePlants()
        {
            if (!IsEnabled || !_enableLifecycleManagement) return;

            float currentTime = Time.time;
            if (currentTime - _lastUpdateTime < _lifecycleUpdateInterval) return;

            _lastUpdateTime = currentTime;
            var startTime = Time.realtimeSinceStartup;

            _plantsToUpdate.Clear();
            _plantsToRemove.Clear();

            // Collect plants that need updates
            foreach (var kvp in _managedPlants)
            {
                var plant = kvp.Value;
                if (plant == null)
                {
                    _plantsToRemove.Add(kvp.Key);
                    continue;
                }

                if (ShouldUpdatePlant(plant))
                {
                    _plantsToUpdate.Add(plant);
                }
            }

            // Remove null plants
            foreach (var plantId in _plantsToRemove)
            {
                UnregisterPlant(plantId);
            }

            // Update plants
            foreach (var plant in _plantsToUpdate)
            {
                try
                {
                    UpdatePlantLifecycle(plant);
                    _stats.PlantsUpdated++;
                }
                catch (System.Exception ex)
                {
                    _stats.UpdateErrors++;
                    if (_enableLogging)
                        ChimeraLogger.LogError("CULTIVATION", $"Error updating plant {plant.PlantId}: {ex.Message}", this);
                }
            }

            // Update statistics
            var endTime = Time.realtimeSinceStartup;
            var updateTime = endTime - startTime;
            _stats.LastUpdateTime = updateTime;
            _stats.AverageUpdateTime = (_stats.AverageUpdateTime + updateTime) / 2f;
            if (updateTime > _stats.MaxUpdateTime) _stats.MaxUpdateTime = updateTime;
        }

        /// <summary>
        /// Get plant by ID
        /// </summary>
        public AdvancedPlantInstance GetPlant(string plantId)
        {
            return _managedPlants.TryGetValue(plantId, out var plant) ? plant : null;
        }

        /// <summary>
        /// Get plants by state
        /// </summary>
        public List<AdvancedPlantInstance> GetPlantsByState(PlantState state)
        {
            var result = new List<AdvancedPlantInstance>();
            foreach (var plant in _managedPlants.Values)
            {
                if (plant.CurrentState == state)
                {
                    result.Add(plant);
                }
            }
            return result;
        }

        /// <summary>
        /// Set system enabled/disabled
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;
            if (_enableLogging)
                ChimeraLogger.Log("CULTIVATION", $"PlantLifecycleManager: {(enabled ? "enabled" : "disabled")}", this);
        }

        /// <summary>
        /// Set update interval
        /// </summary>
        public void SetUpdateInterval(float interval)
        {
            _lifecycleUpdateInterval = Mathf.Max(0.1f, interval);
        }

        #region Private Methods

        /// <summary>
        /// Setup lifecycle events for a plant
        /// </summary>
        private void SetupPlantEvents(AdvancedPlantInstance plant)
        {
            // Subscribe to plant events if available
            // This would connect to plant-specific event systems
        }

        /// <summary>
        /// Cleanup events for a plant
        /// </summary>
        private void CleanupPlantEvents(AdvancedPlantInstance plant)
        {
            // Unsubscribe from plant events
        }

        /// <summary>
        /// Check if plant should be updated this frame
        /// </summary>
        private bool ShouldUpdatePlant(AdvancedPlantInstance plant)
        {
            // Update based on plant state and time since last update
            return plant.NeedsUpdate ||
                   (Time.time - plant.LastUpdateTime) >= _lifecycleUpdateInterval;
        }

        /// <summary>
        /// Update individual plant lifecycle
        /// </summary>
        private void UpdatePlantLifecycle(AdvancedPlantInstance plant)
        {
            var previousState = plant.CurrentState;
            var previousGrowthStage = plant.GrowthStage;

            // Process lifecycle transitions
            ProcessGrowthStageTransition(plant);
            ProcessStateTransition(plant);
            ProcessHarvestReadiness(plant);

            // Update timestamp
            plant.LastUpdateTime = Time.time;

            // Fire events if state changed
            if (previousState != plant.CurrentState)
            {
                OnPlantStateChanged?.Invoke(plant);
            }

            if (previousGrowthStage != plant.GrowthStage)
            {
                OnPlantGrowthStageChanged?.Invoke(plant);
            }
        }

        /// <summary>
        /// Process growth stage transitions
        /// </summary>
        private void ProcessGrowthStageTransition(AdvancedPlantInstance plant)
        {
            // Check if plant should advance to next growth stage
            if (plant.GrowthProgress >= 1.0f && plant.GrowthStage != GrowthStage.Harvest)
            {
                var nextStage = GetNextGrowthStage(plant.GrowthStage);
                if (nextStage != plant.GrowthStage)
                {
                    plant.GrowthStage = nextStage;
                    plant.GrowthProgress = 0f;
                }
            }
        }

        /// <summary>
        /// Process plant state transitions
        /// </summary>
        private void ProcessStateTransition(AdvancedPlantInstance plant)
        {
            switch (plant.CurrentState)
            {
                case PlantState.Healthy:
                    if (plant.HealthPercentage < 50f)
                        plant.CurrentState = PlantState.Stressed;
                    break;

                case PlantState.Stressed:
                    if (plant.HealthPercentage > 75f)
                        plant.CurrentState = PlantState.Healthy;
                    else if (plant.HealthPercentage < 25f)
                        plant.CurrentState = PlantState.Dying;
                    break;

                case PlantState.Dying:
                    if (plant.HealthPercentage > 50f)
                        plant.CurrentState = PlantState.Stressed;
                    else if (plant.HealthPercentage <= 0f)
                        plant.CurrentState = PlantState.Dead;
                    break;
            }
        }

        /// <summary>
        /// Process harvest readiness
        /// </summary>
        private void ProcessHarvestReadiness(AdvancedPlantInstance plant)
        {
            if (plant.GrowthStage == GrowthStage.Harvest && !plant.IsHarvestReady)
            {
                if (plant.GrowthProgress >= 0.8f) // 80% progress for harvest readiness
                {
                    plant.IsHarvestReady = true;
                    OnPlantHarvestReady?.Invoke(plant);
                }
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
                case GrowthStage.Harvest: return GrowthStage.Harvest; // Stay at harvest
                default: return currentStage;
            }
        }

        #endregion
    }

    /// <summary>
    /// Plant lifecycle management statistics
    /// </summary>
    [System.Serializable]
    public struct PlantLifecycleStats
    {
        public int ManagedPlants;
        public int RegisteredPlants;
        public int PlantsUpdated;
        public int UpdateErrors;
        public float AverageUpdateTime;
        public float MaxUpdateTime;
        public float LastUpdateTime;
    }
}