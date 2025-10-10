using System;
using UnityEngine;
using ProjectChimera.Data.Processing;

namespace ProjectChimera.Systems.Processing
{
    /// <summary>
    /// Helper utilities for CuringSystem calculations.
    /// Extracted to maintain Phase 0 file size compliance.
    /// </summary>
    public static class CuringSystemHelpers
    {
        /// <summary>
        /// Calculates terpene preservation (0-1).
        /// </summary>
        public static float CalculateTerpenePreservation(ProcessingBatch batch)
        {
            // Ideal humidity preserves terpenes
            if (batch.JarHumidity >= 0.60f && batch.JarHumidity <= 0.64f)
                return 1.0f;

            // Slight deviation
            if (batch.JarHumidity >= 0.58f && batch.JarHumidity <= 0.66f)
                return 0.9f;

            // Too dry = terpene loss
            if (batch.JarHumidity < 0.55f)
                return 0.6f - (0.55f - batch.JarHumidity);

            // Too humid = ok preservation but mold risk
            return 0.8f;
        }

        /// <summary>
        /// Gets human-readable curing status.
        /// </summary>
        public static string GetCuringStatus(ProcessingBatch batch, CuringJarConfig jarConfig)
        {
            if (jarConfig.NeedsBurping())
                return "⏰ Burp needed";

            if (batch.JarHumidity > 0.70f)
                return "⚠️ Too humid - burp soon";

            if (batch.JarHumidity < 0.58f)
                return "⚠️ Too dry - reduce burping";

            if (batch.JarHumidity >= 0.60f && batch.JarHumidity <= 0.64f)
                return "✅ Perfect cure conditions";

            return "✓ Curing in progress";
        }

        /// <summary>
        /// Gets burp frequency based on cure weeks elapsed.
        /// </summary>
        public static int GetBurpFrequency(int weeksElapsed)
        {
            if (weeksElapsed < 2) return 24;    // Daily week 1-2
            if (weeksElapsed < 4) return 48;    // Every 2 days week 3-4
            return 168;                          // Weekly week 5+
        }
    }
}
