// REFACTORED: Wind Zone Manager
// Extracted from WindSystem for better separation of concerns

using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Services.SpeedTree.Environmental
{
    /// <summary>
    /// Manages wind zones and their settings
    /// </summary>
    public class WindZoneManager
    {
        private readonly Dictionary<WindZone, WindZoneSettings> _windZones = new Dictionary<WindZone, WindZoneSettings>();
        private readonly List<WindZone> _activeWindZones = new List<WindZone>();

        public IReadOnlyList<WindZone> ActiveWindZones => _activeWindZones;
        public int WindZoneCount => _windZones.Count;

        public void RegisterWindZone(WindZone windZone)
        {
            if (windZone == null || _windZones.ContainsKey(windZone)) return;

            var settings = new WindZoneSettings(windZone);
            _windZones[windZone] = settings;
            _activeWindZones.Add(windZone);

            ChimeraLogger.Log("SPEEDTREE/WIND", $"Registered wind zone: {windZone.name}", null);
        }

        public void UnregisterWindZone(WindZone windZone)
        {
            if (windZone == null) return;

            _windZones.Remove(windZone);
            _activeWindZones.Remove(windZone);

            ChimeraLogger.Log("SPEEDTREE/WIND", $"Unregistered wind zone: {windZone.name}", null);
        }

        public void UpdateWindZone(WindZone windZone)
        {
            if (windZone == null || !_windZones.ContainsKey(windZone)) return;

            var settings = _windZones[windZone];
            settings.Strength = windZone.windMain;
            settings.Direction = windZone.transform.forward;
            settings.Strength *= UnityEngine.Random.Range(0.9f, 1.1f);
        }

        public WindZoneSettings GetWindZoneSettings(WindZone windZone)
        {
            return _windZones.TryGetValue(windZone, out var settings) ? settings : null;
        }

        public void Clear()
        {
            _windZones.Clear();
            _activeWindZones.Clear();
        }
    }
}

