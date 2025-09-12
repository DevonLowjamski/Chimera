using ProjectChimera.Core.Logging;
using UnityEngine;
using System.Collections.Generic;
using System;

namespace ProjectChimera.Core
{
    /// <summary>
    /// SIMPLE: Basic service health monitoring aligned with Project Chimera's service monitoring vision.
    /// Focuses on essential service status checking without complex diagnostics.
    /// </summary>
    public class ServiceHealthMonitor : MonoBehaviour
    {
        [Header("Basic Settings")]
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private float _checkInterval = 30f;

        // Basic service tracking
        private Dictionary<Type, ServiceStatus> _serviceStatus = new Dictionary<Type, ServiceStatus>();
        private bool _isMonitoring = false;
        private float _lastCheckTime = 0f;

        // Events
        public System.Action<string, ServiceStatus> OnServiceHealthChanged;
        public System.Action<string> OnServiceError;

        // Properties
        public bool IsMonitoring => _isMonitoring;
        public int TrackedServicesCount => _serviceStatus.Count;

        /// <summary>
        /// Initialize the health monitor
        /// </summary>
        public void Initialize()
        {
            _isMonitoring = true;

            if (_enableLogging)
            {
                ChimeraLogger.Log("[ServiceHealthMonitor] Initialized successfully");
            }
        }

        /// <summary>
        /// Shutdown the health monitor
        /// </summary>
        public void Shutdown()
        {
            _isMonitoring = false;
            _serviceStatus.Clear();

            if (_enableLogging)
            {
                ChimeraLogger.Log("[ServiceHealthMonitor] Shutdown completed");
            }
        }

        /// <summary>
        /// Update method for periodic checks
        /// </summary>
        private void Update()
        {
            if (!_isMonitoring) return;

            if (Time.time - _lastCheckTime >= _checkInterval)
            {
                PerformHealthCheck();
                _lastCheckTime = Time.time;
            }
        }

        /// <summary>
        /// Perform basic health check
        /// </summary>
        private void PerformHealthCheck()
        {
            // Simple health check - could be expanded based on specific services
            bool allHealthy = true;

            foreach (var serviceType in _serviceStatus.Keys.ToList())
            {
                var status = CheckServiceHealth(serviceType);

                if (status != _serviceStatus[serviceType])
                {
                    _serviceStatus[serviceType] = status;
                    OnServiceHealthChanged?.Invoke(serviceType.Name, status);

                    if (_enableLogging)
                    {
                        ChimeraLogger.Log($"[ServiceHealthMonitor] Service {serviceType.Name} status: {status}");
                    }
                }

                if (status == ServiceStatus.Failed)
                {
                    allHealthy = false;
                    OnServiceError?.Invoke($"Service {serviceType.Name} failed");
                }
            }

            if (_enableLogging && !allHealthy)
            {
                ChimeraLogger.LogWarning("[ServiceHealthMonitor] Some services are unhealthy");
            }
        }

        /// <summary>
        /// Register a service for monitoring
        /// </summary>
        public void RegisterService(Type serviceType)
        {
            if (!_serviceStatus.ContainsKey(serviceType))
            {
                _serviceStatus[serviceType] = ServiceStatus.Healthy;

                if (_enableLogging)
                {
                    ChimeraLogger.Log($"[ServiceHealthMonitor] Registered service: {serviceType.Name}");
                }
            }
        }

        /// <summary>
        /// Unregister a service from monitoring
        /// </summary>
        public void UnregisterService(Type serviceType)
        {
            if (_serviceStatus.Remove(serviceType))
            {
                if (_enableLogging)
                {
                    ChimeraLogger.Log($"[ServiceHealthMonitor] Unregistered service: {serviceType.Name}");
                }
            }
        }

        /// <summary>
        /// Get service status
        /// </summary>
        public ServiceStatus GetServiceStatus(Type serviceType)
        {
            return _serviceStatus.GetValueOrDefault(serviceType, ServiceStatus.Unknown);
        }

        /// <summary>
        /// Check if all services are healthy
        /// </summary>
        public bool AreAllServicesHealthy()
        {
            return _serviceStatus.Values.All(status => status == ServiceStatus.Healthy);
        }

        #region Private Methods

        private ServiceStatus CheckServiceHealth(Type serviceType)
        {
            try
            {
                // Simple health check - in a real implementation, this would check
                // if the service is responding, has valid state, etc.
                // For now, assume all services are healthy
                return ServiceStatus.Healthy;
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError($"[ServiceHealthMonitor] Health check failed for {serviceType.Name}: {ex.Message}");
                return ServiceStatus.Failed;
            }
        }

        #endregion
    }

    /// <summary>
    /// Service health report containing overall health status and diagnostics
    /// </summary>
    [System.Serializable]
    public class ServiceHealthReport
    {
        public bool IsHealthy { get; set; }
        public DateTime CheckTimestamp { get; set; }
        public DateTime GeneratedAt { get; set; }
        public Dictionary<Type, ServiceHealthStatus> ServiceHealth { get; set; }
        public List<string> HealthIssues { get; set; }
        public int TotalServices { get; set; }
        public int HealthyServices { get; set; }
        public int UnhealthyServices { get; set; }
        public int CriticalServices { get; set; }
        public List<string> CriticalErrors { get; set; }
        public List<string> Warnings { get; set; }
        public string OverallStatus { get; set; }

        public ServiceHealthReport()
        {
            CheckTimestamp = DateTime.Now;
            GeneratedAt = DateTime.Now;
            ServiceHealth = new Dictionary<Type, ServiceHealthStatus>();
            HealthIssues = new List<string>();
            CriticalErrors = new List<string>();
            Warnings = new List<string>();
            OverallStatus = "Unknown";
        }
    }

    /// <summary>
    /// Detailed health status for individual services
    /// </summary>
    [System.Serializable]
    public class ServiceHealthStatus
    {
        public ServiceStatus Status { get; set; }
        public string StatusMessage { get; set; }
        public DateTime LastCheck { get; set; }
        public TimeSpan ResponseTime { get; set; }
        public Dictionary<string, object> Diagnostics { get; set; }

        public ServiceHealthStatus()
        {
            Status = ServiceStatus.Unknown;
            StatusMessage = string.Empty;
            LastCheck = DateTime.Now;
            ResponseTime = TimeSpan.Zero;
            Diagnostics = new Dictionary<string, object>();
        }
    }

    /// <summary>
    /// Simple service status enum
    /// </summary>
    public enum ServiceStatus
    {
        Unknown,
        Healthy,
        Warning,
        Failed
    }
}
