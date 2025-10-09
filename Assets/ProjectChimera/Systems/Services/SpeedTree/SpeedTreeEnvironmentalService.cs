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

            ChimeraLogger.Log("SPEEDTREE/ENV", "SpeedTreeEnvironmentalService Initialize", this);

            // Initialize modular systems in dependency order
            InitializeEnvironmentalResponseSystem();
            InitializeWindSystem();
            InitializeSeasonalSystem();
            InitializeStressVisualizationSystem();

            // Connect system events
            ConnectSystemEvents();

            IsInitialized = true;
            ChimeraLogger.Log("SPEEDTREE/ENV", "SpeedTreeEnvironmentalService Initialized", this);
        }

        public void Shutdown()
        {
            if (!IsInitialized) return;

            ChimeraLogger.Log("SPEEDTREE/ENV", "SpeedTreeEnvironmentalService Shutdown", this);

            // Shutdown systems in reverse order
            if (stressVisualizationSystem != null) stressVisualizationSystem.Shutdown();
            if (seasonalSystem != null) seasonalSystem.Shutdown();
            if (windSystem != null) windSystem.Shutdown();
            if (environmentalResponseSystem != null) environmentalResponseSystem.Shutdown();

            IsInitialized = false;
            ChimeraLogger.Log("SPEEDTREE/ENV", "SpeedTreeEnvironmentalService Shutdown Complete", this);
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
            EnvironmentalServiceDelegator.SafeDelegate(IsInitialized, 
                () => environmentalResponseSystem?.UpdatePlantEnvironmentalResponse(plantId, conditions),
                nameof(UpdateEnvironmentalResponse), this);
        }

        /// <summary>
        /// Applies environmental conditions to a plant
        /// </summary>
        public void ApplyEnvironmentalConditions(int plantId, EnvironmentalConditions conditions)
        {
            EnvironmentalServiceDelegator.SafeDelegate(IsInitialized,
                () => environmentalResponseSystem?.ApplyEnvironmentalConditions(plantId, conditions),
                nameof(ApplyEnvironmentalConditions), this);
        }

        /// <summary>
        /// Updates seasonal changes for plants
        /// </summary>
        public void UpdateSeasonalChanges(IEnumerable<int> plantIds)
        {
            EnvironmentalServiceDelegator.SafeDelegate(IsInitialized,
                () => seasonalSystem?.ApplySeasonalEffects(Time.deltaTime),
                nameof(UpdateSeasonalChanges), this);
        }

        /// <summary>
        /// Updates plant stress visualization
        /// </summary>
        public void UpdatePlantStressVisualization(int plantId, float healthLevel, float stressLevel)
        {
            EnvironmentalServiceDelegator.SafeDelegate(IsInitialized,
                () => stressVisualizationSystem?.UpdatePlantStressVisualization(plantId, healthLevel, stressLevel),
                nameof(UpdatePlantStressVisualization), this);
        }

        /// <summary>
        /// Updates wind system
        /// </summary>
        public void UpdateWindSystem()
        {
            EnvironmentalServiceDelegator.SafeDelegate(IsInitialized,
                () => windSystem?.UpdateGlobalWind(),
                nameof(UpdateWindSystem), this);
        }

        #endregion

        #region System Control Methods

        // ISpeedTreeEnvironmentalService: UpdateEnvironment
        public void UpdateEnvironment(float deltaTime)
        {
            if (!ValidateInitialization()) return;

            // Step environmental systems forward
            environmentalResponseSystem?.Tick(deltaTime);
            windSystem?.UpdateGlobalWind();
            seasonalSystem?.UpdateSeasonalSystem(deltaTime);
            stressVisualizationSystem?.Tick(deltaTime);
        }

        // ISpeedTreeEnvironmentalService: GetEnvironmentalResponse
        public ProjectChimera.Systems.Services.SpeedTree.Environmental.EnvironmentalResponseData GetEnvironmentalResponse(int plantId)
        {
            // Try to construct from detailed response if available
            var response = environmentalResponseSystem?.GetPlantResponseData(plantId);
            var data = new ProjectChimera.Systems.Services.SpeedTree.Environmental.EnvironmentalResponseData
            {
                PlantId = plantId
            };

            if (response.HasValue)
            {
                // Map stresses (0-1, where higher stress => lower influence)
                data.TemperatureInfluence = 1f - Mathf.Clamp01(response.Value.TemperatureStress);
                data.HumidityInfluence = 1f - Mathf.Clamp01(response.Value.HumidityStress);
                data.LightInfluence = 1f - Mathf.Clamp01(response.Value.LightStress);
                data.WindInfluence = 0f; // Not tracked in PlantResponseData; assume neutral

                data.OverallResponseStrength = Mathf.Clamp01(
                    (data.TemperatureInfluence + data.HumidityInfluence + data.LightInfluence) / 3f);
                data.IsStressed = response.Value.OverallStress > 0.7f;
                data.LeafColor = data.IsStressed ? Color.yellow : Color.green;
                return data;
            }

            // Fall back to neutral/default if no data yet
            data.UpdateResponse(22.5f, 50f, 500f, 0f);
            return data;
        }

        // ISpeedTreeEnvironmentalService: UpdateWindSettings
        public void UpdateWindSettings(float windSpeed, Vector3 windDirection, float turbulence)
        {
            if (!ValidateInitialization()) return;
            windSystem?.SetWindStrength(windSpeed);
            windSystem?.SetWindDirection(windDirection);
            // Turbulence parameter not directly exposed; handled internally in wind shaders/variation
        }

        // ISpeedTreeEnvironmentalService: EnableStressVisualization (explicit to avoid property name clash)
        void ISpeedTreeEnvironmentalService.EnableStressVisualization(bool enabled)
        {
            if (stressVisualizationSystem != null)
            {
                stressVisualizationSystem.enabled = enabled;
            }
        }

        // ISpeedTreeEnvironmentalService: GetSeasonalEffects
        public ProjectChimera.Systems.Services.SpeedTree.Environmental.SeasonalEffects GetSeasonalEffects()
        {
            var effects = new ProjectChimera.Systems.Services.SpeedTree.Environmental.SeasonalEffects();
            // If SeasonalSystem provided environmental modifiers, map them here; else defaults are fine
            return effects;
        }

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
                ChimeraLogger.LogWarning("SPEEDTREE/ENV", "SetWindStrength failed", this);
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
                ChimeraLogger.LogWarning("SPEEDTREE/ENV", "SetWindDirection failed", this);
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
                ChimeraLogger.LogWarning("SPEEDTREE/ENV", "SetSeason failed", this);
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
                ChimeraLogger.LogWarning("SPEEDTREE/ENV", "SetStressVisualizationIntensity failed", this);
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
            catch (Exception)
            {
                ChimeraLogger.LogWarning("SPEEDTREE/ENV", "Tick failed", this);
            }
        }

        public int TickPriority => ProjectChimera.Core.Updates.TickPriority.SpeedTreeServices;
        public bool IsTickable => enabled && gameObject.activeInHierarchy;

        public virtual void OnRegistered()
        {
            ChimeraLogger.Log("SPEEDTREE/ENV", "Registered", this);
        }

        public virtual void OnUnregistered()
        {
            ChimeraLogger.Log("SPEEDTREE/ENV", "Unregistered", this);
        }

        #endregion

        #region Helper Methods

        private bool ValidateInitialization()
        {
            if (!IsInitialized)
            {
                ChimeraLogger.LogWarning("SPEEDTREE/ENV", "Not initialized", this);
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
