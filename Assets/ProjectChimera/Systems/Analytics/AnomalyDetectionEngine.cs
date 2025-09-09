using UnityEngine;
using ProjectChimera.Core.Logging;
using System.Collections.Generic;
using System.Linq;
using System;

namespace ProjectChimera.Systems.Analytics
{
    /// <summary>
    /// Specialized engine for detecting anomalies in system behavior and data patterns.
    /// Handles real-time anomaly detection, alert generation, and system health monitoring.
    /// </summary>
    public class AnomalyDetectionEngine
    {
        private readonly AnalyticsCore _analyticsCore;
        private readonly AnomalyDetector _anomalyDetector;
        private readonly AlertManager _alertManager;
        
        // Anomaly tracking
        private List<AnomalyAlert> _detectedAnomalies = new List<AnomalyAlert>();
        private Dictionary<string, SystemHealthProfile> _systemHealthProfiles = new Dictionary<string, SystemHealthProfile>();
        private Queue<AnomalyAnalysisRequest> _anomalyAnalysisQueue = new Queue<AnomalyAnalysisRequest>();

        // Anomaly thresholds
        private float _predictionConfidenceThreshold;
        private int _maxAlertsPerMinute;

        // Events
        public event Action<AnomalyAlert> OnAnomalyDetected;
        public event Action<SystemHealthAlert> OnSystemHealthIssue;
        public event Action<PerformanceAnomaly> OnPerformanceAnomalyDetected;

        public AnomalyDetectionEngine(AnalyticsCore analyticsCore)
        {
            _analyticsCore = analyticsCore;
            _predictionConfidenceThreshold = 0.7f; // Default threshold
            _maxAlertsPerMinute = 10; // Default alert limit
            
            _anomalyDetector = new AnomalyDetector(_predictionConfidenceThreshold);
            _alertManager = new AlertManager(_maxAlertsPerMinute);
            
            // Configure components
            _anomalyDetector.EnableRealTimeDetection(_analyticsCore.EnableAnalytics);
            _alertManager.EnableAlerts(_analyticsCore.EnableAnomalyDetection);
            
            InitializeSystemHealthProfiles();
            
            ChimeraLogger.Log("[AnomalyDetectionEngine] Anomaly detection engine initialized");
        }

        private void InitializeSystemHealthProfiles()
        {
            // Initialize baseline profiles for different system components
            var systemComponents = new[] { "memory", "cpu", "fps", "network", "storage", "gameplay" };
            
            foreach (var component in systemComponents)
            {
                _systemHealthProfiles[component] = new SystemHealthProfile
                {
                    ComponentName = component,
                    CreatedAt = DateTime.UtcNow,
                    LastUpdated = DateTime.UtcNow,
                    BaselineMetrics = new Dictionary<string, float>(),
                    ThresholdLevels = GetDefaultThresholds(component),
                    IsActive = true
                };
            }
        }

        /// <summary>
        /// Detect anomalies in system behavior
        /// </summary>
        public AnomalyAlert DetectAnomalies(string dataSource, object[] dataPoints)
        {
            if (!_analyticsCore.EnableAnomalyDetection)
                return null;

            try
            {
                // Create synthetic event for anomaly detection
                var syntheticEvent = new AnalyticsEvent
                {
                    EventId = Guid.NewGuid().ToString(),
                    EventType = "data_anomaly",
                    Timestamp = DateTime.UtcNow,
                    Category = dataSource,
                    Properties = new Dictionary<string, object> { ["data_points"] = dataPoints }
                };

                var anomaly = _anomalyDetector.DetectAnomaly(syntheticEvent);

                if (anomaly != null)
                {
                    _detectedAnomalies.Add(anomaly);
                    _analyticsCore.UpdateMetrics(m => m.AnomaliesDetected++);
                    OnAnomalyDetected?.Invoke(anomaly);

                    // Generate alert if severity is high
                    if (anomaly.Severity >= AnomalySeverity.High)
                    {
                        CreateAnomalyAlert(anomaly, dataSource);
                    }

                    ChimeraLogger.Log($"[AnomalyDetectionEngine] Anomaly detected in {dataSource}: {anomaly.Description}");
                }

                return anomaly;
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[AnomalyDetectionEngine] Anomaly detection failed for {dataSource}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Monitor system performance for anomalies
        /// </summary>
        public PerformanceAnomaly MonitorSystemPerformance()
        {
            try
            {
                var performanceMetrics = CollectPerformanceMetrics();
                var anomaly = AnalyzePerformanceMetrics(performanceMetrics);
                
                if (anomaly != null)
                {
                    OnPerformanceAnomalyDetected?.Invoke(anomaly);
                    
                    if (anomaly.Severity >= PerformanceAnomalySeverity.High)
                    {
                        CreatePerformanceAlert(anomaly);
                    }
                }
                
                return anomaly;
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[AnomalyDetectionEngine] Performance monitoring failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Analyze system health trends
        /// </summary>
        public SystemHealthAlert AnalyzeSystemHealth(string componentName)
        {
            try
            {
                if (!_systemHealthProfiles.TryGetValue(componentName, out var profile))
                {
                    return null;
                }

                var currentMetrics = CollectComponentMetrics(componentName);
                var healthScore = CalculateHealthScore(profile, currentMetrics);
                
                if (healthScore < profile.ThresholdLevels["health_threshold"])
                {
                    var healthAlert = new SystemHealthAlert
                    {
                        ComponentName = componentName,
                        HealthScore = healthScore,
                        AlertLevel = DetermineAlertLevel(healthScore),
                        DetectedAt = DateTime.UtcNow,
                        Description = $"System health degraded for {componentName}: {healthScore:P0}",
                        RecommendedActions = GetHealthRecommendations(componentName, healthScore)
                    };
                    
                    OnSystemHealthIssue?.Invoke(healthAlert);
                    return healthAlert;
                }
                
                return null;
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[AnomalyDetectionEngine] System health analysis failed for {componentName}: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// Queue anomaly analysis request
        /// </summary>
        public void QueueAnomalyAnalysis(string dataSource, object[] dataPoints, AnomalyAnalysisType analysisType)
        {
            var request = new AnomalyAnalysisRequest
            {
                RequestId = Guid.NewGuid().ToString(),
                DataSource = dataSource,
                DataPoints = dataPoints,
                AnalysisType = analysisType,
                RequestedAt = DateTime.UtcNow
            };

            _anomalyAnalysisQueue.Enqueue(request);
        }

        /// <summary>
        /// Process queued anomaly analysis requests
        /// </summary>
        public void ProcessAnomalyAnalysisQueue()
        {
            var processedRequests = 0;
            const int maxRequestsPerFrame = 3;

            while (_anomalyAnalysisQueue.Count > 0 && processedRequests < maxRequestsPerFrame)
            {
                var request = _anomalyAnalysisQueue.Dequeue();

                try
                {
                    ProcessAnomalyAnalysisRequest(request);
                    processedRequests++;
                }
                catch (Exception ex)
                {
                    ChimeraLogger.LogError($"[AnomalyDetectionEngine] Failed to process anomaly analysis request: {ex.Message}");
                }
            }
        }

        private void ProcessAnomalyAnalysisRequest(AnomalyAnalysisRequest request)
        {
            switch (request.AnalysisType)
            {
                case AnomalyAnalysisType.DataAnomaly:
                    DetectAnomalies(request.DataSource, request.DataPoints);
                    break;
                    
                case AnomalyAnalysisType.PerformanceAnomaly:
                    MonitorSystemPerformance();
                    break;
                    
                case AnomalyAnalysisType.SystemHealth:
                    AnalyzeSystemHealth(request.DataSource);
                    break;
            }
        }

        private void CreateAnomalyAlert(AnomalyAlert anomaly, string dataSource)
        {
            _alertManager.CreateAlert(new AnalyticsAlert
            {
                AlertType = AlertType.Anomaly,
                Message = $"Anomaly detected in {dataSource}: {anomaly.Description}",
                Severity = (AlertSeverity)anomaly.Severity,
                Timestamp = DateTime.UtcNow,
                Data = anomaly
            });
        }

        private void CreatePerformanceAlert(PerformanceAnomaly anomaly)
        {
            _alertManager.CreateAlert(new AnalyticsAlert
            {
                AlertType = AlertType.Performance,
                Message = $"Performance anomaly detected: {anomaly.Description}",
                Severity = (AlertSeverity)anomaly.Severity,
                Timestamp = DateTime.UtcNow,
                Data = anomaly
            });
        }

        private Dictionary<string, float> CollectPerformanceMetrics()
        {
            return new Dictionary<string, float>
            {
                ["fps"] = 1f / Time.deltaTime,
                ["memory_usage"] = GC.GetTotalMemory(false) / (1024f * 1024f), // MB
                ["cpu_usage"] = GetCPUUsage(),
                ["load_time"] = GetAverageLoadTime(),
                ["frame_time"] = Time.deltaTime * 1000f // ms
            };
        }

        private PerformanceAnomaly AnalyzePerformanceMetrics(Dictionary<string, float> metrics)
        {
            // Check each metric against thresholds
            foreach (var metric in metrics)
            {
                var threshold = GetPerformanceThreshold(metric.Key);
                
                if (metric.Value > threshold)
                {
                    return new PerformanceAnomaly
                    {
                        MetricName = metric.Key,
                        CurrentValue = metric.Value,
                        ThresholdValue = threshold,
                        Severity = DeterminePerformanceSeverity(metric.Value, threshold),
                        DetectedAt = DateTime.UtcNow,
                        Description = $"{metric.Key} exceeded threshold: {metric.Value:F2} > {threshold:F2}"
                    };
                }
            }
            
            return null;
        }

        private Dictionary<string, float> CollectComponentMetrics(string componentName)
        {
            return componentName switch
            {
                "memory" => new Dictionary<string, float> { ["usage"] = GC.GetTotalMemory(false) / (1024f * 1024f) },
                "cpu" => new Dictionary<string, float> { ["usage"] = GetCPUUsage() },
                "fps" => new Dictionary<string, float> { ["current"] = 1f / Time.deltaTime },
                "network" => new Dictionary<string, float> { ["latency"] = GetNetworkLatency() },
                _ => new Dictionary<string, float>()
            };
        }

        private float CalculateHealthScore(SystemHealthProfile profile, Dictionary<string, float> currentMetrics)
        {
            if (currentMetrics.Count == 0)
                return 1.0f; // Perfect health if no metrics

            var totalScore = 0f;
            var metricCount = 0;

            foreach (var metric in currentMetrics)
            {
                var normalizedScore = NormalizeMetricScore(metric.Key, metric.Value);
                totalScore += normalizedScore;
                metricCount++;
            }

            return metricCount > 0 ? totalScore / metricCount : 1.0f;
        }

        private float NormalizeMetricScore(string metricName, float value)
        {
            // Normalize different metrics to 0-1 scale (1 = healthy, 0 = critical)
            return metricName switch
            {
                "usage" => Mathf.Clamp01(1f - (value / 1000f)), // Higher usage = lower health
                "latency" => Mathf.Clamp01(1f - (value / 500f)), // Higher latency = lower health
                "current" => Mathf.Clamp01(value / 60f), // Higher FPS = better health
                _ => 1.0f
            };
        }

        private SystemHealthAlertLevel DetermineAlertLevel(float healthScore)
        {
            return healthScore switch
            {
                < 0.3f => SystemHealthAlertLevel.Critical,
                < 0.5f => SystemHealthAlertLevel.High,
                < 0.7f => SystemHealthAlertLevel.Medium,
                _ => SystemHealthAlertLevel.Low
            };
        }

        private List<string> GetHealthRecommendations(string componentName, float healthScore)
        {
            var recommendations = new List<string>();
            
            switch (componentName)
            {
                case "memory":
                    recommendations.Add("Consider garbage collection");
                    recommendations.Add("Check for memory leaks");
                    break;
                    
                case "cpu":
                    recommendations.Add("Optimize performance-intensive operations");
                    recommendations.Add("Distribute workload across frames");
                    break;
                    
                case "fps":
                    recommendations.Add("Reduce graphical complexity");
                    recommendations.Add("Optimize rendering pipeline");
                    break;
            }
            
            return recommendations;
        }

        private Dictionary<string, float> GetDefaultThresholds(string componentName)
        {
            return componentName switch
            {
                "memory" => new Dictionary<string, float> { ["health_threshold"] = 0.7f, ["critical_threshold"] = 0.3f },
                "cpu" => new Dictionary<string, float> { ["health_threshold"] = 0.8f, ["critical_threshold"] = 0.4f },
                "fps" => new Dictionary<string, float> { ["health_threshold"] = 0.75f, ["critical_threshold"] = 0.5f },
                "network" => new Dictionary<string, float> { ["health_threshold"] = 0.8f, ["critical_threshold"] = 0.4f },
                _ => new Dictionary<string, float> { ["health_threshold"] = 0.7f, ["critical_threshold"] = 0.3f }
            };
        }

        private float GetPerformanceThreshold(string metricName)
        {
            return metricName switch
            {
                "fps" => 30f, // Minimum acceptable FPS
                "memory_usage" => 512f, // MB
                "cpu_usage" => 80f, // Percentage
                "load_time" => 5000f, // Milliseconds
                "frame_time" => 33.33f, // Milliseconds (30 FPS)
                _ => float.MaxValue
            };
        }

        private PerformanceAnomalySeverity DeterminePerformanceSeverity(float currentValue, float threshold)
        {
            var ratio = currentValue / threshold;
            
            return ratio switch
            {
                > 2f => PerformanceAnomalySeverity.Critical,
                > 1.5f => PerformanceAnomalySeverity.High,
                > 1.2f => PerformanceAnomalySeverity.Medium,
                _ => PerformanceAnomalySeverity.Low
            };
        }

        // Mock helper methods (would be implemented with real system monitoring)
        private float GetCPUUsage() => UnityEngine.Random.Range(20f, 80f);
        private float GetAverageLoadTime() => UnityEngine.Random.Range(1000f, 3000f);
        private float GetNetworkLatency() => UnityEngine.Random.Range(50f, 200f);

        // Event handlers
        public void OnDataEventCollected(DataEvent dataEvent)
        {
            // Convert to analytics event and check for anomalies in real-time
            var eventMetrics = ExtractEventMetrics(dataEvent);
            
            if (eventMetrics.Length > 0)
            {
                QueueAnomalyAnalysis(dataEvent.EventType, eventMetrics, AnomalyAnalysisType.DataAnomaly);
            }
        }

        public void OnSystemStateChanged(string systemId, SystemState newState)
        {
            var stateMetrics = new object[] { newState.Data };
            QueueAnomalyAnalysis($"system_state_{systemId}", stateMetrics, AnomalyAnalysisType.SystemHealth);
        }

        private object[] ExtractEventMetrics(DataEvent dataEvent)
        {
            if (dataEvent.Data is Dictionary<string, object> dataDict)
            {
                return dataDict.Values.ToArray();
            }
            
            return new object[] { dataEvent.Data };
        }

        public void CleanupOldData(int retentionDays)
        {
            var cutoffTime = DateTime.UtcNow.AddDays(-retentionDays);
            _detectedAnomalies.RemoveAll(a => a.DetectedAt < cutoffTime);
            
            // Update system health profile last updated times
            foreach (var profile in _systemHealthProfiles.Values)
            {
                if (DateTime.UtcNow - profile.LastUpdated > TimeSpan.FromDays(retentionDays))
                {
                    profile.LastUpdated = DateTime.UtcNow;
                }
            }
        }

        public int GetDetectedAnomaliesCount() => _detectedAnomalies.Count;
        public int GetQueueSize() => _anomalyAnalysisQueue.Count;
        public SystemHealthProfile GetSystemHealthProfile(string componentName) => 
            _systemHealthProfiles.TryGetValue(componentName, out var profile) ? profile : null;
    }

    public class AnomalyAnalysisRequest
    {
        public string RequestId { get; set; }
        public string DataSource { get; set; }
        public object[] DataPoints { get; set; }
        public AnomalyAnalysisType AnalysisType { get; set; }
        public DateTime RequestedAt { get; set; }
    }

    public enum AnomalyAnalysisType
    {
        DataAnomaly,
        PerformanceAnomaly,
        SystemHealth
    }
}