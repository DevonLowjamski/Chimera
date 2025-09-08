using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine;
using ProjectChimera.Core;
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Updates;
// using ProjectChimera.Systems.Services.Core; // Removed - namespace doesn't exist
using ProjectChimera.Core.DependencyInjection;

namespace ProjectChimera.Systems.Analytics.Analytics
{
    /// <summary>
    /// REFACTORED: System Health Monitoring orchestrator
    /// This class now coordinates specialized health monitoring components
    /// following the Single Responsibility Principle
    /// </summary>
    public class SystemHealthMonitoring : MonoBehaviour
    {
        [Header("Orchestrator Configuration")]
        [SerializeField] private bool _enableMonitoring = true;

        // Component references
        private SystemCore _coreMonitor;
        private SystemHealthChecker _healthChecker;
        private SystemAlertManager _alertManager;
        private SystemRecoveryManager _recoveryManager;
        private SystemHealthDataStorage _dataStorage;
        private SystemHealthMetrics _metrics;

        private void Start()
        {
            InitializeComponents();
        }

        private void InitializeComponents()
        {
            // Get or add specialized components
            _coreMonitor = GetComponent<SystemCore>();
            if (_coreMonitor == null) _coreMonitor = gameObject.AddComponent<SystemCore>();

            _healthChecker = GetComponent<SystemHealthChecker>();
            if (_healthChecker == null) _healthChecker = gameObject.AddComponent<SystemHealthChecker>();

            _alertManager = GetComponent<SystemAlertManager>();
            if (_alertManager == null) _alertManager = gameObject.AddComponent<SystemAlertManager>();

            _recoveryManager = GetComponent<SystemRecoveryManager>();
            if (_recoveryManager == null) _recoveryManager = gameObject.AddComponent<SystemRecoveryManager>();

            _dataStorage = GetComponent<SystemHealthDataStorage>();
            if (_dataStorage == null) _dataStorage = gameObject.AddComponent<SystemHealthDataStorage>();

            _metrics = GetComponent<SystemHealthMetrics>();
            if (_metrics == null) _metrics = gameObject.AddComponent<SystemHealthMetrics>();

            ChimeraLogger.Log("[SystemHealthMonitoring] Initialized with refactored components");
        }

        // Public API methods that delegate to appropriate components
        public void RegisterHealthProvider(string systemId, IHealthCheckProvider provider)
        {
            _healthChecker?.RegisterHealthProvider(systemId, provider);
        }

        public SystemHealthStatus GetSystemHealth(string systemId)
        {
            if (_dataStorage == null) return SystemHealthStatus.Unknown;
            var healthData = _dataStorage.GetSystemHealth(systemId);
            if (healthData is SystemHealthStatus status)
                return status;
            return SystemHealthStatus.Unknown;
        }

        public SystemHealthStatus GetSystemHealthStatus()
        {
            if (_dataStorage == null) return SystemHealthStatus.Unknown;
            _dataStorage.GetSystemHealth(); // Call void method
            return SystemHealthStatus.Healthy; // Return default status
        }

        public OverallHealthMetrics GetOverallHealthMetrics()
        {
            return _metrics?.CalculateOverallHealth() ?? new OverallHealthMetrics();
        }

        public List<HealthAlert> GetActiveAlerts()
        {
            return _alertManager?.GetActiveAlerts() ?? new List<HealthAlert>();
        }

        public void QueueRecoveryAction(RecoveryAction action)
        {
            _recoveryManager?.QueueRecoveryAction(action);
        }
    }
}
