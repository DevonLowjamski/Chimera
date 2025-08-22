using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Linq;
using ProjectChimera.Core.Events;

namespace ProjectChimera.Core
{
    /// <summary>
    /// Handles systematic discovery and initialization of all game managers.
    /// Extracted from DIGameManager for modular architecture.
    /// Manages phased initialization with dependency resolution.
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
        
        // State tracking
        private Dictionary<Type, ChimeraManager> _discoveredManagers = new Dictionary<Type, ChimeraManager>();
        private List<ChimeraManager> _initializationOrder = new List<ChimeraManager>();
        private bool _isInitializing = false;
        private DateTime _initializationStartTime;
        
        // Events
        public System.Action<InitializationPhase> OnPhaseStarted;
        public System.Action<InitializationPhase> OnPhaseCompleted;
        public System.Action<ChimeraManager, bool> OnManagerInitialized;
        public System.Action<InitializationResult> OnInitializationCompleted;
        public System.Action<string> OnInitializationError;
        
        // Properties
        public bool IsInitializing => _isInitializing;
        public int DiscoveredManagerCount => _discoveredManagers.Count;
        public IEnumerable<ChimeraManager> DiscoveredManagers => _discoveredManagers.Values;
        
        /// <summary>
        /// Start the complete game system initialization process
        /// </summary>
        public IEnumerator InitializeAllGameSystems()
        {
            if (_isInitializing)
            {
                LogDebug("Initialization already in progress, skipping");
                yield break;
            }
            
            _isInitializing = true;
            _initializationStartTime = DateTime.Now;
            LogDebug("Starting comprehensive game system initialization");
            
            var result = new InitializationResult();
            
            // Execute initialization phases - no try-catch to allow yield statements
            // Phase 0: Auto-discover managers
            yield return StartCoroutine(ExecutePhase(InitializationPhase.Discovery, () => 
                StartCoroutine(DiscoverAllManagers())));
            
            // Phase 1: Initialize core systems first
            yield return StartCoroutine(ExecutePhase(InitializationPhase.CoreSystems, () => 
                StartCoroutine(InitializeCoreManagers())));
            
            // Phase 2: Initialize domain systems
            yield return StartCoroutine(ExecutePhase(InitializationPhase.DomainSystems, () => 
                StartCoroutine(InitializeDomainManagers())));
            
            // Phase 3: Initialize progression systems
            yield return StartCoroutine(ExecutePhase(InitializationPhase.ProgressionSystems, () => 
                StartCoroutine(InitializeProgressionManagers())));
            
            // Phase 4: Initialize UI systems
            yield return StartCoroutine(ExecutePhase(InitializationPhase.UISystems, () => 
                StartCoroutine(InitializeUIManagers())));
            
            // Phase 5: Final validation and health checks
            yield return StartCoroutine(ExecutePhase(InitializationPhase.Validation, () => 
                StartCoroutine(ValidateAllSystems())));
            
            result.Success = true;
            result.InitializedManagerCount = _discoveredManagers.Count;
            result.InitializationTime = DateTime.Now - _initializationStartTime;
            
            LogDebug($"Game system initialization completed successfully in {result.InitializationTime.TotalSeconds:F2}s");
            
            _isInitializing = false;
            OnInitializationCompleted?.Invoke(result);
        }
        
        /// <summary>
        /// Execute a specific initialization phase with logging and error handling
        /// </summary>
        private IEnumerator ExecutePhase(InitializationPhase phase, System.Func<Coroutine> phaseCoroutine)
        {
            LogDebug($"Starting phase: {phase}");
            OnPhaseStarted?.Invoke(phase);
            
            var startTime = DateTime.Now;
            
            // Execute phase coroutine - handle errors after yield
            bool phaseSuccessful = false;
            Exception phaseException = null;
            
            // Yield outside try-catch
            yield return phaseCoroutine();
            phaseSuccessful = true;
            
            // Handle results after yield
            if (phaseSuccessful)
            {
                var duration = (DateTime.Now - startTime).TotalSeconds;
                LogDebug($"Completed phase {phase} in {duration:F2}s");
                OnPhaseCompleted?.Invoke(phase);
            }
            else
            {
                LogError($"Phase {phase} failed: {phaseException?.Message}");
                throw phaseException;
            }
            
            yield return new WaitForSeconds(_phaseDelaySeconds);
        }
        
        /// <summary>
        /// Auto-discover all managers in the scene
        /// </summary>
        public IEnumerator DiscoverAllManagers()
        {
            LogDebug("Discovering all ChimeraManager instances");
            
            try
            {
                // Find all ChimeraManager instances using Unity 6 API
                var allManagers = UnityEngine.Object.FindObjectsByType<ChimeraManager>(FindObjectsSortMode.None);
                
                _discoveredManagers.Clear();
                
                foreach (var manager in allManagers)
                {
                    if (manager != null && !_discoveredManagers.ContainsValue(manager))
                    {
                        var managerType = manager.GetType();
                        _discoveredManagers[managerType] = manager;
                        LogDebug($"Discovered manager: {manager.ManagerName} ({managerType.Name})");
                    }
                }
                
                // Sort managers by initialization priority
                _initializationOrder = _discoveredManagers.Values
                    .OrderBy(m => (int)m.Priority)
                    .ToList();
                
                LogDebug($"Discovery complete: {_discoveredManagers.Count} managers found");
            }
            catch (Exception ex)
            {
                LogError($"Manager discovery failed: {ex.Message}");
                throw;
            }
            
            yield return new WaitForEndOfFrame();
        }
        
        /// <summary>
        /// Initialize core system managers (highest priority)
        /// </summary>
        public IEnumerator InitializeCoreManagers()
        {
            LogDebug("Initializing core system managers");
            
            var coreManagers = _initializationOrder
                .Where(m => m.Priority == ManagerPriority.Critical || m.Priority == ManagerPriority.High)
                .ToList();
            
            foreach (var manager in coreManagers)
            {
                yield return StartCoroutine(InitializeManager(manager));
            }
            
            LogDebug($"Core managers initialized: {coreManagers.Count}");
        }
        
        /// <summary>
        /// Initialize domain-specific managers (cultivation, environment, economy)
        /// </summary>
        public IEnumerator InitializeDomainManagers()
        {
            LogDebug("Initializing domain system managers");
            
            var domainManagers = _initializationOrder
                .Where(m => m.Priority == ManagerPriority.Normal)
                .Where(m => IsDomainManager(m))
                .ToList();
            
            foreach (var manager in domainManagers)
            {
                yield return StartCoroutine(InitializeManager(manager));
            }
            
            LogDebug($"Domain managers initialized: {domainManagers.Count}");
        }
        
        /// <summary>
        /// Initialize progression and analytics managers
        /// </summary>
        public IEnumerator InitializeProgressionManagers()
        {
            LogDebug("Initializing progression system managers");
            
            var progressionManagers = _initializationOrder
                .Where(m => IsProgressionManager(m))
                .ToList();
            
            foreach (var manager in progressionManagers)
            {
                yield return StartCoroutine(InitializeManager(manager));
            }
            
            LogDebug($"Progression managers initialized: {progressionManagers.Count}");
        }
        
        /// <summary>
        /// Initialize UI and presentation managers (lowest priority)
        /// </summary>
        public IEnumerator InitializeUIManagers()
        {
            LogDebug("Initializing UI system managers");
            
            var uiManagers = _initializationOrder
                .Where(m => m.Priority == ManagerPriority.Low)
                .ToList();
            
            foreach (var manager in uiManagers)
            {
                yield return StartCoroutine(InitializeManager(manager));
            }
            
            LogDebug($"UI managers initialized: {uiManagers.Count}");
        }
        
        /// <summary>
        /// Initialize a specific manager with error handling and recovery
        /// </summary>
        public IEnumerator InitializeManager(ChimeraManager manager)
        {
            if (manager == null) yield break;
            
            bool success = false;
            int attempts = 0;
            bool shouldWaitForRetry = false;
            
            while (!success && attempts < _maxRecoveryAttempts)
            {
                attempts++;
                
                // Execute initialization outside try-catch to allow yield
                bool initSuccessful = false;
                Exception initException = null;
                
                try
                {
                    LogDebug($"Initializing {manager.ManagerName} (attempt {attempts})");
                    
                    if (!manager.IsInitialized)
                    {
                        manager.Initialize();
                        initSuccessful = true;
                    }
                    else
                    {
                        success = true;
                        LogDebug($"Manager {manager.ManagerName} already initialized");
                        initSuccessful = true;
                    }
                }
                catch (Exception ex)
                {
                    initException = ex;
                    initSuccessful = false;
                }
                
                // Handle yield and results outside try-catch
                if (initSuccessful && !manager.IsInitialized)
                {
                    yield return new WaitForEndOfFrame();
                    
                    if (manager.IsInitialized)
                    {
                        success = true;
                        LogDebug($"Successfully initialized: {manager.ManagerName}");
                    }
                    else
                    {
                        LogError($"Manager {manager.ManagerName} failed to initialize properly");
                    }
                }
                else if (!initSuccessful)
                {
                    LogError($"Failed to initialize {manager.ManagerName} (attempt {attempts}): {initException?.Message}");
                    
                    if (attempts < _maxRecoveryAttempts)
                    {
                        yield return new WaitForSeconds(0.5f);
                    }
                }
            }
            
            OnManagerInitialized?.Invoke(manager, success);
            
            if (!success)
            {
                LogError($"Failed to initialize {manager.ManagerName} after {attempts} attempts");
            }
        }
        
        /// <summary>
        /// Validate all systems are properly initialized
        /// </summary>
        public IEnumerator ValidateAllSystems()
        {
            LogDebug("Validating all initialized systems");
            
            var validationResult = ValidateSystemIntegrity();
            
            if (!validationResult.IsValid && _attemptServiceRecovery)
            {
                LogDebug("System validation failed, attempting recovery");
                yield return StartCoroutine(AttemptSystemRecovery(validationResult));
            }
            
            LogDebug($"System validation complete: {validationResult.ValidatedSystems} systems validated");
        }
        
        /// <summary>
        /// Attempt to recover failed systems
        /// </summary>
        public IEnumerator AttemptSystemRecovery(ValidationResult validationResult)
        {
            LogDebug("Attempting system recovery");
            
            foreach (var failedManager in validationResult.FailedManagers)
            {
                if (failedManager != null && !failedManager.IsInitialized)
                {
                    LogDebug($"Attempting to recover: {failedManager.ManagerName}");
                    yield return StartCoroutine(InitializeManager(failedManager));
                    
                    if (failedManager.IsInitialized)
                    {
                        LogDebug($"Successfully recovered: {failedManager.ManagerName}");
                    }
                    else
                    {
                        LogError($"Failed to recover: {failedManager.ManagerName}");
                    }
                }
            }
        }
        
        /// <summary>
        /// Validate system integrity and return detailed result
        /// </summary>
        public ValidationResult ValidateSystemIntegrity()
        {
            var result = new ValidationResult
            {
                ValidatedSystems = 0,
                FailedManagers = new List<ChimeraManager>(),
                ValidationErrors = new List<string>()
            };
            
            foreach (var manager in _discoveredManagers.Values)
            {
                if (manager == null)
                {
                    result.ValidationErrors.Add("Null manager found in registry");
                    continue;
                }
                
                if (manager.IsInitialized)
                {
                    result.ValidatedSystems++;
                }
                else
                {
                    result.FailedManagers.Add(manager);
                    result.ValidationErrors.Add($"Manager {manager.ManagerName} failed to initialize");
                }
            }
            
            result.IsValid = result.FailedManagers.Count == 0;
            return result;
        }
        
        /// <summary>
        /// Check if manager is a domain-specific manager
        /// </summary>
        private bool IsDomainManager(ChimeraManager manager)
        {
            var typeName = manager.GetType().Name.ToLower();
            return typeName.Contains("plant") || typeName.Contains("cultivation") ||
                   typeName.Contains("environment") || typeName.Contains("economy") ||
                   typeName.Contains("construction") || typeName.Contains("breeding");
        }
        
        /// <summary>
        /// Check if manager is a progression-related manager
        /// </summary>
        private bool IsProgressionManager(ChimeraManager manager)
        {
            var typeName = manager.GetType().Name.ToLower();
            return typeName.Contains("progression") || typeName.Contains("skill") ||
                   typeName.Contains("research") || typeName.Contains("achievement") ||
                   typeName.Contains("analytics");
        }
        
        /// <summary>
        /// Get manager by type from discovered managers
        /// </summary>
        public T GetDiscoveredManager<T>() where T : ChimeraManager
        {
            var targetType = typeof(T);
            return _discoveredManagers.TryGetValue(targetType, out var manager) ? manager as T : null;
        }
        
        /// <summary>
        /// Clear all discovered managers (for cleanup)
        /// </summary>
        public void ClearDiscoveredManagers()
        {
            _discoveredManagers.Clear();
            _initializationOrder.Clear();
            LogDebug("Cleared all discovered managers");
        }
        
        private void LogDebug(string message)
        {
            if (_enablePhaseLogging)
                Debug.Log($"[GameSystemInitializer] {message}");
        }
        
        private void LogError(string message)
        {
            Debug.LogError($"[GameSystemInitializer] {message}");
        }
    }
    
    /// <summary>
    /// Initialization phases for systematic manager setup
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
    /// Result of complete system initialization
    /// </summary>
    public class InitializationResult
    {
        public bool Success { get; set; }
        public int InitializedManagerCount { get; set; }
        public TimeSpan InitializationTime { get; set; }
        public string ErrorMessage { get; set; }
    }
    
    /// <summary>
    /// Result of system validation
    /// </summary>
    public class ValidationResult
    {
        public bool IsValid { get; set; }
        public int ValidatedSystems { get; set; }
        public List<ChimeraManager> FailedManagers { get; set; } = new List<ChimeraManager>();
        public List<string> ValidationErrors { get; set; } = new List<string>();
    }
}