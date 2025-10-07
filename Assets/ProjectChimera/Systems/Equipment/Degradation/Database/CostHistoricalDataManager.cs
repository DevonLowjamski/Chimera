using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;
using ProjectChimera.Data.Equipment;

namespace ProjectChimera.Systems.Equipment.Degradation.Database
{
    /// <summary>
    /// REFACTORED: Cost Historical Data Manager - Coordinator
    /// Single Responsibility: Managing historical cost data, filtering, and data maintenance
    /// Uses: CostHistoricalDataStructures.cs for data types
    /// </summary>
    public class CostHistoricalDataManager
    {
        private readonly bool _enableLogging;
        private readonly int _maxHistorySize;
        private readonly bool _compressHistoricalData;

        // Historical data storage
        private readonly List<CostDataPoint> _allCostHistory = new List<CostDataPoint>();
        private readonly Dictionary<MalfunctionType, List<CostDataPoint>> _historicalByType = new Dictionary<MalfunctionType, List<CostDataPoint>>();
        private readonly Dictionary<EquipmentType, List<CostDataPoint>> _historicalByEquipment = new Dictionary<EquipmentType, List<CostDataPoint>>();

        // Data compression and maintenance
        private readonly Queue<CostDataPoint> _compressionQueue = new Queue<CostDataPoint>();
        private DateTime _lastMaintenance = DateTime.Now;

        // Historical data statistics
        private HistoricalDataStatistics _historicalStats = new HistoricalDataStatistics();

        // Events
        public event System.Action<CostDataPoint> OnDataPointAdded;
        public event System.Action<int> OnHistoryMaintenance;
        public event System.Action<HistoricalDataSummary> OnDataSummaryUpdated;

        public CostHistoricalDataManager(bool enableLogging = false, int maxHistorySize = 100, bool compressHistoricalData = true)
        {
            _enableLogging = enableLogging;
            _maxHistorySize = maxHistorySize;
            _compressHistoricalData = compressHistoricalData;
        }

        // Properties
        public HistoricalDataStatistics Statistics => _historicalStats;
        public int TotalHistoricalEntries => _allCostHistory.Count;
        public DateTime OldestEntry => _allCostHistory.Count > 0 ? _allCostHistory.Min(h => h.Timestamp) : DateTime.MinValue;
        public DateTime NewestEntry => _allCostHistory.Count > 0 ? _allCostHistory.Max(h => h.Timestamp) : DateTime.MinValue;

        public void Initialize()
        {
            _lastMaintenance = DateTime.Now;
            if (_enableLogging)
                ChimeraLogger.LogInfo("HIST_DATA", "Historical data manager initialized", null);
        }

        public void AddCostDataPoint(CostDataPoint dataPoint)
        {
            try
            {
                _allCostHistory.Add(dataPoint);

                if (!_historicalByType.ContainsKey(dataPoint.MalfunctionType))
                    _historicalByType[dataPoint.MalfunctionType] = new List<CostDataPoint>();
                _historicalByType[dataPoint.MalfunctionType].Add(dataPoint);

                if (!_historicalByEquipment.ContainsKey(dataPoint.EquipmentType))
                    _historicalByEquipment[dataPoint.EquipmentType] = new List<CostDataPoint>();
                _historicalByEquipment[dataPoint.EquipmentType].Add(dataPoint);

                _historicalStats.TotalDataPoints++;
                _historicalStats.LastDataPointAdded = dataPoint.Timestamp;

                if (_allCostHistory.Count > _maxHistorySize)
                    PerformHistoryMaintenance();

                OnDataPointAdded?.Invoke(dataPoint);

                if (_enableLogging)
                    ChimeraLogger.LogInfo("HIST_DATA", $"Added data point: {dataPoint.MalfunctionType} on {dataPoint.EquipmentType} - Cost: {dataPoint.ActualCost:F2}", null);
            }
            catch (Exception ex)
            {
                _historicalStats.DataAddErrors++;
                if (_enableLogging)
                    ChimeraLogger.LogError("HIST_DATA", $"Failed to add cost data point: {ex.Message}", null);
            }
        }

        public List<CostDataPoint> GetHistoricalData(MalfunctionType? type = null, EquipmentType? equipmentType = null,
                                                    DateTime? startDate = null, DateTime? endDate = null,
                                                    MalfunctionSeverity? severity = null, int? maxResults = null)
        {
            try
            {
                IEnumerable<CostDataPoint> query = _allCostHistory;

                if (type.HasValue)
                    query = query.Where(h => h.MalfunctionType == type.Value);
                if (equipmentType.HasValue)
                    query = query.Where(h => h.EquipmentType == equipmentType.Value);
                if (severity.HasValue)
                    query = query.Where(h => h.Severity == severity.Value);
                if (startDate.HasValue)
                    query = query.Where(h => h.Timestamp >= startDate.Value);
                if (endDate.HasValue)
                    query = query.Where(h => h.Timestamp <= endDate.Value);

                query = query.OrderByDescending(h => h.Timestamp);

                if (maxResults.HasValue)
                    query = query.Take(maxResults.Value);

                var results = query.ToList();
                _historicalStats.HistoricalQueries++;

                if (_enableLogging)
                    ChimeraLogger.LogInfo("HIST_DATA", $"Historical query returned {results.Count} results", null);

                return results;
            }
            catch (Exception ex)
            {
                _historicalStats.QueryErrors++;
                if (_enableLogging)
                    ChimeraLogger.LogError("HIST_DATA", $"Failed to execute historical query: {ex.Message}", null);
                return new List<CostDataPoint>();
            }
        }

        public List<CostDataPoint> GetHistoricalDataForType(MalfunctionType type, int? maxResults = null)
        {
            if (!_historicalByType.TryGetValue(type, out var typeData))
                return new List<CostDataPoint>();

            IEnumerable<CostDataPoint> query = typeData.OrderByDescending(h => h.Timestamp);
            if (maxResults.HasValue)
                query = query.Take(maxResults.Value);

            return query.ToList();
        }

        public List<CostDataPoint> GetHistoricalDataForEquipment(EquipmentType equipmentType, int? maxResults = null)
        {
            if (!_historicalByEquipment.TryGetValue(equipmentType, out var equipmentData))
                return new List<CostDataPoint>();

            IEnumerable<CostDataPoint> query = equipmentData.OrderByDescending(h => h.Timestamp);
            if (maxResults.HasValue)
                query = query.Take(maxResults.Value);

            return query.ToList();
        }

        public void LoadHistoricalData(List<CostDataPoint> historicalData)
        {
            try
            {
                _allCostHistory.Clear();
                _historicalByType.Clear();
                _historicalByEquipment.Clear();

                foreach (var dataPoint in historicalData.OrderBy(h => h.Timestamp))
                    AddCostDataPoint(dataPoint);

                _historicalStats.DataLoads++;

                if (_enableLogging)
                    ChimeraLogger.LogInfo("HIST_DATA", $"Loaded {historicalData.Count} historical data points", null);
            }
            catch (Exception ex)
            {
                _historicalStats.LoadErrors++;
                if (_enableLogging)
                    ChimeraLogger.LogError("HIST_DATA", $"Failed to load historical data: {ex.Message}", null);
            }
        }

        public HistoricalDataSummary GetDataSummary(DateTime startDate, DateTime endDate)
        {
            try
            {
                var periodData = _allCostHistory
                    .Where(h => h.Timestamp >= startDate && h.Timestamp <= endDate)
                    .ToList();

                if (periodData.Count == 0)
                {
                    return new HistoricalDataSummary
                    {
                        PeriodStart = startDate,
                        PeriodEnd = endDate,
                        TotalDataPoints = 0
                    };
                }

                var summary = new HistoricalDataSummary
                {
                    PeriodStart = startDate,
                    PeriodEnd = endDate,
                    TotalDataPoints = periodData.Count,
                    AverageCost = periodData.Average(h => h.ActualCost),
                    MinCost = periodData.Min(h => h.ActualCost),
                    MaxCost = periodData.Max(h => h.ActualCost),
                    AverageRepairTime = periodData.Average(h => h.ActualTime.TotalHours),
                    TotalCost = periodData.Sum(h => h.ActualCost),
                    TotalRepairTime = periodData.Sum(h => h.ActualTime.TotalHours)
                };

                var costVariance = periodData.Average(h => Math.Pow(h.ActualCost - summary.AverageCost, 2));
                summary.CostStandardDeviation = Math.Sqrt(costVariance);

                summary.MalfunctionTypeBreakdown = periodData
                    .GroupBy(h => h.MalfunctionType)
                    .ToDictionary(g => g.Key, g => g.Count());

                summary.EquipmentTypeBreakdown = periodData
                    .GroupBy(h => h.EquipmentType)
                    .ToDictionary(g => g.Key, g => g.Count());

                summary.SeverityBreakdown = periodData
                    .GroupBy(h => h.Severity)
                    .ToDictionary(g => g.Key, g => g.Count());

                OnDataSummaryUpdated?.Invoke(summary);
                return summary;
            }
            catch (Exception ex)
            {
                _historicalStats.AnalysisErrors++;
                if (_enableLogging)
                    ChimeraLogger.LogError("HIST_DATA", $"Failed to generate data summary: {ex.Message}", null);
                return new HistoricalDataSummary();
            }
        }

        public CostDistributionAnalysis GetCostDistribution(List<CostDataPoint> dataPoints)
        {
            if (dataPoints == null || dataPoints.Count == 0)
                return new CostDistributionAnalysis { SampleSize = 0 };

            try
            {
                var costs = dataPoints.Select(d => d.ActualCost).OrderBy(c => c).ToList();

                var analysis = new CostDistributionAnalysis
                {
                    SampleSize = costs.Count,
                    Mean = costs.Average(),
                    MinValue = costs.Min(),
                    MaxValue = costs.Max(),
                    Range = costs.Max() - costs.Min()
                };

                analysis.Median = GetPercentile(costs, 50);
                analysis.Mode = costs.GroupBy(c => c).OrderByDescending(g => g.Count()).First().Key;
                analysis.Percentile25 = GetPercentile(costs, 25);
                analysis.Percentile75 = GetPercentile(costs, 75);
                analysis.Percentile90 = GetPercentile(costs, 90);
                analysis.Percentile95 = GetPercentile(costs, 95);

                var variance = costs.Average(c => Math.Pow(c - analysis.Mean, 2));
                analysis.Variance = variance;
                analysis.StandardDeviation = Math.Sqrt(variance);

                return analysis;
            }
            catch (Exception ex)
            {
                _historicalStats.AnalysisErrors++;
                if (_enableLogging)
                    ChimeraLogger.LogError("HIST_DATA", $"Failed to calculate cost distribution: {ex.Message}", null);
                return new CostDistributionAnalysis { SampleSize = 0 };
            }
        }

        public void ClearOldData(int daysToKeep)
        {
            try
            {
                var cutoffDate = DateTime.Now.AddDays(-daysToKeep);
                var removedCount = _allCostHistory.RemoveAll(h => h.Timestamp < cutoffDate);

                foreach (var typeList in _historicalByType.Values)
                    typeList.RemoveAll(h => h.Timestamp < cutoffDate);

                foreach (var equipmentList in _historicalByEquipment.Values)
                    equipmentList.RemoveAll(h => h.Timestamp < cutoffDate);

                _historicalStats.DataClears++;

                if (_enableLogging)
                    ChimeraLogger.LogInfo("HIST_DATA", $"Cleared {removedCount} old data points (older than {daysToKeep} days)", null);
            }
            catch (Exception ex)
            {
                if (_enableLogging)
                    ChimeraLogger.LogError("HIST_DATA", $"Failed to clear old data: {ex.Message}", null);
            }
        }

        public List<CostDataPoint> GetAllHistoricalData()
        {
            return new List<CostDataPoint>(_allCostHistory);
        }

        public void ClearAllData()
        {
            _allCostHistory.Clear();
            _historicalByType.Clear();
            _historicalByEquipment.Clear();
            _historicalStats.DataClears++;

            if (_enableLogging)
                ChimeraLogger.LogInfo("HIST_DATA", "All historical data cleared", null);
        }

        private void PerformHistoryMaintenance()
        {
            try
            {
                var toRemove = _allCostHistory.Count - _maxHistorySize;
                if (toRemove <= 0) return;

                var oldestEntries = _allCostHistory.OrderBy(h => h.Timestamp).Take(toRemove).ToList();

                foreach (var entry in oldestEntries)
                {
                    _allCostHistory.Remove(entry);
                    _historicalByType[entry.MalfunctionType]?.Remove(entry);
                    _historicalByEquipment[entry.EquipmentType]?.Remove(entry);
                }

                _historicalStats.MaintenanceOperations++;
                _lastMaintenance = DateTime.Now;

                OnHistoryMaintenance?.Invoke(toRemove);

                if (_enableLogging)
                    ChimeraLogger.LogInfo("HIST_DATA", $"History maintenance: removed {toRemove} oldest entries", null);
            }
            catch (Exception ex)
            {
                _historicalStats.MaintenanceErrors++;
                if (_enableLogging)
                    ChimeraLogger.LogError("HIST_DATA", $"History maintenance failed: {ex.Message}", null);
            }
        }

        private float GetPercentile(List<float> sortedValues, int percentile)
        {
            if (sortedValues == null || sortedValues.Count == 0) return 0f;

            var index = (int)Math.Ceiling(percentile / 100.0 * sortedValues.Count) - 1;
            index = Mathf.Clamp(index, 0, sortedValues.Count - 1);

            return sortedValues[index];
        }
    }
}

