// REFACTORED: Yield Optimization Data Structures
// Extracted from YieldOptimizationManager for better separation of concerns

using System;

namespace ProjectChimera.Systems.Cultivation.Core
{
    /// <summary>
    /// Yield optimization data per plant
    /// </summary>
    public struct YieldOptimizationData
    {
        public string PlantId;
        public bool IsActive;
        public float CurrentQualityScore;
        public float PredictedYield;
        public float OptimalHarvestTime;
        public float LastOptimizationUpdate;
    }

    /// <summary>
    /// Yield optimization statistics
    /// </summary>
    [Serializable]
    public struct YieldOptimizationStats
    {
        public int ActiveOptimizations;
        public int OptimizationsProcessed;
        public int OptimizationErrors;
        public float AverageOptimizationTime;
        public float MaxOptimizationTime;
        public float LastOptimizationTime;
    }

    /// <summary>
    /// Yield prediction data
    /// </summary>
    [Serializable]
    public struct YieldPrediction
    {
        public string PlantId;
        public float BaseYield;
        public float PredictedYield;
        public float QualityScore;
        public float HealthModifier;
        public float GrowthModifier;
        public float EnvironmentalModifier;
        public float MaturityModifier;
        public QualityGrade QualityGrade;
    }

    /// <summary>
    /// Quality factors for yield calculation
    /// </summary>
    [Serializable]
    public struct QualityFactors
    {
        public float HealthFactor;
        public float GrowthFactor;
        public float EnvironmentalFactor;
        public float CareFactor;
        public float GeneticsFactor;
        public float OverallScore;
    }

    /// <summary>
    /// Quality grade enumeration
    /// </summary>
    public enum QualityGrade
    {
        Poor,
        Low,
        Medium,
        High,
        Premium
    }
}

