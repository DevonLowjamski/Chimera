using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectChimera.Data.Genetics;
using ProjectChimera.Data.Shared;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Cultivation
{
    [System.Serializable]
    public class StressFactor
    {
        public object StressType { get; set; }
        public float Severity { get; set; }
        public float Duration { get; set; }
        public bool IsAcute { get; set; } // True for sudden stress, false for chronic

        /// <summary>
        /// Stress multiplier for damage calculation
        /// </summary>
        public float StressMultiplier { get; set; } = 1f;

        /// <summary>
        /// Damage per second for this stress factor
        /// </summary>
        public float DamagePerSecond { get; set; } = 0.01f;

        /// <summary>
        /// Get stress type name for processing
        /// </summary>
        public string GetStressTypeName()
        {
            return StressType?.ToString() ?? "Unknown";
        }

        /// <summary>
        /// Check if this stress factor is biotic (disease/pest related)
        /// </summary>
        public bool IsBiotic()
        {
            var typeName = GetStressTypeName();
            return typeName.Contains("Disease") || typeName.Contains("Pest") || typeName.Contains("Biotic");
        }

        /// <summary>
        /// Check if this is a critical stress level
        /// </summary>
        public bool IsCritical => Severity > 0.8f;
    }

    /// <summary>
    /// Active stressor affecting plant health
    /// </summary>

}
