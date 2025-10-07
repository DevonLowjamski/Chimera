using System;
using UnityEngine;

using ProjectChimera.Core.Logging;
using ProjectChimera.Core.ManagerRegistration;

namespace ProjectChimera.Core
{
    /// <summary>
    /// REFACTORED: Manager registration provider orchestrating focused registration components (Week 8)
    /// Delegates to CoreManagerRegistration, UIManagerRegistration, ConstructionManagerRegistration,
    /// DomainManagerRegistration, and AssetServiceRegistration for SRP compliance
    /// </summary>
    public class ManagerRegistrationProvider : ServiceProviderBase
    {
        [Header("Manager Registration Settings")]
        [SerializeField] private bool _registerCoreManagers = true;
        [SerializeField] private bool _registerUIManagers = true;
        [SerializeField] private bool _registerConstructionManagers = true;
        [SerializeField] private bool _registerDomainManagers = true;
        [SerializeField] private bool _registerAssetServices = true;
        [SerializeField] private bool _registerUpdateOrchestrator = true;

        // Focused registration components (SRP compliance)
        private readonly CoreManagerRegistration _coreManagerRegistration = new CoreManagerRegistration();
        private readonly UIManagerRegistration _uiManagerRegistration = new UIManagerRegistration();
        private readonly ConstructionManagerRegistration _constructionManagerRegistration = new ConstructionManagerRegistration();
        private readonly DomainManagerRegistration _domainManagerRegistration = new DomainManagerRegistration();
        private readonly AssetServiceRegistration _assetServiceRegistration = new AssetServiceRegistration();

        // Registration counters for stats
        private int _registeredCoreManagers = 0;
        private int _registeredUIManagers = 0;
        private int _registeredConstructionManagers = 0;
        private int _registeredDomainManagers = 0;
        private int _registeredAssetServices = 0;
        private int _registeredUpdateOrchestrator = 0;

        // Logging methods
        private void LogProviderAction(string action)
        {
            ChimeraLogger.LogInfo("ManagerRegistrationProvider", "$1");
        }

        private void LogProviderError(string error)
        {
            ChimeraLogger.LogInfo("ManagerRegistrationProvider", "$1");
        }

        private void LogProviderWarning(string warning)
        {
            ChimeraLogger.LogInfo("ManagerRegistrationProvider", "$1");
        }

        // Configuration method
        public void ConfigureProvider()
        {
            LogProviderAction("Configuring ManagerRegistrationProvider");
            // Configuration logic can be added here if needed
        }

        public override void RegisterManagerInterfaces(IServiceContainer serviceContainer)
        {
            LogProviderAction("Starting manager interface registration with focused components");

            try
            {
                var totalRegistered = 0;

                // Register core manager interfaces using focused component
                if (_registerCoreManagers)
                {
                    var count = _coreManagerRegistration.RegisterCoreManagerInterfaces(serviceContainer);
                    totalRegistered += count;
                    _registeredCoreManagers += count;
                }

                // Register UI system interfaces using focused component
                if (_registerUIManagers)
                {
                    var count = _uiManagerRegistration.RegisterUIManagerInterfaces(serviceContainer);
                    totalRegistered += count;
                    _registeredUIManagers += count;
                }

                // Register construction system interfaces using focused component
                if (_registerConstructionManagers)
                {
                    var count = _constructionManagerRegistration.RegisterConstructionManagerInterfaces(serviceContainer);
                    totalRegistered += count;
                    _registeredConstructionManagers += count;
                }

                // Register domain-specific manager interfaces using focused component
                if (_registerDomainManagers)
                {
                    var count = _domainManagerRegistration.RegisterDomainManagerInterfaces(serviceContainer);
                    totalRegistered += count;
                    _registeredDomainManagers += count;
                }

                // Register asset service interfaces using focused component
                if (_registerAssetServices)
                {
                    var count = _assetServiceRegistration.RegisterAssetServiceInterfaces(serviceContainer);
                    totalRegistered += count;
                    _registeredAssetServices += count;
                }

                // Register update orchestrator using focused component
                if (_registerUpdateOrchestrator)
                {
                    var count = _assetServiceRegistration.RegisterUpdateOrchestratorInterface(serviceContainer);
                    totalRegistered += count;
                    _registeredUpdateOrchestrator += count;
                }

                LogProviderAction($"Manager interface registration completed: {totalRegistered} interfaces registered");
            }
            catch (Exception ex)
            {
                LogProviderError($"Error registering manager interfaces: {ex.Message}");
                throw;
            }
        }



        public override void RegisterUtilityServices(IServiceContainer serviceContainer)
        {
            // This provider doesn't handle utility services
        }

        public override void RegisterDataServices(IServiceContainer serviceContainer)
        {
            // This provider doesn't handle data services
        }

        public override void RegisterNullImplementations(IServiceContainer serviceContainer)
        {
            // Null implementations are registered as part of manager interface registration
        }

        public override void InitializeServices(IServiceContainer serviceContainer)
        {
            LogProviderAction("Initializing manager registration services");

            try
            {
                // Validate manager registrations
                ValidateManagerRegistrations(serviceContainer);

                LogProviderAction("Manager registration services initialized successfully");
            }
            catch (Exception ex)
            {
                LogProviderError($"Error initializing manager registration services: {ex.Message}");
                throw;
            }
        }

        public override void ValidateServices(IServiceContainer serviceContainer)
        {
            LogProviderAction("Validating manager registration services");

            try
            {
                bool allValid = true;

                // Validate core managers using focused component
                if (_registerCoreManagers)
                {
                    allValid &= _coreManagerRegistration.ValidateRegistrations(serviceContainer);
                }

                // Validate UI managers using focused component
                if (_registerUIManagers)
                {
                    allValid &= _uiManagerRegistration.ValidateRegistrations(serviceContainer);
                }

                // Validate construction managers using focused component
                if (_registerConstructionManagers)
                {
                    allValid &= _constructionManagerRegistration.ValidateRegistrations(serviceContainer);
                }

                // Validate domain managers using focused component
                if (_registerDomainManagers)
                {
                    allValid &= _domainManagerRegistration.ValidateRegistrations(serviceContainer);
                }

                // Validate asset services using focused component
                if (_registerAssetServices)
                {
                    allValid &= _assetServiceRegistration.ValidateAssetServices(serviceContainer);
                }

                // Validate update orchestrator using focused component
                if (_registerUpdateOrchestrator)
                {
                    allValid &= _assetServiceRegistration.ValidateUpdateOrchestrator(serviceContainer);
                }

                if (allValid)
                {
                    LogProviderAction("All manager registration services validated successfully");
                }
                else
                {
                    LogProviderWarning("Some manager registration services failed validation");
                }
            }
            catch (Exception ex)
            {
                LogProviderError($"Error validating manager registration services: {ex.Message}");
            }
        }

        private void ValidateManagerRegistrations(IServiceContainer serviceContainer)
        {
            // Validate that factory registrations work correctly
            var testResolve = serviceContainer.TryResolve<ITimeManager>();
            if (testResolve == null)
            {
                throw new InvalidOperationException("Manager factory registration validation failed");
            }
        }


        private bool ValidateManagerService<T>(IServiceContainer serviceContainer, string serviceName) where T : class
        {
            try
            {
                var service = serviceContainer.TryResolve<T>();
                if (service != null)
                {
                    LogProviderAction($"Manager service '{serviceName}' validated successfully");
                    return true;
                }
                else
                {
                    LogProviderWarning($"Manager service '{serviceName}' not available");
                    return false;
                }
            }
            catch (Exception ex)
            {
                LogProviderError($"Error validating manager service '{serviceName}': {ex.Message}");
                return false;
            }
        }

        private bool ValidateAllResults(bool[] results, string category)
        {
            bool allValid = true;
            foreach (var result in results)
            {
                allValid &= result;
            }

            if (!allValid)
            {
                LogProviderWarning($"{category} validation had failures");
            }

            return allValid;
        }

        /// <summary>
        /// Get manager registration statistics
        /// </summary>
        public ManagerRegistrationStats GetRegistrationStats()
        {
            return new ManagerRegistrationStats
            {
                CoreManagers = _registeredCoreManagers,
                UIManagers = _registeredUIManagers,
                ConstructionManagers = _registeredConstructionManagers,
                DomainManagers = _registeredDomainManagers,
                AssetServices = _registeredAssetServices,
                UpdateOrchestrator = _registeredUpdateOrchestrator,
                TotalRegistered = _registeredCoreManagers + _registeredUIManagers +
                                _registeredConstructionManagers + _registeredDomainManagers +
                                _registeredAssetServices + _registeredUpdateOrchestrator
            };
        }

        /// <summary>
        /// Manager registration statistics
        /// </summary>
        public class ManagerRegistrationStats
        {
            public int CoreManagers { get; set; }
            public int UIManagers { get; set; }
            public int ConstructionManagers { get; set; }
            public int DomainManagers { get; set; }
            public int AssetServices { get; set; }
            public int UpdateOrchestrator { get; set; }
            public int TotalRegistered { get; set; }
        }
    }
}
