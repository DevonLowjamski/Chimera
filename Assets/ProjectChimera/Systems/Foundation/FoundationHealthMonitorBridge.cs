using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core;
using ProjectChimera.Core.Foundation;

namespace ProjectChimera.Systems.Foundation
{
    /// <summary>
    /// REFACTORED: Thin MonoBehaviour wrapper for FoundationHealthMonitorService
    /// Bridges Unity lifecycle events to Core layer service
    /// Complies with clean architecture: Unity-specific code in Systems, business logic in Core
    /// </summary>
    public class FoundationHealthMonitorBridge : MonoBehaviour
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
        private static FoundationHealthMonitorBridge _instance;

        public static FoundationHealthMonitorBridge Instance => _instance;

        public bool IsEnabled => _service?.IsEnabled ?? false;
        public HealthMonitorStats GetStats() => _service?.GetStats() ?? new HealthMonitorStats();

        // Events
        public event System.Action<string, SystemHealth> OnHealthChanged
        {
            add { if (_service != null) _service.OnHealthChanged += value; }
            remove { if (_service != null) _service.OnHealthChanged -= value; }
        }

        public event System.Action<string, SystemHealth> OnHealthAlert
        {
            add { if (_service != null) _service.OnHealthAlert += value; }
            remove { if (_service != null) _service.OnHealthAlert -= value; }
        }

        public event System.Action<string> OnCriticalSystemFailure
        {
            add { if (_service != null) _service.OnCriticalSystemFailure += value; }
            remove { if (_service != null) _service.OnCriticalSystemFailure -= value; }
        }

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                InitializeService();
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
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

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }
    }
}
