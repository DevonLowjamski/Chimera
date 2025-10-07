// REFACTORED: Cost Historical Data Structures
// Extracted from CostHistoricalDataManager for better separation of concerns

using System;
using System.Collections.Generic;
using ProjectChimera.Data.Equipment;

namespace ProjectChimera.Systems.Equipment.Degradation.Database
{
    /// <summary>
    /// Statistics for historical data manager operations
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
}

