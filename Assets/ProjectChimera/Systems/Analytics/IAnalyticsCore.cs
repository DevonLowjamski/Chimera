using System;
using System.Collections.Generic;

namespace ProjectChimera.Systems.Analytics
{
    /// <summary>
    /// Core analytics interface for basic metric operations
    /// </summary>
    public interface IAnalyticsCore
    {
        bool IsEnabled { get; }
        float GetMetric(string metricName);
        Dictionary<string, float> GetCurrentMetrics();
        void RecordMetric(string metricName, float value, string facilityName = null);
        void ClearMetrics();
        void Initialize();
        void Shutdown();
        string GetCurrentMetricString();
    }
}
