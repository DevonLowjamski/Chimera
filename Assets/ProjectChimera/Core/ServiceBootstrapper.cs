using ProjectChimera.Core.Logging;
using UnityEngine;

namespace ProjectChimera.Core
{
    /// <summary>
    /// SIMPLE: Basic service bootstrapper aligned with Project Chimera's service initialization vision.
    /// Focuses on essential service setup without complex dependency injection.
    /// </summary>
    public class ServiceBootstrapper : MonoBehaviour
    {
        [Header("Basic Settings")]
        [SerializeField] private bool _initializeOnAwake = true;
        [SerializeField] private bool _enableLogging = true;

        private bool _isBootstrapped = false;

        // Singleton pattern
        private static ServiceBootstrapper _instance;
        public static ServiceBootstrapper Instance => _instance;

        #region Unity Lifecycle

        private void Awake()
        {
            // Singleton enforcement
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);

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
        /// Bootstrap basic services
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
                LogBootstrap("Starting basic service bootstrap");

                // Initialize basic services
                InitializeBasicServices();

                _isBootstrapped = true;
                LogBootstrap("Service bootstrap completed successfully");
            }
            catch (System.Exception ex)
            {
                ChimeraLogger.LogInfo("ServiceBootstrapper", "$1");
                throw;
            }
        }

        /// <summary>
        /// Check if services are bootstrapped
        /// </summary>
        public bool IsBootstrapped => _isBootstrapped;

        /// <summary>
        /// Force re-bootstrap (for testing)
        /// </summary>
        public void ReBootstrap()
        {
            _isBootstrapped = false;
            BootstrapServices();
        }

        #endregion

        #region Private Methods

        private void InitializeBasicServices()
        {
            LogBootstrap("Initializing basic services");

            // Initialize core systems that are always needed
            // This could include things like logging, configuration, etc.

            // Example: Initialize logging system
            // UnityEngine.Debug is always available, no instance check needed
            {
                // Create basic logger if needed
                LogBootstrap("Basic logger initialized");
            }

            LogBootstrap("Basic services initialized");
        }

        private void LogBootstrap(string message)
        {
            if (_enableLogging)
            {
                ChimeraLogger.LogInfo("ServiceBootstrapper", "$1");
            }
        }

        #endregion
    }
}
