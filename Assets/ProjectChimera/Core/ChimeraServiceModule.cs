using System;
using UnityEngine;
using ProjectChimera.Core.DependencyInjection;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core.DependencyInjection
{
    /// <summary>
    /// REFACTORED: Core service module has been decomposed into focused service components.
    /// This file now serves as a reference implementation using the new component structure.
    ///
    /// New Component Structure:
    /// - ServiceModuleCore.cs: Core service module infrastructure and component coordination
    /// - ManagerRegistrationProvider.cs: Manager interface registration and factory methods
    /// - UtilityServiceProvider.cs: Utility services that don't depend on Unity MonoBehaviours
    /// - DataServiceProvider.cs: Data access services, serialization, and persistence
    /// - NullImplementationProvider.cs: Null object pattern implementations for graceful degradation
    /// </summary>

    // The ChimeraServiceModule functionality has been moved to focused component files.
    // This file is kept for reference and to prevent breaking changes during migration.
    //
    // To use the new component structure, inherit from ServiceModuleCore:
    //
    // public class ChimeraServiceModule : ServiceModuleCore
    // {
    //     // Your custom service module implementation
    // }
    //
    // The following service areas are now available in their focused components:
    //
    // From ManagerRegistrationProvider.cs:
    // - Core manager interface registration (ITimeManager, IDataManager, etc.)
    // - UI system interface registration (IUIManager, ISchematicLibraryPanel, etc.)
    // - Construction system interface registration (IGridPlacementController, etc.)
    // - Domain manager interface registration (IPlantManager, IGeneticsManager, etc.)
    // - Asset service interface registration (ISchematicAssetService)
    // - Update orchestrator interface registration (IUpdateOrchestrator)
    //
    // From UtilityServiceProvider.cs:
    // - Configuration services (IConfigurationService, IApplicationSettingsService)
    // - Math utilities (IMathUtilityService, IRandomNumberService)
    // - Helper services (IStringUtilityService, ICollectionUtilityService)
    // - Cross-cutting services (IValidationService, ICachingService)
    // - Performance services (IPerformanceMonitoringService, IMemoryManagementService)
    //
    // From DataServiceProvider.cs:
    // - Data persistence services (ISaveDataService, IPlayerPreferenceService)
    // - Serialization services (IJsonSerializationService, IBinarySerializationService)
    // - Data access services (IDatabaseAccessService, IFileDataAccessService)
    // - Validation services (IDataValidationService)
    // - Caching services (IDataCacheService)
    //
    // From NullImplementationProvider.cs:
    // - Null object pattern implementations for all manager interfaces
    // - Graceful degradation when services are not available
    // - Fallback implementations with safe no-op behavior

    /// <summary>
    /// Concrete implementation of ChimeraServiceModule using the new component structure.
    /// Inherits all functionality from ServiceModuleCore and specialized service providers.
    /// </summary>
    public class ChimeraServiceModule : ServiceModuleCore
    {
        public override string ModuleName => "ProjectChimera.Core";

        [Header("ChimeraServiceModule Settings")]
        [SerializeField] private bool _enableManagerRegistration = true;
        [SerializeField] private bool _enableUtilityServices = true;
        [SerializeField] private bool _enableDataServices = true;
        [SerializeField] private bool _enableNullImplementations = true;

        // Legacy support properties
        public bool EnableManagerRegistration => _enableManagerRegistration;
        public bool EnableUtilityServices => _enableUtilityServices;
        public bool EnableDataServices => _enableDataServices;
        public bool EnableNullImplementations => _enableNullImplementations;

        // Service provider fields
        private ManagerRegistrationProvider _managerRegistrationProvider;
        private UtilityServiceProvider _utilityServiceProvider;
        private DataServiceProvider _dataServiceProvider;
        private NullImplementationProvider _nullImplementationProvider;

        // Logging methods
        private void LogModuleAction(string action)
        {
            ChimeraLogger.Log("CORE", $"ChimeraServiceModule: {action}", this);
        }

        private void LogModuleError(string error)
        {
            ChimeraLogger.LogError("CORE", $"ChimeraServiceModule Error: {error}", this);
        }

        private void LogModuleWarning(string warning)
        {
            ChimeraLogger.LogWarning("CORE", $"ChimeraServiceModule Warning: {warning}", this);
        }

        // Metrics methods
        private ChimeraServiceModuleMetrics GetModuleMetrics()
        {
            return new ChimeraServiceModuleMetrics
            {
                ModuleVersion = "1.0.0",
                IsConfigured = _enableManagerRegistration || _enableUtilityServices || _enableDataServices || _enableNullImplementations,
                IsInitialized = _managerRegistrationProvider != null || _utilityServiceProvider != null || _dataServiceProvider != null || _nullImplementationProvider != null,
                ConfigurationTime = Time.timeSinceLevelLoad,
                InitializationTime = Time.timeSinceLevelLoad,
                DependencyCount = 4, // Manager, Utility, Data, Null providers
                ComponentCount = 4
            };
        }

        protected override void InitializeServiceComponents()
        {
            base.InitializeServiceComponents();

            // Initialize service providers
            InitializeServiceProviders();

            // Configure Chimera-specific service provider settings
            ConfigureChimeraServiceProviders();
        }

        private void InitializeServiceProviders()
        {
            // Create service providers
            if (_enableManagerRegistration)
                _managerRegistrationProvider = new ManagerRegistrationProvider();

            if (_enableUtilityServices)
                _utilityServiceProvider = new UtilityServiceProvider();

            if (_enableDataServices)
                _dataServiceProvider = new DataServiceProvider();

            if (_enableNullImplementations)
                _nullImplementationProvider = new NullImplementationProvider();
        }

        private void ConfigureChimeraServiceProviders()
        {
            LogModuleAction("Configuring Chimera service providers");

            // Configure each provider if enabled
            if (_managerRegistrationProvider != null)
                _managerRegistrationProvider.ConfigureProvider();

            if (_utilityServiceProvider != null)
                _utilityServiceProvider.ConfigureProvider();

            if (_dataServiceProvider != null)
                _dataServiceProvider.ConfigureProvider();

            if (_nullImplementationProvider != null)
                _nullImplementationProvider.ConfigureProvider();
        }

        protected override void ConfigureServiceProviders(IServiceContainer serviceContainer)
        {
            LogModuleAction("Configuring Chimera service providers");

            // Configure manager interfaces if enabled
            if (_enableManagerRegistration && _managerRegistrationProvider != null)
            {
                _managerRegistrationProvider.RegisterManagerInterfaces(serviceContainer);
            }

            // Configure utility services if enabled
            if (_enableUtilityServices && _utilityServiceProvider != null)
            {
                _utilityServiceProvider.RegisterUtilityServices(serviceContainer);
            }

            // Configure data services if enabled
            if (_enableDataServices && _dataServiceProvider != null)
            {
                _dataServiceProvider.RegisterDataServices(serviceContainer);
            }

            // Configure null implementations if enabled
            if (_enableNullImplementations && _nullImplementationProvider != null)
            {
                _nullImplementationProvider.RegisterNullImplementations(serviceContainer);
            }
        }

        protected override void InitializeServiceProviders(IServiceContainer serviceContainer)
        {
            LogModuleAction("Initializing Chimera service providers");

            // Initialize manager registration provider
            if (_enableManagerRegistration && _managerRegistrationProvider != null)
            {
                _managerRegistrationProvider.InitializeServices(serviceContainer);
            }

            // Initialize utility service provider
            if (_enableUtilityServices && _utilityServiceProvider != null)
            {
                _utilityServiceProvider.InitializeServices(serviceContainer);
            }

            // Initialize data service provider
            if (_enableDataServices && _dataServiceProvider != null)
            {
                _dataServiceProvider.InitializeServices(serviceContainer);
            }

            // Initialize null implementation provider
            if (_enableNullImplementations && _nullImplementationProvider != null)
            {
                _nullImplementationProvider.InitializeServices(serviceContainer);
            }
        }

        protected override void ValidateServiceProviders(IServiceContainer serviceContainer)
        {
            bool allValid = true;

            // Validate manager registration provider
            if (_enableManagerRegistration && _managerRegistrationProvider != null)
            {
                allValid &= _managerRegistrationProvider.ValidateServices(serviceContainer);
            }

            // Validate utility service provider
            if (_enableUtilityServices && _utilityServiceProvider != null)
            {
                allValid &= _utilityServiceProvider.ValidateServices(serviceContainer);
            }

            // Validate data service provider
            if (_enableDataServices && _dataServiceProvider != null)
            {
                allValid &= _dataServiceProvider.ValidateServices(serviceContainer);
            }

            // Validate null implementation provider
            if (_enableNullImplementations && _nullImplementationProvider != null)
            {
                allValid &= _nullImplementationProvider.ValidateServices(serviceContainer);
            }

            if (!allValid)
            {
                ChimeraLogger.LogWarning("Service Validation", "Some services failed validation", this);
            }
        }

        private void ConfigureChimeraServiceProviders()
        {
            // Configure manager registration provider settings
            if (_managerRegistrationProvider != null)
            {
                // Enable all manager categories by default for Chimera
                // This could be made configurable in the future
            }

            // Configure utility service provider settings
            if (_utilityServiceProvider != null)
            {
                // Enable all utility service categories by default
            }

            // Configure data service provider settings
            if (_dataServiceProvider != null)
            {
                // Enable all data service categories by default
            }

            // Configure null implementation provider settings
            if (_nullImplementationProvider != null)
            {
                // Enable all null implementation categories by default
            }
        }

        /// <summary>
        /// Legacy support method for backward compatibility
        /// </summary>
        public ManagerRegistrationProvider.ManagerRegistrationStats GetManagerRegistrationStats()
        {
            return _managerRegistrationProvider?.GetRegistrationStats() ?? new ManagerRegistrationProvider.ManagerRegistrationStats();
        }

        /// <summary>
        /// Legacy support method for backward compatibility
        /// </summary>
        public UtilityServiceProvider.UtilityServiceStats GetUtilityServiceStats()
        {
            return _utilityServiceProvider?.GetUtilityStats() ?? new UtilityServiceProvider.UtilityServiceStats();
        }

        /// <summary>
        /// Legacy support method for backward compatibility
        /// </summary>
        public DataServiceProvider.DataServiceStats GetDataServiceStats()
        {
            return _dataServiceProvider?.GetDataStats() ?? new DataServiceProvider.DataServiceStats();
        }

        /// <summary>
        /// Legacy support method for backward compatibility
        /// </summary>
        public NullImplementationProvider.NullImplementationStats GetNullImplementationStats()
        {
            return _nullImplementationProvider?.GetNullStats() ?? new NullImplementationProvider.NullImplementationStats();
        }

        /// <summary>
        /// Get manager registration provider
        /// </summary>
        public ManagerRegistrationProvider ManagerRegistrationProvider => _managerRegistrationProvider;

        /// <summary>
        /// Get utility service provider
        /// </summary>
        public UtilityServiceProvider UtilityServiceProvider => _utilityServiceProvider;

        /// <summary>
        /// Get data service provider
        /// </summary>
        public DataServiceProvider DataServiceProvider => _dataServiceProvider;

        /// <summary>
        /// Get null implementation provider
        /// </summary>
        public NullImplementationProvider NullImplementationProvider => _nullImplementationProvider;

        /// <summary>
        /// Get comprehensive service module metrics
        /// </summary>
        public ChimeraServiceModuleMetrics GetChimeraModuleMetrics()
        {
            var baseMetrics = GetModuleMetrics();

            return new ChimeraServiceModuleMetrics
            {
                ModuleName = baseMetrics.ModuleName,
                ModuleVersion = baseMetrics.ModuleVersion,
                IsConfigured = baseMetrics.IsConfigured,
                IsInitialized = baseMetrics.IsInitialized,
                ConfigurationTime = baseMetrics.ConfigurationTime,
                InitializationTime = baseMetrics.InitializationTime,
                DependencyCount = baseMetrics.DependencyCount,
                ComponentCount = baseMetrics.ComponentCount,
                ManagerRegistrationStats = GetManagerRegistrationStats(),
                UtilityServiceStats = GetUtilityServiceStats(),
                DataServiceStats = GetDataServiceStats(),
                NullImplementationStats = GetNullImplementationStats()
            };
        }

        #if UNITY_EDITOR
        /// <summary>
        /// Editor-only method for testing service module
        /// </summary>
        [UnityEngine.ContextMenu("Test Chimera Service Module")]
        private void TestChimeraServiceModule()
        {
            if (UnityEngine.Application.isPlaying)
            {
                LogModuleAction("Testing Chimera service module...");
                LogModuleAction($"Manager registration: {(_enableManagerRegistration ? "Enabled" : "Disabled")}");
                LogModuleAction($"Utility services: {(_enableUtilityServices ? "Enabled" : "Disabled")}");
                LogModuleAction($"Data services: {(_enableDataServices ? "Enabled" : "Disabled")}");
                LogModuleAction($"Null implementations: {(_enableNullImplementations ? "Enabled" : "Disabled")}");

                var metrics = GetChimeraModuleMetrics();
                LogModuleAction($"Module configured: {metrics.IsConfigured}");
                LogModuleAction($"Module initialized: {metrics.IsInitialized}");
                LogModuleAction($"Total components: {metrics.ComponentCount}");
                LogModuleAction($"Total manager registrations: {metrics.ManagerRegistrationStats.TotalRegistered}");
                LogModuleAction($"Total utility services: {metrics.UtilityServiceStats.TotalRegistered}");
                LogModuleAction($"Total data services: {metrics.DataServiceStats.TotalRegistered}");
                LogModuleAction($"Total null implementations: {metrics.NullImplementationStats.TotalRegistered}");
            }
            else
            {
                ChimeraLogger.Log("[ChimeraServiceModule] Test only works during play mode");
            }
        }
        #endif

        /// <summary>
        /// Chimera service module metrics data structure
        /// </summary>
        public class ChimeraServiceModuleMetrics : ServiceModuleMetrics
        {
            public ManagerRegistrationProvider.ManagerRegistrationStats ManagerRegistrationStats { get; set; }
            public UtilityServiceProvider.UtilityServiceStats UtilityServiceStats { get; set; }
            public DataServiceProvider.DataServiceStats DataServiceStats { get; set; }
            public NullImplementationProvider.NullImplementationStats NullImplementationStats { get; set; }
        }
    }
}
