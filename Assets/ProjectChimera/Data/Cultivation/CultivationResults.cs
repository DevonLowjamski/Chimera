using System;
using UnityEngine;

namespace ProjectChimera.Data.Cultivation
{
    /// <summary>
    /// Result of a watering operation
    /// </summary>
    [System.Serializable]
    public class WateringResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public float WaterAmountApplied { get; set; }
        public float SoilMoistureAfter { get; set; }
        public DateTime Timestamp { get; set; }

        public WateringResult()
        {
            Timestamp = DateTime.Now;
        }

        public static WateringResult CreateSuccess(float waterAmount, float soilMoisture)
        {
            return new WateringResult
            {
                Success = true,
                Message = "Watering completed successfully",
                WaterAmountApplied = waterAmount,
                SoilMoistureAfter = soilMoisture
            };
        }

        public static WateringResult CreateFailure(string reason)
        {
            return new WateringResult
            {
                Success = false,
                Message = reason,
                WaterAmountApplied = 0f,
                SoilMoistureAfter = 0f
            };
        }
    }

    /// <summary>
    /// Result of a feeding operation
    /// </summary>
    [System.Serializable]
    public class FeedingResult
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public float NutrientAmountApplied { get; set; }
        public float NutrientLevelAfter { get; set; }
        public string NutrientType { get; set; }
        public DateTime Timestamp { get; set; }

        public FeedingResult()
        {
            Timestamp = DateTime.Now;
        }

        public static FeedingResult CreateSuccess(string nutrientType, float nutrientAmount, float nutrientLevel)
        {
            return new FeedingResult
            {
                Success = true,
                Message = "Feeding completed successfully",
                NutrientType = nutrientType,
                NutrientAmountApplied = nutrientAmount,
                NutrientLevelAfter = nutrientLevel
            };
        }

        public static FeedingResult CreateFailure(string reason)
        {
            return new FeedingResult
            {
                Success = false,
                Message = reason,
                NutrientAmountApplied = 0f,
                NutrientLevelAfter = 0f
            };
        }
    }

    /// <summary>
    /// Watering schedule configuration
    /// </summary>
    [System.Serializable]
    public class WateringSchedule
    {
        public bool IsEnabled { get; set; } = true;
        public float IntervalHours { get; set; } = 24f;
        public float FrequencyHours { get; set; } = 24f; // Alias for IntervalHours
        public float WaterAmount { get; set; } = 1f;
        public float AmountPerPlant { get; set; } = 1f; // Alias for WaterAmount
        public float MinSoilMoisture { get; set; } = 0.3f;
        public DateTime NextWateringTime { get; set; }
        public DateTime LastWateringTime { get; set; }
        public string ScheduleName { get; set; } = "Default Watering";

        public WateringSchedule()
        {
            NextWateringTime = DateTime.Now.AddHours(IntervalHours);
        }

        public bool IsTimeToWater()
        {
            return IsEnabled && DateTime.Now >= NextWateringTime;
        }

        public void UpdateNextWateringTime()
        {
            NextWateringTime = DateTime.Now.AddHours(IntervalHours);
        }
    }

    /// <summary>
    /// Feeding schedule configuration
    /// </summary>
    [System.Serializable]
    public class FeedingSchedule
    {
        public bool IsEnabled { get; set; } = true;
        public float IntervalHours { get; set; } = 72f; // 3 days default
        public float FrequencyHours { get; set; } = 72f; // Alias for IntervalHours
        public float NutrientAmount { get; set; } = 0.5f;
        public string NutrientType { get; set; } = "General";
        public float MinNutrientLevel { get; set; } = 0.2f;
        public DateTime NextFeedingTime { get; set; }
        public DateTime LastNutrientTime { get; set; }
        public object CurrentMix { get; set; } // Nutrient mix configuration
        public string ScheduleName { get; set; } = "Default Feeding";

        public FeedingSchedule()
        {
            NextFeedingTime = DateTime.Now.AddHours(IntervalHours);
        }

        public bool IsTimeToFeed()
        {
            return IsEnabled && DateTime.Now >= NextFeedingTime;
        }

        public void UpdateNextFeedingTime()
        {
            NextFeedingTime = DateTime.Now.AddHours(IntervalHours);
        }
    }
}
