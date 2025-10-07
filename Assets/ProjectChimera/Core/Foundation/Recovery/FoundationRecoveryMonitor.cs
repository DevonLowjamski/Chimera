using UnityEngine;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using System.Linq;
using ProjectChimera.Core.Foundation;

namespace ProjectChimera.Core.Foundation.Recovery
{
    /// <summary>
    /// REFACTORED: Foundation Recovery Monitor - Focused health monitoring and failure detection
    /// Handles system health monitoring, failure detection, and recovery trigger management
    /// Single Responsibility: Health monitoring and recovery trigger management
    /// </summary>
    public class FoundationRecoveryMonitor : MonoBehaviour
    {
        [Header("Monitoring Settings")]
        [SerializeField] private bool _enableHealthMonitoring = true;
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private float _healthCheckInterval = 5f;
        [SerializeField] private int _maxSystemsPerCheck = 10;

        [Header("Health Thresholds")]
        [SerializeField] private float _criticalHealthThreshold = 0.3f; // 30%
        [SerializeField] private float _warningHealthThreshold = 0.6f; // 60%
        [SerializeField] private int _consecutiveFailuresForCritical = 3;

        // System references
        private FoundationSystemRegistry _systemRegistry;
        private FoundationHealthMonitor _healthMonitor;

        // Health tracking
        private readonly Dictionary<string, SystemHealthData> _systemHealthData = new Dictionary<string, SystemHealthData>();
        private readonly List<string> _criticalSystems = new List<string>();
        private readonly List<string> _warningSystems = new List<string>();

        // Timing
        private float _lastHealthCheck;

        // Properties
        public bool IsEnabled { get; private set; } = true;
        public int CriticalSystemCount => _criticalSystems.Count;
        public int WarningSystemCount => _warningSystems.Count;

        // Events
        public System.Action<string, SystemHealth> OnHealthAlert;
        public System.Action<string> OnCriticalFailure;
        public System.Action<string, SystemHealth> OnHealthImproved;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            // Use SafeResolve helper for clean dependency injection with fallback
            _systemRegistry = DependencyResolutionHelper.SafeResolve<FoundationSystemRegistry>(this, "FOUNDATION");
            _healthMonitor = DependencyResolutionHelper.SafeResolve<FoundationHealthMonitor>(this, "FOUNDATION");

            if (_systemRegistry == null)
            {
                ChimeraLogger.LogError("FOUNDATION", "Critical dependency FoundationSystemRegistry not found", this);
            }

            if (_healthMonitor == null)
            {
                ChimeraLogger.LogWarning("FOUNDATION", "FoundationHealthMonitor not found - health monitoring may be limited", this);
            }

            // Subscribe to health monitor events if available
            if (_healthMonitor != null)
            {
                _healthMonitor.OnHealthAlert += HandleExternalHealthAlert;
                _healthMonitor.OnCriticalSystemFailure += HandleExternalCriticalFailure;
            }

            if (_enableLogging)
                ChimeraLogger.Log("FOUNDATION", "🩺 FoundationRecoveryMonitor initialized", this);
        }

        /// <summary>
        /// Process health checks for all systems
        /// </summary>
        public void ProcessHealthChecks()
        {
            if (!IsEnabled || !_enableHealthMonitoring) return;

            if (Time.time - _lastHealthCheck < _healthCheckInterval) return;

            if (_systemRegistry == null)
            {
                if (_enableLogging)
                    ChimeraLogger.LogWarning("FOUNDATION", "System registry not available for health checks", this);
                return;
            }

            var systems = _systemRegistry.GetRegisteredSystems();
            ProcessSystemHealthChecks(systems);

            _lastHealthCheck = Time.time;
        }

        /// <summary>
        /// Get critical systems that need recovery
        /// </summary>
        public string[] GetCriticalSystems()
        {
            return _criticalSystems.ToArray();
        }

        /// <summary>
        /// Get warning systems that may need attention
        /// </summary>
        public string[] GetWarningSystems()
        {
            return _warningSystems.ToArray();
        }

        /// <summary>
        /// Get health data for specific system
        /// </summary>
        public SystemHealthData GetSystemHealthData(string systemName)
        {
            _systemHealthData.TryGetValue(systemName, out var healthData);
            return healthData;
        }

        /// <summary>
        /// Get health data for all systems
        /// </summary>
        public Dictionary<string, SystemHealthData> GetAllSystemHealthData()
        {
            return new Dictionary<string, SystemHealthData>(_systemHealthData);
        }

        /// <summary>
        /// Force health check for specific system
        /// </summary>
        public void ForceHealthCheck(string systemName)
        {
            if (!IsEnabled || _systemRegistry == null) return;

            var system = _systemRegistry.GetSystem(systemName);
            if (system != null)
            {
                ProcessSingleSystemHealthCheck(system);
            }
        }

        /// <summary>
        /// Set system enabled/disabled
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;

            if (!enabled)
            {
                ClearHealthData();
            }

            if (_enableLogging)
                ChimeraLogger.Log("FOUNDATION", $"FoundationRecoveryMonitor: {(enabled ? "enabled" : "disabled")}", this);
        }

        #region Private Methods

        /// <summary>
        /// Process health checks for multiple systems
        /// </summary>
        private void ProcessSystemHealthChecks(IFoundationSystem[] systems)
        {
            int systemsChecked = 0;

            foreach (var system in systems)
            {
                if (systemsChecked >= _maxSystemsPerCheck)
                    break;

                ProcessSingleSystemHealthCheck(system);
                systemsChecked++;
            }

            if (_enableLogging && systemsChecked > 0)
                ChimeraLogger.Log("FOUNDATION", $"Processed health checks for {systemsChecked} systems", this);
        }

        /// <summary>
        /// Process health check for single system
        /// </summary>
        private void ProcessSingleSystemHealthCheck(IFoundationSystem system)
        {
            if (system == null) return;

            string systemName = system.SystemName;
            SystemHealth currentHealth = system.CheckHealth();

            // Get or create health data
            if (!_systemHealthData.TryGetValue(systemName, out var healthData))
            {
                healthData = new SystemHealthData
                {
                    SystemName = systemName,
                    FirstCheckTime = Time.time
                };
            }

            SystemHealth previousHealth = healthData.CurrentHealth;
            healthData.CurrentHealth = currentHealth;
            healthData.LastCheckTime = Time.time;
            healthData.CheckCount++;

            // Update failure tracking
            if (currentHealth == SystemHealth.Failed || currentHealth == SystemHealth.Critical)
            {
                healthData.ConsecutiveFailures++;
                if (healthData.ConsecutiveFailures == 1)
                {
                    healthData.FirstFailureTime = Time.time;
                }
            }
            else
            {
                if (healthData.ConsecutiveFailures > 0)
                {
                    healthData.TotalFailureEpisodes++;
                    healthData.ConsecutiveFailures = 0;
                }
            }

            // Update health history
            if (healthData.HealthHistory == null)
            {
                healthData.HealthHistory = new List<SystemHealth>();
            }
            healthData.HealthHistory.Add(currentHealth);

            // Maintain history size (keep last 50 entries)
            while (healthData.HealthHistory.Count > 50)
            {
                healthData.HealthHistory.RemoveAt(0);
            }

            _systemHealthData[systemName] = healthData;

            // Update system categorization
            UpdateSystemCategorization(systemName, currentHealth);

            // Fire events for health changes
            if (previousHealth != currentHealth)
            {
                HandleHealthChange(systemName, currentHealth, previousHealth);
            }
        }

        /// <summary>
        /// Update system categorization based on health
        /// </summary>
        private void UpdateSystemCategorization(string systemName, SystemHealth health)
        {
            // Remove from all categories first
            _criticalSystems.Remove(systemName);
            _warningSystems.Remove(systemName);

            // Add to appropriate category
            switch (health)
            {
                case SystemHealth.Failed:
                case SystemHealth.Critical:
                    if (!_criticalSystems.Contains(systemName))
                        _criticalSystems.Add(systemName);
                    break;
                case SystemHealth.Warning:
                    if (!_warningSystems.Contains(systemName))
                        _warningSystems.Add(systemName);
                    break;
            }
        }

        /// <summary>
        /// Handle health change events
        /// </summary>
        private void HandleHealthChange(string systemName, SystemHealth currentHealth, SystemHealth previousHealth)
        {
            // Check for critical failure conditions
            var healthData = _systemHealthData[systemName];
            bool isCriticalFailure = (currentHealth == SystemHealth.Failed || currentHealth == SystemHealth.Critical) &&
                                   healthData.ConsecutiveFailures >= _consecutiveFailuresForCritical;

            if (isCriticalFailure)
            {
                OnCriticalFailure?.Invoke(systemName);

                if (_enableLogging)
                    ChimeraLogger.LogError("FOUNDATION", $"Critical failure detected for {systemName} ({healthData.ConsecutiveFailures} consecutive failures)", this);
            }
            else if (currentHealth == SystemHealth.Critical || currentHealth == SystemHealth.Warning)
            {
                OnHealthAlert?.Invoke(systemName, currentHealth);

                if (_enableLogging)
                    ChimeraLogger.LogWarning("FOUNDATION", $"Health alert for {systemName}: {previousHealth} -> {currentHealth}", this);
            }

            // Check for health improvements
            if (IsHealthImprovement(previousHealth, currentHealth))
            {
                OnHealthImproved?.Invoke(systemName, currentHealth);

                if (_enableLogging)
                    ChimeraLogger.Log("FOUNDATION", $"Health improved for {systemName}: {previousHealth} -> {currentHealth}", this);
            }
        }

        /// <summary>
        /// Check if health change represents an improvement
        /// </summary>
        private bool IsHealthImprovement(SystemHealth previous, SystemHealth current)
        {
            int previousScore = GetHealthScore(previous);
            int currentScore = GetHealthScore(current);
            return currentScore > previousScore;
        }

        /// <summary>
        /// Get numeric score for health state
        /// </summary>
        private int GetHealthScore(SystemHealth health)
        {
            return health switch
            {
                SystemHealth.Healthy => 4,
                SystemHealth.Warning => 3,
                SystemHealth.Critical => 2,
                SystemHealth.Failed => 1,
                _ => 0
            };
        }

        /// <summary>
        /// Handle external health alerts
        /// </summary>
        private void HandleExternalHealthAlert(string systemName, SystemHealth health)
        {
            OnHealthAlert?.Invoke(systemName, health);
        }

        /// <summary>
        /// Handle external critical failures
        /// </summary>
        private void HandleExternalCriticalFailure(string systemName)
        {
            OnCriticalFailure?.Invoke(systemName);
        }

        /// <summary>
        /// Clear all health data
        /// </summary>
        private void ClearHealthData()
        {
            _systemHealthData.Clear();
            _criticalSystems.Clear();
            _warningSystems.Clear();

            if (_enableLogging)
                ChimeraLogger.Log("FOUNDATION", "Health data cleared", this);
        }

        #endregion

        private void OnDestroy()
        {
            if (_healthMonitor != null)
            {
                _healthMonitor.OnHealthAlert -= HandleExternalHealthAlert;
                _healthMonitor.OnCriticalSystemFailure -= HandleExternalCriticalFailure;
            }
        }
    }

    #region Data Structures

    /// <summary>
    /// System health data structure
    /// </summary>
    [System.Serializable]
    public struct SystemHealthData
    {
        public string SystemName;
        public SystemHealth CurrentHealth;
        public int ConsecutiveFailures;
        public int TotalFailureEpisodes;
        public int CheckCount;
        public float FirstCheckTime;
        public float LastCheckTime;
        public float FirstFailureTime;
        public List<SystemHealth> HealthHistory;
    }

    #endregion
}
