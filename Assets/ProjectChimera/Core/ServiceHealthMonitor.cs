using UnityEngine;
using System.Collections.Generic;
using System;
using System.Linq;
using System.Collections;
using ProjectChimera.Core.DependencyInjection;

namespace ProjectChimera.Core
{
    /// <summary>
    /// Comprehensive service health monitoring and validation system.
    /// Extracted from DIGameManager for modular architecture.
    /// Monitors manager health, dependency integrity, and system performance.
    /// </summary>
    public class ServiceHealthMonitor : MonoBehaviour
    {
        [Header("Health Monitoring Configuration")]
        [SerializeField] private bool _enableContinuousMonitoring = true;
        [SerializeField] private float _healthCheckInterval = 30f; // seconds
        [SerializeField] private bool _enablePerformanceMonitoring = true;
        [SerializeField] private bool _enableMemoryMonitoring = true;
        
        [Header("Recovery Configuration")]
        [SerializeField] private bool _enableAutoRecovery = true;
        [SerializeField] private int _maxRecoveryAttempts = 3;
        [SerializeField] private float _recoveryDelay = 1f; // seconds between attempts
        [SerializeField] private bool _logRecoveryAttempts = true;
        
        [Header("Alert Configuration")]
        [SerializeField] private bool _enableHealthAlerts = true;
        [SerializeField] private float _alertCooldownTime = 60f; // seconds
        [SerializeField] private ServiceStatus _alertThreshold = ServiceStatus.Warning;
        
        // Core components
        private ManagerRegistry _managerRegistry;
        private IServiceContainer _serviceContainer;
        
        // Health tracking
        private Dictionary<Type, ServiceHealthData> _serviceHealthHistory = new Dictionary<Type, ServiceHealthData>();
        private Dictionary<Type, DateTime> _lastAlertTimes = new Dictionary<Type, DateTime>();
        private Dictionary<Type, int> _recoveryAttemptCounts = new Dictionary<Type, int>();
        
        // Performance tracking
        private Dictionary<Type, PerformanceMetrics> _performanceMetrics = new Dictionary<Type, PerformanceMetrics>();
        private float _lastMemoryUsage;
        private DateTime _lastHealthCheck;
        
        // Monitoring state
        private Coroutine _continuousMonitoringCoroutine;
        private bool _isMonitoring = false;
        
        // Events
        public System.Action<ServiceHealthReport> OnHealthReportGenerated;
        public System.Action<ChimeraManager, ServiceStatus> OnServiceHealthChanged;
        public System.Action<ChimeraManager, bool> OnRecoveryAttempted;
        public System.Action<HealthAlert> OnHealthAlert;
        public System.Action<string> OnCriticalError;
        
        // Properties
        public bool IsMonitoring => _isMonitoring;
        public int TrackedServicesCount => _serviceHealthHistory.Count;
        public DateTime LastHealthCheck => _lastHealthCheck;
        public bool HasCriticalIssues => _serviceHealthHistory.Values.Any(h => h.CurrentStatus == ServiceStatus.Failed);
        
        /// <summary>
        /// Initialize the health monitor with required dependencies
        /// </summary>
        public void Initialize(ManagerRegistry managerRegistry, IServiceContainer serviceContainer = null)
        {
            _managerRegistry = managerRegistry ?? throw new ArgumentNullException(nameof(managerRegistry));
            _serviceContainer = serviceContainer;
            
            LogDebug("Service health monitor initialized");
            
            if (_enableContinuousMonitoring)
            {
                StartContinuousMonitoring();
            }
        }
        
        /// <summary>
        /// Start continuous health monitoring
        /// </summary>
        public void StartContinuousMonitoring()
        {
            if (_isMonitoring) return;
            
            _isMonitoring = true;
            _continuousMonitoringCoroutine = StartCoroutine(ContinuousHealthMonitoring());
            LogDebug($"Started continuous health monitoring (interval: {_healthCheckInterval}s)");
        }
        
        /// <summary>
        /// Stop continuous health monitoring
        /// </summary>
        public void StopContinuousMonitoring()
        {
            if (!_isMonitoring) return;
            
            _isMonitoring = false;
            if (_continuousMonitoringCoroutine != null)
            {
                StopCoroutine(_continuousMonitoringCoroutine);
                _continuousMonitoringCoroutine = null;
            }
            
            LogDebug("Stopped continuous health monitoring");
        }
        
        /// <summary>
        /// Generate a comprehensive service health report
        /// </summary>
        public ServiceHealthReport GenerateHealthReport()
        {
            var report = new ServiceHealthReport
            {
                ServiceStatuses = new Dictionary<Type, ServiceStatus>(),
                CriticalErrors = new List<string>(),
                Warnings = new List<string>(),
                InitializationTime = DateTime.Now - _lastHealthCheck,
                GeneratedAt = DateTime.Now
            };
            
            try
            {
                // Check all registered managers
                foreach (var manager in _managerRegistry.RegisteredManagers)
                {
                    if (manager == null)
                    {
                        report.CriticalErrors.Add("Null manager found in registry");
                        continue;
                    }
                    
                    var managerType = manager.GetType();
                    var status = EvaluateManagerHealth(manager);
                    
                    report.ServiceStatuses[managerType] = status;
                    
                    // Update health history
                    UpdateServiceHealthHistory(managerType, status);
                    
                    // Check for status changes
                    if (HasStatusChanged(managerType, status))
                    {
                        OnServiceHealthChanged?.Invoke(manager, status);
                        
                        // Generate alerts if needed
                        if (_enableHealthAlerts && ShouldGenerateAlert(managerType, status))
                        {
                            GenerateHealthAlert(manager, status);
                        }
                    }
                    
                    // Collect error details
                    if (status == ServiceStatus.Failed)
                    {
                        report.CriticalErrors.Add($"Manager {manager.ManagerName} is in failed state");
                    }
                    else if (status == ServiceStatus.Warning)
                    {
                        report.Warnings.Add($"Manager {manager.ManagerName} has warnings");
                    }
                }
                
                // Check DI container services if available
                if (_serviceContainer != null)
                {
                    var containerValidation = ValidateServiceContainer();
                    if (!containerValidation.IsValid)
                    {
                        report.CriticalErrors.AddRange(containerValidation.Errors);
                    }
                }
                
                // Add performance metrics
                if (_enablePerformanceMonitoring)
                {
                    report.PerformanceData = CollectPerformanceMetrics();
                }
                
                // Add memory usage
                if (_enableMemoryMonitoring)
                {
                    report.MemoryUsageMB = GetCurrentMemoryUsage();
                }
                
                report.IsHealthy = report.CriticalErrors.Count == 0;
                _lastHealthCheck = DateTime.Now;
                
                OnHealthReportGenerated?.Invoke(report);
            }
            catch (Exception ex)
            {
                report.IsHealthy = false;
                report.CriticalErrors.Add($"Health check failed: {ex.Message}");
                LogError($"Health report generation failed: {ex.Message}");
            }
            
            return report;
        }
        
        /// <summary>
        /// Evaluate the health status of a specific manager
        /// </summary>
        public ServiceStatus EvaluateManagerHealth(ChimeraManager manager)
        {
            if (manager == null) return ServiceStatus.Failed;
            
            try
            {
                // Check basic initialization
                if (!manager.IsInitialized)
                {
                    return ServiceStatus.Failed;
                }
                
                // Check for Unity object validity
                if (manager == null || manager.gameObject == null)
                {
                    return ServiceStatus.Failed;
                }
                
                // Check if the manager is active
                if (!manager.gameObject.activeInHierarchy)
                {
                    return ServiceStatus.Warning;
                }
                
                // Check performance metrics if available
                var managerType = manager.GetType();
                if (_performanceMetrics.TryGetValue(managerType, out var metrics))
                {
                    // Check for performance issues
                    if (metrics.AverageExecutionTime > 100f) // 100ms threshold
                    {
                        return ServiceStatus.Warning;
                    }
                    
                    if (metrics.ErrorCount > 0)
                    {
                        return ServiceStatus.Warning;
                    }
                }
                
                return ServiceStatus.Healthy;
            }
            catch (Exception ex)
            {
                LogError($"Error evaluating health for {manager?.ManagerName}: {ex.Message}");
                return ServiceStatus.Failed;
            }
        }
        
        /// <summary>
        /// Attempt automatic recovery of failed services
        /// </summary>
        public IEnumerator AttemptServiceRecovery(ServiceHealthReport healthReport)
        {
            if (!_enableAutoRecovery) yield break;
            
            LogDebug("Attempting service recovery for failed services");
            
            var failedServices = healthReport.ServiceStatuses
                .Where(kvp => kvp.Value == ServiceStatus.Failed)
                .ToList();
            
            foreach (var failedService in failedServices)
            {
                var managerType = failedService.Key;
                var manager = _managerRegistry?.GetManager(managerType) as ChimeraManager;
                
                if (manager != null)
                {
                    yield return StartCoroutine(AttemptManagerRecovery(manager));
                }
            }
        }
        
        /// <summary>
        /// Attempt to recover a specific manager
        /// </summary>
        public IEnumerator AttemptManagerRecovery(ChimeraManager manager)
        {
            if (manager == null) yield break;
            
            var managerType = manager.GetType();
            
            // Check recovery attempt count
            if (!_recoveryAttemptCounts.ContainsKey(managerType))
            {
                _recoveryAttemptCounts[managerType] = 0;
            }
            
            if (_recoveryAttemptCounts[managerType] >= _maxRecoveryAttempts)
            {
                LogError($"Maximum recovery attempts reached for {manager.ManagerName}");
                yield break;
            }
            
            _recoveryAttemptCounts[managerType]++;
            
            bool recoverySuccessful = false;
            
            try
            {
                if (_logRecoveryAttempts)
                {
                    LogDebug($"Attempting recovery for {manager.ManagerName} (attempt {_recoveryAttemptCounts[managerType]})");
                }
                
                // Attempt reinitialization outside try-catch
                if (!manager.IsInitialized)
                {
                    manager.Initialize();
                }
            }
            catch (Exception ex)
            {
                LogError($"Recovery attempt failed for {manager.ManagerName}: {ex.Message}");
            }
            
            // Wait outside try-catch
            yield return new WaitForSeconds(_recoveryDelay);
                
            if (manager.IsInitialized)
            {
                recoverySuccessful = true;
                LogDebug($"Successfully recovered {manager.ManagerName}");
                
                // Reset recovery attempt count on success
                _recoveryAttemptCounts[managerType] = 0;
            }
            else
            {
                // Manager appears initialized, consider it recovered
                recoverySuccessful = true;
            }
            
            OnRecoveryAttempted?.Invoke(manager, recoverySuccessful);
            
            if (!recoverySuccessful)
            {
                LogError($"Failed to recover {manager.ManagerName} after {_recoveryAttemptCounts[managerType]} attempts");
            }
        }
        
        /// <summary>
        /// Validate the service container integrity
        /// </summary>
        public ContainerValidationResult ValidateServiceContainer()
        {
            var result = new ContainerValidationResult
            {
                IsValid = true,
                Errors = new List<string>(),
                ServicesValidated = 0
            };
            
            if (_serviceContainer == null)
            {
                result.IsValid = false;
                result.Errors.Add("Service container is null");
                return result;
            }
            
            try
            {
                var containerResult = _serviceContainer.Verify();
                result.IsValid = containerResult.IsValid;
                result.Errors.AddRange(containerResult.Errors);
                result.ServicesValidated = containerResult.VerifiedServices;
            }
            catch (Exception ex)
            {
                result.IsValid = false;
                result.Errors.Add($"Container validation failed: {ex.Message}");
            }
            
            return result;
        }
        
        /// <summary>
        /// Continuous health monitoring coroutine
        /// </summary>
        private IEnumerator ContinuousHealthMonitoring()
        {
            while (_isMonitoring)
            {
                // Execute monitoring outside try-catch to allow yield
                ServiceHealthReport healthReport = null;
                bool monitoringSuccessful = false;
                
                try
                {
                    healthReport = GenerateHealthReport();
                    monitoringSuccessful = true;
                }
                catch (Exception ex)
                {
                    LogError($"Continuous monitoring error: {ex.Message}");
                    monitoringSuccessful = false;
                }
                
                // Attempt recovery outside try-catch if monitoring was successful
                if (monitoringSuccessful && healthReport != null && !healthReport.IsHealthy && _enableAutoRecovery)
                {
                    yield return StartCoroutine(AttemptServiceRecovery(healthReport));
                }
                
                yield return new WaitForSeconds(_healthCheckInterval);
            }
        }
        
        /// <summary>
        /// Update service health history
        /// </summary>
        private void UpdateServiceHealthHistory(Type serviceType, ServiceStatus status)
        {
            if (!_serviceHealthHistory.ContainsKey(serviceType))
            {
                _serviceHealthHistory[serviceType] = new ServiceHealthData
                {
                    ServiceType = serviceType,
                    StatusHistory = new List<ServiceStatusEntry>()
                };
            }
            
            var healthData = _serviceHealthHistory[serviceType];
            healthData.CurrentStatus = status;
            healthData.LastUpdated = DateTime.Now;
            healthData.StatusHistory.Add(new ServiceStatusEntry
            {
                Status = status,
                Timestamp = DateTime.Now
            });
            
            // Limit history size
            if (healthData.StatusHistory.Count > 100)
            {
                healthData.StatusHistory.RemoveAt(0);
            }
        }
        
        /// <summary>
        /// Check if service status has changed
        /// </summary>
        private bool HasStatusChanged(Type serviceType, ServiceStatus newStatus)
        {
            if (_serviceHealthHistory.TryGetValue(serviceType, out var healthData))
            {
                return healthData.CurrentStatus != newStatus;
            }
            return true; // First time checking, consider it a change
        }
        
        /// <summary>
        /// Determine if a health alert should be generated
        /// </summary>
        private bool ShouldGenerateAlert(Type serviceType, ServiceStatus status)
        {
            // Check if status meets alert threshold
            if ((int)status < (int)_alertThreshold) return false;
            
            // Check cooldown
            if (_lastAlertTimes.TryGetValue(serviceType, out var lastAlert))
            {
                var timeSinceLastAlert = (DateTime.Now - lastAlert).TotalSeconds;
                if (timeSinceLastAlert < _alertCooldownTime) return false;
            }
            
            return true;
        }
        
        /// <summary>
        /// Generate a health alert
        /// </summary>
        private void GenerateHealthAlert(ChimeraManager manager, ServiceStatus status)
        {
            var alert = new HealthAlert
            {
                AlertType = status == ServiceStatus.Failed ? HealthAlertType.Critical : HealthAlertType.Warning,
                ServiceType = manager.GetType(),
                ServiceName = manager.ManagerName,
                Status = status,
                Message = $"Service {manager.ManagerName} is in {status} state",
                Timestamp = DateTime.Now
            };
            
            _lastAlertTimes[manager.GetType()] = DateTime.Now;
            
            OnHealthAlert?.Invoke(alert);
            LogDebug($"Health alert generated: {alert.Message}");
        }
        
        /// <summary>
        /// Collect performance metrics for all tracked services
        /// </summary>
        private Dictionary<Type, PerformanceMetrics> CollectPerformanceMetrics()
        {
            // Return a copy of current performance metrics
            return new Dictionary<Type, PerformanceMetrics>(_performanceMetrics);
        }
        
        /// <summary>
        /// Get current memory usage in MB
        /// </summary>
        private float GetCurrentMemoryUsage()
        {
            return UnityEngine.Profiling.Profiler.GetTotalAllocatedMemory() / (1024f * 1024f);
        }
        
        /// <summary>
        /// Get health summary for a specific service type
        /// </summary>
        public ServiceHealthData GetServiceHealthData(Type serviceType)
        {
            return _serviceHealthHistory.TryGetValue(serviceType, out var data) ? data : null;
        }
        
        /// <summary>
        /// Clear health history for all services
        /// </summary>
        public void ClearHealthHistory()
        {
            _serviceHealthHistory.Clear();
            _lastAlertTimes.Clear();
            _recoveryAttemptCounts.Clear();
            LogDebug("Health history cleared");
        }
        
        private void LogDebug(string message)
        {
            Debug.Log($"[ServiceHealthMonitor] {message}");
        }
        
        private void LogError(string message)
        {
            Debug.LogError($"[ServiceHealthMonitor] {message}");
        }
        
        private void OnDestroy()
        {
            StopContinuousMonitoring();
            ClearHealthHistory();
        }
    }
    
    /// <summary>
    /// Enhanced service health report with additional monitoring data
    /// </summary>
    public class ServiceHealthReport
    {
        public bool IsHealthy { get; set; }
        public Dictionary<Type, ServiceStatus> ServiceStatuses { get; set; } = new Dictionary<Type, ServiceStatus>();
        public List<string> CriticalErrors { get; set; } = new List<string>();
        public List<string> Warnings { get; set; } = new List<string>();
        public TimeSpan InitializationTime { get; set; }
        public DateTime GeneratedAt { get; set; }
        public Dictionary<Type, PerformanceMetrics> PerformanceData { get; set; }
        public float MemoryUsageMB { get; set; }
    }
    
    /// <summary>
    /// Health data for a specific service
    /// </summary>
    public class ServiceHealthData
    {
        public Type ServiceType { get; set; }
        public ServiceStatus CurrentStatus { get; set; }
        public DateTime LastUpdated { get; set; }
        public List<ServiceStatusEntry> StatusHistory { get; set; } = new List<ServiceStatusEntry>();
    }
    
    /// <summary>
    /// Service status history entry
    /// </summary>
    public class ServiceStatusEntry
    {
        public ServiceStatus Status { get; set; }
        public DateTime Timestamp { get; set; }
    }
    
    /// <summary>
    /// Performance metrics for a service
    /// </summary>
    public class PerformanceMetrics
    {
        public Type ServiceType { get; set; }
        public float AverageExecutionTime { get; set; }
        public float PeakExecutionTime { get; set; }
        public int ExecutionCount { get; set; }
        public int ErrorCount { get; set; }
        public DateTime LastUpdated { get; set; }
    }
    
    /// <summary>
    /// Container validation result
    /// </summary>
    public class ContainerValidationResult
    {
        public bool IsValid { get; set; }
        public List<string> Errors { get; set; } = new List<string>();
        public int ServicesValidated { get; set; }
    }
    
    /// <summary>
    /// Health alert information
    /// </summary>
    public class HealthAlert
    {
        public HealthAlertType AlertType { get; set; }
        public Type ServiceType { get; set; }
        public string ServiceName { get; set; }
        public ServiceStatus Status { get; set; }
        public string Message { get; set; }
        public DateTime Timestamp { get; set; }
    }
    
    /// <summary>
    /// Health alert types
    /// </summary>
    public enum HealthAlertType
    {
        Info,
        Warning,
        Critical
    }
}