using System;
using UnityEngine;
using ProjectChimera.Core.DependencyInjection;
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core
{
    /// <summary>
    /// Handles registration of manager interfaces with factory methods and dependency injection.
    /// Provides fallback implementations and manages interface-based service resolution.
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

        // Manager registration state
        private int _registeredCoreManagers = 0;
        private int _registeredUIManagers = 0;
        private int _registeredConstructionManagers = 0;
        private int _registeredDomainManagers = 0;
        private int _registeredAssetServices = 0;
        private int _registeredUpdateOrchestrator = 0;

        // Logging methods
        private void LogProviderAction(string action)
        {
            ChimeraLogger.Log("CORE", $"ManagerRegistrationProvider: {action}", this);
        }

        private void LogProviderError(string error)
        {
            ChimeraLogger.LogError("CORE", $"ManagerRegistrationProvider Error: {error}", this);
        }

        private void LogProviderWarning(string warning)
        {
            ChimeraLogger.LogWarning("CORE", $"ManagerRegistrationProvider Warning: {warning}", this);
        }

        // Configuration method
        public void ConfigureProvider()
        {
            LogProviderAction("Configuring ManagerRegistrationProvider");
            // Configuration logic can be added here if needed
        }

        public override void RegisterManagerInterfaces(IServiceContainer serviceContainer)
        {
            LogProviderAction("Starting manager interface registration");

            try
            {
                // Register core manager interfaces
                if (_registerCoreManagers)
                {
                    RegisterCoreManagerInterfaces(serviceContainer);
                }

                // Register UI system interfaces
                if (_registerUIManagers)
                {
                    RegisterUIManagerInterfaces(serviceContainer);
                }

                // Register construction system interfaces
                if (_registerConstructionManagers)
                {
                    RegisterConstructionManagerInterfaces(serviceContainer);
                }

                // Register domain-specific manager interfaces
                if (_registerDomainManagers)
                {
                    RegisterDomainManagerInterfaces(serviceContainer);
                }

                // Register asset service interfaces
                if (_registerAssetServices)
                {
                    RegisterAssetServiceInterfaces(serviceContainer);
                }

                // Register update orchestrator
                if (_registerUpdateOrchestrator)
                {
                    RegisterUpdateOrchestratorInterface(serviceContainer);
                }

                var totalRegistered = _registeredCoreManagers + _registeredUIManagers +
                                    _registeredConstructionManagers + _registeredDomainManagers +
                                    _registeredAssetServices + _registeredUpdateOrchestrator;

                LogProviderAction($"Manager interface registration completed: {totalRegistered} interfaces registered");
            }
            catch (Exception ex)
            {
                LogProviderError($"Error registering manager interfaces: {ex.Message}");
                throw;
            }
        }

        private void RegisterCoreManagerInterfaces(IServiceContainer serviceContainer)
        {
            LogProviderAction("Registering core manager interfaces");

            // Core Manager Interfaces
            // Use existing TimeManager if available, otherwise create a simple null implementation
            if (!serviceContainer.IsRegistered<ITimeManager>())
            {
                serviceContainer.RegisterInstance<ITimeManager>(new TimeManager());
            }
            _registeredCoreManagers++;

            if (!serviceContainer.IsRegistered<IDataManager>())
            {
                serviceContainer.RegisterInstance<IDataManager>(new NullDataManager());
            }
            _registeredCoreManagers++;

            if (!serviceContainer.IsRegistered<IEventManager>())
            {
                serviceContainer.RegisterInstance<IEventManager>(new InMemoryEventManager());
            }
            _registeredCoreManagers++;

            if (!serviceContainer.IsRegistered<ISettingsManager>())
            {
                serviceContainer.RegisterInstance<ISettingsManager>(new PlayerPrefsSettingsManager());
            }
            _registeredCoreManagers++;

            LogProviderAction($"Core manager interfaces registered: {_registeredCoreManagers}");
        }

        private void RegisterUIManagerInterfaces(IServiceContainer serviceContainer)
        {
            LogProviderAction("Registering UI manager interfaces");

            // UI System Interfaces
            if (!serviceContainer.IsRegistered<IUIManager>())
            {
                serviceContainer.RegisterInstance<IUIManager>(new NullUIManager());
            }
            _registeredUIManagers++;

            if (!serviceContainer.IsRegistered<ISchematicLibraryPanel>())
            {
                serviceContainer.RegisterInstance<ISchematicLibraryPanel>(new NullSchematicLibraryPanel());
            }
            _registeredUIManagers++;

            if (!serviceContainer.IsRegistered<IConstructionPaletteManager>())
            {
                serviceContainer.RegisterInstance<IConstructionPaletteManager>(new NullConstructionPaletteManager());
            }
            _registeredUIManagers++;

            LogProviderAction($"UI manager interfaces registered: {_registeredUIManagers}");
        }

        private void RegisterConstructionManagerInterfaces(IServiceContainer serviceContainer)
        {
            LogProviderAction("Registering construction manager interfaces");

            // Construction System Interfaces
            if (!serviceContainer.IsRegistered<IGridPlacementController>())
            {
                serviceContainer.RegisterInstance<IGridPlacementController>(new NullGridPlacementController());
            }
            _registeredConstructionManagers++;

            if (!serviceContainer.IsRegistered<IGridSystem>())
            {
                serviceContainer.RegisterInstance<IGridSystem>(new NullGridSystem());
            }
            _registeredConstructionManagers++;

            if (!serviceContainer.IsRegistered<IInteractiveFacilityConstructor>())
            {
                serviceContainer.RegisterInstance<IInteractiveFacilityConstructor>(new NullInteractiveFacilityConstructor());
            }
            _registeredConstructionManagers++;

            if (!serviceContainer.IsRegistered<IConstructionCostManager>())
            {
                serviceContainer.RegisterInstance<IConstructionCostManager>(new NullConstructionCostManager());
            }
            _registeredConstructionManagers++;

            LogProviderAction($"Construction manager interfaces registered: {_registeredConstructionManagers}");
        }

        private void RegisterDomainManagerInterfaces(IServiceContainer serviceContainer)
        {
            LogProviderAction("Registering domain manager interfaces");

            // Domain-Specific Manager Interfaces
            if (!serviceContainer.IsRegistered<IPlantManager>())
            {
                serviceContainer.RegisterInstance<IPlantManager>(new NullPlantManager());
            }
            _registeredDomainManagers++;

            if (!serviceContainer.IsRegistered<IGeneticsManager>())
            {
                serviceContainer.RegisterInstance<IGeneticsManager>(new NullGeneticsManager());
            }
            _registeredDomainManagers++;

            if (!serviceContainer.IsRegistered<IEnvironmentalManager>())
            {
                serviceContainer.RegisterInstance<IEnvironmentalManager>(new NullEnvironmentalManager());
            }
            _registeredDomainManagers++;

            if (!serviceContainer.IsRegistered<IEconomyManager>())
            {
                serviceContainer.RegisterInstance<IEconomyManager>(new NullEconomyManager());
            }
            _registeredDomainManagers++;

            if (!serviceContainer.IsRegistered<IProgressionManager>())
            {
                serviceContainer.RegisterInstance<IProgressionManager>(new NullProgressionManager());
            }
            _registeredDomainManagers++;

            if (!serviceContainer.IsRegistered<IResearchManager>())
            {
                serviceContainer.RegisterInstance<IResearchManager>(new NullResearchManager());
            }
            _registeredDomainManagers++;

            if (!serviceContainer.IsRegistered<IAudioManager>())
            {
                serviceContainer.RegisterInstance<IAudioManager>(new NullAudioManager());
            }
            _registeredDomainManagers++;

            LogProviderAction($"Domain manager interfaces registered: {_registeredDomainManagers}");
        }

        private void RegisterAssetServiceInterfaces(IServiceContainer serviceContainer)
        {
            LogProviderAction("Registering asset service interfaces");

            // Asset Service Interfaces
            if (!serviceContainer.IsRegistered<ISchematicAssetService>())
            {
                serviceContainer.RegisterInstance<ISchematicAssetService>(new ResourcesBasedSchematicAssetService());
            }
            _registeredAssetServices++;

            LogProviderAction($"Asset service interfaces registered: {_registeredAssetServices}");
        }

        private void RegisterUpdateOrchestratorInterface(IServiceContainer serviceContainer)
        {
            LogProviderAction("Registering update orchestrator interface");

            // Update Management Interface
            if (!serviceContainer.IsRegistered<IUpdateOrchestrator>())
            {
                serviceContainer.RegisterInstance<IUpdateOrchestrator>(new NullUpdateOrchestrator());
            }
            _registeredUpdateOrchestrator++;

            LogProviderAction($"Update orchestrator interface registered: {_registeredUpdateOrchestrator}");
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

                // Validate core managers if registered
                if (_registerCoreManagers)
                {
                    allValid &= ValidateCoreManagers(serviceContainer);
                }

                // Validate UI managers if registered
                if (_registerUIManagers)
                {
                    allValid &= ValidateUIManagers(serviceContainer);
                }

                // Validate construction managers if registered
                if (_registerConstructionManagers)
                {
                    allValid &= ValidateConstructionManagers(serviceContainer);
                }

                // Validate domain managers if registered
                if (_registerDomainManagers)
                {
                    allValid &= ValidateDomainManagers(serviceContainer);
                }

                // Validate asset services if registered
                if (_registerAssetServices)
                {
                    allValid &= ValidateAssetServices(serviceContainer);
                }

                // Validate update orchestrator if registered
                if (_registerUpdateOrchestrator)
                {
                    allValid &= ValidateUpdateOrchestrator(serviceContainer);
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

        private bool ValidateCoreManagers(IServiceContainer serviceContainer)
        {
            var validationResults = new[]
            {
                ValidateManagerService<ITimeManager>(serviceContainer, "TimeManager"),
                ValidateManagerService<IDataManager>(serviceContainer, "DataManager"),
                ValidateManagerService<IEventManager>(serviceContainer, "EventManager"),
                ValidateManagerService<ISettingsManager>(serviceContainer, "SettingsManager")
            };

            return ValidateAllResults(validationResults, "Core Managers");
        }

        private bool ValidateUIManagers(IServiceContainer serviceContainer)
        {
            var validationResults = new[]
            {
                ValidateManagerService<IUIManager>(serviceContainer, "UIManager"),
                ValidateManagerService<ISchematicLibraryPanel>(serviceContainer, "SchematicLibraryPanel"),
                ValidateManagerService<IConstructionPaletteManager>(serviceContainer, "ConstructionPaletteManager")
            };

            return ValidateAllResults(validationResults, "UI Managers");
        }

        private bool ValidateConstructionManagers(IServiceContainer serviceContainer)
        {
            var validationResults = new[]
            {
                ValidateManagerService<IGridPlacementController>(serviceContainer, "GridPlacementController"),
                ValidateManagerService<IGridSystem>(serviceContainer, "GridSystem"),
                ValidateManagerService<IInteractiveFacilityConstructor>(serviceContainer, "InteractiveFacilityConstructor"),
                ValidateManagerService<IConstructionCostManager>(serviceContainer, "ConstructionCostManager")
            };

            return ValidateAllResults(validationResults, "Construction Managers");
        }

        private bool ValidateDomainManagers(IServiceContainer serviceContainer)
        {
            var validationResults = new[]
            {
                ValidateManagerService<IPlantManager>(serviceContainer, "PlantManager"),
                ValidateManagerService<IGeneticsManager>(serviceContainer, "GeneticsManager"),
                ValidateManagerService<IEnvironmentalManager>(serviceContainer, "EnvironmentalManager"),
                ValidateManagerService<IEconomyManager>(serviceContainer, "EconomyManager"),
                ValidateManagerService<IProgressionManager>(serviceContainer, "ProgressionManager"),
                ValidateManagerService<IResearchManager>(serviceContainer, "ResearchManager"),
                ValidateManagerService<IAudioManager>(serviceContainer, "AudioManager")
            };

            return ValidateAllResults(validationResults, "Domain Managers");
        }

        private bool ValidateAssetServices(IServiceContainer serviceContainer)
        {
            var validationResults = new[]
            {
                ValidateManagerService<ISchematicAssetService>(serviceContainer, "SchematicAssetService")
            };

            return ValidateAllResults(validationResults, "Asset Services");
        }

        private bool ValidateUpdateOrchestrator(IServiceContainer serviceContainer)
        {
            var validationResults = new[]
            {
                ValidateManagerService<IUpdateOrchestrator>(serviceContainer, "UpdateOrchestrator")
            };

            return ValidateAllResults(validationResults, "Update Orchestrator");
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
