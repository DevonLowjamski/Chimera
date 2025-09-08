using ProjectChimera.Core.Logging;
using System.Collections.Generic;
using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Core.DependencyInjection;
using ProjectChimera.Data.Shared;
using ProjectChimera.Data.Environment;
using EnvironmentalConditions = ProjectChimera.Data.Shared.EnvironmentalConditions;

namespace ProjectChimera.Systems.Cultivation
{
    /// <summary>
    /// PC-013-2: Cultivation Environmental Manager - Handles environmental conditions for cultivation zones
    /// Extracted from monolithic CultivationManager for Single Responsibility Principle
    /// </summary>
    public class CultivationEnvironmentalManager : IEnvironmentalManager
    {
        [Header("Default Environmental Settings")]
        [SerializeField] private EnvironmentalConditions _defaultEnvironment;
        
        // Runtime data
        private Dictionary<string, EnvironmentalConditions> _zoneEnvironments = new Dictionary<string, EnvironmentalConditions>();
        private Dictionary<string, string> _plantZoneAssignments = new Dictionary<string, string>();
        
        // Dependencies
        private IPlantLifecycleManager _plantLifecycleManager;
        
        public bool IsInitialized { get; private set; }
        
        public CultivationEnvironmentalManager(IPlantLifecycleManager plantLifecycleManager)
        {
            _plantLifecycleManager = plantLifecycleManager;
        }
        
        public void Initialize()
        {
            if (IsInitialized) return;
            
            ChimeraLogger.Log("[CultivationEnvironmentalManager] Initializing environmental management...");
            
            // Register with ServiceLocator for dependency injection
            try
            {
                var serviceContainer = ServiceContainerFactory.Instance;
                serviceContainer?.RegisterSingleton<IEnvironmentalManager>(this);
                ChimeraLogger.Log("[CultivationEnvironmentalManager] Registered with ServiceContainer");
            }
            catch (System.Exception ex)
            {
                ChimeraLogger.LogError($"[CultivationEnvironmentalManager] Failed to register with ServiceContainer: {ex.Message}");
            }
            
            // Initialize default environment if not set
            if (_defaultEnvironment.Temperature == 0f)
            {
                _defaultEnvironment = EnvironmentalConditions.CreateIndoorDefault();
            }
            
            // Set up default growing zone
            _zoneEnvironments["default"] = _defaultEnvironment;
            
            IsInitialized = true;
            ChimeraLogger.Log("[CultivationEnvironmentalManager] Environmental management initialized.");
        }
        
        public void Shutdown()
        {
            if (!IsInitialized) return;
            
            ChimeraLogger.Log("[CultivationEnvironmentalManager] Shutting down environmental management...");
            
            _zoneEnvironments.Clear();
            _plantZoneAssignments.Clear();
            
            IsInitialized = false;
        }
        
        /// <summary>
        /// Updates environmental conditions for a specific zone.
        /// </summary>
        public void SetZoneEnvironment(string zoneId, EnvironmentalConditions environment)
        {
            if (!IsInitialized)
            {
                ChimeraLogger.LogError("[CultivationEnvironmentalManager] Cannot set zone environment: Manager not initialized.");
                return;
            }
            
            if (string.IsNullOrEmpty(zoneId))
            {
                ChimeraLogger.LogWarning("[CultivationEnvironmentalManager] Cannot set zone environment: Zone ID is null or empty.");
                return;
            }
            
            // Validate environment conditions
            environment = ValidateEnvironmentalConditions(environment);
            
            _zoneEnvironments[zoneId] = environment;
            ChimeraLogger.Log($"[CultivationEnvironmentalManager] Updated environment for zone '{zoneId}': {environment}");
            
            // Notify affected plants about environment change
            NotifyPlantsOfEnvironmentChange(zoneId);
        }
        
        /// <summary>
        /// Gets environmental conditions for a specific zone.
        /// </summary>
        public EnvironmentalConditions GetZoneEnvironment(string zoneId)
        {
            if (!IsInitialized) return _defaultEnvironment;
            
            return _zoneEnvironments.TryGetValue(zoneId ?? "default", out EnvironmentalConditions environment) 
                ? environment 
                : _defaultEnvironment;
        }
        
        /// <summary>
        /// Gets environmental conditions for a specific plant.
        /// </summary>
        public EnvironmentalConditions GetEnvironmentForPlant(string plantId)
        {
            if (!IsInitialized) return _defaultEnvironment;
            
            // Check if plant has a specific zone assignment
            if (_plantZoneAssignments.TryGetValue(plantId, out string zoneId))
            {
                return GetZoneEnvironment(zoneId);
            }
            
            // Default to 'default' zone
            return GetZoneEnvironment("default");
        }
        
        /// <summary>
        /// Assigns a plant to a specific environmental zone
        /// </summary>
        public void AssignPlantToZone(string plantId, string zoneId)
        {
            if (!IsInitialized)
            {
                ChimeraLogger.LogError("[CultivationEnvironmentalManager] Cannot assign plant to zone: Manager not initialized.");
                return;
            }
            
            if (string.IsNullOrEmpty(plantId))
            {
                ChimeraLogger.LogWarning("[CultivationEnvironmentalManager] Cannot assign plant to zone: Plant ID is null or empty.");
                return;
            }
            
            // Ensure zone exists
            if (!_zoneEnvironments.ContainsKey(zoneId))
            {
                ChimeraLogger.LogWarning($"[CultivationEnvironmentalManager] Zone '{zoneId}' does not exist. Creating with default environment.");
                _zoneEnvironments[zoneId] = _defaultEnvironment;
            }
            
            _plantZoneAssignments[plantId] = zoneId;
            ChimeraLogger.Log($"[CultivationEnvironmentalManager] Assigned plant '{plantId}' to zone '{zoneId}'.");
        }
        
        /// <summary>
        /// Removes a plant's zone assignment
        /// </summary>
        public void RemovePlantZoneAssignment(string plantId)
        {
            if (!IsInitialized) return;
            
            if (_plantZoneAssignments.Remove(plantId))
            {
                ChimeraLogger.Log($"[CultivationEnvironmentalManager] Removed zone assignment for plant '{plantId}'.");
            }
        }
        
        /// <summary>
        /// Gets all available zones
        /// </summary>
        public IEnumerable<string> GetAvailableZones()
        {
            return IsInitialized ? _zoneEnvironments.Keys : new string[0];
        }
        
        /// <summary>
        /// Creates a new environmental zone
        /// </summary>
        public void CreateZone(string zoneId, EnvironmentalConditions environment)
        {
            if (!IsInitialized)
            {
                ChimeraLogger.LogError("[CultivationEnvironmentalManager] Cannot create zone: Manager not initialized.");
                return;
            }
            
            if (string.IsNullOrEmpty(zoneId))
            {
                ChimeraLogger.LogWarning("[CultivationEnvironmentalManager] Cannot create zone: Zone ID is null or empty.");
                return;
            }
            
            if (_zoneEnvironments.ContainsKey(zoneId))
            {
                ChimeraLogger.LogWarning($"[CultivationEnvironmentalManager] Zone '{zoneId}' already exists. Updating environment.");
            }
            
            // Validate environment conditions
            environment = ValidateEnvironmentalConditions(environment);
            
            _zoneEnvironments[zoneId] = environment;
            ChimeraLogger.Log($"[CultivationEnvironmentalManager] Created zone '{zoneId}' with environment: {environment}");
        }
        
        /// <summary>
        /// Removes an environmental zone
        /// </summary>
        public bool RemoveZone(string zoneId)
        {
            if (!IsInitialized) return false;
            
            if (zoneId == "default")
            {
                ChimeraLogger.LogWarning("[CultivationEnvironmentalManager] Cannot remove default zone.");
                return false;
            }
            
            if (_zoneEnvironments.Remove(zoneId))
            {
                // Reassign plants in this zone to default
                var plantsToReassign = new List<string>();
                foreach (var kvp in _plantZoneAssignments)
                {
                    if (kvp.Value == zoneId)
                    {
                        plantsToReassign.Add(kvp.Key);
                    }
                }
                
                foreach (var plantId in plantsToReassign)
                {
                    _plantZoneAssignments[plantId] = "default";
                }
                
                ChimeraLogger.Log($"[CultivationEnvironmentalManager] Removed zone '{zoneId}' and reassigned {plantsToReassign.Count} plants to default zone.");
                return true;
            }
            
            return false;
        }
        
        /// <summary>
        /// Gets environmental statistics for all zones
        /// </summary>
        public Dictionary<string, EnvironmentalConditions> GetZoneStatistics()
        {
            return IsInitialized ? new Dictionary<string, EnvironmentalConditions>(_zoneEnvironments) : new Dictionary<string, EnvironmentalConditions>();
        }
        
        private EnvironmentalConditions ValidateEnvironmentalConditions(EnvironmentalConditions environment)
        {
            // Validate temperature range (reasonable for cannabis cultivation)
            environment.Temperature = Mathf.Clamp(environment.Temperature, 15f, 35f);
            
            // Validate humidity range
            environment.Humidity = Mathf.Clamp(environment.Humidity, 20f, 80f);
            
            // Validate CO2 levels (atmospheric to enriched)
            environment.CO2Level = Mathf.Clamp(environment.CO2Level, 300f, 1500f);
            
            // Validate light intensity (0-100% or PPFD equivalent)
            environment.LightIntensity = Mathf.Clamp(environment.LightIntensity, 0f, 100f);
            
            return environment;
        }
        
        private void NotifyPlantsOfEnvironmentChange(string zoneId)
        {
            // This could trigger plant stress calculations or growth adjustments
            // For now, just log the notification
            var plantsInZone = new List<string>();
            foreach (var kvp in _plantZoneAssignments)
            {
                if (kvp.Value == zoneId)
                {
                    plantsInZone.Add(kvp.Key);
                }
            }
            
            if (plantsInZone.Count > 0)
            {
                ChimeraLogger.Log($"[CultivationEnvironmentalManager] Notified {plantsInZone.Count} plants of environment change in zone '{zoneId}'.");
            }
        }
    }
}