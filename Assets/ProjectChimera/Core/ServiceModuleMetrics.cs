using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core
{
    /// <summary>
    /// Metrics collection for service modules
    /// Tracks performance, usage, and health of service components
    /// </summary>
    [System.Serializable]
    public class ServiceModuleMetrics
    {
        public string ModuleName;
        public DateTime StartTime;
        public TimeSpan Uptime;
        public int TotalRequests;
        public int SuccessfulRequests;
        public int FailedRequests;
        public long TotalProcessingTime; // in milliseconds
        public int ActiveServices;
        public int TotalServices;
        public Dictionary<string, ServiceMetrics> ServiceMetrics = new();
        public List<PerformanceSnapshot> PerformanceHistory = new();

        // Additional properties for ChimeraServiceModule
        public string ModuleVersion = "1.0.0";
        public bool IsConfigured = false;
        public bool IsInitialized = false;
        public float ConfigurationTime = 0f;
        public float InitializationTime = 0f;
        public int DependencyCount = 0;
        public int ComponentCount = 0;

        /// <summary>
        /// Record a service request
        /// </summary>
        public void RecordRequest(string serviceName, bool success, long processingTimeMs)
        {
            TotalRequests++;
            if (success)
            {
                SuccessfulRequests++;
            }
            else
            {
                FailedRequests++;
            }

            TotalProcessingTime += processingTimeMs;

            if (!ServiceMetrics.ContainsKey(serviceName))
            {
                ServiceMetrics[serviceName] = new ServiceMetrics { ServiceName = serviceName };
            }

            ServiceMetrics[serviceName].RecordRequest(success, processingTimeMs);
        }

        /// <summary>
        /// Update uptime
        /// </summary>
        public void UpdateUptime()
        {
            Uptime = DateTime.Now - StartTime;
        }

        /// <summary>
        /// Add performance snapshot
        /// </summary>
        public void AddPerformanceSnapshot()
        {
            var snapshot = new PerformanceSnapshot
            {
                Timestamp = DateTime.Now,
                TotalRequests = TotalRequests,
                SuccessfulRequests = SuccessfulRequests,
                FailedRequests = FailedRequests,
                AverageResponseTime = GetAverageResponseTime(),
                MemoryUsage = GC.GetTotalMemory(false),
                ActiveServices = ActiveServices
            };

            PerformanceHistory.Add(snapshot);

            // Keep only last 100 snapshots
            if (PerformanceHistory.Count > 100)
            {
                PerformanceHistory.RemoveAt(0);
            }
        }

        /// <summary>
        /// Get success rate percentage
        /// </summary>
        public float GetSuccessRate()
        {
            return TotalRequests > 0 ? (float)SuccessfulRequests / TotalRequests * 100f : 0f;
        }

        /// <summary>
        /// Get average response time in milliseconds
        /// </summary>
        public float GetAverageResponseTime()
        {
            return TotalRequests > 0 ? (float)TotalProcessingTime / TotalRequests : 0f;
        }

        /// <summary>
        /// Get service health score (0-100)
        /// </summary>
        public float GetHealthScore()
        {
            if (TotalServices == 0) return 0f;

            float successRate = GetSuccessRate();
            float serviceAvailability = (float)ActiveServices / TotalServices * 100f;
            float averageResponseScore = Mathf.Max(0, 100 - GetAverageResponseTime() / 10f); // Penalize slow responses

            return (successRate + serviceAvailability + averageResponseScore) / 3f;
        }

        /// <summary>
        /// Get metrics summary
        /// </summary>
        public string GetSummary()
        {
            return $"{ModuleName}: {ActiveServices}/{TotalServices} services, {GetSuccessRate():F1}% success, {GetAverageResponseTime():F1}ms avg response";
        }
    }

    /// <summary>
    /// Individual service metrics
    /// </summary>
    [System.Serializable]
    public class ServiceMetrics
    {
        public string ServiceName;
        public int TotalRequests;
        public int SuccessfulRequests;
        public int FailedRequests;
        public long TotalProcessingTime; // in milliseconds
        public DateTime LastRequestTime;
        public int ErrorCount;
        public string LastErrorMessage;

        /// <summary>
        /// Record a service request
        /// </summary>
        public void RecordRequest(bool success, long processingTimeMs)
        {
            TotalRequests++;
            if (success)
            {
                SuccessfulRequests++;
            }
            else
            {
                FailedRequests++;
                ErrorCount++;
            }

            TotalProcessingTime += processingTimeMs;
            LastRequestTime = DateTime.Now;
        }

        /// <summary>
        /// Record an error
        /// </summary>
        public void RecordError(string errorMessage)
        {
            ErrorCount++;
            LastErrorMessage = errorMessage;
            FailedRequests++;
        }

        /// <summary>
        /// Get success rate
        /// </summary>
        public float GetSuccessRate()
        {
            return TotalRequests > 0 ? (float)SuccessfulRequests / TotalRequests * 100f : 0f;
        }

        /// <summary>
        /// Get average response time
        /// </summary>
        public float GetAverageResponseTime()
        {
            return TotalRequests > 0 ? (float)TotalProcessingTime / TotalRequests : 0f;
        }

        /// <summary>
        /// Get service health score
        /// </summary>
        public float GetHealthScore()
        {
            float successRate = GetSuccessRate();
            float errorPenalty = Mathf.Min(ErrorCount * 5f, 50f); // Max 50 point penalty
            return Mathf.Max(0, successRate - errorPenalty);
        }
    }

    /// <summary>
    /// Performance snapshot for historical tracking
    /// </summary>
    [System.Serializable]
    public class PerformanceSnapshot
    {
        public DateTime Timestamp;
        public int TotalRequests;
        public int SuccessfulRequests;
        public int FailedRequests;
        public float AverageResponseTime;
        public long MemoryUsage;
        public int ActiveServices;
    }
}
