using UnityEngine;
using System;
using ProjectChimera.Data.Cultivation.Plant;
using ProjectChimera.Data.Cultivation;
using ProjectChimera.Data.Genetics;
using ProjectChimera.Data.Shared;
using PlantStrainSO = ProjectChimera.Data.Cultivation.PlantStrainSO;

namespace ProjectChimera.Systems.Gameplay
{
    /// <summary>
    /// Plant information data structure for gameplay monitoring and display
    /// Contains essential plant data for UI and monitoring systems
    /// </summary>
    [System.Serializable]
    public class PlantInfo
    {
        [Header("Plant Identity")]
        public string PlantID;
        public string PlantName;
        public string StrainName;

        [Header("Current Status")]
        public PlantGrowthStage CurrentStage;
        public float AgeInDays;
        public float OverallHealth;
        public Vector3 Position;

        [Header("Growth Metrics")]
        public float CurrentHeight;
        public float CurrentWidth;
        public float GrowthProgress;
        public float MaturityLevel;

        [Header("Environmental Needs")]
        public float WaterLevel;
        public float NutrientLevel;
        public float LightExposure;
        public float Temperature;
        public float Humidity;

        [Header("Health Indicators")]
        public float StressLevel;
        public float Vigor;
        public bool HasIssues;
        public string[] CurrentIssues;

        [Header("Harvest Information")]
        public bool IsHarvestReady;
        public float EstimatedYield;
        public DateTime EstimatedHarvestDate;
        public float QualityRating;

        /// <summary>
        /// Creates PlantInfo from a PlantInstanceSO
        /// </summary>
        public static PlantInfo FromPlantInstance(PlantInstanceSO plantInstance)
        {
            if (plantInstance == null) return null;

            return new PlantInfo
            {
                PlantID = plantInstance.PlantID,
                PlantName = plantInstance.PlantName,
                StrainName = GetStrainName(plantInstance.Strain),
                CurrentStage = plantInstance.CurrentGrowthStage,
                AgeInDays = plantInstance.AgeInDays,
                OverallHealth = plantInstance.OverallHealth,
                Position = plantInstance.WorldPosition,
                CurrentHeight = plantInstance.CurrentHeight,
                CurrentWidth = plantInstance.CurrentWidth,
                GrowthProgress = plantInstance.GrowthProgress,
                MaturityLevel = plantInstance.MaturityLevel,
                WaterLevel = plantInstance.WaterLevel,
                NutrientLevel = plantInstance.NutrientLevel,
                LightExposure = 0.8f, // Default value
                Temperature = 22f, // Default value
                Humidity = 60f, // Default value
                StressLevel = plantInstance.StressLevel,
                Vigor = plantInstance.Vigor,
                HasIssues = plantInstance.StressLevel > 0.5f || plantInstance.OverallHealth < 0.7f,
                CurrentIssues = plantInstance.StressLevel > 0.5f ? new[] { "High stress" } : new string[0],
                IsHarvestReady = plantInstance.MaturityLevel >= 1f,
                EstimatedYield = 50f, // Default estimation
                EstimatedHarvestDate = DateTime.Now.AddDays(7), // Default estimation
                QualityRating = plantInstance.OverallHealth
            };
        }

        /// <summary>
        /// Gets a summary string for display
        /// </summary>
        public string GetDisplaySummary()
        {
            return $"{PlantName} - {CurrentStage} - Health: {OverallHealth:F1} - Age: {AgeInDays:F0}d";
        }

        /// <summary>
        /// Checks if the plant requires immediate attention
        /// </summary>
        public bool RequiresAttention()
        {
            return HasIssues || WaterLevel < 0.3f || NutrientLevel < 0.3f || OverallHealth < 0.5f;
        }

        /// <summary>
        /// Safely extracts strain name from PlantStrainSO, handling potential type conflicts
        /// </summary>
        private static string GetStrainName(object strain)
        {
            if (strain == null) return "Unknown";

            // Use pattern matching instead of reflection
            switch (strain)
            {
                case ProjectChimera.Data.Cultivation.PlantStrainSO cultivationStrain:
                    return cultivationStrain.StrainName ?? "Unknown";
                case ProjectChimera.Data.Genetics.GeneticPlantStrainSO geneticsStrain:
                    return geneticsStrain.StrainName ?? "Unknown";
                case ProjectChimera.Data.Genetics.CannabisStrainAssetSO cannabisStrain:
                    return cannabisStrain.StrainName ?? "Unknown";
                default:
                    return strain.ToString() ?? "Unknown";
            }
        }
    }
}
