
using ProjectChimera.Core.Logging;

namespace ProjectChimera.Core.ManagerRegistration
{
    /// <summary>
    /// FOCUSED: UI manager registration for Project Chimera
    /// Single responsibility: Register UI and interface managers
    /// Extracted from ManagerRegistrationProvider.cs for SRP compliance (Week 8)
    /// </summary>
    public class UIManagerRegistration
    {
        private int _registeredCount = 0;

        /// <summary>
        /// Register all UI manager interfaces with fallback implementations
        /// </summary>
        public int RegisterUIManagerInterfaces(IServiceContainer serviceContainer)
        {
            if (serviceContainer == null) return 0;

            _registeredCount = 0;

            ChimeraLogger.LogInfo("UIManagerRegistration", "$1");

            // Register IUIManager
            if (!serviceContainer.IsRegistered<IUIManager>())
            {
                serviceContainer.RegisterInstance<IUIManager>(new NullUIManager());
                _registeredCount++;
                ChimeraLogger.LogInfo("UIManagerRegistration", "$1");
            }

            // Register ISchematicLibraryPanel
            if (!serviceContainer.IsRegistered<ISchematicLibraryPanel>())
            {
                serviceContainer.RegisterInstance<ISchematicLibraryPanel>(new NullSchematicLibraryPanel());
                _registeredCount++;
                ChimeraLogger.LogInfo("UIManagerRegistration", "$1");
            }

            // Register IConstructionPaletteManager (UI-related)
            if (!serviceContainer.IsRegistered<IConstructionPaletteManager>())
            {
                serviceContainer.RegisterInstance<IConstructionPaletteManager>(new NullConstructionPaletteManager());
                _registeredCount++;
                ChimeraLogger.LogInfo("UIManagerRegistration", "$1");
            }

            ChimeraLogger.LogInfo("UIManagerRegistration", "$1");
            return _registeredCount;
        }

        /// <summary>
        /// Get count of registered UI managers
        /// </summary>
        public int GetRegisteredCount()
        {
            return _registeredCount;
        }

        /// <summary>
        /// Check if UI managers are properly registered
        /// </summary>
        public bool ValidateRegistrations(IServiceContainer serviceContainer)
        {
            if (serviceContainer == null) return false;

            var isValid = serviceContainer.IsRegistered<IUIManager>() &&
                         serviceContainer.IsRegistered<ISchematicLibraryPanel>() &&
                         serviceContainer.IsRegistered<IConstructionPaletteManager>();

            ChimeraLogger.LogInfo("UIManagerRegistration", "$1");
            return isValid;
        }
    }
}