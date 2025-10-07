using System;
using System.Collections.Generic;

namespace ProjectChimera.Core.DI.Validation
{
    /// <summary>
    /// Validation status enumeration
    /// </summary>
    public enum ValidationStatus
    {
        Passed,
        Warning,
        Failed
    }

    /// <summary>
    /// Error severity levels
    /// </summary>
    public enum ErrorSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// Warning severity levels
    /// </summary>
    public enum WarningSeverity
    {
        Low,
        Medium,
        High,
        Critical
    }

    /// <summary>
    /// Validation results container
    /// </summary>
    [Serializable]
    public class ValidationResults
    {
        public bool IsValid;
        public ValidationStatus ValidationStatus;
        public DateTime ValidationTime;
        public string ValidatorVersion;

        public int TotalServicesRegistered;
        public int ValidatedServices;
        public int SingletonsFound;
        public int TransientsFound;
        public int ScopedServicesFound;

        public bool DependenciesValidated;
        public bool CircularDependenciesChecked;

        public List<ValidationError> Errors = new List<ValidationError>();
        public List<ValidationWarning> Warnings = new List<ValidationWarning>();
        public List<ValidationWarning> CriticalWarnings = new List<ValidationWarning>();
        public List<Type> MissingDependencies = new List<Type>();
        public List<List<Type>> CircularDependencies = new List<List<Type>>();

        public string GetSummary()
        {
            return $"Validation {ValidationStatus}: {ValidatedServices}/{TotalServicesRegistered} services validated, " +
                   $"{Errors.Count} errors, {Warnings.Count} warnings";
        }
    }

    /// <summary>
    /// Validation error details
    /// </summary>
    [Serializable]
    public class ValidationError
    {
        public ErrorSeverity Severity;
        public string ErrorType;
        public string Message;
        public string Context;
        public string StackTrace;
        public DateTime Timestamp = DateTime.Now;
    }

    /// <summary>
    /// Validation warning details
    /// </summary>
    [Serializable]
    public class ValidationWarning
    {
        public WarningSeverity Severity;
        public string WarningType;
        public string Message;
        public string Suggestion;
        public DateTime Timestamp = DateTime.Now;
    }

    /// <summary>
    /// Service health status
    /// </summary>
    public enum ServiceHealthStatus
    {
        Healthy,
        Degraded,
        Unhealthy,
        Unknown
    }

    /// <summary>
    /// Service health check result
    /// </summary>
    [Serializable]
    public class ServiceHealthCheck
    {
        public string ServiceName;
        public Type ServiceType;
        public ServiceHealthStatus Status;
        public string Message;
        public DateTime CheckTime;
        public TimeSpan ResponseTime;
        public Dictionary<string, string> Metadata = new Dictionary<string, string>();
    }

    /// <summary>
    /// Overall health report
    /// </summary>
    [Serializable]
    public class HealthReport
    {
        public ServiceHealthStatus OverallStatus;
        public DateTime ReportTime;
        public int TotalServices;
        public int HealthyServices;
        public int DegradedServices;
        public int UnhealthyServices;
        public List<ServiceHealthCheck> HealthChecks = new List<ServiceHealthCheck>();

        public string GetSummary()
        {
            return $"Overall: {OverallStatus} - {HealthyServices}/{TotalServices} healthy, " +
                   $"{DegradedServices} degraded, {UnhealthyServices} unhealthy";
        }
    }
}

