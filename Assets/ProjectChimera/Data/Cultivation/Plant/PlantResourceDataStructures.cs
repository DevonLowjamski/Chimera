using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectChimera.Data.Cultivation.Plant
{
    /// <summary>
    /// REFACTORED: Plant Resource Data Structures
    /// Single Responsibility: Data structures for plant resource management
    /// Extracted from PlantResourceHandler for better separation of concerns
    /// </summary>

    /// <summary>
    /// Plant resource statistics
    /// </summary>
    [System.Serializable]
    public struct PlantResourceStats
    {
        public int WateringEvents;
        public int FeedingEvents;
        public int TrainingEvents;
        public int ResourceUpdates;
        public float TotalWaterApplied;
        public float TotalNutrientsApplied;
    }

    /// <summary>
    /// Plant resource summary
    /// </summary>
    [System.Serializable]
    public struct PlantResourceSummary
    {
        public float WaterLevel;
        public float NutrientLevel;
        public float EnergyReserves;
        public float OverallResourceStatus;
        public bool HasCriticalWater;
        public bool HasCriticalNutrients;
        public bool HasCriticalEnergy;
        public DateTime LastWatering;
        public DateTime LastFeeding;
        public DateTime LastTraining;
        public Dictionary<string, float> NutrientBreakdown;
        public float NextWateringRecommendation;
        public float NextFeedingRecommendation;
    }

    /// <summary>
    /// Watering schedule
    /// </summary>
    [System.Serializable]
    public struct WateringSchedule
    {
        public float FrequencyInDays;
        public float AmountPerWatering;
        public string TimeOfDay;
    }

    /// <summary>
    /// Feeding schedule
    /// </summary>
    [System.Serializable]
    public struct FeedingSchedule
    {
        public float FrequencyInDays;
        public Dictionary<string, float> NutrientMix;
    }

    /// <summary>
    /// Watering result
    /// </summary>
    [System.Serializable]
    public struct WateringResult
    {
        public float WaterAmount;
        public float PreviousLevel;
        public float NewLevel;
        public bool Success;
        public DateTime Timestamp;
        public string Message;
    }

    /// <summary>
    /// Feeding result
    /// </summary>
    [System.Serializable]
    public struct FeedingResult
    {
        public Dictionary<string, float> AppliedNutrients;
        public float TotalNutrientValue;
        public float PreviousLevel;
        public float NewLevel;
        public bool Success;
        public DateTime Timestamp;
        public string Message;
    }

    /// <summary>
    /// Training result
    /// </summary>
    [System.Serializable]
    public struct TrainingResult
    {
        public string TrainingType;
        public bool Success;
        public float StressIncrease;
        public DateTime TrainingDate;
        public float EnergyCost;
    }

    /// <summary>
    /// Training outcome (alias for backward compatibility)
    /// </summary>
    [System.Serializable]
    public struct TrainingOutcome
    {
        public string TrainingType;
        public bool Success;
        public float StressIncrease;
        public DateTime TrainingDate;
        public float EnergyCost;
    }
}

// Extension methods for easier calculation
public static class DictionaryExtensions
{
    public static float Sum(this IEnumerable<float> values)
    {
        float sum = 0f;
        foreach (var value in values)
        {
            sum += value;
        }
        return sum;
    }
}

