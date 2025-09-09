using System;
using System.Collections.Generic;
using UnityEngine;

namespace ProjectChimera.Systems.Analytics
{
    /// <summary>
    /// Comprehensive data types for the Analytics system
    /// Defines all DTOs and data structures used by analytics engines
    /// </summary>

    #region Anomaly Detection Types

    [Serializable]
    public class SystemHealthAlert
    {
        public string AlertId { get; set; }
        public string AlertType { get; set; }
        public string ComponentName { get; set; }
        public SystemHealthAlertLevel AlertLevel { get; set; }
        public float HealthScore { get; set; }
        public string Message { get; set; }
        public string Description { get; set; }
        public string Source { get; set; }
        public DateTime Timestamp { get; set; }
        public DateTime DetectedAt { get; set; }
        public List<string> RecommendedActions { get; set; } = new List<string>();
        public Dictionary<string, object> ContextData { get; set; } = new Dictionary<string, object>();
        public bool IsResolved { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public string Resolution { get; set; }
    }

    [Serializable]
    public class PerformanceAnomaly
    {
        public string AnomalyId { get; set; }
        public string MetricName { get; set; }
        public PerformanceAnomalySeverity Severity { get; set; }
        public float ExpectedValue { get; set; }
        public float ActualValue { get; set; }
        public float CurrentValue { get; set; }
        public float ThresholdValue { get; set; }
        public float Deviation { get; set; }
        public DateTime Timestamp { get; set; }
        public DateTime DetectedAt { get; set; }
        public string Description { get; set; }
        public Dictionary<string, object> Context { get; set; } = new Dictionary<string, object>();
        public bool IsResolved { get; set; }
        public string ResolutionStrategy { get; set; }
    }

    [Serializable]
    public class SystemHealthProfile
    {
        public string ProfileId { get; set; }
        public string SystemName { get; set; }
        public string ComponentName { get; set; }
        public SystemHealthAlertLevel CurrentHealthLevel { get; set; }
        public float OverallScore { get; set; }
        public Dictionary<string, float> ComponentScores { get; set; } = new Dictionary<string, float>();
        public Dictionary<string, float> BaselineMetrics { get; set; } = new Dictionary<string, float>();
        public Dictionary<string, float> ThresholdLevels { get; set; } = new Dictionary<string, float>();
        public List<SystemHealthAlert> ActiveAlerts { get; set; } = new List<SystemHealthAlert>();
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdated { get; set; }
        public bool IsActive { get; set; } = true;
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public enum SystemHealthAlertLevel
    {
        Low,
        Medium,
        High,
        Info,
        Warning,
        Critical,
        Fatal
    }

    public enum PerformanceAnomalySeverity
    {
        Low,
        Medium,
        High,
        Critical
    }


    #endregion

    #region Behavior Analysis Types

    [Serializable]
    public class PlayerJourneyInsight
    {
        public string InsightId { get; set; }
        public string PlayerId { get; set; }
        public string InsightType { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
        public DateTime GeneratedAt { get; set; }
        public DateTime AnalyzedAt { get; set; }
        public DateTime FirstSessionDate { get; set; }
        public DateTime LastSessionDate { get; set; }
        public int TotalSessions { get; set; }
        public TimeSpan TotalPlayTime { get; set; }
        public List<string> MostUsedFeatures { get; set; } = new List<string>();
        public List<string> ProgressionMilestones { get; set; } = new List<string>();
        public string EngagementTrend { get; set; }
        public float ChurnRisk { get; set; }
        public float Confidence { get; set; }
        public List<string> Recommendations { get; set; } = new List<string>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    #endregion

    #region Predictive Analytics Types

    [Serializable]
    public class TrendForecast
    {
        public string ForecastId { get; set; }
        public string MetricName { get; set; }
        public string DataSource { get; set; }
        public string ForecastType { get; set; }
        public TimeSpan ForecastWindow { get; set; }
        public List<DataPoint> HistoricalData { get; set; } = new List<DataPoint>();
        public List<DataPoint> HistoricalDataPoints { get; set; } = new List<DataPoint>();
        public List<DataPoint> ForecastedData { get; set; } = new List<DataPoint>();
        public List<float> ForecastedValues { get; set; } = new List<float>();
        public string TrendDirection { get; set; }
        public Dictionary<string, float> ConfidenceInterval { get; set; } = new Dictionary<string, float>();
        public float Seasonality { get; set; }
        public float Volatility { get; set; }
        public float Accuracy { get; set; }
        public DateTime GeneratedAt { get; set; }
        public TimeSpan ForecastHorizon { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        public string ModelUsed { get; set; }
    }

    [Serializable]
    public class MLInsight
    {
        public string InsightId { get; set; }
        public MLInsightType InsightType { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public float Confidence { get; set; }
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
        public List<string> KeyFindings { get; set; } = new List<string>();
        public Dictionary<string, float> FeatureImportance { get; set; } = new Dictionary<string, float>();
        public List<string> Recommendations { get; set; } = new List<string>();
        public string PredictedOutcome { get; set; }
        public DateTime GeneratedAt { get; set; }
        public string ModelId { get; set; }
        public List<string> SupportingEvidence { get; set; } = new List<string>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();

        public static MLInsight CreateDisabled(string reason)
        {
            return new MLInsight
            {
                InsightId = Guid.NewGuid().ToString(),
                InsightType = MLInsightType.General,
                Title = "ML Insight Generation Disabled",
                Description = reason,
                Confidence = 0.0f,
                GeneratedAt = DateTime.Now
            };
        }

        public static MLInsight CreateFailed(string errorMessage)
        {
            return new MLInsight
            {
                InsightId = Guid.NewGuid().ToString(),
                InsightType = MLInsightType.Alert,
                Title = "ML Insight Generation Failed",
                Description = errorMessage,
                Confidence = 0.0f,
                GeneratedAt = DateTime.Now
            };
        }
    }

    [Serializable]
    public class DataPoint
    {
        public DateTime Timestamp { get; set; }
        public float Value { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        public string Source { get; set; }
    }

    [Serializable]
    public class TrainingDataPoint
    {
        public List<float> Features { get; set; } = new List<float>();
        public float Target { get; set; }
        public DateTime Timestamp { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        public float Weight { get; set; } = 1.0f;
    }

    [Serializable]
    public class PredictionModel
    {
        public string ModelId { get; set; }
        public string ModelType { get; set; }
        public string Description { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        public List<string> FeatureNames { get; set; } = new List<string>();
        public List<string> InputFeatures { get; set; } = new List<string>();
        public string OutputClass { get; set; }
        public float Accuracy { get; set; }
        public float AccuracyScore { get; set; }
        public int TrainingDataSize { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime LastTrained { get; set; }
        public bool IsActive { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    public enum MLInsightType
    {
        Trend,
        Anomaly,
        Pattern,
        Prediction,
        Recommendation,
        Alert,
        PlayerBehavior,
        SystemPerformance,
        GameBalance,
        General
    }

    #endregion

    #region Reporting Types

    [Serializable]
    public class ReportSummary
    {
        public string ReportId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public ReportType ReportType { get; set; }
        public DateTime GeneratedAt { get; set; }
        public TimeRange TimeRange { get; set; }
        public TimeSpan TimeWindow { get; set; }
        public Dictionary<string, object> SummaryData { get; set; } = new Dictionary<string, object>();
        public List<string> KeyInsights { get; set; } = new List<string>();
        public List<string> KeyFindings { get; set; } = new List<string>();
        public float HealthScore { get; set; }
        public string TrendAnalysis { get; set; }
        public List<string> CriticalAlerts { get; set; } = new List<string>();
        public Dictionary<string, object> MetricsSummary { get; set; } = new Dictionary<string, object>();
        public List<string> Recommendations { get; set; } = new List<string>();
        public string TemplateUsed { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    [Serializable]
    public class ReportTemplate
    {
        public string TemplateId { get; set; }
        public string Name { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public ReportType ReportType { get; set; }
        public List<string> RequiredMetrics { get; set; } = new List<string>();
        public Dictionary<string, object> TemplateConfig { get; set; } = new Dictionary<string, object>();
        public string Layout { get; set; }
        public List<string> DefaultSections { get; set; } = new List<string>();
        public List<string> Sections { get; set; } = new List<string>();
        public TimeSpan GenerationInterval { get; set; }
        public AnalyticsPriority Priority { get; set; } = AnalyticsPriority.Normal;
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
    }

    #endregion

}
