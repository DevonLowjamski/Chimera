using UnityEngine;

namespace ProjectChimera.Core.DependencyInjection
{
    /// <summary>
    /// Bootstrap component that automatically sets up the dependency injection system
    /// Add this component to your main GameManager or create a dedicated bootstrapper GameObject
    /// </summary>
    public class ServiceBootstrapper : MonoBehaviour
    {
        [Header("Bootstrapper Configuration")]
        [SerializeField] private bool _initializeOnAwake = true;
        [SerializeField] private bool _registerCoreModule = true;
        [SerializeField] private bool _enableDebugLogging = true;

        [Header("Module Registration")]
        [SerializeField] private string[] _moduleAssemblies = new string[]
        {
            "ProjectChimera.Systems.Cultivation",
            "ProjectChimera.Systems.Environment", 
            "ProjectChimera.Systems.Genetics",
            "ProjectChimera.Systems.Economy"
        };

        private ServiceManager _serviceManager;
        private bool _isBootstrapped = false;

        #region Unity Lifecycle

        private void Awake()
        {
            if (_initializeOnAwake)
            {
                BootstrapServices();
            }
        }

        private void Start()
        {
            if (!_isBootstrapped)
            {
                BootstrapServices();
            }
        }

        #endregion

        #region Bootstrapping

        /// <summary>
        /// Bootstrap the dependency injection system
        /// </summary>
        public void BootstrapServices()
        {
            if (_isBootstrapped)
            {
                LogBootstrap("Services already bootstrapped");
                return;
            }

            try
            {
                LogBootstrap("Starting service bootstrap");

                // Initialize service manager
                _serviceManager = ServiceManager.Instance;
                
                // Register core module
                if (_registerCoreModule)
                {
                    RegisterCoreModule();
                }

                // Auto-discover and register modules
                AutoDiscoverModules();

                _isBootstrapped = true;
                LogBootstrap("Service bootstrap completed successfully");
            }
            catch (System.Exception ex)
            {
                Debug.LogError($"[ServiceBootstrapper] Bootstrap failed: {ex.Message}");
            }
        }

        private void RegisterCoreModule()
        {
            var coreModule = new ChimeraServiceModule();
            _serviceManager.RegisterModule(coreModule);
            LogBootstrap("Core module registered");
        }

        private void AutoDiscoverModules()
        {
            LogBootstrap("Auto-discovering service modules");

            // For now, we'll manually register known modules
            // In a full implementation, this could use reflection to find IServiceModule implementations
            
            LogBootstrap($"Module auto-discovery completed");
        }

        #endregion

        #region Public API

        /// <summary>
        /// Get a service from the dependency injection system
        /// </summary>
        public T GetService<T>() where T : class
        {
            if (!_isBootstrapped)
            {
                LogBootstrap("Services not bootstrapped yet - attempting bootstrap");
                BootstrapServices();
            }

            return _serviceManager?.GetService<T>();
        }

        /// <summary>
        /// Check if the service system is ready
        /// </summary>
        public bool IsBootstrapped => _isBootstrapped && _serviceManager != null && _serviceManager.IsInitialized;

        /// <summary>
        /// Get the service manager instance
        /// </summary>
        public ServiceManager GetServiceManager()
        {
            return _serviceManager;
        }

        #endregion

        #region Utility

        private void LogBootstrap(string message)
        {
            if (_enableDebugLogging)
            {
                Debug.Log($"[ServiceBootstrapper] {message}");
            }
        }

        #endregion

        #region Static Access

        private static ServiceBootstrapper _instance;
        
        /// <summary>
        /// Global bootstrapper instance for easy access
        /// </summary>
        public static ServiceBootstrapper Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = FindObjectOfType<ServiceBootstrapper>();
                    if (_instance == null)
                    {
                        Debug.LogWarning("[ServiceBootstrapper] No ServiceBootstrapper found in scene");
                    }
                }
                return _instance;
            }
        }

        /// <summary>
        /// Quick access to get services globally
        /// </summary>
        public static T GetGlobalService<T>() where T : class
        {
            return Instance?.GetService<T>();
        }

        #endregion
    }
}