using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectChimera.Data.Genetics;
using ProjectChimera.Data.Shared;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Cultivation
{
    [System.Serializable]
    public class ActiveStressor
    {
        public bool IsActive { get; set; } = true;
        public float Intensity { get; set; }
        public object StressSource { get; set; }
        public float StartTime { get; set; }
        public float Duration { get; set; }

        /// <summary>
        /// Check if the stressor has been active for a significant duration
        /// </summary>
        public bool IsChronic => Duration > 300f; // 5 minutes

        /// <summary>
        /// Get the current severity based on intensity and duration
        /// </summary>
        public float GetCurrentSeverity()
        {
            float stressMultiplier = 1f;

            // Try to get stress multiplier from different possible types
            if (StressSource is StressFactor stressFactor)
            {
                stressMultiplier = stressFactor.StressMultiplier;
            }
            else if (StressSource is ProjectChimera.Data.Simulation.EnvironmentalStressSO environmentalStress)
            {
                stressMultiplier = environmentalStress.StressMultiplier;
            }

            float baseSeverity = Intensity * stressMultiplier;

            // Chronic stress becomes more severe over time
            if (IsChronic)
            {
                float chronicMultiplier = 1f + (Duration - 300f) / 600f; // +50% after 10 minutes
                baseSeverity *= Mathf.Clamp(chronicMultiplier, 1f, 1.5f);
            }

            return Mathf.Clamp01(baseSeverity);
        }
    }

    /// <summary>
    /// Source of stress affecting plant health
    /// </summary>

}
