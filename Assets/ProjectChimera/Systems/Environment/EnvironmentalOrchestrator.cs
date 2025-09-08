using ProjectChimera.Core.Logging;
using System.Collections.Generic;
using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Data.Shared;
using ProjectChimera.Core.Events;
using ProjectChimera.Core.Updates;
using EnvironmentalConditions = ProjectChimera.Data.Shared.EnvironmentalConditions;

namespace ProjectChimera.Systems.Environment
{
    /// <summary>
    /// PC013-6e: Environmental Orchestrator - Updated for service-based architecture
    /// Coordinates with the new EnvironmentManager and its services
    /// Provides high-level environmental coordination and optimization
    /// </summary>
    public class EnvironmentalOrchestrator : ChimeraManager, ITickable
    {
        [Header("Orchestrator Configuration")]
        [SerializeField] private bool _enableAutoEnvironmentalControl = true;
        [SerializeField] private float _environmentalUpdateInterval = 30f; // 30 seconds
        [SerializeField] private float _coordinationSensitivity = 1f;
        [SerializeField] private int _maxEnvironmentalZones = 25;
        
        [Header("Event Channels")]
        [SerializeField] private SimpleGameEventSO _onEnvironmentalOptimization;
        [SerializeField] private SimpleGameEventSO _onEnvironmentalAlert;
        [SerializeField] private SimpleGameEventSO _onConditionsChanged;
        
        // Service dependencies
        private EnvironmentManager _environmentManager;
        private ClimateControlManager _climateControlManager;
        private HVACSystemManager _hvacSystemManager;
        private SensorNetworkManager _sensorNetworkManager;
        
        // Orchestration state
        private Dictionary<string, EnvironmentalConditions> _zoneConditions = new Dictionary<string, EnvironmentalConditions>();
        private List<string> _activeAlerts = new List<string>();
        private float _lastEnvironmentalUpdate = 0f;
        
        public override ManagerPriority Priority => ManagerPriority.Normal;
        
        // Properties
        public bool IsInitialized { get; private set; }
        public string ManagerName => "Environmental Orchestrator";
        public int ActiveZones => _zoneConditions.Count;
        public int ActiveAlerts => _activeAlerts.Count;
        public bool EnableAutoEnvironmentalControl 
        { 
            get => _enableAutoEnvironmentalControl; 
            set => _enableAutoEnvironmentalControl = value; 
        }
        
        // ITickable implementation
        int ITickable.Priority => TickPriority.EnvironmentalManager;
        public bool Enabled => IsInitialized;
        
        protected override void OnManagerInitialize()
        {
            ChimeraLogger.Log("[EnvironmentalOrchestrator] Initializing environmental orchestration system...");
            
            // Get references to managers
            _environmentManager = GameManager.Instance?.GetManager<EnvironmentManager>();
            _climateControlManager = GameManager.Instance?.GetManager<ClimateControlManager>();
            _hvacSystemManager = GameManager.Instance?.GetManager<HVACSystemManager>();
            _sensorNetworkManager = GameManager.Instance?.GetManager<SensorNetworkManager>();
            
            if (_environmentManager == null)
            {
                ChimeraLogger.LogWarning("[EnvironmentalOrchestrator] EnvironmentManager not found - some features may be limited");
            }
            
            _lastEnvironmentalUpdate = Time.time;
            IsInitialized = true;
            
            // Register with UpdateOrchestrator
            UpdateOrchestrator.Instance.RegisterTickable(this);
            
            ChimeraLogger.Log($"[EnvironmentalOrchestrator] Initialized with {(_environmentManager != null ? "full" : "limited")} environmental integration");
        }
        
        protected override void OnManagerShutdown()
        {
            if (!IsInitialized) return;
            
            ChimeraLogger.Log("[EnvironmentalOrchestrator] Shutting down environmental orchestration...");
            
            // Unregister from UpdateOrchestrator
            if (UpdateOrchestrator.Instance != null)
            {
                UpdateOrchestrator.Instance.UnregisterTickable(this);
            }
            
            _zoneConditions.Clear();
            _activeAlerts.Clear();
            
            IsInitialized = false;
        }
        
        public void Tick(float deltaTime)
        {
            if (!IsInitialized) return;
            
            float currentTime = Time.time;
            if (currentTime - _lastEnvironmentalUpdate >= _environmentalUpdateInterval)
            {
                OrchestratEnvironmentalSystems();
                _lastEnvironmentalUpdate = currentTime;
            }
        }
        
        /// <summary>
        /// Main orchestration method for environmental systems
        /// </summary>
        private void OrchestratEnvironmentalSystems()
        {
            if (!_enableAutoEnvironmentalControl) return;
            
            // Update global conditions from EnvironmentManager
            if (_environmentManager != null && _environmentManager.IsInitialized)
            {
                var globalConditions = _environmentManager.GlobalConditions;
                ProcessGlobalEnvironmentalUpdate(globalConditions);
            }
            
            // Coordinate with other environmental managers
            CoordinateEnvironmentalManagers();
            
            // Process optimization
            if (_enableAutoEnvironmentalControl)
            {
                OptimizeEnvironmentalSettings();
            }
            
            // Trigger update events
            _onConditionsChanged?.Raise();
        }
        
        /// <summary>
        /// Processes global environmental updates
        /// </summary>
        private void ProcessGlobalEnvironmentalUpdate(EnvironmentalConditions globalConditions)
        {
            // Update zone conditions with global data
            var globalZoneId = "global";
            var previousConditions = _zoneConditions.GetValueOrDefault(globalZoneId, default);
            _zoneConditions[globalZoneId] = globalConditions;
            
            // Check for significant changes
            if (HasSignificantChange(previousConditions, globalConditions))
            {
                ChimeraLogger.Log($"[EnvironmentalOrchestrator] Significant environmental change detected: T={globalConditions.Temperature:F1}°C, H={globalConditions.Humidity:F1}%");
                _onEnvironmentalAlert?.Raise();
            }
        }
        
        /// <summary>
        /// Coordinates with other environmental managers
        /// </summary>
        private void CoordinateEnvironmentalManagers()
        {
            // Coordinate with climate control if available
            if (_climateControlManager != null)
            {
                // Climate control coordination logic
                var activeClimateZones = _climateControlManager.ActiveClimateZones;
                if (activeClimateZones > 0)
                {
                    // Process climate zone coordination
                    CoordinateClimateZones();
                }
            }
            
            // Coordinate with HVAC if available
            if (_hvacSystemManager != null)
            {
                var activeHVACZones = _hvacSystemManager.ActiveHVACZones;
                if (activeHVACZones > 0)
                {
                    // Process HVAC coordination
                    CoordinateHVACZones();
                }
            }
            
            // Coordinate with sensor network if available
            if (_sensorNetworkManager != null)
            {
                var totalSensors = _sensorNetworkManager.TotalSensors;
                if (totalSensors > 0)
                {
                    // Process sensor data coordination
                    CoordinateSensorNetwork();
                }
            }
        }
        
        /// <summary>
        /// Optimizes environmental settings based on current conditions
        /// </summary>
        private void OptimizeEnvironmentalSettings()
        {
            if (_environmentManager == null || !_environmentManager.IsInitialized)
                return;
            
            var globalConditions = _environmentManager.GlobalConditions;
            
            // Simple optimization logic - can be enhanced
            bool optimizationNeeded = false;
            
            // Check temperature optimization
            if (globalConditions.Temperature < 20f || globalConditions.Temperature > 28f)
            {
                optimizationNeeded = true;
            }
            
            // Check humidity optimization
            if (globalConditions.Humidity < 40f || globalConditions.Humidity > 70f)
            {
                optimizationNeeded = true;
            }
            
            // Check light optimization
            if (globalConditions.LightIntensity < 200f || globalConditions.LightIntensity > 1200f)
            {
                optimizationNeeded = true;
            }
            
            if (optimizationNeeded)
            {
                PerformEnvironmentalOptimization(globalConditions);
            }
        }
        
        /// <summary>
        /// Performs environmental optimization
        /// </summary>
        private void PerformEnvironmentalOptimization(EnvironmentalConditions currentConditions)
        {
            // Create optimized conditions
            var optimizedConditions = currentConditions;
            
            // Temperature optimization
            if (currentConditions.Temperature < 20f)
                optimizedConditions.Temperature = 22f;
            else if (currentConditions.Temperature > 28f)
                optimizedConditions.Temperature = 26f;
            
            // Humidity optimization
            if (currentConditions.Humidity < 40f)
                optimizedConditions.Humidity = 50f;
            else if (currentConditions.Humidity > 70f)
                optimizedConditions.Humidity = 60f;
            
            // Light optimization
            if (currentConditions.LightIntensity < 200f)
                optimizedConditions.LightIntensity = 600f;
            else if (currentConditions.LightIntensity > 1200f)
                optimizedConditions.LightIntensity = 800f;
            
            // Apply optimized conditions
            _environmentManager.SetGlobalConditions(optimizedConditions);
            
            // Trigger optimization event
            _onEnvironmentalOptimization?.Raise();
            
            ChimeraLogger.Log($"[EnvironmentalOrchestrator] Environmental optimization applied: T={optimizedConditions.Temperature:F1}°C, H={optimizedConditions.Humidity:F1}%");
        }
        
        /// <summary>
        /// Gets environmental conditions for a specific zone
        /// </summary>
        public EnvironmentalConditions GetZoneConditions(string zoneId)
        {
            if (_zoneConditions.TryGetValue(zoneId, out var conditions))
            {
                return conditions;
            }
            
            // Fallback to environment manager
            if (_environmentManager != null && _environmentManager.IsInitialized)
            {
                return _environmentManager.GlobalConditions;
            }
            
            return EnvironmentalConditions.CreateIndoorDefault();
        }
        
        /// <summary>
        /// Gets orchestration statistics
        /// </summary>
        public EnvironmentalOrchestrationStatistics GetOrchestrationStatistics()
        {
            return new EnvironmentalOrchestrationStatistics
            {
                ActiveZones = _zoneConditions.Count,
                ActiveAlerts = _activeAlerts.Count,
                AutoControlEnabled = _enableAutoEnvironmentalControl,
                CoordinationSensitivity = _coordinationSensitivity,
                EnvironmentManagerConnected = _environmentManager?.IsInitialized ?? false,
                LastUpdateTime = _lastEnvironmentalUpdate
            };
        }
        
        #region Private Helper Methods
        
        private void CoordinateClimateZones()
        {
            // Placeholder for climate zone coordination
            ChimeraLogger.Log("[EnvironmentalOrchestrator] Coordinating climate zones");
        }
        
        private void CoordinateHVACZones()
        {
            // Placeholder for HVAC zone coordination  
            ChimeraLogger.Log("[EnvironmentalOrchestrator] Coordinating HVAC zones");
        }
        
        private void CoordinateSensorNetwork()
        {
            // Placeholder for sensor network coordination
            ChimeraLogger.Log("[EnvironmentalOrchestrator] Coordinating sensor network");
        }
        
        private bool HasSignificantChange(EnvironmentalConditions previous, EnvironmentalConditions current)
        {
            if (previous.Temperature == 0f) return false; // No previous data
            
            float tempDiff = Mathf.Abs(current.Temperature - previous.Temperature);
            float humidityDiff = Mathf.Abs(current.Humidity - previous.Humidity);
            float lightDiff = Mathf.Abs(current.LightIntensity - previous.LightIntensity);
            
            return tempDiff > 2f || humidityDiff > 10f || lightDiff > 100f;
        }
        
        #endregion
    }
    
    /// <summary>
    /// Environmental orchestration statistics
    /// </summary>
    [System.Serializable]
    public class EnvironmentalOrchestrationStatistics
    {
        public int ActiveZones;
        public int ActiveAlerts;
        public bool AutoControlEnabled;
        public float CoordinationSensitivity;
        public bool EnvironmentManagerConnected;
        public float LastUpdateTime;
        
        public override string ToString()
        {
            return $"Orchestration Stats: {ActiveZones} zones, {ActiveAlerts} alerts, Auto: {AutoControlEnabled}";
        }
    }
}