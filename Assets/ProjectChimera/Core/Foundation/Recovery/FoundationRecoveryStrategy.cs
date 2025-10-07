using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using ProjectChimera.Core.Logging;
using Logger = ProjectChimera.Core.Logging.ChimeraLogger;
using System.Linq;
using ProjectChimera.Core.Foundation;

namespace ProjectChimera.Core.Foundation.Recovery
{
    /// <summary>
    /// REFACTORED: Foundation Recovery Strategy - Focused recovery strategy execution and management
    /// Handles strategy selection, execution, and validation for failed systems
    /// Single Responsibility: Recovery strategy execution and management
    /// </summary>
    public class FoundationRecoveryStrategy : MonoBehaviour
    {
        [Header("Strategy Settings")]
        [SerializeField] private bool _enableStrategyExecution = true;
        [SerializeField] private bool _enableLogging = false;
        [SerializeField] private int _maxConcurrentRecoveries = 3;

        [Header("Strategy Configuration")]
        [SerializeField] private bool _enableReinitializeStrategy = true;
        [SerializeField] private bool _enableRestartStrategy = true;
        [SerializeField] private bool _enableDependencyRecoveryStrategy = true;
        [SerializeField] private bool _enableGracefulDegradationStrategy = true;

        // Active recovery operations
        private readonly HashSet<string> _activeRecoveries = new HashSet<string>();
        private readonly Dictionary<string, Coroutine> _recoveryCoroutines = new Dictionary<string, Coroutine>();

        // System references
        private FoundationSystemRegistry _systemRegistry;

        // Properties
        public bool IsEnabled { get; private set; } = true;
        public int ActiveRecoveryCount => _activeRecoveries.Count;

        // Events
        public System.Action<string, RecoveryStrategy> OnRecoveryStarted;
        public System.Action<string, bool, RecoveryStrategy, float> OnRecoveryCompleted;
        public System.Action<string, RecoveryStrategy, string> OnRecoveryFailed;
        public System.Action<string> OnSystemDegraded;

        private void Start()
        {
            Initialize();
        }

        private void Initialize()
        {
            // Use SafeResolve helper for clean dependency injection with fallback
            _systemRegistry = DependencyResolutionHelper.SafeResolve<FoundationSystemRegistry>(this, "FOUNDATION");

            if (_systemRegistry == null)
            {
                Logger.LogError("FOUNDATION", "Critical dependency FoundationSystemRegistry not found", this);
            }

            if (_enableLogging)
                Logger.Log("FOUNDATION", "⚡ FoundationRecoveryStrategy initialized", this);
        }

        /// <summary>
        /// Start recovery operation for specific system
        /// </summary>
        public bool StartRecovery(string systemName, RecoveryTrigger trigger)
        {
            if (!IsEnabled || !_enableStrategyExecution) return false;

            if (_activeRecoveries.Contains(systemName))
            {
                if (_enableLogging)
                    Logger.LogWarning("FOUNDATION", $"Recovery already active for: {systemName}", this);
                return false;
            }

            if (_activeRecoveries.Count >= _maxConcurrentRecoveries)
            {
                if (_enableLogging)
                    Logger.LogWarning("FOUNDATION", $"Maximum concurrent recoveries reached, queuing: {systemName}", this);
                return false;
            }

            var system = _systemRegistry?.GetSystem(systemName);
            if (system == null)
            {
                if (_enableLogging)
                    Logger.LogWarning("FOUNDATION", $"System not found for recovery: {systemName}", this);
                return false;
            }

            // Determine appropriate recovery strategy
            var strategy = DetermineRecoveryStrategy(system);

            _activeRecoveries.Add(systemName);
            OnRecoveryStarted?.Invoke(systemName, strategy);

            if (_enableLogging)
                Logger.Log("FOUNDATION", $"Starting recovery for {systemName} using {strategy} strategy", this);

            // Start recovery coroutine
            var coroutine = StartCoroutine(ExecuteRecoveryStrategy(system, strategy));
            _recoveryCoroutines[systemName] = coroutine;

            return true;
        }

        /// <summary>
        /// Stop recovery operation for specific system
        /// </summary>
        public bool StopRecovery(string systemName)
        {
            if (!_activeRecoveries.Contains(systemName)) return false;

            if (_recoveryCoroutines.TryGetValue(systemName, out var coroutine))
            {
                StopCoroutine(coroutine);
                _recoveryCoroutines.Remove(systemName);
            }

            _activeRecoveries.Remove(systemName);

            if (_enableLogging)
                Logger.Log("FOUNDATION", $"Recovery stopped for: {systemName}", this);

            return true;
        }

        /// <summary>
        /// Check if system is under recovery
        /// </summary>
        public bool IsSystemUnderRecovery(string systemName)
        {
            return _activeRecoveries.Contains(systemName);
        }

        /// <summary>
        /// Get systems currently under recovery
        /// </summary>
        public string[] GetSystemsUnderRecovery()
        {
            return _activeRecoveries.ToArray();
        }

        /// <summary>
        /// Set system enabled/disabled
        /// </summary>
        public void SetEnabled(bool enabled)
        {
            IsEnabled = enabled;

            if (!enabled)
            {
                // Stop all active recoveries
                var activeRecoveries = _activeRecoveries.ToArray();
                foreach (var systemName in activeRecoveries)
                {
                    StopRecovery(systemName);
                }
            }

            if (_enableLogging)
                Logger.Log("FOUNDATION", $"FoundationRecoveryStrategy: {(enabled ? "enabled" : "disabled")}", this);
        }

        #region Private Methods

        /// <summary>
        /// Determine appropriate recovery strategy for system
        /// </summary>
        private RecoveryStrategy DetermineRecoveryStrategy(IFoundationSystem system)
        {
            // Strategy selection logic based on system type and previous attempts
            var systemHealth = system.CheckHealth();

            // For critical failures, try restart first
            if (systemHealth == SystemHealth.Failed)
            {
                if (_enableRestartStrategy)
                    return RecoveryStrategy.Restart;
            }

            // For degraded systems, try reinitialization
            if (systemHealth == SystemHealth.Critical || systemHealth == SystemHealth.Warning)
            {
                if (_enableReinitializeStrategy)
                    return RecoveryStrategy.Reinitialize;
            }

            // Try dependency recovery for systems with dependencies
            if (_enableDependencyRecoveryStrategy && _systemRegistry != null)
            {
                var dependencies = _systemRegistry.GetSystemDependencies(system.SystemName);
                if (dependencies != null && dependencies.Length > 0)
                {
                    return RecoveryStrategy.DependencyRecovery;
                }
            }

            // Fallback to graceful degradation
            if (_enableGracefulDegradationStrategy)
                return RecoveryStrategy.GracefulDegradation;

            // Final fallback
            return RecoveryStrategy.Restart;
        }

        /// <summary>
        /// Execute recovery strategy
        /// </summary>
        private IEnumerator ExecuteRecoveryStrategy(IFoundationSystem system, RecoveryStrategy strategy)
        {
            string systemName = system.SystemName;
            bool success = false;
            string errorMessage = null;
            float startTime = Time.time;

            try
            {
                switch (strategy)
                {
                    case RecoveryStrategy.Reinitialize:
                        success = ExecuteReinitializeStrategy(system);
                        break;
                    case RecoveryStrategy.Restart:
                        success = ExecuteRestartStrategy(system);
                        break;
                    case RecoveryStrategy.DependencyRecovery:
                        success = ExecuteDependencyRecoveryStrategy(system);
                        break;
                    case RecoveryStrategy.GracefulDegradation:
                        success = ExecuteGracefulDegradationStrategy(system);
                        break;
                }
            }
            catch (System.Exception ex)
            {
                success = false;
                errorMessage = ex.Message;
            }

            // Wait for recovery to take effect
            yield return new WaitForSeconds(2f);

            // Verify recovery success
            if (success && system.IsEnabled)
            {
                var health = system.CheckHealth();
                success = health == SystemHealth.Healthy || health == SystemHealth.Warning;
            }

            float duration = Time.time - startTime;

            // Clean up tracking
            _activeRecoveries.Remove(systemName);
            _recoveryCoroutines.Remove(systemName);

            // Fire completion events
            if (success)
            {
                OnRecoveryCompleted?.Invoke(systemName, true, strategy, duration);

                if (_enableLogging)
                    Logger.Log("FOUNDATION", $"Successfully recovered {systemName} using {strategy} ({duration:F2}s)", this);
            }
            else
            {
                OnRecoveryFailed?.Invoke(systemName, strategy, errorMessage ?? "Recovery strategy failed");

                if (_enableLogging)
                    Logger.LogError("FOUNDATION", $"Failed to recover {systemName} using {strategy} - {errorMessage}", this);
            }
        }

        /// <summary>
        /// Execute reinitialize recovery strategy
        /// </summary>
        private bool ExecuteReinitializeStrategy(IFoundationSystem system)
        {
            try
            {
                return system.Initialize();
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Execute restart recovery strategy
        /// </summary>
        private bool ExecuteRestartStrategy(IFoundationSystem system)
        {
            try
            {
                system.Shutdown();
                // Brief pause handled by the coroutine that calls this
                return system.Initialize();
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Execute dependency recovery strategy
        /// </summary>
        private bool ExecuteDependencyRecoveryStrategy(IFoundationSystem system)
        {
            if (_systemRegistry == null) return false;

            try
            {
                // Attempt to recover system dependencies first
                var dependencies = _systemRegistry.GetSystemDependencies(system.SystemName);
                if (dependencies != null)
                {
                    foreach (var dependency in dependencies)
                    {
                        var depSystem = _systemRegistry.GetSystem(dependency);
                        if (depSystem != null && !depSystem.IsEnabled)
                        {
                            if (!depSystem.Initialize())
                                return false;
                        }
                    }
                }

                // Then attempt to recover the system itself
                return system.Initialize();
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Execute graceful degradation strategy
        /// </summary>
        private bool ExecuteGracefulDegradationStrategy(IFoundationSystem system)
        {
            try
            {
                // Graceful degradation - disable system but report as successful recovery
                system.SetEnabled(false);
                OnSystemDegraded?.Invoke(system.SystemName);

                if (_enableLogging)
                    Logger.LogWarning("FOUNDATION", $"Applied graceful degradation to: {system.SystemName}", this);

                return true; // Consider degradation as successful recovery
            }
            catch
            {
                return false;
            }
        }

        #endregion

        private void OnDestroy()
        {
            // Stop all active recovery operations
            var activeRecoveries = _activeRecoveries.ToArray();
            foreach (var systemName in activeRecoveries)
            {
                StopRecovery(systemName);
            }
        }
    }
}
