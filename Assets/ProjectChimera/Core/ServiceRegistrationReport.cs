using System;
using System.Collections.Generic;
using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core
{
    /// <summary>
    /// Comprehensive service registration report for Project Chimera
    /// Contains detailed information about service health, dependencies, and performance metrics
    /// </summary>
    [System.Serializable]
    public class ServiceRegistrationReport
    {
        [Header("Report Metadata")]
        public DateTime GeneratedAt;
        public string Version = "1.0";
        public ServiceHealth OverallHealth = ServiceHealth.Unknown;

        [Header("System Status")]
        public bool IsBootstrapped;
        public bool ServiceManagerInitialized;

        [Header("Service Statistics")]
        public int TotalServices;
        public int RegisteredServices;
        public int CriticalServices;
        public int CriticalFailures;
        public int NullImplementations;
        public int Warnings;

        [Header("Validation Results")]
        public List<ServiceValidationResult> ValidationResults = new List<ServiceValidationResult>();

        [Header("Performance Metrics")]
        public ServiceManagerMetrics ServiceManagerMetrics;

        [Header("Issues and Errors")]
        public List<string> DependencyIssues = new List<string>();
        public List<string> Errors = new List<string>();
        public List<string> Recommendations = new List<string>();

        /// <summary>
        /// Calculate service health percentage (0-100)
        /// </summary>
        public float GetHealthPercentage()
        {
            if (TotalServices == 0) return 0f;
            
            var healthyServices = RegisteredServices - NullImplementations;
            return (float)healthyServices / TotalServices * 100f;
        }

        /// <summary>
        /// Get a summary string of the report
        /// </summary>
        public string GetSummary()
        {
            return $"Services: {RegisteredServices}/{TotalServices} registered, " +
                   $"Health: {OverallHealth} ({GetHealthPercentage():F1}%), " +
                   $"Critical Failures: {CriticalFailures}, " +
                   $"Warnings: {Warnings}";
        }

        /// <summary>
        /// Check if the service system is ready for production
        /// </summary>
        public bool IsProductionReady()
        {
            return OverallHealth == ServiceHealth.Healthy &&
                   CriticalFailures == 0 &&
                   GetHealthPercentage() >= 95f;
        }

        /// <summary>
        /// Get recommendations for improving service health
        /// </summary>
        public List<string> GenerateRecommendations()
        {
            var recommendations = new List<string>();

            if (CriticalFailures > 0)
            {
                recommendations.Add($"Address {CriticalFailures} critical service failures immediately");
            }

            if (NullImplementations > TotalServices * 0.5f)
            {
                recommendations.Add("High number of null implementations - consider implementing missing services");
            }

            if (!ServiceManagerInitialized)
            {
                recommendations.Add("ServiceManager not initialized - check bootstrap process");
            }

            if (DependencyIssues.Count > 0)
            {
                recommendations.Add("Resolve dependency issues to prevent cascading failures");
            }

            if (GetHealthPercentage() < 80f)
            {
                recommendations.Add("Service health below 80% - review service registrations");
            }

            return recommendations;
        }
    }

    /// <summary>
    /// Service health levels
    /// </summary>
    [System.Serializable]
    public enum ServiceHealth
    {
        Unknown = 0,
        Critical = 1,    // Multiple critical failures
        Warning = 2,     // Some critical failures or many warnings
        Healthy = 3      // All critical services working
    }

    /// <summary>
    /// Extended service validation result with additional diagnostic information
    /// </summary>
    [System.Serializable]
    public class ServiceDiagnosticInfo
    {
        public string ServiceName;
        public string InterfaceType;
        public string ImplementationType;
        public ServiceLifetime Lifetime;
        public DateTime LastResolved;
        public int ResolutionCount;
        public float AverageResolutionTime;
        public List<string> Dependencies = new List<string>();
        public List<string> Dependents = new List<string>();
        public bool HasCircularDependency;
        public string HealthStatus;
        public List<string> Issues = new List<string>();
    }
}