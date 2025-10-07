using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core.Initialization
{
    /// <summary>
    /// REFACTORED: Phase Execution Service - Focused initialization phase management
    /// Single Responsibility: Managing and executing initialization phases for managers
    /// Extracted from GameSystemInitializer for better SRP compliance
    /// </summary>
    public partial class PhaseExecutionService : MonoBehaviour
    {
        private readonly bool _enableLogging;
        private readonly float _phaseDelaySeconds;
        private readonly bool _enableErrorRecovery;
        private readonly int _maxRecoveryAttempts;

        // Phase execution state
        private readonly Dictionary<ChimeraManager, InitializationAttempt> _initializationAttempts = new Dictionary<ChimeraManager, InitializationAttempt>();

        // Events
        public event System.Action<InitializationPhase> OnPhaseStarted;
        public event System.Action<InitializationPhase> OnPhaseCompleted;
        public event System.Action<ChimeraManager, bool> OnManagerInitialized;
        public event System.Action<string> OnPhaseError;

        public PhaseExecutionService(bool enableLogging = true, float phaseDelaySeconds = 0.1f,
                                   bool enableErrorRecovery = true, int maxRecoveryAttempts = 3)
        {
            _enableLogging = enableLogging;
            _phaseDelaySeconds = phaseDelaySeconds;
            _enableErrorRecovery = enableErrorRecovery;
            _maxRecoveryAttempts = maxRecoveryAttempts;
        }

        #region Phase Execution

        /// <summary>
        /// Execute a specific initialization phase with logging and error handling
        /// </summary>
        public IEnumerator ExecutePhase(InitializationPhase phase, System.Func<IEnumerator> phaseCoroutine)
        {
            if (_enableLogging)
                ChimeraLogger.LogInfo("INIT", $"Starting phase: {phase}", this);

            OnPhaseStarted?.Invoke(phase);
            var startTime = DateTime.Now;

            // Execute provided coroutine (exceptions will be logged by inner routines)
            yield return StartCoroutine(phaseCoroutine());

            var duration = (DateTime.Now - startTime).TotalSeconds;
            if (_enableLogging)
                ChimeraLogger.LogInfo("INIT", $"Completed phase {phase} in {duration:F2}s", this);

            OnPhaseCompleted?.Invoke(phase);

            yield return new WaitForSeconds(_phaseDelaySeconds);
        }

        /// <summary>
        /// Initialize managers by category
        /// </summary>
        public IEnumerator InitializeManagersByCategory(IEnumerable<ChimeraManager> managers, ManagerCategory category)
        {
            var managerList = managers.ToList();
            if (_enableLogging)
                ChimeraLogger.LogInfo("INIT", $"Initializing {managerList.Count} {category} managers", this);

            foreach (var manager in managerList)
            {
                yield return StartCoroutine(InitializeManager(manager));
            }

            if (_enableLogging)
                ChimeraLogger.LogInfo("INIT", $"Completed {category} manager initialization", this);
        }

        /// <summary>
        /// Initialize a single manager with error recovery
        /// </summary>
        public IEnumerator InitializeManager(ChimeraManager manager)
        {
            if (manager == null)
            {
                ChimeraLogger.LogWarning("INIT", "Attempted to initialize null manager", this);
                yield break;
            }

            var managerType = manager.GetType();
            if (_enableLogging)
                ChimeraLogger.LogInfo("INIT", $"Initializing manager: {managerType.Name}", this);

            // Track initialization attempts
            if (!_initializationAttempts.ContainsKey(manager))
            {
                _initializationAttempts[manager] = new InitializationAttempt();
            }

            var attempt = _initializationAttempts[manager];
            var attemptIndex = 0;
            var initialized = false;
            string lastError = string.Empty;

            while (attemptIndex < _maxRecoveryAttempts && !initialized)
            {
                attempt.AttemptCount++;
                _lastInitializationFailed = false;
                _lastInitializationError = string.Empty;

                yield return StartCoroutine(ExecuteManagerInitialization(manager));

                if (!_lastInitializationFailed)
                {
                    initialized = true;
                    attempt.Success = true;
                    OnManagerInitialized?.Invoke(manager, true);
                    if (_enableLogging)
                        ChimeraLogger.LogInfo("INIT", $"Successfully initialized: {managerType.Name}", this);
                    break;
                }

                // Failed this attempt
                lastError = _lastInitializationError;
                attempt.LastError = lastError;
                attempt.Success = false;
                attemptIndex++;

                if (_enableErrorRecovery && attemptIndex < _maxRecoveryAttempts)
                {
                    ChimeraLogger.LogWarning("INIT",
                        $"Manager {managerType.Name} initialization failed (attempt {attemptIndex}): {lastError}. Retrying...", this);
                    yield return new WaitForSeconds(0.5f * attemptIndex); // Exponential backoff
                }
                else
                {
                    ChimeraLogger.LogError("INIT",
                        $"Manager {managerType.Name} initialization failed after {attempt.AttemptCount} attempts: {lastError}", this);
                    OnManagerInitialized?.Invoke(manager, false);
                }
            }
        }

        /// <summary>
        /// Execute the actual manager initialization
        /// PHASE 0: Use interface pattern instead of reflection
        /// </summary>
        private IEnumerator ExecuteManagerInitialization(ChimeraManager manager)
        {
            var startTime = Time.realtimeSinceStartup;

            // PHASE 0: No reflection - managers should override Initialize() in ChimeraManager base class
            // ChimeraManager has virtual Initialize() method that can be overridden
            try
            {
                // Call base Initialize() - no reflection needed
                manager.Initialize();
            }
            catch (Exception ex)
            {
                _lastInitializationFailed = true;
                _lastInitializationError = ex.GetBaseException().Message;
                if (_enableLogging)
                    ChimeraLogger.LogError("INIT", $"Initialize() failed for {manager.GetType().Name}: {_lastInitializationError}", this);
                // Early out; don't proceed to async part
                yield break;
            }

            // For IInitializable managers, call InitializeAsync
            if (manager is IInitializable initializableManager)
            {
                yield return StartCoroutine(CallInitializeAsync(initializableManager));
            }

            // Allow one frame for initialization to complete
            yield return null;

            var initTime = (Time.realtimeSinceStartup - startTime) * 1000f;
            if (_enableLogging)
                ChimeraLogger.LogInfo("INIT", $"Manager {manager.GetType().Name} initialized in {initTime:F1}ms", this);
        }

        /// <summary>
        /// Call InitializeAsync for IInitializable managers
        /// </summary>
        private IEnumerator CallInitializeAsync(IInitializable initializable)
        {
            var initializeTask = initializable.InitializeAsync();

            // Wait for the task to complete
            while (!initializeTask.IsCompleted)
            {
                yield return null;
            }

            // Check for errors
            if (initializeTask.IsFaulted && initializeTask.Exception != null)
            {
                _lastInitializationFailed = true;
                _lastInitializationError = initializeTask.Exception.GetBaseException().Message;
                if (_enableLogging)
                    ChimeraLogger.LogError("INIT", $"InitializeAsync() failed: {_lastInitializationError}", this);
            }
        }

        #endregion

        #region Utility Methods

        /// <summary>
        /// Get initialization statistics
        /// </summary>
        public InitializationStatistics GetStatistics()
        {
            var stats = new InitializationStatistics
            {
                TotalManagers = _initializationAttempts.Count,
                SuccessfullyInitialized = _initializationAttempts.Values.Count(a => a.Success),
                FailedInitialization = _initializationAttempts.Values.Count(a => !a.Success),
                TotalAttempts = _initializationAttempts.Values.Sum(a => a.AttemptCount)
            };

            return stats;
        }

        /// <summary>
        /// Get managers that failed initialization
        /// </summary>
        public IEnumerable<ChimeraManager> GetFailedManagers()
        {
            return _initializationAttempts
                .Where(kvp => !kvp.Value.Success)
                .Select(kvp => kvp.Key);
        }

        /// <summary>
        /// Reset initialization tracking
        /// </summary>
        public void ResetInitializationTracking()
        {
            _initializationAttempts.Clear();

            if (_enableLogging)
                ChimeraLogger.LogInfo("INIT", "Reset initialization tracking", this);
        }

        #endregion
    }

    /// <summary>
    /// Initialization phases
    /// </summary>
    public enum InitializationPhase
    {
        Discovery,
        CoreSystems,
        DomainSystems,
        ProgressionSystems,
        UISystems,
        Validation
    }

    /// <summary>
    /// Interface for managers that support async initialization
    /// </summary>
    public interface IInitializable
    {
        System.Threading.Tasks.Task InitializeAsync();
    }

    /// <summary>
    /// Tracks initialization attempts for a manager
    /// </summary>
    [System.Serializable]
    public class InitializationAttempt
    {
        public int AttemptCount = 0;
        public bool Success = false;
        public string LastError = string.Empty;
        public DateTime LastAttemptTime = DateTime.Now;
    }

    /// <summary>
    /// Initialization statistics
    /// </summary>
    [System.Serializable]
    public struct InitializationStatistics
    {
        public int TotalManagers;
        public int SuccessfullyInitialized;
        public int FailedInitialization;
        public int TotalAttempts;
    }

    // Internal state flags (kept internal to this file)
    partial class PhaseExecutionService
    {
        private bool _lastInitializationFailed = false;
        private string _lastInitializationError = string.Empty;
    }
}
