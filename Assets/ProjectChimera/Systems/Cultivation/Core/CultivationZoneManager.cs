using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using ProjectChimera.Data.Cultivation;
using System;

namespace ProjectChimera.Systems.Cultivation.Core
{
    /// <summary>
    /// REFACTORED: Cultivation Zone Manager - Focused cultivation zone coordination
    /// Handles zone management, environmental monitoring, and zone-specific cultivation tasks
    /// Single Responsibility: Cultivation zone management and coordination
    /// </summary>
    public class CultivationZoneManager : MonoBehaviour
    {
        [Header("Zone Management Settings")]
        [SerializeField] private bool _enableZoneManagement = true;
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private float _zoneUpdateInterval = 3f;
        [SerializeField] private int _maxZones = 50;

        [Header("Zone Monitoring")]
        [SerializeField] private bool _enableEnvironmentalMonitoring = true;
        [SerializeField] private float _environmentalCheckInterval = 2f;
        [SerializeField] private bool _enableAutomaticOptimization = true;

        // Zone management
        private readonly Dictionary<string, CultivationZone> _registeredZones = new Dictionary<string, CultivationZone>();
        private readonly Dictionary<string, CultivationZoneData> _zoneData = new Dictionary<string, CultivationZoneData>();
        private readonly List<string> _activeZones = new List<string>();
        private readonly List<string> _zonesToUpdate = new List<string>();

        // Statistics
        private CultivationZoneStats _stats = new CultivationZoneStats();

        // Timing
        private float _lastZoneUpdate;
        private float _lastEnvironmentalCheck;

        // Properties
        public bool IsEnabled { get; private set; } = true;
        public CultivationZoneStats GetStats() => _stats;
        public int GetActiveZoneCount() => _activeZones.Count;
        public Dictionary<string, CultivationZone> GetActiveZones() => new Dictionary<string, CultivationZone>(_registeredZones);

        // Events
        public System.Action<string, CultivationZone> OnZoneRegistered;
        public System.Action<string> OnZoneUnregistered;
        public System.Action<string, EnvironmentalConditions> OnZoneEnvironmentalChange;
        public System.Action<string, int> OnZonePlantCountChanged;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            _lastZoneUpdate = Time.time;
            _lastEnvironmentalCheck = Time.time;
            _stats = new CultivationZoneStats();

            if (_enableLogging)
                ChimeraLogger.Log("CULTIVATION", "🏗️ CultivationZoneManager initialized", this);
        }

        /// <summary>
        /// Update all registered zones
        /// </summary>
        public void UpdateZones()
        {
            if (!IsEnabled || !_enableZoneManagement) return;

            float currentTime = Time.time;

            // Update zones
            if (currentTime - _lastZoneUpdate >= _zoneUpdateInterval)
            {
                UpdateZoneManagement();
                _lastZoneUpdate = currentTime;
            }

            // Check environmental conditions
            if (_enableEnvironmentalMonitoring && currentTime - _lastEnvironmentalCheck >= _environmentalCheckInterval)
            {
                CheckEnvironmentalConditions();
                _lastEnvironmentalCheck = currentTime;
            }
        }

        /// <summary>
        /// Register cultivation zone
        /// </summary>
        public bool RegisterZone(CultivationZone zone)
        {
            if (!_enableZoneManagement || zone == null || string.IsNullOrEmpty(zone.ZoneId))
            {
                _stats.RegistrationErrors++;
                return false;
            }

            if (_registeredZones.Count >= _maxZones)
            {
                if (_enableLogging)
                    ChimeraLogger.LogWarning("CULTIVATION", $"Maximum zone limit ({_maxZones}) reached", this);
                return false;
            }

            if (_registeredZones.ContainsKey(zone.ZoneId))
            {
                if (_enableLogging)
                    ChimeraLogger.Log("CULTIVATION", $"Zone {zone.ZoneId} already registered", this);
                return false;
            }

            // Register zone
            _registeredZones[zone.ZoneId] = zone;

            // Initialize zone data
            var zoneData = new CultivationZoneData
            {
                ZoneId = zone.ZoneId,
                IsActive = true,
                PlantCount = 0,
                LastUpdate = Time.time,
                EnvironmentalConditions = GetDefaultEnvironmentalConditions(),
                OptimizationScore = 0.5f
            };

            _zoneData[zone.ZoneId] = zoneData;
            _activeZones.Add(zone.ZoneId);

            // Update statistics
            _stats.RegisteredZones++;
            _stats.ActiveZones = _activeZones.Count;

            OnZoneRegistered?.Invoke(zone.ZoneId, zone);

            if (_enableLogging)
                ChimeraLogger.Log("CULTIVATION", $"Registered cultivation zone: {zone.ZoneId}", this);

            return true;
        }

        /// <summary>
        /// Unregister cultivation zone
        /// </summary>
        public bool UnregisterZone(string zoneId)
        {
            if (!_registeredZones.ContainsKey(zoneId))
                return false;

            var zone = _registeredZones[zoneId];

            // Remove from collections
            _registeredZones.Remove(zoneId);
            _zoneData.Remove(zoneId);
            _activeZones.Remove(zoneId);

            // Update statistics
            _stats.ActiveZones = _activeZones.Count;

            OnZoneUnregistered?.Invoke(zoneId);

            if (_enableLogging)
                ChimeraLogger.Log("CULTIVATION", $"Unregistered cultivation zone: {zoneId}", this);

            return true;
        }

        /// <summary>
        /// Get zone by ID
        /// </summary>
        public CultivationZone GetZone(string zoneId)
        {
            return _registeredZones.TryGetValue(zoneId, out var zone) ? zone : null;
        }

        /// <summary>
        /// Get zone data by ID
        /// </summary>
        public CultivationZoneData? GetZoneData(string zoneId)
        {
            return _zoneData.TryGetValue(zoneId, out var data) ? data : null;
        }

        /// <summary>
        /// Get environmental conditions for zone
        /// </summary>
        public EnvironmentalConditions GetZoneEnvironmentalConditions(string zoneId)
        {
            if (_zoneData.TryGetValue(zoneId, out var data))
            {
                return data.EnvironmentalConditions;
            }
            return GetDefaultEnvironmentalConditions();
        }

        /// <summary>
        /// Update plant count for zone
        /// </summary>
        public void UpdateZonePlantCount(string zoneId, int plantCount)
        {
            if (_zoneData.TryGetValue(zoneId, out var data))
            {
                var previousCount = data.PlantCount;
                data.PlantCount = plantCount;
                _zoneData[zoneId] = data;

                if (previousCount != plantCount)
                {
                    OnZonePlantCountChanged?.Invoke(zoneId, plantCount);
                }
            }
        }

        /// <summary>
        /// Get zones by criteria
        /// </summary>
        public List<CultivationZone> GetZonesByCriteria(Func<CultivationZone, CultivationZoneData, bool> criteria)
        {
            var result = new List<CultivationZone>();

            foreach (var kvp in _registeredZones)
            {
                if (_zoneData.TryGetValue(kvp.Key, out var data))
                {
                    if (criteria(kvp.Value, data))
                    {
                        result.Add(kvp.Value);
                    }
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

            // Update all zone data activity status
            var zoneIds = new List<string>(_zoneData.Keys);
            foreach (var zoneId in zoneIds)
            {
                var data = _zoneData[zoneId];
                data.IsActive = enabled;
                _zoneData[zoneId] = data;
            }

            if (_enableLogging)
                ChimeraLogger.Log("CULTIVATION", $"CultivationZoneManager: {(enabled ? "enabled" : "disabled")}", this);
        }

        #region Private Methods

        /// <summary>
        /// Update zone management
        /// </summary>
        private void UpdateZoneManagement()
        {
            var startTime = Time.realtimeSinceStartup;

            _zonesToUpdate.Clear();
            foreach (var zoneId in _activeZones)
            {
                if (_zoneData.TryGetValue(zoneId, out var data) && data.IsActive)
                {
                    _zonesToUpdate.Add(zoneId);
                }
            }

            foreach (var zoneId in _zonesToUpdate)
            {
                try
                {
                    UpdateZone(zoneId);
                    _stats.ZoneUpdates++;
                }
                catch (System.Exception ex)
                {
                    _stats.UpdateErrors++;
                    if (_enableLogging)
                        ChimeraLogger.LogError("CULTIVATION", $"Zone update error for {zoneId}: {ex.Message}", this);
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
        /// Update individual zone
        /// </summary>
        private void UpdateZone(string zoneId)
        {
            if (!_zoneData.TryGetValue(zoneId, out var data))
                return;

            // Update zone data
            data.LastUpdate = Time.time;

            // Calculate optimization score
            data.OptimizationScore = CalculateZoneOptimizationScore(zoneId, data);

            // Apply automatic optimizations if enabled
            if (_enableAutomaticOptimization)
            {
                ApplyZoneOptimizations(zoneId, data);
            }

            // Update zone data
            _zoneData[zoneId] = data;
        }

        /// <summary>
        /// Check environmental conditions for all zones
        /// </summary>
        private void CheckEnvironmentalConditions()
        {
            foreach (var zoneId in _activeZones)
            {
                if (_zoneData.TryGetValue(zoneId, out var data))
                {
                    var previousConditions = data.EnvironmentalConditions;
                    var currentConditions = MonitorZoneEnvironment(zoneId);

                    if (!AreEnvironmentalConditionsEqual(previousConditions, currentConditions))
                    {
                        data.EnvironmentalConditions = currentConditions;
                        _zoneData[zoneId] = data;

                        OnZoneEnvironmentalChange?.Invoke(zoneId, currentConditions);
                    }
                }
            }
        }

        /// <summary>
        /// Monitor environmental conditions for zone
        /// </summary>
        private EnvironmentalConditions MonitorZoneEnvironment(string zoneId)
        {
            // This would interface with actual environmental monitoring systems
            // For now, simulate some environmental variation
            var conditions = GetDefaultEnvironmentalConditions();

            // Add some realistic variation
            conditions.Temperature += UnityEngine.Random.Range(-2f, 2f);
            conditions.Humidity += UnityEngine.Random.Range(-0.1f, 0.1f);
            conditions.LightLevel = Mathf.Clamp01(conditions.LightLevel + UnityEngine.Random.Range(-0.05f, 0.05f));

            return conditions;
        }

        /// <summary>
        /// Calculate zone optimization score
        /// </summary>
        private float CalculateZoneOptimizationScore(string zoneId, CultivationZoneData data)
        {
            float score = 0.5f; // Base score

            // Environmental factors
            var conditions = data.EnvironmentalConditions;
            float environmentalScore = (conditions.LightLevel + conditions.WaterLevel + conditions.NutrientLevel) / 3f;
            score += environmentalScore * 0.3f;

            // Plant density factor
            if (data.PlantCount > 0)
            {
                float densityScore = Mathf.Clamp01(data.PlantCount / 10f); // Optimal around 10 plants
                score += densityScore * 0.2f;
            }

            // Temperature factor
            float tempOptimal = Mathf.Clamp01(1f - Mathf.Abs(conditions.Temperature - 22f) / 10f);
            score += tempOptimal * 0.2f;

            return Mathf.Clamp01(score);
        }

        /// <summary>
        /// Apply automatic optimizations to zone
        /// </summary>
        private void ApplyZoneOptimizations(string zoneId, CultivationZoneData data)
        {
            var conditions = data.EnvironmentalConditions;

            // Optimize lighting
            if (conditions.LightLevel < 0.7f)
            {
                conditions.LightLevel = Mathf.Min(conditions.LightLevel + 0.05f, 0.8f);
            }

            // Optimize water levels
            if (conditions.WaterLevel < 0.6f)
            {
                conditions.WaterLevel = Mathf.Min(conditions.WaterLevel + 0.03f, 0.7f);
            }

            // Update conditions
            data.EnvironmentalConditions = conditions;
        }

        /// <summary>
        /// Get default environmental conditions
        /// </summary>
        private EnvironmentalConditions GetDefaultEnvironmentalConditions()
        {
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
        /// Check if environmental conditions are equal
        /// </summary>
        private bool AreEnvironmentalConditionsEqual(EnvironmentalConditions a, EnvironmentalConditions b)
        {
            return Mathf.Approximately(a.LightLevel, b.LightLevel) &&
                   Mathf.Approximately(a.WaterLevel, b.WaterLevel) &&
                   Mathf.Approximately(a.NutrientLevel, b.NutrientLevel) &&
                   Mathf.Approximately(a.Temperature, b.Temperature) &&
                   Mathf.Approximately(a.Humidity, b.Humidity);
        }

        #endregion
    }

    /// <summary>
    /// Cultivation zone data structure
    /// </summary>
    [System.Serializable]
    public struct CultivationZoneData
    {
        public string ZoneId;
        public bool IsActive;
        public int PlantCount;
        public float LastUpdate;
        public EnvironmentalConditions EnvironmentalConditions;
        public float OptimizationScore;
    }

    /// <summary>
    /// Cultivation zone management statistics
    /// </summary>
    [System.Serializable]
    public struct CultivationZoneStats
    {
        public int RegisteredZones;
        public int ActiveZones;
        public int ZoneUpdates;
        public int UpdateErrors;
        public int RegistrationErrors;
        public float AverageUpdateTime;
        public float MaxUpdateTime;
        public float LastUpdateTime;
    }
}