// REFACTORED: Stress Visualization Data Structures
// Extracted from StressVisualizationSystem for better separation of concerns

using System;
using UnityEngine;

namespace ProjectChimera.Systems.Services.SpeedTree.Environmental
{
    /// <summary>
    /// Plant-specific stress visualization data
    /// </summary>
    [Serializable]
    public class PlantStressVisualization
    {
        public int PlantId;
        public float HealthLevel; // 0-1 (0 = dead, 1 = perfect health)
        public float StressLevel; // 0-1 (0 = no stress, 1 = maximum stress)
        public float LastUpdateTime;

        public PlantStressVisualization(int plantId)
        {
            PlantId = plantId;
            HealthLevel = 1f; // Start healthy
            StressLevel = 0f; // Start with no stress
            LastUpdateTime = Time.time;
        }
    }

    /// <summary>
    /// Plant stress levels data structure
    /// </summary>
    [Serializable]
    public struct PlantStressLevels
    {
        public float HealthLevel;
        public float StressLevel;
    }

    /// <summary>
    /// Environmental stress indicators for comprehensive plant stress analysis
    /// </summary>
    [Serializable]
    public class EnvironmentalStressIndicators
    {
        public float TemperatureStress; // 0-1
        public float HumidityStress;    // 0-1
        public float LightStress;       // 0-1
        public float NutrientStress;    // 0-1
        public float PestStress;        // 0-1
        public float DiseaseStress;     // 0-1

        public static EnvironmentalStressIndicators Default => new EnvironmentalStressIndicators
        {
            TemperatureStress = 0f,
            HumidityStress = 0f,
            LightStress = 0f,
            NutrientStress = 0f,
            PestStress = 0f,
            DiseaseStress = 0f
        };
    }
}

