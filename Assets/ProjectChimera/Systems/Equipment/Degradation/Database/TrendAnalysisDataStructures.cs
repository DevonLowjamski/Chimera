using System;
using System.Collections.Generic;
using ProjectChimera.Data.Equipment;

namespace ProjectChimera.Systems.Equipment.Degradation.Database
{
    /// <summary>
    /// PHASE 0 REFACTORED: Trend Analysis Data Structures
    /// Single Responsibility: Define all trend analysis data types
    /// Extracted from CostTrendAnalysisManager (722 lines → 4 files <500 lines each)
    /// </summary>

    /// <summary>
    /// Trend analysis statistics
    /// </summary>
    [Serializable]
    public class TrendAnalysisStatistics
    {
        public int TrendUpdates = 0;
        public int TrendUpdateErrors = 0;
        public int ComprehensiveAnalyses = 0;
        public int AnalysisErrors = 0;
        public int PredictionsGenerated = 0;
        public int PredictionErrors = 0;
        public int AlertsGenerated = 0;
        public double TotalAnalysisTime = 0.0;
        public DateTime LastAnalysis = DateTime.MinValue;

        /// <summary>
        /// Get average analysis time in milliseconds
        /// </summary>
        public double AverageAnalysisTime => ComprehensiveAnalyses > 0 ? TotalAnalysisTime / ComprehensiveAnalyses : 0.0;

        /// <summary>
        /// Get success rate for trend updates
        /// </summary>
        public float UpdateSuccessRate => TrendUpdates > 0 ? (float)(TrendUpdates - TrendUpdateErrors) / TrendUpdates : 0f;

        /// <summary>
        /// Get prediction success rate
        /// </summary>
        public float PredictionSuccessRate => PredictionsGenerated > 0 ? (float)(PredictionsGenerated - PredictionErrors) / PredictionsGenerated : 0f;
    }

    /// <summary>
    /// Trend analysis results for malfunction types
    /// </summary>
    [Serializable]
    public class TrendAnalysis
    {
        public MalfunctionType MalfunctionType;
        public TrendDirection TrendDirection;
        public float CostSlope;
        public float TimeSlope;
        public double CostRSquared;
        public double TimeRSquared;
        public float Confidence;
        public int AnalysisPeriodDays;
        public int SampleSize;
        public float RecentAverageCost;
        public float HistoricalAverageCost;
        public float PercentageChange;
        public float Volatility;
        public float CoefficientOfVariation;
        public DateTime LastAnalysis;
        public DateTime LastDataPoint;

        /// <summary>
        /// Check if trend is significant
        /// </summary>
        public bool IsSignificant(float threshold) => Math.Abs(CostSlope) >= threshold || Math.Abs(TimeSlope) >= threshold;

        /// <summary>
        /// Check if analysis is recent
        /// </summary>
        public bool IsRecent(int maxAgeDays) => (DateTime.Now - LastAnalysis).TotalDays <= maxAgeDays;
    }

    /// <summary>
    /// Equipment trend analysis results
    /// </summary>
    [Serializable]
    public class EquipmentTrendAnalysis
    {
        public EquipmentType EquipmentType;
        public TrendDirection OverallTrend;
        public float OverallCostSlope;
        public double CostRSquared;
        public float Confidence;
        public int AnalysisPeriodDays;
        public int SampleSize;
        public Dictionary<MalfunctionType, MalfunctionTrendData> MalfunctionBreakdown;
        public DateTime LastAnalysis;
        public DateTime LastDataPoint;

        /// <summary>
        /// Get most common malfunction type
        /// </summary>
        public MalfunctionType? GetMostCommonMalfunction()
        {
            if (MalfunctionBreakdown == null || MalfunctionBreakdown.Count == 0)
                return null;

            var maxCount = 0;
            MalfunctionType? mostCommon = null;

            foreach (var kvp in MalfunctionBreakdown)
            {
                if (kvp.Value.Count > maxCount)
                {
                    maxCount = kvp.Value.Count;
                    mostCommon = kvp.Key;
                }
            }

            return mostCommon;
        }
    }

    /// <summary>
    /// Malfunction trend data within equipment analysis
    /// </summary>
    [Serializable]
    public class MalfunctionTrendData
    {
        public int Count = 0;
        public double AverageCost = 0.0;
        public double AverageTime = 0.0;
        public TrendDirection TrendDirection = TrendDirection.Stable;

        /// <summary>
        /// Update with new data point
        /// </summary>
        public void UpdateWithDataPoint(float cost, float timeHours)
        {
            if (Count == 0)
            {
                AverageCost = cost;
                AverageTime = timeHours;
            }
            else
            {
                AverageCost = ((AverageCost * Count) + cost) / (Count + 1);
                AverageTime = ((AverageTime * Count) + timeHours) / (Count + 1);
            }
            Count++;
        }
    }

    /// <summary>
    /// Linear regression calculation result
    /// </summary>
    [Serializable]
    public struct RegressionResult
    {
        public float Slope;
        public float Intercept;
        public double RSquared;

        /// <summary>
        /// Check if regression is reliable
        /// </summary>
        public readonly bool IsReliable(double minRSquared = 0.5) => RSquared >= minRSquared;

        /// <summary>
        /// Predict value at given X position
        /// </summary>
        public readonly float Predict(int x) => (Slope * x) + Intercept;
    }

    /// <summary>
    /// Trend prediction result
    /// </summary>
    [Serializable]
    public struct TrendPrediction
    {
        public bool Success;
        public string ErrorMessage;
        public MalfunctionType MalfunctionType;
        public int PredictionPeriodDays;
        public float BaseCost;
        public float PredictedCost;
        public float ConfidenceIntervalLow;
        public float ConfidenceIntervalHigh;
        public float Confidence;
        public PredictionRisk PredictionRisk;

        /// <summary>
        /// Get prediction range
        /// </summary>
        public readonly float PredictionRange => ConfidenceIntervalHigh - ConfidenceIntervalLow;

        /// <summary>
        /// Get percentage change from base
        /// </summary>
        public readonly float PercentageChange => BaseCost > 0 ? ((PredictedCost - BaseCost) / BaseCost) * 100f : 0f;

        /// <summary>
        /// Create successful prediction
        /// </summary>
        public static TrendPrediction CreateSuccess(MalfunctionType type, int days, float baseCost, float predictedCost, float confidence)
        {
            return new TrendPrediction
            {
                Success = true,
                MalfunctionType = type,
                PredictionPeriodDays = days,
                BaseCost = baseCost,
                PredictedCost = predictedCost,
                Confidence = confidence
            };
        }

        /// <summary>
        /// Create failed prediction
        /// </summary>
        public static TrendPrediction CreateFailure(MalfunctionType type, string errorMessage)
        {
            return new TrendPrediction
            {
                Success = false,
                MalfunctionType = type,
                ErrorMessage = errorMessage
            };
        }
    }

    /// <summary>
    /// Trend alert information
    /// </summary>
    [Serializable]
    public struct TrendAlert
    {
        public TrendAlertType AlertType;
        public MalfunctionType MalfunctionType;
        public AlertSeverity Severity;
        public float PercentageChange;
        public float Confidence;
        public DateTime Timestamp;

        /// <summary>
        /// Create trend alert
        /// </summary>
        public static TrendAlert Create(TrendAlertType type, MalfunctionType malfunction, AlertSeverity severity, float percentageChange, float confidence)
        {
            return new TrendAlert
            {
                AlertType = type,
                MalfunctionType = malfunction,
                Severity = severity,
                PercentageChange = percentageChange,
                Confidence = confidence,
                Timestamp = DateTime.Now
            };
        }

        /// <summary>
        /// Check if alert is critical
        /// </summary>
        public readonly bool IsCritical => Severity == AlertSeverity.Critical;
    }

    #region Enumerations

    /// <summary>
    /// Trend direction enumeration
    /// </summary>
    public enum TrendDirection
    {
        Decreasing,
        Stable,
        Increasing
    }

    /// <summary>
    /// Prediction risk levels
    /// </summary>
    public enum PredictionRisk
    {
        Low,
        Medium,
        High
    }

    /// <summary>
    /// Trend alert types
    /// </summary>
    public enum TrendAlertType
    {
        CostIncrease,
        CostDecrease,
        VolatilityIncrease,
        PatternChange
    }

    /// <summary>
    /// Alert severity levels
    /// </summary>
    public enum AlertSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }

    #endregion
}

