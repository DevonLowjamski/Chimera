
using ProjectChimera.Core.Logging;
using ProjectChimera.Core.Interfaces;

namespace ProjectChimera.Core.ManagerRegistration
{
    /// <summary>
    /// FOCUSED: Construction manager registration for Project Chimera
    /// Single responsibility: Register construction and building-related managers
    /// Extracted from ManagerRegistrationProvider.cs for SRP compliance (Week 8)
    /// </summary>
    public class ConstructionManagerRegistration
    {
        private int _registeredCount = 0;

        /// <summary>
        /// Register all construction manager interfaces with fallback implementations
        /// </summary>
        public int RegisterConstructionManagerInterfaces(IServiceContainer serviceContainer)
        {
            if (serviceContainer == null) return 0;

            _registeredCount = 0;

            ChimeraLogger.LogInfo("ConstructionManagerRegistration", "$1");

            // Register IGridPlacementController
            if (!serviceContainer.IsRegistered<IGridPlacementController>())
            {
                serviceContainer.RegisterInstance<IGridPlacementController>(new NullGridPlacementController());
                _registeredCount++;
                ChimeraLogger.LogInfo("ConstructionManagerRegistration", "$1");
            }

            // Register IGridSystem
            if (!serviceContainer.IsRegistered<IGridSystem>())
            {
                serviceContainer.RegisterInstance<IGridSystem>(new NullGridSystem());
                _registeredCount++;
                ChimeraLogger.LogInfo("ConstructionManagerRegistration", "$1");
            }

            // Register IInteractiveFacilityConstructor
            if (!serviceContainer.IsRegistered<IInteractiveFacilityConstructor>())
            {
                serviceContainer.RegisterInstance<IInteractiveFacilityConstructor>(new NullInteractiveFacilityConstructor());
                _registeredCount++;
                ChimeraLogger.LogInfo("ConstructionManagerRegistration", "$1");
            }

            // Register IConstructionCostManager
            if (!serviceContainer.IsRegistered<IConstructionCostManager>())
            {
                serviceContainer.RegisterInstance<IConstructionCostManager>(new NullConstructionCostManager());
                _registeredCount++;
                ChimeraLogger.LogInfo("ConstructionManagerRegistration", "$1");
            }

            ChimeraLogger.LogInfo("ConstructionManagerRegistration", "$1");
            return _registeredCount;
        }

        /// <summary>
        /// Get count of registered construction managers
        /// </summary>
        public int GetRegisteredCount()
        {
            return _registeredCount;
        }

        /// <summary>
        /// Check if construction managers are properly registered
        /// </summary>
        public bool ValidateRegistrations(IServiceContainer serviceContainer)
        {
            if (serviceContainer == null) return false;

            var isValid = serviceContainer.IsRegistered<IGridPlacementController>() &&
                         serviceContainer.IsRegistered<IGridSystem>() &&
                         serviceContainer.IsRegistered<IInteractiveFacilityConstructor>() &&
                         serviceContainer.IsRegistered<IConstructionCostManager>();

            ChimeraLogger.LogInfo("ConstructionManagerRegistration", "$1");
            return isValid;
        }
    }
}