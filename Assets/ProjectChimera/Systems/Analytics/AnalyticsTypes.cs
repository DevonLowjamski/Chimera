using System;

namespace ProjectChimera.Systems.Analytics
{
    /// <summary>
    /// Time range enumeration for analytics queries
    /// </summary>
    public enum TimeRange
    {
        LastHour,
        Last6Hours,
        Last12Hours,
        Last24Hours,
        Last3Days,
        LastWeek,
        Last2Weeks,
        LastMonth,
        Last3Months,
        Last6Months,
        LastYear,
        AllTime
    }

    /// <summary>
    /// Metric data point interface
    /// </summary>
    public interface IMetricDataPoint
    {
        string MetricName { get; set; }
        float Value { get; set; }
        DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Implementation of metric data point
    /// </summary>
    public class MetricDataPoint : IMetricDataPoint
    {
        public string MetricName { get; set; }
        public float Value { get; set; }
        public DateTime Timestamp { get; set; }
    }
}
