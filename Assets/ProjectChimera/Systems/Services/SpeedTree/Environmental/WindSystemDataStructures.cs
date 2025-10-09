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
        public int TotalGusts;
        public System.DateTime LastStatisticsUpdate;

        // Aliases for backward compatibility
        public float GlobalStrength { get => GlobalWindStrength; set => GlobalWindStrength = value; }
        public Vector3 GlobalDirection { get => WindDirection; set => WindDirection = value; }
        public int GustCount { get => TotalGusts; set => TotalGusts = value; }
        public float AverageStrength { get => (GlobalWindStrength + CurrentWindStrength) / 2f; }
        public System.DateTime LastUpdate { get => LastStatisticsUpdate; set => LastStatisticsUpdate = value; }
    }
}

