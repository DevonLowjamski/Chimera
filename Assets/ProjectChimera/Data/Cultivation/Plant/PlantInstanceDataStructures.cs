// REFACTORED: Plant Instance Data Structures
// Extracted from PlantInstanceSO for better separation of concerns

using System;

namespace ProjectChimera.Data.Cultivation.Plant
{
    /// <summary>
    /// Yield calculation result
    /// </summary>
    [System.Serializable]
    public class YieldCalculation
    {
        public float EstimatedYield;
        public float YieldConfidence;
        public DateTime CalculationDate;
    }

    /// <summary>
    /// Harvest readiness recommendation
    /// </summary>
    [System.Serializable]
    public class HarvestRecommendationSimple
    {
        public bool IsReady;
        public DateTime OptimalHarvestDate;
        public string RecommendationReason;
    }

    /// <summary>
    /// Post-harvest processing parameters
    /// </summary>
    [System.Serializable]
    public class PostHarvestProcessSimple
    {
        public string ProcessType;
        public float Duration;
        public float Temperature;
        public float Humidity;
    }

    /// <summary>
    /// Result of watering operation
    /// </summary>
    [System.Serializable]
    public class WateringResultSimple
    {
        public float WaterAmount;
        public bool Success;
        public DateTime Timestamp;
        public string Message;
    }

    /// <summary>
    /// Result of feeding operation
    /// </summary>
    [System.Serializable]
    public class FeedingResultSimple
    {
        public float NutrientAmount;
        public bool Success;
        public DateTime Timestamp;
        public string Message;
    }
}

