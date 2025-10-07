using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Rendering.Core
{
    /// <summary>
    /// REFACTORED: Lighting Zone Manager
    /// Focused component for managing lighting zones and spatial lighting optimization
    /// </summary>
    public class LightingZoneManager : MonoBehaviour
    {
        [Header("Lighting Zone Settings")]
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private int _maxLightingZones = 50;
        [SerializeField] private float _zoneUpdateInterval = 0.5f;

        // Zone management
        private readonly Dictionary<int, LightingZone> _lightingZones = new Dictionary<int, LightingZone>();
        private float _lastZoneUpdate;

        // Performance tracking
        private LightingZoneStats _stats = new LightingZoneStats();

        // Properties
        public bool IsEnabled { get; private set; } = true;
        public int ActiveZoneCount => _lightingZones.Count;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            ResetStats();
            SetupDefaultZones();

            if (_enableLogging)
                ChimeraLogger.Log("RENDERING", "✅ LightingZoneManager initialized", this);
        }

        /// <summary>
        /// Create new lighting zone
        /// </summary>
        public int CreateLightingZone(Bounds bounds, LightingZoneType zoneType)
        {
            if (!IsEnabled) return -1;
            if (_lightingZones.Count >= _maxLightingZones)
            {
                if (_enableLogging)
                    ChimeraLogger.LogWarning("RENDERING", $"Maximum lighting zones reached: {_maxLightingZones}", this);
                return -1;
            }

            int zoneID = _lightingZones.Count;

            var zone = new LightingZone
            {
                ID = zoneID,
                Bounds = bounds,
                ZoneType = zoneType,
                IsActive = true,
                AssignedLights = new List<GrowLight>(),
                LightingLevel = 1f,
                CreationTime = Time.time
            };

            _lightingZones[zoneID] = zone;
            _stats.ZonesCreated++;

            if (_enableLogging)
                ChimeraLogger.Log("RENDERING", $"Created lighting zone {zoneID} ({zoneType}) at bounds: {bounds}", this);

            return zoneID;
        }

        /// <summary>
        /// Remove lighting zone
        /// </summary>
        public bool RemoveLightingZone(int zoneID)
        {
            if (!IsEnabled || !_lightingZones.ContainsKey(zoneID)) return false;

            var zone = _lightingZones[zoneID];

            // Clear assigned lights
            zone.AssignedLights.Clear();

            _lightingZones.Remove(zoneID);
            _stats.ZonesDestroyed++;

            if (_enableLogging)
                ChimeraLogger.Log("RENDERING", $"Removed lighting zone {zoneID}", this);

            return true;
        }

        /// <summary>
        /// Get lighting zone by ID
        /// </summary>
        public LightingZone? GetLightingZone(int zoneID)
        {
            return _lightingZones.TryGetValue(zoneID, out var zone) ? zone : null;
        }

        /// <summary>
        /// Get all lighting zones
        /// </summary>
        public LightingZone[] GetAllZones()
        {
            var zones = new LightingZone[_lightingZones.Count];
            _lightingZones.Values.CopyTo(zones, 0);
            return zones;
        }

        /// <summary>
        /// Assign grow light to zone
        /// </summary>
        public void AssignLightToZone(int zoneID, GrowLight light)
        {
            if (!IsEnabled || light == null) return;
            if (!_lightingZones.TryGetValue(zoneID, out var zone)) return;

            if (!zone.AssignedLights.Contains(light))
            {
                zone.AssignedLights.Add(light);
                _stats.LightAssignments++;

                if (_enableLogging)
                    ChimeraLogger.Log("RENDERING", $"Assigned grow light {light.ID} to zone {zoneID}", this);
            }
        }

        /// <summary>
        /// Remove grow light from zone
        /// </summary>
        public void RemoveLightFromZone(int zoneID, GrowLight light)
        {
            if (!IsEnabled || light == null) return;
            if (!_lightingZones.TryGetValue(zoneID, out var zone)) return;

            if (zone.AssignedLights.Remove(light))
            {
                _stats.LightRemovals++;

                if (_enableLogging)
                    ChimeraLogger.Log("RENDERING", $"Removed grow light {light.ID} from zone {zoneID}", this);
            }
        }

        /// <summary>
        /// Find zone containing position
        /// </summary>
        public int FindZoneContaining(Vector3 position)
        {
            foreach (var kvp in _lightingZones)
            {
                if (kvp.Value.IsActive && kvp.Value.Bounds.Contains(position))
                {
                    return kvp.Key;
                }
            }
            return -1; // No zone found
        }

        /// <summary>
        /// Get zones by type
        /// </summary>
        public LightingZone[] GetZonesByType(LightingZoneType zoneType)
        {
            var zones = new List<LightingZone>();
            foreach (var zone in _lightingZones.Values)
            {
                if (zone.ZoneType == zoneType && zone.IsActive)
                {
                    zones.Add(zone);
                }
            }
            return zones.ToArray();
        }

        /// <summary>
        /// Update all lighting zones (called by LightingCore)
        /// </summary>
        public void UpdateZones()
        {
            if (!IsEnabled) return;
            if (Time.time - _lastZoneUpdate < _zoneUpdateInterval) return;

            foreach (var zone in _lightingZones.Values)
            {
                if (!zone.IsActive) continue;

                UpdateZoneLightingLevel(zone);
            }

            _lastZoneUpdate = Time.time;
            _stats.ZoneUpdates++;
            UpdateStats();
        }

        /// <summary>
        /// Set zone enabled/disabled
        /// </summary>
        public void SetZoneEnabled(int zoneID, bool enabled)
        {
            if (!_lightingZones.TryGetValue(zoneID, out var zone)) return;

            if (zone.IsActive != enabled)
            {
                zone.IsActive = enabled;
                _stats.ZoneToggleEvents++;

                if (_enableLogging)
                    ChimeraLogger.Log("RENDERING", $"Zone {zoneID}: {(enabled ? "enabled" : "disabled")}", this);
            }
        }

        /// <summary>
        /// Get zone statistics
        /// </summary>
        public LightingZoneStats GetStats()
        {
            return _stats;
        }

        /// <summary>
        /// Set manager enabled/disabled
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;

            if (!enabled)
            {
                // Deactivate all zones when disabled
                foreach (var zone in _lightingZones.Values)
                {
                    zone.IsActive = false;
                }
            }

            if (_enableLogging)
                ChimeraLogger.Log("RENDERING", $"LightingZoneManager: {(enabled ? "enabled" : "disabled")}", this);
        }

        private void SetupDefaultZones()
        {
            // Create a default indoor lighting zone
            CreateLightingZone(new Bounds(Vector3.zero, Vector3.one * 50f), LightingZoneType.Indoor);
        }

        private void UpdateZoneLightingLevel(LightingZone zone)
        {
            // Calculate zone lighting level based on assigned lights
            float totalLighting = 0f;
            int activeLights = 0;

            foreach (var light in zone.AssignedLights)
            {
                if (light.IsActive)
                {
                    // Calculate light contribution based on distance from zone center
                    float distance = Vector3.Distance(light.Position, zone.Bounds.center);
                    float maxDistance = zone.Bounds.size.magnitude;
                    float contribution = light.Intensity * (1f - Mathf.Clamp01(distance / maxDistance));

                    totalLighting += contribution;
                    activeLights++;
                }
            }

            // Normalize lighting level
            zone.LightingLevel = activeLights > 0 ? Mathf.Clamp01(totalLighting / (activeLights * 2f)) : 0f;
            zone.ActiveLightCount = activeLights;
        }

        private void UpdateStats()
        {
            _stats.ActiveZones = 0;
            _stats.TotalAssignedLights = 0;

            foreach (var zone in _lightingZones.Values)
            {
                if (zone.IsActive)
                {
                    _stats.ActiveZones++;
                    _stats.TotalAssignedLights += zone.AssignedLights.Count;
                }
            }

            _stats.TotalZones = _lightingZones.Count;
        }

        private void ResetStats()
        {
            _stats = new LightingZoneStats
            {
                TotalZones = 0,
                ActiveZones = 0,
                ZonesCreated = 0,
                ZonesDestroyed = 0,
                ZoneUpdates = 0,
                ZoneToggleEvents = 0,
                LightAssignments = 0,
                LightRemovals = 0,
                TotalAssignedLights = 0
            };
        }
    }

    /// <summary>
    /// Lighting zone statistics
    /// </summary>
    [System.Serializable]
    public struct LightingZoneStats
    {
        public int TotalZones;
        public int ActiveZones;
        public int ZonesCreated;
        public int ZonesDestroyed;
        public int ZoneUpdates;
        public int ZoneToggleEvents;
        public int LightAssignments;
        public int LightRemovals;
        public int TotalAssignedLights;
    }
}