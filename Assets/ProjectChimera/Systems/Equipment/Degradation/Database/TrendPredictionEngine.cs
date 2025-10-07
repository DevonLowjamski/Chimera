using System;
using ProjectChimera.Core.Logging;
using ProjectChimera.Data.Equipment;

namespace ProjectChimera.Systems.Equipment.Degradation.Database
{
    /// <summary>
    /// PHASE 0 REFACTORED: Trend Prediction Engine
    /// Single Responsibility: Generate cost predictions and trend alerts based on analysis
    /// Extracted from CostTrendAnalysisManager (722 lines → 4 files <500 lines each)
    /// </summary>
    public class TrendPredictionEngine
    {
        private readonly int _trendAnalysisPeriodDays;
        private readonly float _significantTrendThreshold;
        private readonly bool _enableLogging;

        private TrendAnalysisStatistics _stats;

        public event Action<TrendAlert> OnTrendAlert;

        public TrendPredictionEngine(int trendAnalysisPeriodDays, float significantTrendThreshold,
                                    TrendAnalysisStatistics stats, bool enableLogging = false)
        {
            _trendAnalysisPeriodDays = trendAnalysisPeriodDays;
            _significantTrendThreshold = significantTrendThreshold;
            _stats = stats;
            _enableLogging = enableLogging;
        }

        /// <summary>
        /// Update statistics reference
        /// </summary>
        public void UpdateStatistics(TrendAnalysisStatistics stats)
        {
            _stats = stats;
        }

        #region Prediction

        /// <summary>
        /// Predict future costs based on current trends
        /// </summary>
        public TrendPrediction PredictFutureCosts(MalfunctionType type, TrendAnalysis trend, int daysIntoFuture)
        {
            try
            {
                if (trend == null)
                {
                    return TrendPrediction.CreateFailure(type, "No trend data available");
                }

                if (trend.Confidence < 0.3f)
                {
                    return TrendPrediction.CreateFailure(type, "Insufficient confidence in trend data");
                }

                var prediction = new TrendPrediction
                {
                    Success = true,
                    MalfunctionType = type,
                    PredictionPeriodDays = daysIntoFuture,
                    BaseCost = trend.RecentAverageCost,
                    Confidence = trend.Confidence
                };

                // Linear extrapolation based on current slope
                var dailyChangeRate = trend.CostSlope / _trendAnalysisPeriodDays;
                prediction.PredictedCost = trend.RecentAverageCost + (dailyChangeRate * daysIntoFuture);

                // Calculate confidence interval
                var errorMargin = prediction.PredictedCost * (1 - trend.Confidence) * 0.5f;
                prediction.ConfidenceIntervalLow = prediction.PredictedCost - errorMargin;
                prediction.ConfidenceIntervalHigh = prediction.PredictedCost + errorMargin;

                // Assess prediction risk
                prediction.PredictionRisk = AssessPredictionRisk(trend, prediction.PredictedCost);

                _stats.PredictionsGenerated++;

                if (_enableLogging)
                    ChimeraLogger.LogInfo("TREND_ANALYSIS",
                        $"Prediction for {type}: ${prediction.PredictedCost:F2} in {daysIntoFuture} days (Confidence: {prediction.Confidence:P1}, Risk: {prediction.PredictionRisk})", null);

                return prediction;
            }
            catch (Exception ex)
            {
                _stats.PredictionErrors++;

                if (_enableLogging)
                    ChimeraLogger.LogError("TREND_ANALYSIS", $"Failed to predict future costs for {type}: {ex.Message}", null);

                return TrendPrediction.CreateFailure(type, ex.Message);
            }
        }

        /// <summary>
        /// Assess prediction risk based on volatility
        /// </summary>
        private PredictionRisk AssessPredictionRisk(TrendAnalysis trend, float predictedCost)
        {
            if (predictedCost <= 0)
                return PredictionRisk.High;

            var volatilityRatio = trend.Volatility / predictedCost;

            if (volatilityRatio > 0.3f)
                return PredictionRisk.High;
            else if (volatilityRatio > 0.15f)
                return PredictionRisk.Medium;
            else
                return PredictionRisk.Low;
        }

        #endregion

        #region Alert Generation

        /// <summary>
        /// Check for trend alerts and generate notifications
        /// </summary>
        public void CheckForTrendAlerts(MalfunctionType type, TrendAnalysis trend)
        {
            if (trend == null)
                return;

            // Check for significant cost increase
            if (trend.PercentageChange > (_significantTrendThreshold * 100) && trend.Confidence > 0.5f)
            {
                var severity = DetermineCostAlertSeverity(trend.PercentageChange, trend.Confidence);
                var alert = TrendAlert.Create(
                    TrendAlertType.CostIncrease,
                    type,
                    severity,
                    trend.PercentageChange,
                    trend.Confidence
                );

                _stats.AlertsGenerated++;
                OnTrendAlert?.Invoke(alert);

                if (_enableLogging)
                    ChimeraLogger.LogWarning("TREND_ANALYSIS",
                        $"ALERT: {type} cost increased {trend.PercentageChange:F1}% (Severity: {severity})", null);
            }

            // Check for significant cost decrease
            if (trend.PercentageChange < -(_significantTrendThreshold * 100) && trend.Confidence > 0.5f)
            {
                var severity = DetermineCostAlertSeverity(Math.Abs(trend.PercentageChange), trend.Confidence);
                var alert = TrendAlert.Create(
                    TrendAlertType.CostDecrease,
                    type,
                    severity,
                    trend.PercentageChange,
                    trend.Confidence
                );

                _stats.AlertsGenerated++;
                OnTrendAlert?.Invoke(alert);

                if (_enableLogging)
                    ChimeraLogger.LogInfo("TREND_ANALYSIS",
                        $"ALERT: {type} cost decreased {Math.Abs(trend.PercentageChange):F1}% (Severity: {severity})", null);
            }

            // Check for high volatility
            if (trend.CoefficientOfVariation > 0.5f && trend.Confidence > 0.4f)
            {
                var severity = DetermineVolatilityAlertSeverity(trend.CoefficientOfVariation, trend.Confidence);
                var alert = TrendAlert.Create(
                    TrendAlertType.VolatilityIncrease,
                    type,
                    severity,
                    trend.CoefficientOfVariation * 100f,
                    trend.Confidence
                );

                _stats.AlertsGenerated++;
                OnTrendAlert?.Invoke(alert);

                if (_enableLogging)
                    ChimeraLogger.LogWarning("TREND_ANALYSIS",
                        $"ALERT: {type} high volatility detected (CV: {trend.CoefficientOfVariation:F2})", null);
            }

            // Check for pattern change (R-squared drop with confidence)
            if (trend.CostRSquared < 0.3 && trend.SampleSize > 20 && trend.Confidence < 0.4f)
            {
                var alert = TrendAlert.Create(
                    TrendAlertType.PatternChange,
                    type,
                    AlertSeverity.Medium,
                    (float)(1 - trend.CostRSquared) * 100f,
                    trend.Confidence
                );

                _stats.AlertsGenerated++;
                OnTrendAlert?.Invoke(alert);

                if (_enableLogging)
                    ChimeraLogger.LogWarning("TREND_ANALYSIS",
                        $"ALERT: {type} pattern change detected (R²: {trend.CostRSquared:F3})", null);
            }
        }

        /// <summary>
        /// Determine alert severity for cost changes
        /// </summary>
        private AlertSeverity DetermineCostAlertSeverity(float percentageChange, float confidence)
        {
            var absoluteChange = Math.Abs(percentageChange);

            if (absoluteChange > 50f && confidence > 0.7f)
                return AlertSeverity.Critical;
            else if (absoluteChange > 30f && confidence > 0.6f)
                return AlertSeverity.High;
            else if (absoluteChange > 15f && confidence > 0.5f)
                return AlertSeverity.Medium;
            else
                return AlertSeverity.Low;
        }

        /// <summary>
        /// Determine alert severity for volatility
        /// </summary>
        private AlertSeverity DetermineVolatilityAlertSeverity(float coefficientOfVariation, float confidence)
        {
            if (coefficientOfVariation > 1.0f && confidence > 0.6f)
                return AlertSeverity.Critical;
            else if (coefficientOfVariation > 0.75f && confidence > 0.5f)
                return AlertSeverity.High;
            else if (coefficientOfVariation > 0.5f && confidence > 0.4f)
                return AlertSeverity.Medium;
            else
                return AlertSeverity.Low;
        }

        #endregion

        #region Advanced Predictions

        /// <summary>
        /// Generate cost forecast with multiple time horizons
        /// </summary>
        public TrendForecast GenerateCostForecast(MalfunctionType type, TrendAnalysis trend, int[] forecastDays)
        {
            if (trend == null || forecastDays == null || forecastDays.Length == 0)
            {
                return new TrendForecast { Success = false, ErrorMessage = "Invalid forecast parameters" };
            }

            var forecast = new TrendForecast
            {
                Success = true,
                MalfunctionType = type,
                BaseCost = trend.RecentAverageCost,
                BaseConfidence = trend.Confidence,
                ForecastPeriods = new TrendPrediction[forecastDays.Length]
            };

            for (int i = 0; i < forecastDays.Length; i++)
            {
                forecast.ForecastPeriods[i] = PredictFutureCosts(type, trend, forecastDays[i]);
            }

            if (_enableLogging)
                ChimeraLogger.LogInfo("TREND_ANALYSIS",
                    $"Generated {forecastDays.Length}-period forecast for {type}", null);

            return forecast;
        }

        #endregion
    }

    /// <summary>
    /// Multi-period cost forecast
    /// </summary>
    [Serializable]
    public struct TrendForecast
    {
        public bool Success;
        public string ErrorMessage;
        public MalfunctionType MalfunctionType;
        public float BaseCost;
        public float BaseConfidence;
        public TrendPrediction[] ForecastPeriods;

        /// <summary>
        /// Get forecast for specific day
        /// </summary>
        public TrendPrediction GetForecastForDay(int day)
        {
            if (ForecastPeriods == null || ForecastPeriods.Length == 0)
                return TrendPrediction.CreateFailure(MalfunctionType, "No forecast data");

            // Find closest matching period
            TrendPrediction closest = ForecastPeriods[0];
            int minDiff = Math.Abs(ForecastPeriods[0].PredictionPeriodDays - day);

            for (int i = 1; i < ForecastPeriods.Length; i++)
            {
                int diff = Math.Abs(ForecastPeriods[i].PredictionPeriodDays - day);
                if (diff < minDiff)
                {
                    minDiff = diff;
                    closest = ForecastPeriods[i];
                }
            }

            return closest;
        }
    }
}

