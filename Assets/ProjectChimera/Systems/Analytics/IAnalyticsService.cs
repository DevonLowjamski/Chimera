using System;
using System.Collections.Generic;

namespace ProjectChimera.Systems.Analytics
{
    /// <summary>
    /// Core analytics service interface
    /// </summary>
    public interface IAnalyticsService
    {
        void RecordMetric(string metricName, float value);
        float GetCurrentMetric(string metricName);
        Dictionary<string, float> GetAllMetrics();
        Dictionary<string, object> GetCurrentMetrics();
        Dictionary<string, object> GetCurrentMetrics(string category);
        List<MetricDataPoint> GetMetricHistory(string metricKey, TimeRange timeRange);
        List<Dictionary<string, object>> GetMetricHistory(string metricKey, int count, string category = null);
        void ClearMetrics();
        float GetMetric(string metricName);
    }

    /// <summary>
    /// Analytics manager interface
    /// </summary>
    public interface IAnalyticsManager
    {
        IAnalyticsService GetService();
        void Initialize();
        void Shutdown();
    }
}
