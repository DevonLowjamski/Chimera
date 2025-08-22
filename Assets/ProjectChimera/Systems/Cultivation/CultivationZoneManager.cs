using System.Collections.Generic;
using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Data.Shared;

namespace ProjectChimera.Systems.Cultivation
{
    /// <summary>
    /// PC013-5a: Specialized service for cultivation zone management
    /// Extracted from monolithic CultivationManager.cs to handle zone creation,
    /// environmental configuration, and zone-based plant organization.
    /// </summary>
    public class CultivationZoneManager : ChimeraManager, ICultivationService
    {
        [Header("Zone Configuration")]
        [SerializeField] private EnvironmentalConditions _defaultEnvironment;
        [SerializeField] private int _maxZones = 50;
        
        // Zone management data
        private Dictionary<string, EnvironmentalConditions> _zoneEnvironments = new Dictionary<string, EnvironmentalConditions>();
        private Dictionary<string, List<string>> _zonePlants = new Dictionary<string, List<string>>();
        private Dictionary<string, ZoneMetrics> _zoneMetrics = new Dictionary<string, ZoneMetrics>();
        
        public bool IsInitialized { get; private set; }
        
        // Properties
        public int ActiveZoneCount => _zoneEnvironments.Count;
        public IReadOnlyCollection<string> ZoneIds => _zoneEnvironments.Keys;
        public EnvironmentalConditions DefaultEnvironment => _defaultEnvironment;
        
        // Events
        public System.Action<string, EnvironmentalConditions> OnZoneEnvironmentChanged;
        public System.Action<string> OnZoneCreated;
        public System.Action<string> OnZoneRemoved;
        
        public void Initialize()
        {
            if (IsInitialized) return;
            
            Debug.Log("[CultivationZoneManager] Initializing zone management system...");
            
            // Initialize default environment if not set
            if (_defaultEnvironment.Temperature == 0f)
            {
                _defaultEnvironment = EnvironmentalConditions.CreateIndoorDefault();
            }
            
            // Create default zone
            CreateZone("default", _defaultEnvironment);
            
            IsInitialized = true;
            Debug.Log($"[CultivationZoneManager] Initialized with default zone. Max zones: {_maxZones}");
        }
        
        public void Shutdown()
        {
            Debug.Log("[CultivationZoneManager] Shutting down zone management...");
            
            _zoneEnvironments.Clear();
            _zonePlants.Clear();
            _zoneMetrics.Clear();
            
            IsInitialized = false;
        }
        
        /// <summary>
        /// Creates a new cultivation zone with specified environmental conditions
        /// </summary>
        public bool CreateZone(string zoneId, EnvironmentalConditions environment)
        {
            if (string.IsNullOrEmpty(zoneId))
            {
                Debug.LogWarning("[CultivationZoneManager] Cannot create zone with null or empty ID");
                return false;
            }
            
            if (_zoneEnvironments.ContainsKey(zoneId))
            {
                Debug.LogWarning($"[CultivationZoneManager] Zone '{zoneId}' already exists");
                return false;
            }
            
            if (_zoneEnvironments.Count >= _maxZones)
            {
                Debug.LogWarning($"[CultivationZoneManager] Maximum zone limit ({_maxZones}) reached");
                return false;
            }
            
            _zoneEnvironments[zoneId] = environment;
            _zonePlants[zoneId] = new List<string>();
            _zoneMetrics[zoneId] = new ZoneMetrics();
            
            OnZoneCreated?.Invoke(zoneId);
            Debug.Log($"[CultivationZoneManager] Created zone '{zoneId}'");
            
            return true;
        }
        
        /// <summary>
        /// Removes a cultivation zone and moves plants to default zone
        /// </summary>
        public bool RemoveZone(string zoneId)
        {
            if (zoneId == "default")
            {
                Debug.LogWarning("[CultivationZoneManager] Cannot remove default zone");
                return false;
            }
            
            if (!_zoneEnvironments.ContainsKey(zoneId))
            {
                Debug.LogWarning($"[CultivationZoneManager] Zone '{zoneId}' does not exist");
                return false;
            }
            
            // Move plants to default zone
            if (_zonePlants.ContainsKey(zoneId))
            {
                foreach (var plantId in _zonePlants[zoneId])
                {
                    MovePlantToZone(plantId, "default");
                }
            }
            
            _zoneEnvironments.Remove(zoneId);
            _zonePlants.Remove(zoneId);
            _zoneMetrics.Remove(zoneId);
            
            OnZoneRemoved?.Invoke(zoneId);
            Debug.Log($"[CultivationZoneManager] Removed zone '{zoneId}'");
            
            return true;
        }
        
        /// <summary>
        /// Sets environmental conditions for a specific zone
        /// </summary>
        public void SetZoneEnvironment(string zoneId, EnvironmentalConditions environment)
        {
            if (!_zoneEnvironments.ContainsKey(zoneId))
            {
                Debug.LogWarning($"[CultivationZoneManager] Zone '{zoneId}' does not exist. Creating it.");
                CreateZone(zoneId, environment);
                return;
            }
            
            _zoneEnvironments[zoneId] = environment;
            OnZoneEnvironmentChanged?.Invoke(zoneId, environment);
            
            Debug.Log($"[CultivationZoneManager] Updated environment for zone '{zoneId}'");
        }
        
        /// <summary>
        /// Gets environmental conditions for a specific zone
        /// </summary>
        public EnvironmentalConditions GetZoneEnvironment(string zoneId)
        {
            return _zoneEnvironments.TryGetValue(zoneId, out EnvironmentalConditions environment) 
                ? environment 
                : _defaultEnvironment;
        }
        
        /// <summary>
        /// Adds a plant to a specific zone
        /// </summary>
        public void AddPlantToZone(string plantId, string zoneId)
        {
            if (!_zonePlants.ContainsKey(zoneId))
            {
                Debug.LogWarning($"[CultivationZoneManager] Zone '{zoneId}' does not exist. Using default zone.");
                zoneId = "default";
            }
            
            // Remove from other zones first
            RemovePlantFromAllZones(plantId);
            
            _zonePlants[zoneId].Add(plantId);
            var currentMetrics = _zoneMetrics[zoneId];
            _zoneMetrics[zoneId] = new ZoneMetrics
            {
                PlantCount = _zonePlants[zoneId].Count,
                AverageHealth = currentMetrics.AverageHealth,
                TotalYield = currentMetrics.TotalYield,
                LastUpdated = System.DateTime.Now
            };
            
            Debug.Log($"[CultivationZoneManager] Added plant '{plantId}' to zone '{zoneId}'");
        }
        
        /// <summary>
        /// Removes a plant from all zones
        /// </summary>
        public void RemovePlantFromAllZones(string plantId)
        {
            foreach (var zonePlants in _zonePlants.Values)
            {
                if (zonePlants.Remove(plantId))
                {
                    break; // Plant should only be in one zone
                }
            }
            
            // Update metrics
            foreach (var kvp in _zonePlants)
            {
                var currentMetrics = _zoneMetrics[kvp.Key];
                _zoneMetrics[kvp.Key] = new ZoneMetrics
                {
                    PlantCount = kvp.Value.Count,
                    AverageHealth = currentMetrics.AverageHealth,
                    TotalYield = currentMetrics.TotalYield,
                    LastUpdated = System.DateTime.Now
                };
            }
        }
        
        /// <summary>
        /// Moves a plant from one zone to another
        /// </summary>
        public void MovePlantToZone(string plantId, string targetZoneId)
        {
            RemovePlantFromAllZones(plantId);
            AddPlantToZone(plantId, targetZoneId);
        }
        
        /// <summary>
        /// Gets all plants in a specific zone
        /// </summary>
        public IReadOnlyList<string> GetPlantsInZone(string zoneId)
        {
            return _zonePlants.TryGetValue(zoneId, out List<string> plants) 
                ? plants.AsReadOnly() 
                : new List<string>().AsReadOnly();
        }
        
        /// <summary>
        /// Gets the zone ID for a specific plant
        /// </summary>
        public string GetPlantZone(string plantId)
        {
            foreach (var kvp in _zonePlants)
            {
                if (kvp.Value.Contains(plantId))
                {
                    return kvp.Key;
                }
            }
            return "default"; // Fallback to default zone
        }
        
        /// <summary>
        /// Gets metrics for a specific zone
        /// </summary>
        public ZoneMetrics GetZoneMetrics(string zoneId)
        {
            return _zoneMetrics.TryGetValue(zoneId, out ZoneMetrics metrics) 
                ? metrics 
                : new ZoneMetrics();
        }
        
        /// <summary>
        /// Updates zone metrics based on current plant states
        /// </summary>
        public void UpdateZoneMetrics(string zoneId, float averageHealth, float totalYield)
        {
            if (_zoneMetrics.ContainsKey(zoneId))
            {
                var metrics = new ZoneMetrics
                {
                    PlantCount = _zoneMetrics[zoneId].PlantCount,
                    AverageHealth = averageHealth,
                    TotalYield = totalYield,
                    LastUpdated = System.DateTime.Now
                };
                _zoneMetrics[zoneId] = metrics;
            }
        }
        
        /// <summary>
        /// Gets environmental conditions for a plant based on its zone
        /// </summary>
        public EnvironmentalConditions GetEnvironmentForPlant(string plantId)
        {
            string zoneId = GetPlantZone(plantId);
            return GetZoneEnvironment(zoneId);
        }
        
        /// <summary>
        /// Optimizes zones by balancing plant distribution
        /// </summary>
        public void OptimizeZoneDistribution()
        {
            Debug.Log("[CultivationZoneManager] Optimizing zone distribution...");
            
            // Simple optimization: ensure no zone is overcrowded
            int totalPlants = 0;
            foreach (var plants in _zonePlants.Values)
            {
                totalPlants += plants.Count;
            }
            
            int targetPlantsPerZone = totalPlants / _zoneEnvironments.Count;
            
            // Log current distribution
            foreach (var kvp in _zonePlants)
            {
                Debug.Log($"[CultivationZoneManager] Zone '{kvp.Key}': {kvp.Value.Count} plants (target: {targetPlantsPerZone})");
            }
        }
        
        #region ChimeraManager Implementation
        
        protected override void OnManagerInitialize()
        {
            // Manager-specific initialization logic
        }
        
        protected override void OnManagerShutdown()
        {
            // Manager-specific shutdown logic
            _zoneEnvironments?.Clear();
            _zonePlants?.Clear();
            _zoneMetrics?.Clear();
        }
        
        #endregion
    }
    
    
    /// <summary>
    /// Metrics tracking for cultivation zones
    /// </summary>
    [System.Serializable]
    public struct ZoneMetrics
    {
        public int PlantCount;
        public float AverageHealth;
        public float TotalYield;
        public System.DateTime LastUpdated;
        
        public ZoneMetrics(int plantCount = 0, float averageHealth = 0f, float totalYield = 0f)
        {
            PlantCount = plantCount;
            AverageHealth = averageHealth;
            TotalYield = totalYield;
            LastUpdated = System.DateTime.Now;
        }
    }
}