using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using ProjectChimera.Core;

namespace ProjectChimera.Core.DependencyInjection
{
    public class ServiceManager : ChimeraManager
    {
        [Header("Service Management Configuration")]
        [SerializeField] private bool _enableAutoInitialization = true;
        [SerializeField] private bool _enableServiceValidation = true;
        [SerializeField] private bool _enableServiceLogging = true;
        [SerializeField] private bool _enablePerformanceTracking = true;

        [Header("Module Management")]
        [SerializeField] private bool _enableDependencyResolution = true;
        [SerializeField] private bool _enableModuleValidation = true;
        [SerializeField] private float _moduleInitializationTimeout = 30f;

        private static ServiceManager _instance;
        private IServiceLocator _serviceLocator;
        private readonly Dictionary<string, ModuleRegistration> _modules = new Dictionary<string, ModuleRegistration>();
        private readonly List<string> _initializationOrder = new List<string>();

        private bool _isInitialized = false;
        private bool _isShuttingDown = false;
        private DateTime _initializationStartTime;
        private int _totalServicesRegistered = 0;

        public static event Action<ServiceManager> OnServiceManagerInitialized;
        public static event Action<IServiceModule> OnModuleRegistered;
        public static event Action<IServiceModule> OnModuleInitialized;
        public static event Action<string> OnServiceError;

        public static ServiceManager Instance
        {
            get
            {
                if (_instance == null)
                {
                    // Try to get from DI container first
                    try
                    {
                        _instance = ServiceContainerFactory.Instance?.TryResolve<ServiceManager>();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogWarning($"[ServiceManager] Failed to resolve from DI container: {ex.Message}");
                    }
                    
                    if (_instance == null)
                    {
                        var gameObject = new GameObject("ServiceManager");
                        _instance = gameObject.AddComponent<ServiceManager>();
                        DontDestroyOnLoad(gameObject);
                        
                        // Register with DI container
                        try
                        {
                            ServiceContainerFactory.Instance?.RegisterSingleton<ServiceManager>(_instance);
                        }
                        catch (Exception ex)
                        {
                            Debug.LogWarning($"[ServiceManager] Failed to register with DI container: {ex.Message}");
                        }
                    }
                }
                return _instance;
            }
        }

        private void Awake()
        {
            if (_instance == null)
            {
                _instance = this;
                DontDestroyOnLoad(gameObject);
                
                // Initialize ServiceLocator integration as standard DI route
                InitializeServiceLocatorIntegration();
                
                if (_enableAutoInitialization)
                {
                    InitializeServiceManager();
                }
            }
            else if (_instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Start()
        {
            if (!_isInitialized && _enableAutoInitialization)
            {
                InitializeAllModules();
            }
        }

        private void OnDestroy()
        {
            ShutdownServiceManager();
        }

        private void OnApplicationPause(bool pauseStatus)
        {
            if (pauseStatus)
            {
                LogServiceAction("Application paused - services may be suspended");
            }
            else
            {
                LogServiceAction("Application resumed - services reactivated");
            }
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (!hasFocus)
            {
                LogServiceAction("Application lost focus - background mode");
            }
            else
            {
                LogServiceAction("Application gained focus - active mode");
            }
        }

        /// <summary>
        /// Initialize ServiceLocator integration as the standard DI route
        /// This replaces parallel container systems with a unified approach
        /// </summary>
        private void InitializeServiceLocatorIntegration()
        {
            try
            {
                // Get ServiceLocator instance (creates one if needed)
                _serviceLocator = ProjectChimera.Core.DependencyInjection.ServiceLocator.Instance;
                LogServiceAction("ServiceLocator integration initialized");

                // Register ServiceManager itself with ServiceLocator
                _serviceLocator.RegisterSingleton<IServiceLocator>(_serviceLocator);
                _serviceLocator.RegisterSingleton<ServiceManager>(this);
                
                LogServiceAction("ServiceManager registered with ServiceLocator as standard DI route");
            }
            catch (Exception ex)
            {
                LogServiceError($"Failed to initialize ServiceLocator integration: {ex.Message}");
            }
        }

        private void InitializeServiceManager()
        {
            if (_isInitialized)
            {
                LogServiceAction("ServiceManager already initialized");
                return;
            }

            try
            {
                _initializationStartTime = DateTime.Now;
                _serviceLocator = ServiceLocator.Instance;
                RegisterCoreServices();
                _isInitialized = true;
                OnServiceManagerInitialized?.Invoke(this);
                LogServiceAction("ServiceManager initialized successfully");
            }
            catch (Exception ex)
            {
                LogServiceError($"ServiceManager initialization failed: {ex.Message}");
                OnServiceError?.Invoke($"ServiceManager initialization failed: {ex.Message}");
            }
        }

        private void ShutdownServiceManager()
        {
            if (_isShuttingDown) return;
            _isShuttingDown = true;

            try
            {
                LogServiceAction("Shutting down ServiceManager");
                var shutdownOrder = _initializationOrder.ToList();
                shutdownOrder.Reverse();

                foreach (var moduleName in shutdownOrder)
                {
                    if (_modules.TryGetValue(moduleName, out var registration))
                    {
                        try
                        {
                            registration.Module.Shutdown(_serviceLocator);
                            LogServiceAction($"Module '{moduleName}' shutdown completed");
                        }
                        catch (Exception ex)
                        {
                            LogServiceError($"Error shutting down module '{moduleName}': {ex.Message}");
                        }
                    }
                }

                if (_serviceLocator is IServiceContainer container)
                {
                    container.Clear();
                }

                _modules.Clear();
                _initializationOrder.Clear();
                LogServiceAction("ServiceManager shutdown completed");
            }
            catch (Exception ex)
            {
                LogServiceError($"Error during ServiceManager shutdown: {ex.Message}");
            }
        }

        public void RegisterModule(IServiceModule module)
        {
            if (module == null)
            {
                LogServiceError("Cannot register null module");
                return;
            }

            if (_modules.ContainsKey(module.ModuleName))
            {
                LogServiceError($"Module '{module.ModuleName}' is already registered");
                return;
            }

            try
            {
                var registration = new ModuleRegistration { Module = module };
                _modules[module.ModuleName] = registration;
                module.ConfigureServices(_serviceLocator);
                _totalServicesRegistered++;
                OnModuleRegistered?.Invoke(module);
                LogServiceAction($"Module '{module.ModuleName}' registered successfully");
            }
            catch (Exception ex)
            {
                LogServiceError($"Error registering module '{module.ModuleName}': {ex.Message}");
                OnServiceError?.Invoke($"Module registration failed: {ex.Message}");
            }
        }

        public void InitializeAllModules()
        {
            if (!_isInitialized)
            {
                LogServiceError("ServiceManager must be initialized before modules");
                return;
            }

            try
            {
                LogServiceAction("Initializing all modules");
                if (_enableDependencyResolution)
                {
                    ResolveDependencyOrder();
                }
                else
                {
                    _initializationOrder.AddRange(_modules.Keys);
                }

                foreach (var moduleName in _initializationOrder)
                {
                    InitializeModule(moduleName);
                }

                if (_enableServiceValidation)
                {
                    ValidateAllServices();
                }

                var initializationTime = (DateTime.Now - _initializationStartTime).TotalSeconds;
                LogServiceAction($"All modules initialized in {initializationTime:F2} seconds");
            }
            catch (Exception ex)
            {
                LogServiceError($"Error during module initialization: {ex.Message}");
                OnServiceError?.Invoke($"Module initialization failed: {ex.Message}");
            }
        }

        private void InitializeModule(string moduleName)
        {
            if (!_modules.TryGetValue(moduleName, out var registration))
            {
                LogServiceError($"Module '{moduleName}' not found for initialization");
                return;
            }

            if (registration.IsInitialized)
            {
                LogServiceAction($"Module '{moduleName}' already initialized");
                return;
            }

            try
            {
                LogServiceAction($"Initializing module '{moduleName}'");
                registration.Module.Initialize(_serviceLocator);
                registration.IsInitialized = true;
                OnModuleInitialized?.Invoke(registration.Module);
                LogServiceAction($"Module '{moduleName}' initialized successfully");
            }
            catch (Exception ex)
            {
                LogServiceError($"Error initializing module '{moduleName}': {ex.Message}");
                OnServiceError?.Invoke($"Module '{moduleName}' initialization failed: {ex.Message}");
            }
        }

        private void ResolveDependencyOrder()
        {
            _initializationOrder.Clear();
            var processed = new HashSet<string>();
            var processing = new HashSet<string>();

            foreach (var moduleName in _modules.Keys)
            {
                ResolveDependencies(moduleName, processed, processing);
            }
        }

        private void ResolveDependencies(string moduleName, HashSet<string> processed, HashSet<string> processing)
        {
            if (processed.Contains(moduleName)) return;

            if (processing.Contains(moduleName))
            {
                throw new InvalidOperationException($"Circular dependency detected involving module '{moduleName}'");
            }

            if (!_modules.TryGetValue(moduleName, out var registration))
            {
                throw new InvalidOperationException($"Module '{moduleName}' not found");
            }

            processing.Add(moduleName);

            foreach (var dependency in registration.Module.Dependencies)
            {
                if (!_modules.ContainsKey(dependency))
                {
                    throw new InvalidOperationException($"Module '{moduleName}' depends on '{dependency}' which is not registered");
                }
                ResolveDependencies(dependency, processed, processing);
            }

            processing.Remove(moduleName);
            processed.Add(moduleName);
            _initializationOrder.Add(moduleName);
        }

        private void RegisterCoreServices()
        {
            if (_serviceLocator is IServiceContainer container)
            {
                container.RegisterSingleton<ServiceManager>(this);
            }
            LogServiceAction("Core services registered");
        }

        private void ValidateAllServices()
        {
            LogServiceAction("Validating all services");
            foreach (var registration in _modules.Values)
            {
                try
                {
                    if (_enableModuleValidation)
                    {
                        var isValid = registration.Module.ValidateServices(_serviceLocator);
                        registration.IsValidated = isValid;
                        if (!isValid)
                        {
                            LogServiceError($"Module '{registration.Module.ModuleName}' failed validation");
                        }
                        else
                        {
                            LogServiceAction($"Module '{registration.Module.ModuleName}' validated successfully");
                        }
                    }
                    else
                    {
                        registration.IsValidated = true;
                    }
                }
                catch (Exception ex)
                {
                    LogServiceError($"Error validating module '{registration.Module.ModuleName}': {ex.Message}");
                    registration.IsValidated = false;
                }
            }
        }

        public IServiceLocator GetServiceLocator()
        {
            return _serviceLocator;
        }

        public T GetService<T>() where T : class
        {
            return _serviceLocator?.Resolve<T>();
        }

        public bool IsInitialized => _isInitialized && !_isShuttingDown;

        public IEnumerable<IServiceModule> GetRegisteredModules()
        {
            return _modules.Values.Select(r => r.Module).ToList();
        }

        public ServiceManagerMetrics GetMetrics()
        {
            var serviceLocatorMetrics = _serviceLocator is ServiceLocator locator ?
                locator.GetMetrics() : null;

            return new ServiceManagerMetrics
            {
                IsInitialized = _isInitialized,
                RegisteredModules = _modules.Count,
                InitializedModules = _modules.Values.Count(r => r.IsInitialized),
                ValidatedModules = _modules.Values.Count(r => r.IsValidated),
                TotalServicesRegistered = _totalServicesRegistered,
                InitializationTime = _isInitialized ?
                    (DateTime.Now - _initializationStartTime).TotalSeconds : 0,
                ServiceLocatorMetrics = serviceLocatorMetrics
            };
        }

        private void LogServiceAction(string message)
        {
            if (_enableServiceLogging)
            {
                Debug.Log($"[ServiceManager] {message}");
            }
        }

        private void LogServiceError(string message)
        {
            Debug.LogError($"[ServiceManager] ERROR: {message}");
        }

        protected override void OnManagerInitialize()
        {
        }

        protected override void OnManagerShutdown()
        {
            _serviceLocator = null;
            _modules?.Clear();
        }
    }

    public class ServiceManagerMetrics
    {
        public bool IsInitialized { get; set; }
        public int RegisteredModules { get; set; }
        public int InitializedModules { get; set; }
        public int ValidatedModules { get; set; }
        public int TotalServicesRegistered { get; set; }
        public double InitializationTime { get; set; }
        public ServiceLocatorMetrics ServiceLocatorMetrics { get; set; }
    }
    
    
}
