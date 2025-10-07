using System;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;
using ProjectChimera.Data.Equipment;

namespace ProjectChimera.Systems.Equipment.Degradation.Database
{
    /// <summary>
    /// PHASE 0 REFACTORED: Cost Trend Analysis Manager (Coordinator)
    /// Single Responsibility: Orchestrate trend analysis components and manage lifecycle
    /// Refactored from 722 lines → 309 lines (4 files total, all <500 lines)
    /// Dependencies: TrendMetricsCalculator, TrendPredictionEngine
    /// </summary>
    public class CostTrendAnalysisManager
    {
        private readonly bool _enableLogging;
        private readonly bool _enableTrendAnalysis;
        private readonly int _trendAnalysisPeriodDays;
        private readonly float _significantTrendThreshold;
        private readonly int _minimumTrendSamples;

        // Component dependencies
        private TrendMetricsCalculator _metricsCalculator;
        private TrendPredictionEngine _predictionEngine;

        // Trend analysis storage
        private readonly Dictionary<MalfunctionType, TrendAnalysis> _trendAnalyses = new Dictionary<MalfunctionType, TrendAnalysis>();
        private readonly Dictionary<EquipmentType, EquipmentTrendAnalysis> _equipmentTrends = new Dictionary<EquipmentType, EquipmentTrendAnalysis>();

        // Trend statistics
        private TrendAnalysisStatistics _trendStats = new TrendAnalysisStatistics();

        // Events
        public event Action<MalfunctionType, TrendAnalysis> OnTrendAnalysisUpdated;
        public event Action<EquipmentType, EquipmentTrendAnalysis> OnEquipmentTrendUpdated;
        public event Action<TrendAlert> OnTrendAlert;

        public TrendAnalysisStatistics Statistics => _trendStats;
        public bool IsTrendAnalysisEnabled => _enableTrendAnalysis;
        public int AnalyzedMalfunctionTypes => _trendAnalyses.Count;
        public int AnalyzedEquipmentTypes => _equipmentTrends.Count;

        public CostTrendAnalysisManager(bool enableLogging = false, bool enableTrendAnalysis = true,
                                      int trendAnalysisPeriodDays = 30, float significantTrendThreshold = 0.1f,
                                      int minimumTrendSamples = 10)
        {
            _enableLogging = enableLogging;
            _enableTrendAnalysis = enableTrendAnalysis;
            _trendAnalysisPeriodDays = trendAnalysisPeriodDays;
            _significantTrendThreshold = significantTrendThreshold;
            _minimumTrendSamples = minimumTrendSamples;
        }

        #region Initialization

        /// <summary>
        /// Initialize trend analysis manager and components
        /// </summary>
        public void Initialize()
        {
            if (_enableTrendAnalysis)
            {
                // Initialize component calculators
                _metricsCalculator = new TrendMetricsCalculator(_significantTrendThreshold, _enableLogging);
                _predictionEngine = new TrendPredictionEngine(_trendAnalysisPeriodDays, _significantTrendThreshold, 
                                                             _trendStats, _enableLogging);

                // Forward prediction engine events
                _predictionEngine.OnTrendAlert += HandleTrendAlert;

                InitializeTrendAnalyses();
            }

            if (_enableLogging)
                ChimeraLogger.LogInfo("TREND_ANALYSIS", "Cost trend analysis manager initialized", null);
        }

        /// <summary>
        /// Initialize trend analyses for all malfunction and equipment types
        /// </summary>
        private void InitializeTrendAnalyses()
        {
            var malfunctionTypes = Enum.GetValues(typeof(MalfunctionType)).Cast<MalfunctionType>();
            foreach (var type in malfunctionTypes)
            {
                _trendAnalyses[type] = new TrendAnalysis
                {
                    MalfunctionType = type,
                    AnalysisPeriodDays = _trendAnalysisPeriodDays,
                    LastAnalysis = DateTime.MinValue,
                    TrendDirection = TrendDirection.Stable,
                    Confidence = 0.0f
                };
            }

            var equipmentTypes = Enum.GetValues(typeof(EquipmentType)).Cast<EquipmentType>();
            foreach (var type in equipmentTypes)
            {
                _equipmentTrends[type] = new EquipmentTrendAnalysis
                {
                    EquipmentType = type,
                    AnalysisPeriodDays = _trendAnalysisPeriodDays,
                    LastAnalysis = DateTime.MinValue,
                    OverallTrend = TrendDirection.Stable,
                    Confidence = 0.0f
                };
            }

            if (_enableLogging)
                ChimeraLogger.LogInfo("TREND_ANALYSIS", 
                    $"Initialized trend analyses for {_trendAnalyses.Count} malfunction types and {_equipmentTrends.Count} equipment types", null);
        }

        #endregion

        #region Trend Analysis

        /// <summary>
        /// Update trend analysis with new data point
        /// </summary>
        public void UpdateTrendAnalysis(CostDataPoint dataPoint)
        {
            if (!_enableTrendAnalysis)
                return;

            try
            {
                // Update malfunction type trend
                UpdateMalfunctionTrend(dataPoint);

                // Update equipment type trend
                UpdateEquipmentTrend(dataPoint);

                _trendStats.TrendUpdates++;

                if (_enableLogging)
                    ChimeraLogger.LogInfo("TREND_ANALYSIS", 
                        $"Updated trends for {dataPoint.MalfunctionType} on {dataPoint.EquipmentType}", null);
            }
            catch (Exception ex)
            {
                _trendStats.TrendUpdateErrors++;

                if (_enableLogging)
                    ChimeraLogger.LogError("TREND_ANALYSIS", $"Failed to update trend analysis: {ex.Message}", null);
            }
        }

        /// <summary>
        /// Perform comprehensive trend analysis on historical data
        /// </summary>
        public void PerformComprehensiveAnalysis(List<CostDataPoint> historicalData)
        {
            if (!_enableTrendAnalysis || historicalData == null || historicalData.Count == 0)
                return;

            try
            {
                var analysisStartTime = DateTime.Now;

                // Analyze trends for each malfunction type
                foreach (var malfunctionType in _trendAnalyses.Keys.ToList())
                {
                    AnalyzeMalfunctionTypeTrend(malfunctionType, historicalData);
                }

                // Analyze trends for each equipment type
                foreach (var equipmentType in _equipmentTrends.Keys.ToList())
                {
                    AnalyzeEquipmentTypeTrend(equipmentType, historicalData);
                }

                var analysisTime = (DateTime.Now - analysisStartTime).TotalMilliseconds;
                _trendStats.ComprehensiveAnalyses++;
                _trendStats.TotalAnalysisTime += analysisTime;
                _trendStats.LastAnalysis = DateTime.Now;

                // Update prediction engine statistics
                _predictionEngine.UpdateStatistics(_trendStats);

                if (_enableLogging)
                    ChimeraLogger.LogInfo("TREND_ANALYSIS", 
                        $"Completed comprehensive trend analysis in {analysisTime:F1}ms", null);
            }
            catch (Exception ex)
            {
                _trendStats.AnalysisErrors++;

                if (_enableLogging)
                    ChimeraLogger.LogError("TREND_ANALYSIS", $"Comprehensive trend analysis failed: {ex.Message}", null);
            }
        }

        /// <summary>
        /// Analyze trend for specific malfunction type
        /// </summary>
        private void AnalyzeMalfunctionTypeTrend(MalfunctionType type, List<CostDataPoint> historicalData)
        {
            var typeData = historicalData
                .Where(h => h.MalfunctionType == type)
                .Where(h => h.Timestamp >= DateTime.Now.AddDays(-_trendAnalysisPeriodDays))
                .OrderBy(h => h.Timestamp)
                .ToList();

            if (typeData.Count < _minimumTrendSamples)
            {
                if (_enableLogging)
                    ChimeraLogger.LogWarning("TREND_ANALYSIS", 
                        $"Insufficient data for {type} trend analysis: {typeData.Count} samples", null);
                return;
            }

            var trend = _trendAnalyses[type];
            
            // Use metrics calculator to compute trend metrics
            _metricsCalculator.CalculateTrendMetrics(typeData, trend);

            trend.LastAnalysis = DateTime.Now;
            trend.SampleSize = typeData.Count;

            // Use prediction engine to check for trend alerts
            _predictionEngine.CheckForTrendAlerts(type, trend);

            OnTrendAnalysisUpdated?.Invoke(type, trend);
        }

        /// <summary>
        /// Analyze trend for specific equipment type
        /// </summary>
        private void AnalyzeEquipmentTypeTrend(EquipmentType type, List<CostDataPoint> historicalData)
        {
            var equipmentData = historicalData
                .Where(h => h.EquipmentType == type)
                .Where(h => h.Timestamp >= DateTime.Now.AddDays(-_trendAnalysisPeriodDays))
                .OrderBy(h => h.Timestamp)
                .ToList();

            if (equipmentData.Count < _minimumTrendSamples)
                return;

            var equipmentTrend = _equipmentTrends[type];
            
            // Use metrics calculator to compute equipment trend metrics
            _metricsCalculator.CalculateEquipmentTrendMetrics(equipmentData, equipmentTrend);

            equipmentTrend.LastAnalysis = DateTime.Now;
            equipmentTrend.SampleSize = equipmentData.Count;

            OnEquipmentTrendUpdated?.Invoke(type, equipmentTrend);
        }

        #endregion

        #region Trend Prediction

        /// <summary>
        /// Predict future costs based on current trends
        /// </summary>
        public TrendPrediction PredictFutureCosts(MalfunctionType type, int daysIntoFuture)
        {
            if (!_trendAnalyses.TryGetValue(type, out var trend))
            {
                return TrendPrediction.CreateFailure(type, "No trend data available");
            }

            return _predictionEngine.PredictFutureCosts(type, trend, daysIntoFuture);
        }

        /// <summary>
        /// Generate multi-period cost forecast
        /// </summary>
        public TrendForecast GenerateCostForecast(MalfunctionType type, int[] forecastDays)
        {
            if (!_trendAnalyses.TryGetValue(type, out var trend))
            {
                return new TrendForecast { Success = false, ErrorMessage = "No trend data available" };
            }

            return _predictionEngine.GenerateCostForecast(type, trend, forecastDays);
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Update malfunction trend with single data point
        /// </summary>
        private void UpdateMalfunctionTrend(CostDataPoint dataPoint)
        {
            if (_trendAnalyses.TryGetValue(dataPoint.MalfunctionType, out var trend))
            {
                // Use metrics calculator for moving average update
                _metricsCalculator.UpdateMovingAverage(ref trend.RecentAverageCost, dataPoint.ActualCost);
                trend.LastDataPoint = dataPoint.Timestamp;
            }
        }

        /// <summary>
        /// Update equipment trend with single data point
        /// </summary>
        private void UpdateEquipmentTrend(CostDataPoint dataPoint)
        {
            if (_equipmentTrends.TryGetValue(dataPoint.EquipmentType, out var equipmentTrend))
            {
                // Update malfunction breakdown
                if (equipmentTrend.MalfunctionBreakdown == null)
                    equipmentTrend.MalfunctionBreakdown = new Dictionary<MalfunctionType, MalfunctionTrendData>();

                if (!equipmentTrend.MalfunctionBreakdown.TryGetValue(dataPoint.MalfunctionType, out var malfunctionData))
                {
                    malfunctionData = new MalfunctionTrendData();
                    equipmentTrend.MalfunctionBreakdown[dataPoint.MalfunctionType] = malfunctionData;
                }

                malfunctionData.UpdateWithDataPoint(dataPoint.ActualCost, (float)dataPoint.ActualTime.TotalHours);
                equipmentTrend.LastDataPoint = dataPoint.Timestamp;
            }
        }

        /// <summary>
        /// Handle trend alert from prediction engine
        /// </summary>
        private void HandleTrendAlert(TrendAlert alert)
        {
            OnTrendAlert?.Invoke(alert);
        }

        #endregion

        #region Data Access

        /// <summary>
        /// Get trend analysis for malfunction type
        /// </summary>
        public TrendAnalysis GetTrendAnalysis(MalfunctionType type)
        {
            return _trendAnalyses.TryGetValue(type, out var trend) ? trend : null;
        }

        /// <summary>
        /// Get equipment trend analysis
        /// </summary>
        public EquipmentTrendAnalysis GetEquipmentTrend(EquipmentType type)
        {
            return _equipmentTrends.TryGetValue(type, out var trend) ? trend : null;
        }

        /// <summary>
        /// Get all malfunction trend analyses
        /// </summary>
        public Dictionary<MalfunctionType, TrendAnalysis> GetAllTrendAnalyses()
        {
            return new Dictionary<MalfunctionType, TrendAnalysis>(_trendAnalyses);
        }

        /// <summary>
        /// Get all equipment trend analyses
        /// </summary>
        public Dictionary<EquipmentType, EquipmentTrendAnalysis> GetAllEquipmentTrends()
        {
            return new Dictionary<EquipmentType, EquipmentTrendAnalysis>(_equipmentTrends);
        }

        /// <summary>
        /// Reset trend analysis statistics
        /// </summary>
        public void ResetStatistics()
        {
            _trendStats = new TrendAnalysisStatistics();
            _predictionEngine.UpdateStatistics(_trendStats);

            if (_enableLogging)
                ChimeraLogger.LogInfo("TREND_ANALYSIS", "Trend analysis statistics reset", null);
        }

        #endregion
    }
}

