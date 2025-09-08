using ProjectChimera.Core.Logging;
using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Core.Updates;
using ProjectChimera.Data.Shared;
using ProjectChimera.Core.Events;
using System.Collections.Generic;
using EnvironmentalConditions = ProjectChimera.Data.Shared.EnvironmentalConditions;

namespace ProjectChimera.Systems.Environment
{
    /// <summary>
    /// PC-013-3: Lean Environment Manager - Coordinates environmental services
    /// Replaces monolithic EnvironmentalManager with service-based architecture
    /// Focuses solely on coordination, delegation, and high-level environmental state
    /// </summary>
    public class EnvironmentManager : DIChimeraManager, ITickable
    {
        [Header("Environment Coordination")]
        [SerializeField] private float _environmentalUpdateInterval = 2f;
        [SerializeField] private bool _enableEnvironmentalLogging = false;
        
        [Header("Event Channels")]
        [SerializeField] private SimpleGameEventSO _onEnvironmentalChange;
        [SerializeField] private SimpleGameEventSO _onEnvironmentalAlert;
        
        // Service Dependencies
        private AtmosphereService _atmosphereService;
        private HVAC_Service _hvacService;
        
        // Environmental tracking
        private Dictionary<Vector3, EnvironmentalConditions> _positionEnvironments = new Dictionary<Vector3, EnvironmentalConditions>();
        private Dictionary<string, EnvironmentalConditions> _plantEnvironments = new Dictionary<string, EnvironmentalConditions>();
        private EnvironmentalConditions _globalConditions;
        private float _lastEnvironmentalUpdate = 0f;
        
        public override ManagerPriority Priority => ManagerPriority.High;
        
        // Properties
        public bool IsInitialized { get; private set; }
        public EnvironmentalConditions GlobalConditions => _globalConditions;
        public int TrackedPositions => _positionEnvironments.Count;
        public int TrackedPlants => _plantEnvironments.Count;
        
        protected override void OnManagerInitialize()
        {
            ChimeraLogger.Log("[EnvironmentManager] Initializing lean environment coordination system...");
            
            // Initialize environmental services
            _atmosphereService = new AtmosphereService();
            _hvacService = new HVAC_Service();
            
            // Initialize services
            _atmosphereService.Initialize();
            _hvacService.Initialize();
            
            // Set default global conditions
            _globalConditions = EnvironmentalConditions.CreateIndoorDefault();
            _lastEnvironmentalUpdate = Time.time;
            
            IsInitialized = true;
            ChimeraLogger.Log($"[EnvironmentManager] Initialized successfully. Services active: AtmosphereService, HVAC_Service");
        }
        
        protected override void OnManagerShutdown()
        {
            if (!IsInitialized) return;
            
            ChimeraLogger.Log("[EnvironmentManager] Shutting down environment coordination...");
            
            // Shutdown services
            _atmosphereService?.Shutdown();
            _hvacService?.Shutdown();
            
            // Clear tracking data
            _positionEnvironments.Clear();
            _plantEnvironments.Clear();
            
            IsInitialized = false;
        }
        
        #region ITickable Implementation
        
        int ITickable.Priority => TickPriority.EnvironmentalManager;
        bool ITickable.Enabled => IsInitialized;
        
        public void Tick(float deltaTime)
        {
            if (!IsInitialized) return;
            
            float currentTime = Time.time;
            if (currentTime - _lastEnvironmentalUpdate >= _environmentalUpdateInterval)
            {
                UpdateEnvironmentalSystems();
                _lastEnvironmentalUpdate = currentTime;
            }
        }
        
        public void OnRegistered()
        {
            ChimeraLogger.LogVerbose("[EnvironmentManager] Registered with UpdateOrchestrator");
        }
        
        public void OnUnregistered()
        {
            ChimeraLogger.LogVerbose("[EnvironmentManager] Unregistered from UpdateOrchestrator");
        }
        
        #endregion
        
        /// <summary>
        /// Updates all environmental systems through service coordination
        /// </summary>
        private void UpdateEnvironmentalSystems()
        {
            // Update global atmospheric conditions
            if (_atmosphereService != null && _atmosphereService.IsInitialized)
            {
                var atmosphericData = _atmosphereService.CalculateAtmosphericTurbulence(
                    Vector3.zero, 
                    _globalConditions
                );
                
                // Apply atmospheric effects to global conditions
                ApplyAtmosphericEffects(atmosphericData);
            }
            
            // Update HVAC systems
            if (_hvacService != null && _hvacService.IsInitialized)
            {
                var hvacResponse = _hvacService.CalculateHVACResponse(
                    "global",
                    _globalConditions,
                    _globalConditions // Target same as current for stability
                );
                
                // Apply HVAC effects to global conditions
                ApplyHVACEffects(hvacResponse);
            }
            
            // Update position-specific environments
            UpdatePositionEnvironments();
            
            // Trigger environmental change event
            _onEnvironmentalChange?.Raise();
            
            if (_enableEnvironmentalLogging)
            {
                ChimeraLogger.Log($"[EnvironmentManager] Environmental update: T={_globalConditions.Temperature:F1}°C, H={_globalConditions.Humidity:F1}%, L={_globalConditions.LightIntensity:F0}PPFD");
            }
        }
        
        /// <summary>
        /// Updates environments for specific positions
        /// </summary>
        private void UpdatePositionEnvironments()
        {
            var positionsToUpdate = new List<Vector3>(_positionEnvironments.Keys);
            
            foreach (var position in positionsToUpdate)
            {
                var localConditions = CalculateLocalEnvironmentalConditions(position);
                _positionEnvironments[position] = localConditions;
            }
        }
        
        /// <summary>
        /// Gets environmental conditions for a specific plant
        /// </summary>
        public EnvironmentalConditions GetEnvironmentForPlant(string plantId)
        {
            if (_plantEnvironments.TryGetValue(plantId, out var conditions))
            {
                return conditions;
            }
            
            // Return global conditions as fallback
            return _globalConditions;
        }
        
        /// <summary>
        /// Gets environmental conditions for a specific position
        /// </summary>
        public EnvironmentalConditions GetEnvironmentForPosition(Vector3 position)
        {
            if (_positionEnvironments.TryGetValue(position, out var conditions))
            {
                return conditions;
            }
            
            // Calculate and cache new position
            var localConditions = CalculateLocalEnvironmentalConditions(position);
            _positionEnvironments[position] = localConditions;
            
            return localConditions;
        }
        
        /// <summary>
        /// Gets cultivation-specific environmental conditions
        /// </summary>
        public EnvironmentalConditions GetCultivationConditions(Vector3 position)
        {
            var conditions = GetEnvironmentForPosition(position);
            
            // Ensure cultivation-specific fields are properly set
            if (conditions.PhotoperiodHours <= 0f) conditions.PhotoperiodHours = 18f;
            if (conditions.pH <= 0f) conditions.pH = 6.0f;
            if (conditions.WaterAvailability <= 0f) conditions.WaterAvailability = 80f;
            if (conditions.ElectricalConductivity <= 0f) conditions.ElectricalConductivity = 1200f;
            
            return conditions;
        }
        
        /// <summary>
        /// Updates environmental conditions for a specific plant
        /// </summary>
        public void UpdatePlantEnvironment(string plantId, EnvironmentalConditions conditions)
        {
            _plantEnvironments[plantId] = conditions;
        }
        
        /// <summary>
        /// Removes environmental tracking for a plant
        /// </summary>
        public void RemovePlantEnvironment(string plantId)
        {
            _plantEnvironments.Remove(plantId);
        }
        
        /// <summary>
        /// Sets global environmental conditions
        /// </summary>
        public void SetGlobalConditions(EnvironmentalConditions conditions)
        {
            var previousConditions = _globalConditions;
            _globalConditions = conditions;
            
            // Check for significant changes
            if (HasSignificantEnvironmentalChange(previousConditions, conditions))
            {
                _onEnvironmentalAlert?.Raise();
                
                if (_enableEnvironmentalLogging)
                {
                    ChimeraLogger.Log($"[EnvironmentManager] Significant environmental change detected");
                }
            }
        }
        
        /// <summary>
        /// Gets comprehensive environmental statistics
        /// </summary>
        public EnvironmentalStatistics GetEnvironmentalStatistics()
        {
            return new EnvironmentalStatistics
            {
                GlobalTemperature = _globalConditions.Temperature,
                GlobalHumidity = _globalConditions.Humidity,
                GlobalLightIntensity = _globalConditions.LightIntensity,
                GlobalCO2Level = _globalConditions.CO2Level,
                TrackedPositions = _positionEnvironments.Count,
                TrackedPlants = _plantEnvironments.Count,
                AtmosphereServiceActive = _atmosphereService?.IsInitialized ?? false,
                HVACServiceActive = _hvacService?.IsInitialized ?? false,
                LastUpdateTime = _lastEnvironmentalUpdate
            };
        }
        
        #region Private Helper Methods
        
        private void ApplyAtmosphericEffects(AtmosphericTurbulenceData atmosphericData)
        {
            if (atmosphericData == null) return;
            
            // Apply minor atmospheric variations to global conditions
            _globalConditions.AirFlow = atmosphericData.TurbulenceIntensity * 0.5f;
            _globalConditions.AirVelocity = atmosphericData.TurbulenceIntensity * 0.3f;
            _globalConditions.BarometricPressure = 1013.25f + (atmosphericData.TurbulenceIntensity * 2f);
        }
        
        private void ApplyHVACEffects(HVACControlResponse hvacResponse)
        {
            if (hvacResponse == null) return;
            
            // Apply HVAC adjustments to global conditions
            _globalConditions.Temperature += hvacResponse.TemperatureAdjustment;
            _globalConditions.Humidity += hvacResponse.HumidityAdjustment;
            _globalConditions.AirFlow += hvacResponse.AirflowAdjustment;
            
            // Clamp values to realistic ranges
            _globalConditions.Temperature = Mathf.Clamp(_globalConditions.Temperature, 10f, 35f);
            _globalConditions.Humidity = Mathf.Clamp(_globalConditions.Humidity, 20f, 90f);
            _globalConditions.AirFlow = Mathf.Clamp(_globalConditions.AirFlow, 0f, 2f);
        }
        
        private EnvironmentalConditions CalculateLocalEnvironmentalConditions(Vector3 position)
        {
            // Start with global conditions
            var localConditions = _globalConditions;
            
            // Apply position-specific variations
            float positionVariation = Mathf.PerlinNoise(position.x * 0.1f, position.z * 0.1f);
            
            localConditions.Temperature += (positionVariation - 0.5f) * 2f; // ±1°C variation
            localConditions.Humidity += (positionVariation - 0.5f) * 10f;   // ±5% variation
            localConditions.AirFlow += (positionVariation - 0.5f) * 0.2f;   // ±0.1 variation
            
            return localConditions;
        }
        
        private bool HasSignificantEnvironmentalChange(EnvironmentalConditions previous, EnvironmentalConditions current)
        {
            float tempDiff = Mathf.Abs(current.Temperature - previous.Temperature);
            float humidityDiff = Mathf.Abs(current.Humidity - previous.Humidity);
            float lightDiff = Mathf.Abs(current.LightIntensity - previous.LightIntensity);
            
            return tempDiff > 3f || humidityDiff > 15f || lightDiff > 200f;
        }
        
        #endregion
    }
    
    /// <summary>
    /// Environmental statistics data structure
    /// </summary>
    [System.Serializable]
    public class EnvironmentalStatistics
    {
        public float GlobalTemperature;
        public float GlobalHumidity;
        public float GlobalLightIntensity;
        public float GlobalCO2Level;
        public int TrackedPositions;
        public int TrackedPlants;
        public bool AtmosphereServiceActive;
        public bool HVACServiceActive;
        public float LastUpdateTime;
        
        public override string ToString()
        {
            return $"Environment Stats: T={GlobalTemperature:F1}°C, H={GlobalHumidity:F1}%, L={GlobalLightIntensity:F0}PPFD, Positions={TrackedPositions}, Plants={TrackedPlants}";
        }
    }
}