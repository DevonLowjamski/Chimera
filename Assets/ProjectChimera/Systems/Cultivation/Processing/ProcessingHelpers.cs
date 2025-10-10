using UnityEngine;

namespace ProjectChimera.Systems.Cultivation.Processing
{
    /// <summary>
    /// Helper methods for Processing system calculations.
    /// Extracted to maintain Phase 0 file size compliance.
    /// </summary>
    public static class ProcessingHelpers
    {
        /// <summary>
        /// Gets drying duration in days based on method.
        /// </summary>
        public static float GetDryingDuration(DryingMethod method,
            float hangDryDays, float rackDryDays, float freezeDryDays)
        {
            return method switch
            {
                DryingMethod.HangDry => hangDryDays,
                DryingMethod.RackDry => rackDryDays,
                DryingMethod.FreezeDry => freezeDryDays,
                _ => hangDryDays
            };
        }

        /// <summary>
        /// Calculates moisture loss over time using exponential decay curve.
        /// </summary>
        public static float CalculateMoistureLoss(float startMoisture,
            float targetMoisture, float progress)
        {
            float moistureRange = startMoisture - targetMoisture;
            float currentMoisture = startMoisture - (moistureRange * progress);
            return Mathf.Max(currentMoisture, targetMoisture);
        }

        /// <summary>
        /// Gets curing quality multiplier based on method.
        /// </summary>
        public static float GetCuringQualityMultiplier(CuringMethod method,
            float jarQuality, float turkeyBagQuality, float groveBagQuality)
        {
            return method switch
            {
                CuringMethod.JarCuring => jarQuality,
                CuringMethod.TurkeyBag => turkeyBagQuality,
                CuringMethod.GroveBag => groveBagQuality,
                _ => jarQuality
            };
        }

        /// <summary>
        /// Calculates curing quality improvement over time.
        /// Quality increases from base to peak, then stabilizes.
        /// </summary>
        public static float CalculateCuringQuality(float baseQuality,
            float elapsedWeeks, float minWeeks, float optimalWeeks,
            float methodMultiplier)
        {
            if (elapsedWeeks < minWeeks)
            {
                float earlyProgress = elapsedWeeks / minWeeks;
                return baseQuality * (0.8f + (0.2f * earlyProgress));
            }

            float progress = Mathf.Clamp01((elapsedWeeks - minWeeks) / (optimalWeeks - minWeeks));
            float qualityImprovement = 0.3f * progress;
            float currentQuality = baseQuality + qualityImprovement;

            return Mathf.Clamp01(currentQuality * methodMultiplier);
        }

        /// <summary>
        /// Calculates terpene preservation during curing.
        /// Terpenes develop early, then slowly degrade.
        /// </summary>
        public static float CalculateTerpenePreservation(float elapsedWeeks,
            float maxWeeks)
        {
            float peakWeeks = maxWeeks * 0.5f;

            if (elapsedWeeks <= peakWeeks)
            {
                return 0.85f + (0.15f * (elapsedWeeks / peakWeeks));
            }

            float degradationProgress = (elapsedWeeks - peakWeeks) / (maxWeeks - peakWeeks);
            float degradation = 0.1f * degradationProgress;
            return Mathf.Max(0.85f, 1.0f - degradation);
        }

        /// <summary>
        /// Calculates required burps for jar curing based on elapsed weeks.
        /// Week 1-2: Daily burping (14 burps)
        /// Week 3-4: Every 2 days (7 burps)
        /// Week 5+: Every 3 days
        /// </summary>
        public static int CalculateRequiredBurps(float elapsedWeeks)
        {
            if (elapsedWeeks <= 2f)
            {
                return (int)(elapsedWeeks * 7f);
            }
            else if (elapsedWeeks <= 4f)
            {
                return 14 + (int)((elapsedWeeks - 2f) * 3.5f);
            }
            else
            {
                return 21 + (int)((elapsedWeeks - 4f) * 2.33f);
            }
        }

        /// <summary>
        /// Gets market value multiplier for quality grade.
        /// </summary>
        public static float GetQualityMarketMultiplier(ProcessingQuality quality)
        {
            return quality switch
            {
                ProcessingQuality.PremiumPlus => 2.0f,
                ProcessingQuality.Premium => 1.5f,
                ProcessingQuality.Good => 1.0f,
                ProcessingQuality.Average => 0.7f,
                ProcessingQuality.Poor => 0.4f,
                _ => 1.0f
            };
        }
    }
}
