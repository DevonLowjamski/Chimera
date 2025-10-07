using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using System.Linq;
using System;

namespace ProjectChimera.Core.Foundation
{
    /// <summary>
    /// REFACTORED: Foundation Health Monitor Service (POCO - Unity-independent core)
    /// Single Responsibility: System health monitoring and diagnostics
    /// Extracted from FoundationHealthMonitor for clean architecture compliance
    /// </summary>
    public class FoundationHealthMonitorService
    {
        private readonly bool _enableHealthMonitoring;
        private readonly bool _enableLogging;
        private readonly float _healthCheckInterval;
        private readonly int _maxHealthHistory;
        private readonly float _warningThreshold;
        private readonly float _criticalThreshold;
        private readonly int _consecutiveFailuresForCritical;
        private readonly bool _enableHealthAlerts;
        private readonly bool _enableCriticalAlerts;
        private readonly float _alertCooldownTime;

        // Health tracking
        private readonly Dictionary<string, SystemHealthData> _systemHealthData = new Dictionary<string, SystemHealthData>();
        private readonly Dictionary<string, Queue<HealthCheckResult>> _healthHistory = new Dictionary<string, Queue<HealthCheckResult>>();
        private readonly Dictionary<string, float> _lastAlertTime = new Dictionary<string, float>();

        // System references
        private FoundationSystemRegistry _systemRegistry;

        // Statistics
        private HealthMonitorStats _stats = new HealthMonitorStats();

        // Properties
        public bool IsEnabled { get; private set; } = true;
        public HealthMonitorStats GetStats() => _stats;

        // Events
        public Action<string, SystemHealth> OnHealthChanged;
        public Action<string, SystemHealth> OnHealthAlert;
        public Action<string> OnCriticalSystemFailure;

        public FoundationHealthMonitorService(
            bool enableHealthMonitoring = true,
            bool enableLogging = true,
            float healthCheckInterval = 10f,
            int maxHealthHistory = 50,
            float warningThreshold = 0.7f,
            float criticalThreshold = 0.3f,
            int consecutiveFailuresForCritical = 3,
            bool enableHealthAlerts = true,
            bool enableCriticalAlerts = true,
            float alertCooldownTime = 30f)
        {
            _enableHealthMonitoring = enableHealthMonitoring;
            _enableLogging = enableLogging;
            _healthCheckInterval = healthCheckInterval;
            _maxHealthHistory = maxHealthHistory;
            _warningThreshold = warningThreshold;
            _criticalThreshold = criticalThreshold;
            _consecutiveFailuresForCritical = consecutiveFailuresForCritical;
            _enableHealthAlerts = enableHealthAlerts;
            _enableCriticalAlerts = enableCriticalAlerts;
            _alertCooldownTime = alertCooldownTime;
        }

        public void Initialize(FoundationSystemRegistry systemRegistry)
        {
            _stats = new HealthMonitorStats();
            _systemRegistry = systemRegistry;

            if (_systemRegistry == null)
            {
                ChimeraLogger.LogError("FOUNDATION", "Critical dependency FoundationSystemRegistry not found", null);
            }

            if (_enableLogging)
                ChimeraLogger.Log("FOUNDATION", "🏥 FoundationHealthMonitor initialized", null);
        }

        /// <summary>
        /// Monitor all registered system health
        /// </summary>
        public void MonitorSystemHealth(float currentTime)
        {
            if (!IsEnabled || !_enableHealthMonitoring || _systemRegistry == null)
                return;

            var systems = _systemRegistry.GetRegisteredSystems();
            foreach (var system in systems)
            {
                CheckSystemHealth(system, currentTime);
            }

            UpdateHealthStatistics();
        }

        /// <summary>
        /// Check specific system health
        /// </summary>
        public SystemHealth CheckSystemHealth(IFoundationSystem system, float currentTime)
        {
            if (!IsEnabled || system == null)
                return SystemHealth.Unknown;

            try
            {
                var currentHealth = system.CheckHealth();
                var healthResult = new HealthCheckResult
                {
                    SystemName = system.SystemName,
                    Health = currentHealth,
                    CheckTime = currentTime,
                    IsResponsive = true
                };

                ProcessHealthCheckResult(healthResult, currentTime);
                return currentHealth;
            }
            catch (Exception ex)
            {
                var healthResult = new HealthCheckResult
                {
                    SystemName = system.SystemName,
                    Health = SystemHealth.Failed,
                    CheckTime = currentTime,
                    IsResponsive = false,
                    ErrorMessage = ex.Message
                };

                ProcessHealthCheckResult(healthResult, currentTime);
                return SystemHealth.Failed;
            }
        }

        /// <summary>
        /// Get system health by name
        /// </summary>
        public SystemHealth GetSystemHealth(string systemName)
        {
            if (_systemHealthData.TryGetValue(systemName, out var healthData))
            {
                return healthData.CurrentHealth;
            }
            return SystemHealth.Unknown;
        }

        /// <summary>
        /// Get system health data
        /// </summary>
        public SystemHealthData GetSystemHealthData(string systemName)
        {
            _systemHealthData.TryGetValue(systemName, out var healthData);
            return healthData;
        }

        /// <summary>
        /// Get all system health data
        /// </summary>
        public Dictionary<string, SystemHealthData> GetAllSystemHealthData()
        {
            return new Dictionary<string, SystemHealthData>(_systemHealthData);
        }

        /// <summary>
        /// Get health history for system
        /// </summary>
        public HealthCheckResult[] GetSystemHealthHistory(string systemName)
        {
            if (_healthHistory.TryGetValue(systemName, out var history))
            {
                return history.ToArray();
            }
            return new HealthCheckResult[0];
        }

        /// <summary>
        /// Get unhealthy systems
        /// </summary>
        public string[] GetUnhealthySystems()
        {
            var unhealthySystems = new List<string>();

            foreach (var kvp in _systemHealthData)
            {
                if (kvp.Value.CurrentHealth == SystemHealth.Warning ||
                    kvp.Value.CurrentHealth == SystemHealth.Critical ||
                    kvp.Value.CurrentHealth == SystemHealth.Failed)
                {
                    unhealthySystems.Add(kvp.Key);
                }
            }

            return unhealthySystems.ToArray();
        }

        /// <summary>
        /// Get systems requiring immediate attention
        /// </summary>
        public string[] GetCriticalSystems()
        {
            var criticalSystems = new List<string>();

            foreach (var kvp in _systemHealthData)
            {
                if (kvp.Value.CurrentHealth == SystemHealth.Critical ||
                    kvp.Value.CurrentHealth == SystemHealth.Failed)
                {
                    criticalSystems.Add(kvp.Key);
                }
            }

            return criticalSystems.ToArray();
        }

        /// <summary>
        /// Generate health report
        /// </summary>
        public HealthReport GenerateHealthReport(float currentTime)
        {
            var report = new HealthReport
            {
                ReportTime = currentTime,
                TotalSystems = _systemHealthData.Count,
                HealthySystems = 0,
                WarningSystems = 0,
                CriticalSystems = 0,
                FailedSystems = 0,
                SystemDetails = new List<SystemHealthSummary>()
            };

            foreach (var kvp in _systemHealthData)
            {
                var healthData = kvp.Value;

                switch (healthData.CurrentHealth)
                {
                    case SystemHealth.Healthy: report.HealthySystems++; break;
                    case SystemHealth.Warning: report.WarningSystems++; break;
                    case SystemHealth.Critical: report.CriticalSystems++; break;
                    case SystemHealth.Failed: report.FailedSystems++; break;
                }

                report.SystemDetails.Add(new SystemHealthSummary
                {
                    SystemName = kvp.Key,
                    Health = healthData.CurrentHealth,
                    LastCheckTime = healthData.LastCheckTime,
                    ConsecutiveFailures = healthData.ConsecutiveFailures,
                    HealthScore = healthData.HealthScore
                });
            }

            return report;
        }

        /// <summary>
        /// Set system enabled/disabled
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;

            if (!enabled)
            {
                _systemHealthData.Clear();
                _healthHistory.Clear();
                _lastAlertTime.Clear();
            }

            if (_enableLogging)
                ChimeraLogger.Log("FOUNDATION", $"FoundationHealthMonitor: {(enabled ? "enabled" : "disabled")}", null);
        }

        #region Private Methods

        /// <summary>
        /// Process health check result
        /// </summary>
        private void ProcessHealthCheckResult(HealthCheckResult result, float currentTime)
        {
            // Update or create health data
            if (!_systemHealthData.TryGetValue(result.SystemName, out var healthData))
            {
                healthData = new SystemHealthData
                {
                    SystemName = result.SystemName,
                    FirstCheckTime = result.CheckTime
                };
            }

            var previousHealth = healthData.CurrentHealth;
            healthData.CurrentHealth = result.Health;
            healthData.LastCheckTime = result.CheckTime;
            healthData.TotalChecks++;

            // Update consecutive failures
            if (result.Health == SystemHealth.Failed || !result.IsResponsive)
            {
                healthData.ConsecutiveFailures++;
                healthData.TotalFailures++;
            }
            else
            {
                healthData.ConsecutiveFailures = 0;
            }

            // Calculate health score (0.0 to 1.0)
            healthData.HealthScore = CalculateHealthScore(result.Health, healthData.ConsecutiveFailures);

            _systemHealthData[result.SystemName] = healthData;

            // Add to health history
            AddToHealthHistory(result);

            // Check for health changes and alerts
            if (previousHealth != result.Health)
            {
                OnHealthChanged?.Invoke(result.SystemName, result.Health);
                CheckForHealthAlerts(result.SystemName, result.Health, currentTime);
            }

            // Check for critical system failure
            if (healthData.ConsecutiveFailures >= _consecutiveFailuresForCritical &&
                result.Health == SystemHealth.Failed)
            {
                OnCriticalSystemFailure?.Invoke(result.SystemName);

                if (_enableLogging)
                    ChimeraLogger.LogError("FOUNDATION", $"Critical system failure detected: {result.SystemName}", null);
            }
        }

        /// <summary>
        /// Add result to health history
        /// </summary>
        private void AddToHealthHistory(HealthCheckResult result)
        {
            if (!_healthHistory.TryGetValue(result.SystemName, out var history))
            {
                history = new Queue<HealthCheckResult>();
                _healthHistory[result.SystemName] = history;
            }

            history.Enqueue(result);

            // Maintain history size limit
            while (history.Count > _maxHealthHistory)
            {
                history.Dequeue();
            }
        }

        /// <summary>
        /// Calculate health score based on status and failures
        /// </summary>
        private float CalculateHealthScore(SystemHealth health, int consecutiveFailures)
        {
            float baseScore = health switch
            {
                SystemHealth.Healthy => 1.0f,
                SystemHealth.Warning => 0.7f,
                SystemHealth.Critical => 0.3f,
                SystemHealth.Failed => 0.0f,
                _ => 0.5f
            };

            // Reduce score based on consecutive failures
            float failurePenalty = Clamp01(consecutiveFailures * 0.1f);
            return Clamp01(baseScore - failurePenalty);
        }

        /// <summary>
        /// Check for health alerts
        /// </summary>
        private void CheckForHealthAlerts(string systemName, SystemHealth health, float currentTime)
        {
            if (!_enableHealthAlerts)
                return;

            bool shouldAlert = false;
            bool isCritical = false;

            switch (health)
            {
                case SystemHealth.Warning:
                    shouldAlert = true;
                    break;
                case SystemHealth.Critical:
                case SystemHealth.Failed:
                    shouldAlert = true;
                    isCritical = true;
                    break;
            }

            if (shouldAlert && !isCritical && _enableCriticalAlerts)
                shouldAlert = false; // Only critical alerts enabled

            if (shouldAlert && ShouldSendAlert(systemName, currentTime))
            {
                OnHealthAlert?.Invoke(systemName, health);
                _lastAlertTime[systemName] = currentTime;

                if (_enableLogging)
                    ChimeraLogger.LogWarning("FOUNDATION", $"Health alert for system: {systemName} ({health})", null);
            }
        }

        /// <summary>
        /// Check if alert should be sent (respecting cooldown)
        /// </summary>
        private bool ShouldSendAlert(string systemName, float currentTime)
        {
            if (!_lastAlertTime.TryGetValue(systemName, out var lastAlert))
                return true;

            return (currentTime - lastAlert) >= _alertCooldownTime;
        }

        /// <summary>
        /// Update health monitoring statistics
        /// </summary>
        private void UpdateHealthStatistics()
        {
            _stats.TotalSystemsMonitored = _systemHealthData.Count;
            _stats.HealthySystems = _systemHealthData.Values.Count(h => h.CurrentHealth == SystemHealth.Healthy);
            _stats.UnhealthySystems = _systemHealthData.Values.Count(h => h.CurrentHealth != SystemHealth.Healthy);
            _stats.CriticalSystems = _systemHealthData.Values.Count(h => h.CurrentHealth == SystemHealth.Critical || h.CurrentHealth == SystemHealth.Failed);

            if (_stats.TotalSystemsMonitored > 0)
            {
                _stats.OverallHealthScore = _systemHealthData.Values.Average(h => h.HealthScore);
            }
        }

        private float Clamp01(float value)
        {
            if (value < 0f) return 0f;
            if (value > 1f) return 1f;
            return value;
        }

        #endregion
    }
}
