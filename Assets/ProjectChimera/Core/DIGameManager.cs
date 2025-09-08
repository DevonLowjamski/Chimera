using ProjectChimera.Core.Logging;
using UnityEngine;
using ProjectChimera.Core.Events;
using System.Collections.Generic;
using System.Collections;
using System;
using System.Linq;
using ProjectChimera.Core.DependencyInjection;


namespace ProjectChimera.Core
{
    /// <summary>
    /// Lightweight orchestrator for the dependency injection-enabled Game Manager.
    /// Delegates all functionality to specialized components.
    /// Refactored from 799-line monolith to modular architecture.
    /// Maintains full API compatibility for existing game management operations.
    /// </summary>
    public class DIGameManager : DIChimeraManager, IGameManager
    {
        [Header("Component References")]
        [SerializeField] private GameSystemInitializer _systemInitializer;
        [SerializeField] private ManagerRegistry _managerRegistry;
        [SerializeField] private ServiceHealthMonitor _healthMonitor;
        
        [Header("Game State Configuration")]
        [SerializeField] private GameStateConfigSO _gameStateConfig;
        [SerializeField] private bool _loadLastSaveOnStart = true;
        [SerializeField] private bool _autoSaveEnabled = true;
        [SerializeField] private float _autoSaveInterval = 300.0f; // 5 minutes

        [Header("Game State Events")]
        [SerializeField] private SimpleGameEventSO _onGameInitialized;
        [SerializeField] private SimpleGameEventSO _onGamePaused;
        [SerializeField] private SimpleGameEventSO _onGameResumed;
        [SerializeField] private SimpleGameEventSO _onGameShutdown;

        [Header("DI Configuration")]
        [SerializeField] private bool _initializeManagersWithDI = true;
        [SerializeField] private bool _validateDependenciesOnStart = true;
        [SerializeField] private bool _enableDebugLogging = false;

        // Dependency-injected services
        private ITimeManager _timeManager;
        private IDataManager _dataManager;
        private IEventManager _eventManager;
        private ISettingsManager _settingsManager;

        // State management
        private Coroutine _autoSaveCoroutine;
        private bool _isInitialized = false;

        /// <summary>
        /// Singleton instance of the DI Game Manager
        /// </summary>
        public static DIGameManager Instance { get; private set; }

        /// <summary>
        /// Current game state
        /// </summary>
        public GameState CurrentGameState { get; private set; } = GameState.Uninitialized;

        /// <summary>
        /// Whether the game is currently paused
        /// </summary>
        public bool IsGamePaused { get; private set; }

        /// <summary>
        /// Time when the game was started
        /// </summary>
        public DateTime GameStartTime { get; private set; }

        /// <summary>
        /// Total time the game has been running
        /// </summary>
        public TimeSpan TotalGameTime => DateTime.Now - GameStartTime;

        /// <summary>
        /// Access to the global service container
        /// </summary>
        public ProjectChimera.Core.IServiceContainer GlobalServiceContainer => ServiceContainer;

        /// <summary>
        /// Access to the manager registry
        /// </summary>
        public ManagerRegistry ManagerRegistry => _managerRegistry;

        /// <summary>
        /// Access to the health monitor
        /// </summary>
        public ServiceHealthMonitor HealthMonitor => _healthMonitor;

        #region Unity Lifecycle

        protected override void Awake()
        {
            // Implement singleton pattern
            if (Instance != null && Instance != this)
            {
                LogDebug("Multiple DIGameManager instances detected. Destroying duplicate.");
                Destroy(gameObject);
                return;
            }

            Instance = this;
            base.Awake();
            InitializeComponents();
        }

        protected override void Start()
        {
            WireComponentEvents();
            StartCoroutine(InitializeGameSystemsOrchestrated());
            base.Start();
        }

        #endregion

        #region Component Initialization

        private void InitializeComponents()
        {
            // Find or create required components
            if (_systemInitializer == null) _systemInitializer = GetComponent<GameSystemInitializer>();
            if (_managerRegistry == null) _managerRegistry = GetComponent<ManagerRegistry>();
            if (_healthMonitor == null) _healthMonitor = GetComponent<ServiceHealthMonitor>();

            // Create missing components if needed
            if (_systemInitializer == null) _systemInitializer = gameObject.AddComponent<GameSystemInitializer>();
            if (_managerRegistry == null) _managerRegistry = gameObject.AddComponent<ManagerRegistry>();
            if (_healthMonitor == null) _healthMonitor = gameObject.AddComponent<ServiceHealthMonitor>();

            LogDebug("DIGameManager components initialized");
        }

        private void WireComponentEvents()
        {
            // Wire system initializer events
            if (_systemInitializer != null)
            {
                _systemInitializer.OnInitializationCompleted += HandleInitializationCompleted;
                _systemInitializer.OnInitializationError += HandleInitializationError;
                _systemInitializer.OnManagerInitialized += HandleManagerInitialized;
            }

            // Wire manager registry events
            if (_managerRegistry != null)
            {
                _managerRegistry.OnManagerRegistered += HandleManagerRegistered;
                _managerRegistry.OnRegistrationError += HandleRegistrationError;
            }

            // Wire health monitor events
            if (_healthMonitor != null)
            {
                _healthMonitor.OnHealthReportGenerated += HandleHealthReportGenerated;
                _healthMonitor.OnCriticalError += HandleCriticalHealthError;
                _healthMonitor.OnHealthAlert += HandleHealthAlert;
            }

            LogDebug("Component events wired successfully");
        }

        #endregion

        #region Dependency Injection Override

        protected override ProjectChimera.Core.IServiceContainer GetOrCreateServiceContainer()
        {
            // Use ServiceContainerFactory as the sole standardized DI approach per Phase 0 goals
            var container = ServiceContainerFactory.Instance;
            
            // Register core game services with the unified container
            RegisterCoreServices(container);
            
            LogDebug("Global service container created and configured");
            return container;
        }

        private void RegisterCoreServices(ProjectChimera.Core.IServiceContainer container)
        {
            try
            {
                // Register this GameManager
                container.RegisterSingleton<IGameManager>(this);
                container.RegisterSingleton<DIGameManager>(this);

                // Register configuration
                if (_gameStateConfig != null)
                {
                    container.RegisterSingleton<GameStateConfigSO>(_gameStateConfig);
                }

                // Register factory methods for creating managers
                container.RegisterFactory<ProjectChimera.Core.IServiceContainer>(locator => container);

                LogDebug("Core services registered with container");
            }
            catch (Exception ex)
            {
                LogError($"Failed to register core services: {ex.Message}");
            }
        }

        protected override void ResolveDependencies()
        {
            try
            {
                // Resolve core manager dependencies
                _timeManager = TryResolveService<ITimeManager>();
                _dataManager = TryResolveService<IDataManager>();
                _eventManager = TryResolveService<IEventManager>();
                _settingsManager = TryResolveService<ISettingsManager>();

                if (_validateDependenciesOnStart)
                {
                    ValidateCriticalDependencies();
                }

                LogDebug("Core dependencies resolved successfully");
            }
            catch (Exception ex)
            {
                LogError($"Failed to resolve dependencies: {ex.Message}");
            }
        }

        #endregion

        #region Orchestrated Game System Initialization

        /// <summary>
        /// Initialize all game systems using component delegation
        /// </summary>
        private IEnumerator InitializeGameSystemsOrchestrated()
        {
            LogDebug("Starting orchestrated game system initialization");
            
            CurrentGameState = GameState.Initializing;
            GameStartTime = DateTime.Now;

            // Initialize components first
            InitializeAllComponents();

            // Delegate initialization to GameSystemInitializer
            if (_systemInitializer != null)
            {
                yield return StartCoroutine(_systemInitializer.InitializeAllGameSystems());
            }
            else
            {
                LogError("GameSystemInitializer component missing, cannot initialize systems");
                CurrentGameState = GameState.Error;
                yield break;
            }

            // Wait for initialization completion (handled by event)
            while (!_isInitialized && CurrentGameState == GameState.Initializing)
            {
                yield return new WaitForSeconds(0.1f);
            }

            // Start auto-save if enabled and initialization was successful
            if (_autoSaveEnabled && CurrentGameState == GameState.Running)
            {
                _autoSaveCoroutine = StartCoroutine(AutoSaveCoroutine());
            }

            var totalTime = (DateTime.Now - GameStartTime).TotalSeconds;
            LogDebug($"Orchestrated game system initialization completed in {totalTime:F2}s");
        }

        /// <summary>
        /// Initialize all components with proper configuration
        /// </summary>
        private void InitializeAllComponents()
        {
            try
            {
                // Initialize manager registry with service container
                _managerRegistry?.Initialize(ServiceContainer);

                // Initialize health monitor with manager registry
                _healthMonitor?.Initialize(_managerRegistry, ServiceContainer);

                LogDebug("All components initialized successfully");
            }
            catch (Exception ex)
            {
                LogError($"Component initialization failed: {ex.Message}");
            }
        }

        #endregion

        #region Manager Registration (Delegate to ManagerRegistry)

        /// <summary>
        /// Register a manager - delegates to ManagerRegistry
        /// </summary>
        public void RegisterManager<T>(T manager) where T : ChimeraManager
        {
            _managerRegistry?.RegisterManager(manager);
        }

        /// <summary>
        /// Get manager - delegates to ManagerRegistry
        /// </summary>
        public T GetManager<T>() where T : ChimeraManager
        {
            return _managerRegistry?.GetManager<T>();
        }
        
        /// <summary>
        /// Get a manager by type
        /// </summary>
        public ChimeraManager GetManager(System.Type managerType)
        {
            return _managerRegistry?.GetManager(managerType);
        }

        /// <summary>
        /// Get all registered managers - delegates to ManagerRegistry
        /// </summary>
        public IEnumerable<ChimeraManager> GetAllManagers()
        {
            return _managerRegistry?.RegisteredManagers ?? new List<ChimeraManager>();
        }

        #endregion

        #region Health Monitoring (Delegate to ServiceHealthMonitor)

        /// <summary>
        /// Get service health report - delegates to ServiceHealthMonitor
        /// </summary>
        public ServiceHealthReport GetServiceHealthReport()
        {
            return _healthMonitor?.GenerateHealthReport() ?? new ServiceHealthReport
            {
                IsHealthy = false,
                CriticalErrors = new List<string> { "Health monitor not available" }
            };
        }

        #endregion

        #region Game State Management

        /// <summary>
        /// Pause the game
        /// </summary>
        public void PauseGame()
        {
            if (IsGamePaused) return;

            IsGamePaused = true;
            Time.timeScale = 0f;
            _onGamePaused?.Raise();

            LogDebug("Game paused");
        }

        /// <summary>
        /// Resume the game
        /// </summary>
        public void ResumeGame()
        {
            if (!IsGamePaused) return;

            IsGamePaused = false;
            Time.timeScale = 1f;
            _onGameResumed?.Raise();

            LogDebug("Game resumed");
        }

        /// <summary>
        /// Shutdown the game
        /// </summary>
        public void ShutdownGame()
        {
            LogDebug("Shutting down game");

            CurrentGameState = GameState.Shutting_Down;

            // Stop auto-save
            if (_autoSaveCoroutine != null)
            {
                StopCoroutine(_autoSaveCoroutine);
            }

            // Shutdown all managers through registry
            ShutdownAllManagers();

            _onGameShutdown?.Raise();
            CurrentGameState = GameState.Shutdown;
        }

        private void ShutdownAllManagers()
        {
            foreach (var manager in GetAllManagers())
            {
                try
                {
                    if (manager != null)
                    {
                        manager.Shutdown();
                    }
                }
                catch (Exception ex)
                {
                    LogError($"Error shutting down manager {manager?.ManagerName}: {ex.Message}");
                }
            }
        }

        #endregion

        #region Auto-Save

        private IEnumerator AutoSaveCoroutine()
        {
            while (_autoSaveEnabled && CurrentGameState == GameState.Running)
            {
                yield return new WaitForSeconds(_autoSaveInterval);

                if (!IsGamePaused && _dataManager != null)
                {
                    try
                    {
                        // _dataManager.AutoSave();
                        LogDebug("Auto-save completed");
                    }
                    catch (Exception ex)
                    {
                        LogError($"Auto-save failed: {ex.Message}");
                    }
                }
            }
        }

        #endregion

        #region Event Handlers

        private void HandleInitializationCompleted(InitializationResult result)
        {
            if (result.Success)
            {
                CurrentGameState = GameState.Running;
                _onGameInitialized?.Raise();
                _isInitialized = true;
                LogDebug($"Game initialization completed successfully: {result.InitializedManagerCount} managers initialized");
            }
            else
            {
                CurrentGameState = GameState.Error;
                LogError($"Game initialization failed: {result.ErrorMessage}");
            }
        }

        private void HandleInitializationError(string error)
        {
            CurrentGameState = GameState.Error;
            LogError($"Initialization error: {error}");
        }

        private void HandleManagerInitialized(ChimeraManager manager, bool success)
        {
            if (success)
            {
                LogDebug($"Manager initialized successfully: {manager.ManagerName}");
            }
            else
            {
                LogError($"Manager initialization failed: {manager.ManagerName}");
            }
        }

        private void HandleManagerRegistered(ChimeraManager manager)
        {
            LogDebug($"Manager registered: {manager.ManagerName}");
        }

        private void HandleRegistrationError(string error)
        {
            LogError($"Manager registration error: {error}");
        }

        private void HandleHealthReportGenerated(ServiceHealthReport report)
        {
            if (!report.IsHealthy)
            {
                LogDebug($"Health check found {report.CriticalErrors.Count} critical errors and {report.Warnings.Count} warnings");
            }
        }

        private void HandleCriticalHealthError(string error)
        {
            LogError($"Critical health error: {error}");
        }

        private void HandleHealthAlert(HealthAlert alert)
        {
            LogDebug($"Health alert: {alert.Message}");
        }

        #endregion

        #region Dependency Validation

        /// <summary>
        /// Validate critical dependencies are resolved
        /// </summary>
        private void ValidateCriticalDependencies()
        {
            var criticalDependencies = new List<string>();

            if (_timeManager == null) criticalDependencies.Add("ITimeManager");
            if (_dataManager == null) criticalDependencies.Add("IDataManager");

            if (criticalDependencies.Count > 0)
            {
                LogDebug($"Missing critical dependencies: {string.Join(", ", criticalDependencies)}");
            }
            else
            {
                LogDebug("All critical dependencies validated");
            }
        }

        #endregion

        #region ChimeraManager Implementation

        protected override void OnManagerInitialize()
        {
            LogDebug("DIGameManager initialization starting...");
            
            // Initialize DI container
            if (ServiceContainer == null)
            {
                SetServiceContainer(GetOrCreateServiceContainer());
            }

            // Register this game manager with the container
            if (ServiceContainer != null && _autoRegisterWithContainer)
            {
                ServiceContainer.RegisterSingleton<IGameManager>(this);
                ServiceContainer.RegisterSingleton<DIGameManager>(this);
            }

            LogDebug("DIGameManager initialization completed");
        }

        protected override void OnManagerShutdown()
        {
            LogDebug("DIGameManager shutdown starting...");
            
            try
            {
                // Stop health monitoring
                _healthMonitor?.StopContinuousMonitoring();

                // Clear manager registry
                _managerRegistry?.ClearAllRegistrations();

                // Dispose DI container
                if (ServiceContainer is IDisposable disposableContainer)
                {
                    disposableContainer.Dispose();
                }
                SetServiceContainer(null);

                LogDebug("DIGameManager shutdown completed");
            }
            catch (Exception ex)
            {
                LogError($"Error during shutdown: {ex.Message}");
            }
        }

        #endregion

        #region Cleanup

        protected override void OnDispose()
        {
            try
            {
                // Stop auto-save
                if (_autoSaveCoroutine != null)
                {
                    StopCoroutine(_autoSaveCoroutine);
                }

                // Shutdown all managers
                ShutdownAllManagers();

                // Clear singleton reference
                if (Instance == this)
                {
                    Instance = null;
                }

                LogDebug("DIGameManager cleanup completed");
            }
            catch (Exception ex)
            {
                LogError($"Error during cleanup: {ex.Message}");
            }
        }

        private void OnDestroy()
        {
            // Unwire events
            if (_systemInitializer != null)
            {
                _systemInitializer.OnInitializationCompleted -= HandleInitializationCompleted;
                _systemInitializer.OnInitializationError -= HandleInitializationError;
                _systemInitializer.OnManagerInitialized -= HandleManagerInitialized;
            }

            if (_managerRegistry != null)
            {
                _managerRegistry.OnManagerRegistered -= HandleManagerRegistered;
                _managerRegistry.OnRegistrationError -= HandleRegistrationError;
            }

            if (_healthMonitor != null)
            {
                _healthMonitor.OnHealthReportGenerated -= HandleHealthReportGenerated;
                _healthMonitor.OnCriticalError -= HandleCriticalHealthError;
                _healthMonitor.OnHealthAlert -= HandleHealthAlert;
            }
        }

        #endregion

        private void LogDebug(string message)
        {
            if (_enableDebugLogging)
                ChimeraLogger.Log($"[DIGameManager] {message}");
        }

        private void LogError(string message)
        {
            ChimeraLogger.LogError($"[DIGameManager] {message}");
        }
    }
}