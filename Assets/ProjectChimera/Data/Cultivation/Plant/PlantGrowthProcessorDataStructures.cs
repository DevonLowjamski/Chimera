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

        // Backward-compatible alias
        public int TotalGrowthCycles => GrowthProcessingCycles;
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
        public PlantGrowthStage GrowthStage;

        // Backward-compatible aliases
        public float Age { get => AgeInDays; set => AgeInDays = value; }
        public float Progress { get => GrowthProgress; set => GrowthProgress = value; }
        public PlantGrowthStage Stage { get => GrowthStage; set => GrowthStage = value; }
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
        public string Error;
        public float CurrentHeight;
        public float CurrentWidth;

        // Backward-compatible aliases
        public string ErrorMessage { get => Error; set => Error = value; }
        public float Height { get => CurrentHeight; set => CurrentHeight = value; }
        public float Width { get => CurrentWidth; set => CurrentWidth = value; }
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
        public float RootBiomass;
        public float LeafBiomass;
        public float StemBiomass;
        public PlantGrowthStats GrowthStats;

        // Backward-compatible aliases
        public float LeafArea { get => CurrentLeafArea; set => CurrentLeafArea = value; }
        public float RootMass { get => RootBiomass; set => RootBiomass = value; }
        public float LeafMass { get => LeafBiomass; set => LeafBiomass = value; }
        public float StemMass { get => StemBiomass; set => StemBiomass = value; }
        public DateTime LastUpdate { get => LastGrowthUpdate; set => LastGrowthUpdate = value; }
        public int TotalMeasurements { get => GrowthHistoryEntries; set => GrowthHistoryEntries = value; }
        public PlantGrowthStats Stats { get => GrowthStats; set => GrowthStats = value; }
    }

}
