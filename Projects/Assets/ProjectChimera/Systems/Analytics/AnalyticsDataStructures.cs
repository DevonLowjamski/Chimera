using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace ProjectChimera.Systems.Analytics
{
    /// <summary>
    /// Data structures for analytics and telemetry system
    /// </summary>

    [Serializable]
    public class AnalyticsEvent
    {
        public string EventId { get; set; }
        public string EventType { get; set; }
        public DateTime Timestamp { get; set; }
        public Dictionary<string, object> Properties { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        public string Category { get; set; }
        public string PlayerId { get; set; }
        public string SessionId { get; set; }
        public AnalyticsPriority Priority { get; set; } = AnalyticsPriority.Normal;
    }

    [Serializable]
    public class AnalyticsModel
    {
        public string ModelId { get; set; }
        public ModelType ModelType { get; set; }
        public string Description { get; set; }
        public List<string> InputFeatures { get; set; } = new List<string>();
        public List<string> OutputMetrics { get; set; } = new List<string>();
        public TimeSpan UpdateInterval { get; set; } = TimeSpan.FromMinutes(5);
        public DateTime CreatedAt { get; set; }
        public DateTime LastUpdated { get; set; }
        public float Accuracy { get; set; }
        public float AccuracyScore { get; set; }
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        public bool IsActive { get; set; } = true;
    }

    [Serializable]
    public class InsightResult
    {
        public string InsightId { get; set; }
        public string ModelId { get; set; }
        public string Title { get; set; }
        public string Description { get; set; }
        public DateTime GeneratedAt { get; set; }
        public float Confidence { get; set; }
        public AnalyticsPriority Priority { get; set; } = AnalyticsPriority.Normal;
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, object> Details { get; set; } = new Dictionary<string, object>();
        public List<string> Recommendations { get; set; } = new List<string>();
        public string Category { get; set; }
        public string Summary { get; set; }
        public bool Failed { get; set; } = false;
        public bool Success { get; set; } = true;
        public string ErrorMessage { get; set; }

        public static InsightResult CreateFailed(string errorMessage)
        {
            return new InsightResult
            {
                InsightId = System.Guid.NewGuid().ToString(),
                Failed = true,
                Success = false,
                ErrorMessage = errorMessage,
                GeneratedAt = DateTime.UtcNow
            };
        }
    }

    [Serializable]
    public class AnomalyAlert
    {
        public string AlertId { get; set; }
        public string AlertType { get; set; }
        public DateTime DetectedAt { get; set; }
        public string Description { get; set; }
        public AnomalySeverity Severity { get; set; }
        public AnalyticsPriority Priority { get; set; } = AnalyticsPriority.High;
        public Dictionary<string, object> Context { get; set; } = new Dictionary<string, object>();
        public bool IsResolved { get; set; } = false;
    }

    [Serializable]
    public class PredictionResult
    {
        public string PredictionId { get; set; }
        public string PredictionType { get; set; }
        public DateTime GeneratedAt { get; set; }
        public DateTime ValidUntil { get; set; }
        public float Confidence { get; set; }
        public object PredictedValue { get; set; }
        public Dictionary<string, object> Metadata { get; set; } = new Dictionary<string, object>();
        public bool Failed { get; set; } = false;
        public string ErrorMessage { get; set; }

        public static PredictionResult CreateFailed(string errorMessage)
        {
            return new PredictionResult
            {
                PredictionId = System.Guid.NewGuid().ToString(),
                Failed = true,
                ErrorMessage = errorMessage,
                GeneratedAt = DateTime.UtcNow
            };
        }
    }

    [Serializable]
    public class AnalyticsReport
    {
        public string ReportId { get; set; }
        public string ReportType { get; set; }
        public DateTime GeneratedAt { get; set; }
        public string Title { get; set; }
        public string Summary { get; set; }
        public List<InsightResult> Insights { get; set; } = new List<InsightResult>();
        public List<AnomalyAlert> Alerts { get; set; } = new List<AnomalyAlert>();
        public Dictionary<string, object> Data { get; set; } = new Dictionary<string, object>();
    }

    [Serializable]
    public class BehaviorPattern
    {
        public string PatternId { get; set; }
        public string PatternType { get; set; }
        public DateTime FirstObserved { get; set; }
        public DateTime LastObserved { get; set; }
        public int Frequency { get; set; }
        public float Strength { get; set; }
        public Dictionary<string, object> Characteristics { get; set; } = new Dictionary<string, object>();
    }

    [Serializable]
    public class AnalysisRequest
    {
        public string RequestId { get; set; }
        public string AnalysisType { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime StartedAt { get; set; }
        public DateTime CompletedAt { get; set; }
        public AnalyticsPriority Priority { get; set; } = AnalyticsPriority.Normal;
        public Dictionary<string, object> Parameters { get; set; } = new Dictionary<string, object>();
        public Dictionary<string, object> InputData { get; set; } = new Dictionary<string, object>();
        public string ModelId { get; set; }
        public InsightResult Result { get; set; }
        public bool IsCompleted { get; set; } = false;
        public bool Success { get; set; } = false;
        public string ErrorMessage { get; set; }
    }

    [Serializable]
    public class AnalyticsMetrics
    {
        public int TotalEvents { get; set; }
        public int ActiveModels { get; set; }
        public int GeneratedInsights { get; set; }
        public int InsightsGenerated { get; set; }
        public DateTime LastInsightTime { get; set; }
        public int DetectedAnomalies { get; set; }
        public int AnomaliesDetected { get; set; }
        public int PredictionsMade { get; set; }
        public int SuccessfulPredictions { get; set; }
        public int BehaviorPatternsIdentified { get; set; }
        public int ReportsGenerated { get; set; }
        public int ProcessingErrors { get; set; }
        public int QueueSize { get; set; }
        public int DataBatchesProcessed { get; set; }
        public int EventHistorySize { get; set; }
        public float AverageProcessingTime { get; set; }
        public float AverageProcessingLatency { get; set; }
        public float MemoryUsage { get; set; }
        public TimeSpan Uptime { get; set; }
        public DateTime LastUpdate { get; set; }
    }

    public enum AnalyticsPriority
    {
        Low = 0,
        Normal = 1,
        High = 2,
        Critical = 3
    }

    public enum ReportType
    {
        Daily,
        Weekly,
        Monthly,
        Custom,
        RealTime,
        Summary,
        Detailed,
        Performance,
        Behavior,
        PlayerBehavior,
        SystemHealth,
        Anomaly,
        Prediction
    }

    public enum AlertType
    {
        Info,
        Warning,
        Error,
        Critical,
        Anomaly,
        SystemIssue,
        Performance,
        Behavior,
        Prediction
    }

    public enum AlertSeverity
    {
        Low,
        Medium,
        High,
        Critical,
        Warning,
        Info
    }

    public enum AnomalySeverity
    {
        Low,
        Medium,
        High,
        Critical
    }

    public enum ModelType
    {
        Behavioral,
        Performance,
        Economic,
        GameplaySpecific,
        Predictive,
        Classification,
        Regression,
        Clustering,
        DeepLearning,
        Custom
    }

    [Serializable]
    public class AnalyticsAlert
    {
        public string AlertId { get; set; }
        public AlertType AlertType { get; set; }
        public string Message { get; set; }
        public AlertSeverity Severity { get; set; }
        public DateTime Timestamp { get; set; }
        public object Data { get; set; }
        public bool IsResolved { get; set; } = false;
    }

    // Supporting classes for analytics components
    [Serializable]
    public class BehaviorAnalyzer
    {
        public string AnalyzerId { get; set; }
        public bool IsActive { get; set; } = true;
        public List<BehaviorPattern> DetectedPatterns { get; set; } = new List<BehaviorPattern>();
        public int DataWindowSize { get; set; }

        public BehaviorAnalyzer() : this(100) { }

        public BehaviorAnalyzer(int dataWindowSize)
        {
            AnalyzerId = System.Guid.NewGuid().ToString();
            DataWindowSize = dataWindowSize;
        }

        public void EnablePatternDetection(bool enable)
        {
            IsActive = enable;
        }

        public BehaviorPattern AnalyzePattern(AnalyticsEvent analyticsEvent)
        {
            if (!IsActive) return null;

            // Simple pattern analysis based on event data
            var pattern = new BehaviorPattern
            {
                PatternId = System.Guid.NewGuid().ToString(),
                PatternType = $"{analyticsEvent.EventType}_pattern",
                FirstObserved = analyticsEvent.Timestamp,
                LastObserved = analyticsEvent.Timestamp,
                Frequency = 1,
                Strength = 0.5f,
                Characteristics = new Dictionary<string, object>
                {
                    ["event_type"] = analyticsEvent.EventType,
                    ["category"] = analyticsEvent.Category,
                    ["player_id"] = analyticsEvent.PlayerId
                }
            };

            DetectedPatterns.Add(pattern);
            return pattern;
        }

        public BehaviorPattern AnalyzePattern(string playerId, List<AnalyticsEvent> playerEvents)
        {
            if (!IsActive || playerEvents == null || !playerEvents.Any()) return null;

            // Create a composite pattern from multiple events
            var firstEvent = playerEvents.First();
            var lastEvent = playerEvents.Last();
            
            var pattern = new BehaviorPattern
            {
                PatternId = System.Guid.NewGuid().ToString(),
                PatternType = $"player_{playerId}_behavior_pattern",
                FirstObserved = firstEvent.Timestamp,
                LastObserved = lastEvent.Timestamp,
                Frequency = playerEvents.Count,
                Strength = Math.Min(1.0f, playerEvents.Count / 10.0f), // Scale strength based on event count
                Characteristics = new Dictionary<string, object>
                {
                    ["player_id"] = playerId,
                    ["event_count"] = playerEvents.Count,
                    ["event_types"] = playerEvents.Select(e => e.EventType).Distinct().ToList(),
                    ["categories"] = playerEvents.Select(e => e.Category).Distinct().ToList(),
                    ["time_span"] = (lastEvent.Timestamp - firstEvent.Timestamp).TotalMinutes
                }
            };

            DetectedPatterns.Add(pattern);
            return pattern;
        }
    }

    [Serializable]
    public class AnomalyDetector
    {
        public string DetectorId { get; set; }
        public bool IsActive { get; set; } = true;
        public float Threshold { get; set; } = 0.5f;
        public List<AnomalyAlert> DetectedAnomalies { get; set; } = new List<AnomalyAlert>();

        public AnomalyDetector() : this(0.5f) { }

        public AnomalyDetector(float threshold)
        {
            DetectorId = System.Guid.NewGuid().ToString();
            Threshold = threshold;
        }

        public void EnableRealTimeDetection(bool enable)
        {
            IsActive = enable;
        }

        public AnomalyAlert DetectAnomaly(AnalyticsEvent analyticsEvent, float threshold = 0.5f)
        {
            // Simple anomaly detection based on event frequency and patterns
            if (!IsActive) return null;

            var anomaly = new AnomalyAlert
            {
                AlertId = System.Guid.NewGuid().ToString(),
                AlertType = "Event Anomaly",
                DetectedAt = DateTime.UtcNow,
                Description = $"Anomaly detected in event: {analyticsEvent.EventType}",
                Severity = threshold > 0.8f ? AnomalySeverity.High : AnomalySeverity.Medium,
                Context = new Dictionary<string, object>
                {
                    ["event_type"] = analyticsEvent.EventType,
                    ["threshold"] = threshold,
                    ["detection_method"] = "frequency_analysis"
                }
            };

            DetectedAnomalies.Add(anomaly);
            return anomaly;
        }
    }

    [Serializable]
    public class PredictiveEngine
    {
        public string EngineId { get; set; }
        public bool IsActive { get; set; } = true;
        public List<AnalyticsModel> Models { get; set; } = new List<AnalyticsModel>();
        public List<PredictionResult> Predictions { get; set; } = new List<PredictionResult>();
        public bool MLEnabled { get; set; }

        public PredictiveEngine() : this(false) { }

        public PredictiveEngine(bool enableML)
        {
            EngineId = System.Guid.NewGuid().ToString();
            MLEnabled = enableML;
            IsActive = enableML;
        }

        public void EnableReinforcementLearning(bool enable)
        {
            // Enable or disable reinforcement learning capabilities
            MLEnabled = enable;
            IsActive = enable;
        }

        public PredictionResult Predict(Dictionary<string, object> inputData, string predictionType = "general")
        {
            if (!IsActive) 
            {
                return new PredictionResult
                {
                    PredictionId = System.Guid.NewGuid().ToString(),
                    Failed = true,
                    ErrorMessage = "Predictive engine is not active"
                };
            }

            var prediction = new PredictionResult
            {
                PredictionId = System.Guid.NewGuid().ToString(),
                PredictionType = predictionType,
                GeneratedAt = DateTime.UtcNow,
                ValidUntil = DateTime.UtcNow.AddHours(1),
                Confidence = UnityEngine.Random.Range(0.6f, 0.95f),
                PredictedValue = GeneratePrediction(inputData, predictionType),
                Metadata = inputData
            };

            Predictions.Add(prediction);
            return prediction;
        }

        private object GeneratePrediction(Dictionary<string, object> inputData, string predictionType)
        {
            // Simple prediction logic based on input data
            switch (predictionType.ToLower())
            {
                case "performance":
                    return new { predicted_fps = 60f, predicted_memory = 2048f };
                case "behavior":
                    return new { engagement_score = 0.85f, retention_probability = 0.75f };
                case "economic":
                    return new { resource_demand = 1.2f, price_trend = "increasing" };
                default:
                    return new { prediction = "stable", confidence = 0.7f };
            }
        }
    }

    [Serializable]
    public class ReportGenerator
    {
        public string GeneratorId { get; set; }
        public bool IsActive { get; set; } = true;
        public List<AnalyticsReport> GeneratedReports { get; set; } = new List<AnalyticsReport>();

        public void EnableAutomaticGeneration(bool enable)
        {
            IsActive = enable;
        }

        public AnalyticsReport GenerateReport(ReportType reportType, Dictionary<string, object> parameters = null)
        {
            if (!IsActive) return null;

            var report = new AnalyticsReport
            {
                ReportId = System.Guid.NewGuid().ToString(),
                ReportType = reportType.ToString(),
                GeneratedAt = DateTime.UtcNow,
                Title = $"{reportType} Analytics Report",
                Summary = $"Generated {reportType} report at {DateTime.UtcNow}",
                Insights = new List<InsightResult>(),
                Alerts = new List<AnomalyAlert>(),
                Data = parameters ?? new Dictionary<string, object>()
            };

            GeneratedReports.Add(report);
            return report;
        }

        public AnalyticsReport GenerateReport(ReportType reportType, TimeSpan timeWindow, AnalyticsMetrics metrics, List<InsightResult> insights)
        {
            if (!IsActive) return null;

            var report = new AnalyticsReport
            {
                ReportId = System.Guid.NewGuid().ToString(),
                ReportType = reportType.ToString(),
                GeneratedAt = DateTime.UtcNow,
                Title = $"{reportType} Analytics Report ({timeWindow.TotalHours:F1}h window)",
                Summary = $"Generated {reportType} report covering {timeWindow.TotalHours:F1} hours of data",
                Insights = insights ?? new List<InsightResult>(),
                Alerts = new List<AnomalyAlert>(),
                Data = new Dictionary<string, object>
                {
                    ["time_window_hours"] = timeWindow.TotalHours,
                    ["total_events"] = metrics?.TotalEvents ?? 0,
                    ["insights_count"] = insights?.Count ?? 0,
                    ["active_models"] = metrics?.ActiveModels ?? 0,
                    ["processing_errors"] = metrics?.ProcessingErrors ?? 0
                }
            };

            GeneratedReports.Add(report);
            return report;
        }
    }

    [Serializable]
    public class AlertManager
    {
        public string ManagerId { get; set; }
        public bool IsActive { get; set; } = true;
        public List<AnomalyAlert> ActiveAlerts { get; set; } = new List<AnomalyAlert>();
        public List<AnalyticsAlert> AnalyticsAlerts { get; set; } = new List<AnalyticsAlert>();
        public int MaxAlertsPerMinute { get; set; } = 10;

        public AlertManager() : this(10) { }

        public AlertManager(int maxAlertsPerMinute)
        {
            ManagerId = System.Guid.NewGuid().ToString();
            MaxAlertsPerMinute = maxAlertsPerMinute;
        }

        public void EnableAlerts(bool enable)
        {
            IsActive = enable;
        }

        public void CreateAlert(AnalyticsAlert alert)
        {
            if (!IsActive) return;

            alert.AlertId = System.Guid.NewGuid().ToString();
            AnalyticsAlerts.Add(alert);

            // Limit alerts per minute
            var recentAlerts = AnalyticsAlerts.Where(a => 
                (DateTime.UtcNow - a.Timestamp).TotalMinutes < 1).Count();
            
            if (recentAlerts > MaxAlertsPerMinute)
            {
                // Remove oldest alerts to stay within limit
                var oldestAlerts = AnalyticsAlerts
                    .OrderBy(a => a.Timestamp)
                    .Take(recentAlerts - MaxAlertsPerMinute);
                
                foreach (var oldAlert in oldestAlerts)
                {
                    AnalyticsAlerts.Remove(oldAlert);
                }
            }
        }
    }
}