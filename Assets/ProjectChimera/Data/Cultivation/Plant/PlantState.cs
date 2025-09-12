using UnityEngine;
using System;
using ProjectChimera.Data.Shared;
using ProjectChimera.Data.Genetics;

namespace ProjectChimera.Data.Cultivation.Plant
{
    /// <summary>
    /// SIMPLE: Basic plant state management aligned with Project Chimera's cultivation vision.
    /// Focuses on essential plant information for basic cultivation mechanics.
    /// </summary>
    public static class PlantState
    {
        /// <summary>
        /// Create a basic plant state
        /// </summary>
        public static PlantStateData CreatePlantState(
            string plantId,
            string plantName,
            Vector3 worldPosition)
        {
            return new PlantStateData
            {
                // Basic identity
                PlantID = plantId,
                PlantName = plantName,
                WorldPosition = worldPosition,

                // Basic state
                CurrentGrowthStage = PlantGrowthStage.Seedling,
                AgeInDays = 0f,
                DaysInCurrentStage = 0f,

                // Basic physical
                CurrentHeight = 5f, // cm
                CurrentWidth = 2f,

                // Basic health
                Health = 1f, // 0-1 scale

                // Basic resources
                WaterLevel = 0.8f,
                NutrientLevel = 0.7f,

                // Basic growth
                DailyGrowthRate = 0f,

                // Basic timestamps
                PlantedDate = DateTime.Now,
                LastWatering = DateTime.Now,
                LastFeeding = DateTime.Now.AddDays(-1)
            };
        }

        /// <summary>
        /// Update plant state for daily growth
        /// </summary>
        public static void UpdateDailyGrowth(PlantStateData plantState, float growthAmount)
        {
            if (plantState == null) return;

            plantState.CurrentHeight += growthAmount;
            plantState.AgeInDays += 1f;
            plantState.DaysInCurrentStage += 1f;
            plantState.DailyGrowthRate = growthAmount;
        }

        /// <summary>
        /// Update plant health
        /// </summary>
        public static void UpdateHealth(PlantStateData plantState, float healthChange)
        {
            if (plantState == null) return;

            plantState.Health = Mathf.Clamp01(plantState.Health + healthChange);
        }

        /// <summary>
        /// Water the plant
        /// </summary>
        public static void WaterPlant(PlantStateData plantState, float waterAmount)
        {
            if (plantState == null) return;

            plantState.WaterLevel = Mathf.Clamp01(plantState.WaterLevel + waterAmount);
            plantState.LastWatering = DateTime.Now;
        }

        /// <summary>
        /// Feed the plant nutrients
        /// </summary>
        public static void FeedPlant(PlantStateData plantState, float nutrientAmount)
        {
            if (plantState == null) return;

            plantState.NutrientLevel = Mathf.Clamp01(plantState.NutrientLevel + nutrientAmount);
            plantState.LastFeeding = DateTime.Now;
        }

        /// <summary>
        /// Check if plant needs watering
        /// </summary>
        public static bool NeedsWatering(PlantStateData plantState)
        {
            if (plantState == null) return false;
            return plantState.WaterLevel < 0.3f;
        }

        /// <summary>
        /// Check if plant needs feeding
        /// </summary>
        public static bool NeedsFeeding(PlantStateData plantState)
        {
            if (plantState == null) return false;
            return plantState.NutrientLevel < 0.3f;
        }

        /// <summary>
        /// Check if plant is healthy
        /// </summary>
        public static bool IsHealthy(PlantStateData plantState)
        {
            if (plantState == null) return false;
            return plantState.Health > 0.6f;
        }

        /// <summary>
        /// Get plant age in days
        /// </summary>
        public static int GetAgeInDays(PlantStateData plantState)
        {
            if (plantState == null) return 0;
            return (int)plantState.AgeInDays;
        }

        /// <summary>
        /// Get days since last watering
        /// </summary>
        public static int GetDaysSinceWatering(PlantStateData plantState)
        {
            if (plantState == null) return 0;
            return (int)(DateTime.Now - plantState.LastWatering).TotalDays;
        }

        /// <summary>
        /// Get days since last feeding
        /// </summary>
        public static int GetDaysSinceFeeding(PlantStateData plantState)
        {
            if (plantState == null) return 0;
            return (int)(DateTime.Now - plantState.LastFeeding).TotalDays;
        }

        /// <summary>
        /// Validate basic plant state
        /// </summary>
        public static bool ValidatePlantState(PlantStateData plantState)
        {
            if (plantState == null) return false;
            if (string.IsNullOrEmpty(plantState.PlantID)) return false;
            if (plantState.Health < 0f || plantState.Health > 1f) return false;
            if (plantState.WaterLevel < 0f || plantState.WaterLevel > 1f) return false;
            if (plantState.NutrientLevel < 0f || plantState.NutrientLevel > 1f) return false;
            return true;
        }

        /// <summary>
        /// Get plant summary
        /// </summary>
        public static PlantSummary GetPlantSummary(PlantStateData plantState)
        {
            if (plantState == null) return null;

            return new PlantSummary
            {
                PlantID = plantState.PlantID,
                PlantName = plantState.PlantName,
                CurrentStage = plantState.CurrentGrowthStage,
                AgeInDays = (int)plantState.AgeInDays,
                Height = plantState.CurrentHeight,
                Health = plantState.Health,
                WaterLevel = plantState.WaterLevel,
                NutrientLevel = plantState.NutrientLevel,
                NeedsWatering = NeedsWatering(plantState),
                NeedsFeeding = NeedsFeeding(plantState),
                IsHealthy = IsHealthy(plantState)
            };
        }
    }

    /// <summary>
    /// Basic plant state data
    /// </summary>
    [System.Serializable]
    public class PlantStateData
    {
        // Basic identity
        public string PlantID;
        public string PlantName;
        public Vector3 WorldPosition;

        // Basic state
        public PlantGrowthStage CurrentGrowthStage;
        public float AgeInDays;
        public float DaysInCurrentStage;

        // Basic physical
        public float CurrentHeight; // cm
        public float CurrentWidth;  // cm

        // Basic health
        public float Health; // 0-1
        public PlantSex Sex; // Plant gender

        // Basic resources
        public float WaterLevel;   // 0-1
        public float NutrientLevel; // 0-1

        // Basic growth
        public float DailyGrowthRate;

        // Basic timestamps
        public DateTime PlantedDate;
        public DateTime LastWatering;
        public DateTime LastFeeding;
    }

    /// <summary>
    /// Plant summary
    /// </summary>
    [System.Serializable]
    public class PlantSummary
    {
        public string PlantID;
        public string PlantName;
        public PlantGrowthStage CurrentStage;
        public int AgeInDays;
        public float Height;
        public float Health;
        public float WaterLevel;
        public float NutrientLevel;
        public bool NeedsWatering;
        public bool NeedsFeeding;
        public bool IsHealthy;
    }

    /// <summary>
    /// Plant sex/gender enumeration
    /// </summary>
    public enum PlantSex
    {
        Unknown,
        Male,
        Female,
        Hermaphrodite
    }
}
