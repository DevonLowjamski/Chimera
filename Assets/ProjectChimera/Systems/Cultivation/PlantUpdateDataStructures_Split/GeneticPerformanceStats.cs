using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectChimera.Data.Genetics;
using ProjectChimera.Data.Shared;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Cultivation
{
    [System.Serializable]
    public class GeneticPerformanceStats
    {
        public int TotalCalculations { get; set; }
        public double AverageCalculationTimeMs { get; set; }
        public double CacheHitRatio { get; set; }
        public int BatchCalculations { get; set; }
        public double AverageBatchTimeMs { get; set; }
        public double AverageUpdateTimeMs { get; set; }
        public int CacheSize { get; set; }
        public DateTime LastUpdate { get; set; }

        /// <summary>
        /// Check if performance is within acceptable thresholds
        /// </summary>
        public bool IsPerformanceGood()
        {
            return AverageCalculationTimeMs < 5.0 && // Less than 5ms per calculation
                   CacheHitRatio > 0.7 && // At least 70% cache hit rate
                   AverageUpdateTimeMs < 10.0; // Less than 10ms per update
        }

        /// <summary>
        /// Get performance summary string
        /// </summary>
        public string GetPerformanceSummary()
        {
            return $"Calculations: {TotalCalculations}, Avg Time: {AverageCalculationTimeMs:F2}ms, " +
                   $"Cache Hit: {CacheHitRatio:P1}, Batch Avg: {AverageBatchTimeMs:F2}ms";
        }
    }

    /// <summary>
    /// Cannabinoid profile for harvest results
    /// </summary>

}
