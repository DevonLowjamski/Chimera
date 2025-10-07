// REFACTORED: Data Structures
// Extracted from PlantGrowthProcessor.cs for better separation of concerns

using UnityEngine;
using ProjectChimera.Data.Shared;
using System;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Data.Cultivation.Plant
{
    public struct PlantGrowthStats
    {
        public int GrowthProcessingCycles;
        public int GrowthMeasurements;
        public int EnvironmentalUpdates;
        public int GeneticUpdates;
        public float TotalBiomassGained;
    }

    public struct GrowthMeasurement
    {
        public DateTime Timestamp;
        public float AgeInDays;
        public float GrowthProgress;
        public float TotalBiomass;
        public float Height;
        public float Width;
        public float HealthFactor;
        public float ResourceFactor;
        public float EnvironmentalFactor;
        public float GrowthModifier;
    }

    public struct GrowthComputationResult
    {
        public bool Success;
        public float BiomassGain;
        public float HeightGain;
        public float WidthGain;
        public float GrowthModifier;
        public PlantGrowthStage RecommendedStage;
        public float ProcessingTime;
    }

    public struct PlantGrowthSummary
    {
        public float CurrentProgress;
        public float TotalBiomass;
        public float CurrentHeight;
        public float CurrentWidth;
        public float CurrentLeafArea;
        public float DailyGrowthRate;
        public float BiomassAccumulation;
        public float RootDevelopmentRate;
        public float EnvironmentalFactor;
        public float GeneticVigorModifier;
        public PlantGrowthStage RecommendedStage;
        public DateTime LastGrowthUpdate;
        public int GrowthHistoryEntries;
    }

}
