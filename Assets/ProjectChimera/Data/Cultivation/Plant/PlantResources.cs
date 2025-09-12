using UnityEngine;
using ProjectChimera.Data.Shared;

namespace ProjectChimera.Data.Cultivation.Plant
{
    /// <summary>
    /// SIMPLE: Basic plant resource management aligned with Project Chimera's cultivation vision.
    /// Focuses on essential plant care operations for basic cultivation mechanics.
    /// </summary>
    public static class PlantResources
    {
        // Basic consumption rates
        private const float DAILY_WATER_CONSUMPTION = 0.1f;
        private const float DAILY_NUTRIENT_CONSUMPTION = 0.05f;

        /// <summary>
        /// Update plant resources (simple daily consumption)
        /// </summary>
        public static void UpdatePlantResources(PlantStateData plantState, float deltaTime)
        {
            if (plantState == null) return;

            // Simple daily consumption
            float dailyConsumption = deltaTime / (24f * 60f * 60f); // Convert to daily fraction

            // Water consumption
            plantState.WaterLevel = Mathf.Clamp01(plantState.WaterLevel - DAILY_WATER_CONSUMPTION * dailyConsumption);

            // Nutrient consumption
            plantState.NutrientLevel = Mathf.Clamp01(plantState.NutrientLevel - DAILY_NUTRIENT_CONSUMPTION * dailyConsumption);

            // Energy decreases with low resources
            if (plantState.WaterLevel < 0.3f || plantState.NutrientLevel < 0.3f)
            {
                plantState.EnergyReserves = Mathf.Clamp01(plantState.EnergyReserves - 0.02f * dailyConsumption);
            }
            else
            {
                // Recover energy when resources are adequate
                plantState.EnergyReserves = Mathf.Clamp01(plantState.EnergyReserves + 0.01f * dailyConsumption);
            }
        }

        /// <summary>
        /// Water the plant
        /// </summary>
        public static void WaterPlant(PlantStateData plantState, float waterAmount)
        {
            if (plantState == null) return;

            plantState.WaterLevel = Mathf.Clamp01(plantState.WaterLevel + waterAmount);
            plantState.LastWatering = System.DateTime.Now;

            // Reduce energy drain when well-watered
            if (plantState.WaterLevel > 0.7f)
            {
                plantState.EnergyReserves = Mathf.Clamp01(plantState.EnergyReserves + 0.05f);
            }
        }

        /// <summary>
        /// Feed the plant nutrients
        /// </summary>
        public static void FeedPlant(PlantStateData plantState, float nutrientAmount)
        {
            if (plantState == null) return;

            plantState.NutrientLevel = Mathf.Clamp01(plantState.NutrientLevel + nutrientAmount);

            // Boost energy when well-fed
            if (plantState.NutrientLevel > 0.7f)
            {
                plantState.EnergyReserves = Mathf.Clamp01(plantState.EnergyReserves + 0.03f);
            }
        }

        /// <summary>
        /// Check if plant needs watering
        /// </summary>
        public static bool NeedsWatering(PlantStateData plantState)
        {
            return plantState != null && plantState.WaterLevel < 0.4f;
        }

        /// <summary>
        /// Check if plant needs feeding
        /// </summary>
        public static bool NeedsFeeding(PlantStateData plantState)
        {
            return plantState != null && plantState.NutrientLevel < 0.4f;
        }

        /// <summary>
        /// Get plant health status
        /// </summary>
        public static PlantHealthStatus GetHealthStatus(PlantStateData plantState)
        {
            if (plantState == null) return PlantHealthStatus.Dead;

            float averageResources = (plantState.WaterLevel + plantState.NutrientLevel + plantState.EnergyReserves) / 3f;

            if (averageResources > 0.7f) return PlantHealthStatus.Healthy;
            if (averageResources > 0.4f) return PlantHealthStatus.Fair;
            if (averageResources > 0.1f) return PlantHealthStatus.Poor;
            return PlantHealthStatus.Critical;
        }

        /// <summary>
        /// Get resource recommendations
        /// </summary>
        public static string GetResourceRecommendation(PlantStateData plantState)
        {
            if (plantState == null) return "Plant data unavailable";

            var recommendations = new System.Collections.Generic.List<string>();

            if (NeedsWatering(plantState))
                recommendations.Add("Water the plant");

            if (NeedsFeeding(plantState))
                recommendations.Add("Feed the plant nutrients");

            if (plantState.EnergyReserves < 0.3f)
                recommendations.Add("Address resource deficiencies");

            if (recommendations.Count == 0)
                return "Plant resources are adequate";

            return string.Join(", ", recommendations);
        }
    }

    /// <summary>
    /// Plant health status enum
    /// </summary>
    public enum PlantHealthStatus
    {
        Dead,
        Critical,
        Poor,
        Fair,
        Healthy
    }
}
