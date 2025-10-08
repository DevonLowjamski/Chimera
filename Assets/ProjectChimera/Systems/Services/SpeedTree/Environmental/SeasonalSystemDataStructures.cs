// REFACTORED: Seasonal System Data Structures
// Extracted from SeasonalSystem for better separation of concerns

using System;
using UnityEngine;
using ProjectChimera.Data.Cultivation;

namespace ProjectChimera.Systems.Services.SpeedTree.Environmental
{
    /// <summary>
    /// Seasonal visual and environmental effects profile
    /// </summary>
    [Serializable]
    public struct SeasonalEffectProfile
    {
        public Color Tint;
        public float Brightness;
        public float Contrast;
        public float GrowthMultiplier;
        public float StressMultiplier;
        public float TemperatureModifier;
        public float HumidityModifier;
        public float LightIntensityModifier;
        public string Description;
    }

    /// <summary>
    /// Plant seasonal data tracking
    /// </summary>
    [Serializable]
    public class PlantSeasonalData
    {
        public int PlantId;
        public Season CurrentSeason;
        public float SeasonalAdaptation; // 0-1, how well adapted to current season
        public float LastSeasonUpdate;

        public PlantSeasonalData(int plantId)
        {
            PlantId = plantId;
            SeasonalAdaptation = 0.5f; // Start at 50% adaptation
            LastSeasonUpdate = Time.time;
        }
    }

    /// <summary>
    /// Seasonal system performance statistics
    /// </summary>
    [Serializable]
    public struct SeasonalStatistics
    {
        public Season CurrentSeason;
        public float TransitionProgress;
        public int RegisteredPlants;
        public bool SeasonalChangesEnabled;
        public float TransitionDuration;
        public float UpdateFrequency;
    }

    /// <summary>
    /// Seasonal conditions for environmental calculations
    /// </summary>
    [Serializable]
    public struct SeasonalConditions
    {
        public Season Season;
        public float TemperatureModifier;
        public float HumidityModifier;
        public float LightIntensityModifier;
        public float GrowthRateModifier;
        public float TransitionProgress;
    }
}

