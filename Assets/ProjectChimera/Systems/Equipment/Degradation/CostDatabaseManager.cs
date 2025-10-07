using System;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using ProjectChimera.Data.Equipment;

namespace ProjectChimera.Systems.Equipment.Degradation
{
    /// <summary>
    /// PHASE 0 REFACTORED: Cost Database Manager
    /// Single Responsibility: Manage cost data, caching, and historical data
    /// Extracted from MalfunctionCostEstimator (782 lines → 4 files <500 lines each)
    /// </summary>
    public class CostDatabaseManager
    {
        private readonly Dictionary<MalfunctionType, BaseCostData> _baseCostDatabase;
        private readonly Dictionary<string, CostEstimate> _estimateCache;
        private readonly bool _enableLogging;
        private readonly int _maxHistorySize = 100;

        public IReadOnlyDictionary<MalfunctionType, BaseCostData> BaseCostDatabase => _baseCostDatabase;

        public CostDatabaseManager(bool enableLogging)
        {
            _baseCostDatabase = new Dictionary<MalfunctionType, BaseCostData>();
            _estimateCache = new Dictionary<string, CostEstimate>();
            _enableLogging = enableLogging;
            InitializeDatabase();
        }

        /// <summary>
        /// Get base cost data with fallback
        /// </summary>
        public BaseCostData GetBaseCostData(MalfunctionType type)
        {
            if (_baseCostDatabase.TryGetValue(type, out var data))
            {
                return data;
            }

            // Fallback to WearAndTear
            return _baseCostDatabase[MalfunctionType.WearAndTear];
        }

        /// <summary>
        /// Update cost database with actual repair data
        /// </summary>
        public void UpdateCostDatabase(MalfunctionType type, float actualCost, TimeSpan actualTime, MalfunctionSeverity severity)
        {
            if (!_baseCostDatabase.TryGetValue(type, out var costData))
                return;

            // Update historical data
            costData.UpdateHistory(actualCost, (float)actualTime.TotalMinutes, _maxHistorySize);

            if (_enableLogging)
            {
                ChimeraLogger.Log("EQUIPMENT",
                    $"Updated cost database for {type}: Avg Cost=${costData.AverageActualCost:F2}, Avg Time={costData.AverageActualTime:F1}min",
                    null);
            }
        }

        /// <summary>
        /// Cache cost estimate
        /// </summary>
        public void CacheEstimate(CostEstimate estimate)
        {
            if (estimate != null && !string.IsNullOrEmpty(estimate.EstimateId))
            {
                _estimateCache[estimate.EstimateId] = estimate;
            }
        }

        /// <summary>
        /// Get cached estimate
        /// </summary>
        public CostEstimate GetCachedEstimate(string estimateId)
        {
            return _estimateCache.TryGetValue(estimateId, out var estimate) ? estimate : null;
        }

        /// <summary>
        /// Clear estimate cache
        /// </summary>
        public void ClearCache()
        {
            _estimateCache.Clear();

            if (_enableLogging)
            {
                ChimeraLogger.Log("EQUIPMENT", "Cost estimate cache cleared", null);
            }
        }

        /// <summary>
        /// Get cache statistics
        /// </summary>
        public (int count, DateTime oldestEstimate) GetCacheStats()
        {
            var oldestDate = DateTime.MaxValue;
            foreach (var estimate in _estimateCache.Values)
            {
                if (estimate.EstimationTime < oldestDate)
                {
                    oldestDate = estimate.EstimationTime;
                }
            }

            return (_estimateCache.Count, oldestDate == DateTime.MaxValue ? DateTime.Now : oldestDate);
        }

        #region Database Initialization

        /// <summary>
        /// Initialize base cost database with default values
        /// </summary>
        private void InitializeDatabase()
        {
            _baseCostDatabase[MalfunctionType.MechanicalFailure] = BaseCostData.CreateDefault(
                baseCost: 500f,
                baseTime: 120,
                partsRatio: 0.4f,
                laborRatio: 0.6f
            );

            _baseCostDatabase[MalfunctionType.ElectricalFailure] = BaseCostData.CreateDefault(
                baseCost: 450f,
                baseTime: 90,
                partsRatio: 0.3f,
                laborRatio: 0.7f
            );

            _baseCostDatabase[MalfunctionType.SensorDrift] = BaseCostData.CreateDefault(
                baseCost: 200f,
                baseTime: 30,
                partsRatio: 0.5f,
                laborRatio: 0.5f
            );

            _baseCostDatabase[MalfunctionType.OverheatingProblem] = BaseCostData.CreateDefault(
                baseCost: 300f,
                baseTime: 60,
                partsRatio: 0.35f,
                laborRatio: 0.65f
            );

            _baseCostDatabase[MalfunctionType.SoftwareError] = BaseCostData.CreateDefault(
                baseCost: 350f,
                baseTime: 45,
                partsRatio: 0.1f,
                laborRatio: 0.9f
            );

            _baseCostDatabase[MalfunctionType.WearAndTear] = BaseCostData.CreateDefault(
                baseCost: 400f,
                baseTime: 75,
                partsRatio: 0.45f,
                laborRatio: 0.55f
            );
        }

        #endregion

        #region Historical Data Analysis

        /// <summary>
        /// Get average cost for malfunction type
        /// </summary>
        public float GetAverageCost(MalfunctionType type)
        {
            if (_baseCostDatabase.TryGetValue(type, out var data))
            {
                return data.AverageActualCost > 0f ? data.AverageActualCost : data.BaseCost;
            }
            return 0f;
        }

        /// <summary>
        /// Get average time for malfunction type
        /// </summary>
        public float GetAverageTime(MalfunctionType type)
        {
            if (_baseCostDatabase.TryGetValue(type, out var data))
            {
                return data.AverageActualTime > 0f ? data.AverageActualTime : data.BaseTimeMinutes;
            }
            return 0f;
        }

        /// <summary>
        /// Check if historical data is available
        /// </summary>
        public bool HasHistoricalData(MalfunctionType type)
        {
            if (_baseCostDatabase.TryGetValue(type, out var data))
            {
                return data.ActualCostHistory.Count > 0;
            }
            return false;
        }

        /// <summary>
        /// Get cost trend (positive = increasing, negative = decreasing)
        /// </summary>
        public float GetCostTrend(MalfunctionType type)
        {
            if (!_baseCostDatabase.TryGetValue(type, out var data))
                return 0f;

            if (data.ActualCostHistory.Count < 5)
                return 0f;

            // Compare recent average to overall average
            float recentSum = 0f;
            int recentCount = Math.Min(5, data.ActualCostHistory.Count);
            for (int i = data.ActualCostHistory.Count - recentCount; i < data.ActualCostHistory.Count; i++)
            {
                recentSum += data.ActualCostHistory[i];
            }
            float recentAvg = recentSum / recentCount;

            return recentAvg - data.AverageActualCost;
        }

        #endregion
    }
}
