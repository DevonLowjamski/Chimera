using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectChimera.Data.Genetics;
using ProjectChimera.Data.Shared;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Cultivation
{
    [System.Serializable]
    public class CannabinoidProfile
    {
        public float THC { get; set; }
        public float CBD { get; set; }
        public float CBG { get; set; }
        public float CBN { get; set; }
        public float CBC { get; set; }
        public float THCA { get; set; }
        public float CBDA { get; set; }

        /// <summary>
        /// Get total cannabinoid content
        /// </summary>
        public float GetTotalCannabinoids()
        {
            return THC + CBD + CBG + CBN + CBC + THCA + CBDA;
        }

        /// <summary>
        /// Get THC to CBD ratio
        /// </summary>
        public float GetTHCToCBDRatio()
        {
            return CBD > 0f ? THC / CBD : float.MaxValue;
        }

        /// <summary>
        /// Classify the cannabinoid profile
        /// </summary>
        public string GetProfileType()
        {
            float ratio = GetTHCToCBDRatio();

            if (THC > 0.15f && CBD < 0.05f) return "THC Dominant";
            if (CBD > 0.15f && THC < 0.05f) return "CBD Dominant";
            if (ratio >= 0.5f && ratio <= 2f) return "Balanced";
            if (THC < 0.05f && CBD < 0.05f) return "Low Potency";

            return "Mixed Profile";
        }
    }

    /// <summary>
    /// Terpene profile for harvest results
    /// </summary>

}
