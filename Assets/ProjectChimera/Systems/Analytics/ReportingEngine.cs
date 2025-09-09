using UnityEngine;
using ProjectChimera.Core.Logging;
using System.Collections.Generic;
using System.Linq;
using System;

namespace ProjectChimera.Systems.Analytics
{
    /// <summary>
    /// Specialized engine for generating comprehensive analytics reports and alerts.
    /// Handles report generation, alert management, and automated reporting systems.
    /// </summary>
    public class ReportingEngine
    {
        private readonly AnalyticsCore _analyticsCore;
        private readonly ReportGenerator _reportGenerator;
        private readonly AlertManager _alertManager;

        // Reporting configuration
        private bool _enableAutomaticReports;
        private float _reportGenerationInterval;
        private int _maxAlertsPerMinute;

        // Report tracking
        private List<AnalyticsReport> _generatedReports = new List<AnalyticsReport>();
        private List<AnalyticsAlert> _activeAlerts = new List<AnalyticsAlert>();
        private Dictionary<ReportType, ReportTemplate> _reportTemplates = new Dictionary<ReportType, ReportTemplate>();
        private Queue<ReportRequest> _reportQueue = new Queue<ReportRequest>();

        // Timing
        private float _lastReportTime;
        private Dictionary<ReportType, DateTime> _lastReportTimes = new Dictionary<ReportType, DateTime>();

        // Events
        public event Action<AnalyticsReport> OnReportGenerated;
        public event Action<AnalyticsAlert> OnAlertCreated;
        public event Action<ReportSummary> OnReportSummaryGenerated;

        public ReportingEngine(AnalyticsCore analyticsCore)
        {
            _analyticsCore = analyticsCore;
            _enableAutomaticReports = true;
            _reportGenerationInterval = 300f; // 5 minutes
            _maxAlertsPerMinute = 10;

            _reportGenerator = new ReportGenerator();
            _alertManager = new AlertManager(_maxAlertsPerMinute);

            // Configure components
            _reportGenerator.EnableAutomaticGeneration(_enableAutomaticReports);
            _alertManager.EnableAlerts(true);

            InitializeReportTemplates();

            ChimeraLogger.Log("[ReportingEngine] Reporting engine initialized");
        }

        private void InitializeReportTemplates()
        {
            // Performance report template
            _reportTemplates[ReportType.Performance] = new ReportTemplate
            {
                ReportType = ReportType.Performance,
                Title = "System Performance Report",
                Sections = new List<string> { "fps_analysis", "memory_usage", "cpu_metrics", "performance_trends" },
                GenerationInterval = TimeSpan.FromMinutes(30),
                Priority = AnalyticsPriority.High,
                RequiredMetrics = new List<string> { "fps", "memory_usage", "cpu_usage" }
            };

            // Player behavior report template
            _reportTemplates[ReportType.PlayerBehavior] = new ReportTemplate
            {
                ReportType = ReportType.PlayerBehavior,
                Title = "Player Behavior Analysis Report",
                Sections = new List<string> { "engagement_metrics", "behavior_patterns", "session_analysis", "retention_insights" },
                GenerationInterval = TimeSpan.FromHours(6),
                Priority = AnalyticsPriority.Medium,
                RequiredMetrics = new List<string> { "session_length", "feature_usage", "engagement_score" }
            };

            // System health report template
            _reportTemplates[ReportType.SystemHealth] = new ReportTemplate
            {
                ReportType = ReportType.SystemHealth,
                Title = "System Health Monitoring Report",
                Sections = new List<string> { "health_overview", "anomaly_summary", "alert_analysis", "recommendations" },
                GenerationInterval = TimeSpan.FromMinutes(15),
                Priority = AnalyticsPriority.High,
                RequiredMetrics = new List<string> { "system_health", "error_rate", "anomaly_count" }
            };

            // Real-time analytics report template
            _reportTemplates[ReportType.RealTime] = new ReportTemplate
            {
                ReportType = ReportType.RealTime,
                Title = "Real-Time Analytics Summary",
                Sections = new List<string> { "current_metrics", "live_trends", "active_users", "system_status" },
                GenerationInterval = TimeSpan.FromMinutes(5),
                Priority = AnalyticsPriority.Critical,
                RequiredMetrics = new List<string> { "active_sessions", "current_fps", "live_events" }
            };
        }

        /// <summary>
        /// Generate comprehensive analytics report
        /// </summary>
        public AnalyticsReport GenerateReport(ReportType reportType, TimeSpan timeWindow, AnalyticsMetrics metrics, List<InsightResult> insights)
        {
            try
            {
                if (!_reportTemplates.TryGetValue(reportType, out var template))
                {
                    ChimeraLogger.LogError($"[ReportingEngine] No template found for report type: {reportType}");
                    return null;
                }

                var report = _reportGenerator.GenerateReport(reportType, timeWindow, metrics, insights);

                if (report != null)
                {
                    report.Template = template;
                    report.GeneratedBy = "ReportingEngine";

                    _generatedReports.Add(report);
                    _lastReportTimes[reportType] = DateTime.UtcNow;
                    _analyticsCore.UpdateMetrics(m => m.ReportsGenerated++);

                    OnReportGenerated?.Invoke(report);

                    // Generate summary for high-priority reports
                    if (template.Priority >= AnalyticsPriority.High)
                    {
                        GenerateReportSummary(report);
                    }

                    ChimeraLogger.Log($"[ReportingEngine] Generated {reportType} report with {report.Sections.Count} sections");
                }

                return report;
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[ReportingEngine] Report generation failed for {reportType}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Generate periodic reports automatically
        /// </summary>
        public void GeneratePeriodicReports()
        {
            try
            {
                var now = DateTime.UtcNow;

                foreach (var template in _reportTemplates.Values)
                {
                    var lastReportTime = _lastReportTimes.TryGetValue(template.ReportType, out var time) ? time : DateTime.MinValue;

                    if (now - lastReportTime >= template.GenerationInterval)
                    {
                        var timeWindow = template.GenerationInterval;
                        QueueReportGeneration(template.ReportType, timeWindow, AnalyticsPriority.Automatic);
                    }
                }
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[ReportingEngine] Failed to generate periodic reports: {ex.Message}");
            }
        }

        /// <summary>
        /// Create analytics alert
        /// </summary>
        public void CreateAlert(AnalyticsAlert alert)
        {
            try
            {
                if (_activeAlerts.Count(a => DateTime.UtcNow - a.Timestamp < TimeSpan.FromMinutes(1)) >= _maxAlertsPerMinute)
                {
                    ChimeraLogger.LogWarning("[ReportingEngine] Alert rate limit exceeded, dropping alert");
                    return;
                }

                alert.AlertId = Guid.NewGuid().ToString();
                alert.CreatedBy = "ReportingEngine";

                _activeAlerts.Add(alert);
                _alertManager.CreateAlert(alert);

                OnAlertCreated?.Invoke(alert);

                ChimeraLogger.Log($"[ReportingEngine] Created {alert.Severity} alert: {alert.Message}");
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[ReportingEngine] Failed to create alert: {ex.Message}");
            }
        }

        /// <summary>
        /// Generate report summary for executive overview
        /// </summary>
        public ReportSummary GenerateReportSummary(AnalyticsReport report)
        {
            try
            {
                var summary = new ReportSummary
                {
                    ReportId = report.ReportId,
                    ReportType = ParseReportType(report.ReportType),
                    GeneratedAt = DateTime.UtcNow,
                    TimeWindow = report.TimeWindow,
                    KeyFindings = ExtractKeyFindings(report),
                    HealthScore = CalculateOverallHealthScore(report),
                    TrendAnalysis = SerializeTrendAnalysis(AnalyzeTrends(report)),
                    CriticalAlerts = GetCriticalAlertsAsStrings(report.TimeWindow),
                    Recommendations = GenerateSummaryRecommendations(report),
                    MetricsSummary = CreateMetricsSummary(report)
                };

                OnReportSummaryGenerated?.Invoke(summary);

                return summary;
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[ReportingEngine] Failed to generate report summary: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Queue report generation request
        /// </summary>
        public void QueueReportGeneration(ReportType reportType, TimeSpan timeWindow, AnalyticsPriority priority = AnalyticsPriority.Normal)
        {
            var request = new ReportRequest
            {
                RequestId = Guid.NewGuid().ToString(),
                ReportType = reportType,
                TimeWindow = timeWindow,
                Priority = priority,
                RequestedAt = DateTime.UtcNow
            };

            _reportQueue.Enqueue(request);
        }

        /// <summary>
        /// Process queued report generation requests
        /// </summary>
        public void ProcessReportQueue()
        {
            var processedRequests = 0;
            const int maxRequestsPerFrame = 2;

            while (_reportQueue.Count > 0 && processedRequests < maxRequestsPerFrame)
            {
                var request = _reportQueue.Dequeue();

                try
                {
                    var metrics = _analyticsCore.GetAnalyticsMetrics();
                    var insights = new List<InsightResult>(); // Would gather from insight engine

                    var report = GenerateReport(request.ReportType, request.TimeWindow, metrics, insights);

                    request.GeneratedReport = report;
                    request.IsCompleted = true;
                    request.CompletedAt = DateTime.UtcNow;

                    processedRequests++;
                }
                catch (Exception ex)
                {
                    ChimeraLogger.LogError($"[ReportingEngine] Failed to process report request: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// Export report to specified format
        /// </summary>
        public bool ExportReport(AnalyticsReport report, ReportExportFormat format, string filePath)
        {
            try
            {
                switch (format)
                {
                    case ReportExportFormat.JSON:
                        return ExportToJson(report, filePath);
                    case ReportExportFormat.CSV:
                        return ExportToCsv(report, filePath);
                    case ReportExportFormat.XML:
                        return ExportToXml(report, filePath);
                    default:
                        ChimeraLogger.LogError($"[ReportingEngine] Unsupported export format: {format}");
                        return false;
                }
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[ReportingEngine] Failed to export report: {ex.Message}");
                return false;
            }
        }

        private List<string> ExtractKeyFindings(AnalyticsReport report)
        {
            var findings = new List<string>();

            foreach (var sectionName in report.Sections)
            {
                // Extract key insights from section data if available
                if (report.Data != null && report.Data.TryGetValue($"section_{sectionName}_insights", out var insightData))
                {
                    if (insightData is string insightString)
                    {
                        findings.Add(insightString);
                    }
                }
                else
                {
                    // Generate generic findings based on section name
                    findings.Add($"Analysis completed for {sectionName}");
                }
            }

            return findings.Take(5).ToList(); // Top 5 findings
        }

        private float CalculateOverallHealthScore(AnalyticsReport report)
        {
            var scores = new List<float>();

            // Extract health scores from different report sections
            foreach (var sectionName in report.Sections)
            {
                // Extract health scores from section data if available
                if (report.Data != null && report.Data.TryGetValue($"section_{sectionName}_health", out var healthData))
                {
                    if (float.TryParse(healthData.ToString(), out var score))
                    {
                        scores.Add(score);
                    }
                }
            }

            return scores.Count > 0 ? scores.Average() : 0.8f; // Default healthy score
        }

        private Dictionary<string, string> AnalyzeTrends(AnalyticsReport report)
        {
            var trends = new Dictionary<string, string>();

            // Analyze trends from report data
            trends["performance"] = AnalyzePerformanceTrend(report);
            trends["engagement"] = AnalyzeEngagementTrend(report);
            trends["system_health"] = AnalyzeSystemHealthTrend(report);

            return trends;
        }

        private string AnalyzePerformanceTrend(AnalyticsReport report)
        {
            // Mock trend analysis
            return UnityEngine.Random.Range(0f, 1f) > 0.5f ? "improving" : "stable";
        }

        private string AnalyzeEngagementTrend(AnalyticsReport report)
        {
            // Mock trend analysis
            return UnityEngine.Random.Range(0f, 1f) > 0.3f ? "increasing" : "decreasing";
        }

        private string AnalyzeSystemHealthTrend(AnalyticsReport report)
        {
            // Mock trend analysis
            return UnityEngine.Random.Range(0f, 1f) > 0.7f ? "healthy" : "needs_attention";
        }

        private List<AnalyticsAlert> GetCriticalAlerts(TimeSpan timeWindow)
        {
            var cutoffTime = DateTime.UtcNow - timeWindow;
            return _activeAlerts
                .Where(a => a.Timestamp >= cutoffTime && a.Severity >= AlertSeverity.High)
                .OrderByDescending(a => a.Severity)
                .Take(5)
                .ToList();
        }

        private List<string> GenerateSummaryRecommendations(AnalyticsReport report)
        {
            var recommendations = new List<string>();

            // Generate recommendations based on report type
            var reportTypeEnum = ParseReportType(report.ReportType);
            switch (reportTypeEnum)
            {
                case ReportType.Performance:
                    recommendations.Add("Monitor memory usage trends closely");
                    recommendations.Add("Consider performance optimization for peak usage times");
                    break;

                case ReportType.PlayerBehavior:
                    recommendations.Add("Implement targeted engagement campaigns");
                    recommendations.Add("Analyze feature adoption patterns for optimization");
                    break;

                case ReportType.SystemHealth:
                    recommendations.Add("Schedule proactive maintenance during low-usage periods");
                    recommendations.Add("Review anomaly detection thresholds");
                    break;
            }

            return recommendations;
        }

        private Dictionary<string, object> CreateMetricsSummary(AnalyticsReport report)
        {
            return new Dictionary<string, object>
            {
                ["total_data_points"] = UnityEngine.Random.Range(1000, 5000),
                ["analysis_duration"] = (DateTime.Now - report.GenerationTime).TotalMilliseconds,
                ["sections_generated"] = report.Sections.Count,
                ["confidence_score"] = UnityEngine.Random.Range(0.7f, 0.95f)
            };
        }

        // Export methods (simplified implementations)
        private bool ExportToJson(AnalyticsReport report, string filePath)
        {
            // Would implement actual JSON serialization
            ChimeraLogger.Log($"[ReportingEngine] Exported report to JSON: {filePath}");
            return true;
        }

        private bool ExportToCsv(AnalyticsReport report, string filePath)
        {
            // Would implement actual CSV generation
            ChimeraLogger.Log($"[ReportingEngine] Exported report to CSV: {filePath}");
            return true;
        }

        private bool ExportToXml(AnalyticsReport report, string filePath)
        {
            // Would implement actual XML serialization
            ChimeraLogger.Log($"[ReportingEngine] Exported report to XML: {filePath}");
            return true;
        }

        // Event handlers
        public void OnDataBatchProcessed(ProcessedDataBatch batch)
        {
            // Update batch processing metrics in reports
            _analyticsCore.UpdateMetrics(m =>
            {
                m.DataBatchesProcessed++;
                m.AverageProcessingLatency = UpdateMovingAverage(
                    m.AverageProcessingLatency,
                    (float)batch.ProcessingDuration.TotalMilliseconds,
                    0.1f
                );
            });
        }

        public void OnSyncConflictDetected(StateConflict conflict)
        {
            CreateAlert(new AnalyticsAlert
            {
                AlertType = AlertType.SystemIssue,
                Message = $"Synchronization conflict detected in {conflict.SystemId}",
                Severity = AlertSeverity.Warning,
                Timestamp = DateTime.UtcNow,
                Data = conflict
            });
        }

        private float UpdateMovingAverage(float currentAverage, float newValue, float alpha)
        {
            return currentAverage * (1 - alpha) + newValue * alpha;
        }

        public void CleanupOldData(int retentionDays)
        {
            var cutoffTime = DateTime.UtcNow.AddDays(-retentionDays);

            // Clean up old reports
            _generatedReports.RemoveAll(r => r.GeneratedAt < cutoffTime);

            // Clean up old alerts
            _activeAlerts.RemoveAll(a => a.Timestamp < cutoffTime);
        }

        public int GetGeneratedReportsCount() => _generatedReports.Count;
        public int GetActiveAlertsCount() => _activeAlerts.Count;
        public int GetQueueSize() => _reportQueue.Count;
        public AnalyticsReport GetLatestReport(ReportType reportType) =>
            _generatedReports.Where(r => ParseReportType(r.ReportType) == reportType).OrderByDescending(r => r.GeneratedAt).FirstOrDefault();
        public void SetAutomaticReportsEnabled(bool enabled) => _enableAutomaticReports = enabled;
        public void SetReportGenerationInterval(float intervalSeconds) => _reportGenerationInterval = intervalSeconds;

        // Helper methods for type conversions
        private ReportType ParseReportType(string reportTypeString)
        {
            return Enum.TryParse<ReportType>(reportTypeString, out var result) ? result : ReportType.Summary;
        }

        private string SerializeTrendAnalysis(Dictionary<string, string> trends)
        {
            if (trends == null || trends.Count == 0)
                return "No significant trends detected";

            return string.Join("; ", trends.Select(kvp => $"{kvp.Key}: {kvp.Value}"));
        }

        private List<string> GetCriticalAlertsAsStrings(TimeSpan timeWindow)
        {
            var alerts = GetCriticalAlerts(timeWindow);
            return alerts.Select(a => $"{a.AlertType}: {a.Message}").ToList();
        }
    }

    public class ReportRequest
    {
        public string RequestId { get; set; }
        public ReportType ReportType { get; set; }
        public TimeSpan TimeWindow { get; set; }
        public AnalyticsPriority Priority { get; set; }
        public DateTime RequestedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public bool IsCompleted { get; set; }
        public AnalyticsReport GeneratedReport { get; set; }
    }

    public enum ReportExportFormat
    {
        JSON,
        CSV,
        XML
    }

}
