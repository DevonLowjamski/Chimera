using UnityEngine;
using System.Collections.Generic;

namespace ProjectChimera.Core.Foundation
{
    /// <summary>
    /// DEPRECATED: Use FoundationHealthMonitorService (Core.Foundation) + FoundationHealthMonitorBridge (Systems.Foundation) instead
    /// This wrapper maintained for backward compatibility during migration
    /// Architecture violation: MonoBehaviour in Core layer
    /// </summary>
    [System.Obsolete("Use FoundationHealthMonitorService (Core.Foundation) + FoundationHealthMonitorBridge (Systems.Foundation) instead")]
    public class FoundationHealthMonitor : MonoBehaviour
    {
        [Header("Health Monitoring Settings")]
        [SerializeField] private bool _enableHealthMonitoring = true;
        [SerializeField] private bool _enableLogging = true;
        [SerializeField] private float _healthCheckInterval = 10f;
        [SerializeField] private int _maxHealthHistory = 50;

        [Header("Health Thresholds")]
        [SerializeField] private float _warningThreshold = 0.7f;
        [SerializeField] private float _criticalThreshold = 0.3f;
        [SerializeField] private int _consecutiveFailuresForCritical = 3;

        [Header("Alert Settings")]
        [SerializeField] private bool _enableHealthAlerts = true;
        [SerializeField] private bool _enableCriticalAlerts = true;
        [SerializeField] private float _alertCooldownTime = 30f;

        private FoundationHealthMonitorService _service;

        // Properties
        public bool IsEnabled => _service?.IsEnabled ?? false;
        public HealthMonitorStats GetStats() => _service?.GetStats() ?? new HealthMonitorStats();

        // Events
        public System.Action<string, SystemHealth> OnHealthChanged
        {
            get => _service?.OnHealthChanged;
            set { if (_service != null) _service.OnHealthChanged = value; }
        }

        public System.Action<string, SystemHealth> OnHealthAlert
        {
            get => _service?.OnHealthAlert;
            set { if (_service != null) _service.OnHealthAlert = value; }
        }

        public System.Action<string> OnCriticalSystemFailure
        {
            get => _service?.OnCriticalSystemFailure;
            set { if (_service != null) _service.OnCriticalSystemFailure = value; }
        }

        private void Awake()
        {
            InitializeService();
        }

        private void Start()
        {
            Initialize();
        }

        private void InitializeService()
        {
            _service = new FoundationHealthMonitorService(
                _enableHealthMonitoring,
                _enableLogging,
                _healthCheckInterval,
                _maxHealthHistory,
                _warningThreshold,
                _criticalThreshold,
                _consecutiveFailuresForCritical,
                _enableHealthAlerts,
                _enableCriticalAlerts,
                _alertCooldownTime
            );
        }

        #region Public API (delegates to service)

        public void Initialize()
        {
            var systemRegistry = DependencyResolutionHelper.SafeResolve<FoundationSystemRegistry>(this, "FOUNDATION");
            _service?.Initialize(systemRegistry);
        }

        public void MonitorSystemHealth()
            => _service?.MonitorSystemHealth(Time.time);

        public SystemHealth CheckSystemHealth(IFoundationSystem system)
            => _service?.CheckSystemHealth(system, Time.time) ?? SystemHealth.Unknown;

        public SystemHealth GetSystemHealth(string systemName)
            => _service?.GetSystemHealth(systemName) ?? SystemHealth.Unknown;

        public SystemHealthData GetSystemHealthData(string systemName)
            => _service?.GetSystemHealthData(systemName) ?? new SystemHealthData();

        public Dictionary<string, SystemHealthData> GetAllSystemHealthData()
            => _service?.GetAllSystemHealthData() ?? new Dictionary<string, SystemHealthData>();

        public HealthCheckResult[] GetSystemHealthHistory(string systemName)
            => _service?.GetSystemHealthHistory(systemName) ?? new HealthCheckResult[0];

        public string[] GetUnhealthySystems()
            => _service?.GetUnhealthySystems() ?? new string[0];

        public string[] GetCriticalSystems()
            => _service?.GetCriticalSystems() ?? new string[0];

        public HealthReport GenerateHealthReport()
            => _service?.GenerateHealthReport(Time.time) ?? new HealthReport();

        public void SetEnabled(bool enabled)
            => _service?.SetEnabled(enabled);

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// System health data
    /// </summary>
    [System.Serializable]
    public struct SystemHealthData
    {
        public string SystemName;
        public SystemHealth CurrentHealth;
        public float FirstCheckTime;
        public float LastCheckTime;
        public int TotalChecks;
        public int TotalFailures;
        public int ConsecutiveFailures;
        public float HealthScore; // 0.0 to 1.0
    }

    /// <summary>
    /// Health check result
    /// </summary>
    [System.Serializable]
    public struct HealthCheckResult
    {
        public string SystemName;
        public SystemHealth Health;
        public float CheckTime;
        public bool IsResponsive;
        public string ErrorMessage;
    }

    /// <summary>
    /// Health report
    /// </summary>
    [System.Serializable]
    public struct HealthReport
    {
        public float ReportTime;
        public int TotalSystems;
        public int HealthySystems;
        public int WarningSystems;
        public int CriticalSystems;
        public int FailedSystems;
        public List<SystemHealthSummary> SystemDetails;
    }

    /// <summary>
    /// System health summary
    /// </summary>
    [System.Serializable]
    public struct SystemHealthSummary
    {
        public string SystemName;
        public SystemHealth Health;
        public float LastCheckTime;
        public int ConsecutiveFailures;
        public float HealthScore;
    }

    /// <summary>
    /// Health monitor statistics
    /// </summary>
    [System.Serializable]
    public struct HealthMonitorStats
    {
        public int TotalSystemsMonitored;
        public int HealthySystems;
        public int UnhealthySystems;
        public int CriticalSystems;
        public float OverallHealthScore;
        public int TotalHealthChecks;
        public int TotalAlerts;
    }

    #endregion
}
