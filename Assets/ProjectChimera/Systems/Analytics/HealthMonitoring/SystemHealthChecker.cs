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

namespace ProjectChimera.Systems.Analytics.HealthMonitoring
{
    /// <summary>
    /// Handles execution and validation of system health checks
    /// Focused component extracted from SystemHealthMonitoring
    /// </summary>
    public class SystemHealthChecker : MonoBehaviour
    {
        [Header("Health Check Configuration")]
        [SerializeField] private float _healthCheckInterval = 5f;
        [SerializeField] private int _maxRetries = 3;
        [SerializeField] private float _retryDelay = 2f;

        private readonly Dictionary<string, IHealthCheckProvider> _healthProviders = new Dictionary<string, IHealthCheckProvider>();
        private float _lastHealthCheckTime = 0f;

        public void ProcessHealthChecks(float deltaTime)
        {
            if (Time.time - _lastHealthCheckTime >= _healthCheckInterval)
            {
                ExecuteHealthChecks();
                _lastHealthCheckTime = Time.time;
            }
        }

        private void ExecuteHealthChecks()
        {
            foreach (var provider in _healthProviders.Values)
            {
                try
                {
                    var result = provider.CheckHealth();
                    ProcessHealthCheckResult(result);
                }
                catch (System.Exception ex)
                {
                    ChimeraLogger.LogError($"Health check failed: {ex.Message}");
                }
            }
        }

        private void ProcessHealthCheckResult(HealthCheckResult result)
        {
            // Process and store health check results
            var dataStorage = GetComponent<SystemHealthDataStorage>();
            dataStorage?.StoreHealthCheckResult(result);

            // Trigger alerts if needed
            if (result.Status != HealthStatus.Healthy)
            {
                var alertManager = GetComponent<SystemAlertManager>();
                alertManager?.TriggerHealthAlert(result);
            }
        }

        public void RegisterHealthProvider(string systemId, IHealthCheckProvider provider)
        {
            _healthProviders[systemId] = provider;
        }

        public void UnregisterHealthProvider(string systemId)
        {
            _healthProviders.Remove(systemId);
        }
    }
}
