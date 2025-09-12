using UnityEngine;
using ProjectChimera.Data.Shared;

namespace ProjectChimera.Data.Cultivation.Plant
{
    /// <summary>
    /// SIMPLE: Basic plant growth mechanics aligned with Project Chimera's cultivation vision.
    /// Focuses on essential plant growth stages and basic development tracking.
    /// </summary>
    public static class PlantGrowth
    {
        // Basic growth parameters
        private const float BASE_GROWTH_RATE = 1f; // cm per day
        private const float SEEDLING_HEIGHT = 5f;
        private const float VEGETATIVE_HEIGHT = 50f;
        private const float FLOWERING_HEIGHT = 150f;

        /// <summary>
        /// Process basic daily plant growth
        /// </summary>
        public static GrowthResult ProcessDailyGrowth(PlantInstance plant, float timeMultiplier = 1f)
        {
            if (plant == null) return null;

            var result = new GrowthResult
            {
                PlantID = plant.PlantID,
                InitialHeight = plant.CurrentHeight,
                InitialStage = plant.CurrentGrowthStage
            };

            // Basic growth calculation
            float growthRate = CalculateGrowthRate(plant);
            float dailyGrowth = growthRate * timeMultiplier;

            // Apply growth
            plant.CurrentHeight += dailyGrowth;
            plant.AgeInDays += timeMultiplier;

            // Check for stage transition
            bool stageTransitioned = CheckStageTransition(plant);

            // Update result
            result.FinalHeight = plant.CurrentHeight;
            result.FinalStage = plant.CurrentGrowthStage;
            result.HeightGrowth = dailyGrowth;
            result.StageTransitioned = stageTransitioned;
            result.GrowthRate = growthRate;

            return result;
        }

        /// <summary>
        /// Calculate basic growth rate for a plant
        /// </summary>
        public static float CalculateGrowthRate(PlantInstance plant)
        {
            if (plant == null) return 0f;

            float baseRate = BASE_GROWTH_RATE;

            // Modify based on growth stage
            switch (plant.CurrentGrowthStage)
            {
                case PlantGrowthStage.Seedling:
                    baseRate *= 0.5f;
                    break;
                case PlantGrowthStage.Vegetative:
                    baseRate *= 1.0f;
                    break;
                case PlantGrowthStage.Flowering:
                    baseRate *= 0.3f;
                    break;
                default:
                    baseRate *= 0.1f;
                    break;
            }

            // Modify based on health (simple implementation)
            if (plant.Health < 0.5f)
            {
                baseRate *= 0.5f; // Reduced growth when unhealthy
            }

            return baseRate;
        }

        /// <summary>
        /// Check if plant should transition to next growth stage
        /// </summary>
        public static bool CheckStageTransition(PlantInstance plant)
        {
            if (plant == null) return false;

            PlantGrowthStage nextStage = GetNextStage(plant.CurrentGrowthStage);
            if (nextStage == plant.CurrentGrowthStage) return false;

            // Simple time-based transitions
            float daysInStage = plant.AgeInDays - GetStageStartDay(plant.CurrentGrowthStage);

            switch (plant.CurrentGrowthStage)
            {
                case PlantGrowthStage.Seedling:
                    if (daysInStage >= 14f) // 2 weeks
                    {
                        plant.CurrentGrowthStage = nextStage;
                        return true;
                    }
                    break;
                case PlantGrowthStage.Vegetative:
                    if (daysInStage >= 30f) // 30 days
                    {
                        plant.CurrentGrowthStage = nextStage;
                        return true;
                    }
                    break;
                case PlantGrowthStage.Flowering:
                    // Stay in flowering until harvested
                    break;
            }

            return false;
        }

        /// <summary>
        /// Get the next growth stage
        /// </summary>
        public static PlantGrowthStage GetNextStage(PlantGrowthStage currentStage)
        {
            switch (currentStage)
            {
                case PlantGrowthStage.Seedling:
                    return PlantGrowthStage.Vegetative;
                case PlantGrowthStage.Vegetative:
                    return PlantGrowthStage.Flowering;
                case PlantGrowthStage.Flowering:
                    return PlantGrowthStage.Flowering; // Stay in flowering
                default:
                    return currentStage;
            }
        }

        /// <summary>
        /// Get the starting day for a growth stage
        /// </summary>
        private static float GetStageStartDay(PlantGrowthStage stage)
        {
            switch (stage)
            {
                case PlantGrowthStage.Seedling:
                    return 0f;
                case PlantGrowthStage.Vegetative:
                    return 14f; // After seedling
                case PlantGrowthStage.Flowering:
                    return 44f; // After vegetative
                default:
                    return 0f;
            }
        }

        /// <summary>
        /// Calculate expected height for a growth stage
        /// </summary>
        public static float GetExpectedHeightForStage(PlantGrowthStage stage)
        {
            switch (stage)
            {
                case PlantGrowthStage.Seedling:
                    return SEEDLING_HEIGHT;
                case PlantGrowthStage.Vegetative:
                    return VEGETATIVE_HEIGHT;
                case PlantGrowthStage.Flowering:
                    return FLOWERING_HEIGHT;
                default:
                    return 0f;
            }
        }

        /// <summary>
        /// Get growth stage display name
        /// </summary>
        public static string GetStageDisplayName(PlantGrowthStage stage)
        {
            switch (stage)
            {
                case PlantGrowthStage.Seedling:
                    return "Seedling";
                case PlantGrowthStage.Vegetative:
                    return "Vegetative";
                case PlantGrowthStage.Flowering:
                    return "Flowering";
                default:
                    return "Unknown";
            }
        }

        /// <summary>
        /// Calculate growth progress (0-1)
        /// </summary>
        public static float CalculateGrowthProgress(PlantInstance plant)
        {
            if (plant == null) return 0f;

            float expectedHeight = GetExpectedHeightForStage(plant.CurrentGrowthStage);
            if (expectedHeight <= 0f) return 1f;

            return Mathf.Clamp01(plant.CurrentHeight / expectedHeight);
        }
    }

    /// <summary>
    /// Basic growth result
    /// </summary>
    [System.Serializable]
    public class GrowthResult
    {
        public string PlantID;
        public float InitialHeight;
        public PlantGrowthStage InitialStage;
        public float FinalHeight;
        public PlantGrowthStage FinalStage;
        public float HeightGrowth;
        public bool StageTransitioned;
        public float GrowthRate;
    }
}
