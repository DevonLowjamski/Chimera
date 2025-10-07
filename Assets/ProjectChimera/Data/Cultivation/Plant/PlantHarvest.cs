using UnityEngine;
using System;


namespace ProjectChimera.Data.Cultivation.Plant
{
    /// <summary>
    /// REFACTORED: Plant harvesting system decomposed into focused components.
    /// This file now serves as a coordinator for specialized harvest components.
    ///
    /// New Component Structure:
    /// - HarvestReadinessCalculator.cs: Determines optimal harvest timing
    /// - YieldCalculator.cs: Calculates harvest yields and predictions
    /// - QualityAssessor.cs: Assesses harvest quality and characteristics
    /// - HarvestProcessor.cs: Handles harvest execution and processing
    /// - CuringAdvisor.cs: Provides post-harvest curing recommendations
    /// - HarvestValuer.cs: Calculates economic value of harvests
    /// - HarvestReporter.cs: Generates harvest reports and analytics
    /// </summary>
    public static class PlantHarvest
    {
        // Harvest quality thresholds
        private const float PREMIUM_QUALITY_THRESHOLD = 0.9f;
        private const float GOOD_QUALITY_THRESHOLD = 0.75f;
        private const float FAIR_QUALITY_THRESHOLD = 0.6f;

        // Yield calculation factors
        private const float GENETIC_YIELD_FACTOR = 0.7f;
        private const float ENVIRONMENTAL_YIELD_FACTOR = 0.2f;
        private const float CULTIVATION_YIELD_FACTOR = 0.1f;

        // Note: Harvest calculations are now performed by specialized component classes:
        // - HarvestReadinessCalculator: Readiness and window calculations
        // - YieldCalculator: Yield predictions
        // - QualityAssessor: Quality assessments
        // These are instantiated and used by PlantHarvestOperator
    }

    /// <summary>
    /// Yield prediction data structure
    /// </summary>
    [System.Serializable]
    public class YieldPrediction
    {
        public string PlantID;
        public float PredictedWetYield;
        public float PredictedDryYield;
        public float YieldConfidence;
        public string YieldQuality;
        public DateTime PredictionDate;

        // Additional properties for compatibility
        public float PredictedYieldGrams => PredictedDryYield;
        public float ConfidenceLevel => YieldConfidence;
        public YieldFactors Factors { get; set; } = new YieldFactors();
        public YieldBreakdown YieldBreakdown { get; set; } = new YieldBreakdown();
    }

    /// <summary>
    /// Yield factors data structure
    /// </summary>
    [System.Serializable]
    public class YieldFactors
    {
        public float GeneticFactor;
        public float EnvironmentalFactor;
        public float CultivationFactor;
        public float NutrientFactor;
        public float WaterFactor;
        public float PestFactor;
        public float TrainingFactor;
    }

    /// <summary>
    /// Yield breakdown data structure
    /// </summary>
    [System.Serializable]
    public class YieldBreakdown
    {
        public float FlowerYield;
        public float LeafYield;
        public float StemYield;
        public float RootYield;
    }

    /// <summary>
    /// Harvest result data structure
    /// </summary>
    [System.Serializable]
    public class HarvestResult
    {
        public string PlantID;
        public float TotalWeightGrams;
        public float QualityScore;
        public float MoistureContent;
        public DateTime HarvestDate;
        public string HarvestNotes;
    }

    /// <summary>
    /// Harvest quality data structure
    /// </summary>
    [System.Serializable]
    public class HarvestQuality
    {
        public string PlantID;
        public float OverallQuality;
        public float TrichomeDevelopment;
        public float PistilBrowning;
        public float AromaIntensity;
        public float DensityScore;
        public string QualityGrade;
        public DateTime AssessmentDate;

        // Additional properties for compatibility
        public float OverallQualityScore { get; set; } = 0f;
        public HarvestQualityGrade QualityGradeEnum
        {
            get
            {
                switch (QualityGrade)
                {
                    case "Excellent": return HarvestQualityGrade.Excellent;
                    case "Good": return HarvestQualityGrade.Good;
                    case "Fair": return HarvestQualityGrade.Fair;
                    case "Poor": return HarvestQualityGrade.Poor;
                    default: return HarvestQualityGrade.Poor;
                }
            }
        }
        public QualityFactors QualityFactors { get; set; } = new QualityFactors();
        public string QualityDescription { get; set; } = "Unknown";
        public QualityMetrics QualityMetrics { get; set; } = new QualityMetrics();
    }

    /// <summary>
    /// Quality factors data structure
    /// </summary>
    [System.Serializable]
    public class QualityFactors
    {
        public float TrichomeQuality;
        public float CannabinoidProfile;
        public float TerpeneProfile;
        public float Appearance;
        public float Aroma;
    }

    /// <summary>
    /// Quality metrics data structure
    /// </summary>
    [System.Serializable]
    public class QualityMetrics
    {
        public float EstimatedTHC;
        public float EstimatedCBD;
        public float EstimatedTerpeneContent;
        public string MarketGrade;
        public float ConsistencyScore;
    }



    /// <summary>
    /// Quality comparison data structure
    /// </summary>
    [System.Serializable]
    public class QualityComparison
    {
        public HarvestQuality Quality1;
        public HarvestQuality Quality2;
        public float ScoreDifference;
        public int GradeDifference;
        public string ImprovementDirection;
        public UnityEngine.Color ImprovementColor;

        // Backward compatibility properties
        public HarvestQuality BaselineQuality => Quality1;
        public HarvestQuality CurrentQuality => Quality2;
        public float ImprovementScore => ScoreDifference;
        public string ComparisonNotes => $"{ImprovementDirection} - Score difference: {ScoreDifference}";
    }

    /// <summary>
    /// Quality grade enumeration
    /// </summary>
    public enum HarvestQualityGrade
    {
        Poor,
        Fair,
        Good,
        Excellent,
        Premium
    }

    // HarvestReadiness and HarvestWindow are defined in HarvestReadinessCalculator.cs to avoid duplication
}
