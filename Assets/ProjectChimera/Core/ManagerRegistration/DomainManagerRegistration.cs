
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core.ManagerRegistration
{
    /// <summary>
    /// FOCUSED: Domain manager registration for Project Chimera
    /// Single responsibility: Register game domain managers (Plant, Genetics, Environmental, Economy, etc.)
    /// Extracted from ManagerRegistrationProvider.cs for SRP compliance (Week 8)
    /// </summary>
    public class DomainManagerRegistration
    {
        private int _registeredCount = 0;

        /// <summary>
        /// Register all domain manager interfaces with fallback implementations
        /// </summary>
        public int RegisterDomainManagerInterfaces(IServiceContainer serviceContainer)
        {
            if (serviceContainer == null) return 0;

            _registeredCount = 0;

            ChimeraLogger.LogInfo("DomainManagerRegistration", "$1");

            // Register IPlantManager
            if (!serviceContainer.IsRegistered<IPlantManager>())
            {
                serviceContainer.RegisterInstance<IPlantManager>(new NullPlantManager());
                _registeredCount++;
                ChimeraLogger.LogInfo("DomainManagerRegistration", "$1");
            }

            // Register IGeneticsManager
            if (!serviceContainer.IsRegistered<IGeneticsManager>())
            {
                serviceContainer.RegisterInstance<IGeneticsManager>(new NullGeneticsManager());
                _registeredCount++;
                ChimeraLogger.LogInfo("DomainManagerRegistration", "$1");
            }

            // Register IEnvironmentalManager
            if (!serviceContainer.IsRegistered<IEnvironmentalManager>())
            {
                serviceContainer.RegisterInstance<IEnvironmentalManager>(new NullEnvironmentalManager());
                _registeredCount++;
                ChimeraLogger.LogInfo("DomainManagerRegistration", "$1");
            }

            // Register IEconomyManager
            if (!serviceContainer.IsRegistered<IEconomyManager>())
            {
                serviceContainer.RegisterInstance<IEconomyManager>(new NullEconomyManager());
                _registeredCount++;
                ChimeraLogger.LogInfo("DomainManagerRegistration", "$1");
            }

            // Register IProgressionManager
            if (!serviceContainer.IsRegistered<IProgressionManager>())
            {
                serviceContainer.RegisterInstance<IProgressionManager>(new NullProgressionManager());
                _registeredCount++;
                ChimeraLogger.LogInfo("DomainManagerRegistration", "$1");
            }

            // Register IResearchManager
            if (!serviceContainer.IsRegistered<IResearchManager>())
            {
                serviceContainer.RegisterInstance<IResearchManager>(new NullResearchManager());
                _registeredCount++;
                ChimeraLogger.LogInfo("DomainManagerRegistration", "$1");
            }

            // Register IAudioManager
            if (!serviceContainer.IsRegistered<IAudioManager>())
            {
                serviceContainer.RegisterInstance<IAudioManager>(new NullAudioManager());
                _registeredCount++;
                ChimeraLogger.LogInfo("DomainManagerRegistration", "$1");
            }

            ChimeraLogger.LogInfo("DomainManagerRegistration", "$1");
            return _registeredCount;
        }

        /// <summary>
        /// Get count of registered domain managers
        /// </summary>
        public int GetRegisteredCount()
        {
            return _registeredCount;
        }

        /// <summary>
        /// Check if domain managers are properly registered
        /// </summary>
        public bool ValidateRegistrations(IServiceContainer serviceContainer)
        {
            if (serviceContainer == null) return false;

            var isValid = serviceContainer.IsRegistered<IPlantManager>() &&
                         serviceContainer.IsRegistered<IGeneticsManager>() &&
                         serviceContainer.IsRegistered<IEnvironmentalManager>() &&
                         serviceContainer.IsRegistered<IEconomyManager>() &&
                         serviceContainer.IsRegistered<IProgressionManager>() &&
                         serviceContainer.IsRegistered<IResearchManager>() &&
                         serviceContainer.IsRegistered<IAudioManager>();

            ChimeraLogger.LogInfo("DomainManagerRegistration", "$1");
            return isValid;
        }
    }
}