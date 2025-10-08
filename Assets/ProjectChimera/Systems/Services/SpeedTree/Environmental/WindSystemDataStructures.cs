// REFACTORED: Wind System Data Structures
// Extracted from WindSystem for better separation of concerns

using System;
using UnityEngine;

namespace ProjectChimera.Systems.Services.SpeedTree.Environmental
{
    /// <summary>
    /// Wind zone configuration settings
    /// </summary>
    [Serializable]
    public class WindZoneSettings
    {
        public float Strength;
        public Vector3 Direction;
        public float Radius;
        public WindZone Zone;

        public WindZoneSettings(WindZone zone)
        {
            Zone = zone;
            Strength = zone.windMain;
            Direction = zone.transform.forward;
            Radius = zone.radius;
        }
    }

    /// <summary>
    /// Wind system statistics and performance metrics
    /// </summary>
    [Serializable]
    public struct WindStatistics
    {
        public float GlobalWindStrength;
        public float CurrentWindStrength;
        public Vector3 WindDirection;
        public int ActiveWindZones;
        public bool WindAnimationEnabled;
        public float UpdateFrequency;
    }
}

