using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectChimera.Data.Genetics;
using ProjectChimera.Data.Shared;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Cultivation
{
    [System.Serializable]
    public class PlantUpdateConfiguration
    {
        public bool EnableStressSystem { get; set; } = true;
        public bool EnableGxEInteractions { get; set; } = true;
        public bool EnableAdvancedGenetics { get; set; } = true;
        public bool EnablePerformanceOptimization { get; set; } = true;
        public float CacheUpdateInterval { get; set; } = 5f; // Seconds
        public int MaxCacheSize { get; set; } = 1000;
        public float StressThreshold { get; set; } = 0.3f;
        public float AdaptationRate { get; set; } = 0.1f;

        /// <summary>
        /// Create default configuration
        /// </summary>
        public static PlantUpdateConfiguration CreateDefault()
        {
            return new PlantUpdateConfiguration();
        }

        /// <summary>
        /// Create high-performance configuration
        /// </summary>
        public static PlantUpdateConfiguration CreateHighPerformance()
        {
            return new PlantUpdateConfiguration
            {
                EnableStressSystem = true,
                EnableGxEInteractions = false, // Disable for better performance
                EnableAdvancedGenetics = false, // Disable for better performance
                EnablePerformanceOptimization = true,
                CacheUpdateInterval = 10f, // Longer intervals
                MaxCacheSize = 500 // Smaller cache
            };
        }

        /// <summary>
        /// Create maximum quality configuration
        /// </summary>
        public static PlantUpdateConfiguration CreateMaxQuality()
        {
            return new PlantUpdateConfiguration
            {
                EnableStressSystem = true,
                EnableGxEInteractions = true,
                EnableAdvancedGenetics = true,
                EnablePerformanceOptimization = false, // Prioritize accuracy over speed
                CacheUpdateInterval = 1f, // Frequent updates
                MaxCacheSize = 2000, // Larger cache
                AdaptationRate = 0.05f // Slower, more realistic adaptation
            };
        }
    }

    /// <summary>
    /// Update statistics for monitoring system performance
    /// </summary>

}
