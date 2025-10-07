
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core.ManagerRegistration
{
    /// <summary>
    /// FOCUSED: Asset service registration for Project Chimera
    /// Single responsibility: Register asset and update orchestration services
    /// Extracted from ManagerRegistrationProvider.cs for SRP compliance (Week 8)
    /// </summary>
    public class AssetServiceRegistration
    {
        private int _registeredAssetServices = 0;
        private int _registeredUpdateOrchestrator = 0;

        /// <summary>
        /// Register asset service interfaces
        /// </summary>
        public int RegisterAssetServiceInterfaces(IServiceContainer serviceContainer)
        {
            if (serviceContainer == null) return 0;

            _registeredAssetServices = 0;

            ChimeraLogger.LogInfo("AssetServiceRegistration", "$1");

            // Register ISchematicAssetService
            if (!serviceContainer.IsRegistered<ISchematicAssetService>())
            {
                serviceContainer.RegisterInstance<ISchematicAssetService>(new NullSchematicAssetService());
                _registeredAssetServices++;
                ChimeraLogger.LogInfo("AssetServiceRegistration", "$1");
            }

            ChimeraLogger.LogInfo("AssetServiceRegistration", "$1");
            return _registeredAssetServices;
        }

        /// <summary>
        /// Register update orchestrator interface
        /// </summary>
        public int RegisterUpdateOrchestratorInterface(IServiceContainer serviceContainer)
        {
            if (serviceContainer == null) return 0;

            _registeredUpdateOrchestrator = 0;

            ChimeraLogger.LogInfo("AssetServiceRegistration", "$1");

            // Register IUpdateOrchestrator
            if (!serviceContainer.IsRegistered<IUpdateOrchestrator>())
            {
                serviceContainer.RegisterInstance<IUpdateOrchestrator>(new NullUpdateOrchestrator());
                _registeredUpdateOrchestrator++;
                ChimeraLogger.LogInfo("AssetServiceRegistration", "$1");
            }

            ChimeraLogger.LogInfo("AssetServiceRegistration", "$1");
            return _registeredUpdateOrchestrator;
        }

        /// <summary>
        /// Get count of registered asset services
        /// </summary>
        public int GetAssetServiceCount()
        {
            return _registeredAssetServices;
        }

        /// <summary>
        /// Get count of registered update orchestrator
        /// </summary>
        public int GetUpdateOrchestratorCount()
        {
            return _registeredUpdateOrchestrator;
        }

        /// <summary>
        /// Get total registration count
        /// </summary>
        public int GetTotalCount()
        {
            return _registeredAssetServices + _registeredUpdateOrchestrator;
        }

        /// <summary>
        /// Check if asset services are properly registered
        /// </summary>
        public bool ValidateAssetServices(IServiceContainer serviceContainer)
        {
            if (serviceContainer == null) return false;

            var isValid = serviceContainer.IsRegistered<ISchematicAssetService>();

            ChimeraLogger.LogInfo("AssetServiceRegistration", "$1");
            return isValid;
        }

        /// <summary>
        /// Check if update orchestrator is properly registered
        /// </summary>
        public bool ValidateUpdateOrchestrator(IServiceContainer serviceContainer)
        {
            if (serviceContainer == null) return false;

            var isValid = serviceContainer.IsRegistered<IUpdateOrchestrator>();

            ChimeraLogger.LogInfo("AssetServiceRegistration", "$1");
            return isValid;
        }
    }
}
