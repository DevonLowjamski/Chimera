// REFACTORED: Seasonal Effects Manager
// Extracted from SeasonalSystem for better separation of concerns

using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectChimera.Systems.Services.SpeedTree.Environmental
{
    /// <summary>
    /// Manages seasonal effect profiles and their application
    /// </summary>
    public class SeasonalEffectsManager
    {
        private readonly Dictionary<Season, SeasonalEffectProfile> _seasonalEffects;
        private readonly Dictionary<int, PlantSeasonalData> _plantSeasonalData;

        public SeasonalEffectsManager()
        {
            _seasonalEffects = new Dictionary<Season, SeasonalEffectProfile>();
            _plantSeasonalData = new Dictionary<int, PlantSeasonalData>();
            InitializeSeasonalEffects();
        }

        private void InitializeSeasonalEffects()
        {
            // Spring effects - growth and renewal
            _seasonalEffects[Season.Spring] = new SeasonalEffectProfile
            {
                Season = Season.Spring,
                GrowthMultiplier = 1.2f,
                HealthMultiplier = 1.1f,
                ColorTint = new Color(0.8f, 1.0f, 0.8f, 1f),
                Brightness = 1.1f,
                Contrast = 1.0f,
                Description = "Spring - Enhanced growth and vitality"
            };

            // Summer effects - peak growth
            _seasonalEffects[Season.Summer] = new SeasonalEffectProfile
            {
                Season = Season.Summer,
                GrowthMultiplier = 1.0f,
                HealthMultiplier = 1.0f,
                ColorTint = new Color(1.0f, 1.0f, 0.9f, 1f),
                Brightness = 1.2f,
                Contrast = 1.1f,
                Description = "Summer - Optimal growing conditions"
            };

            // Autumn effects - preparation for dormancy
            _seasonalEffects[Season.Autumn] = new SeasonalEffectProfile
            {
                Season = Season.Autumn,
                GrowthMultiplier = 0.8f,
                HealthMultiplier = 0.9f,
                ColorTint = new Color(1.0f, 0.85f, 0.6f, 1f),
                Brightness = 0.9f,
                Contrast = 1.2f,
                Description = "Autumn - Slowing growth, changing colors"
            };

            // Winter effects - dormancy
            _seasonalEffects[Season.Winter] = new SeasonalEffectProfile
            {
                Season = Season.Winter,
                GrowthMultiplier = 0.5f,
                HealthMultiplier = 0.8f,
                ColorTint = new Color(0.8f, 0.9f, 1.0f, 1f),
                Brightness = 0.8f,
                Contrast = 0.9f,
                Description = "Winter - Reduced growth, stress resilience"
            };
        }

        public SeasonalEffectProfile? GetSeasonalEffect(Season season)
        {
            return _seasonalEffects.TryGetValue(season, out var profile) ? profile : (SeasonalEffectProfile?)null;
        }

        public void RegisterPlant(int plantId, Season season)
        {
            if (!_plantSeasonalData.ContainsKey(plantId))
            {
                _plantSeasonalData[plantId] = new PlantSeasonalData
                {
                    PlantId = plantId,
                    CurrentSeason = season,
                    AccumulatedSeasonalEffects = 0f,
                    LastSeasonChange = DateTime.Now
                };
            }
        }

        public void UnregisterPlant(int plantId)
        {
            _plantSeasonalData.Remove(plantId);
        }

        public PlantSeasonalData GetPlantSeasonalData(int plantId)
        {
            return _plantSeasonalData.TryGetValue(plantId, out var data) ? data : null;
        }

        public void UpdatePlantSeason(int plantId, Season newSeason, float effectStrength)
        {
            if (_plantSeasonalData.TryGetValue(plantId, out var data))
            {
                data.CurrentSeason = newSeason;
                data.AccumulatedSeasonalEffects += effectStrength;
                data.LastSeasonChange = DateTime.Now;
            }
        }

        public void Clear()
        {
            _plantSeasonalData.Clear();
        }

        public int PlantCount => _plantSeasonalData.Count;
    }
}

