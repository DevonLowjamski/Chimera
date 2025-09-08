using System.Collections.Generic;

namespace ProjectChimera.Systems.Analytics
{
    /// <summary>
    /// Reporting analytics interface for data retrieval and historical analysis
    /// </summary>
    public interface IReportingAnalytics
    {
        List<MetricDataPoint> GetMetricHistory(string metricName, TimeRange timeRange);
        List<MetricDataPoint> GetFilteredDataPoints(string metricName, TimeRange timeRange, string facilityName);
        void SetFacilityFilter(string facilityName);
        string GetCurrentFacilityFilter();
        List<string> GetAvailableFacilities();
        void RecordMetricForFacility(string facilityName, string metricName, float value);
        Dictionary<string, float> GetFacilityMetrics(string facilityName);
        void Initialize();
        void Shutdown();
    }
}
