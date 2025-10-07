using System;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;
using ProjectChimera.Data.Equipment;

namespace ProjectChimera.Systems.Equipment.Degradation.Database
{
    /// <summary>
    /// PHASE 0 REFACTORED: Trend Metrics Calculator
    /// Single Responsibility: Calculate trend metrics, regression analysis, and statistical measures
    /// Extracted from CostTrendAnalysisManager (722 lines → 4 files <500 lines each)
    /// </summary>
    public class TrendMetricsCalculator
    {
        private readonly float _significantTrendThreshold;
        private readonly bool _enableLogging;

        public TrendMetricsCalculator(float significantTrendThreshold, bool enableLogging = false)
        {
            _significantTrendThreshold = significantTrendThreshold;
            _enableLogging = enableLogging;
        }

        #region Main Trend Calculations

        /// <summary>
        /// Calculate comprehensive trend metrics for malfunction type
        /// </summary>
        public void CalculateTrendMetrics(List<CostDataPoint> data, TrendAnalysis trend)
        {
            if (data == null || data.Count < 2)
            {
                if (_enableLogging)
                    ChimeraLogger.LogWarning("TREND_ANALYSIS", "Insufficient data for trend metrics calculation", null);
                return;
            }

            // Calculate linear regression for cost trend
            var costRegression = CalculateLinearRegression(data.Select((d, i) => (i, d.ActualCost)).ToList());
            trend.CostSlope = costRegression.Slope;
            trend.CostRSquared = costRegression.RSquared;

            // Calculate linear regression for time trend
            var timeRegression = CalculateLinearRegression(data.Select((d, i) => (i, (float)d.ActualTime.TotalHours)).ToList());
            trend.TimeSlope = timeRegression.Slope;
            trend.TimeRSquared = timeRegression.RSquared;

            // Determine overall trend direction
            var significantCostTrend = Math.Abs(trend.CostSlope) >= _significantTrendThreshold;
            var significantTimeTrend = Math.Abs(trend.TimeSlope) >= _significantTrendThreshold;

            if (significantCostTrend || significantTimeTrend)
            {
                if (trend.CostSlope > 0 || trend.TimeSlope > 0)
                    trend.TrendDirection = TrendDirection.Increasing;
                else
                    trend.TrendDirection = TrendDirection.Decreasing;
            }
            else
            {
                trend.TrendDirection = TrendDirection.Stable;
            }

            // Calculate confidence based on R-squared values and sample size
            var avgRSquared = (trend.CostRSquared + trend.TimeRSquared) / 2;
            var sampleConfidence = Math.Min(data.Count / 100f, 1.0f);
            trend.Confidence = (float)(avgRSquared * sampleConfidence);

            // Calculate recent vs historical comparison
            CalculateRecentComparison(data, trend);

            // Calculate volatility
            CalculateVolatility(data, trend);

            if (_enableLogging)
                ChimeraLogger.LogInfo("TREND_ANALYSIS",
                    $"Metrics calculated: Slope={trend.CostSlope:F2}, R²={trend.CostRSquared:F3}, Confidence={trend.Confidence:P1}", null);
        }

        /// <summary>
        /// Calculate equipment trend metrics
        /// </summary>
        public void CalculateEquipmentTrendMetrics(List<CostDataPoint> data, EquipmentTrendAnalysis equipmentTrend)
        {
            if (data == null || data.Count < 2)
                return;

            // Group by malfunction type for equipment
            var malfunctionBreakdown = data
                .GroupBy(d => d.MalfunctionType)
                .ToDictionary(g => g.Key, g => new MalfunctionTrendData
                {
                    Count = g.Count(),
                    AverageCost = g.Average(d => d.ActualCost),
                    AverageTime = g.Average(d => d.ActualTime.TotalHours),
                    TrendDirection = CalculateSimpleTrend(g.OrderBy(d => d.Timestamp).Select(d => d.ActualCost).ToList())
                });

            equipmentTrend.MalfunctionBreakdown = malfunctionBreakdown;

            // Calculate overall equipment trend
            var costRegression = CalculateLinearRegression(data.Select((d, i) => (i, d.ActualCost)).ToList());
            equipmentTrend.OverallCostSlope = costRegression.Slope;
            equipmentTrend.CostRSquared = costRegression.RSquared;

            // Determine overall trend
            if (Math.Abs(equipmentTrend.OverallCostSlope) >= _significantTrendThreshold)
            {
                equipmentTrend.OverallTrend = equipmentTrend.OverallCostSlope > 0 ? TrendDirection.Increasing : TrendDirection.Decreasing;
            }
            else
            {
                equipmentTrend.OverallTrend = TrendDirection.Stable;
            }

            equipmentTrend.Confidence = (float)(equipmentTrend.CostRSquared * Math.Min(data.Count / 50f, 1.0f));

            if (_enableLogging)
                ChimeraLogger.LogInfo("TREND_ANALYSIS",
                    $"Equipment metrics calculated: Slope={equipmentTrend.OverallCostSlope:F2}, Breakdowns={malfunctionBreakdown.Count}", null);
        }

        #endregion

        #region Statistical Calculations

        /// <summary>
        /// Calculate linear regression on data points
        /// </summary>
        public RegressionResult CalculateLinearRegression(List<(int X, float Y)> dataPoints)
        {
            if (dataPoints == null || dataPoints.Count < 2)
            {
                return new RegressionResult
                {
                    Slope = 0,
                    Intercept = 0,
                    RSquared = 0
                };
            }

            var n = dataPoints.Count;
            var sumX = dataPoints.Sum(p => p.X);
            var sumY = dataPoints.Sum(p => p.Y);
            var sumXY = dataPoints.Sum(p => p.X * p.Y);
            var sumX2 = dataPoints.Sum(p => p.X * p.X);
            var sumY2 = dataPoints.Sum(p => p.Y * p.Y);

            // Calculate slope and intercept
            var slope = (n * sumXY - sumX * sumY) / (n * sumX2 - sumX * sumX);
            var intercept = (sumY - slope * sumX) / n;

            // Calculate R-squared
            var yMean = sumY / n;
            var ssTotal = dataPoints.Sum(p => Math.Pow(p.Y - yMean, 2));
            var ssResidual = dataPoints.Sum(p => Math.Pow(p.Y - (slope * p.X + intercept), 2));
            var rSquared = ssTotal > 0 ? 1 - (ssResidual / ssTotal) : 0;

            return new RegressionResult
            {
                Slope = slope,
                Intercept = intercept,
                RSquared = Math.Max(0, rSquared) // Ensure non-negative
            };
        }

        /// <summary>
        /// Calculate simple trend direction from values
        /// </summary>
        public TrendDirection CalculateSimpleTrend(List<float> values)
        {
            if (values == null || values.Count < 2)
                return TrendDirection.Stable;

            var midpoint = values.Count / 2;
            var firstHalf = values.Take(midpoint).Average();
            var secondHalf = values.Skip(midpoint).Average();

            var percentageChange = Math.Abs((secondHalf - firstHalf) / firstHalf);

            if (percentageChange < _significantTrendThreshold)
                return TrendDirection.Stable;

            return secondHalf > firstHalf ? TrendDirection.Increasing : TrendDirection.Decreasing;
        }

        /// <summary>
        /// Calculate recent vs historical comparison
        /// </summary>
        public void CalculateRecentComparison(List<CostDataPoint> data, TrendAnalysis trend)
        {
            if (data == null || data.Count < 2)
                return;

            var midpoint = data.Count / 2;
            var recentData = data.Skip(midpoint).ToList();
            var historicalData = data.Take(midpoint).ToList();

            if (recentData.Count > 0 && historicalData.Count > 0)
            {
                trend.RecentAverageCost = recentData.Average(d => d.ActualCost);
                trend.HistoricalAverageCost = historicalData.Average(d => d.ActualCost);

                if (trend.HistoricalAverageCost > 0)
                {
                    trend.PercentageChange = ((trend.RecentAverageCost - trend.HistoricalAverageCost) / trend.HistoricalAverageCost) * 100f;
                }
            }

            if (_enableLogging)
                ChimeraLogger.LogInfo("TREND_ANALYSIS",
                    $"Comparison: Recent=${trend.RecentAverageCost:F2}, Historical=${trend.HistoricalAverageCost:F2}, Change={trend.PercentageChange:F1}%", null);
        }

        /// <summary>
        /// Calculate volatility metrics
        /// </summary>
        public void CalculateVolatility(List<CostDataPoint> data, TrendAnalysis trend)
        {
            if (data == null || data.Count < 2)
                return;

            var costs = data.Select(d => d.ActualCost).ToList();
            var mean = costs.Average();

            if (mean <= 0)
            {
                trend.Volatility = 0;
                trend.CoefficientOfVariation = 0;
                return;
            }

            var variance = costs.Average(c => Math.Pow(c - mean, 2));
            trend.Volatility = (float)Math.Sqrt(variance);
            trend.CoefficientOfVariation = trend.Volatility / mean;

            if (_enableLogging)
                ChimeraLogger.LogInfo("TREND_ANALYSIS",
                    $"Volatility: σ={trend.Volatility:F2}, CV={trend.CoefficientOfVariation:F3}", null);
        }

        #endregion

        #region Moving Average Updates

        /// <summary>
        /// Update moving average with new data point
        /// </summary>
        public void UpdateMovingAverage(ref float currentAverage, float newValue, float smoothingFactor = 0.1f)
        {
            if (currentAverage == 0)
            {
                currentAverage = newValue;
            }
            else
            {
                currentAverage = (currentAverage * (1f - smoothingFactor)) + (newValue * smoothingFactor);
            }
        }

        /// <summary>
        /// Calculate exponential moving average
        /// </summary>
        public float CalculateExponentialMovingAverage(List<float> values, float smoothingFactor = 0.2f)
        {
            if (values == null || values.Count == 0)
                return 0f;

            var ema = values[0];
            for (int i = 1; i < values.Count; i++)
            {
                ema = (values[i] * smoothingFactor) + (ema * (1f - smoothingFactor));
            }

            return ema;
        }

        #endregion

        #region Validation

        /// <summary>
        /// Validate data quality for analysis
        /// </summary>
        public bool ValidateDataQuality(List<CostDataPoint> data, int minimumSamples, out string errorMessage)
        {
            errorMessage = string.Empty;

            if (data == null || data.Count == 0)
            {
                errorMessage = "No data provided";
                return false;
            }

            if (data.Count < minimumSamples)
            {
                errorMessage = $"Insufficient samples: {data.Count} < {minimumSamples}";
                return false;
            }

            // Check for valid costs
            if (data.Any(d => d.ActualCost < 0))
            {
                errorMessage = "Invalid negative costs detected";
                return false;
            }

            // Check for valid timestamps
            if (data.Any(d => d.Timestamp == DateTime.MinValue))
            {
                errorMessage = "Invalid timestamps detected";
                return false;
            }

            return true;
        }

        #endregion
    }
}

