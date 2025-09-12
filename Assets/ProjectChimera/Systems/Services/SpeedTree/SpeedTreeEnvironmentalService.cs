using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;
using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Systems.Services.SpeedTree.Environmental;
using EnvironmentalConditions = ProjectChimera.Data.Shared.EnvironmentalConditions;

namespace ProjectChimera.Systems.Services.SpeedTree
{
    /// <summary>
    /// PC014-5c: SpeedTree Environmental Service - Orchestrator
    /// Coordinates modular environmental systems for cannabis cultivation plants.
    /// Manages: Environmental Response, Wind System, Seasonal Changes, and Stress Visualization.
    /// </summary>
    public class SpeedTreeEnvironmentalService : MonoBehaviour, ITickable, ISpeedTreeEnvironmentalService
    {
        #region Modular Components

        [Header("Modular Environmental Systems")]
        [SerializeField] private EnvironmentalResponseSystem environmentalResponseSystem;
        [SerializeField] private WindSystem windSystem;
        [SerializeField] private SeasonalSystem seasonalSystem;
        [SerializeField] private StressVisualizationSystem stressVisualizationSystem;

        #endregion

        #region Properties

        public bool IsInitialized { get; private set; }
        public bool EnableEnvironmentalResponse => environmentalResponseSystem?.enabled ?? false;
        public bool EnableWindAnimation => windSystem?.enabled ?? false;
        public bool EnableSeasonalChanges => seasonalSystem?.enabled ?? false;
        public bool EnableStressVisualization => stressVisualizationSystem?.enabled ?? false;

        #endregion

        #region Events

        public event Action<EnvironmentalConditions> OnEnvironmentalConditionsChanged;
        public event Action<float> OnWindStrengthChanged;
        public event Action<int, float> OnPlantStressChanged;
        public event Action<Season> OnSeasonChanged;
        public event Action<int, Season, float> OnPlantSeasonalEffect;

        #endregion

        #region IService Implementation

        public void Initialize()
        {
            if (IsInitialized) return;

            ChimeraLogger.Log("[SpeedTreeEnvironmentalService] Initializing environmental orchestrator...");

            // Initialize modular systems in dependency order
            InitializeEnvironmentalResponseSystem();
            InitializeWindSystem();
            InitializeSeasonalSystem();
            InitializeStressVisualizationSystem();

            // Connect system events
            ConnectSystemEvents();

            IsInitialized = true;
            ChimeraLogger.Log("[SpeedTreeEnvironmentalService] Environmental orchestrator initialized successfully");
        }

        public void Shutdown()
        {
            if (!IsInitialized) return;

            ChimeraLogger.Log("[SpeedTreeEnvironmentalService] Shutting down environmental orchestrator...");

            // Shutdown systems in reverse order
            if (stressVisualizationSystem != null) stressVisualizationSystem.Shutdown();
            if (seasonalSystem != null) seasonalSystem.Shutdown();
            if (windSystem != null) windSystem.Shutdown();
            if (environmentalResponseSystem != null) environmentalResponseSystem.Shutdown();

            IsInitialized = false;
            ChimeraLogger.Log("[SpeedTreeEnvironmentalService] Environmental orchestrator shutdown complete");
        }

        private void InitializeEnvironmentalResponseSystem()
        {
            if (environmentalResponseSystem == null)
            {
                environmentalResponseSystem = gameObject.AddComponent<EnvironmentalResponseSystem>();
            }
            environmentalResponseSystem.Initialize();
        }

        private void InitializeWindSystem()
        {
            if (windSystem == null)
            {
                windSystem = gameObject.AddComponent<WindSystem>();
            }
            windSystem.Initialize();
        }

        private void InitializeSeasonalSystem()
        {
            if (seasonalSystem == null)
            {
                seasonalSystem = gameObject.AddComponent<SeasonalSystem>();
            }
            seasonalSystem.Initialize();
        }

        private void InitializeStressVisualizationSystem()
        {
            if (stressVisualizationSystem == null)
            {
                stressVisualizationSystem = gameObject.AddComponent<StressVisualizationSystem>();
            }
            stressVisualizationSystem.Initialize();
        }

        private void ConnectSystemEvents()
        {
            // Connect wind system events
            if (windSystem != null)
            {
                windSystem.OnWindStrengthChanged += (strength) => OnWindStrengthChanged?.Invoke(strength);
            }

            // Connect seasonal system events
            if (seasonalSystem != null)
            {
                seasonalSystem.OnSeasonChanged += (season) => OnSeasonChanged?.Invoke(season);
                seasonalSystem.OnPlantSeasonalEffect += (plantId, season, effect) =>
                    OnPlantSeasonalEffect?.Invoke(plantId, season, effect);
            }

            // Connect stress visualization events
            if (stressVisualizationSystem != null)
            {
                stressVisualizationSystem.OnPlantStressVisualizationChanged += (plantId, stress) =>
                    OnPlantStressChanged?.Invoke(plantId, stress);
            }

            // Connect environmental response events
            if (environmentalResponseSystem != null)
            {
                environmentalResponseSystem.OnPlantEnvironmentalResponse += (plantId, conditions) =>
                    OnEnvironmentalConditionsChanged?.Invoke(conditions);
            }
        }

        #endregion

        #region Core Environmental Operations

        /// <summary>
        /// Updates environmental response for a plant
        /// </summary>
        public void UpdateEnvironmentalResponse(int plantId, EnvironmentalConditions conditions)
        {
            if (!ValidateInitialization()) return;

            try
            {
                environmentalResponseSystem?.UpdatePlantEnvironmentalResponse(plantId, conditions);
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[SpeedTreeEnvironmentalService] Failed to update environmental response for plant {plantId}: {ex.Message}");
            }
        }

        /// <summary>
        /// Applies environmental conditions to a plant
        /// </summary>
        public void ApplyEnvironmentalConditions(int plantId, EnvironmentalConditions conditions)
        {
            if (!ValidateInitialization()) return;

            try
            {
                environmentalResponseSystem?.ApplyEnvironmentalConditions(plantId, conditions);
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[SpeedTreeEnvironmentalService] Failed to apply conditions to plant {plantId}: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates seasonal changes for plants
        /// </summary>
        public void UpdateSeasonalChanges(IEnumerable<int> plantIds)
        {
            if (!ValidateInitialization()) return;

            try
            {
                seasonalSystem?.ApplySeasonalEffects(plantIds);
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[SpeedTreeEnvironmentalService] Failed to update seasonal changes: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates plant stress visualization
        /// </summary>
        public void UpdatePlantStressVisualization(int plantId, float healthLevel, float stressLevel)
        {
            if (!ValidateInitialization()) return;

            try
            {
                stressVisualizationSystem?.UpdatePlantStressVisualization(plantId, healthLevel, stressLevel);
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[SpeedTreeEnvironmentalService] Failed to update stress visualization for plant {plantId}: {ex.Message}");
            }
        }

        /// <summary>
        /// Updates wind system
        /// </summary>
        public void UpdateWindSystem()
        {
            if (!ValidateInitialization()) return;

            try
            {
                windSystem?.UpdateGlobalWind();
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[SpeedTreeEnvironmentalService] Failed to update wind system: {ex.Message}");
            }
        }

        #endregion

        #region System Control Methods

        /// <summary>
        /// Sets wind strength
        /// </summary>
        public void SetWindStrength(float strength)
        {
            if (!ValidateInitialization()) return;

            try
            {
                windSystem?.SetWindStrength(strength);
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[SpeedTreeEnvironmentalService] Failed to set wind strength: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets wind direction
        /// </summary>
        public void SetWindDirection(Vector3 direction)
        {
            if (!ValidateInitialization()) return;

            try
            {
                windSystem?.SetWindDirection(direction);
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[SpeedTreeEnvironmentalService] Failed to set wind direction: {ex.Message}");
            }
        }

        /// <summary>
        /// Forces a specific season
        /// </summary>
        public void SetSeason(Season season)
        {
            if (!ValidateInitialization()) return;

            try
            {
                seasonalSystem?.SetSeason(season);
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[SpeedTreeEnvironmentalService] Failed to set season: {ex.Message}");
            }
        }

        /// <summary>
        /// Sets stress visualization intensity
        /// </summary>
        public void SetStressVisualizationIntensity(float intensity)
        {
            if (!ValidateInitialization()) return;

            try
            {
                stressVisualizationSystem?.SetVisualizationIntensity(intensity);
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[SpeedTreeEnvironmentalService] Failed to set visualization intensity: {ex.Message}");
            }
        }

        #endregion

        #region Query Methods

        /// <summary>
        /// Gets current season
        /// </summary>
        public Season GetCurrentSeason()
        {
            if (!ValidateInitialization()) return Season.Spring;
            return seasonalSystem?.GetCurrentSeason() ?? Season.Spring;
        }

        /// <summary>
        /// Gets current wind strength
        /// </summary>
        public float GetCurrentWindStrength()
        {
            if (!ValidateInitialization()) return 0f;
            return windSystem?.GetCurrentWindStrength() ?? 0f;
        }

        /// <summary>
        /// Gets current wind direction
        /// </summary>
        public Vector3 GetCurrentWindDirection()
        {
            if (!ValidateInitialization()) return Vector3.zero;
            return windSystem?.GetCurrentWindDirection() ?? Vector3.zero;
        }

        /// <summary>
        /// Gets seasonal growth multiplier
        /// </summary>
        public float GetSeasonalGrowthMultiplier()
        {
            if (!ValidateInitialization()) return 1f;
            return seasonalSystem?.GetSeasonalGrowthMultiplier() ?? 1f;
        }

        /// <summary>
        /// Gets seasonal stress multiplier
        /// </summary>
        public float GetSeasonalStressMultiplier()
        {
            if (!ValidateInitialization()) return 1f;
            return seasonalSystem?.GetSeasonalStressMultiplier() ?? 1f;
        }

        #endregion

        #region Unity Lifecycle

        protected virtual void Start()
        {
            // Register with UpdateOrchestrator
            UpdateOrchestrator.Instance?.RegisterTickable(this);
        }

        protected virtual void OnDestroy()
        {
            // Unregister from UpdateOrchestrator
            UpdateOrchestrator.Instance?.UnregisterTickable(this);
        }

        #endregion

        #region ITickable Implementation

        public void Tick(float deltaTime)
        {
            if (!ValidateInitialization() || !enabled || !gameObject.activeInHierarchy) return;

            try
            {
                // Update all environmental systems
                environmentalResponseSystem?.Tick(deltaTime);
                windSystem?.Tick(deltaTime);
                seasonalSystem?.Tick(deltaTime);
                stressVisualizationSystem?.Tick(deltaTime);
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[SpeedTreeEnvironmentalService] Error during environmental update: {ex.Message}");
            }
        }

        public int Priority => 10; // Environmental updates have medium priority
        public bool Enabled => enabled && gameObject.activeInHierarchy;

        public virtual void OnRegistered()
        {
            ChimeraLogger.Log("[SpeedTreeEnvironmentalService] Registered with UpdateOrchestrator");
        }

        public virtual void OnUnregistered()
        {
            ChimeraLogger.Log("[SpeedTreeEnvironmentalService] Unregistered from UpdateOrchestrator");
        }

        #endregion

        #region Helper Methods

        private bool ValidateInitialization()
        {
            if (!IsInitialized)
            {
                ChimeraLogger.LogWarning("[SpeedTreeEnvironmentalService] Service not initialized");
                return false;
            }
            return true;
        }

        #endregion

        #region Legacy Interface Support

        // Support for legacy interface methods (can be removed after full migration)
        public void UpdateEnvironmentalResponse(int plantId, object conditions)
        {
            if (conditions is EnvironmentalConditions envConditions)
            {
                UpdateEnvironmentalResponse(plantId, envConditions);
            }
        }

        public void ApplyEnvironmentalConditions(int plantId, object conditions)
        {
            if (conditions is EnvironmentalConditions envConditions)
            {
                ApplyEnvironmentalConditions(plantId, envConditions);
            }
        }

        #endregion
    }
}
