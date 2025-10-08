using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using ProjectChimera.Core.Logging;
using ProjectChimera.Data.Equipment;

namespace ProjectChimera.Systems.Equipment.Degradation.Database
{
    /// <summary>
    /// REFACTORED: Cost Historical Data Manager - Focused historical data tracking and analysis
    /// Single Responsibility: Managing historical cost data, filtering, and data maintenance
    /// Extracted from CostDatabaseManager for better SRP compliance
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

        #region Initialization

        /// <summary>
        /// Initialize historical data manager
        /// </summary>
        public void Initialize()
        {
            _lastMaintenance = DateTime.Now;

            if (_enableLogging)
                ChimeraLogger.LogInfo("HIST_DATA", "Historical data manager initialized", null);
        }

        #endregion

        #region Data Management

        /// <summary>
        /// Add a new cost data point to history
        /// </summary>
        public void AddCostDataPoint(CostDataPoint dataPoint)
        {
            try
            {
                // Add to main history
                _allCostHistory.Add(dataPoint);

                // Add to type-specific history
                if (!_historicalByType.ContainsKey(dataPoint.MalfunctionType))
                {
                    _historicalByType[dataPoint.MalfunctionType] = new List<CostDataPoint>();
                }
                _historicalByType[dataPoint.MalfunctionType].Add(dataPoint);

                // Add to equipment-specific history
                if (!_historicalByEquipment.ContainsKey(dataPoint.EquipmentType))
                {
                    _historicalByEquipment[dataPoint.EquipmentType] = new List<CostDataPoint>();
                }
                _historicalByEquipment[dataPoint.EquipmentType].Add(dataPoint);

                // Update statistics
                _historicalStats.TotalDataPoints++;
                _historicalStats.LastDataPointAdded = dataPoint.Timestamp;

                // Check if maintenance is needed
                if (_allCostHistory.Count > _maxHistorySize)
                {
                    PerformHistoryMaintenance();
                }

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

        /// <summary>
        /// Get historical data with filtering options
        /// </summary>
        public List<CostDataPoint> GetHistoricalData(MalfunctionType? type = null, EquipmentType? equipmentType = null,
                                                    DateTime? startDate = null, DateTime? endDate = null,
                                                    MalfunctionSeverity? severity = null, int? maxResults = null)
        {
            try
            {
                IEnumerable<CostDataPoint> query = _allCostHistory;

                // Apply filters
                if (type.HasValue)
                {
                    query = query.Where(h => h.MalfunctionType == type.Value);
                }

                if (equipmentType.HasValue)
                {
                    query = query.Where(h => h.EquipmentType == equipmentType.Value);
                }

                if (severity.HasValue)
                {
                    query = query.Where(h => h.Severity == severity.Value);
                }

                if (startDate.HasValue)
                {
                    query = query.Where(h => h.Timestamp >= startDate.Value);
                }

                if (endDate.HasValue)
                {
                    query = query.Where(h => h.Timestamp <= endDate.Value);
                }

                // Sort by timestamp (newest first)
                query = query.OrderByDescending(h => h.Timestamp);

                // Apply result limit
                if (maxResults.HasValue)
                {
                    query = query.Take(maxResults.Value);
                }

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

        /// <summary>
        /// Get historical data for specific malfunction type
        /// </summary>
        public List<CostDataPoint> GetHistoricalDataForType(MalfunctionType type, int? maxResults = null)
        {
            if (!_historicalByType.TryGetValue(type, out var typeData))
                return new List<CostDataPoint>();

            IEnumerable<CostDataPoint> query = typeData.OrderByDescending(h => h.Timestamp);

            if (maxResults.HasValue)
                query = query.Take(maxResults.Value);

            return query.ToList();
        }

        /// <summary>
        /// Get historical data for specific equipment type
        /// </summary>
        public List<CostDataPoint> GetHistoricalDataForEquipment(EquipmentType equipmentType, int? maxResults = null)
        {
            if (!_historicalByEquipment.TryGetValue(equipmentType, out var equipmentData))
                return new List<CostDataPoint>();

            IEnumerable<CostDataPoint> query = equipmentData.OrderByDescending(h => h.Timestamp);

            if (maxResults.HasValue)
                query = query.Take(maxResults.Value);

            return query.ToList();
        }

        /// <summary>
        /// Load historical data from external source
        /// </summary>
        public void LoadHistoricalData(List<CostDataPoint> historicalData)
        {
            try
            {
                _allCostHistory.Clear();
                _historicalByType.Clear();
                _historicalByEquipment.Clear();

                foreach (var dataPoint in historicalData.OrderBy(h => h.Timestamp))
                {
                    AddCostDataPoint(dataPoint);
                }

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

        #endregion

        #region Data Analysis

        /// <summary>
        /// Get data summary for a specific period
        /// </summary>
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

                // Calculate cost standard deviation
                var costVariance = periodData.Average(h => Math.Pow(h.ActualCost - summary.AverageCost, 2));
                summary.CostStandardDeviation = Math.Sqrt(costVariance);

                // Group by malfunction type
                summary.MalfunctionTypeBreakdown = periodData
                    .GroupBy(h => h.MalfunctionType)
                    .ToDictionary(g => g.Key, g => g.Count());

                // Group by equipment type
                summary.EquipmentTypeBreakdown = periodData
                    .GroupBy(h => h.EquipmentType)
                    .ToDictionary(g => g.Key, g => g.Count());

                // Group by severity
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

                return new HistoricalDataSummary { PeriodStart = startDate, PeriodEnd = endDate };
            }
        }

        /// <summary>
        /// Get cost distribution analysis
        /// </summary>
        public CostDistributionAnalysis GetCostDistribution(MalfunctionType? type = null, EquipmentType? equipmentType = null)
        {
            try
            {
                var data = GetHistoricalData(type, equipmentType);

                if (data.Count == 0)
                {
                    return new CostDistributionAnalysis { SampleSize = 0 };
                }

                var costs = data.Select(d => d.ActualCost).OrderBy(c => c).ToList();

                var analysis = new CostDistributionAnalysis
                {
                    SampleSize = costs.Count,
                    Mean = costs.Average(),
                    Median = GetMedian(costs),
                    Mode = GetMode(costs),
                    MinValue = costs.Min(),
                    MaxValue = costs.Max(),
                    Range = costs.Max() - costs.Min()
                };

                // Calculate percentiles
                analysis.Percentile25 = GetPercentile(costs, 25);
                analysis.Percentile75 = GetPercentile(costs, 75);
                analysis.Percentile90 = GetPercentile(costs, 90);
                analysis.Percentile95 = GetPercentile(costs, 95);

                // Calculate standard deviation and variance
                var variance = costs.Average(c => Math.Pow(c - analysis.Mean, 2));
                analysis.StandardDeviation = Math.Sqrt(variance);
                analysis.Variance = variance;

                return analysis;
            }
            catch (Exception ex)
            {
                _historicalStats.AnalysisErrors++;

                if (_enableLogging)
                    ChimeraLogger.LogError("HIST_DATA", $"Failed to generate cost distribution analysis: {ex.Message}", null);

                return new CostDistributionAnalysis { SampleSize = 0 };
            }
        }

        #endregion

        #region Data Maintenance

        /// <summary>
        /// Perform history maintenance to manage data size
        /// </summary>
        public void PerformHistoryMaintenance()
        {
            try
            {
                var removedCount = 0;

                if (_allCostHistory.Count > _maxHistorySize)
                {
                    // Calculate how many items to remove
                    var itemsToRemove = _allCostHistory.Count - _maxHistorySize;

                    if (_compressHistoricalData)
                    {
                        // Compress older data instead of removing it entirely
                        removedCount = CompressOldData(itemsToRemove);
                    }
                    else
                    {
                        // Remove oldest entries
                        removedCount = RemoveOldestEntries(itemsToRemove);
                    }

                    // Clean up type and equipment specific histories
                    CleanupSpecificHistories();
                }

                _historicalStats.MaintenanceOperations++;
                _lastMaintenance = DateTime.Now;

                OnHistoryMaintenance?.Invoke(removedCount);

                if (_enableLogging)
                    ChimeraLogger.LogInfo("HIST_DATA", $"History maintenance completed: {removedCount} items processed", null);
            }
            catch (Exception ex)
            {
                _historicalStats.MaintenanceErrors++;

                if (_enableLogging)
                    ChimeraLogger.LogError("HIST_DATA", $"History maintenance failed: {ex.Message}", null);
            }
        }

        /// <summary>
        /// Compress old data by aggregating similar entries
        /// </summary>
        private int CompressOldData(int itemsToProcess)
        {
            var sortedHistory = _allCostHistory.OrderBy(h => h.Timestamp).ToList();
            var compressedCount = 0;

            // Group oldest entries by malfunction type and equipment type, then create aggregated entries
            var oldestEntries = sortedHistory.Take(itemsToProcess);
            var groupedEntries = oldestEntries
                .GroupBy(h => new { h.MalfunctionType, h.EquipmentType, Date = h.Timestamp.Date })
                .Where(g => g.Count() > 1) // Only compress groups with multiple entries
                .ToList();

            foreach (var group in groupedEntries)
            {
                var avgCost = group.Average(h => h.ActualCost);
                var avgTime = TimeSpan.FromTicks((long)group.Average(h => h.ActualTime.Ticks));
                var mostCommonSeverity = group.GroupBy(h => h.Severity).OrderByDescending(g => g.Count()).First().Key;

                // Create compressed entry
                var compressedEntry = new CostDataPoint
                {
                    Timestamp = group.Key.Date.AddHours(12), // Noon of that day
                    ActualCost = avgCost,
                    ActualTime = avgTime,
                    Severity = mostCommonSeverity,
                    EquipmentType = group.Key.EquipmentType,
                    MalfunctionType = group.Key.MalfunctionType
                };

                // Remove original entries
                foreach (var originalEntry in group)
                {
                    _allCostHistory.Remove(originalEntry);
                    compressedCount++;
                }

                // Add compressed entry
                _allCostHistory.Add(compressedEntry);
            }

            return compressedCount;
        }

        /// <summary>
        /// Remove oldest entries from history
        /// </summary>
        private int RemoveOldestEntries(int itemsToRemove)
        {
            var sortedHistory = _allCostHistory.OrderBy(h => h.Timestamp).ToList();
            var entriesToRemove = sortedHistory.Take(itemsToRemove).ToList();

            foreach (var entry in entriesToRemove)
            {
                _allCostHistory.Remove(entry);
            }

            return entriesToRemove.Count;
        }

        /// <summary>
        /// Clean up type and equipment specific histories
        /// </summary>
        private void CleanupSpecificHistories()
        {
            var allTimestamps = new HashSet<DateTime>(_allCostHistory.Select(h => h.Timestamp));

            // Clean malfunction type histories
            foreach (var typeHistory in _historicalByType.Values)
            {
                typeHistory.RemoveAll(h => !allTimestamps.Contains(h.Timestamp));
            }

            // Clean equipment type histories
            foreach (var equipmentHistory in _historicalByEquipment.Values)
            {
                equipmentHistory.RemoveAll(h => !allTimestamps.Contains(h.Timestamp));
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Calculate median value
        /// </summary>
        private float GetMedian(List<float> values)
        {
            var sortedValues = values.OrderBy(v => v).ToList();
            var count = sortedValues.Count;

            if (count % 2 == 0)
            {
                return (sortedValues[count / 2 - 1] + sortedValues[count / 2]) / 2f;
            }
            else
            {
                return sortedValues[count / 2];
            }
        }

        /// <summary>
        /// Calculate mode value (most frequent)
        /// </summary>
        private float GetMode(List<float> values)
        {
            // Round to nearest 10 for grouping
            var roundedValues = values.Select(v => Math.Round(v / 10f) * 10f);
            var groups = roundedValues.GroupBy(v => v);

            if (groups.Any())
            {
                return (float)groups.OrderByDescending(g => g.Count()).First().Key;
            }

            return 0f;
        }

        /// <summary>
        /// Calculate percentile value
        /// </summary>
        private float GetPercentile(List<float> sortedValues, int percentile)
        {
            var index = (percentile / 100f) * (sortedValues.Count - 1);
            var lower = (int)Math.Floor(index);
            var upper = (int)Math.Ceiling(index);

            if (lower == upper)
            {
                return sortedValues[lower];
            }

            var weight = index - lower;
            return sortedValues[lower] * (1 - weight) + sortedValues[upper] * weight;
        }

        /// <summary>
        /// Reset historical data statistics
        /// </summary>
        public void ResetStatistics()
        {
            _historicalStats = new HistoricalDataStatistics();

            if (_enableLogging)
                ChimeraLogger.LogInfo("HIST_DATA", "Historical data statistics reset", null);
        }

        /// <summary>
        /// Clear all historical data
        /// </summary>
        public void ClearAllHistory()
        {
            _allCostHistory.Clear();
            _historicalByType.Clear();
            _historicalByEquipment.Clear();
            _compressionQueue.Clear();

            _historicalStats.DataClears++;

            if (_enableLogging)
                ChimeraLogger.LogInfo("HIST_DATA", "All historical data cleared", null);
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Historical data statistics
    /// </summary>
    [System.Serializable]
    public class HistoricalDataStatistics
    {
        public int TotalDataPoints = 0;
        public int DataAddErrors = 0;
        public int HistoricalQueries = 0;
        public int QueryErrors = 0;
        public int DataLoads = 0;
        public int LoadErrors = 0;
        public int AnalysisErrors = 0;
        public int MaintenanceOperations = 0;
        public int MaintenanceErrors = 0;
        public int DataClears = 0;
        public DateTime LastDataPointAdded = DateTime.MinValue;
    }

    /// <summary>
    /// Historical data summary for a period
    /// </summary>
    [System.Serializable]
    public struct HistoricalDataSummary
    {
        public DateTime PeriodStart;
        public DateTime PeriodEnd;
        public int TotalDataPoints;
        public float AverageCost;
        public float MinCost;
        public float MaxCost;
        public float TotalCost;
        public double AverageRepairTime;
        public double TotalRepairTime;
        public double CostStandardDeviation;
        public Dictionary<MalfunctionType, int> MalfunctionTypeBreakdown;
        public Dictionary<EquipmentType, int> EquipmentTypeBreakdown;
        public Dictionary<MalfunctionSeverity, int> SeverityBreakdown;
    }

    /// <summary>
    /// Cost distribution analysis results
    /// </summary>
    [System.Serializable]
    public struct CostDistributionAnalysis
    {
        public int SampleSize;
        public float Mean;
        public float Median;
        public float Mode;
        public float MinValue;
        public float MaxValue;
        public float Range;
        public float Percentile25;
        public float Percentile75;
        public float Percentile90;
        public float Percentile95;
        public double StandardDeviation;
        public double Variance;
    }

    #endregion
}