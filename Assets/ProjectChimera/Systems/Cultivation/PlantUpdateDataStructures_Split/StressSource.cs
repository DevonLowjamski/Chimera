using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectChimera.Data.Genetics;
using ProjectChimera.Data.Shared;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Cultivation
{
    [System.Serializable]
    public class StressSource
    {
        public object StressType { get; set; }
        public float DamagePerSecond { get; set; }
        public float StressMultiplier { get; set; } = 1f;
        public string Description { get; set; }

        /// <summary>
        /// Check if this is a biotic stress source
        /// </summary>
        public bool IsBiotic()
        {
            var typeName = StressType?.ToString() ?? "";
            return typeName.Contains("Biotic") || typeName.Contains("Disease") || typeName.Contains("Pest");
        }

        /// <summary>
        /// Check if this is an abiotic stress source
        /// </summary>
        public bool IsAbiotic()
        {
            var typeName = StressType?.ToString() ?? "";
            return typeName.Contains("Abiotic") || typeName.Contains("Temperature") ||
                   typeName.Contains("Light") || typeName.Contains("Water") || typeName.Contains("Nutrient");
        }
    }

    /// <summary>
    /// Performance metrics for genetic calculations
    /// </summary>

}
