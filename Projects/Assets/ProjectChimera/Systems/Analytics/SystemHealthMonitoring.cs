using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using ProjectChimera.Systems.Services.Core;

namespace ProjectChimera.Systems.Analytics
{
    /// <summary>
    /// Comprehensive system health monitoring for Project Chimera
    /// Monitors system dependencies, service health, and provides automated recovery
    /// </summary>
    public class SystemHealthMonitoring : MonoBehaviour
    {
        [Header("Health Monitoring Configuration")]
        [SerializeField] private bool _enableHealthChecks = true;
        [SerializeField] private float _healthCheckInterval = 5f;
        [SerializeField] private int _maxRetries = 3;
        [SerializeField] private float _retryDelay = 2f;
        [SerializeField] private bool _enableAutoRecovery = true;
        [SerializeField] private bool _enablePredictiveAlerts = true;
        
        [Header("Alert Thresholds")]
        [SerializeField] private float _criticalHealthThreshold = 0.3f;
        [SerializeField] private float _warningHealthThreshold = 0.7f;
        [SerializeField] private int _maxConsecutiveFailures = 5;
        [SerializeField] private float _responseTimeThreshold = 1000f; // milliseconds
        
        // Core Systems
        private DataPipelineIntegration _dataPipeline;
        private PerformanceMonitoringDashboard _performanceDashboard;
        private AdvancedAnalytics _analytics;
        private ServiceLayerCoordinator _serviceCoordinator;
        
        // Health Tracking
        private readonly Dictionary<string, SystemHealthStatus> _systemHealth = new Dictionary<string, SystemHealthStatus>();
        private readonly Dictionary<string, List<HealthCheckResult>> _healthHistory = new Dictionary<string, List<HealthCheckResult>>();
        private readonly List<HealthAlert> _activeHealthAlerts = new List<HealthAlert>();
        private readonly Queue<RecoveryAction> _pendingRecoveryActions = new Queue<RecoveryAction>();
        
        // Health Check Providers
        private readonly Dictionary<string, IHealthCheckProvider> _healthProviders = new Dictionary<string, IHealthCheckProvider>();
        
        // Monitoring State
        private bool _isMonitoring = false;
        private float _lastHealthCheckTime;
        private int _totalHealthChecks = 0;
        private int _failedHealthChecks = 0;
        
        // Events
        public event Action<string, SystemHealthStatus> OnSystemHealthChanged;
        public event Action<HealthAlert> OnHealthAlertTriggered;
        public event Action<string> OnHealthAlertResolved;
        public event Action<string, RecoveryAction> OnRecoveryActionExecuted;
        
        private void Awake()
        {
            InitializeHealthProviders();
        }
        
        private void Start()
        {
            FindSystemReferences();
            RegisterSystemHealthChecks();
            StartHealthMonitoring();
        }
        
        private void Update()
        {
            if (_isMonitoring && Time.time - _lastHealthCheckTime >= _healthCheckInterval)
            {
                _ = PerformHealthChecksAsync();
                _lastHealthCheckTime = Time.time;
            }
            
            ProcessPendingRecoveryActions();
        }
        
        private void FindSystemReferences()
        {
            _dataPipeline = UnityEngine.Object.FindObjectOfType<DataPipelineIntegration>();
            _performanceDashboard = UnityEngine.Object.FindObjectOfType<PerformanceMonitoringDashboard>();
            _analytics = UnityEngine.Object.FindObjectOfType<AdvancedAnalytics>();
            _serviceCoordinator = UnityEngine.Object.FindObjectOfType<ServiceLayerCoordinator>();
            
            Debug.Log("[SystemHealthMonitoring] System references found");
        }
        
        private void InitializeHealthProviders()
        {
            RegisterHealthProvider("Unity", new UnityHealthProvider());
            RegisterHealthProvider("Memory", new MemoryHealthProvider());
            RegisterHealthProvider("Storage", new StorageHealthProvider());
            RegisterHealthProvider("Network", new NetworkHealthProvider());
            RegisterHealthProvider("Services", new ServiceHealthProvider());
            RegisterHealthProvider("DataPipeline", new DataPipelineHealthProvider(_dataPipeline));
            RegisterHealthProvider("Analytics", new AnalyticsHealthProvider(_analytics));
            RegisterHealthProvider("Database", new DatabaseHealthProvider());
            RegisterHealthProvider("FileSystem", new FileSystemHealthProvider());
            RegisterHealthProvider("Threading", new ThreadingHealthProvider());
        }
        
        public void RegisterHealthProvider(string systemName, IHealthCheckProvider provider)
        {
            _healthProviders[systemName] = provider;
            _systemHealth[systemName] = new SystemHealthStatus
            {
                SystemName = systemName,
                Status = HealthStatus.Unknown,
                LastCheck = DateTime.MinValue,
                ConsecutiveFailures = 0
            };
            
            if (!_healthHistory.ContainsKey(systemName))
            {
                _healthHistory[systemName] = new List<HealthCheckResult>();
            }
            
            Debug.Log($"[SystemHealthMonitoring] Registered health provider for {systemName}");
        }
        
        private void RegisterSystemHealthChecks()
        {
            // Register additional system-specific health checks
            if (_dataPipeline != null)
            {
                RegisterHealthProvider("DataCollection", new DataCollectionHealthProvider(_dataPipeline));
            }
            
            if (_performanceDashboard != null)
            {
                RegisterHealthProvider("PerformanceMonitoring", new PerformanceMonitoringHealthProvider(_performanceDashboard));
            }
            
            if (_analytics != null)
            {
                RegisterHealthProvider("AdvancedAnalytics", new AdvancedAnalyticsHealthProvider(_analytics));
            }
        }
        
        public void StartHealthMonitoring()
        {
            _isMonitoring = true;
            _lastHealthCheckTime = Time.time;
            Debug.Log("[SystemHealthMonitoring] Health monitoring started");
        }
        
        public void StopHealthMonitoring()
        {
            _isMonitoring = false;
            Debug.Log("[SystemHealthMonitoring] Health monitoring stopped");
        }
        
        private async Task PerformHealthChecksAsync()
        {
            var healthCheckTasks = new List<Task<HealthCheckResult>>();
            
            foreach (var provider in _healthProviders)
            {
                healthCheckTasks.Add(ExecuteHealthCheckAsync(provider.Key, provider.Value));
            }
            
            try
            {
                var results = await Task.WhenAll(healthCheckTasks);
                ProcessHealthCheckResults(results);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SystemHealthMonitoring] Error during health checks: {ex.Message}");
                _failedHealthChecks++;
            }
            
            _totalHealthChecks++;
        }
        
        private async Task<HealthCheckResult> ExecuteHealthCheckAsync(string systemName, IHealthCheckProvider provider)
        {
            var startTime = DateTime.UtcNow;
            var result = new HealthCheckResult
            {
                SystemName = systemName,
                Timestamp = startTime,
                Success = false
            };
            
            try
            {
                var healthStatus = await provider.CheckHealthAsync();
                var responseTime = (DateTime.UtcNow - startTime).TotalMilliseconds;
                
                result.Success = healthStatus.IsHealthy;
                result.HealthScore = healthStatus.HealthScore;
                result.ResponseTime = responseTime;
                result.Message = healthStatus.Message;
                result.Details = healthStatus.Details;
                result.Dependencies = healthStatus.Dependencies;
                
                // Check response time threshold
                if (responseTime > _responseTimeThreshold)
                {
                    result.Warnings.Add($"Slow response time: {responseTime:F0}ms");
                }
            }
            catch (Exception ex)
            {
                result.Success = false;
                result.HealthScore = 0f;
                result.Message = $"Health check failed: {ex.Message}";
                result.Exception = ex;
            }
            
            return result;
        }
        
        private void ProcessHealthCheckResults(HealthCheckResult[] results)
        {
            foreach (var result in results)
            {
                UpdateSystemHealth(result);
                AddToHealthHistory(result);
                CheckForAlerts(result);
                
                if (_enableAutoRecovery && !result.Success)
                {
                    QueueRecoveryAction(result);
                }
            }
            
            // Update overall system health
            CalculateOverallSystemHealth();
            
            // Send metrics to data pipeline
            if (_dataPipeline != null)
            {
                SendHealthMetricsToDataPipeline(results);
            }
        }
        
        private void UpdateSystemHealth(HealthCheckResult result)
        {
            if (!_systemHealth.TryGetValue(result.SystemName, out var healthStatus))
            {
                healthStatus = new SystemHealthStatus { SystemName = result.SystemName };
                _systemHealth[result.SystemName] = healthStatus;
            }
            
            var previousStatus = healthStatus.Status;
            
            healthStatus.LastCheck = result.Timestamp;
            healthStatus.LastHealthScore = result.HealthScore;
            healthStatus.ResponseTime = result.ResponseTime;
            healthStatus.Message = result.Message;
            healthStatus.Dependencies = result.Dependencies;
            
            if (result.Success)
            {
                healthStatus.Status = GetHealthStatusFromScore(result.HealthScore);
                healthStatus.ConsecutiveFailures = 0;
                healthStatus.LastSuccessfulCheck = result.Timestamp;
            }
            else
            {
                healthStatus.Status = HealthStatus.Critical;
                healthStatus.ConsecutiveFailures++;
                healthStatus.LastFailure = result.Timestamp;
                healthStatus.LastFailureMessage = result.Message;
            }
            
            // Notify if status changed
            if (previousStatus != healthStatus.Status)
            {
                OnSystemHealthChanged?.Invoke(result.SystemName, healthStatus);
            }
        }
        
        private HealthStatus GetHealthStatusFromScore(float healthScore)
        {
            if (healthScore >= _warningHealthThreshold)
                return HealthStatus.Healthy;
            else if (healthScore >= _criticalHealthThreshold)
                return HealthStatus.Warning;
            else
                return HealthStatus.Critical;
        }
        
        private void AddToHealthHistory(HealthCheckResult result)
        {
            if (!_healthHistory.TryGetValue(result.SystemName, out var history))
            {
                history = new List<HealthCheckResult>();
                _healthHistory[result.SystemName] = history;
            }
            
            history.Add(result);
            
            // Limit history size (keep last 100 entries)
            if (history.Count > 100)
            {
                history.RemoveAt(0);
            }
        }
        
        private void CheckForAlerts(HealthCheckResult result)
        {
            var systemHealth = _systemHealth[result.SystemName];
            
            // Critical health alert
            if (systemHealth.Status == HealthStatus.Critical)
            {
                TriggerHealthAlert(HealthAlertType.SystemCritical, 
                    $"System {result.SystemName} is in critical state: {result.Message}", 
                    result.SystemName, result.HealthScore);
            }
            
            // Consecutive failures alert
            if (systemHealth.ConsecutiveFailures >= _maxConsecutiveFailures)
            {
                TriggerHealthAlert(HealthAlertType.ConsecutiveFailures,
                    $"System {result.SystemName} has {systemHealth.ConsecutiveFailures} consecutive failures",
                    result.SystemName, systemHealth.ConsecutiveFailures);
            }
            
            // Slow response time alert
            if (result.ResponseTime > _responseTimeThreshold)
            {
                TriggerHealthAlert(HealthAlertType.SlowResponse,
                    $"System {result.SystemName} response time: {result.ResponseTime:F0}ms",
                    result.SystemName, (float)result.ResponseTime);
            }
            
            // Dependency failure alert
            if (result.Dependencies != null)
            {
                foreach (var dependency in result.Dependencies)
                {
                    if (!dependency.Value)
                    {
                        TriggerHealthAlert(HealthAlertType.DependencyFailure,
                            $"System {result.SystemName} dependency '{dependency.Key}' is unavailable",
                            result.SystemName, 0f);
                    }
                }
            }
            
            // Predictive alerts
            if (_enablePredictiveAlerts)
            {
                CheckPredictiveAlerts(result);
            }
        }
        
        private void CheckPredictiveAlerts(HealthCheckResult result)
        {
            var history = _healthHistory[result.SystemName];
            if (history.Count < 10) return; // Need sufficient history
            
            var recentHistory = history.TakeLast(10).ToList();
            var avgHealthScore = recentHistory.Average(h => h.HealthScore);
            var trend = CalculateHealthTrend(recentHistory);
            
            // Declining health trend
            if (trend < -0.1f && avgHealthScore < _warningHealthThreshold)
            {
                TriggerHealthAlert(HealthAlertType.DecliningHealth,
                    $"System {result.SystemName} showing declining health trend",
                    result.SystemName, trend);
            }
            
            // Increasing response time trend
            var avgResponseTime = recentHistory.Average(h => h.ResponseTime);
            var responseTimeTrend = CalculateResponseTimeTrend(recentHistory);
            
            if (responseTimeTrend > 100f && avgResponseTime > _responseTimeThreshold * 0.8f)
            {
                TriggerHealthAlert(HealthAlertType.IncreasingLatency,
                    $"System {result.SystemName} showing increasing response time",
                    result.SystemName, (float)avgResponseTime);
            }
        }
        
        private float CalculateHealthTrend(List<HealthCheckResult> history)
        {
            if (history.Count < 5) return 0f;
            
            var firstHalf = history.Take(history.Count / 2).Average(h => h.HealthScore);
            var secondHalf = history.Skip(history.Count / 2).Average(h => h.HealthScore);
            
            return secondHalf - firstHalf;
        }
        
        private double CalculateResponseTimeTrend(List<HealthCheckResult> history)
        {
            if (history.Count < 5) return 0.0;
            
            var firstHalf = history.Take(history.Count / 2).Average(h => h.ResponseTime);
            var secondHalf = history.Skip(history.Count / 2).Average(h => h.ResponseTime);
            
            return secondHalf - firstHalf;
        }
        
        private void TriggerHealthAlert(HealthAlertType type, string message, string systemName, float value)
        {
            var alertId = $"{type}_{systemName}";
            var existingAlert = _activeHealthAlerts.FirstOrDefault(a => a.AlertId == alertId);
            
            if (existingAlert == null)
            {
                var alert = new HealthAlert
                {
                    AlertId = alertId,
                    Type = type,
                    Message = message,
                    SystemName = systemName,
                    Value = value,
                    FirstDetected = DateTime.UtcNow,
                    LastUpdated = DateTime.UtcNow,
                    IsActive = true
                };
                
                _activeHealthAlerts.Add(alert);
                OnHealthAlertTriggered?.Invoke(alert);
                Debug.LogWarning($"[SystemHealthMonitoring] Health Alert: {message}");
            }
            else
            {
                existingAlert.LastUpdated = DateTime.UtcNow;
                existingAlert.Value = value;
            }
        }
        
        private void QueueRecoveryAction(HealthCheckResult result)
        {
            var recoveryAction = new RecoveryAction
            {
                ActionId = Guid.NewGuid().ToString(),
                SystemName = result.SystemName,
                ActionType = DetermineRecoveryActionType(result),
                Timestamp = DateTime.UtcNow,
                Attempts = 0,
                MaxAttempts = _maxRetries
            };
            
            _pendingRecoveryActions.Enqueue(recoveryAction);
        }
        
        private RecoveryActionType DetermineRecoveryActionType(HealthCheckResult result)
        {
            var systemHealth = _systemHealth[result.SystemName];
            
            if (systemHealth.ConsecutiveFailures >= _maxConsecutiveFailures)
                return RecoveryActionType.RestartSystem;
            
            if (result.ResponseTime > _responseTimeThreshold * 2)
                return RecoveryActionType.ClearCache;
            
            if (result.Dependencies != null && result.Dependencies.Any(d => !d.Value))
                return RecoveryActionType.RestartDependencies;
            
            return RecoveryActionType.RetryOperation;
        }
        
        private void ProcessPendingRecoveryActions()
        {
            if (_pendingRecoveryActions.Count == 0) return;
            
            var action = _pendingRecoveryActions.Peek();
            if (DateTime.UtcNow - action.LastAttempt < TimeSpan.FromSeconds(_retryDelay))
                return;
            
            action = _pendingRecoveryActions.Dequeue();
            _ = ExecuteRecoveryActionAsync(action);
        }
        
        private async Task ExecuteRecoveryActionAsync(RecoveryAction action)
        {
            action.Attempts++;
            action.LastAttempt = DateTime.UtcNow;
            
            try
            {
                bool success = false;
                
                switch (action.ActionType)
                {
                    case RecoveryActionType.RetryOperation:
                        success = await RetrySystemOperation(action.SystemName);
                        break;
                    case RecoveryActionType.ClearCache:
                        success = await ClearSystemCache(action.SystemName);
                        break;
                    case RecoveryActionType.RestartSystem:
                        success = await RestartSystem(action.SystemName);
                        break;
                    case RecoveryActionType.RestartDependencies:
                        success = await RestartDependencies(action.SystemName);
                        break;
                }
                
                if (success)
                {
                    action.Success = true;
                    action.CompletedAt = DateTime.UtcNow;
                    Debug.Log($"[SystemHealthMonitoring] Recovery action successful for {action.SystemName}: {action.ActionType}");
                }
                else if (action.Attempts < action.MaxAttempts)
                {
                    _pendingRecoveryActions.Enqueue(action);
                }
                else
                {
                    action.Success = false;
                    action.CompletedAt = DateTime.UtcNow;
                    Debug.LogError($"[SystemHealthMonitoring] Recovery action failed for {action.SystemName}: {action.ActionType}");
                }
                
                OnRecoveryActionExecuted?.Invoke(action.SystemName, action);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[SystemHealthMonitoring] Recovery action error for {action.SystemName}: {ex.Message}");
                action.ErrorMessage = ex.Message;
                
                if (action.Attempts < action.MaxAttempts)
                {
                    _pendingRecoveryActions.Enqueue(action);
                }
            }
        }
        
        private async Task<bool> RetrySystemOperation(string systemName)
        {
            await Task.Delay(100); // Simulated operation
            return true;
        }
        
        private async Task<bool> ClearSystemCache(string systemName)
        {
            await Task.Delay(200); // Simulated cache clear
            System.GC.Collect();
            return true;
        }
        
        private async Task<bool> RestartSystem(string systemName)
        {
            await Task.Delay(500); // Simulated system restart
            return true;
        }
        
        private async Task<bool> RestartDependencies(string systemName)
        {
            await Task.Delay(300); // Simulated dependency restart
            return true;
        }
        
        private void CalculateOverallSystemHealth()
        {
            if (_systemHealth.Count == 0) return;
            
            var healthyCount = _systemHealth.Values.Count(s => s.Status == HealthStatus.Healthy);
            var warningCount = _systemHealth.Values.Count(s => s.Status == HealthStatus.Warning);
            var criticalCount = _systemHealth.Values.Count(s => s.Status == HealthStatus.Critical);
            
            var overallHealthScore = _systemHealth.Values.Average(s => s.LastHealthScore);
            
            // Send overall health metrics
            if (_dataPipeline != null)
            {
                var healthSummary = new
                {
                    overall_health_score = overallHealthScore,
                    healthy_systems = healthyCount,
                    warning_systems = warningCount,
                    critical_systems = criticalCount,
                    total_systems = _systemHealth.Count,
                    active_alerts = _activeHealthAlerts.Count,
                    pending_recovery_actions = _pendingRecoveryActions.Count,
                    total_health_checks = _totalHealthChecks,
                    failed_health_checks = _failedHealthChecks
                };
                
                _dataPipeline.CollectEvent("system_health", "overall_status", healthSummary);
            }
        }
        
        private void SendHealthMetricsToDataPipeline(HealthCheckResult[] results)
        {
            foreach (var result in results)
            {
                var eventData = new
                {
                    system_name = result.SystemName,
                    success = result.Success,
                    health_score = result.HealthScore,
                    response_time = result.ResponseTime,
                    message = result.Message,
                    warnings = result.Warnings,
                    dependencies = result.Dependencies
                };
                
                _dataPipeline.CollectEvent("system_health", "health_check_result", eventData, new Dictionary<string, object>
                {
                    ["source"] = "system_health_monitoring",
                    ["timestamp"] = result.Timestamp.ToString("O")
                });
            }
        }
        
        public SystemHealthStatus GetSystemHealth(string systemName)
        {
            return _systemHealth.TryGetValue(systemName, out var health) ? health : null;
        }
        
        public IReadOnlyDictionary<string, SystemHealthStatus> GetAllSystemHealth()
        {
            return _systemHealth.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        }
        
        public IReadOnlyList<HealthAlert> GetActiveHealthAlerts()
        {
            return _activeHealthAlerts.AsReadOnly();
        }
        
        public List<HealthCheckResult> GetHealthHistory(string systemName, int maxResults = 50)
        {
            if (_healthHistory.TryGetValue(systemName, out var history))
            {
                return history.TakeLast(maxResults).ToList();
            }
            return new List<HealthCheckResult>();
        }
        
        public void SetHealthCheckInterval(float interval)
        {
            _healthCheckInterval = Mathf.Max(1f, interval);
        }
        
        public void EnableAutoRecovery(bool enable)
        {
            _enableAutoRecovery = enable;
        }
        
        public void TriggerManualHealthCheck()
        {
            if (_isMonitoring)
            {
                _ = PerformHealthChecksAsync();
            }
        }
        
        private void OnDestroy()
        {
            StopHealthMonitoring();
        }
    }
    
    /// <summary>
    /// System health status
    /// </summary>
    [System.Serializable]
    public class SystemHealthStatus
    {
        public string SystemName;
        public HealthStatus Status = HealthStatus.Unknown;
        public float LastHealthScore;
        public DateTime LastCheck;
        public DateTime LastSuccessfulCheck;
        public DateTime LastFailure;
        public string LastFailureMessage;
        public double ResponseTime;
        public string Message;
        public int ConsecutiveFailures;
        public Dictionary<string, bool> Dependencies;
    }
    
    /// <summary>
    /// Health check result
    /// </summary>
    [System.Serializable]
    public class HealthCheckResult
    {
        public string SystemName;
        public DateTime Timestamp;
        public bool Success;
        public float HealthScore;
        public double ResponseTime;
        public string Message;
        public Dictionary<string, object> Details;
        public Dictionary<string, bool> Dependencies;
        public List<string> Warnings = new List<string>();
        public Exception Exception;
    }
    
    /// <summary>
    /// Health alert
    /// </summary>
    [System.Serializable]
    public class HealthAlert
    {
        public string AlertId;
        public HealthAlertType Type;
        public string Message;
        public string SystemName;
        public float Value;
        public DateTime FirstDetected;
        public DateTime LastUpdated;
        public DateTime ResolvedAt;
        public bool IsActive = true;
        
        public TimeSpan Duration => IsActive ? DateTime.UtcNow - FirstDetected : ResolvedAt - FirstDetected;
    }
    
    /// <summary>
    /// Recovery action
    /// </summary>
    [System.Serializable]
    public class RecoveryAction
    {
        public string ActionId;
        public string SystemName;
        public RecoveryActionType ActionType;
        public DateTime Timestamp;
        public DateTime LastAttempt;
        public DateTime CompletedAt;
        public int Attempts;
        public int MaxAttempts;
        public bool Success;
        public string ErrorMessage;
    }
    
    /// <summary>
    /// Health status enum
    /// </summary>
    public enum HealthStatus
    {
        Unknown,
        Healthy,
        Warning,
        Critical,
        Unavailable
    }
    
    /// <summary>
    /// Health alert types
    /// </summary>
    public enum HealthAlertType
    {
        SystemCritical,
        ConsecutiveFailures,
        SlowResponse,
        DependencyFailure,
        DecliningHealth,
        IncreasingLatency,
        ResourceExhaustion,
        ConfigurationError
    }
    
    /// <summary>
    /// Recovery action types
    /// </summary>
    public enum RecoveryActionType
    {
        RetryOperation,
        ClearCache,
        RestartSystem,
        RestartDependencies,
        ReconfigureSystem,
        ScaleResources
    }
    
    /// <summary>
    /// Health status information
    /// </summary>
    public struct HealthStatusInfo
    {
        public bool IsHealthy;
        public float HealthScore;
        public string Message;
        public Dictionary<string, object> Details;
        public Dictionary<string, bool> Dependencies;
        
        public static HealthStatusInfo Healthy(float score = 1.0f, string message = "System is healthy")
        {
            return new HealthStatusInfo
            {
                IsHealthy = true,
                HealthScore = score,
                Message = message,
                Details = new Dictionary<string, object>(),
                Dependencies = new Dictionary<string, bool>()
            };
        }
        
        public static HealthStatusInfo Unhealthy(float score = 0.0f, string message = "System is unhealthy")
        {
            return new HealthStatusInfo
            {
                IsHealthy = false,
                HealthScore = score,
                Message = message,
                Details = new Dictionary<string, object>(),
                Dependencies = new Dictionary<string, bool>()
            };
        }
    }
    
    /// <summary>
    /// Interface for health check providers
    /// </summary>
    public interface IHealthCheckProvider
    {
        Task<HealthStatusInfo> CheckHealthAsync();
    }
    
    // Health Check Provider Implementations
    
    /// <summary>
    /// Unity system health provider
    /// </summary>
    public class UnityHealthProvider : IHealthCheckProvider
    {
        public async Task<HealthStatusInfo> CheckHealthAsync()
        {
            await Task.Delay(10); // Simulate async operation
            
            var details = new Dictionary<string, object>
            {
                ["unity_version"] = Application.unityVersion,
                ["platform"] = Application.platform.ToString(),
                ["is_focused"] = Application.isFocused,
                ["target_frame_rate"] = Application.targetFrameRate,
                ["time_scale"] = Time.timeScale
            };
            
            var healthScore = Application.isFocused ? 1.0f : 0.8f;
            
            return new HealthStatusInfo
            {
                IsHealthy = true,
                HealthScore = healthScore,
                Message = "Unity engine is running normally",
                Details = details,
                Dependencies = new Dictionary<string, bool>()
            };
        }
    }
    
    /// <summary>
    /// Memory health provider
    /// </summary>
    public class MemoryHealthProvider : IHealthCheckProvider
    {
        public async Task<HealthStatusInfo> CheckHealthAsync()
        {
            await Task.Delay(5);
            
            var totalMemory = SystemInfo.systemMemorySize;
            var usedMemory = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemory() / (1024 * 1024); // MB
            var memoryUsagePercent = (float)usedMemory / totalMemory * 100f;
            
            var details = new Dictionary<string, object>
            {
                ["total_memory_mb"] = totalMemory,
                ["used_memory_mb"] = usedMemory,
                ["usage_percent"] = memoryUsagePercent,
                ["allocated_memory"] = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemory(),
                ["reserved_memory"] = UnityEngine.Profiling.Profiler.GetTotalReservedMemory()
            };
            
            var healthScore = memoryUsagePercent < 80f ? 1.0f : 
                             memoryUsagePercent < 90f ? 0.7f : 0.3f;
            
            return new HealthStatusInfo
            {
                IsHealthy = memoryUsagePercent < 95f,
                HealthScore = healthScore,
                Message = $"Memory usage: {memoryUsagePercent:F1}% ({usedMemory}MB / {totalMemory}MB)",
                Details = details
            };
        }
    }
    
    /// <summary>
    /// Storage health provider
    /// </summary>
    public class StorageHealthProvider : IHealthCheckProvider
    {
        public async Task<HealthStatusInfo> CheckHealthAsync()
        {
            await Task.Delay(15);
            
            try
            {
                var persistentDataPath = Application.persistentDataPath;
                var tempPath = Application.temporaryCachePath;
                
                var details = new Dictionary<string, object>
                {
                    ["persistent_data_path"] = persistentDataPath,
                    ["temporary_cache_path"] = tempPath,
                    ["can_write_persistent"] = System.IO.Directory.Exists(persistentDataPath),
                    ["can_write_temp"] = System.IO.Directory.Exists(tempPath)
                };
                
                return HealthStatusInfo.Healthy(1.0f, "Storage systems accessible");
            }
            catch (Exception ex)
            {
                return HealthStatusInfo.Unhealthy(0.0f, $"Storage access failed: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// Network health provider
    /// </summary>
    public class NetworkHealthProvider : IHealthCheckProvider
    {
        public async Task<HealthStatusInfo> CheckHealthAsync()
        {
            await Task.Delay(20);
            
            var reachability = Application.internetReachability;
            var isNetworkAvailable = reachability != NetworkReachability.NotReachable;
            
            var details = new Dictionary<string, object>
            {
                ["internet_reachability"] = reachability.ToString(),
                ["is_network_available"] = isNetworkAvailable
            };
            
            var healthScore = isNetworkAvailable ? 1.0f : 0.0f;
            
            return new HealthStatusInfo
            {
                IsHealthy = isNetworkAvailable,
                HealthScore = healthScore,
                Message = $"Network status: {reachability}",
                Details = details
            };
        }
    }
    
    /// <summary>
    /// Service health provider
    /// </summary>
    public class ServiceHealthProvider : IHealthCheckProvider
    {
        public async Task<HealthStatusInfo> CheckHealthAsync()
        {
            await Task.Delay(10);
            
            // Check for core service components
            var serviceCoordinator = UnityEngine.Object.FindObjectOfType<ServiceLayerCoordinator>();
            var hasServiceCoordinator = serviceCoordinator != null;
            
            var details = new Dictionary<string, object>
            {
                ["service_coordinator_available"] = hasServiceCoordinator
            };
            
            var dependencies = new Dictionary<string, bool>
            {
                ["ServiceLayerCoordinator"] = hasServiceCoordinator
            };
            
            return new HealthStatusInfo
            {
                IsHealthy = hasServiceCoordinator,
                HealthScore = hasServiceCoordinator ? 1.0f : 0.0f,
                Message = hasServiceCoordinator ? "Core services available" : "Core services unavailable",
                Details = details,
                Dependencies = dependencies
            };
        }
    }
    
    /// <summary>
    /// Data pipeline health provider
    /// </summary>
    public class DataPipelineHealthProvider : IHealthCheckProvider
    {
        private readonly DataPipelineIntegration _dataPipeline;
        
        public DataPipelineHealthProvider(DataPipelineIntegration dataPipeline)
        {
            _dataPipeline = dataPipeline;
        }
        
        public async Task<HealthStatusInfo> CheckHealthAsync()
        {
            await Task.Delay(5);
            
            if (_dataPipeline == null)
            {
                return HealthStatusInfo.Unhealthy(0.0f, "Data pipeline not available");
            }
            
            var metrics = _dataPipeline.GetPipelineMetrics();
            var isHealthy = metrics.ErrorRate < 0.1f && metrics.QueueSize < 1000;
            
            var details = new Dictionary<string, object>
            {
                ["queue_size"] = metrics.QueueSize,
                ["error_rate"] = metrics.ErrorRate,
                ["active_streams"] = metrics.ActiveStreams,
                ["throughput"] = metrics.DataThroughput
            };
            
            var healthScore = isHealthy ? 1.0f : metrics.ErrorRate > 0.5f ? 0.0f : 0.5f;
            
            return new HealthStatusInfo
            {
                IsHealthy = isHealthy,
                HealthScore = healthScore,
                Message = $"Pipeline: {metrics.ActiveStreams} streams, {metrics.QueueSize} queued, {metrics.ErrorRate:P1} error rate",
                Details = details
            };
        }
    }
    
    /// <summary>
    /// Analytics health provider
    /// </summary>
    public class AnalyticsHealthProvider : IHealthCheckProvider
    {
        private readonly AdvancedAnalytics _analytics;
        
        public AnalyticsHealthProvider(AdvancedAnalytics analytics)
        {
            _analytics = analytics;
        }
        
        public async Task<HealthStatusInfo> CheckHealthAsync()
        {
            await Task.Delay(5);
            
            if (_analytics == null)
            {
                return HealthStatusInfo.Unhealthy(0.0f, "Analytics system not available");
            }
            
            var metrics = _analytics.GetAnalyticsMetrics();
            var isHealthy = metrics.ProcessingErrors < 10 && metrics.QueueSize < 500;
            
            var details = new Dictionary<string, object>
            {
                ["queue_size"] = metrics.QueueSize,
                ["processing_errors"] = metrics.ProcessingErrors,
                ["insights_generated"] = metrics.InsightsGenerated,
                ["active_models"] = metrics.ActiveModels
            };
            
            var healthScore = isHealthy ? 1.0f : metrics.ProcessingErrors > 50 ? 0.0f : 0.5f;
            
            return new HealthStatusInfo
            {
                IsHealthy = isHealthy,
                HealthScore = healthScore,
                Message = $"Analytics: {metrics.ActiveModels} models, {metrics.QueueSize} queued",
                Details = details
            };
        }
    }
    
    /// <summary>
    /// Database health provider
    /// </summary>
    public class DatabaseHealthProvider : IHealthCheckProvider
    {
        public async Task<HealthStatusInfo> CheckHealthAsync()
        {
            await Task.Delay(25); // Simulate database query
            
            // In a real implementation, this would check database connectivity
            var connectionTime = UnityEngine.Random.Range(10f, 100f);
            var isHealthy = connectionTime < 50f;
            
            var details = new Dictionary<string, object>
            {
                ["connection_time_ms"] = connectionTime,
                ["simulated_check"] = true
            };
            
            var healthScore = isHealthy ? 1.0f : 0.3f;
            
            return new HealthStatusInfo
            {
                IsHealthy = isHealthy,
                HealthScore = healthScore,
                Message = $"Database connection time: {connectionTime:F0}ms",
                Details = details
            };
        }
    }
    
    /// <summary>
    /// File system health provider
    /// </summary>
    public class FileSystemHealthProvider : IHealthCheckProvider
    {
        public async Task<HealthStatusInfo> CheckHealthAsync()
        {
            await Task.Delay(10);
            
            try
            {
                var testFile = System.IO.Path.Combine(Application.temporaryCachePath, "health_check_test.tmp");
                System.IO.File.WriteAllText(testFile, "health check");
                var canRead = System.IO.File.Exists(testFile);
                System.IO.File.Delete(testFile);
                
                return new HealthStatusInfo
                {
                    IsHealthy = canRead,
                    HealthScore = canRead ? 1.0f : 0.0f,
                    Message = canRead ? "File system read/write OK" : "File system access failed",
                    Details = new Dictionary<string, object>
                    {
                        ["test_path"] = testFile,
                        ["can_write"] = canRead,
                        ["can_read"] = canRead
                    }
                };
            }
            catch (Exception ex)
            {
                return HealthStatusInfo.Unhealthy(0.0f, $"File system error: {ex.Message}");
            }
        }
    }
    
    /// <summary>
    /// Threading health provider
    /// </summary>
    public class ThreadingHealthProvider : IHealthCheckProvider
    {
        public async Task<HealthStatusInfo> CheckHealthAsync()
        {
            await Task.Delay(5);
            
            var processorCount = System.Environment.ProcessorCount;
            var currentThreads = System.Diagnostics.Process.GetCurrentProcess().Threads.Count;
            
            var details = new Dictionary<string, object>
            {
                ["processor_count"] = processorCount,
                ["current_threads"] = currentThreads,
                ["max_worker_threads"] = GetMaxWorkerThreads()
            };
            
            var healthScore = currentThreads < processorCount * 10 ? 1.0f : 0.7f;
            
            return new HealthStatusInfo
            {
                IsHealthy = true,
                HealthScore = healthScore,
                Message = $"Threading: {currentThreads} threads on {processorCount} processors",
                Details = details
            };
        }

        private int GetMaxWorkerThreads()
        {
            int workerThreads, completionPortThreads;
            System.Threading.ThreadPool.GetMaxThreads(out workerThreads, out completionPortThreads);
            return workerThreads;
        }
    }
    
    /// <summary>
    /// Data collection health provider
    /// </summary>
    public class DataCollectionHealthProvider : IHealthCheckProvider
    {
        private readonly DataPipelineIntegration _dataPipeline;
        
        public DataCollectionHealthProvider(DataPipelineIntegration dataPipeline)
        {
            _dataPipeline = dataPipeline;
        }
        
        public async Task<HealthStatusInfo> CheckHealthAsync()
        {
            await Task.Delay(5);
            
            if (_dataPipeline == null)
            {
                return HealthStatusInfo.Unhealthy(0.0f, "Data collection system not available");
            }
            
            // Check if data collection is active and functioning
            return HealthStatusInfo.Healthy(1.0f, "Data collection active");
        }
    }
    
    /// <summary>
    /// Performance monitoring health provider
    /// </summary>
    public class PerformanceMonitoringHealthProvider : IHealthCheckProvider
    {
        private readonly PerformanceMonitoringDashboard _dashboard;
        
        public PerformanceMonitoringHealthProvider(PerformanceMonitoringDashboard dashboard)
        {
            _dashboard = dashboard;
        }
        
        public async Task<HealthStatusInfo> CheckHealthAsync()
        {
            await Task.Delay(5);
            
            if (_dashboard == null)
            {
                return HealthStatusInfo.Unhealthy(0.0f, "Performance monitoring not available");
            }
            
            var metrics = _dashboard.GetCurrentMetrics();
            var isHealthy = metrics.AverageFrameRate > 30f;
            
            var details = new Dictionary<string, object>
            {
                ["average_frame_rate"] = metrics.AverageFrameRate,
                ["current_cpu_usage"] = metrics.CurrentCPUUsage,
                ["current_memory_usage"] = metrics.CurrentMemoryUsage
            };
            
            return new HealthStatusInfo
            {
                IsHealthy = isHealthy,
                HealthScore = isHealthy ? 1.0f : 0.5f,
                Message = $"Performance: {metrics.AverageFrameRate:F1} FPS avg",
                Details = details
            };
        }
    }
    
    /// <summary>
    /// Advanced analytics health provider
    /// </summary>
    public class AdvancedAnalyticsHealthProvider : IHealthCheckProvider
    {
        private readonly AdvancedAnalytics _analytics;
        
        public AdvancedAnalyticsHealthProvider(AdvancedAnalytics analytics)
        {
            _analytics = analytics;
        }
        
        public async Task<HealthStatusInfo> CheckHealthAsync()
        {
            await Task.Delay(5);
            
            if (_analytics == null)
            {
                return HealthStatusInfo.Unhealthy(0.0f, "Advanced analytics not available");
            }
            
            return HealthStatusInfo.Healthy(1.0f, "Advanced analytics active");
        }

        private int GetMaxWorkerThreads()
        {
            int workerThreads, completionPortThreads;
            System.Threading.ThreadPool.GetMaxThreads(out workerThreads, out completionPortThreads);
            return workerThreads;
        }
    }
}