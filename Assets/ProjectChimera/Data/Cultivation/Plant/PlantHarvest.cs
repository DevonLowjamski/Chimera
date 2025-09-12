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

        /// <summary>
        /// Determines if plant is ready for harvest
        /// </summary>
        public static HarvestReadiness AssessHarvestReadiness(PlantStateData plantState)
        {
            // Delegate to HarvestReadinessCalculator
            return HarvestReadinessCalculator.AssessHarvestReadiness(plantState);
        }

        /// <summary>
        /// Calculates yield prediction for a plant
        /// </summary>
        public static YieldPrediction CalculateYieldPrediction(PlantStateData plantState)
        {
            // Delegate to YieldCalculator
            return YieldCalculator.CalculateYieldPrediction(plantState);
        }

        /// <summary>
        /// Calculates harvest quality based on plant state
        /// </summary>
        public static HarvestQuality CalculateHarvestQuality(PlantStateData plantState)
        {
            // Delegate to QualityAssessor
            return QualityAssessor.CalculateHarvestQuality(plantState);
        }

        /// <summary>
        /// Calculates harvest window based on plant state
        /// </summary>
        public static HarvestWindow CalculateHarvestWindow(PlantStateData plantState)
        {
            // Delegate to HarvestReadinessCalculator
            return HarvestReadinessCalculator.CalculateHarvestWindow(plantState);
        }
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
        public float OverallQualityScore => OverallQuality;
        public QualityGrade QualityGradeEnum => QualityGrade == "Excellent" ? QualityGrade.Excellent :
                                                QualityGrade == "Good" ? QualityGrade.Good :
                                                QualityGrade == "Fair" ? QualityGrade.Fair : QualityGrade.Poor;
        public QualityFactors QualityFactors { get; set; } = new QualityFactors();
        public string QualityDescription => $"Quality: {QualityGrade}";
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
    /// Quality metrics data structure
    /// </summary>
    [System.Serializable]
    public class QualityMetrics
    {
        public float OverallScore;
        public float VisualAppeal;
        public float AromaScore;
        public float StructureScore;
        public DateTime EvaluationDate;
    }

    /// <summary>
    /// Quality comparison data structure
    /// </summary>
    [System.Serializable]
    public class QualityComparison
    {
        public HarvestQuality BaselineQuality;
        public HarvestQuality CurrentQuality;
        public float ImprovementScore;
        public string ComparisonNotes;
    }

    /// <summary>
    /// Quality grade enumeration
    /// </summary>
    public enum QualityGrade
    {
        Poor,
        Fair,
        Good,
        Excellent,
        Premium
    }

    // HarvestReadiness and HarvestWindow are defined in HarvestReadinessCalculator.cs to avoid duplication
}
