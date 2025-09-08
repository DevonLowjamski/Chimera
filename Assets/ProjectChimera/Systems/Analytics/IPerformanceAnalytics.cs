using System;
using System.Collections.Generic;

namespace ProjectChimera.Systems.Analytics
{
    /// <summary>
    /// Performance analytics interface for derived metrics and calculations
    /// </summary>
    public interface IPerformanceAnalytics
    {
        void UpdateDerivedMetrics();
        float CalculateEfficiencyMetric(string metricName);
        float GetAverageMetric(string metricName, TimeRange timeRange);
        float GetMaxMetric(string metricName, TimeRange timeRange);
        float GetMinMetric(string metricName, TimeRange timeRange);
        Dictionary<string, float> GetPerformanceSummary();
        List<MetricDataPoint> GetAverageMetricData(string metricName, TimeRange timeRange, DateTime startTime, DateTime endTime);
        List<MetricDataPoint> GetMaxMetricData(string metricName, TimeRange timeRange, DateTime startTime, DateTime endTime);
        List<MetricDataPoint> GetMinMetricData(string metricName, TimeRange timeRange, DateTime startTime, DateTime endTime);
    }
}
