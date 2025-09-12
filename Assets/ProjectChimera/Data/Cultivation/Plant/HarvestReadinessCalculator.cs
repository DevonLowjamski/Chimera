using UnityEngine;
using System;
using System.Collections.Generic;

namespace ProjectChimera.Data.Cultivation.Plant
{
    /// <summary>
    /// Harvest Readiness Calculator - Determines optimal harvest timing.
    /// Analyzes plant maturity, trichome development, health status,
    /// and environmental factors to determine the best harvest time.
    /// </summary>
    public static class HarvestReadinessCalculator
    {
        // Harvest quality thresholds
        private const float PREMIUM_QUALITY_THRESHOLD = 0.9f;
        private const float GOOD_QUALITY_THRESHOLD = 0.75f;
        private const float FAIR_QUALITY_THRESHOLD = 0.6f;

        /// <summary>
        /// Determines if plant is ready for harvest
        /// </summary>
        public static HarvestReadiness AssessHarvestReadiness(PlantStateData plantState)
        {
            var readiness = new HarvestReadiness
            {
                PlantID = plantState.PlantID,
                IsReadyForHarvest = false,
                RecommendedHarvestDate = DateTime.MinValue,
                ReadinessScore = 0f
            };

            // Check if plant is in harvest stage
            if (plantState.CurrentGrowthStage != PlantGrowthStage.Ripening &&
                plantState.CurrentGrowthStage != PlantGrowthStage.Harvest)
            {
                readiness.Reason = "Plant not in harvest stage";
                return readiness;
            }

            // Calculate readiness factors
            float maturityReadiness = plantState.MaturityLevel;
            float trichomeReadiness = CalculateTrichomeReadiness(plantState);
            float pistilReadiness = CalculatePistilReadiness(plantState);
            float healthReadiness = plantState.OverallHealth;

            readiness.ReadinessScore = (maturityReadiness + trichomeReadiness + pistilReadiness + healthReadiness) / 4f;

            // Determine if ready for harvest
            readiness.IsReadyForHarvest = readiness.ReadinessScore >= FAIR_QUALITY_THRESHOLD;

            // Calculate recommended harvest date
            readiness.RecommendedHarvestDate = CalculateRecommendedHarvestDate(plantState, readiness.ReadinessScore);

            // Provide reasoning
            readiness.Reason = GenerateReadinessReason(readiness, maturityReadiness, trichomeReadiness, pistilReadiness, healthReadiness);

            // Calculate days until optimal harvest
            if (readiness.IsReadyForHarvest)
            {
                readiness.DaysUntilOptimal = CalculateDaysUntilOptimal(readiness.RecommendedHarvestDate);
            }

            return readiness;
        }

        /// <summary>
        /// Calculates harvest window based on plant state
        /// </summary>
        public static HarvestWindow CalculateHarvestWindow(PlantStateData plantState)
        {
            var window = new HarvestWindow
            {
                PlantID = plantState.PlantID,
                EarliestHarvestDate = DateTime.Now,
                OptimalHarvestDate = DateTime.Now.AddDays(7),
                LatestHarvestDate = DateTime.Now.AddDays(14)
            };

            // Adjust window based on plant characteristics
            if (plantState.Strain?.FloweringTime != null)
            {
                int baseDays = (int)plantState.Strain.FloweringTime;
                window.EarliestHarvestDate = DateTime.Now.AddDays(baseDays - 3);
                window.OptimalHarvestDate = DateTime.Now.AddDays(baseDays);
                window.LatestHarvestDate = DateTime.Now.AddDays(baseDays + 7);
            }

            // Adjust for environmental conditions
            if (plantState.Environment?.Temperature > 30f)
            {
                // Hot conditions accelerate ripening
                window.EarliestHarvestDate = window.EarliestHarvestDate.AddDays(-2);
                window.OptimalHarvestDate = window.OptimalHarvestDate.AddDays(-1);
                window.LatestHarvestDate = window.LatestHarvestDate.AddDays(-3);
            }

            // Calculate window quality scores
            window.EarlyHarvestQuality = CalculateWindowQuality(plantState, window.EarliestHarvestDate);
            window.OptimalHarvestQuality = CalculateWindowQuality(plantState, window.OptimalHarvestDate);
            window.LateHarvestQuality = CalculateWindowQuality(plantState, window.LatestHarvestDate);

            return window;
        }

        /// <summary>
        /// Calculates trichome readiness based on plant state
        /// </summary>
        private static float CalculateTrichomeReadiness(PlantStateData plantState)
        {
            // Trichome development is key indicator for harvest readiness
            // This would analyze trichome color, size, and density
            float trichomeMaturity = plantState.MaturityLevel;

            // Adjust based on strain characteristics
            if (plantState.Strain?.TrichomeDensity != null)
            {
                trichomeMaturity *= plantState.Strain.TrichomeDensity;
            }

            // Environmental factors affecting trichome development
            if (plantState.Environment?.Humidity < 40f)
            {
                // Low humidity can stress trichome development
                trichomeMaturity *= 0.9f;
            }

            return Mathf.Clamp01(trichomeMaturity);
        }

        /// <summary>
        /// Calculates pistil readiness
        /// </summary>
        private static float CalculatePistilReadiness(PlantStateData plantState)
        {
            // Pistil browning is another harvest indicator
            float pistilReadiness = plantState.MaturityLevel * 0.8f;

            // Strain-specific pistil characteristics
            if (plantState.Strain?.PistilColorChange != null)
            {
                pistilReadiness *= plantState.Strain.PistilColorChange;
            }

            return Mathf.Clamp01(pistilReadiness);
        }

        /// <summary>
        /// Calculates recommended harvest date
        /// </summary>
        private static DateTime CalculateRecommendedHarvestDate(PlantStateData plantState, float readinessScore)
        {
            DateTime baseDate = DateTime.Now;

            if (readinessScore >= PREMIUM_QUALITY_THRESHOLD)
            {
                // Ready now for premium quality
                return baseDate;
            }
            else if (readinessScore >= GOOD_QUALITY_THRESHOLD)
            {
                // Ready in 2-3 days
                return baseDate.AddDays(2.5f);
            }
            else if (readinessScore >= FAIR_QUALITY_THRESHOLD)
            {
                // Ready in 5-7 days
                return baseDate.AddDays(6);
            }
            else
            {
                // Not ready yet
                return baseDate.AddDays(10);
            }
        }

        /// <summary>
        /// Generates readiness reason string
        /// </summary>
        private static string GenerateReadinessReason(HarvestReadiness readiness, float maturity, float trichomes, float pistils, float health)
        {
            if (readiness.ReadinessScore >= PREMIUM_QUALITY_THRESHOLD)
            {
                return "Premium quality - All indicators show optimal harvest timing";
            }
            else if (readiness.ReadinessScore >= GOOD_QUALITY_THRESHOLD)
            {
                return "Good quality - Plant showing strong harvest indicators";
            }
            else if (readiness.ReadinessScore >= FAIR_QUALITY_THRESHOLD)
            {
                return "Fair quality - Plant approaching harvest readiness";
            }
            else
            {
                var lowestFactor = Mathf.Min(maturity, trichomes, pistils, health);
                if (lowestFactor == maturity) return "Low maturity - Plant needs more time to develop";
                if (lowestFactor == trichomes) return "Trichomes not ready - Wait for full development";
                if (lowestFactor == pistils) return "Pistils not browned - Allow more ripening time";
                if (lowestFactor == health) return "Plant health issues - Address before harvesting";
                return "Plant not ready for harvest";
            }
        }

        /// <summary>
        /// Calculates days until optimal harvest
        /// </summary>
        private static int CalculateDaysUntilOptimal(DateTime recommendedDate)
        {
            return Mathf.Max(0, (int)(recommendedDate - DateTime.Now).TotalDays);
        }

        /// <summary>
        /// Calculates harvest quality for a specific date
        /// </summary>
        private static float CalculateWindowQuality(PlantStateData plantState, DateTime harvestDate)
        {
            // Simplified quality calculation based on timing
            float daysFromNow = (float)(harvestDate - DateTime.Now).TotalDays;

            if (daysFromNow < 0)
            {
                // Past optimal time - quality decreases
                return Mathf.Max(0.3f, 1f - Mathf.Abs(daysFromNow) * 0.1f);
            }
            else if (daysFromNow < 3)
            {
                // Within optimal window
                return 0.9f + (daysFromNow * 0.03f);
            }
            else
            {
                // Future harvest - quality prediction
                return Mathf.Max(0.4f, 0.8f - (daysFromNow - 3) * 0.05f);
            }
        }
    }

    /// <summary>
    /// Harvest readiness assessment data structure
    /// </summary>
    [System.Serializable]
    public class HarvestReadiness
    {
        public string PlantID;
        public bool IsReadyForHarvest;
        public DateTime RecommendedHarvestDate;
        public float ReadinessScore;
        public string ReadinessReason;
        public int DaysUntilOptimal;
        public HarvestWindow OptimalHarvestWindow;
    }

    /// <summary>
    /// Optimal harvest window data structure
    /// </summary>
    [System.Serializable]
    public class HarvestWindow
    {
        public DateTime StartDate;
        public DateTime EndDate;
        public float QualityScore;

        // Additional properties for compatibility
        public string PlantID { get; set; }
        public DateTime EarliestHarvestDate => StartDate;
        public DateTime OptimalHarvestDate => StartDate.AddDays((EndDate - StartDate).TotalDays / 2);
        public DateTime LatestHarvestDate => EndDate;

        // Additional properties for harvest quality
        public float EarlyHarvestQuality => QualityScore * 0.8f;
        public float OptimalHarvestQuality => QualityScore;
        public float LateHarvestQuality => QualityScore * 0.6f;
    }
}
