using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using UnityEngine;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core.DI.Validation
{
    /// <summary>
    /// Service Health Monitor
    /// Monitors service health, performance, and availability
    /// Part of Phase 0 - Service Validation implementation
    /// </summary>
    public class ServiceHealthMonitor
    {
        private readonly ServiceContainer _container;
        private readonly bool _enableLogging;
        private readonly float _checkInterval = 30f; // seconds

        private Dictionary<Type, ServiceHealthCheck> _lastHealthChecks = new Dictionary<Type, ServiceHealthCheck>();
        private Dictionary<Type, List<TimeSpan>> _responseTimeHistory = new Dictionary<Type, List<TimeSpan>>();
        private Dictionary<Type, int> _failureCount = new Dictionary<Type, int>();

        private float _lastCheckTime;
        private HealthReport _lastHealthReport;

        public HealthReport LastHealthReport => _lastHealthReport;
        public float CheckInterval => _checkInterval;

        public ServiceHealthMonitor(ServiceContainer container, bool enableLogging = true, float checkInterval = 30f)
        {
            _container = container ?? throw new ArgumentNullException(nameof(container));
            _enableLogging = enableLogging;
            _checkInterval = checkInterval;
        }

        /// <summary>
        /// Perform health check on all registered services
        /// </summary>
        public HealthReport PerformHealthCheck()
        {
            var report = new HealthReport
            {
                ReportTime = DateTime.Now,
                OverallStatus = ServiceHealthStatus.Healthy
            };

            try
            {
                var registrations = _container.GetRegistrations().Values.ToList();
                report.TotalServices = registrations.Count;

                foreach (var registration in registrations)
                {
                    var healthCheck = CheckServiceHealth(registration);
                    report.HealthChecks.Add(healthCheck);

                    switch (healthCheck.Status)
                    {
                        case ServiceHealthStatus.Healthy:
                            report.HealthyServices++;
                            break;
                        case ServiceHealthStatus.Degraded:
                            report.DegradedServices++;
                            if (report.OverallStatus == ServiceHealthStatus.Healthy)
                                report.OverallStatus = ServiceHealthStatus.Degraded;
                            break;
                        case ServiceHealthStatus.Unhealthy:
                            report.UnhealthyServices++;
                            report.OverallStatus = ServiceHealthStatus.Unhealthy;
                            break;
                    }

                    _lastHealthChecks[registration.ServiceType] = healthCheck;
                }

                _lastHealthReport = report;
                _lastCheckTime = Time.time;

                if (_enableLogging)
                {
                    LogHealthReport(report);
                }
            }
            catch (Exception ex)
            {
                report.OverallStatus = ServiceHealthStatus.Unhealthy;
                if (_enableLogging)
                {
                    ChimeraLogger.LogError("HEALTH", $"Health check failed: {ex.Message}", null);
                }
            }

            return report;
        }

        /// <summary>
        /// Check health of a specific service
        /// </summary>
        public ServiceHealthCheck CheckServiceHealth(ServiceRegistrationData registration)
        {
            var healthCheck = new ServiceHealthCheck
            {
                ServiceName = registration.ServiceType.Name,
                ServiceType = registration.ServiceType,
                CheckTime = DateTime.Now,
                Status = ServiceHealthStatus.Healthy
            };

            var stopwatch = Stopwatch.StartNew();

            try
            {
                // 1. Check if service can be resolved
                if (!_container.IsRegistered(registration.ServiceType))
                {
                    healthCheck.Status = ServiceHealthStatus.Unhealthy;
                    healthCheck.Message = "Service not registered";
                    return healthCheck;
                }

                // 2. Attempt to resolve the service
                try
                {
                    var instance = _container.Resolve(registration.ServiceType);

                    if (instance == null)
                    {
                        healthCheck.Status = ServiceHealthStatus.Unhealthy;
                        healthCheck.Message = "Service resolved to null";
                        return healthCheck;
                    }

                    healthCheck.Metadata["InstanceType"] = instance.GetType().Name;

                    // 3. Check if service is IHealthCheckable
                    if (instance is IServiceHealthCheckable healthCheckable)
                    {
                        var serviceHealth = healthCheckable.PerformHealthCheck();
                        healthCheck.Status = serviceHealth.Status;
                        healthCheck.Message = serviceHealth.Message;
                        healthCheck.Metadata["CustomHealthCheck"] = "true";
                    }
                    else
                    {
                        healthCheck.Message = "Service resolved successfully";
                    }

                    // 4. Check for MonoBehaviour lifecycle issues
                    if (instance is MonoBehaviour monoBehaviour)
                    {
                        if (monoBehaviour == null || monoBehaviour.gameObject == null)
                        {
                            healthCheck.Status = ServiceHealthStatus.Unhealthy;
                            healthCheck.Message = "MonoBehaviour or GameObject is destroyed";
                        }
                        else if (!monoBehaviour.gameObject.activeInHierarchy)
                        {
                            healthCheck.Status = ServiceHealthStatus.Degraded;
                            healthCheck.Message = "MonoBehaviour GameObject is inactive";
                        }
                    }
                }
                catch (Exception resolveEx)
                {
                    healthCheck.Status = ServiceHealthStatus.Unhealthy;
                    healthCheck.Message = $"Failed to resolve: {resolveEx.Message}";
                    IncrementFailureCount(registration.ServiceType);
                }

                stopwatch.Stop();
                healthCheck.ResponseTime = stopwatch.Elapsed;

                // Record response time history
                RecordResponseTime(registration.ServiceType, healthCheck.ResponseTime);

                // Check for slow response times
                if (healthCheck.ResponseTime.TotalMilliseconds > 100 && healthCheck.Status == ServiceHealthStatus.Healthy)
                {
                    healthCheck.Status = ServiceHealthStatus.Degraded;
                    healthCheck.Message = $"Slow response time: {healthCheck.ResponseTime.TotalMilliseconds:F2}ms";
                }

                // Check failure history
                if (GetFailureCount(registration.ServiceType) > 3 && healthCheck.Status == ServiceHealthStatus.Healthy)
                {
                    healthCheck.Status = ServiceHealthStatus.Degraded;
                    healthCheck.Message = $"Recent failures detected ({GetFailureCount(registration.ServiceType)} in history)";
                }

                healthCheck.Metadata["Lifetime"] = registration.Lifetime.ToString();
                healthCheck.Metadata["AvgResponseTime"] = $"{GetAverageResponseTime(registration.ServiceType).TotalMilliseconds:F2}ms";
                healthCheck.Metadata["FailureCount"] = GetFailureCount(registration.ServiceType).ToString();
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                healthCheck.ResponseTime = stopwatch.Elapsed;
                healthCheck.Status = ServiceHealthStatus.Unhealthy;
                healthCheck.Message = $"Health check exception: {ex.Message}";
                IncrementFailureCount(registration.ServiceType);
            }

            return healthCheck;
        }

        /// <summary>
        /// Get health check for a specific service type
        /// </summary>
        public ServiceHealthCheck GetServiceHealth(Type serviceType)
        {
            if (_lastHealthChecks.TryGetValue(serviceType, out var healthCheck))
                return healthCheck;

            return new ServiceHealthCheck
            {
                ServiceName = serviceType.Name,
                ServiceType = serviceType,
                Status = ServiceHealthStatus.Unknown,
                Message = "No health check performed yet",
                CheckTime = DateTime.MinValue
            };
        }

        /// <summary>
        /// Update health monitoring (call from game loop)
        /// </summary>
        public void Update(float deltaTime)
        {
            if (Time.time - _lastCheckTime >= _checkInterval)
            {
                PerformHealthCheck();
            }
        }

        /// <summary>
        /// Get all unhealthy services
        /// </summary>
        public List<ServiceHealthCheck> GetUnhealthyServices()
        {
            return _lastHealthChecks.Values
                .Where(hc => hc.Status == ServiceHealthStatus.Unhealthy)
                .ToList();
        }

        /// <summary>
        /// Get all degraded services
        /// </summary>
        public List<ServiceHealthCheck> GetDegradedServices()
        {
            return _lastHealthChecks.Values
                .Where(hc => hc.Status == ServiceHealthStatus.Degraded)
                .ToList();
        }

        /// <summary>
        /// Reset failure count for a service
        /// </summary>
        public void ResetFailureCount(Type serviceType)
        {
            if (_failureCount.ContainsKey(serviceType))
                _failureCount[serviceType] = 0;
        }

        /// <summary>
        /// Clear all health history
        /// </summary>
        public void ClearHistory()
        {
            _lastHealthChecks.Clear();
            _responseTimeHistory.Clear();
            _failureCount.Clear();
            _lastHealthReport = null;
        }

        private void RecordResponseTime(Type serviceType, TimeSpan responseTime)
        {
            if (!_responseTimeHistory.ContainsKey(serviceType))
                _responseTimeHistory[serviceType] = new List<TimeSpan>();

            _responseTimeHistory[serviceType].Add(responseTime);

            // Keep only last 20 entries
            if (_responseTimeHistory[serviceType].Count > 20)
                _responseTimeHistory[serviceType].RemoveAt(0);
        }

        private TimeSpan GetAverageResponseTime(Type serviceType)
        {
            if (!_responseTimeHistory.ContainsKey(serviceType) || _responseTimeHistory[serviceType].Count == 0)
                return TimeSpan.Zero;

            var average = _responseTimeHistory[serviceType].Average(ts => ts.TotalMilliseconds);
            return TimeSpan.FromMilliseconds(average);
        }

        private void IncrementFailureCount(Type serviceType)
        {
            if (!_failureCount.ContainsKey(serviceType))
                _failureCount[serviceType] = 0;

            _failureCount[serviceType]++;
        }

        private int GetFailureCount(Type serviceType)
        {
            return _failureCount.ContainsKey(serviceType) ? _failureCount[serviceType] : 0;
        }

        private void LogHealthReport(HealthReport report)
        {
            ChimeraLogger.LogInfo("HEALTH", "=== SERVICE HEALTH REPORT ===", null);
            ChimeraLogger.LogInfo("HEALTH", report.GetSummary(), null);

            if (report.UnhealthyServices > 0)
            {
                ChimeraLogger.LogError("HEALTH", "UNHEALTHY SERVICES:", null);
                foreach (var check in report.HealthChecks.Where(c => c.Status == ServiceHealthStatus.Unhealthy))
                {
                    ChimeraLogger.LogError("HEALTH", $"  {check.ServiceName}: {check.Message}", null);
                }
            }

            if (report.DegradedServices > 0 && _enableLogging)
            {
                ChimeraLogger.LogWarning("HEALTH", "DEGRADED SERVICES:");
                foreach (var check in report.HealthChecks.Where(c => c.Status == ServiceHealthStatus.Degraded))
                {
                    ChimeraLogger.LogWarning("HEALTH", $"  {check.ServiceName}: {check.Message}");
                }
            }
        }
    }

    /// <summary>
    /// Interface for services that support custom health checks
    /// </summary>
    public interface IServiceHealthCheckable
    {
        ServiceHealthCheck PerformHealthCheck();
    }
}

