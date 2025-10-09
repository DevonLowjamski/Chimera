// REFACTORED: Seasonal System Data Structures
// Extracted from SeasonalSystem for better separation of concerns

using System;
using UnityEngine;

namespace ProjectChimera.Systems.Services.SpeedTree.Environmental
{
    /// <summary>
    /// Seasonal visual and environmental effects profile
    /// </summary>
    [Serializable]
    public struct SeasonalEffectProfile
    {
        public Season SeasonType; // The season this profile represents
        public Color Tint;
        public float Brightness;
        public float Contrast;
        public float GrowthMultiplier;
        public float StressMultiplier;
        public float TemperatureModifier;
        public float HumidityModifier;
        public float LightIntensityModifier;
        public string Description;

        // Backward-compatible aliases
        public Season Season { get => SeasonType; set => SeasonType = value; }
        public Color ColorTint { get => Tint; set => Tint = value; }
        public float HealthMultiplier { get => StressMultiplier; set => StressMultiplier = value; }
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
        public float LastSeasonUpdate; // Time.time of last update
        public float AccumulatedEffects; // Cumulative seasonal effects
        public DateTime LastChange; // DateTime of last season change

        public PlantSeasonalData(int plantId)
        {
            PlantId = plantId;
            SeasonalAdaptation = 0.5f; // Start at 50% adaptation
            LastSeasonUpdate = Time.time;
            AccumulatedEffects = 0f;
            LastChange = DateTime.Now;
        }

        // Parameterless constructor for compatibility
        public PlantSeasonalData()
        {
            PlantId = -1;
            SeasonalAdaptation = 0.5f;
            LastSeasonUpdate = Time.time;
            AccumulatedEffects = 0f;
            LastChange = DateTime.Now;
        }

        // Backward-compatible aliases
        public float AccumulatedSeasonalEffects { get => AccumulatedEffects; set => AccumulatedEffects = value; }
        public DateTime LastSeasonChange { get => LastChange; set => LastChange = value; }
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
        public float SeasonTimer;
        public DateTime LastUpdate;
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
        public float GrowthMultiplier;
        public float HealthMultiplier;
        public bool IsTransitioning;
    }
}

