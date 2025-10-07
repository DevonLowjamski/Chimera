using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectChimera.Data.Genetics;
using ProjectChimera.Data.Shared;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Systems.Cultivation
{
    [System.Serializable]
    internal class StressResponse
    {
        public float OverallStressLevel { get; set; }
        public float AdaptiveCapacity { get; set; }
        public List<StressFactor> ActiveStresses { get; set; } = new List<StressFactor>();

        /// <summary>
        /// Check if the stress response indicates significant stress
        /// </summary>
        public bool HasSignificantStress => OverallStressLevel > 0.3f;

        /// <summary>
        /// Check if the plant has good adaptive capacity
        /// </summary>
        public bool HasGoodAdaptiveCapacity => AdaptiveCapacity > 0.7f;

        /// <summary>
        /// Get the most severe stress factor
        /// </summary>
        public StressFactor GetMostSevereStress()
        {
            StressFactor mostSevere = null;
            float maxSeverity = 0f;

            foreach (var stress in ActiveStresses)
            {
                if (stress.Severity > maxSeverity)
                {
                    maxSeverity = stress.Severity;
                    mostSevere = stress;
                }
            }

            return mostSevere;
        }
    }

    /// <summary>
    /// Represents a specific stress factor affecting the plant
    /// </summary>

}
