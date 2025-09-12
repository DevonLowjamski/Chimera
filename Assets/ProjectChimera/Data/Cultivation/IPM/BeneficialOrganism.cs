using UnityEngine;
using System;

namespace ProjectChimera.Data.Cultivation.IPM
{
    /// <summary>
    /// Represents a beneficial organism used in biological pest control
    /// </summary>
    [System.Serializable]
    public class BeneficialOrganism
    {
        [Header("Basic Information")]
        public string OrganismName;
        public BeneficialType OrganismType; // Renamed to match usage
        public BeneficialType BeneficialType;
        public string ScientificName;
        public string Description;
        public float LifeCycle = 21f; // Days for complete life cycle
        public bool RequiresRepeatedReleases = false;
        public float ToleratedTemperatureRange = 5f; // Temperature tolerance range
        public string SpecialRequirements = "";
        public float EffectivenessRating = 0.8f;

        [Header("Target Pests")]
        public string[] TargetPests;
        public float[] EffectivenessAgainstTargets;

        [Header("Environmental Requirements")]
        public Vector2 OptimalTemperature = new Vector2(20f, 28f);
        public Vector2 OptimalHumidity = new Vector2(60f, 80f);
        public Vector2 OptimalLightHours = new Vector2(16f, 18f);
        public Vector2 OptimalPhotoperiod = new Vector2(12f, 16f); // Added for compatibility
        public float OptimalPH = 6.5f;

        [Header("Release and Establishment")]
        public float ReleaseRate = 2f; // per square meter
        public float EstablishmentTime = 7f; // days
        public float PopulationBuildTime = 14f; // days to reach effective population
        public float Lifespan = 30f; // days

        [Header("Effectiveness Metrics")]
        public float BaseEffectiveness = 0.8f;
        public float Persistence = 0.7f; // how long effects last after release
        public float ReproductionRate = 1.2f;

        [Header("Cost and Availability")]
        public float CostPerUnit = 0.5f;
        public bool IsCommerciallyAvailable = true;
        public string Supplier;
        public string StorageRequirements;

        /// <summary>
        /// Check if environmental conditions are suitable for this organism
        /// </summary>
        public bool IsEnvironmentSuitable(float temperature, float humidity, float ph)
        {
            return temperature >= OptimalTemperature.x && temperature <= OptimalTemperature.y &&
                   humidity >= OptimalHumidity.x && humidity <= OptimalHumidity.y &&
                   Mathf.Abs(ph - OptimalPH) <= 0.5f;
        }

        /// <summary>
        /// Calculate effectiveness based on environmental conditions
        /// </summary>
        public float CalculateEffectiveness(float temperature, float humidity, float ph)
        {
            if (!IsEnvironmentSuitable(temperature, humidity, ph))
                return BaseEffectiveness * 0.5f; // Reduced effectiveness in suboptimal conditions

            return BaseEffectiveness;
        }

        /// <summary>
        /// Get the recommended release schedule
        /// </summary>
        public ReleaseSchedule GetRecommendedSchedule()
        {
            return new ReleaseSchedule
            {
                InitialRelease = ReleaseRate,
                FollowUpReleases = ReleaseRate * 0.5f,
                ReleaseInterval = EstablishmentTime / 2f,
                TotalReleases = 3
            };
        }
    }

    /// <summary>
    /// Release schedule for beneficial organisms
    /// </summary>
    [System.Serializable]
    public class ReleaseSchedule
    {
        public float InitialRelease;
        public float FollowUpReleases;
        public float ReleaseInterval;
        public int TotalReleases;
        public DateTime LastReleaseDate;
        public DateTime NextReleaseDate;
        
        /// <summary>
        /// Check if a release should occur at the given time
        /// </summary>
        public bool ShouldRelease(DateTime currentTime)
        {
            return currentTime >= NextReleaseDate;
        }
    }
}
