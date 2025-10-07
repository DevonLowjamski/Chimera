using System;
using System.Collections.Generic;
using ProjectChimera.Data.Shared;

namespace ProjectChimera.Data.Cultivation.Plant
{
    /// <summary>
    /// PHASE 0 REFACTORED: Harvest Data Structures
    /// Single Responsibility: Define all harvest-related data types
    /// Extracted from PlantHarvestOperator (785 lines → 4 files <500 lines each)
    /// </summary>

    /// <summary>
    /// Post-harvest processing method
    /// </summary>
    public enum PostHarvestMethod
    {
        QuickDry,
        StandardDrying,
        SlowDry,
        FreezeProcessing,
        WetTrimming,
        DryTrimming
    }

    /// <summary>
    /// Plant harvest statistics
    /// </summary>
    [Serializable]
    public struct PlantHarvestStats
    {
        public int ReadinessChecks;
        public int YieldCalculations;
        public int PotencyCalculations;
        public int HarvestAttempts;
        public int HarvestsCompleted;

        /// <summary>
        /// Create empty statistics
        /// </summary>
        public static PlantHarvestStats CreateEmpty()
        {
            return new PlantHarvestStats
            {
                ReadinessChecks = 0,
                YieldCalculations = 0,
                PotencyCalculations = 0,
                HarvestAttempts = 0,
                HarvestsCompleted = 0
            };
        }
    }

    /// <summary>
    /// Harvest readiness result
    /// </summary>
    [Serializable]
    public struct HarvestReadinessResult
    {
        public float OverallReadiness;
        public bool IsReady;
        public Dictionary<string, float> ReadinessFactors;
        public float EstimatedYield;
        public float EstimatedPotency;
        public DateTime OptimalHarvestDate;
        public float DaysUntilOptimal;
        public float CalculationTime;

        /// <summary>
        /// Create not ready result
        /// </summary>
        public static HarvestReadinessResult CreateNotReady(float readiness, string reason)
        {
            return new HarvestReadinessResult
            {
                OverallReadiness = readiness,
                IsReady = false,
                ReadinessFactors = new Dictionary<string, float>(),
                EstimatedYield = 0f,
                EstimatedPotency = 0f,
                OptimalHarvestDate = DateTime.Now.AddDays(7),
                DaysUntilOptimal = 7f,
                CalculationTime = 0f
            };
        }
    }

    /// <summary>
    /// Harvest readiness assessment
    /// </summary>
    [Serializable]
    public struct HarvestReadinessDetails
    {
        public bool IsReadyForHarvest;
        public float ReadinessScore;
        public string ReadinessReason;
        public DateTime OptimalHarvestDate;
        public bool IsInOptimalWindow;
        public float EstimatedYield;
        public float EstimatedPotency;
    }

    /// <summary>
    /// Harvest recommendation
    /// </summary>
    [Serializable]
    public struct HarvestRecommendation
    {
        public float ReadinessScore;
        public bool IsReady;
        public DateTime OptimalHarvestDate;
        public DateTime HarvestWindowStart;
        public DateTime HarvestWindowEnd;
        public float EstimatedYield;
        public float EstimatedPotency;
        public string RecommendedMethod;
        public HarvestQualityGrade QualityPrediction;
        public bool IsInOptimalWindow;
        public float DaysUntilOptimal;
        public string RecommendationReason;

        /// <summary>
        /// Create not ready recommendation
        /// </summary>
        public static HarvestRecommendation CreateNotReady()
        {
            return new HarvestRecommendation
            {
                ReadinessScore = 0f,
                IsReady = false,
                OptimalHarvestDate = DateTime.Now.AddDays(30),
                EstimatedYield = 0f,
                EstimatedPotency = 0f,
                IsInOptimalWindow = false
            };
        }
    }

    /// <summary>
    /// Harvest recommendation details
    /// </summary>
    [Serializable]
    public struct HarvestRecommendationDetails
    {
        public bool IsReady;
        public DateTime OptimalHarvestDate;
        public float DaysUntilOptimal;
        public string RecommendationReason;
        public float EstimatedYield;
        public float EstimatedPotency;
        public string RecommendedMethod;
        public HarvestQuality QualityPrediction;
    }

    /// <summary>
    /// Post-harvest processing recommendations
    /// </summary>
    [Serializable]
    public struct PostHarvestProcessDetails
    {
        public string ProcessType;
        public float Duration;
        public float Temperature;
        public float Humidity;
        public List<string> Recommendations;

        /// <summary>
        /// Create standard drying process
        /// </summary>
        public static PostHarvestProcessDetails CreateStandardDrying()
        {
            return new PostHarvestProcessDetails
            {
                ProcessType = "StandardDrying",
                Duration = 7f,
                Temperature = 20f,
                Humidity = 55f,
                Recommendations = new List<string>
                {
                    "Maintain consistent temperature",
                    "Monitor humidity levels",
                    "Avoid direct light exposure"
                }
            };
        }
    }

    /// <summary>
    /// Shared post-harvest process recommendation
    /// </summary>
    [Serializable]
    public class PostHarvestProcess
    {
        public string ProcessType;
        public float Duration;
        public float Temperature;
        public float Humidity;
        public List<string> Recommendations;
    }

    /// <summary>
    /// Harvest execution result
    /// </summary>
    [Serializable]
    public struct HarvestExecutionResult
    {
        public bool Success;
        public string Message;
        public float ActualYield;
        public float ActualPotency;
        public HarvestQualityGrade Quality;
        public DateTime HarvestTimestamp;
        public PostHarvestMethod Method;
        public float ProcessingTime;

        /// <summary>
        /// Create success result
        /// </summary>
        public static HarvestExecutionResult CreateSuccess(float yield, float potency, HarvestQualityGrade quality, PostHarvestMethod method)
        {
            return new HarvestExecutionResult
            {
                Success = true,
                Message = "Harvest completed successfully",
                ActualYield = yield,
                ActualPotency = potency,
                Quality = quality,
                HarvestTimestamp = DateTime.Now,
                Method = method,
                ProcessingTime = 0f
            };
        }

        /// <summary>
        /// Create failure result
        /// </summary>
        public static HarvestExecutionResult CreateFailure(string message)
        {
            return new HarvestExecutionResult
            {
                Success = false,
                Message = message,
                ActualYield = 0f,
                ActualPotency = 0f,
                Quality = HarvestQualityGrade.Poor,
                HarvestTimestamp = DateTime.Now,
                ProcessingTime = 0f
            };
        }
    }

    /// <summary>
    /// Harvest attempt record
    /// </summary>
    [Serializable]
    public struct HarvestAttempt
    {
        public DateTime Timestamp;
        public string Method;
        public float ReadinessScore;
        public float Yield;
        public float Potency;
        public HarvestQualityGrade Quality;
        public bool Success;
    }

    /// <summary>
    /// Harvest readiness factors
    /// </summary>
    [Serializable]
    public struct HarvestReadinessFactors
    {
        public float TrichomeReadiness;
        public float PistilReadiness;
        public float CalyxSwelling;
        public float MaturityScore;
        public float EnvironmentalScore;

        /// <summary>
        /// Calculate weighted readiness score
        /// </summary>
        public float CalculateWeightedScore(float trichomeWeight, float maturityWeight, float envWeight)
        {
            return (TrichomeReadiness * trichomeWeight) +
                   (MaturityScore * maturityWeight) +
                   (EnvironmentalScore * envWeight);
        }
    }

    /// <summary>
    /// Harvest readiness assessment result
    /// </summary>
    [Serializable]
    public class HarvestReadiness
    {
        public string PlantID;
        public bool IsReadyForHarvest;
        public DateTime RecommendedHarvestDate;
        public float ReadinessScore;
        public string ReadinessReason;
        public string Reason => ReadinessReason; // Alias for compatibility
        public int DaysUntilOptimal;
        public HarvestWindow OptimalHarvestWindow;

        // Additional properties
        public string FloweringTime;
        public float TrichomeDensity;
        public string PistilColorChange;
    }

    /// <summary>
    /// Optimal harvest window data structure
    /// </summary>
    [Serializable]
    public class HarvestWindow
    {
        public DateTime StartDate;
        public DateTime EndDate;
        public float QualityScore;

        // Additional properties for compatibility
        public string PlantID { get; set; }
        public DateTime EarliestHarvestDate { get; set; } = DateTime.MinValue;
        public DateTime OptimalHarvestDate { get; set; } = DateTime.MinValue;
        public DateTime LatestHarvestDate { get; set; } = DateTime.MinValue;

        // Additional properties for harvest quality
        public float QualityAtEarliest { get; set; } = 0f;
        public float QualityAtOptimal { get; set; } = 1f;
        public float QualityAtLatest { get; set; } = 0.8f;
    }
}

