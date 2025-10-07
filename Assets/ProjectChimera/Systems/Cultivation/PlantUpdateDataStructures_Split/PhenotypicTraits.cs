using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectChimera.Data.Genetics;
using ProjectChimera.Data.Shared;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Cultivation
{
    [System.Serializable]
    public class PhenotypicTraits
    {
        public float YieldMultiplier { get; set; } = 1f;
        public float PotencyMultiplier { get; set; } = 1f;
        public float GrowthRateMultiplier { get; set; } = 1f;
        public float DiseaseResistance { get; set; } = 1f;
        public float FloweringTime { get; set; } = 60f; // Days
        public float StretchFactor { get; set; } = 1f;
        public float BudDensity { get; set; } = 1f;
        public float TrichromeProduction { get; set; } = 1f;
        public float PlantHeight { get; set; } = 1f; // meters
        public float HeatTolerance { get; set; } = 1f;
        public float ColdTolerance { get; set; } = 1f;
        public float DroughtTolerance { get; set; } = 1f;
        public float QualityMultiplier { get; set; } = 1f;

        /// <summary>
        /// Calculate overall plant quality based on traits
        /// </summary>
        public float GetOverallQuality()
        {
            float qualityScore = (YieldMultiplier + PotencyMultiplier + DiseaseResistance +
                                 BudDensity + TrichromeProduction) / 5f;

            return Mathf.Clamp01(qualityScore);
        }

        /// <summary>
        /// Get trait category (Indica-dominant, Sativa-dominant, or Hybrid)
        /// </summary>
        public string GetTraitCategory()
        {
            // Simple classification based on flowering time and stretch factor
            if (FloweringTime < 55f && StretchFactor < 0.8f)
                return "Indica-Dominant";
            else if (FloweringTime > 70f && StretchFactor > 1.2f)
                return "Sativa-Dominant";
            else
                return "Hybrid";
        }

        /// <summary>
        /// Check if traits indicate a premium cultivar
        /// </summary>
        public bool IsPremiumCultivar()
        {
            return YieldMultiplier > 1.2f && PotencyMultiplier > 1.2f &&
                   DiseaseResistance > 0.8f && TrichromeProduction > 1.1f;
        }
    }

    /// <summary>
    /// Configuration options for plant update processing
    /// </summary>

}
