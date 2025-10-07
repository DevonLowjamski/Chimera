using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Initialization;
using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using ProjectChimera.Core.Events;

namespace ProjectChimera.Core
{
    /// <summary>
    /// REFACTORED: Game System Initializer - Coordinator using SRP-compliant components
    /// Single Responsibility: Coordinating the discovery, initialization, and validation of game managers
    /// Uses composition with ManagerDiscoveryService, PhaseExecutionService, and SystemValidationService
    /// Reduced from 518 lines to maintain SRP compliance
    /// </summary>
    public class GameSystemInitializer : MonoBehaviour
    {
        [Header("Initialization Configuration")]
        [SerializeField] private bool _enablePhaseLogging = true;
        [SerializeField] private float _phaseDelaySeconds = 0.1f;
        [SerializeField] private bool _enableErrorRecovery = true;
        [SerializeField] private int _maxRecoveryAttempts = 3;

        [Header("Phase Configuration")]
        [SerializeField] private bool _autoDiscoverManagers = true;
        [SerializeField] private bool _validateDependenciesAfterInit = true;
        [SerializeField] private bool _attemptServiceRecovery = true;

        // Composition: Delegate responsibilities to focused components
        private ManagerDiscoveryService _discoveryService;
        private PhaseExecutionService _phaseExecutionService;
        private SystemValidationService _validationService;

        // Coordinator state
        private bool _isInitializing = false;
        private bool _isInitialized = false;
        private DateTime _initializationStartTime;

        // Events
        public System.Action<InitializationPhase> OnPhaseStarted;
        public System.Action<InitializationPhase> OnPhaseCompleted;
        public System.Action<ChimeraManager, bool> OnManagerInitialized;
        public System.Action<InitializationResult> OnInitializationCompleted;
        public System.Action<string> OnInitializationError;

        // Properties
        public bool IsInitializing => _isInitializing;
        public bool IsInitialized => _isInitialized;
        public int DiscoveredManagerCount => _discoveryService?.DiscoveredManagerCount ?? 0;
        public IEnumerable<ChimeraManager> DiscoveredManagers => _discoveryService?.DiscoveredManagers ?? Enumerable.Empty<ChimeraManager>();

        #region Unity Lifecycle

        private void Awake()
        {
            InitializeComponents();
        }

        private void OnDestroy()
        {
            Cleanup();
        }

        #endregion

        #region Initialization

        private void InitializeComponents()
        {
            try
            {
                // Initialize services using composition
                _discoveryService = new ManagerDiscoveryService(_enablePhaseLogging, _autoDiscoverManagers);
                _phaseExecutionService = gameObject.AddComponent<PhaseExecutionService>();
                _validationService = new SystemValidationService(_enablePhaseLogging, _validateDependenciesAfterInit, _attemptServiceRecovery);

                // Wire up events between components
                _phaseExecutionService.OnPhaseStarted += OnPhaseStartedInternal;
                _phaseExecutionService.OnPhaseCompleted += OnPhaseCompletedInternal;
                _phaseExecutionService.OnManagerInitialized += OnManagerInitializedInternal;
                _phaseExecutionService.OnPhaseError += OnInitializationErrorInternal;

                _discoveryService.OnManagerDiscovered += OnManagerDiscoveredInternal;
                _validationService.OnManagerValidated += OnManagerValidatedInternal;

                if (_enablePhaseLogging)
                {
                    ChimeraLogger.LogInfo("INIT", "GameSystemInitializer components initialized", this);
                }
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError("INIT", $"Failed to initialize GameSystemInitializer components: {ex.Message}", this);
            }
        }

        /// <summary>
        /// Start the complete game system initialization process using composed services
        /// </summary>
        public IEnumerator InitializeAllGameSystems()
        {
            if (_isInitializing)
            {
                if (_enablePhaseLogging)
                    ChimeraLogger.LogInfo("INIT", "Initialization already in progress, skipping", this);
                yield break;
            }

            _isInitializing = true;
            _initializationStartTime = DateTime.Now;

            if (_enablePhaseLogging)
                ChimeraLogger.LogInfo("INIT", "Starting comprehensive game system initialization with composition pattern", this);

            var result = new InitializationResult();

            try
            {
                // Phase 0: Auto-discover managers
                yield return StartCoroutine(_phaseExecutionService.ExecutePhase(InitializationPhase.Discovery,
                    () => SafeCoroutine(DiscoverAllManagers)));

                var discoveredManagers = _discoveryService.DiscoveredManagers.ToList();
                if (discoveredManagers.Count == 0)
                {
                    throw new InvalidOperationException("No managers discovered for initialization");
                }

                // Phase 1: Initialize core systems first
                yield return StartCoroutine(_phaseExecutionService.ExecutePhase(InitializationPhase.CoreSystems,
                    () => SafeCoroutine(() => _phaseExecutionService.InitializeManagersByCategory(
                        _discoveryService.GetManagersByCategory(ManagerCategory.Core), ManagerCategory.Core))));

                // Phase 2: Initialize domain systems
                yield return StartCoroutine(_phaseExecutionService.ExecutePhase(InitializationPhase.DomainSystems,
                    () => SafeCoroutine(() => _phaseExecutionService.InitializeManagersByCategory(
                        _discoveryService.GetManagersByCategory(ManagerCategory.Domain), ManagerCategory.Domain))));

                // Phase 3: Initialize progression systems
                yield return StartCoroutine(_phaseExecutionService.ExecutePhase(InitializationPhase.ProgressionSystems,
                    () => SafeCoroutine(() => _phaseExecutionService.InitializeManagersByCategory(
                        _discoveryService.GetManagersByCategory(ManagerCategory.Progression), ManagerCategory.Progression))));

                // Phase 4: Initialize UI systems
                yield return StartCoroutine(_phaseExecutionService.ExecutePhase(InitializationPhase.UISystems,
                    () => SafeCoroutine(() => _phaseExecutionService.InitializeManagersByCategory(
                        _discoveryService.GetManagersByCategory(ManagerCategory.UI), ManagerCategory.UI))));

                // Phase 5: Final validation and health checks
                yield return StartCoroutine(_phaseExecutionService.ExecutePhase(InitializationPhase.Validation,
                    () => SafeCoroutine(ValidateAllSystems)));

                result.Success = true;
                result.InitializedManagerCount = discoveredManagers.Count;
                result.InitializationTime = DateTime.Now - _initializationStartTime;

                _isInitialized = true;

                if (_enablePhaseLogging)
                {
                    ChimeraLogger.LogInfo("INIT",
                        $"Game system initialization completed successfully in {result.InitializationTime.TotalSeconds:F2}s", this);
                }
            }
            finally
            {
                _isInitializing = false;
                OnInitializationCompleted?.Invoke(result);
            }
        }

        #endregion

        #region Coordinator Methods

        /// <summary>
        /// Discover all managers using the discovery service
        /// </summary>
        private IEnumerator DiscoverAllManagers()
        {
            var result = _discoveryService.DiscoverAllManagers();
            yield return null; // Allow one frame for discovery

            if (!result.Success)
            {
                throw new InvalidOperationException($"Manager discovery failed: {result.ErrorMessage}");
            }

            if (_enablePhaseLogging)
            {
                ChimeraLogger.LogInfo("INIT",
                    $"Discovered {result.SuccessfullyDiscovered} managers in {result.DiscoveryTime.TotalMilliseconds:F1}ms", this);
            }
        }

        /// <summary>
        /// Validate all systems using the validation service
        /// </summary>
        private IEnumerator ValidateAllSystems()
        {
            var discoveredManagers = _discoveryService.DiscoveredManagers;
            var validationSummary = _validationService.ValidateAllSystems(discoveredManagers);

            yield return null; // Allow one frame for validation

            if (!validationSummary.OverallValid)
            {
                var errorMessage = $"System validation failed: {validationSummary.InvalidSystems} invalid systems";
                if (_enablePhaseLogging)
                {
                    ChimeraLogger.LogWarning("INIT", errorMessage, this);
                    foreach (var error in validationSummary.AllErrors.Take(5)) // Log first 5 errors
                    {
                        ChimeraLogger.LogWarning("INIT", $"Validation error: {error}", this);
                    }
                }
            }
            else if (_enablePhaseLogging)
            {
                ChimeraLogger.LogInfo("INIT",
                    $"All {validationSummary.ValidSystems} systems validated successfully", this);
            }
        }

        #endregion

        #region Event Handlers

        private void OnPhaseStartedInternal(InitializationPhase phase)
        {
            OnPhaseStarted?.Invoke(phase);
        }

        private void OnPhaseCompletedInternal(InitializationPhase phase)
        {
            OnPhaseCompleted?.Invoke(phase);
        }

        private void OnManagerInitializedInternal(ChimeraManager manager, bool success)
        {
            OnManagerInitialized?.Invoke(manager, success);
        }

        private void OnInitializationErrorInternal(string error)
        {
            OnInitializationError?.Invoke(error);
        }

        private void OnManagerDiscoveredInternal(ChimeraManager manager)
        {
            if (_enablePhaseLogging)
                ChimeraLogger.LogInfo("INIT", $"Discovered manager: {manager.GetType().Name}", this);
        }

        private void OnManagerValidatedInternal(ChimeraManager manager, bool isValid)
        {
            if (_enablePhaseLogging && !isValid)
                ChimeraLogger.LogWarning("INIT", $"Manager validation failed: {manager.GetType().Name}", this);
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get initialization statistics from all services
        /// </summary>
        public InitializationStatistics GetInitializationStatistics()
        {
            var discoveryStats = _discoveryService?.GetStatistics() ?? new DiscoveryStatistics();
            var phaseStats = _phaseExecutionService?.GetStatistics() ?? new ProjectChimera.Core.Initialization.InitializationStatistics();

            return new InitializationStatistics
            {
                DiscoveredManagers = discoveryStats.TotalDiscovered,
                InitializedManagers = phaseStats.SuccessfullyInitialized,
                FailedInitializations = phaseStats.FailedInitialization,
                IsInitialized = _isInitialized
            };
        }

        /// <summary>
        /// Get a specific manager by type
        /// </summary>
        public T GetManager<T>() where T : ChimeraManager
        {
            return _discoveryService?.GetManager<T>();
        }

        /// <summary>
        /// Check if initialization is complete and successful
        /// </summary>
        public bool IsSystemReady()
        {
            return _isInitialized && !_isInitializing && DiscoveredManagerCount > 0;
        }

        #endregion

        #region Cleanup

        private void Cleanup()
        {
            try
            {
                // Cleanup event handlers
                if (_phaseExecutionService != null)
                {
                    _phaseExecutionService.OnPhaseStarted -= OnPhaseStartedInternal;
                    _phaseExecutionService.OnPhaseCompleted -= OnPhaseCompletedInternal;
                    _phaseExecutionService.OnManagerInitialized -= OnManagerInitializedInternal;
                    _phaseExecutionService.OnPhaseError -= OnInitializationErrorInternal;
                }

                if (_discoveryService != null)
                {
                    _discoveryService.OnManagerDiscovered -= OnManagerDiscoveredInternal;
                }

                if (_validationService != null)
                {
                    _validationService.OnManagerValidated -= OnManagerValidatedInternal;
                }

                if (_enablePhaseLogging)
                    ChimeraLogger.LogInfo("INIT", "GameSystemInitializer cleanup completed", this);
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError("INIT", $"Error during GameSystemInitializer cleanup: {ex.Message}", this);
            }
        }

        #endregion

        #region Helpers

        // Wrap a coroutine factory in a try/catch without yielding inside try/catch
        private IEnumerator SafeCoroutine(Func<IEnumerator> coroutineFactory)
        {
            IEnumerator inner = null;
            try
            {
                inner = coroutineFactory?.Invoke();
            }
            catch (Exception ex)
            {
                ChimeraLogger.LogError("INIT", $"Coroutine creation failed: {ex.Message}", this);
            }

            if (inner != null)
            {
                // run the yielded coroutine outside try/catch
                yield return StartCoroutine(inner);
            }
        }

        #endregion
    }

    #region Data Structures

    /// <summary>
    /// Overall initialization result
    /// </summary>
    [System.Serializable]
    public struct InitializationResult
    {
        public bool Success;
        public int InitializedManagerCount;
        public TimeSpan InitializationTime;
        public string ErrorMessage;
    }

    /// <summary>
    /// Initialization statistics
    /// </summary>
    [System.Serializable]
    public struct InitializationStatistics
    {
        public int DiscoveredManagers;
        public int InitializedManagers;
        public int FailedInitializations;
        public bool IsInitialized;
    }

    #endregion
}
