
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core.ManagerRegistration
{
    /// <summary>
    /// FOCUSED: Core manager registration for Project Chimera
    /// Single responsibility: Register core infrastructure managers (Time, Data, Event, Settings)
    /// Extracted from ManagerRegistrationProvider.cs for SRP compliance (Week 8)
    /// </summary>
    public class CoreManagerRegistration
    {
        private int _registeredCount = 0;

        /// <summary>
        /// Register all core manager interfaces with fallback implementations
        /// </summary>
        public int RegisterCoreManagerInterfaces(IServiceContainer serviceContainer)
        {
            if (serviceContainer == null) return 0;

            _registeredCount = 0;

            ChimeraLogger.LogInfo("CoreManagerRegistration", "$1");

            // Register ITimeManager
            if (!serviceContainer.IsRegistered<ITimeManager>())
            {
                serviceContainer.RegisterInstance<ITimeManager>(new TimeManager());
                _registeredCount++;
                ChimeraLogger.LogInfo("CoreManagerRegistration", "$1");
            }

            // Register IDataManager
            if (!serviceContainer.IsRegistered<IDataManager>())
            {
                serviceContainer.RegisterInstance<IDataManager>(new NullDataManager());
                _registeredCount++;
                ChimeraLogger.LogInfo("CoreManagerRegistration", "$1");
            }

            // Register IEventManager
            if (!serviceContainer.IsRegistered<IEventManager>())
            {
                serviceContainer.RegisterInstance<IEventManager>(new InMemoryEventManager());
                _registeredCount++;
                ChimeraLogger.LogInfo("CoreManagerRegistration", "$1");
            }

            // Register ISettingsManager
            if (!serviceContainer.IsRegistered<ISettingsManager>())
            {
                serviceContainer.RegisterInstance<ISettingsManager>(new PlayerPrefsSettingsManager());
                _registeredCount++;
                ChimeraLogger.LogInfo("CoreManagerRegistration", "$1");
            }

            ChimeraLogger.LogInfo("CoreManagerRegistration", "$1");
            return _registeredCount;
        }

        /// <summary>
        /// Get count of registered core managers
        /// </summary>
        public int GetRegisteredCount()
        {
            return _registeredCount;
        }

        /// <summary>
        /// Check if core managers are properly registered
        /// </summary>
        public bool ValidateRegistrations(IServiceContainer serviceContainer)
        {
            if (serviceContainer == null) return false;

            var isValid = serviceContainer.IsRegistered<ITimeManager>() &&
                         serviceContainer.IsRegistered<IDataManager>() &&
                         serviceContainer.IsRegistered<IEventManager>() &&
                         serviceContainer.IsRegistered<ISettingsManager>();

            ChimeraLogger.LogInfo("CoreManagerRegistration", "$1");
            return isValid;
        }
    }
}